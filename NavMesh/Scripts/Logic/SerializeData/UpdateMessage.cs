using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

namespace CCengine
{ 
    [System.Serializable]
    public class UpdateMessage : INetworkSerializable
    {
        public List<EntityMovedMessage> EntityMovedMessages;
        public List<HealthChangedMessage> HealthChangedMessages;
        public List<EntitySpawnedMessage> EntitySpawnedMessages;
        // public List<EntityDestroyedMessage> EntityDestroyedMessages;

        public GameStateMessage gameStateMessage;

        public UpdateMessage()
        {
            EntityMovedMessages = new List<EntityMovedMessage>();
            HealthChangedMessages = new List<HealthChangedMessage>();
            EntitySpawnedMessages = new List<EntitySpawnedMessage>();
            // EntityDestroyedMessages = new List<EntityDestroyedMessage>();
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            // Serialize EntityMovedMessages
            NetworkTool.NetSerializeList(serializer, ref EntityMovedMessages);

            // Serialize HealthChangedMessages
            NetworkTool.NetSerializeList(serializer, ref HealthChangedMessages);

            // Serialize EntitySpawnedMessages
            NetworkTool.NetSerializeList(serializer, ref EntitySpawnedMessages);

            // Serialize GameStateMessage
            bool hasGameStateMessage = gameStateMessage != null;
            serializer.SerializeValue(ref hasGameStateMessage);
            if (hasGameStateMessage)
            {
                if (serializer.IsReader)
                {
                    gameStateMessage = new GameStateMessage();
                }
                gameStateMessage.NetworkSerialize(serializer);
            }
        } 
    }

    [System.Serializable]
    public class GameStateMessage : INetworkSerializable
    {
        public int CurrentTurn;
        public GameState State;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CurrentTurn);
            serializer.SerializeValue(ref State);
        }
    }

    [System.Serializable]
    public class EntityMovedMessage : INetworkSerializable
    {
        public string EntityId;
        public EntityType EntityType;
        public SLVector3 NewPosition;

        public EntityMovedMessage() { } // Parameterless constructor for deserialization

        public EntityMovedMessage(string entityId, EntityType entityType, SLVector3 newPosition)
        {
            EntityId = entityId;
            EntityType = entityType;
            NewPosition = newPosition;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref EntityId);
            serializer.SerializeValue(ref EntityType);
            serializer.SerializeValue(ref NewPosition);
        }
    }

    [System.Serializable]
    public class HealthChangedMessage : INetworkSerializable
    {
        public string EntityId;
        public EntityType EntityType;
        public int NewHealth;

        public HealthChangedMessage() { } // Parameterless constructor for deserialization

        public HealthChangedMessage(string entityId, EntityType entityType, int newHealth)
        {
            EntityId = entityId;
            EntityType = entityType;
            NewHealth = newHealth;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (EntityId == null) EntityId = string.Empty;
            serializer.SerializeValue(ref EntityId);
            serializer.SerializeValue(ref EntityType);
            serializer.SerializeValue(ref NewHealth);
        }
    }

    [System.Serializable]
    public class EntitySpawnedMessage : INetworkSerializable
    {
        public string Id;
        public EntityType EntityType;
        public string Name;
        public Team Team;
        // Optional variables
        public SLVector3 InitialPosition;
        public Lane Lane;
        public int MaxHealth;
        public int Atk;
        public int Mana;
        public EntitySpawnedMessage() { } // Parameterless constructor for deserialization

        public EntitySpawnedMessage(
            string id,
            EntityType entityType,
            string name,
            Team team,
            SLVector3 initialPosition,
            Lane lane,
            int maxHealth,
            int atk,
            int mana)
        {
            Id = id;
            EntityType = entityType;
            Name = name;
            Team = team;

            InitialPosition = initialPosition;
            Lane = lane;
            MaxHealth = maxHealth;
            Atk = atk;
            Mana = mana;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Id);
            serializer.SerializeValue(ref EntityType);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Team);

            // Serialize optional variables
            serializer.SerializeValue(ref InitialPosition);
            serializer.SerializeValue(ref Lane);
            serializer.SerializeValue(ref MaxHealth);
            serializer.SerializeValue(ref Atk);
            serializer.SerializeValue(ref Mana);
        }
    }

    // public class EntityDestroyedMessage
    // {
    //     public int EntityId;

    //     public EntityDestroyedMessage(int entityId)
    //     {
    //         EntityId = entityId;
    //     }
    // }
}
