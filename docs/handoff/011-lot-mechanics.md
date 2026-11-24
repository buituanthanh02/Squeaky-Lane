# 011 — Bãi xe và cơ chế ra hình · L ≤ 1,5 ngày · APPROVE có điều kiện (P1)
Đang cầm: Người làm · Nhánh 011-lot-mechanics đã có, cùng main 26a59d0 (đã gồm010) · AI: Codex (code), Claude (visual/review)
Liên quan: 010 (cách làm CarCatalog) · play-plan §4 hàng 011 · PROJECT §5.1 (cơ chế bậc A/B)

## Mục tiêu (người chơi)
Nhìn bãi là hiểu luật: mỗi cơ chế bậc A có hình riêng, đọc rõ ở camera 70° trên điện thoại. Bãi trông như bãi xe thật, không còn khối xám. Luật, input và animation không đổi.

## Cách làm
- **`MechanicCatalog`** (ScriptableObject, giống `CarCatalog`): loại cơ chế → prefab. `BoardView` dựng qua catalog. Thiếu prefab thì dùng khối cũ và cảnh báo một lần. Prefab có trạng thái (đèn, trụ xoay, bàn xoay) có một component nhỏ nhận trạng thái từ `BoardView`, không tự đọc luật.
- **Nguồn hình** (người làm duyệt nhập Kenney CC0, 25/11): Car Kit đã có `cone.fbx`. City Kit (Roads), Racing Kit, Particle Pack do người làm tải về. **Claude chốt danh sách model cụ thể sau khi xem thư mục FBX.** Chỉ nhập file dùng tới vào `Assets/_Project/Art/Mechanics/Kenney/`.
- Thứ không có sẵn thì **dựng từ khối đơn giản** bằng menu Editor (như `KenneyCarsWindow`): cổng rửa, vũng, bàn xoay, portal, trụ xoay.
- Mọi màu và kích thước nằm trong config hoặc catalog, không viết cứng.

## Hình từng cơ chế (Claude quyết; số là điểm xuất phát, chỉnh theo ảnh)
| Cơ chế | Hình | Số |
|---|---|---|
| Mặt bãi | Nhựa đường, vạch ô trắng mờ, bó vỉa quanh bãi | nền `#3A3F47` · vạch rộng 0,03 ô, alpha 0,6 · bó vỉa `#C9CED6`, cao 0,15 |
| Cổng màu | Vòm ở mép bãi, hai cột + xà, rèm sọc đúng màu xe | cao 0,9 · cột 0,12 · màu từ `VisualPalette` |
| Cổng nước | Vòm trắng, rèm xanh nước, 2–3 vòi trên xà | `#4FC3F7` + trắng |
| Vũng bùn | Đĩa dẹt méo, hơi bóng | `#5B3A1E` · bán kính 0,42 ô · cao 0,02 |
| Cọc | `cone.fbx` của Car Kit | cao ~0,45 ô |
| Đèn giao thông | Cột đèn (City Kit nếu có) **+ vạch dừng trên mặt đất** đổi đỏ/xanh theo pha | đỏ `#E53935` · xanh `#43A047` · vạch rộng 0,12 ô |
| Bàn xoay 2×2 | Đĩa xám, vòng mũi tên trắng (nút XOAY giữ nguyên) | đường kính 1,9 ô · `#8A93A3` |
| Portal | Khung cửa trên tường, vòng sáng; mỗi cặp một màu riêng, không trùng màu xe | cặp 1 `#00E5FF` · cặp 2 `#FF4081` · cặp 3 `#C6FF00` |
| Trụ xoay | Cột thấp + tay chắn chỉ hướng đang chặn | `#F9A825` sọc đen · tay dài 0,9 ô |
| Xe tải, xe | Giữ như 010 | — |

**Bậc B** (ô vòi xịt, bùn văng, vũng sơn), barie, chốt: giữ khối cũ. Vũng sơn nếu kịp thì dùng chung hình vũng bùn, đổi màu.

## Model đã chốt (Claude, 25/11, xem preview trong thư mục kit)
Phong cách: **City Kit Roads và Car Kit** cùng dòng (colormap gradient) → ưu tiên. Racing Kit kiểu phẳng trắng/đỏ, chỉ dùng cho khung cổng và tay chắn; vật liệu gán lại trong prefab, không dùng material nhúng của FBX.

