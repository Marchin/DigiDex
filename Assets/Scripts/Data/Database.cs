using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

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
            buttons.Add(new ButtonData("No", PopupManager.Instance.Back));
            buttons.Add(new ButtonData("Yes", () => {
                ListsInternal.Remove(list);
                PopupManager.Instance.Back();
                SaveLists();
            }));
            msgPopup.Populate($"Delete '{list}' list?", "Delete List", buttonDataList: buttons);
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

    public void CopyToClipboard(IEnumerable<KeyValuePair<string, HashSet<Hash128>>> lists) {
        if (lists != null) {
            var serializable = lists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(h => h.ToString()));
            string jsonListData = JsonConvert.SerializeObject(serializable);
            string copyData = JsonConvert.SerializeObject(new KeyValuePair<string, string>(DataKey, jsonListData));
            ApplicationManager.Instance.SaveClipboard(copyData);
        }
    }
}
