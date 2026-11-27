using System;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Presentation;
using static SqueakyLane.EditorTools.EditorAssets;

namespace SqueakyLane.EditorTools
{
    // 021: đặt importer cho hình logo và gán logo_layout.json, sprite, tia pháo vào UiTheme. Chạy lại được, không sửa scene.
    public static class WinLogoTools
    {
        private const string Art = UiArt + "/Logo/";

        [MenuItem("Tools/Squeaky Lane/021 UI/Setup Win Logo")]
        public static void Build()
        {
            RequireEditMode();
            var theme = RequireAsset<UiTheme>(UiThemePath);
            var layoutAsset = RequireAsset<TextAsset>(Art + "logo_layout.json");
            var layout = JsonUtility.FromJson<WinShow.Layout>(layoutAsset.text);
            if (layout == null || layout.pieces == null || layout.pieces.Length == 0)
            {
                throw new InvalidOperationException("logo_layout.json rỗng hoặc sai dạng.");
            }

            var data = new SerializedObject(theme);
            data.FindProperty("winLogoLayout").objectReferenceValue = layoutAsset;
            var sprites = data.FindProperty("winLogoSprites");
            sprites.arraySize = layout.pieces.Length;
            for (int i = 0; i < layout.pieces.Length; i++)
            {
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = Import(layout.pieces[i].sprite);
            }

            data.FindProperty("winFxDot").objectReferenceValue = Import("fx_dot");
            data.FindProperty("winFxTrail").objectReferenceValue = Import("fx_trail");
            // Ảnh ghép sẵn dành cho main menu về sau: đặt importer luôn cho đồng bộ.
            Import("logo_full");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssetIfDirty(theme);
            Debug.Log("021: đã gán logo " + layout.pieces.Length + " mảnh và tia pháo vào UiTheme.");
        }

        private static Sprite Import(string name)
        {
            string path = Art + name + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Thiếu hình " + path);
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || !importer.alphaIsTransparency
                || importer.spriteImportMode != SpriteImportMode.Single || importer.wrapMode != TextureWrapMode.Clamp
                || importer.npotScale != TextureImporterNPOTScale.None)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }

            return RequireAsset<Sprite>(path);
        }
    }
}
