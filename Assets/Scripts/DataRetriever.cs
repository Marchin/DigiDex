using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System.Xml;
using System.IO;

public class DataRetriever {
    [MenuItem("DigiDex/Retrieve Data")]
    public static async void RetrieveData() {
        const string WikimonBaseURL = "https://wikimon.net";
        const string DigimonList = WikimonBaseURL + "/List_of_Digimon";
        const string DigimonsFolderX = "Assets/Art/Digimons/Digimon[{0}]";
        
        // TODO: Add the new images either in the last folder or on a new one depending on the wether the last folder is full

        XmlDocument doc = new XmlDocument();
        doc.Load(DigimonList);
        XmlNodeList table = doc.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[@class='wikitable']/tbody/tr/td[1]/a");
        int digimonCount = 0;
        for (int i = 1; i < table.Count; i++) {
            string digimonLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";

            if (!string.IsNullOrEmpty(digimonLinkSubFix)) {
                string digimonNameSafe = digimonLinkSubFix.Replace(":", string.Empty);
                string artPath = string.Format(DigimonsFolderX, digimonCount / 16);
                string digimonArtPath = artPath + digimonNameSafe + ".png";
                if (!File.Exists(digimonArtPath)) {
                    if (!Directory.Exists(artPath)) {
                        Directory.CreateDirectory(artPath);
                    }
                    string digimonLink = WikimonBaseURL + digimonLinkSubFix;
                    try {
                        doc.Load(digimonLink);
                        XmlNode image = doc.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[1]/td/div/div/a/img");
                        if (image != null) {
                            string linkToImage = WikimonBaseURL + image.Attributes.GetNamedItem("src").InnerText;
                            using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(linkToImage)) {
                                await textureRequest.SendWebRequest();
                                if (!textureRequest.isNetworkError) {
                                    var texture = DownloadHandlerTexture.GetContent(textureRequest);
                                    var data = texture.EncodeToPNG();
                                    var file = File.Create(digimonArtPath);
                                    file.Write(data, 0, data.Length);
                                    file.Close();
                                    ++digimonCount;
                                }
                            }
                        }
                    } catch {
                        Debug.Log($"Could not retrieve image from {digimonLinkSubFix}");
                    }
                } else {
                    ++digimonCount;
                }
            }
        }

        int folderCount = digimonCount / 16;
        for (int i = 0; i < folderCount; i++) {
            string digimonsFoldersI = string.Format(DigimonsFolderX, i);
            string spriteAtlasPath = digimonsFoldersI + ".spriteatlas";
            if (!File.Exists(digimonsFoldersI)) {
                SpriteAtlas spriteAtlas = new SpriteAtlas();
                Object folder = AssetDatabase.LoadAssetAtPath(digimonsFoldersI, typeof(Object));
                spriteAtlas.Add(new Object[] { folder });
                AssetDatabase.CreateAsset(spriteAtlas, spriteAtlasPath);
            }
        }
    }
}
