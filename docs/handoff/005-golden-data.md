# 005 — Dữ liệu chuẩn prototype     Cỡ: M (≤ nửa ngày)     Trạng thái: Chờ merge
Đang cầm: Người làm        AI tham gia: Codex, Claude (review)
Liên quan: 002 (hoãn/gộp); 003–004; core-plan.md §4     Chờ nhận: —

## Mục tiêu
Có bộ dữ liệu và nước đi chuẩn từ prototype để việc 006 kiểm luật C# theo từng bước, việc 007 kiểm số nước ngắn nhất; chưa phải 25 màn cuối của game.

## Phạm vi — Làm / Không làm
- Làm: 31 level JSON dạng object, trace xác định 8 chuỗi/màn × tối đa 25 nước hợp lệ, tools export/make-traces/verify bằng Node có sẵn, README định dạng/cách chạy.
- Không làm: sửa prototype, Unity/Core/Game/test C#, JSON loader/solver C# (006–007), 25 màn cuối/Resources, package/settings/asset/Editor, commit/push/merge.
- Người làm giao 005 = duyệt core-plan §4. Node v24.14.1 có sẵn; Git sạch, main/origin/main sau fetch cùng 997ae60 (code 004 + review APPROVE đã có trong main); nhánh 005-golden-data tạo --no-track từ main.

## Các bước
1. Kiểm Git/004 APPROVE đã merge/Node; tạo nhánh, phiếu và sổ việc.
2. Đọc module logic.js/levels.js, metadata levels-export; chốt schema theo plan, mặc định tường minh để JsonUtility nạp được.
3. Viết export.js, make-traces.js, verify.js (CommonJS, chỉ Node builtin); gom helper trong các script này, không chia file cho từng hàm.
4. Sinh 31 JSON + 31 trace. PRNG seed cố định, không Math.random; giữ nguyên thứ tự sự kiện/kết quả nguồn.
5. Verify round-trip về prototype, solve maxStates=300000 cho 28 màn có minMoves; 17–19 giữ -1/chỉ kiểm nạp và phát lại trace, không gọi đó là vô nghiệm. So dữ liệu/sự kiện/state từng bước; chạy lại generator và so hash byte.
6. Kiểm dung lượng ~≤3 MB, Git chỉ đúng phạm vi và prototype không đổi; README/PROJECT/phiếu; bàn giao Claude và dừng, chưa làm 006.

## Tệp dự kiến
- docs/golden/tools/export.js, make-traces.js, verify.js.
- docs/golden/levels/level_01.json … level_31.json.
- docs/golden/traces/trace_01.json … trace_31.json.
- docs/golden/README.md; PROJECT.md §13/§14; phiếu này.
- Tất cả ngoài Assets: không có meta, không cần Editor/Unity Test Runner.

## Tiêu chí xong — V1 bằng Node
- verify.js in qua 31/31; nói rõ 28 màn so minMoves, 3 màn chưa có kết luận, mọi trace khớp.
- 31 level, mỗi mảng tọa độ/portal/chốt đã chuyển sang object, thiếu trường = mảng rỗng/false; len/mud/phase/fixed mặc định tường minh, need/màu/hướng giữ nguyên; minMoves null→-1. Không chuyển hint/tutorial.
- 31 trace, mỗi màn 8 chuỗi seed cố định, mỗi chuỗi ≤25 nước lấy từ nextMoves; đủ command/moved/events/state, giữ gate side/i/color và mọi trường sự kiện con.
- Chạy lại cả export/make-traces ra byte y hệt; không ghi thời gian hoặc kết quả đo vào dữ liệu sinh.
- Tổng docs/golden ~≤3 MB; git diff/status đúng file, prototype không đổi; không cần Unity vì không đụng Assets/C#.

