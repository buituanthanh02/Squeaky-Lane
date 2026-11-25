# 013-S4 · Sprite bong bóng xà phòng (phiếu nhỏ cho Codex)     Cỡ: S (≤ 1 giờ)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Claude (giao việc, review), Codex (làm)
Liên quan: 013 (S5 bong bóng sau này), 014 VFX (chưa mở). Nhánh: `013-theme-ui`. Đây là **phiếu thử quy trình** Claude giao việc → Codex làm → Claude review qua MCP, đồng thời là asset có dùng thật cho VFX sau.

## Mục tiêu (nhìn từ người chơi)
Có sẵn một sprite bong bóng xà phòng trong và nhẹ, để các hiệu ứng rửa xe (014/S5) dùng lại. Sprite phải đọc được là "bong bóng" ngay cả khi chỉ to ~24 px trên điện thoại, và không che khuất xe.

## Phạm vi — Làm / Không làm
**Làm** (chỉ trong vùng art/hiệu ứng, `AGENTS.md` §1.3):
1. Tạo texture **procedural bằng code Editor** (không dùng asset ngoài, không vẽ tay): `Assets/_Project/Art/VFX/Textures/bubble.png`.
   - 128×128, nền trong suốt hoàn toàn ngoài vòng tròn; RGBA.
   - Thân bong bóng gần trong suốt: alpha thân ≈ 0,10–0,18; **viền** (vành rìa) sáng hơn alpha ≈ 0,55–0,75, dày ~6–8% bán kính, mép mềm 1–2 px; một **điểm sáng** (highlight) trắng nhỏ ở góc trên-trái (alpha ~0,9, bán kính ~8% ) và một vệt sáng phụ mờ ở góc dưới-phải. Màu gần trắng hơi xanh nhạt (`#EAF6FF`), không màu sặc sỡ.
   - Import: Texture Type **Sprite (2D and UI)**, `alphaIsTransparency` bật, mipmap tắt, nén **Uncompressed** hoặc RGBA32, Filter Bilinear, Wrap Clamp, `maxTextureSize` 128.
2. Tạo material `Assets/_Project/Art/VFX/Materials/M_Bubble.mat`: shader có sẵn của Built-in dùng được cho hạt (ví dụ `Particles/Standard Unlit` hoặc `Sprites/Default`), alpha blend, `_MainTex` = bubble.png, **không ZWrite**, không tạo shader mới. Nêu rõ shader đã chọn và vì sao.
3. Một **menu Editor mới** `Tools/Squeaky Lane/014 VFX/Create Bubble Sprite` (file mới, tên rõ, trong vùng Editor riêng của hiệu ứng) để sinh lại texture + material; **idempotent** (chạy hai lần: file giống hệt, không nhân đôi, không đổi GUID của `.png`/`.mat`).
**Không làm:** không nối vào code game, scene hay prefab; không hệ hạt; không sửa Core/Game/Presentation/`Play.unity`; không thêm package; không tải gì từ ngoài; không commit.

## Tệp dự kiến
- Mới: `Art/VFX/Textures/bubble.png` (+ `.meta`), `Art/VFX/Materials/M_Bubble.mat` (+ `.meta`), một file Editor tool (tên gợi ý `BubbleSpriteTools.cs`).
- Cần Editor: có (chạy menu). **Codex được lái Editor cho phiếu này; Claude chỉ đọc.**

## Tiêu chí xong (đo được) · bậc kiểm V2
1. Biên dịch sạch, Console **0 lỗi / 0 cảnh báo mới** (Claude kiểm bằng `read_console`).
2. `bubble.png`: đúng 128×128, góc ảnh (0,0) alpha = 0, tâm thân alpha trong 0,10–0,18, pixel trên vành alpha ≥ 0,55; importer đúng các cờ ở trên.
3. `M_Bubble.mat` dùng đúng shader nêu trong báo cáo, `_MainTex` trỏ `bubble.png`.
4. Chạy menu **hai lần**: `git status` không có thay đổi mới ở lần hai, GUID giữ nguyên.
5. Không có file nào đổi ngoài `Art/VFX/**` và file tool Editor mới (`git status`).
6. Báo cáo ghi kèm ảnh xem trước (PNG xem thử) hoặc đường dẫn để Claude xem được.

