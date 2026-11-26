# 016 — Thế giới đồ chơi: trạm rửa giữa phố, xe mảng đậm nhạt, cơ chế vẽ lại, chọn màn, main menu     Cỡ: L (chia 6 phần)     Trạng thái: Chờ người làm chơi thử
Đang cầm: Claude        AI tham gia: Claude (code, nối, kiểm), Codex (016-a…d: shader, cơ chế, phố, ảnh IP)
Liên quan: 015 (hầm rửa, Toy Lit, khay bo góc). Nhánh: `016-toy-world` (rẽ từ `015-toy-look` vì 015 chưa merge).

## Yêu cầu người làm (26/11, ảnh màn 7 + ảnh game đỗ xe + ảnh menu IP)
1. Theme khó hiểu ("đồng không mông quạnh có mỗi chỗ rửa"): bãi là **trạm rửa nằm giữa khu phố**, xe ra cổng **chạy theo đường tới lối ra gần nhất** rồi khuất (người làm chọn 26/11).
2. Xe: ngoài viền tối, muốn **mảng màu đậm nhạt** (nóc/mui đậm hơn…), tham chiếu video YouTube `--AF6LwQntQ` và ảnh game đỗ xe.
3. Vẽ lại **toàn bộ cơ chế** theo kiểu game Rollic/Voodoo.
4. Sửa UI **màn chọn màn** cho đẹp (Claude tự chỉnh).
5. **Main menu** đầu game: bố cục học ảnh menu mẫu, **chỉ nút chạy được** (LEVEL n, Chọn màn, Cài đặt; người làm chọn 26/11); nút lấy từ project cũ (`Sand Puzzle Clone/_Project.rar`); ảnh trung tâm là **ảnh IP xe được rửa**, Codex thử tạo ảnh. **Không vẽ mặt ở đâu cả** (giữ `[CHỐT]` cũ; người làm xác nhận 26/11).

## Phân việc
| Phần | Ai | Phiếu |
|---|---|---|
| Shader mảng đậm nhạt | Codex viết, Claude áp | 016-a |
| Cơ chế vẽ lại (prefab mới, giữ cấu trúc) | Codex viết tool, Claude chạy + đổi catalog | 016-b |
| Props phố + vật liệu đường | Codex | 016-c |
| Ảnh IP main menu | Codex thử tạo ảnh | 016-d |
| Đường vòng quanh bãi (mesh theo cỡ màn) + xe ra cổng chạy theo đường | Claude | — |
| Màn chọn màn + main menu | Claude | — |

## Điều dễ làm sai
Đường vòng ăn mất bề ngang màn dọc (bãi nhỏ đi) · xe đi đường đặt chỗ sai làm xe khác chờ vô cớ · xe chạy đường xuyên qua hầm rửa · prefab cơ chế mới thiếu phần bắt buộc (đèn đỏ/xanh, phần quay của trụ, che mờ) · màu cổng/xe khó phân biệt sau khi đổi tông · menu thêm nút giả · số hard-code.

## Cắt nếu trễ (theo thứ tự)
Props phố phụ → ảnh IP (dùng nền tĩnh có sẵn) → vẽ lại cơ chế ít gặp (portal, trụ) → đường vòng (giữ xe ra thẳng như cũ). **Không cắt:** APK thử máy, main menu tối thiểu.

