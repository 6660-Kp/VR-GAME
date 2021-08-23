using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace Autohand.Demo{
    public class SteamVRHandControllerLink : MonoBehaviour{
        public Hand hand;

        public SteamVR_Input_Sources handType;
        public SteamVR_Action_Single grabAction;
        public SteamVR_Action_Boolean squeezeAction;

        bool grabbing;

        public bool Grab() {
            return grabAction.GetAxis(handType) > 0.8f;
        }

        public void Update() {
            bool grab = Grab();

            if(!grabbing && grab){
                grabbing = true;
                hand.Grab();
            }

            if(grabbing && !grab) {
                grabbing = false;
                hand.Release();
            }
            
            if(squeezeAction.GetLastState(handType) != squeezeAction.GetState(handType)) {
                if(squeezeAction.GetState(handType))
                    hand.Squeeze();
                else
                    hand.Unsqueeze();
            }

            hand.SetGrip(grabAction.GetAxis(handType));
        }
    }
}
