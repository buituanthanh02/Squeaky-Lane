# 004 — Core luật phần 2     Cỡ: M (≤ nửa ngày)     Trạng thái: Chờ review
Đang cầm: Claude        AI tham gia: Codex
Liên quan: 003; core-plan.md §1/§3     Chờ nhận: —

## Mục tiêu
Xe dùng được portal/trụ xoay và các vật cản động theo đúng prototype; Core trả đủ trạng thái/sự kiện để phần hiển thị, hoàn tác và solver dùng chung.

## Phạm vi — Làm / Không làm
- Làm: portal gắn tường, hai mặt cổng của trụ theo hướng hiện tại, RotatePylon, barie đếm xe/BarriersOpened, SwitchPressed và chốt A/B; nối Occupancy/MoveResolver; test EditMode cơ chế mới và hồi quy 003.
- Không làm: JSON/golden/solver (005–007), Game/session/config, visual/MonoBehaviour/scene/prefab, package/settings/asmdef, sửa prototype, commit/push/merge.
- Người làm báo 003 đã APPROVE/merge và giao 004 = duyệt mục 3 trong core-plan.md. Main sạch và có cả commit code 0241c3b + review 86cb2e3; sau fetch main/origin/main cùng 86cb2e3. Nhánh mới tạo từ main bằng --no-track.

## Các bước
1. Kiểm main sạch/003 APPROVE đã merge, tạo nhánh 004-core-rules-2, phiếu và sổ việc.
2. Đọc lại từng hàm portalTry/pylonFaces/faceList/switchPressed/barriersOpened, nhánh vật cản và tryPush trong logic.js.
3. Port theo thứ tự vật cản động/trụ → portal → nối Move; giữ hợp đồng model và thứ tự sự kiện 003.
4. Test portal đổi trục/hai chiều/không đủ thân/chỉ 1 lần; trụ bốn hướng/fixed/nối hai mặt; barie theo số xe rửa; chốt ở đầu lượt; tải đẩy qua barie/đèn đỏ bị chặn. Giữ 74 test 003.
5. Unity import sinh meta, biên dịch/Console và Test Runner EditMode Run All; kiểm git status/meta/diff.
6. Sửa cây PROJECT §8 và sổ việc §13, điền phiếu; chuyển gậy Claude + [BÀN GIAO], đưa commit đề xuất và dừng review. Chưa bắt đầu 005.

## Tệp dự kiến (Editor khi import/test)
- Mới: Core/Rules/PortalRule.cs, PylonRule.cs, BarrierRule.cs. Các helper/enum nhỏ đặt cùng file cơ chế, không tách vụn.
- Sửa: Core/Rules/MoveResolver.cs, Occupancy.cs; Core/Model/GameState.cs (setter nội bộ cho bản sao hướng trụ, không đổi hợp đồng công khai).
- Mới: Tests/EditMode/PortalPylonTests.cs, BarrierSwitchTests.cs; chỉ bổ sung test vào file cũ nếu cần.
- PROJECT.md §8/§13; phiếu này. Đường dẫn code/test dưới Assets/_Project/.
- [Editor] Unity tự sinh meta cho file mới; không sửa nội dung GUID, không đổi/xóa asset cũ.

## Tiêu chí xong — V1 + V2
- Mỗi cơ chế ≥2 test (qua/bị chặn), portal ngang↔dọc và tối đa 1 lần/lượt; trụ xoay 4 lần về cũ, fixed không xoay, hướng nối đúng; barie mở theo Need; chốt dùng vị trí đầu lượt; tải đẩy bị chặn bởi barie/đèn đỏ.
- State đầu vào bất biến, no-op giữ tham chiếu; Moves/event/recordEvents=false nhất quán; 74 test cũ không yếu đi.
- Đã đối chiếu từng hàm nguồn được yêu cầu. Chưa có golden trace, không tuyên bố khớp 31 màn.
- Unity biên dịch sạch; Console không lỗi/cảnh báo mới; toàn bộ EditMode qua, ghi tổng/xanh/đỏ và nội dung lỗi nguyên văn nếu có.
- Core không UnityEngine/LINQ/chuỗi trong đường nóng/static mutable. Git chỉ có file thuộc phiếu, đủ meta.

