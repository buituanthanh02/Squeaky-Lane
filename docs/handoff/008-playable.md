# 008 — Chơi được trên màn hình     Cỡ: L (~1 ngày)     Trạng thái: Đang làm
Đang cầm: Codex        AI tham gia: Codex
Liên quan: 003–007; play-plan.md §2     Chờ nhận: —

## Mục tiêu
Chọn một trong 31 màn thử, chạm xe để chơi, hoàn tác, dùng đồng hồ/gợi ý và thấy kết quả ngay trong Game view. Dựng khối đơn giản để kiểm luật và input trước khi làm art.

## Phạm vi — Làm / Không làm
- A: GameSession, LevelTimer, StuckDetector, HintProvider; GameConfig/FeelConfig/CameraConfig, ConfigProvider; test và đo tìm kiếm tích hợp.
- B: asmdef SqueakyLane.Presentation dùng chung Presentation/UI → Core, Game, TMP, uGUI; menu Sync Levels từ docs/golden/levels sang Resources, level_order; VisualPalette, BoardView/CarView; camera 65° căn safeArea; prefab và scene Play.unity mỏng, thêm Build Settings qua Editor.
- C: InputController chạm nửa xe/vuốt/nút bàn xoay/trụ, UI không xuyên chạm; ActionAnimator theo events, nhận lệnh liên tiếp và đặt chỗ đường hình; hủy animation khi Undo/Restart; nút debug Undo/Restart/Hint/trước/sau, chữ giờ/thắng/thua/kẹt.
- Không làm: Navigator/panel/luồng điều hướng (009-A), âm thanh/rung/art ngoài/VFX đẹp, package/settings khác, APK, đổi luật/golden, commit/push/merge. Người làm quyết commit các mốc A/B/C, Claude review một lần cuối.

