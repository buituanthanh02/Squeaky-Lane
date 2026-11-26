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
- **Nội dung:** **25 màn** (gồm tutorial), làm thủ công từng màn (không sinh ngẫu nhiên lúc chạy). **Cập nhật 23/11:** người làm duyệt thêm trình sửa màn/config ngoài Play vào **009-D**, làm sau khi sửa lỗi chữ UI; thay ưu tiên cũ "25 màn trước, tool sau". Tool hỗ trợ xe/cổng/cơ chế, xem trước và lưu/xuất JSON; phạm vi chi tiết và giới hạn một ngày ở phiếu 009. Màn định nghĩa bằng JSON + file config (mục 7.5). `[CHỐT]`

---

## 2. Cách làm việc giữa người và AI

### 2.1. Vai trò `[CHỐT]`
| Ai | Làm gì |
|---|---|
| **Người làm (điều phối)** | Quyết định cuối cùng. Chơi thử, build APK, **merge/push**. Duyệt việc giao Codex. Mang thông tin qua lại khi AI chưa nối được. |
| **Claude (chủ trì code, từ 25/11)** | Lên kế hoạch, **viết và sửa code chính** (Core, Game, Presentation, UI, tool Editor, test), quyết định visual và feeling, kiểm bằng Unity MCP, **commit trên nhánh việc**. Gọi Codex bằng phiếu nhỏ khi cần VFX/asset. |
| **Codex (được gọi ra, từ 25/11)** | Làm **VFX** (prefab hạt, shader, material hiệu ứng) và **asset 2D/3D còn thiếu** trong vùng art/hiệu ứng (`AGENTS.md` §1.3). Không viết logic game, không commit. |

Hai AI **không nói chuyện trực tiếp với nhau**; mọi thứ đi qua phiếu và người làm. Quy tắc phối hợp cũ (Claude review/visual, Codex code chính) ở commit `c092dac`. `[CHỐT 25/11]`
`[ĐỀ XUẤT]` Vì vậy cần "phiếu bàn giao" ngắn nằm trong repo (`docs/handoff/NNN-<việc>.md`): Claude ghi mục tiêu, phạm vi, tiêu chí xong, điều cấm; Codex đọc phiếu, làm, ghi lại đã làm gì và chưa chắc gì; Claude review dựa trên diff + phiếu. Người làm không phải chép tay. Cuối phiếu có mục **Trao đổi** (chỉ thêm dòng, thẻ `[HỎI]` `[TRẢ LỜI]` `[BÀN GIAO]` `[BÁO]`) để hai AI hỏi–đáp–bàn giao; chat mới đọc phiếu để biết việc đang ở đâu (`AGENTS.md` §1.6). `[CHỐT 22/11]`

### 2.2. Quy tắc bắt buộc với AI `[CHỐT]`
1. **Không chiều theo người làm.** Người làm là người mới; AI phải **phản biện và tranh luận** khi thấy ý kiến sai/rủi ro/mâu thuẫn, nêu lý do và bằng chứng, rồi mới theo quyết định (nếu người làm vẫn giữ). Không "thông đồng", không khen cho vui.
2. **Hỏi người làm để duyệt** trước các thay đổi ở danh sách "phải hỏi" (2.3). Sửa lỗi nhỏ có thể sửa thẳng hoặc hỏi cách sửa.
3. **Quy tắc trong tài liệu này có thể sai hoặc rườm.** Người làm là người mới, AI thấy quy tắc nào vô lý/thừa/mâu thuẫn thì **nói ra và bàn lại**, không im lặng làm theo.
4. **Việc thất bại hoặc vượt giới hạn → dừng lại và nói chuyện với người làm**, không tự vòng vèo/làm tắt phá hoại.
5. Luôn trả lời bằng **tiếng Việt**. Code sạch, chia class theo trách nhiệm, comment tiếng Việt giải thích rõ; khi giao code mẫu thì kèm cấu trúc thư mục. Viết vừa sức junior đọc và giải thích; **không chia vụn thành một file cho mọi kiểu dữ liệu**. Enum/struct và kiểu nhỏ liên quan được gom chung; component Unity vẫn một class một file. `[CHỐT — người làm nhắc lại 22/11]`
6. Các file thuộc phiếu 009-01: mỗi dòng một lệnh; `if`/`for` luôn có ngoặc `{}`. Không format file ngoài phạm vi. `[CHỐT 24/11]`

### 2.3. Ranh giới tự làm / phải hỏi `[CHỐT]`
- **Tự làm trên nhánh riêng:** sửa/viết code, chạy test, viết tài liệu trong nhánh đó.
- **Phải hỏi trước:** đổi package hoặc Project Settings; xóa asset; thêm asset bên ngoài; đổi luật chơi; merge vào `main`.
- **AI (cả Claude và Codex) được sửa trực tiếp scene/prefab** vì hạn 30/11, theo hàng rào ở `AGENTS.md` §4.2 (ưu tiên qua Editor/menu Editor; sửa YAML tay chỉ khi không có Editor và thay đổi nhỏ). `[CHỐT]`

### 2.4. Git `[CHỐT]` + `[ĐỀ XUẤT]` phần chi tiết
- **Việc nhỏ** (cỡ S, ≤ 2 file code/config/tài liệu, không đụng luật/asset/scene/prefab/package/settings): **sửa và commit thẳng**, không nhánh riêng. `[CHỐT]`
- **Việc lớn**: **một nhánh mới**, đặt theo **mã việc**, không theo AI: `NNN-<mô-tả>` (ví dụ `004-hint-button`). `NNN` tăng theo thứ tự mở việc để xem lại nhánh cũ theo trình tự; cả hai AI cùng commit lên nhánh của việc đó theo luật "người cầm gậy". `[CHỐT]` Sửa lỗi/làm lại một việc đã có thì đặt `NNN-MM-<mô-tả>` (ví dụ `003-01-fix-undo-timer`), đi bằng nhánh riêng từ `main` để nhánh việc đang làm bỏ được mà không mất bản sửa; chi tiết ở `AGENTS.md` §8. `[CHỐT]` AI nào tham gia ghi ở phiếu và dòng cuối commit.
- **Claude được commit** trên nhánh việc theo mốc; **Codex không commit**; **không AI nào push/merge** (người làm làm). Commit message kết thúc bằng dòng `AI: …` ghi AI tham gia. `[CHỐT 25/11]`
- Không merge vào `main` khi chưa có bằng chứng kiểm đúng bậc V (MCP/test/Console) và người làm chưa chơi thử. `[CHỐT 25/11, thay "Claude review" cũ]`
- Khi phiên làm việc có repo, người làm sẽ mở chat riêng gắn repo để Claude xem nhánh/diff. `[CHỐT]` (chat này chỉ để lấy ý tưởng và kết luận tổng thể).

