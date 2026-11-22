# PROJECT.md — SQUEAKY LANE (bản Unity 3D)

> Game puzzle mobile: **chạm cho xe lăn ra khỏi bãi, qua đúng cổng rửa cùng màu**.
> Tài liệu này dành cho AI (Claude, Codex) đọc trước khi làm việc. Ngày cập nhật: 2026-11-22.
> Prototype HTML v10 (31 màn) chỉ là **tham chiếu luật và công cụ sinh/kiểm màn**, không phải đặc tả cuối. Khi tài liệu này và prototype lệch nhau, tài liệu này (đã được người làm xác nhận) thắng, trừ phần "luật lõi" ở mục 5 mà prototype (`logic.js`) là nguồn đối chiếu.

**Nhãn bằng chứng dùng trong file:**
`[CHỐT]` người làm đã nói rõ · `[FILE]` đọc từ prototype · `[ĐỀ XUẤT]` AI đề xuất, chưa được duyệt · `[CHƯA CHỐT]` còn mở, cần hỏi.
Dòng nào không có nhãn là mô tả, không phải quyết định.

---

## 1. Bối cảnh và mục tiêu

- **Người làm:** sinh viên PTIT, thực tập sinh Unity dev, **làm một mình**. `[CHỐT]`
- **Mục đích:** bài kiểm tra để qua vòng thực tập. Không kiếm tiền, không phát hành. Mục tiêu chính: cho các anh lead thấy **game này làm được gì và thắng người chơi bằng cái gì**. `[CHỐT]`
- **Người duyệt:** anh lead của công ty game puzzle. Góp ý đã có: mỗi màu xe một dáng riêng, thao tác chạm mượt hơn vuốt, nên làm 3D để xe là nhân vật IP, level design cần hấp dẫn hơn, **ít blocker hơn**, cơ chế phải ghép được với nhau. `[CHỐT]`
- **Hạn chót: 30/11/2026** (tính từ 22/11 còn 8 ngày). `[CHỐT]`
- **Phạm vi bản này:** lõi chơi + visual + VFX + SFX + cảm giác (feeling). **Chưa làm:** menu, cài đặt, shop, meta, quảng cáo, analytics. `[CHỐT]`
- **Nội dung:** **25 màn** (gồm tutorial), làm thủ công từng màn (không sinh ngẫu nhiên lúc chạy). Ưu tiên hiện tại: **làm đủ 25 màn trước**; tool dựng màn trong Editor và phần setup nâng cao **làm sau**. Màn định nghĩa bằng JSON + file config (mục 7.5). `[CHỐT]`

---

## 2. Cách làm việc giữa người và AI

### 2.1. Vai trò `[CHỐT]`
| Ai | Làm gì |
|---|---|
| **Người làm (điều phối)** | Quyết định cuối cùng. Thao tác trong Unity Editor, commit, push, merge. Mang thông tin qua lại giữa hai AI. |
| **Claude** | Lên kế hoạch, quyết định visual và feeling, phân tích cảm giác chơi, **sửa trực tiếp** (visual, và cả code khi Codex không làm được), **review code và quyết định khi nào đủ tốt để push**. Vẽ minh hoạ/ý tưởng riêng với người làm (ngoài luồng repo). |
| **Codex** | Viết code ổn định, setup Unity cơ bản, làm các việc lớn theo plan của Claude. |

Hai AI **không nói chuyện trực tiếp với nhau**. Mọi thứ đi qua người làm.
`[ĐỀ XUẤT]` Vì vậy cần "phiếu bàn giao" ngắn nằm trong repo (`docs/handoff/NNN-<việc>.md`): Claude ghi mục tiêu, phạm vi, tiêu chí xong, điều cấm; Codex đọc phiếu, làm, ghi lại đã làm gì và chưa chắc gì; Claude review dựa trên diff + phiếu. Người làm không phải chép tay. Cuối phiếu có mục **Trao đổi** (chỉ thêm dòng, thẻ `[HỎI]` `[TRẢ LỜI]` `[BÀN GIAO]` `[BÁO]`) để hai AI hỏi–đáp–bàn giao; chat mới đọc phiếu để biết việc đang ở đâu (`AGENTS.md` §1.6). `[CHỐT 22/11]`

### 2.2. Quy tắc bắt buộc với AI `[CHỐT]`
1. **Không chiều theo người làm.** Người làm là người mới; AI phải **phản biện và tranh luận** khi thấy ý kiến sai/rủi ro/mâu thuẫn, nêu lý do và bằng chứng, rồi mới theo quyết định (nếu người làm vẫn giữ). Không "thông đồng", không khen cho vui.
2. **Hỏi người làm để duyệt** trước các thay đổi ở danh sách "phải hỏi" (2.3). Sửa lỗi nhỏ có thể sửa thẳng hoặc hỏi cách sửa.
3. **Quy tắc trong tài liệu này có thể sai hoặc rườm.** Người làm là người mới, AI thấy quy tắc nào vô lý/thừa/mâu thuẫn thì **nói ra và bàn lại**, không im lặng làm theo.
4. **Việc thất bại hoặc vượt giới hạn → dừng lại và nói chuyện với người làm**, không tự vòng vèo/làm tắt phá hoại.
5. Luôn trả lời bằng **tiếng Việt**. Code sạch, chia nhỏ class, comment tiếng Việt giải thích rõ; khi giao code mẫu thì kèm cấu trúc thư mục. `[CHỐT — sở thích đã lưu]`

