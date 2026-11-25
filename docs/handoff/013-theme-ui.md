# 013 — Theme + UI · L ≤ 1 ngày (time-box cứng) · Plan (asset đã duyệt 25/11, chờ 012 merge)
Đang cầm: Codex (sau khi 012 merge và mở nhánh `013-theme-ui` từ `main`) · AI: Codex (code/tool/import), Claude (chốt số visual, review ảnh)
Liên quan: 012 · thay hàng 013 "Khung nhìn" trong play-plan §4 (V1–V7 của 011-01 gộp vào đây) · PROJECT §3 (IP), `AGENTS.md` §2 (asset ngoài phải hỏi)

## Mục tiêu (người chơi)
Mở game thấy một **trạm rửa xe ban ngày**: trời xanh nhạt, bãi nằm trên nền cỏ/vỉa hè có cây, hàng rào, đèn đường và nhà rửa xe phía sau; nút bấm to, bo tròn, có đáy đổ bóng như bản HTML v10, chữ Baloo 2 có dấu. **Không còn** chữ `Button`, `New Text`, ô xám, nền xanh đậm trống. Luật, Core, input, cảm giác (012) không đổi.

## Quyết định thiết kế (Claude, có lý do đọc luật)
- **Làm bằng asset CC0 + code, không tự vẽ sprite.** Nút/panel/icon vẽ bằng code (SDF, gradient) cho ra hình phẳng thiếu viền sáng và bevel; bản của Kenney đã chỉnh tay và cùng tác giả với Car Kit đang dùng. Code chỉ làm thứ code làm tốt: gradient, tint màu, bố cục.
- **Không đục asset từ game thương mại.** Vi phạm bản quyền, bài nộp cho lead công ty game.
- **Theme = "Trạm rửa xe phố"** (HTML: Khu phố + Gara). Một theme cho bản nộp; `ThemeConfig` chỉ làm khi còn giờ. Farm/Beach/Snow… bỏ.

## Asset ngoài `[ĐÃ DUYỆT 25/11: người làm duyệt cả 3 gói; Nunito chưa duyệt]`
Người làm tải, giải nén vào `Downloads` đúng tên thư mục; Codex chỉ nhập **file dùng tới** (không nhập 430 file), kèm `License.txt`:
| Gói | Nguồn | Thư mục trong Downloads | Dùng cho |
|---|---|---|---|
| Kenney UI Pack (CC0, 430 file) | kenney.nl/assets/ui-pack | `kenney_ui-pack` | nút, panel, thanh giờ, toggle, sao |
| Kenney Game Icons (CC0, 105 icon) | kenney.nl/assets/game-icons | `kenney_game-icons` | menu, loa, hoàn tác, gợi ý, chơi lại, mũi tên |
| Baloo 2 (SIL OFL) | fonts.google.com/specimen/Baloo+2 | `Baloo_2` (tên thật sau khi giải nén; dùng thư mục `static`: `Baloo2-ExtraBold.ttf`, `Baloo2-Bold.ttf`; **không** dùng file VariableFont) | tiêu đề, số, nút |
Nunito: chỉ dùng nếu người làm xác nhận trên trang Google Fonts là có tiếng Việt; không thì dùng Baloo 2 cho mọi chữ.
**Sprite chọn sơ bộ (Claude đọc thư mục trong Downloads 25/11; Codex xác nhận khi nhập).** UI Pack: `PNG/<màu>/Default/` (cỡ `Double` nếu mờ trên máy thật). Kiểu có đáy bóng giống HTML là họ `*_depth_*`.
- Nút chữ nhật chính (vàng): `Yellow/button_rectangle_depth_gradient` · nút thường (trắng/xám): `Grey/button_rectangle_depth_gradient` hoặc `_flat` · nút nguy hiểm (Thua/Chơi lại phụ): `Red/...` · nút xanh: `Blue/...`. Nút tròn icon: `button_round_depth_*`. Chọn một họ (`gradient` hoặc `flat`) cho cả game, không trộn.
- Thẻ hộp thoại: 9-slice từ `Grey|Blue/button_square_depth_border` (UI Pack không có file tên "panel"; xem `Sample.png`). Thanh giờ: nền `Grey/slide_horizontal_grey`, phần đầy `Blue|Yellow/slide_horizontal_color` (đổi đỏ khi thấp giờ như `lowClock` hiện tại). Toggle: `Blue/check_square_color_checkmark` và `Grey/check_square_grey`. Sao: `Yellow/star` và `Yellow/star_outline` (hoặc `star_outline_depth`) cho 3 sao thắng/LevelButton.
- Icon (Game Icons, bản **White** để tint): `pause` (PauseButton), `audioOn`/`audioOff`, `return` hoặc `backward` (Hoàn tác), `question` hoặc `exclamation` (Gợi ý; **không có bóng đèn**), `phone` (Rung; không có icon rung riêng), `home`/`menuGrid` (Chọn màn), `star`, `next`. Chơi lại: `Extra/icon_repeat_*` trong UI Pack.
- Font: Kenney Future trong UI Pack **không có tiếng Việt, không dùng**.
**Font TMP:** tạo **Font Asset tĩnh** (Static atlas) có Latin + Latin Extended Additional (tiếng Việt, U+1EA0–1EF9 và U+00C0–01B0). Không dùng Dynamic: làm bẩn asset khi Play (như `LiberationSans SDF - Fallback`) và khựng lúc sinh ký tự.

