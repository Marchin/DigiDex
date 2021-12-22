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
    private CentralDatabase _centralDB;
    private string _lastCopyText;
    private bool _checkingClipboard;
    public bool Initialized { get; private set; }
    
    private async void Start() {
        _lastCopyText = PlayerPrefs.GetString(LastClipboardPref, "");

        await Addressables.InitializeAsync();

        _centralDB = await Addressables.LoadAssetAsync<CentralDatabase>(
            CentralDatabase.CentralDBAssetName);

        if (_centralDB == null) {
            UnityUtils.Quit();
            return;
        }

        UserDataManager.Instance.Sync().Forget();

        Initialized = true;
        CheckClipboard();
    }

    private void OnApplicationFocus(bool focus) {
        if (Initialized && focus) {
            CheckClipboard();
        }
    }

    private async void CheckClipboard() {
        if (!_checkingClipboard && (GUIUtility.systemCopyBuffer != _lastCopyText)) {
            _checkingClipboard = true;
            _lastCopyText = GUIUtility.systemCopyBuffer;
            PlayerPrefs.SetString(LastClipboardPref, _lastCopyText);
            if (UserDataManager.Instance.IsValidData(GUIUtility.systemCopyBuffer, out var db, out var data)) {
                var newLists = data.Where(l => !db.Lists.ContainsKey(l.Key));
                var listsInConflict = data.Except(newLists);
                if (listsInConflict.Count() > 0) {
                    foreach (var list in listsInConflict)  {
                        const string NameConflict = "Name Conflict";

                        bool areEqual = list.Value.Except(db.Lists[list.Key]).Count() == 0;
                        if (areEqual) {
                            continue;
                        }

                        bool renameList = false;
                        var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                        msgPopup.ShowCloseButton = false;
                        List<ButtonData> buttons = new List<ButtonData>(2);
                        buttons.Add(new ButtonData { Text = "Yes", Callback = () => {
                            renameList = true;
                            PopupManager.Instance.Back();
                        }});
                        buttons.Add(new ButtonData { Text = "No", Callback = PopupManager.Instance.Back });
                        msgPopup.Populate(
                            $"There's already a list called {list.Key}," + 
                                " do you want to rename the new one and add it?",
                            NameConflict,
                            buttonDataList: buttons);

                        await UniTask.WaitWhile(() => PopupManager.Instance.ActivePopup == msgPopup);

                        if (renameList) {
                            var inputPopup = await PopupManager.Instance.GetOrLoadPopup<InputPopup>();
                                inputPopup.Populate($"{list.Key} is already in use please select a new name",
                                NameConflict,
                                name => {
                                    if (!db.Lists.Keys.Contains(name)) {
                                        foreach (var entry in list.Value) {
                                            db.AddEntryToList(name, entry);
                                        }
                                        PopupManager.Instance.Back();
                                    }
                                }
                            );
                            await UniTask.WaitWhile(() => inputPopup.gameObject.activeSelf);
                        }
                    }
                }
                
                if (newLists.Count() > 0) {
                    Debug.LogWarning("New lists");
                    StringBuilder sb = new StringBuilder();
                    foreach (var list in newLists) {
                        sb.AppendLine(list.Key);
                    }
                    var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                    msgPopup.ShowCloseButton = false;
                    List<ButtonData> buttons = new List<ButtonData>(2);
                    buttons.Add(new ButtonData { Text = "Yes", Callback = () => {
                        foreach (var list in newLists) {
                            foreach (var entry in list.Value) {
                                db.AddEntryToList(list.Key, entry);
                            }
                        }
                        PopupManager.Instance.Back();
                    }});
                    buttons.Add(new ButtonData { Text = "No", Callback = PopupManager.Instance.Back });
                    msgPopup.Populate(sb.ToString(), "Add List", null, buttonDataList: buttons);

                    await UniTask.WaitWhile(() => msgPopup.gameObject.activeSelf);
                }
            }
            _checkingClipboard = false;
        }
    }
    
    public void SaveClipboard(string data) {
        _lastCopyText = data;
        PlayerPrefs.SetString(LastClipboardPref, _lastCopyText);
        GUIUtility.systemCopyBuffer = data;
    }

    public IDatabase GetDatabase(IDataEntry entry) {
        IDatabase result = null;

        switch (entry) {
            case Digimon digimon: {
                result = _centralDB.DigimonDB;
            } break;

            default: {
                Debug.LogError("Entry does not have corresponding database");
            } break;
        }

        return result;
    }
    
    public IDatabase GetDatabase<T>() where T : IDataEntry {
        switch (typeof(T).ToString()) {
            case nameof(Digimon): {
                return _centralDB.DigimonDB;
            }
            default: {
                Debug.LogError("Could not fetch database based on entry");
                return null;
            }
        }
    }

    public List<IDatabase> GetDatabases() => _centralDB?.GetDatabases();
    
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            PopupManager.Instance.Back();
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
