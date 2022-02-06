using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PerformanceManager : MonoBehaviourSingleton<PerformanceManager> {
    private const int EfficiencyFrameRate = 30;
    // HACK: if we use 60 some devices go only up to 45, using 61 works as intended
    private const int HighPerformanceFrameRate = 61;
    public OperationBySubscription HighPerformance { get; private set; }

    private void Awake() {
        Application.targetFrameRate = EfficiencyFrameRate;
        HighPerformance = new OperationBySubscription(
            onStart: () => Application.targetFrameRate = HighPerformanceFrameRate,
            onAllFinished: () => Application.targetFrameRate = EfficiencyFrameRate
        );
    }
}
