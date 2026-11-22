# 007 — Solver     Cỡ: L (≤ 1 ngày)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Codex
Liên quan: 003–006; core-plan.md §6     Chờ nhận: —

## Mục tiêu
Tìm số lượt ít nhất để kiểm màn, tìm đường thắng/gợi ý theo từng bước để sau này không chặn một frame bằng toàn bộ phép tìm kiếm; có số đo thật cho màn 16.

## Phạm vi — Làm / Không làm
- Làm: StateKey không chuỗi, BFS Solver.Solve, WinSearch/PathSearch.Step(budget), test golden minMoves/đường đi/key và đo thời gian/cấp phát màn 16 trong Editor.
- Không làm: luật mới/đổi luật/nguồn/JSON/golden trace, GameSession/StuckDetector/HintProvider/config (008), MonoBehaviour/scene/prefab, package/settings/asmdef, commit/push/merge.
- Git sạch, 006 code + APPROVE đã vào main. Sau fetch main/origin/main cùng f32f34f; tạo 007-solver --no-track từ main. Người làm giao 007 và nhắc đo màn 16 = duyệt core-plan §6.

## Các bước
1. Kiểm Git/006 APPROVE đã merge; tạo nhánh/phiếu/sổ việc.
2. Đọc lại stateKey/nextMoves/solve/winSearch/pathSearch trong logic.js và API Core đã kiểm ở 006.
3. Viết khóa byte đóng gói theo kích thước bãi; so byte đầy đủ khi hash trùng, có comparer riêng. Giữ dữ liệu tương lai ảnh hưởng luật; không lưu chuỗi trong vòng tìm kiếm.
4. BFS theo queue để tối ưu số nước; tìm thắng/đường bất kỳ dùng stack theo số xe còn lại. Dùng chung bộ duyệt lệnh không tạo List theo từng node, gọi recordEvents=false, giữ parent index để dựng đường đi.
5. Test 28 minMoves đã biết, ghi rõ 17–19 chưa có đáp án tối ưu; phát lại đường tìm được, kiểm thắng/kẹt/hết ngân sách; duyệt hết đồ thị các màn nhỏ và test khóa không lẫn trạng thái luật.
6. Đo màn 16 trong Editor bằng Stopwatch + GC.GetAllocatedBytesForCurrentThread (nạp JSON ngoài vùng đo); báo mẫu/min/median/max, số trạng thái, cấp phát. Đo WinSearch.Step(1); nếu một mở rộng >2ms thì ghi và đề xuất.
7. Unity biên dịch/Console/Test Runner; kiểm phạm vi/meta; điền phiếu/PROJECT, bàn giao Claude rồi dừng, chưa làm 008.

## Tệp dự kiến (Editor khi import/test)
- Core/Solver/StateKey.cs: key/encoder/comparer gom cùng file.
- Core/Solver/Solver.cs: BFS facade và kiểu lệnh/kết quả nhỏ.
- Core/Solver/StepSearch.cs: engine dùng chung, WinSearch/PathSearch và cursor nội bộ, không tách mỗi kiểu nhỏ một file.
- Tests/EditMode/SolverTests.cs, StateKeyTests.cs, SolverPerformanceTests.cs (helper/DTO nằm cùng file).
- PROJECT.md §8/§13; phiếu này. Đường dẫn C# dưới Assets/_Project; meta do Unity import sinh.
- Bằng chứng đo ghi Logs/007-solver-performance.txt (đã ignore, không commit). Không đổi luật Core trừ khi có bằng chứng cần thiết rồi ghi lại trước khi sửa; ưu tiên đo rồi báo giới hạn.

