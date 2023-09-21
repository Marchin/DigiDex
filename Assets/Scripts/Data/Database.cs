using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

public interface IDataEntry {
    string DisplayName { get; }
    string Name { get; set; }
    List<string> DubNames { get; set; }
    string Profile { get; set; }
    AssetReferenceAtlasedSprite Sprite { get; set; }
    Hash128 Hash { get; set; }
    List<InformationData> ExtractInformationData();
#if UNITY_EDITOR
    string LinkSubFix { get; set; }
#endif
}

public interface IEvolvable : IDataEntry {
    AssetReferenceEvolutionData EvolutionDataRef { get; set; }
}

[Serializable]
public class EntryIndex : IEquatable<EntryIndex> {
    [SerializeField] private string _typeName = default;
    public Hash128 Hash;

    public EntryIndex(Type type, Hash128 hash) {
        Debug.Assert(type.GetInterface(nameof(IDataEntry)) != null, $"Invalid type {type}");
        _typeName = type.FullName;
        Hash = hash;
    }

    public IDataEntry FetchEntryData() {
        MethodInfo method = typeof(ApplicationManager).GetMethod(nameof(ApplicationManager.Instance.GetDatabase),
            new Type[0]);
        MethodInfo generic = method.MakeGenericMethod(Type.GetType(_typeName));
        Database db = generic.Invoke(ApplicationManager.Instance, null) as Database;
        IDataEntry result = db?.EntryDict[Hash];

        return result;
    }

    public bool Equals(EntryIndex other) {
        bool areEqual = this._typeName == other._typeName &&
            this.Hash == other.Hash;

        return areEqual;
    }

    public override bool Equals(object other) {
        //Check whether the compared object is null.
        if (System.Object.ReferenceEquals(other, null)) return false;

        //Check whether the compared object references the same data.
        if (System.Object.ReferenceEquals(this, other)) return true;

        return this.Equals(other as Evolution);
    }

    public override int GetHashCode() {
        return _typeName.GetHashCode() * Hash.GetHashCode();
    }

    public static bool operator ==(EntryIndex entry1, EntryIndex entry2) {
      if (((object)entry1) == null || ((object)entry2) == null) {
        return System.Object.Equals(entry1, entry2);
      }

      return entry1.Equals(entry2);
   }

   public static bool operator !=(EntryIndex entry1, EntryIndex entry2) {
      if (((object)entry1) == null || ((object)entry2) == null) {
        return !System.Object.Equals(entry1, entry2);
      }

      return !(entry1.Equals(entry2));
   }
}

public abstract class Database : ScriptableObject {
    protected const string ListsFilter = "Lists";
    protected const string InListToggle = "In List";
    public abstract string DisplayName { get; }
    public abstract List<IDataEntry> Entries { get; }
    public abstract List<FilterData> RetrieveFiltersData();
    public abstract List<ToggleActionData> RetrieveTogglesData();
    private Dictionary<Hash128, IDataEntry> _entryDict;
    public Dictionary<Hash128, IDataEntry> EntryDict {
        get {
            if (_entryDict == null) {
                _entryDict = new Dictionary<Hash128, IDataEntry>(Entries.Count);
                for (int iEntry = 0; iEntry < Entries.Count; ++iEntry) {
                    if (_entryDict.ContainsKey(Entries[iEntry].Hash)) {
                        Debug.LogError($"{Entries[iEntry].Name} duplicates {_entryDict[Entries[iEntry].Hash].Name}");
                    } else {
                        _entryDict.Add(Entries[iEntry].Hash, Entries[iEntry]);
                    }
                }
            }
            return _entryDict;
        }
    }

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

    public void AddEntryToList(string list, Hash128 entry) {
        if (!ListsInternal.ContainsKey(list)) {
            ListsInternal.Add(list, new HashSet<Hash128>());
        }
        ListsInternal[list].Add(entry);
        SaveLists();
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

    public async UniTask<bool> RemoveList(string list) {
        bool result = false;
        if (ListsInternal.ContainsKey(list)) {
            var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
            List<ButtonData> buttons = new List<ButtonData>();
            buttons.Add(new ButtonData("No", () => _ = PopupManager.Instance.Back()));
            buttons.Add(new ButtonData("Yes", () => {
                result = true;
                ListsInternal.Remove(list);
                _ = PopupManager.Instance.Back();
                SaveLists();
            }));
            msgPopup.Populate($"Delete '{list}' list?", "Delete List", buttonDataList: buttons);
            
            await UniTask.WaitWhile(() =>
                ((msgPopup != null) && (PopupManager.Instance.ActivePopup == msgPopup)) ||
                PopupManager.Instance.ClosingPopup);
        }
        return result;
    }

    private void SaveLists() {
        if (_lists != null) {
            Dictionary<string, List<string>> lists = new Dictionary<string, List<string>>();
            foreach (var kvp in _lists) {
                List<string> hashes = new List<string>(kvp.Value.Count);
                foreach (var hash in kvp.Value) {
                    hashes.Add(hash.ToString());
                }
                lists.Add(kvp.Key, hashes);
            }
            string jsonData = JsonConvert.SerializeObject(lists);
            UserDataManager.Instance.Save(DisplayName, jsonData);
        }
    }

    private Dictionary<string, HashSet<Hash128>> LoadLists() {
        string jsonData = UserDataManager.Instance.Load(DisplayName);
        return ParseListData(jsonData);
    }

    public void ClearListsCache() {
        _lists = null;
    }

    public Dictionary<string, HashSet<Hash128>> ParseListData(string data) {
        var strings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data) ??
            new Dictionary<string, List<string>>();
        
        var hashesList = new Dictionary<string, HashSet<Hash128>>();

        foreach (var kvp in strings) {
            HashSet<Hash128> hashes = new HashSet<Hash128>(kvp.Value.Count);
            foreach (var hash in kvp.Value) {
                hashes.Add(Hash128.Parse(hash));
            }
            hashesList.Add(kvp.Key, hashes);
        }
        
        return hashesList;
    }

