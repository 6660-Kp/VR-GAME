using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand {
    public delegate void HandGrabEvent(Hand hand, Grabbable grabbable);

    public enum HandType {
        both,
        right,
        left
    }

    [RequireComponent(typeof(Rigidbody))]
    public class Hand : MonoBehaviour {

        [Header("Follow Settings")]
        [Tooltip("Follow target, the hand will always try to match this transforms rotation and position with rigidbody movements")]
        public Transform follow;
        public Vector3 followPositionOffset;
        [Tooltip("The maximum allowed velocity of the hand"), Min(0)]
        public float maxVelocity = 3;
        
        [Tooltip("Follow target speed (This will cause jittering if turned too high)"), Min(0)]
        public float followPositionStrength = 20;
        [Tooltip("Follow target rotation speed (This will cause jittering if turned too high)"), Min(0)]
        public float followRotationStrength = 20;
        [Tooltip("Returns hand to the target after this distance"), Min(0)]
        public float maxFollowDistance = 0.75f;


        [Header("Hand")]
        public bool left = false;
        [Tooltip("An empty GameObject that should be placed on the surface of the center of the palm")]
        public Transform palmTransform;
        [Tooltip("The amount of look assist rotation to apply each frame"), Min(0)]
        public float lookAssistSpeed = 1;
        [Tooltip("Amplifier for applied velocity on released object"), Min(0)]
        public float throwPower = 1.25f;
        [Tooltip("Maximum distance for pickup"), Min(0)]
        public float reachDistance = 0.3f;
        [Tooltip("Offsets the width of the grab area -- Turn this down for less weird grabs")]
        public float grabSpreadOffset = 0;
        [Tooltip("The number of seconds that the hand collision should ignore the released object\n (Good for increased placement precision and resolves clipping errors)"), Min(0)]
        public float ignoreReleaseTime = 0.25f;
        [Tooltip("Increase for closer finger results / Decrease for less physics checks - The number of steps the fingers take when bending between 0-1 to grab something")]
        public int fingerBendSteps = 100;

        [Header("Fingers")]
        public Finger[] fingers;


        [Header("Pose")]
        [Tooltip("Turn this on when you want to animate the hand or use other IK Drivers")]
        public bool disableIK = false;
        [Tooltip("How much the fingers sway from the velocity")]
        public float swayStrength = 0.7f;
        [Tooltip("This will offset each fingers bend (0 is no bend, 1 is full bend)")]
        public float gripOffset = 0.1f;

        float idealGrip = 1f;
        float currGrip = 1f;

#if UNITY_EDITOR
        [Header("Editor")]
        public bool showGizmos = false;
        [Tooltip("Turn this on to enable autograbbing for editor rigging")]
        public bool editorAutoGrab = false;
        [Tooltip("By default procedural grab uses the collider instead of the layer to grab - this will use layer instead")]
        public bool useLayerBasedAutoGrab = false;
#endif
        [HideInInspector]
        public bool freezePos = false;
        [HideInInspector]
        public bool freezeRot = false;

        internal GameObject lookingAtObj = null;
        internal Grabbable holdingObj = null;

        Transform moveTo;
        Quaternion rotationOffset;
        RaycastHit lastLookHit;
        Rigidbody body;
        FixedJoint joint1;
        FixedJoint joint2;
        bool squeezing = false;
        float triggerPoint;
        Vector3[] handRays;
        Transform grabPoint;
        bool grabbing = false;
        bool grabLocked;
        Vector3 preRenderPos;
        Quaternion preRenderRot;
        GrabbablePose grabPose;
        Vector3 palmOffset;
        bool teleporting;
        int handLayers;
        Vector3 startGrabPos;
        Quaternion startGrabRot;
        bool grabbingFrame = false;

        ///Events for all my programmers out there :)
	    public event HandGrabEvent OnBeforeGrabbed;
	    public event HandGrabEvent OnGrabbed;
        
	    public event HandGrabEvent OnBeforeReleased;
        public event HandGrabEvent OnReleased;

        public event HandGrabEvent OnForcedRelease;

        public event HandGrabEvent OnSqueezed;
        public event HandGrabEvent OnUnsqueezed;

        public event HandGrabEvent OnHighlight;
        public event HandGrabEvent OnStopHighlight;


        public void Start() {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            if(body.collisionDetectionMode == CollisionDetectionMode.Discrete)
                body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            moveTo = new GameObject().transform;
            moveTo.name = "HAND FOLLOW POINT";
            if(Camera.main != null && !Camera.main.GetComponent<HandStabilizer>())
                Camera.main.gameObject.AddComponent<HandStabilizer>();

            Initialize();
        }

        public void Initialize() {
            //This precalculates the rays so it has to do less math in realtime
            List<Vector3> rays = new List<Vector3>();
            for(int i = 0; i < 100; i++) {
                float ampI = Mathf.Pow(i, 1.05f + grabSpreadOffset/10f) / (Mathf.PI * 0.8f);
                rays.Add(Quaternion.Euler(0, Mathf.Cos(i) * ampI + 90, Mathf.Sin(i) * ampI) * -Vector3.right);
            }
            handRays = rays.ToArray();

            //Sets hand to layer "Hand"
            SetLayerRecursive(transform, LayerMask.NameToLayer("Hand"));
            
            //preretrieve layermask
            handLayers = LayerMask.GetMask("Hand", "HandHolding", "HandReleasing");
        }

        public void Update() {
            if(grabbing || body.isKinematic)
                return;

            //Sets [Move To] Object
            moveTo.position = follow.position + transform.rotation*followPositionOffset;
            moveTo.rotation = follow.rotation;

            //Does Look Assist
            if(lookAssistSpeed > 0)
                LookAssist();

            //Adjust the [Move To] based on held offsets 
            if(holdingObj != null) {
                moveTo.position += transform.rotation*holdingObj.heldPositionOffset;
                if(left){
                    var leftRot = -holdingObj.heldRotationOffset;
                    leftRot.x *= -1;
                    moveTo.localRotation *= Quaternion.Euler(leftRot);
                }
                else
                    moveTo.localRotation *= Quaternion.Euler(holdingObj.heldRotationOffset);

                moveTo.Rotate(moveTo.right, holdingObj.heldPositionOffset.x);
                moveTo.Rotate(moveTo.up, holdingObj.heldPositionOffset.y);
                moveTo.Rotate(moveTo.forward, holdingObj.heldPositionOffset.z);

                //This helps stabilize the hand while holding
                if(grabLocked && holdingObj.HeldCount() == 1) {
                    if(!freezePos)
                        transform.position = grabPoint.transform.position;
                    if(!freezeRot)
                        transform.rotation = grabPoint.transform.rotation;
                }
            }

            if(grabPose == null){
                DeterminPose();
            }
        }
        
        
        public void FixedUpdate(){
            if(grabbing || body.isKinematic)
                return;

            //Calls physics movements
            if(!freezePos) MoveTo();
            if(!freezeRot) TorqueTo();
            
            //Strongly stabilizes one handed holding
            if(holdingObj != null && grabLocked && holdingObj.HeldCount() == 1 && !teleporting) {
                if(!freezePos) transform.position = grabPoint.transform.position;
                if(!freezeRot) transform.rotation = grabPoint.transform.rotation;
            }
        }
        
        public void OnPreRender() {
            //Hides fixed joint jitterings
            if(holdingObj != null && grabLocked) {
                preRenderPos = transform.position;
                preRenderRot = transform.rotation;
                transform.position = grabPoint.transform.position;
                transform.rotation = grabPoint.transform.rotation;
            }
            //Hides weird grab flicker
            if(grabbingFrame) {
                preRenderPos = transform.position;
                preRenderRot = transform.rotation;
                transform.position = startGrabPos;
                transform.rotation = startGrabRot;
            }
        }
        
        public void OnPostRender() {
            //Return to physics position after frame has been rendered
            if((holdingObj != null && grabLocked) || grabbingFrame) {
                transform.position = preRenderPos;
                transform.rotation = preRenderRot;
            }
        }


        /// <summary>Moves the hand to the controller position using physics movement</summary>
        void MoveTo() {
            var movePos = moveTo.position;
            var distance = Vector3.Distance(movePos, transform.position);

            //Returns hand to controller if out of distance - for teleporting / hand being stuck in by physics
            if(distance > maxFollowDistance && (holdingObj == null || !holdingObj.lockHandOnGrab)) {
                teleporting = true;
                //If youre holding an item and it is set to release on teleport
                if(holdingObj != null && holdingObj.releaseOnTeleport)
                    ForceReleaseGrab();

                SetTransformToFollow();
            }
            else
                teleporting = false;

            //Reduces potential jittering
            if(distance < 0.005f) {
                transform.position = movePos;
            }

            //Sets velocity linearly based on distance from hand
            distance = Vector3.Distance(movePos, transform.position);
            var vel = (movePos - transform.position).normalized * followPositionStrength * distance;
            vel.x = Mathf.Clamp(vel.x, -maxVelocity, maxVelocity);
            vel.y = Mathf.Clamp(vel.y, -maxVelocity, maxVelocity);
            vel.z = Mathf.Clamp(vel.z, -maxVelocity, maxVelocity);
            body.velocity = vel;
        }


        /// <summary>Rotates the hand to the controller rotation using physics movement</summary>
        void TorqueTo() {
            var toRot = rotationOffset * moveTo.rotation;
            float angleDist = Quaternion.Angle(body.rotation, toRot);
            Quaternion desiredRotation = Quaternion.Lerp(body.rotation, toRot, Mathf.Clamp(angleDist, 0, 2) / 4f);

            var kp = 90f * followRotationStrength;
            var kd = 50f;
            Vector3 x;
            float xMag;
            Quaternion q = desiredRotation * Quaternion.Inverse(transform.rotation);
            q.ToAngleAxis(out xMag, out x);
            x.Normalize();
            x *= Mathf.Deg2Rad;
            Vector3 pidv = kp * x * xMag - kd * body.angularVelocity;
            Quaternion rotInertia2World = body.inertiaTensorRotation * transform.rotation;
            pidv = Quaternion.Inverse(rotInertia2World) * pidv;
            pidv.Scale(body.inertiaTensor);
            pidv = rotInertia2World * pidv;
            body.AddTorque(pidv);
        }

        void SetTransformToFollow() {
            if(holdingObj != null){
                var lastParent = holdingObj.body.transform.parent;
                holdingObj.body.transform.parent = transform;
                transform.position = moveTo.position;
                transform.rotation = moveTo.rotation;
                holdingObj.body.transform.parent = lastParent;
            }
            else{
                transform.position = moveTo.position;
                transform.rotation = moveTo.rotation;
            }
        }


        /// <summary>Sets the hands grip</summary>
        /// <param name="grip">0-1</param>
        public void SetGrip(float grip) {
            triggerPoint = grip;
        }


        /// <summary>Function for controller trigger fully pressed</summary>
        public void Grab() {
            if(!grabbing && !squeezing && holdingObj == null) {
                RaycastHit closestHit;
                if(HandClosestHit(out closestHit, reachDistance, LayerMask.GetMask("Grabbable", "Grabbing", "Holding", "Releasing")) != Vector3.zero)
                    StartCoroutine(GrabObject(closestHit));
            }
            else if(holdingObj != null) {
                if(holdingObj.GetComponent<GrabLock>()) {
                    holdingObj.GetComponent<GrabLock>().OnGrabPressed?.Invoke();
                }
            }
        }


        /// <summary>Function for controller trigger unpressed</summary>
        public void Release() {
            //Do the holding object calls and sets
            if(holdingObj != null) {
                if(holdingObj.GetComponent<GrabLock>())
                    return;
                OnBeforeReleased?.Invoke(this, holdingObj);

                if(squeezing)
                    holdingObj.OnUnsqueeze(this);
                holdingObj.OnRelease(this, true);
                OnReleased?.Invoke(this, holdingObj);
            }
            BreakGrabConnection();
        }


        /// <summary>This will force release the hand without throwing or calling OnRelease\nEx. losing grip on something instead of throwing</summary>
        public void ForceReleaseGrab() {
            //Do the holding object calls and sets
            if(holdingObj != null) {
                if(squeezing)
                    holdingObj.OnUnsqueeze(this);
                OnForcedRelease?.Invoke(this, holdingObj);
                holdingObj.OnForceReleaseEvent?.Invoke(this, holdingObj);
            }
            BreakGrabConnection();
        }


        public void BreakGrabConnection(){
            holdingObj = null;
            grabLocked = false;
            grabPose = null;

            //This helps prevent clipping when releasing items by letting items drop through on release
            StartCoroutine(TimedResetLayer(ignoreReleaseTime, "HandReleasing", "Hand"));

            //Destroy Junk
            if(grabPoint != null)
                Destroy(grabPoint.gameObject);
            if(joint1 != null)
                Destroy(joint1);
            if(joint2 != null)
                Destroy(joint2);
        }
        

        /// <summary>Event for controller grip</summary>
        public void Squeeze() {
            squeezing = true;
            OnSqueezed?.Invoke(this, holdingObj);
            holdingObj?.OnSqueeze(this);
        }

        
        /// <summary>Event for controller ungrip</summary>
        public void Unsqueeze() {
            squeezing = false;
            OnUnsqueezed?.Invoke(this, holdingObj);
            holdingObj?.OnUnsqueeze(this);
        }


        /// <summary>This is used to simulate and trigger pull-apart effect</summary>
        void OnJointBreak(float breakForce) {
            if(joint1 != null)
                Destroy(joint1);
            if(joint2 != null)
                Destroy(joint2);
            holdingObj?.OnHandJointBreak(this);
            ForceReleaseGrab();
        }


        /// <summary>Returns the hands velocity times its strength</summary>
        internal Vector3 ThrowVelocity(){
            return body.velocity * throwPower;
        }


        /// <summary>Determines how the hand should look/move based on its flags</summary>
        void DeterminPose(){
            if(!grabbing && !squeezing && holdingObj == null){
                idealGrip = triggerPoint;
            }
            //Responsable for movement sway
            UpdateFingers();
        }


        /// <summary>Rotates the hand towards the object it's aiming to pick up</summary>
        void LookAssist(){
            if(holdingObj == null){
                Grabbable lookingAtGrab;
                RaycastHit hit;
                var dir = HandClosestHit(out hit, reachDistance, LayerMask.GetMask("Grabbable", "Grabbing", "Releasing"));
                //Zero means it didn't hit
                if(dir != Vector3.zero){
                    //Changes look target
                    if(hit.collider.transform.gameObject != lookingAtObj){
                        //Unhighlights current target if found
                        if(lookingAtObj != null && GetGrabbable(lookingAtObj, out lookingAtGrab)){
                            OnStopHighlight?.Invoke(this, lookingAtGrab);
                            lookingAtGrab.Unhighlight();
                        }

                        //Highlights new target if found
                        lookingAtObj = hit.collider.transform.gameObject;
                        if(GetGrabbable(lookingAtObj, out lookingAtGrab)){
                            OnHighlight?.Invoke(this, lookingAtGrab);
                            lookingAtGrab.Highlight();
                        }
                    }

                    rotationOffset = Quaternion.RotateTowards(rotationOffset, Quaternion.FromToRotation(palmTransform.forward, hit.point - transform.position), 50f * Time.deltaTime * lookAssistSpeed);
                }
                //If it was looking at something but now it's not
                else if(lookingAtObj != null){
                    //Just in case the object your hand is looking at is destroyed
                    if(GetGrabbable(lookingAtObj, out lookingAtGrab)){
                        OnStopHighlight?.Invoke(this, lookingAtGrab);
                        lookingAtGrab.Unhighlight();
                    }

                    lookingAtObj = null;
                    rotationOffset = Quaternion.identity;
                }
                //If you're seeing nothing reset offset
                else{
                    rotationOffset = Quaternion.identity;
                }
            }
            //If you're holding something reset offset
            else{
                rotationOffset = Quaternion.identity;
            }
        }

        /// <summary>Returns true if there is a grabbable or link, out null if there is none</summary>
        bool GetGrabbable(GameObject obj, out Grabbable grabbable) {
            if(obj == null){
                grabbable = null;
                return false;
            }

            if(obj.GetComponent<Grabbable>()){
                grabbable = obj.GetComponent<Grabbable>();
                return true;
            }

            if(obj.GetComponent<GrabbableChild>()){
                grabbable = obj.GetComponent<GrabbableChild>().grabParent;
                return true;
            }

            grabbable = null;
            return false;
        }
        

        /// <summary>Takes a hit from a grabbable object and moves the hand towards that point, then calculates ideal hand shape</summary>
        IEnumerator GrabObject(RaycastHit hit) {

            Grabbable lookingAtGrab;
            if(GetGrabbable(lookingAtObj, out lookingAtGrab))
                lookingAtGrab.Unhighlight();

            lookingAtObj = null;

            Grabbable tempHoldingObj;
            if(!GetGrabbable(hit.collider.gameObject, out tempHoldingObj))
                 yield break;
            
            //Checks if the grabbable script is enabled
            if(!tempHoldingObj.enabled)
                yield break;

            //If the hand doesn't match the settings
            if((tempHoldingObj.handType == HandType.left && !left) || (tempHoldingObj.handType == HandType.right && left))
                yield break;
            
            //Hand Swap - One Handed Items
            if(tempHoldingObj.singleHandOnly && tempHoldingObj.HeldCount() > 0){
                tempHoldingObj.ForceHandsRelease();
                yield return new WaitForFixedUpdate();
                yield return new WaitForEndOfFrame();
            }
            

            holdingObj = tempHoldingObj;
            startGrabPos = transform.position;
            startGrabRot = transform.rotation;
            
            OnBeforeGrabbed?.Invoke(this, holdingObj);

            //GrabbableBase check - cancels grab if failed
            var grabBase = holdingObj.GetComponent<GrabbablePointBase>();
            if(grabBase != null && !grabBase.Align(this))
                yield break;


            //Set layers for grabbing
            var originalLayer = LayerMask.LayerToName(holdingObj.gameObject.layer);
            holdingObj.SetLayerRecursive(holdingObj.transform, LayerMask.NameToLayer("Grabbable"), LayerMask.NameToLayer("Grabbing"));
            holdingObj.SetLayerRecursive(holdingObj.transform, LayerMask.NameToLayer("Holding"), LayerMask.NameToLayer("Grabbing"));
            holdingObj.SetLayerRecursive(holdingObj.transform, LayerMask.NameToLayer("Releasing"), LayerMask.NameToLayer("Grabbing"));
            SetLayerRecursive(transform, LayerMask.NameToLayer("HandHolding"));
            
            //SETS GRAB POINT
            grabPoint = new GameObject().transform;
            grabPoint.position = hit.point;
            grabPoint.parent = hit.transform;

            grabbing = true;
            palmOffset = transform.position - palmTransform.position;
            moveTo.position = grabPoint.transform.position + palmOffset;
            moveTo.rotation = grabPoint.transform.rotation;
            freezeRot = true;
            
            //Aligns the hand using the closest hit point
            IEnumerator AutoAllign() {
                foreach (var finger in fingers)
                    finger.ResetBend();
                
                var mass = body.mass;
                var dir = HandClosestHit(out _, reachDistance, LayerMask.GetMask("Grabbing"));

                var palmOriginParent = palmTransform.parent;
                palmTransform.parent = null;
                var originParent = transform.parent;
                transform.parent = palmTransform;

                palmTransform.LookAt(grabPoint.position, transform.forward);

                transform.parent = originParent;
                palmTransform.parent = palmOriginParent;
                body.mass = 0;
                
                
                transform.position = grabPoint.position + palmOffset;

                grabbingFrame = true;
                body.WakeUp();
                holdingObj.body.WakeUp();
                Physics.SyncTransforms();
                
                yield return new WaitForFixedUpdate();
                body.WakeUp();
                grabbingFrame = false;
                if(holdingObj != null)
                    holdingObj.body.WakeUp();
                body.mass = mass;
            }

            //Predetermined Pose
            if (holdingObj.GetComponent<GrabbablePose>()) {
                grabPose = holdingObj.GetComponent<GrabbablePose>();
                grabPose.SetGrabPose(this);
            }

            //Allign Position
            else if(grabBase == null){
                yield return StartCoroutine(AutoAllign());
            }

            if(holdingObj != null) {
                //Finger Bend
                if(grabPose == null){
                    foreach(var finger in fingers)
                        finger.BendFingerUntilHit(fingerBendSteps, LayerMask.GetMask("Grabbing"));
                }
                
                
                //Connect Joints
                joint1 = gameObject.AddComponent<FixedJoint>();
                joint1.connectedBody = holdingObj.body;
                joint1.breakForce = 10000000;
                joint1.breakTorque = 10000000;
                
                joint1.connectedMassScale = 1;
                joint1.massScale = 1;
                joint1.enableCollision = false;
                joint1.enablePreprocessing = false;
                
                joint2 = holdingObj.body.gameObject.AddComponent<FixedJoint>();
                joint2.connectedBody = body;
                joint2.breakForce = 10000000;
                joint2.breakTorque = 10000000;
                
                joint2.connectedMassScale = 1;
                joint2.massScale = 1;
                joint2.enableCollision = false;
                joint2.enablePreprocessing = false;
                
                grabPoint.transform.position = transform.position;
                grabPoint.transform.rotation = transform.rotation;
                OnGrabbed?.Invoke(this, holdingObj);
                holdingObj.OnGrab(this);

                yield return new WaitForFixedUpdate();
                
                if(holdingObj != null) {
                    joint1.breakForce = holdingObj.jointBreakForce;
                    joint1.breakTorque = holdingObj.jointBreakForce;

                    joint2.breakForce = holdingObj.jointBreakForce;
                    joint2.breakTorque = holdingObj.jointBreakForce;

                    grabLocked = true;
                }

                if(grabPose != null || grabBase != null)
                    transform.rotation = moveTo.rotation;

            }
            
            //Reset Values
            freezePos = false;
            freezeRot = false;
            grabbing = false;

            yield return new WaitForEndOfFrame();
            if(grabPose != null || grabBase != null)
                transform.rotation = follow.rotation;
        }


        public void SetHeldPose(PoseData pose, Grabbable grabbable) {
            holdingObj = grabbable;
            OnBeforeGrabbed?.Invoke(this, holdingObj);

            holdingObj.transform.position = transform.position;

            //Set Pose
            pose.SetPose(this, grabbable.transform);
            SetLayerRecursive(transform, LayerMask.NameToLayer("HandHolding"));

            //Connect Joints
            joint1 = gameObject.AddComponent<FixedJoint>();
            joint1.connectedBody = holdingObj.body;
            joint1.breakForce = holdingObj.jointBreakForce;
            joint1.breakTorque = holdingObj.jointBreakForce;
                
            joint1.connectedMassScale = 1;
            joint1.massScale = 1;
            joint1.enableCollision = false;
            joint1.enablePreprocessing = false;
                
            joint2 = holdingObj.body.gameObject.AddComponent<FixedJoint>();
            joint2.connectedBody = body;
            joint2.breakForce = holdingObj.jointBreakForce;
            joint2.breakTorque = holdingObj.jointBreakForce;
                
            joint2.connectedMassScale = 1;
            joint2.massScale = 1;
            joint2.enableCollision = false;
            joint2.enablePreprocessing = false;
                
            grabPoint = new GameObject().transform;
            grabPoint.parent = holdingObj.transform;
            grabPoint.transform.position = transform.position;
            grabPoint.transform.rotation = transform.rotation;
                
            OnGrabbed?.Invoke(this, holdingObj);
            holdingObj.OnGrab(this);

            SetTransformToFollow();

            grabLocked = true;
            
            //Reset Values
            grabbing = false;
            
        }
        
        public PoseData GetHeldPose() {
            return new PoseData(this);
        }

        public PoseData GetHeldPose(out Grabbable held) {
            held = holdingObj;
            return new PoseData(this);
        }
        


        public Grabbable GetHeld() {
            return holdingObj;
        }



        /// <summary>Breifly disabled the hand collision (used on release to manage collision overlap bugs)</summary>
        IEnumerator TimedResetLayer(float seconds, string startLayer, string resetLayer) {
            SetLayerRecursive(transform, LayerMask.NameToLayer(startLayer));
            yield return new WaitForSeconds(seconds);
            SetLayerRecursive(transform, LayerMask.NameToLayer(resetLayer));
        }

        /// <summary>Moves the hand back from a frozen grab position smoothly (releasing grip on a door or other stable object)</summary>
        IEnumerator ReturnOffset(float seconds) {
            float time = 0;
            var offset = transform.GetChild(0);
            Vector3 startPos = offset.localPosition;
            Quaternion StartRot = offset.localRotation;
            while(time < seconds) {
                time += Time.fixedDeltaTime;
                offset.localPosition = Vector3.Lerp(startPos, Vector3.zero, time / seconds);
                offset.localRotation = Quaternion.Lerp(StartRot, Quaternion.identity, time / seconds);
                yield return new WaitForFixedUpdate();
            }
            offset.localPosition = Vector3.zero;
        }

        
        /// <summary>Finds the closest raycast from a cone of rays -> Returns average direction of all hits</summary>
        public Vector3 HandClosestHit(out RaycastHit closestHit, float dist, int layerMask) {
            List<RaycastHit> hits = new List<RaycastHit>();
            foreach(var ray in handRays) {
                RaycastHit hit;
                if(Physics.Raycast(palmTransform.position, palmTransform.rotation * ray, out hit, dist, layerMask))
                    hits.Add(hit);
            }
            if(hits.Count > 0) {
                closestHit = hits[0];
                var closestHitIndex = 0;
                var minMax = new Vector2(1f, 1.05f);
                Vector3 dir = Vector3.zero;
                for(int i = 0; i < hits.Count; i++) {
                    var closestMulti = Mathf.Lerp(minMax.x, minMax.y, ((float)closestHitIndex)/hits.Count);
                    var multi = Mathf.Lerp(minMax.x, minMax.y, ((float)i)/hits.Count);
                    if(hits[i].distance*multi < closestHit.distance*closestMulti){
                        closestHit = hits[i];
                        closestHitIndex = i;
                    }
                    dir += hits[i].point - palmTransform.position;
                }

                return dir/hits.Count;
            }

            closestHit = new RaycastHit();
            return Vector3.zero;
        }
        
        
        /// <summary>update fingers based on hand velocity and trigger grip value</summary>
        void UpdateFingers() {
            if(!holdingObj && !disableIK) {
                float vel = -palmTransform.InverseTransformDirection(body.velocity).z;
                float grip = idealGrip + gripOffset + swayStrength * vel / 6f;

                if(currGrip != grip) {
                    bool less = (currGrip < grip) ? true : false;
                    currGrip += ((currGrip < grip) ? Time.deltaTime : -Time.deltaTime) * (1 + Mathf.Abs(currGrip - grip) * 20);
                    if(less && currGrip > grip)
                        currGrip = grip;

                    else if(!less && currGrip < grip)
                        currGrip = grip;

                    foreach(var finger in fingers)
                        finger.SetFingerBend(currGrip);
                }
            }
        }


        [ContextMenu("Relaxed Hand")]
        public void RelaxHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(gripOffset);
        }

        [ContextMenu("Opened Hand")]
        public void OpenHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(0);
        }
        
        [ContextMenu("Closed Hand")]
        public void CloseHand() {
            foreach(var finger in fingers)
                finger.SetFingerBend(1);
        }

        [ContextMenu("Bend Fingers Until Hit")]
        public void ProceduralFingerBend() {
            ProceduralFingerBend(~handLayers);
        }
        
        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend(int layermask){
            foreach(var finger in fingers){
                finger.BendFingerUntilHit(fingerBendSteps, layermask);
            }
        }

        /// <summary>Bends each finger until they hit</summary>
        public void ProceduralFingerBend(RaycastHit hit){
            foreach(var finger in fingers){
                finger.BendFingerUntilHit(fingerBendSteps, hit);
            }
        }
        



        public static void SetLayerRecursive(Transform obj, int fromLayer, int toLayer) {
            if(obj.gameObject.layer == fromLayer) {
                obj.gameObject.layer = toLayer;
            }
            for(int i = 0; i < obj.childCount; i++) {
                SetLayerRecursive(obj.GetChild(i), toLayer, fromLayer);
            }
        }

        public static void SetLayerRecursive(Transform obj, int newLayer) {
            obj.gameObject.layer = newLayer;
            for(int i = 0; i < obj.childCount; i++) {
                SetLayerRecursive(obj.GetChild(i), newLayer);
            }
        }
        

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            Gizmos.DrawLine(transform.position, (transform.position+transform.rotation*palmOffset));
            if(showGizmos) {
                if(handRays == null || handRays.Length == 0) {
                    if(palmTransform != null) {
                        Vector3 handDir = -Vector3.right;
                        for(int i = 0; i < 100; i++) {
                            float ampI = Mathf.Pow(i, 1.05f + grabSpreadOffset/10f) / (Mathf.PI * 0.8f);
                            Gizmos.DrawRay(palmTransform.position, palmTransform.rotation * Quaternion.Euler(0, Mathf.Cos(i) * ampI + 90, Mathf.Sin(i) * ampI) * handDir * reachDistance);
                        }
                    }
                }
                else {
                    foreach(var ray in handRays) {
                        Gizmos.DrawRay(palmTransform.position, palmTransform.rotation * ray * reachDistance);
                    }

                    Gizmos.color = Color.green;
                    RaycastHit hit;
                    var avgDir = HandClosestHit(out hit, reachDistance, LayerMask.GetMask("Grabbable", "Grabbing", "Holding", "Releasing"));
                    if(avgDir != Vector3.zero) {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(palmTransform.position, avgDir);
                    }

                }
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, maxFollowDistance);
            }
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(palmTransform.position, palmTransform.forward*reachDistance);
        }
#endif
    }
}
