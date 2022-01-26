using System.Linq;
using System.Collections.Generic;

public class AppmonDatabase : Database {
    public override string DisplayName => "Appmon";
    
    private const string TypesFilter = "Types";
    private const string GradesFilter = "Grades";
    private const string InListToggle = "In List";
    private const string ReverseToggle = "Reverse";
    private const string ListsFilter = "Lists";
    public List<Appmon> Appmons;
    public List<AppmonGrade> Grades;
    public List<AppmonType> Types;
    public override IEnumerable<IDataEntry> Entries => Appmons;

    public override List<FilterData> RetrieveFiltersData() {
        List<FilterData> filters = new List<FilterData>();

        FilterData typesFilter = new FilterData(
            name: TypesFilter,
            getFilteringComponent: element => (element as Appmon).TypeIDs
        );
        typesFilter.Elements = new List<FilterEntryData>(Types.Count);
        for (int iType = 0; iType < Types.Count; ++iType) {
            typesFilter.Elements.Add(new FilterEntryData { Name = Types[iType].Name, Sprite = Types[iType].Sprite });
        }
        filters.Add(typesFilter);

        FilterData gradesFilter = new FilterData(
            name: GradesFilter,
            getFilteringComponent: element => (element as Appmon).GradeIDs
        );
        gradesFilter.Elements = new List<FilterEntryData>(Grades.Count);
        for (int iGrade = 0; iGrade < Grades.Count; ++iGrade) {
            gradesFilter.Elements.Add(new FilterEntryData { Name = Grades[iGrade].Name });
        }
        filters.Add(gradesFilter);

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
            foreach (var list in Lists) {
                listsFilter.Elements.Add(new FilterEntryData { Name = list.Key });
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
                foreach (var list in Lists) {
                    listsFilter.Elements.Add(new FilterEntryData { Name = list.Key });
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
