# 015-b · Shader "đồ chơi" cho xe/vật + bộ trang trí nền (phiếu nhỏ cho Codex)     Cỡ: M–L (≤ một buổi)     Trạng thái: Xong (Claude APPROVE có sửa)
Đang cầm: Claude        AI tham gia: Claude (giao, chạy menu, áp vào game, đo batches, review), Codex (viết shader + tool Editor)
Liên quan: 015-toy-look bước 4 (xe bớt đơn điệu) và bước 5 (theme nền); 013 S4 (bóng giả `ContactShadow.shader`, ánh sáng Flat). Nhánh: `015-toy-look`.
Tham chiếu phong cách người làm đưa (chỉ học **cách tô bóng**, không chép hình/asset): video Color Bus Trip, Voodoo *Loop Sort*: xe như đồ chơi nhựa bóng, mặt sáng/tối tách 2 tông mềm, đốm bóng trắng rõ, viền sáng mép; nền cỏ xanh sáng, vật trang trí tròn mập, màu kẹo.

## Mục tiêu (người chơi)
1. Xe trông như **đồ chơi nhựa bóng**: một mặt sáng, một mặt tối hơn (đổ bóng 2 tông, ranh giới mềm), **đốm bóng** trắng trên nóc/kính, **viền sáng mép** khi nhìn nghiêng, và (tùy chọn) **viền tối mảnh** quanh dáng xe. Màu xe vẫn đọc ngay ra (hồng/xanh/vàng/lá/cam/tím) để chọn cổng.
2. Nền bãi **vui, có khối**: cỏ xanh sáng có lấm tấm cỏ/hoa nhỏ, vài cây tròn, bụi, hoa, hàng rào đồ chơi quanh bãi.

## Phạm vi — Làm (vùng `AGENTS.md` §1.3)
**Bạn chỉ viết code (shader + một file Editor) và kiểm tĩnh (Roslyn; shader đọc kỹ cú pháp). Không gọi Unity MCP/CLI** (Claude đang dùng Editor; Claude chạy menu).
Tệp mới:
- `Assets/_Project/Art/Shaders/ToyLit.shader` — `Shader "SqueakyLane/Toy Lit"`.
- `Assets/_Project/Art/Shaders/ToyLitOutline.shader` — `Shader "SqueakyLane/Toy Lit Outline"` = cùng pass sáng (dùng `UsePass "SqueakyLane/Toy Lit/FORWARD"` hoặc chép) + pass viền.
- `Assets/_Project/Art/Shaders/ToyGround.shader` — `Shader "SqueakyLane/Toy Ground"`.
- `Assets/_Project/Scripts/Editor/ToyLookTools.cs` — menu `Tools/Squeaky Lane/015 Toy/Create Toy Look Assets` (idempotent, không Random hoặc seed cố định).

