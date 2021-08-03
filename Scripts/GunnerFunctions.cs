
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GunnerFunctions : UdonSharpBehaviour
{
    public UdonSharpBehaviour[] ExtensionUdonBehaviours;
    public UdonSharpBehaviour[] Dial_Functions_L;
    public UdonSharpBehaviour[] Dial_Functions_R;
    private UdonSharpBehaviour CurrentSelectedFunctionL;
    private UdonSharpBehaviour CurrentSelectedFunctionR;
    void Start()
    {

    }
}
