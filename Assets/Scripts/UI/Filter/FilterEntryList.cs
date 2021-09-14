using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.EventSystems;

public class FilterEntryList : DataList<FilterEntryElement, FilterEntryData>, IPointerEnterHandler, IPointerExitHandler {
    public UnityEngine.Object LastCaller;
    private CancellationTokenSource _cts = new CancellationTokenSource();

    public void OnPointerEnter(PointerEventData eventData) {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }

    public async void OnPointerExit(PointerEventData eventData) {
        try {
            await UniTask.Delay(400, cancellationToken: _cts.Token);
            gameObject.SetActive(false);
        } catch {
        }
    }
}
