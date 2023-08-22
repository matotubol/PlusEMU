using Plus.HabboHotel.Users.Clothing;
using System.Text.Json;

namespace Plus.Utilities.FigureData;

public class FigureDataUtility : IFigureDataUtility
{

    public FigureData FigureData { get; private set; }
    public async Task<FigureData> InitFromJsonAsync()
    {
        FigureData figureData;

        var projectSolutionPath = Directory.GetCurrentDirectory();
        var jsonDataPath = Path.Combine(projectSolutionPath, "Config", "FigureData.json");

        using (var stream = File.OpenRead(jsonDataPath))
        {
            figureData = await JsonSerializer.DeserializeAsync<FigureData>(stream);
        }

        if (figureData == null)
        {
            throw new InvalidOperationException("The deserialized FigureData is null.");
        }

        if (figureData.SetTypes == null || !figureData.SetTypes.Any())
        {
            throw new InvalidOperationException("FigureData.SetTypes is null or empty.");
        }

        if (figureData.Palettes == null || !figureData.Palettes.Any())
        {
            throw new InvalidOperationException("FigureData.Palettes is null or empty.");
        }

        return figureData;
    }
}

