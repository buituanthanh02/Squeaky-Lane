using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Game;

[assembly: InternalsVisibleTo("SqueakyLane.Tests.EditMode")]
namespace SqueakyLane.EditorTools
{
    // Các kiểu nhỏ cùng phục vụ định dạng JSON, không phải asset/config mới và không vào APK.
    [Serializable]
    internal sealed class LevelDesignData
    {
        public int id = 1;
        public string name = "Màn mới";
        public int cols = 5, rows = 5;
        public List<DesignCar> cars = new List<DesignCar>();
        public List<DesignGate> gates = new List<DesignGate>();
        public List<DesignCell> cones = new List<DesignCell>(), muds = new List<DesignCell>(), sprays = new List<DesignCell>(), tables = new List<DesignCell>();
        public List<DesignPaint> paints = new List<DesignPaint>();
        public List<DesignLight> lights = new List<DesignLight>();
        public List<DesignBarrier> barriers = new List<DesignBarrier>();
        public bool splash = false;
        public List<DesignPortal> portals = new List<DesignPortal>();
        public List<DesignPylon> pylons = new List<DesignPylon>();
        public List<DesignSwitch> switches = new List<DesignSwitch>();
        public int minMoves = -1;
        public bool solveExhausted = false;
        public float timeLimit = 0;
        public int hints = 3;
    }

    [Serializable]
    internal class DesignCell
    {
        public int r = 0, c = 0;
    }

    [Serializable]
    internal sealed class DesignCar : DesignCell
    {
        public string axis = "h", color = "pink";
        public int len = 2, mud = 0;
        public bool truck = false;
    }

    [Serializable]
    internal sealed class DesignGate
    {
        public string side = "r", color = "pink";
        public int i = 0;
    }

    [Serializable]
    internal sealed class DesignPaint : DesignCell
    {
        public string color = "pink";
    }

    [Serializable]
    internal sealed class DesignLight : DesignCell
    {
        public int phase = 0;
    }

    [Serializable]
    internal sealed class DesignBarrier : DesignCell
    {
        public int need = 1;
    }

    [Serializable]
    internal sealed class DesignFace : DesignCell
    {
        public string side = "N";
    }

    [Serializable]
    internal sealed class DesignPortal
    {
        public DesignFace a = new DesignFace(), b = new DesignFace();
    }

    [Serializable]
    internal sealed class DesignPylon : DesignCell
    {
        public string o = "h";
        public bool @fixed = false;
        public DesignFace[] to =
        {
            new DesignFace(),
            new DesignFace()
        };
    }

    [Serializable]
    internal sealed class DesignSwitch : DesignCell
    {
        public List<DesignCell> a = new List<DesignCell>(), b = new List<DesignCell>();
    }

    [Serializable]
    internal sealed class DesignOrder
    {
        public int[] ids = Array.Empty<int>();
    }

    internal readonly struct DesignKind
    {
        public string JsonField { get; }
        public string Label { get; }
        public string ShortLabel { get; }
        public string AllowedFields { get; }

        public DesignKind(string jsonField, string label, string shortLabel, string allowedFields)
        {
            JsonField = jsonField;
            Label = label;
            ShortLabel = shortLabel;
            AllowedFields = allowedFields;
        }
    }

    // Thứ tự giữ nguyên vì cửa sổ đang lưu chỉ số loại vật thể trong bản nháp.
    internal static class DesignKinds
    {
        public const int Car = 0, Gate = 1, Cone = 2, Mud = 3, Spray = 4, Table = 5;
        public const int Paint = 6, Light = 7, Barrier = 8, Portal = 9, Pylon = 10, Switch = 11;
        public static readonly DesignKind[] All =
        {
            new DesignKind("cars", "Xe", "Xe", "r c axis color len mud truck"),
            new DesignKind("gates", "Cổng mép bãi", "Cổng", "side color i"),
            new DesignKind("cones", "Cọc", "Cọc", "r c"),
            new DesignKind("muds", "Vũng bùn", "Bùn", "r c"),
            new DesignKind("sprays", "Ô vòi xịt", "Xịt", "r c"),
            new DesignKind("tables", "Bàn xoay", "Bàn", "r c"),
            new DesignKind("paints", "Vũng sơn", "Sơn", "r c color"),
            new DesignKind("lights", "Đèn", "Đèn", "r c phase"),
            new DesignKind("barriers", "Barie đếm xe", "Barie", "r c need"),
            new DesignKind("portals", "Portal", "Portal", "a b"),
            new DesignKind("pylons", "Trụ xoay", "Trụ", "r c o fixed to"),
            new DesignKind("switches", "Chốt liên động", "Chốt", "r c a b")
        };
    }

