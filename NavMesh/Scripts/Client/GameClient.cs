using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

namespace CCengine.Client
{
    /// <summary>
    /// Main script for the client-side of the game, should be in game scene only
    /// Will connect to server, then connect to the game on that server (with uid) and then will send game settings
    /// During the game, will send all actions performed by the player and receive game refreshes
    /// </summary>

    public class GameClient : MonoBehaviour
    {
        //--- These settings are set in the menu scene and when the game start will be sent to server

        public static GameSettings game_settings = GameSettings.Default;
        public static PlayerSettings player_settings = PlayerSettings.Default;
        public static PlayerSettings ai_settings = PlayerSettings.DefaultAI;
        public static string observe_user = null; //Which user should it observe, null if not an obs

        //-----

        public UnityAction onConnectServer;
        public UnityAction onConnectGame;
        public UnityAction<int> onPlayerReady;

        public UnityAction<SLGameData> onGameStart;
        public UnityAction<int> onGameEnd;              //winner player_id
        public UnityAction<int> onNewTurn;              //current player_id

        public UnityAction<UpdateMessage> onUpdateMessage;
        public UnityAction<MsgAssignTask> onTaskAssigned;
        public UnityAction<MsgTaskCompleted> onTaskCompleted;

        // public UnityAction<MsgEntityMove> onEntityMove;

        public UnityAction<int, string> onChatMsg;  //player_id, msg
        public UnityAction<string> onServerMsg;  //msg
        public UnityAction<SLGameData> onRefreshAll;

        private int player_id = 0; //Player playing on this device;
        private SLGameData sl_gd;

        private bool observe_mode = false;
        private int observe_player_id = 0;
        private float timer = 0f;


        private Dictionary<ushort, RefreshEvent> registered_commands = new Dictionary<ushort, RefreshEvent>();

        private static GameClient instance;

        protected virtual void Awake()
        {
            instance = this;
            Application.targetFrameRate = 120;
        }

        protected virtual void Start()
        {
            RegisterRefresh(GameAction.Connected, OnConnectedToGame);
            RegisterRefresh(GameAction.PlayerReady, OnPlayerReady);
            RegisterRefresh(GameAction.GameStart, OnGameStart);
            RegisterRefresh(GameAction.GameEnd, OnGameEnd);
            // RegisterRefresh(GameAction.NewTurn, OnNewTurn);

            // RegisterRefresh(GameAction.EntityMoved, OnEntityMoved);
            RegisterRefresh(GameAction.AssignTask, OnTaskAssigned);
            RegisterRefresh(GameAction.TaskCompleted, OnTaskCompleted);

            RegisterRefresh(GameAction.ChatMessage, OnChat);
            RegisterRefresh(GameAction.ServerMessage, OnServerMsg);
            RegisterRefresh(GameAction.UpdateMessage, OnUpdateMessage); 
            RegisterRefresh(GameAction.RefreshAll, OnRefreshAll);

            TcgNetwork.Get().onConnect += OnConnectedServer;
            TcgNetwork.Get().Messaging.ListenMsg("refresh", OnReceiveRefresh);

            ConnectToAPI();
            ConnectToServer();
        }

        protected virtual void OnDestroy()
        {
            TcgNetwork.Get().onConnect -= OnConnectedServer;
            TcgNetwork.Get().Messaging.UnListenMsg("refresh");
        }

        protected virtual void Update()
        {
            bool is_starting = sl_gd == null || sl_gd.state == GameState.Connecting;
            bool is_client = !game_settings.IsHost();
            bool is_connecting = TcgNetwork.Get().IsConnecting();
            bool is_connected = TcgNetwork.Get().IsConnected();

            //Exit game scene if cannot connect after a while
            if (is_starting && is_client)
            {
                timer += Time.deltaTime;
                if (timer > 10f)
                {
                    SceneNav.GoTo("Menu");
                }
            }
 
            //Reconnect to server
            if (!is_starting && !is_connecting && is_client && !is_connected)
            {
                timer += Time.deltaTime;
                if (timer > 5f)
                {
                    timer = 0f;
                    ConnectToServer();
                }
            }
        }

        #region Connect To

