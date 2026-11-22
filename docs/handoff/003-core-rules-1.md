# 003 — Core luật phần 1     Cỡ: L (≤ 1 ngày)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Codex, Claude
Liên quan: 001; core-plan.md §1–2     Chờ nhận: —

## Mục tiêu
Luật xe chạy, rửa, hiệu ứng ô, đẩy và xoay hoạt động bằng C# thuần có test, để phần hình ảnh sau này dùng chung kết quả với solver và hoàn tác.

## Phạm vi — Làm / Không làm
- Làm: model đầy đủ (bao gồm dữ liệu portal/trụ/barie/chốt cho 004), LevelDefBuilder, GameState.Initial; Occupancy, MoveResolver, GateRule, TileEffects, Splash, PushRule, LightRule, TableRule và IsWin.
- Không làm: luật portal/trụ/barie/chốt (004), JSON/golden/solver (005–007), Game/session/config (008), MonoBehaviour, scene/prefab, asset ngoài, package/settings/asmdef, commit/push/merge.
- Người làm gửi prompt việc 003 = duyệt kế hoạch trong core-plan.md §2. Mỗi việc phải review/merge trước khi bắt đầu việc sau.

## Các bước
1. Git sạch, fetch origin; tạo `003-core-rules-1` từ main mới nhất bằng `--no-track`; lập phiếu và sổ việc.
2. Đối chiếu `docs/reference/prototype/src/logic.js`; tạo kiểu bất biến, builder và state; không xóa marker 001.
3. Viết từng luật phần 1; giữ thứ tự sự kiện, hiệu ứng và ảnh chụp occupancy của đầu lệnh; không LINQ/chuỗi trong đường nóng, không static có thể đổi.
4. Thêm test EditMode bằng màn nhỏ dựng qua builder; hai chiều/hai trục, các trường hợp biên, chuỗi đẩy và bất biến.
5. Unity import sinh meta, biên dịch; chạy toàn bộ EditMode; đọc Console/log và kiểm diff.
6. Cập nhật PROJECT.md §8/§13, điền phiếu, chuyển gậy Claude và dừng review. Không bắt đầu 004.

## Tệp dự kiến
- `Assets/_Project/Scripts/Core/Model/*.cs`: enum Axis/Side/GateEdge/TileColor; Cell, CarDef/CarState, GateDef, PaintDef, LightDef, BarrierDef, SwitchDef, PortalFace/PortalDef/PylonDef; LevelDef, LevelDefBuilder, GameState, MoveEvent/MoveEventKind/StopReason/PushedCarEvent, MoveResult.
- Điều chỉnh người làm duyệt 22/11: gộp Model thành 8 file, không một file cho mọi kiểu nhỏ: Enums, LevelObjects, CarDef (kèm CarState), LevelDef (kèm ReadOnlyData), LevelDefBuilder, GameState, MoveEvent (kèm PushedCarEvent), MoveResult. Giữ tên kiểu/namespace/giá trị enum và nội dung luật.
- Cách đọc: dữ liệu nhỏ ở Enums/LevelObjects; dữ liệu xe ở CarDef; luồng xử lý lệnh ở Rules/MoveResolver. Chia theo chức năng để người làm giải thích được code.
- `Assets/_Project/Scripts/Core/Rules/*.cs`: Occupancy, MoveResolver, GateRule, TileEffects, PushRule, LightRule, TableRule, GameRules.
- `Assets/_Project/Tests/EditMode/*Tests.cs`: test model và từng nhóm luật phần 1.
- [Editor import] Unity tự sinh `.meta` cho script/thư mục mới; không tạo/sửa meta bằng tay.
- `PROJECT.md` §8/§13; phiếu này. Không sửa `core-plan.md` hay prototype.

## Tiêu chí xong — V1 + V2
- Test: cổng đúng/sai màu, bùn chặn; nước làm sạch/dội tối đa 1 lần; vũng bùn dùng 1 lần; vòi xịt; sơn; splash khi lao tới và không khi sát sẵn; cọc; nhịp đèn; bàn xoay được/bị chặn; tải đẩy được/bị tường/chuỗi 2 xe/xe vuông góc/xe bị đẩy nhận hiệu ứng; state cũ không đổi; no-op giữ cùng tham chiếu. Mỗi nhóm ≥1 test và bao phủ chiều ngược.
- Core không UnityEngine, không LINQ/chuỗi trong Move/Occupancy; không static có thể đổi; enum giữ thứ tự core-plan.
- Unity biên dịch sạch, Console không lỗi/cảnh báo mới, toàn bộ EditMode qua; báo số test/tên đỏ/dòng Console nguyên văn.
- File chỉ thuộc phạm vi, đủ meta. Golden trace chưa áp dụng vì việc 005 chưa làm; không tự tuyên bố khớp 31 màn.

