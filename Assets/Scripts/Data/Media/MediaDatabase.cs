using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediaDatabase : Database {
    public override string DisplayName => "Media";
    private List<IDataEntry> _entries;
    public override List<IDataEntry> Entries {
        get {
            if (_entries == null) {
                // _entries = new List<IDataEntry>(Characters.Count);
                // foreach (var digimon in Characters) {
                //     _entries.Add(digimon);
                // }
            }

            return _entries;
        }
    }
    
    public override List<FilterData> RetrieveFiltersData() {
        List<FilterData> filters = new List<FilterData>();

        return filters;
    }

    
    public override List<ToggleActionData> RetrieveTogglesData() {
        List<ToggleActionData> toggles = new List<ToggleActionData>();

        return toggles;
    }

}
