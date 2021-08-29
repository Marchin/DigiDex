using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEngine;
using UnityEditor.U2D;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;

public class DataRetriever {
    [MenuItem("DigiDex/Retrieve Data")]
    public static async void RetrieveData() {
        const string WikimonBaseURL = "https://wikimon.net";
        const string DigimonList = WikimonBaseURL + "/List_of_Digimon";
        const string ArtDigimonsPathX = "Assets/Art/Digimons/Digimon[{0}]";
        const string DigimonsDataPath = "Assets/Data/Digimons";
        
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
                
                    if (!File.Exists(digimonArtPath)) {
                        XmlNode image = digimonSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[1]/td/div/div/a/img");
                        if (image != null) {
                            string linkToImage = WikimonBaseURL + image.Attributes.GetNamedItem("src").InnerText;
                            using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(linkToImage)) {
                                await textureRequest.SendWebRequest();
                                if (textureRequest.result != UnityWebRequest.Result.ConnectionError) {
                                    var texture = DownloadHandlerTexture.GetContent(textureRequest);
                                    var data = texture.EncodeToPNG();
                                    var file = File.Create(digimonArtPath);
                                    file.Write(data, 0, data.Length);
                                    file.Close();
                                    AssetDatabase.Refresh();
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
            if (!File.Exists(digimonsFoldersI)) {
                SpriteAtlas spriteAtlas = new SpriteAtlas();
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath(digimonsFoldersI, typeof(UnityEngine.Object));
                spriteAtlas.Add(new UnityEngine.Object[] { folder });
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Finished");
    }

    
    [MenuItem("DigiDex/Clean Local Data")]
    public static void CleanLocalData() {
        if (Directory.Exists("Assets/Art/Digimons")) {
            Directory.Delete("Assets/Art/Digimons", true);
        }
        if (File.Exists("Assets/Art/Digimons.meta")) {
            File.Delete("Assets/Art/Digimons.meta");
        }
        if (Directory.Exists("Assets/Data/Digimons")) {
            Directory.Delete("Assets/Data/Digimons", true);
        }
        if (File.Exists("Assets/Data/Digimons.meta")) {
            File.Delete("Assets/Data/Digimons.meta");
        }
        AssetDatabase.Refresh();
    }
}
