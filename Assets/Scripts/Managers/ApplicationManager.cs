using System;
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
    public OperationBySubscription ShowLoadingScreen { get; private set; }
    public OperationBySubscription ShowLoadingWheel { get; private set; }
    public OperationBySubscription LockScreen { get; private set; }
    private OperationBySubscription.Subscription _loadingWheelSubscription;
    public event Action OverrideBack;
    private DataCenter _centralDB;
    public bool Initialized { get; private set; }
    
    private async void Start() {
        ShowLoadingScreen = new OperationBySubscription(
            onStart: () => {
                _loadingScreen.SetActive(true);
                _loadingWheelSubscription = ShowLoadingWheel.Subscribe();
            },
            onAllFinished: () => {
                _loadingScreen.SetActive(false);
                _loadingWheelSubscription.Finish();
            }
        );

        ShowLoadingWheel = new OperationBySubscription(
            onStart: () => _loadingWheel.SetActive(true),
            onAllFinished: () => _loadingWheel.SetActive(false)
        );

        LockScreen = new OperationBySubscription(
            onStart: () => _inputLock.SetActive(true),
            onAllFinished: () => _inputLock.SetActive(false)
        );

        UserDataManager.Instance.OnLocalDataOverriden += () => {
            foreach (var db in _centralDB.GetDatabases()) {
                db.ClearListsCache();
            }
        };

        await Addressables.InitializeAsync();

        _centralDB = await Addressables.LoadAssetAsync<DataCenter>(
            DataCenter.DataCenterAssetName);

        if (_centralDB == null) {
            UnityUtils.Quit();
            return;
        }

        if (!UserDataManager.Instance.HasNamingBeenPicked) {
            await UserDataManager.Instance.Sync();

            // Chech if naming setting has been loaded from sync
            if (!UserDataManager.Instance.HasNamingBeenPicked) {
                await PickNaming(showCloseButton: false);
            }
        } else {
            UserDataManager.Instance.Sync().Forget();
        }


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
            if (OverrideBack != null && OverrideBack.GetInvocationList().Length > 0) {
                OverrideBack?.Invoke();
            } else {
                _ = PopupManager.Instance.Back();
            }
        }
    }
}