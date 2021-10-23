using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityGoogleDrive;
using UnityGoogleDrive.Data;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

public class UserDataManager : MonoBehaviourSingleton<UserDataManager> {
    private const string SaveFileName = "DigiDex Save.json";
    private const string SaveFileCopyName = "DigiDex Save (Copy).json";
    private const string LocalDataPref = "local_data";
    private const string LastLocalSavePref = "last_local_save";
    private readonly List<string> ListFieldsQuery = new List<string> { "files/name, files/id, files/modifiedTime" };
    private readonly List<string> FileFieldsQuery = new List<string> { "name, id, modifiedTime" };
    private Dictionary<string, string> _dataDict;
    public event Action OnBeforeSave;
    private string _fileID;
    private GoogleDriveSettings _driveSettings;
    public User UserData { get; private set; }
    public bool IsUserCached => _driveSettings.IsAnyAuthTokenCached();
    public bool IsUserLoggedIn => UserData != null && _userConfirmedData;
    private string _dataOnLoad;
    private bool _userConfirmedData;
    private long _lastSyncTime;
    private bool _isSaving;
    public event Action OnAuthChanged;
    
    private void Awake() {
        _driveSettings = GoogleDriveSettings.LoadFromResources();
        string localData = PlayerPrefs.GetString(LocalDataPref);
        _dataOnLoad = localData;
        _dataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(localData) ??
            new Dictionary<string, string>();
    }

    public async UniTask Sync() {
        if (IsUserCached) {
            await Login();
        }
    }

    public async UniTask Login() {
        if (IsUserLoggedIn) {
            return;
        }
        var handle = ApplicationManager.Instance.DisplayLoadingScreen();
        try {
            var aboutRequest = UnityGoogleDrive.GoogleDriveAbout.Get();
            aboutRequest.Fields = new List<string> { "user" };
            await aboutRequest.Send();

            if (string.IsNullOrEmpty(aboutRequest.Error)) {
                UserData = aboutRequest.ResponseData.User;
                var saveFileLocation = await GetSaveMetadata();
                if (saveFileLocation != null) {
                    _fileID = saveFileLocation.Id;
                    var downloadRequest = GoogleDriveFiles.Download(_fileID);
                    var fileData = await downloadRequest.Send();

                    if (fileData.Content != null) {
                        long localModifiedTime = long.Parse(PlayerPrefs.GetString(LastLocalSavePref, "0"));
                        if (_dataDict.Count > 0 && saveFileLocation.ModifiedTime.Value.Ticks != localModifiedTime) {
                            var popup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                            popup.ShowCloseButton = false;
                            ToggleData keepCopyToggle = new ToggleData { Name = "Keep a copy", IsOn = true };
                            Action keepLocal = async () => {
                                if (keepCopyToggle.IsOn) {
                                    var copyFile = new UnityGoogleDrive.Data.File {
                                        Name = SaveFileCopyName + DateTime.Now.ToString(),
                                        Content = fileData.Content
                                    };
                                    _ = GoogleDriveFiles.Create(copyFile).Send();
                                }

                                string jsonData = PlayerPrefs.GetString(LocalDataPref);
                                var file = new UnityGoogleDrive.Data.File {
                                    Name = SaveFileName,
                                    Content = Encoding.ASCII.GetBytes(jsonData)
                                };
                                var handle = ApplicationManager.Instance.DisplayLoadingScreen();
                                var updateRequest = UnityGoogleDrive.GoogleDriveFiles.Update(_fileID, file);
                                updateRequest.Fields = FileFieldsQuery;
                                file = await updateRequest.Send();
                                _dataOnLoad = jsonData;
                                _userConfirmedData = true;
                                RefreshDataDate(file.ModifiedTime.Value);
                                PopupManager.Instance.Back();
                                OnAuthChanged?.Invoke();
                                handle.Complete();
                            };
                            Action keepCloud = () => {
                                if (keepCopyToggle.IsOn) {
                                    string localData = PlayerPrefs.GetString(LocalDataPref);
                                    var file = new UnityGoogleDrive.Data.File {
                                        Name = SaveFileCopyName,
                                        Content = Encoding.ASCII.GetBytes(localData)
                                    };
                                    GoogleDriveFiles.Create(file).Send();
                                }
                                string jsonData = Encoding.ASCII.GetString(fileData.Content);
                                _dataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                                _dataOnLoad = jsonData;
                                _userConfirmedData = true;
                                RefreshDataDate(saveFileLocation.ModifiedTime.Value);
                                PopupManager.Instance.Back();
                                OnAuthChanged?.Invoke();
                            };

                            List<ButtonData> buttons = new List<ButtonData>(2);
                            buttons.Add(new ButtonData { Text = "Local", Callback = keepLocal });
                            buttons.Add(new ButtonData { Text = "Cloud", Callback = keepCloud });
                            List<ToggleData> toggles = new List<ToggleData>(1);
                            toggles.Add(keepCopyToggle);
                            string msg = "There's a newer version of your data, which one you want to use?";
                            popup.Populate(msg, "Data Conflict", buttonDataList: buttons, toggleDataList: toggles);
                        } else {
                            _userConfirmedData = true;
                        }
                    } else {
                    }
                } else {
                    Debug.LogError("File Location not found");
                }
            } else {
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate("Failed to authenticate your google account", "Authentication Fail");
            }
        } catch (Exception ex) {
            Debug.LogError($"{ex.Message} \n {ex.StackTrace}");
        } finally {
            handle.Complete();
        }
    }

