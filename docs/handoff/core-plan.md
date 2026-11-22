# Kế hoạch lõi game (việc 003 → 008) — Codex làm, Claude chỉ review
Lập 2026-11-22 15:11 bởi Claude, theo yêu cầu người làm: **làm lõi game trước**; việc 002 (Claude làm dữ liệu màn) **hoãn**, phần dữ liệu và trace được gộp vào việc 005 của Codex.
Người làm gửi prompt "làm theo core-plan" = **duyệt kế hoạch** các việc dưới đây (mỗi việc vẫn có phiếu riêng).

## 0. Quy trình chung cho mọi việc
- Đọc `PROJECT.md` → `AGENTS.md` → `CODEX.md` → file này. Chat mới: làm theo `AGENTS.md` §1.6.
- Mỗi việc: **BƯỚC 0 như việc 001** (git sạch; nhánh `NNN-tên-ngắn` từ `main` bằng `--no-track`; phiếu `docs/handoff/NNN-….md` theo mẫu §1.5, lấy nội dung từ mục tương ứng bên dưới; dòng sổ việc §13).
- Làm theo thứ tự **003 → 004 → 005 → 006 → 007 → 008**. Mỗi việc xong thì: điền phiếu, dòng `[BÀN GIAO]` cho Claude (sửa `Đang cầm: Claude`), đưa nội dung commit, rồi **DỪNG chờ review**. Chỉ làm việc kế tiếp khi phiếu có **APPROVE** và người làm đã merge. Việc kế tiếp phải rẽ từ `main` mới nhất.
- Phản đối kế hoạch: ghi vào phiếu **trước khi làm** (và `[HỎI]` ở mục Trao đổi) nếu thấy sai hoặc rủi ro. Không tự đổi phạm vi.
- Kiểm chứng: biên dịch sạch, Console không lỗi/cảnh báo mới, Test Runner EditMode (hoặc `unity test`). **Báo nguyên văn**: số test, tên test đỏ, dòng Console. Không nói "chạy được" khi chưa chạy.
- Số liệu "chưa chắc" ghi rõ. Phát hiện ngoài phạm vi: ghi vào phiếu, không sửa lén. Không tự commit/push/merge.

## 1. Quy ước thiết kế chung (để 6 việc khớp nhau)
- **Core** (`SqueakyLane.Core`, namespace `SqueakyLane.Core.Model` / `.Rules` / `.Solver`): C# thuần, **không** `using UnityEngine`, không LINQ và không tạo chuỗi trong đường nóng (`Move`, `Occupancy`, solver). Solver sẽ gọi hàng trăm nghìn lần trên điện thoại. Không `static` có thể đổi.
- **Game** (`SqueakyLane.Game`): được dùng UnityEngine, nhưng việc 006/008 chỉ viết lớp thường, chưa có MonoBehaviour.
- **Enum** (chỉ thêm cuối, không đổi thứ tự): `Axis {Horizontal, Vertical}`; `Side {N, W, S, E}` (cạnh ô, dùng cho portal/trụ); `GateEdge {Top, Bottom, Left, Right}` (cạnh bãi: `t`/`b`/`l`/`r` trong dữ liệu); `TileColor {Pink, Blue, Yellow, Green, Orange, Purple, Water}` (`Water` chỉ cho cổng; màu xe và màu vũng sơn là 6 màu đầu).
- **Dữ liệu màn** (`LevelDef`, bất biến sau khi dựng): `Cols`, `Rows`, cổng, xe (`CarDef`: trục, vị trí, màu, `Len`, `Truck`, `Mud` ban đầu), cọc, vũng bùn, vòi xịt, vũng sơn, đèn (`Phase`), bàn xoay, barie (`Need`), chốt liên động, portal, trụ xoay, cờ `Splash`.
- **Trạng thái** (`GameState`, bất biến từ bên ngoài): mảng `CarState` (hàng, cột, trục, màu, `Mud`, `Alive`), cờ vũng bùn còn lại, số lần xoay từng trụ (0..3), `Moves`. `Move` trả **trạng thái mới** (sao chép), không sửa trạng thái cũ → hoàn tác chỉ là giữ trạng thái cũ.
- **Sự kiện** (`MoveEvent`): loại `Step, Paint, Mud, Spray, Water, Exit, Stop, Splash, Push, Tele, Rotate, Pyl` + dữ liệu số (không chuỗi). Lý do `Stop` là enum `StopReason {Edge, WrongGate, Muddy, Car, Wall, Push}`.
- **Kết quả**: `MoveResult { GameState State; IReadOnlyList<MoveEvent> Events; bool Moved; bool Exited }`. Nước đi không có hiệu lực thì `Moved = false` và `State` là chính trạng thái cũ.
- **Nguồn luật duy nhất**: `docs/reference/prototype/src/logic.js` (chỉ đọc). Chỗ mơ hồ hoặc nghi sai → ghi vào phiếu và hỏi, **không tự sửa luật**.
- Comment tiếng Việt giải thích "vì sao". Mỗi luật một class nhỏ.

