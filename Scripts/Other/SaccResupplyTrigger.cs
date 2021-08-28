
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccResupplyTrigger : UdonSharpBehaviour
{
    [SerializeField] private SaccEntity SendEventTo;
    [SerializeField] private float ResupplyInitialDelay = 1;
    [SerializeField] private float ResupplyDelay = 1;
    [SerializeField] private int ResupplyLayer = 27;
    [SerializeField] private string EventName = "SFEXT_O_ReSupply";
    private float LastResupplyTime = 0;
    private int NumTriggers = 0;
    private bool InResupplyZone;
    private Collider ThisCollider;
    private bool Initialized = false;
    private void Initialize()
    {
        ThisCollider = gameObject.GetComponent<Collider>();
    }
    private void Update()
    {
        if (InResupplyZone)
        {
            if (Time.time - LastResupplyTime > ResupplyDelay)
            {
                LastResupplyTime = Time.time;
                SendEventTo.SendEventToExtensions(EventName);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.gameObject.layer == ResupplyLayer)
        {
            float tim = Time.time;
            if (NumTriggers == 0) { LastResupplyTime = Mathf.Min((tim + ResupplyDelay) - ResupplyInitialDelay, tim); }
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
}
