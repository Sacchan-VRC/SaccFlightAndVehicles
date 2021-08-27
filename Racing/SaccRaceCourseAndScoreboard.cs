
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class SaccRaceCourseAndScoreboard : UdonSharpBehaviour
{
    public GameObject[] RaceCheckpoints;
    public GameObject RaceObjects;
    public Text TimeText;
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public string InstanceRecord = "Instance Record : None";
    [System.NonSerializedAttribute] [UdonSynced(UdonSyncMode.None)] public float BestTime = 999999;
    [System.NonSerializedAttribute] public string MyRecord = "My Record : None";
    [System.NonSerializedAttribute] public string MyLastTime = "My Last Time : None";
    [System.NonSerializedAttribute] public string MyName = "MyName";
    [System.NonSerializedAttribute] public float MyRecordTime = 999999;
    [System.NonSerializedAttribute] public float MyTime = 999999;
    [System.NonSerializedAttribute] public string MyPlaneType;
    private void Start()
    {
        if (Networking.LocalPlayer != null)
        {
            MyName = Networking.LocalPlayer.displayName;
        }
        UpdateTimes();
    }

    public void UpdateMyLastTime()
    {
        MyLastTime = string.Concat("My Last Time : ", MyPlaneType, " : ", MyTime);
    }
    public void UpdateMyRecord()
    {
        MyRecord = string.Concat("My Record : ", MyPlaneType, " : ", MyTime);
    }
    public void UpdateInstanceRecord()
    {
        InstanceRecord = string.Concat("Instance Record : ", MyName, " : ", MyPlaneType, " : ", BestTime);
    }
    public void UpdateTimes()
    {
        TimeText.text = string.Concat(InstanceRecord, "\n", MyRecord, "\n", MyLastTime);
    }

}
