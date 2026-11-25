# 013 — Theme + UI · L ≤ 1 ngày (time-box cứng) · Claude đã chốt visual v2 (25/11), Codex làm tiếp
Đang cầm: Codex (S2; kèm ảnh/Run All S1 cùng báo cáo S2) · Nhánh `013-theme-ui` từ main b12842f (012 đã merge), working tree chưa commit · AI: Codex (code/tool/import), Claude (chốt số visual, review ảnh)
Liên quan: 012 · thay hàng 013 "Khung nhìn" trong play-plan §4 (V1–V7 của 011-01 gộp vào đây) · PROJECT §3 (IP), `AGENTS.md` §2 (asset ngoài phải hỏi)

## QUYẾT ĐỊNH VISUAL V2 (Claude chốt, người làm duyệt 25/11) — GHI ĐÈ các mục dưới khi mâu thuẫn
Mockup tham chiếu: `docs/handoff/013-mockup-v2.html` (mở bằng trình duyệt; chỉ là bố cục, màu, cách xếp lớp, không phải sprite cuối). Phong cách dòng game casual (Rollic: Bus Jam, Sand Blocks) **chỉ để học cách làm**: không dùng sprite/icon/bố cục 1:1 của họ.
**Vì sao đổi:** 3 ảnh 25/11 lỗi do **UI sáng nằm trên thế giới tối** (chữ xanh đậm trên nền navy, icon trắng trên nút trắng) và nút chỉ có 1 viền mỏng + 1 dải đáy phẳng. Viền vàng quanh màn hình là **lớp Safe Area của Simulator**, không phải của game (đã có từ video cũ): bỏ qua.
**Giữ nguyên toàn bộ P1 đã làm** (import, font Static, menu, binding). Việc còn lại là sửa theo v2, không làm lại từ đầu.

### 1. HUD dạng "viên thuốc" navy (thay nút trắng)
- Pill: nền `#14336A`, viền ngoài `#0A1E44` 3 px, viền sáng bên trong `#4F8FE0` 2 px, đáy dày + bóng. Chữ trắng, **viền chữ `#0A1E44`** (TMP Outline). Ba pill: **đồng hồ (giữa, to nhất)**, **"Màn N" (phải)**, **"N nước" (nhỏ, dưới đồng hồ)**. Bỏ tiêu đề dài "Màn 23 · Chốt hai chiều" khỏi HUD.
- `ClockFill`: thanh giờ nằm trong pill đồng hồ hoặc bỏ nếu rối; thấp giờ đổi đỏ (`lowClock`). Sửa 9-slice (đặt Border đúng): ảnh 2 thanh bị méo hai đầu.
- `PauseButton`: nút **tròn** xanh `#3FA9F5` góc trái trên, icon `pause` trắng.
- **Đáy chỉ 2 booster** (66 × 66): `UndoButton` xanh + huy hiệu đỏ `-3s`; `HintButton` vàng + huy hiệu số lần còn. Dưới mỗi nút một **nhãn** pill navy nhỏ ("Hoàn tác", "Gợi ý"). Hết lượt gợi ý: nút xám, huy hiệu xám. `RestartButton` **ẩn khỏi HUD** (`SetActive(false)`, không xóa, không đổi `restartButtons` của `NavigationView`); "Chơi lại" nằm ở Pause, Thắng, Thua.
- `MessageText` không còn thẻ luôn hiện: chỉ hiện bong bóng trắng bo tròn ngắn khi có thông báo (chặn, gợi ý) rồi mờ đi.
Kích thước mockup tính theo phone rộng ~300 px; nhân theo `CanvasScaler` (booster 66 là ~22% bề ngang SafeArea, tối thiểu 48 dp).

