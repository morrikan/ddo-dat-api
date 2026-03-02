using System.Collections.Generic;

namespace DdoDatApi.Models;

public record SetBonusInfo
{
    public string Name { get; init; }
    public List<string> Descriptions { get; init; }
}
