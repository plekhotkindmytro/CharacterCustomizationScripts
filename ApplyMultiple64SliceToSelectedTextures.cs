using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;

public class ApplyMultiple64SliceToSelectedTextures : MonoBehaviour
{
    const int PIXELS_PER_UNIT = 64;

    [MenuItem("Tools/Pixel Art 2D Textures/Apply 64x64 Slice")]
    static void Apply64x64Slice()
    {
        foreach (var obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if(AssetDatabase.IsValidFolder(assetPath)) 
            {
                TraverseFolderRecursive(assetPath);

            } else
            {
                Debug.LogError("Selected asset is not a dirrectory");
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void TraverseFolderRecursive(string folderPath)
    {
        // Get all asset paths in the specified folder (including subfolders)
        string[] assetPaths = AssetDatabase.FindAssets("", new[] { folderPath });

        foreach (string assetPath in assetPaths)
        {
            string assetFullPath = AssetDatabase.GUIDToAssetPath(assetPath);
            if (CheckIfTexture(assetFullPath))
            {
                ApplyTextureImportSettings(assetFullPath);
                SliceTextureByCellSize(assetFullPath);
            }
        }
    }

    private static void SliceTextureByCellSize(string texturePath)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(texture);

        dataProvider.InitSpriteEditorDataProvider();

        AddSpriteRect(dataProvider, texture);
        dataProvider.Apply();

        AssetImporter assetImporter = dataProvider.targetObject as AssetImporter;
        assetImporter.SaveAndReimport();
    }

    private static void ApplyTextureImportSettings(string texturePath)
    {
        TextureImporter textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        textureImporter.spritePixelsPerUnit = PIXELS_PER_UNIT;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.SaveAndReimport();
    }

    static bool CheckIfTexture(string path)
    {
        AssetImporter importer = AssetImporter.GetAtPath(path);
        return importer is TextureImporter;
    }

    private static void AddSpriteRect(ISpriteEditorDataProvider dataProvider, Texture2D texture2D)
    {
        Rect[] rects = InternalSpriteUtility.GenerateGridSpriteRectangles(
                 texture2D, Vector2.zero, new Vector2(PIXELS_PER_UNIT, PIXELS_PER_UNIT), Vector2.zero);

        List<SpriteRect> spriteRects = new List<SpriteRect>();
        List<SpriteNameFileIdPair> spriteNameFileIdPairs = new List<SpriteNameFileIdPair>();
        for (int i = 0; i < rects.Length; i++)
        {
            var newSpriteRect = new SpriteRect()
            {
                name = texture2D.name + "_" + i,
                spriteID = GUID.Generate(),
                rect = rects[i],
                alignment = SpriteAlignment.Center
            };
            
            spriteRects.Add(newSpriteRect);
            spriteNameFileIdPairs.Add(new SpriteNameFileIdPair(newSpriteRect.name, newSpriteRect.spriteID));
        };

        dataProvider.SetSpriteRects(spriteRects.ToArray());

        // Note: This section is only for Unity 2021.2 and newer
        // Register the new Sprite Rect's name and GUID with the ISpriteNameFileIdDataProvider
        var spriteNameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        spriteNameFileIdDataProvider.SetNameFileIdPairs(spriteNameFileIdPairs);
        // End of Unity 2021.2 and newer section
    }


}
