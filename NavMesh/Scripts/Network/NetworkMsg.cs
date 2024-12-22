﻿
using Unity.Netcode;
using UnityEngine.Events;
using UnityEngine;

namespace CCengine
{
    #region -------- Connection --------

    public class MsgPlayerConnect : INetworkSerializable
    {
        public string user_id;
        public string username;
        public string game_uid;
        public int nb_players;
        public bool observer; //join as observer

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref user_id);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref game_uid);
            serializer.SerializeValue(ref nb_players);
            serializer.SerializeValue(ref observer);
        }
    }

    public class MsgAfterConnected : INetworkSerializable
    {
        public bool success;
        public int player_id;
        public SLGameData game_data;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref success);
            serializer.SerializeValue(ref player_id);

            if (serializer.IsReader)
            {
                int size = 0;
                serializer.SerializeValue(ref size);
                if (size > 0)
                {
                    byte[] bytes = new byte[size];
                    serializer.SerializeValue(ref bytes);
                    game_data = NetworkTool.Deserialize<SLGameData>(bytes);
                }
            }

            if (serializer.IsWriter)
            {
                byte[] bytes = NetworkTool.Serialize(game_data);
                int size = bytes.Length;
                serializer.SerializeValue(ref size);
                if (size > 0)
                    serializer.SerializeValue(ref bytes);
            }
        }
    }
    #endregion

    #region -------- Matchmaking --------

    public class MsgMatchmaking : INetworkSerializable
    {
        public string user_id;
        public string username;
        public string group;
        public int players;
        public int elo;
        public bool refresh;
        public float time;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref user_id);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref players);
            serializer.SerializeValue(ref elo);
            serializer.SerializeValue(ref refresh);
            serializer.SerializeValue(ref time);
        }
    }

    public class MatchmakingResult : INetworkSerializable
    {
        public bool success;
        public int players;
        public string group;
        public string server_url;
        public string game_uid;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref success);
            serializer.SerializeValue(ref players);
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref server_url);
            serializer.SerializeValue(ref game_uid);
        }
    }

    public class MsgMatchmakingList : INetworkSerializable
    {
        public string username;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref username);
        }
    }

    [System.Serializable]
    public struct MatchmakingListItem : INetworkSerializable
    {
        public string group;
        public string user_id;
        public string username;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref user_id);
            serializer.SerializeValue(ref username);
        }
    }

    public class MatchmakingList : INetworkSerializable
    {
        public MatchmakingListItem[] items;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            NetworkTool.NetSerializeArray(serializer, ref items);
        }
    }

    [System.Serializable]
    public class MatchListItem : INetworkSerializable
    {
        public string group;
        public string username;
        public string game_uid;
        public string game_url;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref group);
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref game_uid);
            serializer.SerializeValue(ref game_url);
        }
    }

    public class MatchList : INetworkSerializable
    {
        public MatchListItem[] items;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            NetworkTool.NetSerializeArray(serializer, ref items);
        }
    }
    #endregion

    #region -------- In Game --------

    public class MsgPlayCard : INetworkSerializable
    {
        public int playerId;
        public string cardId;
        public string[] athleteIds;
        public SLVector3 targetPosition;

        public void NetworkSerialize<TSerializer>(BufferSerializer<TSerializer> serializer) where TSerializer : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref cardId);
            serializer.SerializeValue(ref targetPosition);
            NetworkTool.NetSerializeStringArray(serializer, ref athleteIds); // Use dedicated method for string arrays
        }
    }

    public class MsgCard : INetworkSerializable
    {
        public string card_uid;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref card_uid);
        }
    }

    public class MsgPlayer : INetworkSerializable
    {
        public int player_id;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player_id);
        }
    }


    public class MsgInt : INetworkSerializable
    {
        public int value;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref value);
        }
    }

    // public class MsgEntityMove : INetworkSerializable
    // {
    //     public string entityId;
    //     public EntityType entityType;
    //     public Vector2 newPosition;

    //     public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    //     {
    //         serializer.SerializeValue(ref entityId);
    //         serializer.SerializeValue(ref entityType);
    //         serializer.SerializeValue(ref newPosition);
    //     }
    // }

    public class MsgAssignTask : INetworkSerializable
    {
        public string athleteId;
        public string taskId;
        public TaskType taskType;
        public Vector3 targetPosition;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref athleteId);
            serializer.SerializeValue(ref taskId);
            serializer.SerializeValue(ref taskType);
            serializer.SerializeValue(ref targetPosition);
        }
    }

    public class MsgTaskCompleted : INetworkSerializable
    {
        public string athleteId;
        public string taskId;
        public bool success;
        public string result; // Optional: Additional information about the task result

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref athleteId);
            serializer.SerializeValue(ref taskId);
            serializer.SerializeValue(ref success);
            serializer.SerializeValue(ref result);
        }
    }

    public class MsgChat : INetworkSerializable
    {
        public int player_id;
        public string msg;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref player_id);
            serializer.SerializeValue(ref msg);
        }
    }

    // public class MsgRefreshAll : INetworkSerializable
    // {
    //     public GameData game_data;

    //     public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    //     {
    //         if (serializer.IsReader)
    //         {
    //             int size = 0;
    //             serializer.SerializeValue(ref size);
    //             if (size > 0)
    //             {
    //                 byte[] bytes = new byte[size];
    //                 serializer.SerializeValue(ref bytes);
    //                 game_data = NetworkTool.Deserialize<GameData>(bytes);
    //             }
    //         }

    //         if (serializer.IsWriter)
    //         {
    //             byte[] bytes = NetworkTool.Serialize(game_data);
    //             int size = bytes.Length;
    //             serializer.SerializeValue(ref size);
    //             if (size > 0)
    //                 serializer.SerializeValue(ref bytes);
    //         }
    //     }
    // }
    #endregion

}