
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SwivelScreenToggle : UdonSharpBehaviour
{
    public Animator Screen;
    void Interact()
    {
        Screen.SetBool("swiveled", !Screen.GetBool("swiveled"));
    }
}