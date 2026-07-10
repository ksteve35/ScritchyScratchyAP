from typing import Dict, Any, List
from worlds.AutoWorld import World, WebWorld
from BaseClasses import Tutorial, Item, ItemClassification, Region, Location
from .items import (item_table, ItemType, PROGRESSIVE_ITEM_COUNTS,
                    PRESTIGE_PROGRESSIVE_ITEM_COUNTS, BONUS_ITEM_COUNTS,
                    JACKPOT_POINTS_PER_ITEM, STREAK_ITEM_COUNTS)
from .locations import (location_table, REGIONS, PRESTIGE_LOCATION_COUNT,
                        CATALOG_1_TICKETS, CATALOG_2_TICKETS,
                        CATALOG_3_TICKETS, CATALOG_4_TICKETS)
from .rules import set_rules


class SSLocation(Location):
    game = "Scritchy Scratchy"


class SSItem(Item):
    game = "Scritchy Scratchy"


class SSWebWorld(WebWorld):
    theme = "stone"
    tutorials = [
        Tutorial(
            tutorial_name="Setup Guide",
            description="A guide to setting up the Scritchy Scratchy Archipelago mod.",
            language="English",
            file_name="setup_en.md",
            link="setup/en",
            authors=["TheAgentOfChaos"],
        )
    ]