## Điều cấm / rủi ro / thứ có thể cắt nếu trễ
- Không sửa prototype hoặc luật tự ý. Số 80 vòng bảo vệ Move giữ theo prototype bằng hằng có tên, không phải tuning cảm giác.
- Rủi ro trọng yếu: dữ liệu bị sửa từ ngoài; thứ tự sơn→bùn→xịt; splash khi tải đẩy khác đâm thường; đèn dựa Moves, occupancy lấy đầu lệnh, push kiểm trạng thái hiện tại; xe tải Len=3; Table không áp hiệu ứng ô khi xoay (theo prototype).
- Giảm rủi ro bằng struct/collection chỉ đọc và sao chép; test số liệu/sự kiện và state cũ. Các mảng tra cứu dựng một lần theo LevelDef; không nhớ đệm static.
- Không cắt test; nếu gấp có thể gom GateRule/TileEffects vào MoveResolver như core-plan cho phép (ghi lại). Time-box ≤1 ngày; trễ ≥nửa ngày báo người làm; quá gấp đôi dừng.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch và vì sao
- Đã viết model đầy đủ theo hợp đồng và các luật phần 1: Occupancy, MoveResolver, GateRule, TileEffects, PushRule, LightRule, TableRule, GameRules; chưa có luật 004.
- Đã bổ sung 5 file test: MovementRuleTests, TileAndSplashTests, PushRuleTests, TableAndModelTests, RuleTestSupport. Giữ CoreAssemblyTests của 001.
- Theo yêu cầu người làm, gộp Model **24 → 8 file**. So sánh trực tiếp: 21 phần nội dung nguồn được chuyển nguyên văn, 3 file còn lại giữ nguyên; không đổi logic trong lượt gộp. Kiểm GUID các script Model không có tham chiếu từ asset khác. Giữ meta/GUID đã có ở file đích (Axis → Enums, Cell → LevelObjects); bỏ script dư cùng meta sau khi gộp. Thực hiện bằng thao tác file, không điều khiển Editor, không tự tạo hay sửa nội dung GUID.
- GameState không tạo ReadOnlyCollection theo từng state; truy cập qua CarAt/CarCount/MudAt/PylonTurnsAt. MoveEvent là readonly struct; Move/Rotate có recordEvents=false để solver không tạo event. Chưa đo GC/hiệu năng trên máy.
- **Đã kiểm bằng CLI:** dùng Roslyn và reference Mono 4.8 đi kèm Unity 2022.3.62f2 để biên dịch Core/test; không thêm package hoặc project phụ vào repo. Core chỉ tham chiếu thư viện .NET. Runner tạm ngoài repo dùng NUnit có sẵn trong PackageCache, chạy bằng Mono của Unity.
- Kết quả nguyên văn: `Core và test: compile OK, không có cảnh báo compiler.`; `NUnit (Mono CLI): Passed=74, Failed=0, Skipped=0, Inconclusive=0`. Không có test đỏ, không skip.
- **Đã kiểm trong Unity Editor (20:06, sau khi người làm cho phép điều khiển trở lại):** đúng Unity 2022.3.62f2, Refresh script bằng Ctrl+R, chờ Editor sẵn sàng. Core và Tests đã được Unity biên dịch (DLL trong Library/ScriptAssemblies sau lần gom file; log có `*** Tundra build success (1.49 seconds), 10 items updated, 358 evaluated`; không có error CS/warning CS).
- **Unity Test Runner → EditMode → Run All:** tổng **74**, số xanh nguyên văn **`74`**, đỏ **`0`**, chưa chạy **`0`**. Không có tên test đỏ. Đây là lần chạy trong Editor, tách biệt kết quả NUnit CLI V1 ở trên.
- **Console trước và sau Run All:** danh sách trống; bộ đếm Log **`0`**, Warning **`0`**, Error **`0`**. Không có lỗi/cảnh báo mới, không có dòng lỗi/cảnh báo để chép. Không bấm Clear Console. Chưa chạy Play/Android vì phạm vi 003 chỉ là Core thuần và test EditMode.
- **Kiểm phạm vi sau test:** `git status --porcelain --untracked-files=all` có 46 file (kể cả meta), tất cả thuộc phiếu; mỗi asset mới có meta, không meta mồ côi, không trùng GUID trong _Project; Model 8 file/Rules 8 file khớp cây tài liệu; Core không `using UnityEngine`/`System.Linq`, asmdef vẫn noEngineReferences=true; `git diff --check` qua. Scene/prefab/package/settings/asmdef không đổi.
- PROJECT.md §8 đã bỏ đoạn trạng thái tạm và liệt kê đúng các file Model/Rules. Giữ nguyên §2.2 mục 5 và dòng 003 ở §13 theo review của người làm. Đã sửa xong các yêu cầu review sớm, chuyển gậy Claude; chưa tự APPROVE, chưa commit/push/merge, không bắt đầu 004.