### 2. Công thức nút 5 lớp (mọi nút chunky; số trong `UiTheme`)
1. **Thân:** sprite Kenney họ `*_depth_gradient` (xanh `#3FA9F5`, vàng `#FFC93C`; tròn cho Pause/refresh), đã có gradient và dải đáy.
2. **Viền ngoài tối `#0E2A55`** 3 px: `UnityEngine.UI.Outline` (distance ~3) trên Image.
3. **Bóng tiếp đất mềm:** Image con phía sau, sprite bóng mờ **tạo một lần bằng menu Editor** (PNG procedural, không phải asset ngoài), đen alpha .28, lệch xuống ~9 px.
4. **Vệt sáng góc** (glint): Image con trắng alpha .7, 14 × 6, bo, xoay −20°, góc trên-trái (cắt được).
5. **Icon** Game Icons trắng + `Shadow` (0, −2) navy alpha .55. Nhấn: `UiButtonPress` hạ 3 px + co 0,94.
Màu có nghĩa: **vàng = hành động chính** (Tiếp tục, Màn kế, Gợi ý, Chơi lại ở hộp Thua); **xanh = phụ trợ** (Pause, Hoàn tác, toggle); **đỏ** chỉ cho huy hiệu và banner kẹt xe; **xám** cho đã hết hoặc khóa.
**TMP:** 3 material preset dùng chung: `HudOutline` (Baloo2 ExtraBold, Outline `#0A1E44` ~0,3), `HudSmall` (Bold, Outline mỏng), `CardText` (Bold, `#10315A`, không outline). **Atlas Padding ≥ 9 khi tạo lại Font Asset tĩnh**, nếu không viền chữ bị cắt.

### 3. Thẻ hộp thoại (Paused, Won, Lost)
Thẻ trắng bo ~22, viền ngoài `#0A1E44` 3 px, dải đáy trong `#DCE6F2`, bóng dưới; **rộng ~80% SafeArea, không phủ gần hết màn** (ảnh 3 sai). Dải tiêu đề (ribbon) nổi lên mép trên thẻ: Pause **xanh** "Tạm dừng", Thắng **vàng** "Thắng!", Thua **đỏ `#E24B4A`** "Hết giờ" hoặc "Kẹt xe". Nền mờ `#0A1E44` alpha .5.
- **Won:** 3 sao Kenney `star` có viền `#0A1E44` (giữa to, nhô lên), sao rỗng xám khi không đạt; dòng "Màn N · còn X s"; hàng nút: tròn xanh `refresh` + vàng rộng "Màn kế". Pháo giấy dùng hệ hạt 012 sẵn có.
- **Paused:** vàng "Tiếp tục" (play), trắng "Chơi lại", trắng "Chọn màn"; dưới là 2 toggle **tròn** xanh (icon `audioOn/Off`, `phone`) kèm nhãn "Âm thanh", "Rung" (chữ `#10315A` trên nền trắng; ảnh 3 sai: chữ trắng trên xám).
- **LevelSelect:** nút màn vuông bo (xanh khi mở, **vàng** khi là màn hiện tại, xám + ổ khóa khi khóa), số màn + 3 sao nhỏ có viền; tiêu đề "Squeaky Lane / Trạm rửa xe phố" giữ như ảnh 1 (đã ổn); 2 toggle ở đáy dùng kiểu tròn như Pause.
- **StuckBanner:** pill đỏ `#E24B4A` viền navy "Kẹt xe — hoàn tác?" ngay dưới đồng hồ.

