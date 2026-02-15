using DdoDatApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoK.Sdk.Ddo;
using VoK.Sdk.Ddo.Enums;
using VoK.Sdk.Enums;

namespace DdoDatApi.Caching;

public class IndexLoader
{
    private static string CachePath => Path.Combine(AppContext.BaseDirectory, "indexcache.json");

    public static void RefreshCacheFromDats(CancellationToken token)
    {
        var indexData = new IndexData();
        var glfs = DatSource.GameLogicDat.FileList;
        var dbpRange = DatSource.IdRanges.FirstOrDefault(r => r.Name == ObjectType.DbProperties.ToString());
        var timer = Stopwatch.StartNew();

        Console.WriteLine($"Building Cache from GameLogic dat file (scanning {glfs.Count} dat objects)...");
        var found = 0;
        var emitEveryPercent = 5;
        var lastEmit = 0;

        foreach (var glf in glfs)
        {
            if (token.IsCancellationRequested) return;

            var id = glf.Id;
            if (id >= 0x70000000 && id < 0x70FFFFFF)
                id += 0x09000000;

            if (dbpRange.IsInRange(id))
            {
                var dbp = DatSource.PropertyMaster.GetPropertyCollection(id);
                if (dbp == null) continue;
                var wt = dbp.GetWeenieType();

                if (!indexData.WeenieTypes.ContainsKey(wt))
                    indexData.WeenieTypes.Add(wt, new List<uint>());
                indexData.WeenieTypes[wt].Add(id);

                var name = NameGenerator.GetName(DatSource.PropertyMaster, dbp, null);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (!indexData.NameLookup.ContainsKey(id))
                        indexData.NameLookup.Add(id, name);
                    // some ids are duplicates? what's up with that?

                    if (!indexData.Names.ContainsKey(name))
                        indexData.Names.Add(name, new List<uint>());
                    indexData.Names[name].Add(id);
                }

                if (wt == (uint)WeenieType.Spell)
                    indexData.Spells.Add(id);

                var questName = dbp.GetStringInfoProperty((uint)DdoProperty.Quest_Name);
                if (questName != null)
                    indexData.Quests.Add(new QuestIndex() { Id = id, Name = questName.Text });

                if (wt == 0x0000004F)
                {
                    var canBeUsed = dbp.GetBytePropertyValue((uint)DdoProperty.Usage_CanBeUsed) ?? 0;
                    if (canBeUsed > 0)
                        indexData.NPCs.Add(id);
                }
                found++;

                var progress = 100 * found / glfs.Count;
                if (progress >= (lastEmit + emitEveryPercent))
                {
                    Console.WriteLine($"{progress}% complete");
                    lastEmit = progress;
                }
            }
        }

        Console.WriteLine($"Successfully built indexes over {found} items in {timer.Elapsed}.");

        DatCache.Index = indexData;
        using (StreamWriter sw = File.CreateText(CachePath))
        using (JsonTextWriter writer = new JsonTextWriter(sw))
        {
            // Wrap the StreamWriter in a JsonSerializer
            JsonSerializer serializer = new JsonSerializer();
            writer.Formatting = Formatting.Indented;

            // Serialize the object directly into the file stream
            serializer.Serialize(writer, indexData);
        }

        var fileInfo = new FileInfo(CachePath);
        var kb = fileInfo.Length / 1024;
        var mb = kb / 1024;
        var sizeStr = mb > 0 ? $"{mb}MB" : $"{kb}kb";
        Console.WriteLine($"Successfully saved indexing data ({sizeStr}) to {CachePath}");

    }

    public static void LoadIndexCache()
    {
        if (!File.Exists(CachePath))
            return;

        var fileInfo = new FileInfo(CachePath);
        var kb = fileInfo.Length / 1024;
        var mb = kb / 1024;
        var sizeStr = mb > 0 ? $"{mb}MB" : $"{kb}kb";

        using (FileStream fileStream = File.Open(CachePath, FileMode.Open))
        using (StreamReader streamReader = new StreamReader(fileStream))
        using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
        {
            JsonSerializer serializer = new JsonSerializer();
            var index = serializer.Deserialize<IndexData>(jsonReader);
            DatCache.Index = index;

            // You can now work with the 'movie' object
            Console.WriteLine($"Loaded Indexing data ({sizeStr}) from {CachePath}");
        }
    }
}
