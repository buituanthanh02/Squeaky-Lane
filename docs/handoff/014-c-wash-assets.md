# 014-c · Asset rửa xe hoạt hình: vẩy bùn + trạm rửa (phiếu nhỏ cho Codex)     Cỡ: M (≤ nửa ngày)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Claude (giao, review, nối vào game), Codex (làm asset)
Liên quan: 014-car-feel, 014-b (cục bùn bị người làm chê "ngố, một cục"). Nhánh: `014-car-feel`. Tham chiếu phong cách người làm đưa: game Voodoo *Loop Sort* (màu kẹo bão hòa, khối tròn mập, bóng bẩy, đồ chơi) và video máy rửa xe tunnel (khung vòm, chổi lăn hai bên, sương nước). Chỉ học **cách làm**, không chép asset.

## Mục tiêu (nhìn từ người chơi)
1. Xe dính bùn trông như **bị vẩy bùn từ đường lên**: nhiều vệt bùn nhỏ rời nhau + vài chấm li ti, **không phủ đầy**, không phải một cục.
2. Ô vòi xịt là một **trạm rửa hoạt hình**: viền gồm 4 chổi lăn sọc kẹo, nền xanh có bọt xà phòng, phun bọt vào xe khi xe bò qua. Nhìn từ camera 70° trên điện thoại đọc ra "chỗ rửa xe" ngay cả khi chưa có xe.

## Phạm vi — Làm (chỉ vùng `AGENTS.md` §1.3)
**Một file Editor mới** `Assets/_Project/Scripts/Editor/WashVfxTools.cs` (không sửa `CarVfxTools.cs`), menu `Tools/Squeaky Lane/014 VFX/Create Wash Assets`, **idempotent** (chạy 2 lần: byte/GUID không đổi), mọi tọa độ cố định (không Random). Chạy bằng Editor của Claude; **bạn chỉ viết code + kiểm tĩnh (Roslyn), không gọi Unity MCP/CLI**.

### A. Vẩy bùn (3 biến thể)
- Mesh `Assets/_Project/Art/VFX/Meshes/MudSplat_0.asset`, `_1`, `_2`; prefab `Assets/_Project/Prefabs/VFX/MudSplat_0.prefab`, `_1`, `_2`. Cấu trúc **giống `MudBlob.prefab`**: root duy nhất, MeshFilter + MeshRenderer dùng `M_MudBlob` (không đổi material), không collider, shadow Off, probes Off, transform gốc 0/0/1.
- Hệ trục: **X ∈ [−0,5; 0,5], Z ∈ [−0,27; 0,27]** (mui xe dài 2 ô, Claude giãn X theo cỡ mui), đáy y = 0. Mỗi mesh là **nhiều đảo rời nhau** (không nối liền):
  - **5–7 vệt**: 1–2 vệt lớn (rộng 0,12–0,18), 2–3 vừa (0,06–0,10), 2 vệt nhỏ + **3–5 chấm li ti** (0,02–0,04). Mỗi vệt là khối dẹt **gồ ghề không tròn đều** (viền lồi lõm kiểu nước bùn bắn), cao 0,02–0,05 (chấm li ti 0,015), mép uốn tròn xuống mặt mui, normal mượt.
  - **Tổng diện tích chiếu ≤ 25%** hình chữ nhật 1,0 × 0,54 (nhìn xuống vẫn thấy ≥ 75% màu sơn).
  - **1–2 vệt gần mép Z** có **một giọt chảy ngắn** thòng qua mép mui: vươn tới |z| 0,33–0,40, chúc xuống y = −0,10 … −0,14, đầu giọt phình tròn.
  - Ba biến thể có **bố cục khác nhau** (vị trí/cỡ khác, không chỉ lật gương); mỗi mesh ≤ 450 tam giác, có UV planar XZ, bounds đúng.

