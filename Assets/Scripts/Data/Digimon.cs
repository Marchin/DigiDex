using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

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
    public List<int> AttributeIDs;
    public List<int> FieldIDs;
    public List<int> TypeIDs;
    public List<int> LevelIDs;
    public List<int> PreEvolutionIDs;
    public List<int> EvolutionIDs;
    public string LinkSubFix;
}