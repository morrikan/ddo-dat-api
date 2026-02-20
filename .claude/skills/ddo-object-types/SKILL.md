---
name: ddo-object-types
description: Use when identifying what kind of game object a property collection represents — NPC, item, etc.
---

# DDO Object Types

After fetching an object from `/DbProperties/<id>`, use these rules to classify it.

## NPC

- `WeenieType` is `0x0000004F` or `0x0001004F`
- Property `Render_NeverSelectable` must NOT have value 1

## Item

- `WeenieType` is `0x00020081`
- Has a non-zero value for `Inventory_CompatibleSlot`
