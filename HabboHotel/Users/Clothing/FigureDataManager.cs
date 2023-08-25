using Dapper;
using Microsoft.Extensions.Logging;
using Plus.Database;
using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.Users.Clothing.Parts;
using Plus.Utilities.FigureData;
using Plus.Utilities.FigureData.Types;
using System.Data;
using Z.Dapper.Plus;

namespace Plus.HabboHotel.Users.Clothing;
internal class FigureDataManager : IFigureDataManager
{
    private readonly ICatalogManager _catalogManager;
    private readonly ILogger<FigureDataManager> _logger;
    private readonly IFigureDataUtility _figureDataUtility;
    private readonly IDatabase _database;

    private const bool DEBUG = true;
    private const int MaxItemComponents = 5; //Should always be 2 + highest colorindex of parts of the item.//TODO make dynamic
    private const string Male = "M";
    private const string Female = "F";
    private const string Unisex = "U";

    public FigureDataManager(ICatalogManager catalogManager, ILogger<FigureDataManager> logger, IFigureDataUtility figureDataUtility, IDatabase database)
    {
        _catalogManager = catalogManager;
        _logger = logger;
        _figureDataUtility = figureDataUtility;
        _database = database;
    }

    public async Task InitAsync()
    {
        FigureData figureData = await _figureDataUtility.InitFromJsonAsync();
        ConfigureDapperPlusMappings();
        await IngestDataToDatabaseAsync(figureData);
    }

    public async Task UpdateFigureData() => await _figureDataUtility.UpdateFromJsonAsync();

    private void LogMessage(string message)
    {
        if (DEBUG)
        {
            Console.WriteLine(message);
        }
    }
    public void ConfigureDapperPlusMappings()
    {
        DapperPlusManager.Entity<Palette>()
        .Table("figure_palettes")  // Assuming this is the table name for Palette
        .Identity(x => x.Id)
        .AfterAction((kind, x) => {
            if (kind == DapperPlusActionKind.Insert || kind == DapperPlusActionKind.Merge)
            {
                x.Colors.ForEach(y => y.PaletteId = x.Id);
            }
        });

        // Mapping for figure_palettes
        DapperPlusManager.Entity<Color>()
            .Table("figure_palettes")
            .Identity(x => x.Id)
            .Map(x => x.Id, "color_id")
            .Map(x => x.PaletteId, "palette_id")
            .Map(x => x.Club);

        // Mapping for figure_sets
        DapperPlusManager.Entity<Set>()
            .Table("figure_sets")
            .Identity(x => x.Id)
            .Map(x => x.Id, "id")
            .Map(x => x.SetTypeReference, "set_type")
            .Map(x => x.Gender, "gender")
            .Map(x => x.Club, "club")
            .Map(x => x.Sellable, "sellable")
            .Map(x => x.Selectable, "selectable")
            .Map(x => x.Colors, "colors");

        // Mapping for figure_types
        DapperPlusManager.Entity<SetType>()
           .Table("figure_types")
           .Map(x => x.Type, "type")
           .Map(x => x.PaletteId, "paletteId");
    }

    public async Task IngestDataToDatabaseAsync(FigureData figureData)
    {
        using var connection = _database.Connection();

        var setTypeDataList = new List<SetType>();
        var setDataList = new List<Set>();
        var paletteDataList = new List<Color>();

        foreach (var setType in figureData.SetTypes)
        {
            setTypeDataList.Add(setType);

            foreach (var set in setType.Sets)
            {
                set.SetTypeReference = setType.Type;
                set.Colors = set.Parts.Max(part => part.ColorIndex);
                setDataList.Add(set);
            }
        }

        foreach (var palette in figureData.Palettes)
        {
            paletteDataList.AddRange(palette.Colors);

            foreach(var palettes in palette.Colors)
            {
                palettes.PaletteId = palette.Id;
                paletteDataList.Add(palettes);
            }
        }
        try
        {
            await connection.BulkActionAsync(x => x.BulkMerge(setTypeDataList));
            await connection.BulkActionAsync(x => x.BulkMerge(setDataList));
            await connection.BulkActionAsync(x => x.BulkMerge(paletteDataList));
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while ingesting data to the database: {ex.Message}");
        }
    }

