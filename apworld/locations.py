from dataclasses import dataclass
from typing import Optional
from BaseClasses import LocationProgressType


@dataclass
class SSLocationData:
    region: str
    address: Optional[int] = None  # None = event location
    progress_type: LocationProgressType = LocationProgressType.DEFAULT


BASE_ID = 777000000

# Ticket Types
BASE_TICKETS = [
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
    "Final Chance",
]

SUPER_TICKETS = [
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
]

TICKET_THRESHOLDS = [1, 5, 10, 15]
SUPER_TICKET_THRESHOLDS = [1]

# Final Chance variants each get one dedicated, one-time location instead of
# sharing a combined cash-out counter/threshold like every other ticket, fires
# the first time that specific variant is cashed out. Region matches whichever
# catalog the AP item for that variant actually becomes available in, not the
# catalog it happens to be listed under in CATALOG_4_TICKETS.
FINAL_CHANCE_CASH_OUT_LOCATIONS = [
    ("Cash Out Final Chance", "Catalog 1"),
    ("Cash Out Final Chance 2", "Catalog 2"),
    ("Cash Out Final Chance 3", "Catalog 3"),
    ("Cash Out Final Chance 4", "Catalog 4"),
]

# Catalog Groupings
# Mirrors the in-game catalog unlock order.
# Used for region assignments and entrance rules in __init__.py.
CATALOG_1_TICKETS = ["Two Win", "Mini Scratch", "Apple Tree", "Quick Cash", "Lucky Cat"]
CATALOG_2_TICKETS = ["Sand Dollars", "Scratch My Back", "Snake Eyes", "The Bomb"]
CATALOG_3_TICKETS = ["Bank Break", "Xmas Countdown", "Thrift Store", "Berry Picking"]
CATALOG_4_TICKETS = ["Trick or Treat", "Slot Machine", "To the Moon", "Booster Pack", "Final Chance"]

# Upgrades
SINGLE_PURCHASE_UPGRADES = [
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
]

# (upgrade_id, max_level), one check per level from 1 to max_level
# Scratch Bot Strength trimmed from 20 to 10 (matches items.py
# PROGRESSIVE_ITEM_COUNTS, item and location counts cut).
MULTI_LEVEL_UPGRADES = [
    ("Scratch Luck",         45),
    ("Scratch Bot Speed",    30),
    ("Scratch Bot Capacity", 10),
    ("Scratch Bot Strength", 10),
    ("Fan Speed",             5),
    ("Fan Battery",           5),
    ("Mundo Speed",          10),
    ("Spell Charge Speed",   10),
    ("Buying Speed",         10),
    ("Timer Capacity",       10),
    ("Timer Charge",         10),
    ("Warp Speed",            3),
    # Scratch Size: 2 levels per coin tier, applied via Buy(-1)/Buy(1)
    ("Scratch Size Base Coin",      2),
    ("Scratch Size Tin Coin",       2),
    ("Scratch Size Aluminum Coin",  2),
    ("Scratch Size Copper Coin",    2),
    ("Scratch Size Bronze Coin",    2),
    ("Scratch Size Iron Coin",      2),
    ("Scratch Size Steel Coin",     2),
    ("Scratch Size Titanium Coin",  2),
    ("Scratch Size Tungsten Coin",  2),
]

# Prestige Perks
PRESTIGE_LOCATION_COUNT = 4  # Number of prestige event check locations

# Single-purchase prestige perks
PRESTIGE_SINGLE_PERKS = [
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
]

# Multi-level prestige perks: (perk_id, max_level)
# Self Made Millionaire, Less is More, Clean Freak, Refund, and Collector were
# trimmed from 10 to 5 max levels. They were the most expensive (cumulative JP
# cost in the thousands) and among the most consistently implicated chains in
# Fill.FillError generation failures.
PRESTIGE_MULTI_PERKS = [
    ("Jackpot Power",          5),
    ("Tool Belt",              5),
    ("Self Made Millionaire",  5),
    ("Booster Kit",            5),
    ("Recycling",              5),
    ("Less is More",           5),
    ("Ignorance is Bliss",     5),
    ("Big Winner",             7),
    ("Completionist",          5),
    ("Clean Freak",            5),
    ("Smart Investment",       5),
    ("Learn by Doing",         5),
    ("Refund",                 5),
    ("Soft Hands",             5),
    ("Collector",              5),
    ("Experienced",            5),
    ("Built Different",        5),
]