        public virtual void ConnectToAPI()
        {
            // Should already be logged in from the menu
            // If not connected, start in test mode (this means game scene was launched directly from Unity)

            if (!Authenticator.Get().IsSignedIn())
            {
                Authenticator.Get().LoginTest("Player");

                // if (!player_settings.HasDeck())
                // {
                //     player_settings.deck = new UserDeckData(GameplayData.Get().test_deck);
                // }

                // if (!ai_settings.HasDeck())
                // {
                //     ai_settings.deck = new UserDeckData(GameplayData.Get().test_deck_ai);
                //     ai_settings.ai_level = GameplayData.Get().ai_level;
                // }
            }

            //Set avatar, cardback based on your api data
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                player_settings.avatar = udata.GetAvatar();
                player_settings.cardback = udata.GetCardback();
            }
        }

        public virtual async void ConnectToServer()
        {
            await System.Threading.Tasks.Task.Delay(100); //Wait for initialization to finish

            if (TcgNetwork.Get().IsActive())
                return; // Already connected

            if (game_settings.IsHost() && NetworkData.Get().solo_type == SoloType.Offline)
            {
                TcgNetwork.Get().StartHostOffline();    //WebGL dont support hosting a game, must join a dedicated server, in solo it starts a offline mode that doesn't use netcode at all
            }
            else if (game_settings.IsHost())
            {
                TcgNetwork.Get().StartHost(NetworkData.Get().port);       //Host a game, either solo or for P2P, still using netcode in solo to have consistant behavior when testing solo vs multi
            }
            else
            {
                TcgNetwork.Get().StartClient(game_settings.GetUrl(), NetworkData.Get().port);       //Join server
            }
        }

        public virtual async void ConnectToGame(string uid)
        {
            await System.Threading.Tasks.Task.Delay(100); //Wait for initialization to finish

            if (!TcgNetwork.Get().IsActive())
                return; //Not connected to server

            Debug.Log("Connect to Game: " + uid);

            MsgPlayerConnect nplayer = new MsgPlayerConnect();
            nplayer.user_id = Authenticator.Get().UserID;
            nplayer.username = Authenticator.Get().Username;
            nplayer.game_uid = uid;
            nplayer.nb_players = game_settings.nb_players;
            nplayer.observer = game_settings.game_type == GameType.Observer;

            Messaging.SendObject("connect", ServerID, nplayer, NetworkDelivery.Reliable);
        }

        public virtual void Disconnect()
        {
            TcgNetwork.Get().Disconnect();
        }

        private void RegisterRefresh(ushort tag, UnityAction<SerializedData> callback)
        {
            RefreshEvent cmdevt = new RefreshEvent();
            cmdevt.tag = tag;
            cmdevt.callback = callback;
            registered_commands.Add(tag, cmdevt);
        }

