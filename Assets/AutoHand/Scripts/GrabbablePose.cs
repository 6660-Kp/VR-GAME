using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    [System.Serializable]
    public struct PoseData{

        public Vector3 handOffset;
        public Vector3 rotationOffset;
        public Vector3[] posePositions;
        public Quaternion[] poseRotations;
        
        public PoseData(Hand hand, Grabbable grabbable) {
            posePositions = new Vector3[0];
            poseRotations = new Quaternion[0];
            handOffset = new Vector3();
            rotationOffset = new Vector3();
            SavePose(hand, grabbable.transform);
        }

        public PoseData(Hand hand) {
            posePositions = new Vector3[0];
            poseRotations = new Quaternion[0];
            handOffset = new Vector3();
            rotationOffset = new Vector3();
            SavePose(hand, null);
        }

        public void SavePose(Hand hand, Transform relativeTo) {
            var posePositionsList = new List<Vector3>();
            var poseRotationsList = new List<Quaternion>();
            
            if(relativeTo != null){
                var handParent = hand.transform.parent;
                hand.transform.parent = relativeTo;
                handOffset = hand.transform.localPosition;
                rotationOffset = hand.transform.localEulerAngles;
                hand.transform.parent = handParent;
            }
            else {
                handOffset = hand.transform.localPosition;
                rotationOffset = hand.transform.localEulerAngles;
            }

            foreach(var finger in hand.fingers) {
                AssignChildrenPose(finger.transform);
            }

            void AssignChildrenPose(Transform obj) {
                AddPoint(obj.localPosition, obj.localRotation);
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j));
                }
            }

            void AddPoint(Vector3 pos, Quaternion rot) {
                posePositionsList.Add(pos);
                poseRotationsList.Add(rot);
            }
            
            posePositions = new Vector3[posePositionsList.Count];
            poseRotations = new Quaternion[posePositionsList.Count];
            for(int i = 0; i < posePositionsList.Count; i++) {
                posePositions[i] = posePositionsList[i];
                poseRotations[i] = poseRotationsList[i];
            }
        }

        public void SetPose(Hand hand, Transform relativeTo){
            if(relativeTo != null){
                var handParent = hand.transform.parent;
                var originalScale = hand.transform.localScale;
                hand.transform.parent = relativeTo;
                hand.transform.localPosition = handOffset;
                hand.transform.localEulerAngles = rotationOffset;
                hand.transform.parent = handParent;
                hand.transform.localScale = originalScale;
            }
            else {
                hand.transform.localPosition = handOffset;
                hand.transform.localEulerAngles = rotationOffset;
            }

            int i = -1;
            void AssignChildrenPose(Transform obj, PoseData pose) {
                i++;
                obj.localPosition = pose.posePositions[i];
                obj.localRotation = pose.poseRotations[i];
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j), pose);
                }
            }

            foreach(var finger in hand.fingers) {
                AssignChildrenPose(finger.transform, this);
            }
        }
    }


    [RequireComponent(typeof(Grabbable))]
    public class GrabbablePose : MonoBehaviour{
#if UNITY_EDITOR
        [Header("Editor")]
        [Tooltip("Used to pose for the grabbable")]
        public Hand editorHand;
#endif
        [HideInInspector]
        public PoseData rightPose;
        [HideInInspector]
        public bool rightPoseSet = false;
        [HideInInspector]
        public PoseData leftPose;
        [HideInInspector]
        public bool leftPoseSet = false;

        Grabbable grabbable;

        private void Start() {
            var grabbable = GetComponent<Grabbable>();
            if(!leftPoseSet && !rightPoseSet){
                Debug.LogError("Grabbable Pose has not been set for either hand", this);
                grabbable.enabled = false;
            }
            else if(!leftPoseSet && rightPoseSet && !(grabbable.handType == HandType.right)){
                Debug.Log("Setting Grabbable to right hand only because left handed pose not set", this);
                grabbable.handType = HandType.right;
            }
            else if(leftPoseSet && !rightPoseSet && !(grabbable.handType == HandType.left)){
                Debug.Log("Setting Grabbable to left hand only because right handed pose not set", this);
                grabbable.handType = HandType.left;
            }

        }

        public void SetGrabPose(Hand hand) {
            PoseData pose;
            if(hand.left){
                if(leftPoseSet)
                    pose = leftPose;
                else
                    return;
            }
            else{
                if(rightPoseSet)
                    pose = rightPose;
                else
                    return;
            }

            var handParent = hand.transform.parent;
            var originalScale = hand.transform.localScale;
            hand.transform.parent = transform;
            hand.transform.localPosition = pose.handOffset;
            hand.transform.localEulerAngles = pose.rotationOffset;
            hand.transform.parent = handParent;
            hand.transform.localScale = originalScale;
            
            int i = -1;
            void AssignChildrenPose(Transform obj) {
                i++;
                obj.localPosition = pose.posePositions[i];
                obj.localRotation = pose.poseRotations[i];
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j));
                }
            }

            foreach(var 
                finger in hand.fingers) {
                AssignChildrenPose(finger.transform);
            }
        }
        
