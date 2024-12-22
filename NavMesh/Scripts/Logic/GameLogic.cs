using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using UnityEngine.AI;

namespace CCengine
{
    public enum EntityType { CreepWave, Hero, Tower }
    public class GameLogic
    {
        // Game Events
        public UnityAction onGameStart;
        public UnityAction<Player> onGameEnd;
        public UnityAction onTurnStart;
        public UnityAction onTurnEnd;
        public UnityAction<UpdateMessage> onUpdateTurn;
        public UnityAction onRefresh;

        // Entity events
        // public UnityAction<string, EntityType, Vector3> onEntityMoved;
        // public UnityAction<string> onEntityDestroyed;
        // public UnityAction<string, EntityType, int> onHealthChanged;
        // public UnityAction<string, EntityType, Vector3> onEntitySpawned;
        // public UnityAction<Card> onCardPlayed;

        // Update Message
        private UpdateMessage updateMessage;

        // Game Data
        public GameData gameData { get; private set; }

        // Pathfinding
        private Dictionary<string, Vector3[]> creepPaths = new Dictionary<string, Vector3[]>();


        // Turn Management
        private int spawnCreepTurn = 4;
        public bool IsResolving = false;
        public bool IsGameStarted = false;
        public bool IsGameEnded = false;

        // public ResolveQueue ResolveQueue { get { return resolve_queue; } }

        public GameLogic(GameData game)
        {
            Debug.Log("GameLogic created.");
            gameData = game;

            // Trigger game start event
            onGameStart?.Invoke();
        }

        #region Card

