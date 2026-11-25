# AGENTS.md — Sổ tay làm việc cho AI (Squeaky Lane)

> **Cả Claude và Codex đọc file này.** Phần riêng: `CLAUDE.md`, `CODEX.md`. Sự thật về game: `PROJECT.md`.
> Kế thừa `AGENTS.md` + `CODEX.md` của CatHome2.5D, sửa cho dự án 3D mobile, hạn 30/11, hai AI cùng làm.
> **Thứ tự đọc:** `PROJECT.md` → file này → file riêng của bạn. Không đọc lại file chưa đổi. Hai file mâu thuẫn nhau → **dừng, hỏi người làm**.
> Nhãn: `[CHỐT]` người làm đã quyết · `[FILE]` đọc từ repo · `[ĐỀ XUẤT]` · `[CHƯA KIỂM CHỨNG]`.
> Thông tin kỹ thuật của dự án (phiên bản Unity, pipeline, package, repo…) nằm ở **`PROJECT.md` §3.1**, không lặp ở đây.
> **Quy tắc phối hợp mới `[CHỐT 25/11]`:** **Claude chủ trì code**; **Codex được gọi ra** làm VFX và asset 2D/3D còn thiếu; cả hai nối Unity qua **MCP**; Claude được commit trên nhánh việc. Quy tắc cũ (Claude review/visual, Codex code) là commit `c092dac`; quay lại bằng `git checkout c092dac -- AGENTS.md CLAUDE.md CODEX.md PROJECT.md`.

---

## 0. Người làm là ai

- Thực tập sinh Unity, làm một mình, **quyết định sản phẩm và thiết kế**. Đây là bài thi thực tập: người làm phải **hiểu và giải thích được** code mình nộp. `[CHỐT]`
- Nói **tiếng Việt**, giải thích thuật ngữ khi cần; giữ nguyên tên code, API, đường dẫn, lệnh.
- Người làm còn mới: hay nói thiếu, có thể sai. Hãy **hỏi và phản biện** (mục 3.9), không chiều theo.
- Hai AI không nói chuyện trực tiếp với nhau; mọi thứ đi qua **người làm và phiếu bàn giao** (mục 1).

## 1. Hai AI cùng làm một việc

### 1.1. Vai trò `[CHỐT]`
| | Làm gì |
|---|---|
| **Claude (chủ trì)** | Lập kế hoạch, **viết và sửa code chính** (Core, Game, Presentation, UI, tool Editor, test), quyết định visual/feeling, kiểm bằng MCP (biên dịch, test, Console, Play, chụp Game view), commit trên nhánh việc. Gọi Codex bằng một phiếu nhỏ khi cần VFX hoặc asset. |
| **Codex (được gọi ra)** | Làm **VFX** (prefab hạt, shader, material hiệu ứng) và **asset 2D/3D còn thiếu** (props, sprite/texture procedural, bóng, material, Asset Lab), chỉ trong vùng file ở 1.3. Không viết logic game, không commit. |
| **Người làm** | Quyết định cuối. Chơi thử, build APK, **merge/push**. Duyệt việc giao Codex. |

Mọi **số cảm giác** (thời lượng, độ nảy, màu, góc camera, giờ, gợi ý) nằm trong `GameConfig` hoặc JSON màn, **không hard-code**, để chỉnh không cần sửa code.

### 1.2. "Người cầm gậy" `[ĐỀ XUẤT]`
Mỗi việc lớn có **một nhánh** và **một AI đang cầm** tại một thời điểm (ghi ở đầu phiếu: `Đang cầm: Codex|Claude|Người làm`). **Mặc định Claude cầm**; khi cần VFX/asset thì Claude mở phiếu nhỏ (mục tiêu, vùng file, đầu ra, ngân sách, cách kiểm), trao gậy Codex và Codex trao lại khi xong. Chỉ người cầm gậy được sửa file (ngoại lệ duy nhất: thêm dòng vào mục **Trao đổi** của phiếu, xem 1.6). Muốn trao gậy thì sửa `Đang cầm` ở đầu phiếu, thêm dòng `[BÀN GIAO]` vào mục Trao đổi rồi báo người làm. Nhờ đó hai AI **không bao giờ sửa cùng một file cùng lúc**.
**Người lái Editor (MCP) `[CHỐT 25/11]`:** tại một thời điểm chỉ người cầm gậy được sửa scene/asset, bấm Play hoặc lưu scene; AI còn lại chỉ đọc (Console, hierarchy). Không lưu `Play.unity` khi người làm hoặc AI kia đang sửa dở.

### 1.3. Sửa trực tiếp `[CHỐT]`
Vì hạn 30/11, AI cầm gậy được sửa trực tiếp (không cần xin từng lần), với các hàng rào ở mục 4.2. Chỉ những việc ở mục 2 "phải hỏi" mới cần xin.
- **Claude:** mọi thứ trong repo (code, config, scene, prefab, tài liệu).
- **Codex `[CHỐT 25/11]`:** chỉ **vùng art/hiệu ứng**: `Assets/_Project/Art/**`, `Prefabs/Environment`, `Prefabs/VFX`, config/asset riêng của hiệu ứng, shader/material, tool Editor và view **riêng** của hiệu ứng (file mới, tên rõ). **Không sửa** Core, Game, `NavigationView`, `BoardView`, `ActionAnimator`, `GameBootstrap`, `Play.unity` (trừ khi phiếu liệt kê), test của Claude. Cần nối vào những chỗ đó thì ghi `[HỎI]` cho Claude làm.

### 1.4. Review `[CHỐT 25/11]`
- **Code của Claude:** bằng chứng là biên dịch/test/Console **qua MCP** (hoặc CLI khi Editor đóng) ghi vào phiếu, rồi **người làm chơi thử trước khi merge**. Codex chỉ review diff khi **người làm yêu cầu**.
- **Asset/VFX của Codex:** **Claude review** (ngân sách batch/hạt, quy tắc asset, ảnh Game view, Codex có ở trong vùng 1.3 không).
- Chưa nối MCP hoặc chưa chạy: ghi **"chưa chạy"**, không viết "đã test". Claude APPROVE chỉ nghĩa là "ổn theo những gì Claude thấy", **không thay thế** việc người làm chơi thử.

