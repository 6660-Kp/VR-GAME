using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Autohand{
    public class HandStabilizer : MonoBehaviour{
        //This is the script that hides unstable joints without compromising joint functionality
        Hand[] hands;
        void Start(){
            hands = FindObjectsOfType<Hand>();
        }
        
        private void OnPostRender() {
            foreach(var hand in hands) {
                hand.OnPostRender();
            }
        }
        private void OnPreRender() {
            foreach(var hand in hands) {
                hand.OnPreRender();
            }
        }
    }
}