--- AI làm điền khi xong ---
## Đã làm (26/11)
- **Phố quanh trạm rửa:** lưới phố hình # (`SceneThemeView.BuildStreets`, một mesh, `M_ToyRoad`), cách mép bãi `streetInner` 0,8 ô, rộng 0,95; camera chừa `streetInner + streetWidth × streetVisible` mỗi bên (bãi nhỏ hơn trước: bãi 5 cột còn ~60% bề ngang). Khay hạ còn 0,06 để ngang đường. Nhà/cây xếp hàng ngoài hai phố ngang (`SpawnTown`).
- **Xe ra cổng:** chạy tới tim phố ở tốc độ tối đa, cua 90° (bán kính 0,5, 0,22 s) về phía lối ra gần hơn, chạy 12 ô trong 0,9 s rồi biến mất; đoạn ngoài bãi không giữ chỗ. FeelConfig nhóm "016". Không đổi Core.
- **Cơ chế vẽ lại:** `MechanicCatalog` trỏ sang `Prefabs/Mechanics/Toy` (cổng barie 6 màu, cọc, đèn, bàn xoay, trụ, portal, bùn). Dải màu trên nền trước cổng bật lại.
- **Xe:** nóc/mui đậm hơn thân (material xe `_TopTint` 0,88).
- **Main menu** (`Tools → Squeaky Lane → 016 UI → Build Main Menu`, `MainMenuTools`): ảnh IP, tên game, nút MÀN n, thanh Chọn màn / Trang chủ / Cài đặt, hộp Cài đặt. `NavigationView` thêm các ô 016; để trống thì chạy như cũ.
- **Chọn màn:** nền ảnh IP phủ tím, ribbon "CHỌN MÀN", khung popup tím, ẩn thanh cuộn, ô màn đang chơi tới màu cam, nút nhà về menu.
- Scene: chi tiết ở `docs/handoff/016-scene-changes.md` (chỉ thêm object, không mất object nào).

## Test và kết quả (MCP)
- EditMode **585 · 584 Passed · 0 Failed · 1 Skipped** (thêm `StreetExit016Tests` 2 bài). Console 0 lỗi khi Play.
- Simulator 1284×2778: ảnh `Logs/016-shots/11-menu, 13-level08, 14-exit-a, 15-exit-b, 17-level16, 18-level03`; màn Chọn màn ở `02-levels`.
- Chạm nửa xe: 846/846 điểm trên 25 màn đúng xe, đúng nửa. Màn 16: 167 lượt vẽ 3D (như 015), chưa tính UI (UI menu mới không hiện lúc chơi).

## Giả định · Chưa kiểm
- Chưa kiểm: bấm nút menu bằng tay (mới gọi qua code và chụp), hộp Cài đặt, đèn giao thông/trụ/portal/bùn mới trong Play, xe lùi ra cổng cua trên phố, máy thật/fps, APK.
- Hầm rửa cạnh trái/phải: thảm đè ngang phố dọc, xe chạy trên phố đó có thể xuyên qua thảm.
- Tồn tại từ trước: nút "XOAY" che xe trên bàn xoay.

## Đã làm · Nút bàn xoay và khung Cài đặt

- `TableButtons` dùng sprite mũi tên nổi `icon_turntable.png` thay nhãn XOAY. Huy hiệu chuyển sang nửa bàn đối diện xe theo hướng hiện tại và có khoảng hở với hình chiếu collider. Khi bàn trống, vùng chạm tắt. Vùng chạm giữ 9 mm và tiếp tục ưu tiên qua GraphicRaycaster.
- `UiTheme` thêm sprite và thông số `tableBadgeRatio` 0,49, `tableBadgeLaneOffset` 0,65 ô, `tableBadgeCarGap` 0,04 đường kính vùng chạm. Bố cục hộp Cài đặt cũng lấy từ `UiTheme`; thay đổi từng object trong [016-scene-changes.md](016-scene-changes.md). `MainMenuTools` dùng cùng bố cục khi dựng lại menu.
- **V2:** Unity MCP biên dịch sạch. EditMode Run All **585 tổng · 584 qua · 0 lỗi · 1 bỏ qua** (bài Profiler chạy tay có sẵn). Play Console 0 lỗi, 0 cảnh báo. Màn 8: chạm mép vùng nút xoay đủ bốn lượt, kể cả một điểm cũng trúng collider xe. Tâm xe vẫn nhận raycast 3D, hình nút tách khỏi collider ở cả bốn hướng và bàn trống không chặn chạm. Cài đặt mở được, hai công tắc đổi trạng thái rồi được trả về giá trị ban đầu. Nút X và chạm lớp nền đều đóng hộp, chạm khoảng trống trong hộp giữ nguyên.
- **V3:** iPhone 12 Pro Max Simulator, 1284×2778. Ảnh trong `Logs/016-shots/`: `21-settings-fixed.png`, `22-turntable-horizontal.png`, `23-turntable-vertical.png`, `25-turntable-opposite.png`.

### Chưa kiểm

