using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Game;
using static SqueakyLane.EditorTools.EditorAssets;

namespace SqueakyLane.EditorTools
{
    public static class PlayTools
    {
        private const string Levels = "Assets/_Project/Resources/Levels";

        [MenuItem("Tools/Squeaky Lane/Sync Levels")]
        public static void SyncLevels()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Thoát Play trước khi đồng bộ dữ liệu.");
            // Khi đã có kho thiết kế, không lấy golden ghi đè màn người làm sửa.
            if (LevelDesignIO.HasDesignSource) { LevelDesignIO.Export(); return; }
            string source = Path.GetFullPath(Path.Combine(Application.dataPath, "../docs/golden/levels"));
            var paths = Directory.GetFiles(source, "level_*.json"); Array.Sort(paths, StringComparer.Ordinal);
            if (paths.Length != 31) throw new InvalidOperationException("Cần đúng 31 màn chuẩn, hiện có " + paths.Length);
            var texts = new string[paths.Length]; var ids = new int[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                texts[i] = File.ReadAllText(paths[i]); LevelJsonLoader.Load(texts[i], paths[i]);
                ids[i] = int.Parse(Path.GetFileNameWithoutExtension(paths[i]).Substring(6));
            }
            EnsureFolder(Levels);
            for (int i = 0; i < paths.Length; i++) WriteIfChanged(Levels + "/" + Path.GetFileName(paths[i]), texts[i]);
            WriteIfChanged(Levels + "/level_order.json", JsonUtility.ToJson(new LevelOrder { ids = ids }, true) + "\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Sync Levels: 31/31 JSON chuẩn và level_order; Unity sinh/giữ .meta.");
        }

        private static void WriteIfChanged(string path, string text) { if (!File.Exists(path) || File.ReadAllText(path) != text) File.WriteAllText(path, text, new System.Text.UTF8Encoding(false)); }
        [Serializable] private sealed class LevelOrder { public int[] ids; }
    }
}
