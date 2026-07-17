using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace ScritchyScratchyAP
{
    // Serializable save data. Persists across game sessions.
    public class TrackingData
    {
        public Dictionary<string, int> TicketCashOutCounts { get; set; } = new();
        public Dictionary<string, int> UpgradePurchaseCounts { get; set; } = new();
        public HashSet<string> SentLocations { get; set; } = new();
        // Counts of each AP item received (rebuilt from server on every connect)
        public Dictionary<string, int> ReceivedItemCounts { get; set; } = new();
        // Current level of each multi-level upgrade within this prestige cycle.
        // All multi-level upgrades (including Scratch Size) reset their displayed level
        // to 1 on prestige, so this must be reset to 0 alongside that. It's what tells a
        // manual buy which "Buy {apId} Level N" check corresponds to the level just
        // reached, as opposed to whatever check happens to be next in the lifetime backlog.
        public Dictionary<string, int> ProgressiveCycleLevels { get; set; } = new();
        // Highest cumulative received-count already applied for one-shot, non-idempotent
        // items (cash injections, traps, streaks, Jackpot Points). Persisted across
        // reconnects, unlike ReceivedItemCounts which is rebuilt from scratch on every
        // connect via the server's full item replay. Without this, every reconnect (most
        // notably F6's forced wipe and reconnect) would re-grant every cash injection/trap
        // ever received, since the replay refires ApplyItem for the entire item history.
        public Dictionary<string, int> AppliedNonIdempotentCounts { get; set; } = new();
        // Which AP seed and slot this save's progress belongs to. Used by
        // TrackingManager.ReconcileSaveForSeed to detect "this is a different
        // playthrough" and switch to, or create, the right per-playthrough save
        // file instead of silently mixing two playthroughs' progress together.
        public string LastSeedName { get; set; } = "";
        public string LastSlotName { get; set; } = "";
    }

    public static class TrackingManager
    {
        private static TrackingData _data = new();
        private static readonly object _saveLock = new();

        // Set true while ItemApplicator is calling Buy() so the ShopBuy patch
        // does not fire outgoing checks or update UpgradePurchaseCounts.
        public static bool IsApplyingReceivedItem = false;

        private static string DataDir => Path.Combine(
            BepInEx.Paths.BepInExRootPath, "data", "ScritchyScratchyAP");

        // Active save file for whichever playthrough is currently connected. Not
        // meaningful until ReconcileSaveForSeed runs (right after connecting, once
        // the AP seed name is known), which is always before anything calls Save().
        private static string _activeSavePath = Path.Combine(DataDir, "saves", "_unassigned.json");

        // Call this after AP connection is established
        public static void Initialize()
        {
            Plugin.Log.LogInfo("AP Tracking: Initialized.");
        }

        // Called once per successful connection, right after the AP seed name becomes
        // known before the item-received subscription/login replay even starts, so
        // there's no window where a replay could land in the wrong file. Detects
        // whether the currently loaded save belongs to a different playthrough than
        // the one we just connected to, and if so, stashes it under its own
        // per-(seed, slot) file and loads, or creates, the correct one instead.
        // This is what prevents one playthrough's progress from bleeding into
        // another's, and lets a player freely switch between multiple ongoing
        // playthroughs without losing progress on any of them.
        public static void ReconcileSaveForSeed(string seedName, string slotName)
        {
            if (string.IsNullOrEmpty(seedName))
            {
                Plugin.Log.LogWarning("AP: ReconcileSaveForSeed called with empty seed name, skipping.");
                return;
            }

            bool sameIdentity = _data.LastSeedName == seedName && _data.LastSlotName == slotName;
            if (sameIdentity)
            {
                _activeSavePath = SaveFilePathFor(seedName, slotName);
                Plugin.Log.LogInfo("AP: Reconnecting to the same playthrough, keeping existing save.");
                return;
            }

            if (!string.IsNullOrEmpty(_data.LastSeedName))
            {
                // Switching to a different playthrough than the one currently loaded.
                // Preserve the old one under its own file before touching _data.
                string previousPath = SaveFilePathFor(_data.LastSeedName, _data.LastSlotName);
                WriteToPath(previousPath, _data);
                Plugin.Log.LogInfo($"AP: Switched playthroughs - saved previous progress to '{previousPath}'.");
            }

            string newPath = SaveFilePathFor(seedName, slotName);
            if (File.Exists(newPath))
            {
                _data = ReadFromPath(newPath) ?? new TrackingData();
                Plugin.Log.LogInfo($"AP: Loaded existing save for this playthrough from '{newPath}'.");
            }
            else
            {
                _data = new TrackingData();
                Plugin.Log.LogInfo("AP: No existing save for this playthrough - starting fresh.");
            }

            _data.LastSeedName = seedName;
            _data.LastSlotName = slotName;
            _activeSavePath = newPath;
            Save();
        }

        private static string SaveFilePathFor(string seedName, string slotName)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"{seedName}|{slotName}"));
            string hex = BitConverter.ToString(hash).Replace("-", "").Substring(0, 24).ToLowerInvariant();
            return Path.Combine(DataDir, "saves", $"{hex}.json");
        }

        // Received Item Tracking
        // Called by ArchipelagoManager on each received item event.
        // Counts are reset on every new connection so the full item
        // list from the server always rebuilds cleanly.
        public static void ResetReceivedItems()
        {
            // Clear in-memory only. Do not save here. ReconcileSaveForSeed loads the
            // real save (with SentLocations) before this is called, so saving now
            // would overwrite that data with an empty ReceivedItemCounts. The server
            // replay will rebuild counts via IncrementReceivedItem which saves incrementally.
            _data.ReceivedItemCounts.Clear();
        }

        public static int IncrementReceivedItem(string itemName)
        {
            if (!_data.ReceivedItemCounts.ContainsKey(itemName))
                _data.ReceivedItemCounts[itemName] = 0;
            _data.ReceivedItemCounts[itemName]++;
            Save();
            return _data.ReceivedItemCounts[itemName];
        }

        public static Dictionary<string, int> GetReceivedItemCounts()
        {
            return _data.ReceivedItemCounts;
        }

        public static int GetUpgradePurchaseCount(string upgradeId)
        {
            return _data.UpgradePurchaseCounts.TryGetValue(upgradeId, out int cnt) ? cnt : 0;
        }

        // Progressive Upgrade Cycle Levels
        // Tracks each multi-level upgrade's real current level within the active
        // prestige cycle (reset to 0 on prestige). Used by Patch_ShopTryBuy to
        // award the check matching the level actually just reached, instead of
        // whatever check is next in the lifetime backlog.
        public static int GetProgressiveCycleLevel(string apId)
            => _data.ProgressiveCycleLevels.TryGetValue(apId, out int lvl) ? lvl : 0;

        public static int IncrementProgressiveCycleLevel(string apId)
        {
            _data.ProgressiveCycleLevels.TryGetValue(apId, out int cur);
            int next = cur + 1;
            _data.ProgressiveCycleLevels[apId] = next;
            Save();
            return next;
        }

        // Non-Idempotent Item Application Deuplication
        // For one-shot items (cash injections, traps, streaks, Jackpot Points) that
        // apply an effect directly rather than setting persistent state, ApplyItem must
        // not re-run for a given cumulative count more than once, since a reconnect
        // replays the entire item history from count 1 again.
        public static int GetAppliedNonIdempotentCount(string itemName)
            => _data.AppliedNonIdempotentCounts.TryGetValue(itemName, out int cnt) ? cnt : 0;

        // Returns true and records the new high-water mark only if totalReceived is
        // higher than what's already been applied for this item, i.e. this is a
        // genuinely new copy of the item, not a replay of one already handled.
        public static bool TryMarkNonIdempotentApplied(string itemName, int totalReceived)
        {
            int already = GetAppliedNonIdempotentCount(itemName);
            if (totalReceived <= already) return false;
            _data.AppliedNonIdempotentCounts[itemName] = totalReceived;
            Save();
            return true;
        }

        // Ticket Cash-Outs
        public static void OnTicketCashedOut(string rawSharedId)
        {
            // Goal condition
            if (Locations.IsGoalTicket(rawSharedId))
            {
                TriggerGoal();
                return;
            }

            // Excluded tickets
            if (rawSharedId == "Loan") return;

            // Final Chance variants each have their own dedicated one-time location
            // instead of sharing a combined cash-out counter/threshold. Send it (or
            // no-op if already sent) as soon as that specific variant is cashed out.
            if (Locations.IsFinalChanceVariant(rawSharedId))
            {
                Plugin.Log.LogInfo($"AP Tracking: {rawSharedId} cashed out");
                TrySendCheck(Locations.GetFinalChanceCashOutLocationName(rawSharedId));
                Save();
                return;
            }

            // Must be a tracked ticket type
            bool isBase = Array.IndexOf(Locations.BaseTickets, rawSharedId) >= 0;
            bool isSuper = Array.IndexOf(Locations.SuperTickets, rawSharedId) >= 0;
            if (!isBase && !isSuper) return;

            // Increment counter
            if (!_data.TicketCashOutCounts.ContainsKey(rawSharedId))
                _data.TicketCashOutCounts[rawSharedId] = 0;
            _data.TicketCashOutCounts[rawSharedId]++;

            int count = _data.TicketCashOutCounts[rawSharedId];
            Plugin.Log.LogInfo($"AP Tracking: {rawSharedId} cashed out x{count}");

            // Check thresholds and send any newly reached ones
            var thresholds = isSuper ? Locations.SuperTicketCashOutThresholds : Locations.TicketCashOutThresholds;
            foreach (int threshold in thresholds)
            {
                if (count >= threshold)
                {
                    string locationName = $"Cash Out {rawSharedId} {threshold}";
                    TrySendCheck(locationName);
                }
            }

            Save();
        }

        // Upgrade Purchases
        public static bool IsLocationSent(string locationName)
            => _data?.SentLocations.Contains(locationName) == true;

        public static void OnUpgradePurchased(string upgradeId, int buyParam)
        {
            // Coin-tier AP application: auto-complete the previous coin's scratch size checks.
            // Must run before IsApplyingReceivedItem guard, this fires when AP applies a coin.
            // For non-coin upgrades and non-tracked IDs (tickets) this is a no-op.
            if (Array.IndexOf(Locations.SinglePurchaseUpgrades, upgradeId) >= 0)
                AutoCompletePreviousScratchSize(upgradeId);

            // All check-sending for manual upgrade clicks is handled by Patch_ShopBuy.Prefix.
            // This method is only reached when Prefix lets the buy through:
            //   - AP-applying an item (IsApplyingReceivedItem = true)
            //   - Non-tracked ShopPanel buys (ticket purchases, Super tickets, etc.)
        }

        // Achievements
        public static void OnAchievementTriggered(string achievementId)
        {
            if (Array.IndexOf(Locations.Achievements, achievementId) < 0)
            {
                Plugin.Log.LogInfo($"AP Tracking: Achievement '{achievementId}' not in location list, skipping.");
                return;
            }

            TrySendCheck($"Achievement: {achievementId}");
            Save();
        }

        // Auto-Complete Scratch Size on Coin Tier Upgrade
        // When player buys a new coin tier, the previous tier's
        // scratch size upgrades are granted as free AP checks
        private static readonly Dictionary<string, string> PreviousScratchSizeByCoin = new()
        {
            { "Tin Coin", "Scratch Size Base Coin" },
            { "Aluminum Coin", "Scratch Size Tin Coin" },
            { "Copper Coin", "Scratch Size Aluminum Coin" },
            { "Bronze Coin", "Scratch Size Copper Coin" },
            { "Iron Coin", "Scratch Size Bronze Coin" },
            { "Steel Coin", "Scratch Size Iron Coin" },
            { "Titanium Coin", "Scratch Size Steel Coin" },
            { "Tungsten Coin", "Scratch Size Titanium Coin" },
        };

        private static void AutoCompletePreviousScratchSize(string coinUpgradeId)
        {
            if (!PreviousScratchSizeByCoin.TryGetValue(coinUpgradeId, out string previousScratchSize)) return;

            Plugin.Log.LogInfo($"AP Tracking: Auto-completing {previousScratchSize} (coin tier skipped).");
            TrySendCheck($"Buy {previousScratchSize} Level 1");
            TrySendCheck($"Buy {previousScratchSize} Level 2");
        }

        // Catches up any coin tiers that were already bought before this session started,
        // ensuring their previous tier's Scratch Size checks are auto-completed. Safe to
        // call repeatedly, TrySendCheck no-ops for already-sent locations. Runs once at
        // startup right after the save is loaded.
        public static void CatchUpCoinAutoCompletes()
        {
            foreach (var coinId in PreviousScratchSizeByCoin.Keys)
            {
                if (IsLocationSent($"Buy {coinId}"))
                    AutoCompletePreviousScratchSize(coinId);
            }
        }

        // Prestige Events
        public static void OnPrestige()
        {
            try
            {
                var saveData = SaveData.Current;
                if (saveData == null) return;
                int count = saveData.prestigeCount;
                if (count <= 0 || count > Locations.PrestigeLocationCount) return;
                string locationName = $"Prestige {count}";
                Plugin.Log.LogInfo($"AP Tracking: Prestige #{count} - checking '{locationName}'");
                TrySendCheck(locationName);
                _data.ProgressiveCycleLevels.Clear();
                Plugin.Log.LogInfo("AP Tracking: Prestige - cleared progressive upgrade cycle levels.");
                Save();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"AP Tracking: OnPrestige failed: {e.Message}");
            }
        }

        public static void OnPrestigePerkBought(string perkId, int manualBuyCount)
        {
            if (IsApplyingReceivedItem) return;

            // Single-purchase prestige perks
            if (Array.IndexOf(Locations.PrestigeSinglePerks, perkId) >= 0)
            {
                if (manualBuyCount == 1)
                    TrySendCheck($"Buy Prestige Perk {perkId}");
                Save();
                return;
            }

            // Multi-level prestige perks
            // manualBuyCount is the game's own buy count for this perk. It already
            // reflects exactly how many levels have actually been purchased in-game
            // this respec cycle. Resets to 0 by the player respeccing.
            foreach (var (id, max) in Locations.PrestigeMultiPerks)
            {
                if (id != perkId) continue;
                int level = Math.Min(manualBuyCount, max);
                for (int l = 1; l <= level; l++)
                    TrySendCheck($"Buy Prestige Perk {perkId} Level {l}");
                Save();
                return;
            }
        }

        // Goal
        private static void TriggerGoal()
        {
            Plugin.Log.LogInfo("AP: *** GOAL REACHED - Final Chance won! ***");
            if (!ArchipelagoManager.Connected) return;
            try
            {
                ArchipelagoManager.Session.SetGoalAchieved();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"AP: Failed to set goal achieved: {e.Message}");
            }
        }

        // Core Check Sender
        public static void TrySendCheck(string locationName)
        {
            // Already sent this session or a previous session
            if (_data.SentLocations.Contains(locationName)) return;

            long? locationId = Locations.GetLocationId(locationName);
            if (locationId == null) return;

            if (!ArchipelagoManager.Connected)
            {
                Plugin.Log.LogWarning($"AP: Not connected, cannot send check '{locationName}'");
                return;
            }

            _data.SentLocations.Add(locationName);
            ArchipelagoManager.SendCheck(locationId.Value);
            Plugin.Log.LogInfo($"AP: Check sent: '{locationName}' (ID: {locationId})");
        }

        // Save
        public static void Save() => WriteToPath(_activeSavePath, _data);

        private static void WriteToPath(string path, TrackingData data)
        {
            lock (_saveLock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(path);
                    if (dir != null) Directory.CreateDirectory(dir);
                    File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"AP: Failed to save tracking data to '{path}': {e.Message}");
                }
            }
        }

        private static TrackingData ReadFromPath(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<TrackingData>(json);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"AP: Failed to load tracking data from '{path}': {e.Message}");
                return null;
            }
        }

        public static void WipeSave()
        {
            lock (_saveLock)
            {
                // Preserve AppliedNonIdempotentCounts across the wipe. F6 forces a fresh
                // reconnect that replays the entire item history from the server. If this
                // dict wiped too, every cash injection/trap/streak ever received would
                // re-apply on the replay, letting the player farm unlimited money by
                // spamming F6. Everything else (SentLocations, cash-out counts, etc.) is
                // intentionally reset since F6 exists to test a from-scratch save.
                var preservedApplied = _data.AppliedNonIdempotentCounts;
                _data = new TrackingData { AppliedNonIdempotentCounts = preservedApplied };
                try
                {
                    if (File.Exists(_activeSavePath))
                        File.Delete(_activeSavePath);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"AP: Failed to delete save file: {e.Message}");
                }
            }
            Plugin.Log.LogInfo("AP: Save wiped.");
        }
    }
}