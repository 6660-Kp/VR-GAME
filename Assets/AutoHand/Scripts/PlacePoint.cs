using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    #if UNITY_EDITOR
        [ExecuteInEditMode]
    #endif
    [RequireComponent(typeof(SphereCollider))]
    public class PlacePoint : MonoBehaviour{
        [Header("Placement")]
        public float placeRadius = 0.1f;

        [Tooltip("Positional Offset of the original point")]
        public Vector3 offset;

        [Tooltip("Will allow placement for any name containing this array of strings, leave blank any grabbable")]
        public string[] placeNames;

        [Tooltip("Makes the object being placedObject kinematic")]
        public bool makeKinematic = false;

        [Tooltip("Snaps an object to the point at start, leave blank if empty")]
        public Grabbable startPlaced;

        [Header("Joint")]
        [Tooltip("The rigidbody to attach the placed grabbable to - leave empty means no joint")]
        public Rigidbody jointLink;
        public int jointBreakForce = 500;
        public int jointBreakTorque = 500;

        [Header("Events")]
        [Tooltip("Whether or not the held objects highlight will be triggered by the entering place point")]
        public bool callGrabbableHighlight = false;
        
        public UnityEvent OnPlace;
        public UnityEvent OnRemove;
        public UnityEvent OnHighlight;
        public UnityEvent OnStopHighlight;

        FixedJoint joint;
        
#if UNITY_EDITOR
        [Header("Editor")]
        public Grabbable editorGrabbable;
        Vector3 preEditorPos;
        Grabbable lastEditorGrabbable;
#endif

        Grabbable placedObject;
        bool occupied = false;
        SphereCollider col;
        Transform originParent;

        void Start(){
#if UNITY_EDITOR
            lastEditorGrabbable = null;
            if(!EditorApplication.isPlaying)
                return;
#endif

            col = gameObject.GetComponent<SphereCollider>();
            col.radius = placeRadius;
            col.isTrigger = true;
            col.gameObject.layer = LayerMask.NameToLayer("PlacePoint");
            if(startPlaced != null){
                startPlaced.SetPlacePoint(this);
                Place(startPlaced);
            }
        }

        
#if UNITY_EDITOR
        void Update() {
            if(EditorApplication.isPlaying)
                return;

            if(editorGrabbable != null){
                if(lastEditorGrabbable != editorGrabbable) {
                    if(lastEditorGrabbable != null){
                        Remove(lastEditorGrabbable);
                        lastEditorGrabbable.transform.position = preEditorPos;
                    }

                    preEditorPos = editorGrabbable.transform.position;
                }
                else{
                    originParent = editorGrabbable.transform.parent;
                    editorGrabbable.transform.parent = transform;
                    editorGrabbable.transform.localPosition = offset;
                    editorGrabbable.transform.localRotation = Quaternion.identity;
                    editorGrabbable.transform.parent = originParent;
                
                }
            }
            lastEditorGrabbable = editorGrabbable;
        }
#endif

        public virtual bool CanPlace(Transform placeObj) {
            if(occupied)
                return false;

            if(placeNames.Length == 0)
                return true;

            foreach(var placeName in placeNames) {
                if(placeObj.name.Contains(placeName)){
                    return true;
                }
            }
            return false;
        }
        
        public virtual void Place(Grabbable placeObj) {
            originParent = placeObj.transform.parent;
            placeObj.transform.parent = transform;
            placeObj.transform.localPosition = offset;
            placeObj.transform.localRotation = Quaternion.identity;
            placeObj.body.isKinematic = makeKinematic;
            
            if(jointLink != null && joint == null){
                joint = jointLink.gameObject.AddComponent<FixedJoint>();
                joint.connectedBody = placeObj.body;
                joint.breakForce = jointBreakForce;
                joint.breakTorque = jointBreakTorque;
                
                joint.connectedMassScale = 1;
                joint.massScale = 1;
                joint.enableCollision = false;
                joint.enablePreprocessing = false;
            }

            occupied = true;
            placedObject = placeObj;
            OnPlace?.Invoke();
        }

        public virtual void Remove(Grabbable placeObj) {
            if(makeKinematic)
                placeObj.body.isKinematic = false;
            placeObj.transform.parent = originParent;
            OnRemove?.Invoke();
            occupied = false;
            placedObject = null;
        }

        private void OnJointBreak(float breakForce) {
            if(joint != null)
                Destroy(joint);
            if(placedObject != null)
                Remove(placedObject);
        }

        public Grabbable PlacedObject() {
            return placedObject;
        }

        internal float Distance(Transform from) {
            return Vector3.Distance(from.position, transform.position+offset);
        }

        internal void Highlight() {
            if(placedObject == null)
                OnHighlight?.Invoke();
        }

        internal void StopHighlight() {
            if(placedObject == null)
                OnStopHighlight?.Invoke();
        }

        void OnDrawGizmosSelected() {
            gameObject.GetComponent<SphereCollider>().radius = placeRadius;
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + offset, 0.0025f);
        }

    }
}
