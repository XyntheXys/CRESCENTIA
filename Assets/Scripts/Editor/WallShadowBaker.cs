using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Bakes a soft shadow of the wall and platform tiles onto the background, so the playable layer
// stands out from the scenery. Run again after editing tiles: menu CRESCENTIA > Bake Wall Shadow.
public static class WallShadowBaker
{
    private const int pixelsPerUnit = 8;
    private const int paddingUnits = 4;
    private const int blurRadius = 4;        // pixels per box-blur pass, 3 passes approximate a gaussian
    private const string outputFolder = "Assets/Arts/MapLevels/Lighting";
    private static readonly Vector2 lightOffset = new Vector2(0.15f, -0.4f); // world units, light from upper left

    [MenuItem("CRESCENTIA/Bake Wall Shadow")]
    public static void Bake()
    {
        Tilemap walls = FindTilemap("Grid/walls");
        Tilemap platforms = FindTilemap("Grid/platforms");
        if (walls == null)
        {
            Debug.LogError("Bake Wall Shadow: the open scene has no Grid/walls tilemap.");
            return;
        }

        BoundsInt cells = UsedCellBounds(walls, platforms);
        int width = (cells.size.x + paddingUnits * 2) * pixelsPerUnit;
        int height = (cells.size.y + paddingUnits * 2) * pixelsPerUnit;
        Vector3 origin = walls.CellToWorld(new Vector3Int(cells.xMin - paddingUnits, cells.yMin - paddingUnits, 0));

        // Solid mask, shifted by the light offset
        float[] mask = new float[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3 world = origin + new Vector3((x + 0.5f) / pixelsPerUnit - lightOffset.x, (y + 0.5f) / pixelsPerUnit - lightOffset.y, 0f);
                bool solid = walls.HasTile(walls.WorldToCell(world)) || (platforms != null && platforms.HasTile(platforms.WorldToCell(world)));
                mask[y * width + x] = solid ? 1f : 0f;
            }
        }

        for (int pass = 0; pass < 3; pass++)
        {
            mask = BoxBlur(mask, width, height, blurRadius, true);
            mask = BoxBlur(mask, width, height, blurRadius, false);
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(mask[i] * 255f));
        texture.SetPixels32(pixels);
        texture.Apply();

        Scene scene = walls.gameObject.scene;
        Directory.CreateDirectory(outputFolder);
        string path = $"{outputFolder}/{scene.name}_WallShadow.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        ConfigureImporter(path, Mathf.Max(width, height));
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        SpriteRenderer shadow = FindOrCreateRenderer();
        Undo.RecordObject(shadow, "Bake Wall Shadow");
        Undo.RecordObject(shadow.transform, "Bake Wall Shadow");
        shadow.sprite = sprite;
        shadow.transform.position = new Vector3(origin.x, origin.y, 0f);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"Bake Wall Shadow: wrote {path} ({width}x{height}) and updated {shadow.name}.");
    }

    private static Tilemap FindTilemap(string path)
    {
        GameObject go = GameObject.Find(path);
        return go ? go.GetComponent<Tilemap>() : null;
    }

    // Bounds of the cells that actually hold tiles (read only, the tilemaps are not modified)
    private static BoundsInt UsedCellBounds(Tilemap walls, Tilemap platforms)
    {
        Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, 1);
        foreach (Tilemap tilemap in new[] { walls, platforms })
        {
            if (tilemap == null) continue;
            foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell)) continue;
                min = Vector3Int.Min(min, new Vector3Int(cell.x, cell.y, 0));
                max = Vector3Int.Max(max, new Vector3Int(cell.x + 1, cell.y + 1, 1));
            }
        }
        var bounds = new BoundsInt();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    private static float[] BoxBlur(float[] src, int width, int height, int radius, bool horizontal)
    {
        float[] dst = new float[src.Length];
        int length = horizontal ? width : height;
        int lines = horizontal ? height : width;
        float norm = 1f / (radius * 2 + 1);

        for (int line = 0; line < lines; line++)
        {
            float sum = 0f;
            for (int k = -radius; k <= radius; k++) sum += src[Index(Mathf.Clamp(k, 0, length - 1), line, width, horizontal)];
            for (int i = 0; i < length; i++)
            {
                dst[Index(i, line, width, horizontal)] = sum * norm;
                sum += src[Index(Mathf.Min(i + radius + 1, length - 1), line, width, horizontal)];
                sum -= src[Index(Mathf.Max(i - radius, 0), line, width, horizontal)];
            }
        }
        return dst;
    }

    private static int Index(int i, int line, int width, bool horizontal) => horizontal ? line * width + i : i * width + line;

    private static void ConfigureImporter(string path, int largestSide)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = Mathf.Clamp(Mathf.NextPowerOfTwo(largestSide), 32, 8192);

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // Keeps an existing WallShadow (and whatever color / order was tuned on it); creates one on first bake
    private static SpriteRenderer FindOrCreateRenderer()
    {
        GameObject existing = GameObject.Find("WallShadow");
        if (existing != null && existing.TryGetComponent(out SpriteRenderer found)) return found;

        var go = new GameObject("WallShadow");
        GameObject lighting = GameObject.Find("Environment/Lighting");
        if (lighting != null) go.transform.SetParent(lighting.transform, false);
        Undo.RegisterCreatedObjectUndo(go, "Create WallShadow");

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.06f, 0.025f, 0.015f, 0.8f);
        renderer.sortingOrder = -10;
        return renderer;
    }
}
