using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public static class DataRetriever {
    public const string LocalArtPath = "Assets/Art/";
    public const string RemoteArtPath = "Assets/Remote/Art/";
    public const string DataPath = "Assets/Remote/Data/";
    public const string RemoteArtGroupName = "Remote Art";
    public const string LocalArtGroupName = "Local Art";
    public const string DBGroupName = "Databases";
    public const string WikimonBaseURL = "https://wikimon.net";
    public const string DataCenterPath = DataPath + DataCenter.DataCenterAssetName + ".asset";
    private static Dictionary<string, XmlDocument> _sitesData;
    public static Dictionary<string, XmlDocument> SitesData {
        get {
            if (_sitesData == null) {
                _sitesData = new Dictionary<string, XmlDocument>();
            }
            
            return _sitesData;
        }
    }
    private static Dictionary<string, string> _sitesFinalLink;
    public static Dictionary<string, string> SitesFinalLink {
        get {
            if (_sitesFinalLink == null) {
                _sitesFinalLink = new Dictionary<string, string>();
            }
            
            return _sitesFinalLink;
        }
    }

    private static string RemoveXmlComments(string xml) {
        List<(int, int)> ranges = new List<(int, int)>();
        int index = 0;
        int startingIndex = 0;
        for (int i = 0; i < xml.Length; ++i) {
            switch (xml[i]) {
                case '<': {
                    if (index <= 3) {
                        index = 1;
                        startingIndex = i;
                    } else if (index > 4) {
                        index = 4;
                    }
                } break;
                case '!': {
                    if (index == 1) {
                        index = 2;
                    } else if (index == 2 || index == 3) {
                        index = 0;
                    } else if (index > 4) {
                        index = 4;
                    }
                } break;
                case '-': {
                    if (index >= 2 && index <= 5) {
                        ++index;
                    } else if (index == 6) {
                        index = 4;
                    }
                } break;
                case '>': {
                    if (index == 6) {
                        ranges.Add((startingIndex, i));
                        index = 0;
                    }
                } break;

                default: {
                    if (index <= 2) {
                        index = 0;
                    } else if (index > 4) {
                        index = 4;
                    }
                } break;
            }
        }

        string result = xml;
        for (int iRange = 0; iRange < ranges.Count; ++iRange) {
            int baseIndex = ranges[iRange].Item1;
            for (int jRange = 0; jRange < iRange; ++jRange) {
                baseIndex -= (ranges[jRange].Item2 - ranges[jRange].Item1);
            }
            result = result.Remove(baseIndex, ranges[iRange].Item2 - ranges[iRange].Item1);
        }

        return result;
    }

    public static async UniTask<XmlDocument> GetSite(string linkSubFix) {
        if (string.IsNullOrEmpty(linkSubFix)) {
            return null;
        }

        if (!SitesData.ContainsKey(linkSubFix)) {
            SitesData.Add(linkSubFix, null);
            string link = WikimonBaseURL + linkSubFix;
            try {
                List<string> subLinks = new List<string> { linkSubFix };
                string data = "";
                using (UnityWebRequest request = UnityWebRequest.Get(link)) {
                    await request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.ConnectionError) {
                        string finalLinkSubFix = request.url.Replace(WikimonBaseURL, "");
                        if (finalLinkSubFix != linkSubFix) {
                            subLinks.Add(finalLinkSubFix);
                            linkSubFix = finalLinkSubFix;
                        }
                        data = Encoding.ASCII.GetString(request.downloadHandler.data);
                        data = RemoveXmlComments(data);
                    } else {
                        return null;
                    }
                }

                XmlDocument site = new XmlDocument();
                site.LoadXml(data);
                // Sometimes name variants are used for the list, we look for the name used in the profile
                XmlNode redirectNode = site.SelectSingleNode("/html/body/div/div/div/div/div/div/div/ul[@class='redirectText']/li/a");
                while (redirectNode != null) {
                    string newLinkSubFix = redirectNode.Attributes.GetNamedItem("href").InnerText;
                    Debug.Log($"Redirecting from {linkSubFix} to {newLinkSubFix}");
                    subLinks.Add(newLinkSubFix);
                    linkSubFix = newLinkSubFix;
                    site = await DataRetriever.GetSite(linkSubFix);
                    redirectNode = site.SelectSingleNode("/html/body/div/div/div/div/div/div/div/ul[@class='redirectText']/li/a");
                }

                var linkNode = site.SelectSingleNode("//link[@rel='canonical']");
                if (linkNode != null) {
                    string newLinkSubFix = linkNode.Attributes.GetNamedItem("href")?.InnerText.Replace(WikimonBaseURL, "");

                    if (!string.IsNullOrEmpty(newLinkSubFix) && (newLinkSubFix != linkSubFix)) {
                        subLinks.Add(newLinkSubFix);
                        linkSubFix = newLinkSubFix;
                    }
                }

                foreach (var subLink in subLinks) {
                    SitesFinalLink[subLink] = linkSubFix;
                    SitesData[subLink] = site;
                }
            } catch (Exception ex) {
                SitesData.Remove(linkSubFix);
                Debug.LogError($"Error while loading {linkSubFix}: \n {ex.Message} \n {ex.StackTrace}");
                return null;
            }
        } else {
            await UniTask.WaitWhile(() => SitesData[linkSubFix] == null);
        }
        
        return SitesData[linkSubFix];
    }

    public static AddressableAssetGroup GetOrAddAddressableGroup(string name) {
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        AddressableAssetGroup group = addressablesSettings.groups.Find(g => g.Name == name);
        if (group == null) {
            AddressableAssetGroupTemplate template = AssetDatabase.LoadAssetAtPath<AddressableAssetGroupTemplate>
                ("Assets/AddressableAssetsData/AssetGroupTemplates/Packed Assets.asset");
            group = addressablesSettings.CreateGroup(name, false, false, false, template.SchemaObjects);
        }
        return group;
    }

    public static string AddresableSafe(this string name) {
        return name.Replace(":", string.Empty).Replace("/", "-");
    }

    public static DataCenter GetCentralDatabase() {
        DataCenter centralDB = GetOrCreateScriptableObject<DataCenter>(DataCenterPath);

        return centralDB;
    }

    public static T GetOrCreateScriptableObject<T>(string path) where T : ScriptableObject {
        T scriptableObj = null;
        if (!File.Exists(path)) {
            scriptableObj = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(scriptableObj, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } else {
            scriptableObj = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return scriptableObj;

    }

    public static async void GetEvolutions<T>(Database db, string entriesDataPath, string evolutionsDataPath, string evolutionGroupName) where T : IEvolvable {
        long start = DateTime.Now.Ticks;

        if (!Directory.Exists(evolutionsDataPath)) {
            Directory.CreateDirectory(evolutionsDataPath);
        }

        var paths = Directory.GetFiles(entriesDataPath, "*.asset");
        Array.Sort<string>(paths, (x, y) => y.CompareTo(y));
        List<(IDataEntry entry, EvolutionData evolutionData)> pairtList = new List<(IDataEntry d, EvolutionData ed)>();
        for (int iEntry = 0; iEntry < paths.Length; iEntry++) {
            IDataEntry entryData = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(paths[iEntry]) as IDataEntry;
            string evolutionDataPath =  $"{evolutionsDataPath}/{entryData.Name.AddresableSafe()} Evolutions.asset";
            EvolutionData evolutionData = GetOrCreateScriptableObject<EvolutionData>(evolutionDataPath);
            pairtList.Add((entryData, evolutionData));
            EditorUtility.SetDirty(evolutionData);
            EditorUtility.SetDirty(entryData as UnityEngine.Object);

            XmlDocument entrySite = null;

            try {
                entrySite = await GetSite(entryData.LinkSubFix);

                evolutionData.PreEvolutions = await ParseEvolutionList("Evolves_From");
                evolutionData.PreEvolutions.Sort((x, y) => {
                    if (x.Types.HasFlag(EvolutionType.Main)) {
                        return y.Types.HasFlag(EvolutionType.Main) ? x.DebugName.CompareTo(y.DebugName) : -1;
                    } else if (y.Types.HasFlag(EvolutionType.Main)) {
                        return 1;
                    } else {
                        return x.DebugName.CompareTo(y.DebugName);
                    }
                });

                evolutionData.Evolutions = await ParseEvolutionList("Evolves_To");
                evolutionData.Evolutions.Sort((x, y) => {
                    if (x.Types.HasFlag(EvolutionType.Main)) {
                        return y.Types.HasFlag(EvolutionType.Main) ? x.DebugName.CompareTo(y.DebugName) : -1;
                    } else if (y.Types.HasFlag(EvolutionType.Main)) {
                        return 1;
                    } else {
                        return x.DebugName.CompareTo(y.DebugName);
                    }
                });
            } catch (Exception ex) {
                Debug.Log($"{entryData.Name} - {ex.Message} \n {ex.StackTrace}");
            }


            ////////////////////////////////
            // Function Helpers
            ////////////////////////////////

            async UniTask<List<Evolution>> ParseEvolutionList(string headerName) {
                List<Evolution> evolutions = new List<Evolution>();

                XmlNodeList header = entrySite.SelectNodes($"/html/body/div/div/div/div/div/div/h2/span[@id='{headerName}']");
                // Check if there're entries to be parsed
                if (header?.Item(0)?.ParentNode.NextSibling.Name == "ul") {
                    XmlNodeList evolutionsNode = header.Item(0).ParentNode.NextSibling.SelectNodes("li");
                    for (int iField = 0; iField < evolutionsNode.Count; ++iField) {
                        XmlNode entryNode = evolutionsNode.Item(iField).FirstChild;
                        string name = entryNode.InnerText;
                        if (name.StartsWith("Any ")) {
                            continue;
                        }

                        var auxNode = entryNode.Name == "b"? entryNode.FirstChild : entryNode;
                        
                        IDataEntry entry = db.Entries.Find(d => d.Name == name);
                        string fuseEntryLinkSubFix = auxNode?.Attributes?.GetNamedItem("href")?.InnerText;

                        if (entry == null && !string.IsNullOrEmpty(fuseEntryLinkSubFix)) {
                            // Sometimes name variants are used for the list, we look for the name used in the profile
                            try {
                                XmlDocument fuseEntrySite = await GetSite(fuseEntryLinkSubFix);
                                name = fuseEntrySite.SelectSingleNode("//*[@id='firstHeading']").InnerText;
                            } catch (Exception ex) {
                                Debug.Log($"{name} - {fuseEntryLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
                            }

                            entry = db.Entries.Find(d => d.Name == name);
                        }

                        if (entry != null) {
                            List<Evolution> evolutionMethods = new List<Evolution>();

                            EvolutionType baseEvolutionType = EvolutionType.Regular;
                            if (entryNode.Name == "b") {
                                baseEvolutionType = EvolutionType.Main;
                            }
                            
                            EntryIndex entryIndex = new EntryIndex(typeof(T), entry.Hash);
                            Evolution method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType, FusionEntries = new EntryIndex[0] };

                            bool isWarp = false;
                            bool oneOrMoreOptionals = false;

                            XmlNode siblingNode = entryNode.NextSibling;
                            while (siblingNode != null) {
                                if (siblingNode.InnerText == "Warp Evolution") {
                                    isWarp = true;
                                } else if (siblingNode.InnerText.Contains("with")) {
                                    bool isOptional = siblingNode.InnerText.Contains("without") ||
                                        (siblingNode.Name == "b" && siblingNode.NextSibling.InnerText.Contains("without"));
                                    
                                    if (isOptional) {
                                        // The first optional means that the entry can evolve with the base element alone and we always record it
                                        // Otherwise we only record the method if it has any changes from the base element
                                        if (!oneOrMoreOptionals || (method.Types != baseEvolutionType)) {
                                            evolutions.Add(method);
                                            oneOrMoreOptionals = true;
                                        }
                                        method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType };
                                        
                                        if (siblingNode.Name == "b") {
                                            // skip "without" since we already parsed it
                                            siblingNode = siblingNode.NextSibling;
                                        }
                                    }
                                    
                                    // Start reading components
                                    siblingNode = siblingNode?.NextSibling;
                                    
                                    List<(EntryIndex index, bool isMain)> fusionIDs = new List<(EntryIndex index, bool isMain)>();
                                    bool recordFusionsTogether = false;
                                    bool recordFusionsSeparated = false;

                                    while (siblingNode != null) {
                                        if (siblingNode.InnerText.Contains("Digimental")) {
                                            // Record fusion in the case of EntryA(with EntryB or NotEntry)
                                            RecordConcatenatedFusions();
                                            method.Types |= EvolutionType.Armor;
                                            CheckMain(ref method, siblingNode);
                                            evolutions.Add(method);
                                            method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType };
                                        } else if (siblingNode.InnerText.Contains("Spirit")) {
                                            // Record fusion in the case of EntryA(with EntryB or NotEntry)
                                            RecordConcatenatedFusions();
                                            method.Types |= EvolutionType.Spirit;
                                            CheckMain(ref method, siblingNode);
                                            evolutions.Add(method);
                                            method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType };
                                        } else if (siblingNode.InnerText.Trim() == "Slide Evolution") {
                                            // Record fusion in the case of EntryA(with EntryB or NotEntry)
                                            RecordConcatenatedFusions();
                                            method.Types |= EvolutionType.Side;
                                            CheckMain(ref method, siblingNode);
                                            evolutions.Add(method);
                                            method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType };
                                        } else if (siblingNode.Name == "b" || siblingNode.Name == "a") {
                                            XmlNode aux = (siblingNode.Name == "b") ? siblingNode.FirstChild : siblingNode;
                                            string materialLink = aux?.Attributes?.GetNamedItem("href")?.Value;
                                            
                                            if (!string.IsNullOrEmpty(materialLink)) {
                                                IDataEntry fusion = db.Entries.Find(d => d.LinkSubFix == materialLink);
                                                if (fusion == null) {
                                                    try {
                                                        XmlDocument materialSite = await GetSite(materialLink);
                                                        fusion = db.Entries.Find(d => d.LinkSubFix == materialLink);
                                                    } catch (Exception ex) {
                                                        Debug.Log($"{name}: Failed to get material {materialLink} - {ex.Message} \n {ex.StackTrace}");
                                                    }
                                                }
                                                if (fusion != null) {
                                                    method.Types |= EvolutionType.Fusion;
                                                    EntryIndex fusionEntry = new EntryIndex(
                                                        typeof(T), 
                                                        fusion.Hash
                                                    );
                                                    fusionIDs.Add((fusionEntry, siblingNode.Name == "b"));
                                                }
                                                RecordConcatenatedFusions();
                                            }
                                        } else if (siblingNode.InnerText.Contains("or")) {
                                            if (fusionIDs.Count > 0) {
                                                recordFusionsSeparated = true;
                                                RecordConcatenatedFusions();
                                            }
                                        } else if (siblingNode.InnerText.Contains("and")) {
                                            if (fusionIDs.Count > 0) {
                                                recordFusionsTogether = true;
                                            }
                                        } else if (siblingNode.InnerText.Contains(')')) {
                                            if (method.Types != baseEvolutionType) {
                                                // Record fusion in the case of EntryA(with EntryB or NotEntry)
                                                RecordConcatenatedFusions();
                                                RecordFuseRemanents();
                                                method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType };
                                            }
                                            break;
                                        }

                                        siblingNode = siblingNode.NextSibling;

                                        void CheckMain(ref Evolution evo, XmlNode node) {
                                            if (node.Name != "b") {
                                                evo.Types &= ~EvolutionType.Main;
                                            }
                                        }

                                        void RecordConcatenatedFusions() {
                                            if (recordFusionsTogether) {
                                                if (fusionIDs.Count > 0) {
                                                    EntryIndex[] fusionIndices = new EntryIndex[fusionIDs.Count];
                                                    for (int iFusion = 0; iFusion < fusionIDs.Count; ++iFusion) {
                                                        fusionIndices[iFusion] = fusionIDs[iFusion].index;
                                                    }
                                                    method.FusionEntries = fusionIndices;
                                                    if (!fusionIDs[0].isMain) {
                                                        method.Types &= ~EvolutionType.Main;
                                                    }
                                                    fusionIDs.Clear();
                                                    evolutions.Add(method);
                                                    method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType };
                                                }
                                                recordFusionsTogether = false;
                                            }
                                            if (recordFusionsSeparated) {
                                                var a = method;
                                                var b = evolutionMethods;
                                                for (int iFusionID = 0; iFusionID < fusionIDs.Count; ++iFusionID) {
                                                    method.FusionEntries = new EntryIndex[] { fusionIDs[iFusionID].index };
                                                    method.Types = baseEvolutionType;
                                                    if (!fusionIDs[iFusionID].isMain) {
                                                        method.Types &= ~EvolutionType.Main;
                                                    }
                                                    method.Types |= EvolutionType.Fusion;
                                                    evolutions.Add(method);
                                                    method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType | EvolutionType.Fusion };
                                                }
                                                fusionIDs.Clear();
                                                recordFusionsSeparated = false;
                                            }
                                        }
                                    }

                                    RecordFuseRemanents();

                                    void RecordFuseRemanents() {
                                        if (fusionIDs.Count > 0) {
                                            EntryIndex[] fusionIndices = new EntryIndex[fusionIDs.Count];
                                            for (int iFusion = 0; iFusion < fusionIDs.Count; ++iFusion) {
                                                fusionIndices[iFusion] = fusionIDs[iFusion].index;
                                            }
                                            method.FusionEntries = fusionIndices;
                                            if (!fusionIDs[0].isMain) {
                                                method.Types &= ~EvolutionType.Main;
                                            }
                                            fusionIDs.Clear();
                                            evolutions.Add(method);
                                            method = new Evolution { Entry = entryIndex, DebugName = name, Types = baseEvolutionType | EvolutionType.Fusion };
                                        }
                                    }
                                }

                                siblingNode = siblingNode?.NextSibling;
                            }

                            if ((evolutionMethods.Count == 0) ||
                                ((method.Types > EvolutionType.Main) &&
                                    !(method.Types.HasFlag(EvolutionType.Fusion) && (method.FusionEntries?.Length ?? 0) == 0))
                            ) {
                                evolutions.Add(method);
                            }

                            for (int iMethod = 0; iMethod < evolutionMethods.Count; ++iMethod) {
                                // Warp means the an evolution stage gets skipped independent of the method
                                if (isWarp) {
                                    evolutionMethods[iMethod].Types |= EvolutionType.Warp;
                                }
                                evolutions.Add(evolutionMethods[iMethod]);
                            }
                        }
                    }
                }

                evolutions.RemoveAll(e => e.Types.HasFlag(EvolutionType.Fusion) && (e.FusionEntries?.Length ?? 0) == 0);

                List<Evolution> disinctEvolutions = new List<Evolution>(evolutions.Count);
                for (int iEvolution = 0; iEvolution < evolutions.Count; ++iEvolution) {
                    if (!disinctEvolutions.Contains(evolutions[iEvolution])) {
                        disinctEvolutions.Add(evolutions[iEvolution]);
                    }
                }

                return disinctEvolutions;
            }
        }

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        var evolutionPaths = Directory.GetFiles(evolutionsDataPath, "*.asset");
        Array.Sort<string>(evolutionPaths, (x, y) => y.CompareTo(y));
        AddressableAssetGroup group = GetOrAddAddressableGroup(evolutionGroupName);
        for (int iEvolutionPath = 0; iEvolutionPath < evolutionPaths.Length; ++iEvolutionPath) {
            settings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(evolutionPaths[iEvolutionPath]).ToString(), group);
        }

        foreach(var entryEvoData in pairtList) {
            string evolutionDataPath = $"{evolutionsDataPath}/{entryEvoData.entry.Name.AddresableSafe()} Evolutions.asset";
            (entryEvoData.entry as IEvolvable).EvolutionDataRef = new AssetReferenceEvolutionData(
                AssetDatabase.GUIDFromAssetPath(evolutionDataPath).ToString());
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Evolutions retrieved {new TimeSpan(DateTime.Now.Ticks - start)}");
    }
}