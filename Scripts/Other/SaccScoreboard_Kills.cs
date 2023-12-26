
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SaccScoreboard_Kills : UdonSharpBehaviour
    {
        public Text Scores;
        [System.NonSerializedAttribute, UdonSynced] public string TopKiller = "Nobody";

        [System.NonSerializedAttribute, UdonSynced] public ushort TopKills = 0;
        [System.NonSerializedAttribute, UdonSynced] public ushort Deadvehicles = 0;
        [System.NonSerializedAttribute] public ushort MyKills = 0;
        [System.NonSerializedAttribute] public ushort MyBestKills = 0;
        private VRCPlayerApi localPlayer;
        public override void OnDeserialization()
        {
            UpdateScores();
        }
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            UpdateScores();
        }
        public void PlaneDied()
        {
            Deadvehicles++;
            RequestSerialization();
            OnDeserialization();
        }
        public void UpdateScores()
        {
            Scores.text = string.Concat("Instance Best Killing Spree: ", TopKiller, " : ", TopKills, "\nMy Best Killing Spree: ", MyBestKills, "\nDestroyed Vehicles: ", Deadvehicles);
        }
        public void UpdateTopKiller()
        {
            if (!localPlayer.IsOwner(gameObject)) { Networking.SetOwner(localPlayer, gameObject); }
            TopKiller = localPlayer.displayName;
            TopKills = MyKills;
            RequestSerialization();
            OnDeserialization();
        }
    }
}