## Tiêu chí xong — V1 + V2
- 28 minMoves đã biết khớp 100% (màn 16 dùng maxStates=300000); 17–19 giữ chưa biết, không tự thay golden. Đường Solver/PathSearch phát lại bằng Core thì thắng.
- WinSearch phân biệt thắng, chắc chắn kẹt và hết ngân sách/chưa kết luận; Step có giới hạn số node mở rộng và dừng đúng; không thread/UnityEngine.
- Key gồm vị trí/trục/màu/bùn/alive xe sống, vũng bùn, trụ, chẵn lẻ Moves khi có đèn; test từng trường + toàn bộ đồ thị vài màn nhỏ; hash collision không đồng nghĩa key bằng nhau.
- Unity compile sạch, Console không lỗi/cảnh báo mới, toàn bộ EditMode qua (giữ 229 test cũ); ghi số xanh/đỏ và số đo thật màn 16. Đo hiệu năng là báo cáo, không ép ngưỡng để cho xanh.
- Core không LINQ/chuỗi đường nóng/static mutable; mọi asset mới đủ meta; Git đúng phạm vi.

## Điều cấm / rủi ro / thứ có thể cắt nếu trễ
- Không hard-code một giới hạn state để tự nhận màn vô nghiệm. MaxStates là tham số; exhausted=false khi hết ngân sách, true chỉ khi có kết luận.
- Màn 17 có 15 xe/7×7: chỉ các trường xe đã cần 195 bit, nên chọn mảng byte đóng gói độ rộng theo bãi (không cắt xuống 128 bit). Đánh đổi là một mảng key mỗi trạng thái ứng viên; phải đo GC.
- Xe đã ra khỏi bãi chỉ cần alive=false trong key (như nguồn), các thuộc tính xe chết không ảnh hưởng luật nữa; bỏ Moves tuyệt đối nhưng giữ parity nếu có đèn. Key được gắn với một LevelDef, không trộn màn.
- Step(budget) tính theo node mở rộng, không hứa số ms cố định; một node vẫn có nhiều lệnh. Đo Step(1) để nhìn độ trễ từng node; nếu lớn đề xuất chia nhỏ đến lệnh.
- Core Move hiện vẫn clone 3 mảng + state, tạo occupancy và MoveResult; tắt event không có nghĩa hết cấp phát. Chưa dùng pool/workspace hoặc refactor luật khi chưa có số đo.
- Mỗi trạng thái trong hàng đợi có một GameState; parent lưu index/lệnh. Giải phóng tham chiếu state đã mở rộng và dữ liệu tìm kiếm khi xong để tránh giữ toàn đồ thị sau khi lấy kết quả.
- Time-box L ≤1 ngày, vượt gấp đôi dừng báo. Kế hoạch ban đầu không bỏ số đo màn 16 hoặc kiểm key. Người làm sau đó duyệt hoãn xác nhận cấp phát và chạy lại bộ đo (xem Trao đổi); giữ nguyên các test, không tắt hoặc làm yếu assertion.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch và vì sao
- [Đã kiểm trong code] Thêm 3 file Core/Solver: `StateKey.cs` (khóa byte/encoder/comparer), `Solver.cs` (BFS, SolverMove/SolveResult), `StepSearch.cs` (engine dùng chung, WinSearch/PathSearch). Không đổi luật Core hay dữ liệu chuẩn, không scene/prefab/config/package/settings.
- Khóa đóng gói theo kích thước bãi, so đủ byte sau hash; gắn với LevelDef. Xe đã ra chỉ giữ alive=false; chỉ giữ Moves parity khi có đèn. Bộ duyệt lệnh dùng struct, không tạo List lệnh mỗi node, gọi recordEvents=false. Parent lưu index/lệnh; bỏ tham chiếu GameState đã mở rộng và giải phóng đồ thị sau khi xong.
- BFS trả đường ít lượt nhất; WinSearch/PathSearch ưu tiên ít xe còn lại, Step(budget) theo số node. Explored đếm trạng thái mới, không tính gốc; Expanded đếm node đã mở rộng. Chạm giới hạn maxStates mà chưa kết luận thì Exhausted=false, không báo kẹt chắc chắn.
- Thêm `SolverTests.cs`, `StateKeyTests.cs`, `SolverPerformanceTests.cs`: 28 minMoves khớp và phát lại thắng, PathSearch trên cả 31 màn phát lại thắng, ngân sách/thắng/kẹt/đầu vào sai; kiểm từng trường khóa, hash bucket trùng, tọa độ rộng/>128 bit, duyệt hết 3 đồ thị nhỏ với oracle chuỗi độc lập trong test. Màn 17–19 chỉ kiểm tìm được đường, không tuyên bố tối ưu.
- [Đã kiểm trong Editor, lượt trước bản sửa bộ đo] Unity 2022.3.62f2, Test Runner EditMode hiển thị: **314 passed, 0 failed, 0 remaining**. Không có test đỏ. Console trước lượt chạy: **0 log, 0 warning, 0 error**. Không khẳng định đây là kết quả của bản sửa Profiler cuối hoặc đã đọc lại Console sau lần sửa đó.
- [Đã đo trong Editor/Mono] Màn 16, maxStates=300000, warmup màn 1, AMD Ryzen 7 7735HS, RAM 15610 MB; I/O ngoài vùng đo. Ba mẫu BFS: **1631,394 / 1587,401 / 1582,617 ms**; min/median/max: **1582,617 / 1587,401 / 1631,394 ms**. Cả ba: **9 nước, Explored=79056, Expanded=46115**. GC0/1/2 lần lượt 45/45/45, 44/44/44, 45/45/45.
- [Đã đo trong Editor/Mono] WinSearch màn 16: tổng **0,183 ms**, Explored=58, Expanded=9; gọi Step(1) 10 lần, cao nhất **0,044 ms**. Đây là một lượt đo trên máy này, không bảo đảm mọi node/mọi màn dưới 2 ms.
- **Cấp phát chưa có số hợp lệ:** GC.GetAllocatedBytesForCurrentThread trả 0 dù có GC. Không diễn giải 0 là không cấp phát. Đã thay bộ đo bằng ProfilerRecorder `GC Allocated In Frame.CurrentValue`, kiểm đơn vị byte và hiệu chuẩn mẫu 1 MiB; **bản sửa này chưa chạy lại/kiểm biên dịch trong Editor**. Nếu hoạt động, đây là ước lượng mọi luồng, có nhiễu nền. Người làm cho hoãn bước này, không tự bỏ assertion. Tài liệu API: https://docs.unity3d.com/cn/2022.3/ScriptReference/Unity.Profiling.ProfilerRecorder.CurrentValue.html
- Artifact lượt đo cũ: `Logs/007-solver-performance.txt` (ignored, không commit); số allocatedBytes=0 trong artifact không hợp lệ. Số liệu thời gian đã chép vào phiếu để reviewer không phụ thuộc file ignored.
- [Đã kiểm file] 6 script mới và thư mục Solver có đủ .meta do Unity sinh. PROJECT §8 cập nhật cây Solver, §13 cập nhật 006 đã merge/007 chờ review và quyết định hoãn. Phiếu này bàn giao Claude. Không có thay đổi scene/prefab.
- Lệch kế hoạch được người làm duyệt: hoãn chạy lại phép đo cấp phát và xác nhận bản sửa bộ đo cuối; phần hiệu năng chưa được xác nhận đầy đủ. Không báo đã đạt toàn bộ tiêu chí V2 của phiên bản cuối.
- Tự tin khoảng **85% về chức năng Solver trong phạm vi đã test**; chưa đánh giá được độ tin cậy phép đo cấp phát mới. Cần Claude review khóa/cap/đường đi và phần bộ đo chưa chạy. Không bắt đầu 008.

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- Màn 17–19 chưa biết tối ưu, không lấy đường tìm được bất kỳ thay minMoves. 007 không sinh 25 màn cuối.
- Kết quả Editor không phải FPS/hiệu năng Android; allocated bytes là tổng cấp phát trong vùng đo, không phải RAM còn giữ lại.
- Phiếu 006 còn Chờ merge/Người làm cầm, không sửa phiếu cũ ngoài phạm vi; PROJECT cập nhật đã merge theo main và giao việc của người làm.
- Cần đo lại chi phí Copy/MoveEvent từ việc 003: hiện chưa có số byte đáng tin để kết luận; không thêm pooling hoặc refactor luật khi chưa đo. BFS đồng bộ khoảng 1,59 giây có thể đứng hình nếu gọi thẳng trong gameplay; runtime nên dùng tìm kiếm theo bước, vẫn cần đo trên Android.
- Giới hạn maxStates chặn nghiêm số trạng thái mới nhận; không dựa vào hành vi vượt cap thêm một trạng thái của prototype. Bộ test known minima dùng 300000 và đã qua ở lượt trước.
- Git hiện theo dõi origin/007-solver tại HEAD f32f34f; Codex không push hay thiết lập upstream trong phiên. `docs/handoff/play-plan.md` là file chưa theo dõi ngoài phạm vi 007, giữ nguyên và không đưa chung vào commit 007 nếu chưa có chỉ đạo riêng.
- Chưa kiểm: phép đo Profiler sau sửa, Console/Test Runner của bản đo cuối, Android/IL2CPP, hiệu năng 25 màn cuối (chưa có dữ liệu). Không xóa/tắt test để che phần chưa kiểm.

