using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class ApplicationManager : MonoBehaviourSingleton<ApplicationManager> {
    public class Handle {
        public bool IsComplete { get; private set; }
        public void Complete() => IsComplete = true;
    }

    [SerializeField] private GameObject _loadingScreen = default;
    [SerializeField] private AssetReferenceAtlasedSprite _missingSprite = default;
    public AssetReferenceAtlasedSprite MissingSpirte => _missingSprite;
    private List<Handle> _loadingScreenHandles = new List<Handle>();
    private CentralDatabase _centralDB;
    public bool Initialized { get; private set; }
    
    private async void Start() {
        await Addressables.InitializeAsync();

        _centralDB = await Addressables.LoadAssetAsync<CentralDatabase>(CentralDatabase.CentralDBAssetName);

        if (_centralDB == null) {
            UnityUtils.Quit();
            return;
        }

        await UserDataManager.Instance.Sync();

        Initialized = true;
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

    public async void HideLoadingScreenOnceFinished() {
        _loadingScreen.SetActive(true);
        await UniTask.WaitUntil(() => _loadingScreenHandles.TrueForAll(h => h.IsComplete));
        _loadingScreen.SetActive(false);
        _loadingScreenHandles.Clear();
    }
}
