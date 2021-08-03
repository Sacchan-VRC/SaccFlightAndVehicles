
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ResupplyTrigger : UdonSharpBehaviour
{
    [SerializeField] private EngineController EngineControl;
    [SerializeField] private float ResupplyDelay = 1;
    private float LastResupplyTime = 0;
    private int ResupplyLayer = 0;
    private int NumTriggers = 0;
    private bool InResupplyZone;
    private Collider ThisCollider;
    private bool Initialized = false;
    private void Initialize()
    {
        ResupplyLayer = LayerMaskToLayer(EngineControl.ResupplyLayer);
        ThisCollider = gameObject.GetComponent<Collider>();
    }
    private void Update()
    {
        if (InResupplyZone)
        {
            if (Time.time - LastResupplyTime > ResupplyDelay)
            {
                LastResupplyTime = Time.time;
                EngineControl.SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "ResupplyPlane");
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.layer == ResupplyLayer)
        {
            if (NumTriggers == 0) { LastResupplyTime = Time.time; }//minimum time before resupply is the resupplytime
            NumTriggers += 1;
            InResupplyZone = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.gameObject.layer == ResupplyLayer)
        {
            NumTriggers -= 1;
            if (NumTriggers == 0) { InResupplyZone = false; }
        }
    }
    //collider enabled and disabled so that it does ontriggerenter on enable
    private void OnEnable()
    {
        if (!Initialized) { Initialize(); }
        ThisCollider.enabled = true;
    }
    private void OnDisable()
    {
        ThisCollider.enabled = false;
        InResupplyZone = false;
        NumTriggers = 0;
    }
    private int LayerMaskToLayer(int bitmask)
    {
        int result = bitmask > 0 ? 0 : 31;
        while (bitmask > 1)
        {
            bitmask = bitmask >> 1;
            result++;
        }
        return result;
    }
}