### 4. Bãi và cảnh quanh bãi (3D; mockup SVG chỉ minh họa)
**Lưu ý camera 70°:** nhìn từ trên xuống, **không thấy trời**; "dải trên" của mockup là mặt đất xa có công trình, không phải bầu trời. `camera.backgroundColor` chỉ lót phía sau (không lộ); cảm giác gradient lấy từ vignette.
1. **Bãi thành "khay" có độ dày:** viền vỉa hè `#C3CCD8` có **mặt bên** tối hơn `#7E8B9A` lộ ra ở mép dưới (camera nghiêng), **vạch sáng trắng mờ ở mép trên viền**, và **quad bóng tiếp đất** alpha .28 dưới bãi lên cỏ. Làm trong mesh/prefab bề mặt bãi hiện có (một mesh, một material, không tạo material lúc chạy).
2. **Dải trên (vùng HUD):** `pitsGarage` + `pitsOffice` đặt phía sau bãi làm "nhà rửa xe", hàng `treeSmall`/`treeLarge`, `light-square`. Vật cao để **phía sau bãi (z lớn)**: lên màn hình sẽ nằm trên đỉnh, không che bãi.
3. **Dải dưới (vùng booster):** đoạn đường (City Kit `road-*`) + vạch, một xe đỗ (Car Kit, màu không trùng màu cổng), `cone`/`construction-cone`, hàng rào thấp. Vật phía trước bãi phải **thấp (≤ 0,5)** và cách bãi ≥ 1,5 ô.
4. **Mặt cỏ** `#86C876 → #5C9C56` (hai tông bằng vertex color hoặc hai submesh cùng material), sọc cắt cỏ mờ (cắt được). Kéo dài **ra ngoài mép màn** ở mọi tỉ lệ (iPhone SE đến 12 Pro Max).
5. **Vignette:** Image UI toàn màn, sprite xuyên tâm tạo bằng menu (alpha 0 ở tâm đến navy `#0A1E44` alpha .38 ở mép), 1 draw. **Bong bóng xà phòng bay:** một ParticleSystem riêng ≤ 10 hạt, dùng lại material hạt có sẵn, trắng alpha thấp (cắt được).
6. Props theo quy tắc cũ: ≤ 12 prop/màn, không vật nào trong 1 ô quanh mép bãi, dùng colormap kit, Stats màn 16 **không vượt ~200 batches** (sprite UI gộp một atlas; TMP chỉ 3 material).
7. **Đổi nền theo nhóm màn** (ngày, hoàng hôn, đêm; chỉ đổi màu cỏ, vignette, ánh sáng qua `UiTheme`): làm **cuối cùng nếu còn giờ**, cắt đầu tiên.

### 5. Thứ tự làm (thay "Cách làm" cũ)
**S1** (cứu khỏi tối): áp P2: nền sáng, cỏ + vỉa hè, `padding` 0,1, vignette; Run All, chụp ảnh. **S2:** công thức nút + pill + TMP preset + sửa 9-slice, 2 booster, ẩn Restart HUD. **S3:** thẻ Pause/Thắng/Thua, LevelSelect, banner. **S4:** khay dày + bóng tiếp đất, props dải trên/dưới. **S5:** bong bóng, đổi nền theo nhóm màn, sọc cỏ.
**Cắt nếu trễ (theo thứ tự):** S5, glint, props dải dưới, props dải trên, bóng tiếp đất. **Không cắt:** S1, S2, S3.
Sau mỗi bước S: Run All, Console 0, 1 đến 2 ảnh gửi Claude (không làm hết rồi mới gửi). Báo lại danh sách thay đổi scene.

### 6. Lỗi từ 3 ảnh 25/11 phải hết
Chữ HUD chìm trên nền tối · icon trắng trên nút trắng · thanh giờ méo · nút Pause ô xám · thẻ Pause phủ gần hết màn · chữ toggle trắng trên xám · khối nhạt lớn ở LevelSelect · bãi nhỏ (`padding`) · "Button"/"New Text" còn sót.

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

