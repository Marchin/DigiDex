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

public static class CharacterDataRetriever {
    public const string CharacterSpriteAtlasesGroupName = "Character Sprite Atlases";
    public const string CharacterListGroupName = "Character List";
    public const string CharacterDataGroupName = "Character Data";
    public const string CharacterEvolutionDataGroupName = "Character Evolution Data";
    public const string CharacterGroupListSubFix = "/Group";
    public const int CharactersPerAtlas = 3;
    public const string ArtCharactersPath = DataRetriever.RemoteArtPath + "Characters/";
    public const string CharacterDataPath = DataRetriever.DataPath + "Character/";
    public const string CharactersDataPath = CharacterDataPath + "Characters/";
    public const string CharacterEvolutionsDataPath = CharactersDataPath + "Evolutions/";
    public const string CharacterDBPath = CharacterDataPath + "Character Database.asset";

    public static CharacterDatabase GetCharacterDatabase() {
        if (!Directory.Exists(CharacterDataPath)) {
            Directory.CreateDirectory(CharacterDataPath);
        }

        CharacterDatabase characterDB = DataRetriever.GetOrCreateScriptableObject<CharacterDatabase>(CharacterDBPath);

        return characterDB;
    }

    public static async UniTask<Character> AddCharacter(string characterLinkSubFix) {
        Character characterData = null;

        try {
            var dataGroup = DataRetriever.GetOrAddAddressableGroup(CharacterListGroupName);

            var spriteAtlasGroup = DataRetriever.GetOrAddAddressableGroup(CharacterSpriteAtlasesGroupName);
            var schema = spriteAtlasGroup.GetSchema<BundledAssetGroupSchema>();
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            var addressablesSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            XmlDocument characterSite = await DataRetriever.GetSite(characterLinkSubFix);
            string characterName = characterSite.SelectSingleNode("//*[@id='firstHeading']").InnerText;
            string characterNameSafe = characterName.AddresableSafe();
            string characterArtPath = ArtCharactersPath + characterNameSafe + ".png";
            string characterDataPath = CharactersDataPath + "/" + characterNameSafe + ".asset";

            CharacterDatabase characterDB = GetCharacterDatabase();

            if (!Directory.Exists(ArtCharactersPath)) {
                Directory.CreateDirectory(ArtCharactersPath);
            }
            if (!Directory.Exists(CharactersDataPath)) {
                Directory.CreateDirectory(CharactersDataPath);
            }
            
            Dictionary<string, int> imagesToSkip = new Dictionary<string, int> {};

            bool hasArt = false;
            if (!File.Exists(characterArtPath)) {
                XmlNode image = characterSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr[1]/td/div/div/a");
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
                        if (imagesToSkip.ContainsKey(characterNameSafe)) {
                            int toRemove = Mathf.Min(imagesToSkip[characterNameSafe], imagesList.Count - 1);
                            
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
                                    var file = File.Create(characterArtPath);
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
            
            if (!File.Exists(characterDataPath)) {
                characterData = ScriptableObject.CreateInstance<Character>();
                AssetDatabase.CreateAsset(characterData, characterDataPath);
            } else {
                characterData = AssetDatabase.LoadAssetAtPath<Character>(characterDataPath);
            }

            characterData.LinkSubFix = characterLinkSubFix;
            characterData.Hash = Hash128.Compute(characterData.LinkSubFix);
            characterData.Name = characterName;

            XmlNode profileNode = characterSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                characterSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                characterSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td") ??
                characterSite.SelectSingleNode("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[1]/div[2]/table/tbody/tr[2]/td/div[1]/table/tbody/tr[1]/td/p");

            if (profileNode != null) {
                if (profileNode.FirstChild?.LocalName == "span") {
                    // Remove the "Japanese/English" Toggle
                    profileNode.RemoveChild(profileNode.FirstChild);
                }
                characterData.Profile = profileNode.InnerText;
            } else {
                Debug.Log($"No profile found for {characterNameSafe}");
            }

            XmlNodeList properties = characterSite.SelectNodes("/html/body/div/div[2]/div[2]/div[3]/div[3]/div/table[1]/tbody/tr/td[3]/div[2]/table/tbody/tr[2]/td/table[2]/tbody/tr");
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
                
                XmlNode valueNode = dataNode.FirstChild;
                while (valueNode != null) {
                    if (fieldType == "a" || fieldType == "#text") {
                        string propertyName = valueNode.InnerText;
                    }
                    valueNode = valueNode.NextSibling;
                    fieldType = valueNode?.Name;
                }
            }

            XmlNode dubNode = characterSite.SelectSingleNode("/html/body/div/div/div/div/div/div/table/tbody/tr/td/div/table/tbody/tr/td/div/table/tbody/tr/td/table/tbody/tr/td/table/tbody/tr/td[b='Dub:']");
            characterData.DubNames = new List<string>();
            if (dubNode != null) {
                XmlNode test = dubNode.NextSibling;
                while ((test != null) && (test.InnerText == "")) {
                    test = test.NextSibling;
                }
                if (test != null) {
                    XmlNode child = test.FirstChild;
                    while (child != null) {
                        if (child.Name == "i") {
                            characterData.DubNames.Add(child.FirstChild?.InnerText ?? child.InnerText);
                        }
                        child = child.NextSibling;
                    }
                }
            }

            EditorUtility.SetDirty(characterData);
            AssetDatabase.SaveAssets();

            if (hasArt) {
                // charactersWithArt.Add((characterData, characterArtPath));
            }
        
            addressablesSettings.CreateOrMoveEntry(AssetDatabase.GUIDFromAssetPath(characterDataPath).ToString(), dataGroup);
        } catch (Exception ex) {
            Debug.LogError($"{characterLinkSubFix} - {ex.Message} \n {ex.StackTrace}");
        }
        
        return characterData;
    }
}