## Bảng màu (lấy từ bản HTML v10; số trong `UiTheme`/`VisualPalette`, không hard-code)
Trời `#8FD3FA→#C6EBFF→#E4F6FF` · chữ `#10315A` · chữ phụ `#2C5D8A` · nút thường trắng, đáy `#9CC7E4` · nhấn `#3FA9F5`/`#1F78C1` · nút chính vàng `#FFC93C`, đáy `#D9A21B`, chữ `#5A3B00` · thanh giờ `#7FD8FF→#3FA9F5`, thấp giờ đỏ (đang có `lowClock`) · nền mờ hộp thoại `rgba(20,20,40,.42)` · thẻ trắng bo 28 px, bóng cứng `0 8px 0 rgba(0,0,0,.12)` · cỏ `#6FA86B` · vỉa hè `#9AA3AE`.
Nút: bo 14–16 px, cao ≥ 48 px, đáy đổ bóng 3–4 px, nhấn xuống 2–3 px. Dùng sprite Kenney (xanh/vàng/xám) rồi tint theo bảng màu; đặt 9-slice (Border) bằng menu Editor.

## Danh sách UI (khớp `NavigationView` hiện có; GIỮ NGUYÊN tên object, tham chiếu và On Click)
| Panel | Thành phần cần skin |
|---|---|
| **LevelSelect** | tiêu đề (hiện `New Text`), lưới `LevelButton.prefab` (số màn + 3 sao: sao đầy/rỗng), `SelectButton`, toggle **Âm thanh** và **Rung** (icon loa/rung + trạng thái bật/tắt) |
| **Playing (HUD)** | `PauseButton` (icon menu, hiện chữ `Button`), tên màn, `ClockText` + `ClockFill` (thanh bo tròn), `MovesText`, `MessageText` (thẻ trắng mờ như `#hint`), `UndoButton` (icon hoàn tác + `-3s`), `HintButton` (icon bóng đèn + `-5s` + số lần còn), `RestartButton` (icon chơi lại; hiện `Button`) |
| **Paused** | thẻ trắng bo 28 px; `ResumeButton` (vàng, chính), `RestartButton`, `SelectButton`, toggle Âm thanh/Rung |
| **Won** | thẻ + **3 sao** (đầy/rỗng theo `StarsOnWin`), chữ `Thắng!` + giờ còn; `NextButton` (vàng), `RestartButton`, `SelectButton` |
| **Lost** | thẻ + chữ thua; `RestartButton` (vàng), `SelectButton` |
| **StuckBanner** | dải bo tròn "Kẹt xe — hoàn tác?" (đang là ô trắng trống) |
| Fade | giữ `fadeOverlay` |
Icon dùng Game Icons: menu/gear, loa bật/tắt, mũi tên vòng (hoàn tác), bóng đèn hoặc dấu hỏi (gợi ý), chơi lại, ngôi sao. Thiếu icon nào ghi vào báo cáo, **không vẽ thay**.