### 2.3. Ranh giới tự làm / phải hỏi `[CHỐT]`
- **Tự làm trên nhánh riêng:** sửa/viết code, chạy test, viết tài liệu trong nhánh đó.
- **Phải hỏi trước:** đổi package hoặc Project Settings; xóa asset; thêm asset bên ngoài; đổi luật chơi; merge vào `main`.
- **AI (cả Claude và Codex) được sửa trực tiếp scene/prefab** vì hạn 30/11, theo hàng rào ở `AGENTS.md` §4.2 (ưu tiên qua Editor/menu Editor; sửa YAML tay chỉ khi không có Editor và thay đổi nhỏ). `[CHỐT]`

### 2.4. Git `[CHỐT]` + `[ĐỀ XUẤT]` phần chi tiết
- **Việc nhỏ** (cỡ S, ≤ 2 file code/config/tài liệu, không đụng luật/asset/scene/prefab/package/settings): **sửa và commit thẳng**, không nhánh riêng. `[CHỐT]`
- **Việc lớn**: **một nhánh mới**, đặt theo **mã việc**, không theo AI: `NNN-<mô-tả>` (ví dụ `004-hint-button`). `NNN` tăng theo thứ tự mở việc để xem lại nhánh cũ theo trình tự; cả hai AI cùng commit lên nhánh của việc đó theo luật "người cầm gậy". `[CHỐT]` Sửa lỗi/làm lại một việc đã có thì đặt `NNN-MM-<mô-tả>` (ví dụ `003-01-fix-undo-timer`), đi bằng nhánh riêng từ `main` để nhánh việc đang làm bỏ được mà không mất bản sửa; chi tiết ở `AGENTS.md` §8. `[CHỐT]` AI nào tham gia ghi ở phiếu và dòng cuối commit.
- AI **không tự commit/push**; chỉ đưa sẵn nội dung commit để người làm dán. Commit message kết thúc bằng dòng ghi AI tham gia (theo cấu hình của công cụ).
- Không merge vào `main` khi Claude chưa review. `[ĐỀ XUẤT]` review = đọc diff + đối chiếu phiếu bàn giao + người làm gửi ảnh/video Game view (Claude không chạy được Unity Editor).
- Khi phiên làm việc có repo, người làm sẽ mở chat riêng gắn repo để Claude xem nhánh/diff. `[CHỐT]` (chat này chỉ để lấy ý tưởng và kết luận tổng thể).

### 2.5. Làm việc với Unity Editor `[ĐỀ XUẤT kế thừa từ cách làm cũ của người làm, cần xác nhận lại]`
- Người làm chơi thử, build APK và commit. AI được sửa trực tiếp scene/prefab (`AGENTS.md` §4.2); với việc cần thao tác tay trong Editor thì AI viết từng bước cụ thể hoặc viết menu Editor để người làm bấm.
- Không sửa tay YAML scene/prefab khi Editor đang mở.
- Nếu một sửa đổi phải đợi người làm chạy Play mới kiểm được thì AI ghi rõ "chưa kiểm chứng" thay vì khẳng định.
- Khi sự thật của dự án đổi (luật, thư mục, tool…), **sửa PROJECT.md trong cùng commit**.

### 2.6. Tài liệu quy tắc
Đã tách xong (22/11): quy tắc làm việc chung ở `AGENTS.md`, phần riêng ở `CLAUDE.md` và `CODEX.md`. Mục 2 này chỉ giữ bản tóm tắt; hai file mâu thuẫn nhau thì dừng và hỏi người làm (đầu `AGENTS.md`). `[CHỐT]`

---

## 3. Công nghệ và nền tảng

| Mục | Nội dung |
|---|---|
| Engine | Unity, C#. `[CHỐT]` |
| Phiên bản Unity / pipeline | Khởi tạo từ **project 3D trống**; hiện có khung assembly Core/Game/EditMode của việc 001. Chi tiết ở mục 3.1 và 8. `[FILE]` |
| Thiết bị đích | **Android, cài được bằng APK** để lead tự cài chơi. `[CHỐT]` iOS: `[CHƯA CHỐT]` (chưa thấy nhu cầu). |
| Màn hình | Điện thoại, **màn dọc**, chơi một tay một ngón. `[CHỐT]` |
| Hiệu năng | Tối thiểu **30 fps**, mục tiêu **60 fps**. `[CHỐT]` |
| Dung lượng build | **< 250 MB**. `[CHỐT]` |
| Input | **Input Manager cũ** (xem 3.1). Dự án chưa có package Input System; đổi sang Input System là đổi package, phải hỏi trước. |
| Công nghệ visual thêm (shader, post-processing…) | Chưa quyết, người làm nhờ AI chỉnh nếu sai. `[CHỐT]` — AI nên bắt đầu đơn giản (low-poly, vật liệu phẳng, bóng mềm) và chỉ thêm khi cần. |
| Asset | Tìm trên mạng hoặc dùng AI tạo; linh hoạt vì chỉ một người làm. `[CHỐT]` |

### 3.1. Thông tin project Unity (điền dần)

Người làm đã tạo project 3D trống và sẽ bổ sung thông tin sau. **Quy tắc cho AI:** khi đọc được hoặc được cho biết giá trị thật (từ `ProjectSettings/ProjectVersion.txt`, Package Manager, Player Settings, `git remote -v`…), **điền vào bảng này trong cùng commit**, đổi `[CHƯA ĐIỀN]` thành `[FILE]` kèm nguồn. **Không đoán.** Không đọc được thì hỏi người làm.

