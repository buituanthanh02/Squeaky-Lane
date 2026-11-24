using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;
using SqueakyLane.Presentation;

namespace SqueakyLane.EditorTools
{
    // Undo chỉ giữ bản nháp, không được tua lại đường dẫn/baseline của file đã lưu trên đĩa.
    internal sealed class LevelEditorDraft : ScriptableObject
    {
        public LevelDesignData document;
        public bool timeOverride, hintsOverride;
        public DesignOrder orderDraft;
    }

    public sealed class LevelEditorWindow : EditorWindow
    {
        [SerializeField]
        private LevelEditorDraft draft;
        private LevelDesignData document { get => draft.document; set => draft.document = value; }
        private bool timeOverride { get => draft.timeOverride; set => draft.timeOverride = value; }
        private bool hintsOverride { get => draft.hintsOverride; set => draft.hintsOverride = value; }
        private DesignOrder orderDraft { get => draft.orderDraft; set => draft.orderDraft = value; }

        [SerializeField]
        private string originalJson = "", savedJson = "", openedPath = "";
        [SerializeField]
        private int openedId;
        [SerializeField]
        private string savedOrder = "";
        [SerializeField]
        private int kind, selected, cellR, cellC;
        private int tab, boardMode, sourceChoice, addOrderChoice, configChoice;
        private string[] sources = Array.Empty<string>();
        private string status = "", previewError = "";
        private MessageType statusType = MessageType.Info;
        private bool placeMode, previewDirty = true;
        private string gateSide = "r";
        private int gateIndex;
        private Vector2 propertiesScroll, orderScroll;
        private SerializedObject fields;
        private PreviewRenderUtility preview;
        private BoardView previewBoard;
        private CameraConfig cameraConfig;
        private VisualPalette palette;
        private Editor configEditor;
        private UnityEngine.Object configTarget;
        private PathSearch search;
        private string searchGeometry;
        private int maxStates = 300000;
        private static readonly string[] KindNames = Array.ConvertAll(DesignKinds.All, item => item.Label);
        private static readonly string[] Colors =
        {
            "pink",
            "blue",
            "yellow",
            "green",
            "orange",
            "purple",
            "water"
        };
        [MenuItem("Tools/Squeaky Lane/Level Editor")]
        public static void Open() => GetWindow<LevelEditorWindow>("Squeaky Lane · Màn").Show();
        private void OnEnable()
        {
            if (draft == null)
            {
                draft = CreateInstance<LevelEditorDraft>();
                draft.hideFlags = HideFlags.HideAndDontSave;
            }

            minSize = new Vector2(960, 650);
            saveChangesMessage = "Bản thiết kế/thứ tự có thay đổi chưa lưu.";
            Undo.undoRedoPerformed += AfterUndo;
            EditorApplication.update += SearchStep;
            EditorApplication.playModeStateChanged += PlayModeChanged;
            cameraConfig = AssetDatabase.LoadAssetAtPath<CameraConfig>("Assets/_Project/Config/CameraConfig.asset");
            palette = AssetDatabase.LoadAssetAtPath<VisualPalette>("Assets/_Project/Config/VisualPalette.asset");
            Run(() =>
            {
                ReloadSources();
                if (orderDraft == null)
                {
                    orderDraft = LevelDesignIO.ReadOrder();
                    savedOrder = JsonUtility.ToJson(orderDraft);
                }

                if (document == null && sources.Length > 0)
                {
                    Load(sources[0]);
                }
            });
            fields = new SerializedObject(draft);
            UpdateDirty();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= AfterUndo;
            EditorApplication.update -= SearchStep;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            search = null;
            CleanupPreview();
            if (configEditor != null)
            {
                DestroyImmediate(configEditor);
            }
        }

        private void OnDestroy()
        {
            if (draft != null)
            {
                DestroyImmediate(draft);
            }
        }

        private void PlayModeChanged(PlayModeStateChange state)
        {
            search = null;
            CleanupPreview();
            previewDirty = true;
            Repaint();
        }

        private void OnInspectorUpdate()
        {
            if (tab == 0 && boardMode == 1)
            {
                Repaint();
            }
        }

        private string CurrentJson => document == null ? "" : LevelDesignIO.Serialize(document, timeOverride, hintsOverride, originalJson);

