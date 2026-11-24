# Kế hoạch "chơi được" (việc 008 trở đi) — thay thế core-plan.md mục 7–9
Lập 2026-11-22 bởi Claude theo yêu cầu người làm: **ít việc hơn, mỗi việc lớn hơn, thấy game chạy trên màn hình sớm.** Lõi luật (003–006) đã xong và khớp prototype; 007 (solver) đã review/merge, phần cấp phát đo tiếp ở 008. Từ 008, mỗi nhánh là **một cụm có thể chơi/nghe/nhìn được**, Claude **review một lần ở cuối nhánh** (không review từng bước nhỏ).
Người làm gửi prompt "làm theo play-plan" = **duyệt kế hoạch** việc 008; 009 và sau đó duyệt khi bắt đầu.

## 0. Quy trình (giữ phần cốt lõi, bỏ phần rườm)
- Đọc `PROJECT.md` → `AGENTS.md` → `CODEX.md` → file này; chat mới theo `AGENTS.md` §1.6.
- Mỗi việc: BƯỚC 0 như 001 (git sạch, nhánh `NNN-tên` từ `main` bằng `--no-track`, phiếu từ mục tương ứng, sổ việc). **Trong nhánh được commit nhiều lần theo mốc** (A, B, C…) để có chỗ quay lại; người làm quyết định commit.
- **Kiểm chứng:** biên dịch sạch, Console sạch, Test Runner xanh (test Core/Game/golden **không được đỏ** khi sửa bất cứ gì), báo nguyên văn. Phần nhìn/nghe/cảm giác: người làm chơi thử (Editor và điện thoại) theo **danh sách chơi thử** ở cuối mỗi việc; Codex chụp ảnh/quay Game view khi điều khiển được Editor.
- Xong thì bàn giao Claude một lần: [BÀN GIAO] + nội dung commit. Không bắt đầu việc kế khi chưa APPROVE và merge.
- Phản đối kế hoạch: ghi vào phiếu và `[HỎI]` **trước khi làm**.
- Số cảm giác (thời gian bước, âm lượng, góc camera…) nằm trong config, **không hard-code**.

## 1. Quyết định thiết kế cho cụm "chơi được" `[ĐỀ XUẤT]`, Codex có thể phản đối
- **Thư mục/assembly mới:** `Assets/_Project/Scripts/Presentation/` (hình, âm, rung, hiệu ứng) và `Scripts/UI/` dùng **một asmdef `SqueakyLane.Presentation`** tham chiếu Core, Game, `Unity.TextMeshPro`, `UnityEngine.UI`. (Tạo asmdef mới đã được duyệt cho việc 008.) Quy ước chung: gom kiểu nhỏ; **MonoBehaviour một class một file**.
- **Scene mỏng** `Assets/_Project/Scenes/Play.unity`: chỉ Main Camera, Directional Light, `GameRoot` (có `GameBootstrap`), `EventSystem`, Canvas. Mọi thứ khác **dựng bằng code từ màn JSON** lúc chạy. Thêm Play vào Build Settings (đây là thay đổi Build Settings nhỏ đã được duyệt).
- **Dữ liệu màn lúc chạy:** `Assets/_Project/Resources/Levels/level_NN.json` + `level_order.json` (danh sách id theo thứ tự). Menu `Tools/Squeaky Lane/Sync Levels` chép 31 mẫu golden khi chưa có nguồn thiết kế. **009-D đã viết theo kế hoạch duyệt 23/11:** cửa sổ Level Editor sửa/xem trước ngoài Play, lưu vào `Assets/_Project/LevelDesign/`, xuất JSON/order vào Resources; Sync dùng nguồn thiết kế sau khi khởi tạo để không chép golden đè màn sửa. Bộ golden giữ nguyên để kiểm luật. Người làm đã thử preview/Nhân bản/Undo/Lưu/Export/Play, tạo các bản sao32–34; EditMode352Passed/0Failed/1khôngchạy. Chờ Claude review, còn UI/V3 Android. **25 màn cuối chọn sau** (việc 012).
- **Input:** Input Manager cũ (dự án không có Input System). Dùng `Input.GetMouseButton*` (Unity mô phỏng chuột từ cảm ứng nên chạy cả Editor và điện thoại) hoặc `Input.touches`; một lớp `InputController` đổi chạm → **lệnh** (`MoveCar`, `RotateTable`, `RotatePylon`), không đụng luật.
- **Camera:** trực giao, **nghiêng 65°** (số trong `CameraConfig`), `orthographicSize` tính từ `Cols/Rows` và `Screen.safeArea` để cả bãi nằm vừa màn dọc.
- **Hình khối xám:** xe = hộp (dài `Len` ô, rộng ~0,8 ô, cao ~0,5 ô), màu theo `TileColor` qua `VisualPalette` (ScriptableObject: màu hex mỗi `TileColor`, màu nền, màu cổng). Cọc, vũng bùn, vòi xịt, vũng sơn, bàn xoay, đèn, barie, chốt, portal, trụ xoay = khối/đĩa đơn giản có màu riêng, **đọc được luật trước, đẹp sau** (`CLAUDE.md` §4). Xe Kenney và camera/ánh sáng đẹp làm ở việc 010.

