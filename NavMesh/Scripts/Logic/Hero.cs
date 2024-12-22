using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CCengine
{
    public enum HeroType { Tank, DoT, Burst, Stealth, Mage, Marksman, Support }

    [System.Serializable]
    public class Hero : Entity
    {
        // Basic Information
        public IReadOnlyCollection<HeroType> Types;
        // public Sprite IconSprite { get; private set; }

        // Core Attributes
        public int Mana { get; private set; }
        public int Armor { get; private set; }
        public int MagicResist { get; private set; }
        public int MoveSpeed { get; private set; }

        // Pathfinding
        public bool HasPath => path.Count > 0;
        public SLVector3 currentTarget { get; private set; }
        public Queue<SLVector3> path { get; private set; } = new Queue<SLVector3>();

        [System.NonSerialized]
        private NavMeshAgent _navMeshAgent;

        public NavMeshAgent NavMeshAgent
        {
            get => _navMeshAgent;
            private set => _navMeshAgent = value;
        }


        // Constructor
        public Hero(string name, Team team, SLVector3 position, int maxHealth, int damage, int mana)
            : base(EntityType.Hero, name, team, position, maxHealth, damage)
        {
            Mana = mana;
            Types = new HashSet<HeroType>();
            PreviousPosition = position;
        }

        /// <summary>
        /// Sets the hero types.
        /// </summary>
        /// <param name="newType">A collection of hero types.</param>
        public void AddType(IEnumerable<HeroType> newType)
        {
            if (newType == null)
                throw new ArgumentNullException(nameof(newType));

            Types = new HashSet<HeroType>(newType);
        }
        /// <summary>
        /// Sets the hero's attributes.
        /// </summary>
        public void SetAttributes(int health, int mana, int damage, int abilityPower,
            int armor, int magicResist, int moveSpeed)
        {
            if (health <= 0)
                throw new ArgumentOutOfRangeException(nameof(health), "Health must be positive.");
            if (mana < 0)
                throw new ArgumentOutOfRangeException(nameof(mana), "Mana cannot be negative.");
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative.");
            if (abilityPower < 0)
                throw new ArgumentOutOfRangeException(nameof(abilityPower), "Ability Power cannot be negative.");
            if (armor < 0)
                throw new ArgumentOutOfRangeException(nameof(armor), "Armor cannot be negative.");
            if (magicResist < 0)
                throw new ArgumentOutOfRangeException(nameof(magicResist), "Magic Resist cannot be negative.");
            if (moveSpeed <= 0)
                throw new ArgumentOutOfRangeException(nameof(moveSpeed), "Move Speed must be positive.");

            MaxHealth = health;
            Mana = mana;
            Attack = damage;
            Armor = armor;
            MagicResist = magicResist;
            MoveSpeed = moveSpeed;
        }

        public void SetPath(Vector3[] waypoints)
        {
            path.Clear();
            foreach (Vector3 waypoint in waypoints)
            {
                path.Enqueue(waypoint);
            }

            if (path.Count > 0)
            {
                currentTarget = path.Peek();
            }
        }

        // Update position per turn
        public void MoveAlongPath(float turnDuration)
        {
            if (!HasPath)
                return;

            float distanceToMove = MoveSpeed * turnDuration;
            while (distanceToMove > 0f && HasPath)
            {
                SLVector3 direction = (currentTarget - Position).normalized;
                float distanceToTarget = SLVector3.Distance(Position, currentTarget);

                if (distanceToMove >= distanceToTarget)
                {
                    Position = currentTarget;
                    distanceToMove -= distanceToTarget;
                    path.Dequeue();

                    if (HasPath)
                    {
                        currentTarget = path.Peek();
                    }
                }
                else
                {
                    Position += direction * distanceToMove;
                    distanceToMove = 0f;
                }
            }
        }

        public void Respawn(SLVector3 respawnPosition)
        {
            IsDead = false;
            Position = respawnPosition;
            CurrentHealth = MaxHealth;
            // Reset other necessary properties
        }

        public override bool Equals(object obj)
        {
            if (obj is Hero otherHero)
            {
                return Id == otherHero.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}