## Bạn cần hiểu
- BFS đảm bảo ít lượt nhất; tìm ưu tiên ít xe chỉ nhằm tìm nhanh một đường thắng, không đảm bảo ngắn nhất.
- Hash chỉ giúp tìm nhanh; vẫn phải so đủ dữ liệu key để không bỏ nhầm trạng thái khác.
- Hết ngân sách là chưa biết, khác với đã duyệt hết mà không tìm thấy đường thắng.
- Step chia phép tìm kiếm theo node; số ms thực tế và cấp phát phải đo, không suy từ số dòng code.

## Phản đối kế hoạch
- Chưa có phản đối chặn thực hiện. Chọn mảng byte (phương án có trong plan) vì 128 bit không đủ cho màn lớn; rủi ro cấp phát được ghi và sẽ đo thay vì tự tuyên bố hiệu năng tốt.

--- Người review điền ---
## Verdict
Chưa review. Codex không tự APPROVE code mình viết.

## Trao đổi
2026-11-22 22:20 · Codex → Người làm · [BÁO]: Nhận 007; main sạch, 006 APPROVE đã merge; main/origin/main cùng f32f34f sau fetch. Tạo 007-solver --no-track, gậy Codex. Sẽ đo thật thời gian/cấp phát/số trạng thái màn 16 trong Editor và chi phí Step(1), không làm 008.
2026-11-22 22:40 · Codex → Claude, Người làm · [BÁO]: Người làm yêu cầu bỏ qua bước chạy lại phép đo cấp phát để sửa sau và xin nội dung commit. Giữ nguyên test; 314 qua/0 đỏ là lượt trước bản sửa Profiler cuối, cấp phát 0 byte không hợp lệ. Chưa chạy lại bản đo mới, không nhận V2 cuối đã đủ.
2026-11-22 22:40 · Codex → Claude · [BÀN GIAO]: Đã thêm StateKey/BFS/WinSearch/PathSearch và test; 28 minima khớp, 31 đường PathSearch thắng trong lượt 314 xanh/0 đỏ. Màn 16 BFS median 1587,401 ms, 79056 trạng thái mới; Step(1) max 0,044 ms. Cấp phát/bản sửa bộ đo cuối chưa kiểm, người làm cho hoãn. Nhánh 007-solver, HEAD f32f34f; thay đổi chưa commit. Gậy Claude để review, không làm 008; play-plan.md ngoài phạm vi được giữ nguyên.