# Which perks must be unlocked before each perk can be purchased
# Full transitive prerequisite chains (not just the immediate parent), a location's
# access_rule only checks item receipt, so listing just the immediate parent would let
# the generator hand out, e.g. Picky Eater's item without ever guaranteeing Booster Kit
# or Starter Kit are reachable, even though the perk tree UI requires the whole chain.
# Perks with branching (OR) prerequisites (Magic, Shopping Spree, Loan Shark, Hotkeys,
# Fine Dining, and PlateMaster5000) are intentionally left out of this dict and instead
# get explicit custom rules in rules.py.
PRESTIGE_PERK_PREREQUISITES: dict[str, list[str]] = {
    # Starter Kit branch
    "Booster Kit":            ["Starter Kit"],
    "Pet Lover":              ["Booster Kit", "Starter Kit"],
    "Picky Eater":            ["Pet Lover", "Booster Kit", "Starter Kit"],
    "Electric Fan":           ["Starter Kit"],
    "Air Condition":          ["Electric Fan", "Starter Kit"],
    "Fully Automated":        ["Air Condition", "Electric Fan", "Starter Kit"],
    "Completionist":          ["Starter Kit"],
    "Dishwasher":             ["Completionist", "Starter Kit"],
    # Tool Belt branch
    "Clean Freak":            ["Tool Belt"],
    "Soft Hands":             ["Clean Freak", "Tool Belt"],
    "Self Made Millionaire":  ["Tool Belt"],
    "Learn by Doing":         ["Self Made Millionaire", "Tool Belt"],
    "Built Different":        ["Learn by Doing", "Self Made Millionaire", "Tool Belt"],
    "Ignorance is Bliss":     ["Tool Belt"],
    # Jackpot Power branch
    "Less is More":           ["Jackpot Power"],
    "Refund":                 ["Less is More", "Jackpot Power"],
    "Collector":              ["Refund", "Less is More", "Jackpot Power"],
    "Recycling":              ["Jackpot Power"],
    "Smart Investment":       ["Recycling", "Jackpot Power"],
    "Experienced":            ["Smart Investment", "Recycling", "Jackpot Power"],
    "Big Winner":             ["Jackpot Power"],
}

ACHIEVEMENTS = [
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
]

# Regions
# Entrance rules in __init__.py gate each transition.
REGIONS = [
    "Menu",
    "Beginning",
    "Catalog 1",
    "Prestige Tier 1",
    "Catalog 2",
    "Prestige Tier 2",
    "Catalog 3",
    "Prestige Tier 3",
    "Catalog 4",
    "Lategame",
    "Endgame",
]


# Region Assignment Helper Functions
_TICKET_REGION: dict[str, str] = {
    "Day Job": "Beginning",
    **{t: "Catalog 1" for t in CATALOG_1_TICKETS},
    **{t: "Catalog 2" for t in CATALOG_2_TICKETS},
    **{t: "Catalog 3" for t in CATALOG_3_TICKETS},
    **{t: "Catalog 4" for t in CATALOG_4_TICKETS},
    # Final Chance is grouped with CATALOG_4_TICKETS above for other purposes
    # (achievement ticket lists, price ordering) but is NOT looked up here for its
    # own cash-out locations, those are handled separately in build_location_table
    # via FINAL_CHANCE_CASH_OUT_LOCATIONS, one dedicated region per variant.
}

_SINGLE_UPGRADE_REGION: dict[str, str] = {
    "The Machine":      "Catalog 4",
    "Tin Coin":         "Catalog 1",
    "Aluminum Coin":    "Catalog 2",
    "Copper Coin":      "Catalog 3",
    "Bronze Coin":      "Catalog 3",
    "Iron Coin":        "Catalog 4",
    "Steel Coin":       "Catalog 4",
    "Titanium Coin":    "Catalog 4",
    "Tungsten Coin":    "Catalog 4",
    "Trash Can":        "Catalog 1",
    "Scratch Bot":      "Catalog 4",
    "Fan":              "Catalog 4",
    "Sticky Mat":       "Catalog 2",
    "Badge Collection": "Catalog 2",
    "Mundo":            "Catalog 4",
    "Spell Book":       "Catalog 4",
    "Subscription Bot": "Catalog 3",
    "Egg Timer":        "Catalog 4",
}