## 2. Việc 003 — Core luật phần 1 (cỡ L, ≤ 1 ngày) · nhánh `003-core-rules-1`
**Mục tiêu:** luật cốt lõi chạy được dưới dạng C# thuần có test.
**Làm** (thư mục `Assets/_Project/Scripts/Core/Model` và `Rules`):
1. Model đầy đủ theo mục 1 (kể cả trường cho portal/trụ/barie/chốt để 004 khỏi đổi kiểu; **chưa** viết luật cho chúng).
2. `LevelDefBuilder` (dựng `LevelDef` bằng code cho test) + `GameState.Initial(level)`.
3. Luật: `Occupancy` (bản đồ chiếm chỗ: trống / vật cản / chỉ số xe), `MoveResolver.Move(level, state, car, sign)`, `GateRule` (cổng màu, cổng nước dội 1 lần/lượt, bùn chặn cổng màu), `TileEffects` (vũng bùn, vòi xịt, vũng sơn, **áp cho cả xe bị đẩy**), bùn văng (`Splash`), `PushRule` (xe tải đẩy chuỗi xe, kể cả xe vuông góc), `LightRule` (đèn đỏ khi `(Moves + Phase) % 2 == 1`), `TableRule.Rotate` (bàn xoay 90° chiều kim đồng hồ, bị chặn thì `Moved=false`), `IsWin`.
4. Bỏ qua trong 003: portal, trụ xoay, barie, chốt (việc 004), mọi thứ dính JSON/solver.
**Test EditMode** (dựng màn nhỏ bằng `LevelDefBuilder`, mỗi dòng ≥ 1 test, kèm chiều ngược): cổng đúng màu ra được / sai màu dừng `WrongGate` / có bùn dừng `Muddy` / cổng nước xịt rồi dội và chỉ dội 1 lần / vũng bùn chỉ bám xe đầu tiên rồi biến mất / vòi xịt làm sạch khi lăn qua / vũng sơn đổi màu / bùn văng (xe còn bùn **đang lao tới** đâm xe sạch; đứng sát sẵn thì không) / cọc chặn / đèn đổi sau mỗi lượt / bàn xoay xoay được và bị chặn / xe tải đẩy được, bị tường chặn, đẩy chuỗi 2 xe, xe bị đẩy nhận vũng sơn / hoàn tác (trạng thái cũ không đổi sau `Move`) / `Moved=false` giữ nguyên tham chiếu trạng thái cũ.
**Điều dễ làm sai:** xe `Truck` có `Len = 3`; `Splash` khi **đẩy** khác khi **đâm** (đọc kỹ `logic.js` nhánh `tryPush` và nhánh `stop car`); thứ tự áp hiệu ứng ô (sơn → bùn → vòi xịt) khi vừa vào ô; `Moves` chỉ tăng khi `Moved`; sửa trạng thái cũ trong `Move`; dùng `string` cho màu.
**Tiêu chí xong (V1+V2):** biên dịch sạch; Console sạch; toàn bộ test qua (nêu số test); không có `using UnityEngine` trong Core; diff chỉ có file thuộc phiếu.
**Cắt nếu trễ:** không cắt test; có thể gom `GateRule`/`TileEffects` vào `MoveResolver` nếu thời gian gấp (ghi vào phiếu).

