using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ListsSelectionPopup : Popup {
    public class PopupData {
        public IDataEntry Entry;
    }

    [SerializeField] private ToggleList _lists = default;
    [SerializeField] private Button _addList = default;
    [SerializeField] private Button _closeButton = default;
    private IDataEntry _entry;
    private IDatabase _db;

    private void Awake() {
        _closeButton.onClick.AddListener(PopupManager.Instance.Back);
        _addList.onClick.AddListener(async () => {
            var popup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
            popup.Populate("Enter the new list name", "Add List", async name => {
                if (_db.AddList(name)) {
                    _db.AddEntryToList(name, _entry.Hash);
                    PopupManager.Instance.Back();
                } else {
                    var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                    msgPopup.Populate("List name already exists", "Name Conflict");
                }
                PopulateLists();
            });
        });

    }

    public void Populate(IDataEntry entry) {
        _entry = entry;
        _db = ApplicationManager.Instance.GetDatabase(entry);
        PopulateLists();
    }
    
    private void PopulateLists() {
        _lists.Populate(_db.Lists.Select(kvp => 
            new ToggleData(kvp.Key, _db.Lists[kvp.Key].Contains(_entry.Hash), isOn => {
                if (isOn) {
                    _db.AddEntryToList(kvp.Key, _entry.Hash);
                } else {
                    _db.RemoveEntryFromList(kvp.Key, _entry.Hash);
                }
                PopulateLists();
            }))
        );
    }

    public override object GetRestorationData() {
        PopupData data = new PopupData { Entry = _entry };

        return data;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Entry);
        }
    }
}