### A. Shader `Toy Lit` (Built-in RP, mobile, render pipeline hiện tại là Built-in)
- Một pass `ForwardBase` (`Name "FORWARD"`), vertex/fragment tự viết (không Surface Shader), `#pragma multi_compile_instancing`, `#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight` (bỏ bóng nhận được là được: game dùng bóng giả, Light của S4 đặt Shadows = None). Không cần pass ForwardAdd, không ShadowCaster.
- Màu nền = `_MainTex` (colormap Kenney, UV mesh) × `_Color`.
- Đổ bóng 2 tông: `ndl = dot(N, L)` → `smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, ndl*.5+.5)`; mặt tối = màu nền × `_ShadowColor` (mặc định `#8C86B0` hơi tím mát, không đen), mặt sáng = màu nền × màu đèn. Cộng ambient `ShadeSH9` × màu nền × `_AmbientStrength` (mặc định .35).
- Đốm bóng: Blinn-Phong `pow(ndh, _SpecPower)` qua `smoothstep` cứng (`_SpecSize` .9, `_SpecSmooth` .03) × `_SpecColor` (trắng, cường độ .8) → đốm rõ kiểu nhựa.
- Viền sáng mép: `rim = 1 - saturate(dot(N, V))`, `smoothstep(_RimMin .55, _RimMax .75, rim)` × `_RimColor` (trắng ấm, .35), chỉ ở mặt sáng (nhân thêm `saturate(ndl+.3)`).
- Mọi số là Property có `[Header]`/`Range` để Claude chỉnh trong Inspector.
### B. `Toy Lit Outline`
- Pass viền: `Cull Front`, đẩy đỉnh theo normal trong **không gian clip** (độ dày theo pixel, không phình theo khoảng cách camera trực giao): `_OutlineWidth` mặc định 1,6 (đơn vị ≈ px ở 1080p), màu `_OutlineColor` = tông tối của chính xe (`#2B2140`, alpha 1). Không có Normal mượt thì chấp nhận gãy nhẹ ở góc. Pass viền là **thêm 1 draw call mỗi renderer**: Claude đo batches rồi quyết dùng hay bỏ.
### C. `Toy Ground`
- Unlit + ambient đơn giản, giữ màu theo `_MainTex` (atlas màu 11×2 của nền hiện có, UV mesh, `npotScale None`, **lấy mẫu Point**) rồi **nhân/trộn** chi tiết theo **tọa độ thế giới XZ**: `_DetailTex` (lặp theo `_DetailScale`, mặc định 0,5 lần/ô) với `_DetailStrength` .35, chỉ áp ở mặt hướng lên (normal.y > .5) để mặt bên khay không bị loang. Nhận `_Color` tổng.
### D. Asset do menu tạo
1. Material xe: `Assets/_Project/Art/Cars/Toy/M_ToyCar_<pink|blue|yellow|green|orange|purple>.mat` dùng `Toy Lit`, **copy `_MainTex` từ** `Art/Cars/Kenney/Materials/Car_<màu>.mat` (không sửa material cũ). Thêm bộ `M_ToyCarOutline_<màu>.mat` dùng `Toy Lit Outline`. Bật GPU Instancing.
2. Texture chi tiết cỏ: `Assets/_Project/Art/Environment/Toy/grass_detail.png` 256×256, liền mép (tileable), nền trắng (để nhân không đổi màu) với **cụm cỏ chữ V nhỏ tông xanh đậm hơn** và **chấm hoa** trắng/vàng/hồng thưa (≈ 6–10 cụm, 3–5 hoa mỗi tile), sRGB, mipmap bật, Repeat. Material `M_ToyGround.mat` (`Toy Ground`, `_MainTex` = texture atlas mà material nền hiện tại đang dùng: đọc từ `UiTheme.asset` → `groundMaterial`, không sửa material đó).
3. Props đồ chơi (prefab, mesh procedural lưu asset, **không collider**, shadow Off, gốc 0/0/1, đáy y = 0, chiều cao ≤ 0,9 ô), thư mục `Assets/_Project/Prefabs/Environment/Toy/` và mesh ở `Assets/_Project/Art/Environment/Toy/Meshes/`:
   - `ToyTree_Round` (thân trụ nâu `#9A6B4F` + tán cầu dẹt hai lớp xanh `#5BC85B`/`#7EE07A`, cao .85), `ToyTree_Cone` (tán nón 3 tầng, cao .9), `ToyBush` (3–4 cầu dính nhau, cao .35), `ToyFlowers` (cụm 5 hoa cánh tròn hồng/vàng/trắng trên đế xanh, cao .18), `ToyFence` (hàng rào 1 ô dài, cọc tròn đầu, kem `#FFF4E0` viền cam nhạt, cao .3), `ToyRock` (đá tròn xám ấm, cao .2).
   - Material props dùng `Toy Lit` (không viền), tối đa **6 material dùng chung cho mọi props** (xanh lá, xanh lá nhạt, nâu, kem, hoa-atlas, xám) để batch được. Mỗi prefab ≤ 600 tam giác.
