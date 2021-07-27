
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
    [SerializeField] private bool DoVTOL;
    [SerializeField] private bool DoCruise;
    private float VTOLAngleDivider;

    private void Start()
    {
        float vtolangledif = EngineControl.VTOLMaxAngle - EngineControl.VTOLMinAngle;
        VTOLAngleDivider = EngineControl.VTOLAngleTurnRate / vtolangledif;
    }
    void Update()
    {
        if (DoVTOL)
        {
            float pgup = Input.GetKey(KeyCode.PageUp) ? 1 : 0;
            float pgdn = Input.GetKey(KeyCode.PageDown) ? 1 : 0;
            if (pgup + pgdn != 0)
            {
                EngineControl.VTOLAngleInput = Mathf.Clamp(EngineControl.VTOLAngleInput + ((pgdn - pgup) * (VTOLAngleDivider * Time.smoothDeltaTime)), 0, 1);
            }
        }
        float DeltaTime = Time.deltaTime;
        if (DoCruise)
        {
            float equals = Input.GetKey(KeyCode.Equals) ? DeltaTime * 10 : 0;
            float minus = Input.GetKey(KeyCode.Minus) ? DeltaTime * 10 : 0;
            EngineControl.SetSpeed = Mathf.Max(EngineControl.SetSpeed + (equals - minus), 0);
        }


        if (Input.GetKeyDown(Lfunc1key))
        {
            if (EngineControl.Dial_Functions_L[0] != null) EngineControl.Dial_Functions_L[0].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc2key))
        {
            if (EngineControl.Dial_Functions_L[1] != null) EngineControl.Dial_Functions_L[1].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc3key))
        {
            if (EngineControl.Dial_Functions_L[2] != null) EngineControl.Dial_Functions_L[2].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc4key))
        {
            if (EngineControl.Dial_Functions_L[3] != null) EngineControl.Dial_Functions_L[3].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc5key))
        {
            if (EngineControl.Dial_Functions_L[4] != null) EngineControl.Dial_Functions_L[4].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc6key))
        {
            if (EngineControl.Dial_Functions_L[5] != null) EngineControl.Dial_Functions_L[5].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc7key))
        {
            if (EngineControl.Dial_Functions_L[6] != null) EngineControl.Dial_Functions_L[6].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Lfunc8key))
        {
            if (EngineControl.Dial_Functions_L[7] != null) EngineControl.Dial_Functions_L[7].SendCustomEvent("KeyboardInput");
        }


        if (Input.GetKeyDown(Rfunc1key))
        {
            if (EngineControl.Dial_Functions_R[0] != null) EngineControl.Dial_Functions_R[0].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc2key))
        {
            if (EngineControl.Dial_Functions_R[1] != null) EngineControl.Dial_Functions_R[1].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc3key))
        {
            if (EngineControl.Dial_Functions_R[2] != null) EngineControl.Dial_Functions_R[2].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc4key))
        {
            if (EngineControl.Dial_Functions_R[3] != null) EngineControl.Dial_Functions_R[3].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc5key))
        {
            if (EngineControl.Dial_Functions_R[4] != null) EngineControl.Dial_Functions_R[4].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc6key))
        {
            if (EngineControl.Dial_Functions_R[5] != null) EngineControl.Dial_Functions_R[5].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc7key))
        {
            if (EngineControl.Dial_Functions_R[6] != null) EngineControl.Dial_Functions_R[6].SendCustomEvent("KeyboardInput");
        }
        if (Input.GetKeyDown(Rfunc8key))
        {
            if (EngineControl.Dial_Functions_R[7] != null) EngineControl.Dial_Functions_R[7].SendCustomEvent("KeyboardInput");
        }
    }
}
