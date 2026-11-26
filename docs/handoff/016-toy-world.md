# 016 — Thế giới đồ chơi: trạm rửa giữa phố, xe mảng đậm nhạt, cơ chế vẽ lại, chọn màn, main menu     Cỡ: L (chia 6 phần)     Trạng thái: Chờ người làm chơi thử
Đang cầm: Codex        AI tham gia: Claude (code, nối, kiểm), Codex (016-a…d: shader, cơ chế, phố, ảnh IP)
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

## Bàn giao cho Codex (Claude sắp hết token)
**Chỗ cần sửa còn biết:** bãi nhỏ đi vì camera chừa phố (`UiTheme.streetVisible/streetInner/streetWidth`, `BoardView.FitCamera`) · hầm rửa cạnh trái/phải đè lên phố dọc · nhà phố chủ yếu thấy mái, bị HUD che (`SceneThemeView.SpawnTown`, `ToyTownTools`) · nút "XOAY" che xe (`TableButtons`) · chưa xem trong Play: đèn, trụ, portal, bùn mới, hộp Cài đặt, xe lùi ra cổng cua phố · chưa APK, chưa chốt 25 màn.
**Bẫy:** (1) ghi `.asset` qua MCP xong phải `Resources.UnloadAsset` + `AssetDatabase.ImportAsset(ForceUpdate)`, kiểm `IsDirty=false` và `git diff`. (2) Thoát Play trước khi sửa C#. (3) `execute_code` chỉ C# 6; ngay sau khi vào Play lệnh đầu hay lỗi, gọi lại. (4) Test Runner làm bẩn `Play.unity`: mở scene trống, `git checkout -- Assets/_Project/Scenes/Play.unity`, mở lại. (5) Bố cục menu/chọn màn sửa trong `MainMenuTools` rồi chạy menu `016 UI → Build Main Menu`, không sửa tay. (6) Chụp ảnh: focus cửa sổ Simulator trước, hẹn giờ `ScreenCapture` bằng `EditorApplication.update`. (7) Shader Toy Lit dùng chung: `_TopTint` mặc định 1, chỉ material xe bật. (8) Không commit file S4 chưa theo dõi trong `Art/UI/City` và `Prefabs/Environment/*` (props 0 đỉnh).

## Trao đổi
2026-11-26 10:08 · Claude → Người làm, Codex · [BÁO]: mở nhánh `016-toy-world` từ `015-toy-look`; giao Codex 016-a…d chạy song song (vùng file tách riêng).
2026-11-26 10:30 · Claude → Người làm · [BÁO]: 016 đã chạy trong game, chờ chơi thử.
2026-11-26 10:39 · Claude → Codex · [BÀN GIAO]: nhánh `016-toy-world`, code ở 6478360, Run All 585/584/0/1, Console sạch. Còn việc ở mục 'Bàn giao cho Codex'.
