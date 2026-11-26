using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Presentation;
using static SqueakyLane.EditorTools.EditorAssets;

namespace SqueakyLane.EditorTools
{
    // 016: tạo texture palette + material cho khu phố (CityBuilder) và bật vào UiTheme. Chạy lại được, giữ GUID.
    public static class CitySetupTools
    {
        private const string Art = "Assets/_Project/Art/Environment/City3D";

        [MenuItem("Tools/Squeaky Lane/016 Toy/City Setup")]
        public static void Run()
        {
            RequireEditMode();
            if (!AssetDatabase.IsValidFolder(Art)) { AssetDatabase.CreateFolder("Assets/_Project/Art/Environment", "City3D"); }
            var texture = Palette();
            var ground = Make("M_CityGround", "Unlit/Texture", texture);
            var outlined = Make("M_CityToy", "SqueakyLane/Toy Lit Outline", texture);
            var plain = Make("M_CityToyPlain", "SqueakyLane/Toy Lit", texture);
            foreach (var toy in new[] { outlined, plain })
            {
                toy.SetFloat("_TopTint", 1); toy.SetFloat("_TopSaturation", 1);
                toy.SetColor("_ShadowColor", new Color32(178, 166, 196, 255));
                toy.SetFloat("_RampThreshold", .62f); toy.SetFloat("_RampSmooth", .05f);
                toy.SetFloat("_SpecStrength", 0); toy.SetFloat("_RimStrength", .1f);
                toy.enableInstancing = true;
                EditorUtility.SetDirty(toy);
            }
            outlined.SetFloat("_OutlineWidth", 1.9f); outlined.SetColor("_OutlineColor", new Color32(36, 38, 43, 255));
            AssetDatabase.SaveAssets();

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemePath);
            if (theme == null) { throw new InvalidOperationException("Thiếu UiTheme."); }
            var data = new SerializedObject(theme);
            data.FindProperty("cityEnabled").boolValue = true;
            data.FindProperty("cityGroundMaterial").objectReferenceValue = ground;
            data.FindProperty("cityPropMaterial").objectReferenceValue = outlined;
            // Giữ lựa chọn cảnh xa của người làm (vd đã đổi sang bản không viền).
            if (data.FindProperty("cityFillMaterial").objectReferenceValue == null) { data.FindProperty("cityFillMaterial").objectReferenceValue = outlined; }
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("016 City Setup: đã tạo palette/material và bật khu phố trong UiTheme.");
        }

        private static Texture2D Palette()
        {
            string path = Art + "/city_palette.png";
            var colors = CityBuilder.PaletteHex;
            var source = new Texture2D(colors.Count, 1, TextureFormat.RGBA32, false);
            for (int i = 0; i < colors.Count; i++) { ColorUtility.TryParseHtmlString("#" + colors[i], out var color); source.SetPixel(i, 0, color); }
            source.Apply();
            var bytes = source.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(source);
            bool same = File.Exists(path) && Convert.ToBase64String(File.ReadAllBytes(path)) == Convert.ToBase64String(bytes);
            if (!same) { File.WriteAllBytes(path, bytes); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None
                || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static Material Make(string name, string shaderName, Texture2D texture)
        {
            string path = Art + "/" + name + ".mat";
            var shader = Shader.Find(shaderName);
            if (shader == null) { throw new InvalidOperationException("Thiếu shader " + shaderName); }
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader; material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
