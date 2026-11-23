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
        private CarGesture gesture;
        private EventSystem pointerOwner;
        private PointerEventData uiPointer;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>(16);
        public bool Accepting { get; set; }
        public event Action<SolverMove> Command;
        public event Action CarSelected;
        public void Initialize(Camera camera, BoardView view, GameConfig settings)
        { boardCamera = camera; board = view; config = settings; CancelGesture(); }
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
            if (!Accepting) { CancelGesture(); return; }
            // Một con trỏ chính: chuột được Unity mô phỏng từ touch, không xử lý lại Input.touches.
            if (Input.GetMouseButtonDown(0))
            {
                CancelGesture(); if (OverUI(Input.mousePosition)) return;
                Vector2 down = Input.mousePosition; if (!boardCamera.pixelRect.Contains(down)) return;
                if (Physics.Raycast(boardCamera.ScreenPointToRay(down), out var hit, 100))
                {
                    selected = hit.collider.GetComponentInParent<CarView>();
                    if (selected != null)
                    {
                        var positive = selected.PositiveDirection;
                        // Ghi nửa xe lúc nhấn: xe có thể đang chuyển động trước lúc nhả tay.
                        int tapSign = Vector3.Dot(hit.point - selected.transform.position, positive) >= 0 ? 1 : -1;
                        var screenAxis = (Vector2)(boardCamera.WorldToScreenPoint(selected.transform.position + positive) - boardCamera.WorldToScreenPoint(selected.transform.position));
                        gesture.Begin(down, screenAxis, tapSign, Mathf.Min(Screen.width, Screen.height) * config.SwipeShortSideRatio);
                        CarSelected?.Invoke();
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
            if (selected != null && (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
            {
                if (OverUI(Input.mousePosition) || !selected.gameObject.activeInHierarchy) { CancelGesture(); return; }
                bool released = Input.GetMouseButtonUp(0);
                bool fire = released ? gesture.TryRelease(Input.mousePosition, out int sign) : gesture.TryDrag(Input.mousePosition, out sign);
                if (fire)
                {
                    int index = selected.Index;
                    // Xóa lựa chọn trước callback: giữ tay/nhả tay sau đó không phát thêm lệnh cho cùng gesture.
                    CancelGesture(); Command?.Invoke(new SolverMove(SolverMoveKind.MoveCar, index, sign));
                }
                else if (released) CancelGesture();
            }
        }
        public static float TouchDiameterPixels(float millimeters)
        {
            float dpi = Screen.dpi;
            // DPI 0 hoặc vô lý: dự phòng theo cạnh ngắn, cần xác nhận kích thước thật trên điện thoại.
            return dpi >= 72 && dpi <= 640 ? millimeters * dpi / 25.4f : Mathf.Min(Screen.width, Screen.height) * millimeters / 65f;
        }
        private void CancelGesture() { selected = null; gesture.Cancel(); }
        private void OnDisable() { CancelGesture(); }
    }

    // Bộ nhận dạng nhỏ cùng file: không đọc Input/scene, để test được thời điểm ra lệnh và chống lặp.
    public struct CarGesture
    {
        private Vector2 down, axis;
        private float thresholdSquared;
        private int tapSign;
        private bool active, dragged;
        public void Begin(Vector2 position, Vector2 screenAxis, int tapDirection, float thresholdPixels)
        {
            if (thresholdPixels <= 0 || float.IsNaN(thresholdPixels) || float.IsInfinity(thresholdPixels)) throw new ArgumentOutOfRangeException(nameof(thresholdPixels));
            if (tapDirection != -1 && tapDirection != 1) throw new ArgumentOutOfRangeException(nameof(tapDirection));
            down = position; axis = screenAxis.normalized; thresholdSquared = thresholdPixels * thresholdPixels;
            tapSign = tapDirection; active = axis.sqrMagnitude > 0; dragged = false;
        }
        public bool TryDrag(Vector2 position, out int sign)
        {
            sign = 0; if (!active) return false;
            Vector2 delta = position - down;
            if (delta.sqrMagnitude < thresholdSquared) return false;
            dragged = true;
            float along = Vector2.Dot(delta.normalized, axis);
            if (Mathf.Abs(along) < .65f) return false; // Giữ bộ lọc vuốt ngang thân của bản trước.
            sign = along >= 0 ? 1 : -1; active = false; return true;
        }
        public bool TryRelease(Vector2 position, out int sign)
        {
            if (TryDrag(position, out sign)) return true; // Bắt cả flick nhanh chỉ có mẫu cuối ở frame nhả.
            if (!active) return false;
            bool tap = !dragged; sign = tap ? tapSign : 0; Cancel(); return tap;
        }
        public void Cancel() { active = false; dragged = false; }
    }
}
