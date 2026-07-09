

using HarmonyLib;
using System;

namespace ScritchyScratchyAP
{
    // Fires when a ticket is cashed out
    [HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.OnTicketCashedOut))]
    public class Patch_OnTicketCashedOut
    {
        static void Postfix(Ticket ticket)
        {
            string sharedId = ticket.Data.SharedID;
            TrackingManager.OnTicketCashedOut(sharedId);
            ItemApplicator.OnTicketCashedForStreaks();
        }
    }

    // Shared pending-check state between Patch_ShopTryBuy and Patch_ShopBuy.
    internal static class ShopBuyPending
    {
        public static bool IsManualProgressiveBuy;
        public static string PendingCheck;
        public static string PendingApId;

        public static void Clear()
        {
            IsManualProgressiveBuy = false;
            PendingCheck = null;
            PendingApId = null;
        }
    }

    // Fires when the player clicks a shop button.
    [HarmonyPatch(typeof(ShopPanel), nameof(ShopPanel.TryBuy))]
    public class Patch_ShopTryBuy
    {
        static bool Prefix(ShopPanel __instance, int __0, ref bool __result)
        {
            string id = __instance.Data.id;

            // Day Job, no AP gating needed.
            if (id == "Day Job") return true;

            // AP-triggered inner calls must pass through untouched.
            if (TrackingManager.IsApplyingReceivedItem && id == ItemApplicator.CurrentlyApplyingId)
                return true;

            if (!ArchipelagoManager.Connected) return true;

            var received = TrackingManager.GetReceivedItemCounts();

            // Tickets
            if (Array.IndexOf(Locations.BaseTickets, id) >= 0)
            {
                if (!received.TryGetValue($"Unlock {id}", out int cnt) || cnt < 1)
                {
                    Plugin.Log.LogWarning($"AP: Blocked ticket purchase of '{id}' — not yet received from AP");
                    __result = false;
                    return false;
                }
                return true;
            }

            // Final Chance Variants 2-4
            // Not in Locations.BaseTickets, each variant has its own dedicated
            // cash-out location and its own separate AP unlock item, since each
            // is what naturally reveals the next catalog in vanilla play.
            if (ItemApplicator.FinalChanceVariantUnlockItems.TryGetValue(id, out string requiredFcItem))
            {
                if (!received.TryGetValue(requiredFcItem, out int fcCnt) || fcCnt < 1)
                {
                    Plugin.Log.LogWarning($"AP: Blocked ticket purchase of '{id}' — '{requiredFcItem}' not yet received from AP");
                    __result = false;
                    return false;
                }
                return true;
            }

            // Single-Purchase Upgrades
            if (Array.IndexOf(Locations.SinglePurchaseUpgrades, id) >= 0)
            {
                if (!received.TryGetValue($"Unlock {id}", out int unlockCnt) || unlockCnt < 1)
                {
                    Plugin.Log.LogWarning($"AP: Blocked upgrade '{id}' — 'Unlock {id}' not yet received");
                    __result = false;
                    return false;
                }
                // Let TryBuy run, it handles the real affordability check and deduction
                // itself. Once granted by AP, "Unlock X" authorizes the purchase for good.
                // Coins/upgrades reset to unbought on prestige and must be re-purchased
                // with currency each time. We only resend the AP check the first time,
                // checking IsLocationSent here (not above) would otherwise permanently
                // block every future re-buy after the first prestige, since the location
                // stays "sent" forever.
                string checkName = $"Buy {id}";
                if (!TrackingManager.IsLocationSent(checkName))
                {
                    ShopBuyPending.PendingCheck = checkName;
                    Plugin.Log.LogInfo($"AP ShopBuy: single-purchase '{id}' — letting TryBuy run, check='{checkName}'");
                }
                else
                {
                    Plugin.Log.LogInfo($"AP ShopBuy: single-purchase '{id}' — check already sent, letting TryBuy run without resending (post-prestige re-buy)");
                }
                return true;
            }

            // Progressive Multi-Level Upgrades (includes Scratch Size)
            // Scratch Size panel IDs use underscore in-game ("Scratch Size_Base Coin");
            // AP names use a space, so normalize before comparing to MultiLevelUpgrades.
            string apId = id.Replace("Scratch Size_", "Scratch Size ");
            foreach (var (upgradeId, max) in Locations.MultiLevelUpgrades)
            {
                if (upgradeId != apId) continue;

                if (Locations.MultiLevelUpgradePrerequisites.TryGetValue(id, out string reqGadget))
                {
                    if (!received.TryGetValue($"Unlock {reqGadget}", out int gc) || gc < 1)
                    {
                        Plugin.Log.LogWarning($"AP: Blocked upgrade '{id}' — requires 'Unlock {reqGadget}'");
                        __result = false;
                        return false;
                    }
                }

                // The displayed level resets to 1 on every prestige, so the check we award
                // must track the real current level of this prestige cycle, not "the
                // next AP check nobody's claimed yet". Otherwise the very first re-buy
                // after a prestige would fire whatever high-numbered check happened to
                // be next in the global backlog even though the player is nowhere near there.
                int cycleLevel = TrackingManager.GetProgressiveCycleLevel(apId) + 1;
                if (cycleLevel > max) cycleLevel = max;
                string levelCheck = $"Buy {apId} Level {cycleLevel}";

                ShopBuyPending.PendingApId = apId;
                ShopBuyPending.IsManualProgressiveBuy = true;
                if (!TrackingManager.IsLocationSent(levelCheck))
                {
                    ShopBuyPending.PendingCheck = levelCheck;
                    Plugin.Log.LogInfo($"AP ShopBuy: manual progressive buy — id='{id}' apId='{apId}' cycleLevel={cycleLevel} check='{levelCheck}'");
                }
                else
                {
                    ShopBuyPending.PendingCheck = null;
                    Plugin.Log.LogInfo($"AP ShopBuy: manual progressive buy — id='{id}' apId='{apId}' cycleLevel={cycleLevel} already sent, no resend (post-prestige re-buy)");
                }
                return true;
            }

            return true;
        }

        // TryBuy can still fail on its own from insufficient funds even after gating lets it
        // through. When it does, Buy() is never reached, so clear any pending state here to
        // stop it leaking into an unrelated later purchase.
        static void Postfix(ShopPanel __instance, bool __result)
        {
            if (!__result)
            {
                if (ShopBuyPending.PendingCheck != null)
                    Plugin.Log.LogInfo($"AP ShopBuy: TryBuy declined for '{__instance.Data.id}' (insufficient funds) — clearing pending check.");
                ShopBuyPending.Clear();
            }
        }
    }

    // Fires only once TryBuy's own real affordability check and deduction already
    // succeeded, so no wallet manipulation needed, just send the check and mark
    // the level applied.
    [HarmonyPatch(typeof(ShopPanel), nameof(ShopPanel.Buy))]
    public class Patch_ShopBuy
    {
        static void Postfix(ShopPanel __instance, int __0)
        {
            string postfixId = __instance.Data.id;

            if (TrackingManager.IsApplyingReceivedItem && postfixId == ItemApplicator.CurrentlyApplyingId)
                return;

            if (ShopBuyPending.IsManualProgressiveBuy)
            {
                string check = ShopBuyPending.PendingCheck;
                string apId = ShopBuyPending.PendingApId;
                ShopBuyPending.Clear();
                // Check is null when this cycle's level was already sent in an earlier
                // prestige cycle. The purchase still happened (currency spent, level advanced)
                // but there's no new AP check to award.
                if (check != null)
                {
                    TrackingManager.TrySendCheck(check);
                    Plugin.Log.LogInfo($"AP: Check '{check}' sent (progressive upgrade)");
                }
                int newCycleLevel = TrackingManager.IncrementProgressiveCycleLevel(apId);
                Plugin.Log.LogInfo($"AP: '{apId}' cycle level now {newCycleLevel}");
                ItemApplicator.TrackManualApply(apId);
                return;
            }

            if (ShopBuyPending.PendingCheck != null)
            {
                string check = ShopBuyPending.PendingCheck;
                ShopBuyPending.Clear();
                TrackingManager.TrySendCheck(check);
                Plugin.Log.LogInfo($"AP: Check '{check}' sent (single-purchase upgrade)");
                // Auto-completes the previous coin tier's Scratch Size checks so they
                // don't get permanently softlocked.
                TrackingManager.OnUpgradePurchased(postfixId, __0);
                return;
            }

            TrackingManager.OnUpgradePurchased(postfixId, __0);
        }
    }

    // Fires when an achievement is triggered
    [HarmonyPatch(typeof(AchievementManager), nameof(AchievementManager.TriggerAchievement))]
    public class Patch_TriggerAchievement
    {
        static void Postfix(string __0)
        {
            TrackingManager.OnAchievementTriggered(__0);

        }
    }

    // Re-applies all received AP items whenever the shop is re-populated,
    // then locks any single-purchase upgrades not yet granted by AP.
    [HarmonyPatch(typeof(UpgradeShop), nameof(UpgradeShop.PopulateShop))]
    public class Patch_PopulateShop
    {
        static void Postfix()
        {
            Plugin.Log.LogInfo("AP: Shop populated — re-applying received items, locking unapplied.");
            ItemApplicator.ApplyAll();
            ItemApplicator.LockUnapplied();
            ItemApplicator.ReapplyTemporaryEffects();
        }
    }

    // Re-applies all received AP items whenever the ticket shop is re-populated.
    // This fires after ShowCatalog expands the shop, ensuring newly visible catalog
    // panels get unlocked immediately rather than waiting for the next retry.
    [HarmonyPatch(typeof(TicketShop), nameof(TicketShop.PopulateShop))]
    public class Patch_TicketShopPopulateShop
    {
        static void Postfix()
        {
            Plugin.Log.LogInfo("AP: TicketShop populated — re-applying received items, locking unapplied.");
            ItemApplicator.ApplyAll();
            ItemApplicator.LockUnapplied();
        }
    }

    // TicketShop.PopulateShop() iterates all tickets and calls SpawnShopPanel for each,
    // but it never clears shopPanelDict first. When calling PopulateShop() manually after
    // ShowCatalog() to register newly-visible catalogs, it crashes with a duplicate key
    // exception on "Day Job". This Prefix skips SpawnShopPanel for panels already in the
    // dict, so PopulateShop only spawns the new catalog panels.
    [HarmonyPatch(typeof(TicketShop), "SpawnShopPanel")]
    public class Patch_TicketShopSpawnShopPanel
    {
        static bool Prefix(TicketShop __instance, ShopItemData __0)
        {
            bool skip = __instance.shopPanelDict.ContainsKey(__0.id);
            Plugin.Log.LogInfo($"AP TicketSpawnPanel: {(skip ? "SKIP(dup)" : "SPAWN")} '{__0.id}'");
            return !skip;
        }
    }

    // Fires when the player prestiges (dies at Final Chance).
    // Sends the "Prestige N" check for the N-th prestige (up to 5).
    [HarmonyPatch(typeof(PrestigeManager), nameof(PrestigeManager.Prestige))]
    public class Patch_Prestige
    {
        static void Postfix()
        {
            APUpdateManager.Enqueue(() => TrackingManager.OnPrestige());
        }
    }

    // Fires when the player buys a prestige perk.
    // Prefix: blocks purchase if the required AP item has not been received.
    // Postfix: sends the location check for the perk purchase.
    [HarmonyPatch(typeof(PrestigeUpgradePanel), nameof(PrestigeUpgradePanel.Buy))]
    public class Patch_PrestigePerkBuy
    {
        static bool Prefix(PrestigeUpgradePanel __instance)
        {
            if (!ArchipelagoManager.Connected) return true;
            if (TrackingManager.IsApplyingReceivedItem) return true;

            string perkId = __instance.Data.id;
            var received = TrackingManager.GetReceivedItemCounts();

            // Block single-purchase prestige perks not yet granted by AP
            if (Array.IndexOf(Locations.PrestigeSinglePerks, perkId) >= 0)
            {
                if (!received.TryGetValue($"Unlock {perkId}", out int cnt) || cnt < 1)
                {
                    Plugin.Log.LogWarning($"AP: Blocked prestige perk '{perkId}' — not yet received from AP");
                    return false;
                }
                return true;
            }

            // Block multi-level prestige perks beyond the AP-granted level
            foreach (var (id, _) in Locations.PrestigeMultiPerks)
            {
                if (id != perkId) continue;
                int apLevel = received.TryGetValue($"Progressive {perkId}", out int ap) ? ap : 0;
                int manualBought = 0;
                try { manualBought = SaveData.Current?.GetPrestigeUpgradeBuyCount(perkId) ?? 0; }
                catch { }
                if (manualBought + 1 > apLevel)
                {
                    Plugin.Log.LogWarning($"AP: Blocked prestige perk '{perkId}' level {manualBought + 1} — only {apLevel} AP levels received");
                    return false;
                }
                break;
            }

            return true;
        }

        static void Postfix(PrestigeUpgradePanel __instance)
        {
            if (TrackingManager.IsApplyingReceivedItem) return;
            string perkId = __instance.Data.id;
            try
            {
                int boughtCount = 0;
                try { boughtCount = SaveData.Current?.GetPrestigeUpgradeBuyCount(perkId) ?? 0; }
                catch { }
                APUpdateManager.Enqueue(() => TrackingManager.OnPrestigePerkBought(perkId, boughtCount));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP: Patch_PrestigePerkBuy.Postfix threw for '{perkId}': {ex.Message}");
            }
        }
    }
}