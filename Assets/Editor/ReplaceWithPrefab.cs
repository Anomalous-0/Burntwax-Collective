using UnityEngine;
using UnityEditor;
using System;

public class ReplaceWithPrefabFinal : EditorWindow
{
    [MenuItem("GameObject/Replace With Prefab (Final Fix)", false, 0)]
    static void ReplaceSelectedObjectsWithPrefab()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No objects selected.");
            return;
        }

        string path = EditorUtility.OpenFilePanel("Choose Prefab to Replace With", "Assets", "prefab");
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No prefab selected.");
            return;
        }

        if (path.StartsWith(Application.dataPath))
            path = "Assets" + path.Substring(Application.dataPath.Length);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError("Could not load prefab at path: " + path);
            return;
        }

        float yOffset = 0.05f;

        int replacedCount = 0;

        foreach (GameObject original in selectedObjects)
        {
            if (original == null)
                continue;

            Transform t = original.transform;

            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (newObj == null)
            {
                Debug.LogError("Failed to instantiate prefab.");
                continue;
            }

            Undo.RegisterCreatedObjectUndo(newObj, "Replace With Prefab");

            newObj.transform.SetParent(t.parent);
            newObj.transform.rotation = t.rotation;
            newObj.transform.localScale = Vector3.one;

            // Positioning with collider fix
            Vector3 basePosition = t.position + new Vector3(0, yOffset, 0);

            Collider col = newObj.GetComponentInChildren<Collider>();
            if (col != null)
            {
                float colliderBottom = col.bounds.min.y;
                float currentY = newObj.transform.position.y;
                float correction = currentY - colliderBottom;

                basePosition += new Vector3(0, correction, 0);
            }

            newObj.transform.position = basePosition;

            // Force Unity to update physics transforms
            Physics.SyncTransforms();

            // Fix Rigidbody physics
            Rigidbody rb = newObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Undo.RecordObject(rb, "Rigidbody Setup");
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // Optional: reset sleep state
                rb.WakeUp();
            }

            // Preserve other properties
            newObj.name = original.name;
            newObj.tag = original.tag;
            newObj.layer = original.layer;

            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Transform child = t.GetChild(i);
                child.SetParent(newObj.transform);
            }

            Undo.DestroyObjectImmediate(original);
            replacedCount++;
        }

        Debug.Log($"Replaced {replacedCount} object(s) with prefab. Physics enabled, colliders aligned.");
    }
}