### 2.5. Làm việc với Unity Editor `[ĐỀ XUẤT kế thừa từ cách làm cũ của người làm, cần xác nhận lại]`
- **Unity MCP `[CHỐT 25/11]`:** `com.coplaydev.unity-mcp` (CoplayDev, MIT) ghim `v10.3.0` trong `Packages/manifest.json`; Claude và Codex cùng điều khiển Editor đang mở qua MCP (một AI lái tại một thời điểm, `AGENTS.md` §1.2); công cụ nạp ở chat mới; server chạy cục bộ qua `uv`. Thay quyết định 22/11 "Claude không dùng MCP" và 23/11 "Codex không điều khiển Editor".
- **Để người làm học:** Codex viết code và hướng dẫn từng bước có giải thích; người làm tự setup scene/UI/prefab và gắn tài nguyên. Không dựng tự động hết thay người làm. `[CHỐT 23/11, bắt đầu từ 009]`
- Người làm chơi thử, build APK và commit. AI được sửa trực tiếp scene/prefab (`AGENTS.md` §4.2); với việc cần thao tác tay trong Editor thì AI viết từng bước cụ thể hoặc viết menu Editor để người làm bấm.
- **Sửa Inspector đơn giản** (alpha/màu, checkbox, kích thước): hướng dẫn người làm sửa tay, không tạo thêm menu/tool riêng. Menu Editor dành cho tạo asset hoặc sửa hàng loạt/lặp lại. `[CHỐT 25/11]`
- Không sửa tay YAML scene/prefab khi Editor đang mở.
- **Unity CLI khi Editor ĐÓNG `[CHỐT 25/11]`:** `unity test … --mode EditMode` chạy test; `unity run … -- -executeMethod SqueakyLane.EditorTools.BatchRunner.<ImportPack|ApplyS3|RenderPreviews>` chạy menu 013 hoặc render UI Edit-mode ra `Logs/013-preview` (không có bãi 3D, không thay ảnh Game view thật). Không lái được Editor đang mở (cần package Unity 6+); kiểm `tasklist` trước và không tự tắt Editor của người làm.
- **Asset Lab riêng `[CHỐT 25/11]`:** người làm yêu cầu scene kiểm asset/bóng trong Scene view và chọn mẫu qua config, cập nhật ngay ngoài Play. Menu `S4 Open Asset Lab` tạo/mở `Assets/_Project/Scenes/AssetLab.unity` + `Config/AssetLabConfig.asset`; chưa thêm vào Build Settings. Config thử độc lập cho mẫu, ánh sáng, nền, bóng, góc/cỡ/độ nâng asset; preview không sửa prefab/material/UiTheme của game. Các object xem trước không lưu vào scene/build.
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
| Tên thư mục gốc project, tên scene chính | Thư mục gốc `Squeaky Lane`; scene chơi `Assets/_Project/Scenes/Play.unity` có Main Camera, Directional Light, GameRoot (prefab), EventSystem, Canvas; đã bật trong Build Settings. Scene mẫu `Assets/Scenes/SampleScene.unity` vẫn giữ. `[FILE: Play.unity, ProjectSettings/EditorBuildSettings.asset; người làm chạy Setup Play 23/11]` |
| Package đã thêm (ngoài mặc định) | **Không có.** Chỉ có package mặc định của template (`com.unity.feature.development`, TextMeshPro, Timeline, ugui, Visual Scripting…). `com.unity.test-framework 1.1.33` có sẵn trong `packages-lock.json` nên dùng được EditMode test `[FILE: Packages/manifest.json, packages-lock.json]` |
| Android: package name, scripting backend, min API | `companyName: DefaultCompany`, `productName: Squeaky Lane`. **Package name chưa đặt** (`applicationIdentifier: {}`, đang mặc định) `[CHƯA ĐIỀN]`. **Scripting backend chưa đặt** (`scriptingBackend: {}`, đang mặc định) `[CHƯA ĐIỀN]`. Min API 22, target API 0 (tự động). Kiến trúc `AndroidTargetArchitectures: 1` (hiểu là chỉ ARMv7) `[CHƯA KIỂM CHỨNG]`. Xoay màn hình `defaultScreenOrientation: 4` (tự xoay, **chưa khóa dọc**). `[FILE: ProjectSettings/ProjectSettings.asset]`. Cần quyết trước khi build APK đầu tiên: package name, khóa Portrait, ARM64 (đổi Project Settings là việc phải hỏi) |
| Quy ước đặt tên / ghi chú khác | Cây thư mục và đặt tên: theo mục 8. Mã nguồn prototype để đối chiếu nằm ở `docs/reference/prototype/` (ngoài `Assets`, Unity không biên dịch). `.gitignore` đã có (bỏ `Library`, `Logs`, `UserSettings`, `.vscode`, `*.sln`, `*.apk`, keystore…) `[FILE]` |

**Bộ UI từ Sand Puzzle Clone `[CHỐT 25/11]`:** người làm quyết định lấy một phần sprite UI (khung nút, holder, popup, switch, icon) từ dự án cá nhân "Sand Puzzle Clone" vào `Art/UI/Sprites/Pack` và `Art/UI/Library` để khỏi vẽ lại; đây là bài thực tập nội bộ, không phát hành, không thương mại, không sao chép nguyên game. Quyết định này **ghi đè** dòng V2 "không đục asset từ game thương mại" và danh sách "3 gói đã duyệt" ở phiếu 013 cho phần UI. Không có giấy phép đi kèm nên **không đưa vào bản phát hành**; nguồn, phần lọc bỏ (logo, nhân vật, ảnh minh họa) và cách thay bằng asset CC0 nằm ở `Assets/_Project/Art/UI/Pack-Source.txt`. Gói Kenney (UI Pack, Game Icons) và Baloo 2 vẫn dùng như cũ.

**Ghi chú về asset và IP** `[ĐỀ XUẤT, cần bàn]`: người làm nghiêng về IP nhân vật kiểu phim *Cars*. Dùng nhân vật/asset thật của *Cars* thì vi phạm bản quyền; cho bài thực tập nội bộ có thể chấp nhận để minh hoạ, nhưng **không nên đưa vào build chính thức**. Nên làm nhân vật riêng "lấy cảm hứng" (xe có tính cách qua chuyển động). Hiện chưa có cốt truyện, làm lõi trước. `[CHỐT]` Quyết định cũ "không vẽ mặt lên kính xe" (bị đánh giá creepy) **vẫn giữ**: chỉ dùng ngôn ngữ cơ thể (nhún, chúi, nảy, rũ bùn). `[CHỐT]`

