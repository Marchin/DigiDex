using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
public class FieldReference {
    public int ID;
    public AssetReferenceField Data;
}

[System.Serializable]
public class AssetReferenceField : AssetReferenceT<Field> {
    public AssetReferenceField(string guid) : base(guid) {}
}

public class Field : ScriptableObject {
    public string Name;
    public string Description;
    public AssetReferenceAtlasedSprite Sprite;
}
