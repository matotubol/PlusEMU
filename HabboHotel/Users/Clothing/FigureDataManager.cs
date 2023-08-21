using Microsoft.Extensions.Logging;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.Users.Clothing.Types;
using System.Text.Json;

namespace Plus.HabboHotel.Users.Clothing;
internal class FigureDataManager : IFigureDataManager
{
    private readonly ICatalogManager _catalogManager;
    private readonly ILogger<FigureDataManager> _logger;
    public FigureData FigureData { get; private set; }
    private Dictionary<string, SetType> _indexedSetTypes { get; set; }
    private Dictionary<int, Palette> _indexedPalettes { get; set; }

    private const bool DEBUG = true;
    private const int MaxItemComponents = 4;// Dont change (setType-itemId-optional colorIndex1-optional colorIndex2
    private const string Male = "M";
    private const string Female = "F";
    private const string Unisex = "U";

    public FigureDataManager(ICatalogManager catalogManager, ILogger<FigureDataManager> logger)
    {
        _catalogManager = catalogManager;
        _logger = logger;
        _indexedPalettes = new();
        _indexedSetTypes = new();
    }

    /* Make sure you have no duplicate ids in your SetTypes sets else we cant index them.
     * 1. Get the current directory
     2. Construct the path to the JSON file
     3. Read the JSON file
     3. Read the JSON file
     4. Deserialize the JSON data
     5. Index the data for faster lookups */
    public void Init()
    {
        var projectSolutionPath = Directory.GetCurrentDirectory();
        //TODO make this database configurable.
        var jsonDataPath = Path.Combine(projectSolutionPath, "Config", "FigureData.json");

        if (!File.Exists(jsonDataPath))
        {
            throw new FileNotFoundException($"The file {jsonDataPath} was not found.");
        }

        using (var stream = File.OpenRead(jsonDataPath))
        {
            using var doc = JsonDocument.Parse(stream);
            FigureData = JsonSerializer.Deserialize<FigureData>(doc.RootElement.GetRawText());
        }

        if (FigureData == null)
        {
            throw new InvalidOperationException("The deserialized FigureData is null.");
        }

        if (FigureData.SetTypes == null || !FigureData.SetTypes.Any())
        {
            throw new InvalidOperationException("FigureData.SetTypes is null or empty.");
        }

        foreach (var setType in FigureData.SetTypes)
        {
            if (!string.IsNullOrWhiteSpace(setType.Type) && setType != null)
            {
                _indexedSetTypes[setType.Type] = setType;

                setType.IndexedSets = setType.Sets.ToDictionary(s => s.Id);
            }
        }

        if (FigureData.Palettes == null || !FigureData.Palettes.Any())
        {
            throw new InvalidOperationException("FigureData.Palettes is null or empty.");
        }

        _indexedPalettes = new Dictionary<int, Palette>();
        foreach (var palette in FigureData.Palettes)
        {
            if (palette != null)
            {
                _indexedPalettes[palette.Id] = palette;
            }
        }

        //for testing
        LogMessage(ValidateLook("hd-996000085-1465.ch-989989228.lg-800001582-71.ha-61685620.he-999999026.fa-1205-1324.ca-3176-1294.cc-800001382-1186.cp-800001164", "M", false));
    }

    private void LogMessage(string message)
    {
        if (DEBUG)
        {
            Console.WriteLine(message);
        }
    }
    public bool ValidateColor(int colorIndex, int paletteId, bool hasHabboClub)
    {
        // Check if the palette exists in the _indexedPalettes dictionary
        if (!_indexedPalettes.TryGetValue(paletteId, out var palette))
        {
            LogMessage($"Failed: palette not found for palette ID '{paletteId}'");
            return false;
        }

        // Check if the color exists within the palette
        var color = palette.Colors.FirstOrDefault(c => c.Id == colorIndex);
        if (color == null)
        {
            LogMessage($"Failed: color index '{colorIndex}' not found in palette ID '{paletteId}'");
            return false;
        }

        // If the color requires a Habbo Club membership, check if the user has it
        if (color.Club > 0 && !hasHabboClub)
        {
            LogMessage($"Failed: color index '{colorIndex}' requires Habbo Club membership");
            return false;
        }
        return true;
    }
    public List<Color> GetValidColors(Palette palette, bool hasHabboClub) => palette.Colors.Where(color => color.Club == 0 || (color.Club == 1 && hasHabboClub)).ToList();

    public string GenerateNonClubItem(string setTypeStr, string gender)
    {
        if (!_indexedSetTypes.TryGetValue(setTypeStr, out var setType))
        {
            LogMessage($"setType not found for '{setTypeStr}'");
            return null;
        }

        var validColors = GetValidColors(_indexedPalettes[setType.PaletteId], false);
        var firstValidColor = validColors.FirstOrDefault();

        // Since you're checking multiple conditions, a loop is still needed
        foreach (var set in setType.IndexedSets.Values) // Use IndexedSets.Values to iterate through the Set objects
        {
            if ((set.Gender == Unisex || set.Gender == gender) && set.Club == 0)
            {
                return $"{setTypeStr}-{set.Id}-{firstValidColor?.Id}"; // Safe navigation in case firstValidColor is null
            }
        }
        return null;
    }