## Đã đối chiếu logic.js
Đã đọc lại trực tiếp `docs/reference/prototype/src/logic.js` và từng file C# dưới `Assets/_Project/Scripts/Core/Rules/`; đây là đối chiếu tĩnh, không thay thế golden trace ở 005–006.

| Hàm/nhánh prototype | C# đã port và nhánh đã so |
|---|---|
| `move` (dòng 177–305) | `MoveResolver.Move` + `GateRule`: đầu xe theo trục/chiều; occupancy chụp ở đầu lệnh; cổng đúng màu + sạch thì Exit; còn bùn thì Muddy; sai màu thì WrongGate; thiếu cổng hoặc cổng nước đã dội thì Edge. Nước đặt bùn=0, đổi dấu dir, dội tối đa 1 lần. Stop Wall/Car, Step trước hiệu ứng, tải đẩy xong tiến 1 ô rồi Stop Push; moved = steps>0 hoặc bounced hoặc exited; chỉ tăng Moves khi moved, no-op giữ state cũ; guard 80. Chưa port portalTry/nhánh trụ (004). |
| `tryPush` (117–134) + áp đẩy trong `move` (243–265) | `PushRule.TryChain/Apply`: occupancy của state hiện tại; duyệt toàn thân từng xe theo thứ tự chuỗi, kể cả vuông góc; chặn mép/vật cản/xe tải nguồn; mỗi xe dịch 1 ô; chỉ ô mới phủ nhận hiệu ứng, giữ thứ tự xe/ô và Push chứa sự kiện con. Các loại vật cản 004 chưa có trong Occupancy. |
| Hiệu ứng ô trong `move` (253–261, 291–296) | `TileEffects.Apply` dùng chung khi chạy và bị đẩy: **sơn → bùn → vòi xịt**; chỉ đổi sơn nếu khác màu; vũng bùn tiêu thụ 1 lần, mud=max(mud,1); chỉ phát Spray khi có bùn. Hiệu ứng sau portal chưa port (004). |
| Splash trong `move` (266–278) | `MoveResolver.Splash`: bật Splash, nguồn còn bùn và đích sạch. **Đẩy thành công:** xét sau hiệu ứng xe bị đẩy, không cần steps>0. **Đâm/đẩy thất bại:** cần steps>0; đứng sát sẵn không văng. Giữ thứ tự Push → Splash → Step của tải. |
| `tableUnder`/`rotate` (307–328) | `TableRule`: xe sống dài 2, nằm trọn bàn 2×2; xoay (dr,dc)→(dc,1−dr), chiều kim đồng hồ; occupancy bỏ xe đang xoay; bị cản giữ state cũ; thành công đổi trục, tăng Moves, 1 Rotate event; không áp hiệu ứng ô. Trường hợp không có lệnh hợp lệ được trả MoveResult.Unchanged thay cho null theo hợp đồng core-plan §1. |
| `lightRed` (76) | `LightRule.IsRed`: ((Moves+Phase)&1)==1 tương đương %2==1 với Moves không âm và Phase 0/1 đã validate; no-op không đổi nhịp. |
| `occupancy` (95–108) | `Occupancy.Build`: -1 trống/-2 vật cản/≥0 chỉ số xe; cọc và đèn đỏ ghi trước xe; bỏ xe chết/skipCar; bao phủ Len ô theo trục. Barie/chốt/trụ để 004, không tuyên bố đã port đủ hàm gốc. Prototype bỏ qua tọa độ vật cản ngoài bãi; C# validate cọc/đèn trong LevelDef, không nhận dữ liệu sai đó. |
| `compile`, `initState`, `cloneState`, `washedCount`, `isWin` | `LevelDef`, `GameState.Initial/Copy`, `GameRules`: dựng lookup một lần; copy state đầu vào, giữ mud/pyl/Moves; đếm xe không alive và thắng khi không còn xe sống. C# dùng enum/struct và dữ liệu bất biến theo core-plan, không giữ cách biểu diễn chuỗi của JS. |