---

## 4. Camera và hình học 3D

- Hướng 3D. Luật gần như không đổi so với 2D. `[CHỐT]`
- **Camera cố định**, có thể đổi sau khi có góp ý. `[CHỐT]`
- **Góc nghiêng 60–70° được chấp nhận.** `[CHỐT]` `[ĐỀ XUẤT]` mặc định **65°, camera trực giao (orthographic)**, để chỉnh bằng một biến trong prefab camera.
- Nguyên tắc chọn góc: nếu góc nghiêng làm che mất thông tin chơi (cơ chế) thì dùng nhìn thẳng từ trên xuống; còn nếu vẫn đọc được thì giữ nghiêng để thấy ngôn ngữ cơ thể của xe. `[CHỐT]`
- `[ĐỀ XUẤT]` Quy tắc chống che: vật cao (trụ xoay, đèn giao thông, cổng/barie phía dưới) làm thấp còn ~60–70% so với thực tế; vật nào che xe thì tự mờ dần (fade).
- **Giá trị đang dùng:** `CameraConfig.tiltDegrees = 58` (65 → 70 ngày 23/11; 70 → 58 ngày 26/11 theo phiếu 015 bước 2, người làm duyệt; đã kiểm 846/846 điểm chạm nửa xe trúng đúng xe/nửa trên 25 màn, chưa thử máy thật). Camera vẽ/xóa nền toàn màn; vùng dành cho bãi chỉ dùng để tính kích thước và tâm nhìn, nhằm tránh vệt chữ HUD ngoài vùng camera cũ. Code đã biên dịch CLI; người làm xác nhận hết lỗi chữ sau khi chơi thử. `[FILE: Assets/_Project/Config/CameraConfig.asset, Scripts/Presentation/BoardView.cs; CHỐT 23/11]`
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
| **Bùn bám** (mud 1) | **Giữ** `[CHỐT]` | Xe còn bùn không qua được cổng màu. **Hình `[CHỐT 26/11]`:** "vẩy bùn" (3 kiểu, nhiều đốm rời + 1–2 giọt chảy trên mui, thay cục bùn đơn bị người làm chê), giọt bùn rơi khi xe chạy, vệt bánh đổi nâu khi xe dính bùn; **không** phủ/đổi màu cả thân (màu xe phải đọc được để chọn cổng). **Ô vòi xịt `[CHỐT 26/11]`:** trạm rửa kiểu kẹo (chổi lăn sọc, nền bong bóng, phun bọt); xe dính bùn **bò chậm** 0,7 s qua ô, lắc theo chổi, bùn trôi giữa chừng, ra ô thì bứt tốc lại. Phiếu 014. |
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
- **Vuốt vượt ngưỡng phải ra lệnh ngay khi tay còn giữ trên màn hình**, không đợi nhả tay; mỗi cử chỉ phát tối đa một lệnh, không phát lại lúc nhả. Chạm nhẹ vẫn phân biệt với vuốt. `[CHỐT 23/11]`
- **Nhịp kết thúc màn:** dừng đồng hồ khi luật xác định thắng; đợi xe chạy hết hoạt hình, phát chúc mừng rồi chờ thêm `FeelConfig.winDialogDelaySeconds = 0.9` giây trước khi mở bảng thắng. Giá trị asset tăng 0 → 0.5 khi người làm yêu cầu "lâu thêm 1 tí" (23/11), rồi 0.5 → 0.9 để pháo giấy 1 s của 012 hiện gần hết trước bảng thắng (25/11); chỉnh tiếp ngoài Play nếu cần. `[CHỐT 25/11; chưa Play lại sau khi đổi]`

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
- Tool 009-D mở qua `Tools → Squeaky Lane → Level Editor`: lưới sửa xe/cổng/cơ chế, xem trước 3D, thứ tự màn và ba config Game/Feel/Camera ngoài Play. Người làm đã thử lưới/preview và Nhân bản/Undo/Lưu/Export/Play, báo ổn; đã tạo ba bản sao32–34. Ảnh EditMode xác nhận352Passed/0Failed/1khôngchạy. Còn review và kiểm Android, chưa coi 009 xong. `[FILE; người làm báo/ảnh 23/11]`

### 7.2. Cách làm 25 màn trong ~9 ngày `[CHỐT — người làm đã duyệt]`
1. Dùng lại công cụ sinh/kiểm của prototype (Node, `tools/mech.js`, `CW.solve`) với **bộ cơ chế ở 5.1** để ra 25 màn **được kiểm là giải được**, mỗi cơ chế xuất hiện có chứng minh "tắt đi thì vô nghiệm" (ablation). Claude làm bước này trong workspace riêng, xuất JSON.
2. Unity nạp JSON → dựng bãi (không đặt tay từng xe trong scene). Người làm chỉnh bằng tool Editor 009-D hoặc JSON rồi kiểm/duyệt màn.
3. Xuất kèm **"số nước ngắn nhất" cho từng màn** làm dữ liệu chuẩn (golden file) để đối chiếu solver C# (mục 8) và tính giờ/sao.
4. **Cập nhật 23/11:** người làm đưa tool dựng màn ngoài Play vào 009-D trước khi chốt 25 màn. Không làm hệ node/drag dây phức tạp; dữ liệu vẫn là JSON, solver vẫn phải kiểm màn. `[CHỐT]`
Không tự đặt xe trong scene rồi dùng scene làm dữ liệu màn: dễ lệch luật và khó kiểm bằng solver.

### 7.3. Đường cong học `[ĐỀ XUẤT]`
Màn 1–6: tutorial từng cơ chế (cổng màu → cổng nước + bùn → cọc → đèn → bàn xoay) · 7–15: kết hợp 2 cơ chế · 16–25: cổng tường, trụ xoay, xe tải và vài màn khó. Mỗi màn tối đa 2–3 cơ chế. Mỗi cơ chế mới có màn giới thiệu và tutorial bằng hình (bàn tay + vòng sáng + mũi tên; chữ một dòng ngắn).