    private bool IsMandatoryForGender(SetType setType, string gender, bool hasHabboClub) => gender switch
    {
        Male => hasHabboClub ? setType.MandatoryM1 : setType.MandatoryM0,
        Female => hasHabboClub ? setType.MandatoryF1 : setType.MandatoryF0,
        _ => false
    };

    private string HandleNonClubMembers(SetType setType, string gender, string[] splitItem)
    {
        LogMessage($"Failed: setItem '{splitItem[1]}' is only for HC members");
        return GenerateNonClubItem(setType.Type, gender);
    }
    private string HandleGenderMismatch(SetType setType, string gender, bool hasHabboClub, long setId)
    {
        LogMessage($"gender mismatch for item ID '{setId}'");
        return IsMandatoryForGender(setType, gender, hasHabboClub) ? GenerateNonClubItem(setType.Type, gender) : null;
    }

    private string HandleColorableSetType(SetType setType, string[] splitItem, bool hasHabboClub)
    {
        for (var i = 2; i < splitItem.Length; i++)
        {
            if (!ValidateColor(int.Parse(splitItem[i]), setType.PaletteId, hasHabboClub))
            {
                var firstValidColor = GetValidColors(_indexedPalettes[setType.PaletteId], false).FirstOrDefault();
                splitItem[i] = firstValidColor?.Id.ToString() ?? string.Empty;
            }
        }
        return string.Join("-", splitItem);
    }

    public string ValidateSingleItem(string item, string gender, bool hasHabboClub)
    {
        var splitItem = item.Split('-');

        if (splitItem.Length > MaxItemComponents)
        {
            LogMessage($"Failed: Too many colors specified for item '{item}'");
            return null;
        }

        if (!_indexedSetTypes.TryGetValue(splitItem[0], out var setType))
        {
            LogMessage($"Failed: setType not found for '{splitItem[0]}'");
            return null;
        }
        var setId = int.Parse(splitItem[1]);
        setType.IndexedSets.TryGetValue(int.Parse(splitItem[1]), out var setItem);
        if (setItem == null)
        {
            LogMessage($"Failed: setItem not found for ID '{splitItem[1]}'");
            return null;
        }

        if (!hasHabboClub && setItem.Club > 0)
            return HandleNonClubMembers(setType, gender, splitItem);

        if (setItem.Gender != Unisex && setItem.Gender != gender)
            return HandleGenderMismatch(setType, gender, hasHabboClub, setId);

        return setItem.Colorable ? HandleColorableSetType(setType, splitItem, hasHabboClub) : $"{setType.Type}-{setItem.Id}";
    }

    public bool ValidateMandatorySetTypes(string gender, bool hasHabboClub, List<string> validatedItems)
    {
        var encounteredSetTypes = validatedItems.Select(item => item.Split('-')[0]).ToList();

        foreach (var setType in _indexedSetTypes.Values)
        {
            if (!IsMandatoryForGender(setType, gender, hasHabboClub) || encounteredSetTypes.Contains(setType.Type))
                continue;

            var defaultItem = GenerateNonClubItem(setType.Type, gender);

            if (defaultItem == null)
            {
                // Could not generate a default item for the mandatory setType.
                return false;
            }

            LogMessage($"Failed: missing mandatory setType. added '{defaultItem}'");
            validatedItems.Add(defaultItem);
        }
        return true;
    }

    //TODO add ICollection<ClothingParts> clothingParts
    public string ValidateLook(string look, string gender, bool hasHabboClub)
    {

        if (string.IsNullOrEmpty(look) ||
            (gender != Male && gender != Female))
        {
            // return GenerateDefaultLook(); //TODO
        }

        LogMessage(look);

        var items = look.Split('.');
        var validatedItems = new List<string>();
        var encounteredSetTypes = new HashSet<string>();

        foreach (var item in items)
        {
            var splitItems = item.Split('-');
            var setTypeStr = splitItems[0];

            if (encounteredSetTypes.Contains(setTypeStr))
            {
                // Skip duplicate setType
                LogMessage($"Failed: setType '{setTypeStr}' was duplicate and was removed.");
                continue;
            }
            encounteredSetTypes.Add(setTypeStr);

            var validatedItem = ValidateSingleItem(item, gender, hasHabboClub);
            if (!string.IsNullOrEmpty(validatedItem))
            {
                validatedItems.Add(validatedItem);
            }
        }

        //TODO if fails we should just return default look since looks are required to have mandatory items.
        if (!ValidateMandatorySetTypes(gender, hasHabboClub, validatedItems))
        {
            return string.Empty;
        }

        return string.Join(".", validatedItems);
    }

}