    internal static class LevelDesignIO
    {
        public const string Folder = "Assets/_Project/LevelDesign";
        public const string RuntimeFolder = "Assets/_Project/Resources/Levels";
        private const string OrderName = "/level_order.json";
        public static bool HasDesignSource => Directory.Exists(Folder);
        public static string SourceFolder => HasDesignSource ? Folder : RuntimeFolder;

        public static string FileName(int id) => "level_" + id.ToString("00") + ".json";
        public static string[] ListSources()
        {
            var files = Directory.GetFiles(SourceFolder, "level_*.json");
            var levels = new List<string>();
            foreach (string file in files)
            {
                if (Path.GetFileName(file) == "level_order.json")
                {
                    continue;
                }

                FileId(file);
                levels.Add(file.Replace('\\', '/'));
            }

            levels.Sort((a, b) => FileId(a).CompareTo(FileId(b)));
            return levels.ToArray();
        }

        public static int NextId()
        {
            int max = 0;
            foreach (string path in ListSources())
            {
                max = Math.Max(max, FileId(path));
            }

            return checked(max + 1);
        }

        public static int FileId(string path)
        {
            if (!TryFileId(path, out int id))
            {
                throw new FormatException("Tên file phải là level_NN.json: " + path);
            }

            return id;
        }

        private static bool TryFileId(string path, out int id)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            id = 0;
            return name.StartsWith("level_", StringComparison.Ordinal) && int.TryParse(name.Substring(6), out id) && id > 0 && name == "level_" + id.ToString("00");
        }

        public static LevelDesignData Read(string json)
        {
            var fields = ObjectFields(json);
            CheckNestedSchema(fields);
            // Loader/Core vẫn là nơi quyết định bố cục hợp lệ. Không viết lại luật trong tool.
            LevelJsonLoader.Load(json, "bản thiết kế");
            var data = JsonUtility.FromJson<LevelDesignData>(json);
            if (!fields.ContainsKey("minMoves"))
            {
                data.minMoves = -1;
            }

            data.cars ??= new List<DesignCar>();
            data.gates ??= new List<DesignGate>();
            data.cones ??= new List<DesignCell>();
            data.muds ??= new List<DesignCell>();
            data.sprays ??= new List<DesignCell>();
            data.tables ??= new List<DesignCell>();
            data.paints ??= new List<DesignPaint>();
            data.lights ??= new List<DesignLight>();
            data.barriers ??= new List<DesignBarrier>();
            data.portals ??= new List<DesignPortal>();
            data.pylons ??= new List<DesignPylon>();
            data.switches ??= new List<DesignSwitch>();
            return data;
        }

        public static bool HasField(string json, string key) => ObjectFields(json).ContainsKey(key);
        public static string Serialize(LevelDesignData data, bool timeOverride, bool hintsOverride, string originalJson)
        {
            var fields = ObjectFields(JsonUtility.ToJson(data, true));
            if (!timeOverride)
            {
                fields.Remove("timeLimit");
            }

            if (!hintsOverride)
            {
                fields.Remove("hints");
            }

            // Giữ metadata ngoài schema của tool. Trường lạ trong cơ chế bị chặn khi mở, không âm thầm làm mất.
            if (!string.IsNullOrWhiteSpace(originalJson))
            {
                foreach (var pair in ObjectFields(originalJson))
                {
                    if (!fields.ContainsKey(pair.Key) && pair.Key != "timeLimit" && pair.Key != "hints")
                    {
                        fields.Add(pair.Key, pair.Value);
                    }
                }
            }

            return JoinObject(fields);
        }

