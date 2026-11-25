# 014-b · Asset bùn kiểu D: cục bùn khối + vệt bùn (phiếu nhỏ cho Codex)     Cỡ: S (≤ 1 giờ)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Claude (giao, review, nối vào game), Codex (làm asset)
Liên quan: 014-car-feel (phiếu chính), 014-a (vệt bánh/đèn, cùng tool `CarVfxTools.cs`). Nhánh: `014-car-feel`. Người làm chốt kiểu bùn "D + vệt bùn" 26/11 (`PROJECT.md` §5.1).

## Mục tiêu (nhìn từ người chơi)
Xe dính bùn có **một cục bùn nâu ướt, có khối, nằm trên nóc**, vài **giọt bùn chảy tràn xuống mép nóc** (nhìn từ camera 70° gần như từ trên xuống thấy rõ là "xe này đang bẩn"), nhưng **vẫn thấy màu sơn xe** xung quanh. Xe bẩn chạy thì để lại **vệt bánh màu nâu** thay cho vệt khói trắng.

## Phạm vi — Làm (chỉ vùng `AGENTS.md` §1.3)
Thêm vào **chính menu có sẵn** `Tools/Squeaky Lane/014 VFX/Create Car VFX Assets` (`Assets/_Project/Scripts/Editor/CarVfxTools.cs`, giữ idempotent: chạy 2 lần byte/GUID không đổi; giữ nguyên các asset 014-a và các hằng Claude đã chỉnh: đèn alpha .85/.80, dài .90):
1. **Mesh** `Assets/_Project/Art/VFX/Meshes/MudBlob.asset`, procedural, hạt giống cố định:
   - Hệ trục chuẩn hóa: **X, Z ∈ [−0,5; 0,5]** là dấu chân phần thân cục bùn (Claude sẽ scale X/Z theo cỡ nóc từng xe; Y **giữ đơn vị thế giới**, Claude để scale Y = 1). Đáy thân ở **y = 0** (nằm trên nóc), đỉnh cao **0,10–0,14**.
   - Thân: khối dẹt **gồ ghề, không đối xứng** (gộp 5–7 khối cầu dẹt chồng nhau hoặc nhiễu bán kính), mép thân **uốn tròn xuống**, không cạnh cứng. Nhìn từ trên xuống thấy rõ đường viền lồi lõm như vết bùn văng.
   - **3–4 giọt chảy**: hình giọt nước dài thòng ra ngoài mép thân theo ±Z (2 bên hông) và 1 giọt theo +X hoặc −X, mỗi giọt vươn ra tới |z| = **0,60–0,70** và chúc xuống tới **y = −0,10 … −0,16** (chảy qua mép nóc), đầu giọt phình tròn. Từ trên nhìn xuống, giọt phải lòi ra khỏi viền thân.
   - **≤ 800 tam giác**, normal mượt (recalculate + làm mượt đường nối), có UV (planar XZ là được), không vertex color bắt buộc. Bounds đúng.
2. **Material** `Assets/_Project/Art/VFX/Materials/M_MudBlob.mat`: shader `Standard`, **Opaque**, màu `#5A3418`, Smoothness **0,65** (bùn ướt bóng), Metallic 0. (Claude dùng màu `_Color` của material này cho cả lúc bùn mờ dần khi rửa.)
3. **Prefab** `Assets/_Project/Prefabs/VFX/MudBlob.prefab`: root duy nhất tên `MudBlob`, transform gốc 0/0/1, MeshFilter = MudBlob.asset, MeshRenderer = M_MudBlob, **không collider**, shadow casting **Off** (bóng giả 013 đã có), receive shadows Off, light/reflection probes Off.
4. **Material vệt bùn** `Assets/_Project/Art/VFX/Materials/M_MudTrail.mat`: giống `M_TireTrail` (shader `Legacy Shaders/Particles/Alpha Blended`, `_MainTex` = `tire_trail.png`), màu cuối ≈ nâu `#6B4423`, alpha cuối ≈ **0,75** (dùng hàm `BeamTint` có sẵn để tính `_TintColor`).
5. Ảnh xem trước thêm vào `Logs/014-car-vfx-preview.png` hoặc ảnh riêng `Logs/014-mud-preview.png`: cục bùn nhìn từ trên (góc 70° nếu được) đặt trên một hộp màu xe đỏ `#C0392B` cỡ 0,9 × 0,7, và một dải vệt bùn trên nền nhựa đường `#3A3F4B`.

**Không làm:** nối vào CarView/FeelConfig/ParticleSystem/scene (Claude làm); sửa Core/Game/Presentation/`Play.unity`/12 prefab xe; package/Project Settings; tải ngoài; commit; Play. Unity MCP lần trước cần duyệt và không chạy được: **chỉ viết code + kiểm tĩnh (Roslyn)**, Claude sẽ chạy menu trong Editor.

## Tiêu chí xong (Claude đo lại bằng MCP) · V2
1. Biên dịch sạch; menu chạy 2 lần: mọi file (cũ + mới) giống hệt byte, GUID giữ nguyên.
2. MudBlob: ≤ 800 tam giác; bounds X/Z thân trong ±0,5 nhưng giọt vươn tới |z| 0,60–0,70; minY trong [−0,16; −0,10]; maxY trong [0,10; 0,14]; không normal NaN.
3. Prefab đúng cấu trúc, không collider, shadow Off; M_MudBlob Standard Opaque `#5A3418` smoothness .65; M_MudTrail đúng texture/shader.
4. Ảnh preview: cục bùn đọc ra "bùn văng có giọt chảy", không giống đĩa phẳng hay quả bóng; màu đỏ của hộp xe vẫn thấy ≥ 50% diện tích nóc.

