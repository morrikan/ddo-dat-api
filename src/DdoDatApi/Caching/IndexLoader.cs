using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using VoK.Sdk.Ddo;
using VoK.Sdk.Ddo.Enums;
using VoK.Sdk.Enums;
using VoK.Sdk.Properties;

namespace DdoDatApi.Caching;

public class IndexLoader {
    public static string CachePath => Path.Combine(AppContext.BaseDirectory, "indexcache.json");
    public static string TreasureMapPath => Path.Combine(AppContext.BaseDirectory, "treasuremap.json");
    public static string RecipeMapPath => Path.Combine(AppContext.BaseDirectory, "recipemap.json");

    public static void RefreshCacheFromDats(CancellationToken token) {
        var indexData = new IndexData();
        var treasureMap = new Dictionary<uint, List<uint>>();
        var recipeMap = new Dictionary<uint, List<RecipeData>>();
        var glfs = DatSource.GameLogicDat.FileList;
        var dbpRange = DatSource.IdRanges.FirstOrDefault(r => r.Name == ObjectType.DbProperties.ToString());
        var timer = Stopwatch.StartNew();

        Console.WriteLine($"Building Cache from GameLogic dat file (scanning {glfs.Count} dat objects)...");
        var found = 0;
        var iter = 0;
        var updateFrequency = TimeSpan.FromSeconds(30);
        var lastUpdate = DateTime.UtcNow;

#if DEBUG
        updateFrequency = TimeSpan.FromSeconds(2);
#endif
        indexData.ClientVersion = DatSource.ClientFileInfo.FileVersion;
        indexData.CompiledOnUtc = DateTime.UtcNow;

        foreach (var glf in glfs) {
            if (token.IsCancellationRequested) return;

            var id = glf.Id;
            iter++;
            if (id >= 0x70000000 && id < 0x71000000)
                id += 0x09000000;

            if (dbpRange.IsInRange(id)) {
                var dbp = DatSource.PropertyMaster.GetPropertyCollection(id);
                if (dbp == null) continue;
                var wt = dbp.GetWeenieType();

                if (!indexData.WeenieTypes.ContainsKey(wt))
                    indexData.WeenieTypes.Add(wt, new List<uint>());
                if (!indexData.WeenieTypes[wt].Contains(id))
                    indexData.WeenieTypes[wt].Add(id);

                var name = NameGenerator.GetName(DatSource.PropertyMaster, dbp, null);
                if (!string.IsNullOrWhiteSpace(name)) {
                    if (!indexData.NameLookup.ContainsKey(id))
                        indexData.NameLookup.Add(id, name);
                    // some ids are duplicates? what's up with that?

                    if (!indexData.Names.ContainsKey(name))
                        indexData.Names.Add(name, new List<uint>());

                    if (!indexData.Names[name].Contains(id))
                        indexData.Names[name].Add(id);
                }

                if (wt == (uint)WeenieType.Spell)
                    indexData.Spells.Add(id);

                var questName = dbp.GetStringInfoProperty((uint)DdoProperty.Quest_Name);
                if (questName != null)
                    indexData.Quests.Add(new NamedItem() { Id = id, Name = questName.Text });

                var personality = dbp.GetEnumProperty((uint)DdoProperty.SentientPersonality);
                if (personality != null)
                    indexData.SentientPersonalities.Add(id);

                var treasureArray = dbp.GetProperty((uint)DdoProperty.Treasure_Array);
                if (treasureArray != null) {
                    indexData.TreasureTables.Add(id);
                    var items = new List<uint>();
                    CollectTreasureItems(dbp.Properties.Values, items, new HashSet<uint> { id });
                    if (items.Count > 0)
                        treasureMap[id] = items;
                }

                var enhTree = dbp.GetProperty((uint)DdoProperty.EnhancementTree_Name);
                if (enhTree != null)
                    indexData.EnhancementTrees.Add(id);

                if (wt == 0x0000004F) {
                    var canBeUsed = dbp.GetBytePropertyValue((uint)DdoProperty.Usage_CanBeUsed) ?? 0;
                    if (canBeUsed > 0)
                        indexData.NPCs.Add(id);
                }

                if (wt == 0x00200081 && !string.IsNullOrWhiteSpace(name) && !IsExcludedDevice(name)) {
                    CollectRecipes(dbp, id, name, recipeMap);
                }

                found++;

                if (DateTime.UtcNow > lastUpdate + updateFrequency) {
                    var progress = 100 * iter / glfs.Count;
                    Console.WriteLine($"Progress: {progress}% ({found} through {iter} of {glfs.Count} objects)");
                    lastUpdate = DateTime.UtcNow;
                }
            }
        }

        Console.WriteLine($"Successfully built indexes over {found} items in {timer.Elapsed}.");

        Console.WriteLine($"Loading other miscellaneous index data...");
        indexData.WellKnown.Add("Feat Directory", (uint)Feat.FeatDirectory);
        indexData.WellKnown.Add("Base Skin Mappings", (uint)Uniquedb.BaseSkinMappings);

        // important to note - unless the SDK is rebuilt/updated, this won't change and get new values
        foreach (var wc in Enum.GetValues(typeof(Weeniecontent)))
            if (!indexData.WellKnown.ContainsKey(wc.ToString()))
                indexData.WellKnown.Add(wc.ToString(), (uint)wc);

        indexData.LastIndexDuration = timer.Elapsed;
        Console.WriteLine($"Miscellaneous stuff done.");

        DatCache.Index = indexData;
        DatCache.TreasureMap = treasureMap;
        DatCache.RecipeMap = recipeMap;

        BuildWeaponProficiencyMap();

        SaveJson(CachePath, indexData, "indexing data");
        SaveJson(TreasureMapPath, treasureMap, "treasure map");
        SaveJson(RecipeMapPath, recipeMap, "recipe map");
    }

