using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public static class DataRetriever {
    const string DigimonsDataPath = "Assets/Remote/Data/Digimons";

    [MenuItem("DigiDex/Retrieve Data")]
    public static async void RetrieveData() {
        const string WikimonBaseURL = "https://wikimon.net";
        const string DigimonList = WikimonBaseURL + "/List_of_Digimon";
        const string ArtDigimonsPathX = "Assets/Remote/Art/Digimons/Digimon[{0}]";
        
        // TODO: Add the new images either in the last folder or on a new one depending on the wether the last folder is full

        if (!Directory.Exists(DigimonsDataPath)) {
            Directory.CreateDirectory(DigimonsDataPath);
        }

        XmlDocument digimonListSite = new XmlDocument();
        digimonListSite.Load(DigimonList);
        XmlNodeList table = digimonListSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        int digimonCount = 0;
        for (int i = 0; i < table.Count; i++) {
            string digimonLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";
            string digimonName = table.Item(i)?.InnerText.Trim();

            if (!string.IsNullOrEmpty(digimonLinkSubFix)) {
                string digimonNameSafe = digimonName.Replace(":", string.Empty);
                string artPath = string.Format(ArtDigimonsPathX, digimonCount / 16);
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
                            bool isPNG = linkToImage.EndsWith(".png");
                            bool isJPG = linkToImage.EndsWith(".jpg");
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

                    digimonData.Name = digimonName;

                    if (File.Exists(digimonArtPath)) {
                        digimonData.Image = AssetDatabase.LoadAssetAtPath(digimonArtPath, typeof(Sprite)) as Sprite;
                    }

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

                    EditorUtility.SetDirty(digimonData);
                    AssetDatabase.SaveAssets();

                    ++digimonCount;
                } catch (Exception ex) {
                    Debug.Log($"{digimonLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
                } finally {
                    
                }
            }
        }

        int folderCount = digimonCount / 16;
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
        
        GenerateDigimonList();

        AssetDatabase.SaveAssets();
        Debug.Log("Data Fetched");
    }

    
    [MenuItem("DigiDex/Clean Local Data")]
    public static void CleanLocalData() {
        if (Directory.Exists("Assets/Remote")) {
            Directory.Delete("Assets/Remote", true);
        }
        if (File.Exists("Assets/Remote.meta")) {
            File.Delete("Assets/Remote.meta");
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
        
        digimonList.Digimons = new List<Digimon>();
        var paths = Directory.GetFiles(DigimonsDataPath, "*.asset").OrderBy(path => path).ToArray();
        for (int i = 0; i < paths.Length; i++) {
            Digimon digimon = AssetDatabase.LoadAssetAtPath<Digimon>(paths[i]);
            if (digimon != null) {
                digimonList.Digimons.Add(digimon);
            }
        }
        EditorUtility.SetDirty(digimonList);
        AssetDatabase.SaveAssets();
        Debug.Log("List Generated");
    }

    // public static string GetResourceAssetLocation(this string path) {
    //     string result = path;

    //     if (!string.IsNullOrEmpty(result) && result[0] == '/') {
    //         result = RemoteResourcesPath + path;
    //     } else {
    //         result = RemoteResourcesPath + "/" + path;
    //     }

    //     return result;
    // }
}
