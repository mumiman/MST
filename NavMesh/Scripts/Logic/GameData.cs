using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Unity.Netcode;

namespace CCengine
{
    [System.Serializable]
    public enum GameState
    {
        Connecting = 0, //Players are not connected
        Play = 20,      //Game is being played
        GameEnded = 99,
    }

    [System.Serializable]
    public enum GamePhase
    {
        None = 0,
        StartTurn = 10, //Start of turn resolution
        Main = 20,      //Main play phase
        EndTurn = 30,   //End of turn resolutions
    }

    [System.Serializable]
    public class GameData
    {
        // Unique identifier for the game session
        public string game_uid;
        // Game settings
        public GameSettings settings;

        // Game state and phase
        public GameState state = GameState.Connecting;
        public GamePhase phase = GamePhase.None;
        public float TurnDuration = 0.5f;
        public float TurnTimer = 0f;

        // Players
        public Player[] players;
        private static int athleteIdCounter = 0;

        // public LanePaths LanePaths { get; private set; }
        public int CurrentKeep;
        public int CurrentTurn;

        // Private collections
        private Dictionary<string, Athlete> teamAAthletes;
        private Dictionary<string, Athlete> teamBAthletes;
        private Dictionary<string, Athlete> allAthletes;

        private Dictionary<string, Hero> teamAHeroes;
        private Dictionary<string, Hero> teamBHeroes;
        private Dictionary<string, Hero> allHeroes;

        private Dictionary<string, Tower> teamATowers;
        private Dictionary<string, Tower> teamBTowers;
        private Dictionary<string, Tower> allTowers;

        private Dictionary<Lane, List<CreepWave>> teamACreepWaves;
        private Dictionary<Lane, List<CreepWave>> teamBCreepWaves;
        private Dictionary<string, CreepWave> allCreepWaves;

        // Public read-only properties
        public IReadOnlyDictionary<string, Athlete> TeamAAthletes => teamAAthletes;
        public IReadOnlyDictionary<string, Athlete> TeamBAthletes => teamBAthletes;
        public IReadOnlyDictionary<string, Athlete> AllAthletes => allAthletes;

        public IReadOnlyDictionary<string, Hero> TeamAHeroes => teamAHeroes;
        public IReadOnlyDictionary<string, Hero> TeamBHeroes => teamBHeroes;
        public IReadOnlyDictionary<string, Hero> AllHeroes => allHeroes;

        public IReadOnlyDictionary<string, Tower> TeamATowers => teamATowers;
        public IReadOnlyDictionary<string, Tower> TeamBTowers => teamBTowers;
        public IReadOnlyDictionary<string, Tower> AllTowers => allTowers;

