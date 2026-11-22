# 006 — Nạp JSON và kiểm khớp Core với trace     Cỡ: M (≤ nửa ngày)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Codex
Liên quan: 003–005; core-plan.md §5; review 005     Chờ nhận: —

## Mục tiêu
C# nạp được dữ liệu màn và diễn lại cùng nước đi/sự kiện/trạng thái với prototype, để phần hiển thị và solver có nền luật đã được đối chiếu.

## Phạm vi — Làm / Không làm
- Làm: LevelJsonLoader trong Game dùng JsonUtility + DTO Serializable, chuyển sang LevelDef và báo lỗi có tên màn; EditMode nạp 31 màn và phát lại mọi trace, so cả trạng thái/sự kiện đúng thứ tự.
- Theo review 005: thêm 1 chuỗi lời giải CW.solve cho mỗi màn có minMoves (28 màn), giữ 8 chuỗi random/màn; cập nhật generator/verifier/README, giữ xác định và tổng ~≤3,6 MB. Không sửa level JSON/nguồn prototype/minMoves.
- Nếu trace lệch: xác định là loader/so sánh/Core; chỉ sửa Core khi đọc logic.js xác nhận C# sai, ghi lỗi và bằng chứng trước khi sửa. Nghi luật nguồn sai thì ghi Phản đối/[HỎI], không sửa nguồn/test cho qua.
- Không làm: solver C# (007), session/config/MonoBehaviour/visual/scene/prefab, 25 màn cuối/Resources, package/settings/asmdef, commit/push/merge.
- Main sạch, đã có 005 code + APPROVE (85a5391); fetch xong main/origin/main cùng 85a5391; nhánh 006-loader-trace tạo --no-track từ main. Người làm giao 006 = duyệt core-plan §5 và tiếp nhận review 005.

## Các bước
1. Kiểm Git/005 APPROVE đã merge, tạo nhánh/phiếu/sổ việc.
2. Mở rộng trace theo review, verify Node, so byte khi sinh lại; phân biệt random/solution và ghi schema.
3. Viết loader + DTO gom trong một file; không đọc Resources, bên gọi truyền nội dung JSON và tên nguồn. Mảng vắng/null chuẩn hóa rỗng; kiểm khóa màu/trục/cạnh, tọa độ/chiều dài/chồng xe và dữ liệu cơ chế.
4. Viết test nạp 31 màn, trường hợp thiếu tùy chọn và dữ liệu hỏng; test replay từng trace qua Move/Table/Pylon, so moved/exited, mọi trường event (kể cả Push.fx), state và bất biến state đầu vào. Thông báo lệch có màn/chuỗi/nước/trường/mong đợi/thực tế.
5. Unity tự sinh meta, biên dịch, chạy toàn bộ EditMode, đọc Console; xử lý lệch theo nguồn; kiểm diff/phạm vi.
6. Điền phiếu/PROJECT, bàn giao Claude và dừng. Chưa bắt đầu 007.