### 1.5. Phiếu bàn giao `docs/handoff/NNN-<tên-ngắn>.md`
Một file đi suốt vòng đời một việc. `NNN` là **mã việc**, tăng dần theo thứ tự mở việc (kiểm tra thư mục trước khi lấy số).

```markdown
# NNN — <tên việc>     Cỡ: S/M/L     Trạng thái: Plan | Đang làm | Chờ review | Xong
Đang cầm: Codex | Claude | Người làm        AI tham gia: …
Liên quan: (việc gốc nếu đây là bản sửa/làm lại, ví dụ 003)     Chờ nhận: (bản sửa chưa có trong nhánh này, ví dụ 003-01)
## Mục tiêu (1–2 câu, nhìn từ người chơi)
## Phạm vi — Làm: … / Không làm: …
## Tệp dự kiến (đánh dấu cái cần Editor)
## Tiêu chí xong (đo được) + bậc kiểm chứng (V0–V3)
## Điều cấm / rủi ro / thứ có thể cắt nếu trễ
--- AI làm điền khi xong ---
## Đã làm · Tệp đổi (code/scene/prefab/asset) · Test và kết quả · Lệch kế hoạch và vì sao
## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi (ghi, không sửa)
## Bạn cần hiểu (2–4 gạch: người làm phải giải thích được)
## Phản đối kế hoạch (nếu có) — viết SỚM, trước khi làm
--- Người review điền ---
## Verdict: APPROVE | CHANGES | BLOCK — đã soi / chưa soi; phát hiện [CHẶN] [NÊN] [GỢI Ý]
--- Trao đổi (mọi bên chỉ THÊM dòng ở cuối, không sửa/xóa dòng cũ; xem 1.6) ---
## Trao đổi
YYYY-MM-DD HH:mm · Ai → Ai · [HỎI|TRẢ LỜI|BÀN GIAO|BÁO]: nội dung ngắn
```

### 1.6. Trao đổi giữa hai AI và khi mở chat mới `[CHỐT 22/11]`
- Hai AI **không nói trực tiếp**; mọi thứ đi qua repo (phiếu, `PROJECT.md`). Người làm chỉ cần nói với AI kia: "xem phiếu NNN".
- **Mục "Trao đổi" cuối phiếu:** bên nào (Claude, Codex, người làm) cũng được **thêm dòng mới ở cuối, kể cả khi không cầm gậy**; không sửa hay xóa dòng cũ. Mỗi dòng: `YYYY-MM-DD HH:mm · Ai → Ai · [thẻ]: nội dung ngắn`. Giờ lấy từ đồng hồ máy (lệnh `Get-Date`), không tự đoán.
- **Thẻ:** `[HỎI]` bên nhận hoặc người làm phải trả lời trước khi làm tiếp phần liên quan · `[TRẢ LỜI]` · `[BÀN GIAO]` trao gậy (ghi: đã làm gì, còn gì, nhánh/commit hiện tại, đã kiểm / chưa kiểm) · `[BÁO]` thông tin, không cần trả lời.
- **Trao gậy** = trong cùng một lượt: sửa dòng `Đang cầm` ở đầu phiếu **và** thêm dòng `[BÀN GIAO]`. Thiếu một trong hai thì chưa coi là đã trao.
- **Chat mới không nhớ gì.** Thứ cần nhớ phải nằm trong `PROJECT.md` hoặc phiếu. Việc đầu tiên của AI trong chat mới: đọc theo thứ tự `PROJECT.md` → `AGENTS.md` → file riêng → **phiếu người làm nêu** (đọc cả mục Trao đổi); chạy `git status` và `git log --oneline -n 10` để biết đang ở nhánh nào và những gì vừa đổi (commit cho biết cái gì đã đổi; việc đang ở đâu thì đọc phiếu). Người làm chưa nêu phiếu thì **hỏi "đang làm việc nào?"**, không đoán.
- **Cuối phiên có thay đổi:** thêm dòng `[BÁO]` hoặc `[BÀN GIAO]` để chat sau và AI kia đọc được việc đang ở đâu.
- **Việc nhỏ (không có phiếu):** người làm nói trực tiếp; điều đáng nhớ và ảnh hưởng quyết định thì ghi vào `PROJECT.md`.

---

## 2. Quy tắc nền

Dự án Unity hỏng một cách lặng lẽ: mất GUID, lưu scene dư thay đổi, giá trị serialize tự quay về mặc định đều **không báo lỗi biên dịch** mà vẫn làm game hỏng, và thường được phát hiện sau vài ngày.

**BẠN PHẢI:**
- **Giữ `PROJECT.md` đúng**: thay đổi nào làm một câu trong đó sai thì sửa nó **cùng đợt thay đổi**; báo mục nào đã sửa hoặc vì sao không cần.
- Đọc phần liên quan của `PROJECT.md` và code trước khi viết; theo pattern sẵn có.
- Ưu tiên **Editor đang chạy** hơn sửa file thô khi có kết nối (mục 4).
- Giữ **`.meta` đi kèm** mọi asset. Di chuyển/đổi tên trong cửa sổ Project của Unity.
- **Việc lớn** làm trên **nhánh việc**; **việc nhỏ** sửa và commit thẳng theo mục 8. Không bao giờ push hay merge `main` khi chưa được yêu cầu.
- Trước khi báo xong: biên dịch được, Console **không lỗi/cảnh báo mới**, test liên quan qua, `git status` chỉ có file định sửa (mục 3.3).
- Dựng thứ mới thành **prefab**; sửa scene tối thiểu và **liệt kê mọi thay đổi scene** trong báo cáo.
- Nêu giả định và điều chưa chắc; hỏi khi yêu cầu mơ hồ.