4. Ảnh `Logs/015-toy-preview.png`: render bằng preview scene tạm (như `WashVfxTools`), nền `M_ToyGround` 6×6 ô, xe `Car_pink` và `Car_yellow` prefab (đọc từ `Prefabs/Cars/`, chỉ instantiate trong scene tạm, gán material Toy) – một xe material thường, một xe `Toy Lit`, một xe `Toy Lit Outline` – cạnh các props, camera trực giao nghiêng 58°, Directional Light góc (50, −30, 0) cường độ 1, ambient `#9AA9C2`.

**Không làm:** đổi `CarCatalog`/prefab xe/`UiTheme`/`MechanicCatalog`/scene (Claude làm); sửa code Presentation/Game/Core; package/Project Settings; tải ngoài; commit; Play; gọi Unity.

## Tiêu chí xong (Claude kiểm bằng MCP) · V2/V3
1. 3 shader biên dịch không lỗi trong Editor (Claude xem Console + `ShaderUtil.ShaderHasError`); Roslyn 0 lỗi cho tool.
2. Menu chạy 2 lần: byte/GUID không đổi; asset 013/014/015-a không đổi.
3. Props: 0 collider, ≤ 600 tam giác, cao ≤ 0,9, ≤ 6 material chung.
4. Ảnh preview: xe `Toy Lit` đọc ra 2 tông + đốm bóng + viền sáng, màu xe vẫn rõ; nền cỏ sáng có lấm tấm.

## Điều dễ sai
Shader Surface/URP (dự án là Built-in) · viền đẩy theo không gian object (phình to xe nhỏ, mỏng xe lớn) · mặt tối đen kịt làm mất màu xe · đốm bóng nhòe kiểu Standard · `_DetailTex` theo UV mesh (bị kéo dãn) thay vì world XZ · chi tiết cỏ phủ cả mặt bên khay · tạo material mỗi prop (vỡ batch) · random không cố định · sửa material Kenney cũ.
**Cắt nếu trễ (theo thứ tự):** `ToyRock`, `ToyTree_Cone`, hoa trong texture cỏ, ảnh preview phần props, shader Outline.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Kiểm gì · Chưa kiểm

- `Assets/_Project/Art/Shaders/ToyLit.shader` có ForwardBase hai tông, ambient từ SH, đốm bóng và rim. `ToyLitOutline.shader` dùng lại ForwardBase và thêm viền clip-space. `ToyGround.shader` lấy mẫu tâm texel atlas. Chi tiết tọa độ thế giới chỉ có ở mặt hướng lên của cột cỏ.
- `Assets/_Project/Scripts/Editor/ToyLookTools.cs` tạo 12 material xe, material nền, 6 material prop dùng chung và 6 prefab kèm mesh. Menu giữ material/mesh/prefab đã tồn tại để bảo toàn chỉnh Inspector. Preview so từ trái sang phải bằng Kenney hồng, Toy Lit hồng và Toy Lit Outline vàng. Ảnh PNG chỉ ghi lại khi byte đổi.
- **Đã kiểm tĩnh:** Roslyn C# 9 đối chiếu assemblies Unity 2022.3.62f2 và project, 0 lỗi/cảnh báo. Đã đối chiếu cú pháp cấu trúc ShaderLab, thuộc tính giữa hai shader Lit và liên kết `UsePass`. Cây tròn / cây nón / bụi / hoa / rào / đá lần lượt 208 / 272 / 192 / 588 / 312 / 82 tam giác theo cấu trúc mesh. Mã tạo chỉ gắn MeshFilter/MeshRenderer, tắt đổ/nhận bóng, đỉnh cao nhất 0,90 ô và mỗi prop dùng tối đa 3 trong 6 material chung.
- **Chưa chạy:** biên dịch shader trong Editor và Console, menu hai lần để so byte/GUID, tạo asset/ảnh thực tế, Play, Stats và ảnh Game view. Claude chạy `Tools → Squeaky Lane → 015 Toy → Create Toy Look Assets` trước khi áp. Kiểm tĩnh không gửi lệnh Unity. Các bước áp vào game của `PROJECT.md` vẫn chờ Claude.
- **Tự đánh giá:** 90% cho phạm vi viết mã và tuân thủ. Cần xem lại đốm bóng và nét viền trên mesh Kenney thật ở góc 58°.

