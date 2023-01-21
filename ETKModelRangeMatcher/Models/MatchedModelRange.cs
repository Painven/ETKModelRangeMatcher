using System;
using System.Collections.Generic;
using System.Linq;

namespace ETKModelRangeMatcher.ViewModels;

public class MatchedModelRange
{
    public string Name { get; set; }
    public string Model { get; set; }

    public List<string> AdjacentIds { get; set; } = new();

    public int ChildCount => AdjacentIds.Count();

    public string ToStringFormat()
    {
        if (AdjacentIds.Count > 0)
        {
            return $"{Model}\t{Name}\t{string.Join(",", AdjacentIds)}";
        }

        return string.Empty;
    }

    public static MatchedModelRange Parse(string str)
    {
        var arr = str.Split(new string[] { "\t" }, System.StringSplitOptions.RemoveEmptyEntries);

        if (arr.Length == 3)
        {
            return new MatchedModelRange()
            {
                Model = arr[0],
                Name = arr[1],
                AdjacentIds = arr[2].Split(',').Select(pid => (int.Parse(pid)).ToString()).ToList()
            };
        }
        throw new FormatException(nameof(str));
    }
}