### 7.4. Định dạng dữ liệu màn Unity `[FILE: LevelJsonLoader.cs, docs/golden/levels]`
```json
{
  "id": 1, "name": "Ví dụ", "cols": 5, "rows": 5,
  "gates": [{ "side": "r", "i": 1, "color": "blue" }],
  "cars": [{ "axis": "h", "r": 1, "c": 0, "color": "blue", "len": 2, "truck": false, "mud": 0 }],
  "cones": [{ "r": 3, "c": 3 }], "muds": [], "sprays": [], "tables": [],
  "paints": [], "lights": [], "barriers": [], "switches": [],
  "portals": [], "pylons": [], "splash": false,
  "timeLimit": 45, "hints": 3, "minMoves": -1, "solveExhausted": false
}
```
`r/c` là hàng/cột từ 0, tọa độ ô trên-trái của xe. `axis/o`: `h` ngang, `v` dọc. Cổng mép dùng `side: t/b/l/r`; mặt portal dùng `N/W/S/E`. Màu: pink/blue/yellow/green/orange/purple; water chỉ cho cổng. Các ô là **object `{r,c}`**, không phải mảng `[r,c]` của prototype cũ. Portal là `{a:{r,c,side},b:{r,c,side}}`; trụ là `{r,c,o,fixed,to:[hai mặt]}`; chốt là `{r,c,a:[các ô],b:[các ô]}`. `sign` chỉ dùng trong lệnh: −1 trái/trên, +1 phải/dưới.

Vắng `timeLimit/hints` thì dùng GameConfig; đặt timeLimit=0 là không tính giờ, hints=-1 là không giới hạn. `minMoves=-1` là chưa biết tối ưu. Tool giữ metadata lạ ở gốc (tag/hint/tutorial/...), nhưng chặn mở/sửa khi có field cơ chế lạ để tránh âm thầm mất dữ liệu.

### 7.5. Dữ liệu màn và file config `[CHỐT: dùng JSON + file config]`, chi tiết `[ĐỀ XUẤT]`

- **Mỗi màn một file JSON** `level_NN.json` trong `Assets/_Project/Resources/Levels/`, nạp bằng `Resources.Load<TextAsset>` (chạy ổn trên APK, khỏi xử lý đường dẫn Android). Thứ tự màn do một file/ScriptableObject danh sách màn quyết định.
- **Thiết kế ngoài Play (009-D):** lần lưu đầu khởi tạo `Assets/_Project/LevelDesign/` từ dữ liệu runtime hiện có. `Lưu thiết kế` chỉ ghi kho này; `Xuất vào game` hoặc `Sync Levels` mới chép các màn theo `level_order.json` sang Resources. Khi kho thiết kế đã tồn tại, Sync không dùng golden ghi đè. Bỏ màn khỏi thứ tự không xóa asset; đổi ID là tạo bản mới, không đổi tên file cũ. Có bản sao trước mỗi lần ghi đè ở `Logs/level-editor-backups/` (ignored).
- **Kiểm màn:** tool tái dùng loader/Core để chặn dữ liệu sai. Sửa bố cục làm minMoves=-1/solveExhausted=false; nút tìm đường dùng PathSearch theo ngân sách thời gian, chỉ chứng minh có đường thắng, **không nhận độ dài đó là tối ưu**. Bộ golden ở `docs/golden/` không đổi và không phải kho màn đang thiết kế. Không sinh thư mục/file thiết kế cho tới khi người làm lưu.
- **"Sinh nhanh" nghĩa là sinh bằng tool ngoài game** (Node, từ prototype) rồi bỏ JSON vào thư mục. Game **không sinh màn lúc chạy**: người làm muốn màn làm thủ công, và kiểm "giải được" cần solver nặng.
- **Config chia theo miền** `[CHỐT: mọi thứ liên quan setup nhanh nên là dữ liệu]`, ScriptableObject trong `Assets/_Project/Config/`, đọc qua một `ConfigProvider`:
  - `GameConfig`: công thức giờ (nền 20 s + 5 s/nước), phạt hoàn tác, số lần và phạt giờ của gợi ý, ngưỡng sao, bật/tắt cơ chế bậc B, fps mục tiêu. Mặc định lấy từ mục 6.4.
  - **013 — UiTheme (Presentation, đang dở):** sprite/nút/panel, màu/font Baloo2 TMP Static và atlas11 icon, số UI/cảnh. P1 đã áp qua Editor, giữ binding/OnClick; báo cáo field ở `docs/handoff/013-scene-changes.md`. Claude chốt Visual V2, người làm duyệt25/11: giữ P1, S1→S5. S1 Apply Bright Ground đã chạy14:06: cỏ hai tông/vỉa hè1 mesh/1 material asset, vignette dưới HUD, padding0.1; chưa props/ẩn nhãn. Ảnh Test Runner556 qua/0 lỗi; ảnh Stats162 batches ở384×216,16:9 (chưa xác nhận phép đo đúng màn16/cỡ yêu cầu). Lỗi P1 đặt alpha0 trên Viewport Image có Mask đã sửa trong UiSkinTools, Roslyn0 lỗi. Người làm sửa tay Inspector và xác nhận ổn; scene lưu alpha1/Show Mask Graphic=false. S2 sửa F1–F6 (25/11 15:42): sao hết lỗi (bỏ `tint=0` trong `UiTheme.Stars`), chữ viền đặc (màu `[HDR]` của TMP truyền dạng linear). S3 (Claude, 25/11 16:20): theo yêu cầu người làm, đổi sang bộ sprite UI lọc từ `_Project.rar` của Sand Puzzle Clone (`Art/UI/Sprites/Pack`, `Art/UI/Library`, nguồn ở `Pack-Source.txt`): đồng hồ mm:ss không thanh chạy, bánh răng nhỏ góc phải, viền nút mảnh, hộp Tạm dừng/Thắng/Thua nhiều lớp (Frame, Header, Close, Buttons, Toggles); đã áp lên Play.unity/LevelButton bằng Unity CLI, Run All 561/560/0/1; chưa Play thật/ảnh V3; S3 đã commit (cd32568); S4 giao Codex 25/11 (bóng giả, không bóng thật; kế hoạch ở phiếu 013), S5 hoãn. `[FILE/ẢNH/LOG/CHAT; phiếu013]`
  - `FeelConfig`: độ chúi, độ nảy, rung, thời lượng hiệu ứng, âm lượng (Claude chỉnh nhiều nhất).
    - **012 — chốt25/11:** đã có ngôn ngữ cơ thể/bánh quay, bùn mờ, hạt dùng chung5 hệ/trần60; gói B/C thêm xe ra cổng0.5s nhanh dần/giữ cỡ tới phần đuôi, popup khen/combo, cao độ tiếng Exit, lực va theo số bước Core. Số cuối: bubbles0.45, sparkles0.55, confetti40 hạt/cỡ0.5, popupTextSize0.08; winDialogDelaySeconds0.9. Codex đã review APPROVE phần B/C Claude viết; Roslyn5 lượt0 lỗi, Run All552/0/1 theo người làm báo trong phiếu, video09:53 Console0 theo Claude. Người làm chốt feeling đủ và yêu cầu merge localmain. Khôi phục đúng5 material và font TMP đã lỡ có trong b2d4996 về7e6d876 bằng commit mới theo người làm duyệt; giữ số feeling. Chưa kiểm Android/Stats/GC/nhiệt và riêng popup/Pause/Undo bằng Codex; kiểm ở APK013, gói A hoãn. Không đổi Core/scene/package/settings. `[FILE/PHIẾU/CHAT 25/11; phiếu012]`
    - **014 — xe chạy (đang làm 26/11, nhánh `014-car-feel`):** nhóm "014 — Xe chạy" trong FeelConfig: bứt tốc gia tốc đều tới `1/stepSeconds` sau `accelCells`=1,2 ô (mỗi lượt chậm thêm accelCells×stepSeconds; xe tải đẩy hàng cùng nhịp), lắc lư tắt dần khi đâm/bị đụng (nghiêng 7° × hệ số va, lắc đầu 0,45, 0,5 s), vệt bánh `TrailRenderer` và tia đèn trước (trắng)/khi lùi (đỏ) do `CarRoadFx` gắn root xe; material/prefab từ Codex (phiếu 014-a), trống thì tắt. Vệt xóa khi Hoàn tác/Chơi lại/portal. `[ĐỀ XUẤT, chờ người làm chơi thử]`
    - **015 — Cổng rửa (26/11, nhánh `015-toy-look`):** cổng nước có hầm rửa `WashGate_0` (`MechanicKind.WashGate`, catalog): xe dính bùn bò 1,1 s xuyên hầm ra thảm ngoài bãi (đuôi qua mép +0,3 ô), đứng 0,15 s, lùi 0,45 s rồi lùi tiếp theo luật; số ở FeelConfig nhóm "015 — Cổng rửa". Camera chừa thêm chỗ ở trục có hầm. Không đổi Core. `[ĐỀ XUẤT, chờ người làm xem]`
  - `CameraConfig`: góc nghiêng (mặc định 65°), kích thước camera trực giao, độ mờ khi vật che xe.
  - **Catalog** (thêm màu/cơ chế/theme = thêm một dòng): `CarCatalog` màu → prefab/dáng/vật liệu; `MechanicCatalog` cơ chế → prefab/icon/tutorial; `ThemeConfig`.
  - Quy tắc khi nào dùng config: `AGENTS.md` §7.
