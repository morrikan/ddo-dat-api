using System.Collections.Generic;

namespace DdoDatApi.Caching;

public class DatCache
{
    public static IndexData Index { get; set; } = new();

    /// <summary>
    /// Maps treasure table IDs to the item Weenie IDs they can produce.
    /// </summary>
    public static Dictionary<uint, List<uint>> TreasureMap { get; set; } = new();

    /// <summary>
    /// Maps item Weenie IDs to all recipes that use that item as an ingredient.
    /// </summary>
    public static Dictionary<uint, List<RecipeData>> RecipeMap { get; set; } = new();
}
