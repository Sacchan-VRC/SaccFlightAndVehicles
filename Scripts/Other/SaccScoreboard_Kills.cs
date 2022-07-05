
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

        [System.NonSerializedAttribute, UdonSynced, FieldChangeCallback(nameof(TopKills))] public ushort _topKills = 0;
        public ushort TopKills
        {
            set
            {
                _topKills = value;
                SendCustomEventDelayedSeconds(nameof(UpdateScores), 1);//TopKiller might not be updated yet if this is done instantly
            }
            get => _topKills;
        }
        [System.NonSerializedAttribute, UdonSynced, FieldChangeCallback(nameof(DeadVehicles))] public ushort _deadvehicles = 0;
        public ushort DeadVehicles
        {
            set
            {
                _deadvehicles = value;
                SendCustomEventDelayedSeconds(nameof(UpdateScores), 1);
            }
            get => _deadvehicles;
        }
        [System.NonSerializedAttribute] public ushort MyKills = 0;
        [System.NonSerializedAttribute] public ushort MyBestKills = 0;
        private VRCPlayerApi localPlayer;
        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            UpdateScores();
        }
        public void PlaneDied()
        {
            DeadVehicles++;
            RequestSerialization();
        }
        public void UpdateScores()
        {
            Scores.text = string.Concat("Instance Best Killing Spree: ", TopKiller, " : ", TopKills, "\nMy Best Killing Spree: ", MyBestKills, "\nDestroyed Vehicles: ", DeadVehicles);
        }
        public void UpdateTopKiller()
        {
            if (!localPlayer.IsOwner(gameObject)) { Networking.SetOwner(localPlayer, gameObject); }
            TopKiller = localPlayer.displayName;
            TopKills = MyKills;
            RequestSerialization();
        }
    }
}