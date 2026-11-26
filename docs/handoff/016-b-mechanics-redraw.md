# 016-b · Vẽ lại toàn bộ cơ chế kiểu Voodoo/Rollic (phiếu nhỏ cho Codex)     Cỡ: L (≤ một buổi)     Trạng thái: Xong (Claude review)
Đang cầm: Claude        AI tham gia: Claude (giao, chạy menu, đổi catalog, review), Codex (tool Editor dựng prefab)
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

## Đã làm · Tệp đổi · Kiểm chứng

- Viết `Assets/_Project/Scripts/Editor/ToyMechanicsTools.cs`. Unity tự sinh file `.cs.meta` đi kèm trong lúc làm việc. Cập nhật phiếu này để bàn giao.
- Menu `Tools/Squeaky Lane/016 Toy/Create Toy Mechanics` dựng 14 prefab mới, gồm sáu cổng màu, cọc, đèn, bàn xoay, trụ, ba portal và vũng bùn. Đầu ra của menu gồm 18 mesh trong `Art/Mechanics/Toy/Meshes`, bốn material và hai atlas trong `Art/Mechanics/Toy`, prefab trong `Prefabs/Mechanics/Toy`, ảnh `Logs/016-mechanics-preview.png` nền nhựa đường với camera 58° và `Car_blue`. Các lần chạy sau đối chiếu nội dung trước khi ghi và cập nhật asset tại chỗ khi có thay đổi.
- **Đã kiểm tĩnh** bằng Roslyn C# 9 với assembly của Unity 2022.3.62f2 và cảnh báo được tính là lỗi, kết quả 0 lỗi và 0 cảnh báo. Tính theo số đoạn mesh và giá trị catalog hiện tại, trụ là prefab nặng nhất với tối đa 1.346 tam giác. Vật cao nhất tối đa khoảng 0,895 ô, bàn xoay dày 0,06 ô và bùn cao tối đa 0,035 ô. Đã đối chiếu các tham chiếu đèn, pivot trụ, alpha che mờ và root với `MechanicView`/`BoardView`. Menu kiểm `Usable`, cấu hình che mờ, số tam giác và chiều cao trên prefab trước và sau khi lưu.
- **Chưa chạy Unity** theo yêu cầu của lượt giao. Chưa có prefab/mesh/material sinh bởi menu hoặc ảnh preview. Claude chạy menu hai lần để so byte/GUID, nối catalog, kiểm EditMode, Console, Play và ảnh V3. Đặc biệt kiểm taxi thoát qua barie ở góc chơi và thử che mờ cổng/trụ/portal. Mức tự tin ~90% cho mã và kiểm tĩnh, hình cuối chờ V3. `PROJECT.md` chưa cần đổi ở lượt này vì catalog đang dùng chưa được chuyển sang bộ mới.

## Bạn cần hiểu

- Một atlas màu chứa cả sáu màu cổng theo `VisualPalette` và ba cặp màu portal từ catalog. UV chọn màu khi dựng mesh nên các prefab dùng chung material.
- `MechanicView` bật một trong hai trạng thái đèn, còn `Moving Part` quay độc lập trên tâm đế trụ. Cổng và portal đổi sang material mờ dựng sẵn qua `MechanicOcclusion`.

## Phản đối kế hoạch

- Cần ngang ở khoảng 0,6 ô có thể xuyên nóc vì xe giữ kích thước khi đi qua cổng. Collider taxi cao đến khoảng 0,78 ô tính từ mặt bãi. Chừa mép dưới cần ở khoảng 0,81 ô, vẫn giữ toàn bộ cổng dưới 0,9 ô. Claude cần kiểm hình ở đoạn xe thoát, nhất là lúc thân xe nghiêng.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 10:30): APPROVE
- Đã soi: menu chạy 2 lần giữ nguyên byte; 14 prefab `Usable`, cao ≤ 0,9, ≤ 1 346 tam giác, 0 collider; ảnh preview; trong game cổng/bàn xoay.
- [NÊN → Claude đã xử lý] cổng cạnh trái/phải nhìn nghiêng chỉ còn một vạch mảnh nên khó đọc màu: bật dải màu trên nền (`UiTheme.gateStrips`, sâu 0,5).
- Chưa soi trong Play: đèn giao thông, trụ xoay, portal, vũng bùn mới.

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-b (tool dựng lại prefab cơ chế kiểu đồ chơi).
2026-11-26 10:20 · Codex → Claude · [BÀN GIAO]: bàn giao tool 016-b trên `016-toy-world` (HEAD `55c3d85`). Roslyn sạch và ngân sách mesh đã đối chiếu tĩnh. Còn chạy menu hai lần, nối catalog và kiểm V2/V3, gồm khoảng hở của cần barie lúc xe thoát.
2026-11-26 10:30 · Claude → Người làm · [BÁO]: đã nối vào game trên `016-toy-world`.
