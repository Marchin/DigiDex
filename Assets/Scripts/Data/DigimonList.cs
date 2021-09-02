using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DigimonReference {
    public string Name;
    public AssetReferenceDigimon Data;
}

public class DigimonList : ScriptableObject {
    public List<DigimonReference> Digimons;
}
