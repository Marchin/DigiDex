using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityGoogleDrive;
using UnityGoogleDrive.Data;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

public class UserDataManager : MonoBehaviourSingleton<UserDataManager> {
    private const int Version = 2;
    private const string FileHeader = "DIGIDEX";
    private const string FolderName = "DigiDex";
    private const string SaveFileName = "DigiDex.json";
    private const string SaveFileCopyName_date = "DigiDex(Copy) {0}.json";
    private const string LocalDataPref = "local_data";
    private const string LastLocalSavePref = "last_local_save";
    private const string LastLocalSaveUploadedPref = "last_local_save_uploaded";
    private const string FolderMimeType = "application/vnd.google-apps.folder";
    private readonly List<string> ListFieldsQuery = new List<string> { "files/name, files/id, files/modifiedTime" };
    private readonly List<string> FileFieldsQuery = new List<string> { "name, id, modifiedTime" };
    private const string UsingDubKey = "using_dub";
    private Dictionary<string, string> _listsDataDict;
    public event Action OnBeforeSave;
    private string _fileID;
    private string _folderID;
    private GoogleDriveSettings _driveSettings;
    public User UserData { get; private set; }
    public bool IsUserCached => _driveSettings.IsAnyAuthTokenCached();
    public bool IsUserLoggedIn => UserData != null && _userConfirmedData;
    private string _dataOnLoad;
    private bool _userConfirmedData;
    private bool _isSaving;
    private bool _usingDub;
    public bool UsingDub {
        get => _usingDub;
        set {
            _usingDub = value;
            HasNamingBeenPicked = true;
            SaveAllData();
        }
    }
    public bool HasNamingBeenPicked { get; private set; }
    public bool IsLoggingIn { get; private set; }
    public event Action OnAuthChanged;
    
    
    private void Awake() {
        _listsDataDict = new Dictionary<string, string>();
        _driveSettings = GoogleDriveSettings.LoadFromResources();
        string localData = PlayerPrefs.GetString(LocalDataPref);
        _dataOnLoad = localData;
        _listsDataDict = ParseFileData(localData);
    }

    private Dictionary<string, string> ParseFileData(string fileContent) {
        var result = new Dictionary<string, string>();

        int endOfHeader = fileContent.IndexOf('\n');
        string digidex = fileContent.Substring(0, Mathf.Max(endOfHeader, 0));
        if (endOfHeader >= 0 && digidex == FileHeader) {
            fileContent = fileContent.Substring(endOfHeader + 1, fileContent.Length - (endOfHeader + 1));
            int endOfVersion = fileContent.IndexOf('\n');
            int version = int.Parse(fileContent.Substring(0, Mathf.Max(endOfVersion, 0)));
            if (endOfVersion >= 0) {
                if (version == 1) {
                    fileContent = fileContent.Replace("Digimons", "Digimon").Replace("Appmons", "Appmon");
                    Debug.Log("Data transformed from V1 to V2");
                    version = 2;
                }

                if (version == Version) {
                    fileContent = fileContent.Substring(endOfVersion + 1, fileContent.Length - (endOfVersion + 1));
                    if (fileContent.StartsWith(UsingDubKey)) {
                        HasNamingBeenPicked = true;
                        fileContent = fileContent.Replace(UsingDubKey + ": ", "");
                        int endOfUsingDub = fileContent.IndexOf('\n');
                        bool.TryParse(fileContent.Substring(0, endOfUsingDub), out _usingDub);
                        fileContent = fileContent.Substring(endOfUsingDub + 1, fileContent.Length - (endOfUsingDub + 1));
                    }
                    result = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileContent);
                    Debug.Log("Data Loaded");
                }
            } else {
                Debug.Log("Version Mismatch");
            }
        } else {
            Debug.Log("No Header");
        }

