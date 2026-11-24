# 009-01 — Dọn nợ thiết kế của 009     Cỡ: L (≤ 1 ngày)     Trạng thái: Chờ người làm kiểm nhanh rồi merge
Đang cầm: Người làm        AI tham gia: Codex (làm), Claude (plan, review)
Liên quan: 009     Chờ nhận: —

> Nhánh `009-01-code-cleanup`, tạo từ `main` **sau khi** `009-audio-ui` đã merge (AGENTS §8: sửa/làm lại trong phạm vi phiếu gốc).
> Nguồn: dòng `[BÁO] 2026-11-24 12:21` của Claude trong phiếu 009. Người làm chọn làm ở nhánh riêng (24/11).

## Mục tiêu (nhìn từ người chơi và người nộp bài)
Người chơi: **không thấy gì khác**, trừ một điều: rời app giữa màn (cuộc gọi, kéo thanh thông báo) thì game tự Pause và không mất giờ.
Người làm: giải thích được từng file của vòng chơi. Luật giờ/gợi ý có test. Thêm xe Kenney (010), VFX (011), tutorial (012) không phải dồn tiếp vào `GameBootstrap`.

## Phạm vi
**Cập nhật đã chốt trong chat 24/11:** người làm yêu cầu bỏ bước Editor vì sát hạn. Hoãn **G**; Codex đã gỡ toàn bộ code/test G vừa viết, trả `NavigationView` về bản cũ và giữ vùng camera cũ. Không cần tạo/gán `PlayArea`. F/H/I giữ lại. J/K chưa thực hiện.

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
- A–D [đã kiểm trong code]: thêm `GameConfig.maxTickSeconds` mặc định 0,25 s và kiểm giá trị trong `OnValidate`; `LevelTimer` chặn bước giờ lớn, phạt Undo/Hint không bị chặn theo tick. Callback Pause chỉ xử lý khi đang Playing, chưa thắng/hết giờ và không chuyển màn; focus chỉ dành cho Android player. `LevelSetup` tính giờ/gợi ý/tutorial từ dữ liệu thường; `LevelJsonLoader.Load(..., out LevelInfo)` chỉ parse một lần và dùng giá trị gác cho field tùy chọn. `RefreshHud` chỉ hiển thị; một dòng gán `Accepting` và một dòng gán `timer.Paused` trong `ApplyPlayGate` (ngoại lệ `OnDisable` khóa input).
- E [đã kiểm trong code]: thêm `LevelDesignIOTests` kiểm đọc–lưu–đọc toàn bộ 31 golden + 34 màn thiết kế hiện tại, có/vắng từng override và override bằng 0; thêm 4 test `ObjectFields`. Thêm `LevelSetupTests` kiểm luật setup, metadata và so toàn bộ 34 màn runtime theo config asset thật. Không ghi JSON/asset khi chạy các test này.
- Đổi asmdef đúng phương án đã duyệt: Editor → Core, Game, Presentation, uGUI; test hiện có → Editor; friend assembly chỉ cấp cho test. Không thêm package. Code trong các file đã sửa áp dụng quy ước người làm vừa duyệt, vì vậy diff có cả tách dòng/thêm ngoặc.
- Kiểm tra ngoài Editor: dùng Roslyn C# 9 và DLL Unity cài sẵn, biên dịch bản cuối (sau hoãn G) Game/Presentation/Editor/test thành công; nhánh `UNITY_ANDROID` của Presentation cũng biên dịch thành công. Có 37 cảnh báo CS0649 của các field Inspector có sẵn trong Presentation (Unity vốn bỏ qua mã cảnh báo này trong rsp hiện tại); Game/Editor/test không cảnh báo. Lượt đầu kiểm test thiếu facade `mscorlib` ở cấu hình công cụ kiểm ngoài Editor, đã sửa đường dẫn shim rồi biên dịch lại qua; không phải lỗi source Unity. Công cụ tạm chỉ ở `Logs/009-01-tools`, không vào Git.
- **V1 mốc A–E: 504 passed / 0 failed / 1 skipped**, từ ảnh Test Runner EditMode người làm gửi 24/11 13:13. Một test Explicit là bài đo Profiler có sẵn ở `SolverPerformanceTests`, không thêm/bỏ qua test để đạt kết quả. Chỉ sau kết quả này mới bắt đầu F–I. Các test JsonUtility/đọc–lưu–đọc và đối chiếu toàn bộ 34 màn đều nằm trong lượt xanh này.
- F [đã kiểm trong code]: `TableButtons` là class thường được Bootstrap sở hữu, tạo nút như cũ và giữ `SetAsFirstSibling`; xử lý chọn xe trên bàn, khóa quyền chơi, cập nhật vị trí và hủy root khi đổi màn/hủy Bootstrap. `DebugHud` chỉ còn debug; không tạo object/font OS khi `Initialize(..., false)` hoặc component bị tắt. Nhãn ASCII XOAY dùng font có sẵn của Unity, không thêm asset ngoài. Có thêm một test tình huống bàn xoay với HUD ẩn/khóa input; **test mới này chưa chạy trong Unity**. Test asmdef thêm uGUI để kiểm button trực tiếp, nằm trong thay đổi tham chiếu test đã được duyệt.
- H [đã kiểm trong code]: `FeedbackRouter` nhận event input/animator/UI, xử lý âm/rung/công tắc cài đặt; đăng ký/hủy theo `OnEnable`/`OnDisable`. Bootstrap chỉ truyền `sfxPlayer`/`haptics` lúc khởi tạo router; không gọi trực tiếp hai component này nữa. Không đổi cue, âm lượng, thời lượng rung hay nhịp thắng.
- I [đã kiểm trong code]: `DesignKinds.All` là bảng tên JSON/nhãn/nhãn ngắn/field cho phép, dùng chung cho chọn loại, lưới, geometry và kiểm schema. Giữ thứ tự loại, thay số loại bằng hằng có tên; màu lưới đọc từ `VisualPalette` đã nạp. Các test E giữ nguyên assertion nhưng **chưa chạy lại sau I**.
- G đã viết và hướng dẫn người làm nhưng sau đó được yêu cầu bỏ bước vì sát hạn. Đã gỡ code/test G và hoàn nguyên riêng thay đổi của Codex trong `NavigationView.cs`; giữ vùng `.17/.69` cũ trong chế độ hiện tại. Không đổi field serialize hay scene/prefab để thực hiện G.
- Chưa sửa scene/prefab/JSON/config asset dữ liệu. `.meta` A–E đã được Unity tạo; hai script mới `TableButtons`/`FeedbackRouter` còn chờ Unity import/tạo `.meta` khi người làm quay lại Editor. `GameConfig.asset` trên đĩa vẫn chưa ghi `maxTickSeconds`; đã hướng dẫn lưu 0.25 trong Editor nhưng chưa xác nhận thao tác này. **V2/Console và V3 chưa xác nhận; bản sau F/H/I chưa Run All.** `git diff --check` qua; status chỉ có file trong phạm vi, không stage/commit.
- Mục tiêu ~180 dòng của Bootstrap chưa đạt: sau tách router/nút, file vẫn hơn 500 dòng theo quy ước có ngoặc và mỗi dòng một lệnh. Phần còn dài là Update/Transition và điều phối phiên chơi; không tách thêm hệ thống ngoài phạm vi để chạy theo số dòng.
### Bảng giờ/gợi ý trước → sau B
Nguồn: JSON runtime hiện tại + GameConfig.asset. Bảng được lập bằng đối chiếu dữ liệu và **đã được các test `EveryRuntimeLevelKeepsItsPreviousTimeAndHints` xác nhận trong Run All A–E 504/0/1** bằng JsonUtility thật. Kho hiện tại có 34 file; bảng bao gồm toàn bộ thứ tự đang xuất vào game, kể cả các bản sao ngoài 31 mẫu gốc. I không đổi dữ liệu hay công thức setup; lượt sau I chưa chạy lại.