    public async Task<bool> ValidateColorAsync(int colorIndex, int paletteId, bool hasHabboClub, IDbConnection connection)
    {
        int paletteCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM figure_palettes WHERE palette_id = @PaletteId",
            new { PaletteId = paletteId });

        if (paletteCount == 0)
        {
            LogMessage($"Failed: palette not found for palette ID '{paletteId}'");
            return false;
        }

        int colorCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM figure_palettes WHERE id = @ColorIndex AND palette_id = @PaletteId",
            new { ColorIndex = colorIndex, PaletteId = paletteId });

        if (colorCount == 0)
        {
            LogMessage($"Failed: color index '{colorIndex}' not found in palette ID '{paletteId}'");
            return false;
        }

        int clubRequirement = await connection.ExecuteScalarAsync<int>(
            "SELECT club FROM figure_palettes WHERE id = @ColorIndex AND palette_id = @PaletteId",
            new { ColorIndex = colorIndex, PaletteId = paletteId });

        if (clubRequirement > 0 && !hasHabboClub)
        {
            LogMessage($"Failed: color index '{colorIndex}' requires Habbo Club membership");
            return false;
        }

        return true;
    }

    private async Task<bool> IsMandatoryForGenderAsync(string setType, string gender, bool hasHabboClub,
        IDbConnection connection)
    {
        var query = @"SELECT `mandatory_m_0`, `mandatory_m_1`, `mandatory_f_0`, `mandatory_f_1`
                  FROM `figure_types`
                  WHERE `type` = @Type";

        var setTypeData = await connection.QueryFirstOrDefaultAsync(query, new { Type = setType });

        if (setTypeData == null)
        {
            // setType not found in the database
            return false;
        }

        return gender switch
        {
            Male => hasHabboClub ? setTypeData.mandatory_m_1 : setTypeData.mandatory_m_0,
            Female => hasHabboClub ? setTypeData.mandatory_f_1 : setTypeData.mandatory_f_0,
            _ => false
        };
    }
    /*Generates an non HC item if its type is not Mendatory else removes it.*/
    private async Task<string?> HandleGenderMismatchAsync(string setType, string gender, bool hasHabboClub, IDbConnection connection)
    {
        LogMessage($"gender mismatch for item ");
        
        if (await IsMandatoryForGenderAsync(setType, gender, hasHabboClub, connection))
        {
            return await GenerateNonClubItemAsync(setType, gender, connection);
        }
        return null;
    }
    private async Task<string?> HandleColorableSetTypeAsync(Set setItem, bool hasHabboClub, string[] splitItem, IDbConnection connection)
    {
        var itemType = splitItem[0];
        var setId = splitItem[1];

        var paletteId = await connection.QueryFirstOrDefaultAsync<int>(
            "SELECT paletteId FROM figure_types WHERE type = @Type",
            new { Type = itemType }
        );

        // Loop through the colors and validate each one
        for (var i = 2; i < splitItem.Length; i++)
        {
            // Check if the color is empty
            if (string.IsNullOrEmpty(splitItem[i]))
            {
                // If empty replace with the first valid color
                var firstValidColorId = await GetFirstValidColorAsync(paletteId, connection);
                splitItem[i] = firstValidColorId?.ToString() ?? "0";
                continue;
            }

            var colorId = int.Parse(splitItem[i]);
            bool colorIsValid = await ValidateColorAsync(colorId, paletteId, hasHabboClub, connection);

            if (!colorIsValid)
            {
                var firstValidColorId = await GetFirstValidColorAsync(paletteId, connection);
                splitItem[i] = firstValidColorId?.ToString() ?? "0";
            }
        }

        return string.Join("-", splitItem);
    }


    private async Task<int?> GetFirstValidColorAsync(int paletteId, IDbConnection connection)
    {
        string query = @"SELECT color_id
                     FROM figure_palettes
                     WHERE palette_id = @PaletteId
                     ORDER BY id ASC
                     LIMIT 1";

        var firstValidColorId = await connection.QueryFirstOrDefaultAsync<int?>(query, new { PaletteId = paletteId });

        if (firstValidColorId == null)
        {
            LogMessage($"Failed: No valid color found for paletteId '{paletteId}'");
        }
        else
        {
            LogMessage($"Success: First valid color for paletteId '{paletteId}' is '{firstValidColorId}'");
        }

        return firstValidColorId;
    }


    public async Task<string?> GenerateNonClubItemAsync(string setTypeStr, string gender, IDbConnection connection)
    {
        var firstValidSet = await connection.QueryFirstOrDefaultAsync<Set>(
            @"SELECT id 
          FROM figure_sets 
          WHERE type = @Type AND (gender = @Gender OR gender = 'U') AND club = 0
          LIMIT 1",
            new { Type = setTypeStr, Gender = gender }
        );

        var paletteId = await connection.QueryFirstOrDefaultAsync(
            @"SELECT paletteId 
          FROM figure_types 
          WHERE type = @Type
          LIMIT 1",
            new { Type = setTypeStr }
        );

        if (firstValidSet == null)
        {
            LogMessage($"Failed: No valid set found for type '{setTypeStr}' and gender '{gender}'");
            return null;
        }

        int? firstValidColorId = await GetFirstValidColorAsync(paletteId, connection);

        return $"{setTypeStr}-{firstValidSet.Id}-{firstValidColorId}";
    }

    private async Task<string?> ValidateSingleItemAsync(string item, string gender, bool hasHabboClub, IDbConnection connection)
    {
        var splitItem = item.Split('-');
        var itemType = splitItem[0];

        if (splitItem.Length > MaxItemComponents)
        {
            LogMessage($"Failed: Too many colors specified for item '{item}'");
            return null;
        } 

        // Check if setItem exists and its attributes
        var setItem = await connection.QueryFirstOrDefaultAsync<Set>(
            @"SELECT id, gender, club, colorable 
          FROM figure_sets 
          WHERE id = @Id AND set_type = @Type",
            new { Id = int.Parse(splitItem[1]), Type = splitItem[0] }
        );

        if (setItem == null)
        {
            LogMessage($"Failed: setItem not found for ID '{splitItem[1]}'");
            return null;
        }

        if (!hasHabboClub && setItem.Club == 1)
        {
            return await GenerateNonClubItemAsync(splitItem[0], gender, connection);
        }

        if (setItem.Gender != Unisex && setItem.Gender != gender)
        {
            return await HandleGenderMismatchAsync(itemType, gender, hasHabboClub, connection);
        }

        if (!setItem.Colorable)
        {
            return $"{splitItem[0]}-{setItem.Id}";
        }
        
        return await HandleColorableSetTypeAsync(setItem, hasHabboClub, splitItem, connection);
    }


    //TODO add ICollection<ClothingParts> clothingParts
    public async Task<string> ValidateLookAsync(string look, string gender, ICollection<ClothingParts> clothingParts, bool hasHabboClub)
    {
        using var connection = _database.Connection();

        if (string.IsNullOrEmpty(look) ||
            (gender != Male && gender != Female))
        {
            // return GenerateDefaultLook(); //TODO
        }


        var items = look.Split('.');
        var validatedItems = new List<string?>();
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

            var validatedItem = await ValidateSingleItemAsync(item, gender, hasHabboClub, connection);
            if (!string.IsNullOrEmpty(validatedItem))
            {
                validatedItems.Add(validatedItem);
            }
            LogMessage(validatedItem);

        }

        //TODO if fails we should just return default look since looks are required to have mandatory items.
        /*if (!ValidateMandatorySetTypes(gender, hasHabboClub, validatedItems))
        {
            return string.Empty;
        }*/

        return string.Join(".", validatedItems);
    }

}