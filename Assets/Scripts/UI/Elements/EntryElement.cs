using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;

public class EntryElement : MonoBehaviour, IDataUIElement<IDataEntry> {
    [SerializeField] private Image _entryImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    private AsyncOperationHandle<Sprite> _spriteLoading;
    private CancellationTokenSource _cts;

    public void Populate(IDataEntry data) {
        if (_cts != null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();

        _nameText.text = data.DisplayName;
        _spriteLoading = UnityUtils.LoadSprite(_entryImage, data.Sprite, _cts.Token);
    }
}