## 3. Việc 004 — Core luật phần 2 (cỡ M) · nhánh `004-core-rules-2`
**Làm:** (1) **Portal** gắn tường: `[r,c,cạnh]`, xe tới sát mép thì chui, trồi ở cổng B vuông góc mặt tường B (đổi trục được), thân xe phải trống nếu không cổng như tường, **tối đa 1 lần chui mỗi lượt**. (2) **Trụ xoay**: ô tường có hai mặt cổng, `RotatePylon` +90° một chiều, `PylonFaces(p, turns)` (mặt 0 đi N → W → S → E, mặt 1 đối diện), `fixed` thì không xoay, trụ không chặn bởi xe đứng cạnh. (3) **Barie đếm xe** (`Need`, mở khi số xe đã rửa ≥ Need), `BarriersOpened`. (4) **Chốt liên động** (xe đè chốt → barie A đóng, B mở; tính theo vị trí ở **đầu** lượt). (5) Nối vào `Occupancy`/`MoveResolver` đã có, không đổi hợp đồng ở mục 1.
**Test:** mỗi cơ chế ≥ 2 test (chạy được / bị chặn), portal đổi trục ngang↔dọc, portal chỉ 1 lần/lượt, trụ xoay đủ 4 lần về chỗ cũ, trụ nối cổng đúng theo hướng, barie mở sau khi rửa đủ xe, chốt đổi trạng thái barie theo vị trí xe, xe tải đẩy qua barie/đèn đỏ bị chặn.
**Điều dễ làm sai:** hướng trụ `'h'|'v'` đổi sang số lần xoay (`h=0`, `v=1`); `faceList` phụ thuộc hướng trụ (đừng nhớ đệm sai khi hướng đổi); vị trí đầu xe khi xe chạy ngược chiều; chốt tính ở **đầu lượt**, không phải giữa lượt.
**Tiêu chí xong:** như 003; thêm: đọc lại `logic.js` từng hàm `portalTry`, `pylonFaces`, `faceList`, `switchPressed`, `barriersOpened` và ghi vào phiếu "đã đối chiếu từng hàm".

## 4. Việc 005 — Dữ liệu chuẩn: 31 màn dạng JSON object + golden trace (cỡ M) · nhánh `005-golden-data`
(Đây là việc 002 cũ của Claude, giờ Codex làm bằng `node`. Nếu máy không có `node`, **dừng và hỏi** — không cài thêm.)
**Làm** (thư mục `docs/golden/`, ngoài `Assets`, nên không có `.meta`, không vào APK):
1. `tools/export.js`: đọc `docs/reference/prototype/src/levels.js`, ghi `levels/level_NN.json` (31 file) dạng object mà `JsonUtility` đọc được:
```json
{ "id":1, "name":"…", "cols":4, "rows":5,
  "gates":[{"side":"t","i":1,"color":"pink"}],
  "cars":[{"axis":"h","r":1,"c":0,"color":"blue","len":2,"truck":false,"mud":0}],
  "cones":[{"r":0,"c":0}], "muds":[…], "sprays":[…], "tables":[…],
  "paints":[{"r":0,"c":0,"color":"pink"}],
  "lights":[{"r":0,"c":0,"phase":0}], "barriers":[{"r":0,"c":0,"need":1}],
  "splash":false,
  "portals":[{"a":{"r":0,"c":0,"side":"N"},"b":{"r":0,"c":0,"side":"S"}}],
  "pylons":[{"r":2,"c":2,"o":"h","fixed":false,"to":[{"r":0,"c":0,"side":"S"},{"r":0,"c":0,"side":"N"}]}],
  "switches":[{"r":0,"c":0,"a":[{"r":0,"c":0}],"b":[{"r":0,"c":0}]}],
  "minMoves":2, "solveExhausted":true }
```
   Khóa vắng = mảng rỗng/`false`. `minMoves = -1` nếu solver chưa kết luận (màn 17, 18, 19; màn 16 cần `maxStates` ≥ 300000). Số `minMoves` lấy từ `docs/reference/prototype/levels-export/level_NN.json` (đã có) và **kiểm lại bằng `solve`** trong `verify.js`. Chưa chuyển `tutorial` và `hint`.
2. `tools/make-traces.js`: dùng `logic.js` (module `CW`), với **mỗi màn** 8 chuỗi nước đi ngẫu nhiên, **seed cố định** (tự viết PRNG đơn giản, không dùng `Math.random`), mỗi chuỗi tối đa 25 nước chọn trong các nước hợp lệ (`nextMoves`), ghi `traces/trace_NN.json`: danh sách nước (`kind: "move"|"rot"|"pyl"`, `car`, `sign`) + với mỗi nước: `moved`, `events` (giữ đủ trường như `logic.js`, đổi `gate` thành `side`/`i`/`color`), và trạng thái sau nước (xe: `r,c,axis,color,mud,alive`; `muds`; `pyl`; `moves`).
3. `tools/verify.js`: đọc lại JSON mới → đổi ngược về dạng cũ → `CW.compile` → `solve` → so `minMoves`; chạy lại trace bằng `logic.js` và so từng nước.
4. `README.md` mô tả định dạng và cách chạy (`node docs/golden/tools/verify.js`).
**Tiêu chí xong (V1 bằng node):** `verify.js` in **qua 31/31** (màn `-1` chỉ kiểm không lỗi); tổng `docs/golden/` ≤ ~3 MB; chạy lại `export.js` và `make-traces.js` cho **file y hệt** (xác định); không sửa `docs/reference/prototype/`.
**Điều dễ làm sai:** `null` → `-1` (JsonUtility); mất `phase`, `fixed`, `need`; đảo `r`/`c`; portal/trụ dùng `side` chữ `N|W|S|E`; trace dùng `Math.random`.
**Cắt nếu trễ:** giảm xuống 5 chuỗi × 20 nước; bỏ trace màn 17–19.

