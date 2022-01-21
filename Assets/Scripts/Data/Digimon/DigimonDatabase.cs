using System.Linq;
using System.Collections.Generic;

public class DigimonDatabase : Database {
    public override string DisplayName => "Digimon";
    public override string DataKey => "Digimons";

    private const string FieldsFilter = "Fields";
    private const string AttributesFilter = "Attributes";
    private const string TypesFilter = "Types";
    private const string LevelsFilter = "Levels";
    private const string GroupsFilter = "Groups";
    private const string ListsFilter = "Lists";
    private const string InListToggle = "In List";
    private const string ReverseToggle = "Reverse";
    public override IEnumerable<IDataEntry> Entries => Digimons;
    public List<Digimon> Digimons;
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

        if (Lists.Count > 0) {
            FilterData listsFilter = new FilterData(
                name: ListsFilter,
                getFilteringComponent: element => {
                    List<string> listsNames = Lists.Keys.ToList();
                    return Lists
                        .Where(kvp => kvp.Value.Contains(element.Hash))
                        .Select(kvp => listsNames.IndexOf(kvp.Key))
                        .ToList();
                }
            );
            listsFilter.Elements = new List<FilterEntryData>(Lists.Count);
            for (int iList = 0; iList < Lists.Count; ++iList) {
                listsFilter.Elements.Add(new FilterEntryData { Name = Lists.ElementAt(iList).Key });
            }
            filters.Add(listsFilter);
        }

        return filters;
    }

    public override List<ToggleActionData> RetrieveTogglesData() {
        List<ToggleActionData> toggles = new List<ToggleActionData>();

        toggles.Add(
            new ToggleActionData(
                name: ReverseToggle, 
                action: (list, isOn) => {
                    if (isOn) {
                        return list.Reverse();
                    } else {
                        return list;
                    }
                }
            )
        );

        if (Lists.Count > 0) {
            toggles.Add(
                new ToggleActionData(
                    name: InListToggle, 
                    action: (list, isOn) => {
                        if (isOn) {
                            return list.Where(e => Lists.Any(kvp => kvp.Value.Contains(e.Hash)));
                        } else {
                            return list;
                        }
                    }
                )
            );
        }

        return toggles;
    }

    public override void RefreshFilters(
        ref IEnumerable<FilterData> filters, 
        ref IEnumerable<ToggleActionData> toggles
    ) {
        var listFilter = filters.FirstOrDefault(f => f.Name == ListsFilter);
        if (listFilter != null) {
            var elements = listFilter.Elements.Where(f => Lists.Any(kvp => kvp.Key == f.Name));
            listFilter.Elements = elements
                .Concat(
                    Lists.Where(kvp => !elements.Any(e => e.Name == kvp.Key))
                        .Select(kvp => new FilterEntryData { Name = kvp.Key }))
                .ToList();
            if (listFilter.Elements.Count() <= 0) {
                filters = filters.Where(f => f.Name != ListsFilter);
            }
        } else {
            if (Lists.Count > 0) {
                FilterData listsFilter = new FilterData(
                    name: ListsFilter,
                    getFilteringComponent: element => {
                        List<string> listsNames = Lists.Keys.ToList();
                        return Lists
                            .Where(kvp => kvp.Value.Contains(element.Hash))
                            .Select(kvp => listsNames.IndexOf(kvp.Key))
                            .ToList();
                    }
                );
                listsFilter.Elements = new List<FilterEntryData>(Lists.Count);
                for (int iList = 0; iList < Lists.Count; ++iList) {
                    listsFilter.Elements.Add(new FilterEntryData { Name = Lists.ElementAt(iList).Key });
                }
                filters = filters.Append(listsFilter);
            }
        }

        var inListToggle = toggles.FirstOrDefault(t => t.Name == InListToggle);
        if (inListToggle != null) {
            if (Lists.Count <= 0) {
                toggles = toggles.Where(t => t.Name != InListToggle);
            }
        } else {
            if (Lists.Count > 0) {
                toggles = toggles.Append(
                    new ToggleActionData(
                        name: InListToggle, 
                        action: (list, isOn) => {
                            if (isOn) {
                                return list.Where(e => Lists.Any(kvp => kvp.Value.Contains(e.Hash)));
                            } else {
                                return list;
                            }
                        }
                    )
                );
            }
        }
    }


}
