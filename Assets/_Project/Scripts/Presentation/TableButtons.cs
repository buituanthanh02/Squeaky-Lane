using System;
using UnityEngine;
using UnityEngine.UI;
using SqueakyLane.Core.Model;
using SqueakyLane.Core.Rules;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    // UI chơi thật, độc lập với HUD gỡ lỗi. Bootstrap sở hữu vòng đời của lớp này.
    public sealed class TableButtons : IDisposable
    {
        private Canvas canvas;
        private GameObject root;
        private Button[] buttons = Array.Empty<Button>();
        private RectTransform[] rects = Array.Empty<RectTransform>();
        private int[] cars = Array.Empty<int>();
        private BoxCollider[] carFootprints = Array.Empty<BoxCollider>();
        private GameSession session;
        private BoardView board;
        private Camera camera;
        private GameConfig config;
        private bool accepting;
        private float badgeRatio;
        private float laneOffset;
        private float carGap;
        public event Action<SolverMove> Command;
        public void Initialize(Canvas target)
        {
            canvas = target;
        }

        public void Bind(GameSession game, BoardView view, Camera boardCamera, GameConfig settings)
        {
            ReleaseRoot();
            session = game;
            board = view;
            camera = boardCamera;
            config = settings;
            var theme = board.Theme;
            badgeRatio = theme != null ? theme.TableBadgeRatio : UiTheme.DefaultTableBadgeRatio;
            laneOffset = theme != null ? theme.TableBadgeLaneOffset : UiTheme.DefaultTableBadgeLaneOffset;
            carGap = theme != null ? theme.TableBadgeCarGap : UiTheme.DefaultTableBadgeCarGap;
            int count = session.Level.Tables.Count;
            buttons = new Button[count];
            rects = new RectTransform[count];
            cars = new int[count];
            carFootprints = Array.Empty<BoxCollider>();
            if (count == 0)
            {
                return;
            }

            root = new GameObject("Nút bàn xoay", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            root.transform.SetAsFirstSibling(); // Dialog/fade luôn nằm trên nút chơi.
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
            carFootprints = new BoxCollider[session.State.CarCount];
            for (int i = 0; i < carFootprints.Length; i++)
            {
                carFootprints[i] = board.Car(i).GetComponent<BoxCollider>();
            }

            var sprite = theme != null ? theme.TableRotateBadge : null;
            for (int i = 0; i < count; i++)
            {
                int table = i;
                var go = new GameObject("Vùng chạm bàn xoay", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(root.transform, false);
                go.GetComponent<Image>().color = Color.clear;
                var button = go.GetComponent<Button>();
                var colors = button.colors;
                float pressedTint = theme != null ? theme.PressedTint : colors.pressedColor.r;
                colors.pressedColor = new Color(pressedTint, pressedTint, pressedTint);
                button.colors = colors;
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.None;
                button.navigation = navigation;
                button.onClick.AddListener(() =>
                {
                    if (accepting && cars[table] >= 0)
                    {
                        Command?.Invoke(new SolverMove(SolverMoveKind.RotateTable, cars[table]));
                    }
                });
                buttons[i] = button;
                rects[i] = go.GetComponent<RectTransform>();
                rects[i].anchorMin = rects[i].anchorMax = Vector2.zero;
                rects[i].pivot = new Vector2(.5f, .5f);
                button.targetGraphic = CreateBadge(rects[i], sprite);
            }

            Refresh();
        }

        public void Refresh()
        {
            for (int t = 0; t < buttons.Length; t++)
            {
                cars[t] = -1;
                var table = session.Level.Tables[t];
                for (int i = 0; i < session.State.CarCount; i++)
                {
                    var under = TableRule.TableUnder(session.Level, session.State.CarAt(i), session.Level.Cars[i].Len);
                    if (under.HasValue && under.Value.R == table.R && under.Value.C == table.C)
                    {
                        cars[t] = i;
                        break;
                    }
                }

                buttons[t].gameObject.SetActive(cars[t] >= 0);
            }

            SetAccepting(accepting);
        }

        public void SetAccepting(bool value)
        {
            accepting = value;
            for (int i = 0; i < buttons.Length; i++)
            {
                // Khi dừng Play hoặc đóng app, Canvas có thể bị hủy trước GameRoot (GameBootstrap.OnDisable vẫn gọi vào đây).
                if (buttons[i] != null)
                {
                    buttons[i].interactable = accepting && cars[i] >= 0;
                }
            }
        }

        // Vùng chạm (pixel màn hình) của nút bàn xoay đang hiện; dùng để chiếu sáng và đặt bàn tay.
        public bool TryGetButtonRect(int table, out Rect rect)
        {
            rect = default;
            if (table < 0 || table >= rects.Length || cars[table] < 0 || root == null || !root.activeInHierarchy)
            {
                return false;
            }

            Vector2 size = rects[table].sizeDelta * canvas.scaleFactor;
            rect = new Rect((Vector2)rects[table].position - size * .5f, size);
            return true;
        }

        public void SetVisible(bool value)
        {
            if (root != null)
            {
                root.SetActive(value);
            }
        }

        public void UpdatePositions()
        {
            if (root == null || !root.activeInHierarchy)
            {
                return;
            }

            float touchPixels = InputController.TouchDiameterPixels(config.TableTouchMillimeters);
            for (int i = 0; i < rects.Length; i++)
            {
                int carIndex = cars[i];
                if (carIndex < 0)
                {
                    continue;
                }

                var table = session.Level.Tables[i];
                var car = session.State.CarAt(carIndex);
                float row = table.R + .5f;
                float column = table.C + .5f;
                var center = Project(row, column);
                if (car.Axis == Axis.Horizontal)
                {
                    row += car.R == table.R ? laneOffset : -laneOffset;
                }
                else
                {
                    column += car.C == table.C ? laneOffset : -laneOffset;
                }

                var position = Project(row, column);
                var awayFromCar = (position - center).normalized;
                var footprint = carFootprints[carIndex];
                if (footprint != null && footprint.gameObject.activeInHierarchy)
                {
                    // Góc camera có thể chiếu mái xe qua nửa bàn trống. Đẩy huy hiệu vừa đủ ra khỏi hình chiếu của xe.
                    float clearance = touchPixels * (badgeRatio * .5f + carGap);
                    float edge = ProjectedEdge(footprint, awayFromCar);
                    position += awayFromCar * Mathf.Max(0, edge + clearance - Vector2.Dot(position, awayFromCar));
                }

                rects[i].position = position;
                rects[i].sizeDelta = Vector2.one * touchPixels / canvas.scaleFactor;
            }
        }

        private Vector2 Project(float row, float column)
        {
            return camera.WorldToScreenPoint(board.transform.TransformPoint(board.Cell(row, column, LotSurfaceView.Top)));
        }

        private float ProjectedEdge(BoxCollider footprint, Vector2 direction)
        {
            var transform = footprint.transform;
            var half = footprint.size * .5f;
            var center = camera.WorldToScreenPoint(transform.TransformPoint(footprint.center));
            // Camera trực giao: cộng hình chiếu của ba bán trục hộp để tìm mép xe theo hướng đặt nút.
            var x = camera.WorldToScreenPoint(transform.TransformPoint(footprint.center + Vector3.right * half.x)) - center;
            var y = camera.WorldToScreenPoint(transform.TransformPoint(footprint.center + Vector3.up * half.y)) - center;
            var z = camera.WorldToScreenPoint(transform.TransformPoint(footprint.center + Vector3.forward * half.z)) - center;
            return Vector2.Dot(center, direction) + Mathf.Abs(Vector2.Dot(x, direction))
                + Mathf.Abs(Vector2.Dot(y, direction)) + Mathf.Abs(Vector2.Dot(z, direction));
        }

        private Graphic CreateBadge(RectTransform parent, Sprite sprite)
        {
            var node = new GameObject("Huy hiệu xoay", typeof(RectTransform));
            var rect = (RectTransform)node.transform;
            rect.SetParent(parent, false);
            Vector2 inset = Vector2.one * ((1 - badgeRatio) * .5f);
            rect.anchorMin = inset;
            rect.anchorMax = Vector2.one - inset;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            if (sprite != null)
            {
                var image = node.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return image;
            }

            var glyph = node.AddComponent<Text>();
            glyph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            glyph.text = "↻";
            glyph.alignment = TextAnchor.MiddleCenter;
            glyph.resizeTextForBestFit = true;
            glyph.raycastTarget = false;
            return glyph;
        }

        private void ReleaseRoot()
        {
            if (root != null)
            {
                root.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            root = null;
        }

        public void Dispose()
        {
            ReleaseRoot();
            buttons = Array.Empty<Button>();
            rects = Array.Empty<RectTransform>();
            cars = Array.Empty<int>();
            carFootprints = Array.Empty<BoxCollider>();
        }
    }
}
