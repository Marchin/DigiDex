using System.Collections.Generic;

[System.Flags]
public enum EvolutionType {
    Regular         =   0,
    Main            =   1 << 0,
    Warp            =   1 << 1,
    Side            =   1 << 2,
    DNA             =   1 << 3,
    DNAIsOptional   =   1 << 4,
}

[System.Serializable]
public class Evolution {
    public int DigimonID;
    public EvolutionType Type;
    public List<int[]> FusionIDs;
}
