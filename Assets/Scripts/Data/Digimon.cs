using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

[System.Serializable]
public class DigimonReference {
    public string Name;
    public AssetReferenceDigimon Data;
}

[System.Serializable]
public class AssetReferenceDigimon : AssetReferenceT<Digimon> {
    public AssetReferenceDigimon(string guid) : base(guid) {}
}

public class Digimon : ScriptableObject, IDataEntry, IEvolvable {
    [SerializeField] private string _name;
    [SerializeField] private List<string> _dubNames;
    [FormerlySerializedAs("ProfileData")]
    [SerializeField] private string _profile;
    [FormerlySerializedAs("EvolutionData")]
    [SerializeField] private AssetReferenceEvolutionData _evolutionDataRef;
    [SerializeField] private AssetReferenceAtlasedSprite _sprite;
    [SerializeField] private Hash128 _hash;
    public string Name {
        get => _name;
        set => _name = value;
    }
    public List<string> DubNames {
        get => _dubNames;
        set => _dubNames = value;
    }
    public string Profile {
        get => string.IsNullOrEmpty(_profile) ?
            "No profile available." :
            _profile;
        set => _profile = value;
    }
    public AssetReferenceAtlasedSprite Sprite {
        get {
            if (_sprite.RuntimeKeyIsValid()) {
                return _sprite;
            } else {
                return ApplicationManager.Instance.MissingSpirte;
            }
        }
        set => _sprite = value;
    }
    public AssetReferenceEvolutionData EvolutionDataRef {
        get => _evolutionDataRef;
        set => _evolutionDataRef = value;
    }
    public Hash128 Hash  {
        get => _hash;
        set => _hash = value;
    }
    public int DebutYear;
    public List<int> AttributeIDs;
    public List<int> FieldIDs;
    public List<int> TypeIDs;
    public List<int> LevelIDs;
    public List<int> GroupIDs;
    public AssetReferenceEvolutionData EvolutionData;
#if UNITY_EDITOR
    public string LinkSubFix;
#endif

    public List<InformationData> ExtractInformationData() {
        List<InformationData> information = new List<InformationData>();
        
        DigimonDatabase digimonDB = ApplicationManager.Instance.GetDatabase(this) as DigimonDatabase;
        information.Add(new InformationData { Prefix = "Name", Content = Name });

        if (DubNames.Count > 0) {
            information.Add(new InformationData { Prefix = "Dub Names" });
            for (int iDubName = 0; iDubName < DubNames.Count; ++iDubName) {
                information.Add(new InformationData { Content = DubNames[iDubName], IndentLevel = 1 });
            }
        }

        if (DebutYear > 0) {
            information.Add(new InformationData { Prefix = "Debut", Content = DebutYear.ToString() });
        }

        if (LevelIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Levels" });
            for (int iLevel = 0; iLevel < LevelIDs.Count; ++iLevel) {
                information.Add(new InformationData { Content = digimonDB.Levels[LevelIDs[iLevel]].Name, IndentLevel = 1 });
            }
        }

        if (AttributeIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Attributes" });
            for (int iAttribute = 0; iAttribute < AttributeIDs.Count; ++iAttribute) {
                information.Add(new InformationData { Content = digimonDB.Attributes[AttributeIDs[iAttribute]].Name, IndentLevel = 1 });
            }
        }

        if (TypeIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Types" });
            for (int iType = 0; iType < TypeIDs.Count; ++iType) {
                information.Add(new InformationData { Content = digimonDB.Types[TypeIDs[iType]].Name, IndentLevel = 1 });
            }
        }

        if (GroupIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Groups" });
            for (int iGroup = 0; iGroup < GroupIDs.Count; ++iGroup) {
                DigimonGroup group = digimonDB.Groups[GroupIDs[iGroup]];
                Action onMoreInfo = () => PopupManager.Instance.GetOrLoadPopup<MessagePopup>()
                    .ContinueWith(popup => popup.Populate(group.Description, group.Name))
                    .Forget();
                information.Add(new InformationData {
                    Content = digimonDB.Groups[GroupIDs[iGroup]].Name,
                    IndentLevel = 1,
                    OnMoreInfo = onMoreInfo
                });
            }
        }

        if (FieldIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Fields" });
            for (int iField = 0; iField < FieldIDs.Count; ++iField) {
                Field field = digimonDB.Fields[FieldIDs[iField]];
                Action onMoreInfo = () => PopupManager.Instance.GetOrLoadPopup<MessagePopup>()
                    .ContinueWith(popup => popup.Populate(field.Description, field.Name, field.Sprite))
                    .Forget();
                information.Add(
                    new InformationData {
                        Content = field.Name,
                        SpriteReference = field.Sprite,
                        OnMoreInfo = onMoreInfo,
                        IndentLevel = 1
                    }
                );
            }
        }

        return information;
    }
}