- Chưa so golden trace/minMoves của 31 màn, chưa kiểm tương tác với cơ chế 004; không khẳng định khớp toàn bộ prototype.

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- 003 chỉ chạy đầy đủ các màn không dùng luật 004. Model vẫn giữ đủ dữ liệu để 004 nối vào.
- V1 dùng test màn nhỏ theo logic.js; golden so từng bước và minMoves là các việc sau, không làm trước.
- `MoveEvent` là struct **15 trường** (auto-property), nhiều Nullable và có `IReadOnlyList<PushedCarEvent> PushedCars`: sao chép tốn hơn mong đợi. Chấp nhận hiện tại vì solver ở 007 sẽ gọi `Move(..., recordEvents: false)`; solver chưa được viết nên chưa đo/kiểm đường gọi thực tế. Cần đo lại ở 007, chưa có số byte hay thời gian đo.
- `GameState.Copy()` clone **3 mảng** và tạo 1 GameState: khoảng **4 lần cấp phát mỗi trạng thái**. Đây là đếm theo code, chưa đo; không phải tổng cấp phát của cả Move (còn occupancy, MoveResult, và chuỗi đẩy/event nếu có). Việc 007 cần đo GC/thời gian trước khi quyết định tối ưu tiếp.
- Đã xác nhận import/biên dịch, Console và 74 EditMode test trong Editor sau khi gom file. Chưa Play, chưa kiểm Android, chưa đo hiệu năng/GC, chưa golden trace; không dùng V1 để thay V2. Tự tin khoảng 90% cho phạm vi 003 dựa trên đối chiếu tĩnh + V1 + kiểm Editor; cần Claude review thuật toán và hợp đồng sự kiện, chưa tự APPROVE.
- Phát hiện ngoài phạm vi: Editor.log có lặp dòng `the GUIStateObj is deleted, but is accessed` trước lần Run All; Console vẫn trống/0 cảnh báo/0 lỗi. Chưa điều tra nguyên nhân nội bộ Editor, không sửa ngoài phiếu.

## Bạn cần hiểu
- State cũ được giữ nguyên thì Undo chỉ cần giữ và khôi phục tham chiếu cũ.
- Moves chỉ tăng khi lệnh có hiệu lực; vì vậy chạm xe bị chặn không đổi nhịp đèn.
- Sự kiện có thứ tự là dữ liệu cho animation; hiển thị không tính lại luật.
- Xe tải có thể đẩy ngang xe vuông góc; hiệu ứng chỉ áp trên các ô mới phủ, theo thứ tự prototype.

## Phản đối kế hoạch (nếu có)
- Chưa có phản đối chặn việc. Rủi ro đã nêu trong kế hoạch được xử lý bằng thiết kế bất biến và test; chưa phát hiện mâu thuẫn cần đổi luật/phạm vi.

--- Người review điền ---
## Verdict
Chưa review. Codex không tự APPROVE code mình viết.

