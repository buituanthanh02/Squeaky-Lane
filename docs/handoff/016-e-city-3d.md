# 016-e · Dựng khu phố cartoon 3D theo concept, chụp nhiều góc để người làm duyệt     Cỡ: M–L     Trạng thái: Chờ người làm duyệt ảnh
Đang cầm: Claude        AI tham gia: Claude (giao, review), Codex (dựng, chụp, tự sửa)
Liên quan: 016-toy-world (mục "Giao Claude · Dựng khu phố cartoon bằng 3D": bố cục, khoảng trống, nét viền — đọc và làm theo). Concept: `docs/handoff/016-ref/city-3d-topdown.png`. Nhánh `016-toy-world`.

## Mục tiêu (người chơi)
Mở màn thấy bãi rửa xe nằm giữa khu phố hoạt hình **giống concept**: 3 cửa hàng mái hiên phía trên, công viên nhỏ phía dưới, đường bo góc có vỉa hè và nét viền đậm. Người làm **xem ảnh nhiều góc rồi mới quyết cho vào game**.

## Phạm vi — Làm
- File mới `Assets/_Project/Scripts/Editor/CityDioramaTools.cs`, menu `Tools/Squeaky Lane/016 Toy/City Diorama` (idempotent, không Random). Mesh/material ở `Art/Environment/City3D/`, prefab theo cụm (3 cửa hàng, công viên, chậu/cây hai bên) ở `Prefabs/Environment/City3D/`. Đường, vỉa hè, khay bãi sinh theo **số cột/hàng của màn** (hàm tĩnh nhận LevelDef để sau này đưa vào runtime).
- Hình khối thật (bevel 0,08–0,15 ô), mái hiên sọc, cửa/cửa sổ, tán cây tròn nhiều khối, ghế, bồn hoa, lối dạo cong; **nét viền** than `#24262B` 0,02–0,035 ô (mesh viền hoặc `Toy Lit Outline`), bóng tiếp đất mềm giả lệch xuống-phải. Màu theo concept.
- **Khớp game hiện tại:** đọc `BoardView.FitCamera`, `SceneThemeView`, `UiTheme`: camera trực giao 58°, `streetInner` 0,8, `streetWidth` 0,95, `streetVisible` 0,5 (tim đường xe chạy giữ nguyên), đường chạy ra khỏi khung; chừa thảm hầm rửa và lối qua cổng; vật phía dưới bãi cao ≤ 0,9.
- **Ảnh xem trước dựng bãi thật:** trong scene preview tạm dùng `BoardView.Build` (không gán sceneTheme, để không hiện phố cũ) với `level_03` và `level_23`, catalog xe/cơ chế hiện tại, rồi đặt khu phố mới quanh. Lưu vào `Logs/016-city/`:
  `01-game-level03.png`, `02-game-level23.png` (dọc 1284×2778, đúng khung camera game, phủ mờ vùng HUD trên và booster dưới) · `03-topdown.png` (90°, đặt cạnh concept để so) · `04-hero.png` (phối cảnh 3/4) · `05-shops.png`, `06-park.png` (cận cảnh) · `07-lot-variants.png` (mặt bãi tối hiện tại cạnh xám xanh kiểu concept, cùng xe).
- Bạn được lái Unity qua MCP: biên dịch, chạy menu, **mở ảnh tự so với concept và sửa, tối đa 3 vòng**.

## Không làm
Không nối vào game: không sửa `SceneThemeView`/`UiTheme`/catalog/Play.unity/Presentation/Game/Core; không Play, không lưu scene; không package/settings; không commit; không dùng ảnh 2D dán nền.

## Tiêu chí xong · V3
Biên dịch sạch, Console không lỗi mới; menu chạy 2 lần GUID giữ nguyên; cả khu phố ≤ 12 draw call kể cả viền (gộp mesh theo material), ≤ 40k tam giác, 0 collider; ảnh game đọc rõ bãi, xe, cổng và đọc ra khu phố như concept. Ghi vào phiếu số draw/tam giác và chỗ còn khác concept.

## Cắt nếu trễ
`07` biến thể → `04` hero → bàn ghế/ô che → cụm chậu hai bên.

--- AI làm điền khi xong ---

- **Claude tự dựng** (người làm yêu cầu 26/11, lượt Codex đã dừng, không để lại file).
- `CityDioramaTools.cs` (menu `016 Toy → City Diorama`): asset `Art/Environment/City3D`, 4 prefab `Prefabs/Environment/City3D`, scene xem thử `Scenes/CityPreview.unity` (không vào Build Settings), ảnh `Logs/016-city/00…07`. Biên dịch và chạy menu qua MCP: Console 0 lỗi. Chưa chạy Test, chưa nối vào game.
- 9 draw theo thiết kế (nền 1 + 4 cụm × 2), chưa đo trong game. Khoảng 29,9k tam giác khối + 12,4k nền (hơn mốc 40k một chút).
- Còn khác concept: cửa hàng thấp/nhỏ hơn; sọc mái hiên cùng tông mái nên kém rõ; cụm hai bên gần như bị cắt ở khung game; bàn ghế, ghế dài đơn giản hơn.
- Khi nối vào game: thay `SceneThemeView` nền/phố/props bằng mesh sinh theo màn; kiểm màn 4 cột (cụm bị thu nhỏ), hầm rửa trên/dưới (cụm lùi 2,3 ô).

## Trao đổi
2026-11-26 11:51 · Claude → Codex · [BÀN GIAO]: giao 016-e; Claude không lái Editor trong lúc bạn làm.
2026-11-26 11:59 · Claude → Người làm · [HỎI]: duyệt ảnh `Logs/016-city`, chọn màu bãi (tối / xám xanh / tím) trước khi nối vào game.
