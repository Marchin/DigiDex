using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PerformanceManager : MonoBehaviourSingleton<PerformanceManager> {
    private const int EfficiencyFrameRate = 30;
    // HACK: if we use 60 some devices go only up to 45, using 61 works as intended
    private const int HighPerformanceFrameRate = 61;
    private List<Handle> _maxFPSHandles = new List<Handle>();

    private void Start() {
        Application.targetFrameRate = EfficiencyFrameRate;
    }

    public Handle RequestHighPerformance() {
        Handle handle = new Handle();
        _maxFPSHandles.Add(handle);

        if (_maxFPSHandles.Count == 1) {
            ReturnToTargetFrameRate();
        }

        return handle;
    }

    
    private async void ReturnToTargetFrameRate() {
        Application.targetFrameRate = HighPerformanceFrameRate;
        await UniTask.WaitUntil(() => _maxFPSHandles.TrueForAll(h => h.IsComplete));
        Application.targetFrameRate = EfficiencyFrameRate;
        _maxFPSHandles.Clear();
    }
}