## Báo cáo Codex — dừng theo người làm lúc11:36 ngày25/11
Người làm xem3 ảnh và đánh giá bản hiện tại chưa đạt, yêu cầu tạm không sửa thêm và trao lại Claude. Codex dừng code, không tự tinh chỉnh/revert/commit.
P1 đã có UiTheme/UiThemeAssets/UiSkinTools, UiButtonPress/UiPanelDecoration; NavigationView chỉ đổi cách hiện icon/sao, giữ handler/lệnh. Editor.asmdef thêm tham chiếu Unity.TextMeshPro có sẵn, không thêm package/assembly mới.
Người làm đã bấm Import UI Assets + Apply UI Skin (Editor.log5603/5884): chỉ nhập8 sprite UI +11 sprite icon/sao,2 Baloo2 TTF static,3 license; TMP font lưu Static (m_AtlasPopulationMode0, sourceFontFile null), atlas11 icon.
Đã đổi Play.unity/LevelButton.prefab bằng menu, không YAML tay. Menu báo binding/OnClick trước/sau khớp; tên object cũ giữ nguyên. Thêm trang trí ClockTrack và nhãn MessageText trong StuckBanner (hai widget thuộc danh sách), không thêm control hay listener.
Báo cáo từng field có ở docs/handoff/013-scene-changes.md; lỗi báo cáo: reference asset đang in True/False thay vì tên/GUID (Convert.ToString gặp implicit bool Unity), chưa sửa vì người làm yêu cầu dừng.
P2/P3/P4 MỚI VIẾT, CHƯA ÁP: SceneThemeView/SceneThemeTools (menu3 Apply City Scene), BoardView.sceneTheme, cỏ/vỉa hè/sơn cổng1 mesh/1 material,5 model có sẵn chuẩn hóa/gộp mesh, số xe ẩn qua config. Chưa bake prefab/material cảnh. SceneThemeTools.cs mới chưa có.meta tới khi Unity refresh.
UiTheme.asset hiện environmentPrefab=null, groundMaterial=null, propPrefabs rỗng; camera/padding/ánh sáng/cảnh vẫn bản cũ. Vì vậy ảnh HUD có màu chữ mới nhưng nền tối cũ; chưa phải bản theme hoàn chỉnh.
Ảnh1: LevelSelect ngoài Play, lưới trống không đủ bằng chứng lỗi danh sách runtime; title nhỏ, toggle chữ trắng nhạt trên nền sáng, viewport khối nhạt lớn.
Ảnh2: HUD chữ xanh đậm chìm trên nền tối; Pause/icon trắng nhạt trên nút xám; thanh giờ kéo méo; bãi vẫn nhỏ và nền cũ trống. Ảnh3: thẻ Pause xám to gần hết màn hình, nhiều khoảng trống, nút còn cảm giác mặc định. Viền vàng quanh UI chưa xác định nguồn; Claude xem ảnh trước khi quyết.
Ảnh lưu local (Logs ignored, không phải asset game): Logs/013-review/01-level-select.png,02-playing.png,03-paused.png; người làm gửi kèm3 ảnh cho Claude nếu Claude không đọc được file local.
Đã kiểm: đủ file/icon/font/giấy phép trên Downloads, fontTools kiểm đủ tiếng Việt và mũi tên; Roslyn Game/Presentation/Editor/Tests/Android0 lỗi TRƯỚC khi viết SceneThemeTools/SceneThemeView mới nhất,39 CS0649 ở field serialize sẵn có. Chưa biên dịch toàn bộ working tree mới nhất.
Chưa kiểm: Run All013, Console0/7 màn sau bản hoàn chỉnh, tương tác UI/safe area khác, Stats16/GC/Android. Không coi là đã xong hoặc APPROVE; dừng để tránh tăng code khi visual chưa chốt.
Bạn cần hiểu: P1 đổi font/màu không đủ tạo theme nếu P2 nền/camera còn cũ; sprite 9-slice cần căn hình/biên cho đúng, không chỉ gán texture; Static font tránh phát sinh glyph vào asset khi Play. Không có sửa Core/input/FeelConfig/ActionAnimator012/package/Project Settings.
Claude cần chốt hướng bố cục/tương phản/thẻ/nút/thanh giờ theo3 ảnh, rồi quyết giữ/sửa/bỏ phần code đang dở. Codex không tự triển khai phương án tiếp khi chưa nhận lại gậy. APK đầu tiên vẫn mục tiêu tối26/11.

