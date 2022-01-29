using System.Collections.Generic;

public class AppmonDatabase : Database {
    public override string DisplayName => "Appmon";
    
    private const string TypesFilter = "Types";
    private const string GradesFilter = "Grades";
    private const string ReverseToggle = "Reverse";
    public List<Appmon> Appmons;
    public List<AppmonGrade> Grades;
    public List<AppmonType> Types;
    private List<IDataEntry> _entries;
    public override List<IDataEntry> Entries {
        get {
            if (_entries == null) {
                _entries = new List<IDataEntry>(Appmons.Count);
                foreach (var appmon in Appmons) {
                    _entries.Add(appmon);
                }
            }

            return _entries;
        }
    }

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

        RetrieveListToggle(ref toggles);
        
        return toggles;
    }
}
