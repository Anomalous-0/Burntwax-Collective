using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor script to remap a Skinned Mesh Renderer's bones to an existing skeleton.
/// 
/// HOW TO USE:
/// 1. Place this script in an "Editor" folder in your project (e.g., Assets/Editor/)
/// 2. Drag your new FBX into the scene as a child of your Arms hierarchy
/// 3. Select the NEW mesh GameObject (the one with the Skinned Mesh Renderer from your new FBX)
/// 4. In the Inspector, right-click the Skinned Mesh Renderer component header
///    OR use the menu: Tools > Burntwax > Remap Bones
/// 5. It will find the existing skeleton under "root" and remap all bones by name
/// </summary>
public class BoneRemapper : EditorWindow
{
    private GameObject newMeshObject;
    private Transform existingRoot;

    [MenuItem("Tools/Burntwax/Remap Bones")]
    public static void ShowWindow()
    {
        GetWindow<BoneRemapper>("Bone Remapper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Burntwax Bone Remapper", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label(
            "This tool remaps a new mesh's Skinned Mesh Renderer bones\n" +
            "to your existing skeleton. Drag in the references below.",
            EditorStyles.wordWrappedLabel
        );
        GUILayout.Space(10);

        newMeshObject = (GameObject)EditorGUILayout.ObjectField(
            "New Mesh Object",
            newMeshObject,
            typeof(GameObject),
            true
        );

        existingRoot = (Transform)EditorGUILayout.ObjectField(
            "Existing Root Bone",
            existingRoot,
            typeof(Transform),
            true
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Remap Bones"))
        {
            if (newMeshObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the New Mesh Object.", "OK");
                return;
            }

            if (existingRoot == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the Existing Root Bone.", "OK");
                return;
            }

            RemapBones();
        }
    }

    private void RemapBones()
    {
        SkinnedMeshRenderer smr = newMeshObject.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            smr = newMeshObject.GetComponentInChildren<SkinnedMeshRenderer>();
        }

        if (smr == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No Skinned Mesh Renderer found on the new mesh object or its children.",
                "OK");
            return;
        }

        // Build a dictionary of all bones in the existing skeleton by name
        Dictionary<string, Transform> existingBones = new Dictionary<string, Transform>();
        Transform[] allChildren = existingRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allChildren)
        {
            if (!existingBones.ContainsKey(t.name))
            {
                existingBones[t.name] = t;
            }
            else
            {
                Debug.LogWarning($"Duplicate bone name found: {t.name}. Using first occurrence.");
            }
        }

        // Remap each bone
        Transform[] currentBones = smr.bones;
        Transform[] remappedBones = new Transform[currentBones.Length];
        int successCount = 0;
        int failCount = 0;
        List<string> missingBones = new List<string>();

        for (int i = 0; i < currentBones.Length; i++)
        {
            string boneName = currentBones[i] != null ? currentBones[i].name : "NULL";

            if (currentBones[i] != null && existingBones.TryGetValue(boneName, out Transform match))
            {
                remappedBones[i] = match;
                successCount++;
            }
            else
            {
                remappedBones[i] = null;
                failCount++;
                missingBones.Add(boneName);
            }
        }

        // Apply the remapped bones
        Undo.RecordObject(smr, "Remap Bones");
        smr.bones = remappedBones;

        // Also remap the root bone
        if (smr.rootBone != null && existingBones.TryGetValue(smr.rootBone.name, out Transform rootMatch))
        {
            smr.rootBone = rootMatch;
        }

        EditorUtility.SetDirty(smr);

        // Report results
        string message = $"Remapped {successCount} bones successfully.";
        if (failCount > 0)
        {
            message += $"\n\n{failCount} bones could not be found:\n" + string.Join("\n", missingBones);
            message += "\n\nThese bones may be extra leaf bones from Blender export.";
        }

        EditorUtility.DisplayDialog("Bone Remap Complete", message, "OK");
        Debug.Log($"[BoneRemapper] {message}");
    }
}