## Báo cáo Codex — S1 code/tool sẵn, CHƯA áp/kiểm trong Unity (25/11)
[FILE] Giữ P1/font Static/import/binding và working tree; đọc V2/mockup HTML + ảnh tham chiếu. Trình duyệt công cụ chặn file:// nên chưa xem HTML render; không dùng cách vòng qua chặn.
S1: thêm BrightGroundTools → menu `Tools/Squeaky Lane/013 Theme/S1 Apply Bright Ground`; ẩn menu3 cũ để không áp props/sơn cổng/ẩn số xe trước S4.
UiTheme: grass #6FA86B → #5C9C56 khi áp menu; grassFar #86C876; vignette #0A1E44 alpha0.38; tâm trong suốt radius0.55 (số tạm chờ ảnh); groundOverscan1; cameraPadding0.1. Mọi số trong config.
SceneThemeView: cỏ/vỉa hè chung1 mesh/1 material asset Unlit/Texture để nền không bị ambient cũ làm tối; không tạo material lúc chạy, chưa đổi ánh sáng chờ Claude chốt sau ảnh.
Lệch chi tiết kỹ thuật: gradient cỏ dùng texture procedural2 hàng thay vertex color/2 submesh; vẫn1 mesh/1 material, không thêm draw cho gradient. Vignette PNG256 tạo qua AssetDatabase,1 Image không raycast, dưới UIRoot.
BoardView: FitCamera dùng CameraBackground của theme thay navy cũ; sau căn camera kéo riêng quad cỏ ra ngoài4 góc camera + overscan, không phóng vỉa hè. Không chạy phần này mỗi frame.
Phạm vi S1: propCount0, hideCarNumbers=false, gateStrips=false; chưa dựng props/khay/bóng/S2/S3/S5. P1 font padding8 giữ nguyên; nếu tạo lại ở S2 phải ≥9, chỉ3 preset.
Menu sẽ tạo CityEnvironment.prefab, Ground.mat/GroundColors.png/Vignette.png; gán BoardView.sceneTheme trong GameRoot + scene, CameraConfig.padding0.5→0.1, camera nền nhạt, thêm Canvas/013 Vignette. **Chưa chạy menu nên chưa có các thay đổi asset/scene này.**
Danh sách field thực tế (đường dẫn · component · field · cũ → mới) được append vào docs/handoff/013-scene-changes.md khi áp S1; báo reference asset bằng tên/path, không True/False như report P1 cũ.
[ĐÃ KIỂM] Roslyn Game/Presentation/Editor/Tests/Android0 lỗi sau S1; Presentation39 CS0649 như baseline. Thêm4 ca EditMode kiểm cỏ phủ camera SE/12ProMax và đổi tỉ lệ, vỉa hè không bị kéo, không collider/props. **Test mới chỉ biên dịch, chưa chạy.**
[CHƯA KIỂM] Unity refresh/biên dịch, Run All, Console0, ảnh Game view01/30, Stats16≤~200, safe areaSE, reloadscene/vàoPlay lại/Android. Không có Unity MCP/CLI trong công cụ hiện tại; cần người làm chạy Editor.
git diff --check báo khoảng trắng ở YAML scene/prefab do Unity P1 tạo; không sửa tay YAML hoặc dọn ngoài phạm vi. Asset/script mới chờ Unity Refresh tự tạo.meta, không tự viết GUID.
Bạn cần hiểu: đặt nền camera trong scene chưa đủ vì FitCamera từng ghi đè navy; cỏ phủ góc camera giúp không lộ mép nền ở điện thoại dài; vignette dưới HUD giữ chữ/nút không bị phủ tối.
Chưa có kết luận V2/V3 hoặc APPROVE visual; chờ Run All + Console + 1–2 ảnh để Claude đánh giá S1 rồi mới S2. Không commit/push/merge; APK đầu tiên vẫn tối26/11.

