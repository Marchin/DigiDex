using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public interface IDataObject {
    UniTask<List<InformationData>> ExtractInformationData(CentralDatabase centralDB);
}

public class CentralDatabase : ScriptableObject {
    public DigimonDatabase DigimonDB;
}
