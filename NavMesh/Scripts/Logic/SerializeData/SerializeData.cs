using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace CCengine
{
    [Serializable]
    public class PlayerData : INetworkSerializable
    {
        public int player_id;
        public string username;
        public bool connected;
        public bool ready;

        // Add a parameterless constructor for the serializer
        public PlayerData() { }

        public bool IsReady() { return ready; }
        public bool IsConnected() { return connected; }

        public void NetworkSerialize<TSerializer>(BufferSerializer<TSerializer> serializer) where TSerializer : IReaderWriter
        {
            if (serializer.IsReader)
            {
                // Defensive checks after deserialization
                if (string.IsNullOrEmpty(username))
                {
                    Debug.LogWarning($"PlayerData with ID {player_id} has a null or empty username after deserialization.");
                    
                }
            }
            serializer.SerializeValue(ref player_id);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref connected);
            serializer.SerializeValue(ref ready);
        }
    }

    [Serializable]
    public class AthleteData : INetworkSerializable
    {
        public string athleteId;
        public string name;
        public Team team;
        public string heroId; // ID of the assigned hero, if any

        public void NetworkSerialize<TSerializer>(BufferSerializer<TSerializer> serializer) where TSerializer : IReaderWriter
        {
            if (serializer.IsReader)
            {
                if (string.IsNullOrEmpty(athleteId))
                {
                    Debug.LogError("athleteId has a null ID.");
                }
            }
            serializer.SerializeValue(ref athleteId);
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref team);
            serializer.SerializeValue(ref heroId);
        }
    }

    [Serializable]
    public class HeroData : INetworkSerializable
    {
        public string heroId;
        public string name;
        public Team team;
        public SLVector3 position;
        public int currentHP;
        public int maxHP;

        public HeroData() { }

        public void NetworkSerialize<TSerializer>(BufferSerializer<TSerializer> serializer) where TSerializer : IReaderWriter
        {
            if (serializer.IsReader)
            {
                if (string.IsNullOrEmpty(heroId))
                {
                    Debug.LogError("heroId has a null ID.");
                }
            }
            serializer.SerializeValue(ref heroId);
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref team);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref currentHP);
            serializer.SerializeValue(ref maxHP);
        }
    }

    [Serializable]
    public class TowerData : INetworkSerializable
    {
        public string towerId;
        public string name;
        public Team team;
        public SLVector3 position;
        public int currentHP;
        public int maxHP;

        public TowerData() { }

        public void NetworkSerialize<TSerializer>(BufferSerializer<TSerializer> serializer) where TSerializer : IReaderWriter
        {
            if (serializer.IsReader)
            {
                if (string.IsNullOrEmpty(towerId))
                {
                    Debug.LogError("towerId has a null ID.");
                }
            }
            serializer.SerializeValue(ref towerId);
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref team);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref currentHP);
            serializer.SerializeValue(ref maxHP);
        }
    }

    [Serializable]
    public class CreepWaveData : INetworkSerializable
    {
        public string creepWaveId;
        public string name;
        public Team team;
        public int atk;
        public SLVector3 position;
        public int currentHP;
        public int maxHP;


        // Parameterless constructor for deserialization
        public CreepWaveData() { }

        // Implementation of the NetworkSerialize method
        public void NetworkSerialize<TSerializer>(BufferSerializer<TSerializer> serializer) where TSerializer : IReaderWriter
        {
            if (serializer.IsReader)
            {
                if (string.IsNullOrEmpty(creepWaveId))
                {
                    Debug.LogError("creepWaveId has a null ID.");
                }
            }
            serializer.SerializeValue(ref creepWaveId);
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref team);
            serializer.SerializeValue(ref atk);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref currentHP);
            serializer.SerializeValue(ref maxHP);
        }
    }


    // GameData that have only neccesary serialized data
    // Include whatever fields represent your current game state
    [Serializable]
    public class SLGameData : INetworkSerializable
    {
        public int currentTurn;
        public GameState state;
        public PlayerData[] players;
        public AthleteData[] athletes;
        public HeroData[] heroes;
        public TowerData[] towers;
        public CreepWaveData[] creepWaves;

        public SLGameData() { }

        public void NetworkSerialize<TSerializer>(BufferSerializer<TSerializer> serializer) where TSerializer : IReaderWriter
        {
            Debug.LogError("Serializing SLGameData");
            serializer.SerializeValue(ref currentTurn);
            serializer.SerializeValue(ref state);

            // Serialize arrays of INetworkSerializable types
            NetworkTool.NetSerializeArray<PlayerData, TSerializer>(serializer, ref players);
            NetworkTool.NetSerializeArray<AthleteData, TSerializer>(serializer, ref athletes);
            NetworkTool.NetSerializeArray<HeroData, TSerializer>(serializer, ref heroes);
            NetworkTool.NetSerializeArray<TowerData, TSerializer>(serializer, ref towers);
            NetworkTool.NetSerializeArray<CreepWaveData, TSerializer>(serializer, ref creepWaves);
        }

        public IReadOnlyCollection<HeroData> GetAllHeroes()
        {
            return heroes != null ? Array.AsReadOnly(heroes) : new List<HeroData>().AsReadOnly();
        }

        public IReadOnlyCollection<CreepWaveData> GetAllCreepWaves()
        {
            return creepWaves != null ? Array.AsReadOnly(creepWaves) : new List<CreepWaveData>().AsReadOnly();
        }

        public IReadOnlyCollection<TowerData> GetAllTowers()
        {
            return towers != null ? Array.AsReadOnly(towers) : new List<TowerData>().AsReadOnly();
        }

        public PlayerData GetPlayerData(int id)
        {
            if (id >= 0 && id < players.Length)
                return players[id];
            return null;
        }

        //  a helper method to build SLGameData from the server's GameData
        public static SLGameData FromGameData(GameData gd)
        {
            Debug.LogError("Converting GameData to SLGameData");
            {
                SLGameData sldata = new SLGameData();
                sldata.currentTurn = gd.CurrentTurn;
                sldata.state = gd.state;

                // Convert players
                List<PlayerData> playerList = new List<PlayerData>();
                foreach (Player p in gd.players)
                {
                    playerList.Add(new PlayerData
                    {
                        player_id = p.playerId,
                        username = p.username,
                        connected = p.IsConnected(),
                        ready = p.IsReady()
                    });
                }
                sldata.players = playerList.ToArray();

                // Convert athletes
                List<AthleteData> athleteList = new List<AthleteData>();
                foreach (Athlete a in gd.AllAthletes.Values)
                {
                    if (a.Id == null) Debug.LogError("Athlete has a null ID.");

                    athleteList.Add(new AthleteData
                    {
                        athleteId = a.Id,
                        name = a.Name,
                        team = a.Team,
                        heroId = a.Hero.Id
                    });
                }
                sldata.athletes = athleteList.ToArray();

                // Convert heroes
                List<HeroData> heroList = new List<HeroData>();
                foreach (Hero h in gd.AllHeroes.Values)
                {
                    if (h.Id == null) Debug.LogError("Hero has a null ID.");
                    heroList.Add(new HeroData
                    {
                        heroId = h.Id,
                        name = h.Name,
                        position = new SLVector3(h.Position.x, h.Position.z),
                        currentHP = h.CurrentHealth,
                        maxHP = h.MaxHealth
                    });
                }
                sldata.heroes = heroList.ToArray();

                // Convert towers
                List<TowerData> towerList = new List<TowerData>();
                foreach (Tower tower in gd.AllTowers.Values)
                {
                    if (tower.Id == null) Debug.LogError("Tower has a null ID.");
                    towerList.Add(new TowerData
                    {
                        towerId = tower.Id,
                        name = tower.Name,
                        position = new SLVector3(tower.Position.x, tower.Position.z),
                        currentHP = tower.CurrentHealth,
                        maxHP = tower.MaxHealth
                    });
                }
                sldata.towers = towerList.ToArray();

                // Convert creep waves using foreach
                List<CreepWaveData> creepWaveList = new List<CreepWaveData>();
                foreach (CreepWave cw in gd.AllCreepWaves.Values)
                {
                    if (cw.Id == null) Debug.LogError("CreepWave has a null ID.");
                    creepWaveList.Add(new CreepWaveData
                    {
                        creepWaveId = cw.Id,
                        name = cw.Name,
                        team = cw.Team,
                        maxHP = cw.MaxHealth,
                        atk = cw.Attack,
                        position = new SLVector3(cw.Position.x, cw.Position.z),
                    });
                }
                sldata.creepWaves = creepWaveList.ToArray();

                return sldata;
            }
        }

        public bool HasEnded()
        {
            return state == GameState.GameEnded;
        }
    }

    // [Serializable]
    // public class MsgGameData : INetworkSerializable
    // {
    //     public SLGameData gameData;

    //     public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    //     {
    //         gameData.NetworkSerialize(serializer);
    //     }
    // }

}