- **Đưa model ngoài vào game (việc 010):** phải qua bước kiểm asset/giấy phép/độ nặng → chuẩn hóa pivot, trục, scale, kích thước → tạo prefab bọc với collider và CarView → nối qua CarCatalog → thử chạy/Undo/xoay/portal/đẩy và đo trên máy. Không coi việc thả model vào Assets là đã tích hợp; luật/kích thước lưới vẫn do Core quyết định. `[CHỐT 23/11 — người làm yêu cầu có bước prefab riêng]`
- **Ghi đè theo từng màn** trong JSON: `timeLimit`, `hints` (bỏ trống = dùng công thức/config).
- **010 — xe Kenney:** pink → hatchback-sports, blue → sedan, yellow → taxi, green → suv, orange → van, purple → sedan-sports; `truck.fbx` chỉ dùng cho 6 prefab tải. Sơn đổi cả dáng theo màu mới, giữ root/CarView/collider lưới và luồng hoạt hình. 6 material/colormap chỉ đổi ô thân; CarCatalog qua ConfigProvider, thiếu dữ liệu dùng khối/cảnh báo một lần. Đã có đủ12 prefab đúng bảng, root0/0/1, scale đều và đáy-0.33; collider vẫn theo lưới. Run All 512/0/1 theo ảnh25/11, ảnh Game01/16/20 Console sạch; còn xác nhận các thao tác Play. [CHỐT chat25/11] Hình tải chỉ dài1.534ô: nối dài riêng thùng bằng mesh phái sinh Editor để phủ2.82ô, giữ cabin/bánh đúng tỷ lệ và dời cầu bánh; không sửa FBX nguồn/luật/scale đều. Anchor ScrollRect/Scrollbar/font WIP được Claude chấp nhận, chỉ kiểm cuộn danh sách. `[CHỐT/FILE/ẢNH 25/11; chi tiết phiếu010]`
- **011 — bãi/cơ chế:** `MechanicCatalog` (Config) nhận qua `ConfigProvider`, ánh xạ loại/biến thể → prefab; thiếu/sai prefab dùng khối cũ và cảnh báo một lần trên BoardView. `Lot Mechanics` trong Tools/Squeaky Lane tạo catalog/material/mesh/16 prefab P1 bằng AssetDatabase, chỉ nhập `cone.fbx` và dùng lại colormap Car Kit. Mặt bãi/vạch/bó vỉa một mesh ba submesh, thích nghi kích thước màn khi nạp (không tạo material runtime); đèn/vạch đỏ-xanh nhận pha từ BoardView, tay trụ nhận góc từ ActionAnimator. Người làm gán `Mechanic Catalog` trên GameRoot; preview Editor cũng dùng catalog. [CHỐT chat25/11 theo ảnh29/30] Trụ là cổng tele xoay (Pylon): khung cửa tròn3D dựng đứng ở tâm, hai mặt A/B cùng khung quay90°; đĩa thấp/mũi tên ở chân chỉ hướng vào. Khung cao0.84/dày0.14, tổng cao0.875 gồm chân, không đổi luật. Cổng/portal che xe chuyển sang bộ sharedMaterial alpha0.18 dựng sẵn Editor, trả về rõ khi hết che; số trong catalog, không tạo material runtime. Stats16 trước sửa mới:156/66/57.7 ở568×320 (khác độ phân giải mốc010). Cần bấm Lot Mechanics nút4 rồi Run All/ảnh29–30 lại. P2 City/Racing và P3 chưa triển khai; asset City/Racing người làm đã nhập giữ nguyên, chỉ dọn qua Project theo danh sách duyệt trước commit. Đã tạo asset/gán catalog trong Play.unity; Run All518/0/1 theo ảnh25/11, hai warning do test fallback đã xác nhận stack log. Còn Console khi Play/ảnh/Stats. `[FILE/ẢNH/LOG 25/11; phiếu011]`
- **Test:** nạp cả 25 JSON → solver C# → so `minMoves` trong JSON.

