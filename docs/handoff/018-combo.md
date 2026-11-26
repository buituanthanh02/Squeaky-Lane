# 018 — Combo bằng hình và kỷ lục     Cỡ: M     Trạng thái: Chờ người làm chơi thử
Đang cầm: Claude        AI tham gia: Claude, Codex (018-a vẽ hình)
Liên quan: 017 (nhánh `018-combo` mở từ `017-cleanup`)

## Mục tiêu
Người chơi thấy ngay mình đang nối combo, cuối lượt biết combo cao nhất và kỷ lục; phá kỷ lục thì được ăn mừng rõ.

## Phạm vi
Làm: hình "Combo N"; kỷ lục theo màn; khối combo ở bảng Thắng/Hết giờ; hiệu ứng kỷ lục mới (whoosh, đập, rung thẻ, sao, chớp) và hai tiếng mới. Không làm: đổi luật, sao, Core.
Người làm chốt: kỷ lục riêng từng màn; cửa sổ combo 1,6 → 3 giây; chưa phá kỷ lục thì hiện combo + dòng kỷ lục.

## Đã làm
- Code: `ComboFeedback` (popup, combo cao nhất, ghi kỷ lục), `ComboSummary` (khối kết quả), `ComboLabel` (ghép hình số), tool `ComboUiTools`; `ProgressStore.BestCombo/RecordCombo`; bỏ chữ "Liên tiếp xN" trong `FeelEffects`.
- Config: `FeelConfig.comboWindowSeconds` 1,6 → 3 và 12 field thời gian/biên độ mới; `UiTheme` thêm hình và cỡ; `SfxCatalog` thêm RecordWhoosh (0,55), RecordSlam (0,75).
- Scene `Play.unity`: `…/Playing/018 Combo` (mới, giữa đỉnh, y −440) · `ComboFeedback`; `Won` và `Lost` / `013 Card` · thêm `ComboSummary`; `GameRoot.comboFeedback` · trống → `018 Combo`.

## Test và kết quả
- EditMode **573 · 572 · 0 · 1** (thêm 4 ca `Combo018Tests`).
- Play Simulator màn 20: popup, thắng lần đầu (kỷ lục mới), thắng lại (dòng kỷ lục), hết giờ; Console không lỗi. Kỷ lục thử đã xóa khỏi PlayerPrefs.

## Chưa kiểm
- Mới xem ảnh từng khung, chưa nghe hai tiếng mới và chưa xem chuyển động liền mạch; chưa thử trên máy thật.
- Popup chụp bằng cách gọi thẳng `RegisterExit`; chuỗi thật đạt Combo 5 ở màn 20 khi lệnh xếp hàng liên tục, chưa đo độ khó khi chạm tay.

## Bạn cần hiểu
- Combo đếm ở `ActionAnimator.ExitCombo` (theo đồng hồ hoạt hình, nên tạm dừng không làm đứt chuỗi); `ComboFeedback` chỉ nghe và hiển thị.
- Thân hộp thoại dùng `VerticalLayoutGroup`: khối combo là một hàng có `LayoutElement`, thẻ cao thêm đúng hàng đó.
- Kỷ lục lưu `PlayerPrefs` khóa `squeaky-lane.combo.<id màn>`.

## Trao đổi
2026-11-26 16:29 · Claude → Người làm · [BÁO]: xong trên `018-combo`, chờ chơi thử.
