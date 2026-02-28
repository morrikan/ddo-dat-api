# DDO Dat API

This project serves game data from Dungeons and Dragons Online. Users ask about items, monsters, spells, NPCs, enhancement trees, loot tables, and other game entities. The `ddo-dat-api` MCP server and WebApp provide read-only access to this data.

When this file is loaded, tell the user "Loaded claude.md from ddodatapi"

## Vocabulary

- **Weenie**: A template (class definition) for a game object, stored in DbProperties. Weenies define the base data — properties, effects, stats — but are not what players interact with directly.
- **Instance**: A concrete game object created from a Weenie at runtime. When a player loots an item or encounters a monster, the game instantiates the Weenie, applies its `Effect_OnCreationEffects` in order, and produces the final instance. Players only ever see and care about instances.
- **WeenieId**: An object's DbProperties ID (`propertyCollectionId`). References the Weenie's template data.
- **Effect_OnCreationEffects**: Constructor logic on a Weenie. These effects run in order during instantiation, modifying the instance's properties (e.g., setting Treasure_BaseLevel, applying skill bonuses, binding the item). The template data alone doesn't reflect the final item — you must simulate this effect chain to resolve what the player sees.

## Presentation

- Non-enum integers `0x10000000` and above: show in hex with `0x` prefix, all 8 digits, color `#FFF5C6`.
- Non-enum integers below `0x10000000`: show as regular base-10 numbers.
- Never show a word in more than one color.

## Property Display

These rules apply whenever rendering DbProperties data (used by `ddo-describe`, `ddo-describe-weenie`, and any other property display).

### Property Type Reference

Every property has `propertyType`, `propertyName`, and `propertyId`. The `propertyType` determines which value fields are present:

| propertyType | Value fields | Rendering |
|---|---|---|
| `String` | `value` (string) | Show text |
| `StringInfo` | `value` (string), `key`, `table` | Show `value` text |
| `Byte` | `value` (byte) | 1=true, 0=false for booleans; otherwise the number |
| `Int32` | `value` (int) | Hex |
| `Int64` | `value` (long) | Hex |
| `UInt32` | `value` (uint) | Hex |
| `UInt64` | `value` (ulong) | Hex |
| `Float` | `value` (float) | Decimal |
| `Double` | `value` (double) | Decimal |
| `Enum` | `value` (uint), `sdkEnum`, `enumValue` (string) | Show `enumValue`; fall back to `value` if null |
| `BitField` | `raw` (byte[]), `sdkEnum`, `values` (string[]) | Show `values` — the active flag names |
| `Array` | `properties` (array) | Recurse — each nested item has its own `propertyType` |
| `Vector` | `value` ({X, Y, Z}) | Coordinates |
| `Position` | `region`, `lbX`, `lbY`, `cell`, `heading`, `instanceNum`, `offset`, `rotation` | Location fields |

### Hidden properties

Unless specifically asked, never display these:

| Property | Reason |
|----------|--------|
| PhysObj | Internal rendering |
| ParentingOrientation | Internal rendering |
| Examination_Target_m_aControllers | Internal reference list |
| Physics_EtherealityType | Physics/rendering flag, not gameplay |
| Physics_EtherealToType | Physics/rendering flag, not gameplay |
| Physics_PlacementEtherealToType | Physics/rendering flag, not gameplay |
| Physics_MissilePlacementRaycastEtherealToType | Physics/rendering flag, not gameplay |
| Item_Value | Never relevant to players |
| Missile_InitialSpeed | Never relevant to players |
| Durability_Hardness | Never relevant to players |
| Inventory_AmmoType | Players know this intrinsically |
| Combat_AttackType | Players know this intrinsically |

### Weapon damage line

Combine `BaseWeaponDamageDiceModifier`, `DamageValue`, `DamageFlags`, `Combat_CriticalHitRange`, and `Combat_CriticalHitMod` into a single **Damage** line. Do not show these as separate properties.

Format: `{modifier}[{dice}] {type} {21 - CriticalHitRange}-20x{CriticalHitMod}`
- **Modifier**: if `BaseWeaponDamageDiceModifier` is 1.0, omit it and the brackets. Just show the dice.
- **Type**: first value from `DamageFlags` (e.g., Slash, Pierce)

Examples:
- `3.6[2d6] Pierce 19-20x3` — modifier != 1: show modifier, brackets around dice
- `2d6 Slash 17-20x3` — modifier = 1: no modifier, no brackets

### Hit/Damage ability

