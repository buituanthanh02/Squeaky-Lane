# 009-01 — Dọn nợ thiết kế của 009     Cỡ: L (≤ 1 ngày)     Trạng thái: Plan
Đang cầm: Codex (sau khi 009 đã merge và mở nhánh)        AI tham gia: Codex (làm), Claude (plan, review)
Liên quan: 009     Chờ nhận: —

> Nhánh `009-01-code-cleanup`, tạo từ `main` **sau khi** `009-audio-ui` đã merge (AGENTS §8: sửa/làm lại trong phạm vi phiếu gốc).
> Nguồn: dòng `[BÁO] 2026-11-24 12:21` của Claude trong phiếu 009. Người làm chọn làm ở nhánh riêng (24/11).

## Mục tiêu (nhìn từ người chơi và người nộp bài)
Người chơi: **không thấy gì khác**, trừ một điều: rời app giữa màn (cuộc gọi, kéo thanh thông báo) thì game tự Pause và không mất giờ.
Người làm: giải thích được từng file của vòng chơi. Luật giờ/gợi ý có test. Thêm xe Kenney (010), VFX (011), tutorial (012) không phải dồn tiếp vào `GameBootstrap`.

## Phạm vi
**Làm** (theo thứ tự ưu tiên; P1 bắt buộc, P2 nên, P3 nếu còn giờ):

| # | Ưu tiên | Việc | Tiêu chí |
|---|---|---|---|
| A | P1 | **Tự Pause khi vào nền + chặn bước giờ lớn.** `OnApplicationPause(true)` (và `OnApplicationFocus(false)` trên Android) → `Navigate(Pause)` nếu đang Playing. `LevelTimer.Tick` dùng tối đa `GameConfig.MaxTickSeconds` (mới, mặc định 0,25 s) mỗi lần. | Test: `Tick(10)` với giới hạn 0,25 chỉ trừ 0,25. Đang thắng/chuyển màn thì không mở Pause. |
| B | P1 | **`LevelSetup` (Game, thuần, có test):** từ metadata màn + vị trí trong danh sách + số trong `GameConfig` trả `TimeLimit`, `Hints`, `IsTutorial`. Bỏ phép tính ở [GameBootstrap.cs:109-114](../../Assets/_Project/Scripts/Presentation/GameBootstrap.cs). Nhận số thường (struct/tham số), không nhận `ScriptableObject`, để test không cần asset. | Test phủ: tutorial → không giờ, gợi ý ∞; có `timeLimit` riêng; không có → base + perMove × minMoves; `minMoves = -1` → `UnknownMinimumMoves`; có/không `hints`. Số kết quả với 31 màn **bằng hệt** trước khi sửa. |
| C | P1 | **Metadata đọc một lần, không dò chuỗi.** Thêm `LevelJsonLoader.LoadInfo(json)` (hoặc trả cùng `Load`) gồm `name`, `minMoves`, `timeLimit?`, `hints?`. Phân biệt "vắng" bằng **giá trị gác** (ví dụ `timeLimit = -1`, `hints = int.MinValue` trong lớp dữ liệu private), **không** dùng `json.Contains("\"timeLimit\"")`. | Test: field vắng, field = 0, tên màn chứa chữ `timeLimit` vẫn đọc đúng. |
| D | P1 | **Một trạng thái, một chỗ ghi.** `input.Accepting` và `timer.Paused` chỉ được gán ở **một** hàm (ví dụ `ApplyPlayGate()` gọi cuối `Update` và sau mỗi lệnh điều hướng). `RefreshHud` chỉ vẽ chữ, không đổi trạng thái. | `grep "Accepting ="` và `grep "Paused ="` trong Presentation: mỗi cái chỉ còn 1 dòng gán (trừ `OnDisable`). |
| E | P1 | **Test đọc–lưu–đọc cho Level Editor:** với 31 màn trong `LevelDesign` (hoặc golden): `Read` → `Serialize` → `Read` cho `Geometry` bằng nhau, và giữ nguyên việc có/vắng `timeLimit`/`hints`. Thêm 3–4 test `ObjectFields`: chuỗi có `{`, `,`, `\"` bên trong; object rỗng; key trùng → lỗi. Test này đặt trong asmdef **EditMode test có tham chiếu Editor** (đổi `LevelDesignIO` sang `internal` + `InternalsVisibleTo`, hoặc test asmdef Editor riêng; ghi lựa chọn vào phiếu). | Test xanh; nếu cần asmdef test mới thì **hỏi người làm trước** (đổi asmdef là việc phải hỏi). |
| F | P2 | **Nút bàn xoay ra khỏi `DebugHud`.** Tách thành lớp riêng (ví dụ `TableButtons`, tạo bằng code như hiện nay, không cần prefab mới). `DebugHud` chỉ còn là HUD gỡ lỗi, tắt được hoàn toàn. | Màn có bàn xoay chơi được khi `DebugHud` không hiện; không tạo font OS khi ẩn. |
| G | P2 | **Vùng bãi lấy từ UI, không từ `.17/.69` viết cứng.** `NavigationView` thêm `[SerializeField] RectTransform playArea`; `BoardViewport()` tính từ rect của nó. Chế độ debug (không có `NavigationView`) giữ cách cũ. | **Bước Editor của người làm:** tạo RectTransform `PlayArea` trong Canvas giữa thanh trên và thanh nút, gán vào ô mới. Codex hướng dẫn từng bước. |
| H | P2 | **`FeedbackRouter` (Presentation, class thường, không MonoBehaviour):** nhận sự kiện animator/input/navigation → gọi `SfxPlayer`/`Haptics`. Gỡ ~8 hàm `Play`/`Pulse`/handler khỏi `GameBootstrap`. | `GameBootstrap` không còn gọi thẳng `sfxPlayer`/`haptics` ngoài phần khởi tạo. |
| I | P2 | **Level Editor: một bảng loại vật thể.** Gộp `ListFields`, `KindNames`, mảng nhãn ở `DrawGrid`, chuỗi "allowed" của `CheckNestedSchema` thành **một** mảng mô tả (`tên field JSON · nhãn · nhãn ngắn · các field cho phép`). Bỏ so `kind == 1`, `kind == 9/10/11` bằng số; dùng hằng có tên. `Tints` lấy từ `VisualPalette` đã nạp. | Test E vẫn xanh; tool mở/lưu/xuất y như trước (người làm thử lại). |
| J | P3 | `NavigationView.Initialize`: thay dòng kiểm 22 ô bằng `Require(field, "tên ô")` báo **đúng ô thiếu**. `RefreshHud` gom tham số thành một struct và chỉ cập nhật chữ thắng/thua khi panel đó đang hiện. | — |
| K | P3 | Xóa `Assets/TextMesh Pro/Examples & Extras` (249 file, 3,9 MB, 35 script). **Chỉ khi người làm duyệt rõ** (xóa asset là việc phải hỏi). Xóa trong cửa sổ Project, kiểm Console không lỗi. | — |

