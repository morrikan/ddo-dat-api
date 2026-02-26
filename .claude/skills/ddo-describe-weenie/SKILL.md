---
name: ddo-describe-weenie
description: Use when displaying the raw template (Weenie) data of a game object, without simulating effect processing or instantiation
---

# DDO Describe Weenie

Fetch and display the raw Weenie properties of a game object as-is, with no modification. The Weenie is described exactly as stored — no effects are processed, no equations evaluated, no instantiation simulated.

## Fetching

`curl -s "http://localhost:5138/DbProperties/<id>"`

The ID can be decimal or `0x`-prefixed hex.

Response shape:
```json
{"propertyCollectionId": 123, "name": "...", "properties": [...]}
```

## Display

Follow the **Property Display** rules in `CLAUDE.md` for rendering property types, hidden/conditional properties, weapon damage, etc.

## Effect_OnCreationEffects

Do **not** process or resolve effects. Show the raw `Effect_OnCreationEffects` array entries with their `Effect` reference names and IDs, so the user can see what the template defines without any instantiation logic applied.