## Điều cấm / rủi ro / thứ có thể cắt
- Dễ sai: để alpha thân quá đậm (bong bóng thành đĩa trắng, che xe); alpha premultiplied sai làm viền đen; không bật `alphaIsTransparency` nên viền đen khi nén; đổi GUID khi chạy lại menu; tạo material lúc chạy.
- **Cắt nếu trễ:** vệt sáng phụ dưới-phải; material (chỉ giữ texture).

## Phản đối kế hoạch
Codex ghi `[HỎI]`/`[BÁO]` vào Trao đổi **trước khi làm** nếu thấy sai (kích thước, shader, alpha). Claude sửa phiếu nếu Codex nói đúng.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch
- [ĐÃ LÀM] Thêm `Assets/_Project/Scripts/Editor/BubbleSpriteTools.cs` (+ `.meta`) với đúng menu `Tools/Squeaky Lane/014 VFX/Create Bubble Sprite`. Chỉ sinh asset tĩnh; không có view/runtime code, scene, prefab hay hệ hạt.
- [ĐÃ LÀM] Sinh `Art/VFX/Textures/bubble.png` + `Art/VFX/Materials/M_Bubble.mat`, `.meta` đi kèm và hai folder `.meta` do Unity tạo. Sprite RGBA128, bán kính58px, vành7%R/mép mềm1,5px, thân#EAF6FF, điểm sáng trắng trên-trái và cung phụ dưới-phải.
- [ĐÃ KIỂM PNG độc lập] Ngoài vòng tròn toàn bộ alpha0; góc(0,0)=RGBA(0,0,0,0); tâm(64,64)=RGBA(234,246,255,36) → alpha **0,141176**; vành(64,8)=alpha168/255=**0,658824**; highlight cực đại230/255=**0,901961**.
- [ĐÃ KIỂM EDITOR/MCP] Texture2D128×128 **RGBA32**, Sprite tồn tại, importer Sprite/Single, alphaIsTransparency=true, mipmap=false, Uncompressed, Bilinear, Clamp, maxTextureSize=128, isReadable=false. Dữ liệu RGBA file và dữ liệu importer được kiểm riêng.
- [SHADER] Chọn Built-in **Sprites/Default**: Unlit, alpha blend premultiplied, ZWrite Off; `_MainTex` trỏ `bubble.png`, tint trắng/alpha1, renderQueue3000. PNG lưu **straight alpha**, shader tự nhân RGB×A trong `UnitySprites.cginc/SpriteFrag`; không thêm shader hoặc material runtime. MCP xác nhận shader supported và tham chiếu texture đúng.
- [ĐÃ KIỂM IDempotence] Chạy menu qua MCP **hai lượt**. `git status --porcelain=v1 -uall` trước/sau lượt2 giống hệt; SHA-256 của toàn bộ **34 file Art/VFX + tool/.meta** giống hệt, không thêm file ở lượt2. PNG GUID `df89f2e60f1a6b14388284bef43e56d7`, material GUID `c25b79a337866a441a88b0d5594b9b5d` giữ nguyên (meta byte-identical).
- [ĐÃ KIỂM CONSOLE] Unity refresh/biên dịch và hai lượt menu đã chạy. Console sau từng lượt tạo: **0 Error / 0 Warning**. Refresh ban đầu có1 warning transport MCP, đã ghi nguyên văn trong Trao đổi trước khi clear baseline; không sửa package, validator hoặc che lỗi code.
- [ĐÃ KIỂM PHẠM VI] Không có thay đổi tracked ngoài phạm vi (`git diff --name-only` rỗng); file mới chỉ art/tool và phiếu này. `.mcp.json` có sẵn từ baseline giữ nguyên. Active scene path="", dirty=false, ngoài Play trước/sau; không lưu scene. Không commit.
- [PREVIEW] Đã sinh và xem [PNG xem thử](../../Logs/013-bubble-preview.png) (file ignored): hàng trên navy, hàng dưới checker; mỗi hàng trái→phải **128 / 64 / 24 px**. PNG preview alpha-composite theo color space hiện có, không sửa Project Settings. Đường dẫn tuyệt đối: `C:/Unity Project/Squeaky Lane/Logs/013-bubble-preview.png`.
- Lệch kế hoạch: không cắt vệt phụ/material. Có thêm preview và manifest SHA-256 trong `Logs/` ignored để kiểm, không vào Assets/build. Không tạo config mới: số bake tĩnh theo phiếu nằm trong tool; số particle/thời lượng thuộc014 chưa làm. `PROJECT.md` không cần đổi vì chưa mở/tích hợp014/S5 hoặc đổi sự thật gameplay/kiến trúc.
## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- Giả định: kích thước24px là kích thước toàn sprite, gồm6px đệm mỗi mép ở bản128; alpha đã đo trên file gốc, không phải alpha nhân thêm của particle.
- Chưa kiểm: hiệu ứng hạt trong game/điện thoại thật, nhiều bong bóng chồng nhau, fps/batch; phiếu này không cho nối vào scene/hệ hạt. Preview24 đọc được vòng/điểm sáng theo kiểm ảnh của Codex; **Claude quyết visual, chưa có verdict APPROVE**.
- Ngoài phạm vi: warning WebSocket của MCP sau domain reload; đã ghi và kết nối đã hồi phục. Không sửa package/server. Baseline `.mcp.json` chưa theo dõi không thuộc việc này.
- Tự tin ~90% về tiêu chí asset/code: pixel/importer/material/2 lượt/hash/Console đã kiểm; hình trên thiết bị thật còn chờ ở014/S5.
## Bạn cần hiểu (2–4 gạch)
- PNG phải giữ straight alpha; `Sprites/Default` tự premultiply khi vẽ. Nhân RGB×alpha ngay khi sinh PNG sẽ làm viền bị nhân hai lần và tối đi.
- Material tint trắng/alpha1 giữ đúng alpha texture; vertex color/alpha của particle sẽ nhân thêm vào thân0,14 và vành0,66. Claude chỉnh độ trong tổng qua config hiệu ứng ở014.
- Menu chỉ viết PNG khi byte khác, cập nhật material sẵn có bằng AssetDatabase; không xóa/tạo lại `.meta`, nên chạy lại giữ GUID và mọi tham chiếu tương lai.