**Không làm:** đổi luật chơi hay cảm giác (thời lượng, độ nảy, âm, rung) · đụng Core (`Scripts/Core`) · đổi tên field serialize mà thiếu `[FormerlySerializedAs]` · sửa YAML scene/prefab bằng tay · thêm package · làm việc 010.

## Tệp dự kiến
- `Game/LevelTimer.cs`, `Game/GameConfig.cs` (field `maxTickSeconds`), **mới** `Game/LevelSetup.cs`, `Game/LevelJsonLoader.cs`
- `Presentation/GameBootstrap.cs`, `Presentation/DebugHud.cs`, `Presentation/NavigationView.cs`, **mới** `Presentation/TableButtons.cs`, **mới** `Presentation/FeedbackRouter.cs`
- `Editor/LevelDesignIO.cs`, `Editor/LevelEditorWindow.cs`
- Tests: `SessionAndTimerTests.cs` (A), **mới** `LevelSetupTests.cs` (B, C), test Level Editor (E; có thể cần asmdef, hỏi trước)
- **Cần Editor của người làm:** G (tạo/gán `PlayArea`), lưu lại `GameConfig.asset` sau khi thêm field, K (nếu duyệt)

## Tiêu chí xong + bậc kiểm
- **V1:** Run All EditMode xanh (352 cũ + test mới), 0 đỏ. Số giờ/gợi ý của cả 31 màn trước và sau B giống hệt (Codex in bảng so trong phiếu).
- **V2:** Console 0 lỗi/cảnh báo mới. Play: chọn màn → chơi → Pause/Resume → thắng → Màn kế → để hết giờ → Chơi lại → về Chọn màn; một màn có bàn xoay; hoàn tác; gợi ý. Thoát/vào lại Play một lần. Level Editor: mở → sửa → Lưu → Xuất, không đổi file nào ngoài ý muốn (`git status`).
- **V3 (APK):** đang chơi thì về Home 10 s rồi quay lại → đang ở Pause, giờ không bị trừ 10 s.
- `GameBootstrap.cs` ngắn hẳn (mục tiêu ≤ ~180 dòng; không phải luật cứng, quan trọng là mỗi phần một việc).

