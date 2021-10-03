
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SaccScoreboard_Kills : UdonSharpBehaviour
{
    public string TopKiller
    {
        set
        {
            _TopKiller = value;
            UpdateScores();
        }
        get => _TopKiller;
    }
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public string _TopKiller = "Nobody";
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public int TopKills = 0;
    [System.NonSerializedAttribute] public int MyKills = 0;
    [System.NonSerializedAttribute] public int MyBestKills = 0;
    public Text Scores;
    private VRCPlayerApi localPlayer;
    private bool Initialized;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        UpdateScores();
    }
    public void UpdateScores()
    {
        Scores.text = string.Concat("Instance Best Killing Spree: ", TopKiller, " : ", TopKills, "\nMy Best Killing Spree: ", MyBestKills);
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        RequestSerialization();
    }
}