## Điều cấm / rủi ro / thứ có thể cắt nếu trễ
- Không đổi luật/budget solver nguồn, không sửa minMoves bằng suy đoán, không cài npm package; chỉ dùng node:fs/path/assert (thêm crypto khi kiểm hash bằng lệnh).
- minMoves=-1 và solveExhausted=false ở 17–19 nghĩa là chưa chứng minh tối ưu, không phải không giải được; các đường thắng đã biết không được dùng như minMoves.
- Trace chọn nước hợp lệ nên không thay test no-op/bị chặn 003–004. Giữ cả sự kiện push.cars[].fx (k thay t), Tele và dữ liệu bùn/trụ/Moves.
- events.gate giữ object gồm side/i/color (hoặc null như nguồn); trace command dùng car là chỉ số xe, riêng kind=pyl là chỉ số trụ theo tuple nguồn; sign=0 cho rot/pyl. README phải nói rõ để 006 không hiểu nhầm.
- Ghi trace JSON compact để giữ dung lượng, level JSON dễ đọc; không nén hoặc bỏ trường state. Nếu vẫn quá ngân sách/trễ: plan cho giảm 5×20 hoặc bỏ trace 17–19, phải ghi lý do trước khi cắt.
- Time-box M ≤ nửa ngày; vượt gấp đôi dừng báo. Rủi ro chính là minMoves nguồn lệch hoặc round-trip mất cơ chế: nếu phát hiện thì ghi Phản đối kế hoạch/[HỎI], không sửa nguồn để cho qua.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch và vì sao
- Đã tạo 31 level object JSON, 31 trace, 3 tool Node và README. Giữ nguyên minMoves/solveExhausted từ metadata nguồn, ghi mặc định tường minh, chuyển tọa độ/portal/chốt sang object. Màn 17–19 giữ -1/false, không tự nhận đường thắng đã biết là tối ưu.
- export.js chứa cả chiều chuyển ngược và đối chiếu dữ liệu compiled (Set được chuyển thành mảng khi so); make-traces.js dùng xorshift32/seed cố định và kiểm state nguồn không bị sửa; verify.js đối chiếu dữ liệu, giải lại 28 màn, phát lại lời giải đến thắng, tái tạo kỳ vọng từ nguồn gốc và phát lại trace trên level đã round-trip.
- **Tệp đổi: 68 file** = 31 level + 31 trace + 3 scripts + README + phiếu + PROJECT.md. Không có meta hoặc thay đổi Assets/C#/package/settings/asmdef/quy tắc/prototype. PROJECT §13 ghi 004 đã merge, mở 005 và chuyển Chờ review; §14 thêm đường dẫn dữ liệu chuẩn/README/lệnh kiểm.
- `node --check` qua cả 3 script; `node docs/golden/tools/export.js` exit 0: `Đã xuất 31/31 level JSON object (minMoves chưa biết: 17, 18, 19 = -1).`
- `node docs/golden/tools/make-traces.js` exit 0: `Đã xuất 31/31 trace, 248 chuỗi, 4533 nước (tối đa 25/chuỗi).`
- **Kết quả verify cuối (exit 0, nguyên văn):** `QUA 31/31: 28 minMoves đã kiểm; 3 chưa kết luận; 248 chuỗi/4533 nước khớp; 3059936 byte.` Màn 16 khớp minMoves=9, explored=79056 với maxStates=300000. 17–19 chỉ kiểm nạp/trace đúng như plan, không chạy lại BFS để ép ra đáp án.
- **Xác định:** băm SHA256 62 JSON trước/sau khi chạy lại cả hai generator, so cả tên file và hash: `DETERMINISM: 62/62 JSON byte-identical; aggregate SHA256=92293c183cbef0e5c6cdcf781d32257d60ffb341a2126420b776c1a248dcd1af`. Aggregate băm chuỗi các dòng `đường/dẫn/tương/đối SHA256\n`, sắp theo tên, dấu phân cách đường dẫn `/`; chỉ gồm 62 JSON, không gồm tools/README.
- **Kiểm bắt lỗi:** gọi verifyLevelData/verifyTrace với bản sao JSON trong bộ nhớ: xóa phase, xóa fixed, đổi need, xóa truck, đổi cạnh portal, đổi minMoves, đổi seed, đổi state.moves, bỏ event, đảo sign. Kết quả `REJECTION CHECKS: 10/10 bad fixtures rejected; no files modified.` Không sửa dữ liệu thật để thử lỗi.
- **Dung lượng:** toàn docs/golden 3.059.936 byte ≈3,06 MB/2,92 MiB, dưới giới hạn 3 MiB của verifier, trong ngân sách ~3 MB; giữ đủ 8×25, không cắt trace 17–19. Không có giờ máy/thời gian đo trong JSON sinh.
- **Phân bố trace:** 4321 move + 64 rot + 148 pyl = 4533 nước; có đủ 12 loại sự kiện chính (Step/Stop/Exit/Spray/Water/Splash/Mud/Paint/Rotate/Push/Tele/Pyl). 14 Push, 167 Tele; chưa có hiệu ứng con fx của xe bị đẩy trong bộ ngẫu nhiên này, xem giới hạn bên dưới.
- Kiểm cuối Git: 68 file đúng phạm vi kể cả chưa theo dõi; git diff --check và whitespace các script/README qua; git diff --exit-code trên prototype/Assets/Packages/ProjectSettings/AGENTS/CODEX sạch. Chỉ V1 Node; không chạy Unity Console/Test Runner vì không đổi nội dung Unity.
- Lệch kế hoạch: không đổi phạm vi/luật, không thêm package, không cắt số chuỗi/số nước. Gate trong event giữ object side/i/color hoặc null như nguồn; car là chỉ số trụ khi kind=pyl, đã ghi README để 006 nạp đúng.

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- Dữ liệu 31 màn cũ chỉ dùng kiểm chứng; bản 25 màn chưa sinh. Chưa đối chiếu C# (006), chưa solver C# (007), không kiểm Unity/Android trong 005.
- Phiếu 004 vẫn Chờ merge/Người làm cầm; main đã có code/review và người làm giao 005 nên PROJECT cập nhật 004 Xong, không sửa phiếu 004 ngoài phạm vi.
- Chưa kiểm bằng C# hoặc JsonUtility trong Unity, chưa golden replay Core (006), chưa kiểm solver C# (007), chưa Play/Android/25 màn cuối.
- Trace chỉ chọn nước hợp lệ, không thay test no-op/bị chặn; dù có đủ 12 loại sự kiện chính, không chứng minh mọi tổ hợp. push.cars[].fx hiện đều rỗng: serializer vẫn giữ mọi trường k của fx khi có, nhưng bộ trace này chưa kiểm parser C# đọc hiệu ứng con đó. Test Core 003 đã có xe bị đẩy nhận hiệu ứng; cần người review 006 lưu ý khoảng trống dữ liệu này.
- Seed chuỗi là số unsigned 32 bit; nếu 006 đọc seed thì dùng uint/long, không ép mọi seed vào int có dấu. Replay thực tế dùng command đã lưu, không cần chạy lại PRNG ở C#.
- Tự tin khoảng 90% cho tiêu chí 005 dựa trên verify/round-trip/solve/replay, xác định 62 file và 10 kiểm bắt lỗi. Cần Claude soi schema tương thích DTO 006 và cách giữ event con; không tự APPROVE.