        private void UpdateDirty()
        {
            hasUnsavedChanges = document != null && CurrentJson != savedJson || orderDraft != null && JsonUtility.ToJson(orderDraft) != savedOrder;
        }

        private void AfterUndo()
        {
            search = null;
            previewDirty = true;
            fields = new SerializedObject(draft);
            UpdateDirty();
            Repaint();
        }

        public override void SaveChanges()
        {
            if (SaveAll())
            {
                base.SaveChanges();
            }
        }

        public override void DiscardChanges()
        {
            if (!string.IsNullOrEmpty(savedJson))
            {
                document = LevelDesignIO.Read(savedJson);
                timeOverride = LevelDesignIO.HasField(savedJson, "timeLimit");
                hintsOverride = LevelDesignIO.HasField(savedJson, "hints");
            }
            else
            {
                document = null;
            }

            if (!string.IsNullOrEmpty(savedOrder))
            {
                orderDraft = JsonUtility.FromJson<DesignOrder>(savedOrder);
            }

            AfterUndo();
            base.DiscardChanges();
        }

        private bool CanLeave()
        {
            UpdateDirty();
            if (!hasUnsavedChanges)
            {
                return true;
            }

            int answer = EditorUtility.DisplayDialogComplex("Bản nháp chưa lưu", "Lưu trước khi mở/tạo màn khác?", "Lưu", "Hủy", "Bỏ bản nháp");
            if (answer == 1)
            {
                return false;
            }

            if (answer == 2)
            {
                DiscardChanges();
                return true;
            }

            return SaveAll();
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Thoát Play để sửa màn/config. Cửa sổ không sửa dữ liệu trong lúc chơi.", MessageType.Info);
                return;
            }

            tab = GUILayout.Toolbar(tab, new[] { "Sửa màn", "Thứ tự trong game", "Config ngoài Play" });
            if (tab == 0)
            {
                DrawLevel();
            }
            else if (tab == 1)
            {
                DrawOrder();
            }
            else
            {
                DrawConfigs();
            }

            if (!string.IsNullOrEmpty(status))
            {
                EditorGUILayout.HelpBox(status, statusType);
            }
        }

        private void DrawLevel()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string[] names = Array.ConvertAll(sources, Path.GetFileNameWithoutExtension);
                sourceChoice = Mathf.Clamp(sourceChoice, 0, Math.Max(0, names.Length - 1));
                sourceChoice = EditorGUILayout.Popup(sourceChoice, names, GUILayout.MinWidth(120));
                if (GUILayout.Button("Mở", EditorStyles.toolbarButton))
                {
                    Run(() =>
                    {
                        if (sources.Length > 0 && CanLeave())
                        {
                            Load(sources[sourceChoice]);
                        }
                    });
                }

                if (GUILayout.Button("Mới", EditorStyles.toolbarButton))
                {
                    Run(() =>
                    {
                        if (CanLeave())
                        {
                            New(false);
                        }
                    });
                }

                if (GUILayout.Button("Nhân bản", EditorStyles.toolbarButton))
                {
                    Run(() =>
                    {
                        if (document != null && CanLeave())
                        {
                            New(true);
                        }
                    });
                }

                if (GUILayout.Button("Lưu thiết kế", EditorStyles.toolbarButton))
                {
                    SaveAll();
                }

                if (GUILayout.Button("Xuất vào game", EditorStyles.toolbarButton))
                {
                    Run(() =>
                    {
                        if (SaveAll())
                        {
                            LevelDesignIO.Export();
                        }
                    });
                }

