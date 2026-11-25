# 013-S4b · Sprite bọt xà phòng (phiếu nhỏ cho Codex)     Cỡ: S (≤ 1 giờ)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Claude (giao việc, review), Codex (làm)
Liên quan: 013-s4-bubble-sprite (đã APPROVE, cùng vùng Art/VFX). Nhánh: `013-theme-ui`. Phiếu thử vòng lặp "Claude giao → Codex làm → Claude chấm → yêu cầu làm lại nếu chưa đạt".

## Mục tiêu (nhìn từ người chơi)
Một sprite **cụm bọt xà phòng** để rắc lên xe/mặt đường khi rửa xe: trông như một nắm bọt xốp trắng có khối, đọc được là "bọt" ngay cả khi chỉ to ~32 px trên điện thoại, và không che mất màu xe bên dưới.

## Phạm vi — Làm / Không làm
**Làm** (chỉ `Art/VFX/**` + một file tool Editor mới, `AGENTS.md` §1.3):
1. Texture procedural bằng code Editor (không asset ngoài): `Assets/_Project/Art/VFX/Textures/foam.png`, **128×128** RGBA, nền ngoài cụm hoàn toàn trong suốt.
   - Cụm gồm **5–8 bong bóng/bọt tròn chồng nhau**, kích thước khác nhau (bán kính 14–34 px), xếp thành khối hơi dẹt, **không đối xứng**, tâm cụm gần giữa ảnh.
   - Thân bọt trắng đục (alpha thân 0,80–0,95), **đổ bóng xanh nhạt `#BFD9F2`** ở nửa dưới mỗi bọt để có khối, **viền sáng mảnh** ở mép trên, 1–2 điểm sáng trắng nhỏ trên mỗi bọt lớn. Rìa cụm mềm 1–2 px.
   - Màu chung trắng lạnh, không sặc sỡ.
   - Importer: Sprite (2D and UI), `alphaIsTransparency` bật, không mipmap, Bilinear, Clamp, Uncompressed, `maxTextureSize` 128.
2. Material `Assets/_Project/Art/VFX/Materials/M_Foam.mat` dùng `Sprites/Default` (như `M_Bubble`), `_MainTex` = foam.png.
3. Menu Editor mới `Tools/Squeaky Lane/014 VFX/Create Foam Sprite` trong file mới `FoamSpriteTools.cs`, **idempotent** (chạy 2 lần: file giống hệt, GUID giữ nguyên). Dùng lại hạt giống cố định để ảnh không đổi giữa các lần chạy.
**Không làm:** nối vào game/scene/prefab/hệ hạt; sửa Core/Game/Presentation/Play.unity; package; tải ngoài; commit.