Combine `Combat_HitAbilityMod_Multiple` and `Combat_DamageAbilityMod_Multiple` into a single **Hit/Dmg Ability** line. Truncate each ability name to 3 characters.

Example: `Dex/Dex`, `Str/Str`, `Dex/Str`

### Integer display

- **Enum types**: always show `enumValue` (the human-readable name).
- **Non-enum integers below `0x10000000`**: show as a regular base-10 number (e.g., 100, 5000).
- **Non-enum integers `0x10000000` and above**: show in hex with `0x` prefix, all 8 digits, in color `#FFF5C6`.

### Arrays

Indent nested array content so the hierarchy is visually clear.

### Set bonuses

For any property named like `Item_SetBonus_*`, call `/Set/{value}` to look up the set entry, and display the `SetBonus_Name` from the result.

### Weight

`Inventory_Encumbrance` is stored in tenths of a pound. Divide by 100 and display as `X lbs` (e.g., 5000 → 50 lbs).

### Augment slots

Only count augment slots that have an `Augment_SlotName` property value. Slots without a name don't count.

### Recipes

Fetch recipes for the item via `GET /Recipe/ForItem/{id}`. If the array is empty, omit the Recipes section entirely. If recipes exist, group them by `deviceName` from the response. For each group, show a heading with the device name, then for each recipe fetch its full data via `/DbProperties/{recipeId}` and display as a table with these columns:

| Column | Source |
|--------|--------|
| Cost | `Recipe_Slot_List` → each `Recipe_Slot_Entry` reference name (exclude the item itself) |
| Removed | `Recipe_Result_RemoveMutations` → each `Recipe_Result_MutationDID` reference name |
| Added | `Recipe_Result_AddMutations` → each `Recipe_Result_MutationDID` reference name |

## Display Fields by WeenieType

When displaying an object, show only the fields listed for its WeenieType (in order). Fields not listed here should be omitted unless the user specifically asks.

Render the primary fields (everything except Effects, Recipes, Augment Slots, and Set Bonuses) in a two-column table with **Field** and **Value** columns. Effects, Augment Slots, Set Bonuses, and Recipes are multi-value sections — display those as lists or sub-tables below the main table.

### Augment (`0x000D0081`)

1. Name
2. Augment Type (`Augment_SlotTypes`)
3. Minimum Level (`Usage_MinLevel`)
4. Rarity
5. Bind Status
6. Description (`Item_Description`)
7. Effects (resolved from `Effect_OnCreationEffects` and `Augment_OnEquipEffects`, skip `0x00000000` entries)
8. Set Bonus (`SentientFiligreeSetBonus` — filigrees only)
9. Weight (`Inventory_Encumbrance` ÷ 100)
10. Max Stack Size (`Inventory_MaxStackSize`)

### Jewelry / Clothing (`0x00070081`, `0x00030081`)

1. Name
2. Slot (derived from `Inventory_DefaultSlot` — e.g., Finger, Wrists, Neck, Trinket)
3. Minimum Level (`Usage_MinLevel`)
4. Binding (`Inventory_IsBoundOnAcquire`, `Inventory_IsBoundOnEquip`, `Inventory_BoundToAccount`)
5. Description (`Item_Description`)
6. Effects (resolved `Effect_OnCreationEffects`)
7. Augment Slots (named slots from `Augment_SlotArray` + effect-created slots)
8. Set Bonuses (`Item_SetBonus_1`, `Item_SetBonus_2`)
9. Material (`Material`)
10. Durability (`MaxDurability_Base`)
11. Weight (`Inventory_Encumbrance` ÷ 100)
12. Recipes (see Recipes rendering rules)

### Shield (`0x00010081`)

1. Name
2. Shield Type (`Combat_ShieldType` — e.g., Buckler, Small Shield, Large Shield, Tower Shield)
3. Minimum Level (`Usage_MinLevel`)
4. Binding (`Inventory_IsBoundOnAcquire`, `Inventory_IsBoundOnEquip`, `Inventory_BoundToAccount`)
5. Shield Bonus (`Combat_ShieldBonus`)
6. Max Dex Bonus (`Combat_MaxDexBonus` — omit if 99 or higher, that means no cap)
7. Damage Reduction (`Combat_BlockingDamageReduction`)
8. Armor Check Penalty (`Combat_SkillCheckPenalty` — show as negative; omit if 0)
9. Arcane Spell Failure (`Spell_SpellFailureChance` — show as percentage; omit if 0)
10. Damage (combined line — see Weapon damage line rules; this is shield-bash damage)
11. Hit/Dmg Ability (combined line — see Hit/Damage ability rules)
12. Description (`Item_Description`)
13. Effects (resolved `Effect_OnCreationEffects`)
14. Augment Slots (named slots from `Augment_SlotArray` + effect-created slots)
15. Set Bonuses (`Item_SetBonus_1`, `Item_SetBonus_2`)
16. Material (`Material`)
17. Durability (`MaxDurability_Base`)
18. Weight (`Inventory_Encumbrance` ÷ 100)
19. Recipes (see Recipes rendering rules)

