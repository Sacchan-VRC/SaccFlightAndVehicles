﻿#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;
using TMPro;
using UdonSharp;
using System.Reflection;

namespace SaccFlightAndVehicles
{
    public class SaccFlightMenu : MonoBehaviour
    {
        [MenuItem("SaccFlight/RenameLayers", false, 0)]
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
        [MenuItem("SaccFlight/SetUpReferenceCameraForFlight", false, 1)]
        public static void SetUpReferenceCameraForFlight()
        {
            var Descrip = GetAllVRC_SceneDescriptors();
            foreach (var d in Descrip)
            {
                if (d.ReferenceCamera) { return; }
                else
                {
                    var newcam = new GameObject("REFCAM");
                    newcam.SetActive(false);
                    var cam = newcam.AddComponent<Camera>();
                    cam.nearClipPlane = .3f;
                    cam.farClipPlane = 50000f;
                    newcam.transform.parent = d.transform;
                    d.ReferenceCamera = newcam;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(d);
                    EditorUtility.SetDirty(d);
                }
            }
        }
        static List<VRC.SDKBase.VRC_SceneDescriptor> GetAllVRC_SceneDescriptors()
        {
            var ls = new List<VRC.SDKBase.VRC_SceneDescriptor>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<VRC.SDKBase.VRC_SceneDescriptor>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
    }

    [InitializeOnLoadAttribute]
    public static class PlayModeStateChanged
    {
        static PlayModeStateChanged()
        {
            EditorApplication.playModeStateChanged += SetUpSaccFlightStuff;
        }