_SCRATCH_SIZE_REGION: dict[str, str] = {
    "Scratch Size Base Coin":     "Beginning",
    "Scratch Size Tin Coin":      "Catalog 1",
    "Scratch Size Aluminum Coin": "Catalog 2",
    "Scratch Size Copper Coin":   "Catalog 3",
    "Scratch Size Bronze Coin":   "Catalog 3",
    "Scratch Size Iron Coin":     "Catalog 4",
    "Scratch Size Steel Coin":    "Catalog 4",
    "Scratch Size Titanium Coin": "Catalog 4",
    "Scratch Size Tungsten Coin": "Catalog 4",
}

# Gadget sub-upgrades
_GADGET_UPGRADE_LEVEL_REGIONS: dict[str, list[str]] = {
    "Scratch Bot Speed":    ["Catalog 1"] * 5 + ["Catalog 2"] * 9 + ["Catalog 3"] * 11 + ["Catalog 4"] * 5,
    "Scratch Bot Capacity": ["Catalog 1"] * 4 + ["Catalog 2"] * 6,
    "Fan Speed":            ["Catalog 1"] * 3 + ["Catalog 2"] * 2,
    "Fan Battery":          ["Catalog 1"] * 4 + ["Catalog 2"] * 1,
    "Mundo Speed":          ["Catalog 2"] * 6 + ["Catalog 3"] * 4,
    "Spell Charge Speed":   ["Catalog 3"] * 9 + ["Catalog 4"] * 1,
    "Timer Capacity":       ["Catalog 4"] * 7 + ["Lategame"] * 3,
    "Timer Charge":         ["Catalog 4"] * 7 + ["Lategame"] * 3,
}

# Prestige perk region assignment: prerequisite chain depth sets a floor
# tier (a perk can never be reachable before its own prerequisites are),
# and any individual level whose own JP price is >= PERK_PRICE_THRESHOLD
# is pushed to Lategame regardless of depth, since it's realistically only
# affordable after many prestige cycles' worth of accumulated Jackpot Points.
# Depth-4+ perks (two or more prerequisite perks deep) go straight to Lategame
# even when individually cheap, since their own prerequisites already can't
# resolve earlier than that.
PERK_PRICE_THRESHOLD = 251

_PRESTIGE_SINGLE_PERK_REGION: dict[str, str] = {
    "Starter Kit":      "Prestige Tier 1",
    "Electric Fan":     "Prestige Tier 2",
    "Air Condition":    "Prestige Tier 3",
    "Pet Lover":        "Prestige Tier 3",
    "Dishwasher":       "Prestige Tier 3",
    "Magic":            "Prestige Tier 3",
    "Shopping Spree":   "Prestige Tier 3",
    "Loan Shark":       "Lategame",
    "Picky Eater":      "Lategame",
    "Fully Automated":  "Lategame",
    "Fine Dining":      "Lategame",
    "PlateMaster5000":  "Lategame",
    "Hotkeys":          "Lategame",
}

# One region per level (index 0 = level 1). Cheap early levels of a perk can
# land in an earlier tier than its own most expensive levels, same principle
# already used for Scratch Luck.
_PRESTIGE_MULTI_PERK_LEVEL_REGIONS: dict[str, list[str]] = {
    "Jackpot Power":         ["Prestige Tier 1"] * 5,
    "Tool Belt":             ["Prestige Tier 1"] * 5,
    "Booster Kit":           ["Prestige Tier 2"] * 5,
    "Recycling":             ["Prestige Tier 2"] * 5,
    "Less is More":          ["Prestige Tier 2"] * 10,
    "Ignorance is Bliss":    ["Prestige Tier 2"] * 5,
    "Big Winner":            ["Prestige Tier 2"] * 7,
    "Completionist":         ["Prestige Tier 2"] * 5,
    "Self Made Millionaire": ["Prestige Tier 2"] * 7 + ["Lategame"] * 3,
    "Clean Freak":           ["Prestige Tier 2"] * 8 + ["Lategame"] * 2,
    "Smart Investment":      ["Prestige Tier 3"] * 5,
    "Soft Hands":            ["Prestige Tier 3"] * 5,
    "Learn by Doing":        ["Prestige Tier 3"] * 4 + ["Lategame"] * 1,
    "Refund":                ["Prestige Tier 3"] * 8 + ["Lategame"] * 2,
    "Collector":             ["Lategame"] * 10,
    "Experienced":           ["Lategame"] * 5,
    "Built Different":       ["Lategame"] * 5,
}

