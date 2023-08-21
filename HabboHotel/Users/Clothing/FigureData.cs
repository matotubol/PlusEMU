using Plus.HabboHotel.Users.Clothing.Types;
using System.Text.Json.Serialization;

namespace Plus.HabboHotel.Users.Clothing
{
    public class FigureData
    {
        [JsonPropertyName("setTypes")]
        public List<SetType>? SetTypes { get; set; }

        [JsonPropertyName("palettes")]
        public List<Palette>? Palettes { get; set; }

    }
}