| Vị trí | Màn | minMoves | Giờ cũ → mới (s) | Gợi ý cũ → mới |
|---|---|---|---|---|
| 1 | 01 | 2 | 30 → 30 | 3 → 3 |
| 2 | 02 | 3 | 35 → 35 | 3 → 3 |
| 3 | 03 | 4 | 40 → 40 | 3 → 3 |
| 4 | 04 | 5 | 45 → 45 | 3 → 3 |
| 5 | 05 | 4 | 40 → 40 | 3 → 3 |
| 6 | 06 | 5 | 45 → 45 | 3 → 3 |
| 7 | 07 | 4 | 40 → 40 | 3 → 3 |
| 8 | 08 | 5 | 45 → 45 | 3 → 3 |
| 9 | 09 | 4 | 40 → 40 | 3 → 3 |
| 10 | 10 | 4 | 40 → 40 | 3 → 3 |
| 11 | 11 | 6 | 50 → 50 | 3 → 3 |
| 12 | 12 | 7 | 55 → 55 | 3 → 3 |
| 13 | 13 | 5 | 45 → 45 | 3 → 3 |
| 14 | 14 | 6 | 50 → 50 | 3 → 3 |
| 15 | 15 | 6 | 50 → 50 | 3 → 3 |
| 16 | 16 | 9 | 65 → 65 | 3 → 3 |
| 17 | 17 | -1 | 120 → 120 | 3 → 3 |
| 18 | 18 | -1 | 120 → 120 | 3 → 3 |
| 19 | 19 | -1 | 120 → 120 | 3 → 3 |
| 20 | 20 | 12 | 80 → 80 | 3 → 3 |
| 21 | 21 | 6 | 50 → 50 | 3 → 3 |
| 22 | 22 | 6 | 50 → 50 | 3 → 3 |
| 23 | 23 | 9 | 65 → 65 | 3 → 3 |
| 24 | 24 | 13 | 85 → 85 | 3 → 3 |
| 25 | 25 | 13 | 85 → 85 | 3 → 3 |
| 26 | 26 | 9 | 65 → 65 | 3 → 3 |
| 27 | 27 | 18 | 110 → 110 | 3 → 3 |
| 28 | 28 | 17 | 105 → 105 | 3 → 3 |
| 29 | 29 | 4 | 40 → 40 | 3 → 3 |
| 30 | 30 | 18 | 110 → 110 | 3 → 3 |
| 31 | 31 | 17 | 105 → 105 | 3 → 3 |
| 32 | 32 | 2 | 30 → 30 | 3 → 3 |
| 33 | 33 | 2 | 30 → 30 | 3 → 3 |
| 34 | 34 | 2 | 30 → 30 | 3 → 3 |

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- Giá trị mới duy nhất: `GameConfig.maxTickSeconds` · chưa có → 0,25 s · tránh trừ một bước thời gian lớn. Các số giờ/gợi ý/feel hiện có giữ nguyên. Chờ người làm lưu field vào asset bằng Editor; không sửa YAML.
- `PROJECT.md` đã cập nhật §2.2 (quy ước đã duyệt), §8 (assembly/loader/setup/timer/router/nút bàn xoay và hoãn G), §13 (main đã có 009 và trạng thái nhánh này). V1 chỉ xác nhận mốc A–E; chưa tuyên bố bản cuối qua V1/V2/V3.
- G hoãn theo người làm; J/K chưa làm. K đã được duyệt nhưng phải xóa trong cửa sổ Project; không xóa bằng lệnh. V3 Android vẫn do người làm build/chơi thử.
- Tự tin khoảng 85% về phần code/phạm vi. Cần review kỹ lifecycle router/nút bàn xoay và I giữ schema. Chưa đủ bằng chứng để tự APPROVE: thiếu Run All sau F/H/I, Console/Play, import hai `.meta` và lưu field config mới. Không dùng việc sát hạn để ghi các kiểm tra chưa chạy thành đã qua.
## Bạn cần hiểu
- `Tick` nhận thời gian của **một frame**, không còn là cách đi tắt nhiều giây; phạt Undo/Hint là hành động riêng nên vẫn trừ đủ số giây cấu hình.
- Field JSON vắng khác field bằng 0: 0 giây là màn không tính giờ, 0 gợi ý là không được gợi ý. Giá trị gác trong DTO được đổi thành nullable ở `LevelInfo`; test phải chạy bằng JsonUtility thật.
- `ApplyPlayGate` là nơi duy nhất tính trạng thái khóa input/đồng hồ. HUD chỉ đọc trạng thái để vẽ, tránh việc cập nhật chữ lại làm đổi quyền chơi.
- Test asmdef không tham chiếu trực tiếp được `Assembly-CSharp-Editor`. Asmdef Editor mới giúp test gọi trực tiếp code `internal`; giới hạn Editor bảo đảm công cụ không đi vào APK.
- Nút bàn xoay là UI chơi thật nên sống độc lập với HUD debug. Router chỉ chuyển sự kiện thành âm/rung; nó không quyết định luật chơi. `Connect`/`Disconnect` đi cặp để tránh phát âm nhiều lần sau khi bật/tắt object.
## Phản đối kế hoạch (nếu có) — viết SỚM, trước khi làm
- [đã kiểm trong code] E: `LevelDesignIO` đã `internal`, thuộc `Assembly-CSharp-Editor`. Test asmdef không thể tham chiếu trực tiếp assembly mặc định. Chọn tạo `SqueakyLane.Editor` (Editor only) → Core, Game, Presentation, uGUI; test hiện tại → Editor; cấp `InternalsVisibleTo("SqueakyLane.Tests.EditMode")`. Không có cạnh runtime → Editor. Người làm đã duyệt thay đổi asmdef trong chat trước khi thực hiện.
- [đã kiểm trong code] A đổi hợp đồng `Tick`: các test cũ dùng một `Tick(20..200)` để mô phỏng nhiều giây sẽ không còn đúng. Giữ nguyên kỳ vọng pause/hết giờ/sao, chuyển mô phỏng sang nhiều tick nhỏ; thêm test riêng cho tick lớn và giới hạn tùy chỉnh. Không nới hoặc bỏ assertion cũ.
- [rủi ro] C: cần kiểm chứng giá trị gác bằng `JsonUtility` thật trong Run All; kiểm tĩnh không đủ kết luận hành vi field vắng. Chọn overload `Load(..., out LevelInfo)` để gameplay chỉ parse JSON một lần; `LoadInfo` dùng chung DTO.

