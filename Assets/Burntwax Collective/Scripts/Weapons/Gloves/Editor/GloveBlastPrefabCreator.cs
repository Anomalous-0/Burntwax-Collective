using UnityEditor;
using UnityEngine;

namespace Burntwax.EditorTools
{
    /// <summary>
    /// One-click bake of the plasma-blast prefab. Runs the shared <see cref="GloveBlastVisual"/>
    /// builder, saves its material and prefab as real assets (Unity authors the YAML, so no GUID
    /// guesswork), and pings the result. Drop the created prefab into the glove's Blast Prefab field.
    /// </summary>
    public static class GloveBlastPrefabCreator
    {
        private const string RootFolder = "Assets/Burntwax Collective";
        private const string PrefabFolder = RootFolder + "/Prefabs/Weapons";
        private const string MaterialPath = PrefabFolder + "/GloveBlast.mat";
        private const string PrefabPath = PrefabFolder + "/GloveBlast.prefab";

        [MenuItem("Burntwax/Weapons/Create Glove Blast Prefab")]
        public static void CreateGloveBlastPrefab()
        {
            EnsureFolder(RootFolder + "/Prefabs", RootFolder, "Prefabs");
            EnsureFolder(PrefabFolder, RootFolder + "/Prefabs", "Weapons");

            // A runtime-instanced material can't live inside a prefab; it has to be its own asset.
            Material material = GloveBlastVisual.CreatePlasmaMaterial();
            AssetDatabase.CreateAsset(material, MaterialPath);

            GameObject temp = GloveBlastVisual.Create(Vector3.zero, material).gameObject;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            Object.DestroyImmediate(temp);

            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[GloveBlastPrefabCreator] Created {PrefabPath}. Assign it to the glove's Blast Prefab field.");
        }

        private static void EnsureFolder(string path, string parent, string leaf)
        {
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
