using UnityEngine;
using System.Collections.Generic;
using System;

namespace CCengine
{
    [System.Serializable]
    public class CreepWave : Entity
    {
        public SLVector3[] LanePath { get; private set; }
        public int PathIndex { get; private set; }
        public Lane Lane { get; private set; }

        public CreepWave(string id, string name, Team team, Lane lane, int maxHealth, int damage, SLVector3[] lanePath)
           : base(id, EntityType.CreepWave, name, team, maxHealth, damage)
        {
            if (lanePath == null || lanePath.Length == 0)
            {
                throw new ArgumentException("LanePath cannot be null or empty.", nameof(lanePath));
            }
            LanePath = lanePath;
            PathIndex = 1; // Start at the second position
            Position = lanePath[PathIndex];
            PreviousPosition = Position;
            Lane = lane;
        }
        public void SetPathIndex(int index)
        {
            if (index < 0 || index >= LanePath.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of bounds of the lane path.");
            }
            PathIndex = index;
            Position = LanePath[PathIndex];
        }

        private void MoveAlongPath()
        {
            // if (LanePath == null || LanePath.Length == 0)
            //     return;

            // Vector3 targetPosition = LanePath[PathIndex];
            // Vector3 direction = (targetPosition - Position).normalized;
            // float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // if (distanceThisFrame >= distanceToTarget)
            // {
            //     // Move to next waypoint
            //     transform.position = targetPosition;
            //     currentPathIndex++;

            //     if (currentPathIndex >= creepData.Path.Length)
            //     {
            //         // Reached the end of the path
            //         isMoving = false;
            //         OnReachDestination();
            //     }
            // }
            // else
            // {
            //     // Move towards target
            //     transform.position += direction * distanceThisFrame;
            // }
        }

        private void OnReachDestination()
        {
            // Handle what happens when the creep reaches its destination
        }

        #region HashSet unique
        public override bool Equals(object obj)
        {
            if (obj is CreepWave otherCreepWave)
            {
                return Name == otherCreepWave.Name &&
                       Team == otherCreepWave.Team &&
                       Lane == otherCreepWave.Lane;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Team, Lane);
        }
        #endregion
    }
}