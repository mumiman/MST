using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace CCengine.Server
{
    /// <summary>
    /// Represent one game on the server, when playing solo this will be created locally, 
    /// or if online multiple GameServer, one for each match, will be created by the dedicated server
    /// Manage receiving actions, sending refresh, and running AI
    /// </summary>

    public class GameServer
    {
        public string game_uid; //Game unique ID
        public int nb_players = 2;

        public static float game_expire_time = 30f;      //How long for the game to be deleted when no one is connected
        public static float win_expire_time = 120f;       //How long for a player to be declared winnner if hes the only one connected

        private GameData gameData;
        private GameLogic gameLogic;
        private float expiration = 0f;
        private float win_expiration = 0f;
        private bool is_dedicated_server = false;

        private List<ClientData> players = new List<ClientData>();            //Exclude observers, stays in array when disconnected, only players can send commands
        private List<ClientData> connected_clients = new List<ClientData>();  //Include obervers, removed from array when disconnected, all clients receive refreshes
        // private List<AIPlayer> aiList = new List<AIPlayer>();                //List of all AI players
        private Queue<QueuedGameAction> queuedActions = new Queue<QueuedGameAction>(); //List of action waiting to be processed

        private Dictionary<ushort, CommandEvent> registeredCommands = new Dictionary<ushort, CommandEvent>();

        public GameServer(string uid, int players, bool online)
        {
            Init(uid, players, online);
        }

        ~GameServer()
        {
            Clear();
        }

        protected virtual void Init(string uid, int players, bool online)
        {
            game_uid = uid;
            nb_players = Mathf.Max(players, 2);
            is_dedicated_server = online;
            gameData = new GameData(uid, nb_players);
            gameLogic = new GameLogic(gameData);

            //Commands
            RegisterAction(GameAction.PlayerSettings, ReceivePlayerSettings);
            RegisterAction(GameAction.PlayerSettingsAI, ReceivePlayerSettingsAI);
            RegisterAction(GameAction.GameSettings, ReceiveGameplaySettings);
            RegisterAction(GameAction.AssignTask, ReceivePlayCard);

            RegisterAction(GameAction.Resign, ReceiveResign);
            RegisterAction(GameAction.ChatMessage, ReceiveChat);

            //Events
            gameLogic.onGameStart += OnGameStart;
            gameLogic.onUpdateTurn += OnUpdateTurn;
            gameLogic.onGameEnd += OnGameEnd;

            // gameLogic.onEntityMoved += OnEntityMoved;
            // gameLogic.onCardPlayed += OnCardPlayed;

            // gameLogic.onRefresh += RefreshAll;

        }

        protected virtual void Clear()
        {
            gameLogic.onGameStart -= OnGameStart;
            gameLogic.onGameEnd -= OnGameEnd;

            // gameLogic.onEntityMoved -= OnEntityMoved;
            // gameLogic.onCardPlayed -= OnCardPlayed;

            // gameLogic.onRefresh -= RefreshAll;


        }

        public virtual void Update()
        {
            //Game Expiration if no one is connected or game ended
            int connected_players = CountConnectedClients();
            if (HasGameEnded() || connected_players == 0)
                expiration += Time.deltaTime;

            //Win expiration if all other players left
            if (connected_players == 1 && HasGameStarted() && !HasGameEnded())
                win_expiration += Time.deltaTime;

            if (is_dedicated_server && !HasGameEnded() && IsWinExpired())
                EndExpiredGame();

            //Timer during game
            if (gameData.state == GameState.Play && !gameLogic.IsResolving)
            {
                gameData.TurnTimer -= Time.deltaTime;
                if (gameData.TurnTimer <= 0f)
                {
                    //Time expired during turn
                    gameLogic.ExecuteTurn();
                    gameData.TurnTimer = gameData.TurnDuration;
                }
            }

            //Start Game when ready
            if (gameData.state == GameState.Connecting)
            {
                bool all_connected = gameData.AreAllPlayersConnected();
                bool all_ready = gameData.AreAllPlayersReady();
                if (all_connected && all_ready)
                {
                    StartGame();
                }
            }

            //Process queued actions
            if (queuedActions.Count > 0 && !gameLogic.IsResolving)
            {
                QueuedGameAction action = queuedActions.Dequeue();
                ExecuteAction(action.type, action.client, action.sdata);
            }

            //Update game logic
            // gameLogic.Update(Time.deltaTime);

            // //Update AI
            // foreach (AIPlayer ai in aiList)
            // {
            //     ai.Update();
            // }
        }

        protected virtual void StartGame()
        {
            // //Setup AI
            // bool ai_vs_ai = !is_dedicated_server && GameplayData.Get().ai_vs_ai;
            // foreach (Player player in gameData.players)
            // {
            //     if (player.is_ai || ai_vs_ai)
            //     {
            //         AIPlayer ai_gameplay = AIPlayer.Create(GameplayData.Get().ai_type, gameLogic, player.player_id, player.ai_level);
            //         aiList.Add(ai_gameplay);
            //     }
            // }

            //Start Game
            gameLogic.StartGame();
        }

        //End game when it has expired (only one player is still connected) 
        protected virtual void EndExpiredGame()
        {
            SLGameData gdata = gameLogic.GetGameData();
            foreach (PlayerData player in gdata.players)
            {
                if (player.IsConnected())
                {
                    gameLogic.EndGame(player.player_id);
                    return;
                }
            }
        }

        #region ------ Receive Actions -------

        private void RegisterAction(ushort tag, UnityAction<ClientData, SerializedData> callback)
        {
            CommandEvent commandEvent = new CommandEvent
            {
                tag = tag,
                callback = callback
            };
            registeredCommands.Add(tag, commandEvent);
        }

        public void ReceiveAction(ulong client_id, FastBufferReader reader)
        {
            ClientData client = GetClient(client_id);
            if (client != null)
            {
                reader.ReadValueSafe(out ushort type);
                SerializedData sdata = new SerializedData(reader);
                if (!gameLogic.IsResolving)
                {
                    //Not resolving, execute now
                    ExecuteAction(type, client, sdata);
                }
                else
                {
                    //Resolving, wait before executing
                    QueuedGameAction action = new QueuedGameAction
                    {
                        type = type,
                        client = client,
                        sdata = sdata
                    };

                    sdata.PreRead();
                    queuedActions.Enqueue(action);
                }
            }
        }

        public void ExecuteAction(ushort type, ClientData client, SerializedData sdata)
        {
            bool found = registeredCommands.TryGetValue(type, out CommandEvent command);
            if (found)
                command.callback.Invoke(client, sdata);
        }

        #endregion

        #region Receive
        public void ReceivePlayCard(ClientData iclient, SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null && !gameLogic.IsResolving)
            {
                if (msg.playerId != player.playerId)
                {
                    Debug.LogWarning($"Client attempted to act as another player. Expected player_id {player.playerId}, but got {msg.playerId}.");
                    return;
                }

                Card card = player.GetCard(msg.cardId);
                if (card != null && card.player_id == player.playerId)
                {
                    if (msg.athleteIds != null && msg.athleteIds.Length > 0)
                    {
                        // All athletes were successfully retrieved
                        gameLogic.PlayCard(player, card, msg.targetPosition, msg.athleteIds);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to retrieve all athletes for player {player.playerId}.");
                        // Handle the error (e.g., send an error message to the client)
                    }
                }
            }
        }

        public void ReceivePlayerSettings(ClientData iclient, SerializedData sdata)
        {
            PlayerSettings msg = sdata.Get<PlayerSettings>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                SetPlayerSettings(player.playerId, msg);
            }
        }

        public void ReceivePlayerSettingsAI(ClientData iclient, SerializedData sdata)
        {
            PlayerSettings msg = sdata.Get<PlayerSettings>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                SetPlayerSettingsAI(player.playerId, msg);
            }
        }

        public void ReceiveGameplaySettings(ClientData iclient, SerializedData sdata)
        {
            GameSettings settings = sdata.Get<GameSettings>();
            if (settings != null)
            {
                SetGameSettings(settings);
            }
        }

        // public void ReceivePlayCard(ClientData iclient, SerializedData sdata)
        // {
        //     MsgPlayCard msg = sdata.Get<MsgPlayCard>();
        //     Player player = GetPlayer(iclient);
        //     if (player != null && msg != null && game_data.IsPlayerActionTurn(player) && !gameplay.IsResolving())
        //     {
        //         Card card = player.GetCard(msg.card_uid);
        //         if (card != null && card.player_id == player.player_id)
        //             gameplay.PlayCard(card, msg.slot);
        //     }
        // }

        // public void ReceiveAttackTarget(ClientData iclient, SerializedData sdata)
        // {
        //     MsgAttack msg = sdata.Get<MsgAttack>();
        //     Player player = GetPlayer(iclient);
        //     if (player != null && msg != null && gameData.IsPlayerActionTurn(player) && !gameLogic.IsResolving())
        //     {
        //         Card attacker = player.GetCard(msg.attacker_uid);
        //         Card target = gameData.GetCard(msg.target_uid);
        //         if (attacker != null && target != null && attacker.player_id == player.player_id)
        //         {
        //             gameLogic.AttackTarget(attacker, target);
        //         }
        //     }
        // }


        // public void ReceiveSelectCard(ClientData iclient, SerializedData sdata)
        // {
        //     MsgCard msg = sdata.Get<MsgCard>();
        //     Player player = GetPlayer(iclient);
        //     if (player != null && msg != null && gameData.IsPlayerSelectorTurn(player) && !gameLogic.IsResolving())
        //     {
        //         Card target = gameData.GetCard(msg.card_uid);
        //         gameLogic.SelectCard(target);
        //     }
        // }

        // public void ReceiveSelectPlayer(ClientData iclient, SerializedData sdata)
        // {
        //     MsgPlayer msg = sdata.Get<MsgPlayer>();
        //     Player player = GetPlayer(iclient);
        //     if (player != null && msg != null && gameData.IsPlayerSelectorTurn(player) && !gameLogic.IsResolving())
        //     {
        //         Player target = gameData.GetPlayer(msg.player_id);
        //         gameLogic.SelectPlayer(target);
        //     }
        // }

        // public void ReceiveSelectChoice(ClientData iclient, SerializedData sdata)
        // {
        //     MsgInt msg = sdata.Get<MsgInt>();
        //     Player player = GetPlayer(iclient);
        //     if (player != null && msg != null && gameData.IsPlayerSelectorTurn(player) && !gameLogic.IsResolving())
        //     {
        //         gameLogic.SelectChoice(msg.value);
        //     }
        // }

        // public void ReceiveSelectCost(ClientData iclient, SerializedData sdata)
        // {
        //     MsgInt msg = sdata.Get<MsgInt>();
        //     Player player = GetPlayer(iclient);
        //     if (player != null && msg != null && gameData.IsPlayerSelectorTurn(player) && !gameLogic.IsResolving())
        //     {
        //         gameLogic.SelectCost(msg.value);
        //     }
        // }

        // public void ReceiveCancelSelection(ClientData iclient, SerializedData sdata)
        // {
        //     Player player = GetPlayer(iclient);
        //     if (player != null && gameData.IsPlayerSelectorTurn(player) && !gameLogic.IsResolving())
        //     {
        //         gameLogic.CancelSelection();
        //     }
        // }

        public void ReceiveResign(ClientData iclient, SerializedData sdata)
        {
            Player player = GetPlayer(iclient);
            if (player != null && gameData.state != GameState.Connecting && gameData.state != GameState.GameEnded)
            {
                gameLogic.EndGame(player.playerId);
            }
        }

        public void ReceiveChat(ClientData iclient, SerializedData sdata)
        {
            MsgChat msg = sdata.Get<MsgChat>();
            Player player = GetPlayer(iclient);
            if (player != null && msg != null)
            {
                msg.player_id = player.playerId; //Force player id to sending client to avoid spoofing
                SendToAll(GameAction.ChatMessage, msg, NetworkDelivery.Reliable);
            }
        }

        #endregion

        #region  Setup Commands 

        // public virtual async void SetPlayerDeck(int player_id, string username, UserDeckData deck)
        // {
        //     Player player = gameData.GetPlayer(player_id);
        //     if (player != null && gameData.state == GameState.Connecting)
        //     {
        //         UserData user = Authenticator.Get().UserData; //Offline game, get local user

        //         if (Authenticator.Get().IsApi())
        //             user = await ApiClient.Get().LoadUserData(username); //Online game, validate from api

        //         //Use user API deck
        //         UserDeckData udeck = user?.GetDeck(deck.tid);
        //         if (user != null && udeck != null)
        //         {
        //             if (user.IsDeckValid(udeck))
        //             {
        //                 gameLogic.SetPlayerDeck(player, udeck);
        //                 SendPlayerReady(player);
        //                 return;
        //             }
        //             else
        //             {
        //                 Debug.Log(user.username + " deck is invalid: " + udeck.title);
        //                 return;
        //             }
        //         }

        //         //Use premade deck
        //         DeckData cdeck = DeckData.Get(deck.tid);
        //         if (cdeck != null)
        //             gameLogic.SetPlayerDeck(player, cdeck);

        //         //Trust client in test mode
        //         else if (Authenticator.Get().IsTest())
        //             gameLogic.SetPlayerDeck(player, deck);

        //         //Deck not found
        //         else
        //             Debug.Log("Player " + player_id + " deck not found: " + deck.tid);

        //         SendPlayerReady(player);
        //     }
        // }

        public virtual void SetPlayerSettings(int player_id, PlayerSettings psettings)
        {
            // if (gameData.state != GameState.Connecting)
            //     return; //Cant send setting if game already started

            // Player player = gameData.GetPlayer(player_id);
            // if (player != null && !player.ready)
            // {
            //     player.avatar = psettings.avatar;
            //     player.cardback = psettings.cardback;
            //     player.is_ai = false;
            //     player.ready = true;
            //     SetPlayerDeck(player_id, player.username, psettings.deck);
            //     RefreshAll();
            // }
        }

        public virtual void SetPlayerSettingsAI(int player_id, PlayerSettings psettings)
        {
            // if (gameData.state != GameState.Connecting)
            //     return; //Cant send setting if game already started
            // if (is_dedicated_server)
            //     return; //No AI allowed online server

            // Player player = gameData.GetOpponentPlayer(player_id);
            // if (player != null && !player.ready)
            // {
            //     player.username = psettings.username;
            //     player.avatar = psettings.avatar;
            //     player.cardback = psettings.cardback;
            //     player.is_ai = true;
            //     player.ready = true;
            //     player.ai_level = psettings.ai_level;

            //     SetPlayerDeck(player.player_id, player.username, psettings.deck);
            //     RefreshAll();
            // }
        }

        public virtual void SetGameSettings(GameSettings settings)
        {
            if (gameData.state == GameState.Connecting)
            {
                gameData.settings = settings;
                RefreshAll();
            }
        }

        #endregion

        #region Client

        public void AddClient(ClientData client)
        {
            if (!connected_clients.Contains(client))
                connected_clients.Add(client);
        }

        public void RemoveClient(ClientData client)
        {
            connected_clients.Remove(client);

            Player player = GetPlayer(client);
            if (player != null && player.connected)
            {
                player.connected = false;
                RefreshAll();
            }
        }

        public ClientData GetClient(ulong client_id)
        {
            foreach (ClientData client in connected_clients)
            {
                if (client.client_id == client_id)
                    return client;
            }
            return null;
        }

        public int AddPlayer(ClientData client)
        {
            if (!players.Contains(client))
                players.Add(client);

            int player_id = FindPlayerID(client.user_id);
            Player player = gameData.GetPlayer(player_id);
            if (player != null)
            {
                player.username = client.username;
                player.connected = true;
            }

            return player_id;
        }

        public int FindPlayerID(string user_id)
        {
            int index = 0;
            foreach (ClientData player in players)
            {
                if (player.user_id == user_id)
                    return index;
                index++;
            }
            return -1;
        }

        public Player GetPlayer(ClientData client)
        {
            return GetPlayer(client.user_id);
        }

        public Player GetPlayer(string user_id)
        {
            int player_id = FindPlayerID(user_id);
            return gameData?.GetPlayer(player_id);
        }

        public bool IsPlayer(string user_id)
        {
            Player player = GetPlayer(user_id);
            return player != null;
        }

        public bool IsConnectedPlayer(string user_id)
        {
            Player player = GetPlayer(user_id);
            return player != null && player.connected;
        }

        public int CountPlayers()
        {
            return players.Count;
        }

        public int CountConnectedClients()
        {
            int nb = 0;
            foreach (ClientData client in connected_clients)
            {
                if (client != null && client.IsConnected())
                {
                    nb++;
                }
            }
            return nb;
        }

        #endregion

        #region Check

        public SLGameData GetGameData()
        {
            return gameLogic.GetGameData();
        }

        public virtual bool HasGameStarted()
        {
            return gameLogic.IsGameStarted;
        }

        public virtual bool HasGameEnded()
        {
            return gameLogic.IsGameEnded;
        }

        public virtual bool IsGameExpired()
        {
            return expiration > game_expire_time; //Means that the game expired (everyone left or game ended)
        }

        public virtual bool IsWinExpired()
        {
            return win_expiration > win_expire_time; //Means that only one player is left, and he should win
        }

        #endregion

        #region On (Trigger send message to clients)

        protected virtual void OnGameStart()
        {
            SendToAll(GameAction.GameStart);

            if (is_dedicated_server && Authenticator.Get().IsApi())
            {
                //Create Match
                ApiClient.Get().CreateMatch(gameData);
            }
        }

        protected virtual void OnUpdateTurn(UpdateMessage updateMsg)
        {
            SendToAll(GameAction.UpdateMessage, updateMsg, NetworkDelivery.Reliable);
        }

        protected virtual void OnGameEnd(Player winner)
        {
            MsgPlayer msg = new MsgPlayer();
            msg.player_id = winner != null ? winner.playerId : -1;
            SendToAll(GameAction.GameEnd, msg, NetworkDelivery.Reliable);

            if (is_dedicated_server && Authenticator.Get().IsApi())
            {
                //End Match and give rewards
                ApiClient.Get().EndMatch(gameData, winner.playerId);
            }
        }

        protected virtual void OnCardPlayed(Card card)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.cardId = card.uid;
            SendToAll(GameAction.CardPlayed, mdata, NetworkDelivery.Reliable);
        }

        #endregion

        #region Send

        protected virtual void SendPlayerReady(Player player)
        {
            if (player != null && player.IsReady())
            {
                MsgInt mdata = new MsgInt();
                mdata.value = player.playerId;
                SendToAll(GameAction.PlayerReady, mdata, NetworkDelivery.Reliable);
            }
        }

        public void SendToClient(ulong clientId, ushort tag, INetworkSerializable data, NetworkDelivery delivery)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            writer.WriteNetworkSerializable(data);

            Messaging.Send("refresh", clientId, writer, delivery);

            writer.Dispose();
        }

        public void SendToAll(ushort tag)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            foreach (ClientData iclient in connected_clients)
            {
                if (iclient != null)
                {
                    Messaging.Send("refresh", iclient.client_id, writer, NetworkDelivery.Reliable);
                }
            }
            writer.Dispose();
        }

        public void SendToAll(ushort tag, INetworkSerializable data, NetworkDelivery delivery)
        {
            FastBufferWriter writer = new FastBufferWriter(1024, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(tag);
            writer.WriteNetworkSerializable(data);
            foreach (ClientData iclient in connected_clients)
            {
                if (iclient != null)
                {
                    Messaging.Send("refresh", iclient.client_id, writer, delivery);
                }
            }
            writer.Dispose();
        }

        public virtual void RefreshAll()
        {
            SLGameData sldata = SLGameData.FromGameData(gameData);
            // MsgGameData mdata = new MsgGameData { gameData = sldata };

            SendToAll(GameAction.RefreshAll, sldata, NetworkDelivery.ReliableFragmentedSequenced);
        }

        #endregion

        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }
    }

    public struct QueuedGameAction
    {
        public ushort type;
        public ClientData client;
        public SerializedData sdata;
    }
}