--- Người review điền ---
## Verdict
**APPROVE CÓ ĐIỀU KIỆN** (Claude, 2026-11-24). Người làm yêu cầu rút gọn để sang làm game; điều kiện dưới đây là mức tối thiểu, không bỏ thêm.

**Điều kiện trước khi commit/merge (khoảng 15 phút):**
1. Mở Unity để import `TableButtons.cs`/`FeedbackRouter.cs` (tạo `.meta`). Commit **kèm** hai `.meta` này.
2. Console 0 lỗi đỏ sau khi biên dịch; **Run All EditMode một lần** (bản sau F/H/I chưa chạy).
3. Play nhanh một vòng: Màn 08 bấm nút XOAY được · Pause/Resume · thắng một màn → bảng thắng hiện sau ~0,5 s. Hỏng gì thì dừng, chưa merge.

**Đã soi:** `LevelSetup` (thứ tự `??`/`?:` đúng; tutorial → 0 giây, gợi ý −1) · `LevelTimer` chặn tick, phạt Undo/Hint không bị chặn · `GameBootstrap` toàn bộ: `ApplyPlayGate` là chỗ duy nhất gán `Accepting`/`timer.Paused` (ngoài `OnDisable`), `RefreshHud` chỉ vẽ, `OnApplicationPause` chỉ Pause khi đang Playing chưa thắng/thua, `OnApplicationFocus` chỉ trên Android player · `TableButtons` (hủy root bằng kiểm `!= null`, vẫn `SetAsFirstSibling`, listener kiểm `accepting`) · `FeedbackRouter` (Connect/Disconnect đi cặp, Connect tự Disconnect trước) · `DebugHud` ẩn không tạo font, `RefreshSafeArea` an toàn khi `safe == null` · asmdef Editor/test (Unity 2022.3 có `LegacyRuntime.ttf`; không script Editor nào dùng TMPro).
**Chưa soi kỹ:** diff `LevelEditorWindow.cs` (+873/−166 khi bỏ khoảng trắng, phần lớn là đổi định dạng theo quy ước) và phần `DesignKinds` ngoài bảng khai báo; E chỉ phủ đọc/ghi, không phủ UI cửa sổ. Người làm nên mở tool, mở một màn, Lưu, Xuất một lần trước khi dùng nhiều ở 012.

