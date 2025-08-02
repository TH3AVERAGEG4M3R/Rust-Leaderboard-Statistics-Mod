using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace Oxide.Plugins
{
    [Info("Player Stats Leaderboard", "TH3AVERAGEG4M3R", "1.0.0")]
    [Description("Comprehensive player statistics tracking and leaderboard system")]
    public class PlayerStatsLeaderboard : RustPlugin
    {
        #region Configuration
        
        private Configuration config;
        
        public class Configuration
        {
            [JsonProperty("Enable Chat Commands")]
            public bool EnableChatCommands = true;
            
            [JsonProperty("Auto Save Interval (minutes)")]
            public int AutoSaveInterval = 5;
            
            [JsonProperty("Leaderboard Top Count")]
            public int LeaderboardTopCount = 10;
            
            [JsonProperty("Track PVP Only")]
            public bool TrackPVPOnly = false;
            
            [JsonProperty("Reset Stats on Wipe")]
            public bool ResetStatsOnWipe = false;
            
            [JsonProperty("Enable Distance Tracking")]
            public bool EnableDistanceTracking = true;
            
            [JsonProperty("Enable Farming Stats")]
            public bool EnableFarmingStats = true;
        }
        
        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            SaveConfig();
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }
        
        protected override void SaveConfig() => Config.WriteObject(config);
        
        #endregion
        
        #region Data Classes
        
        public class PlayerStats
        {
            public string PlayerId { get; set; }
            public string PlayerName { get; set; }
            public DateTime LastSeen { get; set; }
            public CombatStats Combat { get; set; } = new CombatStats();
            public FarmingStats Farming { get; set; } = new FarmingStats();
            public TravelStats Travel { get; set; } = new TravelStats();
            public ExplosiveStats Explosives { get; set; } = new ExplosiveStats();
        }
        
        public class CombatStats
        {
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Headshots { get; set; }
            public int OneShotKills { get; set; }
            public float FurthestKill { get; set; }
            public float KDRatio => Deaths > 0 ? (float)Kills / Deaths : Kills;
        }
        
        public class FarmingStats
        {
            public int Wood { get; set; }
            public int Stone { get; set; }
            public int MetalOre { get; set; }
            public int Sulfur { get; set; }
            public int HighQualityMetal { get; set; }
            public int TotalResourcesGathered => Wood + Stone + MetalOre + Sulfur + HighQualityMetal;
        }
        
        public class TravelStats
        {
            public float TotalDistance { get; set; }
            public Vector3 LastPosition { get; set; }
        }
        
        public class ExplosiveStats
        {
            public int RocketsUsed { get; set; }
            public int C4Used { get; set; }
            public int SatchelChargesUsed { get; set; }
            public int BeancanGrenadesUsed { get; set; }
        }
        
        #endregion
        
        #region Fields
        
        private Dictionary<string, PlayerStats> playerStats = new Dictionary<string, PlayerStats>();
        private Dictionary<string, Vector3> lastPositions = new Dictionary<string, Vector3>();
        private Timer saveTimer;
        private const string DataFileName = "PlayerStats";
        
        #endregion
        
        #region Hooks
        
        private void Init()
        {
            LoadData();
            
            // Register permissions
            permission.RegisterPermission("playerstats.admin", this);
            permission.RegisterPermission("playerstats.view", this);
            
            // Start auto-save timer
            if (config.AutoSaveInterval > 0)
            {
                saveTimer = timer.Every(config.AutoSaveInterval * 60f, SaveData);
            }
        }
        
        private void OnServerInitialized()
        {
            // Initialize player positions for distance tracking
            if (config.EnableDistanceTracking)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    lastPositions[player.UserIDString] = player.transform.position;
                }
            }
        }
        
        private void OnPlayerConnected(BasePlayer player)
        {
            var playerId = player.UserIDString;
            
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = new PlayerStats
                {
                    PlayerId = playerId,
                    PlayerName = player.displayName,
                    LastSeen = DateTime.Now
                };
            }
            else
            {
                playerStats[playerId].PlayerName = player.displayName;
                playerStats[playerId].LastSeen = DateTime.Now;
            }
            
            if (config.EnableDistanceTracking)
            {
                lastPositions[playerId] = player.transform.position;
            }
        }
        
        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (config.EnableDistanceTracking && lastPositions.ContainsKey(player.UserIDString))
            {
                lastPositions.Remove(player.UserIDString);
            }
        }
        
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer victim && info?.InitiatorPlayer != null)
            {
                var attacker = info.InitiatorPlayer;
                
                // Skip if PVP only is enabled and victim is NPC
                if (config.TrackPVPOnly && !victim.userID.IsSteamId())
                    return;
                
                var attackerId = attacker.UserIDString;
                var victimId = victim.UserIDString;
                
                // Ensure attacker stats exist
                EnsurePlayerStats(attackerId, attacker.displayName);
                
                // Record kill for attacker
                playerStats[attackerId].Combat.Kills++;
                
                // Check for headshot
                if (info.isHeadshot)
                {
                    playerStats[attackerId].Combat.Headshots++;
                }
                
                // Check for one-shot kill
                if (info.damageTypes.Total() >= victim.MaxHealth())
                {
                    playerStats[attackerId].Combat.OneShotKills++;
                }
                
                // Calculate distance
                var distance = Vector3.Distance(attacker.transform.position, victim.transform.position);
                if (distance > playerStats[attackerId].Combat.FurthestKill)
                {
                    playerStats[attackerId].Combat.FurthestKill = distance;
                }
                
                // Record death for victim (if player)
                if (victim.userID.IsSteamId())
                {
                    EnsurePlayerStats(victimId, victim.displayName);
                    playerStats[victimId].Combat.Deaths++;
                }
            }
        }
        
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!config.EnableFarmingStats) return;
            
            var player = entity as BasePlayer;
            if (player == null) return;
            
            var playerId = player.UserIDString;
            EnsurePlayerStats(playerId, player.displayName);
            
            var stats = playerStats[playerId].Farming;
            
            switch (item.info.shortname)
            {
                case "wood":
                    stats.Wood += item.amount;
                    break;
                case "stones":
                    stats.Stone += item.amount;
                    break;
                case "metal.ore":
                    stats.MetalOre += item.amount;
                    break;
                case "sulfur.ore":
                    stats.Sulfur += item.amount;
                    break;
                case "hq.metal.ore":
                    stats.HighQualityMetal += item.amount;
                    break;
            }
        }
        
        private void OnPlayerTick(BasePlayer player)
        {
            if (!config.EnableDistanceTracking) return;
            
            var playerId = player.UserIDString;
            
            if (lastPositions.ContainsKey(playerId))
            {
                var currentPos = player.transform.position;
                var lastPos = lastPositions[playerId];
                var distance = Vector3.Distance(currentPos, lastPos);
                
                if (distance > 1f) // Only count significant movement
                {
                    EnsurePlayerStats(playerId, player.displayName);
                    playerStats[playerId].Travel.TotalDistance += distance;
                    lastPositions[playerId] = currentPos;
                }
            }
            else
            {
                lastPositions[playerId] = player.transform.position;
            }
        }
        
        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player == null) return;
            
            var playerId = player.UserIDString;
            EnsurePlayerStats(playerId, player.displayName);
            playerStats[playerId].Explosives.RocketsUsed++;
        }
        
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null) return;
            
            var playerId = player.UserIDString;
            EnsurePlayerStats(playerId, player.displayName);
            
            var explosiveStats = playerStats[playerId].Explosives;
            
            switch (entity.ShortPrefabName)
            {
                case "grenade.c4.deployed":
                    explosiveStats.C4Used++;
                    break;
                case "grenade.satchel.deployed":
                    explosiveStats.SatchelChargesUsed++;
                    break;
                case "grenade.beancan.deployed":
                    explosiveStats.BeancanGrenadesUsed++;
                    break;
            }
        }
        
        #endregion
        
        #region Commands
        
        [ChatCommand("stats")]
        private void StatsCommand(BasePlayer player, string command, string[] args)
        {
            if (!config.EnableChatCommands) return;
            
            if (!permission.UserHasPermission(player.UserIDString, "playerstats.view"))
            {
                SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }
            
            if (args.Length > 0)
            {
                // Show stats for specific player (admin only)
                if (!permission.UserHasPermission(player.UserIDString, "playerstats.admin"))
                {
                    SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;
                }
                
                var targetPlayer = BasePlayer.Find(args[0]);
                if (targetPlayer != null)
                {
                    ShowPlayerStats(player, targetPlayer.UserIDString);
                }
                else
                {
                    SendReply(player, lang.GetMessage("PlayerNotFound", this, player.UserIDString));
                }
            }
            else
            {
                ShowPlayerStats(player, player.UserIDString);
            }
        }
        
        [ChatCommand("leaderboard")]
        private void LeaderboardCommand(BasePlayer player, string command, string[] args)
        {
            if (!config.EnableChatCommands) return;
            
            if (!permission.UserHasPermission(player.UserIDString, "playerstats.view"))
            {
                SendReply(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }
            
            var category = args.Length > 0 ? args[0].ToLower() : "kills";
            ShowLeaderboard(player, category);
        }
        
        [ConsoleCommand("playerstats.reset")]
        private void ResetStatsConsole(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !permission.UserHasPermission(arg.Player().UserIDString, "playerstats.admin"))
            {
                arg.ReplyWith(lang.GetMessage("NoPermission", this));
                return;
            }
            
            if (arg.Args.Length > 0)
            {
                var playerId = arg.Args[0];
                if (playerStats.ContainsKey(playerId))
                {
                    playerStats[playerId] = new PlayerStats
                    {
                        PlayerId = playerId,
                        PlayerName = playerStats[playerId].PlayerName,
                        LastSeen = DateTime.Now
                    };
                    arg.ReplyWith($"Reset stats for player {playerId}");
                    SaveData();
                }
                else
                {
                    arg.ReplyWith("Player not found");
                }
            }
            else
            {
                playerStats.Clear();
                arg.ReplyWith("Reset all player stats");
                SaveData();
            }
        }
        
        [ConsoleCommand("playerstats.save")]
        private void SaveStatsConsole(ConsoleSystem.Arg arg)
        {
            SaveData();
            arg.ReplyWith("Player stats saved");
        }
        
        #endregion
        
        #region Helper Methods
        
        private void EnsurePlayerStats(string playerId, string playerName)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = new PlayerStats
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    LastSeen = DateTime.Now
                };
            }
        }
        
        private void ShowPlayerStats(BasePlayer player, string targetPlayerId)
        {
            if (!playerStats.ContainsKey(targetPlayerId))
            {
                SendReply(player, lang.GetMessage("NoStatsFound", this, player.UserIDString));
                return;
            }
            
            var stats = playerStats[targetPlayerId];
            var message = string.Format(lang.GetMessage("PlayerStatsFormat", this, player.UserIDString),
                stats.PlayerName,
                stats.Combat.Kills,
                stats.Combat.Deaths,
                stats.Combat.KDRatio.ToString("F2"),
                stats.Combat.Headshots,
                stats.Combat.OneShotKills,
                stats.Combat.FurthestKill.ToString("F1"),
                stats.Explosives.RocketsUsed,
                stats.Explosives.C4Used,
                stats.Farming.Wood,
                stats.Farming.Stone,
                stats.Farming.MetalOre,
                stats.Farming.Sulfur,
                stats.Travel.TotalDistance.ToString("F1"));
            
            SendReply(player, message);
        }
        
        private void ShowLeaderboard(BasePlayer player, string category)
        {
            var sortedStats = new List<PlayerStats>();
            
            switch (category)
            {
                case "kills":
                    sortedStats = playerStats.Values.OrderByDescending(s => s.Combat.Kills).Take(config.LeaderboardTopCount).ToList();
                    break;
                case "kd":
                case "kdr":
                    sortedStats = playerStats.Values.OrderByDescending(s => s.Combat.KDRatio).Take(config.LeaderboardTopCount).ToList();
                    break;
                case "headshots":
                    sortedStats = playerStats.Values.OrderByDescending(s => s.Combat.Headshots).Take(config.LeaderboardTopCount).ToList();
                    break;
                case "distance":
                    sortedStats = playerStats.Values.OrderByDescending(s => s.Travel.TotalDistance).Take(config.LeaderboardTopCount).ToList();
                    break;
                case "farming":
                    sortedStats = playerStats.Values.OrderByDescending(s => s.Farming.TotalResourcesGathered).Take(config.LeaderboardTopCount).ToList();
                    break;
                default:
                    SendReply(player, lang.GetMessage("InvalidCategory", this, player.UserIDString));
                    return;
            }
            
            var message = string.Format(lang.GetMessage("LeaderboardHeader", this, player.UserIDString), category.ToUpper());
            
            for (int i = 0; i < sortedStats.Count; i++)
            {
                var stats = sortedStats[i];
                var value = GetStatValue(stats, category);
                message += $"\n{i + 1}. {stats.PlayerName}: {value}";
            }
            
            SendReply(player, message);
        }
        
        private string GetStatValue(PlayerStats stats, string category)
        {
            switch (category)
            {
                case "kills":
                    return stats.Combat.Kills.ToString();
                case "kd":
                case "kdr":
                    return stats.Combat.KDRatio.ToString("F2");
                case "headshots":
                    return stats.Combat.Headshots.ToString();
                case "distance":
                    return $"{stats.Travel.TotalDistance:F1}m";
                case "farming":
                    return stats.Farming.TotalResourcesGathered.ToString();
                default:
                    return "N/A";
            }
        }
        
        #endregion
        
        #region Data Management
        
        private void LoadData()
        {
            try
            {
                playerStats = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerStats>>(DataFileName) ?? new Dictionary<string, PlayerStats>();
            }
            catch (Exception ex)
            {
                Puts($"Error loading data: {ex.Message}");
                playerStats = new Dictionary<string, PlayerStats>();
            }
        }
        
        private void SaveData()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject(DataFileName, playerStats);
            }
            catch (Exception ex)
            {
                Puts($"Error saving data: {ex.Message}");
            }
        }
        
        private void Unload()
        {
            saveTimer?.Destroy();
            SaveData();
        }
        
        #endregion
        
        #region Localization
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use this command.",
                ["NoStatsFound"] = "No statistics found for this player.",
                ["PlayerNotFound"] = "Player not found.",
                ["InvalidCategory"] = "Invalid category. Available: kills, kd, headshots, distance, farming",
                ["PlayerStatsFormat"] = "<color=#00ff00>{0}'s Statistics:</color>\n" +
                                       "Kills: {1} | Deaths: {2} | K/D: {3}\n" +
                                       "Headshots: {4} | One-Shot Kills: {5}\n" +
                                       "Furthest Kill: {6}m\n" +
                                       "Rockets Used: {7} | C4 Used: {8}\n" +
                                       "Resources: Wood({9}) Stone({10}) Metal({11}) Sulfur({12})\n" +
                                       "Distance Traveled: {13}m",
                ["LeaderboardHeader"] = "<color=#ffff00>=== {0} LEADERBOARD ===</color>"
            }, this);
        }
        
        #endregion
    }
}
