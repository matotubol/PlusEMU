using Plus.HabboHotel.Users.Clothing.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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