- Cảm giác chạm bằng ngón tay và kích thước vật lý trên máy thật. Play đã kiểm cả bốn hướng ở màn 8.

### Bạn cần hiểu

- Nút có sprite nhỏ và một Image trong suốt lớn hơn để nhận chạm. `InputController` hỏi EventSystem trước raycast 3D, nên điểm nằm trong cả hai vùng được giao cho nút xoay.
- Viền popup dùng cùng sprite, với hệ số 9-slice riêng để phần viền giữ độ dày phù hợp với khung. Công tắc vẫn dùng kết nối gốc trong `NavigationView`.

## Đã làm · Vùng chơi rộng hơn và bản thử nền đô thị 2D

- `UiTheme.streetVisible` 0,90 → 0,50; `CameraConfig.playingBottomInset` 0,17 → 0,12. Chừa đủ chỗ cho hầm rửa như trước. Màn 23 trên iPhone 12 Pro Max Simulator 1284×2778: ô lưới rộng 810 → 880 px (+8,7%), vùng chạm mỗi booster rộng 312 → 255 px (−18%).
- `UiTheme.boosterSize` 66 → 54, `boosterPosition` (50, 44) → (46, 33), `boosterIconSize` 30 → 25. Áp qua menu `016 UI → Apply Booster Layout`, chi tiết scene ở [016-scene-changes.md](016-scene-changes.md).
- Bản thử ở `c86f1f1` trải ba dải sprite 2D ngoài đường. Theo yêu cầu sau đó, đã gỡ gắn sprite khỏi game và dùng lại phố/props 3D hiện có. Ảnh nguồn 2D vẫn dùng được để tham khảo.
- **V2:** EditMode Run All **585 tổng · 584 qua · 0 lỗi · 1 bỏ qua** (bài Profiler thủ công). Play Console 0 lỗi, 0 cảnh báo. Raycast 3D kiểm 846/846 điểm đúng xe và đúng nửa trên 25 màn, toàn bộ nằm trong camera. Màn 8: xoay 1 nước, Hoàn tác trả về 0, Gợi ý nhận click. Màn 3: hai hầm rửa và thảm nhìn rõ.
- **V3 bản thử 2D:** `Logs/016-shots/32-city-after23.png` và `33-city-after03.png`. Chưa đo cảm giác chạm hay hiệu năng trên thiết bị thật.

### Bạn cần hiểu

- Thu booster nhường chiều cao cho vùng chơi. Giảm phần phố camera chừa ở mép mới là thay đổi giúp ô lưới rộng hơn trên máy màn dọc.
- Ảnh concept phục vụ dựng mẫu. Các vị trí đường, xe và cổng trong game lấy từ bàn 3D và dữ liệu màn.

<details>
<summary>Ảnh nguồn và prompt ImageGen tích hợp</summary>

