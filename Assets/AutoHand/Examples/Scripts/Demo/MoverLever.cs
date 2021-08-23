using UnityEngine;

namespace Autohand.Demo{
    public class MoverLever : PhysicsLever{
        public Transform move;
        public Vector3 axis;
        public float speed = 1;
    
        void Update(){
            base.Update();
            if(Mathf.Abs(leverPoint) > buffer)
                move.position = Vector3.MoveTowards(move.position, move.position-axis, Time.deltaTime*speed*(leverPoint));
        }
    }
}
