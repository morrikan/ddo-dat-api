# Architecture & Patterns

## Caching System
- `DatCache` — static holder for all caches (`Index`, `TreasureMap`, `RecipeMap`)
- `IndexLoader.RefreshCacheFromDats()` — iterates all game_logic.dat objects, categorizes by WeenieType
- `IndexLoader.LoadIndexCache()` — loads persisted JSON caches on startup
- JSON persistence via `SaveJson`/`LoadJson` using Newtonsoft.Json
- Cache files: `indexcache.json`, `treasuremap.json`, `recipemap.json`

## ID Normalization
- 0x70* IDs are converted to 0x79* by adding 0x09000000
- Always normalize before storing in caches

## Property Access (SDK)
- `dbp.GetProperty((uint)DdoProperty.X)` — generic
- `dbp.GetArrayProperty(...)` — IArrayProperty
- `dbp.GetStringInfoProperty(...)` — IStringInfoProperty, use `.Text`
- `dbp.GetEnumProperty(...)` — IEnumProperty, use `.UInt32Value` or `.EnumValue`
- `dbp.GetBytePropertyValue(...)` — returns byte?
- Cast to typed interface: `prop is IInt32Property i32` then `i32.Int32Value`

## Extension Methods (Extensions.cs)
- `string.IsValid(out uint parsedId, out IActionResult error)` — hex/decimal ID parsing
- `IPropertyCollection.GetWeenieType()` — returns uint WeenieType

## Controller Pattern
- All controllers use `[ApiController]` + `[Route("[controller]")]`
- ID params are strings, parsed with `id.IsValid(...)`
- 424 FailedDependency when cache not loaded
- Base URL: http://localhost:5138

## EldritchDevice Filtering
Excluded prefixes (not useful crafting stations):
Ruby of, Diamond of, Topaz of, Sapphire of, Tome of, Fragmented Tome of,
Upgrade Tome of, Dust of, +5 Ability, Augments: Level, Ability Score, Test
