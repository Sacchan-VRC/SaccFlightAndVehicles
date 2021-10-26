
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
public class SAV_KillTracker : UdonSharpBehaviour
{
    [SerializeField] private SaccEntity EntityControl;
    [SerializeField] private UdonSharpBehaviour SAVControl;
    public SaccScoreboard_Kills KillsBoard;
    private bool InEditor;
    private VRCPlayerApi localPlayer;
    void SFEXT_L_EntityStart()
    {
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
            EntityControl.SendEventToExtensions("SFEXT_O_GotKilled");
            EntityControl.LastAttacker.SendEventToExtensions("SFEXT_O_GotAKill");
        }
    }
    public void SFEXT_O_PilotEnter()
    {
        KillsBoard.MyKills = 0;
    }
    public void SFEXT_O_GotAKill()
    {
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