## Cập nhật S1 — sau ảnh người làm 25/11, sửa lỗi chọn màn
[LOG/FILE] S1 đã áp14:06, có CityEnvironment/Ground/Vignette và padding0.1; field cũ→mới đã ghi vào013-scene-changes.md. Báo cáo “chưa áp” phía trên là trạng thái trước mốc này.
[ẢNH] Test Runner556 Passed/0 Failed; ảnh cắt phần Skipped nên chưa chốt số đó. Stats162 batches,61.9fps ở384×216/16:9; chưa đủ xác nhận màn16 ở568×320 hoặc hiệu năng Android. Console0 chưa xác nhận từ ảnh.
[FILE] Nguyên nhân nút chọn màn mất: UiSkinTools P1 đặt Viewport Image.color=Color.clear, trong khi Viewport có Mask đang bật/showMaskGraphic=false và CanvasRenderer cullTransparentMesh=true. Alpha0 làm vùng stencil không được ghi, che con; không phải thiếu dữ liệu màn.
Đã sửa UiSkinTools: nếu Image có Mask thì màu trắng alpha1, Mask.showMaskGraphic=false; không Mask mới dùng Color.clear. Thêm menu `S1 Fix Level Select Mask` chỉ sửa2 field này qua SerializedObject, kiểm binding/OnClick trước/sau, backup/lưu scene và append report. Chưa áp menu sửa trong Unity.
Thay đổi scene dự kiến sau menu: Canvas/UIRoot/LevelSelect/.../Viewport · Image · m_Color · RGBA(0,0,0,0)→RGBA(1,1,1,1); cùng Viewport · Mask · m_ShowMaskGraphic · false→false. Menu ghi đường dẫn đầy đủ thực tế.
[ĐÃ KIỂM] Roslyn5 assembly0 lỗi; git diff --check phần code/tài liệu sạch whitespace (có cảnh báo LF/CRLF của Git). Không sửa YAML tay, Core/luật/012/binding/package/settings; không commit.
[CHƯA KIỂM] Menu sửa/Play danh sách nút, cuộn tới màn cuối/chọn màn, reloadscene/vàoPlay lại, Run All sau sửa và Console0; ảnh Game view dọc01/30 cho Claude và SE. Dừng trước S2.
Bạn cần hiểu: Mask dùng độ đục Image để xác định vùng giữ lại; muốn giấu hình Mask thì tắt Show Mask Graphic, không kéo alpha0. Ngoài Play danh sách vẫn trống vì NavigationView.Initialize mới sinh nút lúc chạy.
Tự tin nguyên nhân/sửa code≈90% theo scene + code, chưa xác nhận kết quả hiển thị. Ảnh Game hiện16:9 nên dùng Simulator12ProMax hoặc9:16 để đánh giá bố cục mobile. APK đầu tiên vẫn tối26/11.

## Chốt bàn giao S1 cho Claude — 25/11 14:13
Người làm đã sửa tay Viewport trong Inspector theo hướng dẫn, báo “oke r” và yêu cầu chuyển Claude. Đã đối chiếu scene lưu: Image màu trắng alpha1, Mask.showMaskGraphic=false. Đây là cập nhật sau mục “chưa áp sửa” ở trên.
Đã kiểm: S1 áp qua Editor/log; ảnh Run All556 Passed/0 Failed **trước sửa Mask**; Roslyn0 lỗi sau sửa code; scene đã lưu đúng2 field Mask; người làm xác nhận ổn. Không tự nhận đã Play/Console0 thay người làm.
Chưa kiểm: Run All sau sửa Mask, Console0, ảnh Game view dọc01/30 và SE, cuộn/chọn tới cuối/reloadscene, phép đo Stats đúng cỡ/màn16, Android. Ảnh Stats hiện162 batches/384×216/16:9 chỉ là mốc tham khảo.
Claude review S1/ảnh và phần code đang dở trước khi trao lại Codex làm S2; giữ P1 và working tree chưa commit, S2–S5 chưa làm. Font vẫn Static padding8 của P1, tạo lại ở S2 phải≥9/3 preset.
[CHỐT] Người làm yêu cầu lỗi Inspector nhỏ chỉ hướng dẫn sửa tay; không thêm tool riêng. Menu chỉ cho tạo asset/sửa hàng loạt; đã ghi PROJECT§2.5. Menu sửa Mask đã lỡ viết giữ nguyên, không bắt người làm dùng.