**BẠN KHÔNG ĐƯỢC — trừ khi người làm yêu cầu đúng việc đó trong cuộc trò chuyện (phải hỏi trước):**
- Push, mở PR, merge vào `main`, xóa nhánh, rebase nhánh dùng chung, force-push.
- Đổi phiên bản Unity; thêm/xóa/nâng package (`manifest.json`, `packages-lock.json`). Ngoại lệ đã duyệt 25/11: `com.coplaydev.unity-mcp` (CoplayDev, MIT) ghim `v10.3.0` (Unity MCP).
- Đổi Project Settings: Player, Quality, Graphics, Input, Tags & Layers, Physics, Time, Script Execution Order, Editor, build profiles, scripting defines.
- Sửa tay `guid`/`fileID`, xóa hoặc tạo lại `.meta`, xóa asset/script có thể đang được tham chiếu.
- Đổi tên/đảo thứ tự enum được serialize; đổi tên field serialize mà thiếu `[FormerlySerializedAs]`; đổi tên asset mà tên được dùng làm khóa (đường dẫn `Resources.Load`, tên file JSON màn).
- Lưu scene kèm thay đổi ngẫu nhiên (đồ bị dời, bật/tắt active, UI mở, dấu vết Play mode).
- Tắt, bỏ qua hoặc làm yếu test/validator/`OnValidate` cho qua việc.
- Đụng `.gitignore`, `.gitattributes`, LFS; commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Build*/`, `*.csproj`, `*.sln`.
- Build bản phát hành, ký, upload, hoặc chạm keystore/khóa/chứng chỉ. (APK debug để lead cài: **người làm** tự build trong Editor.)
- Thêm asset từ ngoài vào repo, thêm thư viện/pattern mới (DI, event bus, thư viện async…), **đổi luật chơi** mà chưa được duyệt.

**Giá trị trong `GameConfig` / JSON màn:** dự án một người nên không cần "chủ". AI được đổi giá trị trong phạm vi phiếu, nhưng báo `field · cũ → mới · vì sao`.

Luôn được phép (chỉ đọc): `git status/diff/log/show`, đọc file, xem Editor qua MCP (hierarchy, component, Console).

---

## 3. Cách làm việc với người làm `[CHỐT — kế thừa CODEX.md cũ]`

### 3.1. Làm rõ trước khi làm
- Thiếu thông tin ảnh hưởng thiết kế, phạm vi, đúng/sai hoặc tiêu chí xong → **hỏi trước bước phụ thuộc**. Nói cụ thể điều gì chưa rõ, vì sao quan trọng, nó chặn việc nào. Việc độc lập vẫn làm tiếp.
- **Đừng đoán ý.** Trong Unity đoán sai thường nằm trong scene/prefab/asset, khó thấy và khó sửa hơn code.
- Người làm mới nên nói thiếu: hỏi về luật biên, trải nghiệm mong muốn, đừng chỉ nhận phương án đầu tiên. **Gộp câu hỏi (khoảng ≤ 5)**, tách **chặn ngay** và **để sau**, kèm ví dụ và khuyến nghị.
- Không bắt người làm chọn mọi chi tiết cài đặt vặt: tự quyết dựa trên repo và thiết kế, giải thích khi quan trọng. **Không hỏi lại điều đã `[CHỐT]`** trừ khi có bằng chứng mới hoặc mâu thuẫn thật.
- Yêu cầu mâu thuẫn quyết định trước đó: gọi tên mâu thuẫn, nêu hậu quả, đề xuất phương án khác. Người làm quyết rõ thì cập nhật `PROJECT.md` và làm tiếp.
- Người làm vắng và việc **đảo ngược được**: chọn mặc định hợp lý, ghi giả định vào phiếu, đi tiếp. Việc không đảo ngược được: làm phần chuẩn bị rồi dừng.

> Ví dụ: "Bạn bảo 'cho xe nảy đẹp hơn'. Mình cần rõ: nảy lúc nào (phanh, ra cổng, rửa xong)? Biên độ/thời lượng cụ thể bao nhiêu hay lấy từ game nào làm mẫu? Số này để ở `GameConfig` được không?"

### 3.2. Kế hoạch trước việc lớn
- **Việc lớn:** hệ thống/cơ chế mới, đụng ≥ 3 file, đổi luật, đổi package/settings/asmdef, hoặc đụng scene/prefab → cần **phiếu có kế hoạch** và người làm duyệt. **Việc nhỏ** (cỡ S: lỗi chính tả, sửa ≤ 2 file code/config/tài liệu; **không** đổi luật, asset, scene, prefab, package, settings, asmdef) làm thẳng, không phiếu, không nhánh riêng (mục 8). Thêm một field vào config có sẵn là việc nhỏ (nếu ≤ 2 file); **tạo loại config/catalog mới** là việc lớn. Nghi ngờ nhỏ hay lớn thì coi là lớn. Người làm nói "khỏi plan" thì làm thẳng.
- Kế hoạch gồm: **phạm vi** (làm/không làm) · **các bước** theo thứ tự · **tệp bị ảnh hưởng** (đánh dấu cái cần Editor) · **rủi ro** (thứ tự khởi tạo, static qua lần reload, đổi tên field serialize, GUID, xung đột scene, khác biệt nền tảng, hiệu năng) · **tiêu chí xong** (bậc V ở 3.3).
- **Cỡ việc (hạn 30/11):** S ≤ 1 giờ · M ≤ nửa ngày · L ≤ 1 ngày; lớn hơn thì chia. Vượt **gấp đôi** time-box → dừng báo. Phiếu nêu thứ có thể cắt nếu trễ (thứ tự cắt: `PROJECT.md` §10).

### 3.3. Kiểm chứng (theo bậc)
| Bậc | Áp dụng | Cần có |
|---|---|---|
| **V0** | Chỉ tài liệu | Đối chiếu quyết định, liên kết, định dạng; `git diff --stat` đúng file (kể cả file chưa theo dõi). Không cần Editor/Play |
| **V1** | Logic Core (C# thuần) | EditMode test hoặc .NET; **golden test khớp**; thêm test cho luật mới |
| **V2** | MonoBehaviour / prefab / UI / asset | Editor biên dịch sạch, **Console không lỗi/cảnh báo mới**, Play thử đúng tình huống trong phiếu; đụng khởi tạo/trạng thái thì thêm nạp lại scene và vào lại Play |
| **V3** | Cảm giác / visual / hiệu năng | Ảnh hoặc video Game view, hoặc APK trên máy; đo fps nếu liên quan |

- Phiếu nói bậc nào thì làm bậc đó; không nói thì **tự chọn và nêu**. Chỉ hỏi người làm về **kết quả chấp nhận được**, không đẩy việc chọn kiểm tra thường lệ cho họ.
- **Ai kiểm bậc nào** `[CHỐT 25/11]`: bậc **V1 và V2** (test, biên dịch, Console, Play) do **AI đang cầm gậy** chạy **qua Unity MCP** (hoặc Unity CLI khi Editor đóng) và ghi kết quả vào phiếu. Bậc **V3** (hình, cảm giác): ảnh/video do người làm gửi, hoặc ảnh Game view chụp qua MCP nếu công cụ có `[CHƯA KIỂM CHỨNG]`. Chưa nối MCP/chưa chạy thì ghi **"chưa chạy"**.
- `git diff --stat` chỉ có file định sửa; mỗi asset mới có `.meta`.
- Không có Editor/máy: nói rõ kiểm tra nào chưa chạy. **Đọc tĩnh không phải "đã test".**

### 3.4. Tự đánh giá trước khi giao `[CHỐT — giữ nguyên ngưỡng]`
- Đối chiếu: (1) **kỳ vọng người làm** (làm đúng điều được yêu cầu, gồm tiêu chí xong); (2) **quy tắc** (file này, file riêng, quy ước dự án, và `PROJECT.md` còn đúng không). Một trong hai không đạt thì lặp lại trước khi giao.
- Mục tiêu **tự tin ≥ 80%** cho cả hai (người làm có thể đổi theo từng việc).
- **Tối đa 3 vòng tự sửa.** Vẫn dưới ngưỡng thì **dừng và hỏi**: đã thử gì, kẹt đâu, mức tự tin hiện tại.
- Khi giao: nêu mức tự tin, chỗ cần xem lại, **và danh sách đã kiểm / chưa kiểm** (con số % dễ tô hồng, danh sách thì kiểm được).

> Ví dụ: "~85% tự tin. Cần xem: hồi chiêu dùng thời gian có scale nên đứng khi menu đặt `timeScale = 0`. Thường là đúng, nhưng nếu menu cho hồi chiêu chạy tiếp thì cần unscaled time. Chưa kiểm: trên máy thật."

### 3.5. Lặp đúng mức
- Việc đơn giản: một lượt. Việc phức tạp: **Core C# thuần + test** → `MonoBehaviour`/view → prefab → **scene (cuối cùng, tối thiểu)**.
- Sau mỗi lượt: đã làm gì, còn gì, cần người làm làm gì.
- Khi người làm **duyệt rõ** (ví dụ "oke", "ổn", "duyệt"): đưa **nội dung commit sao chép được** (tiêu đề `[NNN] …` + vài dòng nếu cần). Claude được commit trên nhánh việc theo mục 8 (Codex không commit); push/PR/merge cần yêu cầu riêng. Đồng ý ý tưởng hay kế hoạch **không** có nghĩa việc dở dang sẵn sàng commit.

### 3.6. Giữ phạm vi
Chỉ đổi điều được yêu cầu. Không refactor, đổi tên, format "tiện tay". Thấy việc đáng sửa → **ghi vào phiếu** (và `PROJECT.md` nếu quan trọng), không sửa lén. Diff scene/prefab khó đọc: giữ nhỏ và mô tả bằng lời.

### 3.7. Đọc trước khi viết
Đọc code, prefab, `PROJECT.md` trước. Theo pattern có sẵn (cách tìm service, nối event, chỗ đặt tuning, đặt tên, thư mục). Không đưa pattern mới khi đã có; mâu thuẫn quy tắc thì theo codebase và báo.

### 3.8. Minh bạch
- Nêu giả định. Khi lựa chọn làm đổi gameplay, quy trình, kiến trúc dài hạn hoặc phạm vi: giải thích **đánh đổi** và **khuyến nghị**; hỏi khi mục tiêu hoặc quyền duyệt quyết định. Chi tiết vặt đảo ngược được thì tự chọn.
- **Nhãn bằng chứng:** đã kiểm trong code · đã kiểm trong Editor/Play · đã kiểm trên máy · ước lượng · **chưa kiểm**.

### 3.9. Dạy và phản biện, không thông đồng `[CHỐT]`
- Giải thích lý do 1–2 câu, gắn với nguyên tắc nó phục vụ. Giúp người làm có trực giác Unity (vì sao `Awake` không chạy trên object tắt, vì sao static sống qua reload, vì sao `?.` che object đã hủy…). Mỗi báo cáo có mục **"Bạn cần hiểu"**.
- **Phản biện, không chiều theo:**
  1. Với đề xuất thiết kế/kiến trúc (của người làm **hoặc của AI kia**): nêu ít nhất **một rủi ro hoặc phương án thay thế cụ thể**, kèm bằng chứng và hậu quả (người chơi, hạn 30/11, bảo trì).
  2. Người làm **quyết rõ** thì làm theo ("tranh luận rồi cam kết"), ghi `[CHỐT]` vào `PROJECT.md`, không tranh luận lại trừ khi có bằng chứng mới.
  3. Không khen cho có, không "đồng ý" thay cho việc kiểm tra.
  4. Phản biện theo **mức tác động**, không bới lặt vặt.
  5. Phản biện AI kia qua phiếu: người làm ghi "Phản đối kế hoạch" **trước khi làm**; người review ghi vào verdict. Người làm phân xử.
  6. Quy tắc trong các file này **có thể sai hoặc rườm**: thấy thì nói và đề xuất bỏ/sửa (mục 11).

### 3.10. Xử lý lỗi và khi nào dừng
- Lỗi: giải thích chuyện gì xảy ra, vì sao, các lựa chọn. **Không thử lại y nguyên.**
- **Dừng và nói chuyện với người làm** khi: tự sửa 3 vòng chưa xong · 2 lần lỗi biên dịch liên tiếp chưa rõ nguyên nhân · cần đổi package/settings · vượt phạm vi phiếu hoặc vượt gấp đôi time-box · kết quả khác dự kiến mà không giải thích được · tài liệu mâu thuẫn · sắp làm việc phá hủy. Báo: đã thử gì, kẹt đâu, đề xuất gì. Nếu người làm quyết **bỏ và làm lại**, theo mục 8.
- **Không giải pháp phá hủy** nếu chưa được duyệt riêng: xóa asset/`.meta`, xóa hoặc import lại `Library/`, hoàn lại thay đổi của người khác trong scene/prefab, `git reset --hard`, force-push, tắt test/validator.

---

## 4. Unity Editor và sửa trực tiếp

### 4.1. Kết nối Editor
**Unity MCP `[CHỐT 25/11]`:** `com.coplaydev.unity-mcp` (CoplayDev, MIT) ghim `v10.3.0`, cho Claude và Codex điều khiển Editor **đang mở** (Window → MCP for Unity; server chạy cục bộ qua `uv`, không bật chế độ remote). Công cụ MCP chỉ xuất hiện ở **chat mới** sau khi cấu hình; xem danh sách công cụ để biết tên đúng, đừng đoán. Khi chưa nối: kiểm tra MCP có phản hồi không, nhờ người làm mở đúng phiên bản Unity và bấm Start/Configure, chờ xác nhận rồi thử lại. Editor mất focus có thể bị giảm tốc: khi cần Play, nhờ người làm giữ focus hoặc bật *Run In Background*. Editor đang mở thì **không chạy CLI batch** cho cùng project; Editor đóng thì dùng `unity test`/`unity run` (PROJECT §2.5). Chỉ **một AI lái Editor** tại một thời điểm (1.2).

### 4.2. Sửa trực tiếp scene/prefab `[CHỐT]` (hàng rào `[ĐỀ XUẤT]`)
Được sửa trực tiếp, theo thứ tự ưu tiên:
1. **Qua Editor** (MCP/CLI) khi có kết nối.
2. **Viết một menu Editor** (`Tools/…`) tạo/sửa asset bằng API (AssetDatabase, PrefabUtility) rồi nhờ người làm bấm. Đây là cách an toàn nhất khi **tạo asset mới** (Unity tự tạo `.meta`, GUID đúng).
3. **Sửa YAML tay** chỉ khi không có Editor kết nối và thay đổi **nhỏ, rõ** (thêm/bỏ một component, đổi một giá trị đã biết). Điều kiện: Editor **không đang mở** cảnh/prefab đó (hoặc người làm sẵn sàng nạp lại); `git status` **sạch** trước khi sửa; không tạo/đổi `guid`/`fileID` bằng tay; không tạo prefab/scene/asset **mới** bằng YAML; ghi rõ "sửa YAML trực tiếp" trong báo cáo; người làm mở lại Unity, đọc Console rồi mới commit.

Luôn: **một người cầm gậy** (1.2); sửa nhỏ, ưu tiên prefab hơn scene; **không lưu scene kèm thay đổi ngẫu nhiên**; báo cáo liệt kê `đường dẫn object · component · field · cũ → mới`. **Không bao giờ sửa YAML khi Editor đang mở file đó** (lần lưu sau của Editor sẽ ghi đè).

### 4.3. Console và kiểm tra khi MCP chưa nối `[CHỐT 25/11]`
Có MCP thì đọc Console trực tiếp qua công cụ MCP. Khi MCP chưa nối (hoặc chat chưa nạp công cụ):
- **Đọc `Editor.log`** (chỉ đọc, không sửa/xóa): `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Lọc theo `error CS`, `warning CS`, `Exception`, `NullReference`. Log của lần chạy trước thường nằm ở `Editor-prev.log` `[CHƯA KIỂM CHỨNG]`. Người làm nói rõ đã nạp lại hoặc Play lúc nào để biết đoạn nào là mới.
- **Ảnh/video** Game view, ảnh chụp Console, APK quay màn hình do người làm gửi (bậc V3).
- **Chạy test và Play trong Editor:** AI đang cầm gậy qua MCP (hoặc CLI khi Editor đóng), hoặc người làm; kết quả ghi vào phiếu ("Test và kết quả") hoặc mục Trao đổi.
- Đọc `Editor.log` **không thay** cho "đã Play thử": nhãn bằng chứng vẫn là "đã kiểm trong code/log", không phải "đã kiểm trong Editor/Play".

