
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class EventTestScript : UdonSharpBehaviour
{
    [SerializeField] private Text DebugText;
    private int NumEvents = 26;
    private string[] Event;
    private int[] Ints;

    private void Start()
    {
        Event = new string[NumEvents];
        Ints = new int[NumEvents];
        int x = 0;

        Event[x++] = "Respawn";
        Event[x++] = "PilotEnter";
        Event[x++] = "PilotExit";
        Event[x++] = "PassengerEnter";
        Event[x++] = "PassengerExit";
        Event[x++] = "Explode";
        Event[x++] = "ReSupply";
        Event[x++] = "TakeOff";
        Event[x++] = "TouchDown";
        Event[x++] = "AfterburnerOn";
        Event[x++] = "AfterburnerOff";
        Event[x++] = "CanopyOpened";
        Event[x++] = "CanopyClosed";
        Event[x++] = "GearUp";
        Event[x++] = "GearDown";
        Event[x++] = "FlapsOn";
        Event[x++] = "FlapsOff";
        Event[x++] = "HookDown";
        Event[x++] = "HookUp";
        Event[x++] = "SmokeOn";
        Event[x++] = "SmokeOff";
        Event[x++] = "LimitsOn";
        Event[x++] = "LimitsOff";
        Event[x++] = "PlaneHit";
        Event[x++] = "TakeOwnership";
        Event[x++] = "LoseOwnership";

        x = 0;
        while (x < NumEvents)
        {
            Ints[x] = 0;
            x++;
        }
        CompileString();
    }
    private void CompileString()
    {
        DebugText.text = string.Empty;
        int x = 0;
        foreach (string txt in Event)
        {
            DebugText.text = string.Concat(DebugText.text, Ints[x], ": ", Event[x], "\n");
            x++;
        }
    }
    public void Respawn()
    {
        Ints[0] += 1;
        CompileString();
    }
    public void PilotEnter()
    {
        Ints[1] += 1;
        CompileString();
    }
    public void PilotExit()
    {
        Ints[2] += 1;
        CompileString();
    }
    public void PassengerEnter()
    {
        Ints[3] += 1;
        CompileString();
    }
    public void PassengerExit()
    {
        Ints[4] += 1;
        CompileString();
    }
    public void Explode()
    {
        Ints[5] += 1;
        CompileString();
    }
    public void ReSupply()
    {
        Ints[6] += 1;
        CompileString();
    }
    public void TakeOff()
    {
        Ints[7] += 1;
        CompileString();
    }
    public void TouchDown()
    {
        Ints[8] += 1;
        CompileString();
    }
    public void AfterburnerOn()
    {
        Ints[9] += 1;
        CompileString();
    }
    public void AfterburnerOff()
    {
        Ints[10] += 1;
        CompileString();
    }
    public void CanopyOpened()
    {
        Ints[11] += 1;
        CompileString();
    }
    public void CanopyClosed()
    {
        Ints[12] += 1;
        CompileString();
    }
    public void GearUp()
    {
        Ints[13] += 1;
        CompileString();
    }
    public void GearDown()
    {
        Ints[14] += 1;
        CompileString();
    }
    public void FlapsOn()
    {
        Ints[15] += 1;
        CompileString();
    }
    public void FlapsOff()
    {
        Ints[16] += 1;
        CompileString();
    }
    public void HookDown()
    {
        Ints[17] += 1;
        CompileString();
    }
    public void HookUp()
    {
        Ints[18] += 1;
        CompileString();
    }
    public void SmokeOn()
    {
        Ints[19] += 1;
        CompileString();
    }
    public void SmokeOff()
    {
        Ints[20] += 1;
        CompileString();
    }
    public void LimitsOn()
    {
        Ints[21] += 1;
        CompileString();
    }
    public void LimitsOff()
    {
        Ints[22] += 1;
        CompileString();
    }
    public void PlaneHit()
    {
        Ints[23] += 1;
        CompileString();
    }
    public void TakeOwnership()
    {
        Ints[24] += 1;
        CompileString();
    }
    public void LoseOwnership()
    {
        Ints[25] += 1;
        CompileString();
    }
}
