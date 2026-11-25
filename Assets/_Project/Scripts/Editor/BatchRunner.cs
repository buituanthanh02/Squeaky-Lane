using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SqueakyLane.EditorTools
{
    // Cầu nối cho Unity CLI khi Editor đóng:
    //   unity run "<project>" --editor-version 2022.3.62f2 -- -executeMethod SqueakyLane.EditorTools.BatchRunner.<Tên> -logFile <file>
    // Mở đúng scene Play.unity (các menu 013 yêu cầu scene sạch), chạy menu rồi thoát với mã rõ ràng: 0 = ổn, 1 = lỗi (lỗi in ra Console/log).
    // Không phải menu cho người dùng và không đổi gì ngoài việc gọi lại menu có sẵn.
    public static class BatchRunner
    {
        private const string ScenePath = "Assets/_Project/Scenes/Play.unity";

        // Chỉ đặt importer cho bộ sprite Art/UI/Sprites/Pack + Art/UI/Library (không đụng scene).
        public static void ImportPack() { Run("S3 Import UI Pack", UiPackImport.Run); }

        // Nhập sprite + áp S3 lên Play.unity/LevelButton.prefab, lưu scene, ghi mục S3 trong 013-scene-changes.md.
        public static void ApplyS3()
        {
            Run("S3 Apply UI Pack Skin", () =>
            {
                UiPackImport.Run();
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                UiPackSkinTools.Apply();
            });
        }

        // Render UI của Play.unity ra PNG ở Logs/013-preview (không lưu scene). Xem UiPreviewTools về giới hạn.
        public static void RenderPreviews()
        {
            Run("Render UI preview", () =>
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                UiPreviewTools.RenderAll("Logs/013-preview");
            });
        }

        // Chỉ áp preset chữ (độ đậm/viền), không đụng scene.
        public static void ApplyTextMaterials() { Run("S3 Apply Text Materials", UiPackSkinTools.ApplyTextMaterials); }
        public static void ApplyS4()
        {
            Run("S4 Apply Scenery", () =>
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                SceneryTools.Apply();
            });
        }

        // So sánh các mức độ đậm/viền chữ HudOutline (không lưu gì).
        public static void RenderFontVariants()
        {
            Run("Render font variants", () =>
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                UiPreviewTools.RenderFontVariants("Logs/013-preview-font");
            });
        }

        private static void Run(string title, Action action)
        {
            int code = 0;
            try
            {
                action();
                Debug.Log("[BatchRunner] " + title + " xong.");
            }
            catch (Exception exception)
            {
                Debug.LogError("[BatchRunner] " + title + " lỗi: " + exception);
                code = 1;
            }
            EditorApplication.Exit(code);
        }
    }
}
