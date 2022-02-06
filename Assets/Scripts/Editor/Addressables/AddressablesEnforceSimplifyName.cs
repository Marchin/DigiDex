using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.IO;
using static UnityEditor.AddressableAssets.Settings.AddressableAssetSettings;

public class AddressablesEnforceSimplifyName {
    [InitializeOnLoadMethod]
    private static void RegisterModificationCallback() {
        AddressableAssetSettings.OnModificationGlobal += OnModification;
    }

    private static void OnModification(
        AddressableAssetSettings settings,
        ModificationEvent eventType,
        object data
    ) {

        switch (data) {
            case AddressableAssetEntry entry: {
                ApplyName(entry);
            } break;
            case List<AddressableAssetEntry> entries: {
                foreach (var entry in entries) {
                    ApplyName(entry);
                }
            } break;
            case AddressableAssetGroup group: {
                foreach (var entry in group.entries) {
                    ApplyName(entry);
                }
            } break;
        }
    }

    private static void ApplyName(AddressableAssetEntry entry) {
        Popup popup = (entry.MainAsset as GameObject)?.GetComponent<Popup>();
        if (popup != null) {
            string fileName = Path.GetFileNameWithoutExtension(entry.AssetPath);
            string sufix = popup.Vertical ? Popup.VerticalSufix : "";
            string forcedName = popup.GetType().Name + sufix;
            entry.address = forcedName;
            entry.labels.Add(popup.Vertical ? "popup_vertical" : "popup");
            if (fileName != forcedName) {
                AssetDatabase.RenameAsset(entry.AssetPath, entry.AssetPath.Replace(fileName, forcedName));
            }
        } else {
            entry.address = Path.GetFileNameWithoutExtension(entry.AssetPath);
        }
    }
}
