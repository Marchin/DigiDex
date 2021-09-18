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
        entry.address = Path.GetFileNameWithoutExtension(entry.AssetPath);
    }
}
