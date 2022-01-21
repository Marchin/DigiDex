using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class ApplicationManager : MonoBehaviourSingleton<ApplicationManager> {
    [SerializeField] private GameObject _loadingScreen = default;
    [SerializeField] private GameObject _loadingWheel = default;
    [SerializeField] private GameObject _inputLock = default;
    [SerializeField] private AssetReferenceAtlasedSprite _missingSprite = default;
    public AssetReferenceAtlasedSprite MissingSpirte => _missingSprite;
    private List<Handle> _loadingScreenHandles = new List<Handle>();
    private List<Handle> _loadingWheelHandles = new List<Handle>();
    private List<Handle> _inputLockingHandles = new List<Handle>();
    private DataCenter _centralDB;
    public bool Initialized { get; private set; }
    
    private async void Start() {
        await Addressables.InitializeAsync();

        _centralDB = await Addressables.LoadAssetAsync<DataCenter>(
            DataCenter.DataCenterAssetName);

        if (_centralDB == null) {
            UnityUtils.Quit();
            return;
        }

        if (!UserDataManager.Instance.HasNamingBeenPicked) {
            await PickNaming(showCloseButton: false);
        }

        UserDataManager.Instance.Sync().Forget();

        Initialized = true;
    }

    public async UniTask PickNaming(bool showCloseButton) {
        List<ButtonData> buttonList = new List<ButtonData>();
        buttonList.Add(new ButtonData("Original", () => { 
            UserDataManager.Instance.UsingDub = false;
            _ = PopupManager.Instance.Back();
        }));
        buttonList.Add(new ButtonData("Dub", () => { 
            UserDataManager.Instance.UsingDub = true;
            _ = PopupManager.Instance.Back();
        }));

        string message = "What naming convention do you want to use?";
        string currentNaming = UserDataManager.Instance.UsingDub ? "Dub" : "Original";
        
        if (UserDataManager.Instance.HasNamingBeenPicked) {
            message += $"\n(Current: {currentNaming})";
        }

        var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
        msgPopup.Populate(
            message, 
            "Naming", 
            buttonDataList: buttonList);
        msgPopup.ShowCloseButton = showCloseButton;
        
        await UniTask.WaitWhile(() => (msgPopup != null) && (PopupManager.Instance.ActivePopup == msgPopup));
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