    private static void SaveJson(string path, object data, string label) {
        using (StreamWriter sw = File.CreateText(path))
        using (JsonTextWriter writer = new JsonTextWriter(sw)) {
            JsonSerializer serializer = new JsonSerializer();
            writer.Formatting = Formatting.Indented;
            serializer.Serialize(writer, data);
        }

        var fileInfo = new FileInfo(path);
        var kb = fileInfo.Length / 1024;
        var mb = kb / 1024;
        var sizeStr = mb > 0 ? $"{mb}MB" : $"{kb}kb";
        Console.WriteLine($"Saved {label} ({sizeStr}) to {path}");
    }

    private static void CollectTreasureItems(IEnumerable<IProperty> properties, List<uint> items, HashSet<uint> visited) {
        foreach (var prop in properties) {
            if (prop.PropertyId == (uint)DdoProperty.Treasure_Entity && prop is IInt32Property i32) {
                var val = (uint)(i32.Int32Value ?? 0);
                if (val == 0 || !visited.Add(val))
                    continue;

                var child = DatSource.PropertyMaster.GetPropertyCollection(val);
                if (child == null)
                    continue;

                if (child.GetWeenieType() == 0)
                    CollectTreasureItems(child.Properties.Values, items, visited);
                else {
                    var normalized = val >= 0x70000000 && val < 0x71000000 ? val + 0x09000000 : val;
                    if (!items.Contains(normalized))
                        items.Add(normalized);
                }
            }
            else if (prop is IArrayProperty arr) {
                CollectTreasureItems(arr.Properties, items, visited);
            }
        }
    }

    private static readonly string[] ExcludedDevicePrefixes =
    {
        "Ruby of", "Diamond of", "Topaz of", "Sapphire of",
        "Tome of", "Fragmented Tome of", "Upgrade Tome of",
        "Dust of", "+5 Ability", "Augments: Level",
        "Ability Score", "Test "
    };