## Tiêu chí xong (đo được) · V2
Claude sẽ đo bằng Unity MCP (không tin báo cáo):
1. Biên dịch sạch, Console 0 lỗi / 0 cảnh báo mới.
2. foam.png đúng 128×128; góc (0,0) và 4 góc alpha = 0; **không pixel nào có alpha > 0,02 cách tâm ảnh hơn 60 px** (không chạm mép ảnh).
3. **Độ phủ**: tỉ lệ pixel alpha > 0,5 nằm trong **28–55%** diện tích ảnh (cụm đủ đặc nhưng không phải đĩa trắng).
4. **Có khối**: độ sáng trung bình nửa trên của vùng alpha > 0,5 **cao hơn** nửa dưới ít nhất 0,04 (có bóng xanh ở dưới).
5. Importer đúng các cờ trên; `M_Foam` dùng `Sprites/Default`, `_MainTex` trỏ foam.png.
6. Chạy menu hai lần: `git status` không đổi, GUID giữ nguyên. Chỉ file trong vùng phiếu đổi.
7. Ảnh xem trước `Logs/013-foam-preview.png`: foam ở 128/64/32 px trên nền navy **và** nền xe màu đỏ (#C0392B), để Claude xem.

## Điều dễ sai
Alpha thân quá đậm → đĩa trắng, hoặc quá nhạt → trông như bong bóng; bóng xanh làm bẩn thành xám; cụm chạm mép ảnh làm viền bị cắt; mọi bọt cùng cỡ nên trông như hạt cườm; chạy lại menu đổi nội dung ảnh (random không cố định).
**Cắt nếu trễ:** điểm sáng nhỏ; viền sáng mảnh.

## Phản đối kế hoạch (trước khi làm)
Không chặn kế hoạch. Rủi ro cụ thể: ghép alpha thông thường của nhiều thân bọt 0,9 sẽ đẩy vùng chồng lên gần 1, che màu xe. Giữ bố cục 7 bọt cố định, ghép màu theo lớp nhưng lấy alpha lớn nhất (thân 0,9, điểm sáng tối đa 0,95); Claude cần chấm độ che màu trên preview đỏ ở 32 px. Không cần nguồn ngẫu nhiên vì toàn bộ tọa độ bake cố định.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch
- **Vòng 2: đã sửa source theo Verdict, chờ Claude chạy lại; chưa đạt V2 cho bản mới.** Chỉ sửa `Assets/_Project/Scripts/Editor/FoamSpriteTools.cs` và phiếu này. Các PNG/material/meta đã có khi nhận vòng 2 do Claude tạo ở vòng 1; Codex không sửa chúng. Không gọi Unity MCP, chạy menu, Play, lưu scene hoặc chạy Unity CLI/compiler trong vòng 2.
- **Yêu cầu 1 — vành riêng:** thay cung trắng cũ bằng vành Gaussian có bề rộng nửa cường độ **1,25 px**, đặt bên trong cạnh mềm 1,5 px. Nửa dưới trộn nét `#9FC4E8` với độ phủ **0,6**, nửa trên trắng; chuyển tiếp theo tọa độ y của từng bọt. Độ phủ 0,6 là alpha của nét màu trên thân, không cộng vào alpha PNG. Ghép lớp sau→trước giữ đường vành của phần bọt lộ ra.
- **Yêu cầu 2 — rìa gập ghềnh:** thêm **8 bọt nhỏ** bán kính **4–9 px**, tọa độ cố định, quanh rìa trái/trên/phải/dưới. Vẽ sau các bọt lớn để bọt nhỏ không bị thân lớn phủ mất.
- **Yêu cầu 3 — cỡ và bố cục:** dàn lại 7 bọt lớn bán kính **20–30 px** theo khối ngang, bỏ bọt riêng ở đỉnh cao. Cả cụm 15 bọt có cỡ **4–30 px**; ước lượng hộp vùng alpha>0,02 rộng112×cao75px, **1,493:1**. Không có RNG hoặc thay đổi trạng thái Random.
- **Yêu cầu 4 — lộ màu xe:** `BodyAlpha` **0,9 → 0,8**; tiếp tục lấy alpha lớn nhất giữa các lớp để vùng chồng không thành đĩa đặc. Hai điểm sáng nhỏ/bọt lớn giữ tối đa0,95; bóng thân vẫn #BFD9F2.
- **Yêu cầu 5 — giữ tiêu chí:** không sửa menu, importer, material, thuật toán ghi khi byte khác hoặc preview128/64/32 trên navy/đỏ. **[Ước lượng ngoài Unity]** raster JavaScript đọc tọa độ từ source, mô phỏng công thức và lượng tử hóa RGBA8: độ phủ alpha>0,5 **37,4512%**; bán kính xa nhất alpha>0,02 **56,0045 px**; chênh luma sRGB trên−dưới **0,07985**; alpha tối đa **0,9490**; bốn góc **0**. Cả15 bọt còn pixel vành nửa cường độ nhìn thấy sau chồng lớp (12–144pixel/bọt). Đây là ước lượng toán học, không phải số đo PNG Unity hoặc xác nhận visual.
- **[Đã kiểm source/repo]** Đọc tĩnh phần sửa; giữ nguyên `[MenuItem]` theo kết quả compile vòng1 của Claude. `git diff --check` và `git diff --no-index --check -- NUL Assets/_Project/Scripts/Editor/FoamSpriteTools.cs` không báo lỗi khoảng trắng; Git chỉ nhắc chuẩn hóa LF→CRLF. `git status` có đúng danh sách untracked như lúc nhận việc; tracked diff rỗng. Không commit/push, không thay `.meta`/GUID/asset/scene/config/runtime.
- Lệch phạm vi ban đầu 5–8 bọt là có chủ đích theo Verdict: nay7 bọt lớn+8 bọt nhỏ. Không cần sửa `PROJECT.md`: chỉ chỉnh thuật toán bake của tool đang review; chưa áp bản mới vào game, không đổi sự thật được ghi trong tài liệu đó.
## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- Giả định đo chênh sáng bằng luma sRGB có trọng số như số0,083 Claude báo vòng1. Mô phỏng JavaScript không khớp tuyệt đối float32 của Unity; cần Claude đo PNG thật.
- **Chưa chạy vòng2:** Unity compile/Console, menu tạo PNG/material/preview, đo lại7 tiêu chí, menu lần2 so byte/GUID/status và review hình32px. PNG/material hiện trên đĩa vẫn là kết quả vòng1. Không nhận kết quả compile sạch vòng1 là bằng chứng cho source vòng2.
- Rủi ro cần Claude chấm: nét1,25px ở ảnh128 chỉ còn khoảng0,31px khi thu xuống32; phép đếm vành trong mô phỏng không chứng minh mắt đọc ra từng bọt. Preview đỏ cần xác nhận thân0,8 đủ lộ màu xe và cụm không còn giống mây. Nếu nét mất khi thu nhỏ, Claude quyết định có cần tăng độ tương phản sau khi xem ảnh; Codex chưa tự tăng bề rộng vượt yêu cầu.
- Tự tin **~85% về phần mã và ràng buộc số theo mô phỏng**, chưa chấm đạt visual. Không phản đối yêu cầu sửa vòng2; không phát hiện ngoài phạm vi cần sửa. `.mcp.json` và toàn bộ asset/meta có sẵn được giữ nguyên.
- Claude nhận lại Editor, refresh/compile, chạy menu hai lần, đo PNG/importer/material/SHA/GUID và xem preview32px theo bàn giao; đây là bước kiểm còn lại đã được phân công ở dòng giao vòng2, không phải yêu cầu thêm quyền.
## Bạn cần hiểu (2–4 gạch)
- PNG dùng straight alpha; `Sprites/Default` tự nhân alpha trong shader. Nhân RGB trước khi lưu PNG sẽ khiến mép bị tối.
- Lấy alpha lớn nhất ở vùng chồng giữ thân0,8; ghép alpha kiểu thông thường của hai bọt0,8 sẽ thành0,96 và che màu xe nhiều hơn. Độ phủ nét xanh0,6 chỉ trộn RGB lên thân, không cộng thêm alpha PNG.
- Idempotent cần cả dữ liệu ảnh cố định và chỉ lưu khi thay đổi; kiểm bằng byte/GUID sau hai lần chạy, không chỉ nhìn hai ảnh giống nhau.
- Vành nằm trong cạnh mềm giúp tách từng bọt khi chồng lên nhau; bọt nhỏ vẽ sau giúp phá đường bao giống mây. Thu xuống32px vẫn cần mắt chấm vì số đo ở128px không chứng minh độ rõ khi thu nhỏ.

--- Người review điền ---
## Verdict: CHANGES (vòng 1, Claude)
**Số đo Claude tự chạy trong Unity (MCP):** 128×128, 4 góc alpha 0, bán kính lớn nhất 53,2 (≤60), độ phủ 41,8% (28–55), chênh sáng trên−dưới 0,083 (≥0,04), importer và M_Foam đúng. Khớp ước lượng raster của Codex. **Compile thật sạch** (lỗi CS0616 trong log Codex là báo sai của bước kiểm ngoài Unity, đừng sửa `[MenuItem]`).
**[CHẶN] Ý đồ chưa đạt:** nhìn ảnh `Logs/013-foam-preview.png` thì cụm giống **đám mây hoạt hình** hơn là bọt xà phòng: các bọt chảy vào nhau thành khối trơn, không thấy từng bong bóng, đường viền ngoài mượt như mây, có một mái vòm lớn ở trên.
**[NÊN] Yêu cầu sửa (số cụ thể):** (1) mỗi bọt có **viền riêng thấy được**: vành mảnh 1–1,5 px màu `#9FC4E8` alpha ≈ 0,6 ở rìa dưới và vành sáng trắng ở rìa trên, để đọc ra từng bong bóng; (2) thêm **6–10 bọt nhỏ** (bán kính 4–9 px) bám quanh rìa làm đường viền gập ghềnh; (3) cỡ bọt khác nhau rõ hơn (nhỏ nhất ~4 px, lớn nhất ~30 px), bỏ mái vòm đơn: cụm **dẹt hơn, rộng:cao khoảng 1,5:1**; (4) hạ alpha thân xuống ≈ 0,72–0,85 để màu xe hơi lộ qua; (5) giữ nguyên 7 tiêu chí số đo.
**Đã soi:** số đo, importer, material, compile, ảnh preview. **Chưa soi:** chạy menu lần 2 so GUID (làm sau khi Codex sửa).

## Verdict vòng 2: APPROVE có điều kiện (Claude, qua Unity MCP)
**Đo trong Unity:** 128×128, 4 góc alpha 0, bán kính lớn nhất 56,0 (≤60), độ phủ 37,5% (28–55), chênh sáng trên−dưới 0,080 (≥0,04), bbox rộng:cao 1,49 (yêu cầu ≈1,5), tâm alpha 0,80. Chạy menu lần 2: md5 `foam.png` và `M_Foam.mat` giống hệt, GUID giữ nguyên; Console 0 lỗi / 0 cảnh báo.
**Đã đạt so với vòng 1:** có 8 bọt nhỏ bám rìa làm viền gập ghềnh, cụm dẹt, bỏ mái vòm, thân hơi trong. **[GỢI Ý] Còn lại:** phần thân giữa vẫn là khối trơn, vành riêng từng bọt lớn mờ nên ở cỡ lớn vẫn hơi giống mây; ở 24–48 px (cỡ dùng thật) đọc ổn. Không gọi vòng 3: nếu lên hình thật 014 thấy chưa ưng thì Claude tự chỉnh.
**Chưa soi:** trong hệ hạt thật (014).

## Trao đổi
2026-11-26 00:08 · Claude → Codex · [BÀN GIAO]: Giao phiếu này cho Codex qua tools/codex-run.sh (HEAD a0c77ae). Codex lái Editor, Claude chỉ đọc. Claude sẽ chấm bằng số đo ở mục Tiêu chí; chưa đạt thì Claude yêu cầu làm lại, không tự sửa.
2026-11-26 00:12 · Codex → Claude · [BÀN GIAO]: Trả gậy trên 013-theme-ui, HEAD a0c77ae, không commit. Đã viết FoamSpriteTools.cs; ước lượng raster phủ41,84%, bán kính53,20px, chênh sáng0,07665. MCP refresh bị từ chối vì cần approval nhưng policy=never; Editor chưa compile source, chưa tạo PNG/material/preview/meta, chưa đạt V2. Console chỉ đọc0 lỗi/cảnh báo không đại diện compile mới. Claude cần chạy refresh/menu2 lần, đo byte/GUID/importer và review32px; chi tiết và [HỎI] ở phần AI làm điền.
2026-11-26 00:14 · Claude → Codex · [BÀN GIAO]: VÒNG 2 (CHANGES). Gậy về Codex nhưng CHỈ SỬA CODE: sửa `FoamSpriteTools.cs` theo mục Verdict, KHÔNG gọi Unity MCP, KHÔNG chạy menu. Claude sẽ refresh, chạy menu và đo lại. Có thể tự thử thuật toán bằng raster ngoài Unity, nhưng ghi rõ đó là ước lượng.
2026-11-26 00:16 · Codex → Claude · [BÀN GIAO]: Vòng2 trả gậy trên013-theme-ui, HEAD a0c77ae, không commit. Chỉ sửa FoamSpriteTools.cs và điền phiếu: vành1,25px xanh #9FC4E8 độ phủ0,6/trắng trên; thêm8 bọt nhỏ4–9px; dàn7 bọt lớn20–30px thành cụm1,493:1; alpha thân0,9→0,8. Ước lượng raster RGBA8 phủ37,45%, bán kính56,00px, chênh sáng0,07985, góc0; đã đọc tĩnh/kiểm khoảng trắng, chưa chạy Unity/MCP/menu/đo PNG/so GUID. Claude cần compile, chạy menu2 lần, đo7 tiêu chí và chấm preview32px; rủi ro nét chỉ còn0,31px khi thu nhỏ. PNG/material/meta trên đĩa vẫn là vòng1, giữ nguyên.
2026-11-26 00:18 · Claude → Người làm, Codex · [BÁO]: Vòng 2 APPROVE có điều kiện (xem Verdict vòng 2): số đo đạt hết, menu idempotent (md5/GUID giống), Console 0/0. Claude chạy menu thay Codex vì Codex trong codex exec không có quyền duyệt refresh_unity.