---

## 5. Unity hoạt động thế nào (đọc trước khi viết code)

Phần lớn lỗi AI tạo ra đến từ bảng này. Claude dùng nó làm **checklist review**.

| Chủ đề | Cần biết | Nên làm |
|---|---|---|
| **GUID và `.meta`** | Asset được tham chiếu bằng GUID trong `.meta`, không phải đường dẫn | Di chuyển/đổi tên trong Editor; không sửa/tạo lại `.meta` |
| **Dữ liệu serialize thắng** | Giá trị khởi tạo trong C# chỉ là mặc định cho thể hiện *mới*; giá trị đã lưu trong scene/prefab/asset ghi đè | Muốn đổi giá trị đang chạy thì đổi asset/prefab/scene, không chỉ đổi mặc định trong C# |
| **Đổi tên field** | Đổi tên field serialize làm mất giá trị đã lưu ở mọi nơi | `[FormerlySerializedAs("tenCu")]` |
| **Enum** | Lưu dạng số nguyên | Chỉ thêm cuối; không đảo thứ tự, đổi tên, xóa |
| **Thứ tự vòng đời** | `Awake` → `OnEnable` → `Start`. Mọi `Awake` chạy trước mọi `Start` khi nạp, nhưng thứ tự giữa các script không đảm bảo | Tự khởi tạo trong `Awake`; đọc object khác trong `Start` |
| **Object bị tắt** | GameObject lưu ở trạng thái tắt không chạy `Awake`/`OnEnable` đến khi bật | Đừng giả định singleton của panel đang tắt tồn tại |
| **Fake null** | `UnityEngine.Object` đã hủy `== null` nhưng không phải null C# | Dùng `!= null`, không dùng `?.`/`??` với object Unity |
| **Static và domain reload** | Static sống qua nạp scene, và sống qua Play lại nếu *Enter Play Mode Options* bỏ domain reload | Reset static thay đổi trong `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` |
| **ScriptableObject lúc Play** | Sửa SO lúc Play **lưu lại** trong Editor | Không sửa SO dùng chung lúc chạy; sao chép state sang object thường |
| **Sửa lúc Play** | Sửa object scene lúc Play mất khi thoát | Sửa ngoài Play mode |
| **Prefab và override** | Instance trong scene chỉ lưu override | Sửa prefab, không chất đống override |
| **Luồng chính** | Hầu hết API Unity chỉ chạy trên main thread | Không gọi Unity từ `Task.Run`/thread |
| **Vòng đời async** | `async`/coroutine có thể sống lâu hơn object hoặc Play mode | `destroyCancellationToken`/`Awaitable` hoặc dừng coroutine trong `OnDisable`; không `async void` (trừ handler sự kiện) |
| **Thời gian** | `timeScale = 0` dừng `deltaTime`, `FixedUpdate`, coroutine có scale, nhưng không dừng input/UI | Chọn scaled hay unscaled **có chủ đích** (đồng hồ đếm ngược của màn!) |
| **Assembly** | Thư mục `Editor/` và asmdef Editor không vào build; API `UnityEditor` trong code runtime làm hỏng build | Bọc `#if UNITY_EDITOR` hoặc để trong assembly Editor; **Core không `using UnityEngine`** (`noEngineReferences`) |
| **Resources** | Mọi thứ trong `Resources` đều bị đóng vào build; khóa là chuỗi | Chỉ để JSON màn và thứ nhỏ; coi tên/đường dẫn là API |
| **Vật lý** | Chạy trong `FixedUpdate` | Dùng API Rigidbody trong `FixedUpdate` |
| **Mobile — khung hình** | Trên Android, nếu không đặt thì thường chạy 30 fps `[CHƯA KIỂM CHỨNG trong project này]` | Đặt `Application.targetFrameRate` ở **một chỗ**, giá trị lấy từ `GameConfig` |
| **Mobile — cấp phát** | Cấp phát mỗi frame gây GC giật | Không tạo list/chuỗi/LINQ trong `Update`; cache component; pool vật thể sinh nhiều |
| **Camera trực giao** | Kích thước theo `orthographicSize`, không theo khoảng cách | Căn bãi theo tỉ lệ màn dọc; HUD dùng `Screen.safeArea` |
| **Input cảm ứng** | Vị trí chạm là pixel; xe nằm trong thế giới 3D | Raycast rồi tính trên **trục thân xe trong không gian bàn chơi**; phân biệt chạm/vuốt bằng ngưỡng; UI không để chạm xuyên xuống bàn chơi |
| **Asset 3D lấy từ ngoài/AI** | Số tam giác, vật liệu, kích thước texture có thể quá nặng; có thể vướng bản quyền | Kiểm trước khi đưa vào repo (và hỏi: thêm asset ngoài thuộc nhóm "phải hỏi"); build < 250 MB |

