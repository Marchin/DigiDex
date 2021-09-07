using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DigimonDatabase : ScriptableObject {
    public List<DigimonReference> Digimons;
    public List<FieldReference> Fields;
    public List<AttributeReference> Attributes;
    public List<Type> Types;
    public List<Level> Levels;

    public async UniTask<List<InformationData>> ExtractDigimonData(Digimon digimon) {
        List<InformationData> information = new List<InformationData>();

        if (digimon != null) {
            information.Add(new InformationData { Prefix = "Name", Content = digimon.Name });

            if (digimon.AttributeIDs.Count > 0) {
                information.Add(new InformationData { Prefix = "Attributes" });
                for (int iAttribute = 0; iAttribute < digimon.AttributeIDs.Count; ++iAttribute) {
                    information.Add(new InformationData { Content = Attributes[digimon.AttributeIDs[iAttribute]].Name, IndentLevel = 1 });
                }
            }

            if (digimon.TypeIDs.Count > 0) {
                information.Add(new InformationData { Prefix = "Type" });
                for (int iType = 0; iType < digimon.TypeIDs.Count; ++iType) {
                    information.Add(new InformationData { Content = Types[digimon.TypeIDs[iType]].Name, IndentLevel = 1 });
                }
            }

            if (digimon.FieldIDs.Count > 0) {
                information.Add(new InformationData { Prefix = "Fields" });
                for (int iField = 0; iField < digimon.FieldIDs.Count; ++iField) {
                    Field field = await Addressables.LoadAssetAsync<Field>(Fields[digimon.FieldIDs[iField]].Data);
                    information.Add(new InformationData { Content = field.Name, SpriteReference = field.Sprite, IndentLevel = 1 });
                }
            }
        }

        return information;
    }
}
