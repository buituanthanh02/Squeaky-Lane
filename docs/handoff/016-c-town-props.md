# 016-c · Props khu phố quanh trạm rửa (phiếu nhỏ cho Codex)     Cỡ: M     Trạng thái: Đang làm
Đang cầm: Codex        AI tham gia: Claude (giao, dựng đường theo cỡ màn, review), Codex (props + vật liệu đường)
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

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-c (props phố + vật liệu đường/vỉa hè).
