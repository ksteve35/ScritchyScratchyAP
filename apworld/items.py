from dataclasses import dataclass
from enum import Enum
from typing import Optional
from BaseClasses import ItemClassification


class ItemType(Enum):
    TICKET = "ticket"
    UPGRADE = "upgrade"
    PROGRESSIVE = "progressive"
    MONEY = "money"
    TRAP = "trap"
    LOCKED = "locked"  # Placed as locked items at specific locations, never in the pool


@dataclass
class SSItemData:
    classification: ItemClassification
    item_type: ItemType
    code: Optional[int] = None


BASE_ID = 777000000
ITEM_BASE = BASE_ID + 1000
 
 
# Progressive Upgrade Item Counts
# One progressive item is added to the pool per upgrade level (MaxLevel).
# The C# mod tracks how many have been received and applies that level
# directly to the upgrade.
STREAK_ITEM_COUNTS = {
    "Lucky Streak":   3,
    "Unlucky Streak": 3,
}

# Jackpot Points added to the pool this many times (additive with natural JP earnings)
BONUS_ITEM_COUNTS = {
    "Jackpot Points": 10,
}

JACKPOT_POINTS_PER_ITEM = 10  # JP awarded per Jackpot Points item received

PROGRESSIVE_ITEM_COUNTS = {
    # No prerequisite, always useful, keep full count in pool.
    "Progressive Scratch Luck":         45,
    "Progressive Buying Speed":         10,
    "Progressive Warp Speed":            3,
    # Require base gadget unlock. Full count in pool, every level is AP-gated.
    "Progressive Scratch Bot Capacity": 10,
    "Progressive Scratch Bot Speed":    30,
    # Trimmed from 20 to 10 (10 items and 10 locations cut, same approach
    # as the 5 prestige perks). Its levels beyond 10 were extrapolated
    # and it was implicated in Fill.FillError generation failures.
    "Progressive Scratch Bot Strength": 10,
    "Progressive Fan Speed":             5,
    "Progressive Fan Battery":           5,
    "Progressive Mundo Speed":          10,
    "Progressive Spell Charge Speed":   10,
    "Progressive Timer Capacity":       10,
    "Progressive Timer Charge":         10,
    # Scratch Size: 2 levels per coin tier
    "Progressive Scratch Size Base Coin":      2,
    "Progressive Scratch Size Tin Coin":       2,
    "Progressive Scratch Size Aluminum Coin":  2,
    "Progressive Scratch Size Copper Coin":    2,
    "Progressive Scratch Size Bronze Coin":    2,
    "Progressive Scratch Size Iron Coin":      2,
    "Progressive Scratch Size Steel Coin":     2,
    "Progressive Scratch Size Titanium Coin":  2,
    "Progressive Scratch Size Tungsten Coin":  2,
}

PRESTIGE_PROGRESSIVE_ITEM_COUNTS = {
    "Progressive Jackpot Power":          5,
    "Progressive Tool Belt":              5,
    # These 5 perks were trimmed from 10 to 5 levels each (25 items and 25
    # locations cut). They were both the most expensive (cumulative JP cost in the
    # thousands) and, along with Scratch Luck/Scratch Bot Speed, among the most
    # consistently implicated chains in Fill.FillError generation failures.
    "Progressive Self Made Millionaire":  5,
    "Progressive Booster Kit":            5,
    "Progressive Recycling":              5,
    "Progressive Less is More":           5,
    "Progressive Ignorance is Bliss":     5,
    "Progressive Big Winner":             7,
    "Progressive Completionist":          5,
    "Progressive Clean Freak":            5,
    "Progressive Smart Investment":       5,
    "Progressive Learn by Doing":         5,
    "Progressive Refund":                 5,
    "Progressive Soft Hands":             5,
    "Progressive Collector":              5,
    "Progressive Experienced":            5,
    "Progressive Built Different":        5,
}