## 5. Việc 006 — Bộ nạp JSON + kiểm khớp trace (cỡ M) · nhánh `006-loader-trace`
**Làm:** (1) `Game/LevelJsonLoader`: lớp DTO `[Serializable]` + `JsonUtility.FromJson` → `LevelDef`; báo lỗi rõ ràng khi dữ liệu sai (khóa màu lạ, xe ra ngoài bãi, chồng xe…) bằng exception có tên màn; **không** `Resources.Load` rải rác (đường dẫn truyền vào). (2) Test EditMode đọc `docs/golden/levels/*.json` và `traces/*.json` qua đường dẫn tương đối thư mục gốc project (`Application.dataPath/..`), **phát lại từng trace** bằng Core và so: `moved`, danh sách sự kiện (loại + số liệu), trạng thái sau mỗi nước. Khi lệch: báo `màn · chuỗi · nước thứ · trường lệch · mong đợi/thực tế`. (3) Test nạp: 31 màn nạp được; màn hỏng cố ý thì báo lỗi.
**Đây là cổng đúng/sai của luật Core.** Nếu lệch: sửa Core ở việc này **chỉ khi** đối chiếu `logic.js` xác nhận Core sai; nếu nghi `logic.js` sai thì ghi phiếu và hỏi.
**Tiêu chí xong:** 31/31 màn × mọi chuỗi trace khớp 100%; nêu số bước đã so sánh.
**Điều dễ làm sai:** `JsonUtility` không có trường mặc định cho khóa vắng (khởi tạo mảng rỗng); khác biệt enum màu giữa JSON và Core; so thứ tự sự kiện (phải giữ đúng thứ tự); đường dẫn khác nhau giữa Editor/build (test chỉ chạy trong Editor).

## 6. Việc 007 — Solver (cỡ M–L) · nhánh `007-solver`
**Làm** (`Core/Solver`): (1) **Khóa trạng thái không dùng `string`**: gói vị trí/trục/màu/bùn/alive từng xe + vũng bùn + trụ (+ tính chẵn lẻ của `Moves` nếu màn có đèn) vào struct khóa so sánh nhanh (hai `ulong` hoặc mảng byte băm), dùng `Dictionary`/`HashSet` có `IEqualityComparer` riêng. (2) `Solver.Solve(level, state, maxStates)` BFS ít lượt nhất → `{ Moves | null, Exhausted, Explored }`. (3) `WinSearch`: còn đường thắng không (ưu tiên trạng thái còn ít xe), tìm theo **bước ngân sách** (`Step(budget)` trả về đã xong hay chưa) để gọi chia nhỏ qua nhiều frame; **không dùng thread, không UnityEngine**. (4) `PathSearch`: một đường thắng bất kỳ (cho Gợi ý), cùng kiểu chạy theo bước.
**Test:** `minMoves` của **31 màn** từ `docs/golden/levels` khớp (màn `-1` bỏ qua; màn 16 truyền `maxStates` lớn); `WinSearch` đúng cho màn thắng được và cho trạng thái kẹt dựng tay; `PathSearch` ra chuỗi nước phát lại bằng Core thì thắng; trạng thái khác nhau cho khóa khác nhau (test va chạm khóa trên toàn bộ trạng thái của vài màn nhỏ).
**Đo và báo (không phải tiêu chí đỏ/xanh):** thời gian `Solve` màn 16 trong Editor (ms), số trạng thái, ước lượng cấp phát (Profiler hoặc `GC.GetAllocatedBytesForCurrentThread`). Nếu `WinSearch` một lần mở rộng > 2 ms thì ghi và đề xuất giảm.
**Điều dễ làm sai:** khóa thiếu `Moves % 2` làm đèn sai; khóa trùng cho trạng thái khác nhau (nghiêm trọng); LINQ/`List` mới trong vòng lặp; `maxStates` cố định làm màn 16 báo sai `minMoves`.