        private static void SetUpSaccFlightStuff(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                SetObjectReferences.SaccFlightSetup();
                EditorApplication.playModeStateChanged -= SetUpSaccFlightStuff;
            }
        }
    }
    public class SetObjectReferences : Editor, IVRCSDKBuildRequestedCallback
    {
        public int callbackOrder => 10;
        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            SaccFlightSetup();
            return true;
        }
        [MenuItem("SaccFlight/Debug_OnBuild_SetReferences", false, 1000)]
        public static void SaccFlightSetup()
        {
            SetUpCameras();//sets up saccviewscreencontroller
            SetUpRaceButtons();
            SetUpKillTrackers();
            SetUpWindChangers();
            SetUpVehicleMenu();
            DisableInVehicleOnlys();
            SetEntityTargets();//sets list of targets in each vehicle's saccentity
            SetPlaneList_SaccVehicleEnterer();//^ for SaccVehicleEnterer
            SetPlaneList_RadioBase();
            SaccFlightMenu.SetUpReferenceCameraForFlight();
        }
        public static void SetPlaneList_RadioBase()
        {
            var RBs = GetAllSaccRadioBases().ToArray();
            var Radios = GetAllSAVRadios().ToArray();
            var RZs = GetAllSaccRadioZones().ToArray();
            for (int i = 0; i < RBs.Length; i++)
            {
                RBs[i].AllRadios_RD = Radios;
                RBs[i].RadioZones = RZs;
                PrefabUtility.RecordPrefabInstancePropertyModifications(RBs[i]);
                EditorUtility.SetDirty(RBs[i]);
            }
        }
        public static void SetPlaneList_SaccVehicleEnterer()
        {
            var SEs = GetAllSaccEntitys().ToArray();
            var VEs = GetAllSaccVehicleEnterers().ToArray();
            var SETransforms = new Transform[SEs.Length];
            for (int i = 0; i < SEs.Length; i++)
            {
                SETransforms[i] = SEs[i].transform;
            }
            for (int i = 0; i < VEs.Length; i++)
            {
                VEs[i].AllPlanes = SETransforms;
                PrefabUtility.RecordPrefabInstancePropertyModifications(VEs[i]);
                EditorUtility.SetDirty(VEs[i]);
            }
        }
        public static void DisableInVehicleOnlys()
        {
            var SEs = GetAllSaccEntitys().ToArray();
            foreach (var se in SEs)
            {
                for (int i = 0; i < se.EnableInVehicle.Length; i++)
                { if (se.EnableInVehicle[i]) se.EnableInVehicle[i].SetActive(false); }
                PrefabUtility.RecordPrefabInstancePropertyModifications(se);
                EditorUtility.SetDirty(se);
            }
        }
        public static void SetEntityTargets()
        {
            var SEs = GetAllSaccEntitys().ToArray();
            foreach (var se in SEs)
            {
                var Targets = GetAllAAMTargets(se.AAMTargetsLayer);
                //remove any AAMTargets that are child of this SaccEntity from list
                foreach (var g in Targets)
                {
                    bool breakNow = false;
                    Transform searchTransform = g.transform;
                    while (searchTransform)
                    {
                        if (searchTransform.gameObject == se.gameObject)
                        {
                            Targets.Remove(g);
                            breakNow = true;
                            break;
                        }
                        searchTransform = searchTransform.parent;
                    }
                    if (breakNow) { break; }
                }
                if (Targets.Count > 0)
                { se.AAMTargets = Targets.ToArray(); }
                else
                {
                    //prevent null errors if there's only 1 vehicle in the scene by putting an object in the array
                    se.AAMTargets = new GameObject[1];
                    se.AAMTargets[0] = se.transform.root.gameObject;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(se);
                EditorUtility.SetDirty(se);
            }
        }
        public static void SetUpVehicleMenu()
        {
            var SEs = GetAllSaccEntitys().ToArray();
            var menus = GetAllSaccFlightVehicleMenus();
            var SF = GetAllSaccFlights();
            foreach (SaccFlightVehicleMenu menu in menus)
            {
                menu.Vehicles = SEs;
                if (SF.Count > 0)
                {
                    menu.SaccFlight = SF[0];
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(menu);
                EditorUtility.SetDirty(menu);
            }
        }
        public static void SetUpWindChangers()
        {
            var windchangers = GetAllSAV_WindChangers();
            var SAVs = GetAllSaccAirVehicles().ToArray();
            foreach (var WC in windchangers)
            {
                WC.SaccAirVehicles = SAVs;
                PrefabUtility.RecordPrefabInstancePropertyModifications(WC);
                EditorUtility.SetDirty(WC);
            }
        }
        public static void SetUpKillTrackers()
        {
            var killTrackers = GetAllSAV_KillTrackers();
            var killBoards = GetAllSaccScoreboard_Kills().ToArray();
            if (killBoards.Length > 0)
            {
                foreach (SAV_KillTracker KT in killTrackers)
                {
                    if (!KT.KillsBoard)
                    {
                        KT.KillsBoard = killBoards[0];
                        PrefabUtility.RecordPrefabInstancePropertyModifications(KT);
                        EditorUtility.SetDirty(KT);
                    }
                }
            }
        }
        public static void SetUpRaceButtons()
        {
            var RacingTriggers = GetAllSaccRacingTriggers().ToArray(); ;
            var RaceToggleButtons = GetAllSaccRaceToggleButtons().ToArray();
            if (RaceToggleButtons.Length > 0)
            {
                foreach (SaccRacingTrigger SRT in RacingTriggers)
                {
                    if (!SRT.RaceToggler)
                    {
                        SRT.RaceToggler = RaceToggleButtons[0];
                        PrefabUtility.RecordPrefabInstancePropertyModifications(SRT);
                        EditorUtility.SetDirty(SRT);
                    }
                }
            }
            var Races = GetAllSaccRaceCourseAndScoreboards().ToArray();
            foreach (SaccRaceToggleButton RTB in RaceToggleButtons)
            {
                RTB.RacingTriggers = RacingTriggers;
                RTB.Races = Races;
                PrefabUtility.RecordPrefabInstancePropertyModifications(RTB);
                EditorUtility.SetDirty(RTB);
            }
        }
        public static void SetUpCameras()
        {
            var SVS = GetAllSaccViewScreenControllers();
            foreach (var screen in SVS)
            {
                if (screen.CamPosAutoFill)
                {
                    var campositions = GetAllSceneCameraPoints(screen.CamposSuffix).ToArray();
                    PutCamPositionsInArray(screen, campositions);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(screen);
                    EditorUtility.SetDirty(screen);
                }
            }
        }
        public static void PutCamPositionsInArray(SaccViewScreenController viewscreen, Transform[] CamTransform)
        {
            viewscreen.CamPositions = CamTransform;
        }
        static List<SaccVehicleEnterer> GetAllSaccVehicleEnterers()
        {
            var ls = new List<SaccVehicleEnterer>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccVehicleEnterer>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SAV_Radio> GetAllSAVRadios()
        {
            var ls = new List<SAV_Radio>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SAV_Radio>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccRadioBase> GetAllSaccRadioBases()
        {
            var ls = new List<SaccRadioBase>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccRadioBase>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccRadioZone> GetAllSaccRadioZones()
        {
            var ls = new List<SaccRadioZone>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccRadioZone>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<Transform> GetAllSceneCameraPoints(string CamSuffix, Transform tr = null, List<Transform> ls = null)
        {
            if (tr == null)
            {
                ls = ls ?? new List<Transform>();
                foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects()) GetAllSceneCameraPoints(CamSuffix, g.transform, ls);
            }
            else
            {
                if (tr.name.StartsWith(CamSuffix, System.StringComparison.InvariantCultureIgnoreCase)) ls.Add(tr);
                foreach (Transform t in tr) GetAllSceneCameraPoints(CamSuffix, t, ls);
            }
            return ls;
        }
        static List<SaccViewScreenController> GetAllSaccViewScreenControllers()
        {
            var ls = new List<SaccViewScreenController>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccViewScreenController>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccRacingTrigger> GetAllSaccRacingTriggers()
        {
            var ls = new List<SaccRacingTrigger>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccRacingTrigger>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccRaceToggleButton> GetAllSaccRaceToggleButtons()
        {
            var ls = new List<SaccRaceToggleButton>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccRaceToggleButton>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccRaceCourseAndScoreboard> GetAllSaccRaceCourseAndScoreboards()
        {
            var ls = new List<SaccRaceCourseAndScoreboard>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccRaceCourseAndScoreboard>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SAV_KillTracker> GetAllSAV_KillTrackers()
        {
            var ls = new List<SAV_KillTracker>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SAV_KillTracker>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccScoreboard_Kills> GetAllSaccScoreboard_Kills()
        {
            var ls = new List<SaccScoreboard_Kills>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccScoreboard_Kills>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SAV_WindChanger> GetAllSAV_WindChangers()
        {
            var ls = new List<SAV_WindChanger>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SAV_WindChanger>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccAirVehicle> GetAllSaccAirVehicles()
        {
            var ls = new List<SaccAirVehicle>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccAirVehicle>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccEntity> GetAllSaccEntitys()
        {
            var ls = new List<SaccEntity>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccEntity>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccFlightVehicleMenu> GetAllSaccFlightVehicleMenus()
        {
            var ls = new List<SaccFlightVehicleMenu>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccFlightVehicleMenu>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<SaccFlight> GetAllSaccFlights()
        {
            var ls = new List<SaccFlight>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<SaccFlight>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
        static List<GameObject> GetAllAAMTargets(LayerMask layers)
        {
            var ls = new List<GameObject>();
            var sceneobjs = GetAllSceneObjects();
            int obs = 0;
            List<int> LayerList = ListLayers(layers);
            foreach (GameObject g in sceneobjs)
            {
                obs++;
                if (LayerList.Contains(g.layer))
                {
                    ls.Add(g);
                }
            }
            return ls;
        }
        static List<GameObject> GetAllSceneObjects()
        {
            List<GameObject> objectsInScene = new List<GameObject>();

            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
            {
                if (!EditorUtility.IsPersistent(go.transform.root.gameObject) && !(go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave))
                    objectsInScene.Add(go);
            }
            return objectsInScene;
        }
        static List<int> ListLayers(LayerMask layerMask)//thx https://answers.unity.com/questions/1135055/how-to-get-all-layers-included-in-a-layermask.html
        {
            List<int> ls = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                if (layerMask == (layerMask | (1 << i)))
                {
                    ls.Add(i);
                }
            }
            return ls;
        }
    }
    public class ColliderRenamer : EditorWindow
    {
        [MenuItem("SaccFlight/Make All Static Colliders Tarmac...", false, 4)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ColliderRenamer));
        }
        void OnGUI()
        {
            GUILayout.Label("This button will add '_0' to the end of the names of every\n object that is set to static, has a collider\n and whos name ends with a number.\n This has the effect of making wheels see them as tarmac");
            if (GUILayout.Button("RenameObjects"))
            {
                MakeAllStaticCollidersTarmac();
            }
        }
        public static void MakeAllStaticCollidersTarmac()
        {
            var staticColliders = GetAllColliders();
            foreach (Collider col in staticColliders)
            {
                if (col.gameObject.isStatic)
                {
                    if (col.gameObject.name.Length > 0)
                    {
                        int nameLastChar = col.gameObject.name[col.gameObject.name.Length - 1];
                        if (nameLastChar >= '1' && nameLastChar <= '9')
                        {
                            char[] objname = col.gameObject.name.ToCharArray();
                            char[] newname = new char[col.gameObject.name.Length + 2];
                            for (int i = 0; i < objname.Length; i++)
                            {
                                newname[i] = objname[i];
                            }
                            newname[newname.Length - 2] = '_';
                            newname[newname.Length - 1] = '0';
                            col.gameObject.name = new string(newname);
                        }
                    }
                }
            }
        }
        static List<Collider> GetAllColliders()
        {
            var ls = new List<Collider>();
            foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var objs = g.GetComponentsInChildren<Collider>(true);
                ls.AddRange(objs);
            }
            return ls;
        }
    }
    [CustomEditor(typeof(SaccViewScreenController))]
    public class SaccViewScreenControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            SaccViewScreenController _target = target as SaccViewScreenController;
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            DrawDefaultInspector();
            if (GUILayout.Button("Find Campositions In Scene") && _target.CamPosAutoFill)
            {
                Undo.RecordObject(target, "Array objects changed");
                var AllCamsArray = GetAllSceneCameraPositions(_target.CamposSuffix).ToArray();
                PutCamPositionsInArray(_target, AllCamsArray);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                EditorUtility.SetDirty(_target);//needed
            }
        }
        public void PutCamPositionsInArray(SaccViewScreenController input, Transform[] pos)
        {
            input.CamPositions = pos;
        }
        List<Transform> GetAllSceneCameraPositions(string CamSuffix, Transform tr = null, List<Transform> ls = null)
        {
            if (tr == null)
            {
                ls = ls ?? new List<Transform>();
                foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects()) GetAllSceneCameraPositions(CamSuffix, g.transform, ls);
            }
            else
            {
                if (tr.name.StartsWith(CamSuffix, System.StringComparison.InvariantCultureIgnoreCase)) ls.Add(tr);
                foreach (Transform t in tr) GetAllSceneCameraPositions(CamSuffix, t, ls);
            }
            return ls;
        }
    }
    public class CreateStickDisplay : EditorWindow
    {
        [MenuItem("SaccFlight/Create Stick Displays (Selected)", false, 2)]
        static void CreateDisplays()
        {
            GameObject ui_canvas = null;
            ui_canvas = SetupDisplay(false, ui_canvas);
            SetupDisplay(true, ui_canvas);
            if (ui_canvas)
            { Selection.activeObject = ui_canvas; }
        }
        static GameObject SetupDisplay(bool isR, GameObject ui_canvas)
        {
            Transform selectedTransform = (Selection.activeObject as GameObject).transform;
            SaccEntity SE = null;
            SAV_PassengerFunctionsController PEVC = null;
            Transform checkTrans = selectedTransform.transform;
            while (SE == null && PEVC == null && checkTrans != null)
            {
                SE = checkTrans.GetComponent<SaccEntity>();
                PEVC = checkTrans.GetComponent<SAV_PassengerFunctionsController>();
                checkTrans = checkTrans.parent;
            }
            if (SE == null && PEVC == null)
            {
                Debug.LogError("Failed to Find SaccEntity or PassengerFunctions");
                return null;
            }
            if (SE)
            {
                if (isR)
                {
                    if (SE.Dial_Functions_R.Length == 0) { Debug.LogWarning("No functions in list"); return null; }
                    return CreateDisplay(SE.Dial_Functions_R, SE.RightDialDivideStraightUp, isR, SE.transform, ui_canvas);
                }
                else
                {
                    if (SE.Dial_Functions_L.Length == 0) { Debug.LogWarning("No functions in list"); return null; }
                    return CreateDisplay(SE.Dial_Functions_L, SE.LeftDialDivideStraightUp, isR, SE.transform, ui_canvas);
                }
            }
            else
            {
                if (isR)
                {
                    if (PEVC.Dial_Functions_R.Length == 0) { Debug.LogWarning("No functions in list"); return null; }
                    return CreateDisplay(PEVC.Dial_Functions_R, PEVC.RightDialDivideStraightUp, isR, PEVC.transform, ui_canvas);
                }
                else
                {
                    if (PEVC.Dial_Functions_L.Length == 0) { Debug.LogWarning("No functions in list"); return null; }
                    return CreateDisplay(PEVC.Dial_Functions_L, PEVC.LeftDialDivideStraightUp, isR, PEVC.transform, ui_canvas);
                }
            }
        }
        static GameObject CreateDisplay(UdonSharpBehaviour[] Funcs, bool DivideStraightUp, bool isR, Transform Vehicle, GameObject ui_canvas)
        {
            string MFDMeshID = "a753c14a91335054282f470f1c93f533"; //MFD.fbx
            GameObject MFDMesh = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(MFDMeshID));
            // Check if the object has been loaded successfully
            if (!MFDMesh)
            {
                Debug.LogError("MFD Mesh not found");
                return null;
            }
            Transform selectedTransform = (Selection.activeObject as GameObject).transform;
            int numDFUNCsL = Funcs.Length;
            string meshNameDivider = "StickDisplay";
            string meshNameHighlighter = "StickDisplayHighlighter";
            string meshNameFuncon = "MFD_display_funcon";
            if (numDFUNCsL != 8) // 8 is special case (old default)
            {
                meshNameDivider += numDFUNCsL.ToString();
            }

            Transform parentOfSelected = selectedTransform.parent;

            MeshFilter[] filters = MFDMesh.GetComponentsInChildren<MeshFilter>();

            // Get the material from the asset database by its ID
            string materialID = "7181dec3c88033948a9d183e29921d60";
            Material mat_MFD = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialID));
            materialID = "926bfdc1b229b1e488e1b45dd5701356";
            Material mat_MFD_funcon = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialID));
            materialID = "80242fa653591a446aec58ab8512bf04";
            Material mat_MFD_Smoke_funcon = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialID));
            if (!ui_canvas)
            {
                ui_canvas = new GameObject("StickDisplayCanvas");
                ui_canvas.layer = 31;
                // Add a RectTransform to the canvas
                RectTransform rectTransform = ui_canvas.AddComponent<RectTransform>();
                rectTransform.anchoredPosition3D = Vector3.zero;
                rectTransform.sizeDelta = new Vector2(.1f, .1f);
                rectTransform.pivot = new Vector2(.5f, .5f);
                CanvasScaler canvasScaler = ui_canvas.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                ui_canvas.transform.parent = parentOfSelected;
                ui_canvas.transform.position = Vehicle.position;
                ui_canvas.transform.rotation = Vehicle.rotation * ui_canvas.transform.rotation;
            }
            Undo.RegisterCreatedObjectUndo(ui_canvas, "Create Stick Displays");
            // Add a CanvasScaler to the world space canvas
            string ObjectName = "StickDisplay" + (isR ? "R" : "L");
            GameObject StickDisplay = new GameObject(ObjectName);
            StickDisplay.layer = 31;
            GameObject DisplayHighlighter = new GameObject((isR ? "R" : "L") + "StickDisplayHighlighter");
            DisplayHighlighter.layer = 31;
            GameObject DisplayFuncon = new GameObject("StickFuncon");
            DisplayFuncon.layer = 31;

            List<int> HasFuncon = new List<int>(); // tracks if we need to add a Funcon to objects later
            List<string> HasFuncon_name = new List<string>();
            List<bool> HasFuncon_isSmoke = new List<bool>();

            foreach (var filter in filters)
            {
                if (filter.sharedMesh.name == meshNameDivider)
                {
                    // Assign the mesh to a new MeshFilter component
                    StickDisplay.AddComponent<MeshFilter>().mesh = filter.sharedMesh;
                    StickDisplay.AddComponent<MeshRenderer>().material = mat_MFD;



                    if (DivideStraightUp)
                    {
                        float rot = (360f / Funcs.Length) * -.5f;
                        StickDisplay.transform.rotation = StickDisplay.transform.rotation * Quaternion.AngleAxis(rot, StickDisplay.transform.forward);
                    }

                    for (int i = 0; i < Funcs.Length; i++)
                    {
                        if (!Funcs[i]) { continue; }
                        GameObject TextObj = new GameObject("TextObj");
                        TextObj.layer = 31;
                        TextObj.transform.parent = StickDisplay.transform;
                        // Add a TextMeshPro text object
                        TextMeshProUGUI tmpText = TextObj.AddComponent<TextMeshProUGUI>();
                        string funcName = Funcs[i].GetUdonTypeName();
                        string[] funcNameSplit = funcName.Split("_");
                        string funcName_Clean = string.Empty;
                        if (funcNameSplit.Length > 1)
                        {
                            for (int o = 1; o < funcNameSplit.Length; o++)
                            {
                                funcName_Clean += funcNameSplit[o];
                            }
                        }
                        else { funcName_Clean = funcName; }
                        string funcName_Dial = funcName_Clean.ToUpper();
                        tmpText.rectTransform.sizeDelta = new Vector2(.18f, .14f);
                        tmpText.text = funcName_Dial;
                        tmpText.fontSize = .022f;
                        tmpText.alignment = TextAlignmentOptions.Center;

                        string fontID = "76f1914f4f82852458d2250d86c7f472"; //F1.8-RegularSDF
                        TMP_FontAsset theFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fontID));
                        tmpText.font = theFont;
                        tmpText.color = new Color32(170, 255, 0, 255);

                        Vector3 pos = tmpText.rectTransform.localPosition;
                        pos.y = 0.14f; ;
                        float rot = (360f / Funcs.Length) * i;
                        pos = Quaternion.AngleAxis(rot, -StickDisplay.transform.forward) * pos;

                        tmpText.rectTransform.localPosition = pos;
                        tmpText.rectTransform.rotation = Quaternion.identity;
                        TextObj.name = "MFD_" + funcName_Clean;

                        // save whether or not we need to add a Funcon for later
                        if (Funcs[i].GetType().GetField("Dial_Funcon", BindingFlags.Public | BindingFlags.Instance) != null)
                        {
                            HasFuncon.Add(i);
                            HasFuncon_name.Add("MFD_" + funcName_Clean);
                            HasFuncon_isSmoke.Add(funcName == "DFUNC_Smoke");
                        }
                    }
                }
                else if (filter.sharedMesh.name == meshNameHighlighter)
                {
                    DisplayHighlighter.AddComponent<MeshFilter>().mesh = filter.sharedMesh;
                    DisplayHighlighter.AddComponent<MeshRenderer>().material = mat_MFD;
                }
                else if (filter.sharedMesh.name == meshNameFuncon)
                {
                    DisplayFuncon.AddComponent<MeshFilter>().mesh = filter.sharedMesh;
                    DisplayFuncon.AddComponent<MeshRenderer>().material = mat_MFD_funcon;
                }

                DisplayHighlighter.transform.parent = StickDisplay.transform;
            }

            // Add funcon indicators for funtions that use them
            for (int i = 0; i < HasFuncon.Count; i++)
            {
                GameObject newFuncon = Instantiate(DisplayFuncon);
                newFuncon.layer = 31;
                newFuncon.transform.parent = StickDisplay.transform;
                newFuncon.name = HasFuncon_name[i] + "_FUNCON";
                float rot = (360f / Funcs.Length) * HasFuncon[i];
                newFuncon.transform.localRotation = Quaternion.AngleAxis(rot, -StickDisplay.transform.forward);
                if (HasFuncon_isSmoke[i]) { newFuncon.GetComponent<MeshRenderer>().material = mat_MFD_Smoke_funcon; }
            }

            DestroyImmediate(DisplayFuncon);
            StickDisplay.transform.position += Vector3.right * (isR ? 0.5f : -0.5f);
            StickDisplay.transform.SetParent(ui_canvas.transform);
            StickDisplay.transform.localPosition = StickDisplay.transform.position;
            StickDisplay.transform.localRotation = StickDisplay.transform.rotation;
            return ui_canvas;
        }
    }
}
#endif