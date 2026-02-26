---
name: ddo-resolve-effect
description: Use when resolving an effect from a DbProperties object to determine what it does — produces a human-readable effect name with evaluated placeholders
---

# DDO Resolve Effect

Resolve an effect's properties into a human-readable name describing what the effect does. Modifies the context property collection as effects are processed.

## Input

- **Effect Properties**: the effect's own property collection (from the fetched effect Weenie)
- **Context Properties**: the item's accumulated property state — mutated as effects are applied

## Algorithm

### Case 1: Creation Effect

If the effect has a `Creation_Entity` child property:

1. Fetch the Weenie for the `Creation_Entity` value via `/DbProperties/{id}` and get its `Name` property.
2. Result: `"Create {name}"`
3. If `Creation_Quantity` is also present: `"Create {name} x{quantity}"`
4. Return.

### Case 2: Mod Array Effect

If the effect has a `Mod_Array`, process each `Mod` entry:

#### Processing a Mod

**When Mod_Op is `Push`:**

1. Find the property in **Context Properties** whose propertyId matches `Mod_Destination`. Create it as an empty array if not found. This is the **target array**.
2. Find the property in the Mod specified by `Mod_Source`. This is the **new property**.
3. Add the new property to the target array. The Context Properties now reflect this change.
4. If `Mod_Source` is `0x10000909` (Effect_Entry), the new property is itself an effect. Recurse into **ddo-resolve-effect** with:
   - **Effect Properties**: the Mod's properties (which contain the Effect_Entry)
   - **Context Properties**: the updated parent context (with the modified target array)

**When Mod_Op is `Set`, `Add`, `Subtract`, `Multiply`, or `Divide`:**

These are arithmetic operations on a scalar property in Context Properties.

1. Find the property in **Context Properties** whose propertyId matches `Mod_Destination`. Create it with value 0 if not found. This is the **target property**.
2. Find the property in the Mod specified by `Mod_Source`. Its value is the **base operand**.
3. If the Mod has an `Equation` child property, evaluate it via **ddo-eval-equation** (with context=Context Properties). The Equation scales/transforms the base operand — use the Equation's result as the **final operand**. If there is no Equation, the base operand is the final operand.
4. Apply the Mod_Op to the target property's value:
   - `Set`: target = final operand
   - `Add`: target = target + final operand
   - `Subtract`: target = target - final operand
   - `Multiply`: target = target * final operand
   - `Divide`: target = target / final operand
5. The Context Properties now reflect the updated value.

**When Mod_Op is `Or`, `Xor`, or `And`:**

These are bitwise operations on a BitField property in Context Properties.

1. Find the property in **Context Properties** whose propertyId matches `Mod_Destination`. Create it with value 0 if not found. This is the **target property** (must be a BitField).
2. Find the property in the Mod specified by `Mod_Source`. Its value is the **operand**.
3. Apply the bitwise operation to the target property's raw value:
   - `Or`: target = target | operand
   - `Xor`: target = target ^ operand
   - `And`: target = target & operand
4. The Context Properties now reflect the updated value.

**When Mod_Op is anything else:** STOP. Dump the full Mod entry and its properties, the current Context Properties, and the Effect Properties so we can investigate how to handle this case. Tell the user: "Encountered Mod_Op `{value}` — not yet handled. Here's the data for investigation."

#### Resolving the Effect Name

After processing Mods, build replacements from the display equations and resolve the name:

```
replacements = []

for N in [1, 2, 3, 4]:
    if Effect_DisplayN_Equation exists in Effect Properties:
        rep = ddo-eval-equation(equation, context=Context Properties)
        if rep is not null/empty: replacements.append(rep)

return ddo-stringinfo(Effect Properties[Effect_Name], replacements, context=Context Properties)
```

`Effect_DisplayN_Equation` properties may not all be present — only evaluate those that exist.
