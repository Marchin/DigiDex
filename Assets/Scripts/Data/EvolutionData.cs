using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.AddressableAssets;

[Flags]
public enum EvolutionType {
    Regular = 0,
    Main    = 1 << 0,
    Warp    = 1 << 1,
    Side    = 1 << 2,
    Fusion  = 1 << 3, // This includes DNA, Joggress, etc
    Armor   = 1 << 4,
    Spirit  = 1 << 5,

    // TODO: Matrix or just Fusion with human?
}

[Serializable]
public class AssetReferenceEvolutionData : AssetReferenceT<EvolutionData> {
    public AssetReferenceEvolutionData(string guid) : base(guid) {}
}

[Serializable]
public class EvolutionData : ScriptableObject {
    public List<Evolution> PreEvolutions;
    public List<Evolution> Evolutions;
}

[Serializable]
public class Evolution : IEquatable<Evolution> {
    public EntryIndex Entry;
    [FormerlySerializedAs("Type")]
    public EvolutionType Types;
    public EntryIndex[] FusionEntries;
#if UNITY_EDITOR
    public string DebugName;
#endif

    public bool Equals(Evolution other) {
        bool areEqual = this.Entry == other.Entry &&
            this.Types == other.Types &&
            ((this.FusionEntries == null && other.FusionEntries == null) ||
                (this.FusionEntries != null && other.FusionEntries != null &&
                    this.FusionEntries.Length == other.FusionEntries.Length &&
                    this.FusionEntries.Except(other.FusionEntries).Count() == 0 &&
                    other.FusionEntries.Except(this.FusionEntries).Count() == 0));

        return areEqual;
    }

    public override bool Equals(object other) {
        //Check whether the compared object is null.
        if (System.Object.ReferenceEquals(other, null))return false;

        //Check whether the compared object references the same data.
        if (System.Object.ReferenceEquals(this, other))return true;

        return this.Equals(other as Evolution);
    }

    public override int GetHashCode() {
        int fusionHashes = 1;
        if (FusionEntries != null) {
            for (int iFusion = 0; iFusion < FusionEntries.Length; ++iFusion) {
                fusionHashes *= FusionEntries[iFusion].GetHashCode();
            }
        }
        return Entry.GetHashCode() * Types.GetHashCode() * fusionHashes;
    }

    public static bool operator ==(Evolution evolution1, Evolution evolution2) {
        if (((object)evolution1) == null || ((object)evolution2) == null) {
            return System.Object.Equals(evolution1, evolution2);
        }

        return evolution1.Equals(evolution2);
    }

    public static bool operator !=(Evolution evolution1, Evolution evolution2) {
        if (((object)evolution1) == null || ((object)evolution2) == null) {
            return !System.Object.Equals(evolution1, evolution2);
        }

        return !(evolution1.Equals(evolution2));
    }

    public List<Color> GetEvolutionColors() {
        return GetEvolutionColors(Types);
    }

    public static List<Color> GetEvolutionColors(EvolutionType type) {
        List<Color> colors = new List<Color>();

        var evolutionTypes = Enum.GetValues(typeof(EvolutionType));

        foreach (EvolutionType evolutionType in evolutionTypes) {
            if (type.HasFlag(evolutionType)) {
                switch (evolutionType) {
                    case EvolutionType.Main: {
                        colors.Add(Color.red);
                    } break;
                    case EvolutionType.Warp: {
                        colors.Add(new Color(1f, 0.6f, 0f));
                    } break;
                    case EvolutionType.Side: {
                        colors.Add(Color.magenta);
                    } break;
                    case EvolutionType.Fusion: {
                        colors.Add(Color.green);
                    } break;
                    case EvolutionType.Armor: {
                        colors.Add(Color.cyan);
                    } break;
                    case EvolutionType.Spirit: {
                        colors.Add(Color.yellow);
                    } break;
                }
            }
        }

        return colors;
    }
}