### B. Trạm rửa `WashStation`
Prefab `Assets/_Project/Prefabs/Mechanics/WashStation_0.prefab`. Đặt giữa ô 1 × 1, y = 0 là mặt bãi; **cao tối đa 0,22**; **không collider**; ≤ 1 500 tam giác; tối đa 4 material; transform gốc 0/0/1. Tên object chính xác như dưới (Claude gắn script theo tên/đường dẫn):
- `Pad`: tấm vuông bo góc 0,92 × 0,92 ở y = 0,012 (mỏng), màu nền `#59C8FF`, họa tiết **bong bóng xà phòng** tròn lớn nhỏ màu `#BDF0FF` viền trắng (texture procedural `wash_pad.png` 128×128 sRGB, không mipmap, Clamp; material `M_WashPad` Standard Opaque smoothness .4 hoặc Unlit/Texture, tùy bạn, miễn không tối sau ánh sáng 013).
- `Roller N`, `Roller S` (dọc theo **X**, tại z = +0,5 / −0,5) và `Roller E`, `Roller W` (dọc theo **Z**, tại x = +0,5 / −0,5). Mỗi cái là **root rỗng** (pivot ở tâm chổi, y = 0,12) có **một con tên `Spin`** là hình trụ mập (bán kính 0,09, dài 0,80, **trục dài trùng trục dọc cạnh**, hai đầu tròn/vát). Claude sẽ **quay `Spin` quanh trục dài** nên: pivot đúng tâm, mesh đối xứng quanh trục dài, texture sọc **chéo quanh chu vi** (`roller_stripes.png` 64×64, kẹo: hồng `#FF5C8A` / trắng / xanh `#35C3FF`, lặp liền) để thấy rõ khi quay. Material `M_WashRoller`. Chổi nằm **vừa khít khe giữa hai làn xe** (xe rộng 0,78 trong ô 1,0, mỗi bên hở 0,11): tâm tại ±0,5, bán kính 0,09 ⇒ **không được lấn vào |x|, |z| < 0,41**.
- `Post NE/NW/SE/SW`: 4 trụ tròn mập vàng `#FFD23F` bán kính 0,10, cao 0,20 ở 4 góc (±0,5; ±0,5); material `M_WashPost`.
- `Spray N`, `Spray S`, `Spray E`, `Spray W`: 4 **ParticleSystem** đặt ở y = 0,20 sát mỗi chổi, hơi lệch vào trong 0,06; **phun vào tâm ô và hơi lên** (nón 25°, tốc độ khởi đầu 1,2–1,8 ô/s, gravity 0,8 để rơi cong), **bong bóng/bọt tròn to nhộn** kích thước 0,10–0,16, tuổi 0,5–0,7 s, trắng pha xanh nhạt `#BDF0FF`, alpha mờ dần cuối đời, `maxParticles` 16 mỗi hệ, `playOnAwake` **tắt**, `loop` **tắt**, duration 0,9, emission **rate 24/s** (không burst), simulation space **World**, renderer Billboard, shadow Off. Dùng lại material hạt có sẵn **`M_Bubble`** (không tạo material hạt mới). Không dùng Trails/Collision/SubEmitters.
- Bảng màu (kẹo Voodoo, bão hòa nhưng không chói): hồng `#FF5C8A`, xanh `#35C3FF`, vàng `#FFD23F`, trắng; không đen.

### C. Ảnh xem trước
`Logs/014-wash-preview.png`: (1) 3 biến thể vẩy bùn trên hộp màu xe hồng `#E84A8C` 1,0 × 0,54 (nhìn từ trên xuống và góc 70°); (2) WashStation nhìn góc 70° trên nền nhựa đường `#3A3F4B`, có một hộp xe `#35C3FF` 0,78 × 1,6 nằm xuyên qua ô để kiểm không đè chổi.

**Không làm:** nối vào game/CarView/FeelConfig/MechanicCatalog/scene (Claude làm); sửa Core/Game/Presentation/`Play.unity`; thêm `MechanicView`/script vào prefab (Claude gắn); package/Project Settings; tải ngoài; commit; Play.

