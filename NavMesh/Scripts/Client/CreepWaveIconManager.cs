using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace CCengine.Client
{
    public class CreepWaveIconManager : MonoBehaviour
    {
        public static CreepWaveIconManager Instance;

        [SerializeField] private GameObject creepWavePrefab;
        public Dictionary<string, CreepWaveIconController> creepWaveICs = new Dictionary<string, CreepWaveIconController>();
        private Queue<GameObject> creepWavePool = new Queue<GameObject>();
        private float creepOffset = 0.025f; // Creep position offset in cell for each team 

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }

            InitializePool();
        }


        public Vector2 GetTeamOffset(Team team)
        {
            if (team == Team.TeamA)
            {
                return new Vector2(-creepOffset, -creepOffset);
            }
            else if (team == Team.TeamB)
            {
                return new Vector2(creepOffset, creepOffset);
            }
            else
            {
                return Vector2.zero;
            }
        }

        #region Pooling
        private void InitializePool()
        {
            int poolSize = 40; // Adjust based on expected maximum number of active creeps
            for (int i = 0; i < poolSize; i++)
            {
                GameObject creepWaveGO = Instantiate(creepWavePrefab);
                creepWaveGO.SetActive(false);
                creepWavePool.Enqueue(creepWaveGO);
            }
        }

        public void ActivateCreepWaveIcon(CreepWaveData creepWave)
        {
            if (creepWaveICs.TryGetValue(creepWave.creepWaveId, out CreepWaveIconController cwIC))
            {
                cwIC.gameObject.SetActive(true);
                cwIC.Initialize(creepWave);
            }
            else
            {
                // Get from object pool or instantiate if necessary
                GameObject newCreepWaveGO = GetCreepWaveGO();
                newCreepWaveGO.SetActive(true);
                newCreepWaveGO.name = $"{creepWave.name}";
                newCreepWaveGO.transform.SetParent(this.transform);

                CreepWaveIconController creepIcon = newCreepWaveGO.GetComponent<CreepWaveIconController>();
                if (creepIcon != null)
                {
                    creepIcon.Initialize(creepWave);
                }
                else
                {
                    Debug.LogError("CreepWaveIconController component not found on the creep wave marker prefab.");
                }

                // Register in the dictionary
                creepWaveICs[creepWave.creepWaveId] = creepIcon;
            }
        }

        public GameObject GetCreepWaveGO()
        {
            if (creepWavePool.Count > 0)
            {
                return creepWavePool.Dequeue();
            }
            else
            {
                // Optionally expand pool if needed
                GameObject creepWaveGO = Instantiate(creepWavePrefab);
                creepWaveGO.SetActive(false);
                return creepWaveGO;
            }
        }

        public void MoveCreepWave(string cwId, Vector3 newPosition)
        {
            if (creepWaveICs.TryGetValue(cwId, out CreepWaveIconController creepWaveIC))
            {
                creepWaveIC.AnimateMove(newPosition);
            }
            else
            {
                Debug.LogError("CreepWave GameObject not found.");
            }
        }

        public void UpdateHPCreepWave(string cwId, int newHP)
        {
            if (creepWaveICs.TryGetValue(cwId, out CreepWaveIconController creepWaveIC))
            {
                creepWaveIC.UpdateHealthBar(newHP);
            }
            else
            {
                Debug.LogError("CreepWave GameObject not found.");
            }
        }

        public void DeactivateCreepWaveIcon(string Id)
        {
            if (creepWaveICs.TryGetValue(Id, out CreepWaveIconController creepWaveIC))
            {
                // Return to pool
                ReturnCreepWave(creepWaveIC.gameObject);
            }
            else
            {
                Debug.LogWarning($"Attempted to deactivate non-existent creepWaveId: {creepWaveIC.cwData.creepWaveId}");
            }
        }

        public void ReturnCreepWave(GameObject creepWaveGO)
        {
            CreepWaveIconController creepWaveIC = creepWaveGO.GetComponent<CreepWaveIconController>();
            if (creepWaveIC != null)
            {
                string cwId = creepWaveIC.cwData.creepWaveId;
                if (creepWaveICs.ContainsKey(cwId))
                {
                    creepWaveICs.Remove(cwId);
                }
                else
                {
                    Debug.LogWarning($"Attempted to remove creepWaveId: {cwId} which is not present in the dictionary.");
                }
            }
            else
            {
                Debug.LogError("CreepWaveIconController component not found on the GameObject.");
            }

            creepWaveGO.SetActive(false);
            creepWavePool.Enqueue(creepWaveGO);
        }
        #endregion
    }
}
