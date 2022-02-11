using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

[System.Serializable]
public class AppData {
    public string Name;
    public AssetReferenceAtlasedSprite Sprite;
}

public class Appmon : ScriptableObject, IEvolvable {
    [SerializeField] private string _name;
    [SerializeField] private List<string> _dubNames;
    [SerializeField] private string _profile;
    [SerializeField] private AssetReferenceEvolutionData _evolutionDataRef;
    [SerializeField] private AssetReferenceAtlasedSprite _sprite;
    [SerializeField] private Hash128 _hash;
    [SerializeField] private string _linkSubFix;
    public string Name {
        get => _name;
        set => _name = value;
    }
    public List<string> DubNames {
        get => _dubNames;
        set => _dubNames = value;
    }
    public string DisplayName => (UserDataManager.Instance.UsingDub && DubNames.Count > 0) ? DubNames[0] : Name;
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
    public AppData App;
    public List<int> GradeIDs;
    public List<int> TypeIDs;
    public List<int> Powers;
    public List<Attack> Attacks;
    public int DebutYear;
#if UNITY_EDITOR
    public string LinkSubFix { get => _linkSubFix; set => _linkSubFix = value; }
#endif

    public List<InformationData> ExtractInformationData() {
        List<InformationData> information = new List<InformationData>();

        AppmonDatabase appmonDB = ApplicationManager.Instance.GetDatabase(this) as AppmonDatabase;

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
        
        if (App != null) {
            information.Add(new InformationData { Prefix = "App" });
            information.Add(new InformationData {
                Content = App.Name,
                SpriteReference = App.Sprite,
                IndentLevel = 1,
            });
        }
     
        if (TypeIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Type" });
            for (int iType = 0; iType < TypeIDs.Count; ++iType) {
                AppmonType type = appmonDB.Types[TypeIDs[iType]];
                information.Add(new InformationData {
                    Content = type.Name,
                    SpriteReference = type.Sprite, 
                    IndentLevel = 1
                });
            }
        }
   
        if (GradeIDs.Count > 0) {
            information.Add(new InformationData { Prefix = "Grade" });
            for (int iGrade = 0; iGrade < GradeIDs.Count; ++iGrade) {
                information.Add(new InformationData { Content = appmonDB.Grades[GradeIDs[iGrade]].Name, IndentLevel = 1 });
            }
        }

        if (Powers.Count > 0) {
            information.Add(new InformationData { Prefix = "Power" });
            foreach (int power in Powers) {
                information.Add(new InformationData { Content = power.ToString(), IndentLevel = 1 });
            }
        }

        if (Attacks.Count > 0) {
            information.Add(new InformationData { Prefix = "Attacks" });
            for (int iAttack = 0; iAttack < Attacks.Count; ++iAttack) {
                Attack attack = Attacks[iAttack];
                string displayName = attack.DisplayName;
                string message = attack.Description;
                if (attack.DubNames.Count > 0) {
                    if (!string.IsNullOrEmpty(message)) {
                        message += "\n\n";
                    }
                    message += "Other Names:";
                    if (UserDataManager.Instance.UsingDub) {
                        message += $"\n·{attack.Name}";
                        displayName += $" ({attack.Name})";
                    }

                    for (int iName = (UserDataManager.Instance.UsingDub ? 1 : 0); iName < attack.DubNames.Count; ++iName) {
                        message += $"\n·{attack.DubNames[iName]}";
                    }
                }
                bool hasInfo = !string.IsNullOrEmpty(attack.Description) || (attack.DubNames.Count > 0);
                Action onMoreInfo = hasInfo ?
                    () => PopupManager.Instance.GetOrLoadPopup<MessagePopup>(restore: false)
                        .ContinueWith(popup => popup.Populate(message, attack.DisplayName))
                        .Forget() :
                    (Action)null;
                information.Add(
                    new InformationData {
                        Content = displayName,
                        OnMoreInfo = onMoreInfo,
                        IndentLevel = 1
                    }
                );
            }
        }
        
        return information;
    }
}
