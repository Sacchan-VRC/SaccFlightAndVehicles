
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class KeyboardControls : UdonSharpBehaviour
{
    [SerializeField] EngineController EngineControl;
    [SerializeField] private KeyCode Lfunc1key;
    [SerializeField] private KeyCode Lfunc2key;
    [SerializeField] private KeyCode Lfunc3key;
    [SerializeField] private KeyCode Lfunc4key;
    [SerializeField] private KeyCode Lfunc5key;
    [SerializeField] private KeyCode Lfunc6key;
    [SerializeField] private KeyCode Lfunc7key;
    [SerializeField] private KeyCode Lfunc8key;
    [SerializeField] private KeyCode Rfunc1key;
    [SerializeField] private KeyCode Rfunc2key;
    [SerializeField] private KeyCode Rfunc3key;
    [SerializeField] private KeyCode Rfunc4key;
    [SerializeField] private KeyCode Rfunc5key;
    [SerializeField] private KeyCode Rfunc6key;
    [SerializeField] private KeyCode Rfunc7key;
    [SerializeField] private KeyCode Rfunc8key;
    void Update()
    {
        if (Input.GetKeyDown(Lfunc1key))
        {
            EngineControl.Dial_Functions_L[0].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc2key))
        {
            EngineControl.Dial_Functions_L[1].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc3key))
        {
            EngineControl.Dial_Functions_L[2].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc4key))
        {
            EngineControl.Dial_Functions_L[3].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc5key))
        {
            EngineControl.Dial_Functions_L[4].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc6key))
        {
            EngineControl.Dial_Functions_L[5].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc7key))
        {
            EngineControl.Dial_Functions_L[6].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc8key))
        {
            EngineControl.Dial_Functions_L[7].SendCustomEvent("KeyboardInput");
        }


        if (Input.GetKeyDown(Rfunc1key))
        {
            EngineControl.Dial_Functions_R[0].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc2key))
        {
            EngineControl.Dial_Functions_R[1].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc3key))
        {
            EngineControl.Dial_Functions_R[2].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc4key))
        {
            EngineControl.Dial_Functions_R[3].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc5key))
        {
            EngineControl.Dial_Functions_R[4].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc6key))
        {
            EngineControl.Dial_Functions_R[5].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc7key))
        {
            EngineControl.Dial_Functions_R[6].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc8key))
        {
            EngineControl.Dial_Functions_R[7].SendCustomEvent("KeyboardInput");
        }
    }
}
