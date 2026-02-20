---
name: ddo-property-rendering
description: Use when displaying full details of a game object by ID, or when interpreting property data from the DbProperties endpoint
---

# DDO Property Rendering

Fetch and display the properties of a game object.

## Fetching

`curl -s "http://localhost:5138/DbProperties/<id>"`

The ID can be decimal or `0x`-prefixed hex.

Response shape:
```json
{"propertyCollectionId": 123, "name": "...", "properties": [...]}
```

## Property Type Reference

Every property has `propertyType`, `name`, and `id`. The `propertyType` determines which value fields are present:

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

## Display Guidelines

Present interesting properties in a readable format. Skip properties listed in the hidden properties index below.

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

### Conditional properties

Only show these when the condition is met:

| Property | Show when |
|----------|-----------|
| Inventory_Encumbrance | Value > 999 |
| MaxDurability_Base | Value > 100 |
| Material | Not Steel and not Wood |

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

### Augment slots

Only count augment slots that have an `Augment_SlotName` property value. Slots without a name don't count.
