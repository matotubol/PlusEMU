using System.Text.Json.Serialization;

namespace Plus.Utilities.FigureData.Types;
public class Part
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("colorable")]
    public bool Colorable { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("colorindex")]
    public int ColorIndex { get; set; }
}