    private static bool IsExcludedDevice(string name) {
        foreach (var prefix in ExcludedDevicePrefixes)
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private static void CollectRecipes(IPropertyCollection device, uint deviceId, string deviceName, Dictionary<uint, List<RecipeData>> recipeMap) {
        var recipeList = device.GetArrayProperty((uint)DdoProperty.Device_Recipe_List);
        if (recipeList == null) return;

        foreach (var entry in recipeList.Properties) {
            if (entry.PropertyId != (uint)DdoProperty.Device_Recipe_Entry || entry is not IInt32Property recipeRef)
                continue;

            var recipeId = (uint)(recipeRef.Int32Value ?? 0);
            if (recipeId == 0) continue;

            var recipe = DatSource.PropertyMaster.GetPropertyCollection(recipeId);
            if (recipe == null) continue;

            var recipeName = recipe.GetStringInfoProperty((uint)DdoProperty.Recipe_Name)?.Text;
            var recipeDesc = recipe.GetStringInfoProperty((uint)DdoProperty.Recipe_Description)?.Text;

            var recipeData = new RecipeData {
                RecipeId = recipeId >= 0x70000000 && recipeId < 0x71000000 ? recipeId + 0x09000000 : recipeId,
                Name = recipeName,
                DeviceId = deviceId,
                DeviceName = deviceName
            };

            var slotList = recipe.GetArrayProperty((uint)DdoProperty.Recipe_Slot_List);
            if (slotList == null) continue;

            foreach (var slot in slotList.Properties) {
                if (slot.PropertyId != (uint)DdoProperty.Recipe_Slot_Entry || slot is not IInt32Property slotRef)
                    continue;

                var slotDefId = (uint)(slotRef.Int32Value ?? 0);
                if (slotDefId == 0) continue;

                var slotDef = DatSource.PropertyMaster.GetPropertyCollection(slotDefId);
                if (slotDef == null) continue;

                var ingredientProp = slotDef.GetProperty((uint)DdoProperty.Ingredient_Entity) as IInt32Property;
                if (ingredientProp == null) continue;

                var itemId = (uint)(ingredientProp.Int32Value ?? 0);
                if (itemId == 0) continue;

                var normalizedItemId = itemId >= 0x70000000 && itemId < 0x71000000 ? itemId + 0x09000000 : itemId;

                if (!recipeMap.ContainsKey(normalizedItemId))
                    recipeMap.Add(normalizedItemId, new List<RecipeData>());

                if (!recipeMap[normalizedItemId].Any(r => r.RecipeId == recipeData.RecipeId))
                    recipeMap[normalizedItemId].Add(recipeData);
            }
        }
    }

    public static void LoadIndexCache() {
        if (File.Exists(CachePath)) {
            DatCache.Index = LoadJson<IndexData>(CachePath, "indexing data");
        }

        if (File.Exists(TreasureMapPath)) {
            DatCache.TreasureMap = LoadJson<Dictionary<uint, List<uint>>>(TreasureMapPath, "treasure map");
        }

        if (File.Exists(RecipeMapPath)) {
            DatCache.RecipeMap = LoadJson<Dictionary<uint, List<RecipeData>>>(RecipeMapPath, "recipe map");
        }

        BuildWeaponProficiencyMap();
    }

    private static void BuildWeaponProficiencyMap()
    {
        DatCache.SimpleProficiency  = DatSource.PropertyMaster.GetPropertyCollection(DatCache.SimpleProficiencyId);
        DatCache.MartialProficiency = DatSource.PropertyMaster.GetPropertyCollection(DatCache.MartialProficiencyId);
        DatCache.ExoticProficiency  = DatSource.PropertyMaster.GetPropertyCollection(DatCache.ExoticProficiencyId);

        var baseNames = new Dictionary<uint, string>();
        if (DatCache.SimpleProficiency?.Name  != null) baseNames[DatCache.SimpleProficiencyId]  = DatCache.SimpleProficiency.Name;
        if (DatCache.MartialProficiency?.Name != null) baseNames[DatCache.MartialProficiencyId] = DatCache.MartialProficiency.Name;
        if (DatCache.ExoticProficiency?.Name  != null) baseNames[DatCache.ExoticProficiencyId]  = DatCache.ExoticProficiency.Name;

        var map = new Dictionary<uint, string>();

        foreach (var (name, ids) in DatCache.Index.Names)
        {
            if (!name.StartsWith("Proficiency: ", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var id in ids)
            {
                var props = DatSource.PropertyMaster.GetPropertyCollection(id);
                if (props == null) continue;

                var weaponTypeProp = props.GetEnumProperty((uint)DdoProperty.Feat_WeaponType);
                if (weaponTypeProp == null || weaponTypeProp.UInt32Value == 0) continue;

                var baseFeatRaw = props.GetInt32PropertyValue((uint)DdoProperty.Feat_BaseFeat);
                if (baseFeatRaw == null || baseFeatRaw == 0) continue;

                var baseFeatId = (uint)baseFeatRaw.Value;
                if (baseFeatId >= 0x70000000 && baseFeatId < 0x71000000)
                    baseFeatId += 0x09000000;

                if (baseNames.TryGetValue(baseFeatId, out var profName))
                    map[weaponTypeProp.UInt32Value.Value] = profName;
            }
        }

        DatCache.WeaponProficiencyNames = map;
        Console.WriteLine($"Built weapon proficiency map ({map.Count} weapon types).");
    }

    private static T LoadJson<T>(string path, string label) {
        var fileInfo = new FileInfo(path);
        var kb = fileInfo.Length / 1024;
        var mb = kb / 1024;
        var sizeStr = mb > 0 ? $"{mb}MB" : $"{kb}kb";

        using (FileStream fileStream = File.Open(path, FileMode.Open))
        using (StreamReader streamReader = new StreamReader(fileStream))
        using (JsonTextReader jsonReader = new JsonTextReader(streamReader)) {
            JsonSerializer serializer = new JsonSerializer();
            var data = serializer.Deserialize<T>(jsonReader);
            Console.WriteLine($"Loaded {label} ({sizeStr}) from {path}");
            return data;
        }
    }
}
