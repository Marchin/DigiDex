using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine;

[System.Serializable]
public class CharacterReference {
    public string Name;
    public EntryIndex Index;
}

[System.Serializable]
public class CharactersSet {
    public string Name;
    public List<CharacterReference> CharacterRefs;
}

public class Character : ScriptableObject, IDataEntry {
    [SerializeField] private string _name = default;
    [SerializeField] private List<string> _dubNames = default;
    [SerializeField] private string _profile = default;
    [SerializeField] private AssetReferenceAtlasedSprite _sprite = default;
    [SerializeField] private Hash128 _hash = default;
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
    public Hash128 Hash  {
        get => _hash;
        set => _hash = value;
    }
    
#if UNITY_EDITOR
    [SerializeField] private string _linkSubfix = default;
    public string LinkSubFix {
        get => _linkSubfix;
        set => _linkSubfix = value;
    }
#endif

    public List<InformationData> ExtractInformationData() {
        throw new System.NotImplementedException();
    }
}