--- Người review điền ---
## Verdict: APPROVE (Claude, qua Unity MCP, V2)
**Đã soi:** `git status` chỉ có `Art/VFX/Materials`, `Art/VFX/Textures`, `BubbleSpriteTools.cs`, phiếu và `.mcp.json` (cái cuối không thuộc phiếu này); Console 0 lỗi / 0 cảnh báo (`read_console`); `bubble.png` 128×128, góc (0,0) alpha 0, tâm alpha 0,141 (trong 0,10–0,18); importer Sprite, `alphaIsTransparency` bật, không mipmap, Uncompressed, Bilinear, Clamp, max 128; `M_Bubble` dùng `Sprites/Default`, `_MainTex` = bubble, queue 3000; ảnh xem trước 128/64/24 px: vòng viền, điểm sáng đọc được, bản 24 px vẫn ra "bong bóng", không che nền.
**Chưa soi:** chạy lại menu lần hai để so GUID (chỉ tin báo cáo Codex: hai lần status/hash 34 file/GUID giống hệt); đọc kỹ code `BubbleSpriteTools.cs`; hiển thị trong hệ hạt thật (phiếu 014).
- [GỢI Ý] Điểm đo viền của Claude (64,2) ra alpha 0 vì bán kính vòng nhỏ hơn dự đoán; không phải lỗi, số viền theo báo cáo Codex là 0,659.
- [GỢI Ý] `maxTextureSize` mặc định 128 nhưng tầng Android chưa override: không ảnh hưởng vì ảnh đã 128 px.
**An toàn để commit:** có. Việc người làm nên tự xem: mở `Logs/013-bubble-preview.png`; khi 014 dùng thật thì chỉnh alpha thân nếu thấy che xe.

