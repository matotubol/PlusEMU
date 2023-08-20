using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Plus.HabboHotel.Users.Clothing.Types;
public class SetType
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("paletteId")]
    public int PaletteId { get; set; }

    [JsonPropertyName("mandatory_f_0")]
    public bool MandatoryF0 { get; set; }

    [JsonPropertyName("mandatory_f_1")]
    public bool MandatoryF1 { get; set; }

    [JsonPropertyName("mandatory_m_0")]
    public bool MandatoryM0 { get; set; }

    [JsonPropertyName("mandatory_m_1")]
    public bool MandatoryM1 { get; set; }

    [JsonPropertyName("sets")]
    public List<Set> Sets { get; set; }
}