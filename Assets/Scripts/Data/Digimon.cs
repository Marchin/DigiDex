using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
public class DigimonReference {
    public string Name;
    public AssetReferenceDigimon Data;
}

[System.Serializable]
public class AssetReferenceDigimon : AssetReferenceT<Digimon> {
    public AssetReferenceDigimon(string guid) : base(guid) {}
}

public class Digimon : ScriptableObject {
    public string Name;
    public string ProfileData;
    public AssetReferenceAtlasedSprite Sprite;
    public string LinkSubFix;
}