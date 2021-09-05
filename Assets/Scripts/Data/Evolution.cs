using System;
using System.Collections.Generic;
using System.Linq;

[System.Flags]
public enum EvolutionType {
    Regular             =   0,
    Main                =   1 << 0,
    Warp                =   1 << 1,
    Side                =   1 << 2,
    Fusion              =   1 << 3, // This includes DNA, Joggress, etc
    Armor               =   1 << 4,
    Spirit              =   1 << 5,

    // TODO: Matrix or just Fusion with human?
}

[System.Serializable]
public class Evolution : IEquatable<Evolution> {
    public int DigimonID;
    public string DebugName;
    public EvolutionType Type;
    public int[] FusionIDs;

    public bool Equals(Evolution other) {
        bool areEqual = this.DigimonID == other.DigimonID &&
            this.Type == other.Type &&
            this.FusionIDs.Length == other.FusionIDs.Length &&
            this.FusionIDs.Except(other.FusionIDs).Count() == 0 &&
            other.FusionIDs.Except(this.FusionIDs).Count() == 0;

        return areEqual;
    }

    public override bool Equals(object other) {
        //Check whether the compared object is null.
        if (Object.ReferenceEquals(other, null)) return false;

        //Check whether the compared object references the same data.
        if (Object.ReferenceEquals(this, other)) return true;

        return this.Equals(other as Evolution);
    }

    public override int GetHashCode() {
        int fusionHashes = 1;
        UnityEngine.Debug.Log("Hey");
        for (int iFusion = 0; iFusion < FusionIDs.Length; ++iFusion) {
            fusionHashes *= FusionIDs[iFusion].GetHashCode();
        }
        return DigimonID.GetHashCode() * Type.GetHashCode() * fusionHashes;
    }

    public static bool operator ==(Evolution evolution1, Evolution evolution2) {
      if (((object)evolution1) == null || ((object)evolution2) == null) {
        return Object.Equals(evolution1, evolution2);
      }

      return evolution1.Equals(evolution2);
   }

   public static bool operator !=(Evolution evolution1, Evolution evolution2) {
      if (((object)evolution1) == null || ((object)evolution2) == null) {
        return !Object.Equals(evolution1, evolution2);
      }

      return !(evolution1.Equals(evolution2));
   }
}
