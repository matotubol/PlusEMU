using System.Text.Json.Serialization;

namespace Plus.HabboHotel.Users.Clothing.Types;
public class Part
{
    [JsonPropertyName("id")]
    public long Id { get; set; } //had to be long since there are long ids e.g 2147483648 

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("colorable")]
    public bool Colorable { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("colorindex")]
    public int ColorIndex { get; set; }
}