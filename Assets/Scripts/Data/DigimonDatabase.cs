using System.Collections.Generic;
using UnityEngine;

public class DigimonDatabase : ScriptableObject, IDataDabase {
    public List<Digimon> Digimons;
    public List<Field> Fields;
    public List<Attribute> Attributes;
    public List<Type> Types;
    public List<Level> Levels;

    public List<FilterData> RetrieveFilterData() {
        List<FilterData> filters = new List<FilterData>();

        FilterData fieldsFilter = new FilterData();
        fieldsFilter.Name = "Fields";
        fieldsFilter.Elements = new List<FilterEntryData>(Fields.Count);
        for (int iField = 0; iField < Fields.Count; ++iField) {
            fieldsFilter.Elements.Add(new FilterEntryData { Name = Fields[iField].Name, Sprite = Fields[iField].Sprite });
        }
        filters.Add(fieldsFilter);

        FilterData attributesFilter = new FilterData();
        attributesFilter.Name = "Attributes";
        attributesFilter.Elements = new List<FilterEntryData>(Attributes.Count);
        for (int iAttribute = 0; iAttribute < Attributes.Count; ++iAttribute) {
            attributesFilter.Elements.Add(new FilterEntryData { Name = Attributes[iAttribute].Name, Sprite = Attributes[iAttribute].Sprite });
        }
        filters.Add(attributesFilter);

        FilterData typesFilter = new FilterData();
        typesFilter.Name = "Types";
        typesFilter.Elements = new List<FilterEntryData>(Types.Count);
        for (int iType = 0; iType < Types.Count; ++iType) {
            typesFilter.Elements.Add(new FilterEntryData { Name = Types[iType].Name });
        }
        filters.Add(typesFilter);

        FilterData levelsFilter = new FilterData();
        levelsFilter.Name = "Levels";
        levelsFilter.Elements = new List<FilterEntryData>(Levels.Count);
        for (int iLevel = 0; iLevel < Levels.Count; ++iLevel) {
            levelsFilter.Elements.Add(new FilterEntryData { Name = Levels[iLevel].Name });
        }
        filters.Add(levelsFilter);

        return filters;
    }
}