## 7. Việc 008 — Phiên chơi, đồng hồ, config (cỡ M–L) · nhánh `008-session-timer`
**Làm** (`Game/`, vẫn **chưa có MonoBehaviour/scene**): (1) `GameSession`: giữ `LevelDef`, trạng thái hiện tại, **ngăn xếp hoàn tác** (giữ trạng thái cũ, không giới hạn), `Apply(lệnh)` với lệnh `MoveCar(car, sign)`, `RotateTable(car)`, `RotatePylon(index)` → trả `MoveResult`; `Undo()`; `IsWin`; `Restart()`. (2) `StuckDetector` dùng `WinSearch` chạy theo bước, cung cấp `Poll(budget)` và kết quả `Unknown/Winnable/Stuck`. (3) `HintProvider` dùng `PathSearch`, giới hạn số lần theo màn. (4) `LevelTimer`: đếm ngược theo **`dt` truyền vào** (không đọc `Time`), `Paused`, `ChargeUndo()`, `ChargeHint()`, `StarsOnWin()` theo `PROJECT.md` §6.4 (3★ còn ≥ 50% giờ, 2★ ≥ 20%, còn lại 1★; màn `timeLimit <= 0` không tính giờ, 3★). (5) `GameConfig : ScriptableObject` tại `Assets/_Project/Config/GameConfig.asset` (tạo qua Editor): `BaseSeconds=20`, `SecondsPerMove=5`, `UndoPenalty=3`, `HintsPerLevel=3`, `HintPenalty=5`, `Star3Ratio=0.5`, `Star2Ratio=0.2`, `TargetFps=60`; mỗi field có `[Tooltip]`, `[Min]`/`[Range]`, `OnValidate` chặn giá trị sai. Công thức giờ: `BaseSeconds + SecondsPerMove × minMoves`. **Ghi** `config mới: GameConfig · các field · vì sao` vào phiếu. `ConfigProvider` làm sau (khi có scene).
**Test:** hoàn tác nhiều lần về đúng trạng thái đầu, hoàn tác bị trừ giờ, hết giờ thì `IsOver`, tạm dừng thì không trừ, sao đúng ngưỡng (biên 50%, 20%), màn không tính giờ, gợi ý giảm số lần còn lại và trừ giờ, `StuckDetector` báo `Stuck` ở trạng thái kẹt dựng tay, `GameConfig.OnValidate` chặn giá trị âm.
**Điều dễ làm sai:** đồng hồ dùng thời gian có scale hay `Time` trực tiếp (phải nhận `dt` từ ngoài, quyết định scale ở lớp gọi sau); hoàn tác giữa lúc đang có kết quả `Poll` dở (hủy tìm kiếm cũ); sửa trực tiếp ScriptableObject lúc chạy (sao chép số sang đối tượng thường); đổi tên field sau này mất giá trị (nếu đổi phải có `[FormerlySerializedAs]`).
**Tiêu chí xong:** test qua; asset `GameConfig` có `.meta` do Unity sinh; Console sạch; cập nhật `PROJECT.md` §8 (mô tả đúng cây thư mục đã dựng) và §13.

## 8. "Xong lõi game" nghĩa là gì
- 003–008 đều **APPROVE** và đã merge vào `main`; Test Runner EditMode **toàn bộ xanh** (ghi tổng số test).
- 31/31 màn × mọi trace khớp `logic.js`; `minMoves` của solver khớp dữ liệu chuẩn.
- Chưa có hình ảnh/điều khiển/VFX/âm thanh: đó là giai đoạn sau (khối xám → bãi từ JSON → input → art).

## 9. Lịch và rủi ro
| Mốc | Dự kiến |
|---|---|
| 003 | 22/11 (hôm nay) |
| 004, 005 | 23/11 |
| 006, 007 | 24/11 |
| 008 | 25/11 |
Nếu trễ ≥ nửa ngày ở bất kỳ việc nào, báo người làm để cắt theo `PROJECT.md` §10. **Thứ tự cắt trong lõi:** (1) bớt trace (005), (2) bỏ `HintProvider` (008), (3) giữ `StuckDetector` đơn giản, (4) tuyệt đối không bỏ 003/004/006/007. Rủi ro lớn nhất: 006 lệch trace và phải truy nguyên nhân luật; dự trù thêm nửa ngày.
**Việc phụ thuộc người làm:** đặt package name, khóa Portrait trong Editor (không chặn lõi); chọn nguồn xe (đã chốt Kenney); đưa **25 màn cuối** vào `Resources/Levels` (việc riêng sau lõi, cần chọn từ 31 màn + sinh thêm).