## 2. Việc 008 — Chơi được trên màn hình (cỡ L, ~1 ngày) · nhánh `008-playable`
**Mục tiêu (người chơi):** mở Editor/điện thoại, chọn màn 1–31, chạm để xe chạy đúng luật, hoàn tác, thắng hoặc hết giờ thì thấy thông báo.
**Mốc A — Game logic thuần (có test, chưa cần scene)**
1. `GameSession` (`Game/`): giữ `LevelDef`, `GameState`, **ngăn xếp hoàn tác** (giữ trạng thái cũ, không giới hạn), `Apply(MoveCar|RotateTable|RotatePylon)` → `MoveResult`, `Undo()`, `Restart()`, `IsWin`.
2. `LevelTimer` (nhận `dt` từ ngoài, `Paused`, `ChargeUndo()`, `ChargeHint()`, `StarsOnWin()` theo `PROJECT.md` §6.4; màn `timeLimit <= 0` không tính giờ, 3★).
3. `StuckDetector` (dùng `WinSearch` của 007, chạy theo ngân sách mỗi frame, trạng thái `Unknown/Winnable/Stuck`, **hủy tìm kiếm cũ khi hoàn tác/đi tiếp**) và `HintProvider` (dùng `PathSearch`, giới hạn số lần theo màn).
4. `GameConfig : ScriptableObject` (`Assets/_Project/Config/`): `BaseSeconds=20`, `SecondsPerMove=5`, `UndoPenalty=3`, `HintsPerLevel=3`, `HintPenalty=5`, `Star3Ratio=0.5`, `Star2Ratio=0.2`, `TargetFps=60`, `TutorialLevelsUntimed=0` (số màn đầu không tính giờ, chỉnh sau); `OnValidate` chặn giá trị sai; `FeelConfig` (số tạm: `StepSeconds=0.10`, `ExitSeconds=0.25`, `RotateSeconds=0.25`, `TeleSeconds=0.20`, `BumpSeconds=0.08`); `CameraConfig` (`TiltDegrees=65`, `Padding=0.5`). `ConfigProvider` đọc qua một chỗ. Ghi `config mới: tên · field · vì sao` vào phiếu.
5. **Test:** hoàn tác nhiều lần về trạng thái đầu; hoàn tác trừ giờ; hết giờ; tạm dừng; ngưỡng sao 50%/20% (biên); màn không tính giờ; gợi ý trừ lượt và giờ; `Stuck` ở trạng thái kẹt dựng tay; `OnValidate`. Golden/solver test cũ vẫn xanh.
**Mốc B — Hiển thị bãi và xe**
6. `Play.unity` mỏng + `GameBootstrap` nạp `level_order.json`, dựng màn đang chọn: sàn lưới, viền và cổng màu ở mép (chữ số nhỏ hoặc vạch màu), xe, cọc, vũng bùn, vòi xịt, vũng sơn, bàn xoay, đèn, barie, chốt, portal, trụ xoay. `BoardView`, `CarView` (một class một file), `VisualPalette`.
7. Camera 65° trực giao căn theo bãi và `safeArea`; kiểm màn 1 (4×5) và màn 17 (7×7, 15 xe) đều vừa màn dọc.
**Mốc C — Điều khiển, chuyển động, hoàn tác**
8. `InputController`: (a) **chạm nửa xe** (raycast vào xe → điểm trúng chiếu lên **trục thân xe trong không gian bàn chơi** → so với tâm xe), (b) **chạm vào xe rồi vuốt** theo hướng (chiếu lên trục thân, ngưỡng vài chục pixel, đơn vị theo `Screen.dpi` hoặc phần trăm cạnh ngắn, đưa vào config), (c) **nút ở tâm bàn xoay** (vùng chạm ≥ ~9 mm, **ưu tiên hơn xe**), (d) chạm trụ xoay. Chạm UI không xuyên xuống bãi.
9. `ActionAnimator`: đọc **danh sách sự kiện** từ `MoveResult` (Core không biết hình) và chạy hoạt hình cho từng xe: `step` (trượt từng ô), `stop` (nảy nhẹ), `exit` (chạy ra cổng rồi biến mất), `water` (chui vào cổng nước rồi dội), `mud/paint/spray` (đổi màu/độ bẩn ngay tại ô), `splash`, `push` (xe tải và xe bị đẩy), `tele` (biến mất ở cổng A, hiện ở cổng B), `rotate` (bàn xoay 90°), `pyl`. **Nhiều xe chạy song song:** lệnh mới được nhận ngay; Core tính ngay, hình **đặt chỗ đường đi** để xe không chồng hình (mỗi xe một hàng đợi). Không cấp phát mỗi frame (cache, danh sách tái dùng).
10. **Hoàn tác và chơi lại:** hủy mọi hoạt hình đang chạy, dựng lại hình từ `GameState` cũ (không "tua ngược" hoạt hình). Nút tạm: Undo, Restart, Hint, chọn màn trước/sau (bản **gỡ lỗi**, giao diện đẹp ở 009).
11. Thắng: `IsWin` → hiện chữ "Thắng" + số sao (tạm); hết giờ → "Hết giờ"; kẹt → chữ "Kẹt xe" không chặn. Đồng hồ chỉ **tạm dừng khi thắng/hết giờ**, **kẹt vẫn chạy** (người làm xác nhận 23/11, khớp PROJECT §6.4 và mục 3.1 dưới đây).
**Điều dễ làm sai:** chạm nửa xe tính theo nửa màn hình thay vì trục thân xe; vuốt chéo; nút bàn xoay bị xe nuốt chạm; hoàn tác giữa lúc hoạt hình (xe lệch ô); hết giờ vẫn nhận lệnh; `Time.deltaTime` truyền vào timer khi `timeScale=0` (quyết định có chủ đích scaled/unscaled, ghi vào phiếu); `Stuck` tính tiếp sau khi người chơi đã hoàn tác; static sót qua lần nạp lại màn; `Find*`/`GetComponent` trong `Update`; ScriptableObject bị sửa lúc chạy; camera lệch trên màn dài/ngắn; TextMeshPro mặc định **thiếu chữ có dấu tiếng Việt** (giữ chữ HUD ngắn, kiểm glyph, nếu thiếu thì tạo font atlas dynamic có tiếng Việt, không đổi package).
**Tiêu chí xong:** Test Runner xanh toàn bộ (nêu số); Console sạch; menu Sync Levels chạy được; **người làm chơi thật** (xem danh sách dưới) trên Editor, và nếu có thể trên một điện thoại.
**Danh sách chơi thử (người làm):** (1) mở màn 1, thắng bằng cách chạm; (2) chạm đầu xe và đuôi xe cho xe chạy đúng hướng; (3) vuốt dọc thân xe; (4) hoàn tác giữa lúc xe đang chạy, xe về đúng ô; (5) màn 3 có cổng nước: xe vào được xịt rồi dội ngược; (6) màn 8 bàn xoay: nút tâm bấm được, không bấm nhầm xe; (7) màn 21 portal; (8) màn 29 trụ xoay; (9) màn 20 xe tải đẩy; (10) để hết giờ thấy "Hết giờ"; (11) dựng kẹt xe thấy báo "Kẹt xe"; (12) Hint hiện nước đi và trừ lượt.
**Cắt nếu trễ:** bỏ Hint trong 008 (làm ở 009); bỏ vuốt (giữ chạm nửa xe); đơn giản hóa hoạt hình `water/tele/pyl`. **Không cắt:** hoàn tác, đồng hồ, thắng/thua, input chạm nửa xe.

