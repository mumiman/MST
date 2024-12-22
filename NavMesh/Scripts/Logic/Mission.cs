using UnityEngine;
using System;
using System.Collections.Generic;

namespace CCengine
{
    [System.Serializable]
    public class Mission
    {
        public MissionData taskData;
        public HashSet<Athlete> athletes;
        public int assignedTurn;
        public Vector2 targetPosition;
        public Entity targetEntity;
        // public int duration { get; private set; } // Optional: duration for timed tasks

        public Mission(MissionData data, HashSet<Athlete> athletes, int assignedTurn, Vector2 targetPosition = default, Entity targetEntity = null)
        {
            taskData = data;
            this.athletes = athletes;
            this.assignedTurn = assignedTurn;
            this.targetPosition = targetPosition;
            this.targetEntity = targetEntity;
        }

    }
    public enum TaskType {Idle = 0, Gather, Ward, Gank, TeamFight, Defend, Push, Farm, Jungling }
}