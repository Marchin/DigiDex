using System.Collections.Generic;
using UnityEngine;

public class DigimonDatabase : ScriptableObject {
    public List<DigimonReference> Digimons;
    public List<FieldReference> Fields;
    public List<AttributeReference> Attributes;
    public List<Type> Types;
    public List<Level> Levels;
}