        public void OnReceiveRefresh(ulong client_id, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ushort type);
            bool found = registered_commands.TryGetValue(type, out RefreshEvent command);
            if (found)
            {
                command.callback.Invoke(new SerializedData(reader));
            }
        }

        #endregion

        #region Setting

        public virtual void SendGameSettings()
        {
            if (game_settings.IsOffline())
            {
                //Solo mode, send both your settings and AI settings
                SendGameplaySettings(game_settings);
                SendPlayerSettingsAI(ai_settings);
                SendPlayerSettings(player_settings);
            }
            else
            {
                //Online mode, only send your own settings
                SendGameplaySettings(game_settings);
                SendPlayerSettings(player_settings);
            }
        }

        public void SendPlayerSettings(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettings, psettings, NetworkDelivery.ReliableFragmentedSequenced);
        }

        public void SendPlayerSettingsAI(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettingsAI, psettings, NetworkDelivery.ReliableFragmentedSequenced);
        }

        public void SendGameplaySettings(GameSettings settings)
        {
            SendAction(GameAction.GameSettings, settings, NetworkDelivery.ReliableFragmentedSequenced);
        }
        #endregion

        #region Send Action

        public void AssignTask(string athleteId, string taskCardUid, Vector2 targetPosition)
        {
            MsgAssignTask msg = new MsgAssignTask
            {
                athleteId = athleteId,
                taskId = taskCardUid,
                targetPosition = targetPosition
            };
            SendAction(GameAction.AssignTask, msg);
        }

        public void CancelSelection()
        {
            SendAction(GameAction.CancelSelect);
        }

        public void SendChatMsg(string msg)
        {
            MsgChat chat = new MsgChat();
            chat.msg = msg;
            chat.player_id = player_id;
            SendAction(GameAction.ChatMessage, chat);
        }


        public void Resign()
        {
            SendAction(GameAction.Resign);
        }

        public void SetObserverMode(int player_id)
        {
            observe_mode = true;
            observe_player_id = player_id;
        }

        public void SetObserverMode(string username)
        {
            observe_player_id = 0; //Default value of observe_user not found

            SLGameData data = GetGameData();
            foreach (PlayerData player in data.players)
            {
                if (player.username == username)
                {
                    observe_player_id = player.player_id;
                }
            }
        }

        public void SendAction<T>(ushort type, T data, NetworkDelivery delivery = NetworkDelivery.Reliable) where T : INetworkSerializable
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            writer.WriteNetworkSerializable(data);
            Messaging.Send("action", ServerID, writer, delivery);
            writer.Dispose();
        }

        public void SendAction(ushort type, int data)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            writer.WriteValueSafe(data);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        public void SendAction(ushort type)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        #endregion

        #region --- Receive Refresh (On) ----------------------

        protected virtual void OnConnectedServer()
        {
            ConnectToGame(game_settings.game_uid);
            onConnectServer?.Invoke();
        }

        protected virtual void OnConnectedToGame(SerializedData sdata)
        {
            MsgAfterConnected msg = sdata.Get<MsgAfterConnected>();
            player_id = msg.player_id;
            sl_gd = msg.game_data;
            observe_mode = player_id < 0; //Will usually return -1 if its an observer

            if (observe_mode)
                SetObserverMode(observe_user);

            if (onConnectGame != null)
                onConnectGame.Invoke();

            SendGameSettings();
        }

        protected virtual void OnPlayerReady(SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            int pid = msg.value;

            if (onPlayerReady != null)
                onPlayerReady.Invoke(pid);
        }

        private void OnGameStart(SerializedData sdata)
        {
            SLGameData msg = sdata.Get<SLGameData>();

            // Invoke the onGameStart event with GameData
            onGameStart?.Invoke(msg);
        }

        private void OnGameEnd(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onGameEnd?.Invoke(msg.player_id);
        }

        private void OnNewTurn(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onNewTurn?.Invoke(msg.player_id);
        }

        // private void OnEntityMoved(SerializedData data)
        // {
        //     MsgEntityMove msg = data.Get<MsgEntityMove>();
        //     onEntityMove?.Invoke(msg);
        // }

        private void OnTaskAssigned(SerializedData data)
        {
            MsgAssignTask msg = data.Get<MsgAssignTask>();
            onTaskAssigned?.Invoke(msg);
        }

        private void OnTaskCompleted(SerializedData data)
        {
            MsgTaskCompleted msg = data.Get<MsgTaskCompleted>();
            onTaskCompleted?.Invoke(msg);
        }

        private void OnChat(SerializedData sdata)
        {
            MsgChat msg = sdata.Get<MsgChat>();
            onChatMsg?.Invoke(msg.player_id, msg.msg);
        }

        private void OnServerMsg(SerializedData sdata)
        {
            string msg = sdata.GetString();
            onServerMsg?.Invoke(msg);
        }

        private void OnUpdateMessage(SerializedData sdata) {
            UpdateMessage updateMessage = sdata.Get<UpdateMessage>();
            onUpdateMessage?.Invoke(updateMessage);
        }

        private void OnRefreshAll(SerializedData sdata)
        {
            SLGameData msg = sdata.Get<SLGameData>();
            onRefreshAll?.Invoke(msg);
        }

        #endregion

        #region Get

        public virtual bool IsReady()
        {
            return sl_gd != null && TcgNetwork.Get().IsConnected();
        }

        public PlayerData GetPlayer()
        {
            SLGameData gdata = GetGameData();
            return gdata.GetPlayerData(GetPlayerID());
        }

        public PlayerData GetOpponentPlayer()
        {
            SLGameData gdata = GetGameData();
            return gdata.GetPlayerData(GetOpponentPlayerID());
        }

        public int GetPlayerID()
        {
            if (observe_mode)
                return observe_player_id;
            return player_id;
        }

        public int GetOpponentPlayerID()
        {
            return GetPlayerID() == 0 ? 1 : 0;
        }

        public bool IsObserveMode()
        {
            return observe_mode;
        }

        public SLGameData GetGameData()
        {
            return sl_gd;
        }

        public bool HasEnded()
        {
            return sl_gd != null && sl_gd.HasEnded();
        }

        private void OnApplicationQuit()
        {
            Resign(); //Auto Resign before closing the app. NOTE: doesn't seem to work since the msg dont have time to be sent before it closes
        }

        public bool IsHost { get { return TcgNetwork.Get().IsHost; } }
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }

        public static GameClient Get()
        {
            return instance;
        }
        #endregion
    }

    public class RefreshEvent
    {
        public ushort tag;
        public UnityAction<SerializedData> callback;
    }
}