- [NÊN] `GameConfig.asset` chưa có dòng `maxTickSeconds`. Không chặn: field vắng trong YAML sẽ lấy mặc định 0,25 của C#. Khi tiện, mở asset trong Inspector và Ctrl+S để giá trị nằm trong file.
- [NÊN] Mép biên chưa xử lý: nếu app vào nền **giữa lúc đang fade chuyển màn** thì `Navigate` bị bỏ qua (đang `transitioning`). Quay lại sẽ chơi tiếp, mất tối đa 0,25 s nhờ chặn tick. Chấp nhận được cho hạn 30/11.
- [GỢI Ý] `GameBootstrap` vẫn 575 dòng, chủ yếu do quy ước một lệnh một dòng; số việc nó làm đã giảm (âm/rung, nút bàn xoay, luật setup đã ra ngoài). Không tách thêm trước 30/11.
- G hoãn và J/K chưa làm: chấp nhận. K (xóa TMP Examples) làm lúc nào cũng được, 1 phút trong cửa sổ Project.

--- Trao đổi ---
## Trao đổi
2026-11-24 12:25 · Claude → Codex, Người làm · [BÀN GIAO]: Phiếu plan. Mở nhánh 009-01-code-cleanup từ main SAU khi 009 merge. Làm A–E bắt buộc, F–I nên, J–K nếu còn giờ. Trước khi làm: đọc mục "Quyết định cần người làm" (quy ước code, xóa TMP Examples, asmdef test) và chờ trả lời phần liên quan. Viết "Phản đối kế hoạch" trước khi làm nếu thấy sai. Chưa có code nào đổi.
2026-11-24 12:28 · Người làm → Codex · [TRẢ LỜI]: Duyệt mục K: xóa Assets/TextMesh Pro/Examples & Extras (Người làm trả lời Claude trong chat: "Cho xóa"). Claude đã soát: 130 GUID trong thư mục không được tham chiếu từ Assets/_Project, ProjectSettings hay TextMesh Pro/Resources. Xóa trong cửa sổ Project của Unity (không xóa bằng lệnh), rồi kiểm Console 0 lỗi.
2026-11-24 12:32 · Codex → Người làm, Claude · [BÁO]: Đã fetch origin; main khớp origin/main tại fabb2be, cũng là tip 009-audio-ui. Đã mở nhánh 009-01-code-cleanup từ main bằng --no-track; chưa stage/commit. Người làm duyệt quy ước mỗi dòng một lệnh, if/for có ngoặc và asmdef Editor + tham chiếu test trong chat. Làm A–E rồi chờ Run All xanh trước F–I; thao tác Editor do người làm theo PROJECT §2.5.
2026-11-24 12:39 · Codex → Người làm, Claude · [BÁO]: A–E đã có code/test, biên dịch C# ngoài Editor qua cả Editor/test và nhánh Android; chưa Run All/Console/Play. Đã gửi người làm các bước lưu Max Tick Seconds = 0.25 và chạy EditMode Run All. Chờ kết quả xanh rồi mới F–I; nhánh 009-01-code-cleanup còn toàn bộ thay đổi chưa commit. Bảng 34 màn trong phiếu hiện là đối chiếu tĩnh; test sẽ kiểm lại bằng JsonUtility thật.
2026-11-24 13:13 · Người làm → Codex · [BÁO]: Gửi ảnh Test Runner EditMode: 504 passed, 0 failed, 1 skipped. Codex bắt đầu F–I sau mốc xanh này; ảnh không xác nhận riêng Console hoặc Play.
2026-11-24 13:24 · Codex → Người làm, Claude · [BÁO]: Người làm nói bỏ bước Editor vì gần hạn. Hiểu là hoãn G; đã thông báo và gỡ code/test G vừa viết, giữ camera cũ và NavigationView cũ để scene không bị chặn do thiếu PlayArea. F/H/I giữ lại; phần kiểm Unity sau các thay đổi này chưa chạy.
2026-11-24 13:25 · Codex → Claude, Người làm · [BÀN GIAO]: A–F/H–I đã viết; G hoãn, J/K chưa làm. Nhánh 009-01-code-cleanup, HEAD fabb2be; toàn bộ thay đổi chưa stage/commit. A–E Run All EditMode 504/0/1 theo ảnh; bản cuối biên dịch C# ngoài Editor qua Game/Presentation/Editor/test và nhánh Android; diff check qua. Còn: Unity import hai script mới để có .meta, lưu Max Tick Seconds = 0.25 vào asset, Run All sau F/H/I và V2/V3/Console. Đang cầm chuyển Claude để review code; chưa có verdict, không tự APPROVE. Màn 08 có bàn xoay để kiểm F khi người làm chơi thử.
2026-11-24 13:32 · Claude → Người làm, Codex · [BÀN GIAO]: Review 009-01 xong: APPROVE CÓ ĐIỀU KIỆN (3 điều kiện ở Verdict: mở Unity tạo .meta cho TableButtons/FeedbackRouter, Console sạch + Run All, Play nhanh màn 08/Pause/thắng). Người làm cầm: kiểm, commit, merge. Codex: sau merge sang 010; quy trình rút gọn theo yêu cầu người làm.