## 3. Việc 009 — Âm thanh, rung, giao diện (cỡ L, ~1 ngày) · nhánh `009-audio-ui`
**Mục tiêu:** nghe và rung khi chơi; giao diện đủ dùng cho bản demo. **VFX (hạt, rung camera, chuyển động "nhân vật" của xe) để việc 011.**
1. **SFX:** `SfxCatalog` (ScriptableObject): sự kiện → `AudioClip` + âm lượng + biến thể ngẫu nhiên nhẹ. `SfxPlayer` dùng **pool AudioSource** (không tạo mới mỗi lần). Sự kiện cần tiếng: chạm chọn xe, xe bắt đầu chạy, bước (động cơ nhẹ, có thể gộp), **dừng/va nhẹ** (`stop` theo lý do: car/wall), **ra cổng + rửa** (`exit`), **cổng nước** (`water`), **bùn** (`mud`), **vòi xịt** (`spray`), **sơn** (`paint`), **đẩy** (`push`), **dịch chuyển** (`tele`), **bàn xoay/trụ xoay**, **hoàn tác**, **nút UI**, **thắng**, **thua/hết giờ**, **kẹt xe**, **đếm ngược khi sắp hết giờ**. Âm lượng chung/SFX nằm trong `FeelConfig`; có công tắc bật/tắt âm.
2. **Nguồn âm thanh:** cần người làm tải và duyệt (asset ngoài). Đề xuất bộ **CC0** của Kenney (Interface Sounds, Impact Sounds…) `[CHƯA KIỂM CHỨNG tên gói]`; Codex ghi tên file, nguồn, giấy phép, dung lượng vào phiếu **trước khi** thêm vào repo. Giữ tổng âm thanh nhỏ (build < 250 MB). Chưa có nhạc nền.
3. **Rung (haptics):** lớp `Haptics` (Presentation): `#if UNITY_ANDROID && !UNITY_EDITOR`; rung ngắn khi dừng va chạm, rung nhẹ khi ra cổng, rung dài hơn khi thắng/thua; cường độ/độ dài trong `FeelConfig`; công tắc bật/tắt. Dùng `Handheld.Vibrate()` làm dự phòng; rung có độ dài (API `VibrationEffect`) cần quyền `VIBRATE`, **min API hiện là 22 nên phải kiểm API và dự phòng** `[CHƯA KIỂM CHỨNG: cần thử trên máy thật]`. Đổi Project Settings/Manifest nếu cần là việc **phải hỏi**.
4. **HUD và hộp thoại (uGUI + TMP, `safeArea`):** thanh/số đồng hồ (đổi màu khi < 20%), số nước đi, nút **Hoàn tác** (hiện phạt giờ), **Chơi lại**, **Gợi ý** (hiện số lần còn lại), tên màn; hộp thoại **Thắng** (3 sao, nút Màn kế/Chơi lại), **Hết giờ**, **Kẹt xe** (Hoàn tác/Chơi lại); **chọn màn đơn giản** (lưới nút 1–31, màn đã qua, chưa cần lưu tiến trình) để thay nút gỡ lỗi của 008. Chữ HUD **ngắn**, có dấu tiếng Việt hiển thị đúng. Vùng chạm nút ≥ ~9 mm.
5. **Công tắc Cài đặt tối thiểu:** bật/tắt âm, bật/tắt rung (lưu `PlayerPrefs`, đọc qua một chỗ). Không làm menu, shop, meta (ngoài phạm vi `PROJECT.md` §1).
**Điều dễ làm sai:** tạo `AudioSource`/`GameObject` mỗi lần phát (GC); tiếng chồng lên nhau quá nhiều khi nhiều xe chạy (giới hạn số tiếng cùng loại); rung liên tục làm nóng/khó chịu (giới hạn tần suất); UI che bãi hoặc ăn chạm xuống bàn chơi; quên `safeArea`; nút nhỏ hơn 9 mm; `PlayerPrefs` đọc rải rác; thêm package/quyền Android mà chưa hỏi.
**Tiêu chí xong:** Test Runner xanh; Console sạch; người làm chơi từ màn 1 đến một màn khó trên Editor và **điện thoại**, **nghe đủ âm, thấy rung**, UI không che bãi, đọc được chữ tiếng Việt; ghi **fps và nhiệt máy** sau 10 phút chơi (số liệu thật, dù chưa đạt cũng ghi).
**Cắt nếu trễ:** bớt tiếng biến thể, bớt nút (giữ Hoàn tác, Chơi lại, Thắng/Thua), bỏ công tắc lưu `PlayerPrefs`.

