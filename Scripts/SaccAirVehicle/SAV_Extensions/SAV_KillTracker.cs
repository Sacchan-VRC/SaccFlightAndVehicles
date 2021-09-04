
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SAV_KillTracker : UdonSharpBehaviour
{
    [SerializeField] private SaccEntity EntityControl;
    [SerializeField] private UdonSharpBehaviour SAVControl;
    public SaccScoreboard_Kills KillsBoard;
    private bool InEditor;
    private VRCPlayerApi localPlayer;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        { InEditor = true; }
    }

    public void SFEXT_O_Explode()
    {
        //our killer increases their kills
        float time = Time.time;
        if (EntityControl.LastAttacker != null && ((bool)SAVControl.GetProgramVariable("Occupied") || (time - EntityControl.LastHitTime < 5 && !(bool)SAVControl.GetProgramVariable("Taxiing") && ((time - EntityControl.PilotExitTime) < 5))))
        {
            EntityControl.SendEventToExtensions("SFEXT_O_GotKilled");
            EntityControl.LastAttacker.SendEventToExtensions("SFEXT_O_GotKill");
        }
        //Update Kills board (person with most kills will probably show as having one less kill than they really have until they die, because synced variables will update after this)
        //should be fixed
    }
    public void SFEXT_O_GotKill()
    {
        if (KillsBoard != null && (bool)SAVControl.GetProgramVariable("Piloting"))
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
                    Networking.SetOwner(localPlayer, KillsBoard.gameObject);
                    KillsBoard.TopKiller = localPlayer.displayName;
                    KillsBoard.TopKills = KillsBoard.MyKills;
                    KillsBoard.RequestSerialization();
                    KillsBoard.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UpdateScores");
                }
            }
        }
    }
}