using UnityEngine;
using System;
using System.Linq;
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
    IEnumerable<IDataEntry> EntryList { get; }
    HashSet<Hash128> Favorites { get; }
    Dictionary<string, FilterData> RetrieveFiltersData();
    Dictionary<string, ToggleFilterData> RetrieveTogglesData();
}

[Serializable]
public class EntryIndex : IEquatable<EntryIndex> {
    [SerializeField] private string _typeName = default;
    public int Index;

    public EntryIndex(Type type, int index) {
        Debug.Assert(type.GetInterface(nameof(IDataEntry)) != null, $"Invalid type {type}");
        _typeName = type.ToString();
        Index = index;
    }

    public IDataEntry FetchEntryData() {
        IDataEntry result = ApplicationManager.Instance.GetDatabase(Type.GetType(_typeName))?.EntryList.ElementAt(Index);

        return result;
    }

    public bool Equals(EntryIndex other) {
        bool areEqual = this._typeName == other._typeName &&
            this.Index == other.Index;

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
        return _typeName.GetHashCode() * Index.GetHashCode();
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
    public DigimonDatabase DigimonDB;
}