## Trao đổi
2026-11-25 23:40 · Claude → Codex, Người làm · [BÀN GIAO]: Giao phiếu nhỏ này cho Codex (nhánh 013-theme-ui, HEAD 6bfebf3; `Đang cầm: Codex`). Việc: sinh sprite bong bóng procedural + material + một menu Editor idempotent, chỉ trong `Art/VFX/**` và một file tool Editor mới. Unity MCP đã nối (Claude đã đọc Console thành công). Codex lái Editor, Claude chỉ đọc. Xong thì điền phần "AI làm điền", đổi `Đang cầm: Claude`, thêm dòng [BÀN GIAO] và báo người làm; Claude review bằng MCP. Chưa kiểm: ngoài việc phiếu và quy trình này, chưa có gì chạy.
2026-11-25 23:46 · Codex → Claude, Người làm · [BÁO]: Đã đọc phiếu/Trao đổi, xác nhận013-theme-ui/HEAD6bfebf3; baseline giữ nguyên .mcp.json + phiếu chưa theo dõi. MCP: ngoài Play, không compile/update, Console0 error/warning; có Sprites/Default. Không phản đối kế hoạch. Rủi ro viền6–8%R chỉ còn gần1px ở24px: sẽ kiểm preview128/64/24. Chọn Sprites/Default có ZWriteOff, shader tự premultiply RGB (đối chiếu UnitySprites.cginc), PNG giữ straight alpha để tránh nhân alpha hai lần; không scene/prefab/gameplay.
2026-11-25 23:50 · Codex → Claude, Người làm · [BÁO]: Sau refresh script, MCP báo đã hồi phục kết nối; Console có1 warning từ package MCP: "[WebSocket] Unexpected receive error: WebSocket is not initialised" (McpLog.cs:45). Chưa chạy menu, không có error CS. Ghi lại ở đây trước khi clear Console một lần để đo riêng2 lượt tạo asset; không sửa/tắt cảnh báo trong package hay validator.
2026-11-25 23:56 · Codex → Claude, Người làm · [BÀN GIAO]: Xong sprite/material/menu, trao gậy Claude trên013-theme-ui/HEAD6bfebf3; chưa commit. Đã chạy Unity MCP refresh + menu2 lần, importer Sprite128/RGBA32/Bilinear/Clamp/Uncompressed/max128, shaderSprites/Default/texture đúng, alpha thân.141176/vành.658824/đỉnh.901961, ngoài vòng alpha0. Hai lượt status/hash34file/GUID giống hệt; Console sau2 lượt0Error/0Warning, warning transport lúcrefresh đã ghi riêng. Preview Logs/013-bubble-preview.png (128/64/24 trên navy/checker). Scene pathrỗng/dirtyfalse trước-sau, không Play/lưu scene, không nối game/hạt/prefab; .mcp.json baseline giữ nguyên. Claude review asset/preview/ngân sách trước khi dùng ở014/S5; Codex dừng lái Editor.
2026-11-25 23:59 · Claude → Người làm, Codex · [BÁO]: Review xong qua Unity MCP: APPROVE (đã soi: git status, Console 0/0, texture 128x128 + alpha, importer, material, preview; chưa soi: chạy lại menu lần hai, đọc kỹ code tool). Claude giữ gậy; commit trên 013-theme-ui.
