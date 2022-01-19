using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class ApplicationManager : MonoBehaviourSingleton<ApplicationManager> {
    private const string LastClipboardPref = "last_clipboard";
    [SerializeField] private GameObject _loadingScreen = default;
    [SerializeField] private GameObject _loadingWheel = default;
    [SerializeField] private GameObject _inputLock = default;
    [SerializeField] private AssetReferenceAtlasedSprite _missingSprite = default;
    public AssetReferenceAtlasedSprite MissingSpirte => _missingSprite;
    private List<Handle> _loadingScreenHandles = new List<Handle>();
    private List<Handle> _loadingWheelHandles = new List<Handle>();
    private List<Handle> _inputLockingHandles = new List<Handle>();
    private DataCenter _centralDB;
    private bool _checkingClipboard;
    public bool Initialized { get; private set; }
    
    private async void Start() {
        await Addressables.InitializeAsync();

        _centralDB = await Addressables.LoadAssetAsync<DataCenter>(
            DataCenter.DataCenterAssetName);

        if (_centralDB == null) {
            UnityUtils.Quit();
            return;
        }

        UserDataManager.Instance.Sync().Forget();

        Initialized = true;
    }

    public async UniTask<bool> ParseLists(string input) {
        bool listDetected = false;
        if (!_checkingClipboard) {
            _checkingClipboard = true;
            if (UserDataManager.Instance.IsValidData(input, out var db, out var data)) {
                var newLists = data.Where(l => !db.Lists.ContainsKey(l.Key));
                var listsInConflict = data.Except(newLists);
                if (listsInConflict.Count() > 0) {
                    listDetected = true;
                    
                    List<string> skippedLists = new List<string>();
                    MessagePopup msgPopup = null;
                    foreach (var list in listsInConflict)  {
                        const string NameConflict = "Name Conflict";

                        bool areEqual = list.Value.Except(db.Lists[list.Key]).Count() == 0;
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
                                    if (!string.IsNullOrEmpty(name) && !db.Lists.Keys.Contains(name)) {
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
                
                if (newLists.Count() > 0) {
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
            _checkingClipboard = false;
        }

        return listDetected;
    }

    public Database GetDatabase(IDataEntry entry) {
        Database result = null;

        switch (entry) {
            case Digimon digimon: {
                result = _centralDB.DigimonDB;
            } break;
            case Appmon appmon: {
                result = _centralDB.AppmonDB;
            } break;

            default: {
                Debug.LogError("Entry does not have corresponding database");
            } break;
        }

        return result;
    }
    
    public Database GetDatabase<T>() where T : IDataEntry {
        switch (typeof(T).ToString()) {
            case nameof(Digimon): {
                return _centralDB.DigimonDB;
            }
            case nameof(Appmon): {
                return _centralDB.AppmonDB;
            }
            default: {
                Debug.LogError("Could not fetch database based on entry");
                return null;
            }
        }
    }

    public List<Database> GetDatabases() => _centralDB?.GetDatabases();
    
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            _ = PopupManager.Instance.Back();
        }
    }

    public Handle DisplayLoadingScreen() {
        Handle handle = new Handle();
        _loadingScreenHandles.Add(handle);

        if (_loadingScreenHandles.Count == 1) {
            HideLoadingScreenOnceFinished();
        }

        return handle;
    }

    private async void HideLoadingScreenOnceFinished() {
        _loadingScreen.SetActive(true);
        await UniTask.WaitUntil(() => _loadingScreenHandles.TrueForAll(h => h.IsComplete));
        _loadingScreen.SetActive(false);
        _loadingScreenHandles.Clear();
    }

    public Handle DisplayLoadingWheel() {
        Handle handle = new Handle();
        _loadingWheelHandles.Add(handle);

        if (_loadingWheelHandles.Count == 1) {
            HideLoadingWheelOnceFinished();
        }

        return handle;
    }

    private async void HideLoadingWheelOnceFinished() {
        _loadingWheel.SetActive(true);
        await UniTask.WaitUntil(() => _loadingWheelHandles.TrueForAll(h => h.IsComplete));
        _loadingWheel.SetActive(false);
        _loadingWheelHandles.Clear();
    }

    public Handle LockScreen() {
        Handle handle = new Handle();
        _inputLockingHandles.Add(handle);

        if (_inputLockingHandles.Count == 1) {
            UnlockScreenOnceFinished();
        }

        return handle;
    }

    private async void UnlockScreenOnceFinished() {
        _inputLock.SetActive(true);
        await UniTask.WaitUntil(() => _inputLockingHandles.TrueForAll(h => h.IsComplete));
        _inputLock.SetActive(false);
        _inputLockingHandles.Clear();
    }
}