## Bạn cần hiểu
- Golden data là đáp án chuẩn của prototype, dùng phát hiện bản C# lệch luật; không phải thay luật để khớp dữ liệu.
- Seed cố định giúp lần chạy sau sinh đúng cùng nước đi, reviewer tìm lại đúng lỗi.
- -1 không phải số nước hay bằng chứng vô nghiệm; solver chưa chứng minh được thì giữ chưa biết.
- Trace lưu đủ state và thứ tự sự kiện vì animation cần diễn đúng chuỗi luật, không chỉ đúng kết quả cuối.

## Phản đối kế hoạch
- Chưa có phản đối chặn thực hiện. Chọn kiểm 28 minMoves đã biết, giữ 17–19 chưa kết luận đúng như plan; không tiêu thêm hàng trăm nghìn trạng thái cho 3 màn chưa biết trong bước verify thường lệ.

--- Người review điền ---
## Verdict: APPROVE (Claude, 2026-11-22)
**Đã soi và đã chạy:** Claude chạy `node docs/golden/tools/verify.js` → **QUA 31/31** (28 `minMoves` khớp, 3 màn `-1` chưa kết luận, 248 chuỗi/4533 nước khớp, 3.059.936 byte). Đọc README (định dạng khớp phiếu). Quét JSON: không mảng lồng, không mảng lẫn kiểu (thỏa `JsonUtility`); có 1435 `null`, toàn bộ trong sự kiện (vd `gate:null` ở `stop edge`), không ở dữ liệu màn. Độ phủ sự kiện: step 10485, stop 3367, exit 954, water 264, tele 167, pyl 148, splash 95, rotate 64, paint 50, spray 49, mud 24, push 14.
**Chưa soi:** từng dòng của 3 script `tools/`; việc C# đọc được JSON và phát lại (006); `null` trong sự kiện có làm `JsonUtility` lỗi không `[CHƯA KIỂM CHỨNG]`.
- [NÊN] **Độ phủ của chuỗi ngẫu nhiên mỏng ở vài cơ chế.** Chuỗi ngẫu nhiên 25 nước thường chỉ chạm `exit`: màn 2, 6, 9, 10, 22, 23, 26, 28 hầu như không có sự kiện đặc trưng (vòi xịt, sơn, barie, đèn, chốt chỉ hiện gián tiếp qua trạng thái). `push` chỉ 14 lần ở 3 màn và chưa có `fx` không rỗng (xe bị đẩy nhận sơn/bùn). Đây là điểm yếu do kế hoạch của Claude (chuỗi ngẫu nhiên), không phải lỗi của Codex. Cách sửa rẻ: thêm một chuỗi **theo đường lời giải ngắn nhất** (`CW.solve`) cho mỗi màn đã có `minMoves`; chuỗi này đi qua các cơ chế mà màn được thiết kế để dùng. Xin làm ở bước đầu của 006 (chạy lại `make-traces.js`, vẫn phải xác định, tổng ≤ ~3,6 MB).
- [GỢI Ý] Trace chỉ có nước **có hiệu lực** (`moved=true` cả 4533), vì sinh từ `nextMoves`; các nước không hiệu lực (bị chặn) chỉ có test đơn vị của 003/004 chứ không có trace. Chấp nhận.
- [GỢI Ý] Chuỗi ngẫu nhiên và `verify.js` đều dựa vào chính `logic.js`, nên chứng minh C# **giống prototype**, không chứng minh prototype đúng. Đúng với mục tiêu của lõi.

