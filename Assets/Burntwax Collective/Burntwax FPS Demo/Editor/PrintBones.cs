using UnityEngine;
using UnityEditor;

/// <summary>
/// Select a GameObject with a Skinned Mesh Renderer,
/// then go to Tools > Burntwax > Print Bones
/// </summary>
public class PrintBones : MonoBehaviour
{
    [MenuItem("Tools/Burntwax/Print Bones")]
    public static void LogBones()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            Debug.LogError("No GameObject selected.");
            return;
        }

        SkinnedMeshRenderer smr = selected.GetComponent<SkinnedMeshRenderer>();
        if (smr == null)
        {
            Debug.LogError("No Skinned Mesh Renderer found on selected object.");
            return;
        }

        Transform[] bones = smr.bones;

        if (bones == null || bones.Length == 0)
        {
            Debug.LogWarning("Bones array is NULL or EMPTY.");
            return;
        }

        Debug.Log($"=== BONES ON: {selected.name} === (Total: {bones.Length})");
        for (int i = 0; i < bones.Length; i++)
        {
            string boneName = bones[i] != null ? bones[i].name : "NULL";
            string path = bones[i] != null ? GetPath(bones[i]) : "---";
            Debug.Log($"  Bone [{i}]: {boneName}  |  Path: {path}");
        }

        if (smr.rootBone != null)
            Debug.Log($"  Root Bone: {smr.rootBone.name}");
        else
            Debug.LogWarning("  Root Bone: NULL");
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        Transform parent = t.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
