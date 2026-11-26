# 016-b · Vẽ lại toàn bộ cơ chế kiểu Voodoo/Rollic (phiếu nhỏ cho Codex)     Cỡ: L (≤ một buổi)     Trạng thái: Đang làm
Đang cầm: Codex        AI tham gia: Claude (giao, chạy menu, đổi catalog, review), Codex (tool Editor dựng prefab)
Liên quan: 011 (`Scripts/Editor/LotMechanicsWindow.cs` dựng prefab cơ chế P1), 015-b (shader `SqueakyLane/Toy Lit`, material props chung). Nhánh `016-toy-world`.

## Mục tiêu (người chơi)
Cơ chế trên bãi đang là khối thô (hộp, trụ). Người làm muốn **vẽ lại hết** theo phong cách game giải đố đỗ xe của Voodoo/Rollic (Parking Jam 3D, Car Out, ảnh mẫu người làm gửi: barie sọc đỏ trắng, cọc tròn mập, màu kẹo bão hòa, khối bo tròn, bóng nhựa). Đọc luật ngay từ dáng: nhìn là biết cổng màu nào, cọc chặn, đèn đỏ/xanh, bàn xoay xoay được, trụ/portal dịch chuyển.

## Phạm vi — Làm (chỉ viết code + kiểm tĩnh; **không gọi Unity MCP/CLI**, Claude chạy menu)
Một file Editor mới `Assets/_Project/Scripts/Editor/ToyMechanicsTools.cs`, menu `Tools/Squeaky Lane/016 Toy/Create Toy Mechanics` (idempotent, không Random). Đầu ra **prefab mới** ở `Assets/_Project/Prefabs/Mechanics/Toy/` (không sửa prefab cũ ở `Prefabs/Mechanics/`), mesh ở `Assets/_Project/Art/Mechanics/Toy/Meshes/`, material ở `Assets/_Project/Art/Mechanics/Toy/` (dùng shader `SqueakyLane/Toy Lit`; tối đa ~10 material dùng chung + 6 màu cổng; không tạo material lúc chạy).
**Giữ đúng hợp đồng prefab** như `LotMechanicsWindow.BuildPrefab` (đọc kỹ): root 0/0/1, `MechanicView.Configure(kind, movingPart, red, green)`, không collider, `MechanicView.Usable(prefab, kind)` phải true; cổng/portal gắn `MechanicOcclusion` như `ConfigureOcclusion` (bộ material mờ dựng sẵn). Kích thước chiếm chỗ giống bản cũ (đọc `MechanicCatalog.Design`: cổng rộng 1, cao ≤ .9; cọc cao .45; bàn xoay đường kính 1.9; trụ cao ≤ .9). Vật cao ≤ 0,9 ô (camera 58° vẫn thấy xe sau).
Danh sách (tên prefab = `<Kind>_<variant>.prefab` như cũ):
1. `ColorGate_0..5`: **barie đồ chơi**: hai trụ bo tròn màu xe + cần barie sọc màu xe/trắng nằm ngang phía trên làn (cao ~.6), bảng tròn trên trụ in **icon xe** màu cổng; mặt đất trước cổng không cần (Claude có dải sơn). Màu theo `VisualPalette` (`Config/VisualPalette.asset`, `palette.Car(TileColor)`).
2. `Cone_0`: cọc giao thông mập, cam `#FF7A1A` 2 sọc trắng, đế vuông bo tròn đen nhạt; cao .45.
3. `TrafficLight_0`: cột bo tròn + hộp đèn mập 2 bóng to; con `red` và `green` (hai GameObject trạng thái như bản cũ: đèn sáng = màu tươi + quầng phát sáng giả bằng quad, đèn tắt = xám); vạch dừng trên mặt đất.
4. `Turntable_0`: đĩa tròn nổi dày .06, viền sáng, 2 mũi tên cong lớn chạy vòng (màu trắng trên nền xanh dương `#35C3FF`), có gờ bánh răng ở viền để đọc ra "xoay".
5. `Pylon_0`: cửa dịch chuyển xoay: vòng cổng mập trên đế tròn, `movingPart` là phần quay (giữ giống bản cũ về trục/pivot).
6. `Portal_0..2`: cổng vòm phát sáng theo 3 màu `MechanicCatalog.Design.portalColors`, xoáy bên trong (mesh đĩa xoắn), viền dày.
7. `Mud_0`: vũng bùn bóng, viền lượn sóng, vài bong bóng bùn nổi, nâu `#6B4226`/`#8A5A33`, dẹt (cao ≤ .04).
8. `WaterGate_0` không cần (đã thay bằng hầm rửa 015). `Surface_0`, `WashStation_0` giữ nguyên.
Ảnh preview `Logs/016-mechanics-preview.png`: tất cả prefab mới trên nền `M_ToyGround` hoặc nhựa đường `#3A3F4B`, camera trực giao 58°, kèm một xe `Car_blue` cạnh để so cỡ.

**Không làm:** đổi `MechanicCatalog.asset` (Claude đổi), sửa BoardView/MechanicView/code Presentation, prefab cũ, scene, package/settings, commit, gọi Unity.

## Tiêu chí xong (Claude đo) · V2/V3
1. Roslyn 0 lỗi; Claude chạy menu 2 lần: byte/GUID không đổi; asset cũ không đổi.
2. Mọi prefab `Usable == true`; 0 collider; cao ≤ .9; mỗi prefab ≤ 1 500 tam giác.
3. Claude đổi catalog sang prefab mới: EditMode xanh, Play màn có đèn/bàn xoay/trụ/portal không lỗi; ảnh đọc ra từng cơ chế.

## Điều dễ sai
Thiếu `red`/`green` hoặc `movingPart` → `Usable` false · pivot `movingPart` lệch nên trụ quay lắc · cổng che xe không có che mờ · tạo material mỗi prefab (vỡ batch) · cao quá che xe · màu cổng lệch palette.
**Cắt nếu trễ (thứ tự):** Portal, Pylon, quầng đèn, gờ bánh răng.

--- AI làm điền khi xong ---

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-b (tool dựng lại prefab cơ chế kiểu đồ chơi).
