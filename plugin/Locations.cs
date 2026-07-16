using System.Collections.Generic;

namespace ScritchyScratchyAP
{
    public static class Locations
    {
        public const long BASE_ID = 777000000;

        // Goal:
        // Cashing out Final Chance_Win is the AP goal condition.
        // It is NOT in the location table, it triggers game completion.
        public const string GOAL_TICKET = "Final Chance_Win";

        // Ticket cash-out thresholds
        // Base tickets: checks at cash-outs 1, 5, 10, 15
        // Final Chance: gets 4 dedicated one-time locations instead
        // Loans: excluded from location pool (except the one achievement)
        public static readonly string[] BaseTickets = new[]
        {
            "Day Job",
            "Two Win",
            "Mini Scratch",
            "Apple Tree",
            "Quick Cash",
            "Lucky Cat",
            "Sand Dollars",
            "Scratch My Back",
            "Snake Eyes",
            "The Bomb",
            "Bank Break",
            "Xmas Countdown",
            "Thrift Store",
            "Berry Picking",
            "Trick or Treat",
            "Slot Machine",
            "To the Moon",
            "Booster Pack",
            "Final Chance",      // Cash-out locations handled separately
        };

        // Super tickets: checks at cash-outs 1, 5, 10, 15
        // Super_Final Chance_Win counts toward goal, not a location
        public static readonly string[] SuperTickets = new[]
        {
            "Super_Day Job",
            "Super_Two Win",
            "Super_Mini Scratch",
            "Super_Apple Tree",
            "Super_Quick Cash",
            "Super_Lucky Cat",
            "Super_Sand Dollars",
            "Super_Scratch My Back",
            "Super_Snake Eyes",
            "Super_The Bomb",
            "Super_Bank Break",
            "Super_Xmas Countdown",
            "Super_Thrift Store",
            "Super_Berry Picking",
            "Super_Trick or Treat",
            "Super_Slot Machine",
            "Super_To the Moon",
            "Super_Booster Pack",
            "Super_Final Chance_Win",
        };

        public static readonly int[] TicketCashOutThresholds = { 1, 5, 10, 15 };

        // Final Chance variants each get one dedicated, one-time location instead of
        // sharing a combined cash-out counter/threshold like every other ticket. Fires
        // the first time that specific variant is cashed out. Reuses "Final Chance"'s
        // reserved 10-ID block in the ticket cash-out ID range.
        public static readonly string[] FinalChanceCashOutLocationNames = new[]
        {
            "Cash Out Final Chance",
            "Cash Out Final Chance 2",
            "Cash Out Final Chance 3",
            "Cash Out Final Chance 4",
        };

        // Maps each ticket to the ticket that must already be AP-unlocked before
        // this one is shown as available in the shop. Enforces catalog-order display
        // even when AP delivers items out of natural game progression order.
        public static readonly Dictionary<string, string> TicketPredecessors = new()
        {
            { "Mini Scratch",    "Two Win" },
            { "Apple Tree",      "Mini Scratch" },
            { "Quick Cash",      "Apple Tree" },
            { "Lucky Cat",       "Quick Cash" },
            { "Sand Dollars",    "Lucky Cat" },      // cross-catalog: C2 start requires C1 end
            { "Scratch My Back", "Sand Dollars" },
            { "Snake Eyes",      "Scratch My Back" },
            { "The Bomb",        "Snake Eyes" },
            { "Bank Break",      "The Bomb" },
            { "Xmas Countdown",  "Bank Break" },     // within C3: ordering
            { "Thrift Store",    "Xmas Countdown" },
            { "Berry Picking",   "Thrift Store" },
            { "Trick or Treat",  "Berry Picking" },
            { "Slot Machine",    "Trick or Treat" },  // within C4: ordering
            { "To the Moon",     "Slot Machine" },
            { "Booster Pack",    "To the Moon" },
            // Final Chance has no predecessor here, it's gated solely by its own
            // "Unlock Final Chance" AP item, not by an ancestor ticket. Final Chance
            // is itself the mechanism that reveals Catalog 2 in vanilla play, so
            // requiring Booster Pack (Catalog 4 ticket) first created a circular
            // dependency that permanently softlocked catalog progression.
        };

