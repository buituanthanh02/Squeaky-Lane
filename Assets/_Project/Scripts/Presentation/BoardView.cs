using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField, Tooltip("Theme013: chỉ cảnh ngoài bãi/nhãn xe; thiếu giữ hình cũ.")] private UiTheme sceneTheme = null;
        private LevelDef level;
        private VisualPalette palette;
        private MaterialPropertyBlock block;
        private GameObject content;
        private SceneThemeView environmentView;
        private CarView[] cars;
        private Renderer[] lights, barriers;
        private MechanicView[] lightViews;
        private GameObject[] muds;
        private Transform[] pylons;
        private Font labelFont;
        // Font động dùng chung cho chữ trên bãi; null cho tới lần Build đầu. FeelEffects dùng lại để khỏi tạo thêm một font/atlas.
        public Font LabelFont => labelFont;
        private CarCatalog carCatalog;
        private bool carWarningShown;
        private MechanicCatalog mechanicCatalog;
        private bool mechanicWarningShown, hasSurface;
        private Camera occlusionCamera;
        private readonly List<MechanicOcclusion> occluders = new List<MechanicOcclusion>();
        private readonly List<Renderer> switchWalls = new List<Renderer>();
        private readonly List<Cell> switchWallCells = new List<Cell>();
        // 014: trạm rửa theo ô (r * Cols + c); rỗng khi màn không có vòi xịt hoặc thiếu prefab (dùng khối dự phòng).
        private readonly Dictionary<int, WashStationView> washStations = new Dictionary<int, WashStationView>();
        public bool HasWashStations => washStations.Count > 0;
        public WashStationView WashStationAt(int r, int c)
        {
            return level != null && washStations.TryGetValue(r * level.Cols + c, out var station) ? station : null;
        }
        // 015: hầm rửa ở cổng nước, khóa theo (cạnh, chỉ số); rỗng khi catalog chưa có WashGate (dùng cổng nước cũ).
        private readonly Dictionary<int, WashStationView> washGates = new Dictionary<int, WashStationView>();
        private static int GateKey(GateEdge edge, int index) => (int)edge * 1024 + index;
        public WashStationView WashGateAt(GateDef gate)
        {
            return washGates.TryGetValue(GateKey(gate.Edge, gate.Index), out var station) ? station : null;
        }
        // Tắt mọi trạm đang chạy (Hoàn tác/Chơi lại/hủy hoạt hình).
        public void CancelWash()
        {
            foreach (var station in washStations.Values) { if (station != null) { station.Cancel(); } }
            foreach (var station in washGates.Values) { if (station != null) { station.Cancel(); } }
        }
        public LevelDef Level => level;
        internal UiTheme Theme => sceneTheme;
        // 016: khoảng từ mép bãi tới tim phố (ô); âm khi không có phố (theme tắt/thiếu material) để xe ra cổng như cũ.
        public float StreetCenterOffset => sceneTheme != null && sceneTheme.StreetsEnabled && environmentView != null
            ? sceneTheme.StreetInner + sceneTheme.StreetWidth * .5f : -1;
        public CarView Car(int index) => cars[index];
        public void SetVisible(bool visible) { if (content != null) content.SetActive(visible); }
        public Vector3 Cell(float r, float c, float y = .32f) => new Vector3(c - (level.Cols - 1) * .5f, y, (level.Rows - 1) * .5f - r);
        public Vector3 Position(CarState car, int length) => Cell(car.R + (car.Axis == Axis.Vertical ? (length - 1) * .5f : 0), car.C + (car.Axis == Axis.Horizontal ? (length - 1) * .5f : 0));
        public static float Yaw(Axis axis) => axis == Axis.Horizontal ? 0 : 90;

        public void Build(LevelDef definition, GameState state, VisualPalette colors, CarCatalog vehicles = null, MechanicCatalog mechanics = null)
        {
            if (content != null) { content.SetActive(false); Release(content); }
            environmentView = null;
            level = definition; palette = colors; block = new MaterialPropertyBlock();
            carCatalog = vehicles;
            mechanicCatalog = mechanics;
            occluders.Clear();
            washStations.Clear(); washGates.Clear();
            switchWalls.Clear(); switchWallCells.Clear();
            if (labelFont == null) labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "Droid Sans", "sans-serif" }, 24);
            content = new GameObject("Board Runtime"); content.transform.SetParent(transform, false);
            var surface = SpawnMechanic(MechanicKind.Surface, Vector3.zero);
            hasSurface = surface != null;
            if (hasSurface)
            {
                surface.GetComponent<LotSurfaceView>().Configure(level, mechanicCatalog.Settings);
            }
            else
            {
                for (int r = 0; r < level.Rows; r++) for (int c = 0; c < level.Cols; c++)
                    Box("Ô " + r + "," + c, Cell(r, c, -.08f), new Vector3(.97f, .14f, .97f), palette.Floor);
            }
            for (int i = 0; i < level.Cols; i++) { Edge(GateEdge.Top, i); Edge(GateEdge.Bottom, i); }
            for (int i = 0; i < level.Rows; i++) { Edge(GateEdge.Left, i); Edge(GateEdge.Right, i); }
            foreach (var p in level.Cones)
            {
                if (SpawnMechanic(MechanicKind.Cone, Cell(p.R, p.C, LotSurfaceView.Top)) == null)
                    Box("Cọc", Cell(p.R, p.C, .24f), new Vector3(.65f, .5f, .65f), palette.Cone);
            }
            muds = new GameObject[level.Muds.Count];
            for (int i = 0; i < muds.Length; i++)
            {
                var p = level.Muds[i];
                var visual = SpawnMechanic(MechanicKind.Mud, Cell(p.R, p.C, LotSurfaceView.Top));
                muds[i] = visual != null ? visual.gameObject : Box("Bùn", Cell(p.R, p.C, .015f), new Vector3(.85f, .035f, .85f), palette.Mud).gameObject;
            }
            foreach (var p in level.Sprays)
            {
                var station = SpawnMechanic(MechanicKind.WashStation, Cell(p.R, p.C, LotSurfaceView.Top));
                var washView = station != null ? station.GetComponent<WashStationView>() : null;
                if (washView != null) { washStations[p.R * level.Cols + p.C] = washView; continue; }
                if (station != null) { station.gameObject.SetActive(false); Release(station.gameObject); }
                Box("Vòi xịt", Cell(p.R, p.C, .04f), new Vector3(.65f, .07f, .65f), palette.Car(TileColor.Water)); Label("~", Cell(p.R, p.C, .1f));
            }
            foreach (var p in level.Paints) Box("Sơn", Cell(p.R, p.C, .035f), new Vector3(.8f, .05f, .8f), palette.Car(p.Color));
            foreach (var p in level.Tables)
            {
                if (SpawnMechanic(MechanicKind.Turntable, Cell(p.R + .5f, p.C + .5f, LotSurfaceView.Top)) == null)
                {
                    Box("Bàn xoay", Cell(p.R + .5f, p.C + .5f, .02f), new Vector3(1.95f, .04f, 1.95f), palette.Table);
                    Label("↻", Cell(p.R + .5f, p.C + .5f, .8f));
                }
            }
            lights = new Renderer[level.Lights.Count];
            lightViews = new MechanicView[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                var p = level.Lights[i];
                lightViews[i] = SpawnMechanic(MechanicKind.TrafficLight, Cell(p.R, p.C, LotSurfaceView.Top));
                if (lightViews[i] == null) lights[i] = Box("Đèn", Cell(p.R, p.C, .12f), new Vector3(.75f, .23f, .75f), palette.Closed);
            }
            barriers = new Renderer[level.Barriers.Count];
            for (int i = 0; i < barriers.Length; i++) { var p = level.Barriers[i]; barriers[i] = Box("Barie", Cell(p.R, p.C, .18f), new Vector3(.9f, .35f, .3f), palette.Closed); Label(p.Need.ToString(), Cell(p.R, p.C, .5f)); }
            foreach (var p in level.Switches)
            {
                Box("Chốt", Cell(p.R, p.C, .03f), new Vector3(.65f, .06f, .65f), palette.Table); Label("A/B", Cell(p.R, p.C, .1f));
                AddSwitchWalls(p.A, "A"); AddSwitchWalls(p.B, "B");
            }
            for (int i = 0; i < level.Portals.Count; i++) { var p = level.Portals[i]; Face(p.A, (i + 1).ToString(), i); Face(p.B, (i + 1).ToString(), i); }
            pylons = new Transform[level.Pylons.Count];
            for (int i = 0; i < pylons.Length; i++)
            {
                var p = level.Pylons[i];
                var visual = SpawnMechanic(MechanicKind.Pylon, Cell(p.R, p.C, LotSurfaceView.Top));
                Transform turning;
                if (visual != null)
                {
                    turning = visual.MovingPart;
                }
                else
                {
                    var core = Box("Trụ xoay " + i, Cell(p.R, p.C, .22f), new Vector3(.75f, .42f, .75f), palette.Portal);
                    turning = core.transform;
                    var arm = Box("Hướng trụ", Cell(p.R, p.C, .47f), new Vector3(.15f, .07f, .95f), palette.Wall);
                    arm.transform.SetParent(core.transform, true);
                }
                pylons[i] = turning;
                // Turns=0: mặt A hướng Bắc, mặt B hướng Nam; xoay âm quanh Y là N→W như Core.
                string link = (level.Portals.Count + 1 + i).ToString();
                float labelY = visual != null ? Mathf.Max(mechanicCatalog.Settings.pylonLabelHeight,
                    mechanicCatalog.Settings.pylonDiscHeight + mechanicCatalog.Settings.pylonDoorHeight + mechanicCatalog.Settings.overlayLift) : .85f;
                float labelZ = visual != null ? mechanicCatalog.Settings.pylonEntryOffset : .55f;
                Label(link + "A", new Vector3(0, labelY, labelZ), turning); Label(link + "B", new Vector3(0, labelY, -labelZ), turning);
                Face(p.To[0], link + "A", level.Portals.Count + i); Face(p.To[1], link + "B", level.Portals.Count + i);
            }
            cars = new CarView[state.CarCount];
            for (int i = 0; i < cars.Length; i++)
            {
                var definitionCar = level.Cars[i];
                var color = state.CarAt(i).Color;
                var prefab = carCatalog != null ? carCatalog.Prefab(color, definitionCar.Truck) : null;
                var root = CarView.Usable(prefab, definitionCar.Len) ? Instantiate(prefab, content.transform, false) : new GameObject();
                root.name = "Xe " + (i + 1);
                root.transform.SetParent(content.transform, false);
                var view = root.GetComponent<CarView>();
                if (view == null) view = root.AddComponent<CarView>();
                view.Initialize(i, definitionCar.Len, colors, carCatalog, definitionCar.Truck, color, WarnMissingCar);
                view.ConfigureShadow(sceneTheme);
                cars[i] = view;
                if (sceneTheme == null || !sceneTheme.HideCarNumbers)
                {
                    view.SetLabel(Label((i + 1).ToString(), new Vector3(0, .47f, 0), root.transform));
                }
            }
            Snap(state);
            if (sceneTheme != null && sceneTheme.EnvironmentPrefab != null && sceneTheme.GroundMaterial != null)
            {
                var environment = Instantiate(sceneTheme.EnvironmentPrefab, content.transform, false);
                environmentView = environment.GetComponent<SceneThemeView>();
                environmentView.Configure(level, sceneTheme, carCatalog);
            }
        }
        private void Edge(GateEdge edge, int index)
        {
            var gate = level.GateAt(edge, index);
            float r = edge == GateEdge.Top ? -.65f : edge == GateEdge.Bottom ? level.Rows - .35f : index;
            float c = edge == GateEdge.Left ? -.65f : edge == GateEdge.Right ? level.Cols - .35f : index;
            bool horizontal = edge == GateEdge.Top || edge == GateEdge.Bottom;
            if (gate.HasValue)
            {
                bool water = gate.Value.Color == TileColor.Water;
                if (water && SpawnWashGate(edge, index)) { return; }
                float yaw = edge == GateEdge.Top ? 0 : edge == GateEdge.Bottom ? 180 : edge == GateEdge.Left ? 90 : -90;
                if (SpawnMechanic(water ? MechanicKind.WaterGate : MechanicKind.ColorGate,
                    Cell(r, c, LotSurfaceView.Top), water ? 0 : (int)gate.Value.Color, yaw) != null) return;
            }
            else if (hasSurface) { return; }
            Box(gate.HasValue ? "Cổng " + gate.Value.Color : "Viền", Cell(r, c, .14f), horizontal ? new Vector3(.97f, .28f, .22f) : new Vector3(.22f, .28f, .97f), gate.HasValue ? palette.Car(gate.Value.Color) : palette.Wall);
        }
        // Hầm rửa đặt gốc ở tâm mép bãi, +Z của prefab hướng ra ngoài; thảm kéo dài ra ngoài bãi để xe chạy xuyên qua.
        // Thiếu prefab hợp lệ thì trả false (không cảnh báo) để dùng cổng nước cũ như trước 015.
        private bool SpawnWashGate(GateEdge edge, int index)
        {
            var prefab = mechanicCatalog != null ? mechanicCatalog.Prefab(MechanicKind.WashGate, 0) : null;
            if (!MechanicView.Usable(prefab, MechanicKind.WashGate)) { return false; }
            float r = edge == GateEdge.Top ? -.5f : edge == GateEdge.Bottom ? level.Rows - .5f : index;
            float c = edge == GateEdge.Left ? -.5f : edge == GateEdge.Right ? level.Cols - .5f : index;
            float yaw = edge == GateEdge.Top ? 0 : edge == GateEdge.Bottom ? 180 : edge == GateEdge.Left ? -90 : 90;
            var view = SpawnMechanic(MechanicKind.WashGate, Cell(r, c, LotSurfaceView.Top), 0, yaw);
            var station = view != null ? view.GetComponent<WashStationView>() : null;
            if (station == null) { if (view != null) { view.gameObject.SetActive(false); Release(view.gameObject); } return false; }
            washGates[GateKey(edge, index)] = station;
            return true;
        }
        // Ô dư ngoài bãi cần chừa cho xe chạy xuyên hầm rửa: chỉ tính trục có cổng nước (cạnh trái/phải → ngang).
        private float WashGateReach(bool horizontalEdges)
        {
            if (washGates.Count == 0) { return 0; }
            float longest = 0;
            for (int i = 0; i < cars.Length; i++) { if (cars[i] != null) { longest = Mathf.Max(longest, cars[i].Length); } }
            foreach (var key in washGates.Keys)
            {
                var edge = (GateEdge)(key / 1024);
                bool sideEdge = edge == GateEdge.Left || edge == GateEdge.Right;
                if (sideEdge == horizontalEdges) { return longest; }
            }
            return 0;
        }
        private void Face(PortalFace p, string link, int pair)
        {
            var offset = p.Side == Side.N ? new Vector3(0, 0, .45f) : p.Side == Side.S ? new Vector3(0, 0, -.45f) : p.Side == Side.W ? Vector3.left * .45f : Vector3.right * .45f;
            bool horizontal = p.Side == Side.N || p.Side == Side.S;
            var pos = Cell(p.R, p.C, .18f) + offset;
            int count = mechanicCatalog != null && mechanicCatalog.Settings.portalColors != null ? Mathf.Max(1, mechanicCatalog.Settings.portalColors.Length) : 1;
            var visual = SpawnMechanic(MechanicKind.Portal, Cell(p.R, p.C, LotSurfaceView.Top) + offset,
                pair % count, horizontal ? 0 : 90);
            if (visual != null)
            {
                Label(link, visual.transform.localPosition + Vector3.up * (mechanicCatalog.Settings.portalHeight + mechanicCatalog.Settings.overlayLift));
                return;
            }
            Box("Portal " + link, pos, horizontal ? new Vector3(.82f, .3f, .12f) : new Vector3(.12f, .3f, .82f), palette.Portal); Label(link, pos + Vector3.up * .23f);
        }
        private MechanicView SpawnMechanic(MechanicKind kind, Vector3 position, int variant = 0, float yaw = 0)
        {
            var prefab = mechanicCatalog != null ? mechanicCatalog.Prefab(kind, variant) : null;
            if (!MechanicView.Usable(prefab, kind))
            {
                if (!mechanicWarningShown && (Application.isPlaying || mechanicCatalog != null))
                {
                    mechanicWarningShown = true;
                    Debug.LogWarning("MechanicCatalog thiếu prefab hợp lệ cho " + kind + "/" + variant + "; dùng khối dự phòng.", this);
                }
                return null;
            }
            var instance = Instantiate(prefab, content.transform, false);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            if (kind != MechanicKind.Surface && kind != MechanicKind.Mud && kind != MechanicKind.WashStation)
            {
                var bounds = SceneThemeView.LocalBounds(instance);
                if (bounds.size.sqrMagnitude > 0)
                {
                    ContactShadowView.Attach(instance.transform, sceneTheme, new Vector2(bounds.size.x, bounds.size.z), LotSurfaceView.Top,
                        new Vector2(bounds.center.x, bounds.center.z));
                }
            }
            var occlusion = instance.GetComponent<MechanicOcclusion>();
            if (occlusion != null && occlusion.IsConfigured) { occluders.Add(occlusion); }
            return instance.GetComponent<MechanicView>();
        }
        private Renderer Box(string name, Vector3 position, Vector3 size, Color color, Transform parent = null, bool collider = false)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(parent != null ? parent : content.transform, false); obj.transform.localPosition = position; obj.transform.localScale = size;
            if (!collider) Release(obj.GetComponent<Collider>());
            var renderer = obj.GetComponent<Renderer>(); renderer.sharedMaterial = palette.Material; Colorize(renderer, color); return renderer;
        }
        private Transform Label(string value, Vector3 position, Transform parent = null)
        {
            var obj = new GameObject(value); obj.transform.SetParent(parent != null ? parent : content.transform, false);
            obj.transform.localPosition = position; obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var text = obj.AddComponent<TextMesh>(); text.font = labelFont; text.text = value; text.fontSize = 36; text.characterSize = .08f; text.anchor = TextAnchor.MiddleCenter; text.color = Color.white;
            obj.GetComponent<Renderer>().sharedMaterial = labelFont.material;
            return obj.transform;
        }
        private void WarnMissingCar(TileColor color, bool truck)
        {
            // Các test/preview cũ cố ý không dùng catalog. Game thật luôn báo khi phải dự phòng.
            if (carWarningShown || (!Application.isPlaying && carCatalog == null)) return;
            carWarningShown = true;
            Debug.LogWarning("CarCatalog thiếu prefab/material hợp lệ cho " + color + (truck ? " xe tải" : " xe thường") + "; dùng xe khối dự phòng.", this);
        }
        private void AddSwitchWalls(IReadOnlyList<Cell> cells, string label)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var p = cells[i]; if (!level.Contains(p.R, p.C)) continue;
                switchWallCells.Add(p); switchWalls.Add(Box("Barie " + label, Cell(p.R, p.C, .2f), new Vector3(.85f, .4f, .17f), palette.Closed));
                Label(label, Cell(p.R, p.C, .48f));
            }
        }
        private void Colorize(Renderer renderer, Color color) { block.SetColor("_Color", color); renderer.SetPropertyBlock(block); }
        public void Snap(GameState state)
        {
            for (int i = 0; i < cars.Length; i++) { var c = state.CarAt(i); cars[i].Show(Position(c, cars[i].Length), Yaw(c.Axis), c.Color, c.Mud, c.Alive); }
            RefreshMechanics(state);
        }
        public void RefreshMechanics(GameState state)
        {
            for (int i = 0; i < muds.Length; i++) muds[i].SetActive(state.MudAt(i));
            for (int i = 0; i < lights.Length; i++)
            {
                bool red = LightRule.IsRed(state, level.Lights[i]);
                if (lightViews[i] != null) lightViews[i].SetRed(red);
                else Colorize(lights[i], red ? palette.Closed : palette.Open);
            }
            for (int i = 0; i < barriers.Length; i++)
            {
                var b = level.Barriers[i]; bool open = GameRules.WashedCount(state) >= b.Need;
                for (int s = 0; s < level.Switches.Count; s++)
                {
                    var button = level.Switches[s];
                    var closed = BarrierRule.SwitchPressed(level, state, button) ? button.A : button.B;
                    for (int k = 0; k < closed.Count; k++) if (closed[k].R == b.R && closed[k].C == b.C) open = false;
                }
                Colorize(barriers[i], open ? palette.Open : palette.Closed);
                var scale = barriers[i].transform.localScale; scale.y = open ? .05f : .35f; barriers[i].transform.localScale = scale;
            }
            for (int i = 0; i < pylons.Length; i++) SetPylon(i, state.PylonTurnsAt(i) * -90);
            for (int i = 0; i < switchWalls.Count; i++)
            {
                var cell = switchWallCells[i]; bool closed = false;
                foreach (var button in level.Switches)
                {
                    var group = BarrierRule.SwitchPressed(level, state, button) ? button.A : button.B;
                    for (int k = 0; k < group.Count; k++) if (group[k].R == cell.R && group[k].C == cell.C) closed = true;
                }
                Colorize(switchWalls[i], closed ? palette.Closed : palette.Open);
                var scale = switchWalls[i].transform.localScale; scale.y = closed ? .4f : .04f; switchWalls[i].transform.localScale = scale;
            }
        }
        public void SetPylon(int i, float yaw) { pylons[i].localRotation = Quaternion.Euler(0, yaw, 0); }
        public void ConsumeMud(int i) { if (i >= 0 && i < muds.Length) muds[i].SetActive(false); }
        public Vector3 PylonPosition(int i) { var p = level.Pylons[i]; return transform.TransformPoint(Cell(p.R, p.C, .22f)); }
        public void FitCamera(Camera camera, CameraConfig config, Rect viewport)
        {
            occlusionCamera = camera;
            // Camera phải xóa nền cả màn: nếu chỉ vẽ vùng bãi, chữ HUD ngoài vùng đó có thể lưu vệt của frame cũ.
            // viewport chỉ dùng để chừa chỗ cho HUD, không giới hạn vùng camera vẽ.
            camera.orthographic = true; camera.rect = new Rect(0, 0, 1, 1);
            camera.backgroundColor = sceneTheme != null ? sceneTheme.CameraBackground : palette.Background;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.rotation = Quaternion.Euler(config.TiltDegrees, 0, 0);
            float aspect = camera.pixelRect.width / Mathf.Max(1, camera.pixelRect.height);
            // 015: cạnh có hầm rửa cần thêm chỗ cho cả thân xe ra ngoài bãi (+ thảm), nếu không xe chạy khỏi màn hình.
            // 016: có phố quanh bãi thì chừa thêm bề rộng phố để thấy xe chạy đi.
            float street = StreetCenterOffset > 0 ? sceneTheme.StreetInner + sceneTheme.StreetWidth * sceneTheme.StreetVisible : 0;
            float halfWidth = level.Cols * .5f + Mathf.Max(.9f, Mathf.Max(WashGateReach(true) + .4f, street)) + config.Padding;
            float halfHeight = (level.Rows * .5f + Mathf.Max(.9f, Mathf.Max(WashGateReach(false) + .4f, street)) + config.Padding) * Mathf.Sin(config.TiltDegrees * Mathf.Deg2Rad) + .7f;
            camera.orthographicSize = Mathf.Max(halfHeight / Mathf.Max(.001f, viewport.height),
                halfWidth / (aspect * Mathf.Max(.001f, viewport.width)));
            // Dịch tâm nhìn để bãi vẫn nằm giữa khoảng chơi, kể cả safe area không đối xứng.
            Vector2 center = viewport.center;
            float offsetX = (center.x * 2 - 1) * camera.orthographicSize * aspect;
            float offsetY = (center.y * 2 - 1) * camera.orthographicSize;
            camera.transform.position = transform.position - camera.transform.forward * 25
                - camera.transform.right * offsetX - camera.transform.up * offsetY;
            if (environmentView != null) { environmentView.EnsureGroundCoverage(camera); }
            RefreshOcclusion();
        }
        private void LateUpdate() { RefreshOcclusion(); }
        public void RefreshOcclusion()
        {
            bool enabled = mechanicCatalog != null && mechanicCatalog.Settings.fadeOccluders && occlusionCamera != null && cars != null;
            foreach (var occluder in occluders)
            {
                bool covered = false;
                if (enabled)
                {
                    Bounds obstacle = occluder.VisualBounds;
                    foreach (var car in cars)
                    {
                        if (car != null && car.TryGetVisualBounds(out var bounds) &&
                            MechanicOcclusion.CoversCar(occlusionCamera, obstacle, bounds, mechanicCatalog.Settings.occlusionPadding))
                        {
                            covered = true;
                            break;
                        }
                    }
                }
                occluder.SetFaded(covered);
            }
        }
        private static void Release(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        private void OnDestroy() { if (labelFont != null) Release(labelFont); }
    }
}
