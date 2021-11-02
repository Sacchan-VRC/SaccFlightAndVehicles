
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SaccScoreboard_Kills : UdonSharpBehaviour
{
    [System.NonSerializedAttribute] [UdonSynced, FieldChangeCallback(nameof(TopKiller))] public string _topKiller = "Nobody";
    public string TopKiller
    {
        set
        {
            _topKiller = value;
            SendCustomEventDelayedSeconds(nameof(UpdateScores), 1);//TopKills can be not updated yet if this is done instantly
        }
        get => _topKiller;
    }
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
        //Debug.Log("UpdateScores");
        Scores.text = string.Concat("Instance Best Killing Spree: ", TopKiller, " : ", TopKills, "\nMy Best Killing Spree: ", MyBestKills);
    }
}
