using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CCengine
{
    //Represent the current state of a player during the game (data only)

    [System.Serializable]
    public class Athlete
    {
        // Basic Information
        public string Id { get; protected set;}
        public string Name { get; protected set;}
        public int Age { get; protected set;}

        public HashSet<PlayerRole> PreferredRoles { get; protected set;}
        public PlayerRole Role { get; protected set;}
        public Team Team { get; protected set;}
        public Hero Hero { get; protected set;}
        public Lane AssignedLane { get; protected set;}
        public Mission CurrentTask { get; protected set;}
        public int Importance { get; protected set;} // Carry = 1, Mid = 2, Offlane = 3, Sup1 = 4, Sup2 = 5

        // Attributes with specified ranges
        private float aggressive;  // Range: -1.0 (cautious) to 1.0 (aggressive)
        private float reflex;          // Range: 0.0 (slow) to 1.0 (fast)
        private float skill;           // Range: 0.0 (novice) to 1.0 (expert)
        private float decision;        // Range: 0.0 (poor) to 1.0 (excellent)
        private float teamwork;        // Range: 0.0 (individualistic) to 1.0 (team-oriented)
        private float joker;           // Range: 0.0 (predictable) to 1.0 (unpredictable)

        // Hero Mastery
        public Dictionary<string, int> HeroMastery; // HeroName -> Mastery Level (1-100)

        public float Aggressive { get => aggressive; set => aggressive = Mathf.Clamp(value, -1.0f, 1.0f); }
        public float Reflex { get => reflex; set => reflex = Mathf.Clamp01(value); }
        public float Skill { get => skill; set => skill = Mathf.Clamp01(value); }
        public float Decision { get => decision; set => decision = Mathf.Clamp01(value); }
        public float Teamwork { get => teamwork; set => teamwork = Mathf.Clamp01(value); }
        public float Joker { get => joker; set => joker = Mathf.Clamp01(value); }

        // Constructor
        public Athlete(string id, string name, int age, HashSet<PlayerRole> preferredRoles, Dictionary<string, int> heroMastery,
        float aggresive, float reflex, float skill, float decision, float teamwork, float joker)
        {
            Id = id;
            Name = name;
            Age = age;
            PreferredRoles = preferredRoles;
            HeroMastery = heroMastery;

            Aggressive = aggresive;
            Reflex = reflex;
            Skill = skill;
            Decision = decision;
            Teamwork = teamwork;
            Joker = joker;

            Team = Team.None; // Initialize as not assigned to any team
        }
        public void AssignTeam(Team team)
        {
            Team = team;
        }
        public void AssignHero(Hero hero)
        {
            Hero = hero;
            hero.SetTeam(this.Team);
        }
        public void AssignRole(PlayerRole selectRole)
        {
            Role = selectRole;
            AssignImportance();
            AssignToLane();
        }

        // Method to assign or update hero mastery
        public void SetHeroMastery(string heroName, int masteryLevel)
        {
            if (HeroMastery.ContainsKey(heroName))
                HeroMastery[heroName] = masteryLevel;
            else
                HeroMastery.Add(heroName, masteryLevel);
        }

        /// <summary>
        /// Assigns the hero to a lane based on the player's role.
        /// </summary>
        private void AssignToLane()
        {
            switch (Role)
            {
                case PlayerRole.Offlane:
                case PlayerRole.Sup2:
                    AssignedLane = Lane.Top;
                    break;
                case PlayerRole.Mid:
                    AssignedLane = Lane.Mid;
                    break;
                case PlayerRole.Carry:
                case PlayerRole.Sup1:
                    AssignedLane = Lane.Bottom;
                    break;
                default:
                    throw new ArgumentException("Unknown player role.", nameof(Role));
            }
        }

        private void AssignImportance()
        {
            switch (Role)
            {
                case PlayerRole.Carry:
                    Importance = 1;
                    break;
                case PlayerRole.Mid:
                    Importance = 2;
                    break;
                case PlayerRole.Offlane:
                    Importance = 3;
                    break;
                case PlayerRole.Sup1:
                    Importance = 4;
                    break;
                case PlayerRole.Sup2:
                    Importance = 5;
                    break;
                default:
                    throw new ArgumentException("Unknown player role.", nameof(Role));
            }
        }

        #region Task
        // public void PerformTask(Task task)
        // {
        //     CurrentTask = task;

        //     // Switch based on the task type
        //     switch (task.taskData.taskType)
        //     {
        //         case TaskType.Gather:
        //             // Perform gather action
        //             hero.SetPath(task.targetPosition);
        //             break;

        //         case TaskType.Defend:
        //             // Perform attack action
        //             hero.SetPath(task.targetPosition);
        //             // Additional attack logic
        //             break;

        //         // Add cases for other task types as needed

        //         default:
        //             Debug.LogWarning($"Unknown task type: {task.taskData.taskType}");
        //             break;
        //     }
        // }

        public Mission GetCurrentTask()
        {
            return CurrentTask;
        }

        #endregion
        public override bool Equals(object obj)
        {
            if (obj is Athlete otherPlayer)
            {
                return Id == otherPlayer.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}