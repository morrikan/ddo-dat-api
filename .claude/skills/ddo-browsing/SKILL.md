---
name: ddo-browsing
description: Use when the user wants to browse game object categories, weenie types, enhancement trees, or treasure/loot tables
---

# DDO Browsing

Browse game data by category.

## Weenie Types

When the user asks "what types of objects exist?" or wants to browse a category:

1. `curl -s "http://localhost:5138/DbProperties/WeenieTypeCounts"` — returns all types with counts.
2. If the user picks a type: `curl -s "http://localhost:5138/DbProperties/ByWeenieType/<weenieType>"` — returns `{id, idHex, name}` entries.

## Enhancement Trees

When the user asks about enhancement trees, enhancements, or class trees:

1. `curl -s "http://localhost:5138/DbProperties/EnhancementTrees"` — returns `{id, idHex, name}` for every tree.
2. For details on a specific tree: `curl -s "http://localhost:5138/DbProperties/<id>"` — use `ddo-property-rendering` to display.

## Treasure Tables

When the user asks about loot, treasure, or drop tables:

1. `curl -s "http://localhost:5138/DbProperties/TreasureTables"` — returns an array of IDs.
2. For details: `curl -s "http://localhost:5138/DbProperties/<id>"` — use `ddo-property-rendering` to display.
