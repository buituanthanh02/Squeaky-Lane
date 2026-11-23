using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SqueakyLane.Core.Solver;
using SqueakyLane.Game;

namespace SqueakyLane.Presentation
{
    public sealed class InputController : MonoBehaviour
    {
        private Camera boardCamera;
        private GameConfig config;
        private BoardView board;
        private CarView selected;
        private Vector2 down;
        private Vector3 positive;
        private int tapSign;
        private EventSystem pointerOwner;
        private PointerEventData uiPointer;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(16);
        public bool Accepting { get; set; }
        public event Action<SolverMove> Command;
        public void Initialize(Camera camera, BoardView view, GameConfig settings)
        { boardCamera = camera; board = view; config = settings; selected = null; }
        private bool OverUI(Vector2 position)
        {
            var owner = EventSystem.current; if (owner == null) return false;
            if (pointerOwner != owner) { pointerOwner = owner; uiPointer = new PointerEventData(owner); }
            // Raycast ngay tại con trỏ tránh phụ thuộc Update của EventSystem đã chạy trước hay chưa.
            uiPointer.Reset(); uiPointer.position = position; uiHits.Clear(); owner.RaycastAll(uiPointer, uiHits);
            return uiHits.Count > 0;
        }
        private void Update()
        {
            if (!Accepting) { selected = null; return; }
            // Một con trỏ chính: chuột được Unity mô phỏng từ touch, không xử lý lại Input.touches.
            if (Input.GetMouseButtonDown(0))
            {
                selected = null; if (OverUI(Input.mousePosition)) return;
                down = Input.mousePosition; if (!boardCamera.pixelRect.Contains(down)) return;
                if (Physics.Raycast(boardCamera.ScreenPointToRay(down), out var hit, 100))
                {
                    selected = hit.collider.GetComponentInParent<CarView>();
                    if (selected != null)
                    {
                        positive = selected.PositiveDirection;
                        // Ghi nửa xe lúc nhấn: xe có thể đang chuyển động trước lúc nhả tay.
                        tapSign = Vector3.Dot(hit.point - selected.transform.position, positive) >= 0 ? 1 : -1;
                    }
                }
                if (selected == null)
                {
                    // Trụ là nút riêng; không đoán luật từ collider của khối trang trí.
                    for (int i = 0; i < board.Level.Pylons.Count; i++)
                    {
                        if (board.Level.Pylons[i].Fixed) continue;
                        var point = (Vector2)boardCamera.WorldToScreenPoint(board.PylonPosition(i));
                        if (Vector2.Distance(point, down) <= TouchDiameterPixels(config.TableTouchMillimeters) * .5f)
                        { Command?.Invoke(new SolverMove(SolverMoveKind.RotatePylon, i)); break; }
                    }
                }
            }
            if (Input.GetMouseButtonUp(0) && selected != null)
            {
                var car = selected; selected = null;
                if (OverUI(Input.mousePosition) || !car.gameObject.activeInHierarchy) return;
                Vector2 delta = (Vector2)Input.mousePosition - down;
                int sign;
                if (delta.magnitude >= Mathf.Min(Screen.width, Screen.height) * config.SwipeShortSideRatio)
                {
                    var screenAxis = (Vector2)(boardCamera.WorldToScreenPoint(car.transform.position + positive) - boardCamera.WorldToScreenPoint(car.transform.position));
                    float along = Vector2.Dot(delta.normalized, screenAxis.normalized);
                    if (Mathf.Abs(along) < .65f) return; // Vuốt chủ yếu ngang thân không được thành lệnh dọc thân.
                    sign = along >= 0 ? 1 : -1;
                }
                else sign = tapSign;
                Command?.Invoke(new SolverMove(SolverMoveKind.MoveCar, car.Index, sign));
            }
        }
        public static float TouchDiameterPixels(float millimeters)
        {
            float dpi = Screen.dpi;
            // DPI 0 hoặc vô lý: dự phòng theo cạnh ngắn, cần xác nhận kích thước thật trên điện thoại.
            return dpi >= 72 && dpi <= 640 ? millimeters * dpi / 25.4f : Mathf.Min(Screen.width, Screen.height) * millimeters / 65f;
        }
        private void OnDisable() { selected = null; }
    }
}
