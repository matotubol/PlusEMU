using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plus.HabboHotel.Users.Clothing.Types;

namespace Plus.HabboHotel.Users.Clothing;
public interface IFigureDataManager
{
    FigureData FigureData { get; }

    // This method is responsible for initializing the FigureDataManager.
    // It loads the data, indexes it for quick access, etc.
    void Init();

    // A method to validate a color index for a given paletteId and HabboClub status.
    bool ValidateColor(int colorIndex, int paletteId, bool hasHabboClub);

    // Returns a list of valid colors from a palette based on the HabboClub status.
    List<Color> GetValidColors(Palette palette, bool hasHabboClub);

    // Generates a default non-HC item for a given setType and gender e.g (hd-100-62).
    string GenerateNonClubItem(string setTypeStr, string gender);

    // Validates a single item string (like 'hd-123-4-5') for a given gender and HabboClub status.
    string ValidateSingleItem(string item, string gender, bool hasHabboClub);

    bool ValidateMandatorySetTypes(string gender, bool hasHabboClub, List<string> validatedItems);

    string ValidateLook(string itemString, string gender, bool hasHabboClub);

}