_ACHIEVEMENT_REGION: dict[str, str] = {
    "Death_1":                           "Catalog 4",
    "Death_2":                           "Catalog 4",
    "Death_3":                           "Catalog 4",
    "Death_4":                           "Catalog 4",
    "Take Loan":                         "Catalog 2",
    "Super Jackpot":                     "Catalog 1",
    "Bad Luck":                          "Catalog 1",
    "Jackpot on First Ticket":           "Catalog 1",
    "Trash Jackpot":                     "Catalog 1",
    "Bad kitty":                         "Catalog 1",
    "Lucky cat":                         "Catalog 1",
    "Win your job":                      "Catalog 1",
    "One of each please":                "Catalog 2",
    "Winning streak":                    "Catalog 1",
    "Skip a catalogue":                  "Catalog 2",
    "Spend all the worlds money":        "Catalog 2",
    "Nap time":                          "Catalog 1",
    "Lucky ticket":                      "Catalog 1",
    "Big win":                           "Catalog 2",
    "High level gambling":               "Catalog 3",
    "Clicker minigame":                  "Catalog 2",
    "Idle game":                         "Catalog 4",
    "Visit the Night Market":            "Catalog 2",
    "Walk-in-closet":                    "Catalog 3",
    "Honest work":                       "Catalog 2",
    "Workaholic":                        "Catalog 3",
    "Soul Siphon":                       "Catalog 4",
    "Wizard":                            "Catalog 4",
    "Time machine":                      "Lategame",
    "Scratch Final Chance Without Dying":"Catalog 4",
    "Faithful Servant":                  "Catalog 4",
    "Speedrun":                          "Lategame",
    "Max out skill tree":                "Endgame",
    "Achievement Hunter":                "Endgame",
}


def _scratch_luck_region(level: int) -> str:
    if level <= 1:  return "Beginning"
    if level <= 5:  return "Catalog 1"
    if level <= 10: return "Catalog 2"
    if level <= 20: return "Catalog 3"
    if level <= 30: return "Catalog 4"
    # Levels beyond 30 are money-based, tied to real in-game cost past
    # Catalog 4's own max requirement, not to the JP-based Prestige
    # Tier system (which sits chronologically before Catalog 4 is even
    # reached), so they all land in Lategame rather than being split further.
    return "Lategame"


def _multi_level_region(upgrade_id: str, level: int) -> str:
    if upgrade_id == "Scratch Luck":
        return _scratch_luck_region(level)
    if upgrade_id == "Scratch Bot Strength":
        # In-game cost parity: level 7 = Scratch Luck 14, 8 = 16, 9 = 18, 10 = 20,
        # an exact 2x relationship, extrapolated across the full 1-20 range.
        return _scratch_luck_region(2 * level)
    if upgrade_id == "Buying Speed":
        # In-game cost parity: level 0 = Scratch Luck 22, 1 = 23, 2 = 24, 3 = 25,
        # 4 = 26, an exact +22 relationship. The in-game panel labels levels
        # starting from 0 (cost to go from 0->1 owned), one below our 1-indexed
        # "Level 1" check, hence the -1 adjustment (21 + level instead of 22 + level).
        return _scratch_luck_region(21 + level)
    if upgrade_id == "Warp Speed":
        return "Catalog 4"
    if upgrade_id in _SCRATCH_SIZE_REGION:
        return _SCRATCH_SIZE_REGION[upgrade_id]
    if upgrade_id in _GADGET_UPGRADE_LEVEL_REGIONS:
        regions = _GADGET_UPGRADE_LEVEL_REGIONS[upgrade_id]
        return regions[level - 1] if level - 1 < len(regions) else "Lategame"
    return "Lategame"


