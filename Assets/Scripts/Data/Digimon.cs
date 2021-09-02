using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class AssetReferenceDigimon : AssetReferenceT<Digimon> {
    public AssetReferenceDigimon(string guid) : base(guid) {}
}

public class Digimon : ScriptableObject {
    public string Name;
    public string ProfileData;
    public AssetReferenceAtlasedSprite Image;
    public string LinkSubFix;
}