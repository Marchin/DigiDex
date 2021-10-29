using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class DigimonDatabase : ScriptableObject, IDatabase {
    public string DisplayName => "Digimons";

    public const string FieldsFilter = "Fields";
    public const string AttributesFilter = "Attributes";
    public const string TypesFilter = "Types";
    public const string LevelsFilter = "Levels";
    public const string GroupsFilter = "Groups";
    public const string FavoritesToggle = "Favorites";
    public const string ReverseToggle = "Reverse";
    private const string FavDigimonPref = "fav_digimons";
    public List<Digimon> Digimons;
    public IEnumerable<IDataEntry> EntryList => Digimons.Cast<IDataEntry>();
    public List<Field> Fields;
    public List<Attribute> Attributes;
    public List<DigimonType> Types;
    public List<DigimonGroup> Groups;
    public List<Level> Levels;
    private Dictionary<string, HashSet<Hash128>> _favorites;
    private Dictionary<string, HashSet<Hash128>> ListsInternal {
        get {
            if (_favorites == null) {
                _favorites = LoadFavorites();
            }
            return _favorites;
        }
    }
    public IReadOnlyDictionary<string, HashSet<Hash128>> Lists => ListsInternal;
    private Dictionary<Hash128, Digimon> _digimonDict;
    public Dictionary<Hash128, Digimon> DigimonDict {
        get {
            if (_digimonDict == null) {
                _digimonDict = Digimons.ToDictionary(d => d.Hash);
            }
            return _digimonDict;
        }
    }
    private Dictionary<Hash128, IDataEntry> _entryDict;
    public Dictionary<Hash128, IDataEntry> EntryDict {
        get {
            if (_entryDict == null) {
                _entryDict = EntryList.ToDictionary(d => d.Hash);
            }
            return _entryDict;
        }
    }

    public void AddEntryToList(string list, Hash128 entry) {
        if (ListsInternal.ContainsKey(list)) {
            ListsInternal[list].Add(entry);
            SaveFavorites();
        } else {
            Debug.LogWarning("List doesn't exist");
        }
    }

    public void RemoveEntryFromList(string list, Hash128 entry) {
        if (ListsInternal.ContainsKey(list)) {
            ListsInternal[list].Remove(entry);
            if (ListsInternal[list].Count == 0) {
                ListsInternal.Remove(list);
            }
            SaveFavorites();
        } else {
            Debug.LogWarning("List doesn't exist");
        }
    }

    public bool AddList(string name) {
        if (!ListsInternal.ContainsKey(name)) {
            ListsInternal.Add(name, new HashSet<Hash128>());
            SaveFavorites();
            return true;
        } else {
            return false;
        }
    }

    public List<FilterData> RetrieveFiltersData() {
        List<FilterData> filters = new List<FilterData>();

        FilterData fieldsFilter = new FilterData(
            name: FieldsFilter,
            getFilteringComponent: element => (element as Digimon).FieldIDs
        );
        fieldsFilter.Elements = new List<FilterEntryData>(Fields.Count);
        for (int iField = 0; iField < Fields.Count; ++iField) {
            fieldsFilter.Elements.Add(new FilterEntryData { Name = Fields[iField].Name, Sprite = Fields[iField].Sprite });
        }
        filters.Add(fieldsFilter);
      

        FilterData attributesFilter = new FilterData(
            name: AttributesFilter,
            getFilteringComponent: element => (element as Digimon).AttributeIDs
        );
        attributesFilter.Elements = new List<FilterEntryData>(Attributes.Count);
        for (int iAttribute = 0; iAttribute < Attributes.Count; ++iAttribute) {
            attributesFilter.Elements.Add(new FilterEntryData { Name = Attributes[iAttribute].Name, Sprite = Attributes[iAttribute].Sprite });
        }
        filters.Add(attributesFilter);


        FilterData typesFilter = new FilterData(
            name: TypesFilter,
            getFilteringComponent: element => (element as Digimon).TypeIDs
        );
        typesFilter.Elements = new List<FilterEntryData>(Types.Count);
        for (int iType = 0; iType < Types.Count; ++iType) {
            typesFilter.Elements.Add(new FilterEntryData { Name = Types[iType].Name });
        }
        filters.Add(typesFilter);


        FilterData levelsFilter = new FilterData(
            name: LevelsFilter,
            getFilteringComponent: element => (element as Digimon).LevelIDs
        );
        levelsFilter.Elements = new List<FilterEntryData>(Levels.Count);
        for (int iLevel = 0; iLevel < Levels.Count; ++iLevel) {
            levelsFilter.Elements.Add(new FilterEntryData { Name = Levels[iLevel].Name });
        }
        filters.Add(levelsFilter);

        FilterData groupsFilter = new FilterData(
            name: GroupsFilter,
            getFilteringComponent: element => (element as Digimon).GroupIDs
        );
        groupsFilter.Elements = new List<FilterEntryData>(Groups.Count);
        for (int iGroup = 0; iGroup < Groups.Count; ++iGroup) {
            groupsFilter.Elements.Add(new FilterEntryData { Name = Groups[iGroup].Name });
        }
        filters.Add(groupsFilter);

        return filters;
    }

    public List<ToggleActionData> RetrieveTogglesData() {
        List<ToggleActionData> toggles = new List<ToggleActionData>();

        toggles.Add(
            new ToggleActionData(
                name: ReverseToggle, 
                action: (list, isOn) => {
                    if (isOn) {
                        return list.Reverse();
                    } else {
                        return list;
                    }
                }
            )
        );

        return toggles;
    }

    private void SaveFavorites() {
        if (_favorites != null) {
            var lists = _favorites.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(h => h.ToString()));
            string jsonData = JsonConvert.SerializeObject(lists);
            UserDataManager.Instance.Save(FavDigimonPref, jsonData);
        }
    }

    private Dictionary<string, HashSet<Hash128>> LoadFavorites() {
        string jsonData = UserDataManager.Instance.Load(FavDigimonPref);
        var strings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(jsonData) ??
            new Dictionary<string, List<string>>();
        var hashesList = strings.ToDictionary(kvp => kvp.Key, 
            kvp => new HashSet<Hash128>(kvp.Value.Select(s => Hash128.Parse(s))));

        return hashesList;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Clear Favorites")]
    public static void ClearFavorites() {
        PlayerPrefs.DeleteKey(FavDigimonPref);
    }
#endif
}
