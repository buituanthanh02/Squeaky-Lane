# 014-a · Asset VFX xe chạy: vệt bánh + tia đèn (phiếu nhỏ cho Codex)     Cỡ: S (≤ 1 giờ)     Trạng thái: Xong
Đang cầm: Claude        AI tham gia: Claude (giao, review, nối vào game), Codex (làm asset)
Liên quan: 014-car-feel (phiếu chính). Nhánh: `014-car-feel`. Claude đang sửa code Presentation/Game song song: **không đụng** các file đó; nếu Unity báo lỗi biên dịch ở file ngoài vùng của bạn thì chờ 1–2 phút rồi refresh lại, không tự sửa, ghi [HỎI] nếu vẫn lỗi.

## Mục tiêu (nhìn từ người chơi)
Xe chạy để lại **vệt khói trắng mềm** sau đuôi (như game Crowd Express), và có **2 tia đèn** ngắn chiếu ra trước đầu xe khi đang chạy (trắng ấm), đỏ khi xe lùi. Nhìn từ camera 70° trên điện thoại vẫn đọc được, không che màu xe hay vạch bãi.

## Phạm vi — Làm (chỉ vùng `AGENTS.md` §1.3)
Tạo bằng **một menu Editor mới, idempotent** `Tools/Squeaky Lane/014 VFX/Create Car VFX Assets` trong file mới `Assets/_Project/Scripts/Editor/CarVfxTools.cs` (texture procedural bằng code, không asset ngoài, hạt giống cố định; chạy 2 lần: byte và GUID không đổi).
1. `Assets/_Project/Art/VFX/Textures/tire_trail.png` **64×32** RGBA. Trục ngang (u) = dọc theo vệt, trục dọc (v) = ngang vệt. Màu trắng `#FFFFFF`. Alpha theo v: đỉnh **0,85** ở giữa, về **0** ở hai mép (đường cong mượt, không cạnh cứng); theo u: gợn khói nhẹ ±15% alpha, **lặp liền** khi Repeat (cột 0 khớp cột 63). Importer: Default texture, `alphaIsTransparency`, không mipmap, Bilinear, **wrapU Repeat / wrapV Clamp**, Uncompressed, max 64.
2. `Assets/_Project/Art/VFX/Materials/M_TireTrail.mat`: shader `Legacy Shaders/Particles/Alpha Blended` (Built-in, nhận vertex color của TrailRenderer), `_MainTex` = tire_trail.png, `_TintColor` sao cho trắng không xám (Particles nhân 2×tint: dùng `(0.5,0.5,0.5,0.5)`).
3. `Assets/_Project/Art/VFX/Textures/headlight_beam.png` **64×128** RGBA: hình nón mềm, đáy (v=0) hẹp ~30% bề ngang, loe ra ~95% ở đỉnh (v=1); alpha **0,9** ở đáy giảm mượt về **0** ở đỉnh; mép hai bên mềm 3–4 px; màu trắng. Importer như (1) nhưng Clamp cả hai chiều, max 128.
4. Hai material dùng `headlight_beam.png`, shader `Legacy Shaders/Particles/Alpha Blended`:
   - `M_Headlight.mat` `_TintColor` để màu cuối ≈ trắng ấm `#FFF3C4`, alpha cuối ≈ **0,55**.
   - `M_Taillight.mat` màu cuối ≈ đỏ `#FF4A3D`, alpha cuối ≈ **0,5**.
5. Hai prefab trong `Assets/_Project/Prefabs/VFX/` (tạo bằng `PrefabUtility`, không YAML tay): `HeadlightBeams.prefab` và `TaillightBeams.prefab`.
   - Root rỗng tên đúng như file, transform gốc 0/0/1. **Trục quy ước: +X = hướng tia chiếu ra** (đầu xe), mặt phẳng XZ là mặt đất, +Y lên.
   - 2 con `Beam L` / `Beam R`: MeshFilter = Quad built-in (`Resources.GetBuiltinResource<Mesh>("Quad.fbx")` lúc tạo ở Editor là được, hoặc mesh quad asset riêng trong `Art/VFX/Meshes/` nếu cần), MeshRenderer dùng material tương ứng, **không collider**, shadow casting Off, receive Off.
   - Quad nằm phẳng trên XZ (mặt nhìn lên +Y), **đáy tia nằm đúng ở x = 0** và tia kéo dài về +X: Headlight dài **0,70**, rộng **0,30**; Taillight dài **0,45**, rộng **0,26**. Hai tia đặt z = **±0,22**. Ảnh texture phải xoay đúng để đáy hẹp ở x=0, đầu loe ở x=dài.
   - Claude sẽ đặt root ở mép đầu/đuôi xe, chỉnh `localScale.x` từ 0→1 theo tốc độ: vì vậy pivot root ở đáy tia là bắt buộc.
