using System;
using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

namespace SqueakyLane.EditorTools
{
    // Việc chung của các tool Editor: kiểm chế độ, tạo thư mục, nạp asset bắt buộc.
    internal static class EditorAssets
    {
        public const string UiArt = "Assets/_Project/Art/UI";
        public const string UiThemePath = "Assets/_Project/Config/UiTheme.asset";

        public static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { throw new InvalidOperationException("Thoát Play trước khi chạy menu."); }
        }

        // Tạo bằng AssetDatabase để Unity tự sinh .meta cho từng cấp thư mục.
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) { return; }
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T RequireAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) { throw new InvalidOperationException($"Thiếu {typeof(T).Name}: {path}."); }
            return asset;
        }

        public static void RequireFreePath(string path)
        {
            if (File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                throw new InvalidOperationException($"Đã có file khác tại {path}.");
            }
        }
    }
}