        public static string Geometry(LevelDesignData data)
        {
            var all = ObjectFields(JsonUtility.ToJson(data));
            var geometry = new Dictionary<string, string>();
            foreach (string key in new[]
            {
                "cols",
                "rows",
                "splash"
            }

            )
            {
                geometry.Add(key, all[key]);
            }

            foreach (var kind in DesignKinds.All)
            {
                geometry.Add(kind.JsonField, all[kind.JsonField]);
            }

            return JoinObject(geometry);
        }

        public static LevelDef Validate(string json, int expectedId = 0)
        {
            var data = Read(json);
            if (data.id < 1 || data.id > 999999 || (expectedId != 0 && data.id != expectedId))
            {
                throw new FormatException("ID phải dương, không quá 999999 và khớp tên file.");
            }

            if (string.IsNullOrWhiteSpace(data.name))
            {
                throw new FormatException("Tên màn không được rỗng.");
            }

            if (data.cars.Count == 0)
            {
                throw new FormatException("Màn thiết kế cần ít nhất một xe.");
            }

            if (data.minMoves < -1 || data.hints < -1 || float.IsNaN(data.timeLimit) || float.IsInfinity(data.timeLimit) || data.timeLimit < 0)
            {
                throw new FormatException("minMoves/hints tối thiểu -1; thời gian phải hữu hạn và không âm.");
            }

            return LevelJsonLoader.Load(json, FileName(data.id));
        }

        public static DesignOrder ReadOrder()
        {
            string path = SourceFolder + OrderName;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Kho thiết kế thiếu level_order.json; không dùng golden đè lên kho đang sửa.", path);
            }

