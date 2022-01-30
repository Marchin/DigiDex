using TMPro;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

public class EvolutionDetailsPopup : Popup {
    public class PopupData {
        public IDataEntry Source;
        public Evolution EvolutionInfo;
        public IDataEntry SelectedEntry;
        public bool IsPreEvolution;
        public float Scroll;
    }

    [SerializeField] private TextMeshProUGUI _selectedName = default;
    [SerializeField] private Image _selectedImage = default;
    [SerializeField] private Button _inspectButton = default;
    [SerializeField] private Image _fromImage = default;
    [SerializeField] private Image _toImage = default;
    [SerializeField] private Button _fromButton = default;
    [SerializeField] private Button _toButton = default;
    [SerializeField] private ColorPlusTextList _typesList = default;
    [SerializeField] private InformationElementList _infoList = default;
    [SerializeField] private Button _close = default;
    [SerializeField] private ScrollRect _scroll = default;
    private List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
    private CancellationTokenSource _cts;
    private PopupData _popupData;

    private void Awake() {
        _close.onClick.AddListener(() => _ = PopupManager.Instance.Back());
    }

    public void Populate(IDataEntry source, Evolution evolutionInfo, bool isPreEvolution) {
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        _popupData = new PopupData {
            Source = source,
            EvolutionInfo = evolutionInfo,
            IsPreEvolution = isPreEvolution
        };

        IDataEntry preEvoData = isPreEvolution ? evolutionInfo.Entry.FetchEntryData() : source;
        IDataEntry evoData = isPreEvolution ? source : evolutionInfo.Entry.FetchEntryData();

        _fromButton.onClick.AddListener(() => SelectEntry(preEvoData));
        var handle = _fromImage.LoadSprite(preEvoData.Sprite, _cts.Token);
        _handles.Add(handle);

        _toButton.onClick.AddListener(() => SelectEntry(evoData));
        handle = _toImage.LoadSprite(evoData.Sprite, _cts.Token);
        _handles.Add(handle);

        _typesList.Populate(Evolution.GetEvolutionColorsPlusText(evolutionInfo.Types));

        List<InformationData> information = new List<InformationData>(evolutionInfo.FusionEntries.Length);
        for (int iEntry = 0; iEntry < evolutionInfo.FusionEntries.Length; ++iEntry) {
            IDataEntry entryData = evolutionInfo.FusionEntries[iEntry].FetchEntryData();
            InformationData data = new InformationData {
                Content = entryData.DisplayName,
                SpriteReference = entryData.Sprite,
                OnMoreInfo = () => SelectEntry(entryData)
            };

            information.Add(data);
        }
        _infoList.Populate(information);

        SelectEntry(evoData);
    }

    private void SelectEntry(IDataEntry dataEntry) {
        var handle = _selectedImage.LoadSprite(dataEntry.Sprite, _cts.Token);
        _handles.Add(handle);

        _selectedName.text = dataEntry.DisplayName;

        _popupData.SelectedEntry = dataEntry;

        _inspectButton.onClick.RemoveAllListeners();
        _inspectButton.onClick.AddListener(() => {
            PopupManager.Instance.GetOrLoadPopup<EntryViewPopup>(true).ContinueWith(popup => {
                popup.Initialize(null, null);
                popup.Populate(dataEntry);
            });
        });
    }

    public override object GetRestorationData() {
        _popupData.Scroll = _scroll.verticalNormalizedPosition;
        return _popupData;
    }

    public override void Restore(object data) {
        if (data is PopupData popupData) {
            Populate(popupData.Source, popupData.EvolutionInfo, popupData.IsPreEvolution);
            SelectEntry(popupData.SelectedEntry);
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = popupData.Scroll;
        }
    }

    public override void OnClose() {
        foreach (var handle in _handles) {
            Addressables.Release(handle);
        }
        _handles.Clear();
    }
}
