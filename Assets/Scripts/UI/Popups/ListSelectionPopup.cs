using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
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
            inputPopup.Populate(
                "If you've copied someone else's lists paste it here down below:",
                "Paste Lists",
                async input => {
                    bool validInput = await ApplicationManager.Instance.ParseLists(input);
                    if (!validInput) {
                        var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                        msgPopup.Populate("No list detected", "No List");
                        await UniTask.WaitWhile(() => msgPopup != null && msgPopup.gameObject.activeSelf);
                    }
                    await PopupManager.Instance.Back();
                });
        });

        _copyListsButton.onClick.AddListener(async () => {
            var listsToCopy = _db.Lists.Where(l => _listsToCopy.Contains(l.Key));
            var inputPopup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
            Action<string> onConfirm = (Application.platform == RuntimePlatform.WebGLPlayer) ?
                null :
                (input => { GUIUtility.systemCopyBuffer = input; });
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
        _addToggleList.Populate(_db.Lists.Select(kvp => 
            new ToggleData(kvp.Key, _db.Lists[kvp.Key].Contains(_entry.Hash), isOn => {
                if (isOn) {
                    _db.AddEntryToList(kvp.Key, _entry.Hash);
                } else {
                    _db.RemoveEntryFromList(kvp.Key, _entry.Hash);
                }
                PopulateAddRemoveList();
            }))
        );
    }

    private void PopulateCopyList() {
        _copyToggleList.Populate(_db.Lists.Select(kvp => 
            new ToggleData(kvp.Key, _listsToCopy.Contains(kvp.Key), isOn => {
                if (isOn) {
                    _listsToCopy.Add(kvp.Key);
                } else {
                    if (_listsToCopy.Contains(kvp.Key)) {
                        _listsToCopy.Remove(kvp.Key);
                    }
                }
                _copyListsButton.interactable = (_listsToCopy.Count > 0);
            }))
        );
    }

    private void PopulateDeleteList() {
        _deleteList.Populate(_db.Lists.Select(kvp => 
            new ButtonData(kvp.Key, async () => {
                await _db.RemoveList(kvp.Key);
                PopulateDeleteList();
            }))
        );
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
}