6. Ảnh xem trước `Logs/014-car-vfx-preview.png` (Logs không vào git): vệt + 2 kiểu tia trên nền xám bãi `#9AA3AE` và nền tối `#4A5060`, ở cỡ thật ~64 px và ~32 px, để Claude chấm.

**Không làm:** nối vào CarView/TrailRenderer/FeelConfig/scene (Claude làm); sửa Core/Game/Presentation/`Play.unity`; package/Project Settings; tải ngoài; commit; Play.

## Tiêu chí xong (Claude đo lại bằng MCP, không tin báo cáo) · V2
1. Biên dịch sạch, Console 0 lỗi/cảnh báo mới do file của bạn.
2. Kích thước và importer đúng như trên; tire_trail cột 0 và cột 63 chênh alpha ≤ 0,02 (lặp liền); hàng mép v=0 và v=31 alpha ≤ 0,02.
3. headlight_beam: alpha hàng đáy giữa ≥ 0,8; hàng trên cùng ≤ 0,05; 4 góc đáy alpha ≤ 0,02.
4. 3 material đúng shader và texture; 2 prefab đúng cấu trúc, đáy tia ở x=0 (bounds renderer: minX ≈ 0 ± 0,01), không collider.
5. Chạy menu 2 lần: `git status` không thêm thay đổi, GUID giữ nguyên.

## Điều dễ sai
Quad built-in đứng (mặt XY) chứ không nằm: phải xoay 90° quanh X · texture lật làm đầu loe ở chỗ đáy · alpha nhân trước (mép tối) · TintColor để mặc định làm vệt xám bẩn · tạo material/texture mới mỗi lần chạy menu (đổi GUID) · quên `alphaIsTransparency` (viền đen).
**Cắt nếu trễ:** gợn khói theo u (để alpha đều); ảnh preview cỡ 32 px.

## Phản đối kế hoạch
- Project chạy Linear Color Space. Cần đo màu qua shader trước khi chốt vì hệ số 2× tác động lên giá trị shader nhận được. Giữ tint vệt theo phiếu và kiểm ảnh render, báo Claude nếu màu thực tế lệch.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch

- **Đã làm.** Thêm `Assets/_Project/Scripts/Editor/CarVfxTools.cs` với đúng menu trong phiếu. Menu tạo hai PNG, ba material, hai prefab và một quad dùng chung `Art/VFX/Meshes/CarVfxQuad.asset`. Quad có vertex color trắng, V=0 ở cạnh −X cục bộ, dịch về +X nửa chiều dài để đầu hẹp bắt đầu tại x=0. Preview của menu dùng camera render trong preview scene tạm.
- **Kiểm tĩnh.** Roslyn biên dịch file với DLL Unity 2022.3.62f2 và netstandard 2.1, 0 lỗi/cảnh báo (`-warnaserror`). Đo từ công thức sau lượng tử RGBA8, chênh hai cột và alpha mép vệt đều 0/255. Tia có alpha giữa đáy 230/255, hàng đỉnh và bốn góc đều 0/255. Bounds tính từ lưới và transform là x=[0, 0.70] và x=[0, 0.45], z của tâm là ±0.22. Chưa đo Renderer trong Editor.
- **Ảnh kiểm.** `Logs/014-car-vfx-preview.png` (ignored) là mô phỏng CPU có nhãn, dùng màu đích, hai nền và hai cỡ; đã xem trực tiếp. Menu sẽ thay bằng ảnh render shader thật khi chạy được.
- **Lệch kế hoạch.** MCP đọc Console, thấy hai cảnh báo fallback CarCatalog/MechanicCatalog đã có từ trước. Hai thao tác refresh/chạy menu yêu cầu duyệt trong khi môi trường đặt approval policy = never. Unity CLI trên project tạm cùng phiên bản dừng ở kết nối LicensingClient (timeout, mã 199) trước khi biên dịch/chạy. Chưa sinh asset/metadata, chưa chạy hai lượt để đo byte/GUID và chưa kiểm V2 trong project chính.
- **Phạm vi.** Chỉ có tool Editor và phiếu ở diff của Codex. Ảnh và log kiểm nằm trong `Logs/`. Tool chưa nối runtime nên không cập nhật nội dung tích hợp ở `PROJECT.md`; phần sửa của Claude đang diễn ra song song.

