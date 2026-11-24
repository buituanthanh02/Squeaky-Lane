using System.Collections.Generic;
using UnityEngine;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    public sealed class BoardView : MonoBehaviour
    {
        private LevelDef level;
        private VisualPalette palette;
        private MaterialPropertyBlock block;
        private GameObject content;
        private CarView[] cars;
        private Renderer[] lights, barriers;
        private GameObject[] muds;
        private Transform[] pylons;
        private Font labelFont;
        private CarCatalog carCatalog;
        private bool carWarningShown;
        private readonly List<Renderer> switchWalls = new List<Renderer>();
        private readonly List<Cell> switchWallCells = new List<Cell>();
        public LevelDef Level => level;
        public CarView Car(int index) => cars[index];
        public void SetVisible(bool visible) { if (content != null) content.SetActive(visible); }
        public Vector3 Cell(float r, float c, float y = .32f) => new Vector3(c - (level.Cols - 1) * .5f, y, (level.Rows - 1) * .5f - r);
        public Vector3 Position(CarState car, int length) => Cell(car.R + (car.Axis == Axis.Vertical ? (length - 1) * .5f : 0), car.C + (car.Axis == Axis.Horizontal ? (length - 1) * .5f : 0));
        public static float Yaw(Axis axis) => axis == Axis.Horizontal ? 0 : 90;

        public void Build(LevelDef definition, GameState state, VisualPalette colors, CarCatalog vehicles = null)
        {
            if (content != null) { content.SetActive(false); Release(content); }
            level = definition; palette = colors; block = new MaterialPropertyBlock();
            carCatalog = vehicles;
            switchWalls.Clear(); switchWallCells.Clear();
            if (labelFont == null) labelFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "Droid Sans", "sans-serif" }, 24);
            content = new GameObject("Board Runtime"); content.transform.SetParent(transform, false);
            for (int r = 0; r < level.Rows; r++) for (int c = 0; c < level.Cols; c++)
                Box("Ô " + r + "," + c, Cell(r, c, -.08f), new Vector3(.97f, .14f, .97f), palette.Floor);
            for (int i = 0; i < level.Cols; i++) { Edge(GateEdge.Top, i); Edge(GateEdge.Bottom, i); }
            for (int i = 0; i < level.Rows; i++) { Edge(GateEdge.Left, i); Edge(GateEdge.Right, i); }
            foreach (var p in level.Cones) Box("Cọc", Cell(p.R, p.C, .24f), new Vector3(.65f, .5f, .65f), palette.Cone);
            muds = new GameObject[level.Muds.Count];
            for (int i = 0; i < muds.Length; i++) { var p = level.Muds[i]; muds[i] = Box("Bùn", Cell(p.R, p.C, .015f), new Vector3(.85f, .035f, .85f), palette.Mud).gameObject; }
            foreach (var p in level.Sprays) { Box("Vòi xịt", Cell(p.R, p.C, .04f), new Vector3(.65f, .07f, .65f), palette.Car(TileColor.Water)); Label("~", Cell(p.R, p.C, .1f)); }
            foreach (var p in level.Paints) Box("Sơn", Cell(p.R, p.C, .035f), new Vector3(.8f, .05f, .8f), palette.Car(p.Color));
            foreach (var p in level.Tables) { Box("Bàn xoay", Cell(p.R + .5f, p.C + .5f, .02f), new Vector3(1.95f, .04f, 1.95f), palette.Table); Label("↻", Cell(p.R + .5f, p.C + .5f, .8f)); }
            lights = new Renderer[level.Lights.Count];
            for (int i = 0; i < lights.Length; i++) { var p = level.Lights[i]; lights[i] = Box("Đèn", Cell(p.R, p.C, .12f), new Vector3(.75f, .23f, .75f), palette.Closed); }
            barriers = new Renderer[level.Barriers.Count];
            for (int i = 0; i < barriers.Length; i++) { var p = level.Barriers[i]; barriers[i] = Box("Barie", Cell(p.R, p.C, .18f), new Vector3(.9f, .35f, .3f), palette.Closed); Label(p.Need.ToString(), Cell(p.R, p.C, .5f)); }
            foreach (var p in level.Switches)
            {
                Box("Chốt", Cell(p.R, p.C, .03f), new Vector3(.65f, .06f, .65f), palette.Table); Label("A/B", Cell(p.R, p.C, .1f));
                AddSwitchWalls(p.A, "A"); AddSwitchWalls(p.B, "B");
            }
            for (int i = 0; i < level.Portals.Count; i++) { var p = level.Portals[i]; Face(p.A, i + 1); Face(p.B, i + 1); }
            pylons = new Transform[level.Pylons.Count];
            for (int i = 0; i < pylons.Length; i++)
            {
                var p = level.Pylons[i]; var core = Box("Trụ xoay " + i, Cell(p.R, p.C, .22f), new Vector3(.75f, .42f, .75f), palette.Portal);
                pylons[i] = core.transform;
                // Turns=0: mặt A hướng Bắc, mặt B hướng Nam; xoay âm quanh Y là N→W như Core.
                var arm = Box("Hướng trụ", Cell(p.R, p.C, .47f), new Vector3(.15f, .07f, .95f), palette.Wall);
                arm.transform.SetParent(core.transform, true);
                string link = (level.Portals.Count + 1 + i).ToString();
                Label(link + "A", new Vector3(0, .85f, .55f), core.transform); Label(link + "B", new Vector3(0, .85f, -.55f), core.transform);
                Face(p.To[0], link + "A"); Face(p.To[1], link + "B");
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
                cars[i] = view;
                view.SetLabel(Label((i + 1).ToString(), new Vector3(0, .47f, 0), root.transform));
            }
            Snap(state);
        }
        private void Edge(GateEdge edge, int index)
        {
            var gate = level.GateAt(edge, index);
            float r = edge == GateEdge.Top ? -.65f : edge == GateEdge.Bottom ? level.Rows - .35f : index;
            float c = edge == GateEdge.Left ? -.65f : edge == GateEdge.Right ? level.Cols - .35f : index;
            bool horizontal = edge == GateEdge.Top || edge == GateEdge.Bottom;
            Box(gate.HasValue ? "Cổng " + gate.Value.Color : "Viền", Cell(r, c, .14f), horizontal ? new Vector3(.97f, .28f, .22f) : new Vector3(.22f, .28f, .97f), gate.HasValue ? palette.Car(gate.Value.Color) : palette.Wall);
        }
        private void Face(PortalFace p, int link) => Face(p, link.ToString());
        private void Face(PortalFace p, string link)
        {
            var offset = p.Side == Side.N ? new Vector3(0, 0, .45f) : p.Side == Side.S ? new Vector3(0, 0, -.45f) : p.Side == Side.W ? Vector3.left * .45f : Vector3.right * .45f;
            bool horizontal = p.Side == Side.N || p.Side == Side.S;
            var pos = Cell(p.R, p.C, .18f) + offset;
            Box("Portal " + link, pos, horizontal ? new Vector3(.82f, .3f, .12f) : new Vector3(.12f, .3f, .82f), palette.Portal); Label(link, pos + Vector3.up * .23f);
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
            for (int i = 0; i < lights.Length; i++) Colorize(lights[i], LightRule.IsRed(state, level.Lights[i]) ? palette.Closed : palette.Open);
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
            // Camera phải xóa nền cả màn: nếu chỉ vẽ vùng bãi, chữ HUD ngoài vùng đó có thể lưu vệt của frame cũ.
            // viewport chỉ dùng để chừa chỗ cho HUD, không giới hạn vùng camera vẽ.
            camera.orthographic = true; camera.rect = new Rect(0, 0, 1, 1);
            camera.backgroundColor = palette.Background; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.rotation = Quaternion.Euler(config.TiltDegrees, 0, 0);
            float aspect = camera.pixelRect.width / Mathf.Max(1, camera.pixelRect.height);
            float halfWidth = level.Cols * .5f + .9f + config.Padding;
            float halfHeight = (level.Rows * .5f + .9f + config.Padding) * Mathf.Sin(config.TiltDegrees * Mathf.Deg2Rad) + .7f;
            camera.orthographicSize = Mathf.Max(halfHeight / Mathf.Max(.001f, viewport.height),
                halfWidth / (aspect * Mathf.Max(.001f, viewport.width)));
            // Dịch tâm nhìn để bãi vẫn nằm giữa khoảng chơi, kể cả safe area không đối xứng.
            Vector2 center = viewport.center;
            float offsetX = (center.x * 2 - 1) * camera.orthographicSize * aspect;
            float offsetY = (center.y * 2 - 1) * camera.orthographicSize;
            camera.transform.position = transform.position - camera.transform.forward * 25
                - camera.transform.right * offsetX - camera.transform.up * offsetY;
        }
        private static void Release(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        private void OnDestroy() { if (labelFont != null) Release(labelFont); }
    }
}
