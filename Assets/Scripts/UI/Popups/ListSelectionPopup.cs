using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class ListSelectionPopup : Popup {
    public enum Tab {
        Add,
        Copy,
        Delete
    }

    public class PopupData {
        public IDataEntry Entry;
        public Tab CurrTab;
        public List<string> ListsToCopy;
    }

    [SerializeField] private ToggleList _addToggleList = default;
    [SerializeField] private ToggleList _copyToggleList = default;
    [SerializeField] private ButtonElementList _deleteList = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private Button _copyListsButton = default;
    [SerializeField] private Button _addListButton = default;
    [SerializeField] private Toggle _switchToAdd = default;
    [SerializeField] private Toggle _switchToCopy = default;
    [SerializeField] private Toggle _switchToDelete = default;
    [SerializeField] private GameObject _addListContainer = default;
    [SerializeField] private GameObject _copyListContainer = default;
    [SerializeField] private GameObject _deleteListContainer = default;
    [SerializeField] private Button _pasteListsButton = default;
    private List<string> _listsToCopy = new List<string>();
    private IDataEntry _entry;
    private Database _db;
    private Tab _currTab;

    private void Awake() {
        _closeButton.onClick.AddListener(() => _ = PopupManager.Instance.Back());
        _addListButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
            popup.Populate("Enter the new list name", "Add List", async name => {
                if (!string.IsNullOrEmpty(name) && !_db.Lists.ContainsKey(name)) {
                    _db.AddEntryToList(name, _entry.Hash);
                    _ = PopupManager.Instance.Back();
                } else {
                    var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                    msgPopup.Populate("Name is empty or already in use, please try another one", "Try Again");
                }
                PopulateAddRemoveList();
            });
        });

        _switchToAdd.onValueChanged.AddListener(isOn => {
            _addListContainer.SetActive(isOn);
            _copyListContainer.SetActive(!isOn);
            _deleteListContainer.SetActive(!isOn);
            _pasteListsButton.gameObject.SetActive(true);
            _copyListsButton.gameObject.SetActive(false);

            PopulateAddRemoveList();

            _currTab = Tab.Add;
        });
        _switchToCopy.onValueChanged.AddListener(isOn => {
            _addListContainer.SetActive(!isOn);
            _copyListContainer.SetActive(isOn);
            _deleteListContainer.SetActive(!isOn);
            _pasteListsButton.gameObject.SetActive(false);
            _copyListsButton.gameObject.SetActive(true);

            PopulateCopyList();

            _currTab = Tab.Copy;
        });
        _switchToDelete.onValueChanged.AddListener(isOn => {
            _addListContainer.SetActive(!isOn);
            _copyListContainer.SetActive(!isOn);
            _deleteListContainer.SetActive(isOn);
            _pasteListsButton.gameObject.SetActive(false);
            _copyListsButton.gameObject.SetActive(false);

            PopulateDeleteList();

            _currTab = Tab.Delete;
        });

        _pasteListsButton.onClick.AddListener(async () => {
            var inputPopup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
            bool validClipboard = UserDataManager.Instance.IsValidData(GUIUtility.systemCopyBuffer);
            inputPopup.Populate(
                "If you've copied someone else's lists paste them here down below:",
                "Paste Lists",
                async input => {
                    bool validInput = await ParseLists(input);
                    if (!validInput) {
                        var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                        msgPopup.Populate("No list detected", "No List");
                        await UniTask.WaitWhile(() => msgPopup != null && msgPopup.gameObject.activeSelf);
                    }
                    await PopupManager.Instance.Back();
                },
                "Add",
                validClipboard ? GUIUtility.systemCopyBuffer : ""
            );
        });

        _copyListsButton.onClick.AddListener(async () => {
            var listsToCopy = new Dictionary<string, HashSet<Hash128>>();
            foreach (var kvp in _db.Lists) {
                if (_listsToCopy.Contains(kvp.Key)) {
                    listsToCopy.Add(kvp.Key, kvp.Value);
                }
            }
            var inputPopup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
            Action<string> onConfirm = (Application.platform == RuntimePlatform.WebGLPlayer) ?
                (Action<string>)null :
                (async input => { 
                    GUIUtility.systemCopyBuffer = input; 
                    _ = PopupManager.Instance.Back();
                    var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                    msgPopup.Populate("Lists Copied!", "Copied");
                });
            inputPopup.Populate(
                "Share your lists by copying and sending the text down below:",
                "Copy Lists",
                onConfirm: onConfirm,
                buttonText: "Copy",
                inputText: _db.ConvertListsToText(listsToCopy),
                readOnly: true);
        });

        _addToggleList.OnPopulate += _ => _addListButton.transform.parent.SetAsLastSibling();
    }

    public void Populate(IDataEntry entry, Tab currTab = Tab.Add, List<string> listsToCopy = null) {
        _entry = entry;
        _currTab = currTab;
        _listsToCopy = listsToCopy ?? _listsToCopy;
        _copyListsButton.interactable = (_listsToCopy.Count > 0);
        _db = ApplicationManager.Instance.GetDatabase(entry);

        switch (_currTab) {
            case Tab.Add: {
                _switchToAdd.isOn = false;
                _switchToAdd.isOn = true;
            } break;
            case Tab.Copy: {
                _switchToCopy.isOn = false;
                _switchToCopy.isOn = true;
            } break;
            case Tab.Delete: {
                _switchToDelete.isOn = false;
                _switchToDelete.isOn = true;
            } break;
        }
    }
    
    private void PopulateAddRemoveList() {
        List<ToggleData> toggles = new List<ToggleData>();
        foreach (var kvp in _db.Lists) {
            toggles.Add(new ToggleData(kvp.Key, _db.Lists[kvp.Key].Contains(_entry.Hash), isOn => {
                if (isOn) {
                    _db.AddEntryToList(kvp.Key, _entry.Hash);
                } else {
                    _db.RemoveEntryFromList(kvp.Key, _entry.Hash);
                }
                PopulateAddRemoveList();
            }));
        }
        _addToggleList.Populate(toggles);
    }

    private void PopulateCopyList() {
        List<ToggleData> toggles = new List<ToggleData>();
        foreach (var kvp in _db.Lists) {
            toggles.Add(new ToggleData(kvp.Key, _listsToCopy.Contains(kvp.Key), isOn => {
                if (isOn) {
                    _listsToCopy.Add(kvp.Key);
                } else {
                    if (_listsToCopy.Contains(kvp.Key)) {
                        _listsToCopy.Remove(kvp.Key);
                    }
                }
                _copyListsButton.interactable = (_listsToCopy.Count > 0);
            }));
        }
        _copyToggleList.Populate(toggles);
    }

    private void PopulateDeleteList() {
        List<ButtonData> buttons = new List<ButtonData>();
        foreach (var kvp in _db.Lists) {
            buttons.Add(new ButtonData(kvp.Key, async () => {
                bool removed = await _db.RemoveList(kvp.Key);
                if (removed && _listsToCopy.Contains(kvp.Key)) {
                    _listsToCopy.Remove(kvp.Key);
                }
                PopulateDeleteList();
            }));
        }
        _deleteList.Populate(buttons);
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData {
            Entry = _entry,
            CurrTab = _currTab,
            ListsToCopy = _listsToCopy
        };

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Entry, popupData.CurrTab, popupData.ListsToCopy);
        }
    }

    
    public async UniTask<bool> ParseLists(string input) {
        bool listDetected = false;
        if (UserDataManager.Instance.IsValidData(input, out var db, out var data)) {
            var listsInConflict = new Dictionary<string, HashSet<Hash128>>(data.Count);
            var newLists = new Dictionary<string, HashSet<Hash128>>(data.Count);
            foreach (var kvp in data) {
                if (db.Lists.ContainsKey(kvp.Key)) {
                    listsInConflict.Add(kvp.Key, kvp.Value);
                } else {
                    newLists.Add(kvp.Key, kvp.Value);
                }
            }

            if (listsInConflict.Count > 0) {
                listDetected = true;
                
                List<string> skippedLists = new List<string>();
                MessagePopup msgPopup = null;
                foreach (var list in listsInConflict)  {
                    const string NameConflict = "Name Conflict";

                    bool areEqual = true;
                    foreach (var kvp in list.Value) {
                        if (!db.Lists.ContainsKey(list.Key)) {
                            areEqual = false;
                            break;
                        }
                    }
                    
                    if (areEqual) {
                        skippedLists.Add(list.Key);
                        continue;
                    }

                    bool renameList = false;
                    msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                    List<ButtonData> buttons = new List<ButtonData>(2);
                    buttons.Add(new ButtonData { Text = "No", Callback = async () => { await PopupManager.Instance.Back(); }});
                    buttons.Add(new ButtonData { Text = "Yes", Callback = async () => {
                        renameList = true;
                        await PopupManager.Instance.Back();
                    }});
                    msgPopup.Populate(
                        $"There's already a list called {list.Key}," + 
                            " do you want to rename the new one and add it?",
                        NameConflict,
                        buttonDataList: buttons);
                    msgPopup.ShowCloseButton = false;

                    await UniTask.WaitWhile(() =>
                        ((msgPopup != null) && (PopupManager.Instance.ActivePopup == msgPopup)) ||
                        PopupManager.Instance.ClosingPopup);

                    if (renameList) {
                        var inputPopup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
                            inputPopup.Populate($"{list.Key} is already in use please select a new name",
                            NameConflict,
                            async name => {
                                if (!string.IsNullOrEmpty(name) && !db.Lists.ContainsKey(name)) {
                                    foreach (var entry in list.Value) {
                                        db.AddEntryToList(name, entry);
                                    }
                                    await PopupManager.Instance.Back();
                                } else {
                                    var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                                    msgPopup.Populate("Name is empty or already in use, please try another one", "Try Again");
                                    await UniTask.WaitWhile(() => (msgPopup != null) && (PopupManager.Instance.ActivePopup == msgPopup));
                                }
                            }
                        );
                        await UniTask.WaitWhile(() =>
                            ((inputPopup != null) && inputPopup.gameObject.activeSelf) ||
                            PopupManager.Instance.ClosingPopup);
                    }
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"You already have these exact same lists:");
                foreach (var list in skippedLists) {
                    sb.AppendLine($"· {list}");
                }
                msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate(sb.ToString(), "Skipped List");
                await UniTask.WaitWhile(() =>
                    ((msgPopup != null) && (PopupManager.Instance.ActivePopup == msgPopup)) ||
                    PopupManager.Instance.ClosingPopup);
            }
            
            if (newLists.Count > 0) {
                listDetected = true;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Add the following {db.DisplayName} lists:");
                foreach (var list in newLists) {
                    sb.AppendLine($"· {list.Key}");
                }
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                List<ButtonData> buttons = new List<ButtonData>(2);
                buttons.Add(new ButtonData { Text = "No", Callback = () => { _ = PopupManager.Instance.Back(); }});
                buttons.Add(new ButtonData { Text = "Yes", Callback = () => {
                    foreach (var list in newLists) {
                        foreach (var entry in list.Value) {
                            db.AddEntryToList(list.Key, entry);
                        }
                    }
                    _ = PopupManager.Instance.Back();
                }});
                msgPopup.Populate(sb.ToString(), "Add List", null, buttonDataList: buttons);
                msgPopup.ShowCloseButton = false;

                await UniTask.WaitWhile(() =>
                    ((msgPopup != null) && msgPopup.gameObject.activeSelf) ||
                    PopupManager.Instance.ClosingPopup);
            }
        }

        return listDetected;
    }
}
