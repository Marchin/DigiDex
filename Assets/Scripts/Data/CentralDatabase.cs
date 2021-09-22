using UnityEngine;
using System.Collections.Generic;

public interface IDataObject {
   List<InformationData> ExtractInformationData(CentralDatabase centralDB);
}

public interface IDataDabase {
    Dictionary<string, FilterData> RetrieveFiltersData();
    Dictionary<string, ToggleData> RetrieveTogglesData();
}

public class CentralDatabase : ScriptableObject {
    public DigimonDatabase DigimonDB;
}
