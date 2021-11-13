using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Cysharp.Threading.Tasks;

public class MainMenu : MonoBehaviour {
    [SerializeField] private CanvasScaler _canvasScaler = default;
    [SerializeField] private ButtonElementList _databaseList = default;
    [SerializeField] private Button _loginButton = default;
    [SerializeField] private GameObject _loggingInGO = default;
    [SerializeField] private Button _logoutButton = default;
    [SerializeField] private TextMeshProUGUI _currentMail = default;

    private async void Start() {
        RefreshButtons();
        (_canvasScaler.transform as RectTransform).AdjustToSafeZone();
        PopupManager.Instance.RegisterCanvasScalerForRotationScaling(_canvasScaler);
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
        _logoutButton.onClick.AddListener(() => {
            UserDataManager.Instance.LogOut();
        });

        UserDataManager.Instance.OnAuthChanged += RefreshButtons;

        RefreshButtons();
    }

    private void RefreshButtons() {
        if (UserDataManager.Instance.IsUserLoggedIn) {
            _loginButton.gameObject.SetActive(false);
            _logoutButton.gameObject.SetActive(true);
            _loggingInGO.gameObject.SetActive(false);
            _currentMail.gameObject.SetActive(true);
            _currentMail.text = UserDataManager.Instance.UserData.EmailAddress;
        } else if (UserDataManager.Instance.IsLoggingIn) {
            _loginButton.gameObject.SetActive(false);
            _logoutButton.gameObject.SetActive(false);
            _loggingInGO.gameObject.SetActive(true);
            _currentMail.gameObject.SetActive(false);
        } else {
            _loginButton.gameObject.SetActive(true);
            _logoutButton.gameObject.SetActive(false);
            _loggingInGO.gameObject.SetActive(false);
            _currentMail.gameObject.SetActive(false);
        }
    }
}
