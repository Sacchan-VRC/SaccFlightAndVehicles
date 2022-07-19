#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor;
using UdonSharpEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using VRC.SDKBase.Editor.BuildPipeline;

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
                    cam.farClipPlane = 100000f;
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
        const string TransformPrefix = ":campos";
        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
        {
            SaccFlightSetup();
            return true;
        }
        [MenuItem("SaccFlight/Debug_OnBuild_SetReferences", false, 1000)]
        public static void SaccFlightSetup()
        {
            SetUpCameras();
            SetUpRaceButtons();
            SetUpRaceKillTrackers();
            SetUpWindChangers();
            SetUpPlanesMenu();
            DisableInVehicleOnlys();
            SetEntityTargets();
            SaccFlightMenu.SetUpReferenceCameraForFlight();
        }
        public static void DisableInVehicleOnlys()
        {
            var SEs = GetAllSaccEntitys().ToArray();
            foreach (var se in SEs)
            {
                if (se.InVehicleOnly && se.InVehicleOnly.activeInHierarchy) { se.InVehicleOnly.SetActive(false); }
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
        public static void SetUpPlanesMenu()
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
        public static void SetUpRaceKillTrackers()
        {
            var killTrackers = GetAllSAV_KillTrackers();
            var killBoards = GetAllSaccScoreboard_Killss().ToArray();
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
                    if (!SRT.Button)
                    {
                        SRT.Button = RaceToggleButtons[0];
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
            var campositions = GetAllSceneCameraPoints().ToArray();
            foreach (var screen in SVS)
            {
                PutCamPositionsInArray(screen, campositions);
                PrefabUtility.RecordPrefabInstancePropertyModifications(screen);
                EditorUtility.SetDirty(screen);
            }
        }
        public static void PutCamPositionsInArray(SaccViewScreenController viewscreen, Transform[] CamTransform)
        {
            viewscreen.CamPositions = CamTransform;
        }
        static List<Transform> GetAllSceneCameraPoints(Transform tr = null, List<Transform> ls = null)
        {
            if (tr == null)
            {
                ls = ls ?? new List<Transform>();
                foreach (GameObject g in SceneManager.GetActiveScene().GetRootGameObjects()) GetAllSceneCameraPoints(g.transform, ls);
            }
            else
            {
                if (tr.name.StartsWith(TransformPrefix, System.StringComparison.InvariantCultureIgnoreCase)) ls.Add(tr);
                foreach (Transform t in tr) GetAllSceneCameraPoints(t, ls);
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
        static List<SaccScoreboard_Kills> GetAllSaccScoreboard_Killss()
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
        [MenuItem("SaccFlight/Make All Static Colliders Tarmac...", false, 2)]
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
}
#endif