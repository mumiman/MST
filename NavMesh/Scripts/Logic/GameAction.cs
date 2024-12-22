
using Unity.Netcode;
using UnityEngine.Events;

namespace CCengine
{
    /// <summary>
    /// List of game actions and refreshes, that can be performed by the player or received
    /// </summary>

    public static class GameAction
    {
        public const ushort None = 0;

        //Commands (client to server)
        public const ushort SelectAthlete = 1032;
        public const ushort SelectTask = 1030;
        public const ushort CancelSelect = 1039;
        public const ushort AssignTask = 1040;
        public const ushort TaskCompleted = 8;



        public const ushort Resign = 1050;
        public const ushort ChatMessage = 1090;

        public const ushort PlayerSettings = 1100; //After connect, send player data
        public const ushort PlayerSettingsAI = 1102; //After connect, send player data
        public const ushort GameSettings = 1105; //After connect, send gameplay settings

        //Refresh (server to client)
        public const ushort Connected = 2000;
        public const ushort PlayerReady = 2001;

        public const ushort GameStart = 2010;
        public const ushort GameEnd = 2012;

        public const ushort EntityMoved = 2020;
        public const ushort EntityAttacked = 2030;

        public const ushort CardPlayed = 2040;
        public const ushort TaskAssigned = 2050;

        public const ushort UpdateMessage = 2150;
        public const ushort ServerMessage = 2190; //Server warning msg
        public const ushort RefreshAll = 2100;

        public static string GetString(ushort type)
        {
            switch (type)
            {
                case AssignTask: return "AssignTask";
                case CancelSelect: return "CancelSelect";

                case Resign: return "Resign";
                case ChatMessage: return "ChatMessage";
                case Connected: return "Connected";
                case PlayerReady: return "PlayerReady";
                case GameStart: return "GameStart";
                case GameEnd: return "GameEnd";

                case EntityMoved: return "UnitMoved";
                case EntityAttacked: return "UnitAttacked";
                case TaskAssigned: return "TaskAssigned";
                case CardPlayed: return "CardPlayed";

                case ServerMessage: return "ServerMessage";
                case UpdateMessage: return "UpdateMessage";
                case RefreshAll: return "RefreshAll";
                // Add cases for other actions...
                default: return "UnknownAction";
            }
        }
    }
}