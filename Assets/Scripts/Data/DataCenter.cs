using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Reflection;
using System.Collections.Generic;

public class DataCenter : ScriptableObject {
    public const string DataCenterAssetName = "Central Database";
    public DigimonDatabase DigimonDB;
    public AppmonDatabase AppmonDB;

    public List<Database> GetDatabases() {
        List<Database> result = new List<Database>();

        // TODO: See if we can automate this through reflection
        result.Add(DigimonDB);
        result.Add(AppmonDB);

        return result;
    }
}
