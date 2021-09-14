using UnityEngine;
using System.Collections.Generic;

public interface IDataObject {
    List<InformationData> ExtractInformationData(CentralDatabase centralDB);
}

public interface IDataDabase {
    List<FilterData> RetrieveFilterData();
}

public class CentralDatabase : ScriptableObject {
    public DigimonDatabase DigimonDB;
}
