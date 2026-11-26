# 015-a · Cổng rửa dạng đường hầm cho cổng nước (phiếu nhỏ cho Codex)     Cỡ: M (≤ nửa ngày)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Claude (giao, chạy menu, nối vào game, review), Codex (viết tool Editor)
Liên quan: 015-toy-look bước 1; 014-c (trạm rửa kẹo: `WashVfxTools.cs`, `M_WashRoller`, `M_WashPost`, `M_WashPad`, `M_Bubble`). Nhánh: `015-toy-look`.

## Mục tiêu (người chơi)
Cổng nước ở mép bãi trông như **một đường hầm rửa xe kiểu đồ chơi**: hai chổi đứng sọc kẹo hai bên, một chổi ngang trên nóc, khung vòm vàng, thảm bong bóng chạy ra ngoài bãi. Xe dính bùn **chạy xuyên hẳn qua hầm** ra thảm bên ngoài, chổi quay + phun bọt, rồi lùi lại vào bãi. Nhìn từ camera nghiêng 58° trên điện thoại phải đọc ra "chỗ rửa xe, đi qua được".

## Phạm vi — Làm (chỉ vùng `AGENTS.md` §1.3)
**Một file Editor mới** `Assets/_Project/Scripts/Editor/WashGateTools.cs` (không sửa `WashVfxTools.cs`/`CarVfxTools.cs`/`LotMechanicsWindow.cs`), menu `Tools/Squeaky Lane/015 Toy/Create Wash Gate`. **Idempotent** (chạy 2 lần: byte/GUID không đổi), tọa độ cố định (không Random). **Bạn chỉ viết code + kiểm tĩnh (Roslyn). Không gọi Unity MCP/CLI**: Claude chạy menu trong Editor.

Prefab mới `Assets/_Project/Prefabs/Mechanics/WashGate_0.prefab` (không đụng `WaterGate_0.prefab`). Dùng lại material 014-c: `M_WashRoller` (chổi sọc), `M_WashPost` (vàng), `M_WashPad` (thảm bong bóng), `M_Bubble` (hạt). Được tạo **tối đa 1 material mới** `M_WashGateArch` (Standard, trắng kem `#FFF4E0` hoặc hồng `#FF5C8A`, smoothness .5) nếu cần cho vòm. Mesh mới lưu dưới `Assets/_Project/Art/VFX/Meshes/WashGate_*.asset`.

### Hệ trục prefab (quan trọng)
- Gốc (0,0,0) = **tâm mép bãi** tại ô cổng; y = 0 là mặt bãi. **+Z hướng ra ngoài bãi**, X là bề ngang làn (1 ô, xe rộng 0,78). Transform gốc 0/0/1. Claude tự xoay prefab cho 4 cạnh.
- Xe cao tới y = 0,82 (taxi), xe thường 0,72–0,75; xe tải dài 2,82 ô.

### Thành phần (đặt tên object chính xác, Claude tìm theo tên)
- `Pad`: thảm bong bóng mỏng (y = 0,012) từ z = 0 tới z = **2,0**, rộng 0,92, **đầu ngoài bo tròn** (bán kính 0,46), dùng `M_WashPad`. Viền mép thảm có gờ mỏng màu vàng (`M_WashPost`) cao 0,03 để thảm đọc ra "đường dẫn".
- Khung hầm từ z = 0,05 tới z = 0,95 (sâu 0,9):
  - `Post FL`, `Post FR`, `Post BL`, `Post BR`: 4 trụ tròn mập vàng bán kính 0,07 tại x = ±0,52, z = 0,05 / 0,95, cao 0,92 (`M_WashPost`).
  - `Arch Front`, `Arch Back`: 2 vòm/xà bo tròn nối đỉnh trụ cùng z, đáy vòm **≥ 0,90**, đỉnh ≤ **1,05**. Trên `Arch Front` (phía bãi) có tấm biển nhỏ hình giọt nước/bong bóng nổi (chỉ hình, không chữ).
