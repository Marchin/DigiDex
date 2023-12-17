using System;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public static class AppmonDataRetriever {
    public const string AppmonSpriteAtlasesGroupName = "Appmon Sprite Atlases";
    public const string AppmonListGroupName = "Appmon List";
    public const string AppmonDataGroupName = "Appmon Data";
    public const string AppmonEvolutionDataGroupName = "Appmon Evolution Data";
    public const string AppmonListSubFix = "/List_of_Appmon";
    public const string TypeListSubFix = "/Appmon";
    public const string GradeListSubFix = "/Evolution_Stage";
    public const int AppmonsPerAtlas = 3;
    public const string ArtAppmonFolder = DataRetriever.RemoteArtPath + "Appmon/";
    public const string ArtAppmonsPath = ArtAppmonFolder + "Appmons/";
    public const string ArtAppsPath = ArtAppmonFolder + "Apps/";
    public const string AppmonDataPath = DataRetriever.DataPath + "Appmon/";
    public const string AppmonsDataPath = AppmonDataPath + "Appmons";
    public const string AppmonEvolutionsDataPath = AppmonDataPath + "Appmons/Evolutions";
    public const string AppmonDBPath = AppmonDataPath + "Appmon Database.asset";
    public const string AppsRemoteArtPath = ArtAppmonFolder + "Apps";
    public const string TypesRemoteArtPath = ArtAppmonFolder + "Types";
    public const string SpriteAtlasXPath = ArtAppmonsPath + "Appmons ({0}).spriteatlas";
    
    public static AppmonDatabase GetAppmonDatabase() {
        if (!Directory.Exists(AppmonDataPath)) {
            Directory.CreateDirectory(AppmonDataPath);
        }

        AppmonDatabase appmonDB = DataRetriever.GetOrCreateScriptableObject<AppmonDatabase>(AppmonDBPath);

        return appmonDB;
    }

    [MenuItem("DigiDex/Appmon/Retrieve Data")]
    public static async void RetrieveData() {
        await GenerateTypeList();
        await GenerateGradeList();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        if (!Directory.Exists(AppmonsDataPath)) {
            Directory.CreateDirectory(AppmonsDataPath);
        }
        if (!Directory.Exists(AppsRemoteArtPath)) {
            Directory.CreateDirectory(AppsRemoteArtPath);
        }


        var dataGroup = DataRetriever.GetOrAddAddressableGroup(AppmonListGroupName);

        var spriteAtlasGroup = DataRetriever.GetOrAddAddressableGroup(AppmonSpriteAtlasesGroupName);
        var schema = spriteAtlasGroup.GetSchema<BundledAssetGroupSchema>();
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;

        AppmonDatabase appmonDB = GetAppmonDatabase();

        List<(Appmon appmon, string path)> appmonsWithArt = new List<(Appmon appmon, string path)>();

        string appAtlasPath = AppsRemoteArtPath + "/Apps.spriteatlas";
        SpriteAtlas appAtlas = new SpriteAtlas();
        AssetDatabase.CreateAsset(appAtlas, appAtlasPath);
        EditorUtility.SetDirty(appAtlas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string appAtlasGUID = AssetDatabase.GUIDFromAssetPath(appAtlasPath).ToString();
        addressablesSettings.CreateOrMoveEntry(appAtlasGUID, spriteAtlasGroup);

        Dictionary<string, int> imagesToSkip = new Dictionary<string, int> {
            {"Biomon", 1},
            {"Calcumon", 1},
            {"Consulmon", 1},
            {"Coordemon", 1},
            {"Dantemon", 1},
            {"Deusmon", 1},
            {"Diarimon", 1},
            {"Denpamon", 3},
            {"Docmon", 1},
            {"Dogamon", 1},
            {"DoGatchmon", 1},
            {"Dreammon", 1},
            {"Ecomon", 3},
            {"Gaiamon (Appmon)", 1},
            {"Gatchmon", 1},
            {"Hadesmon", 1},
            {"Jetmon", 1},
            {"Kakeimon", 1},
            {"Kosomon", 1},
            {"Mediamon", 2},
            {"Medicmon", 1},
            {"Messemon", 1},
            {"Mirrormon", 2},
            {"Musclemon", 2},
            {"Musimon", 1},
            {"Navimon (Appmon)", 1},
            {"Offmon", 1},
            {"Oujamon", 1},
            {"Ouranosmon", 1},
            {"Perorimon", 1},
            {"Pokomon (Appmon)", 1},
            {"Poseidomon", 1},
            {"Puzzlemon", 1},
            {"Racemon", 1},
            {"Raidramon", 1},
            {"Rebootmon", 3},
            {"Roamon", 1},
            {"Ropuremon", 1},
            {"Rocketmon", 1},
            {"Savemon", 1},
            {"Sateramon", 1},
            {"Setmon", 1},
            {"Shutmon", 1},
            {"Tarotmon", 2},
            {"Trickmon", 1},
            {"Tutomon", 1},
            {"Uratekumon", 1},
            {"Vegasmon", 1},
            {"Warpmon", 1},
            {"Warudamon", 1},
        };


        XmlDocument appmonListSite = await DataRetriever.GetSite(AppmonListSubFix);
        XmlNodeList table = appmonListSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        for (int i = 0; i < table.Count; i++) {
            string appmonLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(appmonLinkSubFix)) {
                try {
                    XmlDocument appmonSite = await DataRetriever.GetSite(appmonLinkSubFix);
                    string appmonName = appmonSite.SelectSingleNode("//*[@id='firstHeading']").InnerText;
                    string appmonNameSafe = appmonName.AddresableSafe();
                    string appmonArtPath = ArtAppmonsPath + appmonNameSafe + ".png";
                    string appmonDataPath = AppmonsDataPath + "/" + appmonNameSafe + ".asset";

                    if (!Directory.Exists(ArtAppmonsPath)) {
                        Directory.CreateDirectory(ArtAppmonsPath);
                    }
                    if (!Directory.Exists(AppmonsDataPath)) {
                        Directory.CreateDirectory(AppmonsDataPath);
                    }
                    if (!Directory.Exists(AppsRemoteArtPath)) {
                        Directory.CreateDirectory(AppsRemoteArtPath);
                    }

                    bool hasArt = false;
                    if (!File.Exists(appmonArtPath)) {
                        XmlNode image = appmonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[1]/td/div/div/a");
                        if (image != null) {
                            string linkToImagePage = image.Attributes.GetNamedItem("href").InnerText;
                            var hdImageSide = await DataRetriever.GetSite(linkToImagePage);
                            XmlNode hdImage = null;
                            XmlNodeList images = hdImageSide.SelectNodes("//table[@class='wikitable filehistory']/tr/td[4]");
                            List<(XmlNode node, int result)> imagesList = new List<(XmlNode node, int result)>(images?.Count ?? 0);
                            for (int iNode = 0; iNode < images.Count; ++iNode) {
                                XmlNode imageItem = images.Item(iNode);
                                // We extract the image resolution and fetch the best one
                                string[] values = imageItem.InnerText.Split(' ');
                                int width; 
                                int height;
                                if (int.TryParse(values[0], NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out width) &&
                                    int.TryParse(values[2], NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out height)
                                ) {
                                    int value = width * height;
                                    imagesList.Add((images.Item(iNode), value));
                                }
                            }

                            if (imagesList.Count > 0) {
                                imagesList.Sort((x, y) => y.result.CompareTo(x.result));
                                if (imagesToSkip.ContainsKey(appmonNameSafe)) {
                                    int toRemove = Mathf.Min(imagesToSkip[appmonNameSafe], imagesList.Count - 1);
                                    
                                    for (int iRemove = 0; iRemove < toRemove; ++iRemove) {
                                        imagesList.RemoveAt(0);
                                    }
                                }

                                hdImage = imagesList[0].node.PreviousSibling.FirstChild;
                                if (hdImage != null) {
                                    string linkToImage = DataRetriever.WikimonBaseURL + hdImage.Attributes.GetNamedItem("href").InnerText;

                                    using (UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
                                        await request.SendWebRequest();
                                        if (request.result != UnityWebRequest.Result.ConnectionError) {
                                            var data = request.downloadHandler.data;
                                            var file = File.Create(appmonArtPath);
                                            file.Write(data, 0, data.Length);
                                            file.Close();
                                            AssetDatabase.Refresh();
                                            hasArt = true;
                                        }
                                    }
                                }
                            }
                        }
                    } else {
                        hasArt = true;
                    }
                    
                    Appmon appmonData = null;
                    if (!File.Exists(appmonDataPath)) {
                        appmonData = ScriptableObject.CreateInstance<Appmon>();
                        AssetDatabase.CreateAsset(appmonData, appmonDataPath);
                    } else {
                        appmonData = AssetDatabase.LoadAssetAtPath<Appmon>(appmonDataPath);
                    }

                    appmonData.LinkSubFix = appmonLinkSubFix;
                    appmonData.Hash = Hash128.Compute(appmonData.LinkSubFix);
                    appmonData.Name = appmonName;
                    appmonData.TypeIDs = new List<int>();
                    appmonData.GradeIDs = new List<int>();
                    appmonData.Powers = new List<int>();

                    XmlNode profileNode = appmonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        appmonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        appmonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        appmonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td/p");

                    if (profileNode != null) {
                        if (profileNode.FirstChild?.LocalName == "span") {
                            // Remove the "Japanese/English" Toggle
                            profileNode.RemoveChild(profileNode.FirstChild);
                        }
                        appmonData.Profile = profileNode.InnerText;
                    } else {
                        Debug.Log($"No profile found for {appmonNameSafe}");
                    }
    
                    XmlNodeList properties = appmonSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr");
                    string lastCategory = "";
                    for (int iProperties = 0; iProperties < properties.Count; ++iProperties) {
                        XmlNode dataNode = properties.Item(iProperties).FirstChild;
                        if (dataNode == null) {
                            continue;
                        }

                        string fieldType = dataNode?.FirstChild?.Name;
                        if (fieldType == "b") {
                            lastCategory = dataNode.InnerText;
                            dataNode = dataNode.NextSibling;
                            fieldType = dataNode?.FirstChild?.Name;
                        }
                        
                        XmlNode valueNode = dataNode.FirstChild;
                        while (valueNode != null) {
                            if (fieldType == "a" || fieldType == "#text") {
                                string propertyName = valueNode.InnerText;

                                switch (lastCategory) {
                                    case "App": {
                                        string appName = propertyName;
                                        appmonData.App = new AppData { Name = appName };
                                        string appSpritePath = AppsRemoteArtPath + "/" + appName.AddresableSafe() + ".png";
                                        bool hasAppArt = false;
                                        if (!File.Exists(appSpritePath)) {
                                            XmlNode imageNode = appmonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[2]/th/img");
                                            if (imageNode != null) {
                                                string linkToImage = DataRetriever.WikimonBaseURL + imageNode.Attributes.GetNamedItem("src").InnerText;
                                                using (UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
                                                    await request.SendWebRequest();
                                                    if (request.result != UnityWebRequest.Result.ConnectionError) {
                                                        var data = request.downloadHandler.data;
                                                        var file = File.Create(appSpritePath);
                                                        file.Write(data, 0, data.Length);
                                                        file.Close();
                                                        AssetDatabase.Refresh();
                                                        hasAppArt = true;
                                                    }
                                                }
                                            }
                                        } else {
                                            hasAppArt = true;
                                        }
                                        
                                        if (hasAppArt) {
                                            appAtlas.Add(
                                                new Sprite[] { AssetDatabase.LoadAssetAtPath<Sprite>(appSpritePath) }
                                            );
                                            appmonData.App.Sprite = new AssetReferenceAtlasedSprite(appAtlasGUID);
                                            appmonData.App.Sprite.SubObjectName = appName.AddresableSafe();
                                        }
                                    } break;
                                    case "Type": {
                                        var typeIndex = appmonDB.Types.FindIndex(t => t.Name == propertyName);
                                        if (typeIndex >= 0) {
                                            appmonData.TypeIDs.Add(typeIndex);
                                        }
                                    } break;
                                    case "Grade": {
                                        var gradeIndex = appmonDB.Grades.FindIndex(t => t.Name == propertyName.Replace("\n", ""));
                                        if (gradeIndex >= 0) {
                                            appmonData.GradeIDs.Add(gradeIndex);
                                        }
                                    } break;
                                    case "Power": {
                                        if (int.TryParse(propertyName, out int power)) {
                                            appmonData.Powers.Add(power);
                                        }
                                    } break;
                                }
                            }
                            valueNode = valueNode.NextSibling;
                            fieldType = valueNode?.Name;
                        }
                    }

                    XmlNode dubNode = appmonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/table/tbody/tr/td/div/table/tbody/tr/td/div/table/tbody/tr/td/table/tbody/tr/td/table/tbody/tr/td[b='Dub:']");
                    appmonData.DubNames = new List<string>();
                    if (dubNode != null) {
                        XmlNode test = dubNode.NextSibling;
                        while ((test != null) && (test.InnerText == "")) {
                            test = test.NextSibling;
                        }
                        if (test != null) {
                            XmlNode child = test.FirstChild;
                            while (child != null) {
                                if (child.Name == "i") {
                                    appmonData.DubNames.Add(child.FirstChild?.InnerText ?? child.InnerText);
                                }
                                child = child.NextSibling;
                            }
                        }
                    }
                    
                    XmlNode debutYearNode = appmonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/table/tbody/tr/td/div/table/tbody/tr/td/table/tbody/tr/td/table/tbody/tr/td[contains(text(),'Year Active')]")?.NextSibling;
                    int.TryParse(debutYearNode?.InnerText, out appmonData.DebutYear);

                    XmlNode attackHeader = appmonSite.SelectSingleNode("//*[@id='Attack_Techniques']");
                    if (attackHeader?.ParentNode.NextSibling.Name == "table") {
                        XmlNodeList attacks = attackHeader?.ParentNode.NextSibling.FirstChild.ChildNodes;
                        for (int iAttack = 1; iAttack < attacks.Count; ++iAttack) {
                            XmlNode attackData = attacks.Item(iAttack);

                            if (string.IsNullOrEmpty(attackData.FirstChild.InnerText)) {
                                continue;
                            }

                            Attack attack = new Attack();
                            attack.Name = attackData.FirstChild.InnerText;
                            attack.Description = "";
                            XmlNodeList descriptionNodes = attackData.LastChild.ChildNodes;
                            for (int iNode = 0; iNode < descriptionNodes.Count; ++iNode) {
                                XmlNode descNode = descriptionNodes.Item(iNode);
                                if (descNode.Name != "sup") {
                                    attack.Description += descNode.InnerText;
                                }
                            }
                            
                            attack.DubNames = new List<string>();
                            appmonData.Attacks.Add(attack);
                        }
                    }

                    EditorUtility.SetDirty(appmonData);
                    AssetDatabase.SaveAssets();

                    if (hasArt) {
                        appmonsWithArt.Add((appmonData, appmonArtPath));
                    }
                        
                    addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(appmonDataPath).ToString(), dataGroup);
                } catch (Exception ex) {
                    Debug.LogError($"{appmonLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
                }
            }
        }

        int atlasCount = Mathf.CeilToInt((float)appmonsWithArt.Count / (float)AppmonsPerAtlas);
        int iAppmonArt = 0;
        for (int i = 0; i < atlasCount; i++) {
            string spriteAtlasPath = string.Format(SpriteAtlasXPath, i);
            SpriteAtlas spriteAtlas = new SpriteAtlas();
            UnityEngine.Object[] sprites = new UnityEngine.Object[Mathf.Min(AppmonsPerAtlas, appmonsWithArt.Count - (AppmonsPerAtlas * i))];
            for (int j = 0; j < sprites.Length; ++iAppmonArt, ++j) {
                sprites[j] = AssetDatabase.LoadAssetAtPath<Sprite>(appmonsWithArt[iAppmonArt].path);
            }
            TextureImporterPlatformSettings textureSettings = spriteAtlas.GetPlatformSettings("DefaultTexturePlatform");
            textureSettings.crunchedCompression = true;
            spriteAtlas.SetPlatformSettings(textureSettings);
            spriteAtlas.Add(sprites);
            AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            EditorUtility.SetDirty(appAtlas);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    
        for (int i = 0; i < atlasCount; i++) {
            string spriteAtlasPath = string.Format(SpriteAtlasXPath, i);
            string spriteAtlasGUID = AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString();
            addressablesSettings.CreateOrMoveEntry(spriteAtlasGUID, spriteAtlasGroup);

            int max = Mathf.Min((i + 1) * AppmonsPerAtlas, appmonsWithArt.Count);
            for (int iAppmon = i * AppmonsPerAtlas; iAppmon < max; ++iAppmon) {
                appmonsWithArt[iAppmon].appmon.Sprite = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                appmonsWithArt[iAppmon].appmon.Sprite.SubObjectName = appmonsWithArt[iAppmon].appmon.Name.AddresableSafe();
                try {
                    EditorUtility.SetDirty(appmonsWithArt[iAppmon].appmon);
                } catch (Exception ex) {
                    Debug.Log($"{iAppmon}(asset null: {appmonsWithArt[iAppmon].appmon == null}) - {ex.Message} \n {ex.StackTrace}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        GenerateAppmonList();
        GetEvolutions();

        AssetDatabase.Refresh();

        Debug.Log("Data Fetched");
    }

    [MenuItem("DigiDex/Appmon/Generate/List Asset File")]
    public static void GenerateAppmonList() {
        AssetDatabase.Refresh();
        AppmonDatabase appmonDB = GetAppmonDatabase();
        appmonDB.Appmons = new List<Appmon>();
        string[] paths = Directory.GetFiles(AppmonsDataPath, "*.asset");
        Array.Sort<string>(paths, (x, y) => x.CompareTo(y));
        for (int i = 0; i < paths.Length; i++) {
            Appmon appmonData = AssetDatabase.LoadAssetAtPath<Appmon>(paths[i]);
            appmonDB.Appmons.Add(appmonData);
        }
        
        DataCenter dataCenter = DataRetriever.GetCentralDatabase();
        dataCenter.AppmonDB = appmonDB;

        EditorUtility.SetDirty(dataCenter);
        EditorUtility.SetDirty(appmonDB);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var dbGroup = DataRetriever.GetOrAddAddressableGroup(DataRetriever.DBGroupName);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(AppmonDBPath).ToString(), dbGroup);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(DataRetriever.DataCenterPath).ToString(), dbGroup);

        Debug.Log("List Generated");
    }

    [MenuItem("DigiDex/Appmon/Generate/Type List")]
    public async static UniTask GenerateTypeList() {
        XmlDocument typeSite = await DataRetriever.GetSite(TypeListSubFix);
        XmlNodeList table = typeSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[2]/tbody/tr");
        string typesDataPath = AppmonDataPath + "Types";
        if (!Directory.Exists(typesDataPath)) {
            Directory.CreateDirectory(typesDataPath);
        }
        if (!Directory.Exists(TypesRemoteArtPath)) {
            Directory.CreateDirectory(TypesRemoteArtPath);
        }

        string spriteAtlasPath = TypesRemoteArtPath + "/Types.spriteatlas";
        SpriteAtlas spriteAtlas = new SpriteAtlas();
        AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string spriteAtlasGUID = AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString();

        if (!Directory.Exists(ArtAppsPath)) {
            Directory.CreateDirectory(ArtAppsPath);
        }

        List<AppmonType> types = new List<AppmonType>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(AppmonDataGroupName);
        for (int i = 1; i < table.Count; i++) {
            XmlNode typeData = table.Item(i);
            string typeName = typeData.ChildNodes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(typeName)) {
                typeName = typeName.Replace("\n", "");
                AppmonType type = null;
                string typeDataFilePath = typesDataPath + "/" + typeName + ".asset";
                if (!File.Exists(typeDataFilePath)) {
                    type = ScriptableObject.CreateInstance<AppmonType>();
                    AssetDatabase.CreateAsset(type, typeDataFilePath);
                } else {
                    type = AssetDatabase.LoadAssetAtPath<AppmonType>(typeDataFilePath);
                }

                type.Name = typeName;
                
                string typeArtPath = TypesRemoteArtPath + "/" + typeName.AddresableSafe() + ".png";
                bool hasArt = false;
                if (!File.Exists(typeArtPath)) {
                    string linkToImage = DataRetriever.WikimonBaseURL + typeData.ChildNodes.Item(2).FirstChild.FirstChild.Attributes.GetNamedItem("src").InnerText;
                    using (UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
                        await request.SendWebRequest();
                        if (request.result != UnityWebRequest.Result.ConnectionError) {
                            var data = request.downloadHandler.data;
                            var file = File.Create(typeArtPath);
                            file.Write(data, 0, data.Length);
                            file.Close();
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                            hasArt = true;
                        }
                    }
                } else {
                    hasArt = true;
                }

                if (hasArt) {
                    spriteAtlas.Add(new UnityEngine.Sprite[] { AssetDatabase.LoadAssetAtPath<Sprite>(typeArtPath) });
                    type.Sprite = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                    type.Sprite.SubObjectName = typeName.AddresableSafe();
                }
                EditorUtility.SetDirty(type);
                types.Add(type);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int i = 0; i < types.Count; i++) {
            string typeDataPath = typesDataPath + "/" + types[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(typeDataPath).ToString(), listGroup);
        }

        AssetDatabase.Refresh();

        var remoteArtGroup = DataRetriever.GetOrAddAddressableGroup(DataRetriever.RemoteArtGroupName);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString(), remoteArtGroup);

        AppmonDatabase appmonDB = GetAppmonDatabase();
        appmonDB.Types = types;
        EditorUtility.SetDirty(appmonDB);

        AssetDatabase.SaveAssets();
        
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    }

    
    [MenuItem("DigiDex/Appmon/Generate/Grade List")]
    public async static UniTask GenerateGradeList() {
        XmlDocument gradeSite = await DataRetriever.GetSite(GradeListSubFix);
        XmlNodeList table = gradeSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[3]/tbody/tr/td[1]");
        string gradesDataPath = AppmonDataPath + "Grades";
        if (!Directory.Exists(gradesDataPath)) {
            Directory.CreateDirectory(gradesDataPath);
        }

        List<AppmonGrade> grades = new List<AppmonGrade>();
        for (int i = 0; i < table.Count; i++) {
            XmlNode fieldData = table.Item(i);
            string gradeName = fieldData.ChildNodes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(gradeName)) {
                AppmonGrade grade = null;
                string gradeDataPath = gradesDataPath + "/" + gradeName + ".asset";
                if (!File.Exists(gradeDataPath)) {
                    grade = ScriptableObject.CreateInstance<AppmonGrade>();
                    AssetDatabase.CreateAsset(grade, gradeDataPath);
                } else {
                    grade = AssetDatabase.LoadAssetAtPath<AppmonGrade>(gradeDataPath);
                }

                grade.Name = gradeName;
                EditorUtility.SetDirty(grade);
                grades.Add(grade);
            }
        }
        AppmonDatabase appmonDB = GetAppmonDatabase();
        appmonDB.Grades = grades;
        EditorUtility.SetDirty(appmonDB);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(AppmonDataGroupName);
        for (int i = 0; i < grades.Count; i++) {
            string gradeDataPath = gradesDataPath + "/" + grades[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(gradeDataPath).ToString(), listGroup);
        }
    }
    
    [MenuItem("DigiDex/Appmon/Get Evolutions")]
    public static void GetEvolutions() {
        DataRetriever.GetEvolutions<Appmon>(GetAppmonDatabase(), AppmonsDataPath, AppmonEvolutionsDataPath, AppmonEvolutionDataGroupName);
    }
}
