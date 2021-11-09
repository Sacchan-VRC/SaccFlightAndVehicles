
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SaccScoreboard_Kills : UdonSharpBehaviour
{
    [SerializeField] private Text Scores;
    [System.NonSerializedAttribute, UdonSynced] public string TopKiller = "Nobody";

    [System.NonSerializedAttribute, UdonSynced, FieldChangeCallback(nameof(TopKills))] public int _topKills = 0;
    public int TopKills
    {
        set
        {
            _topKills = value;
            SendCustomEventDelayedSeconds(nameof(UpdateScores), 1);//TopKiller might not be updated yet if this is done instantly
        }
        get => _topKills;
    }
    [System.NonSerializedAttribute] public int MyKills = 0;
    [System.NonSerializedAttribute] public int MyBestKills = 0;
    private VRCPlayerApi localPlayer;
    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        UpdateScores();
    }
    public void UpdateScores()
    {
        Scores.text = string.Concat("Instance Best Killing Spree: ", TopKiller, " : ", TopKills, "\nMy Best Killing Spree: ", MyBestKills);
    }
    public void UpdateTopKiller()
    {
        if (!localPlayer.IsOwner(gameObject)) { Networking.SetOwner(localPlayer, gameObject); }
        TopKiller = localPlayer.displayName;
        TopKills = MyKills;
        RequestSerialization();
    }
}