## Điều cấm / rủi ro / thứ có thể cắt nếu trễ
- Không đổi enum/model công khai, luật prototype, package/settings/asmdef/scene/prefab. Không tự commit/push/merge.
- Portal: đầu xe thay đổi theo dấu chạy, ô đích là đuôi xe, kiểm cả thân bằng occupancy đầu lượt; hiệu ứng đích theo thứ tự đuôi → đầu; một lần chui kể cả khi đổi trục/nước dội. Thứ tự mặt trùng phải theo faceList: portal thường trước trụ, mỗi link đi rồi về.
- Trụ: không cache mặt cổng theo LevelDef vì hướng đổi theo GameState. Phương án đơn giản là duyệt cặp cổng/trụ đang có, không tạo faceList/mảng/chuỗi mỗi bước. Đánh đổi: duyệt tuyến tính; hiệu năng cần đo ở 007.
- Chốt/barie/đèn: occupancy chính chụp đầu lệnh; tryPush dựng occupancy từ state hiện tại như prototype, không ép hai nơi dùng cùng một snapshot.
- Fixed: logic.js rotatePylon không tự chặn fixed, nhưng nextMoves lọc fixed; core-plan yêu cầu fixed không xoay. Core sẽ chặn ngay trong API RotatePylon theo kế hoạch đã duyệt, ghi rõ khác biệt bảo vệ đầu vào này.
- Time-box M ≤ nửa ngày; vượt gấp đôi dừng báo. Không cắt test/luật; có thể giảm báo cáo phụ, không làm trước 005.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch và vì sao
- PortalRule nối portal thường và các mặt trụ hai chiều; tính đầu xe theo trục/dấu, kiểm đủ thân đích bằng snapshot occupancy, đổi trục/dấu chạy, áp hiệu ứng đuôi → đầu, tối đa 1 lần/lượt. Chặn portal đích không dùng được như tường; giữ thứ tự liên kết trùng của faceList.
- PylonRule trả hai mặt theo hướng trong state, xoay +1 modulo 4, không bị xe cạnh chặn; fixed/missing trả no-op. BarrierRule kiểm xe đè chốt và trả các barie vừa vượt ngưỡng Need. Occupancy bổ sung trụ/barie/chốt trước khi ghi xe; MoveResolver thêm hai chỗ portalTry (mép bãi và trụ).
- GameState chỉ thêm setter internal cho bản sao hướng trụ; không đổi API/model công khai, enum hoặc hợp đồng MoveResult. recordEvents=false vẫn dùng chung đường tính luật. Không tạo cache/static mutable hay file cho từng kiểu nhỏ.
- **Tệp đổi (15, gồm meta):** mới 3 Rules (PortalRule/PylonRule/BarrierRule) + 3 meta; mới 2 test (PortalPylonTests/BarrierSwitchTests) + 2 meta; sửa GameState/MoveResolver/Occupancy; PROJECT.md và phiếu này. Meta mới do Unity import sinh. Không đổi scene/prefab/package/settings/asmdef, không xóa/đổi tên asset cũ.
- PROJECT.md §8 thêm đúng ba file Rules mới; §13 đánh dấu 003 đã merge theo người làm/main và mở 004, chuyển 004 Chờ review. Không chèn trạng thái kiểm thử tạm vào phần kiến trúc.
- **V1 CLI (trước hai test bổ sung cuối):** Roslyn/Mono của Unity + NUnit sẵn có, runner/DLL tạm ngoài repo: `Core và test: compile OK, không có cảnh báo compiler.`; `NUnit (Mono CLI): Passed=129, Failed=0, Skipped=0, Inconclusive=0`.
- Sau V1 thêm 2 test kiểm riêng tryPush dùng trạng thái chốt hiện tại sau khi tải tiến gần (khác snapshot bản đồ chính); chạy trọn bộ cuối trong Unity, không suy diễn từ 129 test CLI.
- **V2 Unity 2022.3.62f2 (20:35):** import/biên dịch xong; DLL Core/Tests cập nhật 20:34; log nguyên văn `*** Tundra build success (1.72 seconds), 11 items updated, 358 evaluated`, không error CS/warning CS. Test Runner **EditMode → Run All**: tổng **131 = 74 cũ + 57 mới**; bộ đếm xanh **`131`**, đỏ **`0`**, chưa chạy **`0`**; không có test đỏ.
- **Console trước và sau Run All:** danh sách trống; Log **`0`**, Warning **`0`**, Error **`0`**. Không có lỗi/cảnh báo mới hoặc dòng lỗi/cảnh báo để chép; không Clear Console.
- Kiểm cuối: git status đủ 15 file đúng phạm vi; script mới đủ meta, không meta mồ côi/trùng GUID trong _Project; Core không UnityEngine/LINQ/static mutable; noEngineReferences=true và test assembly chỉ Editor không đổi; git diff --check qua.
- Lệch kế hoạch: không thêm/gỡ luật; bỏ cache faceList của JS, duyệt có thứ tự trực tiếp để tránh giữ sai hướng trụ và cấp phát list/khóa chuỗi. Chặn fixed ngay tại RotatePylon theo core-plan thay vì chỉ ở caller như prototype. Hai khác biệt này đã ghi trước khi viết; không phải đổi gameplay đã duyệt.

