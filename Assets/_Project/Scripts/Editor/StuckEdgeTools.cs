using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Presentation;
using static SqueakyLane.EditorTools.EditorAssets;

namespace SqueakyLane.EditorTools
{
    // 016: tạo sprite viền mờ (đậm ở mép, trong ở giữa) và đặt lớp cảnh báo kẹt xe vào Canvas của Play.unity.
    // Chạy lại được: dựng lại object "016 StuckEdge" và gán vào NavigationView.
    public static class StuckEdgeTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";
        private const string SpritePath = UiArt + "/Sprites/stuck_edge.png";
        private const int Size = 128, Border = 60, Fade = 56;

        [MenuItem("Tools/Squeaky Lane/016 UI/Build Stuck Edge")]
        public static void Build()
        {
            RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) { throw new InvalidOperationException("Mở Play.unity trước."); }
            var nav = UnityEngine.Object.FindObjectOfType<NavigationView>(true);
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemePath);
            if (nav == null || theme == null) { throw new InvalidOperationException("Thiếu NavigationView/UiTheme."); }
            var sprite = Sprite();
            var data = new SerializedObject(nav);
            var safe = (RectTransform)data.FindProperty("safeAreaRoot").objectReferenceValue;
            var uiRoot = safe.parent;
            var canvas = uiRoot.parent;
            var old = canvas.Find("016 StuckEdge");
            if (old != null) { UnityEngine.Object.DestroyImmediate(old.gameObject); }
            var item = new GameObject("016 StuckEdge", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            item.layer = canvas.gameObject.layer;
            var rect = (RectTransform)item.transform;
            rect.SetParent(canvas, false);
            // Nằm dưới UIRoot: đồng hồ, nút và hộp thoại luôn vẽ đè lên viền đỏ.
            rect.SetSiblingIndex(uiRoot.GetSiblingIndex());
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = item.GetComponent<Image>();
            image.sprite = sprite; image.type = Image.Type.Sliced; image.fillCenter = false; image.raycastTarget = false;
            image.color = theme.StuckEdgeColor;
            image.pixelsPerUnitMultiplier = Border / theme.StuckEdgeWidth;
            var group = item.GetComponent<CanvasGroup>();
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
            data.FindProperty("stuckEdge").objectReferenceValue = group;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("016: đã đặt viền đỏ cảnh báo kẹt xe (Canvas/016 StuckEdge) và gán NavigationView.stuckEdge.");
        }

        private static Sprite Sprite()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float ex = Mathf.Max(0, Fade - Mathf.Min(x, Size - 1 - x)) / (float)Fade;
                    float ey = Mathf.Max(0, Fade - Mathf.Min(y, Size - 1 - y)) / (float)Fade;
                    float edge = Mathf.Clamp01(Mathf.Sqrt(ex * ex + ey * ey));
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(edge, 1.4f)));
                }
            }
            texture.Apply();
            var bytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            if (!File.Exists(SpritePath) || Convert.ToBase64String(File.ReadAllBytes(SpritePath)) != Convert.ToBase64String(bytes))
            {
                File.WriteAllBytes(SpritePath, bytes);
                AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            var border = new Vector4(Border, Border, Border, Border);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteBorder != border || importer.mipmapEnabled
                || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border; importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null) { throw new InvalidOperationException("Không tạo được sprite viền."); }
            return sprite;
        }
    }
}
