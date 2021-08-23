using UnityEngine;

namespace Autohand.Demo{
    public class Smash : MonoBehaviour{
        public float smashForce = 1;

        public GameObject effect;

        public void DelayedSmash(float delay) {
            Invoke("DoSmash", delay);
        }

        public void DoSmash(){
            var grabbable = GetComponent<Grabbable>();
            var particles = Instantiate(effect, grabbable.transform.position, grabbable.transform.rotation).GetComponent<ParticleSystem>();
        
            ParticleSystem.VelocityOverLifetimeModule module = particles.velocityOverLifetime;
            var rb = GetComponent<Rigidbody>();
            module.x = rb.velocity.x;
            module.y = rb.velocity.y;
            module.z = rb.velocity.z;

            grabbable.ForceHandsRelease();
            Destroy(gameObject);
        }
    }
}