## Đã đối chiếu logic.js
**Đã đối chiếu từng hàm** trong `docs/reference/prototype/src/logic.js`, gồm cả các nhánh bên gọi:

| Hàm nguồn | Đã so với Core |
|---|---|
| `portalTry` (trong move) | PortalRule.TryMove: đầu xe khi chạy ± theo cả hai trục; khớp cả ô và cạnh; tìm link trước khi xét đã chui; đích tính theo vector ngược mặt tường; mọi ô thân phải trong bãi/trống; cập nhật góc thân/trục/dấu; Tele trước các hiệu ứng đuôi → đầu. Cờ teleported nằm trong MoveResolver cho cả lượt, không reset khi nước dội; teleport tăng steps nên tính một lượt. |
| `pylonFaces`, PYL_SIDES/PYL_FACE | PylonRule.PylonFaces: mặt 0 N→W→S→E, mặt 1 đối diện; tọa độ là ô cạnh trụ, cạnh của ô nhìn về trụ. Test đủ 4 hướng và cả hai đầu nối. Model dùng turns 0..3; chuyển chữ h/v của JSON để loader 006, không thêm xử lý JSON trong 004. |
| `faceList` | PortalRule.TryDestination/TryLink: portal thường theo thứ tự dữ liệu trước, mỗi link A→B rồi B→A; tiếp theo từng trụ, link mặt 0/to[0] rồi mặt 1/to[1]. Hướng đọc từ state mỗi lần; không cache bằng chuỗi. Link đầu tiên khớp thắng, kể cả đích nó bị chặn; không thử link khác để lách vật cản. |
| `rotatePylon`, `nextMoves` | RotatePylon clone state, hướng (q+1)&3, Moves+1, event Pyl chỉ số trụ; không kiểm xe sát trụ. Bảo vệ fixed ở API theo plan; JS lọc fixed trong nextMoves, hàm rotatePylon gốc không tự lọc. Missing trả MoveResult.Unchanged theo hợp đồng Core thay cho null. |
| `switchPressed` | BarrierRule.SwitchPressed kiểm mọi ô thân xe sống, không chỉ ô đầu; vẫn xét xe đang skip khỏi occupancy; xe đã ra bãi không đè chốt. |
| `barriersOpened` | BarrierRule.BarriersOpened dùng đúng before < Need và after >= Need, giữ thứ tự dữ liệu; không báo lại barie đã mở trước đó. |
| `occupancy`, `tryPush` | Thứ tự cọc → trụ → barie chưa đủ xe rửa → đèn đỏ → nhóm A/B đóng → xe; A đóng khi chốt có xe, B đóng khi không. Map chính chụp st0; tryPush dựng map từ state hiện tại sau các bước tiến, giữ đúng prototype (2 test riêng). Xe ghi đè vật cản như JS; các ô vật cản mới ngoài bãi được bỏ qua. |
| Các nhánh `move` gọi portalTry | Mép bãi thử portal trước cổng màu/nước; portal blocked dừng Wall. Ô vật cản chỉ thử portal khi là ô trụ, không biến barie/cọc thành portal. Giữ gate/water/stop/push/splash/effects và guard 80 của 003; 74 test cũ đều qua. |