| Mục | Giá trị |
|---|---|
| Phiên bản Unity | `2022.3.62f2` `[FILE: ProjectSettings/ProjectVersion.txt]` |
| Template / render pipeline (3D Built-in hay URP) | **3D Built-in**: `Packages/manifest.json` không có URP/HDRP, `GraphicsSettings.asset` có `m_CustomRenderPipeline: {fileID: 0}` `[FILE]` |
| Input (Input Manager cũ hay Input System) | **Input Manager cũ**: `activeInputHandler: 0` trong `ProjectSettings/ProjectSettings.asset`; không có package Input System `[FILE]` |
| Repo / remote Git, nhánh chính | `https://github.com/buituanthanh02/Squeaky-Lane.git`, nhánh chính `main` `[FILE: git remote -v]`. **Không dùng Git LFS** (`.gitattributes` chỉ có `* text=auto`); chỉ bật khi có asset 3D/âm thanh nặng `[CHỐT 22/11]` |
| Tên thư mục gốc project, tên scene chính | Thư mục gốc `Squeaky Lane`; scene `Assets/Scenes/SampleScene.unity` (chỉ có Main Camera + Directional Light; chưa nằm trong Build Settings: `EditorBuildSettings.asset` có `m_Scenes: []`) `[FILE]` |
| Package đã thêm (ngoài mặc định) | **Không có.** Chỉ có package mặc định của template (`com.unity.feature.development`, TextMeshPro, Timeline, ugui, Visual Scripting…). `com.unity.test-framework 1.1.33` có sẵn trong `packages-lock.json` nên dùng được EditMode test `[FILE: Packages/manifest.json, packages-lock.json]` |
| Android: package name, scripting backend, min API | `companyName: DefaultCompany`, `productName: Squeaky Lane`. **Package name chưa đặt** (`applicationIdentifier: {}`, đang mặc định) `[CHƯA ĐIỀN]`. **Scripting backend chưa đặt** (`scriptingBackend: {}`, đang mặc định) `[CHƯA ĐIỀN]`. Min API 22, target API 0 (tự động). Kiến trúc `AndroidTargetArchitectures: 1` (hiểu là chỉ ARMv7) `[CHƯA KIỂM CHỨNG]`. Xoay màn hình `defaultScreenOrientation: 4` (tự xoay, **chưa khóa dọc**). `[FILE: ProjectSettings/ProjectSettings.asset]`. Cần quyết trước khi build APK đầu tiên: package name, khóa Portrait, ARM64 (đổi Project Settings là việc phải hỏi) |
| Quy ước đặt tên / ghi chú khác | Cây thư mục và đặt tên: theo mục 8. Mã nguồn prototype để đối chiếu nằm ở `docs/reference/prototype/` (ngoài `Assets`, Unity không biên dịch). `.gitignore` đã có (bỏ `Library`, `Logs`, `UserSettings`, `.vscode`, `*.sln`, `*.apk`, keystore…) `[FILE]` |

**Ghi chú về asset và IP** `[ĐỀ XUẤT, cần bàn]`: người làm nghiêng về IP nhân vật kiểu phim *Cars*. Dùng nhân vật/asset thật của *Cars* thì vi phạm bản quyền; cho bài thực tập nội bộ có thể chấp nhận để minh hoạ, nhưng **không nên đưa vào build chính thức**. Nên làm nhân vật riêng "lấy cảm hứng" (xe có tính cách qua chuyển động). Hiện chưa có cốt truyện, làm lõi trước. `[CHỐT]` Quyết định cũ "không vẽ mặt lên kính xe" (bị đánh giá creepy) **vẫn giữ**: chỉ dùng ngôn ngữ cơ thể (nhún, chúi, nảy, rũ bùn). `[CHỐT]`

---

## 4. Camera và hình học 3D

- Hướng 3D. Luật gần như không đổi so với 2D. `[CHỐT]`
- **Camera cố định**, có thể đổi sau khi có góp ý. `[CHỐT]`
- **Góc nghiêng 60–70° được chấp nhận.** `[CHỐT]` `[ĐỀ XUẤT]` mặc định **65°, camera trực giao (orthographic)**, để chỉnh bằng một biến trong prefab camera.
- Nguyên tắc chọn góc: nếu góc nghiêng làm che mất thông tin chơi (cơ chế) thì dùng nhìn thẳng từ trên xuống; còn nếu vẫn đọc được thì giữ nghiêng để thấy ngôn ngữ cơ thể của xe. `[CHỐT]`
- `[ĐỀ XUẤT]` Quy tắc chống che: vật cao (trụ xoay, đèn giao thông, cổng/barie phía dưới) làm thấp còn ~60–70% so với thực tế; vật nào che xe thì tự mờ dần (fade).
- Cạm bẫy 3D: chạm "nửa xe" phải tính trên **trục thân xe trong không gian bàn chơi**, không phải theo nửa trên/dưới màn hình (xem mục 6.2).

---

## 5. Luật chơi cốt lõi `[FILE]` (nguồn đối chiếu: `logic.js` của prototype)

- **Bãi** là lưới `cols × rows`. Mép bãi có **cổng**. Xe dài 2 ô (xe tải dài 3), nằm ngang hoặc dọc, **chỉ chạy dọc thân (tới/lùi) và chạy tới khi vướng**.
- **Cổng màu X:** xe màu X **và sạch bùn** thì chạy xuyên qua → được rửa, rời bãi. Không thì dừng sát mép.
- **Thắng** khi mọi xe ra khỏi bãi. **Kẹt xe** = không còn đường thắng từ trạng thái hiện tại.
- **Trạng thái bất biến:** `Move()` trả trạng thái mới + danh sách sự kiện (`step, paint, mud, spray, water, exit, stop, splash, push, tele`; bàn xoay và trụ xoay có sự kiện riêng `rotate`, `pyl` do `rotate()`, `rotatePylon()` trả về). Hoàn tác = giữ lại trạng thái cũ. Phần hình ảnh chỉ đọc sự kiện, không tự suy luận lại luật.
- Mọi lệnh có thể ra ngay (nhiều xe chạy song song); luật tính ô trống tại lúc ra lệnh, phần hình ảnh đặt chỗ đường đi để xe không chồng hình.

