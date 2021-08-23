using UnityEngine;

namespace Autohand.Demo{
    [RequireComponent(typeof(Rigidbody))]
    
    public class Smasher : MonoBehaviour{
        Rigidbody rb;
        public float forceMulti = 1;
        public GameObject mouse;
        private void Start() {
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision collision) {
            var smash = collision.transform.GetComponent<Smash>();
            if(smash != null) {
                if (rb.velocity.magnitude * forceMulti >= smash.smashForce)
                {
                    Hammer.attack(collision.collider);
                    var index = collision. collider.GetComponent<MouseAI>().index;
                    if(GameManager.instance.isCustom)
                        SpawnMouse(GameManager.instance.startPoints[index],index,1/GameManager.instance.mouseSpeed);
                    else
                    {
                        SpawnMouse(GameManager.instance.startPoints[index], index);
                    }
                    smash.DoSmash();
                }
            }
        }
        public void  SpawnMouse(Transform pos ,int index,float speed)
        {
            var go = Instantiate(mouse, pos.position, Quaternion.Euler(new Vector3(90,0,0)));
            go.GetComponent<MouseAI>().index = index;
            go.GetComponent<MouseAI>().stopTime = speed;
        }  
        public void  SpawnMouse(Transform pos ,int index)
        {
            Instantiate(mouse, pos.position, Quaternion.Euler(new Vector3(90,0,0))).GetComponent<MouseAI>().index = index;
        }
    }
    
}
