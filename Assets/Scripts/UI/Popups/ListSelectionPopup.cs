using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class ListSelectionPopup : Popup {
    public enum Tab {
        Add,
        Copy
    }

    public class PopupData {
        public IDataEntry Entry;
        public Tab CurrTab;
    }

    [SerializeField] private ToggleList _addToggleList = default;
    [SerializeField] private ToggleList _copyToggleList = default;
    [SerializeField] private Button _closeButton = default;
    [SerializeField] private Button _addListButton = default;
    [SerializeField] private Button _copyButton = default;
    [SerializeField] private Button _switchToCopy = default;
    [SerializeField] private Button _switchToAdd = default;
    [SerializeField] private GameObject _addListContainer = default;
    [SerializeField] private GameObject _copyListContainer = default;
    private List<string> _toCopy = new List<string>();
    private IDataEntry _entry;
    private IDatabase _db;
    private Tab _currTab;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
        _addListButton.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
            popup.Populate("Enter the new list name", "Add List", async name => {
                if (_db.AddList(name)) {
                    _db.AddEntryToList(name, _entry.Hash);
                    PopupManager.Instance.Back();
                } else {
                    var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                    msgPopup.Populate("List name already exists", "Name Conflict");
                }
                PopulateAddRemoveLists();
            });
        });
        _copyButton.onClick.AddListener(() => {
            var listsToCopy = _db.Lists.Where(l => _toCopy.Contains(l.Key));
            _db.CopyToClipboard(listsToCopy);
        });
        _switchToCopy.onClick.AddListener(() => {
            _switchToCopy.gameObject.SetActive(false);
            _addListContainer.SetActive(false);
            _addListButton.gameObject.SetActive(false);

            _switchToAdd.gameObject.SetActive(true);
            _copyListContainer.SetActive(true);
            _copyButton.gameObject.SetActive(true);
            PopulateCopyLists();

            _currTab = Tab.Copy;
        });
        _switchToAdd.onClick.AddListener(() => {
            _switchToCopy.gameObject.SetActive(true);
            _addListContainer.SetActive(true);
            _addListButton.gameObject.SetActive(true);
            
            _switchToAdd.gameObject.SetActive(false);
            _copyListContainer.SetActive(false);
            _copyButton.gameObject.SetActive(false);
            PopulateAddRemoveLists();

            _currTab = Tab.Add;
        });
    }

    public void Populate(IDataEntry entry, Tab currTab = Tab.Add) {
        _entry = entry;
        _currTab = currTab;
        _db = ApplicationManager.Instance.GetDatabase(entry);

        switch (_currTab) {
            case Tab.Add: {
                _switchToAdd.onClick.Invoke();
            } break;
            case Tab.Copy: {
                _switchToCopy.onClick.Invoke();
            } break;
        }
    }
    
    private void PopulateAddRemoveLists() {
        _addToggleList.Populate(_db.Lists.Select(kvp => 
            new ToggleData(kvp.Key, _db.Lists[kvp.Key].Contains(_entry.Hash), isOn => {
                if (isOn) {
                    _db.AddEntryToList(kvp.Key, _entry.Hash);
                } else {
                    _db.RemoveEntryFromList(kvp.Key, _entry.Hash);
                }
                PopulateAddRemoveLists();
            }))
        );
    }

    private void PopulateCopyLists() {
        _copyToggleList.Populate(_db.Lists.Select(kvp => 
            new ToggleData(kvp.Key, false, isOn => {
                if (isOn) {
                    _toCopy.Add(kvp.Key);
                } else {
                    if (_toCopy.Contains(kvp.Key)) {
                        _toCopy.Remove(kvp.Key);
                    }
                }
                PopulateAddRemoveLists();
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