| Cơ chế | Model | Ghi chú |
|---|---|---|
| Cọc | Car Kit `cone.fbx` (chép từ `Downloads/kenney_car-kit`) | Cùng colormap với xe |
| Cổng màu | Racing `overhead` | Khung vuông. Tô bằng 6 material màu xe + rèm sọc tự dựng |
| Cổng nước | Racing `overheadRound` | **Khung tròn để khác dáng cổng màu xanh dương**, tô trắng + rèm `#4FC3F7` + vòi tự dựng |
| Đèn giao thông | City `traffic-light` | Thêm đèn đỏ/xanh tự dựng trên đầu cột (bóng Kenney cố định màu) + vạch dừng mặt đất |
| Trụ xoay | City `construction-light` làm cột + Racing `barrierWall` làm tay chắn | Trông như barie cần gạt, tay dài 0,9 ô |
| Barie/chốt (chưa vào 25 màn) | City `construction-barrier` | Chỉ khi còn thời gian |
| Nhà nền (P3) | Racing `treeSmall`, `fenceStraight`; City `light-square`, `dumpster` | Đặt ngoài bãi, không che xe |
| Mặt bãi, bó vỉa, vũng, bàn xoay, portal | Tự dựng | Mặt bãi **một mesh** cho cả bãi, không mỗi ô một object |

**Dọn sau khi dựng xong prefab** (người làm duyệt xóa, xóa trong cửa sổ Project): giữ City `traffic-light`, `construction-light`, `construction-barrier`, `light-square`, `dumpster` + `Textures/variation-a.png`; giữ Racing `overhead`, `overheadRound`, `barrierWall`, `treeSmall`, `fenceStraight` + `Textures/net.png`. Xóa phần còn lại (khoảng 195 FBX) **trước khi commit**.

## Thứ tự (P1 bắt buộc)
1. P1 `MechanicCatalog` + fallback + test (thiếu prefab → khối, cảnh báo 1 lần).
2. P1 Mặt bãi + bó vỉa (không cần bộ mới).
3. P1 Cổng màu, cổng nước, vũng bùn, cọc, bàn xoay (dựng khối + `cone.fbx`).
4. P1 Đèn (+ vạch dừng), trụ xoay, portal.
5. P2 Thay bằng model City Kit/Racing Kit theo bảng "Model đã chốt" (đã chốt 25/11).
6. P3 Nhà nền quanh bãi (cắt đầu tiên).

## Kiểm
Run All + Console sạch. Play: màn có đèn (đổi pha sau mỗi lượt), 08 bàn xoay, một màn portal, một màn trụ xoay, một màn bùn → cổng nước → cổng màu. Ảnh Game view 3 màn gửi Claude. Stats so với 010 (màn 16: 302 batches). Nếu batches tăng mạnh: gộp mặt bãi thành một mesh, bật GPU instancing.

## Điều dễ sai
Mỗi ô một GameObject mặt đất (hàng trăm draw call) · tạo material mới lúc chạy · vật cao che xe ở góc 70° (giữ cao ≤ 0,9) · màu portal trùng màu xe làm người chơi tưởng là cổng màu · prefab tự đọc luật thay vì nhận trạng thái từ `BoardView`.

Báo cáo ~15 dòng; Trao đổi chỉ `[BÀN GIAO]`; không tự commit.