### 5.1. Bộ cơ chế cho bản 25 màn

| Cơ chế | Quyết định | Ghi chú |
|---|---|---|
| Cổng màu (rửa + ra) | **Giữ** (lõi) | |
| **Bùn bám** (mud 1) | **Giữ** `[CHỐT]` | Xe còn bùn không qua được cổng màu. |
| **Vòi phun / cổng nước** (xịt sạch rồi đẩy ngược vào bãi) | **Giữ** `[CHỐT]` | Làm sạch bùn trước cổng màu. Nếu sau này bỏ ô vòi xịt trong bãi thì đây là nơi duy nhất làm sạch → màn có bùn phải có cổng nước. |
| **Vũng bùn** | **Giữ** `[CHỐT]` | Nguồn tạo bùn bám. |
| **Bàn xoay** (2×2) | **Giữ** `[CHỐT]` | Xoay 90° bằng **nút ở trung tâm**, xem mục 6.3. |
| **Cọc** | **Giữ** `[CHỐT]` | |
| **Đèn giao thông** (đổi sau mỗi lượt) | **Giữ** `[CHỐT]` | |
| **Cổng tường (portal)** | **Giữ** `[CHỐT — nói từ vòng trước]` | Xe trồi nguyên thân ở cổng B vuông góc mặt tường B; tối đa 1 lần chui mỗi lượt. |
| **Xe tải đẩy** | **Giữ** `[CHỐT — từ vòng trước]` | Đẩy cả hàng phía trước đúng 1 ô nếu có chỗ. |
| **Xoay 90°** | **Giữ** `[CHỐT]` | Gồm trụ xoay (chạm = xoay thêm 90° một chiều, 4 chạm về chỗ cũ) và bàn xoay. Nếu người làm muốn chỉ một trong hai thì báo lại. |
| Bùn dày "?" (che màu) | **Bỏ khỏi 25 màn** `[CHỐT — lead muốn ít blocker]` | Khó đọc trong 3D. Người làm xác nhận lại 22/11: bỏ. Trong luật, bùn dày chỉ là dữ liệu `mud: 2` (luật chỉ xét `mud > 0`; "che màu" là việc của hiển thị), nên bỏ khỏi màn không tốn gì ở Core. |
| Ô vòi xịt trong bãi · Bùn văng · Vũng sơn | **Tạm giữ, bỏ sau nếu cần** `[CHỐT — "cứ để lại, sẽ bỏ sau"]` | Xem quy tắc ưu tiên bên dưới. |
| Barie đếm xe · Barie liên động (chốt A/B) | **Chưa đưa vào 25 màn**; còn thời gian thì có thể thêm `[CHỐT 22/11]`. Core vẫn port đủ (prototype đã có `barriers`, `switches`) | `[ĐỀ XUẤT]` **chưa đưa vào 25 màn**: đèn giao thông và cọc đã đóng vai blocker; barie liên động cần thêm nhiều visual (chốt, hai barie, trạng thái) và khó đọc trong 3D. Thêm lại nếu còn thời gian. |

**Hệ quả:** 25 màn mới được sinh/kiểm lại với bộ cơ chế ở bảng trên (mục 7), không dùng lại nguyên 31 màn cũ (thừa cơ chế, nhiều màn bãi lớn).

