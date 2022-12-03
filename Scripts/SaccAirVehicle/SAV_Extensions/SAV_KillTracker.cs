
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SAV_KillTracker : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        private SaccEntity EntityControl;
        [Tooltip("Leave empty if you just want to use the SFEXT_O_GotKilled and SFEXT_O_GotAKill events for something else")]
        public SaccScoreboard_Kills KillsBoard;
        private bool InEditor;
        private VRCPlayerApi localPlayer;
        public void SFEXT_L_EntityStart()
        {
            EntityControl = (SaccEntity)SAVControl.GetProgramVariable("EntityControl");
            gameObject.SetActive(false);//this object never needs to be active
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            { InEditor = true; }
        }
        public void SFEXT_G_Explode()
        {
            float time = Time.time;
            if (EntityControl.LastAttacker && EntityControl.LastAttacker.Using && !(bool)SAVControl.GetProgramVariable("Taxiing") && ((bool)SAVControl.GetProgramVariable("Occupied") || (time - (float)SAVControl.GetProgramVariable("LastHitTime") < 5 && ((time - EntityControl.PilotExitTime) < 5))))
            {
                if (EntityControl.LastAttacker != EntityControl)
                {
                    EntityControl.SendEventToExtensions("SFEXT_O_GotKilled");
                    EntityControl.LastAttacker.SendEventToExtensions("SFEXT_O_GotAKill");
                }
            }
            if (localPlayer.IsOwner(KillsBoard.gameObject)) { KillsBoard.PlaneDied(); }
        }
        public void SFEXT_O_PilotEnter()
        {
            if (KillsBoard) { KillsBoard.MyKills = 0; }
        }
        public void SFEXT_O_GotAKill()
        {
            //Debug.Log("SFEXT_O_GotAKill");
            if (KillsBoard && (bool)SAVControl.GetProgramVariable("Piloting"))
            {
                KillsBoard.MyKills++;
                if (KillsBoard.MyKills > KillsBoard.MyBestKills)
                {
                    KillsBoard.MyBestKills = KillsBoard.MyKills;
                }
                if (KillsBoard.MyKills > KillsBoard.TopKills)
                {
                    if (InEditor)
                    {
                        KillsBoard.TopKiller = "Player";
                        KillsBoard.TopKills = KillsBoard.MyKills;
                    }
                    else
                    {
                        KillsBoard.SendCustomEvent("UpdateTopKiller");
                    }
                }
            }
        }
    }
}