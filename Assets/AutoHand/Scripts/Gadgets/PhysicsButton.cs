using UnityEngine;
using UnityEngine.Events;

namespace Autohand.Demo{
    [RequireComponent(typeof(SpringJoint))]
    public class PhysicsButton : MonoBehaviour{
        SpringJoint spring;
        Vector3 startPos;
        bool pressed = false;
    
        public float threshHold = 0.05f;
        [Space]
        public UnityEvent OnPressed;
        public UnityEvent OnUnpressed;

        void Start(){
            startPos = transform.position;
            spring = GetComponent<SpringJoint>();
        }


        private void Update() {
            if(transform.position.y > startPos.y)
                transform.position = new Vector3(transform.position.x, startPos.y, transform.position.z);
        }

        void FixedUpdate(){
            if(!pressed && startPos.y - transform.position.y >= threshHold) {
                pressed = true;
                Pressed();
            }
            else if(pressed && startPos.y - transform.position.y < threshHold){
                pressed = false;
                Unpressed();
            }
        }

        private void OnDrawGizmosSelected() {
            Gizmos.DrawRay(transform.position, -Vector3.up*threshHold);
        }

        public void Pressed() {
            OnPressed?.Invoke();
        }

        public void Unpressed() {
            OnUnpressed?.Invoke();
        }
    }
}