Ảnh gốc tạo bằng ImageGen tích hợp, dựa trên chất đô thị vui nhộn của Crowd Express. Nguồn RGBA trong `Assets/_Project/Art/Environment/Town/`: `City_Shops_2D.png`, `City_Park_2D.png`, `City_Sidewalk_2D.png`. Unity cắt lề trong suốt khi import, giới hạn cạnh dài 1024 px cho hai dải ngang và 512 px cho dải dọc. [Crowd Express](https://play.google.com/store/apps/details?id=com.feelcrafters.buspuzzle&hl=vi)

Phần chung của ba prompt, đặt giữa câu mô tả asset và phần đặc tả riêng.

> Create original 2D environment art for Squeaky Lane, a sunny casual mobile car-wash puzzle. Orthographic overhead street plan, almost straight down (nearly 90 degrees), world aligned horizontal and vertical, one consistent upper-left noon light. Cheerful toy-town graphic shapes with subtle painted volume, generous breathing room, simple softened outlines, muted turquoise, apricot, butter yellow, mint and warm ivory. Medium-low contrast because saturated interactive cars will appear nearby. High production quality, clean silhouette at tiny screen scale. This is only a decorative transparent strip to overlay on a live 3D ground, so actual transparent RGBA background (also clear outer edges), keep small natural soft shadows attached to props. No unrelated game asset copying, no cars, people, letters, text, numbers, logo, interface, landscape horizon, road surface, lane markings, or rectangular opaque backdrop.

- **Cửa hàng ngang**. Use case: stylized-concept. Asset: horizontal north-side city block, landscape canvas about 3:1. Along one continuous horizontal warm ivory sidewalk plinth, build a small pastel teal flat-roof cafe with understated fabric awning, a smaller apricot corner shop, and a butter-yellow urban townhouse. Facing side along lower edge. Distinct top-down silhouettes. Restrained hints of life: two planters, one tiny cafe table and two umbrellas, street tree, patches of sidewalk between all buildings. Center the long shallow row and leave generous transparent padding all around. Shape should fill approximately 80% of width and 60% of height. This is level background art rendered as a flat decal.
- **Công viên ngang**. Use case: stylized-concept. Asset: horizontal south-side neighborhood pocket park, landscape canvas about 3:1. Design a low, shallow ribbon, containing a mint green pocket park with a compact curved planting bed, two round clean-canopy trees spaced out, one peach bench, one neat blue bicycle rack and a short cream pedestrian path, ending in a small terracotta planter. Light tactile shapes, large quiet intervals, all details grouped in three clusters on a narrow horizontal sidewalk island. Center the long shallow design, with clear alpha margins all around. Streets and vehicles are rendered separately in Unity.
- **Vỉa hè dọc**. Use case: stylized-concept. Asset: slender vertical side-street sidewalk, portrait canvas about 1:3. Draw a single loose vertical chain of three small separate details united by a very slim pale sidewalk strip: two top-view round shade trees and one little rounded apricot planter with a short mint bench beside it. Edges all transparent. Spacing is far greater than object diameter, a deliberately quiet street edge that reads even when only one edge is visible. Entire strip is strictly vertical, slender, with no buildings. Asset will decorate the outer edge of left and right roads.

</details>

## Giao Claude · Dựng khu phố cartoon bằng 3D

**Mục tiêu.** Giữ bãi trạm rửa là mảng lớn nhất trên màn. Khu phố ngoài bãi cần đọc được là một khu đô thị vui nhộn qua mái nhà, mái hiên và công viên nhỏ. Dùng [ảnh nhìn thẳng từ trên xuống](016-ref/city-3d-topdown.png) để bố trí. Lưới 6×6 trong ảnh là mẫu minh họa. Game vẫn dùng góc camera hiện tại khi chơi.

| Vị trí | Dựng gì | Khoảng trống cần giữ |
|---|---|---|
| Giữa | Bãi xám xanh, khay thấp bo bốn góc, vỉa hè sáng. Dùng kích thước lưới của từng màn. | Mặt bãi, đầu xe và lối qua cổng dễ đọc. |
| Trên, ngoài đường ngang | Ba khối mái thấp: quán xanh ngọc, tiệm màu đào, nhà góc phố vàng bơ. Cho mặt tiền hướng vào bãi, mỗi khối dùng hình mái hơi khác nhau. Mái hiên và chậu cây tập trung thành cụm nhỏ. | Chừa khe giữa các khối. Giữ khoảng trống phía đồng hồ và thông báo. |
| Dưới, ngoài đường ngang | Công viên nông, hai cây tán tròn cách nhau, một ghế, một bồn cây và lối dạo sáng. | Chừa dải nền thoáng sau booster. |
| Hai bên đường dọc | Một cụm chậu/cây thấp ở mỗi góc chéo nhau. | Giữ phần lớn chiều dài hai mép trống. Đường và lối ra của xe phải đọc được. |

**Hình khối và nét.** Ưu tiên khối hộp thấp có bevel (vát bo cạnh), tán cây tròn và bồn bo góc. Điểm bắt đầu khi dựng mẫu là bo khoảng 0,08–0,15 ô ở mái, bồn cây và mép vỉa hè. Dùng nét đen than `#24262B` mảnh trên viền ngoài mái, bồn, tán lá và cụm vỉa hè. Ước lượng độ dày 0,02–0,035 ô, chỉnh theo Game view. Chi tiết bên trong dùng đường tối nhẹ hơn. Cỏ, vạch đường và đường lưới dùng màu dịu để xe/cổng giữ ưu tiên thị giác.

**Đặt trong Unity.** Dựng prefab theo cụm cửa hàng, công viên và vỉa hè. Đo theo đơn vị ô bãi. Dùng giới hạn đường từ `SceneThemeView` và `UiTheme` để đặt props ngoài mép xa của đường, co theo số hàng/cột của màn. Giữ thiết lập vùng chơi/booster đã chỉnh. Theo dữ liệu màn để mở lối qua cổng và chừa thảm hầm rửa. Vỉa hè liền khối và đầu đường bo trong ảnh chỉ minh họa hình dáng. Trong game, giữ đường chạy ra khỏi khung như thiết lập hiện tại. Props trang trí dùng lớp trình bày và bỏ collider gameplay.

**Kiểm khi giao.** Chụp màn 23 ở Simulator iPhone 12 Pro Max để so độ đọc của lưới và sắc độ viền. Xem màn 3 để kiểm mặt tiền, thảm hầm và đường dọc. Thử một xe đi qua cổng và cua ra phố. Nếu mặt phố lấn HUD hoặc viền đen hút mắt hơn xe, giảm chiều cao props và độ dày viền trước.

**Trạng thái bàn giao.** Concept đã vẽ, chờ Claude dựng 3D. Sau khi gỡ nền sprite, đã chạy EditMode 585/584/0/1 và Play Console 0 lỗi, 0 cảnh báo. Simulator màn 23 dùng lại props 3D cũ, lưới 880 px và vùng chạm booster 255 px. Ảnh kiểm gỡ nền ở `Logs/016-shots/35-city-3d-restored-level23.png`.

<details>
<summary>Prompt ImageGen tích hợp cho ảnh bàn giao</summary>

> Use case: stylized-concept. Create a single original cartoon top-down environment design sheet for Squeaky Lane, a casual mobile car-wash puzzle. The image is a clear modeling reference for rebuilding the town in 3D. Camera is strict orthographic overhead, directly down at 90 degrees with zero tilt; vertical edges of buildings are invisible and their tops have no perspective distortion. Portrait 4:5 composition, centered, full bleed warm sage ground with a little breathing room around the whole map. Show exactly one main square medium-lavender slate parking-board placeholder in the center. It is the largest uninterrupted area, about 56% of total picture width, with a super simple subtle 6-by-6 tile grid and a slim pale rounded-corner rim. No objects inside the grid. Immediately outside that board, show a small neutral pale sidewalk apron and a box of connected gray city streets: two east-west streets and two north-south streets, simple clean four intersections around the board (north/south/east/west describing positions of the bordering streets, not four extra blocks). Roads are gray-blue with sparse cream dashes, intersections are clear. Keep road centerlines and apron easy to identify, with a clear passage out of each side of the board. Outside the upper street, place three separated low rounded rectangular roof masses: teal cafe on left, smaller apricot shop in middle, butter-yellow corner townhouse on right. Add awning strips and limited props, seen only from above. Outside the lower street, place a shallow pocket park with two spaced round-canopy trees, a rounded planting island, one small bench, and a sand path. Along vertical streets add two short offset sidewalk accents with a single planter or small tree each, and leave at least half the length of both side margins quiet. No building or decoration between board and street, no parked cars, no on-road obstacles. Rounded bevel-like corners on roofs, curbs, park islands and planters; a crisp thin near-black charcoal outline (#24262B) on the large outer silhouettes against the flat green ground, and much finer softer dark seams on inner details. Stylized cel shading with one gentle upper-left light, small contact shadows down-right, graphic clean shapes that can be modeled from primitive blocks, medium-low contrast on peripheral details. The board should remain the visual focus. This is an original concept diagram rendered as polished 2D game art. No HUD, labels, text, numbers, legend, arrows, logos, people, or inset panels.

</details>

## Bàn giao cho Codex (Claude sắp hết token)
**Chỗ cần sửa còn biết:** bãi nhỏ đi vì camera chừa phố (`UiTheme.streetVisible/streetInner/streetWidth`, `BoardView.FitCamera`) · hầm rửa cạnh trái/phải đè lên phố dọc · nhà phố chủ yếu thấy mái, bị HUD che (`SceneThemeView.SpawnTown`, `ToyTownTools`) · nút "XOAY" che xe (`TableButtons`) · chưa xem trong Play: đèn, trụ, portal, bùn mới, hộp Cài đặt, xe lùi ra cổng cua phố · chưa APK, chưa chốt 25 màn.
**Bẫy:** (1) ghi `.asset` qua MCP xong phải `Resources.UnloadAsset` + `AssetDatabase.ImportAsset(ForceUpdate)`, kiểm `IsDirty=false` và `git diff`. (2) Thoát Play trước khi sửa C#. (3) `execute_code` chỉ C# 6; ngay sau khi vào Play lệnh đầu hay lỗi, gọi lại. (4) Test Runner làm bẩn `Play.unity`: mở scene trống, `git checkout -- Assets/_Project/Scenes/Play.unity`, mở lại. (5) Bố cục menu/chọn màn sửa trong `MainMenuTools` rồi chạy menu `016 UI → Build Main Menu`, không sửa tay. (6) Chụp ảnh: focus cửa sổ Simulator trước, hẹn giờ `ScreenCapture` bằng `EditorApplication.update`. (7) Shader Toy Lit dùng chung: `_TopTint` mặc định 1, chỉ material xe bật. (8) Không commit file S4 chưa theo dõi trong `Art/UI/City` và `Prefabs/Environment/*` (props 0 đỉnh).

## Trao đổi
2026-11-26 10:08 · Claude → Người làm, Codex · [BÁO]: mở nhánh `016-toy-world` từ `015-toy-look`; giao Codex 016-a…d chạy song song (vùng file tách riêng).
2026-11-26 10:30 · Claude → Người làm · [BÁO]: 016 đã chạy trong game, chờ chơi thử.
2026-11-26 10:39 · Claude → Codex · [BÀN GIAO]: nhánh `016-toy-world`, code ở 6478360, Run All 585/584/0/1, Console sạch. Còn việc ở mục 'Bàn giao cho Codex'.
2026-11-26 11:15 · Codex → Người làm · [BÁO]: hoàn tất nút xoay và hộp Cài đặt trong 016; Run All 585/584/0/1, Play Console sạch, ảnh Simulator ở mục Đã làm. Chờ người làm xem.
2026-11-26 11:33 · Codex → Người làm · [BÁO]: ô lưới màn 23 rộng hơn 8,7%, booster thu 18%, đã gắn nền đô thị 2D. Run All 585/584/0/1 và Console Play sạch, ảnh Simulator ở mục Đã làm.
2026-11-26 11:40 · Codex → Người làm · [BÁO]: gỡ gắn nền sprite, giữ vùng chơi và booster đã chỉnh. Đã để ảnh top-down và hướng dẫn dựng 3D cho Claude trong phiếu 016.
2026-11-26 11:51 · Claude → Người làm, Codex · [BÁO]: nhận việc dựng phố 3D; giao Codex phiếu 016-e (chưa nối vào game, chờ người làm duyệt ảnh).
2026-11-26 14:06 · Claude → Người làm · [BÁO]: sửa âm thanh (người làm nghe rè/lặp): bỏ tiếng bước (clip 1 s ~20 tiếng lách tách phát mỗi ô), 8 tiếng mới tự tổng hợp `tools/make-sfx.py` (xe chạy, 2 tiếng rửa, cổng tường, bùn, kẹt, đếm ngược, thua), mỗi loại một bản + tắt dần `sfxFadeSeconds` 0,05, tiếng rửa phát lúc vào trạm. Run All 591/590/0/1; Play màn 2–3: tối đa 2 tiếng cùng lúc, Console 0 lỗi. Chưa nghe trên máy.
2026-11-26 14:52 · Claude → Người làm · [BÁO]: kẹt xe báo bằng viền đỏ nhấp nháy (bỏ chữ); level_order còn 12 màn 1,2,3,4,5,6,8,10,20,21,29,19 (bản đủ ở c3ef1bc; bỏ 9 barie và 22–23 chốt; chưa màn nào có cọc tiêu). EditMode 576/575/0/1; Play Simulator: màn 20 kẹt → viền sáng-mờ, chơi lại thì tắt, Console sạch (trừ cảnh báo PlayerLoop do lệnh chụp ảnh). Chưa thử máy thật.
