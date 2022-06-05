
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class SaccRaceCourseAndScoreboard : UdonSharpBehaviour
{
    [Tooltip("Can be used by other scripts to get the races name.")]
    public string RaceName;
    [Tooltip("All checkpoint objects for this race in order, animations are sent to them as they are passed")]
    public GameObject[] RaceCheckpoints;
    [Tooltip("Parent of all objects related to this race, including scoreboard and checkpoints")]
    public GameObject RaceObjects;
    public Text TimeText;
    public bool AllowReverse = true;
    [System.NonSerializedAttribute, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(InstanceRecord))] public string _InstanceRecord = "Instance Record : None";
    public string InstanceRecord
    {
        set
        {
            _InstanceRecord = value;
            UpdateTimes();
        }
        get => _InstanceRecord;
    }
    [System.NonSerializedAttribute, UdonSynced(UdonSyncMode.None), FieldChangeCallback(nameof(InstanceRecordReverse))] public string _InstanceRecordReverse = "(R)Instance Record : None";
    public string InstanceRecordReverse
    {
        set
        {
            _InstanceRecordReverse = value;
            UpdateTimes();
        }
        get => _InstanceRecordReverse;
    }
    [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.None)] public float BestTime = Mathf.Infinity;
    [System.NonSerializedAttribute][UdonSynced(UdonSyncMode.None)] public float BestTimeReverse = Mathf.Infinity;
    [System.NonSerializedAttribute] public string MyRecord = "My Record : None";
    [System.NonSerializedAttribute] public string MyLastTime = "My Last Time : None";
    [System.NonSerializedAttribute] public string MyName = "No-one";
    [System.NonSerializedAttribute] public string MyRecordReverse = "(R)My Record : None";
    [System.NonSerializedAttribute] public string MyLastTimeReverse = "(R)My Last Time : None";
    [System.NonSerializedAttribute] public float MyRecordTime = Mathf.Infinity;
    [System.NonSerializedAttribute] public float MyRecordTimeReverse = Mathf.Infinity;
    [System.NonSerializedAttribute] public float MyTime = Mathf.Infinity;
    [System.NonSerializedAttribute] public float MyTimeReverse = Mathf.Infinity;
    [System.NonSerializedAttribute] public string MyVehicleType = "Vehicle";
    private void Start()
    {
        UpdateMyLastTime();
        UpdateMyRecord();
        UpdateInstanceRecord();
        UpdateInstanceRecordReverse();
        UpdateTimes();
        if (Networking.LocalPlayer != null)
        {
            MyName = Networking.LocalPlayer.displayName;
        }
    }

    public void UpdateMyLastTime()
    {
        MyLastTime = string.Concat("My Last Time : ", MyVehicleType, " : ", MyTime);

        if (AllowReverse)
        {
            MyLastTimeReverse = string.Concat("(R)My Last Time : ", MyVehicleType, " : ", MyTimeReverse);
        }
        else
        {
            MyLastTimeReverse = string.Empty;
        }
        UpdateTimes();
    }
    public void UpdateMyRecord()
    {
        MyRecord = string.Concat("My Record : ", MyVehicleType, " : ", MyTime);
        if (AllowReverse)
        {
            MyRecordReverse = string.Concat("(R)My Record : ", MyVehicleType, " : ", MyTimeReverse);
        }
        else
        {
            MyRecordReverse = string.Empty;
        }
        UpdateTimes();
    }
    public void UpdateInstanceRecord()
    {
        InstanceRecord = string.Concat("Instance Record : ", MyName, " : ", MyVehicleType, " : ", BestTime);
    }
    public void UpdateInstanceRecordReverse()
    {
        InstanceRecordReverse = string.Concat("(R)Instance Record : ", MyName, " : ", MyVehicleType, " : ", BestTimeReverse);
    }
    public void UpdateTimes()
    {
        TimeText.text = string.Concat(InstanceRecord, "\n", MyRecord, "\n", MyLastTime);
        if (AllowReverse)
        {
            TimeText.text = string.Concat(TimeText.text, "\n", InstanceRecordReverse, "\n", MyRecordReverse, "\n", MyLastTimeReverse);
        }
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        RequestSerialization();
    }
}