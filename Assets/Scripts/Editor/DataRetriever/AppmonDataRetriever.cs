using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Cysharp.Threading.Tasks;
using HtmlAgilityPack;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.U2D;

public static class AppmonDataRetriever {
    public const string AppmonSpriteAtlasesGroupName = "Appmon Sprite Atlases";
    public const string AppmonListGroupName = "Appmon List";
    public const string AppmonDataGroupName = "Appmon Data";
    public const string AppmonEvolutionDataGroupName = "Appmon Evolution Data";
    public const string AppmonListSubFix = "/List_of_Appmon";
    public const string AttributeListSubFix = "/Appmon_(species)";
    public const string GradeListSubFix = "/Evolution_Stage";
    public const int AppmonsPerAtlas = 3;
    public static string ArtAppmonFolder => Path.Combine(DataRetriever.RemoteArtPath, "Appmon/");
    public static string ArtAppmonsPath => Path.Combine(ArtAppmonFolder, "Appmons/");
    public static string ArtAppsPath => Path.Combine(ArtAppmonFolder, "Apps/");
    public static string AppmonDataPath => Path.Combine(DataRetriever.DataPath, "Appmon/");
    public static string AppmonsDataPath => Path.Combine(AppmonDataPath, "Appmons");
    public static string AppmonEvolutionsDataPath => Path.Combine(AppmonDataPath, "Appmons/Evolutions");
    public static string AppmonDBPath => Path.Combine(AppmonDataPath, "Appmon Database.asset");
    public static string AppsRemoteArtPath => Path.Combine(ArtAppmonFolder, "Apps");
    public static string AttributesRemoteArtPath => Path.Combine(ArtAppmonFolder, "Attributes");
    public static string SpriteAtlasXPath => Path.Combine(ArtAppmonsPath, "Appmons ({0}).spriteatlas");

    public static AppmonDatabase GetAppmonDatabase() {
        if (!Directory.Exists(AppmonDataPath)) {
            Directory.CreateDirectory(AppmonDataPath);
        }

        AppmonDatabase appmonDB = DataRetriever.GetOrCreateScriptableObject<AppmonDatabase>(AppmonDBPath);

        return appmonDB;
    }

