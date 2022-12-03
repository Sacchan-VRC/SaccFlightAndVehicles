
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccRadioZone : UdonSharpBehaviour
    {
        [Header("Stand inside the radius to enable radio, can be attached to a moving object")]
        public SaccRadioBase RadioBase;
        public float ZoneRadius;
        public VRCPlayerApi[] playersinside = new VRCPlayerApi[100];
        [System.NonSerialized] public int numPlayersInside = 0;
        int nextplayer;
        float VoiceNear;
        float VoiceFar;
        float VoiceGain;
        bool InZone;
        void Start()
        {
            SendCustomEventDelayedFrames(nameof(CheckPlayersInside), Random.Range(0, 7));
            VoiceNear = RadioBase.VoiceNear;
            VoiceFar = RadioBase.VoiceFar;
            VoiceGain = RadioBase.VoiceGain;
        }
        public void CheckPlayersInside()
        {
            SendCustomEventDelayedFrames(nameof(CheckPlayersInside), 7);
            VRCPlayerApi[] players = new VRCPlayerApi[100];
            VRCPlayerApi.GetPlayers(players);
            int numplayers = VRCPlayerApi.GetPlayerCount();
            nextplayer++;
            if (nextplayer >= numplayers)
            { nextplayer = 0; }
            VRCPlayerApi nextAPI = players[nextplayer];
            if (Vector3.Distance(nextAPI.GetPosition(), transform.position) < ZoneRadius)
            {
                if (nextAPI.isLocal)
                { RadioBase.MyZone = this; InZone = true; }
                else
                { AddPlayer(nextAPI); }
            }
            else
            {
                if (nextAPI.isLocal)
                {
                    if (InZone)
                    {
                        RadioBase.MyZone = null;
                        InZone = false;
                        SetAllVoicesDefault();
                    }
                }
                else
                { RemovePlayer(nextAPI); }
            }
        }
        public void SetAllVoicesDefault()
        {
            VRCPlayerApi[] players = new VRCPlayerApi[100];
            VRCPlayerApi.GetPlayers(players);
            int numplayers = VRCPlayerApi.GetPlayerCount();
            for (int i = 0; i < numplayers; i++)
            {
                players[i].SetVoiceDistanceNear(0);
                players[i].SetVoiceDistanceFar(25);
                players[i].SetVoiceGain(15);
            }
        }
        //this will break if a player enters a seat while within the trigger because OnPlayerTriggerExit doesn't run
        /*     public override void OnPlayerTriggerEnter(VRCPlayerApi player)
            {
                //add player to
                playersinside[numPlayersInside] = player;
                numPlayersInside++;
            }
            public override void OnPlayerTriggerExit(VRCPlayerApi player)
            {
                //if player is inside, remove
                RemovePlayer(player);
            }*/
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            //if player is inside, remove
            RemovePlayer(player);
        }
        public void AddPlayer(VRCPlayerApi player)
        {
            for (int i = 0; i < numPlayersInside; i++)
            {
                if (player == playersinside[i])
                {
                    //dont add if it's already there
                    return;
                }
            }
            playersinside[numPlayersInside] = player;
            numPlayersInside++;
        }
        public void RemovePlayer(VRCPlayerApi player)
        {
            for (int i = 0; i < numPlayersInside; i++)
            {
                if (player == playersinside[i])
                {
                    playersinside[i].SetVoiceDistanceNear(0);
                    playersinside[i].SetVoiceDistanceFar(25);
                    playersinside[i].SetVoiceGain(15);
                    //remove player
                    numPlayersInside--;
                    playersinside[i] = playersinside[numPlayersInside];
                    playersinside[numPlayersInside] = null;
                    break;
                }
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, ZoneRadius);
        }
    }
}
