# 016-c · Props khu phố quanh trạm rửa (phiếu nhỏ cho Codex)     Cỡ: M     Trạng thái: Xong (Claude review)
Đang cầm: Claude        AI tham gia: Claude (giao, dựng đường theo cỡ màn, review), Codex (props + vật liệu đường)
Liên quan: 015-b (shader `Toy Lit`, `Toy Ground`, props cây/bụi ở `Prefabs/Environment/Toy`). Nhánh `016-toy-world`.

## Mục tiêu (người chơi)
Bãi hiện như "đồng không mông quạnh có mỗi chỗ rửa". Người làm muốn **trạm rửa nằm giữa khu phố**: quanh khay bãi có **đường nhựa vòng quanh** (Claude dựng mesh đường theo cỡ màn), bên ngoài đường là **nhà/cửa hàng nhỏ, đèn đường, cây**, kiểu thị trấn đồ chơi (ảnh game đỗ xe mẫu: đường xám có vạch, vỉa hè kem, nhà khối bo tròn mái màu).

## Phạm vi — Làm (chỉ code + kiểm tĩnh; **không gọi Unity MCP/CLI**)
File mới `Assets/_Project/Scripts/Editor/ToyTownTools.cs`, menu `Tools/Squeaky Lane/016 Toy/Create Toy Town Props` (idempotent). Prefab ở `Assets/_Project/Prefabs/Environment/Town/`, mesh/texture/material ở `Assets/_Project/Art/Environment/Town/`. Dùng shader `SqueakyLane/Toy Lit` (material chung, ≤ 8 material cho cả bộ). Gốc 0/0/1, đáy y = 0, không collider, shadow Off.
1. **Vật liệu đường** `M_ToyRoad.mat` + texture `road_toy.png` 256×256 tileable theo trục U dọc đường: nhựa xám ấm `#5B6170`, **vạch giữa đứt trắng kem**, mép đường có viền sáng mảnh; texture dùng cho mesh đường Claude dựng (UV: U chạy dọc đường theo mét, V ∈ [0,1] ngang đường). Và `M_ToySidewalk.mat` (kem `#EFE3CC`, ô gạch nhạt) cho vỉa hè.
2. **Nhà/cửa hàng** (mỗi cái ~1,6–2,2 ô rộng, cao ≤ 1,4 ô — nằm ngoài bãi nên được cao hơn cơ chế): `TownHouse_A` (nhà mái tam giác đỏ cam), `TownHouse_B` (nhà mái xanh), `TownShop` (cửa hàng mái hiên sọc), `TownGarage` (gara nhỏ cửa cuốn). Khối bo cạnh, cửa sổ xanh nhạt, màu kẹo nhưng nhạt hơn xe để không tranh màu.
3. **Phụ kiện**: `TownLamp` (đèn đường bo tròn, cao ≤ 1,0), `TownBench`, `TownHydrant` (trụ cứu hỏa đỏ), `TownSign` (biển "rửa xe" hình giọt nước, không chữ).
4. Ảnh `Logs/016-town-preview.png`: một đoạn đường thẳng 6 ô + vỉa hè, các nhà/phụ kiện hai bên, camera trực giao 58°, ánh sáng như 015-b.

**Không làm:** mesh đường theo cỡ màn (Claude), sửa `SceneThemeView`/`UiTheme`, scene, package/settings, commit, gọi Unity.

## Tiêu chí xong (Claude đo)
Roslyn 0 lỗi; menu 2 lần byte/GUID giữ; mỗi prefab ≤ 1 200 tam giác, ≤ 3 material; texture đường liền mép theo U; ảnh preview đọc ra "phố đồ chơi".

## Điều dễ sai
Texture đường không liền mép (thấy đường nối) · nhà quá rực tranh màu xe · tạo material mỗi nhà · nhà cao che bãi khi camera nghiêng (Claude đặt nhà ở mép xa, nhưng giữ ≤ 1,4).
**Cắt nếu trễ:** `TownBench`, `TownHydrant`, `TownGarage`.

--- AI làm điền khi xong ---

## Đã làm · Tệp đổi · Kiểm gì