- Chổi (mỗi cái là **root rỗng `Roller …` + một con tên `Spin`**; Spin quay quanh **trục X local của chính Spin**, pivot đúng tâm, mesh đối xứng quanh trục đó, texture sọc chéo quanh chu vi như 014-c):
  - `Roller L` tại (−0,47; 0,45; 0,5) và `Roller R` tại (+0,47; 0,45; 0,5): **chổi đứng** (trục dài theo Y thế giới ⇒ xoay root sao cho X local của Spin trùng Y), bán kính 0,07, dài 0,72 (y 0,09 … 0,81). Mép trong ở |x| = 0,40 ⇒ **không lấn vào |x| < 0,40** (xe 0,39).
  - `Roller Top` tại (0; 0,90; 0,5): chổi ngang theo X, bán kính 0,06, dài 0,86. **Đáy chổi ≥ 0,84** (trên nóc taxi).
- `Spray L`, `Spray R`, `Spray Top`: 3 ParticleSystem trong hầm (z = 0,5), phun bọt vào giữa làn; cấu hình **giống `Spray N` của `WashStation_0`** (014-c): `M_Bubble`, cỡ 0,10–0,16, tuổi 0,5–0,7 s, `maxParticles` 16, `playOnAwake` tắt, `loop` tắt, duration 0,9, rate 24/s, World, Billboard, shadow Off; `Spray Top` phun xuống, hai bên phun vào trong và hơi lên.
- `Drip`: (cắt được) 1 ParticleSystem giọt nước nhỏ rơi từ `Arch Back` khi phun, cùng giới hạn trên, max 8.
- Mọi renderer: **không collider**, `shadowCastingMode` Off, probes Off. Ngân sách: **≤ 2 500 tam giác** toàn prefab, tối đa 5 material.
- Gắn sẵn **`MechanicOcclusion`** giống cổng nước cũ không cần: Claude lo che khuất. Không gắn script nào.

### Ảnh xem trước
`Logs/015-wash-gate-preview.png`: góc nghiêng 58° và nhìn ngang, trên nền nhựa đường `#3A3F4B`, mép bãi ở z = 0; một hộp xe `#35C3FF` 0,78 × 0,75 × 1,6 nằm **giữa hầm** (tâm z = 0,5) để kiểm chổi không đè xe và chổi trên nằm trên nóc. Dùng preview scene tạm như 014-c.

**Không làm:** nối vào BoardView/ActionAnimator/MechanicCatalog/scene (Claude làm); sửa `WashStationView`; Core/Game/Presentation/`Play.unity`; package/settings; tải ngoài; commit; Play.

## Tiêu chí xong (Claude đo lại bằng MCP) · V2
1. Roslyn 0 lỗi; Claude chạy menu 2 lần: byte/GUID mọi file giữ nguyên; asset 014-a/b/c không đổi byte.
2. Đúng tên object; 3 `Spin` quay quanh X local không nhảy hình; không phần nào lấn vào hộp |x| < 0,40, 0 < y < 0,84 trong vùng hầm; cao ≤ 1,05; ≤ 2 500 tam giác; không collider.
3. ParticleSystem đúng cấu hình (playOnAwake off, loop off, World, max 16, `M_Bubble`).
4. Ảnh preview đọc ra "hầm rửa xe đi qua được", tông kẹo như trạm 014-c.

## Điều dễ sai
Trục +Z ngược (thảm nằm trong bãi) · chổi đứng không xoay đúng trục (quay quanh trục ngang) · chổi trên thấp đè nóc xe · pivot Spin lệch · tạo material hạt mới · random không cố định · hạt loop · prefab có collider · sửa file 014.
**Cắt nếu trễ:** `Drip`, biển trên vòm, gờ vàng mép thảm, ảnh nhìn ngang.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Kiểm gì · Chưa kiểm

