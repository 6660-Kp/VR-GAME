using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo{
    [RequireComponent(typeof(HingeJoint))]
    public class PhysicsLever : MonoBehaviour{
        HingeJoint joint;
    
        public float buffer = 0.1f;
        public UnityEvent SwitchOn;
        public UnityEvent SwitchOff;

        bool on;
        protected float leverPoint = 0;

        void Start(){
            joint = GetComponent<HingeJoint>();   
        }

        public void Update(){
            leverPoint = joint.angle/(joint.limits.max - joint.limits.min);
            if(leverPoint <= buffer && on) {
                on = false;
                OnSwitchOn();
            }
            else if(leverPoint >= 1-buffer && !on) {
                on = true;
                OnSwitchOff();
            }
        }

        public virtual void OnSwitchOn() {
            SwitchOn?.Invoke();
        }

        public virtual void OnSwitchOff() {
            SwitchOff?.Invoke();
        }
    }
}