        public IReadOnlyDictionary<Lane, IReadOnlyCollection<CreepWave>> TeamACreepWaves =>
            teamACreepWaves.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyCollection<CreepWave>)kvp.Value.AsReadOnly());

        public IReadOnlyDictionary<Lane, IReadOnlyCollection<CreepWave>> TeamBCreepWaves =>
            teamBCreepWaves.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyCollection<CreepWave>)kvp.Value.AsReadOnly());

        public IReadOnlyDictionary<string, CreepWave> AllCreepWaves => allCreepWaves;

        public const float MAX_ATTACK_RANGE = 2.0f; // Adjust based on your game's maximum attack range
        public List<Entity> teamAEntities { get; private set; }
        public List<Entity> teamBEntities { get; private set; }

        public GameData(string uid, int nb_players)
        {
            Debug.Log("GameData constructor");
            this.game_uid = uid;
            players = new Player[nb_players];
            for (int i = 0; i < nb_players; i++)
                players[i] = new Player(i);
                
            settings = GameSettings.Default;

            teamAAthletes = new Dictionary<string, Athlete>();
            teamBAthletes = new Dictionary<string, Athlete>();
            allAthletes = new Dictionary<string, Athlete>();

            teamAHeroes = new Dictionary<string, Hero>();
            teamBHeroes = new Dictionary<string, Hero>();
            allHeroes = new Dictionary<string, Hero>();

            teamATowers = new Dictionary<string, Tower>();
            teamBTowers = new Dictionary<string, Tower>();
            allTowers = new Dictionary<string, Tower>();

            teamACreepWaves = new Dictionary<Lane, List<CreepWave>>();
            teamBCreepWaves = new Dictionary<Lane, List<CreepWave>>();

            foreach (Lane lane in Enum.GetValues(typeof(Lane)))
            {
                if (lane == Lane.None) continue;

                teamACreepWaves[lane] = new List<CreepWave>();
                teamBCreepWaves[lane] = new List<CreepWave>();
            }

            allCreepWaves = new Dictionary<string, CreepWave>();

            teamAEntities = new List<Entity>();
            teamBEntities = new List<Entity>();

            CurrentKeep = 0; // keep index
            CurrentTurn = 1;

            InitializeAthletes();
            InitializeTowers();
            CreateHeroesToPlayers();

            AddCreepWaves();
        }

        #region Game State Checks

        public bool AreAllPlayersReady()
        {
            int readyCount = 0;
            foreach (Player player in players)
            {
                if (player.IsReady())
                    readyCount++;
            }
            return readyCount >= settings.nb_players;
        }

        public bool AreAllPlayersConnected()
        {
            int connectedCount = 0;
            foreach (Player player in players)
            {
                if (player.IsConnected())
                    connectedCount++;
            }
            return connectedCount >= settings.nb_players;
        }

        public bool HasStarted()
        {
            return state != GameState.Connecting;
        }

        public bool HasEnded()
        {
            return state == GameState.GameEnded;
        }

        #endregion

        #region Player

        public Player GetPlayer(int id)
        {
            if (id >= 0 && id < players.Length)
                return players[id];
            return null;
        }

        #endregion

        #region Athlete

        public void InitializeAthletes()
        {
            Debug.Log("InitializeAthletes");
            // Initialize Team A players
            CreateAthlete("Alice", 25, PlayerRole.Carry, Team.TeamA);
            CreateAthlete("Bob", 22, PlayerRole.Mid, Team.TeamA);
            CreateAthlete("Charlie", 27, PlayerRole.Offlane, Team.TeamA);
            CreateAthlete("Diana", 24, PlayerRole.Sup1, Team.TeamA);
            CreateAthlete("Ethan", 23, PlayerRole.Sup2, Team.TeamA);

            // Initialize Team B players
            CreateAthlete("Fiona", 26, PlayerRole.Carry, Team.TeamB);
            CreateAthlete("George", 21, PlayerRole.Mid, Team.TeamB);
            CreateAthlete("Hannah", 28, PlayerRole.Offlane, Team.TeamB);
            CreateAthlete("Ian", 24, PlayerRole.Sup1, Team.TeamB);
            CreateAthlete("Jack", 22, PlayerRole.Sup2, Team.TeamB);
        }

        private void CreateAthlete(string name, int age, PlayerRole role, Team team)
        {
            // Preferred roles can include the assigned role
            var preferredRoles = new HashSet<PlayerRole> { role };

            // Initialize hero mastery with default values (empty for this example)
            var heroMastery = new Dictionary<string, int>();

            // Randomly generate player attributes within specified ranges
            string id = "athlete" + athleteIdCounter++;
            float aggressive = UnityEngine.Random.Range(-1.0f, 1.0f); // Aggressiveness ranges from -1.0 to 1.0
            float reflex = UnityEngine.Random.Range(0.0f, 1.0f);
            float skill = UnityEngine.Random.Range(0.0f, 1.0f);
            float decision = UnityEngine.Random.Range(0.0f, 1.0f);
            float teamwork = UnityEngine.Random.Range(0.0f, 1.0f);
            float joker = UnityEngine.Random.Range(0.0f, 1.0f);

            // Create the player instance
            Athlete Athlete = new Athlete(id, name, age, preferredRoles, heroMastery,
                aggressive, reflex, skill, decision, teamwork, joker);

            // Assign the role to the player
            Athlete.AssignRole(role);

            AddAthleteToTeam(Athlete, team); ;
        }

        // Adds a Athlete to the specified team
        public void AddAthleteToTeam(Athlete athlete, Team team)
        {
            if (athlete == null)
                throw new ArgumentNullException(nameof(athlete));

            if (athlete.Team != Team.None)
                throw new InvalidOperationException("Player is already assigned to a team.");

            if (allAthletes.ContainsKey(athlete.Id))
                throw new InvalidOperationException("Player already exists in the game.");

            switch (team)
            {
                case Team.TeamA:
                    teamAAthletes.Add(athlete.Id, athlete);
                    break;
                case Team.TeamB:
                    teamBAthletes.Add(athlete.Id, athlete);
                    break;
                default:
                    throw new ArgumentException("Invalid team specified.", nameof(team));
            }

            allAthletes.Add(athlete.Id, athlete);
            athlete.AssignTeam(team);
        }

        // Removes a player from their assigned team
        public void RemoveAthleteFromTeam(Athlete athlete)
        {
            if (athlete == null)
                throw new ArgumentNullException(nameof(athlete));

            switch (athlete.Team)
            {
                case Team.TeamA:
                    if (!teamAAthletes.Remove(athlete.Id))
                        throw new InvalidOperationException("Player not found in Team A.");
                    break;
                case Team.TeamB:
                    if (!teamBAthletes.Remove(athlete.Id))
                        throw new InvalidOperationException("Player not found in Team B.");
                    break;
                default:
                    throw new InvalidOperationException("Player is not assigned to any team.");
            }

            if (!allAthletes.Remove(athlete.Id))
                throw new InvalidOperationException("Player not found in the game.");

            athlete.AssignTeam(Team.None);
        }

        /// <summary>
        /// Gets all players from both Team A and Team B.
        /// </summary>
        /// <returns>A read-only collection of all players in the game.</returns>
        public IReadOnlyCollection<Athlete> GetAllPlayers()
        {
            return allAthletes.Values;
        }

        public Athlete GetAthlete(string atheleteId)
        {
            allAthletes.TryGetValue(atheleteId, out Athlete athelete);
            return athelete;
        }

        #endregion

        #region Hero
        /// <summary>
        /// Gets all heroes from both teams.
        /// </summary>
        /// <returns>A read-only collection of all heroes in the game.</returns>
        public IReadOnlyCollection<Hero> GetAllHeroes()
        {
            return allHeroes.Values;
        }

        /// <summary>
        /// Gets heroes from a team.
        /// </summary>
        /// <param name="team">The team to retrieve heroes from.</param>
        /// <returns>A read-only collection of heroes in the specified team.</returns>
        public IReadOnlyCollection<Hero> GetTeamHeroes(Team team)
        {
            return team switch
            {
                Team.TeamA => teamAHeroes.Values,
                Team.TeamB => teamBHeroes.Values,
                _ => new List<Hero>(), // Return an empty collection for invalid team
            };
        }

        public void AddHero(Hero hero)
        {
            if (hero == null)
                throw new ArgumentNullException(nameof(hero));

            if (allHeroes.ContainsKey(hero.Id))
                throw new InvalidOperationException("Hero already exists in the game.");

            switch (hero.Team)
            {
                case Team.TeamA:
                    teamAHeroes.Add(hero.Id, hero);
                    break;
                case Team.TeamB:
                    teamBHeroes.Add(hero.Id, hero);
                    break;
                default:
                    throw new ArgumentException("Hero team is invalid.", nameof(hero));
            }

            allHeroes.Add(hero.Id, hero);
        }

        public void RemoveHero(Hero hero)
        {
            if (hero == null)
                throw new ArgumentNullException(nameof(hero));

            bool removed = false;
            switch (hero.Team)
            {
                case Team.TeamA:
                    removed = teamAHeroes.Remove(hero.Id);
                    break;
                case Team.TeamB:
                    removed = teamBHeroes.Remove(hero.Id);
                    break;
                default:
                    throw new ArgumentException("Hero team is invalid.", nameof(hero));
            }

            if (removed)
            {
                allHeroes.Remove(hero.Id);
            }
            else
            {
                throw new InvalidOperationException("Hero not found in the specified team.");
            }
        }

        /// <summary>
        /// Retrieves a hero by their unique ID.
        /// </summary>
        /// <param name="heroId">The ID of the hero to retrieve.</param>
        /// <returns>The hero with the specified ID, or null if not found.</returns>
        public Hero GetHeroById(string heroId)
        {
            allHeroes.TryGetValue(heroId, out Hero hero);
            return hero;
        }

        // Method to assign heroes to players
        public void CreateHeroesToPlayers()
        {
            CreateHeroesToTeam(TeamAAthletes);
            CreateHeroesToTeam(TeamBAthletes);
        }

        private void CreateHeroesToTeam(IReadOnlyDictionary<string, Athlete> teamPlayers)
        {
            foreach (Athlete player in teamPlayers.Values)
            {
                Hero hero = CreateHeroForPlayer(player);
                AddHero(hero);
                player.AssignHero(hero);
            }
        }

        // Method to create a hero for a player based on their role
        private Hero CreateHeroForPlayer(Athlete player)
        {
            int maxHealth = 500;
            int damage = 40;
            int mana = 200;

            // Determine hero name and attributes based on player role
            string heroName = GetHeroNameForRole(player);
            Team team = player.Team;

            // Create the hero instance
            SLVector3 startingPosition = team == Team.TeamA ? new SLVector3(0, 0) : new SLVector3(3, 6);

            return new Hero(heroName, team, startingPosition, maxHealth, damage, mana);
        }

        // Method to get a hero name for a given role (placeholder implementation)
        private string GetHeroNameForRole(Athlete player)
        {
            string teamName = player.Team.ToString();

            switch (player.Role)
            {
                case PlayerRole.Carry:
                    return "CarryHero_" + teamName;
                case PlayerRole.Mid:
                    return "MidHero_" + teamName;
                case PlayerRole.Offlane:
                    return "OfflaneHero_" + teamName;
                case PlayerRole.Sup1:
                    return "SupportHero1_" + teamName;
                case PlayerRole.Sup2:
                    return "SupportHero2_" + teamName;
                default:
                    return "DefaultHero_" + teamName;
            }
        }
        #endregion

        #region Creep Waves
        // Get creep waves for a team and lane
        public IReadOnlyCollection<CreepWave> GetCreepWaves(Team team, Lane lane)
        {
            if (team == Team.TeamA)
            {
                if (teamACreepWaves.TryGetValue(lane, out List<CreepWave> creepWaves))
                    return creepWaves;
            }
            else if (team == Team.TeamB)
            {
                if (teamBCreepWaves.TryGetValue(lane, out List<CreepWave> creepWaves))
                    return creepWaves;
            }
            return new List<CreepWave>(); // Return an empty collection if not found
        }

        // Adds creep waves to the game data
        public List<CreepWave> AddCreepWaves()
        {
            List<CreepWave> newlyAddedCreepWaves = new List<CreepWave>();

            int creepHP = 100;
            int creepATK = 10;

            // Adding creep waves to Team A and Team B for each lane
            // Team A
            newlyAddedCreepWaves.Add(AddCreepWaveForTeam(Team.TeamA, Lane.Top, creepHP, creepATK, LanePaths.TopLanePathA));
            newlyAddedCreepWaves.Add(AddCreepWaveForTeam(Team.TeamA, Lane.Mid, creepHP, creepATK, LanePaths.MidLanePathA));
            newlyAddedCreepWaves.Add(AddCreepWaveForTeam(Team.TeamA, Lane.Bottom, creepHP, creepATK, LanePaths.BottomLanePathA));

            // Team B
            newlyAddedCreepWaves.Add(AddCreepWaveForTeam(Team.TeamB, Lane.Top, creepHP, creepATK, LanePaths.TopLanePathB));
            newlyAddedCreepWaves.Add(AddCreepWaveForTeam(Team.TeamB, Lane.Mid, creepHP, creepATK, LanePaths.MidLanePathB));
            newlyAddedCreepWaves.Add(AddCreepWaveForTeam(Team.TeamB, Lane.Bottom, creepHP, creepATK, LanePaths.BottomLanePathB));

            Debug.Log("InitializeCreepWaves of wave: " + CurrentKeep);

            return newlyAddedCreepWaves;
        }

        // Helper method to add a creep wave for a team and lane
        private CreepWave AddCreepWaveForTeam(Team team, Lane lane, int hp, int atk, SLVector3[] lanePath)
        {
            string teamIdentifier = team == Team.TeamA ? "A" : "B";
            string creepWaveName = $"{lane}LaneCreepWave_{teamIdentifier}{CurrentKeep}";

            CreepWave creepWave = new CreepWave(creepWaveName, creepWaveName, team, lane, hp, atk, lanePath);

            // Assign a unique ID to the creep wave (handled in CreepWave constructor if using Entity base class)

            // Add to the team's creep waves
            if (team == Team.TeamA)
            {
                teamACreepWaves[lane].Add(creepWave);
            }
            else
            {
                teamBCreepWaves[lane].Add(creepWave);
            }

            // Add to the global creep waves dictionary
            allCreepWaves.Add(creepWave.Id, creepWave);

            return creepWave;
        }

        // Removes a creep wave from the game data
        public void RemoveCreepWave(CreepWave creepWave)
        {
            if (creepWave == null)
                throw new ArgumentNullException(nameof(creepWave));

            bool removed = false;

            if (creepWave.Team == Team.TeamA)
            {
                removed = teamACreepWaves[creepWave.Lane].Remove(creepWave);
            }
            else if (creepWave.Team == Team.TeamB)
            {
                removed = teamBCreepWaves[creepWave.Lane].Remove(creepWave);
            }

            if (removed)
            {
                // Remove from the global creep waves dictionary
                if (allCreepWaves.Remove(creepWave.Id))
                    Debug.Log($"CreepWave {creepWave.Name} has been removed from the game.");

            }
            else
            {
                Debug.LogError($"CreepWave {creepWave.Name} not found in team's {creepWave.Lane} lane.");
            }
        }

        /// <summary>
        /// Retrieves a creep wave by its unique ID.
        /// </summary>
        /// <param name="creepWaveId">The ID of the creep wave to retrieve.</param>
        /// <returns>The creep wave with the specified ID, or null if not found.</returns>
        public CreepWave GetCreepWaveById(string creepWaveId)
        {
            allCreepWaves.TryGetValue(creepWaveId, out CreepWave creepWave);
            return creepWave;
        }

        #endregion

        #region Tower
        void InitializeTowers()
        {
            int tier1Health = 100;
            int tier2Health = 200;

            int tier1Dmg = 10;
            int tier2Dmg = 20;

            // Team A Tier 1 Towers
            CreateTowerData(-7.0f, 1.0f, Team.TeamA, tier1Health, tier1Dmg); // Tier 1 Tower
            CreateTowerData(-2.6f, -1.3f
            , Team.TeamA, tier1Health, tier1Dmg);  // Tier 1 Tower
            CreateTowerData(0.1f, -5.7f, Team.TeamA, tier1Health, tier1Dmg);  // Tier 1 Tower

            // Team A Tier 2 Towers
            CreateTowerData(-7.0f, -3.2f, Team.TeamA, tier2Health, tier2Dmg); // Tier 2 Tower
            CreateTowerData(-5.4f, -4.1f, Team.TeamA, tier2Health, tier2Dmg);  // Tier 2 Tower
            CreateTowerData(-5.2f, -5.7f, Team.TeamA, tier2Health, tier2Dmg);  // Tier 2 Tower

            // Team B Tier 1 Towers
            CreateTowerData(-0.3f, 7.9f, Team.TeamB, tier1Health, tier1Dmg);  // Tier 1 Tower
            CreateTowerData(2.4f, 4.2f, Team.TeamB, tier1Health, tier1Dmg);  // Tier 1 Tower
            CreateTowerData(6.6f, 1.7f, Team.TeamB, tier1Health, tier1Dmg);  // Tier 1 Tower

            // Team B Tier 2 Towers
            CreateTowerData(5.0f, 8.2f, Team.TeamB, tier2Health, tier2Dmg);  // Tier 2 Tower
            CreateTowerData(5.4f, 6.5f, Team.TeamB, tier2Health, tier2Dmg);  // Tier 2 Tower
            CreateTowerData(6.9f, 5.7f, Team.TeamB, tier2Health, tier2Dmg);  // Tier 2 Tower
        }

        void CreateTowerData(float x, float z, Team team, int health, int dmg)
        {
            Tower tower = new Tower($"Tower{team}_{x},{z}", team, new Vector3(x, 0, z), health, dmg);

            // Add to team-specific and global dictionaries
            switch (team)
            {
                case Team.TeamA:
                    teamATowers[tower.Id] = tower;
                    break;
                case Team.TeamB:
                    teamBTowers[tower.Id] = tower;
                    break;
            }

            allTowers[tower.Id] = tower;
        }

        #endregion

        public void NextTurn()
        {
            CurrentTurn += 1;
        }

        public void NextCreepWave()
        {
            CurrentKeep += 1;
        }

    }
}