- Thêm `Assets/_Project/Scripts/Editor/ToyTownTools.cs` và `.meta` do Unity tự sinh. Menu `Tools → Squeaky Lane → 016 Toy → Create Toy Town Props` tạo tám prefab ở `Prefabs/Environment/Town/`, mesh, ba texture và ba material ở `Art/Environment/Town/`, rồi xuất `Logs/016-town-preview.png` từ preview riêng. Preview dùng cây/nền sẵn có của 015-b, camera trực giao 58° và ánh sáng 015-b.
- Mỗi prefab sinh từ một mesh, một material slot, root mặc định 0/0/1, đáy ở y = 0, không collider, đổ/nhận bóng đều tắt. Dùng chung `M_ToyTownProps.mat` với atlas `town_palette.png`. Menu giữ asset đã tồn tại qua lần chạy lại và chỉ sửa importer khi cấu hình lệch.
- **Kiểm tĩnh:** Roslyn C# 9 với assemblies Unity 2022.3.62f2, 0 lỗi/cảnh báo. Kiểm công thức sinh hình với toán học .NET độc lập: A/B/Shop/Garage/Lamp/Bench/Hydrant/Sign lần lượt 824/644/1080/780/300/660/408/368 tam giác, có diện tích dương ở mọi mặt và thể tích có dấu dương. Bốn nhà rộng 2/2/2,18/2 ô, cao 1,4/1,4/1,13/0,975 ô. Đèn cao 0,92 ô.
- **Kiểm tĩnh texture:** hai cột biên U của đường trùng 256/256 pixel, mép V dùng Clamp. Hai cặp biên vỉa hè trùng 128/128 pixel mỗi trục. Byte PNG đã import và mipmap thực tế chờ kiểm trong Editor.

## Giả định · Chưa kiểm

- `M_ToyRoad.mat` lấy U tăng một đơn vị trên mỗi mét và V = 0..1 từ mép này sang mép kia. Vạch đứt trắng kem lặp mỗi mét. `M_ToySidewalk.mat` dùng UV phẳng lặp hai trục, preview dùng một chu kỳ trên 0,8 ô. Mặt tiền prefab theo local +Z.
- **Chưa chạy:** menu, đo byte/GUID qua hai lần chạy, kiểm Console và shader trong Editor, xem ảnh preview, Play, Stats và APK. Claude chạy menu rồi lấy material và prefab để bố trí vòng đường theo cỡ màn. `PROJECT.md` chưa cần cập nhật ở lượt viết mã vì phố chưa gắn vào cảnh chơi.
- **Tự đánh giá:** 90% cho phạm vi mã và kiểm tĩnh. Cần xem vạch giữa ở góc cua và mật độ nhà quanh bãi trong Game view, nhất là màn 16.

## Bạn cần hiểu

- UV trục U đo theo chiều dài đường nên mesh dài hơn tự lặp thêm vạch. UV trục V luôn phủ hết bề ngang để hai viền sáng bám hai mép.
- Các mảng màu của prop dùng UV trỏ vào tâm texel trong một atlas lọc Point. Mỗi mẫu chỉ cần một submesh và một material slot, dù mái, kính và tường có màu khác nhau.

## Phản đối kế hoạch

- Màn 16 ở 015 đã ước tính khoảng 192 lượt vẽ cả UI. Nếu mỗi prop mới dùng 3 material slot, chỉ vài prop lặp đã có thể vượt mốc 200. Dùng một atlas màu chung và một slot cho mỗi prop. Claude đo lại Stats sau khi rải props để quyết mật độ.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 10:30): APPROVE
- Đã soi: menu 2 lần giữ byte; kích thước prefab; trong game (nhà, đèn, ghế, trụ cứu hỏa, đường).
- [GỢI Ý] nhìn từ 58° nhà chủ yếu thấy mái, một phần bị HUD che; chờ người làm xem. Ảnh preview thiếu nền ở nửa xa (chỉ là ảnh xem trước).

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-c (props phố + vật liệu đường/vỉa hè).
2026-11-26 10:15 · Codex → Claude · [BÀN GIAO]: xong mã menu 016-c trên 016-toy-world (HEAD 55c3d85), Roslyn sạch, đã kiểm giới hạn mesh và mép texture. Claude chạy menu hai lần để so byte/GUID, xem preview, tích hợp vòng đường và đo Stats.
2026-11-26 10:30 · Claude → Người làm · [BÁO]: đã nối vào game trên `016-toy-world`.