    [MenuItem("DigiDex/Appmon/Retrieve Data")]
    public static async void RetrieveData() {
        await GenerateAttributeList();
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

        List < (Appmon appmon, string path) > appmonsWithArt = new List < (Appmon appmon, string path) > ();

        string appAtlasPath = Path.Combine(AppsRemoteArtPath, "Apps.spriteatlas");
        SpriteAtlas appAtlas = new SpriteAtlas();
        AssetDatabase.CreateAsset(appAtlas, appAtlasPath);
        EditorUtility.SetDirty(appAtlas);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string appAtlasGUID = AssetDatabase.GUIDFromAssetPath(appAtlasPath).ToString();
        addressablesSettings.CreateOrMoveEntry(appAtlasGUID, spriteAtlasGroup);

        Dictionary<string, int> imagesToSkip = new Dictionary<string, int> { { "Biomon", 1 },
            { "Calcumon", 1 },
            { "Consulmon", 1 },
            { "Coordemon", 1 },
            { "Dantemon", 1 },
            { "Deusmon", 1 },
            { "Diarimon", 1 },
            { "Denpamon", 3 },
            { "Docmon", 1 },
            { "Dogamon", 1 },
            { "DoGatchmon", 1 },
            { "Dreammon", 1 },
            { "Ecomon", 3 },
            { "Gaiamon (Appmon)", 1 },
            { "Gatchmon", 1 },
            { "Hadesmon", 1 },
            { "Jetmon", 1 },
            { "Kakeimon", 1 },
            { "Kosomon", 1 },
            { "Mediamon", 2 },
            { "Medicmon", 1 },
            { "Messemon", 1 },
            { "Mirrormon", 2 },
            { "Musclemon", 2 },
            { "Musimon", 1 },
            { "Navimon (Appmon)", 1 },
            { "Offmon", 1 },
            { "Oujamon", 1 },
            { "Ouranosmon", 1 },
            { "Perorimon", 1 },
            { "Pokomon (Appmon)", 1 },
            { "Poseidomon", 1 },
            { "Puzzlemon", 1 },
            { "Racemon", 1 },
            { "Raidramon", 1 },
            { "Rebootmon", 3 },
            { "Roamon", 1 },
            { "Ropuremon", 1 },
            { "Rocketmon", 1 },
            { "Savemon", 1 },
            { "Sateramon", 1 },
            { "Setmon", 1 },
            { "Shutmon", 1 },
            { "Tarotmon", 2 },
            { "Trickmon", 1 },
            { "Tutomon", 1 },
            { "Uratekumon", 1 },
            { "Vegasmon", 1 },
            { "Warpmon", 1 },
            { "Warudamon", 1 },
        };

        if (!Directory.Exists(ArtAppmonsPath)) {
            Directory.CreateDirectory(ArtAppmonsPath);
        }
        if (!Directory.Exists(AppmonsDataPath)) {
            Directory.CreateDirectory(AppmonsDataPath);
        }
        if (!Directory.Exists(AppsRemoteArtPath)) {
            Directory.CreateDirectory(AppsRemoteArtPath);
        }

        HtmlDocument appmonListSite = await DataRetriever.GetSite(AppmonListSubFix);
        HtmlNodeCollection table = appmonListSite.DocumentNode.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        for (int i = 0; i < table.Count; i++) {
            string appmonLinkSubFix = table[i]?.Attributes[0]?.Value ?? "";

            if (!string.IsNullOrEmpty(appmonLinkSubFix)) {
                try {
                    HtmlDocument appmonSite = await DataRetriever.GetSite(appmonLinkSubFix);
                    string appmonName = appmonSite.DocumentNode.SelectSingleNode("//*[@id='firstHeading']").InnerText;
                    string appmonNameSafe = appmonName.AddresableSafe();
                    string appmonArtPath = Path.Combine(ArtAppmonsPath, appmonNameSafe + ".png");
                    string appmonDataPath = Path.Combine(AppmonsDataPath + "/" + appmonNameSafe + ".asset");

                    bool hasArt = false;
                    if (!File.Exists(appmonArtPath)) {  
                        HtmlNode image = appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div/div[2]/div[1]/div/div[2]/div/div/a");
                        if (image != null) {
                            string linkToImagePage = image.Attributes["href"].Value;
                            var hdImageSide = await DataRetriever.GetSite(linkToImagePage);
                            HtmlNode hdImage = null;
                            HtmlNodeCollection images = hdImageSide.DocumentNode.SelectNodes("//table[@class='wikitable filehistory']/tr/td[4]");
                            List < (HtmlNode node, int result) > imagesList = new List < (HtmlNode node, int result) > (images?.Count ?? 0);
                            for (int iNode = 0; iNode < images.Count; ++iNode) {
                                HtmlNode imageItem = images[iNode];
                                // We extract the image resolution and fetch the best one
                                string[] values = imageItem.InnerText.Split(' ');
                                int width;
                                int height;
                                if (int.TryParse(values[0], NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out width) &&
                                    int.TryParse(values[2], NumberStyles.AllowThousands, NumberFormatInfo.InvariantInfo, out height)
                                ) {
                                    int value = width * height;
                                    imagesList.Add((images[iNode], value));
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
                                    string linkToImage = DataRetriever.WikimonBaseURL + hdImage.Attributes["href"].Value;

                                    using(UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
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
                    appmonData.AttributeIDs = new List<int>();
                    appmonData.GradeIDs = new List<int>();
                    appmonData.Powers = new List<int>();

                    HtmlNode profileNode = appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td/p");

                    if (profileNode != null) {
                        if (profileNode.FirstChild?.Name == "span") {
                            // Remove the "Japanese/English" Toggle
                            profileNode.RemoveChild(profileNode.FirstChild);
                        }
                        appmonData.Profile = profileNode.InnerText.TrimEnd();
                    } else {
                        Debug.Log($"No profile found for {appmonNameSafe}");
                    }

                    HtmlNodeCollection properties = appmonSite.DocumentNode.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div/div[2]/div[2]/table/tbody/tr");
                    string lastCategory = "";
                    for (int iProperties = 0; iProperties < properties.Count; ++iProperties) {
                        HtmlNode dataNode = properties[iProperties].ChildNodes[1];
                        if (dataNode == null) {
                            continue;
                        }

                        string fieldType = dataNode.FirstChild?.Name;
                        if (fieldType == "b") {
                            lastCategory = dataNode.InnerText.TrimEnd();
                            dataNode = dataNode.NextSibling.NextSibling;
                            fieldType = dataNode?.FirstChild?.Name;
                        }

                        HtmlNode valueNode = dataNode.FirstChild;
                        while (valueNode != null) {
                            if (fieldType == "a" || fieldType == "#text" || fieldType == "font") {
                                string propertyName = valueNode.InnerText;

                                if (string.IsNullOrWhiteSpace(propertyName)) {
                                    valueNode = valueNode?.NextSibling;
                                    fieldType = valueNode?.Name;
                                    continue;
                                }

                                switch (lastCategory) {
                                    case "App Name":
                                        {
                                            string appName = propertyName;
                                            appmonData.App = new AppData { Name = appName };
                                            string appSpritePath = Path.Combine(AppsRemoteArtPath, appName.AddresableSafe() + ".png");
                                            bool hasAppArt = false;
                                            if (!File.Exists(appSpritePath)) {
                                                HtmlNode imageNode = appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div/div[2]/div[2]/table/tbody/tr[1]/th/img");
                                                if (imageNode != null) {
                                                    string linkToImage = DataRetriever.WikimonBaseURL + imageNode.Attributes["src"].Value;
                                                    using(UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
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
                                        }
                                        break;
                                    case "Attribute":
                                        {
                                            var attributeIndex = appmonDB.Attributes.FindIndex(t => t.Name == propertyName);
                                            if (attributeIndex >= 0) {
                                                appmonData.AttributeIDs.Add(attributeIndex);
                                            }
                                        }
                                        break;
                                    case "Grade":
                                        {
                                            var gradeIndex = appmonDB.Grades.FindIndex(t => t.Name == propertyName.Replace("\n", ""));
                                            if (gradeIndex >= 0) {
                                                appmonData.GradeIDs.Add(gradeIndex);
                                            }
                                        }
                                        break;
                                    case "Power":
                                        {
                                            if (int.TryParse(propertyName, out int power)) {
                                                appmonData.Powers.Add(power);
                                            }
                                        }
                                        break;
                                }
                            }
                            valueNode = valueNode?.NextSibling;
                            fieldType = valueNode?.Name;
                        }
                    }

                    HtmlNode dubNode = appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div/div/div/div/div/table/tbody/tr/td/div/table/tbody/tr/td/div/table/tbody/tr/td/table/tbody/tr/td/table/tbody/tr/td[b='Dub:']");
                    appmonData.DubNames = new List<string>();
                    if (dubNode != null) {
                        HtmlNode test = dubNode.NextSibling;
                        while ((test != null) && (test.InnerText == "")) {
                            test = test.NextSibling;
                        }
                        if (test != null) {
                            HtmlNode child = test.FirstChild;
                            while (child != null) {
                                if (child.Name == "i") {
                                    appmonData.DubNames.Add(child.FirstChild?.InnerText ?? child.InnerText);
                                }
                                child = child.NextSibling;
                            }
                        }
                    }

                    HtmlNode debutYearNode = appmonSite.DocumentNode.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[4]/table/tbody/tr[2]/td/table/tbody/tr/td/table/tbody/tr/td[contains(text(),'Year Active')]")?.NextSibling;
                    int.TryParse(debutYearNode?.NextSibling.InnerText.TrimEnd(), out appmonData.DebutYear);

                    appmonData.Attacks = new();
                    HtmlNode attackHeader = appmonSite.DocumentNode.SelectSingleNode("//*[@id='Attack_Techniques']");
                    if (attackHeader?.ParentNode.NextSibling.NextSibling.Name == "table") {
                        HtmlNodeCollection attacks = attackHeader?.ParentNode.NextSibling.NextSibling.ChildNodes[1].ChildNodes;
                        for (int iAttack = 1; iAttack < attacks.Count; ++iAttack) {
                            HtmlNode attackData = attacks[iAttack];

                            if (string.IsNullOrEmpty(attackData.FirstChild?.InnerText)) {
                                continue;
                            }

                            Attack attack = new Attack();
                            attack.Name = attackData.ChildNodes[1]?.InnerText.TrimEnd();
                            HtmlNodeCollection descriptionNodes = attackData.LastChild.ChildNodes;
                            if (descriptionNodes[0].Name == "div") {
                                attack.Description = descriptionNodes[^2].FirstChild?.FirstChild?.InnerText;
                            } else {
                                for (int iNode = 0; iNode < descriptionNodes.Count; ++iNode) {
                                    HtmlNode descNode = descriptionNodes[iNode];
                                    if (descNode.Name != "sup") {
                                        attack.Description += descNode.InnerText;
                                    }
                                }
                            }

                            attack.Description = attack.Description?.TrimEnd() ?? "";
                            HtmlNodeCollection dubNames = attackData.ChildNodes[^3].FirstChild?.ChildNodes;
                            
                            if (dubNames != null) {
                                attack.DubNames = new List<string>(dubNames.Count);
                                for (int iName = 0; iName < dubNames.Count; ++iName) {
                                    if (dubNames[iName].Name == "#text") {
                                        string[] names = dubNames[iName].InnerText.Split('/');
                                        foreach (var name in names) {
                                            if (attack.Name != name && !string.IsNullOrWhiteSpace(name)) {
                                                attack.DubNames.Add(name);
                                            }
                                        }
                                    }
                                }
                            }

                            appmonData.Attacks.Add(attack);
                        }
                        // HtmlNodeCollection attacks = attackHeader?.ParentNode.NextSibling.FirstChild.ChildNodes;
                        // for (int iAttack = 1; iAttack < attacks.Count; ++iAttack) {
                        //     HtmlNode attackData = attacks[iAttack];

                        //     if (string.IsNullOrEmpty(attackData.FirstChild.InnerText)) {
                        //         continue;
                        //     }

                        //     Attack attack = new Attack();
                        //     attack.Name = attackData.FirstChild.InnerText;
                        //     attack.Description = "";
                        //     HtmlNodeCollection descriptionNodes = attackData.LastChild.ChildNodes;
                        //     for (int iNode = 0; iNode < descriptionNodes.Count; ++iNode) {
                        //         HtmlNode descNode = descriptionNodes[iNode];
                        //         if (descNode.Name != "sup") {
                        //             attack.Description += descNode.InnerText;
                        //         }
                        //     }

                        //     attack.DubNames = new List<string>();
                        //     appmonData.Attacks.Add(attack);
                        // }
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

    [MenuItem("DigiDex/Appmon/Generate/Attribute List")]
    public async static UniTask GenerateAttributeList() {
        HtmlDocument attributeSite = await DataRetriever.GetSite(AttributeListSubFix);
        HtmlNodeCollection table = attributeSite.DocumentNode.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[7]/tbody/tr");
        string attributesDataPath = Path.Combine(AppmonDataPath, "Attributes");
        if (!Directory.Exists(attributesDataPath)) {
            Directory.CreateDirectory(attributesDataPath);
        }
        if (!Directory.Exists(AttributesRemoteArtPath)) {
            Directory.CreateDirectory(AttributesRemoteArtPath);
        }

        string spriteAtlasPath = Path.Combine(AttributesRemoteArtPath, "Attributes.spriteatlas");
        SpriteAtlas spriteAtlas = new SpriteAtlas();
        AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string spriteAtlasGUID = AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString();

        if (!Directory.Exists(ArtAppsPath)) {
            Directory.CreateDirectory(ArtAppsPath);
        }

        List<AppmonAttribute> attributes = new List<AppmonAttribute>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(AppmonDataGroupName);
        for (int i = 2; i < table.Count; i++) {
            HtmlNode attributeData = table[i];
            string attributeName = attributeData.ChildNodes[1]?.FirstChild?.InnerText.TrimEnd() ?? "";

            if (!string.IsNullOrEmpty(attributeName)) {
                attributeName = attributeName.Replace("\n", "");
                AppmonAttribute attribute = null;
                string attributeDataFilePath = Path.Combine(attributesDataPath, attributeName + ".asset");
                if (!File.Exists(attributeDataFilePath)) {
                    attribute = ScriptableObject.CreateInstance<AppmonAttribute>();
                    AssetDatabase.CreateAsset(attribute, attributeDataFilePath);
                } else {
                    attribute = AssetDatabase.LoadAssetAtPath<AppmonAttribute>(attributeDataFilePath);
                }

                attribute.Name = attributeName;

                string attributeArtPath = Path.Combine(AttributesRemoteArtPath, attributeName.AddresableSafe() + ".png");
                bool hasArt = false;
                if (!File.Exists(attributeArtPath)) {
                    string linkToImage = DataRetriever.WikimonBaseURL + attributeData.ChildNodes[5].FirstChild.FirstChild.Attributes["src"].Value;
                    using(UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
                        await request.SendWebRequest();
                        if (request.result != UnityWebRequest.Result.ConnectionError) {
                            var data = request.downloadHandler.data;
                            var file = File.Create(attributeArtPath);
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
                    spriteAtlas.Add(new UnityEngine.Sprite[] { AssetDatabase.LoadAssetAtPath<Sprite>(attributeArtPath) });
                    attribute.Sprite = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                    attribute.Sprite.SubObjectName = attributeName.AddresableSafe();
                }
                EditorUtility.SetDirty(attribute);
                attributes.Add(attribute);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int i = 0; i < attributes.Count; i++) {
            string attributeDataPath = Path.Combine(attributesDataPath, attributes[i].Name + ".asset");
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(attributeDataPath).ToString(), listGroup);
        }

        AssetDatabase.Refresh();

        var remoteArtGroup = DataRetriever.GetOrAddAddressableGroup(DataRetriever.RemoteArtGroupName);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString(), remoteArtGroup);

        AppmonDatabase appmonDB = GetAppmonDatabase();
        appmonDB.Attributes = attributes;
        EditorUtility.SetDirty(appmonDB);

        AssetDatabase.SaveAssets();

        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    }

    [MenuItem("DigiDex/Appmon/Generate/Grade List")]
    public async static UniTask GenerateGradeList() {
        HtmlDocument gradeSite = await DataRetriever.GetSite(GradeListSubFix);
        HtmlNodeCollection table = gradeSite.DocumentNode.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[4]/tbody/tr/th[1]/a");
        string gradesDataPath = Path.Combine(AppmonDataPath, "Grades");
        if (!Directory.Exists(gradesDataPath)) {
            Directory.CreateDirectory(gradesDataPath);
        }

        List<AppmonGrade> grades = new List<AppmonGrade>();
        for (int i = 0; i < table.Count; i++) {
            HtmlNode fieldData = table[i];
            string gradeName = fieldData.ChildNodes[0]?.InnerText.TrimEnd() ?? "";

            if (!string.IsNullOrEmpty(gradeName)) {
                AppmonGrade grade = null;
                string gradeDataPath = Path.Combine(gradesDataPath, gradeName + ".asset");
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
            string gradeDataPath = Path.Combine(gradesDataPath, grades[i].Name + ".asset");
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(gradeDataPath).ToString(), listGroup);
        }
    }

    [MenuItem("DigiDex/Appmon/Get Evolutions")]
    public static void GetEvolutions() {
        DataRetriever.GetEvolutions<Appmon>(GetAppmonDatabase(), AppmonsDataPath, AppmonEvolutionsDataPath, AppmonEvolutionDataGroupName);
    }
}