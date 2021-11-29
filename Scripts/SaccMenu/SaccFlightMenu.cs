#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class SaccFlightMenu : MonoBehaviour
{
    [MenuItem("SaccFlight/RenameLayers")]
    private static void SetSaccFlightLayers()
    {
        SetLayerName(23, "Hook");
        SetLayerName(24, "Catapult");
        SetLayerName(25, "AAMTargets");
        SetLayerName(26, "AGMTargets");
        SetLayerName(27, "ReSupply");
        SetLayerName(28, "Racing");
        SetLayerName(31, "OnBoardVehicleLayer");
    }
    private static void SetLayerName(int layer, string name)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
        tagManager.Update();

        var layersProperty = tagManager.FindProperty("layers");
        layersProperty.arraySize = Mathf.Max(layersProperty.arraySize, layer);
        layersProperty.GetArrayElementAtIndex(layer).stringValue = name;

        tagManager.ApplyModifiedProperties();
    }
}
#endif