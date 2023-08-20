using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Plus.HabboHotel.Users.Clothing.Types;
public class Palette
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("colors")]
    public List<Color> Colors { get; set; }
}
