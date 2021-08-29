using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public static class DataRetriever {
    const string RemoteResourcesPath = "Assets/Resources";
    const string DigimonsDataPath = "Remote/Data/Digimons";

    [MenuItem("DigiDex/Retrieve Data")]
    public static async void RetrieveData() {
        const string WikimonBaseURL = "https://wikimon.net";
        const string DigimonList = WikimonBaseURL + "/List_of_Digimon";
        const string ArtDigimonsPathX = "Remote/Art/Digimons/Digimon[{0}]";
        
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

                    if (!Directory.Exists(artPath.GetResourceAssetLocation())) {
                        Directory.CreateDirectory(artPath.GetResourceAssetLocation());
                    }
                
                    if (!Directory.Exists(DigimonsDataPath.GetResourceAssetLocation())) {
                        Directory.CreateDirectory(DigimonsDataPath.GetResourceAssetLocation());
                    }
                
                    if (!File.Exists(digimonArtPath.GetResourceAssetLocation())) {
                        XmlNode image = digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[1]/td/div/div/a/img");
                        if (image != null) {
                            string linkToImage = WikimonBaseURL + image.Attributes.GetNamedItem("src").InnerText;
                            using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(linkToImage)) {
                                await textureRequest.SendWebRequest();
                                if (textureRequest.result != UnityWebRequest.Result.ConnectionError) {
                                    var texture = DownloadHandlerTexture.GetContent(textureRequest);
                                    var data = texture.EncodeToPNG();
                                    var file = File.Create(digimonArtPath.GetResourceAssetLocation());
                                    file.Write(data, 0, data.Length);
                                    file.Close();
                                    AssetDatabase.Refresh();
                                }
                            }
                        }
                    }
                    
                    Digimon digimonData = null;
                    if (!File.Exists(digimonDataPath.GetResourceAssetLocation())) {

                        digimonData = ScriptableObject.CreateInstance<Digimon>();
                        AssetDatabase.CreateAsset(digimonData, digimonDataPath.GetResourceAssetLocation());
                    } else {
                        digimonData = AssetDatabase.LoadAssetAtPath(digimonDataPath.GetResourceAssetLocation(), typeof(Digimon)) as Digimon;
                    }

                    digimonData.Name = digimonName;

                    if (File.Exists(digimonArtPath.GetResourceAssetLocation())) {
                        digimonData.Image = AssetDatabase.LoadAssetAtPath(digimonArtPath.GetResourceAssetLocation(), typeof(Sprite)) as Sprite;
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
            if (!File.Exists(spriteAtlasPath.GetResourceAssetLocation())) {
                SpriteAtlas spriteAtlas = new SpriteAtlas();
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath(digimonsFoldersI.GetResourceAssetLocation(), typeof(UnityEngine.Object));
                spriteAtlas.Add(new UnityEngine.Object[] { folder });
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath.GetResourceAssetLocation());
            }
        }
        
        GenerateDigimonList();

        AssetDatabase.SaveAssets();
        Debug.Log("Data Fetched");
    }

    
    [MenuItem("DigiDex/Clean Local Data")]
    public static void CleanLocalData() {
        if (Directory.Exists("Remote/Art/Digimons".GetResourceAssetLocation())) {
            Directory.Delete("Remote/Art/Digimons".GetResourceAssetLocation(), true);
        }
        if (File.Exists("Remote/Art/Digimons.meta".GetResourceAssetLocation())) {
            File.Delete("Remote/Art/Digimons.meta".GetResourceAssetLocation());
        }
        if (Directory.Exists(DigimonsDataPath.GetResourceAssetLocation())) {
            Directory.Delete(DigimonsDataPath.GetResourceAssetLocation(), true);
        }
        if (File.Exists("Remote/Data/Digimons.meta".GetResourceAssetLocation())) {
            File.Delete("Remote/Data/Digimons.meta".GetResourceAssetLocation());
        }
        if (File.Exists("Remote/Data/DigimonList.asset".GetResourceAssetLocation())) {
            File.Delete("Remote/Data/DigimonList.asset".GetResourceAssetLocation());
        }
        if (File.Exists("Remote/Data/DigimonList.asset.meta".GetResourceAssetLocation())) {
            File.Delete("Remote/Data/DigimonList.asset.meta".GetResourceAssetLocation());
        }
        AssetDatabase.Refresh();
    }

    
    [MenuItem("DigiDex/Generate Digimon List Asset File")]
    public static void GenerateDigimonList() {
        const string DigimonListLocation = "Remote/Data/";
        const string DigimonListPath = DigimonListLocation + "DigimonList.asset";
        AssetDatabase.Refresh();
        DigimonList digimonList = null;
        if (!File.Exists(DigimonListPath.GetResourceAssetLocation())) {
            digimonList = ScriptableObject.CreateInstance<DigimonList>();
            AssetDatabase.CreateAsset(digimonList, DigimonListPath.GetResourceAssetLocation());
        } else {
            digimonList = AssetDatabase.LoadAssetAtPath(DigimonListPath.GetResourceAssetLocation(), typeof(DigimonList)) as DigimonList;
        }
        Digimon[] digimons = Resources.LoadAll<Digimon>(DigimonsDataPath) as Digimon[];
        digimonList.Digimons = new List<Digimon>(digimons);
        EditorUtility.SetDirty(digimonList);
        AssetDatabase.SaveAssets();
        Debug.Log("List Generated");
    }

    public static string GetResourceAssetLocation(this string path) {
        string result = path;

        if (!string.IsNullOrEmpty(result) && result[0] == '/') {
            result = RemoteResourcesPath + path;
        } else {
            result = RemoteResourcesPath + "/" + path;
        }

        return result;
    }
}