**Quy tắc ưu tiên `[ĐỀ XUẤT]`:** phần **luật** (Core, C#) port từ prototype gần như không tốn thêm, nên Core **giữ đủ mọi luật**. Thứ tốn thời gian là **visual + VFX + SFX + tutorial cho từng cơ chế trong 3D**. Vì vậy chia hai bậc:
- **Bậc A (làm kỹ):** cổng màu, bùn bám, vòi phun, vũng bùn, cọc, đèn giao thông, bàn xoay, cổng tường, xe tải, trụ xoay.
- **Bậc B (có luật, làm visual nếu còn thời gian):** ô vòi xịt, bùn văng, vũng sơn.
25 màn chủ yếu dùng bậc A; chỉ dùng bậc B khi visual đã sẵn. Cơ chế nào về sau bị bỏ thì gỡ khỏi dữ liệu màn, không cần sửa Core.

---

## 6. Điều khiển và luật thời gian

### 6.1. Điều khiển (một ngón, một tay) `[CHỐT]`
Hỗ trợ **cả hai**:
1. **Chạm nửa xe:** chạm nửa nào, xe chạy về đầu đó.
2. **Chạm vào xe rồi vuốt** theo hướng muốn đi.
Camera cố định. Hoàn tác, chơi lại luôn có.

### 6.2. Kỹ thuật cho chạm nửa xe `[ĐỀ XUẤT]`
Raycast vào xe → lấy điểm trúng → chiếu lên **trục thân xe trong không gian bàn chơi** → so với tâm xe để biết nửa đầu hay nửa đuôi. Vuốt: bắt đầu trên xe, lấy hướng vuốt chiếu lên trục thân xe, vượt ngưỡng (vài chục pixel) mới tính. Chạm nhẹ (không vuốt) = chạm nửa xe.

### 6.3. Bàn xoay: nút ở trung tâm `[CHỐT]` + chi tiết `[ĐỀ XUẤT]`
- Một **nút đủ to ở tâm bàn xoay** để chạm xoay 90°.
- Xe dài 2 đứng trên bàn 2×2 che hơn nửa bàn, tâm bàn nằm sát mép thân xe → dễ bấm nhầm thành "cho xe chạy". Do đó: nút vẽ **trên lớp UI/world-space phía trên xe**, vùng chạm **ưu tiên nút hơn xe**, vùng chạm lớn hơn icon (≥ ~9 mm trên màn hình thật). Phải thử trên điện thoại thật.
- Trụ xoay (cơ chế tường) thì chạm thẳng vào trụ, không có xung đột.

### 6.4. Thời gian và sao `[CHỐT]` + số liệu `[ĐỀ XUẤT]`
- **Có đồng hồ đếm ngược; hết giờ = thua.** Lý do người làm đưa ra: luật hiện tại dễ cho xe chạy quá tầm làm kẹt, nếu không có áp lực thì người chơi chỉ việc thử sai đến khi qua.
- **Hoàn tác không giới hạn số lần nhưng mỗi lần trừ giờ.**
- **Sao theo thời gian:** chỉ để thưởng người muốn giải nhanh, không phải yếu tố chính.
- `[ĐỀ XUẤT]` ban đầu: `Tổng giờ = 20 s + 5 s × (số nước ngắn nhất do solver tính)`, tinh chỉnh sau playtest. Mỗi lần hoàn tác trừ ~3 s. 3★ nếu thắng khi còn ≥ 50% giờ, 2★ nếu còn ≥ 20%, 1★ nếu thắng. Các màn tutorial (vài màn đầu) **không tính giờ**. Đồng hồ **tạm dừng** khi hiện hộp thoại (thắng, thua, tutorial).
- **Ghi chú tranh luận:** nguyên tắc cũ của người làm là "khó do luật, không do giờ". Bản này đổi sang có giờ. Rủi ro: câu đố cần nghĩ (cổng tường, trụ xoay) sẽ bị áp lực giờ làm mất vui. Cách giảm: giờ rộng, không tính giờ ở màn học cơ chế mới, chỉnh theo thời gian người thật chơi chứ không theo số nước.
- **Chấp nhận thua khi hết giờ** (đã xác nhận lại). `[CHỐT]`
- **Gợi ý: có**, và phải tốn kém: trừ giờ **hoặc** giới hạn số lần như booster. `[CHỐT]` `[ĐỀ XUẤT]` mặc định nhẹ cả hai: tối đa 3 lần/màn, mỗi lần trừ ~5 s; cả hai là biến trong file config (7.5) để chỉnh hoặc tắt. Dùng solver sẵn có. Màn tutorial không giới hạn.
- **"Kẹt xe"**: hộp thoại báo khi hết đường thắng (đồng hồ vẫn chạy, người chơi biết cần hoàn tác sớm). **Giữ** `[CHỐT 22/11]`.

---

## 7. Level

### 7.1. Số lượng và cách làm `[CHỐT]`
- **25 màn**, gồm tutorial. Ít màn khó; chủ yếu để chơi và giới thiệu cơ chế bằng cảm giác. Làm thủ công từng màn.
- Về sau cần **tool để dựng màn** nhanh.

### 7.2. Cách làm 25 màn trong ~9 ngày `[CHỐT — người làm đã duyệt]`
1. Dùng lại công cụ sinh/kiểm của prototype (Node, `tools/mech.js`, `CW.solve`) với **bộ cơ chế ở 5.1** để ra 25 màn **được kiểm là giải được**, mỗi cơ chế xuất hiện có chứng minh "tắt đi thì vô nghiệm" (ablation). Claude làm bước này trong workspace riêng, xuất JSON.
2. Unity chỉ cần **bộ nạp JSON → dựng bãi** (không đặt tay từng xe trong scene). Người làm vẫn chỉnh tay bằng cách sửa JSON/duyệt màn.
3. Xuất kèm **"số nước ngắn nhất" cho từng màn** làm dữ liệu chuẩn (golden file) để đối chiếu solver C# (mục 8) và tính giờ/sao.
4. Tool dựng màn trong Editor và phần setup nâng cao **làm sau**, khi đã đủ 25 màn. `[CHỐT]`
Lý do: dựng tay 25 màn trong Unity + tự kiểm giải được là phần dễ sai và tốn thời gian nhất.

### 7.3. Đường cong học `[ĐỀ XUẤT]`
Màn 1–6: tutorial từng cơ chế (cổng màu → cổng nước + bùn → cọc → đèn → bàn xoay) · 7–15: kết hợp 2 cơ chế · 16–25: cổng tường, trụ xoay, xe tải và vài màn khó. Mỗi màn tối đa 2–3 cơ chế. Mỗi cơ chế mới có màn giới thiệu và tutorial bằng hình (bàn tay + vòng sáng + mũi tên; chữ một dòng ngắn).

### 7.4. Định dạng dữ liệu màn (JSON) `[FILE — rút gọn, bỏ phần cơ chế đã loại]`
```jsonc
{
  "name": "Trụ xoay", "cols": 5, "rows": 5,
  "gates":  [{ "side": "t|b|l|r", "i": 1, "color": "pink|blue|yellow|green|orange|purple|water" }],
  "cars":   [{ "axis": "h|v", "r": 1, "c": 0, "color": "blue", "len": 2, "truck": false, "mud": 0 }],
  "cones":  [[r,c]], "lights": [...], "tables": [[r,c]], "muds": [[r,c]],
  "portals": [[[r,c,"N|E|S|W"],[r,c,"N|E|S|W"]]],
  "pylons":  [{ "r":2,"c":2,"o":"h|v","to":[[r,c,"S"],[r,c,"N"]] }],
  "tutorial": [{ "say": "...", "focus": [...], "do": ["move", 0, -1] }]
}
```
`(r,c)` = ô trên-trái của xe; `sign` ±1 = đầu TRÁI/TRÊN (−1) hoặc PHẢI/DƯỚI (+1). Trường mới cho bản Unity: `timeLimit` (giây, hoặc `null` cho màn không tính giờ), `minMoves` (từ solver). Định dạng thật của prototype (`docs/reference/prototype/levels-export/level_NN.json`) còn có `id`, `tag`, `hint` (chuỗi có thẻ `<b>`), `solveExhausted`, `explored`, các khóa `barriers`, `switches`, `sprays`, `paints`, `splash` (bool), `lights[].phase`, `pylons[].fixed`; phần `tutorial` dùng `focus`/`tap`/`do`. Bộ nạp C# theo định dạng thật này, không theo mẫu rút gọn ở trên.

### 7.5. Dữ liệu màn và file config `[CHỐT: dùng JSON + file config]`, chi tiết `[ĐỀ XUẤT]`

- **Mỗi màn một file JSON** `level_NN.json` trong `Assets/_Project/Resources/Levels/`, nạp bằng `Resources.Load<TextAsset>` (chạy ổn trên APK, khỏi xử lý đường dẫn Android). Thứ tự màn do một file/ScriptableObject danh sách màn quyết định.
- **"Sinh nhanh" nghĩa là sinh bằng tool ngoài game** (Node, từ prototype) rồi bỏ JSON vào thư mục. Game **không sinh màn lúc chạy**: người làm muốn màn làm thủ công, và kiểm "giải được" cần solver nặng.
- **Config chia theo miền** `[CHỐT: mọi thứ liên quan setup nhanh nên là dữ liệu]`, ScriptableObject trong `Assets/_Project/Config/`, đọc qua một `ConfigProvider`:
  - `GameConfig`: công thức giờ (nền 20 s + 5 s/nước), phạt hoàn tác, số lần và phạt giờ của gợi ý, ngưỡng sao, bật/tắt cơ chế bậc B, fps mục tiêu. Mặc định lấy từ mục 6.4.
  - `FeelConfig`: độ chúi, độ nảy, rung, thời lượng hiệu ứng, âm lượng (Claude chỉnh nhiều nhất).
  - `CameraConfig`: góc nghiêng (mặc định 65°), kích thước camera trực giao, độ mờ khi vật che xe.
  - **Catalog** (thêm màu/cơ chế/theme = thêm một dòng): `CarCatalog` màu → prefab/dáng/vật liệu; `MechanicCatalog` cơ chế → prefab/icon/tutorial; `ThemeConfig`.
  - Quy tắc khi nào dùng config: `AGENTS.md` §7.
- **Ghi đè theo từng màn** trong JSON: `timeLimit`, `hints` (bỏ trống = dùng công thức/config).
- **Test:** nạp cả 25 JSON → solver C# → so `minMoves` trong JSON.

---

## 8. Kiến trúc đề xuất `[ĐỀ XUẤT]`

**Hiện trạng việc 001:** đã có `Scripts/Core/SqueakyLane.Core.asmdef` (`noEngineReferences = true`), `Scripts/Game/SqueakyLane.Game.asmdef` (tham chiếu Core) và `Tests/EditMode/SqueakyLane.Tests.EditMode.asmdef` (chỉ Editor; tham chiếu Core, Game, TestRunner và NUnit). Core/Game có marker rỗng để Unity tạo DLL; một EditMode test reflection kiểm Core không tham chiếu `UnityEngine*` đã qua trong Test Runner. Các đường dẫn này nằm dưới `Assets/_Project/`; các thư mục/hệ thống còn lại trong cây bên dưới vẫn là dự kiến, chưa dựng. Game marker là bổ sung được người làm duyệt trong việc 001 để khắc phục cảnh báo asmdef không có script. `[FILE; CHỐT 22/11]`

**Nguyên tắc:** 3 tầng, tầng trên gọi tầng dưới, không ngược lại.
`Core` (luật thuần C#, không dùng `UnityEngine`) ← `Game` (phiên chơi, input, đồng hồ, tutorial) ← `Presentation/UI` (hình, âm, hiệu ứng).
Lợi ích: hoàn tác chỉ là giữ lại trạng thái cũ; solver dùng chung; test luật không cần mở Unity.

```
Assets/_Project/
├─ Scripts/
│  ├─ Core/                     # asmdef "SqueakyLane.Core" (noEngineReferences = true)
│  │  ├─ Model/                 # LevelDef, CarDef, GameState (bất biến), MoveEvent
│  │  ├─ Rules/                 # MoveResolver, GateRule, PushRule, PortalRule, PylonRule,
│  │  │                         #   TurntableRule, LightRule, TileEffects (bùn)
│  │  └─ Solver/                # Solver (BFS), StuckDetector
│  ├─ Game/                     # asmdef "SqueakyLane.Game" → tham chiếu Core
│  │  ├─ GameSession.cs         # trạng thái hiện tại, lịch sử hoàn tác, bắn sự kiện
│  │  ├─ LevelTimer.cs          # đếm ngược, trừ giờ hoàn tác, tính sao
│  │  ├─ InputController.cs     # chạm nửa xe / vuốt / nút bàn xoay / chạm trụ → lệnh
│  │  └─ Tutorial/
│  ├─ Presentation/             # BoardView, CarView, ActionAnimator, BodyLanguage, VFX, Sfx
│  ├─ UI/                       # HUD (đồng hồ, sao), hộp thoại thắng/thua/kẹt
│  └─ Editor/                   # LevelJsonImporter, công cụ kiểm màn (làm sau)
├─ Resources/Levels/            # level_01.json … level_25.json (nạp bằng TextAsset)
├─ Config/                      # GameConfig, FeelConfig, CameraConfig, CarCatalog, MechanicCatalog, ThemeConfig
├─ Art/  Audio/  Prefabs/  Scenes/
└─ Tests/EditMode/              # test luật + so khớp với golden file của prototype
```

### 8.1. Code mẫu (minh hoạ cấu trúc; luật chính xác xem `logic.js`)

**`Core/Rules/PylonRule.cs`**
```csharp
namespace SqueakyLane.Core.Rules
{
    public enum Side { N, W, S, E }

    /// <summary>Luật trụ xoay: mỗi lần chạm xoay thêm 90° cùng một chiều (ngược kim đồng hồ).</summary>
    public static class PylonRule
    {
        // Thứ tự các cạnh khi xoay: Trên → Trái → Dưới → Phải.
        private static readonly Side[] Order = { Side.N, Side.W, Side.S, Side.E };

        /// <summary>Chạm trụ: tăng số lần xoay (0..3). 4 lần chạm thì về chỗ cũ.</summary>
        public static int Rotate(int turns) => (turns + 1) & 3;

        /// <summary>Cạnh mà mặt cổng số 0 đang hướng ra; mặt số 1 nằm ở cạnh đối diện.</summary>
        public static Side Face0(int turns) => Order[turns & 3];
        public static Side Face1(int turns) => Order[(turns + 2) & 3];
    }
}
```

**`Game/LevelTimer.cs`** — đồng hồ, trừ giờ khi hoàn tác, tính sao
```csharp
namespace SqueakyLane.Game
{
    /// <summary>Đồng hồ đếm ngược của một màn. Hết giờ = thua. Không tính giờ nếu TimeLimit &lt;= 0.</summary>
    public sealed class LevelTimer
    {
        public float TimeLimit { get; }      // giây; <= 0 nghĩa là màn không tính giờ
        public float Remaining { get; private set; }
        public bool IsOver => TimeLimit > 0f && Remaining <= 0f;
        public bool Paused { get; set; }     // true khi đang hiện hộp thoại / tutorial

        private const float UndoPenalty = 3f;   // số liệu tạm; sau này đọc từ GameConfig (7.5)

        public LevelTimer(float timeLimit) { TimeLimit = timeLimit; Remaining = timeLimit; }

        public void Tick(float dt)
        {
            if (TimeLimit <= 0f || Paused) return;
            Remaining = System.Math.Max(0f, Remaining - dt);
        }

        /// <summary>Gọi mỗi lần người chơi hoàn tác.</summary>
        public void ChargeUndo() { if (TimeLimit > 0f) Remaining = System.Math.Max(0f, Remaining - UndoPenalty); }

        /// <summary>3★ còn ≥ 50% giờ, 2★ còn ≥ 20%, còn lại 1★. Màn không tính giờ luôn 3★.</summary>
        public int StarsOnWin()
        {
            if (TimeLimit <= 0f) return 3;
            float ratio = Remaining / TimeLimit;
            return ratio >= 0.5f ? 3 : ratio >= 0.2f ? 2 : 1;
        }
    }
}
```

**`Game/InputController.cs`** — khung (chi tiết chạm nửa xe ở mục 6.2)
```csharp
// Thứ tự ưu tiên khi chạm: (1) nút bàn xoay → (2) trụ xoay → (3) xe (chạm nửa hoặc vuốt).
// Chỉ phát "lệnh" (CarMoveCommand / RotateCommand); không tự đụng vào trạng thái luật.
```

---

## 9. Kiểm thử và chất lượng `[ĐỀ XUẤT]`

- **Golden test:** xuất từ prototype danh sách (màn → số nước ngắn nhất, lời giải) cho 25 màn; test EditMode của solver C# phải **khớp 100%**.
- Test luật cho từng cơ chế (cổng nước + bùn, portal, xe tải đẩy, trụ xoay 4 hướng, đèn đổi sau mỗi lượt, bàn xoay).
- Test `LevelTimer`: hoàn tác trừ giờ, hết giờ = thua, sao đúng ngưỡng.
- Chạm nhanh liên tiếp: không xe chồng hình; hoàn tác giữa lúc xe đang chạy phải huỷ mọi animation rồi dựng lại từ trạng thái cũ.
- Kiểm trên điện thoại thật: fps ≥ 30, nút bàn xoay không bấm nhầm, đọc được cơ chế ở góc nghiêng đã chọn, build < 250 MB.
- Claude không chạy được Unity: phần visual/feeling kiểm bằng **ảnh chụp, video Game view hoặc APK quay màn hình** do người làm gửi. Console: Claude đọc `Editor.log` (`AGENTS.md` §4.3); chạy test, Play và xem Console trong Editor do Codex (Unity MCP) hoặc người làm. Claude không dùng cầu nối Unity MCP và không dựng project .NET phụ. `[CHỐT 22/11]`

---

## 10. Lộ trình 9 ngày `[ĐỀ XUẤT — chưa duyệt; chờ xác nhận hạn chót]`

| Ngày | Việc chính | Xong khi |
|---|---|---|
| 1 (21–22/11) | Điền mục 3.1 từ project 3D trống đã tạo; dựng `.asmdef`; Claude xuất 25 màn JSON + golden; port mô hình + `MoveResolver` cơ bản | Project mở được, test mẫu chạy |
| 2–3 | Port đủ luật (bùn, cổng nước, ô vòi xịt, bùn văng, vũng sơn, portal, xe tải, trụ, bàn xoay, đèn); solver C# + golden test | Golden test khớp 100% |
| 4–5 | Dựng bãi từ JSON bằng khối xám; input (chạm nửa xe + vuốt + nút bàn xoay); hoàn tác; đồng hồ; gợi ý (3 lần/màn); thắng/thua | Chơi được 25 màn bằng khối xám trong Editor |
| 6–7 | Thay asset thật/low-poly; camera 65°; chống che; ngôn ngữ cơ thể xe; VFX + SFX | Nhìn và nghe "đủ sống" |
| 8 | Build APK, đo fps, chỉnh giờ/sao; sửa lỗi | Chạy mượt trên máy thật |
| 9 | Đánh bóng, quay video demo, chốt PROJECT.md | Bản gửi lead |

**Thứ tự cắt nếu trễ** (cắt từ dưới lên, từ trên xuống là giữ): lõi chơi + hoàn tác + giờ + thắng/thua → 25 màn chạy được → camera/feeling → SFX/VFX phụ → Gợi ý → asset thật đẹp. Rủi ro lớn: làm 3D + toàn bộ cơ chế + 25 màn trong 9 ngày; nếu hết hạn thì ưu tiên **chỉnh tốt ít cơ chế** hơn là đủ mọi thứ mà thô.

---

## 11. Rủi ro

| Rủi ro | Giảm thiểu |
|---|---|
| Quá nhiều việc trong ~9 ngày | Cắt theo thứ tự ở mục 10; dùng JSON + golden để khỏi dựng/kiểm màn tay |
| Đồng hồ + thua hết giờ làm câu đố bị căng, lỗ hổng cân bằng | Giờ rộng, màn học cơ chế không tính giờ, chỉnh theo người chơi thật |
| Chạm nút bàn xoay bấm nhầm xe | Ưu tiên nút hơn xe, vùng chạm lớn, thử máy thật |
| Che thông tin ở góc nghiêng | Hạ chiều cao vật, tự mờ vật che, có thể đưa về nhìn thẳng từ trên |
| Port luật sang C# lệch prototype | Golden test + test từng cơ chế |
| Hai AI hiểu khác nhau | Phiếu bàn giao trong repo, Claude review trước khi push |
| IP kiểu *Cars* | Dùng nhân vật riêng lấy cảm hứng; xem mục 3 |
| Bản quyền/nhãn hiệu tên game | Tên "Squeaky Lane" là tên làm việc, chưa tra nhãn hiệu `[CHƯA CHỐT]` |

---

## 12. Điểm còn mở

**Đã chốt vòng 21/11 (tối):** hạn 30/11 · thua khi hết giờ · gợi ý có chi phí · project 3D trống đã tạo · JSON + file config · tạm giữ các cơ chế bậc B · ưu tiên 25 màn, tool để sau.

**Còn mở:**
1. Mục 3.1 còn thiếu: package name, scripting backend, khóa Portrait, ARM64 (quyết trước khi build APK đầu tiên).
2. (Đã chốt 22/11: giữ "Kẹt xe".)
3. (Đã chốt 22/11: barie chưa đưa vào 25 màn, còn thời gian thì thêm.)
4. (Đã chốt 22/11: bùn dày "?" bỏ khỏi 25 màn.)
5. Con số giờ/gợi ý mặc định ở mục 6.4 có ổn không.
6. Tên "Squeaky Lane" (chưa tra nhãn hiệu); có cần iOS; lead xem qua APK, có cần thêm video không.

---

## 13. Sổ công việc

| NNN | Việc lớn | Nhánh | AI | Trạng thái | Verdict | Liên quan |
|---|---|---|---|---|---|---|
| — | Tài liệu: `PROJECT.md`, `AGENTS.md`, `CLAUDE.md`, `CODEX.md` (làm trong chat, 21/11) | — | Claude | Xong (đã commit 22/11) | — | — |
| 001 | Khung thư mục và assembly Core/Game/EditMode | `001-project-skeleton` | Codex | Xong (đã merge 22/11) | APPROVE (Claude 22/11) | — |
| 002 | Dữ liệu cho Core: 31 màn cũ sang JSON object + golden trace từ `logic.js` | `002-level-data` (worktree riêng) | Claude | Đang làm | — | — |

(Một dòng mỗi **việc lớn**, `NNN` theo thứ tự mở việc; việc nhỏ không ghi. Không cần ghi hash commit. Cột `Liên quan` nối việc sửa với việc gốc, ví dụ `004`.)

---

## 14. Tài liệu liên quan (Project "CTY - Idea")

- `claude/tram-rua-xe-v10-cong-tuong-tru-xoay.md` — luật cổng tường, trụ xoay, nút Gợi ý, màn 20–31, công cụ.
- `claude/tram-rua-xe-v9-co-che-moi.md` — xe tải, barie liên động, lý do chọn cơ chế.
- `claude/tram-rua-xe-noi-dung-se-sua.md` — dáng xe theo màu, điều khiển chạm, ngôn ngữ cơ thể, theme, góp ý của lead.
- Prototype: `tram-rua-xe-v10.html`; nguồn ở `/home/claude/proto` (`src/logic.js`, `tools/`, `test/`).
- **Bản chép trong repo:** `docs/reference/prototype/` (`src/logic.js`, `tools/`, `test/`, `levels-export/` là 31 màn cũ kèm `minMoves`). Chỉ để đối chiếu, không nằm trong `Assets`. Lưu ý: màn 17–19 chưa có `minMoves` (vượt 300.000 trạng thái), màn 16 cần `maxStates` > 60.000; 25 màn mới theo bộ cơ chế mục 5.1 **chưa được sinh**.