## Báo cáo Codex (P1 bước 1→4)
Phạm vi: catalog/fallback/test → mặt bãi/bó vỉa → cổng/nước/bùn/cone/bàn xoay → đèn/trụ/portal; chưa làm P2/P3, không tự commit.
Code: MechanicCatalog/MechanicView/LotSurfaceView/LotMechanicsWindow/MechanicOcclusion; nối ConfigProvider/GameBootstrap/BoardView/CarView bounds và preview LevelEditorWindow.
Config mới: MechanicCatalog.entries (loại/biến thể/prefab), design (màu/kích thước/chi tiết hình); số mặc định theo bảng Claude, màu cổng từ VisualPalette.
Tạo hình bằng Tools/Squeaky Lane/Lot Mechanics: catalog → chọn riêng Downloads/kenney_car-kit/Models/FBX format/cone.fbx (CC0) → tạo16 prefab P1; reuse colormap010, không nhập OBJ/GLB/kit khác.
Nền/vạch/bó vỉa: một MeshRenderer/mesh ba submesh/material; mesh thích nghi Rows/Cols khi nạp màn và được hủy theo view; chừa bó vỉa tại cổng, giữ mặt Y=-0.01 khớp xe010.
Cổng màu vuông/rèm sọc; nước vòm tròn/trắng/rèm xanh/vòi; vũng bùn méo dẹt; cone scale đều; bàn xoay vòng/mũi tên thuận chiều Core, nút XOAY giữ nguyên.
Đèn: LightDef không có hướng làn nên vạch đỏ/xanh bao ô, cùng pha LightRule. [CHỐT chat theo ảnh29/30] Làm đúng cổng tele xoay (Pylon): một khung cửa tròn3D dựng đứng ở tâm, hai mặt A/B cùng cửa quay90°, có độ dày để không biến mất khi nhìn cạnh; đĩa thấp/mũi tên ở chân chỉ hướng xe vào. Khung cao0.84/dày0.14, đáy0.035 (tổng0.875), số trong catalog. Portal/cổng che xe dùng material asset mờ alpha0.18, hiện rõ lại khi hết che; giữ nhãn/màu cặp.
Các phần tĩnh gộp mesh theo material, đèn/trụ giữ riêng nhóm có trạng thái; chỉ sharedMaterial asset, không tạo material runtime, không collider che raycast xe.
Fallback: thiếu/sai prefab/root/kind/trạng thái → khối cũ; chỉ báo một lần/Board, không báo cho test/preview cũ cố ý không truyền catalog; bậcB/barie/chốt giữ nguyên.
Kiểm trước sửa hình: ảnh Run All518/0/1; hai warning đúng test LogAssert.Expect (đã đối chiếu log). Người làm báo các chức năng/kiểm Play còn lại đều ổn, riêng hướng nhận trụ và portal che xe cần sửa. Bản sửa thêm6 ca: miệng A/B theo Core bốn hướng, che xe camera70°, sharedMaterial/disable; Roslyn5 assembly0 lỗi, chưa Run All mới.
[Editor cần làm] Thoát Play → Refresh → Lot Mechanics nút4 “Cập nhật cổng tele xoay + chống che” cập nhật11 prefab trụ/cổng (backup trước, giữ GUID/catalog, không đụng5 prefab khác) → Run All → Clear Console/Play29,30 + xoay4 lần/Undo, kiểm xe đi khỏi vùng che thì cổng rõ lại.
Ảnh đã có29/30 và Stats16:156 Batches/66SetPass/57.7FPS, Screen568×320,49.1k tris,42 shadow casters; mốc010=302/77/59.5 ở676×380, khác độ phân giải nên không kết luận thay đổi FPS. Cần ảnh29/30 sau sửa để Claude soi hướng nhận/che xe.
Bạn cần hiểu: catalog chọn hình, Core giữ luật; nền chỉ sinh mesh lúc nạp, không mỗi frame; chỉnh design xong bấm dựng lại (backup Logs/011-prefab-backups), prefab sửa tay sẽ bị thay khi chọn dựng lại.
Chưa kiểm/rủi ro: chống che dùng bounds hình chiếu nên có thể mờ sớm ở mép; không cấp phát list/material mỗi frame, chỉ đổi bộ sharedMaterial khi đổi trạng thái, cần đo lại Stats sau sửa. Màu portal gần Pink phân biệt bằng khung/vòng/nhãn;432 asset City/Racing đã stage giữ nguyên. ~85% tự tin code, chưa review/V2/V3 bản sửa; dọn kit qua Project trước commit, P2/P3 chưa làm.
## Verdict (Claude, 25/11)
**APPROVE CÓ ĐIỀU KIỆN cho P1.** Claude không sửa code.
Đã soi: `MechanicCatalog` (enum thêm cuối, trùng loại báo lỗi, chặn vật cao > 0,9) · `MechanicView` chỉ nhận trạng thái, `Usable` kiểm root/collider · `BoardView` dựng qua catalog, mọi loại có fallback khối, đèn đổi pha qua `SetRed` · `LotSurfaceView` một mesh 3 submesh, khớp tọa độ ô, hủy mesh khi đổi màn · `MechanicOcclusion` đổi giữa 2 bộ material asset, không tạo material lúc chạy, không cấp phát mỗi frame · prefab Pylon/Portal dựng lại 01:38 sau code 01:37 (có "Vòng cửa dựng đứng").
Editor.log: ~150 `NullReferenceException` ở `GameBootstrap` OnEnable/LateUpdate/OnDisable là do **biên dịch lại script khi đang Play** (field không serialize như `tableButtons` mất, Awake không chạy lại). Không phải lỗi build. Các lần nạp sau 12030 sạch.
Chưa soi kỹ: `LotMechanicsWindow` (531 dòng, chỉ Editor), test chỉ đọc lướt tên.

