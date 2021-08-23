using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Autohand{
    [RequireComponent(typeof(Grabbable))]
    public class GrabLock : MonoBehaviour{
        //THIS SCRIPT ALLOWS YOU TO HOLD AN OBJECT AFTER TRIGGER RELEASE AND CALL THIS EVENT WITH TRIGGER PRESS
        //JUST IN CASE YOU WANTED TO SHOOT WITH THE PICKUP TRIGGER
        public UnityEvent OnGrabPressed;
    }
}
