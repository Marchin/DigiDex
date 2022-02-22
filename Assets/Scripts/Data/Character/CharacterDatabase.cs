using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterDatabase : Database {
    public override string DisplayName => "Characters";
    public List<Character> Characters { get; private set; } = new List<Character>();
    private List<IDataEntry> _entries;
    public override List<IDataEntry> Entries {
        get {
            if (_entries == null) {
                _entries = new List<IDataEntry>(Characters.Count);
                foreach (var digimon in Characters) {
                    _entries.Add(digimon);
                }
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