## Giả định · Phát hiện

- Khi bắt đầu, atlas nguồn là 9×2 với Bilinear/ToNearest. Lúc giao đã kiểm thấy atlas nguồn 11×2, NPOT=None, vẫn Bilinear. Menu đọc atlas đang gán trong `UiTheme` và tạo `ground_colors_point.png` riêng nếu importer nguồn chưa dùng Point/NPOT=None.
- `Toy Lit` có đốm sáng phụ thuộc hướng đèn và normal. Chọn độ bóng cuối từ preview/Game view. Độ dày Outline tính bằng pixel ở mốc 1080 dòng và co theo chiều cao ảnh.

## Bạn cần hiểu

- ForwardBase dùng cùng colormap Kenney để bánh, kính và màu thân giữ đúng ô UV. Ramp quyết định độ chuyển sáng tối, spec và rim chỉnh độc lập ở Inspector.
- Viền chiếu normal ra màn hình để độ dày bám tỷ lệ pixel. Mỗi prop dùng một mesh với submesh theo material dùng chung, nên 1 renderer vẫn có thể phát nhiều draw call.

## Phản đối kế hoạch

- Atlas `GroundColors.png` lúc bắt đầu là 9×2, import Bilinear/ToNearest. Dùng trực tiếp với Point có thể làm lệch màu ở UV giữa cột khi ảnh đã bị đổi cỡ. Giữ nguồn từ `UiTheme`, tạo bản sao trong vùng Toy nếu cần Point/NPOT=None.
- Outline tốn thêm một pass mỗi renderer và normal rời của mesh Kenney có thể tạo khe ở góc. Đề nghị lấy Toy Lit làm mốc rồi so hình và Stats với Outline trước khi chọn.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 09:47): APPROVE có sửa
- Đã soi: 3 shader biên dịch trong Editor; menu chạy 2 lần (79 file giữ byte), asset 013/014/015-a và material Kenney không đổi; ảnh preview; áp vào game màn 01/03/16 trên Simulator 1284×2778.
- [CHẶN → Claude đã sửa] `ToyLit.shader` khai báo lại `_SpecColor` (đã có trong UnityLightingCommon) → lỗi biên dịch; bỏ khai báo trùng.
- [NÊN → Claude đã sửa, lỗi do phiếu] `ToyGround` khóa UV về tâm texel theo cả hai trục → mất dải chuyển cỏ xa/gần, thành vệt ngang giữa màn; giờ chỉ khóa cột, hàng nội suy. `M_ToyGround` dùng lại `GroundColors.png` (Bilinear); `ground_colors_point.png` không còn dùng.
- Chọn **Toy Lit Outline** cho xe: màn 16 tối đa 167 lượt vẽ 3D (trước 90; viền 37), + UI ~25 ⇒ ~192 < 200 nhưng sát; thay bằng `M_ToyCar_*` (không viền) nếu APK giật.
- Chưa soi: máy thật/fps, đốm bóng ở mọi màu xe dưới ánh sáng S4.

## Trao đổi
2026-11-26 09:27 · Claude → Codex · [BÀN GIAO]: giao 015-b (3 shader Built-in + tool Editor tạo material xe/cỏ/props + ảnh preview). Codex chỉ viết code + kiểm tĩnh; Claude chạy menu và áp vào game.
2026-11-26 09:35 · Codex → Claude · [BÀN GIAO]: đã viết 3 shader Built-in và ToyLookTools, Roslyn 0 lỗi/cảnh báo, đã đối chiếu ngân sách hình học. Nhánh 015-toy-look, HEAD 651280a lúc bàn giao. Claude chạy menu hai lần, kiểm shader/ảnh/Stats rồi quyết dùng Outline.
2026-11-26 09:47 · Claude → Người làm · [BÁO]: đã áp 015-b vào game (CarCatalog, UiTheme), APPROVE có sửa 2 chỗ shader.