---

## 6. Cấu trúc repo

Cây thư mục và quy ước đặt tên: **`PROJECT.md` §8** (Core / Game / Presentation / UI / Editor; `Resources/Levels`; `Config/GameConfig`). Thông tin phiên bản/pipeline/package: **`PROJECT.md` §3.1**. Không sao chép sang đây để khỏi lệch.

## 7. Công thức làm việc (ngắn)

- **Sửa code gameplay:** tạo nhánh (mục 8) → sửa → để Editor biên dịch, đọc Console, chạy test và kiểm đúng bậc V → báo cáo/phiếu, kèm cập nhật `PROJECT.md`.
- **Thêm/sửa giá trị chỉnh được:** đặt vào config (xem "Config" ngay dưới) hoặc JSON màn, không để literal trong logic. Field mới: `[SerializeField]` + `[Tooltip]` (+ `[Range]`/`[Min]`), mặc định = giá trị mong muốn; asset cũ cần lưu lại trong Editor.
- **Config — khi nào và ở đâu** `[CHỐT]`:
  - **Khi nào đưa vào config:** (a) giá trị sẽ chỉnh từ **hai lần trở lên** khi chơi thử; (b) người khác ngoài Codex (Claude, người làm) cần chỉnh mà **không qua code**; (c) thêm một "món" mới (màu xe, cơ chế, theme) mà phải sửa nhiều chỗ. **Không đưa vào config:** luật lõi, cấu trúc code, giá trị dùng một chỗ và không đổi (dùng hằng số có tên).
  - **Mọi thứ liên quan "setup nhanh" ưu tiên là dữ liệu, không phải code:** thêm một màu xe, một cơ chế hay một theme = thêm **một dòng vào catalog** + prefab. Nếu để thêm một "món" mà phải sửa code logic thì đó là dấu hiệu thiếu config: báo trong phiếu.
  - **Chia theo miền, thư mục `Assets/_Project/Config/`:** `GameConfig` (giờ, phạt hoàn tác, gợi ý, ngưỡng sao, bật/tắt cơ chế, fps) · `FeelConfig` (chúi, nảy, rung, thời lượng hiệu ứng, âm lượng) · `CameraConfig` (góc, kích thước, mờ khi bị che) · catalog (`CarCatalog` màu → prefab/dáng/vật liệu, `MechanicCatalog` cơ chế → prefab/icon/tutorial, `ThemeConfig`) · màn chơi `Resources/Levels/level_NN.json` + danh sách thứ tự. **Không gom thành một file khổng lồ** (khó tìm, dễ xung đột merge).
  - **Cách làm:** đọc config qua **một chỗ** (một `ConfigProvider`), không rải `Resources.Load`; ScriptableObject **không sửa lúc chạy** (sao chép sang state runtime); thêm `OnValidate` để chặn giá trị sai; đổi tên field theo `[FormerlySerializedAs]`; JSON màn dùng **khóa chữ** (`"color":"pink"`) và tra qua catalog, vì JSON không tham chiếu được prefab.
  - **Không tạo config "phòng xa":** chỉ tạo khi có lý do ở phiếu, và ghi `config mới: tên · field · vì sao` vào báo cáo.
  - **AI được sửa số** trong file `.asset` (YAML dạng chữ) hoặc JSON, miễn Editor chưa sửa dở asset đó; báo `field · cũ → mới`.