## Điều dễ sai
Mesh quá tròn đều (giống bánh/đá) · giọt nằm trong viền thân nên nhìn từ trên không thấy · normal gãy ở chỗ nối giọt · dùng Random không cố định làm menu đổi file mỗi lần chạy · vỡ idempotent của asset 014-a · Standard Fade thay vì Opaque.
**Cắt nếu trễ:** ảnh preview góc 70° (để từ trên xuống); giọt thứ 4.

## Phản đối kế hoạch
- Ghép các khối cầu chồng nhau dễ để lại đường nối khi material bóng. Dùng một lưới kín với các vòng đỉnh chung cho thân và ba giọt để Unity tính normal liên tục.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch

- `Assets/_Project/Scripts/Editor/CarVfxTools.cs` mở rộng menu hiện có để tạo mesh và prefab `MudBlob`, material `M_MudBlob` Opaque và `M_MudTrail` dùng chung `tire_trail.png`. Menu xuất thêm `Logs/014-mud-preview.png` với nóc đỏ 0,9 × 0,7 và vệt bùn ở góc 70°. Các giá trị đèn 014-a vẫn là .85/.80 và .90. Unity tạo asset cùng `.meta` khi chạy menu.
- **Đã kiểm tĩnh.** Roslyn C# 9 tham chiếu các module Unity 2022.3.62f2, 0 lỗi và 0 cảnh báo. Gọi hàm sinh hình hai lần ngoài Editor cho cùng mảng đỉnh/chỉ số. Mesh kín, 338 đỉnh, 672 tam giác, không có tam giác suy biến. Bounds X [−0,455; 0,660], Y [−0,141; 0,129], Z [−0,630; 0,660]. Ba đầu giọt xuống Y [−0,141; −0,128]. Normal tính từ mặt lưới đều hữu hạn. Phép chiếu silhouette ở 70° ước tính còn thấy ít nhất 54% nóc đỏ.
- **Chưa chạy V2.** Claude chạy menu hai lần, so byte/GUID của asset và prefab, kiểm Console, normal do Unity tính và ảnh PNG thực tế. Các thiết lập material/prefab đã đối chiếu trong code. Kiểm review tĩnh khoảng 90% tự tin. Kiểm bằng Unity MCP, CLI và Play nằm ngoài lượt này.
- **Phạm vi.** Hai tệp Codex sửa là tool và phiếu này. `PROJECT.md` §5.1 đã mô tả kiểu D và vệt bánh nâu. Phiếu dùng phương án ba giọt, chọn ảnh riêng và dùng hệ số X/Z .69/.56 cho nóc mẫu. Tỉ lệ nóc thật của xe và xe tải cần Claude đối chiếu lúc nối.

## Bạn cần hiểu

- Đỉnh dùng chung giữa thân và giọt để `RecalculateNormals` làm mượt cả đường nối. Runtime có thể đổi tỉ lệ X/Z theo nóc và giữ Y = 1 để chiều cao giọt giữ nguyên.
- `M_MudTrail` dùng độ trong từ texture và `BeamTint` để bù hệ số nhân 2 của shader hạt. `M_MudBlob` lấy màu từ `_Color` và dùng bề mặt Opaque ở trạng thái bám nóc.

--- Người review điền ---
## Verdict: APPROVE (Claude, 26/11 02:00) — sau một lần Claude chỉnh màu vệt
**Claude tự đo trong Editor chính (MCP):** biên dịch sạch; menu chạy 2 lần: mọi asset (cũ + mới) giống hệt byte, 014-a không đổi; MudBlob 672 tam giác, bounds minY −0,14 / maxY 0,13 / |z| tới 0,66, không NaN normal; M_MudBlob Standard Opaque `#5A3418` smoothness .65; prefab 3 component, không collider, shadow Off. Ảnh Play màn 02: cục bùn đọc ra "bùn văng có giọt chảy", thấy màu sơn xe quanh nó.
- [NÊN → Claude đã sửa] Vệt bùn `#6B4423`/.75 gần như tàng hình trên nhựa đường tối: đổi `#96622F`/.95 trong `CarVfxTools.cs`.
- [GỢI Ý] Gốc cục bùn hơi to so với nóc xe con (≈ 50% nóc, thấy ở ảnh); chỉnh bằng scale trong `CarView.SwapMudBlob` nếu người làm thấy che số xe.
**Chưa soi:** xe tải, ảnh máy thật.

## Trao đổi
2026-11-26 01:27 · Claude → Codex · [BÀN GIAO]: Giao phiếu 014-b trên nhánh `014-car-feel` (HEAD e25ed4b). Mở rộng `CarVfxTools.cs`: mesh/prefab cục bùn + M_MudBlob + M_MudTrail. Chỉ viết code + Roslyn; Claude chạy menu, đo, nối vào game. Claude sửa Presentation/Game song song, không đụng file của bạn.
2026-11-26 01:33 · Codex → Claude · [BÀN GIAO]: Xong tool 014-b trên `014-car-feel` (HEAD `e25ed4b`, đổi trong working tree). Roslyn sạch, mesh 672 tam giác và bounds đạt khi kiểm tĩnh. Claude chạy menu hai lần, kiểm byte/GUID, Console và PNG, rồi nối asset vào game.
2026-11-26 02:00 · Claude → Codex · [TRẢ LỜI]: Đã chạy menu 2 lần, byte/GUID ổn định, mesh đạt số đo; Claude đổi màu vệt bùn sáng hơn. APPROVE.
