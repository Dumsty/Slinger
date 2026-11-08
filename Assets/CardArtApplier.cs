#if UNITY_EDITOR
// Wrapped in UNITY_EDITOR so this file can live in any folder without breaking game builds.
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool: Tools > SLINGERS > Apply Card Art
///
/// For every card prefab in Assets/Objects/Cards, finds the PNG with the same name
/// (anywhere in Assets), makes an unlit material from it, and adds (or updates) a
/// "Card Art" quad on the front face of the card. The old "Text (TMP)" name label is
/// hidden because the name is already printed on the art. If the card's "icon" field
/// is empty, the art is also assigned there so buff icons show something.
///
/// Safe to run again after you change or add art - it updates in place.
/// </summary>
public static class CardArtApplier
{
    const string PrefabFolder   = "Assets/Objects/Cards";
    const string ArtChildName   = "Card Art";
    const string TextChildName  = "Text (TMP)";

    // Colour for the card model itself, so any edge that shows around the art blends
    // into the black outline of the card art instead of showing red/cyan/white.
    static readonly Color CardBodyColor = new Color(0.03f, 0.03f, 0.04f, 1f);

    const string ToolVersion = "v4";

    // How much of the card face the art covers (1 = edge to edge). Slightly over 1 so
    // no sliver of the card model peeks out around the art.
    const float FaceFill = 1.01f;

    [MenuItem("Tools/SLINGERS/Apply Card Art")]
    public static void ApplyAll()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Mesh quadMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        int applied = 0, missing = 0;

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string cardName = Path.GetFileNameWithoutExtension(prefabPath);
            string artPath = FindArt(cardName);

            if (artPath == null)
            {
                Debug.LogWarning($"[Card Art] No art found for '{cardName}'. Add a PNG named '{cardName}.png' anywhere in Assets.");
                missing++;
                continue;
            }

            Sprite sprite = PrepareTexture(artPath);
            Material mat = GetOrCreateMaterial(cardName, artPath, sprite.texture, shader);

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (AttachArt(root, quadMesh, mat, sprite.texture))
                {
                    DarkenCardBody(root);
                    HideNameText(root);
                    FillEmptyIcon(root, sprite);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    applied++;
                    Debug.Log($"[Card Art] Applied art to {cardName}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog($"Apply Card Art ({ToolVersion})",
            $"Applied art to {applied} card(s).\n{missing} card(s) had no matching image (see Console).",
            "OK");
    }

    /// Imports the PNG as a Sprite so it works both as a texture and for the card's icon field.
    static Sprite PrepareTexture(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (importer.maxTextureSize != 1024)  { importer.maxTextureSize = 1024; changed = true; }
        if (!importer.alphaIsTransparency)    { importer.alphaIsTransparency = true; changed = true; }
        if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }

        if (changed) importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// Finds "<cardName>.png" anywhere under Assets, so it doesn't matter which folder the art is in.
    static string FindArt(string cardName)
    {
        string fallback = null;
        foreach (string guid in AssetDatabase.FindAssets($"\"{cardName}\" t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileName(path).Equals(cardName + ".png", System.StringComparison.OrdinalIgnoreCase))
                continue;
            // Prefer the card-art folder if another image happens to share the name.
            if (path.Contains("/Cards/Art/")) return path;
            if (fallback == null) fallback = path;
        }
        return fallback;
    }

    static Material GetOrCreateMaterial(string cardName, string artPath, Texture2D tex, Shader shader)
    {
        // Materials go in a "Materials" folder next to the art.
        string artFolder = Path.GetDirectoryName(artPath).Replace('\\', '/');
        string matFolder = artFolder + "/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
            AssetDatabase.CreateFolder(artFolder, "Materials");

        string matPath = $"{matFolder}/{cardName} Art.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.shader = shader;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// Adds a quad sized to the card's front face, whichever way the card mesh is oriented.
    static bool AttachArt(GameObject root, Mesh quadMesh, Material mat, Texture2D tex)
    {
        MeshFilter cardFilter = root.GetComponent<MeshFilter>();
        if (cardFilter == null || cardFilter.sharedMesh == null)
        {
            Debug.LogWarning($"[Card Art] '{root.name}' has no card mesh on its root object - skipped.");
            return false;
        }

        Bounds b = cardFilter.sharedMesh.bounds;

        Transform art = root.transform.Find(ArtChildName);
        if (art == null)
        {
            art = new GameObject(ArtChildName).transform;
            art.SetParent(root.transform, false);
        }

        // Note: Unity objects must be null-checked with ==, not ??, or the check can silently fail.
        var mf = art.GetComponent<MeshFilter>();
        if (mf == null) mf = art.gameObject.AddComponent<MeshFilter>();
        var mr = art.GetComponent<MeshRenderer>();
        if (mr == null) mr = art.gameObject.AddComponent<MeshRenderer>();
        mf.sharedMesh = quadMesh;
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // Work out which way the card faces: its thinnest side is its thickness.
        Vector3 size = b.size;
        float faceW, faceH, thickness;
        Vector3 pos;
        Quaternion rot;

        if (size.z <= size.x && size.z <= size.y)
        {
            // Upright card facing -Z (same side as the old name text).
            faceW = size.x; faceH = size.y; thickness = size.z;
            pos = new Vector3(b.center.x, b.center.y, b.min.z);
            rot = Quaternion.identity;
        }
        else if (size.y <= size.x && size.y <= size.z)
        {
            // Card lying flat, facing +Y.
            faceW = size.x; faceH = size.z; thickness = size.y;
            pos = new Vector3(b.center.x, b.max.y, b.center.z);
            rot = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            // Card turned sideways, facing -X.
            faceW = size.z; faceH = size.y; thickness = size.x;
            pos = new Vector3(b.min.x, b.center.y, b.center.z);
            rot = Quaternion.Euler(0f, 90f, 0f);
        }

        // Nudge the art just off the surface so it doesn't flicker against the card.
        float nudge = Mathf.Max(thickness * 0.05f, Mathf.Max(faceW, faceH) * 0.001f);
        pos += rot * new Vector3(0f, 0f, -nudge);

        // Stretch the art to cover the whole card face so none of the card model shows.
        float w = faceW * FaceFill;
        float h = faceH * FaceFill;

        art.localPosition = pos;
        art.localRotation = rot;
        art.localScale = new Vector3(w, h, 1f);
        return true;
    }

    /// Turns the card model's own material near-black so its edges match the art's black border.
    static void DarkenCardBody(GameObject root)
    {
        var body = root.GetComponent<MeshRenderer>();
        if (body == null) return;
        foreach (Material m in body.sharedMaterials)
        {
            if (m == null) continue;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", CardBodyColor);
            if (m.HasProperty("_Color")) m.SetColor("_Color", CardBodyColor);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(m);
        }
    }

    static void HideNameText(GameObject root)
    {
        Transform text = root.transform.Find(TextChildName);
        if (text != null) text.gameObject.SetActive(false);
    }

    static void FillEmptyIcon(GameObject root, Sprite sprite)
    {
        foreach (var mb in root.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            var so = new SerializedObject(mb);
            var icon = so.FindProperty("icon");
            if (icon != null && icon.propertyType == SerializedPropertyType.ObjectReference
                && icon.objectReferenceValue == null)
            {
                icon.objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
