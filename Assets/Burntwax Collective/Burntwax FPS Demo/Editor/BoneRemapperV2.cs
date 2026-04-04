using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Remaps a new Skinned Mesh Renderer's bones to an existing skeleton.
/// 
/// HOW TO USE:
/// 1. Place this script in an "Editor" folder (e.g., Assets/Editor/)
/// 2. Drag your new FBX into the scene (anywhere)
/// 3. Go to Tools > Burntwax > Remap Bones V2
/// 4. "New Mesh Object" = the GameObject with the Skinned Mesh Renderer from your new FBX
/// 5. "Existing Skeleton Root" = the "root" transform from your OLD Arms hierarchy
///    (the one under Arms, NOT under NewArms)
/// 6. Hit Remap — it will match bones by name from the old skeleton only
/// </summary>
public class BoneRemapperV2 : EditorWindow
{
    private GameObject newMeshObject;
    private Transform existingSkeletonRoot;

    [MenuItem("Tools/Burntwax/Remap Bones V2")]
    public static void ShowWindow()
    {
        GetWindow<BoneRemapperV2>("Bone Remapper V2");
    }

    private void OnGUI()
    {
        GUILayout.Label("Burntwax Bone Remapper V2", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label(
            "Drag in the NEW mesh and the OLD skeleton root.\n" +
            "The old root should be the 'root' transform under your\n" +
            "original Arms hierarchy (NOT the one from the new FBX).",
            EditorStyles.wordWrappedLabel
        );
        GUILayout.Space(10);

        newMeshObject = (GameObject)EditorGUILayout.ObjectField(
            "New Mesh Object",
            newMeshObject,
            typeof(GameObject),
            true
        );

        existingSkeletonRoot = (Transform)EditorGUILayout.ObjectField(
            "Existing Skeleton Root",
            existingSkeletonRoot,
            typeof(Transform),
            true
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Remap Bones"))
        {
            if (newMeshObject == null || existingSkeletonRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign both fields.", "OK");
                return;
            }
            RemapBones();
        }
    }

    private void RemapBones()
    {
        // Find all Skinned Mesh Renderers on the new object
        SkinnedMeshRenderer[] smrs = newMeshObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        if (smrs.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No Skinned Mesh Renderers found.", "OK");
            return;
        }

        // Build dictionary from the EXISTING skeleton ONLY
        // We search specifically under the provided root transform
        Dictionary<string, Transform> existingBones = new Dictionary<string, Transform>();
        CollectBones(existingSkeletonRoot, existingBones);

        Debug.Log($"[BoneRemapperV2] Found {existingBones.Count} bones in existing skeleton.");

        int totalRemapped = 0;
        int totalFailed = 0;
        List<string> allMissing = new List<string>();

        foreach (SkinnedMeshRenderer smr in smrs)
        {
            Debug.Log($"[BoneRemapperV2] Remapping: {smr.gameObject.name}");

            Transform[] oldBones = smr.bones;
            Transform[] newBones = new Transform[oldBones.Length];

            for (int i = 0; i < oldBones.Length; i++)
            {
                string boneName = oldBones[i] != null ? oldBones[i].name : null;

                if (boneName != null && existingBones.TryGetValue(boneName, out Transform match))
                {
                    newBones[i] = match;
                    totalRemapped++;
                }
                else
                {
                    newBones[i] = null;
                    totalFailed++;
                    if (boneName != null)
                        allMissing.Add($"{smr.gameObject.name}: {boneName}");
                }
            }

            Undo.RecordObject(smr, "Remap Bones V2");
            smr.bones = newBones;

            // Remap root bone
            if (smr.rootBone != null && existingBones.TryGetValue(smr.rootBone.name, out Transform rootMatch))
            {
                smr.rootBone = rootMatch;
                Debug.Log($"[BoneRemapperV2] Root bone remapped to: {rootMatch.name}");
            }

            EditorUtility.SetDirty(smr);
        }

        // Report
        string msg = $"Remapped {totalRemapped} bones across {smrs.Length} mesh(es).";
        if (totalFailed > 0)
        {
            msg += $"\n\n{totalFailed} bones could not be matched:\n" + string.Join("\n", allMissing);
        }
        else
        {
            msg += "\n\nAll bones matched successfully!";
        }

        EditorUtility.DisplayDialog("Remap Complete", msg, "OK");
        Debug.Log($"[BoneRemapperV2] {msg}");
    }

    private void CollectBones(Transform root, Dictionary<string, Transform> dict)
    {
        // Add the root itself
        if (!dict.ContainsKey(root.name))
            dict[root.name] = root;

        // Recursively add all children
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (!dict.ContainsKey(child.name))
                dict[child.name] = child;
            CollectBones(child, dict);
        }
    }
}