        // Upgrade purchases
        // MaxLevel=1: single check on first purchase
        // MaxLevel=2: checks at level 1 and 2 (Scratch Size)
        // MaxLevel=N: one check per level from 1 to N

        // Single-purchase upgrades (MaxLevel=1)
        public static readonly string[] SinglePurchaseUpgrades = new[]
        {
            "The Machine",
            "Tin Coin",
            "Aluminum Coin",
            "Copper Coin",
            "Bronze Coin",
            "Iron Coin",
            "Steel Coin",
            "Titanium Coin",
            "Tungsten Coin",
            "Trash Can",
            "Scratch Bot",
            "Fan",
            "Sticky Mat",
            "Badge Collection",
            "Mundo",
            "Spell Book",
            "Subscription Bot",
            "Egg Timer",
        };

        // Gadget sub-upgrades that require the base gadget to be unlocked first
        public static readonly Dictionary<string, string> MultiLevelUpgradePrerequisites = new()
        {
            { "Scratch Bot Speed",    "Scratch Bot" },
            { "Scratch Bot Capacity", "Scratch Bot" },
            { "Scratch Bot Strength", "Scratch Bot" },
            { "Fan Speed",            "Fan" },
            { "Fan Battery",          "Fan" },
            { "Mundo Speed",          "Mundo" },
            { "Spell Charge Speed",   "Spell Book" },
            { "Timer Capacity",       "Egg Timer" },
            { "Timer Charge",         "Egg Timer" },
            { "Warp Speed",           "The Machine" },
        };

        // Multi-level upgrades — one check per level from 1 to max
        // Format: (upgradeId, maxLevel)
        // Scratch Size uses Buy(-1)/Buy(1) via ApplyShopUpgradeToLevel (max=2 per coin tier)
        public static readonly (string id, int max)[] MultiLevelUpgrades =
        {
            ("Scratch Luck",              45),
            ("Scratch Bot Speed",         30),
            ("Scratch Bot Capacity",      10),
            ("Scratch Bot Strength",      20),
            ("Fan Speed",                  5),
            ("Fan Battery",                5),
            ("Mundo Speed",               10),
            ("Spell Charge Speed",        10),
            ("Buying Speed",              10),
            ("Timer Capacity",            10),
            ("Timer Charge",              10),
            ("Warp Speed",                 3),
            ("Scratch Size Base Coin",     2),
            ("Scratch Size Tin Coin",      2),
            ("Scratch Size Aluminum Coin", 2),
            ("Scratch Size Copper Coin",   2),
            ("Scratch Size Bronze Coin",   2),
            ("Scratch Size Iron Coin",     2),
            ("Scratch Size Steel Coin",    2),
            ("Scratch Size Titanium Coin", 2),
            ("Scratch Size Tungsten Coin", 2),
        };

        // How many "Progressive {id}" copies actually exist
        // in the AP item pool for each multi-level upgrade
        public static readonly Dictionary<string, int> ProgressiveGatedLevels = new()
        {
            { "Scratch Luck",              45 },
            { "Buying Speed",              10 },
            { "Warp Speed",                 3 },
            { "Scratch Bot Speed",         30 },
            { "Scratch Bot Capacity",      10 },
            { "Scratch Bot Strength",      20 },
            { "Fan Speed",                  5 },
            { "Fan Battery",                5 },
            { "Mundo Speed",               10 },
            { "Spell Charge Speed",        10 },
            { "Timer Capacity",            10 },
            { "Timer Charge",              10 },
            { "Scratch Size Base Coin",     2 },
            { "Scratch Size Tin Coin",      2 },
            { "Scratch Size Aluminum Coin", 2 },
            { "Scratch Size Copper Coin",   2 },
            { "Scratch Size Bronze Coin",   2 },
            { "Scratch Size Iron Coin",     2 },
            { "Scratch Size Steel Coin",    2 },
            { "Scratch Size Titanium Coin", 2 },
            { "Scratch Size Tungsten Coin", 2 },
        };

