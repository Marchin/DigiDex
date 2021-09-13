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
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public static class DataRetriever {
    const string ArtPath = "Assets/Remote/Art/";
    const string DataPath = "Assets/Remote/Data/";
    const string DigimonSpriteAtlasesGroupName = "Digimon Sprite Atlases";
    const string GeneralSpriteAtlasesGroupName = "General Sprite Atlases";
    const string DigimonListGroupName = "Digimon List";
    const string DigimonDataGroupName = "Digimon Data";
    const string DigimonEvolutionDataGroupName = "Digimon Evolution Data";
    const string DBGroupName = "Databases";
    const string WikimonBaseURL = "https://wikimon.net";
    const string DigimonListURL = WikimonBaseURL + "/List_of_Digimon";
    const string FieldListURL = WikimonBaseURL + "/Field";
    const string AttributeListURL = WikimonBaseURL + "/Attribute";
    const string TypeListURL = WikimonBaseURL + "/Type";
    const string LevelListURL = WikimonBaseURL + "/Evolution_Stage";
    const int DigimonsPerAtlas = 16;
    const string ArtDigimonsPathX = ArtPath + "Digimons/Digimon({0})";
    const string DigimonsDataPath = DataPath + "Digimons";
    const string DigimonEvolutionsDataPath = DataPath + "Digimons/Evolutions";
    const string CentralDBPath = DataPath + "Central Database.asset";
    const string DigimonDBPath = DataPath + "Digimon Database.asset";
    const string FieldsArtPath = ArtPath + "Fields";
    const string FieldsDataPath = DataPath + "Fields";

    private static AddressableAssetGroup GetOrAddAddressableGroup(string name) {
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        AddressableAssetGroup group = addressablesSettings.groups.Find(g => g.Name == name);
        if (group == null) {
            group = addressablesSettings.CreateGroup(name, false, false, false, null);
        }
        return group;
    }

    [MenuItem("DigiDex/Retrieve Data")]
    public static async void RetrieveData() {
        // GenerateFieldList();
        // GenerateAttributeList();
        // GenerateTypeList();
        // GenerateLevelList();

        // TODO: Add the new images either in the last folder or on a new one depending on the wether the last folder is full
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        if (!Directory.Exists(DigimonsDataPath)) {
            Directory.CreateDirectory(DigimonsDataPath);
        }

        var dataGroup = GetOrAddAddressableGroup(DigimonListGroupName);

        var spriteAtlasGroup = GetOrAddAddressableGroup(DigimonSpriteAtlasesGroupName);

        DigimonDatabase digimonDB = GetDigimonDatabase();

        List<Digimon> digimonsWithArt = new List<Digimon>();

        XmlDocument digimonListSite = new XmlDocument();
        digimonListSite.Load(DigimonListURL);
        XmlNodeList table = digimonListSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        for (int i = 0; i < table.Count; i++) {
            string digimonLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(digimonLinkSubFix)) {
                try {
                    string digimonLink = WikimonBaseURL + digimonLinkSubFix;

                    XmlDocument digimonSite = new XmlDocument();
                    digimonSite.Load(digimonLink);

                    // Sometimes name variants are used for the list, we look for the name used in the profile
                    XmlNode redirectNode = digimonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/div/ul[@class='redirectText']/li/a");
                    while (redirectNode != null) {
                        string newLinkSubFix = redirectNode.Attributes.GetNamedItem("href").InnerText;
                        Debug.Log($"Redirecting from {digimonLinkSubFix} to {newLinkSubFix}");
                        digimonLinkSubFix = newLinkSubFix;
                        digimonLink = WikimonBaseURL + digimonLinkSubFix;
                        digimonSite.Load(digimonLink);
                        redirectNode = digimonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/div/ul[@class='redirectText']/li/a");
                    }
                    
                    string artPath = string.Format(ArtDigimonsPathX, digimonsWithArt.Count / DigimonsPerAtlas);
                    string digimonName = digimonSite.SelectSingleNode("//*[@id='firstHeading']").InnerText;
                    string digimonNameSafe = digimonName.AddresableSafe();
                    string digimonArtPath = artPath + "/" + digimonNameSafe + ".png";
                    string digimonDataPath = DigimonsDataPath + "/" + digimonNameSafe + ".asset";

                    if (!Directory.Exists(artPath)) {
                        Directory.CreateDirectory(artPath);
                    }
                
                    if (!Directory.Exists(DigimonsDataPath)) {
                        Directory.CreateDirectory(DigimonsDataPath);
                    }

                    bool hasArt = false;
                    if (!File.Exists(digimonArtPath)) {
                        XmlNode image = digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[1]/td/div/div/a/img");
                        if (image != null) {
                            string linkToImage = WikimonBaseURL + image.Attributes.GetNamedItem("src").InnerText;
                            bool isPNG = linkToImage.ToLower().EndsWith(".png");
                            bool isJPG = linkToImage.ToLower().EndsWith(".jpg");
                            if (isPNG || isJPG) {
                                using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(linkToImage)) {
                                    await textureRequest.SendWebRequest();
                                    if (textureRequest.result != UnityWebRequest.Result.ConnectionError) {
                                        var texture = DownloadHandlerTexture.GetContent(textureRequest);
                                        var data = isPNG ? texture.EncodeToPNG() : texture.EncodeToJPG();
                                        var file = File.Create(digimonArtPath);
                                        file.Write(data, 0, data.Length);
                                        file.Close();
                                        AssetDatabase.Refresh();
                                        hasArt = true;
                                    }
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
                        digimonData = AssetDatabase.LoadAssetAtPath(digimonDataPath, typeof(Digimon)) as Digimon;
                    }

                    digimonData.LinkSubFix = digimonLinkSubFix;
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
                        digimonData.ProfileData = profileNode.InnerText;
                    } else {
                        Debug.Log($"No profile found for {digimonNameSafe}");
                    }

                    if (hasArt) {
                        digimonsWithArt.Add(digimonData);
                    }
                    
                    digimonData.AttributeIDs = new List<int>();
                    digimonData.FieldIDs = new List<int>();
                    digimonData.TypeIDs = new List<int>();
                    digimonData.LevelIDs = new List<int>();
                    XmlNodeList properties = digimonSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[5]/div[1]/ul/li/a");
                    for (int iProperties = 0; iProperties < properties.Count; ++iProperties) {
                        string attributeLessName = properties.Item(iProperties).InnerText.Replace(" Attribute", string.Empty);
                        int attributeIndex = digimonDB.Attributes.FindIndex(a => a.Name == attributeLessName);
                        if (attributeIndex >= 0) {
                            digimonData.AttributeIDs.Add(attributeIndex);
                            continue;
                        }
                        string fieldLessName = properties.Item(iProperties).InnerText.Replace(" Field", string.Empty);
                        int fieldIndex = digimonDB.Fields.FindIndex(f => f.Name == fieldLessName);
                        if (fieldIndex >= 0) {
                            digimonData.FieldIDs.Add(fieldIndex);
                            continue;
                        }
                        string typeLessName = properties.Item(iProperties).InnerText.Replace(" Type", string.Empty);
                        int typeIndex = digimonDB.Types.FindIndex(f => f.Name == typeLessName);
                        if (typeIndex >= 0) {
                            digimonData.TypeIDs.Add(typeIndex);
                            continue;
                        }
                        string levelLessName = properties.Item(iProperties).InnerText.Replace(" Level", string.Empty);
                        int levelIndex = digimonDB.Levels.FindIndex(f => f.Name == levelLessName);
                        if (levelIndex >= 0) {
                            digimonData.LevelIDs.Add(levelIndex);
                            continue;
                        }
                    }

                    EditorUtility.SetDirty(digimonData);
                    AssetDatabase.SaveAssets();

                    addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(digimonDataPath).ToString(), dataGroup);
                } catch (Exception ex) {
                    Debug.Log($"{digimonLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
                }
            }
        }

        int folderCount = Mathf.CeilToInt((float)digimonsWithArt.Count / (float)DigimonsPerAtlas);
        for (int i = 0; i < folderCount; i++) {
            string digimonsFoldersI = string.Format(ArtDigimonsPathX, i);
            string spriteAtlasPath = digimonsFoldersI + ".spriteatlas";
            if (!File.Exists(spriteAtlasPath)) {
                SpriteAtlas spriteAtlas = new SpriteAtlas();
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath(digimonsFoldersI, typeof(UnityEngine.Object));
                spriteAtlas.Add(new UnityEngine.Object[] { folder });
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    
        for (int i = 0; i < folderCount; i++) {
            string digimonsFoldersI = string.Format(ArtDigimonsPathX, i);
            string spriteAtlasPath = digimonsFoldersI + ".spriteatlas";
            string spriteAtlasGUID = AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString();
            addressablesSettings.CreateOrMoveEntry(spriteAtlasGUID, spriteAtlasGroup);

            int max = Mathf.Min((i + 1) * DigimonsPerAtlas, digimonsWithArt.Count);
            for (int iDigimon = i * DigimonsPerAtlas; iDigimon < max; ++iDigimon) {
                digimonsWithArt[iDigimon].Sprite = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                digimonsWithArt[iDigimon].Sprite.SubObjectName = digimonsWithArt[iDigimon].Name.AddresableSafe();
                try {
                    EditorUtility.SetDirty(digimonsWithArt[iDigimon]);
                } catch (Exception ex) {
                    Debug.Log($"{iDigimon}(asset null: {digimonsWithArt[iDigimon] == null}) - {ex.Message} \n {ex.StackTrace}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        GenerateDigimonList();
        GetEvolutions();

        Debug.Log("Data Fetched");
    }

    public static string AddresableSafe(this string name) {
        return name.Replace(":", string.Empty);
    }

    [MenuItem("DigiDex/Clean Local Data")]
    public static void CleanLocalData() {
        if (Directory.Exists("Assets/Remote")) {
            Directory.Delete("Assets/Remote", true);
        }
        if (File.Exists("Assets/Remote.meta")) {
            File.Delete("Assets/Remote.meta");
        }
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var groups = settings.groups.FindAll(
            g => g.Name == DBGroupName || 
            g.Name == DigimonListGroupName || 
            g.Name == DigimonDataGroupName || 
            g.Name == DigimonSpriteAtlasesGroupName);
        foreach (var group in groups) {
            settings.RemoveGroup(group);
        }
        AssetDatabase.Refresh();
    }
    
    [MenuItem("DigiDex/Generate Digimon List Asset File")]
    public static void GenerateDigimonList() {
        AssetDatabase.Refresh();
        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Digimons = new List<Digimon>();
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path.Replace(".asset", string.Empty)).ToArray();
        for (int i = 0; i < paths.Length; i++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath(paths[i], typeof(Digimon)) as Digimon;
            digimonDB.Digimons.Add(digimonData);
            //digimonDB.Digimons.Add(new DigimonReference { Name = digimonData.Name, Data = new AssetReferenceDigimon(AssetDatabase.GUIDFromAssetPath(paths[i]).ToString()) });
        }
        
        CentralDatabase centralDB = GetCentralDatabase();
        centralDB.DigimonDB = digimonDB;

        EditorUtility.SetDirty(centralDB);
        EditorUtility.SetDirty(digimonDB);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var dbGroup = GetOrAddAddressableGroup(DBGroupName);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(DigimonDBPath).ToString(), dbGroup);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(CentralDBPath).ToString(), dbGroup);

        Debug.Log("List Generated");
    }

    public static CentralDatabase GetCentralDatabase() {
        CentralDatabase centralDB = GetOrCreateScriptableObject<CentralDatabase>(CentralDBPath);

        return centralDB;
    }

    public static DigimonDatabase GetDigimonDatabase() {
        DigimonDatabase digimonDB = GetOrCreateScriptableObject<DigimonDatabase>(DigimonDBPath);

        return digimonDB;
    }

    public static T GetOrCreateScriptableObject<T>(string path) where T : ScriptableObject {
        T scriptableObj = null;
        if (!File.Exists(path)) {
            scriptableObj = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(scriptableObj, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        } else {
            scriptableObj = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
        }

        return scriptableObj;

    }

    [MenuItem("DigiDex/Generate Field List")]
    public async static void GenerateFieldList() {
        XmlDocument fieldSite = new XmlDocument();
        fieldSite.Load(FieldListURL);
        XmlNodeList table = fieldSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table/tbody/tr");
        string fieldsDataPath = DataPath + "Fields";
        if (!Directory.Exists(fieldsDataPath)) {
            Directory.CreateDirectory(fieldsDataPath);
        }

        List<Field> fields = new List<Field>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = GetOrAddAddressableGroup(DigimonDataGroupName);
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
                    field = AssetDatabase.LoadAssetAtPath(fieldDataPath, typeof(Field)) as Field;
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

        string fieldsArtPath = ArtPath + "Fields";
        if (!Directory.Exists(fieldsArtPath)) {
            Directory.CreateDirectory(fieldsArtPath);
        }

        AssetDatabase.Refresh();
        string spriteAtlasPath = fieldsArtPath + ".spriteatlas";
        SpriteAtlas spriteAtlas = new SpriteAtlas();
        UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath(fieldsArtPath, typeof(UnityEngine.Object));
        spriteAtlas.Add(new UnityEngine.Object[] { folder });
        AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var generalSpriteAtlasGroup = GetOrAddAddressableGroup(GeneralSpriteAtlasesGroupName);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString(), generalSpriteAtlasGroup);

        XmlNodeList images = fieldSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/p[6]/a");
        for (int i = 0; i < images.Count; i++) {
            XmlNode image = images.Item(i);
            string fieldName = image.Attributes.GetNamedItem("title").InnerText.Replace("Category:", string.Empty);
            
            string linkToImage = WikimonBaseURL + image.FirstChild.Attributes.GetNamedItem("src").InnerText;
            bool isPNG = linkToImage.ToLower().EndsWith(".png");
            bool isJPG = linkToImage.ToLower().EndsWith(".jpg");
            string fieldArtPath = FieldsArtPath + "/" + fieldName + (isPNG ? ".png" : ".jpg");
            
            if (!File.Exists(fieldArtPath) && (isPNG || isJPG)) {
                using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(linkToImage)) {
                    await textureRequest.SendWebRequest();
                    if (textureRequest.result != UnityWebRequest.Result.ConnectionError) {
                        var texture = DownloadHandlerTexture.GetContent(textureRequest);
                        var data = isPNG ? texture.EncodeToPNG() : texture.EncodeToJPG();
                        var file = File.Create(fieldArtPath);
                        file.Write(data, 0, data.Length);
                        file.Close();
                        AssetDatabase.Refresh();
                    }
                }
            }

            var field = fields.Find(f => f.Name == fieldName);
            if (field != null) {
                field.Sprite = new AssetReferenceAtlasedSprite(AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString());
                field.Sprite.SubObjectName = fieldName.AddresableSafe();
            }
        }

        AssetDatabase.SaveAssets();

        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Fields = new List<FieldReference>();
        for (int i = 0; i < fields.Count; i++) {
            string fieldDataPath = fieldsDataPath + "/" + fields[i].Name + ".asset";
            digimonDB.Fields.Add(new FieldReference { Name = fields[i].Name, Data = new AssetReferenceField(AssetDatabase.GUIDFromAssetPath(fieldDataPath).ToString()) });
        }
        EditorUtility.SetDirty(digimonDB);
        AssetDatabase.SaveAssets();

        SpriteAtlasUtility.PackAllAtlases(EditorUserBuildSettings.activeBuildTarget);
    }

    [MenuItem("DigiDex/Couple Digimons With Properties")]
    public static void CoupleDigimonFieldData() {
        DigimonDatabase digimonDB = GetDigimonDatabase();
        
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path).ToArray();
        for (int iDigimon = 0; iDigimon < paths.Length; iDigimon++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath(paths[iDigimon], typeof(Digimon)) as Digimon;
            string digimonLink = WikimonBaseURL + digimonData.LinkSubFix;


            XmlDocument digimonSite = new XmlDocument();
            digimonSite.Load(digimonLink);

            digimonData.AttributeIDs = new List<int>();
            digimonData.FieldIDs = new List<int>();
            digimonData.TypeIDs = new List<int>();
            digimonData.LevelIDs = new List<int>();
            XmlNodeList properties = digimonSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[5]/div[1]/ul/li/a");
            for (int iProperties = 0; iProperties < properties.Count; ++iProperties) {
                string attributeLessName = properties.Item(iProperties).InnerText.Replace(" Attribute", string.Empty);
                int attributeIndex = digimonDB.Attributes.FindIndex(a => a.Name == attributeLessName);
                if (attributeIndex >= 0) {
                    digimonData.AttributeIDs.Add(attributeIndex);
                    continue;
                }
                string fieldLessName = properties.Item(iProperties).InnerText.Replace(" Field", string.Empty);
                int fieldIndex = digimonDB.Fields.FindIndex(f => f.Name == fieldLessName);
                if (fieldIndex >= 0) {
                    digimonData.FieldIDs.Add(fieldIndex);
                    continue;
                }
                string typeLessName = properties.Item(iProperties).InnerText.Replace(" Type", string.Empty);
                int typeIndex = digimonDB.Types.FindIndex(f => f.Name == typeLessName);
                if (typeIndex >= 0) {
                    digimonData.TypeIDs.Add(typeIndex);
                    continue;
                }
                string levelLessName = properties.Item(iProperties).InnerText.Replace(" Level", string.Empty);
                int levelIndex = digimonDB.Levels.FindIndex(f => f.Name == levelLessName);
                if (levelIndex >= 0) {
                    digimonData.LevelIDs.Add(levelIndex);
                    continue;
                }
            }

            EditorUtility.SetDirty(digimonData);
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("DigiDex/Generate Attribute List")]
    public static void GenerateAttributeList() {
        XmlDocument attributeSite = new XmlDocument();
        attributeSite.Load(AttributeListURL);
        XmlNodeList table = attributeSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table/tbody/tr/td/a");
        string attributesDataPath = DataPath + "Attributes";
        if (!Directory.Exists(attributesDataPath)) {
            Directory.CreateDirectory(attributesDataPath);
        }

        List<Attribute> attributes = new List<Attribute>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = GetOrAddAddressableGroup(DigimonDataGroupName);
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
                    attribute = AssetDatabase.LoadAssetAtPath(attributeDataPath, typeof(Attribute)) as Attribute;
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
        digimonDB.Attributes = new List<AttributeReference>();
        for (int i = 0; i < attributes.Count; i++) {
            string attributeDataPath = attributesDataPath + "/" + attributes[i].Name + ".asset";
            digimonDB.Attributes.Add(new AttributeReference { Name = attributes[i].Name, Data = new AssetReferenceAttribute(AssetDatabase.GUIDFromAssetPath(attributeDataPath).ToString()) });
        }
        EditorUtility.SetDirty(digimonDB);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("DigiDex/Generate Type List")]
    public static void GenerateTypeList() {
        XmlDocument typeSite = new XmlDocument();
        typeSite.Load(TypeListURL);
        XmlNodeList table = typeSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table/tbody/tr/td[1]/b/a");
        string typesDataPath = DataPath + "Types";
        if (!Directory.Exists(typesDataPath)) {
            Directory.CreateDirectory(typesDataPath);
        }

        List<Type> types = new List<Type>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = GetOrAddAddressableGroup(DigimonDataGroupName);
        for (int i = 0; i < table.Count; i++) {
            XmlNode fieldData = table.Item(i);
            string typeName = fieldData.ChildNodes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(typeName)) {
                Type type = null;
                string typeDataPath = typesDataPath + "/" + typeName + ".asset";
                if (!File.Exists(typeDataPath)) {
                    type = ScriptableObject.CreateInstance<Type>();
                    AssetDatabase.CreateAsset(type, typeDataPath);
                } else {
                    type = AssetDatabase.LoadAssetAtPath(typeDataPath, typeof(Type)) as Type;
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

    [MenuItem("DigiDex/Generate Level List")]
    public static void GenerateLevelList() {
        XmlDocument levelSite = new XmlDocument();
        levelSite.Load(LevelListURL);
        XmlNodeList table = levelSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        string levelsDataPath = DataPath + "Levels";
        if (!Directory.Exists(levelsDataPath)) {
            Directory.CreateDirectory(levelsDataPath);
        }

        List<Level> levels = new List<Level>();
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var listGroup = GetOrAddAddressableGroup(DigimonDataGroupName);
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
                    level = AssetDatabase.LoadAssetAtPath(levelDataPath, typeof(Level)) as Level;
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

        for (int i = 0; i < levels.Count; i++) {
            string levelDataPath = levelsDataPath + "/" + levels[i].Name + ".asset";
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(levelDataPath).ToString(), listGroup);
        }
    }
    
    [MenuItem("DigiDex/Get Evolutions")]
    public static void GetEvolutions() {
        DigimonDatabase digimonDB = GetDigimonDatabase();

        if (!Directory.Exists(DigimonEvolutionsDataPath)) {
            Directory.CreateDirectory(DigimonEvolutionsDataPath);
        }

        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path).ToArray();
        for (int iDigimon = 0; iDigimon < paths.Length; iDigimon++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath(paths[iDigimon], typeof(Digimon)) as Digimon;
            string digimonLink = WikimonBaseURL + digimonData.LinkSubFix;

            string evolutionDataPath = $"{DigimonEvolutionsDataPath}/{digimonData.Name.AddresableSafe()} Evolutions.asset";
            EvolutionData evolutionData = GetOrCreateScriptableObject<EvolutionData>(evolutionDataPath);
            
            XmlDocument digimonSite = new XmlDocument();
            try {
                digimonSite.Load(digimonLink);

                evolutionData.PreEvolutions = ParseEvolutionList("Evolves_From");
                evolutionData.Evolutions = ParseEvolutionList("Evolves_To");

                digimonData.EvolutionData = new AssetReferenceEvolutionData(AssetDatabase.GUIDFromAssetPath(evolutionDataPath).ToString());

                EditorUtility.SetDirty(evolutionData);
                EditorUtility.SetDirty(digimonData);
            } catch (Exception ex) {
                Debug.Log($"{digimonData.Name} - {ex.Message} \n {ex.StackTrace}");
            }


            ////////////////////////////////
            // Function Helpers
            ////////////////////////////////

            List<Evolution> ParseEvolutionList(string headerName) {
                List<Evolution> evolutions = new List<Evolution>();

                XmlNodeList header = digimonSite.SelectNodes($"/html/body/div/div/div/div/div/div/h2/span[@id='{headerName}']");
                // Check if there're digimons to be parsed
                if (header?.Item(0)?.ParentNode.NextSibling.Name == "ul") {
                    XmlNodeList evolutionsNode = header.Item(0).ParentNode.NextSibling.SelectNodes("li");
                    for (int iField = 0; iField < evolutionsNode.Count; ++iField) {
                        XmlNode digimonNode = evolutionsNode.Item(iField).FirstChild;
                        string name = digimonNode.InnerText;
                        if (name.StartsWith("Any ")) {
                            continue;
                        }

                        var auxNode = digimonNode.Name == "b"? digimonNode.FirstChild : digimonNode;
                        
                        int digimonIndex = digimonDB.Digimons.FindIndex(d => d.Name == name);
                        string fuseDigimonLinkSubFix = auxNode?.Attributes?.GetNamedItem("href")?.InnerText;

                        if (digimonIndex < 0 && !string.IsNullOrEmpty(fuseDigimonLinkSubFix)) {
                            // Sometimes name variants are used for the list, we look for the name used in the profile
                            XmlDocument fuseDigimonSite = new XmlDocument();
                            try {
                                fuseDigimonSite.Load(WikimonBaseURL + fuseDigimonLinkSubFix);
                                name = fuseDigimonSite.SelectSingleNode("//*[@id='firstHeading']").InnerText;
                                XmlNode redirectNode = fuseDigimonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/div/ul[@class='redirectText']/li/a");
                                while (redirectNode != null) {
                                    string newLinkSubFix = redirectNode.Attributes.GetNamedItem("href").InnerText;
                                    Debug.Log($"Redirecting from {fuseDigimonLinkSubFix} to {newLinkSubFix}");
                                    fuseDigimonLinkSubFix = newLinkSubFix;
                                    fuseDigimonSite.Load(WikimonBaseURL + fuseDigimonLinkSubFix);
                                    name = fuseDigimonSite.SelectSingleNode("//*[@id='firstHeading']").InnerText;
                                    redirectNode = fuseDigimonSite.SelectSingleNode("/html/body/div/div/div/div/div/div/div/ul[@class='redirectText']/li/a");
                                }
                            } catch (Exception ex) {
                                Debug.Log($"{name} - {ex.Message} \n {ex.StackTrace}");
                            }

                            digimonIndex = digimonDB.Digimons.FindIndex(d => d.Name == name);
                        }

                        if (digimonIndex >= 0) {
                            List<Evolution> evolutionMethods = new List<Evolution>();

                            EvolutionType baseEvolutionType = EvolutionType.Regular;
                            if (digimonNode.Name == "b") {
                                baseEvolutionType = EvolutionType.Main;
                            }
                            
                            Evolution method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType, FusionIDs = new int[0] };

                            bool isWarp = false;
                            bool oneOrMoreOptionals = false;

                            XmlNode siblingNode = digimonNode.NextSibling;
                            while (siblingNode != null) {
                                if (siblingNode.InnerText == "Warp Evolution") {
                                    isWarp = true;
                                } else if (siblingNode.InnerText.Contains("with")) {
                                    bool isOptional = siblingNode.InnerText.Contains("without") ||
                                        (siblingNode.Name == "b" && siblingNode.NextSibling.InnerText.Contains("without"));
                                    
                                    if (isOptional) {
                                        // The first optional means that the digimon can evolve with the base element alone and we always record it
                                        // Otherwise we only record the method if it has any changes from the base element
                                        if (!oneOrMoreOptionals || (method.Type != baseEvolutionType)) {
                                            evolutionMethods.Add(method);
                                            oneOrMoreOptionals = true;
                                        }
                                        method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType };
                                        
                                        if (siblingNode.Name == "b") {
                                            // skip "without" since we already parsed it
                                            siblingNode = siblingNode.NextSibling;
                                        }
                                    }
                                    
                                    // Start reading components
                                    siblingNode = siblingNode?.NextSibling;
                                    
                                    List<(int id, bool isMain)> fusionIDs = new List<(int id, bool isMain)>();
                                    bool recordFusionsTogether = false;
                                    bool recordFusionsSeparated = false;

                                    while (siblingNode != null) {
                                        if (siblingNode.InnerText.Contains("Digimental")) {
                                            // Record fusion in the case of DigimonA(with DigimonB or NotDigimon)
                                            RecordConcatenatedFusions();
                                            method.Type |= EvolutionType.Armor;
                                            CheckMain(ref method, siblingNode);
                                            evolutionMethods.Add(method);
                                            method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType };
                                        } else if (siblingNode.InnerText.Contains("Spirit")) {
                                            // Record fusion in the case of DigimonA(with DigimonB or NotDigimon)
                                            RecordConcatenatedFusions();
                                            method.Type |= EvolutionType.Spirit;
                                            CheckMain(ref method, siblingNode);
                                            evolutionMethods.Add(method);
                                            method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType };
                                        } else if (siblingNode.InnerText.Trim() == "Slide Evolution") {
                                            // Record fusion in the case of DigimonA(with DigimonB or NotDigimon)
                                            RecordConcatenatedFusions();
                                            method.Type |= EvolutionType.Side;
                                            CheckMain(ref method, siblingNode);
                                            evolutionMethods.Add(method);
                                            method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType };
                                        } else if (siblingNode.Name == "b" || siblingNode.Name == "a") {
                                            int fusionIndex = digimonDB.Digimons.FindIndex(d => d.Name == siblingNode.InnerText);
                                            if (fusionIndex > 0) {
                                                method.Type |= EvolutionType.Fusion;
                                                fusionIDs.Add((fusionIndex, siblingNode.Name == "b"));
                                            }
                                            RecordConcatenatedFusions();
                                        } else if (siblingNode.InnerText.Contains("or")) {
                                            if (fusionIDs.Count > 0) {
                                                recordFusionsSeparated = true;
                                            }
                                        } else if (siblingNode.InnerText.Contains("and")) {
                                            if (fusionIDs.Count > 0) {
                                                recordFusionsTogether = true;
                                            }
                                        } else if (siblingNode.InnerText.Contains(')')) {
                                            if (method.Type != baseEvolutionType) {
                                                // Record fusion in the case of DigimonA(with DigimonB or NotDigimon)
                                                RecordConcatenatedFusions();
                                                RecordFuseRemanents();
                                                evolutionMethods.Add(method);
                                                method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType };
                                            }
                                            break;
                                        }

                                        siblingNode = siblingNode.NextSibling;

                                        void CheckMain(ref Evolution evo, XmlNode node) {
                                            if (node.Name != "b") {
                                                evo.Type &= ~EvolutionType.Main;
                                            }
                                        }

                                        void RecordConcatenatedFusions() {
                                            if (recordFusionsTogether) {
                                                if (fusionIDs.Count > 0) {
                                                    method.FusionIDs = fusionIDs.Select(tuple => tuple.id).ToArray();
                                                    if (!fusionIDs[0].isMain) {
                                                        method.Type &= ~EvolutionType.Main;
                                                    }
                                                    fusionIDs.Clear();
                                                    evolutionMethods.Add(method);
                                                    method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType };
                                                }
                                                recordFusionsTogether = false;
                                            }
                                            if (recordFusionsSeparated) {
                                                for (int iFusionID = 0; iFusionID < fusionIDs.Count; ++iFusionID) {
                                                    method.FusionIDs = new int[] { fusionIDs[iFusionID].id };
                                                    method.Type = baseEvolutionType;
                                                    if (!fusionIDs[iFusionID].isMain) {
                                                        method.Type &= ~EvolutionType.Main;
                                                    }
                                                    method.Type |= EvolutionType.Fusion;
                                                    evolutionMethods.Add(method);
                                                    method = new Evolution { DigimonID = digimonIndex, DebugName = name, Type = baseEvolutionType | EvolutionType.Fusion };
                                                }
                                                fusionIDs.Clear();
                                                recordFusionsSeparated = false;
                                            }
                                        }
                                    }

                                    RecordFuseRemanents();

                                    void RecordFuseRemanents() {
                                        if (fusionIDs.Count > 0) {
                                            method.FusionIDs = fusionIDs.Select(tuple => tuple.id).ToArray();
                                            if (!fusionIDs[0].isMain) {
                                                method.Type &= ~EvolutionType.Main;
                                            }
                                        }
                                    }
                                }

                                siblingNode = siblingNode?.NextSibling;
                            }

                            if (evolutionMethods.Count == 0 || (method.Type != baseEvolutionType)) {
                                evolutionMethods.Add(method);
                            }

                            for (int iMethod = 0; iMethod < evolutionMethods.Count; ++iMethod) {
                                // Warp means the an evolution stage gets skipped independent of the method
                                if (isWarp) {
                                    evolutionMethods[iMethod].Type |= EvolutionType.Warp;
                                }
                                evolutions.Add(evolutionMethods[iMethod]);
                            }
                        }
                    }
                }

                return evolutions.Distinct().ToList();
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        var evolutionPaths = Directory.GetFiles(DigimonEvolutionsDataPath, "*.asset").OrderBy(path => path).ToArray();
        AddressableAssetGroup group = GetOrAddAddressableGroup(DigimonEvolutionDataGroupName);
        for (int iEvolutionPath = 0; iEvolutionPath < evolutionPaths.Length; ++iEvolutionPath) {
            settings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(evolutionPaths[iEvolutionPath]).ToString(), group);
        }

        Debug.Log("Evolutions retrieved");
    }
}
