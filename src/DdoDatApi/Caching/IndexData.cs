using System;
using System.Collections.Generic;

namespace DdoDatApi.Caching;

public class IndexData
{
    /// <summary>
    /// UTC Timestamp of when the index was built.
    /// </summary>
    public DateTime? CompiledOnUtc { get; set; } = null;

    /// <summary>
    /// Client version this index was built against.
    /// </summary>
    public string ClientVersion { get; set; } = null;

    /// <summary>
    /// Cache of all the various DbProperty object IDs by WeenieType. Most values have a corresponding
    /// entry in <see cref="WeenieTypes"/>, but not all.
    /// </summary>
    public Dictionary<uint, List<uint>> WeenieTypes { get; set; } = new();

    /// <summary>
    /// Cache of objects that have a specified name.
    /// </summary>
    public Dictionary<string, List<uint>> Names { get; set; } = new();

    /// <summary>
    /// Caches the name of DbProperties object
    /// </summary>
    public Dictionary<uint, string> NameLookup { get; set; } = new();

    /// <summary>
    /// Minor warning - there are over 1000 of these - it's not a short list
    /// </summary>
    public Dictionary<string, uint> WellKnown { get; set; } = new();

    /// <summary>
    /// List of all quests in the Index.
    /// </summary>
    public List<QuestIndex> Quests { get; set; } = new();

    /// <summary>
    /// List of spells. Technically a duplicate of the WeenieType cache for WeenieType.Spell.
    /// </summary>
    public List<uint> Spells { get; set; } = new();

    /// <summary>
    /// Experimental: Cache of DbProperty IDs of NPCs
    /// </summary>
    public List<uint> NPCs { get; set; } = new();

    /// <summary>
    /// Cache of DbProperty IDs of the various Sentient Personalities
    /// </summary>
    public List<uint> SentientPersonalities { get; set; } = new();

    /// <summary>
    /// How long it took to build the index.
    /// </summary>
    public TimeSpan? LastIndexDuration { get; set; } = null;
}

public class QuestIndex
{
    /// <summary>
    /// DbProperties object ID of the quest.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Name of the Quest
    /// </summary>
    public string Name { get; set; }
}
