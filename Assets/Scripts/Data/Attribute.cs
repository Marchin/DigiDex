using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
public class AttributeReference {
    public string Name;
    public AssetReferenceAttribute Data;
}

[System.Serializable]
public class AssetReferenceAttribute : AssetReferenceT<Attribute> {
    public AssetReferenceAttribute(string guid) : base(guid) {}
}

public class Attribute : ScriptableObject {
    public string Name;
    public AssetReferenceAtlasedSprite Sprite;
}