    public void LogOut() {
        _driveSettings?.DeleteCachedAuthTokens();
        UserData = null;
        _userConfirmedData = false;
        OnAuthChanged?.Invoke();
    }

    private async UniTask<UnityGoogleDrive.Data.File> GetSaveMetadata() {
        var filesRequest = GoogleDriveFiles.List();
        filesRequest.Fields = ListFieldsQuery;
        filesRequest.Q = $"name='{SaveFileName}'";
        await filesRequest.Send();
        var saveFileLocation = filesRequest.ResponseData.Files.Find(f => f.Name == SaveFileName);
        return saveFileLocation;
    }

    public void Save(string key, string data) {
        if (_dataDict.ContainsKey(key)) {
            _dataDict[key] = data;
        } else {
            _dataDict.Add(key, data);
        }
        SaveAllData();
    }

    public string Load(string key) {
        if (_dataDict.ContainsKey(key)) {
            return _dataDict[key];
        } else {
            return "";
        }
    }

    public async void SaveAllData() {
        if (_isSaving) {
            return;
        }

        _isSaving = true;

        OnBeforeSave?.Invoke();

        string jsonData = JsonConvert.SerializeObject(_dataDict);

        if (jsonData != _dataOnLoad) {
            PlayerPrefs.SetString(LocalDataPref, jsonData);
            _dataOnLoad = jsonData;
            long localModifiedTime = long.Parse(PlayerPrefs.GetString(LastLocalSavePref));
            RefreshDataDate();

            var saveMetadata = await GetSaveMetadata();
            if (IsUserLoggedIn && saveMetadata.ModifiedTime.Value.Ticks <= localModifiedTime)  {
                var file = new UnityGoogleDrive.Data.File {
                    Name = SaveFileName,
                    Content = Encoding.ASCII.GetBytes(jsonData)
                };
                if (string.IsNullOrEmpty(_fileID)) {
                    var createRequest = UnityGoogleDrive.GoogleDriveFiles.Create(file);
                    createRequest.Fields = FileFieldsQuery;
                    file = await createRequest.Send();
                } else {
                    var updateRequest = UnityGoogleDrive.GoogleDriveFiles.Update(_fileID, file);
                    updateRequest.Fields = FileFieldsQuery;
                    file = await updateRequest.Send();
                }
                RefreshDataDate(file.ModifiedTime.Value);
            }
        }

        _isSaving = false;
    }

    private void RefreshDataDate() {
        RefreshDataDate(DateTime.UtcNow);
    }

    private void RefreshDataDate(DateTime time) {
        PlayerPrefs.SetString(LastLocalSavePref, time.Ticks.ToString());
    }
}