- **Thêm component:** một class một file, tên file = tên class; `[SerializeField] private`; cache `GetComponent` trong `Awake`; đăng ký trong `OnEnable`, hủy trong `OnDisable`; gắn vào **prefab**.
- **Thêm ScriptableObject:** chỉ chứa dữ liệu (không sửa lúc chạy), `OnValidate` không tác dụng phụ; tạo instance trong Editor hoặc qua menu Editor.
- **Sửa màn (JSON):** sửa file → nạp thử qua solver → so/cập nhật `minMoves` → chạy test "25 màn đều giải được".
- **Đổi tên/di chuyển script, asset:** trong cửa sổ Project; tìm tham chiếu chuỗi (`Resources.Load`, tên file JSON, tên sự kiện animation, UnityEvent trong scene/prefab: những thứ này hỏng âm thầm).
- **Thêm package/asmdef:** chỉ khi được yêu cầu; asmdef: vẽ dependency trước (asmdef mới không thấy `Assembly-CSharp`).
- **Chạy test:** Unity Test Framework (EditMode cho logic thuần). `unity test . --mode EditMode …` hoặc batch mode **chỉ khi project không mở trong Editor**; đang mở thì chạy qua Test Runner/MCP. Luật chơi đặt trong class C# thuần để test được.
- **Build:** người làm build APK trong Editor. AI không build, không ký, không upload.
- **Hiệu năng:** đo trước khi sửa (Profiler; trên máy thật cho mobile), báo số trước/sau.

