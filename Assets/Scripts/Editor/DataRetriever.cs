using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public static class DataRetriever {
    const string DigimonsDataPath = "Assets/Remote/Data/Digimons";
    const string digimonSpriteAtlasesGroupName = "Digimon Sprite Atlases";
    const string DigimonDataGroupName = "Digimon Data";
    const string DigimonListGroupName = "Digimon Data List";

    [MenuItem("DigiDex/Retrieve Data")]
    public static async void RetrieveData() {
        const string WikimonBaseURL = "https://wikimon.net";
        const string DigimonList = WikimonBaseURL + "/List_of_Digimon";
        const string ArtDigimonsPathX = "Assets/Remote/Art/Digimons/Digimon({0})";
        const int DigimonsPerAtlas = 16;
        
        // TODO: Add the new images either in the last folder or on a new one depending on the wether the last folder is full
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        if (!Directory.Exists(DigimonsDataPath)) {
            Directory.CreateDirectory(DigimonsDataPath);
        }

        var dataGroup = addressablesSettings.groups.Find(g => g.Name == DigimonDataGroupName);
        if (dataGroup == null) {
            dataGroup = addressablesSettings.CreateGroup(DigimonDataGroupName, false, false, false, null);
        }

        var spriteAtlasGroup = addressablesSettings.groups.Find(g => g.Name == digimonSpriteAtlasesGroupName);
        if (spriteAtlasGroup == null) {
            spriteAtlasGroup = addressablesSettings.CreateGroup(digimonSpriteAtlasesGroupName, false, false, false, null);
        }

        List<Digimon> digimons = new List<Digimon>();

        XmlDocument digimonListSite = new XmlDocument();
        digimonListSite.Load(DigimonList);
        XmlNodeList table = digimonListSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        for (int i = 0; i < table.Count; i++) {
            string digimonLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";
            string digimonName = table.Item(i)?.InnerText.Trim();

            if (!string.IsNullOrEmpty(digimonLinkSubFix)) {
                string digimonNameSafe = digimonName.AddresableSafe();
                string artPath = string.Format(ArtDigimonsPathX, digimons.Count / DigimonsPerAtlas);
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
                                    }
                                }
                            }
                        }
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

                    digimons.Add(digimonData);

                    EditorUtility.SetDirty(digimonData);
                    AssetDatabase.SaveAssets();

                    addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(digimonDataPath).ToString(), dataGroup);
                } catch (Exception ex) {
                    Debug.Log($"{digimonLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
                } finally {
                    
                }
            }
        }


        int folderCount = Mathf.CeilToInt((float)digimons.Count / (float)DigimonsPerAtlas);
        List<SpriteAtlas> spriteAtlases = new List<SpriteAtlas>(folderCount);
        for (int i = 0; i < folderCount; i++) {
            string digimonsFoldersI = string.Format(ArtDigimonsPathX, i);
            string spriteAtlasPath = digimonsFoldersI + ".spriteatlas";
            SpriteAtlas spriteAtlas = null;
            if (!File.Exists(spriteAtlasPath)) {
                spriteAtlas = new SpriteAtlas();
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath(digimonsFoldersI, typeof(UnityEngine.Object));
                spriteAtlas.Add(new UnityEngine.Object[] { folder });
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
                spriteAtlases.Add(spriteAtlas);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SpriteAtlasUtility.PackAtlases(spriteAtlases.ToArray(), EditorUserBuildSettings.activeBuildTarget);
    
        for (int i = 0; i < folderCount; i++) {
            string digimonsFoldersI = string.Format(ArtDigimonsPathX, i);
            string spriteAtlasPath = digimonsFoldersI + ".spriteatlas";
            string spriteAtlasGUID = AssetDatabase.GUIDFromAssetPath(spriteAtlasPath).ToString();
            addressablesSettings.CreateOrMoveEntry(spriteAtlasGUID, spriteAtlasGroup);

            for (int iDigimon = i * DigimonsPerAtlas; iDigimon < Mathf.Min(iDigimon + DigimonsPerAtlas, digimons.Count); ++iDigimon) {
                digimons[iDigimon].Image = new AssetReferenceAtlasedSprite(spriteAtlasGUID);
                digimons[iDigimon].Image.SubObjectName = digimons[iDigimon].Name.AddresableSafe();
                EditorUtility.SetDirty(digimons[iDigimon]);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

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
        var groups = settings.groups.FindAll(g => g.Name == DigimonListGroupName || g.Name == DigimonDataGroupName || g.Name == digimonSpriteAtlasesGroupName);
        foreach (var group in groups) {
            settings.RemoveGroup(group);
        }
        AssetDatabase.Refresh();
    }
    
    [MenuItem("DigiDex/Generate Digimon List Asset File")]
    public static void GenerateDigimonList() {
        const string DigimonListLocation = "Assets/Remote/Data/";
        const string DigimonListPath = DigimonListLocation + "DigimonList.asset";
        AssetDatabase.Refresh();
        DigimonList digimonList = null;
        if (!File.Exists(DigimonListPath)) {
            digimonList = ScriptableObject.CreateInstance<DigimonList>();
            AssetDatabase.CreateAsset(digimonList, DigimonListPath);
        } else {
            digimonList = AssetDatabase.LoadAssetAtPath(DigimonListPath, typeof(DigimonList)) as DigimonList;
        }
        digimonList.Digimons = new List<DigimonReference>();
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path).ToArray();
        for (int i = 0; i < paths.Length; i++) {
            Digimon digimonData = AssetDatabase.LoadAssetAtPath(paths[i], typeof(Digimon)) as Digimon;
            digimonList.Digimons.Add(new DigimonReference { Name = digimonData.Name, Data = new AssetReferenceDigimon(AssetDatabase.GUIDFromAssetPath(paths[i]).ToString()) });
        }
        EditorUtility.SetDirty(digimonList);
        AssetDatabase.SaveAssets();
        
        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var digimonListGroup = addressablesSettings.groups.Find(g => g.Name == DigimonListGroupName);
        if (digimonListGroup == null) {
            digimonListGroup = addressablesSettings.CreateGroup(DigimonListGroupName, false, false, false, null);
        }
        addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(DigimonListPath).ToString(), digimonListGroup);

        Debug.Log("List Generated");
    }
}
