using System;
using System.Collections.Generic;
using UnityEngine;

namespace CCengine
{
    public class LanePaths
    {
        public static SLVector3[] TopLanePathA { get; private set; }
        public static SLVector3[] MidLanePathA { get; private set; }
        public static SLVector3[] BottomLanePathA { get; private set; }

        public static SLVector3[] TopLanePathB { get; private set; }
        public static SLVector3[] MidLanePathB { get; private set; }
        public static SLVector3[] BottomLanePathB { get; private set; }

        public static List<LanePaths> pathsList = new List<LanePaths>(); // Cache for loaded paths

        public static void Load(string folder = "CreepPaths")
        {
            if (pathsList.Count == 0) // Load once to avoid duplicate loads
            {
                CreepPathData[] paths = Resources.LoadAll<CreepPathData>(folder);
                LanePaths lanePaths = new LanePaths();
                lanePaths.Initialize(paths);
                pathsList.Add(lanePaths);
            }
        }

        public void Initialize(CreepPathData[] paths)
        {
            foreach (CreepPathData pathData in paths)
            {
                switch (pathData.team)
                {
                    case Team.TeamA:
                        AssignPathToTeamA(pathData);
                        break;
                    case Team.TeamB:
                        AssignPathToTeamB(pathData);
                        break;
                    default:
                        Debug.LogWarning("Unknown team for path: " + pathData.name);
                        break;
                }
            }
        }

        private void AssignPathToTeamA(CreepPathData pathData)
        {
            switch (pathData.lane)
            {
                case Lane.Top:
                    TopLanePathA = pathData.waypoints;
                    break;
                case Lane.Mid:
                    MidLanePathA = pathData.waypoints;
                    break;
                case Lane.Bottom:
                    BottomLanePathA = pathData.waypoints;
                    break;
                default:
                    Debug.LogWarning("Unknown lane for path: " + pathData.name);
                    break;
            }
        }

        private void AssignPathToTeamB(CreepPathData pathData)
        {
            switch (pathData.lane)
            {
                case Lane.Top:
                    TopLanePathB = pathData.waypoints;
                    break;
                case Lane.Mid:
                    MidLanePathB = pathData.waypoints;
                    break;
                case Lane.Bottom:
                    BottomLanePathB = pathData.waypoints;
                    break;
                default:
                    Debug.LogWarning("Unknown lane for path: " + pathData.name);
                    break;
            }
        }

        public static List<LanePaths> Get()
        {
            if (pathsList.Count > 0)
                return pathsList;
            Debug.LogError("LanePaths not loaded. Call LanePaths.Load() first.");
            return null;
        }
    }
}