                if (GUILayout.Button("Nạp danh sách", EditorStyles.toolbarButton))
                {
                    Run(ReloadSources);
                }
            }

            EditorGUILayout.LabelField("Nguồn: " + LevelDesignIO.SourceFolder + " · Lưu chưa đổi game; Xuất/Sync mới cập nhật game.", EditorStyles.wordWrappedMiniLabel);
            if (document == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(Mathf.Clamp(position.width * .53f, 390, 650))))
                {
                    boardMode = GUILayout.Toolbar(boardMode, new[] { "Lưới chỉnh sửa", "Xem trước 3D" });
                    Rect area = GUILayoutUtility.GetRect(200, Mathf.Max(320, position.height - 230), GUILayout.ExpandWidth(true));
                    if (boardMode == 0)
                    {
                        DrawGrid(area);
                    }
                    else
                    {
                        DrawPreview(area);
                    }

                    EditorGUILayout.LabelField("Ô chọn: hàng " + cellR + ", cột " + cellC + " (bắt đầu từ 0).", EditorStyles.miniLabel);
                    EditorGUILayout.HelpBox("Chọn loại bên phải → chọn vật thể trên lưới/danh sách. Bật Đặt lại rồi chạm ô đích để di chuyển. Portal sửa hai đầu A/B trong bảng.", MessageType.None);
                }

                using (new EditorGUILayout.VerticalScope())
                    DrawProperties();
            }
        }

        private void DrawProperties()
        {
            fields ??= new SerializedObject(draft);
            fields.Update();
            string before = LevelDesignIO.Geometry(document);
            int undoGroup = Undo.GetCurrentGroup();
            propertiesScroll = EditorGUILayout.BeginScrollView(propertiesScroll);
            var doc = fields.FindProperty("document");
            EditorGUI.BeginChangeCheck();
            foreach (string key in new[]
            {
                "id",
                "name",
                "cols",
                "rows",
                "splash"
            }

            )
            {
                EditorGUILayout.PropertyField(doc.FindPropertyRelative(key), Label(key));
            }

            EditorGUILayout.PropertyField(fields.FindProperty("timeOverride"), new GUIContent("Giờ riêng của màn"));
            if (fields.FindProperty("timeOverride").boolValue)
            {
                EditorGUILayout.PropertyField(doc.FindPropertyRelative("timeLimit"), new GUIContent("Giây (0: không tính giờ)"));
            }

            EditorGUILayout.PropertyField(fields.FindProperty("hintsOverride"), new GUIContent("Lượt gợi ý riêng"));
            if (fields.FindProperty("hintsOverride").boolValue)
            {
                EditorGUILayout.PropertyField(doc.FindPropertyRelative("hints"), new GUIContent("Lượt (-1: không giới hạn)"));
            }

            EditorGUILayout.LabelField("minMoves: " + document.minMoves + " · tối ưu đã biết: " + document.solveExhausted);
            kind = EditorGUILayout.Popup("Loại vật thể", kind, KindNames);
            var array = doc.FindPropertyRelative(DesignKinds.All[kind].JsonField);
            selected = Mathf.Clamp(selected, 0, Math.Max(0, array.arraySize - 1));
            if (array.arraySize > 0)
            {
                string[] items = new string[array.arraySize];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = KindNames[kind] + " " + (i + 1);
                }

                selected = EditorGUILayout.Popup("Đang chọn", selected, items);
                var item = array.GetArrayElementAtIndex(selected);
                var property = item.Copy();
                var end = item.GetEndProperty();
                if (property.NextVisible(true))
                {
                    do
                    {
                        if (SerializedProperty.EqualContents(property, end))
                        {
                            break;
                        }

                        if (property.propertyType == SerializedPropertyType.String && property.name == "color")
                        {
                            string[] colors = kind == DesignKinds.Gate ? Colors : new[]
                            {
                                "pink",
                                "blue",
                                "yellow",
                                "green",
                                "orange",
                                "purple"
                            };
                            property.stringValue = colors[EditorGUILayout.Popup("Màu", Math.Max(0, Array.IndexOf(colors, property.stringValue)), colors)];
                        }
                        else if (property.propertyType == SerializedPropertyType.String && (property.name == "axis" || property.name == "o"))
                        {
                            property.stringValue = EditorGUILayout.Popup("Hướng", property.stringValue == "v" ? 1 : 0, new[] { "Ngang (h)", "Dọc (v)" }) == 0 ? "h" : "v";
                        }
                        else if (property.propertyType == SerializedPropertyType.String && property.name == "side" && kind == DesignKinds.Gate)
                        {
                            string[] sides =
                            {
                                "t",
                                "b",
                                "l",
                                "r"
                            };
                            property.stringValue = sides[EditorGUILayout.Popup("Cạnh", Math.Max(0, Array.IndexOf(sides, property.stringValue)), new[] { "Trên", "Dưới", "Trái", "Phải" })];
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(property, Label(property.name), true);
                        }
                    }
                    while (property.NextVisible(false));
                }
            }

            bool changed = EditorGUI.EndChangeCheck();
            fields.ApplyModifiedProperties();
            if (changed)
            {
                if (before != LevelDesignIO.Geometry(document))
                {
                    Undo.RecordObject(draft, "Đổi bố cục màn");
                    document.minMoves = -1;
                    document.solveExhausted = false;
                }

                Undo.CollapseUndoOperations(undoGroup);
                Changed();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Thêm tại ô chọn"))
                {
                    Run(AddItem);
                }

                using (new EditorGUI.DisabledScope(Items.Count == 0))
                    if (GUILayout.Button("Xóa vật thể"))
                    {
                        Change(() => Items.RemoveAt(selected));
                    }
            }

            placeMode = EditorGUILayout.ToggleLeft("Đặt lại vật thể đang chọn vào ô/cạnh chạm", placeMode);
            if (kind == DesignKinds.Portal || kind == DesignKinds.Pylon || kind == DesignKinds.Switch)
            {
                EditorGUILayout.HelpBox("r/c = hàng/cột. Mặt cổng: N trên, S dưới, W trái, E phải. Portal a/b là hai đầu; trụ to cần đúng 2 mặt; chốt a/b là danh sách ô barie.", MessageType.None);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Kiểm dữ liệu"))
                {
                    Run(() =>
                    {
                        LevelDesignIO.Validate(CurrentJson);
                        SetStatus("Dữ liệu hợp lệ; chưa có nghĩa màn giải được.");
                    });
                }

                if (GUILayout.Button(search == null ? "Tìm đường thắng" : "Dừng tìm"))
                {
                    Run(ToggleSearch);
                }
            }

            maxStates = Mathf.Clamp(EditorGUILayout.IntField("Giới hạn trạng thái", maxStates), 1, 2000000);
            EditorGUILayout.EndScrollView();
        }

        private static GUIContent Label(string key)
        {
            switch (key)
            {
                case "id":
                    return new GUIContent("ID màn", "Đổi ID là lưu bản mới; không đổi tên hoặc xóa file cũ.");
                case "name":
                    return new GUIContent("Tên màn");
                case "cols":
                    return new GUIContent("Số cột");
                case "rows":
                    return new GUIContent("Số hàng");
                case "r":
                    return new GUIContent("Hàng (r)");
                case "c":
                    return new GUIContent("Cột (c)");
                case "i":
                    return new GUIContent("Ô dọc cạnh (i)");
                case "len":
                    return new GUIContent("Dài (ô)");
                case "mud":
                    return new GUIContent("Bùn (0 sạch, 1 thường)");
                case "truck":
                    return new GUIContent("Xe tải đẩy");
                case "splash":
                    return new GUIContent("Bùn văng");
                case "need":
                    return new GUIContent("Số xe cần rửa");
                case "phase":
                    return new GUIContent("Pha đèn (0/1)");
                default:
                    return new GUIContent(key);
            }
        }

        private IList Items => (IList)typeof(LevelDesignData).GetField(DesignKinds.All[kind].JsonField).GetValue(document);

        private void AddItem()
        {
            Change(() =>
            {
                object item;
                switch (kind)
                {
                    case DesignKinds.Car:
                        item = new DesignCar();
                        break;
                    case DesignKinds.Gate:
                        item = new DesignGate
                        {
                            side = gateSide,
                            i = gateIndex
                        };
                        break;
                    case DesignKinds.Paint:
                        item = new DesignPaint();
                        break;
                    case DesignKinds.Light:
                        item = new DesignLight();
                        break;
                    case DesignKinds.Barrier:
                        item = new DesignBarrier();
                        break;
                    case DesignKinds.Portal:
                        item = new DesignPortal
                        {
                            a = new DesignFace
                            {
                                r = cellR,
                                c = cellC
                            },
                            b = new DesignFace
                            {
                                r = Math.Max(0, document.rows - 1),
                                c = Math.Max(0, document.cols - 1),
                                side = "S"
                            }
                        };
                        break;
                    case DesignKinds.Pylon:
                        item = new DesignPylon();
                        break;
                    case DesignKinds.Switch:
                        item = new DesignSwitch();
                        break;
                    default:
                        item = new DesignCell();
                        break;
                }

                if (item is DesignCell cell)
                {
                    cell.r = cellR;
                    cell.c = cellC;
                }

                Items.Add(item);
                selected = Items.Count - 1;
            });
        }

        private void Change(Action edit)
        {
            Undo.RecordObject(draft, "Sửa bản thiết kế");
            edit();
            document.minMoves = -1;
            document.solveExhausted = false;
            Changed();
        }

        private void Changed()
        {
            search = null;
            previewDirty = true;
            if (document != null)
            {
                selected = Mathf.Clamp(selected, 0, Math.Max(0, Items.Count - 1));
            }

            UpdateDirty();
            Repaint();
        }

        private void ReloadSources()
        {
            sources = LevelDesignIO.ListSources();
            sourceChoice = Math.Max(0, Array.IndexOf(sources, openedPath));
        }

        private void Load(string path)
        {
            string json = File.ReadAllText(path);
            LevelDesignIO.Validate(json, LevelDesignIO.FileId(path));
            var next = LevelDesignIO.Read(json);
            document = next;
            originalJson = json;
            timeOverride = LevelDesignIO.HasField(json, "timeLimit");
            hintsOverride = LevelDesignIO.HasField(json, "hints");
            openedId = next.id;
            openedPath = path;
            savedJson = CurrentJson;
            selected = cellR = cellC = 0;
            Undo.ClearUndo(draft);
            Changed();
            SetStatus("Đã mở " + path);
        }

        private void New(bool duplicate)
        {
            duplicate = duplicate && document != null;
            document = duplicate ? JsonUtility.FromJson<LevelDesignData>(JsonUtility.ToJson(document)) : new LevelDesignData();
            document.id = LevelDesignIO.NextId();
            document.name = duplicate ? document.name + " (bản sao)" : "Màn " + document.id;
            if (!duplicate)
            {
                originalJson = "";
                timeOverride = hintsOverride = false;
            }

            openedId = 0;
            openedPath = savedJson = "";
            selected = cellR = cellC = 0;
            Undo.ClearUndo(draft);
            Changed();
            SetStatus("Bản nháp mới; chưa tạo file. Thêm xe/cổng rồi lưu.");
        }

        private bool SaveAll()
        {
            bool ok = true;
            Run(() =>
            {
                try
                {
                    if (document != null && (CurrentJson != savedJson || !LevelDesignIO.HasDesignSource))
                    {
                        string json = CurrentJson;
                        int previous = openedId;
                        openedPath = LevelDesignIO.Save(json, openedId);
                        openedId = document.id;
                        originalJson = json;
                        savedJson = CurrentJson;
                        if (previous != document.id && Array.IndexOf(orderDraft.ids, document.id) < 0)
                        {
                            var ids = new List<int>(orderDraft.ids)
                            {
                                document.id
                            };
                            orderDraft.ids = ids.ToArray();
                        }

                        ReloadSources();
                    }

                    if (orderDraft != null && JsonUtility.ToJson(orderDraft) != savedOrder)
                    {
                        LevelDesignIO.SaveOrder(orderDraft);
                        savedOrder = JsonUtility.ToJson(orderDraft);
                    }

                    UpdateDirty();
                    SetStatus("Đã lưu bản thiết kế/thứ tự. Bấm Xuất vào game để áp dụng lúc chơi.");
                }
                catch
                {
                    ok = false;
                    UpdateDirty();
                    throw;
                }
            });
            return ok;
        }

        private void DrawOrder()
        {
            EditorGUILayout.HelpBox("Đổi thứ tự hoặc bỏ khỏi danh sách không xóa file màn. Màn mới được thêm cuối danh sách khi lưu. Lưu rồi Xuất vào game.", MessageType.Info);
            if (orderDraft == null)
            {
                return;
            }

            orderScroll = EditorGUILayout.BeginScrollView(orderScroll);
            for (int i = 0; i < orderDraft.ids.Length; i++)
            {
                int row = i;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField((i + 1) + ". Màn " + orderDraft.ids[i]);
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button("Lên", GUILayout.Width(55)))
                        {
                            OrderChange(ids =>
                            {
                                int n = ids[row];
                                ids[row] = ids[row - 1];
                                ids[row - 1] = n;
                            });
                            break;
                        }

                    using (new EditorGUI.DisabledScope(i + 1 == orderDraft.ids.Length))
                        if (GUILayout.Button("Xuống", GUILayout.Width(55)))
                        {
                            OrderChange(ids =>
                            {
                                int n = ids[row];
                                ids[row] = ids[row + 1];
                                ids[row + 1] = n;
                            });
                            break;
                        }

                    if (GUILayout.Button("Bỏ", GUILayout.Width(55)))
                    {
                        OrderChange(ids => ids.RemoveAt(row));
                        break;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            var available = new List<int>();
            foreach (string path in sources)
            {
                int id = LevelDesignIO.FileId(path);
                if (Array.IndexOf(orderDraft.ids, id) < 0)
                {
                    available.Add(id);
                }
            }

            if (available.Count > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    addOrderChoice = Mathf.Clamp(addOrderChoice, 0, available.Count - 1);
                    addOrderChoice = EditorGUILayout.Popup(addOrderChoice, available.ConvertAll(id => "Màn " + id).ToArray());
                    if (GUILayout.Button("Thêm màn đã lưu"))
                    {
                        OrderChange(ids => ids.Add(available[addOrderChoice]));
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Lưu thứ tự"))
                {
                    Run(() =>
                    {
                        LevelDesignIO.SaveOrder(orderDraft);
                        savedOrder = JsonUtility.ToJson(orderDraft);
                        UpdateDirty();
                    });
                }

                if (GUILayout.Button("Xuất vào game"))
                {
                    Run(() =>
                    {
                        if (SaveAll())
                        {
                            LevelDesignIO.Export();
                        }
                    });
                }
            }
        }

        private void OrderChange(Action<List<int>> edit)
        {
            Undo.RecordObject(draft, "Đổi thứ tự màn");
            var ids = new List<int>(orderDraft.ids);
            edit(ids);
            orderDraft.ids = ids.ToArray();
            UpdateDirty();
        }

        private void DrawConfigs()
        {
            configChoice = GUILayout.Toolbar(configChoice, new[] { "Game", "Feel", "Camera" });
            string name = new[]
            {
                "GameConfig",
                "FeelConfig",
                "CameraConfig"
            }[configChoice];
            var target = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/_Project/Config/" + name + ".asset");
            if (target == null)
            {
                EditorGUILayout.HelpBox("Thiếu asset " + name, MessageType.Error);
                return;
            }

            if (configTarget != target)
            {
                if (configEditor != null)
                {
                    DestroyImmediate(configEditor);
                }

                configTarget = target;
                configEditor = Editor.CreateEditor(target);
            }

            EditorGUILayout.HelpBox("Đang sửa asset thật ngoài Play. Ctrl+Z để hoàn tác; Lưu config để ghi xuống file. Camera cập nhật trong tab Xem trước 3D.", MessageType.Info);
            propertiesScroll = EditorGUILayout.BeginScrollView(propertiesScroll);
            configEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Lưu config"))
            {
                AssetDatabase.SaveAssetIfDirty(target);
                SetStatus("Đã lưu " + name);
            }
        }

        private void ToggleSearch()
        {
            if (search != null)
            {
                search = null;
                SetStatus("Đã dừng tìm, chưa kết luận.");
                return;
            }

            LevelDef level = LevelDesignIO.Validate(CurrentJson);
            searchGeometry = LevelDesignIO.Geometry(document);
            search = new PathSearch(level, GameState.Initial(level), maxStates);
            SetStatus("Đang tìm; sửa màn hoặc đóng cửa sổ sẽ hủy tìm cũ.");
        }

        private void SearchStep()
        {
            if (search == null)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                search = null;
                return;
            }

            Run(() =>
            {
                var budget = SearchBudget.Start(4);
                while (!search.IsDone && !budget.Expired)
                {
                    search.Step(1);
                }

                if (search.IsDone)
                {
                    var result = search.Result;
                    search = null;
                    if (document == null || searchGeometry != LevelDesignIO.Geometry(document))
                    {
                        return;
                    }

                    // PathSearch tìm đường thắng, không bảo đảm tối ưu. Không ghi độ dài đường này thành minMoves.
                    SetStatus(result.Moves != null ? "Giải được: tìm thấy đường " + result.Moves.Count + " nước. Chưa khẳng định tối ưu; không đổi minMoves." : result.Exhausted ? "Đã duyệt hết: không có đường thắng." : "Hết ngân sách trạng thái: chưa kết luận, không phải vô nghiệm.", result.Moves == null ? MessageType.Warning : MessageType.Info);
                }

                Repaint();
            });
        }

        private void DrawGrid(Rect area)
        {
            if (document.cols < 1 || document.rows < 1 || document.cols > 30 || document.rows > 30)
            {
                GUI.Label(area, "Đặt số hàng/cột trong khoảng 1–30 để xem lưới.");
                return;
            }

            const float margin = 28;
            float unit = Mathf.Min((area.width - margin * 2) / document.cols, (area.height - margin * 2) / document.rows);
            var grid = new Rect(area.center.x - unit * document.cols / 2, area.center.y - unit * document.rows / 2, unit * document.cols, unit * document.rows);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = Color.white
                }
            };
            for (int r = 0; r < document.rows; r++)
            {
                for (int c = 0; c < document.cols; c++)
                {
                    Rect cell = GridRect(grid, unit, r, c);
                    EditorGUI.DrawRect(cell, new Color(.2f, .23f, .3f));
                    string label = r + "," + c;
                    for (int k = 0; k < DesignKinds.All.Length; k++)
                    {
                        if (k == DesignKinds.Car || k == DesignKinds.Gate)
                        {
                            continue;
                        }

                        IList list = (IList)typeof(LevelDesignData).GetField(DesignKinds.All[k].JsonField).GetValue(document);
                        for (int i = 0; i < list.Count; i++)
                        {
                            bool hit = list[i] is DesignCell p && p.r == r && p.c == c || list[i] is DesignPortal portal && (portal.a.r == r && portal.a.c == c || portal.b.r == r && portal.b.c == c);
                            if (hit)
                            {
                                label += "\n" + DesignKinds.All[k].ShortLabel + (i + 1);
                            }
                        }
                    }

                    GUI.Label(cell, label, style);
                }
            }

            for (int i = 0; i < document.cars.Count; i++)
            {
                var car = document.cars[i];
                Rect box = GridRect(grid, unit, car.r, car.c);
                box.width = unit * (car.axis == "h" ? car.len : 1) - 4;
                box.height = unit * (car.axis == "v" ? car.len : 1) - 4;
                box.x += 1;
                box.y += 1;
                EditorGUI.DrawRect(box, Tint(car.color));
                GUI.Label(box, "Xe " + (i + 1) + " · " + car.axis + (car.mud > 0 ? " · bùn" : ""), style);
                if (kind == DesignKinds.Car && selected == i)
                {
                    Outline(box, Color.white);
                }
            }

            for (int i = 0; i < document.gates.Count; i++)
            {
                var gate = document.gates[i];
                Rect edge = GateRect(grid, unit, gate.side, gate.i);
                EditorGUI.DrawRect(edge, Tint(gate.color));
                if (kind == DesignKinds.Gate && selected == i)
                {
                    Outline(edge, Color.white);
                }
            }

            Outline(GridRect(grid, unit, cellR, cellC), Color.yellow);
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !area.Contains(e.mousePosition))
            {
                return;
            }

            if (grid.Contains(e.mousePosition))
            {
                cellR = Mathf.Clamp((int)((e.mousePosition.y - grid.y) / unit), 0, document.rows - 1);
                cellC = Mathf.Clamp((int)((e.mousePosition.x - grid.x) / unit), 0, document.cols - 1);
                gateIndex = gateSide == "l" || gateSide == "r" ? cellR : cellC;
                if (placeMode && Items.Count > 0 && Items[selected] is DesignCell movable)
                {
                    Change(() =>
                    {
                        movable.r = cellR;
                        movable.c = cellC;
                    });
                }
                else
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (Items[i] is DesignCar car && cellR >= car.r && cellR < car.r + (car.axis == "v" ? car.len : 1) && cellC >= car.c && cellC < car.c + (car.axis == "h" ? car.len : 1))
                        {
                            selected = i;
                            break;
                        }

                        if (Items[i] is DesignCell p && p.r == cellR && p.c == cellC)
                        {
                            selected = i;
                            break;
                        }

                        if (Items[i] is DesignPortal portal && (portal.a.r == cellR && portal.a.c == cellC || portal.b.r == cellR && portal.b.c == cellC))
                        {
                            selected = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                string side = e.mousePosition.x < grid.x ? "l" : e.mousePosition.x > grid.xMax ? "r" : e.mousePosition.y < grid.y ? "t" : "b";
                int index = side == "l" || side == "r" ? Mathf.Clamp((int)((e.mousePosition.y - grid.y) / unit), 0, document.rows - 1) : Mathf.Clamp((int)((e.mousePosition.x - grid.x) / unit), 0, document.cols - 1);
                gateSide = side;
                gateIndex = index;
                if (kind == DesignKinds.Gate && placeMode && Items.Count > 0)
                {
                    Change(() =>
                    {
                        var gate = (DesignGate)Items[selected];
                        gate.side = side;
                        gate.i = index;
                    });
                }
                else
                {
                    kind = DesignKinds.Gate;
                    for (int i = 0; i < document.gates.Count; i++)
                    {
                        if (document.gates[i].side == side && document.gates[i].i == index)
                        {
                            selected = i;
                            break;
                        }
                    }
                }
            }

            e.Use();
            Repaint();
        }

        private static Rect GridRect(Rect board, float unit, int r, int c) => new Rect(board.x + c * unit, board.y + r * unit, unit - 2, unit - 2);
        private static Rect GateRect(Rect board, float unit, string side, int index)
        {
            if (side == "t" || side == "b")
            {
                return new Rect(board.x + index * unit, side == "t" ? board.y - 8 : board.yMax + 2, unit - 2, 6);
            }

            return new Rect(side == "l" ? board.x - 8 : board.xMax + 2, board.y + index * unit, 6, unit - 2);
        }

        private Color Tint(string color)
        {
            return palette != null && Array.IndexOf(Colors, color) >= 0 && Enum.TryParse(color, true, out TileColor key) ? palette.Car(key) : Color.gray;
        }

        private static void Outline(Rect r, Color color)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 2), color);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 2, r.width, 2), color);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 2, r.height), color);
            EditorGUI.DrawRect(new Rect(r.xMax - 2, r.y, 2, r.height), color);
        }

        private void DrawPreview(Rect area)
        {
            if (previewDirty)
            {
                CleanupPreview();
                previewDirty = false;
                previewError = "";
                try
                {
                    if (palette == null || cameraConfig == null)
                    {
                        throw new InvalidOperationException("Thiếu VisualPalette/CameraConfig trong Config.");
                    }

                    var level = LevelDesignIO.Validate(CurrentJson);
                    preview = new PreviewRenderUtility();
                    var root = new GameObject("Level Editor Preview", typeof(BoardView))
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    preview.AddSingleGO(root);
                    previewBoard = root.GetComponent<BoardView>();
                    previewBoard.Build(level, GameState.Initial(level), palette);
                    preview.lights[0].intensity = 1.2f;
                    preview.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
                    preview.lights[1].intensity = .5f;
                    preview.ambientColor = Color.gray;
                    preview.camera.nearClipPlane = .1f;
                    preview.camera.farClipPlane = 100;
                }
                catch (Exception error)
                {
                    previewError = error.Message;
                    CleanupPreview();
                }
            }

            if (preview == null)
            {
                GUI.Label(area, "Chưa xem trước được:\n" + previewError, EditorStyles.wordWrappedLabel);
                return;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            // PreviewRenderUtility có scene riêng: không sinh object vào Play.unity hay lưu scene ngoài ý muốn.
            Texture image;
            preview.BeginPreview(area, GUIStyle.none);
            try
            {
                previewBoard.FitCamera(preview.camera, cameraConfig, new Rect(0, 0, 1, 1));
                preview.Render();
            }
            finally
            {
                image = preview.EndPreview();
            }

            GUI.DrawTexture(area, image, ScaleMode.StretchToFill, false);
        }

        private void CleanupPreview()
        {
            if (preview != null)
            {
                preview.Cleanup();
            }

            preview = null;
            previewBoard = null;
        }

        private void SetStatus(string text, MessageType type = MessageType.Info)
        {
            status = text;
            statusType = type;
            Repaint();
        }

        private void Run(Action action)
        {
            try
            {
                action();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception error)
            {
                search = null;
                SetStatus(error.Message, MessageType.Error);
            }
        }
    }
}