## Khung nhìn và cảnh quanh bãi (V1–V6 của 011-01)
1. `CameraConfig.padding` 0,5 → 0,1; nền camera `#BFE6FB` (trời nhạt), bỏ xanh đậm.
2. Nền: mặt cỏ rộng quanh bãi + vỉa hè viền bãi (1 mesh/1 material, không tạo material lúc chạy), sinh theo cỡ bãi.
3. Props từ **kit đã có sẵn trong repo** (`Art/Mechanics/Kenney/Racing`, `CityRoads`; tổng 208 FBX, 8,9 MB): `treeSmall`, `treeLarge`, `fenceStraight`/`fenceCurved`, `light-square`, `pitsGarage`/`pitsOffice` (làm "nhà rửa xe" 1–2 cái phía sau bãi), `tent`. Quy tắc: không vật nào trong 1 ô quanh mép bãi, **không che xe ở camera 70°** (thấp hoặc ở phía sau), ≤ 12 prop mỗi màn, dùng chung colormap của kit, chỉ prefab đã chuẩn hóa (pivot/scale/collider tắt).
4. Tấm sơn màu trước cổng (V4) và bỏ chữ nổi 1A/2B, số xe chồng nhau (V5): chỉ làm nếu còn giờ, theo bảng V4/V5 trong `011-01-visual-hand-pass.md`.
5. Ánh sáng (V2): Directional Light (50°, −30°), cường độ 1,1, ambient `#4A5568` → đổi ambient sáng hơn cho ban ngày (Claude chốt sau ảnh).

## Cách làm (thứ tự P1 bắt buộc)
- **P1** Nhập asset (qua menu Editor, backup `Logs/013-asset-backups`), tạo `UiTheme` (ScriptableObject: sprite, màu, font) + menu **Apply UI Skin** gán lên **object đã có** trong scene/prefab bằng `SerializedObject`; báo cáo từng thay đổi scene `đường dẫn · component · field · cũ → mới`. Panel Won/Lost/Paused/LevelSelect/HUD.
- **P2** `padding`, nền camera, mặt cỏ + vỉa hè.
- **P3** Props quanh bãi, ánh sáng. **P4** sơn trước cổng / bỏ chữ nổi.
Không thêm package; không đổi Project Settings; không đổi tên field/object `NavigationView` đã gán; không tạo UI mới ngoài danh sách; không gỡ test.

## Kiểm
Run All + Console 0. Ảnh Game view (Simulator iPhone 12 Pro Max) **7 ảnh**: chọn màn, chơi màn 01, chơi màn 30, Pause, Thắng, Thua, Kẹt xe — gửi Claude. Stats màn 16: **batches không vượt ~200** ở 568×320 (mốc 011: 156). Chữ tiếng Việt không vỡ ô vuông. Mở bằng Simulator tỉ lệ khác (iPhone SE) để kiểm safe area.

## Điều dễ sai
Đổi tên/xóa object `NavigationView` đang gán · sprite chưa đặt 9-slice nên méo · font Dynamic · nhập cả 430 file · prop che xe hoặc nằm trong ô quanh bãi · tạo material theo màn · nhãn khác ngôn ngữ (có chữ Anh) · nút nhỏ hơn 48 px · quên License.txt trong thư mục asset.

## Cắt nếu trễ
P4 → ánh sáng → props cây/hàng rào (giữ nền cỏ + vỉa hè) → toggle Âm thanh/Rung đẹp. **Không cắt:** skin cho HUD và hộp thoại thắng/thua, padding 0,1, bỏ `Button`/`New Text`.

## Việc người làm quyết trước APK `[Project Settings, phải hỏi]`
Package name (đề xuất `com.<tên bạn>.squeakylane`) · khóa Portrait · IL2CPP + ARM64. APK **nội bộ** đo fps/nhiệt/font có thể build trên bản chưa skin; không gửi cho ai trước khi 013 xong.

Báo cáo ~15 dòng; Trao đổi chỉ `[BÀN GIAO]`; không tự commit.

## Trao đổi