Điều kiện trước merge:
1. Run All sau bản cuối (test 01:31, prefab 01:38), Console sạch trong một lần Play mới.
2. Gửi Claude ảnh: màn 29 hoặc 30 (cổng tele xoay), một màn portal, một màn có cổng màu che xe. Claude duyệt hình rồi mới merge.
3. [NÊN, đổi số, không đổi code] `portalColors` → `#FFFFFF`, `#C6FF00`, `#00E5FF`: bỏ `#FF4081` vì gần màu xe hồng (lỗi bảng màu của Claude). Dựng lại prefab portal.

[GỢI Ý] `RefreshOcclusion` tính bounds mỗi xe lại cho từng vật che; đảo vòng (bounds xe một lần mỗi frame) nếu Profiler thấy tốn. Field ẩn `pylonHeight/armLength…` trong Design là thừa vì asset mới tạo ở nhánh này; bỏ được, không gấp.
P2 (model City/Racing): **quyết sau khi Claude xem ảnh**. Nếu hình tự dựng đã đọc rõ thì bỏ P2, sang 012 và build APK.
[Mẹo Editor cho người làm] Preferences → General → Script Changes While Playing = "Recompile After Finished Playing" để tránh loạt lỗi giả trên.

## Trao đổi
2026-11-25 01:02 · Codex → Người làm · [BÀN GIAO]: P1 bước1→4 đã có code/menu/test, nhánh011-lot-mechanics/HEAD26a59d0; chưa commit/stage phần Codex, giữ432 tệp City/Racing đã stage từ trước. Roslyn5 assembly0 lỗi; chưa Unity Run All/Console/Play/ảnh, chưa chuyển Claude review. Editor: Refresh → Tools/Squeaky Lane/Lot Mechanics → nút1/2/3 (chỉ cone.fbx) → gán GameRoot.Mechanic Catalog → Run All/Console sạch → Play10/08/21/29/05 hoặc11, Undo/Play lại; ảnh10/21/30 và Stats16 so302/77/59.5 ở676×380. P2/P3 chưa làm; dọn kit qua Project theo whitelist đã duyệt trước commit.
2026-11-25 01:17 · Codex → Người làm · [BÀN GIAO]: Ảnh Run All518 passed/0 failed/1 skipped. Đã đọc stack Editor.log: cả2 warning là LogAssert.Expect trong CarCatalogIntegrationTests.MissingCatalogEntriesUseBlocksAndWarnOnlyOnceAcrossLevelReloads và MechanicCatalogTests.MissingOrInvalidMechanicsFallBackAndWarnOnceAcrossReloads; giữ nguyên test/cảnh báo. Đã có16 entry và Play.unity gán đúng catalog GUID. Tiếp Clear Console → Play10, kiểm đổi pha/Console; còn các màn/ảnh/Stats trong báo cáo. Chưa commit.
2026-11-25 01:20 · Codex → Người làm · [BÀN GIAO]: Người làm xác nhận bước Clear Console/Play màn10 ổn trong chat; chưa có ảnh mới để đánh giá visual/Stats. Tiếp màn08 bàn xoay, Undo giữa lúc chạy, rồi21 portal/29 trụ/05 hoặc11 bùn; còn ảnh10/21/30 và Stats16. Chưa commit.
2026-11-25 01:33 · Codex → Người làm · [BÀN GIAO]: Người làm báo chức năng đều ổn, gửiảnh29/30 và Stats16=156/66/57.7 ở568×320; yêu cầu miệng nhận trụ rõ hướng và sửa cổng che xe. Đã sửa menu nút4: vòng trụ thấp/hai miệng A-B/mũi tên vào tâm; cổng/portal dùng material asset alpha0.18 khi bounds hình chiếu che xe, phục hồi khi hết che; cache renderer/không tạo material runtime. Thêm6 ca test hướng nhận bốn pha/occlusion70/sharedMaterial+disable, Roslyn5 assembly0 lỗi. Chưa cập nhật asset/Run All mới/ảnh sửa: người làm thoát Play → Refresh → nút4 → Run All → Clear Console/Play29–30, xoay/Undo và kiểm cổng rõ lại khi xe rời; gửi2 ảnh mới. Giữ432 asset đã stage, không commit.
2026-11-25 01:38 · Codex → Người làm · [BÀN GIAO]: Người làm làm rõ đang nói cổng tele xoay (Pylon29/30); bản vòng thấp/ký hiệu trước chưa ra hình cửa. Đã đổi thành một khung cửa tròn3D dựng đứng ở tâm, hai mặt A/B và toàn khung xoay90; thêm độ dày0.14 để nhìn cạnh vẫn thấy, cao0.84+chân0.035=0.875. Đĩa/mũi tên phía dưới giữ hướng vào; khung cũng mờ khi che xe, mũi tên/nhãn giữ rõ. Roslyn0 lỗi; chưa chạy Unity bản mới. Thoát Play/Refresh → nút4 “Cập nhật cổng tele xoay + chống che” → Run All/Console → Play29/30; chưa commit.
2026-11-25 01:40 · Codex → Claude, Người làm · [BÀN GIAO]: Người làm yêu cầu tạm giữ hiện trạng và lấy nội dung commit để Claude tiếp tục. Chuyển gậy Claude, mốc WIP011-lot-mechanics/HEAD26a59d0, Codex chưa commit/stage phần mới. Đã làm P1/menu16 prefab/catalog/fallback; ảnh Run All518/0/1 và người làm xác nhận chức năng ổn TRƯỚC sửa visual cuối. Code mới nhất đổi Pylon thành cổng tele tròn dựng đứng tự quay + chống che bằng material asset, Roslyn0 lỗi; CHƯA xác nhận prefab đã dựng lại/Run All/Console/ảnh mới. Stats16=156/66/57.7 ở568×320. Claude đọc diff và phiếu; giữ nhãn WIP, không coi đủ điều kiện merge. Còn lọc City/Racing qua Project theo whitelist (432 tệp stage sẵn), P2/P3 chưa làm; không xóa/stage tất cả tự động.
2026-11-25 01:45 · Claude → Người làm, Codex · [BÁO]: Kiểm tham chiếu GUID: trong 2 kit City/Racing hiện KHÔNG prefab/asset/scene nào dùng; chỉ Car Kit cone.fbx đang dùng. Hai kit đã nằm trong commit c48dd34 (573 file). Người làm duyệt xóa phần thừa nhưng muốn giữ tới khi chắc không cần: CHƯA xóa. Dọn ở cuối 011 (sau P2) hoặc 014, trong cửa sổ Project, chỉ giữ model prefab dùng thật (kiểm lại GUID trước khi xóa). APK không bị ảnh hưởng vì asset không tham chiếu không vào build.
2026-11-25 01:46 · Claude → Người làm, Codex · [BÀN GIAO]: Review 011 P1 xong: APPROVE CÓ ĐIỀU KIỆN (xem Verdict). Người làm cầm: Run All bản cuối, Play mới Console sạch, gửi Claude 3 ảnh (29/30, portal, cổng che xe). Codex: đổi portalColors sang #FFFFFF/#C6FF00/#00E5FF và dựng lại prefab portal; P2 chờ Claude xem ảnh. Lỗi NullReference trong Editor.log là do biên dịch lại khi đang Play, không phải lỗi code.
2026-11-25 01:50 · Claude → Người làm, Codex · [BÁO]: Đã xem ảnh màn 28/29/30: điều kiện ảnh của 011 đạt (luật đọc được, Run All 518 xanh, Console 0). Còn đổi portalColors. Phần nhìn chuyển sang phiếu 011-01 (làm tay trong Editor).