- Đây là đối chiếu tĩnh + test màn nhỏ; chưa có dữ liệu golden để chứng minh khớp mọi trace/31 màn (005–006).

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- Golden trace/loader/solver chưa có, kiểm qua test màn nhỏ và đối chiếu tĩnh; đo GC/hiệu năng ở 007.
- PROJECT dòng 003 được cập nhật Xong theo main + xác nhận merge của người làm. Phiếu 003 còn Chờ merge/Người làm cầm; không sửa phiếu việc cũ ngoài phạm vi.
- Chưa Play/Android/visual/fps; không tuyên bố có phiên chơi được. Core mới được kiểm qua 131 test EditMode, biên dịch và Console trong Editor.
- Duyệt tuyến tính portal/trụ là lựa chọn đơn giản hiện tại; chưa đo chi phí ở solver. MoveEvent 15 trường và Copy 3 mảng/1 state vẫn giữ như 003, cần đo ở 007; không tối ưu phòng xa.
- Tự tin khoảng 90% cho phạm vi 004 dựa trên đối chiếu từng hàm + 131 test + Editor. Cần Claude soi hướng/đầu nối trùng và hợp đồng sự kiện Tele/Pyl; golden trace 006 vẫn là bước kiểm khớp đầy đủ.

## Bạn cần hiểu
- Portal đặt đủ thân xe từ đuôi sát tường; hướng mặt tường đích quyết định trục và chiều chạy mới.
- Trụ là tường có hai mặt cổng thay đổi theo state; xoay trụ không làm xoay xe đứng cạnh.
- Bản đồ cản chính lấy ở đầu lệnh; riêng tryPush kiểm lại state hiện tại khi tải bắt đầu đẩy, đúng prototype. Không gộp hai thời điểm này thành một.
- State cũ không đổi giúp Undo và solver dùng lại an toàn; tắt ghi event không được đổi kết quả luật.

## Phản đối kế hoạch
- Chưa có phản đối chặn thực hiện. Rủi ro đã ghi trên; không cache faceList và không chia nhỏ kiểu dữ liệu là lựa chọn cài đặt trong phạm vi, không đổi luật.

--- Người review điền ---
## Verdict
Chưa review. Codex không tự APPROVE code mình viết.

## Trao đổi
2026-11-22 20:26 · Codex → Người làm · [BÁO]: Nhận 004 sau khi main sạch và 003 APPROVE đã merge. Fetch xong main/origin/main cùng 86cb2e3; tạo 004-core-rules-2 từ main bằng --no-track. Gậy Codex, chỉ làm 004; giữ file gọn theo cơ chế, không bắt đầu 005.
2026-11-22 20:36 · Codex → Người làm, Claude · [BÁO]: Đã port portal/trụ/barie/chốt và đối chiếu từng hàm nguồn trong bảng. Unity 2022.3.62f2 đã import sinh meta/biên dịch; Test Runner EditMode tổng 131 (74 cũ + 57 mới), xanh 131, đỏ 0, chưa chạy 0; Console trước/sau Log 0/Warning 0/Error 0, không Clear. 15 file đúng phạm vi, đủ meta; không đổi scene/package/settings/asmdef. Không có lỗi cần sửa sau lượt test Editor.
2026-11-22 20:36 · Codex → Claude · [BÀN GIAO]: Gậy chuyển Claude, review 004 trên 004-core-rules-2, HEAD 86cb2e3; code/docs mới chưa commit. Đã làm portal hai chiều/đổi trục/1 lần, trụ 4 hướng/fixed, barie/chốt và nối map/Move; 131 EditMode xanh/0 đỏ, Console 0 lỗi/cảnh báo mới, đủ meta. Chưa Play/Android/hiệu năng/golden trace; lưu ý tryPush dùng state hiện tại còn map chính chụp đầu lượt, faceList được thay bằng duyệt tuyến tính giữ thứ tự. Đề nghị soi hướng/đầu nối trùng và sự kiện Tele/Pyl. Không tự APPROVE/commit/push/merge; dừng chờ review, không bắt đầu 005.