        return result;
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
        Handle loadingWheelHandle = null;
        Handle loadingScreenHandle = null;
        try {
            IsLoggingIn = true;
            OnAuthChanged?.Invoke();
            loadingWheelHandle = ApplicationManager.Instance.DisplayLoadingWheel();
            var aboutRequest = UnityGoogleDrive.GoogleDriveAbout.Get();
            aboutRequest.Fields = new List<string> { "user" };
            await aboutRequest.Send();

            if (string.IsNullOrEmpty(aboutRequest.Error)) {
                UserData = aboutRequest.ResponseData.User;
                
                var folderRequest = GoogleDriveFiles.List();
                folderRequest.Q = $"name='Digidex' and mimeType='{FolderMimeType}'";
                var folderList = await folderRequest.Send();
                if (folderList != null && folderList.Files != null && folderList.Files.Count > 0) {
                    _folderID = folderList.Files[0].Id;
                } else {
                    var folderFile = new UnityGoogleDrive.Data.File {
                        Name = FolderName,
                        MimeType = FolderMimeType
                    };
                    var folder = await GoogleDriveFiles.Create(folderFile).Send();
                    _folderID = folder?.Id;
                }

                var saveFileLocation = await GetSaveMetadata();
                if (saveFileLocation != null) {
                    _fileID = saveFileLocation.Id;
                    var downloadRequest = GoogleDriveFiles.Download(_fileID);
                    var fileData = await downloadRequest.Send();

                    if (fileData.Content != null) {
                        long localModifiedTime = long.Parse(PlayerPrefs.GetString(LastLocalSavePref, "0"));
                        long lastLocalUploadTime = long.Parse(PlayerPrefs.GetString(LastLocalSaveUploadedPref, "0"));
                        if ((_listsDataDict.Count == 0) || saveFileLocation.ModifiedTime.Value.Ticks != localModifiedTime) {
                            if (localModifiedTime > lastLocalUploadTime) {
                                var popup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                                ToggleData keepCopyToggle = new ToggleData { Name = "Keep a copy", IsOn = true };
                                Action keepLocal = async () => {
                                    if (keepCopyToggle.IsOn) {
                                        var copyFile = new UnityGoogleDrive.Data.File {
                                            Name = string.Format(SaveFileCopyName_date, DateTime.Now.ToString()),
                                            Content = fileData.Content,
                                            Parents = new List<string> { _folderID }
                                        };
                                        _ = GoogleDriveFiles.Create(copyFile).Send();
                                    }

                                    string jsonData = PlayerPrefs.GetString(LocalDataPref);
                                    var file = new UnityGoogleDrive.Data.File {
                                        Name = SaveFileName,
                                        Content = Encoding.ASCII.GetBytes(jsonData)
                                    };
                                    loadingWheelHandle.Complete();
                                    loadingScreenHandle = ApplicationManager.Instance.DisplayLoadingScreen();
                                    var updateRequest = UnityGoogleDrive.GoogleDriveFiles.Update(_fileID, file);
                                    updateRequest.Fields = FileFieldsQuery;
                                    file = await updateRequest.Send();
                                    _dataOnLoad = jsonData;
                                    _userConfirmedData = true;
                                    RefreshDataDate(file.ModifiedTime.Value);
                                    _ = PopupManager.Instance.Back();
                                    IsLoggingIn = false;
                                    OnAuthChanged?.Invoke();
                                    loadingScreenHandle.Complete();
                                };
                                Action keepCloud = () => {
                                    if (keepCopyToggle.IsOn) {
                                        string localData = PlayerPrefs.GetString(LocalDataPref);
                                        var file = new UnityGoogleDrive.Data.File {
                                            Name = string.Format(SaveFileCopyName_date, DateTime.Now.ToString()),
                                            Content = Encoding.ASCII.GetBytes(localData),
                                            Parents = new List<string> { _folderID }
                                        };
                                        GoogleDriveFiles.Create(file).Send();
                                    }
                                    string jsonData = Encoding.ASCII.GetString(fileData.Content);
                                    _listsDataDict = ParseFileData(jsonData);
                                    _dataOnLoad = jsonData;
                                    _userConfirmedData = true;
                                    RefreshDataDate(saveFileLocation.ModifiedTime.Value);
                                    _ = PopupManager.Instance.Back();
                                    IsLoggingIn = false;
                                    OnAuthChanged?.Invoke();
                                    loadingWheelHandle.Complete();
                                };

                                List<ButtonData> buttons = new List<ButtonData>(2);
                                buttons.Add(new ButtonData { Text = "Local", Callback = keepLocal });
                                buttons.Add(new ButtonData { Text = "Cloud", Callback = keepCloud });
                                List<ToggleData> toggles = new List<ToggleData>(1);
                                toggles.Add(keepCopyToggle);
                                string msg = "There's a newer version of your data, which one you want to use?";
                                popup.Populate(msg, "Data Conflict", buttonDataList: buttons, toggleDataList: toggles);
                                popup.ShowCloseButton = false;
                            } else {
                                string jsonData = Encoding.ASCII.GetString(fileData.Content);
                                _listsDataDict = ParseFileData(jsonData);
                                _dataOnLoad = jsonData;
                                _userConfirmedData = true;
                                RefreshDataDate(saveFileLocation.ModifiedTime.Value);
                                IsLoggingIn = false;
                                OnAuthChanged?.Invoke();
                                loadingWheelHandle.Complete();
                            }
                        } else {
                            _userConfirmedData = true;
                            IsLoggingIn = false;
                            OnAuthChanged?.Invoke();
                            loadingWheelHandle.Complete();
                        }
                    } else {
                        _userConfirmedData = true;
                        IsLoggingIn = false;
                        loadingWheelHandle.Complete();
                    }
                } else {
                    Debug.LogWarning("File Location not found");
                    _userConfirmedData = true;
                    IsLoggingIn = false;
                    loadingWheelHandle.Complete();
                }
            } else {
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate("Failed to authenticate your google account", "Authentication Fail");
                IsLoggingIn = false;
                loadingWheelHandle.Complete();
            }
        } catch (Exception ex) {
            Debug.LogError($"{ex.Message} \n {ex.StackTrace}");
            IsLoggingIn = false;
            loadingWheelHandle?.Complete();
            loadingScreenHandle?.Complete();
            OnAuthChanged?.Invoke();
        }
    }

    public void LogOut() {
        _driveSettings?.DeleteCachedAuthTokens();
        UserData = null;
        _userConfirmedData = false;
        IsLoggingIn = false;
        OnAuthChanged?.Invoke();
    }

    private async UniTask<UnityGoogleDrive.Data.File> GetSaveMetadata() {
        var filesRequest = GoogleDriveFiles.List();
        filesRequest.Fields = ListFieldsQuery;
        filesRequest.Q = $"name='{SaveFileName}' and '{_folderID}' in parents";
        await filesRequest.Send();
        var saveFileLocation = filesRequest.ResponseData?.Files?.Find(f => f.Name == SaveFileName);
        return saveFileLocation;
    }

    public void Save(string key, string data) {
        if (_listsDataDict.ContainsKey(key)) {
            _listsDataDict[key] = data;
        } else {
            _listsDataDict.Add(key, data);
        }
        SaveAllData();
    }

    public string Load(string key) {
        if (_listsDataDict.ContainsKey(key)) {
            return _listsDataDict[key];
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

        string jsonData = $"DIGIDEX\n{Version}\n{UsingDubKey}: {UsingDub}\n{JsonConvert.SerializeObject(_listsDataDict)}";
        
        if (jsonData != _dataOnLoad) {
            PlayerPrefs.SetString(LocalDataPref, jsonData);
            _dataOnLoad = jsonData;
            long localModifiedTime = long.Parse(PlayerPrefs.GetString(LastLocalSavePref, "0"));
            RefreshDataDate();

            if (IsUserLoggedIn) {
                var loadingWheelHandle = ApplicationManager.Instance.DisplayLoadingWheel();
                var saveMetadata = await GetSaveMetadata();
                bool noRecordConflicts = (saveMetadata == null) ||
                    (saveMetadata.ModifiedTime.Value.Ticks <= localModifiedTime);
                if (IsUserLoggedIn && noRecordConflicts)  {
                    var file = new UnityGoogleDrive.Data.File {
                        Name = SaveFileName,
                        Content = Encoding.ASCII.GetBytes(jsonData)
                    };
                    if (string.IsNullOrEmpty(_fileID)) {
                        file.Parents = new List<string> { _folderID };
                        var createRequest = UnityGoogleDrive.GoogleDriveFiles.Create(file);
                        createRequest.Fields = FileFieldsQuery;
                        file = await createRequest.Send();
                    } else {
                        var updateRequest = UnityGoogleDrive.GoogleDriveFiles.Update(_fileID, file);
                        updateRequest.Fields = FileFieldsQuery;
                        file = await updateRequest.Send();
                    }
                    RefreshDataDate(file.ModifiedTime.Value);
                    PlayerPrefs.SetString(LastLocalSaveUploadedPref, file.ModifiedTime.Value.Ticks.ToString());
                }
                loadingWheelHandle.Complete();
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

    public bool IsValidData(string data, out Database db, out Dictionary<string, HashSet<Hash128>> parsedList) {
        bool isValid = true;
        parsedList = null;
        db = null;
        try {
            KeyValuePair<string, string> parsedData = JsonConvert.DeserializeObject<KeyValuePair<string, string>>(data);
            var dbs = ApplicationManager.Instance.GetDatabases();
            db = dbs.FirstOrDefault(d => d.DataKey == parsedData.Key);
            if (db != default) {
                parsedList = db.ParseListData(parsedData.Value);
            } else {
                isValid = false;
            }
        } catch (Exception ex) {
            isValid = false;
            Debug.LogError($"{ex.Message} - {ex.Message}");
        }

        return isValid;
    }
}
