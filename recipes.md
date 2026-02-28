# Recipe System

## Data Model
- `RecipeData` — RecipeId, Name, DeviceId, DeviceName
- `DatCache.RecipeMap` — `Dictionary<uint, List<RecipeData>>`
  - Key: item weenie ID (the ingredient)
  - Value: all recipes that use that item

## How Recipes Work
- EldritchDevice (WeenieType 0x00200081) has `Device_Recipe_List` (IArrayProperty)
- Each `Device_Recipe_Entry` (IInt32Property) points to a recipe weenie
- Recipe weenie has:
  - `Recipe_Name` (IStringInfoProperty) — display name
  - `Recipe_Description` (IStringInfoProperty) — what it does
  - `Recipe_Slot_List` (IArrayProperty) — ingredient slots
  - `Recipe_Result_AddMutations` — effects added to item
  - `Recipe_Result_RemoveMutations` — effects removed from item
  - `Recipe_Result_ItemDID` — output item (if producing a new item)

## Important: Slot Indirection
`Recipe_Slot_Entry` does NOT point directly to an item. It points to a **slot definition object** which contains:
- `Ingredient_Name` (StringInfo) — display name with quantity
- `Ingredient_Entity` (IInt32Property) — the actual item weenie ID
- `Ingredient_Quantity` (Int32) — how many needed
- `Ingredient_Icon`, `Ingredient_Description`, etc.

Must follow this indirection: Recipe_Slot_Entry → slot def → Ingredient_Entity → actual item ID.

## Upgrade Chains
Many recipes form sequential upgrade paths (e.g., Attuned to Heroism tiers).
Each tier removes previous attunement + old effect, adds next attunement + upgraded effect.
Example: Pinion has 4 tiers costing 3/5/7/10 Commendations of Heroism.

## API
- `GET /Recipe/ForItem/{id}` — returns List<RecipeData> (empty array if none, not 404)
- 424 if cache not built

## Display Rules (in claude.md)
- Group recipes by deviceName
- Show as table: Cost | Removed | Added
- Cost excludes the item itself from ingredient list
- Omit section entirely if no recipes
