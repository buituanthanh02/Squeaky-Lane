using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    // Công cụ xem trước cho phiên chạy Unity CLI (Claude không thấy Game view): dựng UI của Play.unity lên một camera phụ, render ra PNG trong Logs/013-preview.
    // Chỉ đổi scene TRONG BỘ NHỚ (renderMode Canvas, chữ mẫu) và không lưu; chỉ BatchRunner gọi. Không thay cho ảnh Game view thật của người làm (thiếu bãi 3D, safe area, hiệu năng).
    public static class UiPreviewTools
    {
        private static readonly (int w, int h, string name)[] Sizes = { (1284, 2778, "12promax"), (750, 1334, "se") };

        // So sánh độ đậm/viền của chữ tiêu đề (preset HudOutline) trên cùng một hộp thoại: mỗi biến thể chỉ đổi _FaceDilate và _OutlineWidth trong bộ nhớ.
        public static void RenderFontVariants(string outDir)
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            var material = theme.HudOutline;
            float dilate = material.GetFloat("_FaceDilate"), width = material.GetFloat("_OutlineWidth");
            var variants = new[] { (0f, .3f), (.15f, .24f), (.25f, .2f), (.35f, .16f) };
            try
            {
                for (int i = 0; i < variants.Length; i++)
                {
                    material.SetFloat("_FaceDilate", variants[i].Item1);
                    material.SetFloat("_OutlineWidth", variants[i].Item2);
                    ShaderUtilities.UpdateShaderRatios(material);
                    RenderAll(outDir, "v" + i + "-", true);
                }
            }
            finally
            {
                material.SetFloat("_FaceDilate", dilate);
                material.SetFloat("_OutlineWidth", width);
                ShaderUtilities.UpdateShaderRatios(material);
            }
        }

        public static void RenderAll(string outDir, string tag = "", bool firstSizeOnly = false)
        {
            Directory.CreateDirectory(outDir);
            var nav = Object.FindObjectOfType<NavigationView>(true);
            var data = new SerializedObject(nav);
            var canvas = nav.GetComponentInParent<Canvas>(true).rootCanvas;
            var scaler = canvas.GetComponent<CanvasScaler>();
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(UiThemeAssets.ThemePath);
            var cameraObject = new GameObject("PreviewCam");
            var cam = cameraObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            ColorUtility.TryParseHtmlString("#6FA86B", out var grass);
            cam.backgroundColor = grass;
            // Canvas Screen Space - Camera chỉ vẽ khi camera thấy layer của Canvas (Play.unity để Canvas ở layer Default).
            // Bãi 3D chỉ được dựng lúc Play nên ở chế độ Edit không lẫn vào ảnh.
            cam.cullingMask = ~0;
            cam.orthographic = true;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10;
            if (scaler != null) { scaler.enabled = false; }
            // NavigationView chỉ nối ba nhãn này lúc Initialize; gán tay để gọi RefreshHud thật (đúng đường chạy trong game).
            Bind(nav, "undoLabel", data, "undoButton");
            Bind(nav, "hintLabel", data, "hintButton");
            Bind(nav, "nextLabel", data, "nextButton");
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var content = (Transform)data.FindProperty("levelButtonContent").objectReferenceValue;
            var prefab = (Button)data.FindProperty("levelButtonPrefab").objectReferenceValue;
            for (int i = 1; i <= 6; i++)
            {
                var button = Object.Instantiate(prefab, content);
                button.name = "PreviewLevel " + i;
                button.interactable = i != 4;
                button.GetComponentInChildren<TMP_Text>(true).text = "Màn " + i + "\n" + theme.Stars(i % 4);
            }
            foreach (var size in firstSizeOnly ? new[] { Sizes[0] } : Sizes)
            {
                var rt = new RenderTexture(size.w, size.h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                cam.targetTexture = rt;
                // Cùng công thức CanvasScaler Scale With Screen Size (match theo log).
                float logW = Mathf.Log(size.w / scaler.referenceResolution.x, 2), logH = Mathf.Log(size.h / scaler.referenceResolution.y, 2);
                canvas.scaleFactor = Mathf.Pow(2, Mathf.Lerp(logW, logH, scaler.matchWidthOrHeight));
                // Màn chơi: giờ bình thường, rồi sắp hết giờ (pill đỏ) và hết lượt gợi ý.
                var normal = new LevelTimer(125, 3, 5, .5f, .2f);
                nav.RefreshHud("Màn 23", normal, 3, 3, "Hướng này đang bị chặn", true, true, 30, 22, config);
                nav.ShowMessage("Hướng này đang bị chặn");
                nav.ShowScreen(ScreenState.Playing);
                Capture(cam, rt, nav, outDir + "/" + tag + "playing-" + size.name + ".png");
                var low = new LevelTimer(20, 3, 5, .5f, .2f, 20);
                low.Tick(18);
                nav.RefreshHud("Màn 23", low, 5, 0, "", false, true, 30, 22, config);
                nav.ShowMessage("");
                Capture(cam, rt, nav, outDir + "/" + tag + "playing-low-" + size.name + ".png");
                nav.RefreshHud("Màn 23", normal, 3, 3, "", true, true, 30, 22, config);
                foreach (var state in new[] { ScreenState.Won, ScreenState.Paused, ScreenState.Lost, ScreenState.LevelSelect })
                {
                    nav.ShowScreen(state);
                    Capture(cam, rt, nav, outDir + "/" + tag + state.ToString().ToLowerInvariant() + "-" + size.name + ".png");
                }
                cam.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(cameraObject);
        }

        private static void Bind(NavigationView nav, string field, SerializedObject data, string buttonField)
        {
            var button = (Button)data.FindProperty(buttonField).objectReferenceValue;
            TMP_Text label = null;
            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name != "013 BadgeText") { label = text; break; }
            }
            typeof(NavigationView).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(nav, label);
        }

        private static void Capture(Camera cam, RenderTexture rt, NavigationView nav, string path)
        {
            foreach (var body in nav.GetComponentsInChildren<VerticalLayoutGroup>(true)) { LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)body.transform); }
            foreach (var text in nav.GetComponentsInChildren<TMP_Text>(true)) { text.ForceMeshUpdate(); }
            Canvas.ForceUpdateCanvases();
            foreach (var body in nav.GetComponentsInChildren<VerticalLayoutGroup>(true)) { LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)body.transform); }
            foreach (var text in nav.GetComponentsInChildren<TMP_Text>(true)) { text.ForceMeshUpdate(); }
            Canvas.ForceUpdateCanvases();
            cam.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = previous;
            Object.DestroyImmediate(texture);
        }
    }
}
