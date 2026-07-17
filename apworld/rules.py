from worlds.AutoWorld import World
from BaseClasses import CollectionState
from .locations import (SUPER_TICKETS, MULTI_LEVEL_UPGRADES, TICKET_THRESHOLDS, SUPER_TICKET_THRESHOLDS,
                        CATALOG_1_TICKETS, CATALOG_2_TICKETS,
                        CATALOG_3_TICKETS, CATALOG_4_TICKETS,
                        SINGLE_PURCHASE_UPGRADES,
                        PRESTIGE_SINGLE_PERKS, PRESTIGE_MULTI_PERKS,
                        PRESTIGE_PERK_PREREQUISITES)
from .items import PROGRESSIVE_ITEM_COUNTS

def set_rules(world: World) -> None:
    multiworld = world.multiworld
    player = world.player

    # Helper Functions
    def has(item_name: str, state: CollectionState) -> bool:
        return state.has(item_name, player)

    def set_rule_safe(location_name: str, rule):
        try:
            multiworld.get_location(location_name, player).access_rule = rule
        except KeyError:
            pass

    # Ticket Cash-Out Rules
    # Player starts with Day Job, no rule needed.
    #
    # Within each catalog, ticket prices scale by ~10x per step. Each ticket
    # therefore requires the previous one to be unlocked, so the generator
    # cannot give a player an expensive ticket before they have the cheaper
    # ones needed to earn enough money to buy it.
    all_catalog_sequences = [
        CATALOG_1_TICKETS,
        CATALOG_2_TICKETS,
        CATALOG_3_TICKETS,
        CATALOG_4_TICKETS,
    ]

    for sequence in all_catalog_sequences:
        for i, ticket in enumerate(sequence):
            if ticket == "Final Chance":
                continue  # Handled separately below
            for threshold in TICKET_THRESHOLDS:
                location_name = f"Cash Out {ticket} {threshold}"
                if i == 0:
                    # First ticket in catalog: only needs its own unlock
                    set_rule_safe(location_name,
                        lambda state, t=ticket: has(f"Unlock {t}", state))
                else:
                    previous = sequence[i - 1]
                    set_rule_safe(location_name,
                        lambda state, t=ticket, p=previous: (
                            has(f"Unlock {t}", state) and has(f"Unlock {p}", state)
                        ))

    # Final Chance variants each have their own dedicated one-time location (see
    # FINAL_CHANCE_VARIANT_REQUIREMENTS below, set after coin_req_met is defined),
    # they don't go through the generic per-catalog sequential loop above since Final
    # Chance is itself the mechanism that reveals the next catalog, not a ticket
    # gated behind an ancestor in its own catalog.

    # Coin and Scratch Size Requirements for Ticket Cash-Outs
    # A ticket can only be scratched once the player's effective scratch strength
    # (coin base strength + Scratch Size levels bought for that coin, 0-2) meets or
    # exceeds the ticket's hardness. Both figures pulled directly from game data:
    #   Coin base strength: Base=0, Tin=2, Aluminum=4, Copper=6, Bronze=8, Iron=10,
    #                        Steel=12, Titanium=14, Tungsten=16 (each Scratch Size
    #                        level adds +1, max 2 levels per coin, bridging exactly to
    #                        the next coin's base, confirmed by the existing Beginning,
    #                        Catalog 1 rule already requiring 2 Base Coin levels to
    #                        reach Tin Coin's base strength of 2).
    #   Ticket hardness: Two Win=0, Mini Scratch=1, Apple Tree=2, Quick Cash=3,
    #                     Lucky Cat=4, Sand Dollars=5, Scratch My Back=6, Snake Eyes=7,
    #                     The Bomb=8, Bank Break=9, Xmas Countdown=10, Thrift Store=11,
    #                     Berry Picking=12, Final Chance_3=13, Trick or Treat=14,
    #                     Slot Machine=15, To the Moon=16, Booster Pack=17,
    #                     Final Chance=5, Final Chance_2=9, Final Chance_4=18.
    # (ticket => (required coin, minimum Scratch Size level for that coin))
    # Day Job and Two Win need nothing (hardness 0, met by the starting Base Coin).
    TICKET_COIN_REQUIREMENTS = {
        "Mini Scratch":      ("Base Coin", 1),
        "Apple Tree":        ("Tin Coin", 0),
        "Quick Cash":        ("Tin Coin", 1),
        "Lucky Cat":         ("Aluminum Coin", 0),
        "Sand Dollars":      ("Aluminum Coin", 1),
        "Scratch My Back":   ("Copper Coin", 0),
        "Snake Eyes":        ("Copper Coin", 1),
        "The Bomb":          ("Bronze Coin", 0),
        "Bank Break":        ("Bronze Coin", 1),
        "Xmas Countdown":    ("Iron Coin", 0),
        "Thrift Store":      ("Iron Coin", 1),
        "Berry Picking":     ("Steel Coin", 0),
        "Trick or Treat":    ("Titanium Coin", 0),
        "Slot Machine":      ("Titanium Coin", 1),
        "To the Moon":       ("Tungsten Coin", 0),
        "Booster Pack":      ("Tungsten Coin", 1),
    }
    # Final Chance (all 4 variants) isn't in TICKET_COIN_REQUIREMENTS. Each variant's
    # coin/Scratch Size requirement is set directly in FINAL_CHANCE_VARIANT_REQUIREMENTS
    # below instead, since they're keyed by dedicated location name, not a shared
    # "Cash Out {ticket} {threshold}" pattern.

    # Populated later by the coin purchase loop below. Declared here so coin_req_met
    # can reference it.
    _coin_owned_rule: dict = {}

    def coin_req_met(coin: str, level: int, state: CollectionState) -> bool:
        if coin != "Base Coin":
            owned_rule = _coin_owned_rule.get(coin)
            if owned_rule is None or not owned_rule(state):
                return False
        if level > 0 and state.count(f"Progressive Scratch Size {coin}",
                                     player) < level:
            return False
        return True

    def add_coin_req(location_name: str, coin: str, level: int):
        try:
            loc = multiworld.get_location(location_name, player)
            existing = loc.access_rule
            loc.access_rule = lambda state, r=existing, c=coin, lv=level: r(state) and coin_req_met(c, lv, state)
        except KeyError:
            pass

    for ticket, (coin, level) in TICKET_COIN_REQUIREMENTS.items():
        for threshold in TICKET_THRESHOLDS:
            add_coin_req(f"Cash Out {ticket} {threshold}", coin, level)

    # Final Chance Variant Cash-Out Rules
    # Each variant has its own dedicated one-time location, gated by its own AP item plus
    # the coin/Scratch Size level actually needed to scratch it — mirrors the matching
    # region-entrance rules in __init__.py. Only variant 1 additionally needs Scratch
    # Luck level 8 (confirmed in-game). That requirement was never established for
    # variants 2-4.
    FINAL_CHANCE_VARIANT_REQUIREMENTS = {
        "Cash Out Final Chance":   ("Unlock Final Chance",   "Aluminum Coin", 1, 8),
        "Cash Out Final Chance 2": ("Unlock Final Chance 2", "Bronze Coin",   1, None),
        "Cash Out Final Chance 3": ("Unlock Final Chance 3", "Steel Coin",    1, None),
        "Cash Out Final Chance 4": ("Unlock Final Chance 4", "Tungsten Coin", 2, None),
    }

    def final_chance_rule_met(item: str, coin: str, level: int, luck: int | None, state: CollectionState) -> bool:
        if not has(item, state):
            return False
        if luck is not None and state.count("Progressive Scratch Luck", player) < luck:
            return False
        return coin_req_met(coin, level, state)

    for location_name, (item, coin, level, luck) in FINAL_CHANCE_VARIANT_REQUIREMENTS.items():
        set_rule_safe(location_name,
            lambda state, i=item, c=coin, lv=level, lk=luck: final_chance_rule_met(i, c, lv, lk, state))

    # Super Ticket Rules
    # Require base ticket unlock and at least 1 Progressive Scratch Luck. Super variants
    # are the same physical ticket (just a rarer pull), so they need the same coin and
    # Scratch Size level as their base ticket to physically scratch.
    for ticket in SUPER_TICKETS:
        if "Final Chance_Win" in ticket:
            continue  # This is the goal, not a location rule
        base_ticket = ticket.replace("Super_", "")
        coin, level = TICKET_COIN_REQUIREMENTS.get(base_ticket, (None, 0))
        for threshold in SUPER_TICKET_THRESHOLDS:
            location_name = f"Cash Out {ticket} {threshold}"
            if base_ticket == "Day Job":
                # Day Job has no unlock item, super variant only needs Scratch Luck
                set_rule_safe(location_name,
                    lambda state: state.count("Progressive Scratch Luck", player) >= 1)
            else:
                set_rule_safe(location_name,
                    lambda state, bt=base_ticket, c=coin, lv=level: (
                        has(f"Unlock {bt}", state) and
                        state.count("Progressive Scratch Luck", player) >= 1 and
                        (c is None or coin_req_met(c, lv, state))
                    ))

    # Gadget Upgrade Rules
    # Every "Buy {upgrade} Level N" location requires having received at least N
    # copies of "Progressive {upgrade}" from AP. This must match the runtime gate
    # in Patch_ShopTryBuy.Prefix exactly otherwise the generator could place a
    # progression item behind a level the player can never actually purchase.
    gadget_upgrades = {
        "Scratch Bot Speed":    "Unlock Scratch Bot",
        "Scratch Bot Capacity": "Unlock Scratch Bot",
        "Scratch Bot Strength": "Unlock Scratch Bot",
        "Fan Speed":            "Unlock Fan",
        "Fan Battery":          "Unlock Fan",
        "Mundo Speed":          "Unlock Mundo",
        "Spell Charge Speed":   "Unlock Spell Book",
        "Timer Capacity":       "Unlock Egg Timer",
        "Timer Charge":         "Unlock Egg Timer",
        "Warp Speed":           "Unlock The Machine",
    }
    scratch_size_coin_reqs = {
        "Scratch Size Tin Coin":       "Unlock Tin Coin",
        "Scratch Size Aluminum Coin":  "Unlock Aluminum Coin",
        "Scratch Size Copper Coin":    "Unlock Copper Coin",
        "Scratch Size Bronze Coin":    "Unlock Bronze Coin",
        "Scratch Size Iron Coin":      "Unlock Iron Coin",
        "Scratch Size Steel Coin":     "Unlock Steel Coin",
        "Scratch Size Titanium Coin":  "Unlock Titanium Coin",
        "Scratch Size Tungsten Coin":  "Unlock Tungsten Coin",
    }
    for upgrade_id, max_level in MULTI_LEVEL_UPGRADES:
        required_item = gadget_upgrades.get(upgrade_id) or scratch_size_coin_reqs.get(upgrade_id)
        # Gadget-gated upgrades only have HALF their real max_level worth of
        # "Progressive {upgrade}" copies in the item pool. The remaining levels
        # are meant to be freely buyable at normal price once the base gadget is
        # unlocked. Requiring the count past the pool size would demand more copies
        # than any seed ever contains, a guaranteed softlock, so the count check
        # only applies up to the pool size. Levels beyond it fall back to just
        # the gadget/coin req.
        pool_count = PROGRESSIVE_ITEM_COUNTS.get(f"Progressive {upgrade_id}", max_level)
        for level in range(1, max_level + 1):
            set_rule_safe(f"Buy {upgrade_id} Level {level}",
                lambda state, u=upgrade_id, lv=level, req=required_item, pc=pool_count: (
                    (req is None or has(req, state)) and
                    (lv > pc or state.count(f"Progressive {u}", player) >= lv)
                ))

    # Single-Purchase Upgrade Location Rules
    # "Buy X" requires receiving "Unlock X" from AP first.
    # Without this the generator treats these as freely accessible
    # and can place progression items there while putting the unlock
    # behind them, creating a deadlock.
    for upgrade in SINGLE_PURCHASE_UPGRADES:
        set_rule_safe(f"Buy {upgrade}",
            lambda state, u=upgrade: has(f"Unlock {u}", state))

    # Coin Purchase Prerequisites
    # Each coin tier requires its own unlock, both Scratch Size progressive
    # items for the previous tier, and everything the previous coin itself
    # required.
    _coin_order = [
        "Tin Coin", "Aluminum Coin", "Copper Coin", "Bronze Coin", "Iron Coin",
        "Steel Coin", "Titanium Coin", "Tungsten Coin"
    ]
    _coin_scratch_prerequisites = {
        "Tin Coin":      "Scratch Size Base Coin",
        "Aluminum Coin": "Scratch Size Tin Coin",
        "Copper Coin":   "Scratch Size Aluminum Coin",
        "Bronze Coin":   "Scratch Size Copper Coin",
        "Iron Coin":     "Scratch Size Bronze Coin",
        "Steel Coin":    "Scratch Size Iron Coin",
        "Titanium Coin": "Scratch Size Steel Coin",
        "Tungsten Coin": "Scratch Size Titanium Coin",
    }
    previous_coin_rule = lambda state: True  # Base Coin is always owned
    for coin in _coin_order:
        ss_base = _coin_scratch_prerequisites[coin]
        prev_rule = previous_coin_rule
        coin_rule = lambda state, c=coin, ss=ss_base, prev=prev_rule: (prev(
            state) and has(f"Unlock {c}", state) and state.count(
                f"Progressive {ss}", player) >= 2)
        set_rule_safe(f"Buy {coin}", coin_rule)
        _coin_owned_rule[coin] = coin_rule
        previous_coin_rule = coin_rule

    # Achievement Rules

    # Death_1-4: die at Final Chance (forces prestige), no Machine required
    for i in range(1, 5):
        set_rule_safe(f"Achievement: Death_{i}",
            lambda state: has("Unlock Final Chance", state))

    set_rule_safe("Achievement: Scratch Final Chance Without Dying",
        lambda state: has("Unlock Final Chance", state))

    # Nap time: very endgame, gate behind Final Chance at minimum
    set_rule_safe("Achievement: Nap time",
        lambda state: has("Unlock Final Chance", state))

    # Soul Siphon requires The Machine (Faithful Servant is excluded/filler)
    set_rule_safe("Achievement: Soul Siphon",
        lambda state: has("Unlock The Machine", state))

    # Idle Game: requires all four automation gadgets
    set_rule_safe("Achievement: Idle game",
        lambda state: (
            has("Unlock Scratch Bot", state) and
            has("Unlock Mundo", state) and
            has("Unlock Fan", state) and
            has("Unlock Egg Timer", state)
        ))

    # Wizard requires Spell Book
    set_rule_safe("Achievement: Wizard",
        lambda state: has("Unlock Spell Book", state))

    # Time machine: Warp Speed maxed AND Egg Timer (for time-warp mechanic)
    set_rule_safe("Achievement: Time machine",
        lambda state: (
            state.count("Progressive Warp Speed", player) >= 3 and
            has("Unlock Egg Timer", state)
        ))

    # Trash Jackpot: jackpot in Trash Can
    set_rule_safe("Achievement: Trash Jackpot",
        lambda state: has("Unlock Trash Can", state))

    # Clicker minigame: trigger scratch bot clicker mode
    set_rule_safe("Achievement: Clicker minigame",
        lambda state: has("Unlock Scratch Bot", state))

    # Visit the Night Market and Walk-in-closet require Badge Collection
    for ach in ["Visit the Night Market", "Walk-in-closet"]:
        set_rule_safe(f"Achievement: {ach}",
            lambda state: has("Unlock Badge Collection", state))

    # Bad kitty: have Mundo bankrupt you
    set_rule_safe("Achievement: Bad kitty",
        lambda state: has("Unlock Mundo", state))

    # Lucky cat: Mundo earns a super jackpot, needs Scratch Luck to get supers
    set_rule_safe("Achievement: Lucky cat",
        lambda state: (
            has("Unlock Mundo", state) and
            state.count("Progressive Scratch Luck", player) >= 1
        ))

    # Achievements that require at least one prestige
    for ach in ["Super Jackpot", "Lucky ticket", "Big win", "High level gambling",
                "Win your job", "Honest work", "Workaholic"]:
        set_rule_safe(f"Achievement: {ach}",
            lambda state: state.count("Progressive Prestige", player) >= 1)

    # Skip a catalogue: bypass a whole catalog, needs Final Chance 2 (the item that
    # actually reveals Catalog 3, i.e. skipping past Catalog 2) and one prestige
    set_rule_safe("Achievement: Skip a catalogue",
        lambda state: (
            has("Unlock Final Chance 2", state) and
            state.count("Progressive Prestige", player) >= 1
        ))

    # Winning Streak and One of each please: every ticket except Final Chance
    _all_non_final_tickets = (
        CATALOG_1_TICKETS + CATALOG_2_TICKETS + CATALOG_3_TICKETS +
        CATALOG_4_TICKETS[:-1]  # Exclude Final Chance
    )
    for ach in ["Winning streak", "One of each please"]:
        set_rule_safe(f"Achievement: {ach}",
            lambda state, tickets=_all_non_final_tickets: (
                all(has(f"Unlock {t}", state) for t in tickets)
            ))

    # Speedrun: full run to the true endgame (Final Chance 4) + The Machine + multiple prestiges
    set_rule_safe("Achievement: Speedrun",
        lambda state: (
            has("Unlock Final Chance 4", state) and
            has("Unlock The Machine", state) and
            state.count("Progressive Prestige", player) >= 3
        ))

    # Max out skill tree: buying all perk levels, gate on true endgame + deep prestige
    set_rule_safe("Achievement: Max out skill tree",
        lambda state: (
            has("Unlock Final Chance 4", state) and
            state.count("Progressive Prestige", player) >= 3
        ))

    # Achievement Hunter: union of all other achievement requirements
    set_rule_safe("Achievement: Achievement Hunter",
        lambda state: (
            has("Unlock Final Chance 4", state) and
            has("Unlock The Machine", state) and
            has("Unlock Scratch Bot", state) and
            has("Unlock Mundo", state) and
            has("Unlock Fan", state) and
            has("Unlock Egg Timer", state) and
            has("Unlock Spell Book", state) and
            has("Unlock Badge Collection", state) and
            has("Unlock Trash Can", state) and
            state.count("Progressive Warp Speed", player) >= 3 and
            state.count("Progressive Prestige", player) >= 3 and
            all(has(f"Unlock {t}", state) for t in _all_non_final_tickets)
        ))

    # Prestige Perk Location Rules
    _prestige_single_set = set(PRESTIGE_SINGLE_PERKS)

    def perk_has_one(perk_id: str, state: CollectionState) -> bool:
        """True if the player has received at least one copy of this perk's AP item."""
        if perk_id in _prestige_single_set:
            return has(f"Unlock {perk_id}", state)
        return state.count(f"Progressive {perk_id}", player) >= 1

    # Single-purchase prestige perks
    for perk_id in PRESTIGE_SINGLE_PERKS:
        prereqs = PRESTIGE_PERK_PREREQUISITES.get(perk_id, [])
        set_rule_safe(f"Buy Prestige Perk {perk_id}",
            lambda state, p=perk_id, pr=list(prereqs): (
                state.count("Progressive Prestige", player) >= 1 and
                has(f"Unlock {p}", state) and
                all(perk_has_one(req, state) for req in pr)
            ))

    # Multi-level prestige perks, one rule per level
    for perk_id, max_level in PRESTIGE_MULTI_PERKS:
        prereqs = PRESTIGE_PERK_PREREQUISITES.get(perk_id, [])
        for level in range(1, max_level + 1):
            set_rule_safe(f"Buy Prestige Perk {perk_id} Level {level}",
                lambda state, p=perk_id, lv=level, pr=list(prereqs): (
                    state.count("Progressive Prestige", player) >= 1 and
                    state.count(f"Progressive {p}", player) >= lv and
                    all(perk_has_one(req, state) for req in pr)
                ))

    # Branching Prestige Perk Prerequisites
    # These six perks require one of two prerequisite branches rather than a list,
    # so PRESTIGE_PERK_PREREQUISITES intentionally omits them.
    def bliss_or_big_winner(state: CollectionState) -> bool:
        return (
            (perk_has_one("Ignorance is Bliss", state) and perk_has_one("Tool Belt", state))
            or
            (perk_has_one("Big Winner", state) and perk_has_one("Jackpot Power", state))
        )

    def fine_dining_branch(state: CollectionState) -> bool:
        return (
            (perk_has_one("Dishwasher", state) and perk_has_one("Completionist", state) and perk_has_one("Starter Kit", state))
            or
            (perk_has_one("Soft Hands", state) and perk_has_one("Clean Freak", state) and perk_has_one("Tool Belt", state))
        )

    set_rule_safe("Buy Prestige Perk Fine Dining", lambda state: (
        state.count("Progressive Prestige", player) >= 1 and
        has("Unlock Fine Dining", state) and
        fine_dining_branch(state)
    ))
    set_rule_safe("Buy Prestige Perk PlateMaster5000", lambda state: (
        state.count("Progressive Prestige", player) >= 1 and
        has("Unlock PlateMaster5000", state) and
        fine_dining_branch(state)
    ))
    set_rule_safe("Buy Prestige Perk Magic", lambda state: (
        state.count("Progressive Prestige", player) >= 1 and
        has("Unlock Magic", state) and
        bliss_or_big_winner(state)
    ))
    set_rule_safe("Buy Prestige Perk Shopping Spree", lambda state: (
        state.count("Progressive Prestige", player) >= 1 and
        has("Unlock Shopping Spree", state) and
        bliss_or_big_winner(state)
    ))
    set_rule_safe("Buy Prestige Perk Loan Shark", lambda state: (
        state.count("Progressive Prestige", player) >= 1 and
        has("Unlock Loan Shark", state) and
        perk_has_one("Magic", state) and
        bliss_or_big_winner(state)
    ))
    set_rule_safe("Buy Prestige Perk Hotkeys", lambda state: (
        state.count("Progressive Prestige", player) >= 1 and
        has("Unlock Hotkeys", state) and
        perk_has_one("Shopping Spree", state) and
        bliss_or_big_winner(state)
    ))