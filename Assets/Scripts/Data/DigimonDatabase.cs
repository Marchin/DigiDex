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
    public const string ListsFilter = "Lists";
    public const string InListToggle = "In List";
    public const string ReverseToggle = "Reverse";
    public List<Digimon> Digimons;
    public IEnumerable<IDataEntry> EntryList => Digimons.Cast<IDataEntry>();
    public List<Field> Fields;
    public List<Attribute> Attributes;
    public List<DigimonType> Types;
    public List<DigimonGroup> Groups;
    public List<Level> Levels;
    private Dictionary<string, HashSet<Hash128>> _lists;
    private Dictionary<string, HashSet<Hash128>> ListsInternal {
        get {
            if (_lists == null) {
                _lists = LoadLists();
            }
            return _lists;
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
            SaveLists();
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
            SaveLists();
        } else {
            Debug.LogWarning("List doesn't exist");
        }
    }

    public bool AddList(string name) {
        if (!ListsInternal.ContainsKey(name)) {
            ListsInternal.Add(name, new HashSet<Hash128>());
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
            fieldsFilter.Elements.Add(
                new FilterEntryData { Name = Fields[iField].Name, 
                    Sprite = Fields[iField].Sprite });
        }
        filters.Add(fieldsFilter);
      

        FilterData attributesFilter = new FilterData(
            name: AttributesFilter,
            getFilteringComponent: element => (element as Digimon).AttributeIDs
        );
        attributesFilter.Elements = new List<FilterEntryData>(Attributes.Count);
        for (int iAttribute = 0; iAttribute < Attributes.Count; ++iAttribute) {
            attributesFilter.Elements.Add(
                new FilterEntryData { Name = Attributes[iAttribute].Name,
                    Sprite = Attributes[iAttribute].Sprite });
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

        if (Lists.Count > 0) {
            FilterData listsFilter = new FilterData(
                name: ListsFilter,
                getFilteringComponent: element => {
                    List<string> listsNames = Lists.Keys.ToList();
                    return Lists
                        .Where(kvp => kvp.Value.Contains(element.Hash))
                        .Select(kvp => listsNames.IndexOf(kvp.Key))
                        .ToList();
                }
            );
            listsFilter.Elements = new List<FilterEntryData>(Lists.Count);
            for (int iList = 0; iList < Lists.Count; ++iList) {
                listsFilter.Elements.Add(new FilterEntryData { Name = Lists.ElementAt(iList).Key });
            }
            filters.Add(listsFilter);
        }

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

        if (Lists.Count > 0) {
            toggles.Add(
                new ToggleActionData(
                    name: InListToggle, 
                    action: (list, isOn) => {
                        if (isOn) {
                            return list.Where(e => Lists.Any(kvp => kvp.Value.Contains(e.Hash)));
                        } else {
                            return list;
                        }
                    }
                )
            );
        }

        return toggles;
    }

    public void RefreshFilters(
        ref IEnumerable<FilterData> filters, 
        ref IEnumerable<ToggleActionData> toggles
    ) {
        var listFilter = filters.FirstOrDefault(f => f.Name == ListsFilter);
        if (listFilter != null) {
            var elements = listFilter.Elements.Where(f => Lists.Any(kvp => kvp.Key == f.Name));
            listFilter.Elements = elements
                .Concat(
                    Lists.Where(kvp => !elements.Any(e => e.Name == kvp.Key))
                        .Select(kvp => new FilterEntryData { Name = kvp.Key }))
                .ToList();
            if (listFilter.Elements.Count() <= 0) {
                filters = filters.Where(f => f.Name != ListsFilter);
            }
        } else {
            if (Lists.Count > 0) {
                FilterData listsFilter = new FilterData(
                    name: ListsFilter,
                    getFilteringComponent: element => {
                        List<string> listsNames = Lists.Keys.ToList();
                        return Lists
                            .Where(kvp => kvp.Value.Contains(element.Hash))
                            .Select(kvp => listsNames.IndexOf(kvp.Key))
                            .ToList();
                    }
                );
                listsFilter.Elements = new List<FilterEntryData>(Lists.Count);
                for (int iList = 0; iList < Lists.Count; ++iList) {
                    listsFilter.Elements.Add(new FilterEntryData { Name = Lists.ElementAt(iList).Key });
                }
                filters = filters.Append(listsFilter);
            }
        }

        var inListToggle = toggles.FirstOrDefault(t => t.Name == InListToggle);
        if (inListToggle != null) {
            if (Lists.Count <= 0) {
                toggles = toggles.Where(t => t.Name != InListToggle);
            }
        } else {
            if (Lists.Count > 0) {
                toggles = toggles.Append(
                    new ToggleActionData(
                        name: InListToggle, 
                        action: (list, isOn) => {
                            if (isOn) {
                                return list.Where(e => Lists.Any(kvp => kvp.Value.Contains(e.Hash)));
                            } else {
                                return list;
                            }
                        }
                    )
                );
            }
        }
    }

    private void SaveLists() {
        if (_lists != null) {
            var lists = _lists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(h => h.ToString()));
            string jsonData = JsonConvert.SerializeObject(lists);
            UserDataManager.Instance.Save(DisplayName, jsonData);
        }
    }

    public void CopyToClipboard(IEnumerable<KeyValuePair<string, HashSet<Hash128>>> lists) {
        if (lists != null) {
            var serializable = lists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(h => h.ToString()));
            string jsonListData = JsonConvert.SerializeObject(serializable);
            string copyData = JsonConvert.SerializeObject(new KeyValuePair<string, string>(DisplayName, jsonListData));
            ApplicationManager.Instance.SaveClipboard(copyData);
        }
    }

    private Dictionary<string, HashSet<Hash128>> LoadLists() {
        string jsonData = UserDataManager.Instance.Load(DisplayName);
        return ParseListData(jsonData);
    }

    public Dictionary<string, HashSet<Hash128>> ParseListData(string data) {
        var strings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data) ??
            new Dictionary<string, List<string>>();
        var hashesList = strings.ToDictionary(kvp => kvp.Key, 
            kvp => new HashSet<Hash128>(kvp.Value.Select(s => Hash128.Parse(s))));

        return hashesList;
    }
}
