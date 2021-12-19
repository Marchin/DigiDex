using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class ListSelectionPopup : Popup {
    public enum Tab {
        Add,
        Copy,
        Delete
    }

    public class PopupData {
        public IDataEntry Entry;
        public Tab CurrTab;
    }

    [SerializeField] private ToggleList _addToggleList = default;
    [SerializeField] private ToggleList _copyToggleList = default;
    [SerializeField] private ButtonElementList _deleteList = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private Button _addListButton = default;
    [SerializeField] private Toggle _switchToAdd = default;
    [SerializeField] private Toggle _switchToCopy = default;
    [SerializeField] private Toggle _switchToDelete = default;
    [SerializeField] private GameObject _addListContainer = default;
    [SerializeField] private GameObject _copyListContainer = default;
    [SerializeField] private GameObject _deleteListContainer = default;
    private List<string> _toCopy = new List<string>();
    private IDataEntry _entry;
    private IDatabase _db;
    private Tab _currTab;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
        _addListButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
            popup.Populate("Enter the new list name", "Add List", async name => {
                if (!_db.Lists.ContainsKey(name)) {
                    _db.AddEntryToList(name, _entry.Hash);
                    PopupManager.Instance.Back();
                } else {
                    var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                    msgPopup.Populate("List name already exists", "Name Conflict");
                }
                PopulateAddRemoveList();
            });
        });

        _switchToAdd.onValueChanged.AddListener(isOn => {
            _addListContainer.SetActive(isOn);
            _copyListContainer.SetActive(!isOn);
            _deleteListContainer.SetActive(!isOn);

            PopulateAddRemoveList();

            _currTab = Tab.Add;
        });
        _switchToCopy.onValueChanged.AddListener(isOn => {
            _addListContainer.SetActive(!isOn);
            _copyListContainer.SetActive(isOn);
            _deleteListContainer.SetActive(!isOn);

            PopulateCopyList();

            _currTab = Tab.Copy;
        });
        _switchToDelete.onValueChanged.AddListener(isOn => {
            _addListContainer.SetActive(!isOn);
            _copyListContainer.SetActive(!isOn);
            _deleteListContainer.SetActive(isOn);

            PopulateDeleteList();

            _currTab = Tab.Delete;
        });

        _addToggleList.OnPopulate += _ => _addListButton.transform.parent.SetAsLastSibling();
    }

    public void Populate(IDataEntry entry, Tab currTab = Tab.Add) {
        _entry = entry;
        _currTab = currTab;
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
            new ToggleData(kvp.Key, false, isOn => {
                if (isOn) {
                    _toCopy.Add(kvp.Key);
                } else {
                    if (_toCopy.Contains(kvp.Key)) {
                        _toCopy.Remove(kvp.Key);
                    }
                }
                var listsToCopy = _db.Lists.Where(l => _toCopy.Contains(l.Key));
                _db.CopyToClipboard(listsToCopy);
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
        PopupData data = new PopupData { Entry = _entry, CurrTab = _currTab };

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Entry, popupData.CurrTab);
        }
    }
}