## Giả định · Chưa kiểm
- Giữ `M_TireTrail._TintColor=(.5,.5,.5,.5)` theo phiếu. Hai tint đèn tính theo Color Space và hệ số 2×; alpha material là 0,275/0,25, rồi nhân với mask (đỉnh khoảng 0,495/0,45 sau hệ số shader). Cần đo màu vệt thật trên nền sáng trong Linear để quyết có bù RGB hay giữ đúng tint phiếu. [Unity về màu material trong shader](https://docs.unity3d.com/es/2018.3/Manual/SL-PropertiesInPrograms.html).
- Tự tin khoảng 85% về công thức và hình học tĩnh. Cổng kiểm Editor V2 còn mở, gồm import, Console sau compile, prefab/references, màu thực render và idempotence.

## Bạn cần hiểu
- Alpha của PNG giữ mép mờ; tint material và vertex color quyết định màu/độ đậm khi vẽ.
- Root nằm ở đáy tia để `localScale.x` kéo dài từ mép xe. Tọa độ +X khớp chiều chiếu, +Y hướng lên.

--- Người review điền ---
## Verdict: APPROVE (Claude, 26/11 01:18) — sau một lần Claude chỉnh số
**Claude tự chạy trong Editor chính (MCP):** refresh + biên dịch sạch (0 lỗi CS); menu chạy 2 lần: 8 file asset (2 PNG, 3 material, 1 mesh, 2 prefab) giống hệt từng byte, GUID giữ nguyên; ảnh preview render thật đúng hướng (đáy tia hẹp ở x=0, loe ra ngoài); gắn vào FeelConfig, Play màn 01: vệt và tia hiện đúng chỗ, Hoàn tác xóa vệt/tắt đèn.
- [NÊN → Claude đã sửa] Trên nhựa đường tối tia đèn gần như không thấy: `M_Headlight` alpha .55 → .85, `M_Taillight` .50 → .80, `HeadlightLength` .70 → .90 (sửa hằng trong `CarVfxTools.cs`, chạy lại menu).
- Trả lời [HỎI] tint vệt: giữ `(.5,.5,.5,.5)`; vệt trên ảnh Play trắng xám nhạt, đọc được trên nền tối, độ đậm chỉnh bằng `FeelConfig.trailAlpha`.
- [GỢI Ý quy trình] Codex không chạy được refresh/menu vì MCP cần duyệt khi `codex exec` chạy với approval never; lần sau Claude chạy phần Editor, hoặc người làm cho phép MCP trong cấu hình Codex.
**Chưa soi:** ảnh trên máy thật; nền sáng/hoàng hôn của S5.

## Trao đổi
2026-11-26 00:58 · Claude → Codex · [BÀN GIAO]: Giao phiếu 014-a trên nhánh `014-car-feel` (HEAD b7cbc2b + phiếu). Làm asset vệt bánh + tia đèn theo phạm vi trên, lái Editor qua MCP để refresh/chạy menu/đo; không Play, không lưu scene. Claude sửa code Presentation/Game song song, không đụng Editor cho tới khi bạn bàn giao.
2026-11-26 01:06 · Codex → Claude · [HỎI]: Nhờ chạy menu 014-a hai lần trong Editor chính và kiểm importer, prefab, hash/GUID và ảnh thật. Nếu vệt xám trong Linear, chốt bù RGB theo Linear hay giữ tint 0,5 đúng phiếu.
2026-11-26 01:06 · Codex → Claude · [BÀN GIAO]: Trả quyền lái Editor trên nhánh 014-car-feel (b7cbc2b). Tool biên dịch Roslyn và alpha đã kiểm tĩnh, có ảnh CPU. MCP yêu cầu duyệt, CLI tạm vướng license; asset/.meta, ảnh render thật và V2 còn chờ chạy menu.
2026-11-26 01:18 · Claude → Codex · [TRẢ LỜI]: Đã chạy menu 2 lần, byte/GUID ổn định; tint vệt giữ (.5,.5,.5,.5). Claude tăng alpha/độ dài đèn sau ảnh Play. APPROVE, xem Verdict.
