using UnityEngine;
using UnityEngine.AI;
using System;

namespace CCengine
{
    [System.Serializable]
    public abstract class Entity
    {
        private int currentHealth;
        // Public properties
        public string Id { get; protected set; }
        public EntityType EntityType { get; protected set; }
        public string Name { get; protected set; }
        public Team Team { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int Attack { get; protected set; }
        public float AttackRange { get; protected set; }

        // Pathfinding
        public SLVector3 Position { get; protected set; }
        public SLVector3 PreviousPosition { get; protected set; }
        public float Speed { get; protected set; } = 3.5f; // Units per second
        // private Vector3[] path;
        // private int currentWaypointIndex = 1; // Start from the first waypoint after the current position

        // Event
        public event Action<int, int> OnHealthChanged; // Parameters: currentHealth, maxHealth
        public event Action<string> OnDestroyed; // Parameter: id
        public event Action<int> OnDamaged; // Parameter: amount damage

        // Invoked properties
        public int CurrentHealth
        {
            get => currentHealth;
            protected set
            {
                int oldHealth = currentHealth;
                currentHealth = Mathf.Clamp(value, 0, MaxHealth);

                if (currentHealth != oldHealth)
                {
                    OnHealthChanged?.Invoke(currentHealth, MaxHealth);

                    if (currentHealth <= 0 && oldHealth > 0)
                    {
                        OnDestroyed?.Invoke(Id);
                    }
                }
            }
        }

        public bool IsDead
        {
            get => CurrentHealth <= 0;
            protected set
            {
                if (value)
                {
                    // If setting IsDestroyed to true, set CurrentHealth to 0
                    if (CurrentHealth > 0)
                    {
                        CurrentHealth = 0;
                    }
                }
                else
                {
                    // If setting IsDestroyed to false, reset CurrentHealth to MaxHealth
                    if (CurrentHealth <= 0)
                    {
                        CurrentHealth = MaxHealth;
                    }
                }
            }
        }

        // Base constructor
        public Entity(EntityType entityType, string name, Team team, int maxHealth, int atk)
        {
            Id = EntityIdGenerator.GetNextId();
            EntityType = entityType;
            Name = name;
            Team = team;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Attack = atk;
            IsDead = false;
        }
        public Entity(string id, EntityType entityType, string name, Team team, int maxHealth, int atk)
        {
            Id = id;
            EntityType = entityType;
            Name = name;
            Team = team;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            Attack = atk;
            IsDead = false;
        }

        public Entity(EntityType entityType, string name, Team team, Vector3 position, int maxHealth, int atk)
            : this(entityType, name, team, maxHealth, atk)
        {
            Position = position;
        }

        /// <summary>
        /// Attacks the specified target entity.
        /// </summary>
        /// <param name="target">The target entity to attack.</param>
        public virtual void AttackTarget(Entity target)
        {
            if (IsDead)
            {
                Debug.LogWarning($"{Name} cannot attack because it is destroyed.");
                return;
            }

            if (target == null || target.IsDead)
            {
                Debug.LogWarning($"{Name} cannot attack the target because it is null or destroyed.");
                return;
            }

            Debug.Log($"{Name} attacks {target.Name} for {Attack} damage.");
            target.TakeDamage(Attack);
        }

        /// <summary>
        /// Reduces health by the specified amount.
        /// </summary>
        /// <param name="amount">The amount of damage to take.</param>
        public virtual void TakeDamage(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning("Damage amount cannot be negative.");
                return;
            }

            if (IsDead)
            {
                Debug.LogWarning($"{Name} cannot take damage because it is already destroyed.");
                return;
            }

            // Invoke OnDamaged before changing health
            OnDamaged?.Invoke(amount);

            // Reduce CurrentHealth; the setter will handle OnHealthChanged and OnDestroyed
            CurrentHealth -= amount;
        }

        public void UpdateHealth(int newHealth)
        {
            CurrentHealth = newHealth;
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }

        private bool IsValidPosition(Vector2 position)
        {
            // Implement position validation logic here
            return true; // Placeholder
        }

        public void SetTeam(Team team)
        {
            Team = team;
        }      

        public void SetPosition(Vector3 pos)
        {
            Position = pos;
        }

        private void OnReachedDestination()
        {
            // Handle arrival at destination
            Debug.Log($"Entity {Id} has reached its destination.");
            // Additional logic here
        }

        public virtual void ResetEntity(SLVector3 newPosition, int newHealth)
        {
            Position = newPosition;
            CurrentHealth = newHealth;
            IsDead = false;
        }
    }
}