## 8. Git và công việc `[CHỐT]` + chi tiết `[ĐỀ XUẤT]`

- **Việc nhỏ → sửa và commit thẳng** `[CHỐT]` (không nhánh, không phiếu). Điều kiện coi là nhỏ: xem mục 3.2. Trước khi người làm commit: biên dịch sạch, Console không lỗi/cảnh báo mới, test liên quan qua. Commit vào `main` nếu **không có nhánh việc lớn đang mở**; nếu đang có thì commit vào nhánh việc lớn đó (tiêu đề ghi `[nhỏ]`) để `main` luôn gọn. **Ngoại lệ:** sửa lỗi của việc **đã merge** thì không vào nhánh lớn đang mở mà đi bằng nhánh `NNN-MM` (xem bên dưới). Commit làm hỏng build → sửa hoặc revert ngay. Việc nhỏ không cần mã `NNN` và không cần ghi sổ việc (Git log đủ). Claude review gộp theo mốc, không từng việc.
- **Việc lớn → nhánh mới** `[CHỐT]`, đặt theo **mã việc, không theo AI**: `NNN-<mô-tả-ngắn>` (ví dụ `004-water-gate`), tạo từ `main` mới bằng `--no-track`. `NNN` tăng dần theo **thứ tự mở việc lớn** để xem lại nhánh cũ theo trình tự. Một việc lớn = một nhánh, **cả hai AI cùng commit lên nhánh đó** theo luật "người cầm gậy" (1.2). Công cụ có tự gắn tiền tố thì giữ nguyên, miễn phần `NNN-…` còn trong tên.
- `[ĐỀ XUẤT]` Một lúc chỉ nên mở **một** nhánh việc lớn: đỡ xung đột scene/prefab, và đỡ Unity import lại khi đổi nhánh. Nhánh sống lâu thì gộp `main` vào thường xuyên.
- **Nhánh việc lớn là đồ bỏ được** `[CHỐT: lý do của người làm]`: chỉ chứa thay đổi **thuộc phạm vi phiếu của nó**. Phát hiện lỗi hoặc việc thuộc việc **khác** (nhất là việc đã merge) thì **tách ra nhánh riêng và vào `main` độc lập**. Nhờ vậy nếu nhánh đang làm hỏng phải bỏ thì không mất bản sửa.
- **Tên nhánh sửa lỗi / làm lại: `NNN-MM-<mô-tả>`** `[CHỐT]` (ví dụ `003-01-fix-undo-timer`): `NNN` là việc gốc, `MM` là số thứ tự (01, 02…) các nhánh tiếp theo của việc đó. **Không lồng** (`003-01-01`): sửa của bản sửa là `003-02`. Chỉ dùng cho **sửa lỗi, chỉnh hoặc làm lại trong phạm vi phiếu gốc**; tính năng mới dựa trên việc đó vẫn lấy số mới. Dùng gạch ngang, không dùng `/`. Tên nhánh xếp theo việc gốc; **thứ tự mở thật** nằm ở sổ việc.
- **Quay lại sửa việc cũ** (việc gốc là `NNN`):
  - Chưa merge → làm tiếp **trên chính nhánh đó**, cùng phiếu.
  - Đã merge, lỗi nhỏ (đủ điều kiện mục 3.2) và **không có** nhánh lớn đang mở → sửa và commit thẳng `main`.
  - Đã merge, lỗi lớn **hoặc đang có nhánh lớn mở** → nhánh `NNN-MM` từ `main` hiện tại. **Không rẽ từ commit cũ** (sẽ mất thay đổi đã vào `main` sau đó).
  - Cần xem/tái hiện trạng thái cũ → `git show`/`git diff`. Bắt buộc checkout commit cũ thì **hỏi trước** (Unity phải import lại) và không sửa trên đó.