item_table: dict[str, SSItemData] = {

    # Ticket Unlocks
    # Player starts with Day Job, always available, no unlock item needed.
    "Unlock Two Win":           SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 1),
    "Unlock Mini Scratch":      SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 2),
    "Unlock Apple Tree":        SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 3),
    "Unlock Quick Cash":        SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 4),
    "Unlock Lucky Cat":         SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 5),
    "Unlock Sand Dollars":      SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 6),
    "Unlock Scratch My Back":   SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 7),
    "Unlock Snake Eyes":        SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 8),
    "Unlock The Bomb":          SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 9),
    "Unlock Bank Break":        SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 10),
    "Unlock Xmas Countdown":    SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 11),
    "Unlock Thrift Store":      SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 12),
    "Unlock Berry Picking":     SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 13),
    "Unlock Trick or Treat":    SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 14),
    "Unlock Slot Machine":      SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 15),
    "Unlock To the Moon":       SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 16),
    "Unlock Booster Pack":      SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 17),
    "Unlock Final Chance":      SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 18),
    # Final Chance variants 2-4 are separate AP items, each is the mechanism that
    # reveals the next catalog in vanilla play (buying/scratching one opens the next
    # catalog), so each needs its own real gate rather than sharing the base item.
    "Unlock Final Chance 2":    SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 19),
    "Unlock Final Chance 3":    SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 20),
    "Unlock Final Chance 4":    SSItemData(ItemClassification.progression, ItemType.TICKET, ITEM_BASE + 21),

    # Upgrade Unlocks, Single purchase
    "Unlock The Machine":       SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 100),
    "Unlock Tin Coin":          SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 101),
    "Unlock Aluminum Coin":     SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 102),
    "Unlock Copper Coin":       SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 103),
    "Unlock Bronze Coin":       SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 104),
    "Unlock Iron Coin":         SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 105),
    "Unlock Steel Coin":        SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 106),
    "Unlock Titanium Coin":     SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 107),
    "Unlock Tungsten Coin":     SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 108),
    "Unlock Trash Can":         SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 109),
    "Unlock Scratch Bot":       SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 110),
    "Unlock Fan":               SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 111),
    "Unlock Sticky Mat":        SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 112),
    "Unlock Badge Collection":  SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 113),
    "Unlock Mundo":             SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 114),
    "Unlock Spell Book":        SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 115),
    "Unlock Subscription Bot":  SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 116),
    "Unlock Egg Timer":         SSItemData(ItemClassification.progression, ItemType.UPGRADE, ITEM_BASE + 117),

    # Progressive Upgrade Items
    # Each item received advances the upgrade by one level.
    # Added to the pool once per upgrade level.
    "Progressive Scratch Luck":         SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 200),
    "Progressive Scratch Bot Speed":    SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 201),
    "Progressive Scratch Bot Capacity": SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 202),
    "Progressive Scratch Bot Strength": SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 203),
    "Progressive Fan Speed":            SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 204),
    "Progressive Fan Battery":          SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 205),
    "Progressive Mundo Speed":          SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 206),
    "Progressive Spell Charge Speed":   SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 207),
    "Progressive Buying Speed":         SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 208),
    "Progressive Timer Capacity":       SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 209),
    "Progressive Timer Charge":         SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 210),
    "Progressive Warp Speed":           SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 211),
    # Scratch Size: 2 levels per coin tier, gate the next coin tier, so must be progression
    "Progressive Scratch Size Base Coin":      SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 220),
    "Progressive Scratch Size Tin Coin":       SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 221),
    "Progressive Scratch Size Aluminum Coin":  SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 222),
    "Progressive Scratch Size Copper Coin":    SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 223),
    "Progressive Scratch Size Bronze Coin":    SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 224),
    "Progressive Scratch Size Iron Coin":      SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 225),
    "Progressive Scratch Size Steel Coin":     SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 226),
    "Progressive Scratch Size Titanium Coin":  SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 227),
    "Progressive Scratch Size Tungsten Coin":  SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 228),

    # Prestige System
    # Locked item placed at each "Prestige N" location, earned by prestiging in-game
    "Progressive Prestige":              SSItemData(ItemClassification.progression, ItemType.LOCKED,     ITEM_BASE + 500),
    # Additive Jackpot Points bonus (player also earns JP naturally via prestige)
    "Jackpot Points":                    SSItemData(ItemClassification.filler,      ItemType.MONEY,      ITEM_BASE + 501),

    # Single-purchase prestige perks, "Unlock X" authorises the player to buy perk X with JP
    "Unlock Starter Kit":                SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 512),
    "Unlock Electric Fan":               SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 513),
    "Unlock Air Condition":              SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 514),
    "Unlock Pet Lover":                  SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 515),
    "Unlock Dishwasher":                 SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 516),
    "Unlock Magic":                      SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 517),
    "Unlock Shopping Spree":             SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 518),
    "Unlock Loan Shark":                 SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 519),
    "Unlock Picky Eater":                SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 520),
    "Unlock Fully Automated":            SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 521),
    "Unlock Fine Dining":                SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 522),
    "Unlock PlateMaster5000":            SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 523),
    "Unlock Hotkeys":                    SSItemData(ItemClassification.progression, ItemType.UPGRADE,    ITEM_BASE + 524),

    # Multi-level prestige perk progressives, each received copy authorises one more level
    "Progressive Jackpot Power":         SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 540),
    "Progressive Tool Belt":             SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 541),
    "Progressive Self Made Millionaire": SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 542),
    "Progressive Booster Kit":           SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 543),
    "Progressive Recycling":             SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 544),
    "Progressive Less is More":          SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 545),
    "Progressive Ignorance is Bliss":    SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 546),
    "Progressive Big Winner":            SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 547),
    "Progressive Completionist":         SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 548),
    "Progressive Clean Freak":           SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 549),
    "Progressive Smart Investment":      SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 550),
    "Progressive Learn by Doing":        SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 551),
    "Progressive Refund":                SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 552),
    "Progressive Soft Hands":            SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 553),
    "Progressive Collector":             SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 554),
    "Progressive Experienced":           SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 555),
    "Progressive Built Different":       SSItemData(ItemClassification.progression, ItemType.PROGRESSIVE, ITEM_BASE + 556),

    # Money Injections, filler items
    "Small Cash Injection":     SSItemData(ItemClassification.filler, ItemType.MONEY, ITEM_BASE + 401),
    "Large Cash Injection":     SSItemData(ItemClassification.filler, ItemType.MONEY, ITEM_BASE + 402),

    # Trap Items
    # Player loses a percentage of their current balance
    "Debt Trap":                SSItemData(ItemClassification.trap, ItemType.TRAP, ITEM_BASE + 420),
    # Player automatically takes out a loan, gains a debuff until paid off
    "Loan Trap":                SSItemData(ItemClassification.trap,   ItemType.TRAP,  ITEM_BASE + 421),

    # Streak Items, temporary ScratchLuck modifiers (3 tickets each, stackable)
    "Lucky Streak":             SSItemData(ItemClassification.useful, ItemType.MONEY, ITEM_BASE + 430),
    "Unlucky Streak":           SSItemData(ItemClassification.trap,   ItemType.TRAP,  ITEM_BASE + 431),
}


# Helper functions
def get_item_names() -> list[str]:
    return list(item_table.keys())


def get_item_id(name: str) -> Optional[int]:
    data = item_table.get(name)
    return data.code if data else None


def get_items_by_type(item_type: ItemType) -> dict[str, SSItemData]:
    return {k: v for k, v in item_table.items() if v.item_type == item_type}