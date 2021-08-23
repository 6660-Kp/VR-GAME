using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand.Demo{
    public class Pistol : MonoBehaviour
    {
        public Transform barrelTip;
        public Rigidbody slide;
        public float hitPower = 1;
        public float recoilPower = 1;
        public float range = 100;
        public LayerMask layer;

        public void Shoot() {
            RaycastHit hit;
            if(Physics.Raycast(barrelTip.position, barrelTip.forward, out hit, range, layer)){
                var hitBody = hit.transform.GetComponent<Rigidbody>();
                if(hitBody != null) {
                    Debug.DrawRay(barrelTip.position, (hit.point - barrelTip.position), Color.green, 5);
                    hitBody.GetComponent<Smash>()?.DoSmash();
                    hitBody?.AddForceAtPosition((hit.point - barrelTip.position).normalized*hitPower*10, hit.point, ForceMode.Impulse);
                }
            }
            else
                Debug.DrawRay(barrelTip.position, barrelTip.forward*range, Color.red, 1);

            GetComponent<Rigidbody>().AddForceAtPosition(barrelTip.transform.up*hitPower*5, hit.point, ForceMode.Impulse);
        }
   
        public void GrabSlide() {
            slide.GetComponent<ConfigurableJoint>().xMotion = ConfigurableJointMotion.Limited;
        }
   
        public void ReleaseSlide() {
            slide.GetComponent<ConfigurableJoint>().xMotion = ConfigurableJointMotion.Locked;
        }
}
}