## Trao đổi
2026-11-22 20:45 · Codex → Người làm · [BÁO]: Nhận 005; main sạch, sau fetch main/origin/main cùng 997ae60, đã có 004 APPROVE và merge. Tạo 005-golden-data --no-track, Node v24.14.1 sẵn có. Chỉ làm dữ liệu/tool ngoài Assets, không điều khiển Unity, không bắt đầu 006.
2026-11-22 20:54 · Codex → Người làm, Claude · [BÁO]: Sinh đủ 31 level + 31 trace, 248 chuỗi/4533 nước. Verify Node qua 31/31: 28 minMoves khớp, 17–19 giữ -1/chưa kết luận; 3.059.936 byte (~2,92 MiB). Chạy lại export/make-traces cho 62/62 JSON byte-identical; 10/10 mẫu sửa sai trong bộ nhớ bị bắt. Không đổi prototype/Assets/C#/package/settings, không cần V2 Unity. Trace có đủ 12 sự kiện chính nhưng chưa có push.fx không rỗng, đã ghi giới hạn.
2026-11-22 20:54 · Codex → Claude · [BÀN GIAO]: Gậy Claude, review 005 trên nhánh 005-golden-data, HEAD 997ae60, dữ liệu/tool/docs mới chưa commit. Đã làm đúng plan 8 chuỗi × tối đa 25 nước/màn, 3 tools và README schema. V1 Node: verify 31/31, 248 chuỗi/4533 nước khớp, 28 minMoves đã so; 62 JSON sinh lại y hệt; Git 68 file đúng phạm vi, dữ liệu ~3,06 MB. Chưa replay C#/JsonUtility/solver C#/Android; lưu ý gate object/null, fx.k, car là chỉ số trụ khi pyl, seed uint32, chưa có push.fx không rỗng. Chờ verdict; không tự APPROVE/commit/push/merge, không bắt đầu 006.
2026-11-22 20:57 · Claude → Người làm, Codex · [BÀN GIAO]: Review 005 xong, verdict APPROVE (Claude đã chạy verify.js: QUA 31/31). Người làm: commit phần review, merge 005 vào main. Codex: ở bước đầu của 006, thêm cho mỗi màn đã có minMoves một chuỗi theo đường lời giải ngắn nhất (CW.solve) vào trace (sửa make-traces.js, chạy lại, giữ xác định, tổng dung lượng ≤ ~3,6 MB), vì chuỗi ngẫu nhiên phủ mỏng vòi xịt, sơn, barie, đèn, chốt và push.fx; ghi vào phiếu 006.