class ScritchyScratchyWorld(World):
    """
    Scritchy Scratchy is a roguelite idle scratch ticket game.
    Unlock ticket types, upgrades, and gadgets sent from the multiworld
    while sending checks by cashing out tickets and buying upgrades.
    Complete the game by winning the Final Chance ticket.
    """

    game = "Scritchy Scratchy"
    web = SSWebWorld()

    item_name_to_id = {
        name: data.code
        for name, data in item_table.items()
        if data.code is not None
    }

    location_name_to_id = {
        name: data.address
        for name, data in location_table.items()
        if data.address is not None
    }

    # Early Items
    # Only Tin and Aluminum, the two coins actually needed to progress past
    # Catalog 1/2. Marking all 8 (or even 6) coins early was tried and broke
    # generation entirely — it crammed too many items into the small sphere-0
    # location pool (~7 locations) and starved out other early-critical items
    # like Unlock Two Win. Two items leaves plenty of room.
    def generate_early(self) -> None:
        for coin in ["Tin Coin", "Aluminum Coin"]:
            self.multiworld.early_items[self.player][f"Unlock {coin}"] = 1

    # Region Creation
    def create_regions(self) -> None:
        regions: Dict[str, Region] = {}
        for region_name in REGIONS:
            region = Region(region_name, self.player, self.multiworld)
            regions[region_name] = region
            self.multiworld.regions.append(region)

        # Menu is the technical root, beginning is always accessible.
        regions["Menu"].connect(regions["Beginning"])

        # Each catalog transition requires the FIRST (cheapest) ticket of that catalog.
        # Combined with the sequential rules in rules.py, this guarantees the generator
        # places cheaper tickets before more expensive ones in every catalog.
        e = regions["Beginning"].connect(regions["Catalog 1"])
        e.access_rule = lambda state: (
            state.has(f"Unlock {CATALOG_1_TICKETS[0]}", self.player) and
            state.has("Unlock Tin Coin", self.player) and
            state.count("Progressive Scratch Size Base Coin", self.player) >= 2
        )

        # Buying/scratching each Final Chance variant is the actual vanilla mechanism
        # that reveals the next catalog, so each transition also requires the matching
        # "Unlock Final Chance N" AP item (variant 1 = base "Unlock Final Chance").
        # Final Chance variant coin/Scratch Size requirements are derived from game data:
        # each coin's base strength plus up to 2 Scratch Size levels (+1 each) must meet
        # or exceed the ticket's hardness.
        #   Final Chance   hardness=5  -> Aluminum Coin (4) + 1 Scratch Size level
        #   Final Chance 2 hardness=9  -> Bronze Coin   (8) + 1 Scratch Size level
        #   Final Chance 3 hardness=13 -> Steel Coin    (12) + 1 Scratch Size level
        #   Final Chance 4 hardness=18 -> Tungsten Coin (16) + 2 Scratch Size levels (max)
        e = regions["Catalog 1"].connect(regions["Catalog 2"])
        e.access_rule = lambda state: (
            state.has(f"Unlock {CATALOG_2_TICKETS[0]}", self.player) and
            state.has("Unlock Final Chance", self.player) and
            # Final Chance physically requires Scratch Luck level 8 to scratch
            # (confirmed in-game) — without it the AP item alone doesn't let the
            # player reach Catalog 2 in practice.
            state.count("Progressive Scratch Luck", self.player) >= 8 and
            state.has("Unlock Aluminum Coin", self.player) and
            state.count("Progressive Scratch Size Aluminum Coin", self.player) >= 1 and
            all(state.has(f"Unlock {t}", self.player) for t in CATALOG_1_TICKETS)
        )

        e = regions["Catalog 2"].connect(regions["Catalog 3"])
        e.access_rule = lambda state: (
            state.has(f"Unlock {CATALOG_3_TICKETS[0]}", self.player) and
            state.has("Unlock Final Chance 2", self.player) and
            state.has("Unlock Bronze Coin", self.player) and
            state.count("Progressive Scratch Size Bronze Coin", self.player) >= 1 and
            all(state.has(f"Unlock {t}", self.player) for t in CATALOG_2_TICKETS)
        )

        e = regions["Catalog 3"].connect(regions["Catalog 4"])
        e.access_rule = lambda state: (
            state.has(f"Unlock {CATALOG_4_TICKETS[0]}", self.player) and
            state.has("Unlock Final Chance 3", self.player) and
            state.has("Unlock Steel Coin", self.player) and
            state.count("Progressive Scratch Size Steel Coin", self.player) >= 1 and
            all(state.has(f"Unlock {t}", self.player) for t in CATALOG_3_TICKETS)
        )

        e = regions["Catalog 4"].connect(regions["Early Prestige"])
        e.access_rule = lambda state: (
            state.has("Unlock Booster Pack", self.player) and
            state.has("Unlock Final Chance 4", self.player) and
            state.has("Unlock Tungsten Coin", self.player) and
            state.count("Progressive Scratch Size Tungsten Coin", self.player) >= 2
        )

        regions["Early Prestige"].connect(regions["Late Prestige"])
        regions["Late Prestige"].connect(regions["Endgame"])

        # Place all locations into their regions
        for loc_name, loc_data in location_table.items():
            region = regions[loc_data.region]
            location = SSLocation(
                self.player,
                loc_name,
                loc_data.address,
                region
            )
            location.progress_type = loc_data.progress_type
            region.locations.append(location)

        # Place locked Progressive Prestige items at each "Prestige N" location.
        # The server awards these when the player checks the location by prestiging.
        for i in range(1, PRESTIGE_LOCATION_COUNT + 1):
            loc = self.multiworld.get_location(f"Prestige {i}", self.player)
            loc.place_locked_item(SSItem(
                "Progressive Prestige",
                item_table["Progressive Prestige"].classification,
                item_table["Progressive Prestige"].code,
                self.player,
            ))

        # Goal event in Endgame
        goal_region = regions["Endgame"]
        goal_location = SSLocation(
            self.player,
            "Win Final Chance",
            None,
            goal_region
        )
        goal_location.place_locked_item(
            SSItem("Victory", ItemClassification.progression, None, self.player)
        )
        goal_region.locations.append(goal_location)
        self.multiworld.completion_condition[self.player] = \
            lambda state: state.has("Victory", self.player)

    # Item Creation
    def create_item(self, name: str) -> SSItem:
        data = item_table[name]
        return SSItem(name, data.classification, data.code, self.player)

    def create_items(self) -> None:
        items_to_create: List[str] = []

        all_progressive_counts = {**PROGRESSIVE_ITEM_COUNTS, **PRESTIGE_PROGRESSIVE_ITEM_COUNTS}
        all_repeat_counts = {**STREAK_ITEM_COUNTS, **BONUS_ITEM_COUNTS}

        for item_name, data in item_table.items():
            if data.item_type == ItemType.LOCKED:
                continue  # Placed as locked items in create_regions
            elif data.item_type == ItemType.PROGRESSIVE:
                count = all_progressive_counts.get(item_name, 1)
                items_to_create.extend([item_name] * count)
            elif item_name in all_repeat_counts:
                items_to_create.extend([item_name] * all_repeat_counts[item_name])
            else:
                items_to_create.append(item_name)

        # Count how many locations need filling
        location_count = len(self.multiworld.get_unfilled_locations(self.player))
        item_count = len(items_to_create)

        # If there are fewer items than locations, pad with filler
        if item_count < location_count:
            filler_needed = location_count - item_count
            for _ in range(filler_needed):
                items_to_create.append(self.get_filler_item_name())

        # If there are more items than locations, trim least important items
        # (trim filler first, then useful, never progression)
        elif item_count > location_count:
            excess = item_count - location_count
            trimmable = [
                n for n in items_to_create
                if item_table[n].classification in (
                    ItemClassification.filler,
                    ItemClassification.trap
                )
            ]
            for name in trimmable[:excess]:
                items_to_create.remove(name)

        for item_name in items_to_create:
            self.multiworld.itempool.append(self.create_item(item_name))

    def get_filler_item_name(self) -> str:
        return self.multiworld.random.choice([
            "Small Cash Injection",
            "Large Cash Injection",
        ])

    # Rules
    def set_rules(self) -> None:
        set_rules(self)

    # Slot Data
    # Sent to the game client on connection so it knows
    # what settings were used for this seed
    def fill_slot_data(self) -> Dict[str, Any]:
        return {
            "game_version": "0.1.0",
            "death_link": False,
            "jackpot_points_per_item": JACKPOT_POINTS_PER_ITEM,
        }