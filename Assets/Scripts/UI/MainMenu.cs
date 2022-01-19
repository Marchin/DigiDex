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
    [SerializeField] private Button _discordButton = default;
    [SerializeField] private GameObject _loggingInGO = default;

    private async void Start() {
        RefreshButtons();
        PopupManager.Instance.OnRotation += () => (_content.transform as RectTransform).AdjustToSafeZone();
        PopupManager.Instance.RegisterCanvasScalerForRotationScaling(_canvasScaler);
        (_content.transform as RectTransform).AdjustToSafeZone();
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
            List<ButtonData> buttonList = new List<ButtonData>() {
                new ButtonData("Log In", async () => {
                    _ = PopupManager.Instance.Back();
                    await UserDataManager.Instance.Login();
                })
            };
            var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
            msgPopup.Populate("You can log in with your drive account to sync your lists across devices\n\n" +
                "Digidex is a fan-made app as such is not authorized by google and you will get some warnings, Digidex is open source however and you " +
                $"can review our code <color={UnityUtils.LinkColor}><link=\"https://github.com/Marchin/DigiDex\">here</link></color> to verify we aren't doing anything fishy",
                "Log In",
                buttonDataList: buttonList);
        });

        _settingsButton.onClick.AddListener(async () => {
            List<ButtonData> buttonList = new List<ButtonData>();

            buttonList.Add(new ButtonData { Text = "Donate", Callback = async () => {
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate(
                    "Leave us a tip through:\n" +
                    $"·<color={UnityUtils.LinkColor}><link=\"https://www.paypal.com/paypalme/DigidexApp\">Paypal</link></color>\n" +
                    $"·<color={UnityUtils.LinkColor}><link=\"https://cafecito.app/digidex\">Mercado Pago</link></color>\n\n" + 
                    $"Support us on <color={UnityUtils.LinkColor}><link=\"https://www.patreon.com/Digidex\">Patreon</link></color>",
                    "Support Us");
            }});

            if (UserDataManager.Instance.IsUserLoggedIn) {
                buttonList.Add(new ButtonData { Text = "Logout", Callback = async () => {
                        var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>(restore: false);
                        List<ButtonData> buttonList = new List<ButtonData>() {
                            new ButtonData("No", () => {
                                _ = PopupManager.Instance.Back();
                            }),
                            
                            new ButtonData("Yes", () => {
                                UserDataManager.Instance.LogOut();
                                msgPopup.Populate("Logged out");
                            })
                        };

                        msgPopup.Populate(
                            "Are you sure you want to log out?",
                            "Log Out",
                            buttonDataList: buttonList);
                    }
                });
            }
            
            buttonList.Add(new ButtonData { Text = "About", Callback = async () => {
                var msgPopup = await PopupManager.Instance.GetOrLoadPopup<MessagePopup>();
                msgPopup.Populate("DigiDex is a database for all things digimon.\n\nDigiDex doesn't claim ownership of the images nor the data", "About");
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

        _discordButton.onClick.AddListener(() => {
            Application.OpenURL("https://discord.gg/mfH42AMdj8");
        });

        UserDataManager.Instance.OnAuthChanged += RefreshButtons;
        PopupManager.Instance.OnStackChange += RefreshButtons;
        
        _loadingWheel.SetActive(false);

        RefreshButtons();
    }

    private void RefreshButtons() {
        if (UserDataManager.Instance.IsUserLoggedIn) {
            _loginButton.gameObject.SetActive(false);
            _loggingInGO.gameObject.SetActive(false);
        } else if (UserDataManager.Instance.IsLoggingIn) {
            _loginButton.gameObject.SetActive(false);
            _loggingInGO.gameObject.SetActive(true);
        } else {
            _loginButton.gameObject.SetActive(true);
            _loggingInGO.gameObject.SetActive(false);
        }
    }
}
