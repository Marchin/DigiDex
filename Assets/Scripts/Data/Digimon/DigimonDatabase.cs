using System.Collections.Generic;

public class DigimonDatabase : Database {
    public override string DisplayName => "Digimon";

    private const string FieldsFilter = "Fields";
    private const string AttributesFilter = "Attributes";
    private const string TypesFilter = "Types";
    private const string LevelsFilter = "Levels";
    private const string GroupsFilter = "Groups";
    private const string ReverseToggle = "Reverse";
    public List<Digimon> Digimons;
    private List<IDataEntry> _entries;
    public override List<IDataEntry> Entries {
        get {
            if (_entries == null) {
                _entries = new List<IDataEntry>(Digimons.Count);
                foreach (var digimon in Digimons) {
                    _entries.Add(digimon);
                }
            }

            return _entries;
        }
    }
    public List<Field> Fields;
    public List<Attribute> Attributes;
    public List<DigimonType> Types;
    public List<DigimonGroup> Groups;
    public List<Level> Levels;

    public override List<FilterData> RetrieveFiltersData() {
        List<FilterData> filters = new List<FilterData>();

        FilterData fieldsFilter = new FilterData(
            name: FieldsFilter,
            getFilteringComponent: element => (element as Digimon).FieldIDs
        );
        fieldsFilter.Elements = new List<FilterEntryData>(Fields.Count);
        for (int iField = 0; iField < Fields.Count; ++iField) {
            fieldsFilter.Elements.Add(
                new FilterEntryData { Name = Fields[iField].Name, 
                    Sprite = Fields[iField].Sprite });
        }
        filters.Add(fieldsFilter);
      

        FilterData attributesFilter = new FilterData(
            name: AttributesFilter,
            getFilteringComponent: element => (element as Digimon).AttributeIDs
        );
        attributesFilter.Elements = new List<FilterEntryData>(Attributes.Count);
        for (int iAttribute = 0; iAttribute < Attributes.Count; ++iAttribute) {
            attributesFilter.Elements.Add(
                new FilterEntryData { Name = Attributes[iAttribute].Name,
                    Sprite = Attributes[iAttribute].Sprite });
        }
        filters.Add(attributesFilter);


        FilterData typesFilter = new FilterData(
            name: TypesFilter,
            getFilteringComponent: element => (element as Digimon).TypeIDs
        );
        typesFilter.Elements = new List<FilterEntryData>(Types.Count);
        for (int iType = 0; iType < Types.Count; ++iType) {
            typesFilter.Elements.Add(new FilterEntryData { Name = Types[iType].Name });
        }
        filters.Add(typesFilter);


        FilterData levelsFilter = new FilterData(
            name: LevelsFilter,
            getFilteringComponent: element => (element as Digimon).LevelIDs
        );
        levelsFilter.Elements = new List<FilterEntryData>(Levels.Count);
        for (int iLevel = 0; iLevel < Levels.Count; ++iLevel) {
            levelsFilter.Elements.Add(new FilterEntryData { Name = Levels[iLevel].DisplayName });
        }
        filters.Add(levelsFilter);

        FilterData groupsFilter = new FilterData(
            name: GroupsFilter,
            getFilteringComponent: element => (element as Digimon).GroupIDs
        );
        groupsFilter.Elements = new List<FilterEntryData>(Groups.Count);
        for (int iGroup = 0; iGroup < Groups.Count; ++iGroup) {
            groupsFilter.Elements.Add(new FilterEntryData { Name = Groups[iGroup].Name });
        }
        filters.Add(groupsFilter);

        RetrieveListFilter(ref filters);

        return filters;
    }

    public override List<ToggleActionData> RetrieveTogglesData() {
        List<ToggleActionData> toggles = new List<ToggleActionData>();

        toggles.Add(
            new ToggleActionData(
                name: ReverseToggle, 
                action: (list, isOn) => {
                    if (isOn) {
                        list.Reverse();
                    }
                    return list;
                }
            )
        );

        if (Lists.Count > 0) {
            toggles.Add(
                new ToggleActionData(
                    name: InListToggle, 
                    action: (list, isOn) => {
                        if (isOn) {
                            List<IDataEntry> result = new List<IDataEntry>(list.Count);
                            for (int iEntry = 0; iEntry < list.Count; ++iEntry) {
                                foreach (var kvp in Lists) {
                                    if (kvp.Value.Contains(list[iEntry].Hash)) {
                                        result.Add(list[iEntry]);
                                        break;
                                    }
                                }
                            }
                            return result;
                        } else {
                            return list;
                        }
                    }
                )
            );
        }

        return toggles;
    }
}
