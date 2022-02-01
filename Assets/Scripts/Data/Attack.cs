using System.Collections.Generic;

[System.Serializable]
public class Attack {
    public string Name;
    public List<string> DubNames;
    public string Description;
    public string DisplayName => (UserDataManager.Instance.UsingDub && DubNames.Count > 0) ? DubNames[0] : Name;
}
