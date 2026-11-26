
## 016 · Main menu + nút về menu · 2026-11-26 10:28

Canvas/UIRoot/SafeArea/016 MainMenu · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 HeroFloor · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 HeroFloor/016 Hero · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 Title · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 Subtitle · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 PlayButton · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 PlayButton/016 PlayLabel · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabLevels · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabLevels/016 Icon · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabLevels/016 Caption · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabHome · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabHome/016 Icon · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabHome/016 Caption · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabSettings · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabSettings/016 Icon · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 NavBar/016 TabSettings/016 Caption · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings/016 Dim · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings/016 Panel · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings/016 Panel/016 Header · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings/016 Panel/016 Close · GameObject · tạo mới
Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings/016 Panel/016 SoundToggle · Toggle · nhân bản từ LevelSelect/SoundToggle
Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings/016 Panel/016 HapticsToggle · Toggle · nhân bản từ LevelSelect/HapticsToggle
Canvas/UIRoot/SafeArea/LevelSelect/016 HomeButton · GameObject · tạo mới
Canvas/UIRoot/SafeArea/LevelSelect/016 HomeButton/016 Icon · GameObject · tạo mới
Canvas/UIRoot/SafeArea/LevelSelect/016 Backdrop · GameObject · tạo mới
Canvas/UIRoot/SafeArea/LevelSelect/016 Backdrop/016 HeroFloor · GameObject · tạo mới
Canvas/UIRoot/SafeArea/LevelSelect/016 Backdrop/016 HeroFloor/016 Hero · GameObject · tạo mới
Canvas/UIRoot/SafeArea/LevelSelect/016 Backdrop/016 Shade · GameObject · tạo mới
Canvas/UIRoot/SafeArea/LevelSelect/016 Ribbon · GameObject · tạo mới
LevelSelect/Title · TMP_Text · 'Squeaky Lane…' CardText → 'CHỌN MÀN' HudOutline 66 trên ribbon
LevelSelect/LevelScroll · Image/ScrollRect · nền khung popup tím, ẩn thanh cuộn, đệm lưới 36
LevelSelect/SoundToggle/013 Caption · TMP_Text · CardText → HudOutline trắng
LevelSelect/HapticsToggle/013 Caption · TMP_Text · CardText → HudOutline trắng
NavigationView · mainMenu · null → 016 MainMenu
NavigationView · menuSettings · null → 016 MenuSettings
NavigationView · menuPlayButton · null → 016 PlayButton
NavigationView · menuLevelsButton · null → 016 TabLevels
NavigationView · menuSettingsButton · null → 016 TabSettings
NavigationView · menuPlayLabel · null → 016 PlayLabel
NavigationView · homeButtons · → 1 phần tử
NavigationView · menuSettingsCloseButtons · → 2 phần tử
NavigationView · levelCurrentSprite · null → Btn_square_orange_v1
NavigationView · soundToggles · → 3 phần tử
NavigationView · hapticsToggles · → 3 phần tử

## 016 · Căn lại hộp Cài đặt Trang chủ

Gốc bên dưới là `Canvas/UIRoot/SafeArea/016 MainMenu/016 MenuSettings/016 Panel`. Áp `LayoutSettings` qua Unity MCP lên object sẵn có.

- MenuSettings/Dim (cùng cấp Panel) · Image · `raycastTarget` tắt → bật để nút đóng khi chạm lớp nền nhận được sự kiện.
- Panel · Image · `pixelsPerUnitMultiplier` 1 → 1,388889 để viền 9-slice mảnh hơn · `raycastTarget` tắt → bật để chạm trong khung không đóng hộp.
- Panel/Header · RectTransform · neo giữa mép trên, vị trí (0, −40) → (0, −88), kích thước 600×100 → 500×88 · TMP · cỡ chữ 72 → 64.
- Panel/Close · RectTransform · pivot góc phải trên → tâm, vị trí (−30, −30) → (−65, −65), kích thước 110×110 → 94×94 · Image · Sliced → Simple, giữ tỉ lệ ảnh.
- Panel/SoundToggle và HapticsToggle (tên object có tiền tố `016 `) · RectTransform · tâm (±150, −40) → (±155, −55), kích thước 0×0 → 240×184. Background chuyển từ mép trái sang giữa đầu cột, 144×144 → 132×132.
- Hai `013 Caption` · RectTransform · nhãn dàn ngang → giữa dưới icon (240×52) · TMP · CardText → HudOutline, cỡ tự động → 44 cố định.

## 016 · Thu gọn booster trong màn chơi

- `Canvas/UIRoot/SafeArea/Playing/UndoButton` và `HintButton` · RectTransform · `anchoredPosition` (±180, 158,4) → (±165,6, 118,8) · `sizeDelta` 237,6×237,6 → 194,4×194,4, đơn vị Canvas.
- Các con `013 ButtonVisual/013 Body` · Image · hệ số 9-slice 1,0059 → 1,2294 để thu thân nút đồng đều.
- Các con `013 ButtonVisual/013 Icon` · RectTransform · icon 30 → 25 theo mockup, bù lề trong suốt theo sprite. Không đổi nhãn, badge, liên kết hay thứ tự nút.

## 016 · Viền đỏ cảnh báo kẹt xe (menu `016 UI/Build Stuck Edge`)

- `Canvas/016 StuckEdge` · mới (đứng trước `UIRoot`) · Image sliced, không fill giữa, sprite `Art/UI/Sprites/stuck_edge`, màu #FF2740, hệ số 0,4 (viền 150 đơn vị) · CanvasGroup alpha 0, không chặn chạm.
- `NavigationView` · `stuckEdge` · trống → `Canvas/016 StuckEdge`. `StuckBanner` giữ nguyên trong scene, không hiện khi đã gán viền.