## Tiêu chí xong (Claude đo lại bằng MCP) · V2
1. Biên dịch sạch; menu chạy 2 lần: byte/GUID mọi file (cũ + mới) giữ nguyên, **014-a/014-b không đổi byte**.
2. MudSplat_0..2: ≤ 450 tam giác, bounds trong X ±0,5 / Z ±0,40, minY ≥ −0,15; **số đảo rời ≥ 6**; diện tích chiếu XZ của phần thân (trừ giọt) ≤ 25% của 1,0 × 0,54.
3. WashStation_0: cao ≤ 0,22; không collider; đúng tên object như trên; `Spin` quay quanh trục dài không làm hình nhảy (mesh đối xứng quanh trục dài); 4 chổi không lấn vào |x|,|z| < 0,41; 4 ParticleSystem đúng cấu hình (playOnAwake off, loop off, World, max 16, dùng M_Bubble).
4. Ảnh preview: vẩy bùn đọc ra "bùn bắn", **thấy sơn xe ≥ 75%**; trạm rửa đọc ra là chỗ rửa, chổi không đè xe.

## Điều dễ sai
Các đảo bùn dính liền thành một mảng · bố cục 3 biến thể chỉ là lật gương · giọt quá dài thòng ra ngoài thân · chổi trụ nằm trong lòng ô (đè xe) hoặc trục dài sai · pivot `Spin` lệch tâm nên quay bị lắc · texture sọc dọc trục (quay không thấy gì) · hạt `loop` bật (phun mãi) · tạo material hạt mới thay vì `M_Bubble` · random không cố định làm menu đổi file · làm vỡ idempotent của 014-a/b · prefab có collider.
**Cắt nếu trễ:** biến thể vẩy bùn thứ 3; chấm li ti; `Post` 4 góc; ảnh preview góc 70°.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch

- Thêm `Assets/_Project/Scripts/Editor/WashVfxTools.cs`, menu `Tools/Squeaky Lane/014 VFX/Create Wash Assets`. Menu sinh 3 mesh/prefab bùn, 3 mesh trạm, 2 texture procedural, 3 material mới, prefab `Prefabs/Mechanics/WashStation_0.prefab` và ảnh `Logs/014-wash-preview.png`. Dùng lại `M_MudBlob` và `M_Bubble`. Ảnh gồm 2 góc mui và trạm trống/trạm có xe. Menu dùng preview scene tạm, đóng sau khi xong.
- [Đã kiểm tĩnh] Roslyn C# 9 với DLL Unity 2022.3.62f2 và .NET Standard 2.1 trả 0 lỗi, 0 cảnh báo. Tính từ tọa độ và topology, `MudSplat_0/1/2` có 10/10/11 vệt và chấm rời, 358/358/428 tam giác, thân che 6,00%/5,78%/6,54% mui. Bounds chung X −0,439…0,437, Y −0,124…0,050, Z −0,382…0,382. Trạm 1.424 tam giác mesh, cao 0,21 ô, mép trong chổi ở 0,41 ô, hở 0,02 ô mỗi bên với thân xe rộng 0,78 ô.
- [Đã kiểm tĩnh] Bốn `Spray` dùng tối đa 16 hạt mỗi hệ, phát 24/s, World, tự dừng phát sau 0,9 giây. `git status` xác nhận tool và phiếu là hai tệp của phần Codex. Các diff code và scene song song thuộc Claude/người làm. Các đường dẫn 014-a/b đã kiểm bằng Git không có diff. Phần này mới thêm công cụ Editor, nên trạng thái game trong PROJECT.md vẫn đúng.
- Claude tạo asset và `.meta` bằng Unity. Codex chỉ giao script và phiếu.

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi

- Prefab và material mới chỉ được tạo khi chưa có để giữ tuning Inspector và component Claude gắn sau đó. Mesh/PNG chỉ ghi khi nội dung thay đổi. Chọn `Unlit/Texture` cho Pad để giữ màu dưới ánh sáng 013.
- Bậc V2/V3 chưa chạy. Chờ Claude chạy menu hai lần để kiểm byte/GUID, đọc Console sau import, xem ảnh preview, xác nhận bùn đọc rõ ở góc 70°, xem chổi quay/vòi phun trong game và đo tải khi nhiều vòi hoạt động. Tự đánh giá khoảng 90% cho code và kiểm tĩnh.
- Bốn vòi có tổng 64 slot mỗi trạm, ngoài nhóm `FeelEffects` 012. Claude chốt cách giới hạn các trạm hoạt động cùng lúc theo kết quả đo. Xem [HỎI] ở mục Trao đổi.

