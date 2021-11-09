/* Please make your own version of this script if you're making something greatly modified */
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SAV_KeyboardControls : UdonSharpBehaviour
{
    [SerializeField] private UdonSharpBehaviour Lfunc1;
    [SerializeField] private KeyCode Lfunc1key;
    [SerializeField] private UdonSharpBehaviour Lfunc2;
    [SerializeField] private KeyCode Lfunc2key;
    [SerializeField] private UdonSharpBehaviour Lfunc3;
    [SerializeField] private KeyCode Lfunc3key;
    [SerializeField] private UdonSharpBehaviour Lfunc4;
    [SerializeField] private KeyCode Lfunc4key;
    [SerializeField] private UdonSharpBehaviour Lfunc5;
    [SerializeField] private KeyCode Lfunc5key;
    [SerializeField] private UdonSharpBehaviour Lfunc6;
    [SerializeField] private KeyCode Lfunc6key;
    [SerializeField] private UdonSharpBehaviour Lfunc7;
    [SerializeField] private KeyCode Lfunc7key;
    [SerializeField] private UdonSharpBehaviour Lfunc8;
    [SerializeField] private KeyCode Lfunc8key;
    [SerializeField] private UdonSharpBehaviour Rfunc1;
    [SerializeField] private KeyCode Rfunc1key;
    [SerializeField] private UdonSharpBehaviour Rfunc2;
    [SerializeField] private KeyCode Rfunc2key;
    [SerializeField] private UdonSharpBehaviour Rfunc3;
    [SerializeField] private KeyCode Rfunc3key;
    [SerializeField] private UdonSharpBehaviour Rfunc4;
    [SerializeField] private KeyCode Rfunc4key;
    [SerializeField] private UdonSharpBehaviour Rfunc5;
    [SerializeField] private KeyCode Rfunc5key;
    [SerializeField] private UdonSharpBehaviour Rfunc6;
    [SerializeField] private KeyCode Rfunc6key;
    [SerializeField] private UdonSharpBehaviour Rfunc7;
    [SerializeField] private KeyCode Rfunc7key;
    [SerializeField] private UdonSharpBehaviour Rfunc8;
    [SerializeField] private KeyCode Rfunc8key;
    [Header("SAVControl only required for DoVTOL")]
    [SerializeField] UdonSharpBehaviour SAVControl;
    [SerializeField] private bool DoVTOL;
    private string KeyboardInput = "KeyboardInput";
    private float VTOLAngleDivider;
    private void Start()
    {
        if (SAVControl)
        {
            float vtolangledif = (float)SAVControl.GetProgramVariable("VTOLMaxAngle") - (float)SAVControl.GetProgramVariable("VTOLMinAngle");
            VTOLAngleDivider = (float)SAVControl.GetProgramVariable("VTOLAngleTurnRate") / vtolangledif;
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
                SAVControl.SetProgramVariable("VTOLAngleInput", Mathf.Clamp((float)SAVControl.GetProgramVariable("VTOLAngleInput") + ((pgdn - pgup) * (VTOLAngleDivider * Time.smoothDeltaTime)), 0, 1));
            }
        }

        if (Input.GetKeyDown(Lfunc1key))
        {
            if (Lfunc1) Lfunc1.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc2key))
        {
            if (Lfunc2) Lfunc2.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc3key))
        {
            if (Lfunc3) Lfunc3.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc4key))
        {
            if (Lfunc4) Lfunc4.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc5key))
        {
            if (Lfunc5) Lfunc5.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc6key))
        {
            if (Lfunc6) Lfunc6.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc7key))
        {
            if (Lfunc7) Lfunc7.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Lfunc8key))
        {
            if (Lfunc8) Lfunc8.SendCustomEvent(KeyboardInput);
        }


        if (Input.GetKeyDown(Rfunc1key))
        {
            if (Rfunc1) Rfunc1.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc2key))
        {
            if (Rfunc2) Rfunc2.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc3key))
        {
            if (Rfunc3) Rfunc3.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc4key))
        {
            if (Rfunc4) Rfunc4.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc5key))
        {
            if (Rfunc5) Rfunc5.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc6key))
        {
            if (Rfunc6) Rfunc6.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc7key))
        {
            if (Rfunc7) Rfunc7.SendCustomEvent(KeyboardInput);
        }
        if (Input.GetKeyDown(Rfunc8key))
        {
            if (Rfunc8) Rfunc8.SendCustomEvent(KeyboardInput);
        }
    }
}
