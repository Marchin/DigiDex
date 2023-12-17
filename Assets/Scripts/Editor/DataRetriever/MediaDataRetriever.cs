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

public static class MediaDataRetriever {
    public const string MediaSpriteAtlasesGroupName = "Media Sprite Atlases";
    public const string VideoGamesListSubFix = "/List_of_Video_Games";
    public const string MediaListGroupName = "Media List";
    public const string MediaDataGroupName = "Media Data";
    public const string ArtMediaPath = DataRetriever.RemoteArtPath + "Media/";
    public const string MediaDataPath = DataRetriever.DataPath + "Media/";
    public const string VideoGamesDataPath = MediaDataPath + "Video Games";
    public const string MediaDBPath = MediaDataPath + "Media Database.asset";

    public static MediaDatabase GetMediaDatabase() {
        if (!Directory.Exists(MediaDataPath)) {
            Directory.CreateDirectory(MediaDataPath);
        }

        MediaDatabase mediaDB = DataRetriever.GetOrCreateScriptableObject<MediaDatabase>(MediaDBPath);

        return mediaDB;
    }

    [MenuItem("DigiDex/Media/Retrieve Video Games")]
    public static async void RetrieveData() {
        if (!Directory.Exists(VideoGamesDataPath)) {
            Directory.CreateDirectory(VideoGamesDataPath);
        }

        var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

        if (!Directory.Exists(VideoGamesDataPath)) {
            Directory.CreateDirectory(VideoGamesDataPath);
        }

        var dataGroup = DataRetriever.GetOrAddAddressableGroup(MediaListGroupName);

        var spriteAtlasGroup = DataRetriever.GetOrAddAddressableGroup(MediaSpriteAtlasesGroupName);
        var schema = spriteAtlasGroup.GetSchema<BundledAssetGroupSchema>();
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        
        MediaDatabase mediaDB = GetMediaDatabase();
        DataCenter dataCenter = DataRetriever.GetCentralDatabase();

        List<(VideoGame videogame, string path)> videogamesWithArt = new List<(VideoGame videogame, string path)>();
        List<VideoGame> videogames = new List<VideoGame>();
        Dictionary<string, int> imagesToSkip = new Dictionary<string, int> {
        };

        XmlDocument videogameListSite = await DataRetriever.GetSite(VideoGamesListSubFix);

CharacterDataRetriever.ClearCharacterArtQueue();
        XmlNodeList table = videogameListSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/ul/li/a");
        for (int i = 0; i < table.Count; i++) {
            string videogameLinkSubFix = table.Item(i)?.Attributes.Item(0)?.InnerText ?? "";
            if (!string.IsNullOrEmpty(videogameLinkSubFix)) {
                try {
                    XmlDocument videogameSite = await DataRetriever.GetSite(videogameLinkSubFix);

                    if (videogames.Find(d => d.LinkSubFix == videogameLinkSubFix)) {
                        continue;
                    }

                    string videogameName = videogameSite.SelectSingleNode("//*[@id='firstHeading']")?.InnerText;
                    
                    if (videogameName == null) {
                        continue;
                    }

                    string videogameNameSafe = videogameName.AddresableSafe();
                    string videogameArtPath = ArtMediaPath + videogameNameSafe + ".png";
                    string videogameDataPath = VideoGamesDataPath + "/" + videogameNameSafe + ".asset";

                    if (!Directory.Exists(ArtMediaPath)) {
                        Directory.CreateDirectory(ArtMediaPath);
                    }
                
                    if (!Directory.Exists(VideoGamesDataPath)) {
                        Directory.CreateDirectory(VideoGamesDataPath);
                    }

                    bool hasArt = false;
                    if (!File.Exists(videogameArtPath)) {
                        XmlNode image = videogameSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr[3]/td/div[1]/div/a");
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
                                if (imagesToSkip.ContainsKey(videogameNameSafe)) {
                                    int toRemove = Mathf.Min(imagesToSkip[videogameNameSafe], imagesList.Count - 1);
                                    
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
                                            var file = File.Create(videogameArtPath);
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
                    
                    VideoGame videogameData = null;
                    if (!File.Exists(videogameDataPath)) {
                        videogameData = ScriptableObject.CreateInstance<VideoGame>();
                        AssetDatabase.CreateAsset(videogameData, videogameDataPath);
                    } else {
                        videogameData = AssetDatabase.LoadAssetAtPath<VideoGame>(videogameDataPath);
                    }

                    videogameData.LinkSubFix = videogameLinkSubFix;
                    videogameData.Hash = Hash128.Compute(videogameData.LinkSubFix);
                    videogameData.Name = videogameName;
                    videogameData.Profile = "No description available";
                    videogameData.CharactersSet = new List<CharactersSet>();

                    bool storyFound = false;
                    XmlNodeList headers = videogameSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/h1");
                    for (int iNode = 0; iNode < headers.Count; ++iNode) {
                        XmlNode node = headers[iNode];
                        if (node.FirstChild.InnerText == "Story") {
                            if (node.NextSibling.Name == "p") {
                                videogameData.Profile = node.NextSibling.InnerText;
                                storyFound = true;
                            }
                        }

                        string loweredHeader = node.FirstChild.InnerText.ToLower();
                        if (loweredHeader.Contains("character") || loweredHeader.Contains("digimon")) {
                            XmlNode auxNode = node.NextSibling;
                            string lastCategoryName = node.FirstChild.InnerText;
                            while ((auxNode != null) && (auxNode.Name != "h1")) {
                                if (auxNode.Name.Contains("h")) {
                                    lastCategoryName = auxNode.FirstChild.InnerText;
                                }

                                if (lastCategoryName == "Trivia") {
                                    continue;
                                }

                                CharactersSet set = new CharactersSet {
                                    Name = lastCategoryName,
                                    CharacterRefs = new List<CharacterReference>()
                                };

                                XmlNodeList characters = auxNode.SelectNodes(".//li");

                                for (int iCharacter = 0; iCharacter < characters.Count && iCharacter < 10; ++iCharacter) {
                                    XmlNodeList characterNodes = characters[iCharacter].SelectNodes(".//a");
                                    for (int jCharacter = 0; jCharacter < characterNodes.Count && jCharacter < 10; ++jCharacter) {
                                        XmlNode characterNode = characterNodes[jCharacter];
                                        CharacterReference characterRef = new CharacterReference();
                                        string linkSubFix = characterNode.Attributes.GetNamedItem("href")?.InnerText ?? "";

                                        if (!string.IsNullOrEmpty(linkSubFix) && (linkSubFix[0] == '/')) {
                                            if (!DataRetriever.SitesFinalLink.ContainsKey(linkSubFix)) {
                                                await DataRetriever.GetSite(linkSubFix, finalLink => linkSubFix = finalLink);
                                            } else {
                                                linkSubFix = DataRetriever.SitesFinalLink[linkSubFix];
                                            }

                                            XmlDocument characterSite = await DataRetriever.GetSite(linkSubFix);
                                            characterRef.Name = characterSite.SelectSingleNode("//*[@id='firstHeading']")?.InnerText;

                                            if (string.IsNullOrEmpty(characterRef.Name)) {
                                                continue;
                                            }

                                            IDataEntry entry = dataCenter.DigimonDB.Digimons.Find(
                                                d => string.Compare(d.Name, characterRef.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
                                            
                                            if (entry == null) {
                                                entry = dataCenter.AppmonDB.Appmons.Find(
                                                    a => string.Compare(a.Name, characterRef.Name, StringComparison.InvariantCultureIgnoreCase) == 0);
                                            }

                                            if (entry == null) {
                                                entry = await CharacterDataRetriever.AddCharacter(linkSubFix);
                                            }

                                            if (entry != null) {
                                                characterRef.Name = entry.Name;
                                                characterRef.Index = new EntryIndex(entry.GetType(), entry.Hash);
                                            }
                                        }

                                        if (set.CharacterRefs.Find(character => characterRef.Name == character.Name) == null) {
                                            set.CharacterRefs.Add(characterRef);
                                        }
                                    }
                                }

                                if (set.CharacterRefs.Count > 0) {
                                    videogameData.CharactersSet.Add(set);
                                }

                                auxNode = auxNode.NextSibling;
                            }
                        }
                    }

                    if (!storyFound) {
                        headers = videogameSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/h2");
                        for (int iNode = 0; iNode < headers.Count; ++iNode) {
                            XmlNode node = headers[iNode];
                            if (node.FirstChild.InnerText == "Story") {
                                if (node.NextSibling.Name == "p") {
                                    videogameData.Profile = node.NextSibling.InnerText;
                                    storyFound = true;
                                    break;
                                }
                            }
                        }
                    }
                    // XmlNode profileNode = videogameSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/p");

                    // if (profileNode != null) {
                    //     videogameData.Profile = profileNode.InnerText;
                    // } else {
                    //     Debug.Log($"No profile found for {videogameNameSafe}");
                    // }

                    if (hasArt) {
                        videogamesWithArt.Add((videogameData, videogameArtPath));
                    }

                    videogameData.DubNames = new List<string>();
                    videogameData.ReleaseDates = new List<string>();
                    videogameData.Systems = new List<string>();

                    XmlNodeList properties = videogameSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr");
                    for (int iProperties = 0; iProperties < properties.Count; ++iProperties) {
                        XmlNode dataNode = properties.Item(iProperties).FirstChild;
                        if (dataNode == null) {
                            continue;
                        }
                        
                        string propertyName = dataNode.InnerText.Trim();

                        switch (propertyName) {
                            case "Name": {
                                for (int iNode = 0; iNode < dataNode.NextSibling.ChildNodes.Count; ++iNode) {
                                    var node = dataNode.NextSibling.ChildNodes[iNode];
                                    if (node.Name == "#text" || node.Name == "b") {
                                        string name = node.InnerText.Trim()
                                            .Replace("\n", "").Replace("|", "").Trim();

                                        if (name != videogameName) {
                                            videogameData.DubNames.Add(name);
                                        }
                                    }
                                }
                            } break;
                            case "Release Date": {
                                for (int iNode = 0; iNode < dataNode.NextSibling.ChildNodes.Count; ++iNode) {
                                    var node = dataNode.NextSibling.ChildNodes[iNode];
                                    if (node.Name == "#text" || node.Name == "b") {
                                        string date = node.InnerText.Trim()
                                            .Replace("\n", "").Replace("|", "").Trim();
                                        videogameData.ReleaseDates.Add(date);
                                    }
                                }
                            } break;
                            case "System": {
                                for (int iNode = 0; iNode < dataNode.NextSibling.ChildNodes.Count; ++iNode) {
                                    var node = dataNode.NextSibling.ChildNodes[iNode];
                                    if (node.Name == "#text" || node.Name == "b") {
                                        string system = node.InnerText.Trim()
                                            .Replace("\n", "").Replace("|", "").Trim();
                                        videogameData.Systems.Add(system);
                                    }
                                }
                            } break;
                        }
                    }

                    EditorUtility.SetDirty(videogameData);
                    AssetDatabase.SaveAssets();

                    videogames.Add(videogameData);

                    addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(videogameDataPath).ToString(), dataGroup);
                } catch (Exception ex) {
                    Debug.LogError($"{videogameLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
                }
            }
        }
        Debug.LogError("Done");
        CharacterDataRetriever.PackCharacterArt();
    }
}