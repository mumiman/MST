using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CCengine.Client
{
    public class VisualManager : MonoBehaviour
    {
        public static VisualManager Instance { get; private set; }
        private GameClient gameClient;

        [SerializeField] private GameObject towerPrefab;

        // Dictionaries to map entity IDs to their respective controllers
        // private Dictionary<string, AthleteIconController> athleteIcons = new Dictionary<string, AthleteIconController>();

        private Dictionary<string, TowerIconController> towerIcons = new Dictionary<string, TowerIconController>();

        public TMP_Text turnText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Subscribe to GameClient's onRefreshAll and onEntitySpawned events
            gameClient = GameClient.Get();

            gameClient.onGameStart += OnGameStart;
            // gameClient.onRefreshAll += OnRefreshAll;
            gameClient.onUpdateMessage += OnUpdateMessage;

            Debug.Log("VisualManager started and subscribed to GameClient events.");
        }
        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (gameClient != null)
            {
                gameClient.onGameStart -= OnGameStart;
                // gameClient.onRefreshAll -= OnRefreshAll;
                gameClient.onUpdateMessage -= OnUpdateMessage;
            }
        }

        /// <summary>
        /// Registers an entity's visual controller with the VisualManager.
        /// </summary>
        /// <param name="entityId">The unique ID of the entity.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="iconController">The controller managing the entity's visual representation.</param>
        // public void RegisterEntityIcon(string entityId, EntityType entityType, MonoBehaviour iconController)
        // {
        //     switch (entityType)
        //     {
        //         case EntityType.Hero:
        //             heroIcons[entityId] = iconController as HeroIconController;
        //             break;
        //         case EntityType.CreepWave:
        //             creepWaveIcons[entityId] = iconController as CreepWaveIconController;
        //             break;
        //         case EntityType.Tower:
        //             towerIcons[entityId] = iconController as TowerIconController;
        //             break;
        //         default:
        //             Debug.LogWarning($"Unknown EntityType: {entityType} for entity ID: {entityId}");
        //             break;
        //     }
        // }

        private void OnGameStart(SLGameData data)
        {
            // Initialize heroes
            foreach (HeroData hero in data.GetAllHeroes())
            {
                HeroIconManager.Instance.InstantiateHeroIcon(hero);
            }
            // Initialize creep waves
            foreach (CreepWaveData creepWave in data.GetAllCreepWaves())
            {
                CreepWaveIconManager.Instance.ActivateCreepWaveIcon(creepWave);
            }
            // Initialize towers
            foreach (TowerData tower in data.GetAllTowers())
            {
                InitiateTowerIcon(tower);
            }

            // Initialize turn text
            turnText.text = "Current Turn: " + data.currentTurn;
        }

        /// <summary>
        /// Handles the UpdateMessage by processing all contained messages.
        /// </summary>
        /// <param name="updateMessage">The UpdateMessage received from the server.</param>
        private void OnUpdateMessage(UpdateMessage updateMessage)
        {
            // Process EntityMovedMessages
            foreach (var moveMsg in updateMessage.EntityMovedMessages)
            {
                OnEntityMove(moveMsg);
            }

            // Process HealthChangedMessages
            foreach (var healthMsg in updateMessage.HealthChangedMessages)
            {
                OnHealthChanged(healthMsg);
            }

            // Process EntitySpawnedMessages
            foreach (var spawnMsg in updateMessage.EntitySpawnedMessages)
            {
                OnEntitySpawned(spawnMsg);
            }

            // Process GameStateMessage
            if (updateMessage.gameStateMessage != null)
            {
                OnGameStateChanged(updateMessage.gameStateMessage);
            }
        }

        /// <summary>
        /// Handles the EntityMovedMessage by updating the corresponding visual entity.
        /// </summary>
        /// <param name="moveMsg">The movement message received from the server.</param>
        private void OnEntityMove(EntityMovedMessage moveMsg)
        {
            switch (moveMsg.EntityType)
            {
                case EntityType.Hero:
                    if (HeroIconManager.Instance.heroIcons.TryGetValue(moveMsg.EntityId, out HeroIconController heroController))
                    {
                        heroController.AnimateMove(moveMsg.NewPosition);
                    }
                    else
                    {
                        Debug.LogWarning($"HeroIconController not found for entity ID: {moveMsg.EntityId}");
                    }
                    break;

                case EntityType.CreepWave:
                    CreepWaveIconManager.Instance.MoveCreepWave(moveMsg.EntityId, moveMsg.NewPosition);
                    break;

                default:
                    Debug.LogWarning($"Unhandled EntityType: {moveMsg.EntityType} for entity ID: {moveMsg.EntityId}");
                    break;
            }
        }

        /// <summary>
        /// Handles the HealthChangedMessage by updating the corresponding entity's health.
        /// </summary>
        /// <param name="healthMsg">The health change message received from the server.</param>
        private void OnHealthChanged(HealthChangedMessage healthMsg)
        {
            switch (healthMsg.EntityType)
            {
                case EntityType.Hero:
                    if (HeroIconManager.Instance.heroIcons.TryGetValue(healthMsg.EntityId, out HeroIconController heroController))
                    {
                        heroController.UpdateHealthBar(healthMsg.NewHealth);
                    }
                    else
                    {
                        Debug.LogWarning($"HeroIconController not found for entity ID: {healthMsg.EntityId}");
                    }
                    break;

                case EntityType.CreepWave:
                    CreepWaveIconManager.Instance.UpdateHPCreepWave(healthMsg.EntityId, healthMsg.NewHealth);
                    break;
                case EntityType.Tower:
                    if (towerIcons.TryGetValue(healthMsg.EntityId, out TowerIconController towerIC))
                    {
                        towerIC.UpdateHealthBar(healthMsg.NewHealth);
                    }
                    break;
                default:
                    Debug.LogWarning($"No controller found for entity ID: {healthMsg.EntityId} to update health.");
                    break;
            }
        }

        /// <summary>
        /// Handles the EntitySpawnedMessage by creating and registering new entity visuals.
        /// </summary>
        /// <param name="spawnMsg">The entity spawned message received from the server.</param>
        private void OnEntitySpawned(EntitySpawnedMessage spawnMsg)
        {
            // Assign the entityId to the controller
            switch (spawnMsg.EntityType)
            {
                case EntityType.Hero:
                    HeroIconManager.Instance.ActivateHeroIcon(spawnMsg.Id);
                    break;

                case EntityType.CreepWave:
                    CreepWaveData cwData = new CreepWaveData
                    {
                        creepWaveId = spawnMsg.Id,
                        name = spawnMsg.Name,
                        team = spawnMsg.Team,
                        maxHP = spawnMsg.MaxHealth,
                        atk = spawnMsg.Atk,
                        position = spawnMsg.InitialPosition,
                    };
                    CreepWaveIconManager.Instance.ActivateCreepWaveIcon(cwData);
                    break;

                default:
                    Debug.LogWarning($"Unhandled EntityType: {spawnMsg.EntityType} for entity ID: {spawnMsg.Id}");
                    break;
            }

        }

        /// <summary>
        /// Handles the GameStateMessage by updating the game's state.
        /// </summary>
        /// <param name="gameStateMsg">The game state message received from the server.</param>
        private void OnGameStateChanged(GameStateMessage gameStateMsg)
        {
            // Implement logic to update the game's state
            // Example:
            Debug.Log($"Game State Updated: State={gameStateMsg.State}, Turn={gameStateMsg.CurrentTurn}");

            // Example: If the game has ended, trigger end-of-game UI
            if (gameStateMsg.State == GameState.GameEnded)
            {
                TriggerEndGameUI();
            }
        }

        /// <summary>
        /// Triggers the end-of-game UI or logic.
        /// </summary>
        private void TriggerEndGameUI()
        {
            // Implement your end-of-game UI logic here
            Debug.Log("Game has ended. Displaying end-of-game UI.");
            // Example:
            // endGameUIPanel.SetActive(true);
        }

        /// <summary>
        /// Retrieves the appropriate prefab based on the entity type.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>The prefab GameObject.</returns>
        private GameObject GetPrefabForEntityType(EntityType entityType)
        {
            // Implement logic to return the correct prefab based on EntityType
            // This could be set via the Inspector or a resource manager
            switch (entityType)
            {
                case EntityType.Hero:
                    return Resources.Load<GameObject>("Prefabs/HeroPrefab");
                case EntityType.CreepWave:
                    return Resources.Load<GameObject>("Prefabs/CreepWavePrefab");
                case EntityType.Tower:
                    return Resources.Load<GameObject>("Prefabs/TowerPrefab");
                default:
                    Debug.LogWarning($"No prefab defined for EntityType: {entityType}");
                    return null;
            }
        }

        // Placeholder methods for other events
        private void OnGameStart()
        {
            Debug.Log("Game started.");
            // Implement game start logic
        }

        private void OnRefreshAll()
        {
            Debug.Log("Refreshing all visuals.");
            // Implement logic to refresh all visuals
        }

        #region Tower
        private void InitiateTowerIcon(TowerData tower)
        {
            GameObject towerMarker = Instantiate(towerPrefab, tower.position, Quaternion.identity);
            towerMarker.name = $"Tower_{tower.team}_{tower.position.x}_{tower.position.z}";
            towerMarker.transform.SetParent(this.transform);

            TowerIconController towerIC = towerMarker.GetComponent<TowerIconController>();
            if (towerIC != null)
            {
                towerIC.Initialize(tower);
                towerIcons[tower.towerId] = towerIC;
            }
            else
            {
                Debug.LogError("TowerVisual component not found on tower prefab.");
            }
        }

        private void RemoveTowerMarker(TowerData tower)
        {
            if (towerIcons.TryGetValue(tower.towerId, out TowerIconController towerIC))
            {
                Destroy(towerIC);
                towerIcons.Remove(tower.towerId);
            }
        }

        #endregion
    }
}