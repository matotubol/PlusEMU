using Plus.Utilities.FigureData.Types;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace Plus.Utilities.FigureData;

public class FigureDataUtility : IFigureDataUtility
{
    public FigureData FigureData { get; set; }
    public Dictionary<string, SetType> IndexedSetTypes { get; set; }
    public Dictionary<int, Palette> IndexedPalettes { get; set; } //TODO remove and just use 1 Dictionary for whole FigureData

    private async Task<FigureData> DeserializeFigureDataAsync()
    {
        var projectSolutionPath = Directory.GetCurrentDirectory();
        var jsonDataPath = Path.Combine(projectSolutionPath, "Config", "FigureData.json");

        using (var stream = File.OpenRead(jsonDataPath))
        {
            return await JsonSerializer.DeserializeAsync<FigureData>(stream).ConfigureAwait(false);
        }
    }
    public async Task<FigureData> InitFromJsonAsync()
    {
        FigureData = await DeserializeFigureDataAsync().ConfigureAwait(false);

        if (FigureData == null)
        {
            throw new InvalidOperationException("The deserialized FigureData is null.");
        }

        ValidateFigureData();

        InitializeIndexedSetTypes();
        InitializeIndexedPalettes();

        return FigureData;
    }
    private void ValidateFigureData()
    {
        if (FigureData.SetTypes == null || !FigureData.SetTypes.Any())
        {
            throw new InvalidOperationException("FigureData.SetTypes is null or empty.");
        }

        if (FigureData.Palettes == null || !FigureData.Palettes.Any())
        {
            throw new InvalidOperationException("FigureData.Palettes is null or empty.");
        }
    }

    private void InitializeIndexedSetTypes()
    {
        IndexedSetTypes = new Dictionary<string, SetType>();
        foreach (var setType in FigureData.SetTypes)
        {
            if (!string.IsNullOrWhiteSpace(setType.Type))
            {
                IndexedSetTypes[setType.Type] = setType;

                foreach (var set in setType.Sets)
                {
                    set.Hash = CalculateSHA256(set, setType.Type); //set the hash during init
                    setType.IndexedSets = setType.Sets.ToDictionary(s => s.Id);
                }

                setType.IndexedSets = setType.Sets.ToDictionary(s => s.Id);
            }
        }
    }
    private void InitializeIndexedPalettes()
    {
        IndexedPalettes = new Dictionary<int, Palette>();
        foreach (var palette in FigureData.Palettes)
        {
            IndexedPalettes[palette.Id] = palette;
        }
    }
    public async Task UpdateFromJsonAsync()
    {
        try
        {
            var updatedFigureData = await DeserializeFigureDataAsync().ConfigureAwait(false);

            foreach (var updatedSetType in updatedFigureData.SetTypes)
            {
                UpdateSetType(updatedSetType);
            }
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"JSON deserialization error: {jsonEx.Message}");
        }
        catch (IOException ioEx)
        {
            Console.WriteLine($"File read error: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unknown error occurred: {ex.Message}");
        }
    }
    private void UpdateSetType(SetType updatedSetType)
    {
        if (!IndexedSetTypes.TryGetValue(updatedSetType.Type, out var existingSetType))
        {
            // if setType doesnt exist add it
            IndexedSetTypes[updatedSetType.Type] = updatedSetType;
            Console.WriteLine($"Added new setType {updatedSetType.Type}");
            return;
        }

        // these gets updated always as its only 13 items we dont need to hash it.
        existingSetType.PaletteId = updatedSetType.PaletteId;
        existingSetType.MandatoryF0 = updatedSetType.MandatoryF0;
        existingSetType.MandatoryF1 = updatedSetType.MandatoryF1;
        existingSetType.MandatoryM0 = updatedSetType.MandatoryM0;
        existingSetType.MandatoryM1 = updatedSetType.MandatoryM1;

        foreach (var updatedSet in updatedSetType.Sets)
        {
            UpdateSet(updatedSetType, existingSetType, updatedSet);
        }
    }
    private void UpdateSet(SetType updatedSetType, SetType existingSetType, Set updatedSet)
    {
        string newHash = CalculateSHA256(updatedSet, updatedSetType.Type);

        if (!existingSetType.IndexedSets.TryGetValue(updatedSet.Id, out var existingSet))
        {
            // if id doesnt exist in existing setType then add it
            existingSetType.IndexedSets[updatedSet.Id] = updatedSet;
            Console.WriteLine($"Added new set with ID {updatedSet.Id} in setType {updatedSetType.Type}");
            return;
        }

        if (existingSet.Hash != newHash)
        {
            if (!AreSetsEqual(existingSet, updatedSet))
            {
                Console.WriteLine($"Properties are different for set with ID {updatedSet.Id} in setType {updatedSetType.Type}");
            }

            Console.WriteLine($"Updated set with ID {updatedSet.Id} in setType {updatedSetType.Type}");
            Console.WriteLine($"Old Hash: {existingSet.Hash}");
            Console.WriteLine($"New Hash: {newHash}");

            updatedSet.Hash = newHash;
            existingSetType.IndexedSets[updatedSet.Id] = updatedSet;
        }
        else
        {
            Console.WriteLine($"Hashes are the same for set with ID {updatedSet.Id} in setType {updatedSetType.Type}");
        }
    }

    private bool AreSetsEqual(Set set1, Set set2)
    {
        if (set1.Id != set2.Id ||
            set1.Gender != set2.Gender ||
            set1.Club != set2.Club ||
            set1.Colorable != set2.Colorable ||
            set1.Selectable != set2.Selectable ||
            set1.Preselectable != set2.Preselectable ||
            set1.Sellable != set2.Sellable)
        {
            return false;
        }

        if (!ArePartsListsEqual(set1.Parts, set2.Parts))
        {
            return false;
        }

        return true;
    }

    private bool ArePartsListsEqual(List<Part> parts1, List<Part> parts2)
    {
        if (parts1.Count != parts2.Count)
            return false;

        for (int i = 0; i < parts1.Count; i++)
        {
            if (!ArePartsEqual(parts1[i], parts2[i]))
                return false;
        }
        return true;
    }

    private bool ArePartsEqual(Part part1, Part part2)
    {
        return part1.Id == part2.Id &&
               part1.Type == part2.Type &&
               part1.Colorable == part2.Colorable &&
               part1.Index == part2.Index &&
               part1.ColorIndex == part2.ColorIndex;
    }


    private string CalculateSHA256(Set set, string setType)
    {
        var tempSet = new
        {
            SetType = setType,
            Id = set.Id,
            Gender = set.Gender,
            Club = set.Club,
            Colorable = set.Colorable,
            Selectable = set.Selectable,
            Preselectable = set.Preselectable,
            Sellable = set.Sellable,
            Parts = new List<Part>(set.Parts)
        };

        string jsonRepresentation = JsonSerializer.Serialize(tempSet);

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonRepresentation));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }




}