---

## 8. Kiến trúc đề xuất `[ĐỀ XUẤT]`

**Hiện trạng assembly:** `Scripts/Core/SqueakyLane.Core.asmdef` không tham chiếu UnityEngine; Game → Core; Presentation → Game, Core, TMP, uGUI. Việc 009-01 thêm `Scripts/Editor/SqueakyLane.Editor.asmdef` chỉ dành cho Editor → Core, Game, Presentation, uGUI; test EditMode → các assembly trên, uGUI, TestRunner và NUnit. `LevelDesignIO` giữ `internal`, cấp quyền cho test qua `InternalsVisibleTo`; không có cạnh runtime → Editor. Người làm duyệt đổi asmdef 24/11; Run All A–E qua 504/0/1 (ảnh người làm), thay đổi F/H/I chưa chạy lại Run All. Các đường dẫn này nằm dưới `Assets/_Project/`. `[FILE; CHỐT 24/11]`

**Nguyên tắc:** 3 tầng, tầng trên gọi tầng dưới, không ngược lại.
`Core` (luật thuần C#, không dùng `UnityEngine`) ← `Game` (phiên chơi, input, đồng hồ, tutorial) ← `Presentation/UI` (hình, âm, hiệu ứng).
Lợi ích: hoàn tác chỉ là giữ lại trạng thái cũ; solver dùng chung; test luật không cần mở Unity.

```
Assets/_Project/
├─ Scripts/
│  ├─ Core/                     # asmdef "SqueakyLane.Core" (noEngineReferences = true)
│  │  ├─ CoreAssemblyMarker.cs
│  │  ├─ Model/
│  │  │  ├─ Enums.cs            # các enum dùng chung
│  │  │  ├─ LevelObjects.cs     # ô/cổng/sơn/đèn/barie/chốt/portal/trụ
│  │  │  ├─ CarDef.cs           # CarDef + CarState
│  │  │  ├─ LevelDef.cs         # màn bất biến + helper ReadOnlyData
│  │  │  ├─ LevelDefBuilder.cs
│  │  │  ├─ GameState.cs        # trạng thái bất biến từ bên ngoài
│  │  │  ├─ MoveEvent.cs        # MoveEvent + PushedCarEvent
│  │  │  └─ MoveResult.cs
│  │  ├─ Rules/
│  │  │  ├─ MoveResolver.cs
│  │  │  ├─ GateRule.cs
│  │  │  ├─ PushRule.cs
│  │  │  ├─ PortalRule.cs        # portal thường + tra liên kết hai chiều
│  │  │  ├─ PylonRule.cs         # hướng hai mặt cổng + xoay trụ
│  │  │  ├─ BarrierRule.cs       # chốt liên động + barie vừa mở
│  │  │  ├─ TableRule.cs
│  │  │  ├─ LightRule.cs
│  │  │  ├─ TileEffects.cs
│  │  │  ├─ Occupancy.cs
│  │  │  └─ GameRules.cs        # IsWin, WashedCount
│  │  └─ Solver/
│  │     ├─ StateKey.cs         # khóa byte, encoder và comparer
│  │     ├─ Solver.cs           # BFS ít lượt nhất + lệnh/kết quả
│  │     └─ StepSearch.cs       # WinSearch/PathSearch theo ngân sách node
│  ├─ Game/                     # asmdef "SqueakyLane.Game" → tham chiếu Core
│  │  ├─ LevelJsonLoader.cs     # JSON → LevelDef + LevelInfo trong cùng một lần parse; DTO cùng file
│  │  ├─ LevelSetup.cs          # dữ liệu thường → giờ, gợi ý, tutorial; không nhận ScriptableObject
│  │  ├─ GameSession.cs         # snapshot, lịch sử Undo/Restart, dùng lại SolverMove
│  │  ├─ LevelTimer.cs          # dt từ ngoài, giới hạn mỗi tick theo GameConfig; phạt Undo/Hint, sao
│  │  ├─ SearchServices.cs      # SearchBudget theo ms, StuckDetector, HintProvider
│  │  ├─ Navigator.cs           # luồng chọn/chơi/pause/thắng/thua; trả lệnh
│  │  ├─ ProgressStore.cs       # sao tốt nhất và công tắc âm/rung
│  │  ├─ GameConfig.cs
│  │  ├─ FeelConfig.cs
│  │  ├─ CameraConfig.cs
│  │  ├─ ConfigProvider.cs      # điểm đọc Resources duy nhất, nhận CarCatalog bằng tham chiếu
│  │  └─ CarCatalog.cs         # màu → material + prefab thường/tải; đã có asset trong Config (010)
│  ├─ Presentation/             # asmdef SqueakyLane.Presentation → Core, Game, TMP, uGUI
│  │  ├─ GameBootstrap.cs
│  │  ├─ BoardView.cs
│  │  ├─ CarView.cs
│  │  ├─ CarBody.cs             # 012: nhún hình con, quay bánh; reset/Pause theo ActionAnimator
│  │  ├─ FeelEffects.cs         # 012: hạt dùng chung, trần60; offset rung camera
│  │  ├─ InputController.cs     # chạm/vuốt → lệnh; ưu tiên raycast UI
│  │  ├─ ActionAnimator.cs      # events, hàng đợi mỗi xe và đặt chỗ đường hình
│  │  ├─ DebugHud.cs            # chỉ HUD gỡ lỗi; không tạo font/object khi không dùng
│  │  ├─ TableButtons.cs        # class thường, nút bàn xoay độc lập với DebugHud
│  │  ├─ FeedbackRouter.cs      # class thường, nối input/animator/UI với âm/rung và cài đặt
│  │  ├─ NavigationView.cs      # tham chiếu UI do người làm dựng trong Canvas
│  │  ├─ SfxCatalog.cs
│  │  ├─ SfxPlayer.cs
│  │  ├─ Haptics.cs
│  │  └─ VisualPalette.cs
│  └─ Editor/
│     ├─ PlayTools.cs          # Sync dùng kho thiết kế nếu có, nếu chưa có dùng 31 mẫu golden
│     ├─ LevelEditorWindow.cs  # lưới, thuộc tính, preview riêng, Undo bản nháp, config ngoài Play
│     ├─ LevelDesignIO.cs      # schema JSON, kiểm/lưu/xuất/thứ tự; không ghi golden
│     ├─ KenneyTruckMesh.cs   # Editor: chèn đoạn thùng vào mesh phái sinh, giữ cabin/bánh (010)
│     └─ KenneyCarsWindow.cs  # menu tạo màu/prefab, cập nhật 6 tải và nối catalog, không sửa FBX/scene (010)
├─ Resources/Levels/            # 34 màn thử + level_order (31 gốc, 3 bản sao thử tool), chưa phải 25 màn cuối
├─ LevelDesign/                 # kho thiết kế ngoài Play: 34 JSON + level_order; Lưu rồi Xuất/Sync
├─ Config/                      # GameConfig, FeelConfig, CameraConfig, VisualPalette, SfxCatalog + BlockMaterial
├─ Prefabs/                     # GameRoot.prefab, LevelButton.prefab
├─ Scenes/Play.unity
└─ Tests/EditMode/              # test luật + so khớp với golden file của prototype
```

**Tạo tài nguyên:** menu Setup Play tạo khung chơi ban đầu bằng API Unity. UI 009, LevelButton và SfxCatalog do người làm tạo/gắn qua Inspector; code điều hướng/âm/rung đã có nhưng chưa xác nhận đủ V2/V3. 013 P1 đã gán sprite ClockFill và chữ StuckBanner qua menu, S1 nền sáng đã áp. Menu `Tools/Squeaky Lane/013 Theme/S3 Import UI Pack` đặt importer cho bộ sprite, `S3 Apply UI Pack Skin` đã áp (25/11): dựng HUD, hộp thoại nhiều lớp, LevelButton, đúng3 preset HudOutline/HudSmall/CardText, atlas sprite UI; ghi field đầu→cuối vào `docs/handoff/013-scene-changes.md` (mục S3b là tổng hợp viết tay). Runtime chỉ đổi cách hiển thị HUD/thông báo, giữ điều hướng/luật. Các số skin trong UiTheme, bố cục đã áp có thể chỉnh Inspector; tạo/sửa hàng loạt qua menu, lỗi Inspector nhỏ sửa tay. Còn chờ Play thật/Console/ảnh V3/Stats màn 16 của S3; catalog art và tutorial theo phiếu riêng.

**013 S4 — triển khai sẵn, Play chưa áp; Asset Lab đã tạo:** menu `S4 Finish Scenery and Open Lab` tự mở Play, gọi `S4 Apply Scenery` tạo PNG radial + material/shadow quad prefab dùng chung, ánh sáng Flat (Directional1/None, ambient #9AA9C2), khay có mặt bên/mép sáng trong mesh nền, chuẩn hóa props Kenney đã có, lưu rồi quay về Lab. Atlas màu11×2 giữ `npotScale=None` để UV không trộn sai màu; báo một mục S4 ở `docs/handoff/013-scene-changes.md`. Bóng xe ở root, cập nhật theo `CarView.Show`, không có Update/material instance cho bóng; bãi giữ nguyên tọa độ ô. `AssetLab.unity`/`AssetLabConfig.asset` đã được người làm tạo qua menu18:08 và xác nhận đổi mẫu được:36 mẫu xe/tải/cơ chế/props, sửa config thấy ngay trong Scene view, thử bóng bằng property block trên clone, không lưu clone/không thêm Build Settings. Người làm yêu cầu hoàn tất triển khai rồi tự bắt lỗi sau; Run All/Console/Play/ảnh/Stats S4 chưa chốt, không coi là đã đạt V2/V3. Editor đang mở nên không chạy batch trùng project.

**009-01 ngày 24/11:** đã viết A–F/H–I, không đổi scene/prefab. Người làm yêu cầu bỏ bước Editor vì sát hạn nên **hoãn G**: chưa dùng `PlayArea`, giữ vùng camera cũ theo safe area; không cần gán ô mới. Level Editor dùng chung bảng `DesignKinds` cho schema/nhãn và lấy màu lưới từ `VisualPalette`. Chờ kiểm bản sau F/H/I trong Unity; chi tiết ở phiếu 009-01. `[FILE; CHỐT 24/11]`

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
- Kiểm chứng: AI cầm gậy chạy biên dịch/test/Console/Play **qua MCP** (hoặc CLI khi Editor đóng) và ghi vào phiếu; visual/feeling kiểm bằng ảnh Game view (người làm gửi hoặc chụp qua MCP nếu có), video hoặc APK quay màn hình. Chưa nối MCP thì ghi "chưa chạy". `[CHỐT 25/11, thay 22/11]`

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
| 002 | Dữ liệu cho Core: 31 màn cũ sang JSON object + golden trace từ `logic.js` | `002-level-data` (worktree riêng, chưa dùng) | Claude | Hoãn: gộp vào việc 005 của Codex (`docs/handoff/core-plan.md`) | — | — |
| 003 | Model và luật Core phần 1 + EditMode tests | `003-core-rules-1` | Codex | Xong (đã merge 22/11) | APPROVE (Claude 22/11) | 001 |
| 004 | Core luật phần 2: portal, trụ xoay, barie/chốt + EditMode tests | `004-core-rules-2` | Codex | Xong (đã merge 22/11) | APPROVE (Claude 22/11) | 003 |
| 005 | Dữ liệu chuẩn: 31 JSON object + golden trace và tool Node | `005-golden-data` | Codex | Xong (đã merge 22/11) | APPROVE (Claude 22/11) | 002, 004 |
| 006 | Bộ nạp JSON + phát lại golden trace kiểm luật Core | `006-loader-trace` | Codex | Xong (đã merge 22/11) | APPROVE (Claude 22/11) | 003–005 |
| 007 | Solver BFS, tìm thắng/gợi ý theo bước và đo màn 16 | `007-solver` | Codex | Xong (đã merge 22/11); cấp phát kiểm tiếp ở 008 | APPROVE có điều kiện (Claude 22/11) | 003–006 |
| 008 | Chơi được trên màn hình: phiên chơi, khối 3D, input/animation/nút debug | `008-playable` | Codex | Đã merge 23/11; phần chưa kiểm giữ trong phiếu | APPROVE có điều kiện (Claude 23/11) | 003–007 |
| 009 | Điều hướng, HUD/panel, âm thanh/rung và tool sửa màn/config ngoài Play | `009-audio-ui` | Codex | Đã vào main 24/11; EditMode352/0/1; còn V3 Android | APPROVE có điều kiện (Claude 24/11) | 008 |
| 009-01 | Dọn nợ thiết kế 009: Pause nền, setup/metadata/test màn, tách nút bàn xoay và âm/rung, gom schema Editor | `009-01-code-cleanup` | Codex, Claude | Đã vào main 24/11; phần kiểm chưa ghi giữ trong phiếu, G hoãn | APPROVE có điều kiện (Claude 24/11) | 009 |
| 010 | Xe Kenney: prefab bọc, colormap theo màu, CarCatalog và tích hợp hiển thị | `010-kenney-cars` | Codex, Claude | Đủ12 prefab, tải nối thùng2.82ô; chờ Run All mới/Play/ảnh20 trước merge | APPROVE có điều kiện (Claude 25/11) | 008, 009-01 |
| 011 | Bãi xe và cơ chế ra hình: MechanicCatalog, mặt bãi, cơ chế bậc A | `011-lot-mechanics` | Codex, Claude | P1: ảnh Claude đã nhận; portal trắng/chanh/cyan, Run All524/0/1; chờ Console Play sau đổi màu, người làm commit/merge; visual tiếp ở011-01 | APPROVE có điều kiện (Claude 25/11) | 010 |
| 011-01 | Làm đẹp nhanh bằng code/tool: camera/ánh sáng, HUD, tấm sơn trước cổng, bỏ chữ nổi, cảnh quanh bãi; học làm tay dời sau | `011-01-visual-hand-pass` (không mở) | Codex, Claude | Bỏ 25/11: phần V1–V7 gộp vào 013 khung nhìn; mục "Học sau" giữ trong phiếu | — | 011 |
| 012 | Cảm giác lõi: ngôn ngữ cơ thể xe, bánh quay, khoảnh khắc rửa, rung màn hình, ăn mừng thắng | `012-core-feel` | Codex, Claude | Xong theo quyết định người làm25/11; Run All552/0/1 theo phiếu; còn kiểm Android/Stats ở013 | APPROVE B/C (Codex25/11); Claude review phần trước, người làm chốt feeling | 011 |
| 013 | Theme + UI: skin HUD/hộp thoại, font tiếng Việt, camera và cảnh quanh bãi; APK đầu tiên | `013-theme-ui` | Codex, Claude | P1–S3 đã commit; Run All S3 561/560/0/1; S4 code/menu hoàn thiện, chưa áp Play; AssetLab36 mẫu đã tạo và người làm xác nhận đổi mẫu được; người làm tự kiểm lỗi sau, chưa chốt Run All/Console/Stats16; S5 hoãn | Claude: S1 APPROVE có điều kiện; S2 CHANGES→F1–F6/S3 theo người làm; nợ Codex review S3 giữ nguyên, S4 chờ kiểm + Claude review ảnh | 012 |
| 014 | VFX và juice: UI (popup, sao, nút, đồng hồ), gameplay (cổng, rửa, bùn, chọn xe/gợi ý, Hoàn tác), khoảnh khắc (xe thả vào, pháo giấy toàn màn); có thể lấy sprite VFX từ rar | `014-car-feel` (mở 26/11, phần xe chạy) | Claude, Codex | Đang làm phần **xe chạy** (bứt tốc, lắc khi đâm, vệt bánh, đèn) theo `docs/handoff/014-car-feel.md`; asset Codex ở phiếu 014-a; phần UI/cổng/pháo giấy chưa làm | — | 012, 013 |
| 015 | Hình đồ chơi: cổng rửa xuyên qua, camera 58°, chữ nét, shading xe, theme nền | `015-toy-look` (mở 26/11 từ `014-car-feel`) | Claude, Codex | Bước 1 cổng rửa xong (Run All 580/579/0/1); chờ người làm xem ảnh; bước 2–5 chưa làm | 015-a APPROVE (Claude) | 013, 014 |

**Kế hoạch từ 008:** `docs/handoff/play-plan.md` thay `core-plan.md` §7–9; 008 gộp logic phiên chơi + hiển thị + điều khiển, có thể commit theo mốc A/B/C, Claude review một lần cuối. 008 chưa có Navigator/điều hướng; phần đó là 009-A. `[CHỐT 22/11]`

**007 — kiểm chứng còn nợ:** đã chạy EditMode 314 qua/0 đỏ trước lần sửa bộ đo cấp phát. Người làm cho hoãn chạy lại bộ đo; số cấp phát `0 byte` của lượt cũ không hợp lệ, bản đo Profiler mới chưa chạy lại. Thời gian BFS màn 16 trung vị 1587,401 ms trong Editor; không suy thành hiệu năng Android. `[CHỐT 22/11; chi tiết: docs/handoff/007-solver.md]`

(Một dòng mỗi **việc lớn**, `NNN` theo thứ tự mở việc; việc nhỏ không ghi. Không cần ghi hash commit. Cột `Liên quan` nối việc sửa với việc gốc, ví dụ `004`.)

---

## 14. Tài liệu liên quan (Project "CTY - Idea")

- `claude/tram-rua-xe-v10-cong-tuong-tru-xoay.md` — luật cổng tường, trụ xoay, nút Gợi ý, màn 20–31, công cụ.
- `claude/tram-rua-xe-v9-co-che-moi.md` — xe tải, barie liên động, lý do chọn cơ chế.
- `claude/tram-rua-xe-noi-dung-se-sua.md` — dáng xe theo màu, điều khiển chạm, ngôn ngữ cơ thể, theme, góp ý của lead.
- Prototype: `tram-rua-xe-v10.html`; nguồn ở `/home/claude/proto` (`src/logic.js`, `tools/`, `test/`).
- **Bản chép trong repo:** `docs/reference/prototype/` (`src/logic.js`, `tools/`, `test/`, `levels-export/` là 31 màn cũ kèm `minMoves`). Chỉ để đối chiếu, không nằm trong `Assets`. Lưu ý: màn 17–19 chưa có `minMoves` (vượt 300.000 trạng thái), màn 16 cần `maxStates` > 60.000; 25 màn mới theo bộ cơ chế mục 5.1 **chưa được sinh**.
- **Dữ liệu chuẩn dạng object:** `docs/golden/` gồm 31 JSON màn cũ + trace seed cố định, dành cho kiểm loader/luật/solver C#; không phải 25 màn cuối và không vào APK. Trace schemaVersion=2 giữ 8 chuỗi random/màn và thêm một chuỗi lời giải ngắn nhất cho mỗi màn đã biết minMoves. Định dạng/cách sinh và kiểm: `docs/golden/README.md`; lệnh kiểm `node docs/golden/tools/verify.js`. Màn 17–19 giữ `minMoves=-1`, `solveExhausted=false` (chưa kết luận tối ưu). `[FILE]`