            var order = JsonUtility.FromJson<DesignOrder>(File.ReadAllText(path));
            CheckOrder(order);
            return order;
        }

        private static void CheckOrder(DesignOrder order)
        {
            if (order?.ids == null || order.ids.Length == 0)
            {
                throw new FormatException("Danh sách game cần ít nhất một màn.");
            }

            var ids = new HashSet<int>();
            foreach (int id in order.ids)
            {
                if (id <= 0 || !ids.Add(id))
                {
                    throw new FormatException("ID trong thứ tự phải dương và không trùng: " + id);
                }
            }
        }

        public static void InitializeSource()
        {
            EditModeOnly();
            if (HasDesignSource)
            {
                ReadOrder();
                return;
            }

            // Kiểm tất cả trước khi tạo thư mục; bản thiết kế ban đầu lấy đúng dữ liệu đang chạy.
            var order = ReadOrder();
            var paths = ListSources();
            var texts = new string[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                texts[i] = File.ReadAllText(paths[i]);
                Validate(texts[i], FileId(paths[i]));
            }

            foreach (int id in order.ids)
            {
                if (!File.Exists(RuntimeFolder + "/" + FileName(id)))
                {
                    throw new FormatException("Thiếu màn " + id);
                }
            }

            EnsureFolder(Folder);
            for (int i = 0; i < paths.Length; i++)
            {
                Write(Folder + "/" + Path.GetFileName(paths[i]), texts[i]);
            }

            Write(Folder + OrderName, JsonUtility.ToJson(order, true) + "\n");
            AssetDatabase.Refresh();
        }

        public static string Save(string json, int openedId)
        {
            EditModeOnly();
            Validate(json);
            int id = JsonUtility.FromJson<LevelDesignData>(json).id;
            InitializeSource();
            string target = Folder + "/" + FileName(id);
            if (File.Exists(target) && id != openedId)
            {
                throw new InvalidOperationException("ID đã tồn tại. Dùng ID mới hoặc mở đúng màn đó để sửa.");
            }

            Write(target, json);
            AssetDatabase.Refresh();
            return target;
        }

        public static void SaveOrder(DesignOrder order)
        {
            EditModeOnly();
            CheckOrder(order);
            InitializeSource();
            foreach (int id in order.ids)
            {
                string path = Folder + "/" + FileName(id);
                if (!File.Exists(path))
                {
                    throw new FormatException("Hãy lưu màn " + id + " trước khi thêm vào thứ tự.");
                }

                Validate(File.ReadAllText(path), id);
            }

            Write(Folder + OrderName, JsonUtility.ToJson(order, true) + "\n");
            AssetDatabase.Refresh();
        }

        public static void Export()
        {
            EditModeOnly();
            if (!HasDesignSource)
            {
                throw new InvalidOperationException("Chưa có kho thiết kế; lưu một màn hoặc khởi tạo trước.");
            }

            var order = ReadOrder();
            var texts = new string[order.ids.Length];
            // Không để một file lỗi làm bộ runtime bị thay dở trước khi phát hiện lỗi dữ liệu.
            for (int i = 0; i < texts.Length; i++)
            {
                texts[i] = File.ReadAllText(Folder + "/" + FileName(order.ids[i]));
                Validate(texts[i], order.ids[i]);
            }

            EnsureFolder(RuntimeFolder);
            for (int i = 0; i < texts.Length; i++)
            {
                Write(RuntimeFolder + "/" + FileName(order.ids[i]), texts[i]);
            }

            Write(RuntimeFolder + OrderName, JsonUtility.ToJson(order, true) + "\n");
            AssetDatabase.Refresh();
            Debug.Log("Level Editor: đã xuất " + texts.Length + " màn và thứ tự vào game; không sửa golden, không xóa file cũ.");
        }

        private static void EditModeOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Thoát Play trước khi sửa/lưu/xuất màn.");
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void Write(string path, string text)
        {
            if (File.Exists(path) && File.ReadAllText(path) == text)
            {
                return;
            }

            // Sao lưu ngoài Assets, rồi thay nội dung file; .meta/GUID cũ không bị đụng tới.
            string backups = Path.GetFullPath("Logs/level-editor-backups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backups);
            if (File.Exists(path))
            {
                File.Copy(path, Path.Combine(backups, Path.GetFileName(path)));
            }

            string temp = Path.Combine(backups, "new.json");
            File.WriteAllText(temp, text, new UTF8Encoding(false));
            if (File.Exists(path))
            {
                File.Replace(temp, path, null);
            }
            else
            {
                File.Move(temp, path);
            }
        }

        // Chỉ tách thành viên JSON ở gốc để giữ metadata/độ vắng mặt của timeLimit và hints.
        // Không dùng replace chuỗi theo tên field: tên ấy có thể xuất hiện trong tên màn hoặc dữ liệu lồng nhau.
        public static Dictionary<string, string> ObjectFields(string json)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            int at = 0;
            Skip(json, ref at);
            Expect(json, ref at, '{');
            Skip(json, ref at);
            if (at < json.Length && json[at] == '}')
            {
                at++;
                Skip(json, ref at);
                if (at != json.Length)
                {
                    throw new FormatException("Dữ liệu sau JSON.");
                }

                return result;
            }

            while (true)
            {
                Skip(json, ref at);
                int start = at;
                StringEnd(json, ref at);
                string key = JsonUtility.FromJson<StringValue>("{\"value\":" + json.Substring(start, at - start) + "}").value;
                Skip(json, ref at);
                Expect(json, ref at, ':');
                Skip(json, ref at);
                start = at;
                ValueEnd(json, ref at);
                string value = json.Substring(start, at - start).Trim();
                if (value.Length == 0 || result.ContainsKey(key))
                {
                    throw new FormatException("Field rỗng hoặc trùng: " + key);
                }

                result.Add(key, value);
                Skip(json, ref at);
                if (at < json.Length && json[at] == '}')
                {
                    at++;
                    break;
                }

                Expect(json, ref at, ',');
            }

            Skip(json, ref at);
            if (at != json.Length)
            {
                throw new FormatException("Dữ liệu sau JSON.");
            }

            return result;
        }

        private static List<string> ArrayValues(string json)
        {
            var values = new List<string>();
            if (json.Trim() == "null")
            {
                return values;
            }

            int at = 0;
            Skip(json, ref at);
            Expect(json, ref at, '[');
            Skip(json, ref at);
            if (at < json.Length && json[at] == ']')
            {
                return values;
            }

            while (true)
            {
                Skip(json, ref at);
                int start = at;
                ValueEnd(json, ref at);
                values.Add(json.Substring(start, at - start).Trim());
                Skip(json, ref at);
                if (at < json.Length && json[at] == ']')
                {
                    break;
                }

                Expect(json, ref at, ',');
            }

            return values;
        }

        private static void ValueEnd(string json, ref int at)
        {
            int depth = 0;
            while (at < json.Length)
            {
                char ch = json[at];
                if (ch == '"')
                {
                    StringEnd(json, ref at);
                    continue;
                }

                if (ch == '{' || ch == '[')
                {
                    depth++;
                }
                else if (ch == '}' || ch == ']')
                {
                    if (depth == 0)
                    {
                        return;
                    }

                    depth--;
                }
                else if (ch == ',' && depth == 0)
                {
                    return;
                }

                at++;
            }

            throw new FormatException("JSON chưa đóng.");
        }

        private static void StringEnd(string json, ref int at)
        {
            Expect(json, ref at, '"');
            while (at < json.Length)
            {
                char ch = json[at++];
                if (ch == '\\')
                {
                    at++;
                    continue;
                }

                if (ch == '"')
                {
                    return;
                }
            }

            throw new FormatException("Chuỗi JSON chưa đóng.");
        }

        private static void Skip(string json, ref int at)
        {
            while (at < json.Length && char.IsWhiteSpace(json[at]))
            {
                at++;
            }
        }

        private static void Expect(string json, ref int at, char ch)
        {
            if (at >= json.Length || json[at++] != ch)
            {
                throw new FormatException("JSON: cần ký tự " + ch);
            }
        }

        private static string JoinObject(Dictionary<string, string> fields)
        {
            var text = new StringBuilder("{\n");
            bool first = true;
            foreach (var pair in fields)
            {
                if (!first)
                {
                    text.Append(",\n");
                }

                first = false;
                string quoted = JsonUtility.ToJson(new StringValue { value = pair.Key });
                text.Append("  ").Append(quoted.Substring(9, quoted.Length - 10)).Append(": ").Append(pair.Value);
            }

            return text.Append("\n}\n").ToString();
        }

        [Serializable]
        private sealed class StringValue
        {
            public string value;
        }

        private static void CheckNestedSchema(Dictionary<string, string> fields)
        {
            foreach (var kind in DesignKinds.All)
            {
                string key = kind.JsonField;
                if (!fields.TryGetValue(key, out string array))
                {
                    continue;
                }

                foreach (string raw in ArrayValues(array))
                {
                    var item = ObjectFields(raw);
                    Allowed(item, kind.AllowedFields, key);
                    if (key == "portals")
                    {
                        foreach (string end in new[]
                        {
                            "a",
                            "b"
                        }

                        )
                        {
                            if (item.TryGetValue(end, out string face))
                            {
                                Allowed(ObjectFields(face), "r c side", key + "." + end);
                            }
                        }
                    }

                    if (key == "pylons" && item.TryGetValue("to", out string to))
                    {
                        foreach (string face in ArrayValues(to))
                        {
                            Allowed(ObjectFields(face), "r c side", "pylons.to");
                        }
                    }

                    if (key == "switches")
                    {
                        foreach (string end in new[]
                        {
                            "a",
                            "b"
                        }

                        )
                        {
                            if (item.TryGetValue(end, out string cells))
                            {
                                foreach (string cell in ArrayValues(cells))
                                {
                                    Allowed(ObjectFields(cell), "r c", "switches." + end);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Allowed(Dictionary<string, string> item, string allowed, string context)
        {
            var names = new HashSet<string>(allowed.Split(' '));
            foreach (string field in item.Keys)
            {
                if (!names.Contains(field))
                {
                    throw new FormatException(context + "." + field + ": tool chưa hỗ trợ; không mở/sửa để tránh mất dữ liệu.");
                }
            }
        }
    }
}
