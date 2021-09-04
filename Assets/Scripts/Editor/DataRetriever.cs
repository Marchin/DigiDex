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
    const string DigimonDataGroupName = "Digimon Data";
    const string DigimonDBGroupName = "Digimon Database";
    const string WikimonBaseURL = "https://wikimon.net";
    const string DigimonListURL = WikimonBaseURL + "/List_of_Digimon";
    const string FieldListURL = WikimonBaseURL + "/Field";
    const string AttributeListURL = WikimonBaseURL + "/Attribute";
    const string TypeListURL = WikimonBaseURL + "/Type";
    const string LevelListURL = WikimonBaseURL + "/Evolution_Stage";
    const int DigimonsPerAtlas = 16;
    const string ArtDigimonsPathX = ArtPath + "Digimons/Digimon({0})";
    const string DigimonsDataPath = DataPath + "Digimons";
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
        GenerateFieldList();
        GenerateAttributeList();
        GenerateTypeList();
        GenerateLevelList();

        // TODO: Add the new images either in the last folder or on a new one depending on the wether the last folder is full
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        if (!Directory.Exists(DigimonsDataPath)) {
            Directory.CreateDirectory(DigimonsDataPath);
        }

        var dataGroup = GetOrAddAddressableGroup(DigimonDataGroupName);

        var spriteAtlasGroup = GetOrAddAddressableGroup(DigimonSpriteAtlasesGroupName);

        DigimonDatabase digimonDB = GetDigimonDatabase();

        List<Digimon> digimonsWithArt = new List<Digimon>();

        XmlDocument digimonListSite = new XmlDocument();
        digimonListSite.Load(DigimonListURL);
        XmlNodeList table = digimonListSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        for (int i = 0; i < table.Count; i++) {
            string digimonLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";
            string digimonName = table.Item(i)?.InnerText.Trim();

            if (!string.IsNullOrEmpty(digimonLinkSubFix)) {
                string digimonNameSafe = digimonName.AddresableSafe();
                string artPath = string.Format(ArtDigimonsPathX, digimonsWithArt.Count / DigimonsPerAtlas);
                string digimonArtPath = artPath + "/" + digimonNameSafe + ".png";
                string digimonDataPath = DigimonsDataPath + "/" + digimonNameSafe + ".asset";
                string digimonLink = WikimonBaseURL + digimonLinkSubFix;

                try {
                    XmlDocument digimonSite = new XmlDocument();
                    digimonSite.Load(digimonLink);

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
                    
                    digimonData.AttributeIDs?.Clear();
                    digimonData.FieldIDs?.Clear();
                    digimonData.TypeIDs?.Clear();
                    digimonData.LevelIDs?.Clear();
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
                } finally {
                    
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

            for (int iDigimon = i * DigimonsPerAtlas; iDigimon < Mathf.Min(iDigimon + DigimonsPerAtlas, digimonsWithArt.Count); ++iDigimon) {
                digimonsWithArt[iDigimon].Sprite = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                digimonsWithArt[iDigimon].Sprite.SubObjectName = digimonsWithArt[iDigimon].Name.AddresableSafe();
                EditorUtility.SetDirty(digimonsWithArt[iDigimon]);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        GenerateDigimonList();

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
        var groups = settings.groups.FindAll(g => g.Name == DigimonDBGroupName || g.Name == DigimonDataGroupName || g.Name == DigimonSpriteAtlasesGroupName);
        foreach (var group in groups) {
            settings.RemoveGroup(group);
        }
        AssetDatabase.Refresh();
    }
    
    [MenuItem("DigiDex/Generate Digimon List Asset File")]
    public static void GenerateDigimonList() {
        AssetDatabase.Refresh();
        DigimonDatabase digimonDB = GetDigimonDatabase();
        digimonDB.Digimons = new List<DigimonReference>();
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path).ToArray();
        for (int i = 0; i < paths.Length; i++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath(paths[i], typeof(Digimon)) as Digimon;
            digimonDB.Digimons.Add(new DigimonReference { Name = digimonData.Name, Data = new AssetReferenceDigimon(AssetDatabase.GUIDFromAssetPath(paths[i]).ToString()) });
        }
        EditorUtility.SetDirty(digimonDB);
        AssetDatabase.SaveAssets();
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var digimonDBGroup = GetOrAddAddressableGroup(DigimonDBGroupName);
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(DigimonDBPath).ToString(), digimonDBGroup);

        Debug.Log("List Generated");
    }

    public static DigimonDatabase GetDigimonDatabase() {
        DigimonDatabase digimonDB = null;
        if (!File.Exists(DigimonDBPath)) {
            digimonDB = ScriptableObject.CreateInstance<DigimonDatabase>();
            AssetDatabase.CreateAsset(digimonDB, DigimonDBPath);
        } else {
            digimonDB = AssetDatabase.LoadAssetAtPath(DigimonDBPath, typeof(DigimonDatabase)) as DigimonDatabase;
        }

        return digimonDB;
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
        var listGroup = GetOrAddAddressableGroup(DigimonDBGroupName);
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

    [MenuItem("DigiDex/Couple Digimons With Fields")]
    public static void CoupleDigimonFieldData() {
        DigimonDatabase digimonDB = GetDigimonDatabase();
        
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path).ToArray();
        for (int iDigimon = 0; iDigimon < paths.Length; iDigimon++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath(paths[iDigimon], typeof(Digimon)) as Digimon;
            string digimonLink = WikimonBaseURL + digimonData.LinkSubFix;

            digimonData.FieldIDs = new List<int>();

            XmlDocument digimonSite = new XmlDocument();
            digimonSite.Load(digimonLink);

            XmlNodeList fields = digimonSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[2]/th/a");
            for (int iField = 0; iField < fields.Count; ++iField) {
                int fieldIndex = digimonDB.Fields.FindIndex(f => f.Name == fields.Item(iField).Attributes.GetNamedItem("title").InnerText.Replace("Category:", string.Empty));
                if (fieldIndex >= 0) {
                    digimonData.FieldIDs.Add(fieldIndex);
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
        var listGroup = GetOrAddAddressableGroup(DigimonDBGroupName);
        for (int i = 1; i < table.Count; i++) {
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
        var listGroup = GetOrAddAddressableGroup(DigimonDBGroupName);
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
        var listGroup = GetOrAddAddressableGroup(DigimonDBGroupName);
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
}