        // Achievements
        // All 34 achievements are locations.
        // Achievement Hunter (get all achievements) is included,
        // it will naturally be the last check completed.
        public static readonly string[] Achievements = new[]
        {
            "Death_1",
            "Death_2",
            "Death_3",
            "Death_4",
            "Take Loan",
            "Super Jackpot",
            "Bad Luck",
            "Jackpot on First Ticket",
            "Trash Jackpot",
            "Bad kitty",
            "Lucky cat",
            "Win your job",
            "One of each please",
            "Winning streak",
            "Skip a catalogue",
            "Spend all the worlds money",
            "Nap time",
            "Lucky ticket",
            "Big win",
            "High level gambling",
            "Clicker minigame",
            "Idle game",
            "Visit the Night Market",
            "Walk-in-closet",
            "Honest work",
            "Workaholic",
            "Soul Siphon",
            "Wizard",
            "Time machine",
            "Scratch Final Chance Without Dying",
            "Faithful Servant",
            "Speedrun",
            "Max out skill tree",
            "Achievement Hunter",
        };

        // -------------------------------------------------------
        // Location ID Table
        // Generated from the arrays above with a fixed ID scheme:
        //
        // BASE_ID + 0    : base ticket cash-out locations  (19 tickets × 10 slots = 0–189)
        // BASE_ID + 200  : super ticket cash-out locations (19 tickets × 10 slots = 200–389)
        // BASE_ID + 400  : single-purchase upgrade locations (400–417)
        // BASE_ID + 600  : multi-level upgrade locations, one per level (600–785),
        //                  includes Scratch Size (9 × 2 = 18 levels, offsets 168–185)
        // BASE_ID + 900  : achievement locations (900–933)
        //
        // Each ticket block reserves 10 IDs (only 4 used per the thresholds above).
        // Final Chance's block is a special case: instead of j = threshold index, its
        // 4 slots are used for the 4 dedicated FinalChanceCashOutLocationNames entries.
        // -------------------------------------------------------
        // -------------------------------------------------------
        // PRESTIGE PERKS
        // -------------------------------------------------------
        public const int PrestigeLocationCount = 3;

        public static readonly string[] PrestigeSinglePerks = new[]
        {
            "Starter Kit",
            "Electric Fan",
            "Air Condition",
            "Pet Lover",
            "Dishwasher",
            "Magic",
            "Shopping Spree",
            "Loan Shark",
            "Picky Eater",
            "Fully Automated",
            "Fine Dining",
            "PlateMaster5000",
            "Hotkeys",
        };

        public static readonly (string id, int max)[] PrestigeMultiPerks =
        {
            ("Jackpot Power",          5),
            ("Tool Belt",              5),
            ("Self Made Millionaire", 10),
            ("Booster Kit",            5),
            ("Recycling",              5),
            ("Less is More",          10),
            ("Ignorance is Bliss",     5),
            ("Big Winner",             7),
            ("Completionist",          5),
            ("Clean Freak",           10),
            ("Smart Investment",       5),
            ("Learn by Doing",         5),
            ("Refund",                10),
            ("Soft Hands",             5),
            ("Collector",             10),
            ("Experienced",            5),
            ("Built Different",        5),
        };

        public static readonly Dictionary<string, long> LocationIds;

