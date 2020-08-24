
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SwivelScreenToggle : UdonSharpBehaviour
{
    public Animator Screen;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Screen.SetBool("swiveled", !Screen.GetBool("swiveled"));
        }
    }
    private void Interact()
    {
        Screen.SetBool("swiveled", !Screen.GetBool("swiveled"));
    }
}