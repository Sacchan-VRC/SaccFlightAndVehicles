
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class ViewScreenController : UdonSharpBehaviour
{
    public LayerMask AAMTargetsLayer;
    public float DisableDistance = 15;
    public Camera PlaneCamera;
    public GameObject ViewScreen;
    public Text ChannelNumberText;
    [System.NonSerializedAttribute] [HideInInspector] public GameObject[] AAMTargets = new GameObject[80];
    [UdonSynced(UdonSyncMode.None)] public int AAMTarget;
    [System.NonSerializedAttribute] [HideInInspector] public int NumAAMTargets = 0;
    [System.NonSerializedAttribute] [HideInInspector] public VRCPlayerApi localPlayer;
    [System.NonSerializedAttribute] [HideInInspector] public bool Disabled = true;
    [System.NonSerializedAttribute] [HideInInspector] public bool InEditor = true;
    int currenttarget = -1;
    EngineController TargetEngine;
    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (localPlayer != null) InEditor = false;
        //get array of AAM Targets
        RaycastHit[] aamtargs = Physics.SphereCastAll(gameObject.transform.position, 1000000, gameObject.transform.forward, 5, AAMTargetsLayer, QueryTriggerInteraction.Collide);
        NumAAMTargets = aamtargs.Length;

        //populate AAMTargets list
        int n = 0;
        foreach (RaycastHit target in aamtargs)
        {
            EngineController TargetEngineStart = gameObject.GetComponent<EngineController>();//this returns null but unity complains if it's not 'initialized'
            if (target.collider.transform.parent != null)
                TargetEngineStart = target.collider.transform.parent.GetComponent<EngineController>();

            if (TargetEngineStart != null)
            {
                AAMTargets[n] = target.collider.gameObject;
                n++;
            }
            else NumAAMTargets -= 1;
        }
        n = 0;
        //create a unique number based on position in the hierarchy in order to sort the AAMTargets array later, to make sure they're the same among clients 
        float[] order = new float[NumAAMTargets];
        for (int i = 0; AAMTargets[n] != null; i++)
        {
            Transform parent = AAMTargets[n].transform;
            for (int x = 0; parent != null; x++)
            {
                order[n] = float.Parse(order[n].ToString() + parent.transform.GetSiblingIndex().ToString());
                parent = parent.transform.parent;
            }
            n++;
        }
        //sort AAMTargets array based on order
        if (NumAAMTargets > 0)
        {
            SortTargets(AAMTargets, order);
        }
    }

    private void Update()
    {
        if (!Disabled)
        {
            //check for change in target
            if (currenttarget != AAMTarget)
            {
                if (AAMTargets[AAMTarget] != null && AAMTargets[AAMTarget].transform.parent != null)
                {
                    TargetEngine = AAMTargets[AAMTarget].transform.parent.GetComponent<EngineController>();
                }
            }
            currenttarget = AAMTarget;
            //disable if far away
            if (!InEditor)
            {
                if (Vector3.Distance(localPlayer.GetPosition(), gameObject.transform.position) > DisableDistance)
                {
                    ViewScreen.SetActive(false);
                    PlaneCamera.gameObject.SetActive(false);
                    Disabled = true;
                }
            }
            if (TargetEngine.SoundControl.ThisFrameDist > 2000f && !TargetEngine.IsOwner)
                TargetEngine.EffectsControl.Effects();//this is skipped in effectscontroller as an optimization if plane is distant, but the camera can see it close up, so do it here.

            PlaneCamera.transform.rotation = Quaternion.Slerp(PlaneCamera.transform.rotation, AAMTargets[AAMTarget].transform.rotation, 8f * Time.deltaTime);
            Vector3 temp = new Vector3(0, 14, -50);
            temp = TargetEngine.VehicleMainObj.transform.TransformDirection(temp);
            PlaneCamera.transform.position = (AAMTargets[AAMTarget].transform.position + temp);

            ChannelNumberText.text = (AAMTarget + 1).ToString();
        }
    }

    void SortTargets(GameObject[] Targets, float[] order)
    {
        for (int i = 1; i < order.Length; i++)
        {
            for (int j = 0; j < (order.Length - i); j++)
            {
                if (order[j] > order[j + 1])
                {
                    var h = order[j + 1];
                    order[j + 1] = order[j];
                    order[j] = h;
                    var k = Targets[j + 1];
                    Targets[j + 1] = Targets[j];
                    Targets[j] = k;
                }
            }
        }
    }
}
