using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Cysharp.Threading.Tasks;

public class MainMenu : MonoBehaviour {
    [SerializeField] private CanvasScaler _canvasScaler = default;
    [SerializeField] private GameObject _content = default;
    [SerializeField] private GameObject _loadingWheel = default;
    [SerializeField] private ButtonElementList _databaseList = default;
    [SerializeField] private Button _loginButton = default;
    [SerializeField] private Button _settingsButton = default;
    [SerializeField] private Button _twitterButton = default;
    [SerializeField] private Button _igButton = default;
    [SerializeField] private GameObject _loggingInGO = default;

    private async void Start() {
        RefreshButtons();
        (_content.transform as RectTransform).AdjustToSafeZone();
        PopupManager.Instance.OnRotation += () => (_content.transform as RectTransform).AdjustToSafeZone();
        PopupManager.Instance.RegisterCanvasScalerForRotationScaling(_canvasScaler);
        _loadingWheel.SetActive(true);
        await UniTask.WaitUntil(() => ApplicationManager.Instance.Initialized);
        
        var buttonDataList = ApplicationManager.Instance.GetDatabases()
            .Select(db => new ButtonData {
                Text = db.DisplayName,
                Callback = () => {
                    PopupManager.Instance.GetOrLoadPopup<DatabaseViewPopup>().
                        ContinueWith(popup => popup.Populate(db));
                }
            });
        _databaseList.Populate(buttonDataList);
        
        _loginButton.onClick.AddListener(async () => {
            await UserDataManager.Instance.Login();
        });

        _settingsButton.onClick.AddListener(async () => {
            List<ButtonData> buttonList = new List<ButtonData>();

            buttonList.Add(new ButtonData { Text = "Donate", Callback = async () => {
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate(
                    "Leave us a tip through:\n" +
                    $"·<color={UnityUtils.LinkColor}><link=\"https://www.paypal.com/paypalme/DigidexApp\">Paypal</link></color>\n" +
                    $"·<color={UnityUtils.LinkColor}><link=\"https://cafecito.app/digidex\">Mercado Pago</link></color>\n\n" + 
                    $"Support us on <color={UnityUtils.LinkColor}><link=\"https://www.patreon.com/Digidex\">Patreon</link></color>");
            }});

            if (UserDataManager.Instance.IsUserLoggedIn) {
                buttonList.Add(new ButtonData { Text = "Logout", Callback = () => UserDataManager.Instance.LogOut() });
            }
            
            buttonList.Add(new ButtonData { Text = "About", Callback = async () => {
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate("Digidex doesn't claim ownership of the images, nor the data");
            }});


            var settingsPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
            settingsPopup.Populate(
                title: "Settings", 
                buttonDataList: buttonList,
                columns: 1
            );
        });

        _twitterButton.onClick.AddListener(() => {
            Application.OpenURL("https://twitter.com/DigiDexApp");
        });

        _igButton.onClick.AddListener(() => {
            Application.OpenURL("https://www.instagram.com/digidex_app/");
        });

        UserDataManager.Instance.OnAuthChanged += RefreshButtons;
        
        _loadingWheel.SetActive(false);

        RefreshButtons();
    }

    private void RefreshButtons() {
        if (UserDataManager.Instance.IsUserLoggedIn) {
            _loginButton.gameObject.SetActive(false);
            _loggingInGO.gameObject.SetActive(false);
        } else if (UserDataManager.Instance.IsLoggingIn) {
            _loginButton.gameObject.SetActive(false);
        } else {
            _loginButton.gameObject.SetActive(true);
            _loggingInGO.gameObject.SetActive(false);
        }
    }
}
