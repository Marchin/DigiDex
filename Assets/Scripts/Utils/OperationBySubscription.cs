using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class OperationBySubscription  {
    public class Subscription {
        public bool HasFinished { get; private set; }
        public void Finish() => HasFinished = true;
    }

    private Action onStart;
    private Action onAllFinished;
    private List<Subscription> handles = new List<Subscription>();
    public bool IsRunning { get; private set; }

    public OperationBySubscription(Action onStart, Action onAllFinished) {
        this.onStart = onStart;
        this.onAllFinished = onAllFinished;
    }

    public Subscription Subscribe() {
        Subscription handle = new Subscription();
        handles.Add(handle);

        if (handles.Count == 1) {
            WaitUntilAllAreFinished();
        }

        return handle;
    }

    private async void WaitUntilAllAreFinished() {
        onStart?.Invoke();
        IsRunning = true;
        await UniTask.WaitUntil(() => handles.TrueForAll(h => h.HasFinished));
        onAllFinished?.Invoke();
        IsRunning = false;
        handles.Clear();
    }
}
