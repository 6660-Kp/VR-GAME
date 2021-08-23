using UnityEngine;

namespace Autohand.Demo{
    public class FingerBend : MonoBehaviour{
        public Hand hand;
        public Finger thumb;
        public Finger index;
        public Finger middle;
        public Finger ring;
        public Finger pinky;

        [Range(0, 1)]
        public float thumbBend = 1;
        [Range(0, 1)]
        public float indexBend = 0;
        [Range(0, 1)]
        public float middleBend = 1;
        [Range(0, 1)]
        public float ringBend = 1;
        [Range(0, 1)]
        public float pinkyBend = 1;

        void Start(){
            hand.OnSqueezed += OnSqueezed;
            hand.OnUnsqueezed += OnUnsqueezed;
        }
        

        void OnSqueezed(Hand hand, Grabbable grab) {
            Bend();
        }


        void OnUnsqueezed(Hand hand, Grabbable grab) {
            Unbend();
        }
        
        public void Bend() {
            thumb.bendOffset += thumbBend;
            index.bendOffset += indexBend;
            middle.bendOffset += middleBend;
            ring.bendOffset += ringBend;
            pinky.bendOffset += pinkyBend;
        }

        
        public void Unbend() {
            thumb.bendOffset -= thumbBend;
            index.bendOffset -= indexBend;
            middle.bendOffset -= middleBend;
            ring.bendOffset -= ringBend;
            pinky.bendOffset -= pinkyBend;
        }
    }
}