### Weapon (`0x00020081`) — melee and ranged

1. Name
2. Weapon Type (`Combat_WeaponType`)
3. Damage (combined line — see Weapon damage line rules)
4. Hit/Dmg Ability (combined line — see Hit/Damage ability rules)
5. Handedness (two-handed if `Inventory_PrecludedSlot` includes Weapon2)
6. Minimum Level (`Usage_MinLevel`)
7. Binding (`Inventory_IsBoundOnAcquire`, `Inventory_IsBoundOnEquip`, `Inventory_BoundToAccount`)
8. Accepts Sentience (`AcceptsSentience`)
9. Description (`Item_Description`)
10. Effects (resolved `Effect_OnCreationEffects`)
11. Augment Slots (named slots from `Augment_SlotArray` + effect-created slots)
12. Set Bonuses (`Item_SetBonus_1`, `Item_SetBonus_2`)
13. Material (`Material`)
14. Durability (`MaxDurability_Base`)
15. Weight (`Inventory_Encumbrance` ÷ 100)
16. Recipes (see Recipes rendering rules)

## API

Base URL: `http://localhost:5138`. Use `curl -s` to call them.

## Behavior

Ask once per query if the user wants to see a plan or just have it execute.

If you don't know something about DDO game mechanics, say so rather than guessing.

## SDK

When looking for a property in C# code with access to the SDK, reference it by ID rather than by name. The ID is in the `DdoProperty` enum. Access it on a property collection with `GetProperty((uint)DdoProperty.{name})`.

## Common Patterns

- **Name to details**: Search by name, get IDs, fetch properties. This is the most common flow.
- **424 errors**: The index hasn't loaded. Tell the user the cache needs to be rebuilt.
- **ID format**: Use `0x` prefix for hex IDs. The API accepts both `0x`-prefixed hex and plain integers.
- **`0x70` IDs**: `/DbProperties/{id}` automatically converts `0x70______` to `0x79______`.
- **EntityDesc**: An entity has an EntityDesc if and only if it has a `PhysObj` property. The `PhysObj` value is the ID to use when fetching the EntityDesc.

## Skills

Project-specific skills in `.claude/skills/` handle the main workflows:

| Skill | When to use |
|-------|-------------|
| `ddo-name-lookup` | User asks about a named thing |
| `ddo-describe` | Displaying full details of a game object (simulates instantiation) |
| `ddo-describe-weenie` | Displaying raw Weenie template data (no effect processing) |
| `ddo-treasure-mutation` | Instantiating items from treasure tables (difficulty-scaled named items) |
| `ddo-resolve-effect` | Resolving an effect into a human-readable name |
| `ddo-eval-equation` | Evaluating Effect_Display equations |
| `ddo-stringinfo` | Processing StringInfo with placeholder replacements |
| `ddo-strings` | Looking up string table entries by key/table ID |
| `ddo-object-types` | Identifying what kind of game object something is |
| `ddo-browsing` | Browsing categories, enhancement trees, treasure tables |
| `ddo-media` | Images, sounds |
| `ddo-cache` | Checking if data is loaded |
| `ddo-search` | Searching by keyword or pattern |

## Preferences
- Don't guess property types from the SDK — ask the user, then check the nuget packages XML docs. Don't decompile it.
- Don't try to read the DdoProperty enum (too large, crashes)
- Assume enum values exist unless compilation fails
- Use `id.IsValid(out var parsedId, out var error)` extension for ID parsing in the Dat API
- Prefer fields in a table (Field/Value), with multi-value sections (effects, recipes, augments) below
- Use `.editorconfig` settings for brackets when generating C# code

## API Gotchas
- **Set bonus lookup**: `/Set/{value}` takes the uint value (e.g., `0x00000105`), NOT the enum name
- **Material**: `Material` is an Int32 referencing a DbProperties object. Fetch via `/DbProperties/{value}` and read `Material_Name` (StringInfo) for the display name
- **Equation result rounding**: `floor(baseValue + driverProp.value * multiplier)` — always floor the result
- **JSON Parsing**: use `jq` to parse json values instead of piping things through python
