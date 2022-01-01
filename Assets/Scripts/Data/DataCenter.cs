using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Reflection;
using System.Collections.Generic;

public interface IDataEntry {
    string Name { get; set; }
    List<string> DubNames { get; set; }
    string Profile { get; set; }
    AssetReferenceAtlasedSprite Sprite { get; set; }
    Hash128 Hash { get; set; }
    List<InformationData> ExtractInformationData();
#if UNITY_EDITOR
    string LinkSubFix { get; set; }
#endif
}

public interface IEvolvable : IDataEntry {
    AssetReferenceEvolutionData EvolutionDataRef { get; set; }
}

[Serializable]
public class EntryIndex : IEquatable<EntryIndex> {
    [SerializeField] private string _typeName = default;
    public Hash128 Hash;

    public EntryIndex(Type type, Hash128 hash) {
        Debug.Assert(type.GetInterface(nameof(IDataEntry)) != null, $"Invalid type {type}");
        _typeName = type.FullName;
        Hash = hash;
    }

    public IDataEntry FetchEntryData() {
        MethodInfo method = typeof(ApplicationManager).GetMethod(nameof(ApplicationManager.Instance.GetDatabase),
            new Type[0]);
        MethodInfo generic = method.MakeGenericMethod(Type.GetType(_typeName));
        Database db = generic.Invoke(ApplicationManager.Instance, null) as Database;
        IDataEntry result = db?.EntryDict[Hash];

        return result;
    }

    public bool Equals(EntryIndex other) {
        bool areEqual = this._typeName == other._typeName &&
            this.Hash == other.Hash;

        return areEqual;
    }

    public override bool Equals(object other) {
        //Check whether the compared object is null.
        if (System.Object.ReferenceEquals(other, null)) return false;

        //Check whether the compared object references the same data.
        if (System.Object.ReferenceEquals(this, other)) return true;

        return this.Equals(other as Evolution);
    }

    public override int GetHashCode() {
        return _typeName.GetHashCode() * Hash.GetHashCode();
    }

    public static bool operator ==(EntryIndex entry1, EntryIndex entry2) {
      if (((object)entry1) == null || ((object)entry2) == null) {
        return System.Object.Equals(entry1, entry2);
      }

      return entry1.Equals(entry2);
   }

   public static bool operator !=(EntryIndex entry1, EntryIndex entry2) {
      if (((object)entry1) == null || ((object)entry2) == null) {
        return !System.Object.Equals(entry1, entry2);
      }

      return !(entry1.Equals(entry2));
   }
}

public class DataCenter : ScriptableObject {
    public const string DataCenterAssetName = "Central Database";
    public DigimonDatabase DigimonDB;
    public AppmonDatabase AppmonDB;

    public List<Database> GetDatabases() {
        List<Database> result = new List<Database>();

        // TODO: See if we can automate this through reflection
        result.Add(DigimonDB);
        result.Add(AppmonDB);

        return result;
    }
}