## Tệp dự kiến (Editor khi import/test)
- Mới Game/LevelJsonLoader.cs (DTO nằm cùng file); Tests/EditMode/LevelJsonLoaderTests.cs, GoldenTraceTests.cs (DTO trace/helper nhỏ gom cùng file).
- docs/golden/tools/make-traces.js, verify.js; docs/golden/traces/trace_NN.json; docs/golden/README.md.
- Core/*.cs chỉ sửa nếu trace chứng minh lệch so logic.js; ghi cụ thể trước khi sửa.
- PROJECT.md §8/§13/§14; phiếu này. Đường dẫn C# dưới Assets/_Project, meta do Unity import tạo.

## Tiêu chí xong — V1 + V2
- Node verify 31/31, thêm đúng 28 chuỗi solution đã biết tối ưu, giữ random; dữ liệu xác định và ~≤3,6 MB.
- C# nạp đủ 31 màn; dữ liệu sai báo exception có tên nguồn; tùy chọn vắng được xử lý rõ ràng.
- 31/31 màn × mọi chuỗi × mọi bước khớp 100% moved/events/state; báo tổng bước thực tế, không suy từ số test.
- Unity biên dịch sạch, Console không lỗi/cảnh báo mới; toàn bộ EditMode (kể cả 131 cũ) qua, báo tổng/xanh/đỏ nguyên văn.
- Core không UnityEngine, không yếu test/validator; Git chỉ có file thuộc phiếu, đủ meta; không thay nguồn/level JSON cho khớp C#.

## Điều cấm / rủi ro / thứ có thể cắt nếu trễ
- DTO chỉ là dữ liệu nạp, không tạo nhiều file cho từng kiểu nhỏ. Không dùng package/parser mới, DI/event bus hay MonoBehaviour.
- JsonUtility không suy mặc định từ prototype: level format đã ghi len tường minh; loader yêu cầu len=2/3, không coi len=0 là mặc định để tránh nhận dữ liệu hỏng. Mảng/null và bool/int tùy chọn chuẩn hóa sau nạp, không dựa vào constructor DTO.
- Root event Pyl có k là số, còn push.fx có k là chữ: dùng hai DTO khác nhau nhưng cùng file, không ép chung một kiểu field. Gate null trong JSON cần kiểm hành vi thực tế JsonUtility; so theo dữ liệu cổng có nghĩa, không đoán.
- So từng trường theo loại event, không chỉ so số event hoặc state cuối. Màu/trục/cạnh dùng khóa chính xác, không ánh xạ theo thứ tự enum tùy tiện.
- Chỉ test Editor đọc docs/golden qua Application.dataPath/..; runtime loader nhận chuỗi, không giữ đường dẫn máy tuyệt đối hoặc rải Resources.Load.
- Time-box M ≤ nửa ngày; quá gấp đôi dừng báo. Không cắt replay/validator/test; nếu lệch lớn cần báo bằng chứng, không làm trước 007.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch và vì sao
- Thêm Game/LevelJsonLoader.cs: Load(json, sourceName) dùng JsonUtility, DTO riêng tư cùng file, đổi sang LevelDefBuilder/LevelDef bất biến. Chuẩn hóa mảng vắng/null; kiểm màu/trục/cạnh, tọa độ, len/truck/mud, chồng xe, cổng/bàn xoay/phase/need/đầu nối/chốt. Lỗi FormatException giữ tên nguồn và inner exception; không tự đọc Resources/file trong runtime.
- Thêm LevelJsonLoaderTests.cs: 31 màn nạp được, 6 màu, trục/tọa độ, mảng vắng/null, h/v + fixed, cổng nước/cạnh bãi, 23 trường hợp hỏng có tên nguồn/nguyên nhân. Các trường len=0 hoặc vắng bị từ chối theo format object đã ghi, không đoán thành 2.
- Thêm GoldenTraceTests.cs: 31 case replay qua Move/Table/Pylon, so moved/exited và từng trường của 12 loại event theo đúng thứ tự, Push.cars/fx, state xe/bùn/trụ/Moves, và state đầu vào không bị sửa. Lỗi chứa màn/chuỗi/nước/trường/mong đợi/thực tế. DTO Pyl.k (int) và fx.k (string) tách kiểu trong cùng file; cổng null/DTO rỗng được diễn giải là không có cổng.
- Theo review 005, trace schemaVersion=2 giữ nguyên 248 random/4533 nước cũ (đã so nội dung với Git HEAD, bỏ riêng nhãn kind khi so), thêm 28 solution từ CW.solve = 221 nước. Tổng **31 màn, 276 chuỗi, 4754 nước**; solution kết thúc thắng, độ dài khớp minMoves; 17–19 vẫn -1 và không thêm solution.
- Mở rộng make-traces.js/verify.js và README: phân loại random/solution, verifier kiểm mọi chuỗi và giữ nguồn + minMoves, giới hạn 3.600.000 byte. README ghi API loader/schema, PROJECT §8 thêm loader, §13 005 đã merge/006 Chờ review, §14 mô tả schema mở rộng; không chèn trạng thái tạm vào kiến trúc.
- **Fixture Push.fx riêng:** thêm một test dùng kết quả CW.move của màn nhỏ (tải ngang 3,0 dài 3; xe dọc 2,3; sơn green + bùn + xịt tại 2,4). Giữ nguyên JSON nguồn trong test; Core và JsonUtility phải cho Push.fx theo thứ tự paint(green) → mud(idx=0) → spray, rồi Step/Stop Push, state mới đúng và state cũ không đổi. Không thêm dữ liệu giả vào corpus chuẩn.
- **V1 Node cuối, exit 0:** `QUA 31/31: 28 minMoves đã kiểm; 3 chưa kết luận; 276 chuỗi/4754 nước khớp; 3207205 byte.` Dung lượng khoảng 3,21 MB, dưới ~3,6 MB đã duyệt; không cắt random/solution. Không đổi 31 level JSON, minMoves hay prototype.
- **Xác định:** `DETERMINISM: 31/31 trace byte-identical; giu nguyen 248 random sequences/4533 original steps.` Hash tổng trace `f527476d01d554c12100a79df4bfe5ada5d0045ef405e69202e455d10b7b8229` (băm các dòng `trace_NN.json SHA256\n` theo tên). Tái sinh make-traces rồi so hash tất cả 31 file; random cũ so sâu với bản trong HEAD 85a5391.
- **V1 + V2 thực tế trong Unity 2022.3.62f2:** Unity đã import sinh meta/biên dịch; sau người làm mở lại Editor, Test Runner → EditMode → Run All: tổng **229 = 131 cũ + 98 mới**, xanh nguyên văn **`229`**, đỏ **`0`**, chưa chạy **`0`**; không có test đỏ. Cả 31 case replay và corpus totals 276/4754 đều qua; 28 solution thắng, fixture Push.fx qua. **Không phát hiện lệch cần sửa Core trong corpus/fixture này; Core không đổi.**
- **Console trước/sau Run All:** danh sách trống; Log **`0`**, Warning **`0`**, Error **`0`**; không có lỗi/cảnh báo mới hoặc dòng lỗi/cảnh báo để chép. Không Clear Console. Log biên dịch trước khi mở lại có Tundra success 13.57 seconds/8 items updated; sau mở lại là `*** Tundra build success (0.13 seconds), 0 items updated, 358 evaluated`, không error CS/warning CS.
- **Kiểm phạm vi:** 42 file = 31 trace + 2 tool Node + README + PROJECT + phiếu + 3 C# mới/3 meta. Meta do Unity sinh, đủ cặp/không trùng GUID; git diff --check qua. Git diff của Core/prototype/level JSON/scene/package/settings/asmdef/quy tắc sạch; 131 test cũ không sửa. Không commit/push/merge.
- Khó khăn đã xử lý: lệnh compiler thủ công ngoài Editor thiếu facade netstandard 2.1 của UnityEngine (CS0012), nên không dùng nó để kết luận chạy được; đã biên dịch/chạy thật bằng Editor đúng reference. Cửa sổ Unity mất trong lúc chuẩn bị V2, công cụ mở lại chưa có cửa sổ; người làm đã mở lại và xác nhận sẵn sàng, sau đó mới chạy 229 test. Không suy đoán nguyên nhân mất cửa sổ, không sửa package/settings để lách.
- Lệch kế hoạch: bổ sung 28 solution và ngân sách ~3,6 MB theo review 005; thêm fixture Push.fx vì solution vẫn chưa tạo fx không rỗng trong corpus. Không cần sửa Core, không đổi luật/nguồn/đáp án hoặc làm yếu kiểm tra để đạt.

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- Việc này chứng minh C# giống prototype trong corpus, không chứng minh prototype đúng hoặc bao phủ mọi tổ hợp. 17–19 vẫn chưa biết minMoves.
- Phiếu 005 còn Chờ merge/Người làm cầm, không sửa phiếu cũ ngoài phạm vi; PROJECT cập nhật đã merge theo main và việc người làm giao 006.
- Đã xác nhận JsonUtility đọc level/trace và cổng null qua corpus thực tế trong Editor. Corpus vẫn chỉ có lệnh hiệu lực; no-op/bị chặn được kiểm ở 131 test cũ và test loader/fixture, không tự nhận đã so trace mọi lệnh không hiệu lực.
- Thêm solution vẫn chưa sinh Push.fx không rỗng trong corpus; đã bù bằng fixture C# JsonUtility lấy từ CW.move, không tuyên bố mọi tổ hợp tương tác đã được phủ.
- Chưa có solver C#/chưa so tối ưu bằng C# (007); chưa Play/visual/APK/hiệu năng. Golden replay chứng minh giống prototype trên 4754 bước này, không chứng minh prototype đúng trong mọi trường hợp.
- Tự tin khoảng 90% cho phạm vi 006 dựa trên Node verify/determinism, 31 replay × mọi chuỗi, 229 EditMode test và Console. Cần Claude soi validator/DTO và comparator có bỏ sót trường event nào không; không tự APPROVE.

## Bạn cần hiểu
- Loader đổi dữ liệu chữ thành kiểu Core; Core tiếp tục thuần C#, không phụ thuộc JsonUtility/UnityEngine.
- Trace là chuỗi đáp án chuẩn; phải so từng bước và thứ tự event, không chỉ kiểm có thắng.
- Đường lời giải giúp chạm các cơ chế mà random ngắn có thể bỏ qua; không đổi luật hoặc đáp án để ép test qua.
- Sự kiện Pyl.k và Push.fx.k trùng tên nhưng khác kiểu; DTO cần phân biệt để tránh mất dữ liệu âm thầm.

## Phản đối kế hoạch
- Chưa có phản đối chặn thực hiện. Rủi ro độ phủ random đã được review 005 nêu; bổ sung chuỗi solution theo yêu cầu đó trước khi kiểm C#.

--- Người review điền ---
## Verdict
Chưa review. Codex không tự APPROVE code mình viết.

## Trao đổi
2026-11-22 21:19 · Codex → Người làm · [BÁO]: Nhận 006, main sạch, 005 APPROVE đã merge; main/origin/main cùng 85a5391 sau fetch. Tạo 006-loader-trace --no-track. Nhận bổ sung 28 chuỗi solution theo review 005 (~≤3,6 MB), chỉ làm loader/replay/kiểm luật; chưa làm 007.
2026-11-22 21:35 · Codex → Người làm, Claude · [BÁO]: 006 đã nạp đủ 31 màn qua JsonUtility và replay C# khớp 276 chuỗi/4754 nước, không cần sửa Core. Thêm 28 solution nhưng giữ nguyên 248 random/4533 bước cũ, trace schema=2; Node verify qua 31/31, 3.207.205 byte; sinh lại 31 trace y hệt. Unity EditMode 229 xanh/0 đỏ/0 chưa chạy, Console Log 0/Warning 0/Error 0, không Clear. 42 file đúng phạm vi/đủ meta; Core/prototype/level JSON/scene/package/settings không đổi. Fixture Push.fx riêng cũng qua; chưa làm solver C#/007.
2026-11-22 21:35 · Codex → Claude · [BÀN GIAO]: Gậy Claude, review 006 trên 006-loader-trace, HEAD 85a5391, thay đổi chưa commit. Đã có loader + validator có tên nguồn, 31 golden replay so mọi trường/state/bất biến, test JsonUtility và Push.fx; Node 31/31, Unity 229 xanh, Console 0/0/0. Corpus 276 chuỗi/4754 nước, 28 solution mới theo review 005; không thay Core/nguồn/minMoves. Đề nghị soi DTO Pyl.k/fx.k, gate null, len bắt buộc và comparator từng event. Chưa solver C#/Play/Android/hiệu năng hoặc chứng minh mọi tổ hợp; chờ verdict, không tự APPROVE/commit/push/merge, không bắt đầu 007.
