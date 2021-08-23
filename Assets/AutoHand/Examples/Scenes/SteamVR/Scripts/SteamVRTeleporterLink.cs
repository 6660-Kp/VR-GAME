using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

namespace Autohand.Demo{
    public class SteamVRTeleporterLink : MonoBehaviour{
        public Teleporter teleport;
        public SteamVR_Input_Sources handType;
        public SteamVR_Action_Boolean teleportAction;
        bool teleporting;
        
        private void Update() {
            if(teleportAction.GetLastState(handType) != teleportAction.GetState(handType)) {
                if(teleportAction.GetState(handType))
                    teleport.StartTeleport();
                else
                    teleport.Teleport();
            }
        }
    }
}