### 3.1. Ý tưởng điều hướng giữa các màn hình (làm riêng thành **mốc 009-A**, trước UI và âm thanh)
**Mục tiêu:** người chơi đi trọn một vòng *chọn màn → chơi → thắng/thua → màn kế/chơi lại/về chọn màn* mà không có ngõ cụt, và code điều hướng **tách khỏi luật chơi, tách khỏi hình** để test được và đổi giao diện không phải sửa luồng. Chỉ **một scene** (`Play.unity`); mỗi "màn hình" là một panel trong Canvas, bật/tắt bằng `CanvasGroup` (không nạp scene, không giật).
**Các màn hình và hộp thoại**
| Tên | Loại | Nội dung | Đồng hồ |
|---|---|---|---|
| `LevelSelect` | toàn màn | lưới nút 1–N, hiện sao đã đạt (và khóa nếu bật), nút cài đặt âm/rung | — |
| `Playing` | toàn màn | bãi + HUD (đồng hồ, nước đi, Hoàn tác, Gợi ý, Chơi lại, Tạm dừng) | chạy |
| `Paused` | hộp thoại | Tiếp tục · Chơi lại · Về chọn màn · công tắc âm/rung | **dừng** |
| `Won` | hộp thoại | 1–3 sao, thời gian còn lại; **Màn kế** · Chơi lại · Về chọn màn | **dừng** |
| `Lost` | hộp thoại | "Hết giờ"; **Chơi lại** · Về chọn màn | **dừng** |
| `Stuck` | **biểu ngữ không chặn** (không phải hộp thoại) | "Kẹt xe", làm sáng nút Hoàn tác và Chơi lại | **vẫn chạy** (`PROJECT.md` §6.4) |
**Sơ đồ chuyển trạng thái** (`Navigator`, C# thuần, **không** `UnityEngine`; đặt ở `Game/`; có test EditMode):
- Khởi động → `LevelSelect`. `LevelSelect` + chọn màn *k* → `Playing(k)`.
- `Playing` + bấm Tạm dừng hoặc phím Back/Escape → `Paused`; `Paused` + Tiếp tục hoặc Back → `Playing`.
- `Playing` + thắng → `Won`; hết giờ → `Lost`; kẹt xe → hiện biểu ngữ `Stuck` (vẫn ở `Playing`); hoàn tác khỏi trạng thái kẹt → tắt biểu ngữ.
- `Won` + Màn kế → `Playing(k+1)` (nếu hết màn: về `LevelSelect`); Chơi lại → `Playing(k)`; Về chọn màn → `LevelSelect`.
- `Lost` + Chơi lại → `Playing(k)`; Về chọn màn → `LevelSelect`.
- Back/Escape ở `LevelSelect`: không làm gì (không thoát game bất ngờ).
- Mọi chuyển trạng thái có **lệnh ra** (ví dụ `StartLevel(id)`, `RestartLevel`, `PauseClock`, `ResumeClock`, `ShowDialog(kind)`), do `GameBootstrap` thực hiện; `Navigator` không biết `GameSession` hay Unity.
**Quy tắc:** khi có hộp thoại mở thì `InputController` **không nhận chạm xuống bãi**; chuyển hộp thoại xong mới nhận lại. Chuyển màn dùng hiệu ứng mờ ngắn (0,15–0,25 s, số trong `FeelConfig`); hủy mọi hoạt hình đang chạy trước khi nạp màn mới.
**Tiến trình (nhẹ, một chỗ):** `ProgressStore` (Game, bọc `PlayerPrefs`) lưu số sao tốt nhất mỗi màn. Mặc định `UnlockAll = true` trong `GameConfig` để anh lead thử màn nào cũng được; đặt `false` thì màn *k+1* mở khi thắng màn *k*. Không tài khoản, không đám mây.
**Phạm vi:** không làm menu chính, shop, meta (ngoài `PROJECT.md` §1); `LevelSelect` chỉ là lưới nút.
**Test (EditMode, không cần scene):** đi hết sơ đồ trên bằng chuỗi sự kiện và kiểm trạng thái + lệnh ra; Back ở từng trạng thái; thắng ở màn cuối về `LevelSelect`; `Stuck` không đổi trạng thái chính; `ProgressStore` giữ sao tốt nhất (không ghi đè bằng kết quả thấp hơn).
**Điều dễ làm sai:** để mỗi panel tự gọi panel kia (mạng nhện); bấm Chơi lại khi hoạt hình còn chạy; đồng hồ chạy tiếp lúc `Paused`; nhận chạm xuyên hộp thoại; Back thoát hẳn game; `Stuck` dùng hộp thoại chặn khiến đồng hồ ngừng sai luật.
**Ranh giới với 008:** 008 chỉ có nút gỡ lỗi (Undo, Restart, Hint, màn trước/sau) và gọi thẳng `GameSession`; **008 không viết điều hướng**. 009-A thêm `Navigator` + panel rồi **thay** các nút gỡ lỗi; không sửa luật Core hay `GameSession` (nếu thấy cần sửa, ghi [HỎI]).
**Thứ tự trong 009:** 009-A điều hướng (logic + test) → 009-B panel/HUD/UI → 009-C SFX + rung.

## 4. Các việc sau (chỉ phác, chi tiết khi tới)
| Mã | Việc | Ghi chú |
|---|---|---|
| 010 | Xe Kenney (mỗi màu một dáng), bảng màu hồng/tím, camera/ánh sáng đẹp, chống che (mờ vật cao) | Nhập asset **qua Unity**, gọn trong `Assets/_Project/Art/Cars/Kenney/`, bỏ OBJ/GLB/ảnh xem trước không dùng (xóa asset cần duyệt) |
| 011 | **Bãi xe và cơ chế ra hình** (cập nhật 25/11): mặt bãi (nhựa đường, vạch đỗ, viền), hình cho cơ chế bậc A (§5.1): cổng màu, cổng nước, vũng bùn, cọc, đèn, bàn xoay, portal, trụ xoay. `MechanicCatalog` giống `CarCatalog` (cơ chế → prefab), thiếu thì dùng khối cũ | Kenney CC0 cùng phong cách xe (City Kit Roads, Racing Kit…): chỉ nhập model dùng tới. Thứ không có sẵn (cổng rửa, bàn xoay, portal, vũng) dựng từ khối đơn giản theo bảng màu. Bậc B và nhà nền quanh bãi cắt trước |
| 012 | **Cảm giác lõi** `[CHỐT 25/11: ưu tiên gameplay + visual, tutorial xuống sau]`: xe là nhân vật bằng ngôn ngữ cơ thể (nhún khi chạy, chúi + nảy khi phanh/va, rướn khi ra cổng, rũ bùn), bánh quay, khoảnh khắc rửa ở cổng nước/cổng màu (hạt nước, bọt, lấp lánh), rung màn hình nhỏ, ăn mừng thắng | Phiếu `012-core-feel.md`. Số trong `FeelConfig`. VFX từ Particle Pack (Kenney CC0, PNG Transparent). Claude chốt số theo video |
| 013 | **Khung nhìn** (gộp phần đáng giá của 011-01): camera bãi to hơn, cảnh quanh bãi (vỉa hè/cỏ/cây/đèn đường), nền camera không còn xanh đậm trống, bỏ chữ nổi trên bãi (gợi ý chỉ thẳng vào xe), chữ/màu nút HUD | Xem bảng V1–V7 trong `011-01-visual-hand-pass.md`. **APK đầu tiên khi 013 xong (26/11)** |
| 014 | **25 màn chơi**: chọn và sắp 25 màn từ 31 màn có sẵn theo đường cong khó, bỏ màn thử 32–34 và màn dùng barie/chốt, chỉnh giờ theo chơi thử | Dùng Level Editor + solver; `minMoves` từ golden |
| 015 | **Tutorial gọn**: 3 màn đầu có bàn tay/mũi tên chỉ thao tác + một dòng chữ; giới thiệu cơ chế mới bằng một khung hình ở màn đầu tiên có cơ chế đó | Bắt buộc theo yêu cầu lead, nhưng làm đơn giản. Cắt phần khung giới thiệu cơ chế nếu trễ, không cắt 3 màn đầu |
| 016 | Build APK cuối, đo fps (50–60), nhiệt, dung lượng, sửa lỗi | Android: package name, Portrait, IL2CPP + ARM64 |

### 4.1. Tích hợp prefab xe ngoài — bước bắt buộc trong 010 `[CHỐT 23/11]`
Người làm nhắc: nhập asset không đồng nghĩa prefab đã dùng được trong game. Làm theo chuỗi sau, không thay trực tiếp model vào scene rồi coi xong:
1. Kiểm nguồn/giấy phép, mesh, renderer/vật liệu, texture và dung lượng; thống nhất gói được phép thêm. Không nhập toàn bộ file dư vào build.
2. Tạo **prefab bọc** cho game (giữ model nguồn làm con/nested prefab). Root có pivot giữa thân xe theo chuẩn bàn chơi; trục dọc thân là +X ở tư thế ngang, local rotation/scale của root là 0/1. Bù hướng/kích thước/độ cao ở model con, không sửa quy ước tọa độ của Core. Xe thường khớp 2×1 ô, xe tải 3×1 ô; kiểm khoảng hở giữa ô và thân.
3. Collider/vùng chọn theo footprint lưới và CarView ở root; cache renderer, nối màu/vật liệu và phần bùn riêng. Chỉ rõ shader dùng `_Color`, `_BaseColor` hay material variant; không tự tạo material mới mỗi frame để đổi màu.
4. Tạo CarCatalog ánh xạ màu/loại xe → prefab + dữ liệu hiển thị. Thay điểm dựng xe khối trong BoardView bằng catalog, giữ luồng Core → events → CarView/ActionAnimator. Nếu thiếu prefab phải báo rõ hoặc dùng fallback khối; không rải Resources.Load hay hard-code tên model.
5. Kiểm spawn/pivot, chạm đầu/đuôi, chạy tới/lùi, ra cổng, Undo khi đang chạy, bàn xoay, portal đổi hướng và xe tải đẩy; reload/vào Play lại. Đo draw call/cấp phát/fps trên máy đích trước khi nhận là đã tích hợp xong; Claude duyệt visual qua ảnh/video.

**Ghi chú kiểm 008:** người làm đã thử chạm được và duyệt commit mốc đầu. Cần sửa vuốt ở lượt sau commit: vượt ngưỡng là ra lệnh khi đang giữ tay, chỉ một lệnh mỗi gesture, nhả tay không phát lại. Đây vẫn thuộc 008, không bắt đầu 009.

## 5. Lịch (cập nhật 25/11 — ưu tiên gameplay + visual)
| Ngày | Việc |
|---|---|
| 22–24/11 | 007–009 xong |
| 25/11 | 010, 011 xong và merge · bắt đầu 012 cảm giác lõi |
| 26/11 | 012 xong · 013 khung nhìn · **APK đầu tiên tối 26/11** |
| 27/11 | 014 25 màn |
| 28/11 | 015 tutorial gọn · sửa theo APK |
| 29/11 | 016 APK cuối, đo, sửa lỗi |
| 30/11 | Đánh bóng, video demo, chốt tài liệu; học 5 mục "Học sau" (phiếu 011-01) nếu chưa kịp |
**Thứ tự cắt** (từ dưới lên): asset đẹp → VFX phụ → Gợi ý → công tắc cài đặt → tutorial bằng hình. **Không cắt:** chơi được 25 màn, hoàn tác, đồng hồ, thắng/thua, âm thanh, rung (yêu cầu bắt buộc của anh lead: APK hoàn chỉnh, ≥ 25 màn, âm thanh + rung, không crash, 50–60 fps, < 250 MB, máy không nóng).
