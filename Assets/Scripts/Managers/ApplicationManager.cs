using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

public class ApplicationManager : MonoBehaviourSingleton<ApplicationManager> {
    [SerializeField] private CentralDatabase _centralDB = default;
    
    private async void Start() {
        if (_centralDB == null) {
            UnityUtils.Quit();
            return;
        }

        await Addressables.InitializeAsync();

        var databaseViewPopup = await PopupManager.Instance.GetOrLoadPopup<DatabaseViewPopup>();
        databaseViewPopup.Populate(_centralDB.DigimonDB);
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
    
    public IDatabase GetDatabase(Type type) {
        if (type.GetInterface(nameof(IDataEntry)) != null) {
            switch (type.ToString()) {
                case nameof(Digimon): {
                    return _centralDB.DigimonDB;
                }
                default: {
                    Debug.LogError("Could not fetch database based on entry");
                    return null;
                }
            }
        }

        Debug.LogError($"{type} does not implement IDataEntry");
        return null;
    }
    
    private void OnApplicationQuit() {
        _centralDB.DigimonDB.SaveFavorites();
    }
}
