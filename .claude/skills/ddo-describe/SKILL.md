---
name: ddo-describe
description: Use when displaying full details of a game object by ID, or when interpreting property data from the DbProperties endpoint
---

# DDO Describe

Fetch and display the properties of a game object, simulating instantiation.

## Conceptual Model

DbProperties objects are **Weenies** (templates). When the game creates an actual item, it instantiates the Weenie, applies its `Effect_OnCreationEffects` in order, and produces the final instance. This skill simulates that process to show what the player would see.

## Fetching

`curl -s "http://localhost:5138/DbProperties/<id>"`

The ID can be decimal or `0x`-prefixed hex.

Response shape:
```json
{"propertyCollectionId": 123, "name": "...", "properties": [...]}
```

## Display

Follow the **Property Display** rules in `CLAUDE.md` for rendering property types, hidden/conditional properties, weapon damage, etc.

## Effects

When the object has `Effect_OnCreationEffects` (an array of effects applied at instantiation), process them in two passes:

1. **First pass — apply all Mods**: Walk every effect in order using **ddo-resolve-effect**, processing all Mod operations (Set, Add, Push, etc.) to build the full accumulated Context Properties. Do NOT evaluate Display equations during this pass.
2. **Second pass — resolve Display equations**: Now that Context Properties are complete, go back and evaluate each effect's `Effect_DisplayN_Equation` properties via **ddo-eval-equation**, build the replacements, and resolve the effect names via **ddo-stringinfo**.

This two-pass approach is necessary because effects earlier in the list (e.g., skill bonuses) may have Display equations that depend on context values set by effects later in the list (e.g., Power effects). Display the resolved effect names as a list rather than showing the raw effect property trees.
