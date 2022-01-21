using UnityEngine;
using System.Collections.Generic;

public class Level : ScriptableObject {
    public string Name;
    public string DubName;
    public string DisplayName => (UserDataManager.Instance.UsingDub && !string.IsNullOrEmpty(DubName)) ? DubName : Name;
}