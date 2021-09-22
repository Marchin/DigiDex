using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class DigimonDatabase : ScriptableObject, IDataDabase {
    public const string FieldsFilter = "Fields";
    public const string AttributesFilter = "Attirbutes";
    public const string TypesFilter = "Types";
    public const string LevelsFilter = "Levels";
    public const string FavoritesToggle = "Favorites";
    public const string ReverseToggle = "Reverse";
    public List<Digimon> Digimons;
    public List<Field> Fields;
    public List<Attribute> Attributes;
    public List<Type> Types;
    public List<Level> Levels;
    private const string FavDigimonPref = "fav_digimons";

    public Dictionary<string, FilterData> RetrieveFiltersData() {
        Dictionary<string, FilterData> filters = new Dictionary<string, FilterData>();

        FilterData fieldsFilter = new FilterData();
        fieldsFilter.Name = FieldsFilter;
        fieldsFilter.Elements = new List<FilterEntryData>(Fields.Count);
        for (int iField = 0; iField < Fields.Count; ++iField) {
            fieldsFilter.Elements.Add(new FilterEntryData { Name = Fields[iField].Name, Sprite = Fields[iField].Sprite });
        }
        filters.Add(FieldsFilter, fieldsFilter);

        FilterData attributesFilter = new FilterData();
        attributesFilter.Name = AttributesFilter;
        attributesFilter.Elements = new List<FilterEntryData>(Attributes.Count);
        for (int iAttribute = 0; iAttribute < Attributes.Count; ++iAttribute) {
            attributesFilter.Elements.Add(new FilterEntryData { Name = Attributes[iAttribute].Name, Sprite = Attributes[iAttribute].Sprite });
        }
        filters.Add(AttributesFilter, attributesFilter);

        FilterData typesFilter = new FilterData();
        typesFilter.Name = TypesFilter;
        typesFilter.Elements = new List<FilterEntryData>(Types.Count);
        for (int iType = 0; iType < Types.Count; ++iType) {
            typesFilter.Elements.Add(new FilterEntryData { Name = Types[iType].Name });
        }
        filters.Add(TypesFilter, typesFilter);

        FilterData levelsFilter = new FilterData();
        levelsFilter.Name = LevelsFilter;
        levelsFilter.Elements = new List<FilterEntryData>(Levels.Count);
        for (int iLevel = 0; iLevel < Levels.Count; ++iLevel) {
            levelsFilter.Elements.Add(new FilterEntryData { Name = Levels[iLevel].Name });
        }
        filters.Add(LevelsFilter, levelsFilter);

        return filters;
    }

    public Dictionary<string, ToggleData> RetrieveTogglesData() {
        Dictionary<string, ToggleData> toggles = new Dictionary<string, ToggleData>();

        toggles.Add(FavoritesToggle, new ToggleData { Name = FavoritesToggle });
        toggles.Add(ReverseToggle, new ToggleData { Name = ReverseToggle });

        return toggles;
    }

    public void SaveFavorites(HashSet<Hash128> hashes) {
        if (hashes != null) {
            string jsonData = JsonConvert.SerializeObject(hashes.Select(h => h.ToString()));
            PlayerPrefs.SetString(FavDigimonPref, jsonData);
        }
    }

    public HashSet<Hash128> LoadFavorites() {
        string jsonData = PlayerPrefs.GetString(FavDigimonPref, "");
        var strings = JsonConvert.DeserializeObject<List<string>>(jsonData);
        var a = strings.Select(s => Hash128.Parse(s)).ToList();
        HashSet<Hash128> result = new HashSet<Hash128>(a);

        return result;
    }
}