    public string ConvertListsToText(Dictionary<string, HashSet<Hash128>> lists) {
        string result = "";

        if (lists != null) {
            Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
            foreach (var kvp in lists) {
                List<string> hashes = new List<string>(kvp.Value.Count);
                foreach (var hash in kvp.Value) {
                    hashes.Add(hash.ToString());
                }
                data.Add(kvp.Key, hashes);
            }
            string jsonListData = JsonConvert.SerializeObject(data);
            result = JsonConvert.SerializeObject(new KeyValuePair<string, string>(DisplayName, jsonListData));
        }

        return result;
    }
    
    public void RefreshFilters(
        ref List<FilterData> filters, 
        ref List<ToggleActionData> toggles
    ) {
        var listFilter = filters.Find(f => f.Name == ListsFilter);
        if (listFilter != null) {
            var temp = new List<FilterEntryData>(listFilter.Elements.Count);
            foreach (var kvp in Lists) {
                bool found = false;
                for (int iElement = 0; iElement < listFilter.Elements.Count; ++iElement) {
                    if (kvp.Key == listFilter.Elements[iElement].Name) {
                        temp.Add(listFilter.Elements[iElement]);
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    temp.Add(new FilterEntryData { Name = kvp.Key });
                }
            }
            listFilter.Elements = temp;
            if (listFilter.Elements.Count <= 0) {
                filters.RemoveAll(f => f.Name == ListsFilter);
            }
        } else {
            if (Lists.Count > 0) {
                FilterData listsFilter = new FilterData(
                    name: ListsFilter,
                    getFilteringComponent: element => {
                        List<int> result = new List<int>();
                        int index = 0;
                        foreach (var kvp in Lists) {
                            if (kvp.Value.Contains(element.Hash)) {
                                result.Add(index);
                            }
                            ++index;
                        }
                        return result;
                    }
                );
                listsFilter.Elements = new List<FilterEntryData>(Lists.Count);
                foreach (var list in Lists) {
                    listsFilter.Elements.Add(new FilterEntryData { Name = list.Key });
                }
                filters.Add(listsFilter);
            }
        }

        var inListToggle = toggles.Find(t => t.Name == InListToggle);
        if (inListToggle != null) {
            if (Lists.Count <= 0) {
                toggles.RemoveAll(t => t.Name == InListToggle);
            }
        } else {
            if (Lists.Count > 0) {
                toggles.Add(
                    new ToggleActionData(
                        name: InListToggle, 
                        action: (list, isOn) => {
                            if (isOn) {
                                List<IDataEntry> result = new List<IDataEntry>(list.Count);
                                for (int iEntry = 0; iEntry < list.Count; ++iEntry) {
                                    foreach (var kvp in Lists) {
                                        if (kvp.Value.Contains(list[iEntry].Hash)) {
                                            result.Add(list[iEntry]);
                                            break;
                                        }
                                    }
                                }
                                return result;
                            } else {
                                return list;
                            }
                        }
                    )
                );
            }
        }
    }

    protected void RetrieveListFilter(ref List<FilterData> filters) {
        if (Lists.Count > 0) {
            FilterData listsFilter = new FilterData(
                name: ListsFilter,
                getFilteringComponent: element => {
                    List<int> result = new List<int>();
                    int index = 0;
                    foreach (var kvp in Lists) {
                        if (kvp.Value.Contains(element.Hash)) {
                            result.Add(index);
                        }
                        ++index;
                    }
                    return result;
                }
            );
            listsFilter.Elements = new List<FilterEntryData>(Lists.Count);
            foreach (var list in Lists) {
                listsFilter.Elements.Add(new FilterEntryData { Name = list.Key });
            }
            filters.Add(listsFilter);
        }
    }

    protected void RetrieveListToggle(ref List<ToggleActionData> toggles) {
        if (Lists.Count > 0) {
            toggles.Add(
                new ToggleActionData(
                    name: InListToggle, 
                    action: (list, isOn) => {
                        if (isOn) {
                            List<IDataEntry> result = new List<IDataEntry>(list.Count);
                            for (int iEntry = 0; iEntry < list.Count; ++iEntry) {
                                foreach (var kvp in Lists) {
                                    if (kvp.Value.Contains(list[iEntry].Hash)) {
                                        result.Add(list[iEntry]);
                                        break;
                                    }
                                }
                            }
                            return result;
                        } else {
                            return list;
                        }
                    }
                )
            );
        }
    }
}
