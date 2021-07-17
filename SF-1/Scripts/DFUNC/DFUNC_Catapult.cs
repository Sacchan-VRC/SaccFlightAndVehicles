
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Catapult : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private GameObject Dial_Funcon;
    private bool Dial_FunconNULL = true;
    private bool TriggerLastFrame;
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_L_ECStart()
    {
        Dial_FunconNULL = Dial_Funcon == null;
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
    }
    private void SFEXT_O_PilotExit()
    {
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PilotEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.CatapultStatus == 1);
    }
    public void SFEXT_O_PassengerEnter()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(EngineControl.CatapultStatus == 1);
    }
    public void SFEXT_O_CatapultLocked()
    {
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(true);
    }
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

        if (!TriggerLastFrame)
        {
            if (EngineControl.CatapultStatus == 1)
            {
                EngineControl.CatapultStatus = 2;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchCatapult");
            }
        }
        else { TriggerLastFrame = false; }
    }
    public void KeyboardInput()
    {
        if (EngineControl.CatapultStatus == 1)
        {
            EngineControl.CatapultStatus = 2;
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "LaunchCatapult");
        }
    }
    public void LaunchCatapult()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "CatapultLaunchEffects");
        if (!Dial_FunconNULL) Dial_Funcon.SetActive(false);
    }
}