# Location Table Builder
# Mirrors the ID scheme in C# Locations.cs exactly:
#
# BASE_ID + 0   : base ticket cash-out locations  (19 tickets x 10 slots = 0–189)
# BASE_ID + 200 : super ticket cash-out locations (19 tickets x 10 slots = 200–389)
# BASE_ID + 400 : single-purchase upgrade locations (400–417)
# BASE_ID + 600 : multi-level upgrade locations, one per level (600–785)
#                 includes Scratch Size (9 x 2 = 18 levels, offsets 168–185)
# BASE_ID + 900 : achievement locations (900–933)
#
# Each ticket block reserves 10 IDs; j = threshold index (matches C# loop).
def build_location_table() -> dict[str, SSLocationData]:
    table: dict[str, SSLocationData] = {}

    # Base ticket cash-out thresholds
    ticket_base = BASE_ID + 0
    for i, ticket in enumerate(BASE_TICKETS):
        if ticket == "Final Chance":
            # 4 dedicated one-time locations instead of shared thresholds, reuses
            # this entry's reserved 10-ID block, matching C# Locations.cs exactly.
            for v, (name, region) in enumerate(FINAL_CHANCE_CASH_OUT_LOCATIONS):
                addr = ticket_base + (i * 10) + v
                table[name] = SSLocationData(region=region, address=addr)
            continue
        for j, threshold in enumerate(TICKET_THRESHOLDS):
            name = f"Cash Out {ticket} {threshold}"
            addr = ticket_base + (i * 10) + j
            region = _TICKET_REGION.get(ticket, "Catalog 1")
            table[name] = SSLocationData(region=region, address=addr)

    # Super ticket cash-out thresholds, all in Lategame (require Scratch Luck)
    super_base = BASE_ID + 200
    for i, ticket in enumerate(SUPER_TICKETS):
        for j, threshold in enumerate(SUPER_TICKET_THRESHOLDS):
            name = f"Cash Out {ticket} {threshold}"
            addr = super_base + (i * 10) + j
            table[name] = SSLocationData(region="Lategame", address=addr)

    # Single-purchase upgrades
    single_base = BASE_ID + 400
    for i, upgrade in enumerate(SINGLE_PURCHASE_UPGRADES):
        name = f"Buy {upgrade}"
        region = _SINGLE_UPGRADE_REGION.get(upgrade, "Catalog 4")
        table[name] = SSLocationData(region=region, address=single_base + i)

    # Multi-level upgrades, one check per level
    multi_base = BASE_ID + 600
    offset = 0
    for upgrade, max_level in MULTI_LEVEL_UPGRADES:
        for level in range(1, max_level + 1):
            name = f"Buy {upgrade} Level {level}"
            region = _multi_level_region(upgrade, level)
            table[name] = SSLocationData(region=region, address=multi_base + offset + (level - 1))
        offset += max_level

    # Prestige event locations, locked Progressive Prestige items placed here
    prestige_base = BASE_ID + 1000
    _prestige_event_region = ["Prestige Tier 1", "Prestige Tier 2", "Prestige Tier 3", "Lategame"]
    for i in range(PRESTIGE_LOCATION_COUNT):
        name = f"Prestige {i + 1}"
        region = _prestige_event_region[i] if i < len(_prestige_event_region) else "Lategame"
        table[name] = SSLocationData(region=region, address=prestige_base + i)

    # Single prestige perk purchase locations
    single_perk_base = BASE_ID + 1100
    for i, perk_id in enumerate(PRESTIGE_SINGLE_PERKS):
        name = f"Buy Prestige Perk {perk_id}"
        region = _PRESTIGE_SINGLE_PERK_REGION.get(perk_id, "Lategame")
        table[name] = SSLocationData(region=region, address=single_perk_base + i)

    # Multi-level prestige perk purchase locations, one check per level
    multi_perk_base = BASE_ID + 1200
    perk_offset = 0
    for perk_id, max_level in PRESTIGE_MULTI_PERKS:
        level_regions = _PRESTIGE_MULTI_PERK_LEVEL_REGIONS.get(perk_id, [])
        for level in range(1, max_level + 1):
            name = f"Buy Prestige Perk {perk_id} Level {level}"
            region = level_regions[level - 1] if level - 1 < len(level_regions) else "Lategame"
            table[name] = SSLocationData(region=region, address=multi_perk_base + perk_offset + (level - 1))
        perk_offset += max_level

    # Achievements
    # Achievements are unpredictable events, never hold progression items there,
    # as a player could be softlocked waiting on a hard/obscure achievement.
    ach_base = BASE_ID + 900
    for i, achievement in enumerate(ACHIEVEMENTS):
        name = f"Achievement: {achievement}"
        region = _ACHIEVEMENT_REGION.get(achievement, "Lategame")
        table[name] = SSLocationData(
            region=region,
            address=ach_base + i,
            progress_type=LocationProgressType.EXCLUDED,
        )

    return table


location_table = build_location_table()


# Helper Functions
def get_location_names() -> list[str]:
    return list(location_table.keys())


def get_location_id(name: str) -> Optional[int]:
    data = location_table.get(name)
    return data.address if data else None


def get_locations_in_region(region: str) -> dict[str, SSLocationData]:
    return {k: v for k, v in location_table.items() if v.region == region}