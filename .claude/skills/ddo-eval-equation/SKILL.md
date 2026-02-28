---
name: ddo-eval-equation
description: Use when evaluating Effect_Display equations (1-4) to resolve the numeric or text values used in effect names and descriptions
---

# DDO Equation Evaluation

Resolve the value of an Effect equation property by walking its child properties and applying game logic.

## When to Use

- You need the resolved value of `Effect_Display1_Equation`, `Effect_Display2_Equation`, `Effect_Display3_Equation`, or `Effect_Display4_Equation`
- A Mod has an `Equation` child property that scales its operand

## Input

- **equation**: the property collection (children) of one of the supported equation properties
- **context**: the item's accumulated property collection — this includes properties set by earlier effects during creation (e.g., a Mod with `Mod_Op=Set` and `Mod_Channel=Treasure_BaseLevel` that sets `Treasure_BaseLevel=10` means the context should contain `Treasure_BaseLevel=10`)

## Context: How Effects Modify Item Properties

When processing `Effect_OnCreationEffects`, effects are applied in order. A Mod with `Mod_Op=Set` adds or overrides a property on the item's property collection. For example, the Minimum Level effect typically has:
- `Mod_Op`: Set
- `Mod_Channel`: Treasure_BaseLevel
- `Treasure_BaseLevel`: 10

This means after that effect is processed, the item's context should include `Treasure_BaseLevel = 10`. Later equations can then look up `Treasure_BaseLevel` by its property ID (`0x1000A6D1`) in the context.

## Algorithm

If the equation has no `Equation_Solution` property, return empty/null.

`Equation_Solution` names the **target property** that the equation's result applies to — it is a property ID, not the result itself. Proceed to evaluate:

**Zero-value convention**: A value of `0x00000000` for `Equation_ValueDriver`, `Equation_LevelDriver`, or `Equation_Progression` means the field is unused — treat it as absent and skip that evaluation path.

### Step 1: Value Driver Path

```
valueDriver    = equation child property value of Equation_ValueDriver
solution       = equation child property value of Equation_Solution
baseValue      = equation child property value of Equation_ValueDriverBaseBonus ?? 0
multiplier     = equation child property value of Equation_ValueDriverMultiplier ?? 1
driverLocation = equation child property value of Equation_ValueDriverLocation

if valueDriver is present and non-zero:
    if driverLocation == "Source" (0x03):
        // Look up the driver property on the item's Context Properties
        driverProp = context property whose propertyId matches the valueDriver value
    else:
        // Default/Target (0x04): look in the equation's own children
        driverProp = equation child property whose ID matches the valueDriver value
        // If not found in equation children, fall back to context
        if driverProp is null:
            driverProp = context property whose propertyId matches the valueDriver value

    if driverProp is null:
        return empty/null

    return floor(baseValue + (driverProp.value * multiplier))
```

### Step 2: Level Driver + Progression Path

```
levelDriver  = equation child property of Equation_LevelDriver
progression  = equation child property value of Equation_Progression

if levelDriver and progression are both present and non-zero:
    // First, try to find the level driver value in the equation's own children
    levelDriverValue = equation child property whose propertyId matches levelDriver.value

    // If not found, search the item's accumulated context by propertyId
    if levelDriverValue is null:
        levelDriverValue = context property whose propertyId matches levelDriver.value

    // If still not found, return empty/null — we cannot resolve this equation
    if levelDriverValue is null:
        return empty/null

    prgFile  = fetch the Weenie for the progression ID
    prgArray = prgFile child property "Progression_Array"
    prgEntry = prgArray.properties[levelDriverValue - 1]
        // This is a 1-based index into the array

    if prgEntry.propertyName == "Progression_Level_Value":
        return prgEntry.text (or value)
    else if prgEntry.propertyName == "Progression_Level_DID":
        return invoke ddo-name-lookup with prgEntry.value

    otherwise:
        Tell the user: "Tell Morrikan that Claude couldn't resolve equation
        progression propertyName '{prgEntry.propertyName}' and the skill
        needs to be updated."
```

## Notes

- `Equation_Solution` is a property ID naming the target stat being modified — it is NOT the result value. The actual result comes from the Value Driver or Level Driver paths.
- `Equation_ValueDriverLocation` controls where the Value Driver property is looked up:
  - `Source` (`0x03`): look on the **item's Context Properties** (the item being instantiated)
  - `Target` (`0x04`) or absent: look in the **equation's own children** first, then fall back to context
- The progression array is 1-indexed: a `levelDriverValue` of 1 maps to `properties[0]`.
- When the level driver property can't be found in either the equation or the item context, return empty/null rather than guessing. This can be reinvestigated if it causes problems.
