
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
        [System.NonSerialized] public SaccEntity EntityControl;
        [Tooltip("Leave empty if you just want to use the SFEXT_O_GotKilled and SFEXT_O_GotAKill events for something else")]
        public SaccScoreboard_Kills KillsBoard;
        private bool InEditor;
        private VRCPlayerApi localPlayer;
        [SerializeField] private UdonSharpBehaviour KillFeed;
        [SerializeField] private UdonSharpBehaviour[] Callbacks;
        public void SFEXT_L_EntityStart()
        {
            gameObject.SetActive(false);//this object never needs to be active
            localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            { InEditor = true; }
        }
        public void SFEXT_G_Wrecked()
        {
            if (KillsBoard) if (localPlayer.IsOwner(KillsBoard.gameObject)) { KillsBoard.PlaneDied(); }
        }
        public void SFEXT_O_PilotEnter()
        {
            resetMyKills();
        }
        public void SFEXT_O_OnDrop()
        {
            resetMyKills();
        }
        void resetMyKills()
        {
            if (KillsBoard) { KillsBoard.MyKills = 0; }
        }
        public void SFEXT_O_GotAKill()
        {
            //this is called on the plane that died
            SaccEntity myVehicle = EntityControl.LastAttacker;
            if (myVehicle && !(bool)SAVControl.GetProgramVariable("Taxiing"))
            {
                if (myVehicle != EntityControl)
                {
                    if (KillFeed)
                    {
                        KillFeed.SetProgramVariable("KillerPlayerID", myVehicle.UsersID);
                        KillFeed.SetProgramVariable("KilledPlayerID", EntityControl.OwnerAPI.playerId);
                        KillFeed.SetProgramVariable("WeaponType", EntityControl.LastHitWeaponType);
                        KillFeed.SendCustomEvent("sendKillMessage");
                    }
                }

                if (!(KillsBoard && EntityControl.Using)) { return; }
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
                for (int i = 0; i < Callbacks.Length; i++)
                {
                    Callbacks[i].SendCustomEvent("SFKT_GotAKill");
                }
            }
        }
        public void SFEXT_O_Suicide()
        {
            if (KillFeed)
            {
                KillFeed.SetProgramVariable("KillerPlayerID", -1);
                KillFeed.SetProgramVariable("WeaponType", (byte)0);
                KillFeed.SetProgramVariable("KilledPlayerID", localPlayer.playerId);
                KillFeed.SendCustomEvent("sendKillMessage");
            }
            for (int i = 0; i < Callbacks.Length; i++)
            {
                Callbacks[i].SendCustomEvent("SFKT_Suicided");
            }
        }
    }
}