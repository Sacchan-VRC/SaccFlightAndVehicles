
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class DFUNC_Brake : UdonSharpBehaviour
{
    [SerializeField] private bool UseLeftTrigger;
    [SerializeField] private EngineController EngineControl;
    private bool KeyboardActivated;
    public void DFUNC_Selected()
    {
        gameObject.SetActive(true);
    }
    public void DFUNC_Deselected()
    {
        EngineControl.BrakeInput = 0;
        gameObject.SetActive(false);
    }
    public void SFEXT_O_PilotExit()
    {
        EngineControl.BrakeInput = 0;
        gameObject.SetActive(false);
    }
    private void Update()
    {
        float Trigger;
        if (UseLeftTrigger)
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger"); }
        else
        { Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger"); }

        EngineControl.BrakeInput = Trigger;

        if (KeyboardActivated)
        {
            if (Input.GetKey(KeyCode.B))
            {
                EngineControl.BrakeInput = 1;
            }
            else
            {
                EngineControl.BrakeInput = 0;
                KeyboardActivated = false;
                gameObject.SetActive(false);
            }
        }
    }
    public void KeyboardInput()
    {
        gameObject.SetActive(true);
        KeyboardActivated = true;
    }
}
