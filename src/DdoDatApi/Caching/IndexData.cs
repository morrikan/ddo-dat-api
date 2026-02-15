using System;
using System.Collections.Generic;

namespace DdoDatApi.Caching;

public class IndexData
{
    public Dictionary<uint, List<uint>> WeenieTypes { get; set; } = new();

    public Dictionary<string, List<uint>> Names { get; set; } = new();

    public Dictionary<uint, string> NameLookup { get; set; } = new();

    public Dictionary<string, uint> WellKnown { get; set; } = new();

    public List<QuestIndex> Quests { get; set; } = new();

    public Dictionary<string, uint> Enhancements { get; set; } = new();

    public Dictionary<string, List<uint>> Feats { get; set; } = new();

    public List<uint> Spells { get; set; } = new();

    public List<uint> NPCs { get; set; } = new();

    public TimeSpan? LastIndexDuration { get; set; } = null;

}

public class QuestIndex
{
    public uint Id { get; set; }
    public string Name { get; set; }
}
