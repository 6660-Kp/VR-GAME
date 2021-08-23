using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand {
    public class Grabbable : MonoBehaviour {
        [Header("Holding Settings")]
        [Tooltip("The physics body to connect this colliders grab to - if left empty will default to local body")]
        public Rigidbody body;
        public Vector3 heldPositionOffset;
        public Vector3 heldRotationOffset;
        [Space]
        [Tooltip("Which hand this can be held by")]
        public HandType handType = HandType.both;
        [Tooltip("Whether or not this can be grabbed with more than one hand")]
        public bool singleHandOnly = false;
        [Tooltip("This will allow you to move smoothly while holding an item, but will also allow you to move items that are very heavy, good for small items to carry")]
        public bool parentOnGrab = true;
        [Tooltip("Release this object when the hand leaves its max distance")]
        public bool releaseOnTeleport = false;
        [Tooltip("Lock hand in place on grab")]
        public bool lockHandOnGrab = false;


        [Header("Highlight Settings")]
        public Material hightlightMaterial;


        [HideInInspector]
        [Tooltip("Whether or not to apply hands velocity on release\n- Good for things that you can move/roll but not fully pickup")]
        public float throwMultiplyer = 1;
        
        
        [HideInInspector]
        [Tooltip("Whether or not the break call made only when holding with multiple hands")]
        public bool pullApartBreakOnly = true;

        
        [HideInInspector]
        [Tooltip("Adds and links a GrabbableChild to each child with a collider on start - So the hand can grab them")]
        public bool makeChildrenGrabbable = true;


        [Space]
        [HideInInspector]
        [Tooltip("The required force to break the fixedJoint\n " +
                 "Turn this very high to disable (Might cause jitter)\n" +
                "Ideal value depends on hand mass and velocity settings... Try between 1500-3000 for a 10 mass hand")]
        public int jointBreakForce = 1200;
        
        [HideInInspector]
        [Tooltip("The required torque to break the fixedJoint\n " +
                 "Turn this very high to disable (Might cause jitter)\n" +
                "Ideal value depends on hand mass and velocity settings")]
        public int jointBreakTorque = 1200;
        
        //For programmers <3
        public HandGrabEvent OnGrabEvent;
        public HandGrabEvent OnReleaseEvent;
        public HandGrabEvent OnForceReleaseEvent;
        public HandGrabEvent OnSqueezeEvent;
        public HandGrabEvent OnUnsqueezeEvent;
        public HandGrabEvent OnJointBreakEvent;

        [HideInInspector]
        public UnityEvent onGrab;
        [HideInInspector]
        public UnityEvent onRelease;
        [HideInInspector]
        public UnityEvent onSqueeze;
        [HideInInspector]
        public UnityEvent onUnsqueeze;
        [HideInInspector]
        public UnityEvent OnJointBreak;

#if UNITY_EDITOR
        [HideInInspector]
        public bool hideEvents = false;
#endif

        protected bool beingHeld = false;

        Vector3 lastCenterOfMassPos;
        Quaternion lastCenterOfMassRot;
        CollisionDetectionMode detectionMode;
        List<Hand> heldBy;
        private bool throwing;
        bool hightlighting;
        GameObject highlightObj;
        PlacePoint placePoint;
        Transform originalParent;

        protected void Awake() {
            OnAwake();
        }

        /// <summary>Virtual substitute for Awake()</summary>
        public virtual void OnAwake() {
            if(heldBy == null)
                heldBy = new List<Hand>();

            if(body == null){
                if(GetComponent<Rigidbody>())
                    body = GetComponent<Rigidbody>();
                else
                    Debug.LogError("RIGIDBODY MISSING FROM GRABBABLE: " + transform.name + " \nPlease add/attach a rigidbody", this);
            }

            originalParent = body.transform.parent;
            gameObject.layer = LayerMask.NameToLayer("Grabbable");
            
            if(makeChildrenGrabbable)
                MakeChildrenGrabbable();
        }
        

        protected void FixedUpdate() {
            if(beingHeld) {
                lastCenterOfMassRot = body.transform.rotation;
                lastCenterOfMassPos = body.transform.position;
            }
        }

        /// <summary>Called when the hand starts aiming at this item for pickup</summary>
        public virtual void Highlight() {
            if(!hightlighting){
                hightlighting = true;
                if(hightlightMaterial != null){
                    if(GetComponent<MeshRenderer>() == null) {
                        Debug.LogError("Cannot Highlight Grabbable Without MeshRenderer", gameObject);
                        return;
                    }

                    highlightObj = new GameObject();
                    highlightObj.transform.parent = transform;
                    highlightObj.transform.localPosition = Vector3.zero;
                    highlightObj.transform.localRotation = Quaternion.identity;
                    highlightObj.transform.localScale = Vector3.one * 1.001f;

                    highlightObj.AddComponent<MeshFilter>().sharedMesh = GetComponent<MeshFilter>().sharedMesh;
                    highlightObj.AddComponent<MeshRenderer>().materials = new Material[GetComponent<MeshRenderer>().materials.Length];
                    var mats = new Material[GetComponent<MeshRenderer>().materials.Length];
                    for(int i = 0; i < mats.Length; i++) {
                        mats[i] = hightlightMaterial;
                    }
                    highlightObj.GetComponent<MeshRenderer>().materials = mats;
                }
            }
        }
        
        /// <summary>Called when the hand stops aiming at this item</summary>
        public virtual void Unhighlight() {
            if(hightlighting){
                hightlighting = false;
                if(highlightObj != null)
                    Destroy(highlightObj);
            }
        }

        /// <summary>Called by the hands Squeeze() function is called and this item is being held</summary>
        public virtual void OnSqueeze(Hand hand) {
            OnSqueezeEvent?.Invoke(hand, this);
            onSqueeze?.Invoke();
        }


        /// <summary>Called by the hands Unsqueeze() function is called and this item is being held</summary>
        public virtual void OnUnsqueeze(Hand hand) {
            OnUnsqueezeEvent?.Invoke(hand, this);
            onUnsqueeze?.Invoke();
        }

        /// <summary>Called by the hand whenever this item is grabbed</summary>
        public virtual void OnGrab(Hand hand) {
            if(lockHandOnGrab)
                hand.GetComponent<Rigidbody>().isKinematic = true;

            if(parentOnGrab)
                body.transform.parent = hand.transform.parent;
            heldBy?.Add(hand);
            throwing = false;
            beingHeld = true;
            ContinuousCollisionDetection();
            gameObject.layer = LayerMask.NameToLayer("Holding");
            onGrab?.Invoke();
            if(placePoint != null)
                placePoint.Remove(this);

            OnGrabEvent?.Invoke(hand, this);
        }


        /// <summary>Called by the hand whenever this item is release</summary>
        public virtual void OnRelease(Hand hand, bool thrown) {
            if(beingHeld) {
                if(lockHandOnGrab)
                    hand.GetComponent<Rigidbody>().isKinematic = false;

                if(!heldBy.Remove(hand))
                    return;

                OnReleaseEvent?.Invoke(hand, this);

                if(heldBy.Count == 0){
                    beingHeld = false;
                    body.transform.parent = originalParent;
                }

                onRelease?.Invoke();
                if(body != null) {
                    if(!beingHeld && thrown && !throwing && gameObject.layer != LayerMask.NameToLayer("Grabbing")) {
                        throwing = true;
                        body.velocity += hand.ThrowVelocity() * throwMultiplyer;
                        try {
                            body.angularVelocity = GetAngularVelocity();
                        }
                        catch { }
                    }
                    Invoke("OriginalCollisionDetection", 5f);
                    SetLayerRecursive(transform, LayerMask.NameToLayer("Grabbing"), LayerMask.NameToLayer("Releasing"));
                    SetLayerRecursive(transform, LayerMask.NameToLayer("Holding"), LayerMask.NameToLayer("Releasing"));
                    Invoke("ResetLayerAfterRelease", 0.5f);
                }
                
                if(placePoint != null){
                    if(placePoint.CanPlace(transform))
                        placePoint.Place(this);

                    if(placePoint.callGrabbableHighlight)
                        Unhighlight();

                    placePoint.StopHighlight();
                }
            }
        }

        /// <summary>Forces all the hands on this object to relese without applying throw force or calling OnRelease event</summary>
        public void ForceHandsRelease() {
            for(int i = heldBy.Count - 1; i >= 0; i--) {
                heldBy[i].holdingObj = null;
                heldBy[i].ForceReleaseGrab();
                if(beingHeld) {
                    if(lockHandOnGrab)
                        heldBy[i].GetComponent<Rigidbody>().isKinematic = false;
                    

                    OnForceReleaseEvent?.Invoke(heldBy[i], this);
                    heldBy.Remove(heldBy[i]);
                    if(heldBy.Count == 0){
                        beingHeld = false;
                        if(!beingDestroyed)
                            body.transform.parent = originalParent;
                    }

                    if(body != null) {
                        ContinuousCollisionDetection();
                        Invoke("OriginalCollisionDetection", 5f);
                        SetLayerRecursive(transform, LayerMask.NameToLayer("Grabbing"), LayerMask.NameToLayer("Releasing"));
                        SetLayerRecursive(transform, LayerMask.NameToLayer("Holding"), LayerMask.NameToLayer("Releasing"));
                        Invoke("ResetLayerAfterRelease", 0.25f);
                    }

                }
            }
        }

        public int HeldCount() {
            return heldBy.Count;
        }

        bool beingDestroyed = false;
        private void OnDestroy() {
            beingDestroyed = true;
            ForceHandsRelease();
        }

        /// <summary>Called when the joint between the hand and this item is broken\n - Works to simulate pulling item apart event</summary>
        public virtual void OnHandJointBreak(Hand hand) {
            if(!pullApartBreakOnly || heldBy.Count > 1){
                OnJointBreakEvent?.Invoke(hand, this);
                OnJointBreak?.Invoke();
            }
            ForceHandsRelease();
        }

        public bool IsThrowing() {
            return throwing;
        }

        public Vector3 GetVelocity() {
            return lastCenterOfMassPos - transform.position;
        }

        public Vector3 GetAngularVelocity() {
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastCenterOfMassRot);
            deltaRotation.ToAngleAxis(out var angle, out var axis);
            angle *= Mathf.Deg2Rad;
            return (1.0f / Time.fixedDeltaTime) * angle * axis;
        }


        public void OnCollisionEnter(Collision collision) {
            if(throwing && !LayerMask.LayerToName(collision.gameObject.layer).Contains("Hand")) {
                Invoke("ResetThrowing", Time.fixedDeltaTime);
            }
        }

        public void SetPlacePoint(PlacePoint point) {
            placePoint = point;
        }

        public void OnTriggerEnter(Collider other) {
            if(heldBy.Count > 0 && other.GetComponent<PlacePoint>()) {
                var otherPoint = other.GetComponent<PlacePoint>();
                if(otherPoint == null) return;

                if(placePoint != null && placePoint.PlacedObject() != null)
                    return;

                if(placePoint == null || otherPoint.Distance(transform) < placePoint.Distance(transform)){
                    if(otherPoint.CanPlace(transform)){
                        placePoint = other.GetComponent<PlacePoint>();
                        placePoint.Highlight();
                        if(placePoint.callGrabbableHighlight)
                            Highlight();
                    }
                }
            }
        }


        public void OnTriggerExit(Collider other) {
            if(heldBy.Count > 0 && placePoint != null && placePoint == other.GetComponent<PlacePoint>()) {
                placePoint.StopHighlight();
                if(placePoint.callGrabbableHighlight)
                    Unhighlight();
                placePoint = null;
            }
        }

        //Invoked one fixedupdate after impact
        protected void ResetThrowing() {
            throwing = false;
        }

        //Invoked a quatersecond after releasing
        protected void ResetLayerAfterRelease() {
            if(LayerMask.LayerToName(gameObject.layer) != "Grabbing")
                SetLayerRecursive(transform, LayerMask.NameToLayer("Releasing"), LayerMask.NameToLayer("Grabbable"));
        }

        public void SetLayerRecursive(Transform obj, int oldLayer, int newLayer) {
            if(obj.gameObject.layer == oldLayer)
                obj.gameObject.layer = newLayer;
            for(int i = 0; i < obj.childCount; i++) {
                SetLayerRecursive(obj.GetChild(i), oldLayer, newLayer);
            }
        }
        
        void MakeChildrenGrabbable() {
            for(int i = 0; i < transform.childCount; i++) {
                AddChildGrabbableRecursive(transform.GetChild(i));
            }
        }

        void AddChildGrabbableRecursive(Transform obj) {
            if(obj.GetComponent<Collider>() && !obj.GetComponent<GrabbableChild>() && !obj.GetComponent<Grabbable>() && !obj.GetComponent<PlacePoint>()){
                var child = obj.gameObject.AddComponent<GrabbableChild>();
                child.gameObject.layer = LayerMask.NameToLayer("Grabbable");

                child.grabParent = this;
            }
            for(int i = 0; i < obj.childCount; i++) {
                AddChildGrabbableRecursive(obj.GetChild(i));
            }
        }

        public void DoDestroy() {
            Destroy(gameObject);
        }

        //This is called after throwing for better collision results
        protected void ContinuousCollisionDetection() {
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        //Resets to original collision dection
        protected void OriginalCollisionDetection() {
            if(body != null)
                body.collisionDetectionMode = detectionMode;
        }
    }
}
