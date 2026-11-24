using System;
using UnityEngine;
using UnityEngine.UI;
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
        private GameSession session;
        private BoardView board;
        private Camera camera;
        private GameConfig config;
        private bool accepting;
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
            int count = session.Level.Tables.Count;
            buttons = new Button[count];
            rects = new RectTransform[count];
            cars = new int[count];
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
            // Nhãn ASCII dùng font có sẵn của Unity, không tạo font OS cho HUD đang ẩn.
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < count; i++)
            {
                int table = i;
                var go = new GameObject("XOAY", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(root.transform, false);
                go.GetComponent<Image>().color = new Color(.22f, .27f, .39f, .96f);
                var button = go.GetComponent<Button>();
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
                var label = new GameObject("Nhãn", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(go.transform, false);
                var text = label.GetComponent<Text>();
                text.font = font;
                text.fontSize = 22;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.text = "XOAY";
                text.raycastTarget = false;
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
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

                buttons[t].interactable = accepting && cars[t] >= 0;
            }
        }

        public void SetAccepting(bool value)
        {
            accepting = value;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = accepting && cars[i] >= 0;
            }
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

            for (int i = 0; i < rects.Length; i++)
            {
                var table = session.Level.Tables[i];
                rects[i].position = camera.WorldToScreenPoint(board.transform.TransformPoint(board.Cell(table.R + .5f, table.C + .5f, .8f)));
                rects[i].sizeDelta = Vector2.one * InputController.TouchDiameterPixels(config.TableTouchMillimeters) / canvas.scaleFactor;
            }
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
        }
    }
}