## Trao đổi
2026-11-22 15:21 · Codex → Người làm · [BÁO]: Đã đọc PROJECT/AGENTS/CODEX/core-plan theo thứ tự; Git sạch, main và origin/main cùng 1d05dc2 sau fetch. Mở nhánh 003-core-rules-1 từ main, nhận gậy Codex; chỉ triển khai 003, không bắt đầu 004.
2026-11-22 16:24 · Claude → Codex · [HỎI]: Review sớm (chưa phải verdict) khi 003 mới có Model. (1) Gộp bớt file: 6 enum (Axis, GateEdge, MoveEventKind, Side, StopReason, TileColor) vào một file Enums.cs; các struct/ định nghĩa nhỏ (Cell, GateDef, PaintDef, LightDef, BarrierDef, SwitchDef, PortalDef, PortalFace, PylonDef, PushedCarEvent) vào một hoặc hai file; mỗi luật vẫn một class, nhưng không cần một file cho mỗi kiểu 8–15 dòng. (2) GameState: mỗi state đang tốn khoảng 7 lần cấp phát (3 mảng + 3 ReadOnlyCollection); solver sẽ tạo hàng trăm nghìn state. Giữ mảng nội bộ, bỏ ReadOnlyCollection/IReadOnlyList ở đường nóng, đưa truy cập kiểu CarAt(i), CarCount. (3) MoveEvent là class 16 thuộc tính, một lần cấp phát mỗi sự kiện: đổi sang readonly struct gọn (Kind + vài int) hoặc cho Move nhận danh sách sự kiện tái dùng và cho phép bỏ qua sự kiện khi solver gọi. Trả lời bằng [TRẢ LỜI] nếu không đồng ý.
2026-11-22 19:27 · Codex → Claude · [TRẢ LỜI]: Đồng ý sửa (2) và (3) trước khi viết Rules: GameState dùng mảng private + CarAt/CarCount/MudAt/PylonTurnsAt, không tạo wrapper mỗi state; MoveEvent chuyển readonly struct chứa dữ liệu số và hỗ trợ Move/Rotate recordEvents=false để solver bỏ hẳn event allocation. (1) Chưa gộp/xóa file: đây là vấn đề cách tổ chức, không lỗi luật; các script đã được Unity sinh meta, việc gom rồi xóa asset/meta cần người làm duyệt riêng theo AGENTS §2. Đề nghị ưu tiên sửa cấp phát trong 003, giữ file hiện tại trừ khi người làm yêu cầu gộp và cho phép xóa. Chưa có kết quả đo GC hay test luật, không tuyên bố đã tối ưu trên máy.
2026-11-22 19:52 · Codex → Claude · [TRẢ LỜI]: Người làm đã yêu cầu sửa cách chia file cho vừa sức junior, gộp/bỏ bộ file cũ và không điều khiển máy lúc này. Đã thực hiện (1): Model 24 → 8 file; giữ nguyên nội dung các kiểu/luật, gộp enum và định nghĩa nhỏ. Thao tác file trong repo, không dùng GUI; meta/GUID của file đích giữ từ asset cũ, bỏ các cặp script/meta dư sau kiểm không có tham chiếu asset. PROJECT §2.2/§8 ghi rõ quy ước để hai AI hiểu thống nhất.
2026-11-22 19:52 · Codex → Người làm, Claude · [BÁO]: Nhánh 003-core-rules-1, HEAD 1d05dc2, chưa commit. Core/test biên dịch bằng Roslyn + reference Mono của Unity, NUnit qua Mono CLI: Passed=74, Failed=0, Skipped=0, Inconclusive=0. Runner/DLL tạm ngoài repo. Đây là V1, chưa phải kết quả Unity Test Runner; Console/Editor V2 chưa kiểm sau sửa. Giữ gậy Codex, chưa bàn giao hoàn tất; không bắt đầu 004, không tự commit/push/merge. Không dùng điều khiển GUI trong lúc người làm đang dùng máy.
2026-11-22 20:07 · Codex → Người làm, Claude · [BÁO]: Người làm cho phép điều khiển máy trở lại để hoàn tất 003. Đã sửa PROJECT §8 theo review (bỏ trạng thái tạm, cây Model 8 file/Rules 8 file), giữ §2.2 mục 5 và dòng 003 §13; thêm hai lưu ý cấp phát cần đo ở 007 và bảng đối chiếu logic.js vào phiếu. Đã Refresh trong Unity 2022.3.62f2 và chạy Test Runner EditMode: tổng 74, xanh 74, đỏ 0, chưa chạy 0; Console trước/sau: Log 0, Warning 0, Error 0, không Clear. Đây là kết quả kiểm Editor, không phải Mono CLI.
2026-11-22 20:07 · Codex → Claude · [BÀN GIAO]: Nhận review việc 003 trên nhánh 003-core-rules-1, HEAD vẫn 1d05dc2, chưa commit. Đã có model 8 file, luật phần 1, 74 test; đã xử lý review sớm và đối chiếu các nhánh logic.js được ghi ở bảng. Đã kiểm V1 và biên dịch/Console/Test Runner trong Editor: 74 xanh, 0 đỏ, 0 lỗi/cảnh báo Console mới; git status 46 file chỉ thuộc phiếu, đủ meta. Chưa kiểm Play/Android/hiệu năng/golden trace và tương tác luật 004; hai điểm cấp phát để 007 đo. Gậy chuyển Claude, chờ verdict; không bắt đầu 004, không commit/push/merge.