        static Locations()
        {
            LocationIds = new Dictionary<string, long>();

            // Base ticket cash-out thresholds, j mirrors enumerate(TICKET_THRESHOLDS) in Python
            long ticketBase = BASE_ID + 0;
            for (int i = 0; i < BaseTickets.Length; i++)
            {
                if (BaseTickets[i] == "Final Chance")
                {
                    // 4 dedicated one-time locations instead of shared thresholds,
                    // reuses this entry's reserved 10-ID block.
                    for (int v = 0; v < FinalChanceCashOutLocationNames.Length; v++)
                        LocationIds[FinalChanceCashOutLocationNames[v]] = ticketBase + (i * 10) + v;
                    continue;
                }
                for (int j = 0; j < TicketCashOutThresholds.Length; j++)
                {
                    string name = $"Cash Out {BaseTickets[i]} {TicketCashOutThresholds[j]}";
                    LocationIds[name] = ticketBase + (i * 10) + j;
                }
            }

            // Super ticket cash-out thresholds
            long superBase = BASE_ID + 200;
            for (int i = 0; i < SuperTickets.Length; i++)
            {
                for (int j = 0; j < TicketCashOutThresholds.Length; j++)
                {
                    string name = $"Cash Out {SuperTickets[i]} {TicketCashOutThresholds[j]}";
                    LocationIds[name] = superBase + (i * 10) + j;
                }
            }

            // Single-purchase upgrades
            long singleBase = BASE_ID + 400;
            for (int i = 0; i < SinglePurchaseUpgrades.Length; i++)
            {
                string name = $"Buy {SinglePurchaseUpgrades[i]}";
                LocationIds[name] = singleBase + i;
            }

            // Multi-level upgrades, one ID per level
            long multiBase = BASE_ID + 600;
            int offset = 0;
            foreach (var (upgradeId, max) in MultiLevelUpgrades)
            {
                for (int level = 1; level <= max; level++)
                {
                    string name = $"Buy {upgradeId} Level {level}";
                    LocationIds[name] = multiBase + offset + (level - 1);
                }
                offset += max;
            }

            // Prestige event locations
            long prestigeBase = BASE_ID + 1000;
            for (int i = 0; i < PrestigeLocationCount; i++)
                LocationIds[$"Prestige {i + 1}"] = prestigeBase + i;

            // Single prestige perk purchase locations
            long singlePerkBase = BASE_ID + 1100;
            for (int i = 0; i < PrestigeSinglePerks.Length; i++)
                LocationIds[$"Buy Prestige Perk {PrestigeSinglePerks[i]}"] = singlePerkBase + i;

            // Multi-level prestige perk purchase locations, one ID per level
            long multiPerkBase = BASE_ID + 1200;
            int perkOffset = 0;
            foreach (var (perkId, max) in PrestigeMultiPerks)
            {
                for (int level = 1; level <= max; level++)
                    LocationIds[$"Buy Prestige Perk {perkId} Level {level}"] = multiPerkBase + perkOffset + (level - 1);
                perkOffset += max;
            }

            // Achievements
            long achBase = BASE_ID + 900;
            for (int i = 0; i < Achievements.Length; i++)
            {
                string name = $"Achievement: {Achievements[i]}";
                LocationIds[name] = achBase + i;
            }
        }

        // Helper methods

        public static long? GetLocationId(string locationName)
        {
            if (LocationIds.TryGetValue(locationName, out long id))
                return id;
            Plugin.Log.LogWarning($"AP: No location ID found for '{locationName}'");
            return null;
        }

        // Returns true if the given ticket SharedID is a Final Chance variant
        public static bool IsFinalChanceVariant(string sharedId)
        {
            return sharedId == "Final Chance" ||
                   sharedId == "Final Chance_2" ||
                   sharedId == "Final Chance_3" ||
                   sharedId == "Final Chance_4";
        }

        // Returns true if this ticket cash-out should trigger the AP goal
        public static bool IsGoalTicket(string sharedId)
        {
            return sharedId == GOAL_TICKET || sharedId == "Super_Final Chance_Win";
        }

        // Maps a Final Chance variant's SharedID to its own dedicated cash-out
        // location name. Returns null for anything that isn't a Final Chance variant.
        public static string GetFinalChanceCashOutLocationName(string sharedId)
        {
            switch (sharedId)
            {
                case "Final Chance":   return "Cash Out Final Chance";
                case "Final Chance_2": return "Cash Out Final Chance 2";
                case "Final Chance_3": return "Cash Out Final Chance 3";
                case "Final Chance_4": return "Cash Out Final Chance 4";
                default: return null;
            }
        }

        public static int GetMaxLevel(string upgradeId)
        {
            foreach (var (id, max) in MultiLevelUpgrades)
                if (id == upgradeId) return max;
            return 1;
        }
    }
}