- Thêm `Assets/_Project/Scripts/Editor/WashGateTools.cs` với menu `Tools/Squeaky Lane/015 Toy/Create Wash Gate`. Menu tạo `WashGate_0.prefab`, năm mesh `WashGate_{Pad,Rim,Post,Arch,Roller}.asset`, và material vòm kem `M_WashGateArch` (#FFF4E0, smoothness 0,5). Có gờ vàng, biển bong bóng nổi, ba chổi, ba vòi bọt và `Drip`. Ảnh PNG ghép góc 58° và nhìn ngang được render bằng preview scene tạm.
- Đã kiểm tĩnh: Roslyn của Unity 2022.3.62f2 với tham chiếu assembly Editor và `/warnaserror+` báo 0 lỗi, 0 cảnh báo. Đối chiếu Git với `4baa90e` cho thấy asset VFX/cơ chế và các tool tham chiếu không có diff. Tool dựng 1.717 tam giác mesh và dùng 5 material theo mã nguồn. Ba vòi tối đa 48 hạt cộng 8 hạt `Drip`. Không có collider hay script gắn vào prefab từ tool.
- Chưa chạy Unity: menu hai lần để so byte/GUID, import `.meta`, Console, prefab thực tế, giới hạn renderer/hạt, quay `Spin`, ảnh PNG, tích hợp và Play. Claude chạy menu và kiểm bậc V2. Các thay đổi scene, runtime và test có sẵn/song song thuộc phần Claude.
- Giả định: tiêu chí khoảng hở áp cho vật cản phía trên mặt thảm. Mặt `Pad` cao 0,012 theo phiếu. Trong hầm, gờ bắt đầu tại |x| = 0,425, chổi đứng chạm biên lý thuyết |x| = 0,40 và đáy chổi trên ở y = 0,84. Khi đo bằng float, dùng dung sai cho hai biên chổi. Tự tin khoảng 90% về phạm vi và hình học trong mã nguồn. Hình ảnh và trạng thái sau import còn chờ Editor.

## Bạn cần hiểu

- Mesh chổi dựng dọc X và đặt pivot giữa. Root của hai chổi bên xoay 90° quanh Z để X local của `Spin` trùng Y theo prefab, nên Claude dùng cùng một trục X local cho cả ba chổi.
- Lần chạy sau dùng lại prefab/mesh/material đã lưu để giữ GUID và tinh chỉnh Inspector. PNG chỉ ghi khi byte ảnh khác. Nếu cần tạo lại hình học sau khi đã chạy menu, Claude cập nhật asset trong Editor để giữ tham chiếu.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 09:13): APPROVE
- Đã soi: chạy menu 2 lần (14 file mới giữ nguyên byte, asset 014 không đổi); 1.717 tam giác, cao 1,05, 0 collider, 4 hệ hạt, 3 chổi nhận đúng trục; ảnh preview đọc ra hầm rửa; Console 0 lỗi sau import. Claude gắn `MechanicView(WashGate)` + `WashStationView` vào prefab, thêm dòng catalog kind 10.
- Chưa soi: hạt bọt chạy thật trong Play không bước tay (ảnh chụp lúc Editor dừng), máy thật/fps.
- [GỢI Ý] tấm biển bong bóng trên vòm nhìn từ 70° gần như không thấy; để nguyên.

## Trao đổi
2026-11-26 09:04 · Claude → Codex · [BÀN GIAO]: giao 015-a (tool Editor tạo `WashGate_0`). Codex chỉ viết code + kiểm tĩnh; Claude chạy menu, nối vào game.
2026-11-26 09:09 · Codex → Claude · [BÀN GIAO]: đã viết `WashGateTools.cs` trên `015-toy-look` HEAD `4baa90e`, Roslyn 0 lỗi/0 cảnh báo. Claude nhận gậy để chạy menu, kiểm byte/GUID sau hai lượt, preview/Console/prefab và nối game. Unity chưa chạy từ phía Codex.
2026-11-26 09:13 · Claude → Người làm · [BÁO]: đã nối WashGate_0 vào game, APPROVE; ảnh `Logs/015-shots/01..03`.