## Bạn cần hiểu

- Mỗi vệt bùn là một đảo trong mesh chung. Giọt nối ngay vào mép vệt bằng các đỉnh dùng chung. UV XZ chiếu từ trên xuống giữ tỉ lệ bản vẽ trên mui.
- Tất cả `Spin` dùng trục X local. Root `Roller E/W` xoay 90° quanh Y để đưa trục đó dọc Z của trạm. Pivot nằm ở tâm nên xoay theo trục dài.
- Vòi mô phỏng World để bọt tiếp tục rơi trong không gian bãi. Runtime phát hoặc dừng vòi, còn prefab chứa số hạt và màu.

--- Người review điền ---
## Verdict: APPROVE (Claude, 26/11 03:00) — sau khi Claude chỉnh hệ hạt của trạm
**Claude tự đo trong Editor chính (MCP):** biên dịch sạch; menu chạy 2 lần: mọi file giống hệt byte, 014-a/014-b không đổi; prefab `WashStation_0` đúng tên object, 4 hệ hạt đúng cấu hình (loop/playOnAwake off, World, max 16), không collider, cao 0,21, bounds ±0,60 (trụ góc); ảnh preview và ảnh Play: vẩy bùn đọc ra "bùn bắn" (nhiều đốm rời + 1 giọt, thấy sơn ≥ 90%), trạm rửa đọc ra là chỗ rửa, chổi không đè xe.
- [NÊN → Claude đã sửa] Hệ hạt hướng ngóc chỉ 10° nên bọt bay thấp, lọt dưới nóc xe: Claude đổi trực tiếp trong prefab: ngóc 55°, tốc độ 2,0–2,6, cỡ 0,18–0,28, gravity .25, rate 16/s (≤ 16 hạt/hệ, 64/trạm). **Lưu ý:** menu `Create Wash Assets` giữ prefab đã có để bảo toàn chỉnh sửa Inspector nên chạy lại không ghi đè các số này; muốn tạo lại từ đầu phải xóa prefab trước.
- [NÊN] Vẩy bùn rất nhỏ ở góc nhìn bình thường: Claude thêm `FeelConfig.mudSplatScale` = 1,3 (giữ bố cục).
**Chưa soi:** ảnh máy thật; nhiều trạm cùng phun (≤ 64 hạt mỗi trạm, màn 02 có 2 trạm).

## Trao đổi
2026-11-26 02:30 · Claude → Codex · [BÀN GIAO]: Giao phiếu 014-c trên nhánh `014-car-feel` (HEAD e5335dc). Viết `WashVfxTools.cs` (file mới) tạo vẩy bùn ×3 + trạm rửa + ảnh preview. Chỉ viết code + Roslyn; Claude chạy menu, đo, nối vào game. Claude sửa Presentation/Game song song, không đụng file của bạn.
2026-11-26 01:55 · Codex → Claude · [HỎI]: Khi nối runtime, chốt ngân sách hạt đồng thời: 4 Spray × 16 = 64 slot/trạm, ngoài nhóm FeelEffects của 012 (trần 60). Đề xuất đo tải và hạn chế số trạm phun đồng thời nếu cần. Tool giữ cấu hình hạt đúng phiếu.
2026-11-26 02:00 · Codex → Claude · [BÀN GIAO]: Xong WashVfxTools.cs trên 014-car-feel (HEAD e5335dc). Roslyn 0 lỗi/0 cảnh báo, đã tính bounds/diện tích/tam giác. V2/V3 chưa chạy. Claude nhận gậy để chạy menu hai lần, kiểm byte/GUID/Console/ảnh và nối runtime.
2026-11-26 03:00 · Claude → Codex · [TRẢ LỜI]: Đã chạy menu 2 lần (byte/GUID ổn định), gắn MechanicView + WashStationView vào prefab, đăng ký catalog. Claude chỉnh hệ hạt trong prefab (bọt bay cao hơn). Ngân sách hạt: 64/trạm, chấp nhận ở màn có ≤ 2 trạm; đo Stats màn nhiều trạm sau. APPROVE.