- **Đang làm 004 mà phải sửa 003 (đã merge)** `[CHỐT]` + chi tiết `[ĐỀ XUẤT]`: (1) commit tạm 004 (`[004] WIP`), `git status` sạch; (2) từ `main` (có 003, chưa có 004) tạo `003-01-…`, phiếu ghi `Liên quan: 003`, đồng thời ghi `Chờ nhận: 003-01` vào phiếu 004; (3) sửa và kiểm chứng **khi chưa có 004**; (4) người làm duyệt, merge vào `main`; (5) quay về 004, **merge `main` vào**, kiểm tra lại, xóa dòng `Chờ nhận`. Lưu ý: Unity import lại hai lần, và merge `main` vào 004 có thể đụng scene/prefab nếu cả hai cùng sửa (một lý do để giữ scene mỏng).
- **Bỏ và làm lại một nhánh lớn** `[ĐỀ XUẤT]`: (1) dừng theo mục 3.10, **người làm quyết bỏ**; (2) AI lập **danh sách cứu**: commit Core/test dùng lại được (cherry-pick), giá trị config, bài học; (3) ghi **bài học** (vì sao hỏng) vào phiếu để lần sau không lặp lại; (4) **không xóa** nhánh cũ, sổ việc ghi `Bỏ`; chỉ xóa khi người làm yêu cầu; (5) mở `004-01-…` từ `main` hiện tại (đã gồm các bản sửa đã merge), kế hoạch cập nhật theo bài học. Không dùng `git clean` hay `reset --hard` để "dọn".
- **Merge sớm theo lớp** `[ĐỀ XUẤT]`: Core + test (V1) kiểm chứng được độc lập với scene, nên tách việc lớn để **Core vào `main` sớm**, phần view/prefab/scene (dễ hỏng nhất) làm trong việc riêng sau đó. Bỏ nhánh view không mất Core.
- **Các bước mở nhánh mới:** (1) `git status` phải **sạch**, không sạch thì dừng và hỏi, không tự stash/reset; (2) đưa `main` local về bản mới nhất (fetch rồi fast-forward; không có remote thì bỏ qua); (3) lấy mã: việc mới → `NNN` kế tiếp (số lớn nhất trong `docs/handoff/`, sổ việc và `git branch -a`, cộng 1); bản sửa/làm lại → `NNN-MM` kế tiếp của việc gốc; (4) `git switch --no-track -c <tên-nhánh> main`; (5) tạo phiếu có `Liên quan` (nếu là bản sửa/làm lại); (6) thêm một dòng vào sổ việc. Báo người làm biết Unity sẽ import lại khi đổi nhánh.
- Muốn xem lại theo thứ tự thì dựa vào sổ việc (ghi theo thứ tự mở thật) và số `NNN`.
- Không tạo trước nhánh cho ý tưởng chưa làm.
- **Commit `[CHỐT 25/11]`:** **Claude được commit** trên nhánh việc theo mốc (biên dịch/test xanh), tiêu đề `[NNN] …` (việc lớn) hoặc `[nhỏ] …` (việc nhỏ), dòng cuối `AI: Claude` (thêm `, Codex` nếu có phần Codex). **Codex không commit**: báo Claude. **Không push/PR/merge**, không bao giờ vào `main`: người làm làm (hoặc cho phép riêng, refspec cùng tên nhánh).
- Không merge khi chưa có bằng chứng kiểm đúng bậc V của phiếu và người làm chưa chơi thử.
- Scene/prefab: mô tả thay đổi bằng lời trong commit/phiếu (diff YAML khó đọc). Xung đột scene toàn nhiễu `fileID`: **không tự gộp tay**, dừng và hỏi (thường lấy một phía rồi làm lại thay đổi kia trong Editor).
- **Sổ việc** ở `PROJECT.md` §13: một dòng mỗi **việc lớn** `NNN` hoặc `NNN-MM` `| việc | nhánh | AI | trạng thái | verdict | liên quan`, ghi theo **thứ tự mở thật**. Trạng thái có thể là `Bỏ` khi nhánh bị bỏ. Cập nhật khi **mở** và khi **xong**; không ghi hash, reflog, giờ phút.
- **Báo cáo cuối việc** là phần "AI làm điền" của phiếu (mục 1.5). Không sửa lỗi không liên quan trong việc đang làm.

## 9. Xử lỗi nhanh

| Triệu chứng | Nguyên nhân / cách xử lý |
|---|---|
| `NullReferenceException` lúc khởi động | Đọc tham chiếu object khác trong `Awake`/`OnEnable` khi nó chưa khởi tạo hoặc đang tắt. Đọc trong `Start` hoặc xếp hàng việc |
| `MissingReferenceException` | Object đã hủy vẫn được giữ (static, event, `?.` che fake null). Hủy đăng ký trong `OnDisable`/`OnDestroy`, dùng `!= null` |
| "The referenced script (Unknown) is missing" | Script đổi tên/di chuyển mất `.meta`, tên class ≠ tên file, hoặc có lỗi biên dịch. Sửa lỗi biên dịch trước; lấy lại `.meta` từ git |
| Giá trị "không đổi" khi chạy | Bạn sửa mặc định trong C#; giá trị lưu trong scene/prefab/asset thắng |
| Giá trị về mặc định sau khi đổi tên field | Thiếu `[FormerlySerializedAs]` |
| Trạng thái lần chơi trước sót sang lần sau | Static chưa reset (tắt domain reload) hoặc state theo từng lượt qua nhiều scene |
| ScriptableObject đổi sau khi chơi | Code chạy sửa asset; sao chép sang state runtime |
| Xung đột merge scene/prefab đầy `fileID` | Chưa cấu hình UnityYAMLMerge. Đừng gộp tay; dừng và hỏi |
| Texture/âm thanh chỉ là file chữ nhỏ | Chưa kéo LFS: `git lfs install && git lfs pull` |
| Chạy trong Editor, lỗi trong build | API `UnityEditor`, stripping, scene thiếu trong build list, code riêng theo nền tảng |
| Coroutine/async chạy tiếp sau khi object mất | Dừng trong `OnDisable`; dùng `destroyCancellationToken` |
| Chạm không ăn vào xe/UI | Thiếu `EventSystem`, có Image bật raycast che trên, sai Input Module, hoặc vùng chạm nút nằm dưới xe |
| Batch mode / `unity test` fail ngay | Project đang mở ở Editor khác, sai phiên bản, hoặc license chưa kích hoạt |
| Công cụ báo chưa có Editor | Nhờ người làm mở project và nối cầu (mục 4.1) |

## 10. Cài máy (làm một lần)

1. Dùng **đúng phiên bản Unity** ghi ở `ProjectSettings/ProjectVersion.txt`, kèm module Android. Mở project bằng phiên bản khác sẽ nâng cấp nó: đừng làm khi chưa hỏi.
2. **Git LFS** nếu `.gitattributes` có `filter=lfs`: `git lfs install` rồi `git lfs pull`. Repo không dùng LFS thì bỏ qua `[CHƯA CHỐT]`.
3. **UnityYAMLMerge** chỉ cần khi `.gitattributes` trỏ scene/prefab sang `merge=unityyamlmerge`; không thì bỏ qua.
4. Kiểm tra (chỉ đọc, báo nếu sai): Version Control **Visible Meta Files**, Asset Serialization **Force Text**.
5. Mở project một lần và chờ import xong trước khi chạy gì; lần đầu có thể lâu, đừng ngắt.

## 11. Tài liệu sống

- **`PROJECT.md` phải đúng**, sửa cùng đợt thay đổi làm nó sai.
- Các file quy tắc (`AGENTS.md`, `CLAUDE.md`, `CODEX.md`) **chỉ sửa khi người làm duyệt**. AI được **đề xuất** bỏ/sửa quy tắc (có lý do, có ví dụ vướng ở đâu); không tự sửa để lách giới hạn.
- **Rà lại quy tắc vào ngày 3 và ngày 6** của lộ trình (`PROJECT.md` §10): bỏ cái thừa, giữ cái cứu được việc.
