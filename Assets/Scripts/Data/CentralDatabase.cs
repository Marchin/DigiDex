using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public interface IDataEntry {
    string Name { get; set; }
    string Profile { get; set; }
    AssetReferenceAtlasedSprite Sprite { get; set; }
    Hash128 Hash { get; set; }
    List<InformationData> ExtractInformationData();
}

public interface IEvolvable {
    AssetReferenceEvolutionData EvolutionDataRef { get; set; }
}

public interface IDatabase {
    string DisplayName { get; }
    IEnumerable<IDataEntry> EntryList { get; }
    Dictionary<Hash128, IDataEntry> EntryDict { get; }
    IReadOnlyDictionary<string, HashSet<Hash128>> Lists { get; }
    void AddEntryToList(string list, Hash128 entry);
    void RemoveEntryFromList(string list, Hash128 entry);
    List<FilterData> RetrieveFiltersData();
    List<ToggleActionData> RetrieveTogglesData();
    bool AddList(string name);
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
        MethodInfo method = typeof(ApplicationManager).GetMethod(nameof(ApplicationManager.Instance.GetDatabase), new Type[0]);
        MethodInfo generic = method.MakeGenericMethod(Type.GetType(_typeName));
        IDatabase db = generic.Invoke(ApplicationManager.Instance, null) as IDatabase;
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

public class CentralDatabase : ScriptableObject {
    public const string CentralDBAssetName = "Central Database";
    public DigimonDatabase DigimonDB;

    public List<IDatabase> GetDatabases() {
        List<IDatabase> result = new List<IDatabase>();

        // TODO: See if we can automate this through reflection
        result.Add(DigimonDB);

        return result;
    }
}
