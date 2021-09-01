/* Please make your own version of this script if you're making something greatly modified */
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SAV_KeyboardControls : UdonSharpBehaviour
{
    [SerializeField] private KeyCode Lfunc1key;
    [SerializeField] private UdonSharpBehaviour Lfunc1;
    [SerializeField] private KeyCode Lfunc2key;
    [SerializeField] private UdonSharpBehaviour Lfunc2;
    [SerializeField] private KeyCode Lfunc3key;
    [SerializeField] private UdonSharpBehaviour Lfunc3;
    [SerializeField] private KeyCode Lfunc4key;
    [SerializeField] private UdonSharpBehaviour Lfunc4;
    [SerializeField] private KeyCode Lfunc5key;
    [SerializeField] private UdonSharpBehaviour Lfunc5;
    [SerializeField] private KeyCode Lfunc6key;
    [SerializeField] private UdonSharpBehaviour Lfunc6;
    [SerializeField] private KeyCode Lfunc7key;
    [SerializeField] private UdonSharpBehaviour Lfunc7;
    [SerializeField] private KeyCode Lfunc8key;
    [SerializeField] private UdonSharpBehaviour Lfunc8;
    [SerializeField] private KeyCode Rfunc1key;
    [SerializeField] private UdonSharpBehaviour Rfunc1;
    [SerializeField] private KeyCode Rfunc2key;
    [SerializeField] private UdonSharpBehaviour Rfunc2;
    [SerializeField] private KeyCode Rfunc3key;
    [SerializeField] private UdonSharpBehaviour Rfunc3;
    [SerializeField] private KeyCode Rfunc4key;
    [SerializeField] private UdonSharpBehaviour Rfunc4;
    [SerializeField] private KeyCode Rfunc5key;
    [SerializeField] private UdonSharpBehaviour Rfunc5;
    [SerializeField] private KeyCode Rfunc6key;
    [SerializeField] private UdonSharpBehaviour Rfunc6;
    [SerializeField] private KeyCode Rfunc7key;
    [SerializeField] private UdonSharpBehaviour Rfunc7;
    [SerializeField] private KeyCode Rfunc8key;
    [SerializeField] private UdonSharpBehaviour Rfunc8;
    [Header("EngineControl only required for DoVTOL")]
    [SerializeField] SaccAirVehicle EngineControl;
    [SerializeField] private bool DoVTOL;
    private string KeyboardInput = "KeyboardInput";
    private float VTOLAngleDivider;
    private void Start()
    {
        if (EngineControl != null)
        {
            float vtolangledif = EngineControl.VTOLMaxAngle - EngineControl.VTOLMinAngle;
            VTOLAngleDivider = EngineControl.VTOLAngleTurnRate / vtolangledif;
        }
        else
        {
            DoVTOL = false;
        }
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

        if (Input.GetKeyDown(Lfunc1key))
        {
            if (Lfunc1 != null) Lfunc1.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc2key))
        {
            if (Lfunc2 != null) Lfunc2.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc3key))
        {
            if (Lfunc3 != null) Lfunc3.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc4key))
        {
            if (Lfunc4 != null) Lfunc4.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc5key))
        {
            if (Lfunc5 != null) Lfunc5.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc6key))
        {
            if (Lfunc6 != null) Lfunc6.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc7key))
        {
            if (Lfunc7 != null) Lfunc7.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc8key))
        {
            if (Lfunc8 != null) Lfunc8.SendCustomEvent(KeyboardInput);
        }


        if (Input.GetKeyDown(Rfunc1key))
        {
            if (Rfunc1 != null) Rfunc1.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc2key))
        {
            if (Rfunc2 != null) Rfunc2.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc3key))
        {
            if (Rfunc3 != null) Rfunc3.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc4key))
        {
            if (Rfunc4 != null) Rfunc4.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc5key))
        {
            if (Rfunc5 != null) Rfunc5.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc6key))
        {
            if (Rfunc6 != null) Rfunc6.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc7key))
        {
            if (Rfunc7 != null) Rfunc7.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc8key))
        {
            if (Rfunc8 != null) Rfunc8.SendCustomEvent(KeyboardInput);
        }
    }
}