## Điều dễ làm sai
- **Refactor mà đổi hành vi lặng lẽ.** B và D dễ làm lệch thời điểm khóa input (ví dụ lúc chờ xe cuối chạy xong trước bảng thắng). Chạy đúng kịch bản V2, đặc biệt nhịp thắng 0,5 s.
- `OnApplicationPause` cũng chạy **lúc khởi động** trên một số máy với `false`; chỉ xử lý `true`. Không Pause khi đang ở Chọn màn, Won, Lost hoặc đang chuyển màn.
- `OnApplicationFocus(false)` trong **Editor** bắn khi bấm sang cửa sổ khác: chỉ dùng trên Android (`#if UNITY_ANDROID && !UNITY_EDITOR`) hoặc chỉ dùng `OnApplicationPause`, để không làm phiền lúc test trong Editor.
- Giá trị gác ở C: `JsonUtility` để nguyên giá trị khởi tạo khi field vắng, nên gác phải nằm **trong khởi tạo field**, không phải sau khi parse.
- `GameConfig` thêm field: asset cũ cần **lưu lại trong Editor** để có giá trị (AGENTS §5, dữ liệu serialize thắng).
- Tách `TableButtons` khỏi `DebugHud`: giữ `SetAsFirstSibling()` để dialog/fade vẫn nằm trên nút bàn xoay.
- Viết theo quy ước mới nếu người làm duyệt (xem mục dưới); không format lại các file **không** nằm trong phiếu.

## Quyết định cần người làm (trước khi Codex làm phần liên quan)
1. **Quy ước code:** mỗi dòng một lệnh, `if`/`for` luôn có ngoặc `{}`. Claude khuyến nghị **có**, áp dụng cho các file trong phiếu này. Đồng ý thì ghi `[CHỐT]` vào `PROJECT.md`.
2. **K — xóa `TMP Examples & Extras`:** Claude khuyến nghị **xóa**.
3. **E — asmdef test mới** nếu cần: Claude khuyến nghị cho phép (chỉ test, không vào build).

## Thứ có thể cắt nếu trễ (theo thứ tự cắt)
K → J → I → H → G/F (để lại cho đầu 010). **Không cắt** A–E.
Time-box: 1 ngày. Vượt 2 ngày → dừng, báo (AGENTS §3.2). Hôm nay 24/11, hạn 30/11.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch và vì sao
## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
## Bạn cần hiểu
## Phản đối kế hoạch (nếu có) — viết SỚM, trước khi làm

--- Người review điền ---
## Verdict

--- Trao đổi ---
## Trao đổi
2026-11-24 12:25 · Claude → Codex, Người làm · [BÀN GIAO]: Phiếu plan. Mở nhánh 009-01-code-cleanup từ main SAU khi 009 merge. Làm A–E bắt buộc, F–I nên, J–K nếu còn giờ. Trước khi làm: đọc mục "Quyết định cần người làm" (quy ước code, xóa TMP Examples, asmdef test) và chờ trả lời phần liên quan. Viết "Phản đối kế hoạch" trước khi làm nếu thấy sai. Chưa có code nào đổi.
2026-11-24 12:28 · Người làm → Codex · [TRẢ LỜI]: Duyệt mục K: xóa Assets/TextMesh Pro/Examples & Extras (Người làm trả lời Claude trong chat: "Cho xóa"). Claude đã soát: 130 GUID trong thư mục không được tham chiếu từ Assets/_Project, ProjectSettings hay TextMesh Pro/Resources. Xóa trong cửa sổ Project của Unity (không xóa bằng lệnh), rồi kiểm Console 0 lỗi.
