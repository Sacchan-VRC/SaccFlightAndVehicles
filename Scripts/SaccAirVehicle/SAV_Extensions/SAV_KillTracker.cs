
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
            float time = Time.time;
            if (
                EntityControl.LastAttacker
                && EntityControl.LastAttacker.Using
                && time - (float)SAVControl.GetProgramVariable("LastHitTime") < 2
                && !(bool)SAVControl.GetProgramVariable("Taxiing")
                && ((bool)SAVControl.GetProgramVariable("Occupied") || ((time - EntityControl.PilotExitTime) < 5))
                )
            {
                if (EntityControl.LastAttacker != EntityControl)
                {
                    if (KillFeed) { KillFeed.SetProgramVariable("KilledPlayerID", EntityControl.UsersID); }
                    EntityControl.SendEventToExtensions("SFEXT_O_GotKilled");
                    EntityControl.LastAttacker.SendEventToExtensions("SFEXT_O_GotAKill");
                }
            }
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
            //Debug.Log("SFEXT_O_GotAKill");
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
            if (KillFeed)
            {
                Networking.SetOwner(localPlayer, KillFeed.gameObject);
                KillFeed.SendCustomEvent("sendKillMessage");
            }
            for (int i = 0; i < Callbacks.Length; i++)
            {
                Callbacks[i].SendCustomEvent("SFKT_GotAKill");
            }
        }
        public void SFEXT_O_Suicide()
        {
            if (KillFeed)
            {
                Networking.SetOwner(localPlayer, KillFeed.gameObject);
                KillFeed.SetProgramVariable("WeaponType", (short)-1);
                KillFeed.SendCustomEvent("sendKillMessage");
            }
            for (int i = 0; i < Callbacks.Length; i++)
            {
                Callbacks[i].SendCustomEvent("SFKT_Suicided");
            }
        }
        public void SFEXT_O_GunKill()
        {
            if (KillFeed) { KillFeed.SetProgramVariable("WeaponType", (short)0); }
            for (int i = 0; i < Callbacks.Length; i++)
            {
                Callbacks[i].SendCustomEvent("SFKT_GunKill");
            }
        }
        public void SFEXT_O_MissileKill()
        {
            if (KillFeed) { KillFeed.SetProgramVariable("WeaponType", (short)1); }
            for (int i = 0; i < Callbacks.Length; i++)
            {
                Callbacks[i].SendCustomEvent("SFKT_MissileKill");
            }
        }
    }
}