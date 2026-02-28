---
name: ddo-treasure-mutation
description: Use when instantiating an item that gets its effects from a treasure table rather than from its own Weenie — typically difficulty-scaled named items
---

# DDO Treasure Mutation

Instantiate an item whose effects come from an external treasure table rather than from `Effect_OnCreationEffects` on the Weenie itself. These items have multiple difficulty variants, each with a different set of mutations.

## When to Use

- The item Weenie has no `Effect_OnCreationEffects` (bare stat stick)
- The user provides a treasure table ID and asks about a specific item
- You need to show all difficulty variants of an item

## Naming Convention

When multiple versions of an item exist, reference each by its minimum level: `"Item Name (Level X)"` where X is the `Usage_MinLevel` set by the Required Level mutation.

## Treasure Table Chain

Treasure tables can be nested. A typical chain for a named item:

```
Chest Table
  └─ Treasure_Array_{Tier}
       └─ Treasure_Entry (percentile)
            └─ Treasure_Entity → Intermediate Table
                                   └─ Treasure_Array_{Tier}
                                        └─ Treasure_Entry (percentile)
                                             ├─ Treasure_Entity → Item Weenie
                                             └─ Treasure_Mutation_Array → [mutations]
```

Walk the chain until you reach a `Treasure_Entry` that has both a `Treasure_Entity` (the base item Weenie) and a `Treasure_Mutation_Array` (the effects to apply).

## Difficulty Tiers

Each tier has its own `Treasure_Mutation_Array`:

| Property | Tier |
|----------|------|
| `Treasure_Array` | Normal |
| `Treasure_Array_Hard` | Hard |
| `Treasure_Array_Elite` | Elite |
| `Treasure_Array_Epic` | Epic Normal |
| `Treasure_Array_Epic_Hard` | Epic Hard |
| `Treasure_Array_Epic_Elite` | Epic Elite |

`Treasure_Array_Reaper`, `Treasure_Array_Epic_Reaper`, and `Treasure_Array_Epic_Casual` also exist but are not yet fully understood.

## Applying Mutations

Each `Treasure_Mutation_Entry` in the array contains a `Treasure_Mutation` value — this is a WeenieId for an effect. Process these exactly like `Effect_OnCreationEffects` entries:

1. Start with the base Weenie's properties as Context Properties.
2. Apply static mutators (**ddo-describe** § Static Mutators) — note that the Required Level mutation in the `Treasure_Mutation_Array` sets `Treasure_BaseLevel`/`Usage_MinLevel`, which drives the static mutators.
3. Process each `Treasure_Mutation` in order via **ddo-resolve-effect** (first pass: apply Mods, second pass: resolve Display equations).
4. Display the resolved item using the **Property Display** rules in `claude.md`.

## Percentile Branching and Display

A `Treasure_Entry` has a `Treasure_Percentile` (0–100). Multiple entries form a cumulative distribution — a die roll determines which entry is selected. To get the probability of a single entry, subtract the previous entry's percentile (or 0 for the first entry).

Percentile branches can nest across table levels. Multiply probabilities through the chain to get the final drop chance. Example:

```
Intermediate Table (entered at 100%)
├── 93% → Leaf A (no Mythic)
└──  7% → Leaf B
         ├── 93% → mutations + Mythic Weapon Boost +2   → 7% × 93% = 6.51%
         └──  7% → mutations + Mythic Weapon Boost +4   → 7% ×  7% = 0.49%
```

**When displaying effects, show the % chance of each effect.** Effects that appear in every branch are 100%. Effects that only appear in some branches get the combined probability of those branches. This lets players know how likely they are to get a specific enchantment.

Format example:
```
Enchantments:
- +5 Enhancement Bonus
- Blazing
- Incineration
- Red Augment Slot
- Mythic Weapon Boost +2 (6.51%)
- Mythic Weapon Boost +4 (0.49%)
```

Effects without a listed percentage are guaranteed (100%). Only annotate effects that have a probability below 100%.

## Limitations

- We cannot yet automatically find which treasure table an item drops from — the user must provide the table ID.
- Not all treasure tables are catalogued.
