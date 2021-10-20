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
    private const string LocalDataPref = "local_data";
    private const string LastLocalSavePref = "last_local_save";
    private Dictionary<string, string> _dataDict;
    public event Action OnBeforeSave;
    private string _fileID;
    private GoogleDriveSettings _driveSettings;
    public User UserData { get; private set; }
    public bool IsUserCached => _driveSettings.IsAnyAuthTokenCached();
    public bool IsUserLoggedIn => UserData != null;
    
    private void Awake() {
        _driveSettings = GoogleDriveSettings.LoadFromResources();
        string localData = PlayerPrefs.GetString(LocalDataPref);
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
        GoogleDriveAbout.GetRequest aboutRequest = null;
        GoogleDriveFiles.ListRequest filesRequest = null;
        try {
            aboutRequest = UnityGoogleDrive.GoogleDriveAbout.Get();
            aboutRequest.Fields = new List<string> { "user" };
            await aboutRequest.Send();

            if (string.IsNullOrEmpty(aboutRequest.Error)) {
                UserData = aboutRequest.ResponseData.User;
                filesRequest = GoogleDriveFiles.List();
                await filesRequest.Send();
                var saveFileLocation = filesRequest.ResponseData.Files.Find(f => f.Name == SaveFileName);
                if (saveFileLocation != null) {
                    _fileID = saveFileLocation.Id;
                    var fileData = await GoogleDriveFiles.Download(_fileID).Send();
                    if (fileData.Content != null) {
                        string jsonData = Encoding.ASCII.GetString(fileData.Content);
                        _dataDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                        Debug.Log("Data Loaded");
                    } else {
                        Debug.Log("Content Empty");
                    }
                } else {
                    Debug.LogError("File Location not found");
                }
                filesRequest.Dispose();
            } else {
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate("Failed to authenticate your google account", "Authentication Fail");
            }
        } catch (Exception ex) {
            Debug.LogError($"{ex.Message} \n {ex.StackTrace}");
        } finally {
            handle.Complete();
            aboutRequest?.Dispose();
            filesRequest?.Dispose();
        }
    }

    public void LogOut() {
        _driveSettings?.DeleteCachedAuthTokens();
        UserData = null;
    }

    public void Save(string key, string data) {
        if (_dataDict.ContainsKey(key)) {
            _dataDict[key] = data;
        } else {
            _dataDict.Add(key, data);
        }
    }

    public string Load(string key) {
        if (_dataDict.ContainsKey(key)) {
            return _dataDict[key];
        } else {
            return "";
        }
    }

    public void SaveAllData() {
        OnBeforeSave?.Invoke();

        string jsonData = JsonConvert.SerializeObject(_dataDict);
        PlayerPrefs.SetString(LocalDataPref, jsonData);

        if (IsUserLoggedIn) {
            var file = new UnityGoogleDrive.Data.File {
                Name = SaveFileName,
                Content = Encoding.ASCII.GetBytes(jsonData)
            };
            if (string.IsNullOrEmpty(_fileID)) {
                UnityGoogleDrive.GoogleDriveFiles.Create(file).Send();
            } else {
                UnityGoogleDrive.GoogleDriveFiles.Update(_fileID, file).Send();
            }
        }

        PlayerPrefs.SetString(LastLocalSavePref, DateTime.Now.ToString());
    }
}
