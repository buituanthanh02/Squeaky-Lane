# 017 — Dọn dự án trước khi build     Cỡ: M     Trạng thái: Chờ người làm chơi thử
Đang cầm: Claude        AI tham gia: Claude
Liên quan: 016 (nhánh `017-cleanup` mở từ `016-toy-world` tại c596a01)

## Mục tiêu
Người làm đọc và giải thích được dự án dễ hơn trước khi build APK; cách chơi không đổi.

## Phạm vi
Làm: xóa tool Editor dùng một lần; gỡ HUD gỡ lỗi và Asset Lab; gộp code trùng. Không làm: đổi luật, Core, hình, số chỉnh; không xóa asset không dùng (chờ duyệt).
Người làm chọn: giữ tool đang dùng để chỉnh hình; gỡ cả DebugHud và Asset Lab.

## Đã làm
- Xóa 19 file Editor: AssetLab, BatchRunner, BrightGround, BubbleSprite, CarVfx, CoreFeel, FoamSprite, KenneyCars, KenneyTruckMesh, LotMechanics, SceneTheme, Scenery, UiPackImport, UiPackSkin, UiPreview, UiSkin, UiThemeAssets, WashGate, WashVfx. PlayTools chỉ còn Sync Levels (bỏ Setup Play, Measure Search).
- Xóa `DebugHud`, `AssetLabView`, `AssetLabConfig` (+ asset, scene `AssetLab.unity`, material sàn).
- Gộp: `EditorAssets` (thay 5 bản EnsureFolder, 3 bản RequireAsset, 2 bản RequireFreePath), `UnityObjects.Release` (thay 4 bản).
- `GameBootstrap`: một đường UI; thêm CreateSearch, SetBoardVisible, RefreshControls, SetStuck; tách Update thành CheckLevelEnd / PollSearch / PlayCountdown. `BoardViewport` chuyển sang `NavigationView`.
- Prefab `GameRoot` · component `DebugHud` · có → gỡ (qua PrefabUtility). Không đổi `Play.unity`.
- Test bỏ theo phần bị gỡ: 4 test tool xe Kenney (7 ca), 1 test Asset Lab; test nút bàn xoay bỏ phần HUD gỡ lỗi.

## Test và kết quả
- Biên dịch sạch; EditMode **569 · 568 · 0 · 1** (trước: 577; bớt đúng 8 ca đã bỏ).
- Không có script thiếu trong scene/prefab. Play Simulator: chọn màn, gợi ý, hoàn tác, thắng, màn kế, tạm dừng, tiếp tục, về chọn màn, kẹt xe (viền đỏ), chơi lại; Console không lỗi/cảnh báo.
- Chạy thử menu Toy Mechanics, City Setup, Toy Town Props: không lỗi; chỉ City Setup ghi thêm field mặc định vào UiTheme (đã hoàn lại).

## Chưa kiểm / phát hiện ngoài phạm vi
- Chưa chạy trên máy thật; chưa chạy Toy Look Assets, Main Menu, Stuck Edge sau khi gộp helper (mới kiểm bằng biên dịch).
- Khoảng 25 MB asset không còn được game dùng (phần lớn `Art/Mechanics/Kenney`, `Art/UI/Library`) và file rác chưa theo dõi ở `Art/UI/City`, `Prefabs/Environment`: không vào APK qua scene, chờ người làm duyệt xóa.

## Bạn cần hiểu
- Code trong `Editor/` không vào APK; xóa tool chỉ làm dự án gọn, không làm APK nhỏ hơn.
- Xóa tool không mất asset đã tạo (asset đã nằm trong repo); chỉ mất khả năng dựng lại bằng menu.
- `GameBootstrap` giờ bắt buộc có `NavigationView`; thiếu thì báo lỗi ngay ở `Awake`.

## Trao đổi
2026-11-26 15:45 · Claude → Người làm · [BÁO]: dọn xong trên `017-cleanup`; chờ chơi thử.