## Verdict S1 (Claude, 25/11 14:19): APPROVE CÓ ĐIỀU KIỆN (code); visual V3 CHƯA XEM
**Đã soi (đọc code + log):** `BrightGroundTools`, `SceneThemeView`, `UiTheme` (phần S1), diff `BoardView`/`CameraConfig`/`NavigationView`, `SceneThemeTests`, `Editor.log` (không có `error CS`/Exception/NullReference quanh S1). Khớp V2 §4: cỏ hai tông + vỉa hè **một mesh, một material** (Unlit/Texture, atlas procedural 9×2, bilinear, không material runtime); cỏ kéo ra 4 góc camera chỉ chạy sau `FitCamera`, không mỗi frame, vỉa hè không bị kéo, không collider; vignette 256² là Image dưới UIRoot, `raycastTarget` tắt; `padding` 0,1 từ `UiTheme`; menu kiểm `NavigationView` không đổi trước khi lưu scene; mọi số trong `UiTheme`. Lệch nhỏ so với V2 (gradient cỏ bằng texture thay vertex color) **chấp nhận**, lý do hợp lý.
**Chưa soi:** `UiSkinTools`/`UiThemeAssets` (P1), `SceneThemeTools` (ẩn, chưa dùng), 902 dòng diff `Play.unity`.
- [CHẶN merge S1, không chặn S2] **Chưa có ảnh dọc nào**: không đánh giá được nền sáng, vignette, độ sáng xe so với cỏ. Cần 2 ảnh Simulator iPhone 12 Pro Max dọc (màn 01, màn 30) + 1 ảnh iPhone SE; Run All **sau sửa Mask** (kèm số Skipped), Console 0.
- [NÊN] **Ánh sáng chưa chốt:** cỏ Unlit sáng đầy đủ nhưng xe và mặt bãi vẫn ăn ambient cũ `#4A5568` (tối) nên có thể "chìm" so với cỏ. Số khởi điểm (Claude, chỉnh theo ảnh): Directional Light intensity **1,0**, góc giữ (50, −30), ambient color **`#9AA9C2`**; người làm sửa tay trong Lighting/Inspector rồi gửi ảnh (không thêm menu).
- [NÊN] **Stats màn 16:** báo 162 batches ở 384×216 không so được với mốc 011 (156 ở 568×320). Đo lại **màn 16 ở 568×320** trước khi dùng ngưỡng ~200. Vignette là 1 lớp alpha toàn màn: ghi vào danh sách đo ở APK (fill-rate), chưa phải lỗi.
- [GỢI Ý] `UiSkinTools` đã chặn tái phát lỗi Mask alpha 0; thêm 1 dòng cảnh báo vào báo cáo menu nếu gặp Image có Mask để lần sau khỏi sửa tay.
- [GỢI Ý] Báo cáo S1 trước khi áp ghi "chưa áp" và báo cáo sau ghi "đã áp": ở S2 chỉ ghi một báo cáo mới, không để hai trạng thái trái nhau trong phiếu.
**Kết luận:** S1 đủ để bắt đầu S2. Điều kiện để coi S1 xong: ảnh dọc 01/30/SE + Run All + Console 0 + Stats 16 đúng cỡ, **gửi cùng báo cáo S2**, không chờ riêng.

