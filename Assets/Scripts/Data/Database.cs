using System;
using System.Linq;
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
    public abstract string DisplayName { get; }
    public abstract string DataKey { get; }
    public abstract IEnumerable<IDataEntry> Entries { get; }
    public abstract List<FilterData> RetrieveFiltersData();
    public abstract List<ToggleActionData> RetrieveTogglesData();
    public abstract void RefreshFilters(ref IEnumerable<FilterData> filters, ref IEnumerable<ToggleActionData> toggles);
    private Dictionary<Hash128, IDataEntry> _entryDict;
    public Dictionary<Hash128, IDataEntry> EntryDict {
        get {
            if (_entryDict == null) {
                _entryDict = Entries.ToDictionary(d => d.Hash);
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
            var lists = _lists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(h => h.ToString()));
            string jsonData = JsonConvert.SerializeObject(lists);
            UserDataManager.Instance.Save(DataKey, jsonData);
        }
    }

    private Dictionary<string, HashSet<Hash128>> LoadLists() {
        string jsonData = UserDataManager.Instance.Load(DataKey);
        return ParseListData(jsonData);
    }

    public Dictionary<string, HashSet<Hash128>> ParseListData(string data) {
        var strings = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(data) ??
            new Dictionary<string, List<string>>();
        var hashesList = strings.ToDictionary(kvp => kvp.Key, 
            kvp => new HashSet<Hash128>(kvp.Value.Select(s => Hash128.Parse(s))));

        return hashesList;
    }

    public string ConvertListsToText(IEnumerable<KeyValuePair<string, HashSet<Hash128>>> lists) {
        string result = "";

        if (lists != null) {
            var serializable = lists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(h => h.ToString()));
            string jsonListData = JsonConvert.SerializeObject(serializable);
            result = JsonConvert.SerializeObject(new KeyValuePair<string, string>(DataKey, jsonListData));
        }

        return result;
    }
}
