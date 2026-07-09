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
CATALOG_2_TICKETS = ["Sand Dollars", "Scratch My Back", "Snake Eyes", "The Bomb", "Bank Break"]
CATALOG_3_TICKETS = ["Xmas Countdown", "Thrift Store", "Berry Picking", "Trick or Treat"]
CATALOG_4_TICKETS = ["Slot Machine", "To the Moon", "Booster Pack", "Final Chance"]

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
MULTI_LEVEL_UPGRADES = [
    ("Scratch Luck",         45),
    ("Scratch Bot Speed",    30),
    ("Scratch Bot Capacity", 10),
    ("Scratch Bot Strength", 20),
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
PRESTIGE_LOCATION_COUNT = 5  # Number of prestige event check locations

# Single-purchase prestige perks
PRESTIGE_SINGLE_PERKS = [
    "Challenges",
    "Night Market",
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
PRESTIGE_MULTI_PERKS = [
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
    "Catalog 2",
    "Catalog 3",
    "Catalog 4",
    "Early Prestige",
    "Late Prestige",
    "Endgame",
]


# -------------------------------------------------------
# Region Assignment Helper Functions
# -------------------------------------------------------
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
    "The Machine":      "Early Prestige",
    "Tin Coin":         "Catalog 1",
    "Aluminum Coin":    "Catalog 2",
    "Copper Coin":      "Catalog 3",
    "Bronze Coin":      "Catalog 3",
    "Iron Coin":        "Catalog 4",
    "Steel Coin":       "Catalog 4",
    "Titanium Coin":    "Late Prestige",
    "Tungsten Coin":    "Late Prestige",
    "Trash Can":        "Catalog 1",
    "Scratch Bot":      "Early Prestige",
    "Fan":              "Early Prestige",
    "Sticky Mat":       "Catalog 2",
    "Badge Collection": "Catalog 2",
    "Mundo":            "Early Prestige",
    "Spell Book":       "Early Prestige",
    "Subscription Bot": "Catalog 3",
    "Egg Timer":        "Early Prestige",
}

_SCRATCH_SIZE_REGION: dict[str, str] = {
    "Scratch Size Base Coin":     "Beginning",
    "Scratch Size Tin Coin":      "Catalog 1",
    "Scratch Size Aluminum Coin": "Catalog 2",
    "Scratch Size Copper Coin":   "Catalog 3",
    "Scratch Size Bronze Coin":   "Catalog 3",
    "Scratch Size Iron Coin":     "Catalog 4",
    "Scratch Size Steel Coin":    "Catalog 4",
    "Scratch Size Titanium Coin": "Late Prestige",
    "Scratch Size Tungsten Coin": "Late Prestige",
}

_PRESTIGE_PERK_REGION: dict[str, str] = {
    # Roots and depth-1 nodes, Early Prestige
    "Challenges":             "Early Prestige",
    "Night Market":           "Early Prestige",
    "Starter Kit":            "Early Prestige",
    "Jackpot Power":          "Early Prestige",
    "Tool Belt":              "Early Prestige",
    "Big Winner":             "Early Prestige",
    "Recycling":              "Early Prestige",
    "Less is More":           "Early Prestige",
    "Completionist":          "Early Prestige",
    "Electric Fan":           "Early Prestige",
    "Booster Kit":            "Early Prestige",
    "Self Made Millionaire":  "Early Prestige",
    "Clean Freak":            "Early Prestige",
    "Ignorance is Bliss":     "Early Prestige",
    # Depth-2+, Late Prestige
    "Shopping Spree":         "Late Prestige",
    "Magic":                  "Late Prestige",
    "Smart Investment":       "Late Prestige",
    "Refund":                 "Late Prestige",
    "Air Condition":          "Late Prestige",
    "Pet Lover":              "Late Prestige",
    "Soft Hands":             "Late Prestige",
    "Dishwasher":             "Late Prestige",
    "Learn by Doing":         "Late Prestige",
    "Collector":              "Late Prestige",
    "Experienced":            "Late Prestige",
    "Fully Automated":        "Late Prestige",
    "Picky Eater":            "Late Prestige",
    "Fine Dining":            "Late Prestige",
    "PlateMaster5000":        "Late Prestige",
    "Built Different":        "Late Prestige",
    # Deep branch tips, Endgame
    "Hotkeys":                "Endgame",
    "Loan Shark":             "Endgame",
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
    "Idle game":                         "Early Prestige",
    "Visit the Night Market":            "Catalog 2",
    "Walk-in-closet":                    "Catalog 3",
    "Honest work":                       "Catalog 2",
    "Workaholic":                        "Catalog 3",
    "Soul Siphon":                       "Early Prestige",
    "Wizard":                            "Early Prestige",
    "Time machine":                      "Late Prestige",
    "Scratch Final Chance Without Dying":"Catalog 4",
    "Faithful Servant":                  "Early Prestige",
    "Speedrun":                          "Late Prestige",
    "Max out skill tree":                "Endgame",
    "Achievement Hunter":                "Endgame",
}


def _scratch_luck_region(level: int) -> str:
    if level <= 1:  return "Beginning"
    if level <= 5:  return "Catalog 1"
    if level <= 10: return "Catalog 2"
    if level <= 20: return "Catalog 3"
    if level <= 30: return "Catalog 4"
    if level <= 40: return "Early Prestige"
    return "Late Prestige"


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
        return "Early Prestige"
    if upgrade_id in _SCRATCH_SIZE_REGION:
        return _SCRATCH_SIZE_REGION[upgrade_id]
    # Remaining gadget sub-upgrades (Scratch Bot Speed/Capacity, Fan Speed/Battery,
    # Mundo Speed, Spell Charge Speed, Timer Capacity/Charge) have no confirmed cost
    # data yet: levels 1-2 in Early Prestige, 3+ in Late Prestige.
    return "Early Prestige" if level <= 2 else "Late Prestige"


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

    # Super ticket cash-out thresholds, all in Late Prestige (require Scratch Luck)
    super_base = BASE_ID + 200
    for i, ticket in enumerate(SUPER_TICKETS):
        for j, threshold in enumerate(TICKET_THRESHOLDS):
            name = f"Cash Out {ticket} {threshold}"
            addr = super_base + (i * 10) + j
            table[name] = SSLocationData(region="Late Prestige", address=addr)

    # Single-purchase upgrades
    single_base = BASE_ID + 400
    for i, upgrade in enumerate(SINGLE_PURCHASE_UPGRADES):
        name = f"Buy {upgrade}"
        region = _SINGLE_UPGRADE_REGION.get(upgrade, "Early Prestige")
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
    for i in range(PRESTIGE_LOCATION_COUNT):
        name = f"Prestige {i + 1}"
        table[name] = SSLocationData(region="Early Prestige", address=prestige_base + i)

    # Single prestige perk purchase locations
    single_perk_base = BASE_ID + 1100
    for i, perk_id in enumerate(PRESTIGE_SINGLE_PERKS):
        name = f"Buy Prestige Perk {perk_id}"
        region = _PRESTIGE_PERK_REGION.get(perk_id, "Late Prestige")
        table[name] = SSLocationData(region=region, address=single_perk_base + i)

    # Multi-level prestige perk purchase locations, one check per level
    multi_perk_base = BASE_ID + 1200
    perk_offset = 0
    for perk_id, max_level in PRESTIGE_MULTI_PERKS:
        for level in range(1, max_level + 1):
            name = f"Buy Prestige Perk {perk_id} Level {level}"
            region = _PRESTIGE_PERK_REGION.get(perk_id, "Late Prestige")
            table[name] = SSLocationData(region=region, address=multi_perk_base + perk_offset + (level - 1))
        perk_offset += max_level

    # Achievements
    # Achievements are unpredictable events, never hold progression items there,
    # as a player could be softlocked waiting on a hard/obscure achievement.
    ach_base = BASE_ID + 900
    for i, achievement in enumerate(ACHIEVEMENTS):
        name = f"Achievement: {achievement}"
        region = _ACHIEVEMENT_REGION.get(achievement, "Late Prestige")
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