#if UNITY_EDITOR
        public void SetGrabPoseEditor(Hand hand) {
            PoseData pose = new PoseData();

            var handParent = hand.transform.parent;
            var handCopy = Instantiate(hand, hand.transform.position, hand.transform.rotation);
            handCopy.name = "HAND COPY DELETE";
            editorHand = handCopy;
            handCopy.transform.parent = transform;
            handCopy.transform.localPosition = pose.handOffset;
            handCopy.transform.localEulerAngles = pose.rotationOffset;
            handCopy.transform.parent = handParent;
            handCopy.editorAutoGrab = true;
            
            if(hand.left){
                if(leftPoseSet)
                    pose = leftPose;
                else
                    return;
            }
            else{
                if(rightPoseSet)
                    pose = rightPose;
                else
                    return;
            }

            handCopy.transform.localPosition = pose.handOffset;
            handCopy.transform.localEulerAngles = pose.rotationOffset;

            int i = -1;
            void AssignChildrenPose(Transform obj) {
                i++;
                obj.localPosition = pose.posePositions[i];
                obj.localRotation = pose.poseRotations[i];
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j));
                }
            }

            if(pose.posePositions.Length > 0){
                foreach(var finger in handCopy.fingers) {
                    AssignChildrenPose(finger.transform);
                }
            }

        }

        public void Clear() {
            leftPoseSet = false;
            rightPoseSet = false;
        }

        public void SaveGrabPose(Hand hand, bool left){
            var pose = new PoseData();
            
            var posePositionsList = new List<Vector3>();
            var poseRotationsList = new List<Quaternion>();
            
            var handCopy = Instantiate(hand, hand.transform.position, hand.transform.rotation);
            var handParent = handCopy.transform.parent;
            handCopy.transform.parent = transform;
            pose.handOffset = handCopy.transform.localPosition;
            pose.rotationOffset = handCopy.transform.localEulerAngles;
            DestroyImmediate(handCopy.gameObject);

            foreach(var finger in hand.fingers) {
                AssignChildrenPose(finger.transform);
            }

            void AssignChildrenPose(Transform obj) {
                AddPoint(obj.localPosition, obj.localRotation);
                for(int j = 0; j < obj.childCount; j++) {
                    AssignChildrenPose(obj.GetChild(j));
                }
            }

            void AddPoint(Vector3 pos, Quaternion rot) {
                posePositionsList.Add(pos);
                poseRotationsList.Add(rot);
            }
            
            pose.posePositions = new Vector3[posePositionsList.Count];
            pose.poseRotations = new Quaternion[posePositionsList.Count];
            for(int i = 0; i < posePositionsList.Count; i++) {
                pose.posePositions[i] = posePositionsList[i];
                pose.poseRotations[i] = poseRotationsList[i];
            }

            if(left){
                leftPose = pose;
                leftPoseSet = true;
                Debug.Log("Pose Saved - Left");
            }
            else{
                rightPose = pose;
                rightPoseSet = true;
                Debug.Log("Pose Saved - Right");
            }
        }
#endif
    }
}
