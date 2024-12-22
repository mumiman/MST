using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;

namespace CCengine
{
    public class PathEditor : EditorWindow
    {
        [MenuItem("Tools/Calculate Creep Paths")]
        public static void ShowWindow()
        {
            GetWindow<PathEditor>("Creep Path Calculator");
        }

        private List<GameObject> waypoints = new List<GameObject>();
        private string pathName = "NewPath";
        private List<SLVector3> calculatedPath;

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            GUILayout.Label("Creep Path Calculator", EditorStyles.boldLabel);

            // Waypoints List
            GUILayout.Label("Waypoints", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

            for (int i = 0; i < waypoints.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                waypoints[i] = (GameObject)EditorGUILayout.ObjectField("Waypoint " + (i + 1), waypoints[i], typeof(GameObject), true);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    waypoints.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add Waypoint"))
            {
                waypoints.Add(null);
            }

            pathName = EditorGUILayout.TextField("Path Name", pathName);

            if (GUILayout.Button("Calculate Path"))
            {
                CalculatePath();
            }

            if (calculatedPath != null && calculatedPath.Count > 0)
            {
                GUILayout.Label("Path calculated with " + calculatedPath.Count + " waypoints.");
                if (GUILayout.Button("Save Path"))
                {
                    SavePath();
                }
            }
        }

        private void CalculatePath()
        {
            if (waypoints == null || waypoints.Count < 2)
            {
                Debug.LogError("Please assign at least two waypoints.");
                return;
            }

            calculatedPath = new List<SLVector3>();

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                GameObject startWaypoint = waypoints[i];
                GameObject endWaypoint = waypoints[i + 1];

                if (startWaypoint == null || endWaypoint == null)
                {
                    Debug.LogError($"Waypoint {i + 1} or {i + 2} is not assigned.");
                    return;
                }

                NavMeshPath navMeshPath = new NavMeshPath();
                if (NavMesh.CalculatePath(startWaypoint.transform.position, endWaypoint.transform.position, NavMesh.AllAreas, navMeshPath))
                {
                    // Add the corners to the calculatedPath
                    if (navMeshPath.corners.Length > 0)
                    {
                        // If not the first segment, skip the first corner to avoid duplicates
                        int startIndex = (i > 0) ? 1 : 0;
                        for (int j = startIndex; j < navMeshPath.corners.Length; j++)
                        {
                            calculatedPath.Add(navMeshPath.corners[j]);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Failed to calculate path between waypoint {i + 1} and waypoint {i + 2}.");
                    return;
                }
            }

            Debug.Log("Path calculated with " + calculatedPath.Count + " waypoints.");
        }

        private void SavePath()
        {
            if (string.IsNullOrEmpty(pathName))
            {
                Debug.LogError("Please enter a valid path name.");
                return;
            }

            CreepPathData pathData = ScriptableObject.CreateInstance<CreepPathData>();
            pathData.waypoints = calculatedPath.ToArray();

            string assetPath = "Assets/Resources/CreepPaths/" + pathName + ".asset";
            AssetDatabase.CreateAsset(pathData, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log("Path saved to " + assetPath);
        }
    }
}