        public void PlayCard(Player player, Card card, Vector3 targetPosition, string[] athleteIds)
        {
            if (card == null)
            {
                Debug.LogWarning($"Card with UID {card.card_id} not found in player's hand.");
                return;
            }

            // Check if the player is allowed to play this card
            if (!CanPlayCard(player, card))
            {
                Debug.LogWarning($"Player {player.username} cannot play card {card.name} due to insufficient resources or other restrictions.");
                return;
            }

            switch (card.cardType)
            {
                case CardType.TaskAssignment:
                    if (card.assignedTask == null)
                    {
                        Debug.LogWarning("No Assigned task in card.");
                        return;
                    }

                    if (athleteIds != null && athleteIds.Length > 0)
                    {
                        // For each athlete in athleteIds, assign the task
                        foreach (string athleteId in athleteIds)
                        {
                            Athlete athlete = player.GetAthlete(athleteId);
                            if (athlete != null)
                            {
                                // Create a Task instance from the card's TaskData
                                Mission mission = card.assignedTask;
                                // Assign and perform the task
                                ProcessTask(mission);

                                Debug.Log($"Assigned task '{card.name}' to athlete '{athlete.Name}' at position {targetPosition}");
                            }
                            else
                            {
                                Debug.LogWarning($"Athlete with ID {athleteId} not found for player {player.username}.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No athletes specified for TaskAssignment card.");
                        return;
                    }

                    break;

                case CardType.StatBoost:
                    if (card.statBoost == null)
                    {
                        Debug.LogWarning("No StatBoost in card.");
                        return;
                    }

                    if (athleteIds != null && athleteIds.Length > 0)
                    {
                        // For each athlete in athleteIds, apply stat boost
                        foreach (string athleteId in athleteIds)
                        {
                            Athlete athlete = player.GetAthlete(athleteId);
                            if (athlete != null)
                            {
                                // Apply stat boost to athlete
                                // athlete.ApplyStatBoost(card.statBoost);

                                Debug.Log($"Applied stat boost to athlete {athlete.Name}: {card.statBoost}");
                            }
                            else
                            {
                                Debug.LogWarning($"Athlete with ID {athleteId} not found for player {player.username}.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("No athletes specified for StatBoost card.");
                        return;
                    }

                    break;

                // Add cases for other card types

                default:
                    Debug.LogWarning("Unknown card type");
                    break;
            }

            // Remove the card from the player's hand and add to discard pile
            player.cards_hand.Remove(card);
            player.cards_discard.Add(card);

            // Invoke the OnCardPlayed event
            // onCardPlayed?.Invoke(card);
        }

        private bool CanPlayCard(Player player, Card card)
        {
            return player.cards_hand.Contains(card);
        }

        #endregion

        #region Mission

        public void ProcessTask(Mission task)
        {
            switch (task.taskData.taskType)
            {
                case TaskType.Gather:
                    HandleGatherTask(task);
                    break;

                // Add cases for other TaskTypes as needed

                default:
                    Debug.LogWarning("Unhandled task type: " + task.taskData.taskType);
                    break;
            }
        }

        private void HandleGatherTask(Mission task)
        {
            Vector3 targetPosition = new Vector3(task.targetPosition.x, task.targetPosition.y, 0);

            foreach (Athlete athlete in task.athletes)
            {
                Hero hero = athlete.Hero;
                if (hero != null)
                {
                    HeroCalculatePath(hero, targetPosition);
                }
                else
                {
                    Debug.LogWarning("Athlete " + athlete.Name + " does not have a hero assigned.");
                }
            }
        }

        #endregion

        #region Athelete

        public List<Athlete> GetAthletes(string[] athleteIds)
        {
            List<Athlete> athletes = new List<Athlete>();
            foreach (string id in athleteIds)
            {
                Athlete athlete = gameData.GetAthlete(id);
                if (athlete != null)
                {
                    athletes.Add(athlete);
                }
                else
                {
                    // Athlete not found or does not belong to the player
                    Debug.LogWarning($"Athlete with ID {id} not found.");
                    return null; // Or handle according to your error handling strategy
                }
            }
            return athletes;
        }

        #endregion

        #region Hero

        public void AssignHeroesToCombatLaneCells()
        {
            // foreach (Athlete athlete in gameData.GetAllPlayers())
            // {
            //     if (athlete.AssignedLane != Lane.None && combatLaneCells.TryGetValue(athlete.AssignedLane, out Vector2 targetPosition))
            //     {
            //         // Debug.Log("Hero move to combat in " + player.AssignedLane + " lane: " + targetCell.ToString());

            //         // Use NavMesh pathfinder
            //         athlete.Hero.NavMeshAgent.SetDestination(targetPosition);
            //     }
            //     else
            //     {
            //         // No combat cell in this lane, decide whether hero should move forward or stay
            //         // Optionally, move towards the front line or enemy tower
            //     }
            // }
        }

        public void MoveHeroes()
        {
            foreach (Hero hero in gameData.GetAllHeroes())
            {
                if (hero.HasPath)
                {
                    // Move the hero along the path
                    hero.MoveAlongPath(gameData.TurnDuration);

                    // Get the position
                    SLVector3 oldPosition = hero.PreviousPosition;
                    SLVector3 newPosition = hero.Position;

                    // New position, Update the cell's entities
                    if (oldPosition != newPosition)
                    {
                        // Add to updateMessage
                        updateMessage.EntityMovedMessages.Add(new EntityMovedMessage(hero.Id, EntityType.Hero, newPosition));
                    }
                }
            }
        }
        #endregion
        #region Creep wave
        // Creep wave


        #endregion

        #region Combat Resolution

        public void CheckForCombat()
        {
            // Sort entities by X-coordinate
            gameData.teamAEntities.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
            gameData.teamBEntities.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));

            // For each attacker in Team A
            foreach (Entity attacker in gameData.teamAEntities)
            {
                if (!CanAttack(attacker))
                    continue;

                float attackerX = attacker.Position.x;
                float attackerZ = attacker.Position.z;

                float minX = attackerX - GameData.MAX_ATTACK_RANGE;
                float maxX = attackerX + GameData.MAX_ATTACK_RANGE;

                int startIndex = BinarySearchEntitiesByX(gameData.teamBEntities, minX);

                for (int i = startIndex; i < gameData.teamBEntities.Count; i++)
                {
                    Entity target = gameData.teamBEntities[i];

                    if (target.Position.x > maxX)
                        break;

                    if (!CanBeAttacked(target))
                        continue;

                    float deltaZ = Mathf.Abs(target.Position.z - attackerZ);
                    if (deltaZ > GameData.MAX_ATTACK_RANGE)
                        continue;

                    if (IsWithinAttackRange(attacker, target))
                    {
                        if (HasLineOfSight(attacker, target))
                        {
                            StartCombatBetweenEntities(attacker, target);
                        }
                    }
                }
            }

            // Repeat for Team B attacking Team A if necessary
        }

        private int BinarySearchEntitiesByX(List<Entity> entities, float xValue)
        {
            int left = 0;
            int right = entities.Count - 1;
            int result = entities.Count;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                if (entities[mid].Position.x >= xValue)
                {
                    result = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return result;
        }

        private bool CanAttack(Entity entity)
        {
            return !entity.IsDead && entity.AttackRange > 0; //&& !entity.IsStunned;
        }

        private bool CanBeAttacked(Entity entity)
        {
            return !entity.IsDead; //&& !entity.IsInvisible;
        }

        public bool IsWithinAttackRange(Entity attacker, Entity target)
        {
            float dx = attacker.Position.x - target.Position.x;
            float dz = attacker.Position.z - target.Position.z;
            float distanceSquared = dx * dx + dz * dz;
            float attackRangeSquared = attacker.AttackRange * attacker.AttackRange;

            return distanceSquared <= attackRangeSquared;
        }

        private bool HasLineOfSight(Entity attacker, Entity target)
        {
            // Implement line of sight checks if necessary
            return true; // Placeholder
        }

        private void StartCombatBetweenEntities(Entity attacker, Entity target)
        {
            // Implement combat initiation logic
        }

        #endregion
        #region Spawn
        private void SpawnCreepWave()
        {
            // Check if it's time to spawn a new creep wave
            if (gameData.CurrentTurn % spawnCreepTurn == 0)
            {
                gameData.NextCreepWave();
                List<CreepWave> newlySpawnedCreepWaves = gameData.AddCreepWaves();

                // Add spawn messages for each newly spawned creep wave
                foreach (var creepWave in newlySpawnedCreepWaves)
                {
                    updateMessage.EntitySpawnedMessages.Add(new EntitySpawnedMessage
                    (
                        id: creepWave.Id,
                        entityType: EntityType.CreepWave,
                        name: creepWave.Name,

                        team: creepWave.Team,
                        initialPosition: creepWave.Position,
                        lane: creepWave.Lane,
                        maxHealth: creepWave.MaxHealth,
                        atk: creepWave.Attack,
                        mana: 0
                    ));
                }
            }
        }
        #endregion
        #region Pathfinding

        private void HeroCalculatePath(Hero hero, Vector3 targetPosition)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            if (NavMesh.CalculatePath(hero.Position, targetPosition, NavMesh.AllAreas, navMeshPath))
            {
                Vector3[] waypoints = navMeshPath.corners;
                hero.SetPath(waypoints);
            }
            else
            {
                // Handle path not found
                Debug.LogWarning("Path not found from " + hero.Position + " to " + targetPosition);
            }
        }

        #endregion
        #region Messaging

        public UpdateMessage GetUpdateMessage()
        {
            return updateMessage;
        }
        #endregion

        #region Turn Management

        public void StartGame()
        {
            IsGameStarted = true;
            gameData.state = GameState.Play;
            gameData.CurrentTurn = 1;

            // Start the first turn
            StartTurn();
        }

        public void StartTurn()
        {
            onTurnStart?.Invoke();

            // Execute turn actions
            ExecuteTurn();

            // End the turn
            EndTurn();
        }

        public void EndTurn()
        {
            onTurnEnd?.Invoke();

            gameData.CurrentTurn++;

            // Start next turn
            StartTurn();

        }

        #endregion

        #region Game Loop
        // ---- Start turn
        public void ExecuteTurn()
        {
            Debug.Log("Turn: " + gameData.CurrentTurn);
            IsResolving = true;

            UpdateMessage updateMessage = new UpdateMessage();

            SpawnCreepWave();

            // // Move all entities (creep waves and heroes)
            // MoveCreepWaves();

            // // Resolve conflicts in each cell
            // ResolveCombat();

            // Heroes gain XP, money, and other resources
            HandleHeroProgression();

            AssignHeroesToCombatLaneCells();

            MoveHeroes();

            // Strategic warping or any additional turn-based actions
            HandleWarping();

            onUpdateTurn?.Invoke(updateMessage);

            IsResolving = false;

        }

        private void HandleHeroProgression()
        {
            // Logic for handling hero progression, such as gaining XP, money, etc.
            // Debug.Log("Handling hero progression...");
        }

        void HandleWarping()
        {
            // Logic for warping heroes to the nearest tower
            // Example: This could be based on player decisions or automatic rules
            // Debug.Log("Handling strategic warping...");
        }

        public void EndGame(int winnerId)
        {
            Player winner = gameData.players[winnerId];
            // onGameEnd?.Invoke();
            Debug.Log($"Winner is {winner.username} !");
        }

        public SLGameData GetGameData()
        {
            return SLGameData.FromGameData(gameData);
        }

        #endregion
    }
}
