# 019 — Hướng dẫn bằng bãi tập     Cỡ: L     Trạng thái: Chờ người làm chơi thử
Đang cầm: Claude        AI tham gia: Claude
Liên quan: 018 (nhánh `019-tutorial` mở từ `018-combo`)

## Mục tiêu
Người chơi mới được dạy từng thao tác và cơ chế bằng vùng sáng, bàn tay và một dòng chữ, trong bãi tập nhỏ, trước màn đầu tiên cần tới cơ chế đó.

## Phạm vi
Làm: bãi tập, lớp tối khoét lỗ, bàn tay chạm/kéo, Bỏ qua, Xem lại hướng dẫn, Gợi ý chỉ bằng bàn tay, 3 màn đầu không tính giờ.
Không làm: đổi luật/Core/Navigator; bài cho cọc, barie, chốt (chưa màn nào trong danh sách dùng).

Người làm chốt (26/11):
- Cơ chế mới thì vào bãi tập, giữ màn thật không bị biến thành màn hướng dẫn; dạy khi gặp lần đầu.
- Bàn tay lấy từ `_Project.rar` của Sand Puzzle Clone (có cả bấm và kéo).
- Duyệt: nút Bỏ qua, Xem lại trong Cài đặt, màn 1–3 không tính giờ, bước chạm chỉ nhận chạm và bước vuốt chỉ nhận vuốt, chữ một dòng.

## Cách hoạt động
- Mỗi bài là một file `Resources/Tutorial/lesson_NN_<key>.json`: bãi (cùng định dạng level) + object `lesson` {key, order, teaches, steps}.
- Nạp màn: bài chưa học đầu tiên (theo `order`) có cơ chế màn đó cần sẽ chạy trước; xong hoặc Bỏ qua thì đánh dấu đã học (`PlayerPrefs` `squeaky-lane.tutorial.<key>`) và nạp lại đúng màn.
- Đổi thứ tự màn hoặc thêm màn không phải sửa bài: bài bám theo cơ chế, không bám số màn.
- Bước: `next` (chạm để tiếp tục), `move`, `rotate`, `pylon`, `undo`, `win` (tự chơi).
- Lớp tối chỉ chặn chạm ở bước `next` (trừ nút Tạm dừng). Bước thao tác không chặn raycast: lệnh sai bị loại ở `GameBootstrap.Apply`, vì `InputController` hủy cử chỉ khi ngón tay đè lên UI.

| order | Bài | Cơ chế kích hoạt | Hiện trước màn (thứ tự hiện tại) |
|---|---|---|---|
| 0 | basics: chạm, vuốt, cùng màu, thắng | mọi màn | 1 |
| 10 | mud: vòi xịt, vũng bùn, cổng nước | mud, water, spray, puddle | 2 |
| 20 | splash: bùn lây, viền đỏ, Hoàn tác, đồng hồ, Gợi ý | splash | 4 |
| 30 | paint | paint | 6 |
| 40 | table | table | 8 |
| 50 | light | light | 10 |
| 60 | truck | truck | 20 |
| 70 | portal | portal | 21 |
| 80 | pylon | pylon | 29 |

## Đã làm · tệp đổi
- Game: `TutorialData`, `TutorialRunner` (mới); `ConfigProvider.TutorialLessons`; `ProgressStore` cờ đã học.
- Presentation: `TutorialDirector`, `TutorialView`, `SpotlightGraphic` (mới); `GameBootstrap`, `NavigationView`, `TableButtons.TryGetButtonRect`, `InputController.LastCommandWasSwipe`, `UiTheme` (nhóm 019).
- Editor: `TutorialUiTools`, menu `019 UI/Build Tutorial`.
- Asset: `Art/UI/Library/finger2–7.png`, `ic_finger.png` (finger1 đã có); 9 file bài.
- Config: `GameConfig.tutorialLevelsUntimed` 0 → 3 (các field `maxTickSeconds`, `unlockAll`, `lowTimeRatio` được Unity ghi thêm với đúng giá trị mặc định đang dùng). `UiTheme` thêm field `tutorial*`.
- Scene `Play.unity` (qua menu):
  - `Canvas/019 Tutorial` (mới, trên UIRoot, dưới FadeOverlay).
  - `…/016 MenuSettings/016 Panel` · sizeDelta · (760, 520) → (760, 650).
  - `…/016 Panel/016 SoundToggle`, `016 HapticsToggle` · anchoredPosition.y · −55 → 30.
  - `…/016 Panel/019 ReplayTutorial` (mới).
  - `UIRoot` NavigationView · replayTutorialButton/Label · trống → nút mới. `GameRoot.tutorialView` · trống → `019 Tutorial`.
  - Chạy lại menu `016 UI/Build Main Menu` thì phải chạy lại menu 019.

## Test và kết quả
- EditMode 590 chạy, 589 đạt, 0 lỗi, 1 bỏ qua (bài đo thủ công). Có 16 ca `Tutorial019Tests`: chạy thật từng bài trên bãi của nó, thế kẹt ở bài bùn lây, bãi đầu không có ngõ cụt, mọi cơ chế trong `level_order` đều có bài.
- Play trong Editor (Simulator 1284×2778), điều khiển bằng gọi hàm qua MCP: xem ảnh cả 9 bãi; chạy trọn basics → màn 1 và splash → màn 4 (đồng hồ chạy); xoay bàn, xoay trụ, Bỏ qua, Xem lại, Gợi ý trên màn thật. Console không lỗi từ code game.
- Ảnh: `Logs/019-plan/*.png` (không commit).
- Sao và kỷ lục không bị tool/test đổi. Kỷ lục combo 3 của màn 2 xuất hiện khi người làm tự chơi trong lúc kiểm; giữ nguyên. Cờ bài tập đã xóa sau khi kiểm.

## Chưa kiểm
- Chạm/vuốt thật bằng ngón tay ở từng bước (kiểm bằng cách gọi lệnh và raycast).
- Chuyển động liền mạch của bàn tay, viền sáng (mới xem ảnh từng khung).
- Chạy trọn bài paint, light, truck, portal trong Play (test EditMode đã chạy trọn luật; Play chỉ xem bước đầu).
- Điện thoại thật, tỉ lệ màn khác.

## Phát hiện ngoài phạm vi
- Màn 4: hai nước đầu tự nhiên (hồng sang phải, xanh bẩn xuống) đều dẫn tới kẹt. Bài splash dạy trước điều này.
- Chữ gợi ý cũ "xe 3" vô nghĩa vì số trên xe đã tắt: đã thay bằng bàn tay.

## Bạn cần hiểu
- `TutorialRunner` (C# thuần) chỉ biết "lệnh nào được phép, khi nào qua bước"; `TutorialDirector` đổi đích ("car:0", "gate:r,1") thành khung pixel mỗi frame; `TutorialView` chỉ vẽ.
- Bãi tập là một `GameSession` bình thường nên luật, Hoàn tác, viền đỏ chạy y như màn thật; khác ở chỗ không đồng hồ, không ghi sao/kỷ lục.
- Lỗ sáng là mesh tự dựng (chia dải ngang), không cần shader hay material riêng.
- Muốn thêm bài: thêm một file JSON, khai `teaches`; test sẽ chạy thử bài đó.

## Trao đổi
2026-11-26 17:25 · Claude → Người làm · [BÁO]: xong trên `019-tutorial`, chờ chơi thử.
