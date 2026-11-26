# 020 — Chữ khen khi xe sạch ra cổng     Cỡ: S     Trạng thái: Chờ người làm chơi thử
Đang cầm: Claude        AI tham gia: Claude
Liên quan: 012 (chữ khen gốc); nhánh `020-praise-text` mở từ `019-tutorial`

## Mục tiêu
Xe được rửa và ra cổng thì người chơi thấy rõ lời khen ("Sạch bóng!", "Bóng loáng!", "Thơm tho!"). Người làm báo chữ cũ mờ nhạt.

## Đã làm
- Bỏ `TextMesh` trắng mảnh nằm phẳng trên mặt bãi. Chữ mới vẽ trên HUD (`PraisePopups`, class thường do `FeelEffects` sở hữu): Baloo 2 ExtraBold, viền indigo `HudOutline`, chuyển màu trắng → vàng, 3 sao lấp lánh, bật quá cỡ rồi bay lên, nghiêng so le; không chồng nhau, không ra ngoài màn, không chặn chạm.
- Config: `FeelConfig.popupSeconds` 1,1 → 1,35; bỏ `popupTextSize`, `popupRise` (không còn dùng). `UiTheme` nhóm 020: praiseFontSize 92, praiseTop/Bottom, praiseRise 130, praiseTilt 7, praiseSparkSize 72, praiseSparkColor. Nhịp bật dùng chung `comboPopSeconds`, `comboPunchScale`.
- Code nối: `NavigationView.EffectsLayer` (panel Playing), `GameBootstrap` gọi `FeelEffects.SetTextLayer`. Không đổi scene/prefab/Core.

## Test và kết quả
- EditMode 594 chạy, 593 đạt, 0 lỗi, 1 bỏ qua (thêm 4 ca `Praise020Tests`).
- Play trong Editor: màn 1, 2, 5, mỗi màn một xe ra cổng, chụp lúc bật và lúc bay (`Logs/020-praise/sheet-praise.jpg`); Console không lỗi.

## Chưa kiểm
- Chuyển động liền mạch (mới xem ảnh từng khung), nhiều xe ra liền nhau trong Play (test EditMode đã kiểm không chồng), điện thoại thật.

## Bạn cần hiểu
- Chữ vẽ trên Canvas: vị trí lấy bằng `Camera.WorldToScreenPoint` một lần lúc xe qua cổng, nên không bị góc camera làm mỏng chữ.
- Bốn ô chữ được tạo một lần rồi dùng lại, không sinh object mỗi lần xe ra.

## Trao đổi
2026-11-26 17:33 · Claude → Người làm · [BÁO]: xong trên `020-praise-text`, chờ chơi thử.
