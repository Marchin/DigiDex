using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public static class DigimonDataRetriever {
    public const string DigimonSpriteAtlasesGroupName = "Digimon Sprite Atlases";
    public const string DigimonListGroupName = "Digimon List";
    public const string DigimonDataGroupName = "Digimon Data";
    public const string DigimonEvolutionDataGroupName = "Digimon Evolution Data";
    public const string DigimonListSubFix = "/List_of_Digimon";
    public const string FieldListSubFix = "/Field";
    public const string AttributeListSubFix = "/Attribute";
    public const string TypeListSubFix = "/Type";
    public const string LevelListSubFix = "/Evolution_Stage";
    public const string DigimonGroupListSubFix = "/Group";
    public const int DigimonsPerAtlas = 3;
    public const string ArtDigimonsPath = DataRetriever.RemoteArtPath + "Digimons/";
    public const string DigimonDataPath = DataRetriever.DataPath + "Digimon/";
    public const string DigimonsDataPath = DigimonDataPath + "Digimons/";
    public const string DigimonEvolutionsDataPath = DigimonsDataPath + "Evolutions/";
    public const string DigimonDBPath = DigimonDataPath + "Digimon Database.asset";
    public const string FieldsRemoteArtPath = DataRetriever.RemoteArtPath + "Fields";
    public const string FieldsLocalArtPath = DataRetriever.LocalArtPath + "Fields";
    public const string FieldsDataPath = DigimonDataPath + "Fields";
    public const string SpriteAtlasXPath = ArtDigimonsPath + "Digimons ({0}).spriteatlas";
    
    public static DigimonDatabase GetDigimonDatabase() {
        if (!Directory.Exists(DigimonDataPath)) {
            Directory.CreateDirectory(DigimonDataPath);
        }

        DigimonDatabase digimonDB = DataRetriever.GetOrCreateScriptableObject<DigimonDatabase>(DigimonDBPath);

        return digimonDB;
    }

    [MenuItem("DigiDex/Digimon/Retrieve Data")]
    public static async void RetrieveData() {
        if (!Directory.Exists(DigimonDataPath)) {
            Directory.CreateDirectory(DigimonDataPath);
        }

        await GenerateFieldList();
        await GenerateAttributeList();
        await GenerateTypeList();
        await GenerateLevelList();
        await GenerateDigimonGroupList();

        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        if (!Directory.Exists(DigimonsDataPath)) {
            Directory.CreateDirectory(DigimonsDataPath);
        }

        var dataGroup = DataRetriever.GetOrAddAddressableGroup(DigimonListGroupName);

        var spriteAtlasGroup = DataRetriever.GetOrAddAddressableGroup(DigimonSpriteAtlasesGroupName);

        DigimonDatabase digimonDB = GetDigimonDatabase();

        List<(Digimon digimon, string path)> digimonsWithArt = new List<(Digimon digimon, string path)>();


        XmlDocument digimonListSite = await DataRetriever.GetSite(DigimonListSubFix);
        XmlNodeList table = digimonListSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        for (int i = 0; i < table.Count; i++) {
            string digimonLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(digimonLinkSubFix)) {
                try {
                    XmlDocument digimonSite = await DataRetriever.GetSite(digimonLinkSubFix);
                    string digimonName = digimonSite.SelectSingleNode("//*[@id='firstHeading']").InnerText;
                    string digimonNameSafe = digimonName.AddresableSafe();
                    string digimonArtPath = ArtDigimonsPath + digimonNameSafe + ".png";
                    string digimonDataPath = DigimonsDataPath + "/" + digimonNameSafe + ".asset";

                    if (!Directory.Exists(ArtDigimonsPath)) {
                        Directory.CreateDirectory(ArtDigimonsPath);
                    }
                
                    if (!Directory.Exists(DigimonsDataPath)) {
                        Directory.CreateDirectory(DigimonsDataPath);
                    }

                    bool hasArt = false;
                    if (!File.Exists(digimonArtPath)) {
                        XmlNode image = digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[1]/td/div/div/a/img");
                        if (image != null) {
                            string linkToImage = DataRetriever.WikimonBaseURL + image.Attributes.GetNamedItem("src").InnerText;
                            using (UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
                                await request.SendWebRequest();
                                if (request.result != UnityWebRequest.Result.ConnectionError) {
                                    var data = request.downloadHandler.data;
                                    var file = File.Create(digimonArtPath);
                                    file.Write(data, 0, data.Length);
                                    file.Close();
                                    AssetDatabase.Refresh();
                                    hasArt = true;
                                }
                            }
                        }
                    } else {
                        hasArt = true;
                    }
                    
                    Digimon digimonData = null;
                    if (!File.Exists(digimonDataPath)) {
                        digimonData = ScriptableObject.CreateInstance<Digimon>();
                        AssetDatabase.CreateAsset(digimonData, digimonDataPath);
                    } else {
                        digimonData = AssetDatabase.LoadAssetAtPath<Digimon>(digimonDataPath);
                    }

                    digimonData.LinkSubFix = digimonLinkSubFix;
                    digimonData.Hash = Hash128.Compute(digimonData.LinkSubFix);
                    digimonData.Name = digimonName;

                    XmlNode profileNode = digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                        digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td/p");

                    if (profileNode != null) {
                        if (profileNode.FirstChild?.LocalName == "span") {
                            // Remove the "Japanese/English" Toggle
                            profileNode.RemoveChild(profileNode.FirstChild);
                        }
                        digimonData.Profile = profileNode.InnerText;
                    } else {
                        Debug.Log($"No profile found for {digimonNameSafe}");
                    }

                    if (hasArt) {
                        digimonsWithArt.Add((digimonData, digimonArtPath));
                    }
                            
                    digimonData.AttributeIDs = new List<int>();
                    digimonData.FieldIDs = new List<int>();
                    digimonData.TypeIDs = new List<int>();
                    digimonData.LevelIDs = new List<int>();
                    digimonData.GroupIDs = new List<int>();
                    XmlNodeList properties = digimonSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr");
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
                        
                        if (fieldType == "a") {
                            string propertyName = dataNode.FirstChild?.InnerText;

                            switch (lastCategory) {
                                case "Attribute": {
                                    int index = digimonDB.Attributes.FindIndex(a => a.Name == propertyName);
                                    if (index >= 0) {
                                        digimonData.AttributeIDs.Add(index);
                                    }
                                } break;
                                case "Field": {
                                    int index = digimonDB.Fields.FindIndex(f => f.Name == propertyName);
                                    if (index >= 0) {
                                        digimonData.FieldIDs.Add(index);
                                        continue;
                                    }
                                } break;
                                case "Type": {
                                    int index = digimonDB.Types.FindIndex(t => t.Name == propertyName);
                                    if (index >= 0) {
                                        digimonData.TypeIDs.Add(index);
                                    }
                                } break;
                                case "Level": {
                                    int index = digimonDB.Levels.FindIndex(l => l.Name == propertyName);
                                    if (index >= 0) {
                                        digimonData.LevelIDs.Add(index);
                                    }
                                } break;
                                case "Group": {
                                    int index = digimonDB.Groups.FindIndex(g => g.Name == propertyName);
                                    if (index >= 0) {
                                        digimonData.GroupIDs.Add(index);
                                    }
                                } break;
                            }
                        }
                    }

                    XmlNode dubNode = digimonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/table/tbody/tr/td/div/table/tbody/tr/td/div/table/tbody/tr/td/table/tbody/tr/td/table/tbody/tr/td[b='Dub:']");
                    digimonData.DubNames = new List<string>();
                    if (dubNode != null) {
                        XmlNode test = dubNode.NextSibling;
                        while ((test != null) && (test.InnerText == "")) {
                            test = test.NextSibling;
                        }
                        if (test != null) {
                            XmlNode child = test.FirstChild;
                            while (child != null) {
                                if (child.Name == "i") {
                                    digimonData.DubNames.Add(child.FirstChild?.InnerText ?? child.InnerText);
                                }
                                child = child.NextSibling;
                            }
                        }
                    }
                    
                    XmlNode debutYearNode = digimonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/table/tbody/tr/td/div/table/tbody/tr/td/table/tbody/tr/td/table/tbody/tr/td[contains(text(),'Year Active')]")?.NextSibling;
                    int.TryParse(debutYearNode?.InnerText, out digimonData.DebutYear);

                    EditorUtility.SetDirty(digimonData);
                    AssetDatabase.SaveAssets();

                    addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(digimonDataPath).ToString(), dataGroup);
                } catch (Exception ex) {
                    Debug.LogError($"{digimonLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
                }
            }
        }


        int atlasCount = Mathf.CeilToInt((float)digimonsWithArt.Count / (float)DigimonsPerAtlas);
        int iDigimonArt = 0;
        for (int i = 0; i < atlasCount; i++) {
            string spriteAtlasPath = string.Format(SpriteAtlasXPath, i);
            SpriteAtlas spriteAtlas = new SpriteAtlas();
            UnityEngine.Object[] sprites = new UnityEngine.Object[Mathf.Min(DigimonsPerAtlas, digimonsWithArt.Count - (DigimonsPerAtlas * i))];
            for (int j = 0; j < sprites.Length; ++iDigimonArt, ++j) {
                sprites[j] = AssetDatabase.LoadAssetAtPath<Sprite>(digimonsWithArt[iDigimonArt].path);
            }
            spriteAtlas.Add(sprites);
            AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    
        for (int i = 0; i < atlasCount; i++) {
            string spriteAtlasPath = string.Format(SpriteAtlasXPath, i);
            string spriteAtlasGUID = AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString();
            addressablesSettings.CreateOrMoveEntry(spriteAtlasGUID, spriteAtlasGroup);

            int max = Mathf.Min((i + 1) * DigimonsPerAtlas, digimonsWithArt.Count);
            for (int iDigimon = i * DigimonsPerAtlas; iDigimon < max; ++iDigimon) {
                digimonsWithArt[iDigimon].digimon.Sprite = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                digimonsWithArt[iDigimon].digimon.Sprite.SubObjectName = digimonsWithArt[iDigimon].digimon.Name.AddresableSafe();
                try {
                    EditorUtility.SetDirty(digimonsWithArt[iDigimon].digimon);
                } catch (Exception ex) {
                    Debug.Log($"{iDigimon}(asset null: {digimonsWithArt[iDigimon].digimon == null}) - {ex.Message} \n {ex.StackTrace}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        GenerateDigimonList();
        GetEvolutions();

        Debug.Log("Data Fetched");
    }

    [MenuItem("DigiDex/Digimon/Generate/List Asset File")]
    public static void GenerateDigimonList() {
        AssetDatabase.Refresh();
        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Digimons = new List<Digimon>();
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path.Replace(".asset", string.Empty)).ToArray();
        for (int i = 0; i < paths.Length; i++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath<Digimon>(paths[i]);
            digimonDB.Digimons.Add(digimonData);
        }
        
        DataCenter dataCenter = DataRetriever.GetCentralDatabase();
        dataCenter.DigimonDB = digimonDB;

        EditorUtility.SetDirty(dataCenter);
        EditorUtility.SetDirty(digimonDB);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var dbGroup = DataRetriever.GetOrAddAddressableGroup(DataRetriever.DBGroupName);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(DigimonDBPath).ToString(), dbGroup);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(DataRetriever.DataCenterPath).ToString(), dbGroup);

        Debug.Log("List Generated");
    }

    [MenuItem("DigiDex/Digimon/Generate/Field List")]
    public async static UniTask GenerateFieldList() {
        XmlDocument fieldSite = await DataRetriever.GetSite(FieldListSubFix);
        XmlNodeList table = fieldSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table/tbody/tr");
        string fieldsDataPath = DigimonDataPath + "Fields";
        if (!Directory.Exists(fieldsDataPath)) {
            Directory.CreateDirectory(fieldsDataPath);
        }

        List<Field> fields = new List<Field>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(DigimonDataGroupName);
        for (int i = 1; i < table.Count; i++) {
            XmlNode fieldData = table.Item(i);
            string fieldName = fieldData.ChildNodes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(fieldName)) {
                Field field = null;
                string fieldDataPath = fieldsDataPath + "/" + fieldName + ".asset";
                if (!File.Exists(fieldDataPath)) {
                    field = ScriptableObject.CreateInstance<Field>();
                    AssetDatabase.CreateAsset(field, fieldDataPath);
                } else {
                    field = AssetDatabase.LoadAssetAtPath<Field>(fieldDataPath);
                }

                field.Name = fieldName;
                field.Description = fieldData?.ChildNodes.Item(4)?.InnerText ?? "";
                EditorUtility.SetDirty(field);
                fields.Add(field);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int i = 0; i < fields.Count; i++) {
            string fieldDataPath = fieldsDataPath + "/" + fields[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(fieldDataPath).ToString(), listGroup);
        }

        // Art
        AssetDatabase.Refresh();
        List<Field> missingArt = new List<Field>();
        string[] spritePaths = Directory.GetFiles(FieldsLocalArtPath, "*.png");
        if (spritePaths.Length > 0) {
            // We assume there's only one
            string localFieldsSpriteAtlasPath = FieldsLocalArtPath + "/Fields.spriteatlas";
            SpriteAtlas fieldAtlas = new SpriteAtlas();
            SpriteAtlasPackingSettings packingSettings = new SpriteAtlasPackingSettings();
            packingSettings.enableRotation = false;
            packingSettings.enableTightPacking = false;
            fieldAtlas.SetPackingSettings(packingSettings);
            Sprite[] sprites = new Sprite[fieldAtlas.spriteCount];
            AssetDatabase.CreateAsset(fieldAtlas, localFieldsSpriteAtlasPath);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string spriteAtlasGUID = AssetDatabase.GUIDFromAssetPath(localFieldsSpriteAtlasPath).ToString();
            var localArtGroup = DataRetriever.GetOrAddAddressableGroup(DataRetriever.LocalArtGroupName);
            var entry = addressablesSettings.CreateOrMoveEntry(
                spriteAtlasGUID, 
                localArtGroup);

            for (int iField = 0; iField < fields.Count; ++iField) {
                string spritePath = spritePaths.FirstOrDefault(s => Path.GetFileNameWithoutExtension(s) == fields[iField].Name);
                if (!string.IsNullOrEmpty(spritePath)) {
                    fieldAtlas.Add(new UnityEngine.Object[] { AssetDatabase.LoadAssetAtPath<Sprite>(spritePath) });
                    fields[iField].Sprite = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                    fields[iField].Sprite.SubObjectName = Path.GetFileNameWithoutExtension(spritePath).AddresableSafe();
                } else {
                    missingArt.Add(fields[iField]);
                }
            }
        } else {
            missingArt = fields;
        }
        
        if (missingArt.Count > 0) {
            if (!Directory.Exists(FieldsRemoteArtPath)) {
                Directory.CreateDirectory(FieldsRemoteArtPath);
            }

            string spriteAtlasPath = FieldsRemoteArtPath + ".spriteatlas";
            SpriteAtlas spriteAtlas = new SpriteAtlas();
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(FieldsRemoteArtPath);
            spriteAtlas.Add(new UnityEngine.Object[] { folder });
            AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var remoteArtGroup = DataRetriever.GetOrAddAddressableGroup(DataRetriever.RemoteArtGroupName);
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString(), remoteArtGroup);

            XmlNodeList images = fieldSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/p[6]/a");
            for (int i = 0; i < images.Count; i++) {
                XmlNode image = images.Item(i);
                string fieldName = image.Attributes.GetNamedItem("title").InnerText.Replace("Category:", string.Empty);
                
                var field = fields.Find(f => f.Name == fieldName);
                if (field != null) {
                    string linkToImage = DataRetriever.WikimonBaseURL + image.FirstChild.Attributes.GetNamedItem("src").InnerText;
                    string fieldArtPath = FieldsRemoteArtPath + "/" + fieldName + ".png";
                    
                    if (!File.Exists(fieldArtPath)) {
                        using (UnityWebRequest request = UnityWebRequest.Get(linkToImage)) {
                            await request.SendWebRequest();
                            if (request.result != UnityWebRequest.Result.ConnectionError) {
                                var data = request.downloadHandler.data;
                                var file = File.Create(fieldArtPath);
                                file.Write(data, 0, data.Length);
                                file.Close();
                                AssetDatabase.Refresh();
                            }
                        }
                    }
                    
                    field.Sprite = new AssetReferenceAtlasedSprite(AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString());
                    field.Sprite.SubObjectName = fieldName.AddresableSafe();
                }
            }
        }
        
        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Fields = fields;
        EditorUtility.SetDirty(digimonDB);

        AssetDatabase.SaveAssets();
        
        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    }

    [MenuItem("DigiDex/Digimon/Couple Digimons With Properties")]
    public static async void CoupleDigimonData() {
        DigimonDatabase digimonDB = GetDigimonDatabase();
        
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path).ToArray();
        for (int iDigimon = 0; iDigimon < paths.Length; iDigimon++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath<Digimon>(paths[iDigimon]);
            XmlDocument digimonSite = await DataRetriever.GetSite(digimonData.LinkSubFix);

            digimonData.AttributeIDs = new List<int>();
            digimonData.FieldIDs = new List<int>();
            digimonData.TypeIDs = new List<int>();
            digimonData.LevelIDs = new List<int>();
            XmlNodeList properties = digimonSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr");
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
                
                if (fieldType == "a") {
                    string propertyName = dataNode.FirstChild?.InnerText;

                    switch (lastCategory) {
                        case "Attribute": {
                            int attributeIndex = digimonDB.Attributes.FindIndex(a => a.Name == propertyName);
                            if (attributeIndex >= 0) {
                                digimonData.AttributeIDs.Add(attributeIndex);
                                continue;
                            }
                        } break;
                        case "Field": {
                            int fieldIndex = digimonDB.Fields.FindIndex(f => f.Name == propertyName);
                            if (fieldIndex >= 0) {
                                digimonData.FieldIDs.Add(fieldIndex);
                                continue;
                            }
                        } break;
                        case "Type": {
                            int typeIndex = digimonDB.Types.FindIndex(f => f.Name == propertyName);
                            if (typeIndex >= 0) {
                                digimonData.TypeIDs.Add(typeIndex);
                                continue;
                            }
                        } break;
                        case "Level": {
                            int levelIndex = digimonDB.Levels.FindIndex(f => f.Name == propertyName);
                            if (levelIndex >= 0) {
                                digimonData.LevelIDs.Add(levelIndex);
                                continue;
                            }
                        } break;
                        case "Group": {
                            int index = digimonDB.Groups.FindIndex(g => g.Name == propertyName);
                            if (index >= 0) {
                                digimonData.GroupIDs.Add(index);
                                continue;
                            }
                        } break;
                    }
                }
            }
            EditorUtility.SetDirty(digimonData);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("DigiDex/Digimon/Generate/Attribute List")]
    public static async UniTask GenerateAttributeList() {
        XmlDocument attributeSite = await DataRetriever.GetSite(AttributeListSubFix);
        XmlNodeList table = attributeSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table/tbody/tr/td/a");
        string attributesDataPath = DigimonDataPath + "Attributes";
        if (!Directory.Exists(attributesDataPath)) {
            Directory.CreateDirectory(attributesDataPath);
        }

        List<Attribute> attributes = new List<Attribute>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(DigimonDataGroupName);
        for (int i = 0; i < table.Count; i++) {
            XmlNode fieldData = table.Item(i);
            string attributeName = fieldData.ChildNodes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(attributeName)) {
                Attribute attribute = null;
                string attributeDataPath = attributesDataPath + "/" + attributeName + ".asset";
                if (!File.Exists(attributeDataPath)) {
                    attribute = ScriptableObject.CreateInstance<Attribute>();
                    AssetDatabase.CreateAsset(attribute, attributeDataPath);
                } else {
                    attribute = AssetDatabase.LoadAssetAtPath<Attribute>(attributeDataPath);
                }

                attribute.Name = attributeName;
                EditorUtility.SetDirty(attribute);
                attributes.Add(attribute);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int i = 0; i < attributes.Count; i++) {
            string attributeDataPath = attributesDataPath + "/" + attributes[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(attributeDataPath).ToString(), listGroup);
        }

        AssetDatabase.Refresh();

        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Attributes = attributes;
        EditorUtility.SetDirty(digimonDB);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("DigiDex/Digimon/Generate/Type List")]
    public static async UniTask GenerateTypeList() {
        XmlDocument typeSite = await DataRetriever.GetSite(TypeListSubFix);
        XmlNodeList table = typeSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table/tbody/tr/td[1]/b/a");
        string typesDataPath = DigimonDataPath + "Types";
        if (!Directory.Exists(typesDataPath)) {
            Directory.CreateDirectory(typesDataPath);
        }

        List<DigimonType> types = new List<DigimonType>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(DigimonDataGroupName);
        for (int i = 0; i < table.Count; i++) {
            XmlNode fieldData = table.Item(i);
            string typeName = fieldData.ChildNodes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(typeName)) {
                DigimonType type = null;
                string typeDataPath = typesDataPath + "/" + typeName + ".asset";
                if (!File.Exists(typeDataPath)) {
                    type = ScriptableObject.CreateInstance<DigimonType>();
                    AssetDatabase.CreateAsset(type, typeDataPath);
                } else {
                    type = AssetDatabase.LoadAssetAtPath<DigimonType>(typeDataPath);
                }

                type.Name = typeName;
                EditorUtility.SetDirty(type);
                types.Add(type);
            }
        }
        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Types = types;
        EditorUtility.SetDirty(digimonDB);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        for (int i = 0; i < types.Count; i++) {
            string typeDataPath = typesDataPath + "/" + types[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(typeDataPath).ToString(), listGroup);
        }
    }

    [MenuItem("DigiDex/Digimon/Generate/Level List")]
    public static async UniTask GenerateLevelList() {
        XmlDocument levelSite = await DataRetriever.GetSite(LevelListSubFix);
        XmlNodeList table = levelSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        string levelsDataPath = DigimonDataPath + "Levels";
        if (!Directory.Exists(levelsDataPath)) {
            Directory.CreateDirectory(levelsDataPath);
        }

        List<Level> levels = new List<Level>();
        for (int i = 0; i < table.Count; i++) {
            XmlNode fieldData = table.Item(i);
            string levelName = fieldData.ChildNodes.Item(0)?.InnerText ?? "";

            if (levelName == "Digitama" || levelName == "Super Ultimate") {
                continue;
            }

            if (!string.IsNullOrEmpty(levelName)) {
                Level level = null;
                string levelDataPath = levelsDataPath + "/" + levelName + ".asset";
                if (!File.Exists(levelDataPath)) {
                    level = ScriptableObject.CreateInstance<Level>();
                    AssetDatabase.CreateAsset(level, levelDataPath);
                } else {
                    level = AssetDatabase.LoadAssetAtPath<Level>(levelDataPath);
                }

                level.Name = levelName;
                EditorUtility.SetDirty(level);
                levels.Add(level);
            }
        }
        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Levels = levels;
        EditorUtility.SetDirty(digimonDB);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(DigimonDataGroupName);
        for (int i = 0; i < levels.Count; i++) {
            string levelDataPath = levelsDataPath + "/" + levels[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(levelDataPath).ToString(), listGroup);
        }
    }
    
    [MenuItem("DigiDex/Digimon/Generate/Digimon Group List")]
    public static async UniTask GenerateDigimonGroupList() {
        XmlDocument levelSite = await DataRetriever.GetSite(DigimonGroupListSubFix);
        XmlNodeList table = levelSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr");
        string digimonGroupsDataPath = DigimonDataPath + "Groups";
        if (!Directory.Exists(digimonGroupsDataPath)) {
            Directory.CreateDirectory(digimonGroupsDataPath);
        }

        List<DigimonGroup> groups = new List<DigimonGroup>();
        for (int i = 0; i < table.Count; i++) {
            XmlNode groupData = table.Item(i);

            if ((groupData.FirstChild == null) || (groupData.FirstChild.Name != "td")) {
                continue;
            }

            string groupName = groupData.FirstChild?.InnerText ?? "";

            if (!string.IsNullOrEmpty(groupName)) {
                DigimonGroup group = null;
                string groupDataPath = digimonGroupsDataPath + "/" + groupName + ".asset";
                if (!File.Exists(groupDataPath)) {
                    group = ScriptableObject.CreateInstance<DigimonGroup>();
                    AssetDatabase.CreateAsset(group, groupDataPath);
                } else {
                    group = AssetDatabase.LoadAssetAtPath<DigimonGroup>(groupDataPath);
                }

                group.Name = groupName;
                group.Description = groupData?.LastChild.InnerText ?? "";
                EditorUtility.SetDirty(group);
                groups.Add(group);
            }
        }
        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Groups = groups;
        EditorUtility.SetDirty(digimonDB);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = DataRetriever.GetOrAddAddressableGroup(DigimonDataGroupName);
        for (int i = 0; i < groups.Count; i++) {
            string groupDataPath = digimonGroupsDataPath + "/" + groups[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(groupDataPath).ToString(), listGroup);
        }
    }

    [MenuItem("DigiDex/Digimon/Get Evolutions")]
    public static void GetEvolutions() {
        DataRetriever.GetEvolutions<Digimon>(GetDigimonDatabase(), DigimonsDataPath, DigimonEvolutionsDataPath, DigimonEvolutionDataGroupName);
    }
}