## Trao đổi
2026-11-25 11:36 · Codex → Claude, Người làm · [BÀN GIAO]: Người làm đánh giá3 ảnh chưa đạt, yêu cầu tạm dừng sửa và trao đổi Claude. Trao gậy Claude trên013-theme-ui/HEADb12842f, toàn bộ working tree chưa stage/commit. P1 menu/import/skin đã chạy, scene+LevelButton đổi qua Editor; font Static/icon/license có, binding/OnClick menu báo giữ nguyên. P2/P3/P4 mới có code chưa áp và chưa biên dịch bản cuối, UiTheme environment/material null; không bấm menu3/tiếp Play/test khi người làm chưa yêu cầu. Ảnh và danh sách vấn đề ở báo cáo, Logs/013-review; chưa Run All/Stats/7ảnh. Không revert/xóa asset/commit/push/merge. Claude chốt lại visual trước khi Codex viết tiếp.
2026-11-25 11:57 · Claude → Codex, Người làm · [BÀN GIAO]: Người làm duyệt hướng v2 (mockup `docs/handoff/013-mockup-v2.html`, mục "QUYẾT ĐỊNH VISUAL V2" đầu phiếu). Trao gậy Codex: giữ P1, làm theo S1→S5 (S1 trước: nền sáng, cỏ/vỉa hè, padding 0,1, vignette để UI hết chìm), sau mỗi bước Run All + ảnh gửi Claude; đối chiếu mục 4 v2 (khay có độ dày, bóng tiếp đất, dải trên/dưới) khi áp code P2/P3 đã viết. Cắt nếu trễ theo mục 5. Chưa commit/push/merge.
2026-11-25 12:08 · Codex → Người làm, Claude · [BÀN GIAO]: Code/menu S1 sẵn trên013-theme-ui/HEADb12842f, giữ P1 chưa commit. Roslyn5 assembly0 lỗi, chưa chạy Unity/Run All/ảnh/Stats; chưa áp asset/scene S1. Người làm thoát Play/Prefab Mode, Refresh, chạy S1 Apply Bright Ground (không chạy lại menu1/2), Run All + Console rồi chụp Game view01/30, Stats16 và kiểm SE. Menu tự ghi field cũ→mới vào013-scene-changes.md. Chờ kết quả/ảnh Claude trước S2; chưa props/khay/đổi012/settings. APK đầu tiên tối26/11.
2026-11-25 14:10 · Codex → Người làm, Claude · [BÀN GIAO]: S1 đã áp qua Editor/log14:06; nhận ảnh556Passed/0Failed, Stats162 ở384×216/16:9. Sửa nguyên nhân chọn màn bị che do P1 đặt Image Mask alpha0; menu mới S1 Fix Level Select Mask chỉ alpha1 + showMaskGraphic=false, giữ binding, append scene report. Roslyn0 lỗi; người làm Refresh ngoài Play, áp menu mới rồi Play kiểm danh sách/cuộn/chọn màn và Run All/Console; gửi ảnh dọc S1 cho Claude trước S2. Chưa áp/Play bản sửa, chưa chốt Console/Stats đúng cỡ/Android; không commit.
2026-11-25 14:13 · Codex → Claude · [BÀN GIAO]: Theo yêu cầu người làm, trao gậy Claude review S1 trên013-theme-ui/HEADb12842f, toàn bộ working tree chưa commit. S1 nền/cỏ/vỉa hè/padding0.1/vignette đã áp; ảnh556Passed/0Failed trước sửa Mask, Stats162 ở384×216/16:9. Người làm sửa tay Mask chọn màn, báo ổn; đối chiếu scene lưu alpha1/showMaskGraphic=false; UiSkinTools đã chặn tái phát, Roslyn0 lỗi. Còn Run All sau sửa/Console0/ảnh dọc01–30/SE/Stats đúng cỡ/Android; chưa S2–S5. Claude đánh giá S1 rồi quyết bước tiếp; không commit/push/merge. Lỗi Inspector đơn giản chỉ hướng dẫn sửa tay, không thêm tool riêng. APK đầu tiên vẫn tối26/11.
2026-11-25 14:19 · Claude → Codex, Người làm · [BÀN GIAO]: Review S1: APPROVE CÓ ĐIỀU KIỆN (code ổn; V3 chưa xem vì chưa có ảnh dọc), xem Verdict S1. Trao gậy Codex làm S2 (công thức nút 5 lớp, pill HUD, TMP 3 preset padding ≥9, sửa 9-slice, 2 booster, ẩn RestartButton HUD, bỏ Button/New Text) theo V2 §1–2. Người làm sửa tay ánh sáng (Directional 1,0; ambient #9AA9C2) và gửi ảnh dọc 01/30/SE + Run All + Console + Stats màn 16 ở 568×320 cùng báo cáo S2. Chưa commit/push/merge.
