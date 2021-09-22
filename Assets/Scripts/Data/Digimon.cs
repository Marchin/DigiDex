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

public class Digimon : ScriptableObject, IDataObject {
    public string Name;
    public string ProfileData;
    public AssetReferenceAtlasedSprite Sprite;
    public List<int> AttributeIDs;
    public List<int> FieldIDs;
    public List<int> TypeIDs;
    public List<int> LevelIDs;
    public AssetReferenceEvolutionData EvolutionData;
    public Hash128 Hash;
#if UNITY_EDITOR
    public string LinkSubFix;
#endif

    public List<InformationData> ExtractInformationData(CentralDatabase centralDB) {
        List<InformationData> information = new List<InformationData>();
        
        DigimonDatabase digimonDB = centralDB.DigimonDB;
        information.Add(new InformationData { Prefix = "Name", Content = Name });

        if (LevelIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Level" });
            for (int iLevel = 0; iLevel < LevelIDs.Count; ++iLevel) {
                information.Add(new InformationData { Content = digimonDB.Levels[LevelIDs[iLevel]].Name, IndentLevel = 1 });
            }
        }

        if (AttributeIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Attribute" });
            for (int iAttribute = 0; iAttribute < AttributeIDs.Count; ++iAttribute) {
                information.Add(new InformationData { Content = digimonDB.Attributes[AttributeIDs[iAttribute]].Name, IndentLevel = 1 });
            }
        }

        if (TypeIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Type" });
            for (int iType = 0; iType < TypeIDs.Count; ++iType) {
                information.Add(new InformationData { Content = digimonDB.Types[TypeIDs[iType]].Name, IndentLevel = 1 });
            }
        }

        if (FieldIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Field" });
            for (int iField = 0; iField < FieldIDs.Count; ++iField) {
                information.Add(new InformationData { Content = digimonDB.Fields[FieldIDs[iField]].Name, SpriteReference = digimonDB.Fields[FieldIDs[iField]].Sprite, IndentLevel = 1 });
            }
        }

        return information;
    }
}