## Tệp dự kiến (Editor khi tạo/import/test)
- Game: GameSession.cs, LevelTimer.cs, SearchServices.cs (StuckDetector/HintProvider + kiểu nhỏ), GameConfig.cs, FeelConfig.cs, CameraConfig.cs, ConfigProvider.cs.
- Presentation: asmdef, GameBootstrap.cs, BoardView.cs, CarView.cs, InputController.cs, ActionAnimator.cs, VisualPalette.cs; UI debug gom trong một component nếu cần. Một MonoBehaviour một file, không tách vụn enum/struct.
- Editor: menu tạo config/prefab/scene và Sync Levels bằng AssetDatabase/PrefabUtility/EditorSceneManager; không tạo YAML/GUID tay.
- Config/*.asset, Prefabs/*, Scenes/Play.unity, Resources/Levels/*.json + level_order.json và .meta do Unity sinh; ProjectSettings/EditorBuildSettings.asset theo duyệt.
- Tests/EditMode: test session/timer/search/config, test tích hợp cần thiết; SolverPerformanceTests chỉ đổi Explicit nếu đo đỏ/chập chờn, giữ Assert kết quả.
- PROJECT.md §3.1/§8/§13 và phiếu này.

## Các bước / tiêu chí xong — V1 + V2, ảnh V3
1. Git main sạch, fetch main/origin/main cùng afe9593; nhánh 008-playable --no-track. Người làm giao việc là duyệt play-plan §2, asmdef/scene/Build Settings.
2. Mốc A: test Undo nhiều lần/Restart, phạt giờ, dt/pause/hết giờ, sao đúng biên, untimed, gợi ý trừ lượt/giờ; hủy tìm kiếm theo state; chỉ Stuck khi duyệt hết. Stopwatch bọc Step(1), chung ngân sách thời gian mỗi frame, không chỉ node; ghi độ trễ/cấp phát thật khi gắn game.
3. Mốc B: menu Sync Levels chạy, 31 JSON thử và thứ tự nạp được; nhìn đủ cơ chế, màn 1/17 vừa khung dọc; scene mỏng và config không sửa lúc Play.
4. Mốc C: Play/input/events/Undo giữa animation; không nhận lệnh khi hết giờ/thắng; reload scene và vào lại Play; ảnh Game view và danh sách chơi thử. Golden 31 màn/276 chuỗi và test cũ không đỏ, ghi nguyên văn Test Runner/Console.
5. Điền phiếu, sổ việc, bàn giao Claude một lần; không làm 009. Người làm tự chơi Editor/điện thoại và build.

## Điều cấm / rủi ro / cắt nếu trễ
- Không sửa luật để chiều hình; không ngầm đổi JSON chuẩn; không .meta/GUID thủ công, không package/settings ngoài Build Settings đã duyệt.
- Step(1) không thể ngắt giữa node: ngân sách ms là giới hạn mềm, có thể vượt bằng chi phí một node. Đo và báo thay vì hứa cứng 2ms.
- Nhiều lệnh liên tiếp: Core đã đi trước hình, cần hàng đợi/đặt chỗ theo đường sự kiện, Undo hủy toàn bộ. Không chặn mọi input cho đến hết animation.
- Camera/Input theo không gian bàn, safeArea; trụ/portal/bàn xoay không được che xe; glyph tiếng Việt cần kiểm trong Game view.
- Có thể cắt Hint, rồi vuốt, rồi đơn giản water/tele/pyl nếu trễ theo plan; không cắt Undo/đồng hồ/thắng-thua/chạm nửa xe. Vượt gấp đôi time-box thì dừng báo.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch
- A đã viết: GameSession dùng SolverMove của 007, stack snapshot và Changed; LevelTimer nhận dt, pause/phạt/sao; SearchServices bọc WinSearch/PathSearch chung SearchBudget thời gian, bỏ search cũ ngay khi session đổi, hủy đăng ký khi Dispose. Hết maxStates vẫn Unknown; gợi ý chỉ trừ lượt/giờ khi giao được một nước, không trừ khi mới bấm tìm.
- B/C đã viết code, scene đã tạo, **chưa xác nhận Play**: BoardView/CarView dựng khối theo LevelDef; InputController raycast UI trước và chiếu theo thân xe; DebugHud tạo nút bàn xoay trên xe và nút debug; ActionAnimator đọc events, hàng đợi mỗi xe, đặt chỗ bao quét XZ/thời gian kể cả đoạn đứng đợi; Undo hủy hàng đợi và Snap từ Core. Không Navigator, không điều hướng. Bootstrap dùng unscaledDeltaTime, chỉ dừng giờ khi thắng/hết giờ.
- [Đã kiểm file/log/ảnh người làm] Người làm chạy Setup Play lúc 09:11 ngày 23/11. Đã có Config (4 SO + BlockMaterial), GameRoot.prefab, Play.unity, 31 JSON + level_order (1–31); Unity sinh đủ meta. Scene có đúng 5 root: Main Camera, Directional Light, GameRoot instance, EventSystem, Canvas; GameRoot tự tham chiếu Board/Animator/Input/Hud và config trong prefab, camera/canvas được nối qua override scene. Build Settings từ m_Scenes=[] sang một entry Play.unity enabled=1. Không đổi scene mẫu hay setting khác bằng code. Menu Setup có bảo vệ scene đã tồn tại, không dựng đè.
- Config mới (đã tạo instance qua Unity): GameConfig — giờ/phạt/hint/sao/fps/tutorial như plan, thêm SearchMilliseconds=2, SearchMaxStates=300000 để điều chỉnh ngân sách; UnknownMinimumMoves=20 là tạm tính 120 giây cho màn 17–19, không tự nhận minMoves; ngưỡng vuốt theo cạnh ngắn .06, đường kính nút bàn xoay 9 mm. FeelConfig — thời lượng theo plan, BumpDistance=.04, WaterDipCells=.6, RotateMidScale=.65 là số tạm khối để tránh thân quệt lúc xoay. CameraConfig — 65°/padding .5. VisualPalette — màu xe/cơ chế/nền và vật liệu chung, không tạo material mỗi frame.
- HUD debug dùng uGUI Text + font OS dynamic (Arial/Roboto/Droid Sans/sans-serif); chưa tạo TMP atlas hay thêm font ngoài, chưa phải HUD/panel chính thức 009. Cần người làm kiểm dấu tiếng Việt trên Editor/Android. Không tạo Scripts/UI rỗng; DebugHud hiện cùng assembly Presentation.
- Test mới SessionAndTimerTests (15 case) và PlayableViewTests (5 case): kiểm snapshot/Restart, dt/pause/phạt/sao/untimed, hạn gợi ý/cancel/Unknown, config OnValidate, hai làn chạy song song, xe theo sau không chồng đường đã đặt chỗ, Undo hủy Exit, camera dọc cho màn 1 và 17.
- Lượt Test Runner trước: **327 passed, 2 failed, 0 remaining**. Hai đỏ: StarsIncludeExactBoundary(20,2) `Expected: 2 / But was: 1`; Level16_ReportsTimeAllocationsAndSingleExpansionCost `Profiler không bắt được cấp phát mẫu 1 MiB / Expected: greater than or equal to 1048576 / But was: 0`. Đã sửa biên sao bằng bù sai số tỷ lệ 1e-7; bài đo đổi Explicit theo duyệt, giữ nguyên Assert. **Chưa chạy lại Test Runner sau sửa**, chưa chạy 5 case view mới. Không tự nhận V2 xanh.
- **Kết quả mới — ảnh Test Runner do người làm gửi 23/11 09:12:** EditMode **333 passed, 0 failed, 1 not run** (bài đo Explicit). Các test cũ và 20 case mới đều qua; 1 bài đo không được coi là đã đo được cấp phát. Ảnh Setup Play: Console **2 log, 0 warning, 0 error**. Hai log nguyên văn: `Sync Levels: 31/31 JSON chuẩn và level_order; Unity sinh/giữ .meta.`; `Setup Play: Play.unity có Camera, Light, GameRoot, EventSystem, Canvas; đã thêm Build Settings.` Unity log có `Tundra build success` cho code mới. Đây là kiểm compile/test/Setup, chưa thay chơi thử runtime.
- [Người làm chơi thử, 23/11 09:14–09:16] Đã vào Play và **chạm được**; người làm cho commit mốc trước, chưa nhận toàn bộ 008 đã xong. Cảm giác còn lấn cấn được xác định: vuốt phải chạy ngay khi vượt ngưỡng trong lúc còn giữ tay. Code hiện `Input.GetMouseButtonUp(0)` mới xét vuốt nên chưa đạt yêu cầu mới. **Giữ nguyên code đã kiểm cho commit mốc này**, lượt sau commit sửa xử lý khi đang giữ, mỗi gesture tối đa một lệnh và không lặp lúc nhả; thêm test hành vi và người làm thử lại.
- [Đã biên dịch CLI, 23/11 09:07] Dùng Roslyn đi kèm đúng Unity, references/defines lấy từ Bee; output trong Logs/008-cli-compile (ignored): `COMPILE OK: SqueakyLane.Game`, `COMPILE OK: SqueakyLane.Presentation`, `COMPILE OK: Assembly-CSharp-Editor`, `COMPILE OK: SqueakyLane.Tests.EditMode`. Không lỗi/cảnh báo trong lượt CLI cuối. Đây chỉ là biên dịch C#, **không thay Console/Test Runner/Play**.
- Menu Measure Search (Play) đã chuẩn bị: đo HintProvider+StuckDetector màn 16 cùng ngân sách ms, 3 mẫu, hiệu chuẩn cấp phát 1 MiB theo counter SAU frame + nền 4 frame; báo total/peak ms, frame/raw allocated bytes và ước lượng trừ nền mọi luồng. Tạm dừng điều khiển/đồng hồ màn hiện tại trong lúc đo, phục hồi trạng thái Profiler và pause khi xong. Ghi Logs/008-search-performance.txt (ignored). **Chưa chạy, chưa có số cấp phát**; nếu counter không bắt mẫu thì báo unavailable thay vì 0 byte.
- [Đã kiểm file] git diff --check qua (Git chỉ nhắc chuẩn hóa LF→CRLF của Build Settings); mọi asset/thư mục trong Assets/_Project có meta. So sánh text: 31/31 JSON runtime giống hoàn toàn docs/golden/levels, order đúng 1–31. PROJECT cập nhật kiểm thủ công/cây code/sổ việc/scene Build Settings; play-plan sửa mâu thuẫn kẹt theo trả lời người làm. Không commit/push/merge/bàn giao.

## Giả định · Chưa chắc / chưa kiểm · Phát hiện ngoài phạm vi
- 31 màn là bộ thử cũ, không phải 25 màn cuối. Màn 17–19 chưa biết minMoves nên không lấy -1 tính giờ; cần mặc định tạm từ GameConfig và ghi rõ khi tích hợp.
- 007 đã merge theo main và người làm xác nhận; phần cấp phát còn nợ được kiểm lại trong 008, không coi 0 byte là hợp lệ.
- Người làm chốt 23/11: từ giờ không điều khiển máy/Editor; Codex viết code, menu Editor và hướng dẫn để người làm tự chạy kiểm, có thể đọc log. `ProjectSettings/TimelineSettings.asset` và `ProjectSettings/SceneTemplateSettings.json` tự xuất hiện ngoài phạm vi, không sửa/xóa và không đưa vào commit 008; Build Settings nằm trong duyệt.
- Cần kiểm bắt buộc tiếp: Game view/input/camera/animation và reload/vào Play lại; đo tích hợp cấp phát màn 16; Console sau Play. Chưa đủ bằng chứng để bàn giao hay báo xong 008.
- Log Play có `[remote] Dropped touch events: ...`; chưa kết luận nguyên nhân hoặc liên quan đến cảm giác. Hiện thiếu ảnh Game view/kết quả đầy đủ các cơ chế; chỉ xác nhận chơi thử cơ bản theo người làm, không nhận mọi mục checklist đã qua.
- Người làm nhắc có bước tích hợp prefab asset ngoài; đã ghi quy trình vào PROJECT §7.5 và play-plan §4.1 cho 010. 008 vẫn dùng khối; chưa tạo CarCatalog hay nhập asset ngoài trong mốc này.

## Bạn cần hiểu
- GameSession giữ luật/trạng thái; view chỉ thể hiện events. Undo quay về state cũ và dựng lại hình.
- Step theo thời gian chỉ chia việc tìm kiếm, không chứng minh máy Android đạt fps.
- Tài nguyên scene/prefab/config được tạo bằng API Unity để GUID/.meta đúng.
- 008 là vòng chơi với nút debug; điều hướng/panel được làm ở 009-A.

## Danh sách chơi thử — người làm kiểm trong Play
1. Màn 1: bãi/xe hiện, chạm để thắng và thấy sao; chạm đầu/đuôi đi đúng chiều.
2. Vuốt dọc thân, vuốt ngang thân không ra lệnh; chạm nút debug không làm xe dưới nút chạy.
3. Hoàn tác giữa lúc đang chạy/ra cổng: mọi xe về đúng ô, không tiếp tục biến mất; Restart về đầu màn.
4. Màn 3: vào cổng nước rồi dội, bùn được rửa.
5. Màn 8: nút XOAY trên tâm bàn nhận chạm trước xe.
6. Màn 20: xe tải và xe bị đẩy đi cùng, không chồng hình.
7. Màn 21: portal đúng cặp, hiện lại đúng vị trí/hướng.
8. Màn 29: trụ xoay đủ 4 lần về chỗ cũ, A/B theo đúng hướng.
9. Màn 17: bãi 7×7/15 xe nằm vừa màn dọc; tên/chữ tiếng Việt đọc được.
10. Hết giờ hiện Hết giờ và chặn lệnh xe; kẹt hiện chữ nhưng giờ vẫn chạy.
11. Hint giao được một nước thì trừ 1 lượt và phạt giờ, không áp dụng tự động; đi tiếp/Undo không nhận gợi ý cũ.
12. Thoát Play, mở lại scene/vào Play lại: không còn hoạt hình/state cũ, Console không lỗi/cảnh báo mới. Android/fps/nút 9 mm: chưa kiểm, người làm tự build khi đến việc APK.

## Phản đối kế hoạch
- play-plan §2.11 nói đồng hồ tạm dừng khi hộp thoại (kể cả kẹt), trong khi PROJECT §6.4 và play-plan §3.1 chốt Kẹt xe vẫn chạy giờ. Đã được người làm trả lời: giữ giờ chạy khi kẹt; 008 hiện chữ không chặn, chỉ dừng khi thắng/hết giờ.

--- Người review điền ---
## Verdict
Chưa review. Codex không tự APPROVE.

## Trao đổi
2026-11-22 22:48 · Codex → Người làm · [BÁO]: Main sạch, main/origin/main afe9593; tạo 008-playable --no-track. Nhận A–C theo play-plan, không Navigator/009; chưa commit/push/merge.
2026-11-22 22:48 · Codex → Người làm · [HỎI]: Phản đối kế hoạch: §2.11 pause khi kẹt mâu thuẫn PROJECT §6.4/§3.1. Đề nghị Kẹt xe là chữ không chặn, đồng hồ vẫn chạy; thắng/hết giờ mới dừng. Chờ chốt phần phụ thuộc, mốc A độc lập tiếp tục.
2026-11-23 08:43 · Codex → Claude · [BÁO]: Người làm đã trả lời giữ giờ chạy khi kẹt; tiếp tục 008. GameSession/timer/search services và config đã viết, Unity sinh meta; đang thêm test mốc A. Chưa có kết quả kiểm cuối.
2026-11-23 08:57 · Codex → Claude, Người làm · [BÁO]: Người làm yêu cầu từ giờ không điều khiển máy, cần test thì hướng dẫn người làm chạy. Dừng CUA. Lượt mốc A: 327 qua/2 đỏ (StarsIncludeExactBoundary(20,2) và bộ đo Profiler không bắt 1 MiB); đã sửa sai số biên sao và đổi bài đo Explicit theo duyệt, chưa chạy lại. Tiếp tục chuẩn bị code/menu để người làm kiểm.
2026-11-23 09:07 · Codex → Người làm · [BÁO]: Code A–C/menu đã chuẩn bị, 4 assembly biên dịch CLI sạch; chưa thay kiểm Editor. Đề nghị người làm chờ Unity compile, EditMode Run All rồi Tools/Squeaky Lane/Setup Play và vào Play, báo Console/test và ảnh Game view. Sau khi chơi cơ bản chạy được, dùng Measure Search (Play) để lấy số cấp phát thật. Gậy vẫn Codex, chưa bàn giao/commit/009.
2026-11-23 09:12 · Codex → Claude, Người làm · [BÁO]: Ảnh người làm xác nhận EditMode 333 xanh/0 đỏ/1 chưa chạy (Explicit); Setup Play có 2 log/0 warning/0 error. Đã đối chiếu scene 5 root, Build Settings bật Play, đủ meta và 31 JSON runtime giống golden. Chờ Play/input/ảnh Game view, reload và số đo màn 16; chưa bàn giao.
2026-11-23 09:16 · Codex → Claude, Người làm · [BÁO]: Người làm đã Play/chạm được, duyệt commit mốc trước; gậy vẫn Codex, 008 còn đang làm và chưa review cuối. Yêu cầu sửa sau commit: vuốt đủ ngưỡng phát lệnh ngay khi giữ tay, nhả không lặp; hiện code chỉ xét khi MouseButtonUp. Đã ghi bước chuẩn hóa/model→prefab bọc→CarCatalog→kiểm cơ chế ở kế hoạch 010, chưa nhập asset. Chưa đo cấp phát màn 16/chưa kiểm đủ checklist. Nhánh 008-playable, HEAD afe9593, chưa stage/commit/push/merge.
