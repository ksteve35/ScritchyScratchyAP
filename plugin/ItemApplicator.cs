using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScritchyScratchyAP
{
    public static class ItemApplicator
    {
        // Tracks what levels have actually applied this session.
        // Keyed by upgrade/ticket name, value is the level applied (1 for single/ticket).
        // Reset every connection so re-received items re-apply after prestige.
        private static readonly Dictionary<string, int> _appliedLevels = new();

        // Suppresses repeated "deferring X, predecessor Y not yet received" log lines.
        // Cleared when the predecessor arrives via ResetAppliedLevels on reconnect, or
        // when ApplyTicketUnlock successfully passes the predecessor check and clears it.
        private static readonly HashSet<string> _loggedDeferrals = new();

        // Suppresses "Locking X" log spam from LockUnapplied.
        // Only logs the first lock of each item per session, cleared on reconnect.
        private static readonly HashSet<string> _loggedLocks = new();

        // The upgrade ID currently being applied by AP via Buy(). Non-null only during
        // the Buy() call itself. Used by Patch_ShopBuy.Prefix to allow only this exact
        // panel through while IsApplyingReceivedItem is true, blocking collateral Buy()
        // calls the game may trigger as side effects of buying another upgrade.
        private static string _currentlyApplyingId = null;
        public static string CurrentlyApplyingId => _currentlyApplyingId;

        // Set to true when AP progressive upgrades are applied so APUpdateManager
        // can call UpdatePanelsVisibility() on the next frame to refresh the level
        // counter display without rebuilding shopPanelDict.
        public static bool ShopNeedsVisibilityRefresh = false;

        private const int JACKPOT_POINTS_PER_ITEM = 10; // Must match Python JACKPOT_POINTS_PER_ITEM

        // Final Chance variants 2-4 are each their own AP item (unlike the first Final Chance
        // which stays in Locations.BaseTickets). Panel ids use underscores (game data), AP item
        // names use spaces (matches every other "Unlock X" item's naming convention).
        public static readonly Dictionary<string, string> FinalChanceVariantUnlockItems = new()
        {
            { "Final Chance_2", "Unlock Final Chance 2" },
            { "Final Chance_3", "Unlock Final Chance 3" },
            { "Final Chance_4", "Unlock Final Chance 4" },
        };

        // Maps prestige perk ID strings to their PerkType enum values
        public static readonly Dictionary<string, PerkType> PerkTypeByName = new()
        {
            { "Challenges",            PerkType.Challenges },
            { "Night Market",          PerkType.NightMarket },
            { "Starter Kit",           PerkType.StarterKit },
            { "Jackpot Power",         PerkType.JackpotPower },
            { "Tool Belt",             PerkType.ToolBelt },
            { "Self Made Millionaire", PerkType.SelfMadeMillionaire },
            { "Booster Kit",           PerkType.BoosterKit },
            { "Recycling",             PerkType.Recycling },
            { "Less is More",          PerkType.LessIsMore },
            { "Ignorance is Bliss",    PerkType.IgnoranceIsBliss },
            { "Big Winner",            PerkType.BigWinner },
            { "Completionist",         PerkType.Completionist },
            { "Electric Fan",          PerkType.ElectricFan },
            { "Clean Freak",           PerkType.CleanFreak },
            { "Smart Investment",      PerkType.SmartInvestment },
            { "Learn by Doing",        PerkType.LearnByDoing },
            { "Air Condition",         PerkType.AirCondition },
            { "Pet Lover",             PerkType.PetLover },
            { "Dishwasher",            PerkType.Dishwasher },
            { "Magic",                 PerkType.Magic },
            { "Shopping Spree",        PerkType.ShoppingSpree },
            { "Refund",                PerkType.Refund },
            { "Soft Hands",            PerkType.SoftHands },
            { "Collector",             PerkType.Collector },
            { "Loan Shark",            PerkType.LoanShark },
            { "Experienced",           PerkType.Experienced },
            { "Picky Eater",           PerkType.PickyEater },
            { "Fully Automated",       PerkType.FullyAutomated },
            { "Fine Dining",           PerkType.FineDining },
            { "Built Different",       PerkType.BuiltDifferent },
            { "Hotkeys",               PerkType.Hotkeys },
            { "PlateMaster5000",       PerkType.PlateMaster5000 },
        };

        // Temporary ScratchLuck modifiers. Each streak item adds 3 to the counter;
        // the bonus is re-applied after every PopulateShop reset and removed when the
        // counter reaches 0 (decremented on each ticket cash-out).
        private static int _luckyStreakTickets = 0;
        private static int _unluckyStreakTickets = 0;
        private const int STREAK_LUCK_AMOUNT = 25;

        // Called by Patch_ShopBuy.Postfix after a manual progressive upgrade buy.
        // Bumps _appliedLevels by 1 so ApplyAll() treats Buy()'s stat gain as already
        // accounted for and doesn't apply an extra ApplyUpgrade on top.
        public static void TrackManualApply(string upgradeId)
        {
            _appliedLevels.TryGetValue(upgradeId, out int current);
            _appliedLevels[upgradeId] = current + 1;
        }

        public static int GetAppliedLevel(string upgradeId)
        {
            _appliedLevels.TryGetValue(upgradeId, out int level);
            return level;
        }

        public static void ResetAppliedLevels()
        {
            _appliedLevels.Clear();
            _loggedDeferrals.Clear();
            _loggedLocks.Clear();
            ShopNeedsVisibilityRefresh = false;
            _luckyStreakTickets = 0;
            _unluckyStreakTickets = 0;
        }

        // Re-applies shop items from saved counts, called on shop load.
        // Cash injections and traps are intentionally excluded.
        public static void ApplyAll()
        {
            var counts = TrackingManager.GetReceivedItemCounts();
            if (counts.Count == 0) return;

            // Unlocks first so prerequisites exist before progressives run
            foreach (var kvp in counts.OrderBy(k => GetApplyPriority(k.Key)))
            {
                if (!kvp.Key.StartsWith("Unlock ") && !kvp.Key.StartsWith("Progressive "))
                    continue;
                ApplyItem(kvp.Key, kvp.Value);
            }
        }

        // Locks panels for items AP has not yet sent to the player.
        // Called after ApplyAll() so already-received items are applied first.
        // IsLocked doesn't reliably persist, the game resets it each frame.
        // Real purchase enforcement is in Patch_ShopBuy.Prefix.
        public static void LockUnapplied()
        {
            if (!ArchipelagoManager.Connected) return;

            var received = TrackingManager.GetReceivedItemCounts();

            // Single-purchase shop upgrades
            var shop = UnityEngine.Object.FindObjectOfType<UpgradeShop>(true);
            if (shop != null)
            {
                foreach (var upgradeId in Locations.SinglePurchaseUpgrades)
                {
                    if (!shop.shopPanelDict.TryGetValue(upgradeId, out ShopPanel panel)) continue;
                    bool hasItem = received.TryGetValue($"Unlock {upgradeId}", out int cnt) && cnt >= 1;
                    if (!hasItem && !panel.IsLocked)
                    {
                        if (_loggedLocks.Add($"upgrade:{upgradeId}"))
                            Plugin.Log.LogInfo($"AP ItemApplicator: Locking upgrade '{upgradeId}'");
                        try { panel.SetLocked(true); }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogError($"AP ItemApplicator: IsLocked setter threw for '{upgradeId}': {ex.Message}");
                        }
                    }
                }
            }

            // Ticket types, lock all base tickets except Day Job.
            // The game's catalog system naturally unlocks tickets after enough Day Job plays.
            // Re-lock any tickets that AP hasn't granted yet.
            var ticketShop = UnityEngine.Object.FindObjectOfType<TicketShop>(true);
            if (ticketShop != null)
            {
                foreach (var ticketId in Locations.BaseTickets)
                {
                    if (ticketId == "Day Job") continue;
                    if (!ticketShop.shopPanelDict.TryGetValue(ticketId, out ShopPanel panel)) continue;
                    bool hasItem = received.TryGetValue($"Unlock {ticketId}", out int cnt) && cnt >= 1;
                    if (!hasItem && !panel.IsLocked)
                    {
                        if (_loggedLocks.Add($"ticket:{ticketId}"))
                            Plugin.Log.LogInfo($"AP ItemApplicator: Locking ticket '{ticketId}'");
                        try { panel.SetLocked(true); }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogError($"AP ItemApplicator: IsLocked setter threw for ticket '{ticketId}': {ex.Message}");
                        }
                    }
                }

                // Final Chance variants 2-4, same lock logic, keyed by their own AP items.
                foreach (var kvp in FinalChanceVariantUnlockItems)
                {
                    string panelId = kvp.Key;
                    string requiredItem = kvp.Value;
                    if (!ticketShop.shopPanelDict.TryGetValue(panelId, out ShopPanel panel)) continue;
                    bool hasItem = received.TryGetValue(requiredItem, out int cnt) && cnt >= 1;
                    if (!hasItem && !panel.IsLocked)
                    {
                        if (_loggedLocks.Add($"ticket:{panelId}"))
                            Plugin.Log.LogInfo($"AP ItemApplicator: Locking ticket '{panelId}'");
                        try { panel.SetLocked(true); }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogError($"AP ItemApplicator: IsLocked setter threw for ticket '{panelId}': {ex.Message}");
                        }
                    }
                }
            }

            // Gadget sub-upgrades, lock if base gadget not yet received.
            if (shop != null)
            {
                foreach (var (upgradeId, max) in Locations.MultiLevelUpgrades)
                {
                    string panelId = upgradeId.StartsWith("Scratch Size ")
                        ? "Scratch Size_" + upgradeId.Substring("Scratch Size ".Length)
                        : upgradeId;
                    if (!shop.shopPanelDict.TryGetValue(panelId, out ShopPanel panel)) continue;

                    bool missingGadget = Locations.MultiLevelUpgradePrerequisites.TryGetValue(panelId, out string reqGadget)
                        && !(received.TryGetValue($"Unlock {reqGadget}", out int gc) && gc >= 1);

                    int cycleLevel = TrackingManager.GetProgressiveCycleLevel(upgradeId) + 1;
                    int receivedCount = received.TryGetValue($"Progressive {upgradeId}", out int pc) ? pc : 0;
                    // Only the first ProgressiveGatedLevels[upgradeId] levels are AP-gated
                    // (gated gadget upgrades have half their MaxLevel in the pool). Beyond
                    // that, levels are free normal-priced buys, never locked for this reason.
                    int gatedLevels = Locations.ProgressiveGatedLevels.TryGetValue(upgradeId, out int gl) ? gl : max;
                    bool missingProgressive = cycleLevel <= gatedLevels && cycleLevel > receivedCount;

                    bool shouldBeLocked = missingGadget || missingProgressive;
                    if (shouldBeLocked == panel.IsLocked) continue;

                    try
                    {
                        panel.SetLocked(shouldBeLocked);
                        if (shouldBeLocked && _loggedLocks.Add($"progressive:{upgradeId}"))
                        {
                            string reason = missingGadget ? $"requires Unlock {reqGadget}" : $"level {cycleLevel} needs {cycleLevel} received, has {receivedCount}";
                            Plugin.Log.LogInfo($"AP ItemApplicator: Locking progressive '{upgradeId}' ({reason})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"AP ItemApplicator: SetLocked threw for '{upgradeId}': {ex.Message}");
                    }
                }
            }
        }

        // Applies a single item at the given cumulative received count.
        public static void ApplyItem(string itemName, int totalReceived)
        {
            if (itemName.StartsWith("Unlock "))
            {
                ApplyUnlock(itemName.Substring("Unlock ".Length));
            }
            else if (itemName.StartsWith("Progressive "))
            {
                string upgradeId = itemName.Substring("Progressive ".Length);
                // Route prestige multi-level perks to the perk manager
                bool isPrestigePerk = false;
                foreach (var (id, _) in Locations.PrestigeMultiPerks)
                {
                    if (id == upgradeId) { isPrestigePerk = true; break; }
                }
                if (isPrestigePerk)
                    ApplyPrestigePerkProgressive(upgradeId, totalReceived);
                else if (upgradeId == "Prestige")
                    { /* no-op: Progressive Prestige is a locked item earned by prestiging */ }
                else if (upgradeId.StartsWith("Scratch Size "))
                    ApplyShopUpgradeToLevel(upgradeId, totalReceived);
                else
                    ApplyProgressiveUpgrade(upgradeId, totalReceived);
            }
            else
            {
                // These items apply a one-shot effect (add money, trigger a trap, etc.)
                // rather than setting persistent, idempotent state, so they must not
                // re-run for a copy that's already been applied. Without this guard,
                // any reconnect, most notably F6's forced wipe and reconnect, would replay
                // the entire item history and re-grant every cash injection/trap ever
                // received, letting the player farm unlimited money by spamming F6.
                if (!TrackingManager.TryMarkNonIdempotentApplied(itemName, totalReceived))
                    return;

                switch (itemName)
                {
                    case "Small Cash Injection":  ApplyScaledCash(10.0);  break;
                    case "Large Cash Injection":  ApplyScaledCash(100.0); break;
                    case "Jackpot Points":         ApplyJackpotPoints(); break;
                    case "Lucky Streak":   ApplyLuckyStreak(); break;
                    case "Unlucky Streak": ApplyUnluckyStreak(); break;
                    case "Debt Trap":  ApplyDebtTrap(); break;
                    case "Loan Trap":  ApplyLoanTrap(); break;
                    default:
                        Plugin.Log.LogWarning($"AP ItemApplicator: Unhandled item '{itemName}'");
                        break;
                }
            }
        }

        private static int GetApplyPriority(string itemName)
        {
            if (itemName.StartsWith("Unlock "))       return 0;
            if (itemName.StartsWith("Progressive "))  return 1;
            if (itemName.EndsWith("Cash Injection"))  return 2;
            return 3; // Traps last
        }

        // Unlock routing
        private static void ApplyUnlock(string target)
        {
            // Prestige single-purchase perks
            if (Array.IndexOf(Locations.PrestigeSinglePerks, target) >= 0)
            {
                ApplyPrestigePerk(target);
                return;
            }

            // Final Chance variants 2-4, route to the underscore panel id
            // since it doesn't match this space-separated AP item name like
            // FinalChanceVariantUnlockItems. Checked before BaseTickets since
            // these variants are deliberately not in that array.
            foreach (var kvp in FinalChanceVariantUnlockItems)
            {
                if (kvp.Value == $"Unlock {target}")
                {
                    ApplyTicketUnlock(kvp.Key);
                    return;
                }
            }

            // Ticket names go to the ticket handler
            if (Array.IndexOf(Locations.BaseTickets, target) >= 0)
            {
                ApplyTicketUnlock(target);
                return;
            }

            // Single-purchase shop upgrades, explicitly unlock the panel so the
            // player can buy it. SetLocked(true) persists in game state, so call
            // SetLocked(false) here.
            UnlockUpgradePanel(target);
        }

        private static void UnlockUpgradePanel(string upgradeId)
        {
            var shop = UnityEngine.Object.FindObjectOfType<UpgradeShop>(true);
            if (shop == null) return;
            if (!shop.shopPanelDict.TryGetValue(upgradeId, out ShopPanel panel)) return;
            if (!panel.IsLocked) return;
            Plugin.Log.LogInfo($"AP ItemApplicator: Unlocking upgrade panel '{upgradeId}'");
            try { panel.SetLocked(false); }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP ItemApplicator: SetLocked(false) threw for '{upgradeId}': {ex.Message}");
            }
        }

        // Prestige perks
        private static void ApplyPrestigePerk(string perkId)
        {
            if (!PerkTypeByName.TryGetValue(perkId, out PerkType perkType))
            {
                Plugin.Log.LogWarning($"AP ItemApplicator: Unknown prestige perk '{perkId}'");
                return;
            }
            if (_appliedLevels.TryGetValue($"Perk:{perkId}", out int applied) && applied >= 1) return;
            _appliedLevels[$"Perk:{perkId}"] = 1;

            TrackingManager.IsApplyingReceivedItem = true;
            try
            {
                var perkManager = UnityEngine.Object.FindObjectOfType<PerkManager>(true);
                if (perkManager == null)
                {
                    Plugin.Log.LogWarning($"AP ItemApplicator: PerkManager not found, deferring '{perkId}'");
                    return;
                }
                Plugin.Log.LogInfo($"AP ItemApplicator: Activating prestige perk '{perkId}' at level 1");
                perkManager.ActivatePerk(perkType, 1);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP ItemApplicator: ActivatePerk threw for '{perkId}': {ex.Message}");
            }
            finally
            {
                TrackingManager.IsApplyingReceivedItem = false;
            }
        }

        private static void ApplyPrestigePerkProgressive(string perkId, int targetLevel)
        {
            if (!PerkTypeByName.TryGetValue(perkId, out PerkType perkType))
            {
                Plugin.Log.LogWarning($"AP ItemApplicator: Unknown prestige perk '{perkId}'");
                return;
            }
            if (_appliedLevels.TryGetValue($"Perk:{perkId}", out int currentApplied) && currentApplied >= targetLevel) return;
            _appliedLevels[$"Perk:{perkId}"] = targetLevel;

            TrackingManager.IsApplyingReceivedItem = true;
            try
            {
                var perkManager = UnityEngine.Object.FindObjectOfType<PerkManager>(true);
                if (perkManager == null)
                {
                    Plugin.Log.LogWarning($"AP ItemApplicator: PerkManager not found, deferring '{perkId}'");
                    return;
                }
                // Take max of AP level and player's manually bought level to prevent downgrade
                int manualLevel = 0;
                try { manualLevel = SaveData.Current?.GetPrestigeUpgradeBuyCount(perkId) ?? 0; }
                catch { }
                int applyLevel = Math.Max(targetLevel, manualLevel);
                Plugin.Log.LogInfo($"AP ItemApplicator: Prestige perk '{perkId}' → level {applyLevel} (AP:{targetLevel}, manual:{manualLevel})");
                perkManager.ActivatePerk(perkType, applyLevel);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP ItemApplicator: ActivatePerk threw for '{perkId}': {ex.Message}");
            }
            finally
            {
                TrackingManager.IsApplyingReceivedItem = false;
            }
        }

        private static void ApplyJackpotPoints()
        {
            try
            {
                var saveData = SaveData.Current;
                if (saveData == null) return;
                saveData.prestigeCurrency += JACKPOT_POINTS_PER_ITEM;
                Plugin.Log.LogInfo($"AP ItemApplicator: Jackpot Points +{JACKPOT_POINTS_PER_ITEM} (now {saveData.prestigeCurrency})");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP ItemApplicator: ApplyJackpotPoints threw: {ex.Message}");
            }
        }

        // Shop upgrades (single-purchase, Scratch Size, and progressive)
        // Calls ShopPanel.Buy() with the tracking suppress flag active.
        // AP items are free. Saves the wallet balance before buying and
        // restores it after so the player's money is unaffected.
        private static void ApplyShopUpgradeToLevel(string upgradeId, int targetLevel)
        {
            // ApplyUpgrade() does not affect Scratch Size panels. The stat doesn't change.
            // Mark the AP level so ApplyAll() doesn't retry on every cycle.
            // The player's manual Buy() clicks are what actually apply the stat and advance the counter
            _appliedLevels.TryGetValue(upgradeId, out int currentApplied);
            if (currentApplied >= targetLevel) return;

            _appliedLevels[upgradeId] = targetLevel;
            Plugin.Log.LogInfo($"AP ItemApplicator: Scratch Size '{upgradeId}' AP level marked at {targetLevel} (stat applies on player buy)");
        }

        // Progressive upgrades use ApplyUpgrade() directly instead of Buy() so that
        // the purchase-price counter is never incremented. This keeps the cost of the
        // player's first manual buy at the base level-1 price regardless of how many
        // AP levels have been pre-applied.
        private static void ApplyProgressiveUpgrade(string upgradeId, int targetLevel)
        {
            var shop = UnityEngine.Object.FindObjectOfType<UpgradeShop>(true);
            if (shop == null)
            {
                Plugin.Log.LogWarning($"AP ItemApplicator: UpgradeShop not found, deferring '{upgradeId}'");
                return;
            }
            if (!shop.shopPanelDict.ContainsKey(upgradeId))
            {
                Plugin.Log.LogWarning($"AP ItemApplicator: No ShopPanel for '{upgradeId}'");
                return;
            }

            _appliedLevels.TryGetValue(upgradeId, out int currentApplied);
            if (currentApplied >= targetLevel) return;

            _appliedLevels[upgradeId] = targetLevel;

            Plugin.Log.LogInfo($"AP ItemApplicator: '{upgradeId}' {currentApplied} → {targetLevel} (ApplyUpgrade — no cost inflation)");
            TrackingManager.IsApplyingReceivedItem = true;
            try
            {
                for (int i = currentApplied; i < targetLevel; i++)
                    shop.ApplyUpgrade(upgradeId);
                Plugin.Log.LogInfo($"AP ItemApplicator: '{upgradeId}' applied to level {targetLevel}");
                ShopNeedsVisibilityRefresh = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP ItemApplicator: ApplyUpgrade threw for '{upgradeId}': {ex.Message}");
            }
            finally
            {
                TrackingManager.IsApplyingReceivedItem = false;
            }
        }

        // Ticket unlocks
        // Set IsLocked = false on the panel so it appears as available to buy in the ticket shop.
        private static void ApplyTicketUnlock(string ticketName)
        {
            // NOTE: the player intentionally does NOT require predecessor "Unlock X" items to have
            // arrived first.
            var ticketShop = UnityEngine.Object.FindObjectOfType<TicketShop>(true);
            if (ticketShop == null)
            {
                Plugin.Log.LogWarning($"AP ItemApplicator: TicketShop not ready, deferring '{ticketName}'");
                return;
            }

            // Do NOT force the catalog open ourselves.
            // Once the player naturally reaches the catalog through real play, the panel will exist
            // and this same periodic retry will unlock it correctly.
            if (!ticketShop.shopPanelDict.TryGetValue(ticketName, out ShopPanel panel))
            {
                if (_loggedDeferrals.Add($"catalog:{ticketName}"))
                    Plugin.Log.LogInfo($"AP ItemApplicator: Deferring '{ticketName}' — catalog not yet naturally reached (ActiveCatalogs={ticketShop.ActiveCatalogs})");
                return;
            }

            if (!panel.IsLocked) return;

            Plugin.Log.LogInfo($"AP ItemApplicator: Unlocking ticket '{ticketName}'");
            try
            {
                panel.SetLocked(false);
                Plugin.Log.LogInfo($"AP ItemApplicator: Ticket '{ticketName}' unlocked (IsLocked now={panel.IsLocked})");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP ItemApplicator: IsLocked setter threw for '{ticketName}': {ex.Message}");
            }
        }

        // Cash injection amounts scale off the most expensive ticket the player can
        // currently see and buy in the shop. This keeps injections meaningful relative
        // to what the player can actually spend on right now, regardless of which
        // tickets AP has granted.
        //
        // Small = 10x most expensive accessible ticket cost
        // Large = 100x most expensive accessible ticket cost
        private static readonly Dictionary<string, double> TicketCosts = new()
        {
            { "Two Win",         10 },
            { "Mini Scratch",    100 },
            { "Apple Tree",      2_000 },
            { "Quick Cash",      10_000 },
            { "Lucky Cat",       300_000 },
            { "Sand Dollars",    20_000_000 },            // 20 M
            { "Scratch My Back", 500_000_000 },           // 500 M
            { "Snake Eyes",      10_000_000_000 },        // 10 B
            { "The Bomb",        200_000_000_000 },       // 200 B
            { "Bank Break",      200_000_000_000_000 },   // 200 T
            { "Xmas Countdown",  1e16 },                  // 10 Qa
            { "Thrift Store",    5e17 },                  // 500 Qa
            { "Berry Picking",   2e19 },                  // 20 Qi
            { "Trick or Treat",  6e22 },                  // 60 Sx
            { "Slot Machine",    5e24 },                  // 5 Sp
            { "To the Moon",     8e26 },                  // 800 Sp
            { "Booster Pack",    3e28 },                  // 30 Oc
            { "Final Chance",    1e30 },                  // 1 No
        };

        // Scales cash injections to the most expensive ticket that is both AP-received
        // and visible in the shop this run/prestige, not whatever the most expensive
        // AP-received ticket happens to be since AP items can arrive well before their
        // catalog naturally opens, pricing an injection off a Catalog 4 ticket while
        // the player is still stuck in Catalog 1.
        private static double MostExpensiveAccessibleTicketCost()
        {
            double maxCost = 1.0; // Day Job ($1), always accessible baseline

            var ticketShop = UnityEngine.Object.FindObjectOfType<TicketShop>(true);
            if (ticketShop == null) return maxCost;

            var received = TrackingManager.GetReceivedItemCounts();

            foreach (var kvp in TicketCosts)
            {
                if (!received.TryGetValue($"Unlock {kvp.Key}", out int cnt) || cnt < 1) continue;
                if (!ticketShop.shopPanelDict.ContainsKey(kvp.Key)) continue;
                if (kvp.Value > maxCost) maxCost = kvp.Value;
            }
            return maxCost;
        }

        private static void ApplyScaledCash(double multiplier)
        {
            double ticketCost = MostExpensiveAccessibleTicketCost();
            ApplyCash(ticketCost * multiplier);
        }

        // Testing shortcut for the F7 hotkey. Grants the same
        // amount a "Small Cash Injection" item would right now.
        public static void DebugGrantSmallCashInjection()
        {
            ApplyScaledCash(10.0);
        }

        private static void ApplyCash(double amount)
        {
            var wallet = UnityEngine.Object.FindObjectOfType<PlayerWallet>();
            if (wallet == null)
            {
                Plugin.Log.LogWarning($"AP ItemApplicator: PlayerWallet not found, cannot add {amount}");
                return;
            }
            Plugin.Log.LogInfo($"AP ItemApplicator: Adding ${amount} to wallet");
            wallet.AddMoney(amount, "Archipelago");
        }

        // Traps
        // Debt Trap: remove 25% of current balance via SetMoney.
        // Loan Trap: spawns a loan via Player.SpawnLoanWithDialogue.
        private static void ApplyDebtTrap()
        {
            var wallet = UnityEngine.Object.FindObjectOfType<PlayerWallet>();
            if (wallet == null)
            {
                Plugin.Log.LogWarning("AP ItemApplicator: PlayerWallet not found, cannot apply Debt Trap");
                return;
            }
            double reduced = wallet.Money * 0.75;
            Plugin.Log.LogInfo($"AP ItemApplicator: Debt Trap — reducing balance from {wallet.Money:F0} to {reduced:F0}");
            wallet.SetMoney(reduced);
        }

        private static void ApplyLoanTrap()
        {
            try
            {
                var player = UnityEngine.Object.FindObjectOfType<Player>(true);
                if (player == null)
                    throw new Exception("Player not found");

                Plugin.Log.LogInfo("AP ItemApplicator: Loan Trap — spawning loan with dialogue");
                player.SpawnLoanWithDialogue();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"AP ItemApplicator: Loan Trap failed ({ex.Message}) — falling back to Debt Trap effect");
                ApplyDebtTrap();
            }
        }

        // Temporary streak effects
        // ScratchLuck/LuckReduction reset on PopulateShop, so bonuses are
        // re-applied in ReapplyTemporaryEffects. Stacking extends duration.
        private static void ApplyLuckyStreak()
        {
            _luckyStreakTickets += 3;
            var scratching = UnityEngine.Object.FindObjectOfType<PlayerScratching>(true);
            if (scratching != null)
                scratching.ScratchLuck += STREAK_LUCK_AMOUNT;
            Plugin.Log.LogInfo($"AP ItemApplicator: Lucky Streak — ScratchLuck +{STREAK_LUCK_AMOUNT} for {_luckyStreakTickets} ticket(s)");
        }

        private static void ApplyUnluckyStreak()
        {
            _unluckyStreakTickets += 3;
            var scratching = UnityEngine.Object.FindObjectOfType<PlayerScratching>(true);
            if (scratching != null)
                scratching.LuckReduction += STREAK_LUCK_AMOUNT;
            Plugin.Log.LogInfo($"AP ItemApplicator: Unlucky Streak — LuckReduction +{STREAK_LUCK_AMOUNT} for {_unluckyStreakTickets} ticket(s)");
        }

        // Called from Patch_PopulateShop after ApplyAll resets the base luck values.
        public static void ReapplyTemporaryEffects()
        {
            if (_luckyStreakTickets == 0 && _unluckyStreakTickets == 0) return;
            var scratching = UnityEngine.Object.FindObjectOfType<PlayerScratching>(true);
            if (scratching == null) return;

            if (_luckyStreakTickets > 0)
            {
                scratching.ScratchLuck += STREAK_LUCK_AMOUNT;
                Plugin.Log.LogInfo($"AP ItemApplicator: Lucky Streak re-applied — {_luckyStreakTickets} ticket(s) remaining");
            }
            if (_unluckyStreakTickets > 0)
            {
                scratching.LuckReduction += STREAK_LUCK_AMOUNT;
                Plugin.Log.LogInfo($"AP ItemApplicator: Unlucky Streak re-applied — {_unluckyStreakTickets} ticket(s) remaining");
            }
        }

        // Called from Patch_OnTicketCashedOut after each cash-out.
        public static void OnTicketCashedForStreaks()
        {
            if (_luckyStreakTickets > 0)
            {
                _luckyStreakTickets--;
                if (_luckyStreakTickets == 0)
                {
                    Plugin.Log.LogInfo("AP ItemApplicator: Lucky Streak expired");
                    var scratching = UnityEngine.Object.FindObjectOfType<PlayerScratching>(true);
                    if (scratching != null)
                        scratching.ScratchLuck = Math.Max(0, scratching.ScratchLuck - STREAK_LUCK_AMOUNT);
                }
            }
            if (_unluckyStreakTickets > 0)
            {
                _unluckyStreakTickets--;
                if (_unluckyStreakTickets == 0)
                {
                    Plugin.Log.LogInfo("AP ItemApplicator: Unlucky Streak expired");
                    var scratching = UnityEngine.Object.FindObjectOfType<PlayerScratching>(true);
                    if (scratching != null)
                        scratching.LuckReduction = Math.Max(0, scratching.LuckReduction - STREAK_LUCK_AMOUNT);
                }
            }
        }

    }
}
