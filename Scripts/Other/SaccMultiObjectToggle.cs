
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SaccMultiObjectToggle : UdonSharpBehaviour
{
    public GameObject[] ToggleObjs;
    private int current = 0;
    private void Interact()//entering the plane
    {
        current++;
        if (current == ToggleObjs.Length)
        {
            current = 0;
        }
        int o = 0;
        foreach (GameObject obj in ToggleObjs)
        {
            if (o != current)
            {
                obj.SetActive(false);
            }
            else
            {
                obj.SetActive(true);
            }
            o++;
        }
    }
}
