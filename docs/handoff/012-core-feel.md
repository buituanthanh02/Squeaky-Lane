# 012 — Cảm giác lõi · L ≤ 1 ngày · Run All xanh, chờ menu số mới/Play/video
Đang cầm: Người làm (bấm menu số mới, Play/Console/video rồi báo Codex để bàn giao Claude) · Nhánh `012-core-feel` từ main đã có 011 · AI: Codex (code), Claude (số cảm giác, review)
Liên quan: play-plan §4 hàng 012 · PROJECT §4 `[CHỐT]` "xe là nhân vật bằng ngôn ngữ cơ thể, không vẽ mặt, rung màn hình nhỏ"

## Mục tiêu (người chơi)
Thao tác lặp lại cả trăm lần (chạm → xe chạy → va hoặc ra cổng → được rửa) phải **sướng tay**: xe nhún, chúi, nảy như có cân nặng, bánh quay, qua cổng nước có tia nước, qua cổng màu có bọt và lấp lánh, thắng có pháo giấy. Luật và thời điểm Core không đổi.

## Cách làm
- **Chuyển động cơ thể đặt trên `CarView.modelRoot` (con), không đụng root.** Root vẫn do `ActionAnimator` đặt theo lưới, collider và chạm nửa xe không đổi.
- Kích hoạt bằng **sự kiện có sẵn** của `ActionAnimator` (`CarStarted`, `EventStarted` với Stop/Exit/Mud/Spray/Water/Paint/Push…). Không tự suy luật từ vị trí.
- Một component nhỏ trên xe (ví dụ `CarBody`) chạy lò xo theo thời gian; **không cấp phát mỗi frame**. Tắt khi `ActionAnimator.Paused`.
- **Hạt:** mỗi loại một `ParticleSystem` dùng chung (pool), gọi `Emit(n)` tại vị trí. Không `Instantiate` mỗi sự kiện. Mỗi loại một material dùng chung. Tổng hạt sống ≤ 60.
- Sprite VFX: Kenney **Particle Pack** (CC0, đã có ở `Downloads/kenney_particle-pack/PNG (Transparent)`). Chỉ nhập file dùng tới vào `Assets/_Project/Art/VFX/Kenney/`.
- **Mọi số trong `FeelConfig`** (field mới có Tooltip + Range + OnValidate). `bool` bật/tắt từng nhóm để Claude/người làm thử nhanh.

## Số khởi điểm (Claude chốt; chỉnh theo video)
| Khoảnh khắc | Hình | Số |
|---|---|---|
| Khởi hành | Đuôi chùng, mũi ngóc | pitch −5°, 0,08 s, ease-out |
| Phanh / va tường / va xe | Chúi mũi rồi bật lại có vượt | pitch +8° → −3° → 0, tổng 0,22 s · ép thân: dài 0,94, cao 1,04 trong 0,06 s |
| Xe đứng yên bị đụng | Giật nhẹ theo hướng va | 0,05 ô, 0,12 s |
| Ra cổng | Rướn người | kéo dài 1,06 theo trục thân, pitch −4° |
| Lội vũng bùn | Lún nhẹ | hạ 0,03, ép cao 0,97, 0,12 s |
| Được rửa (cổng nước, ô vòi) | Rũ bùn | lắc roll ±6°, 3 nhịp, 0,35 s · lớp bùn mờ dần 0,2 s thay vì tắt ngay |
| Bánh xe | Quay theo quãng đường | góc = quãng đường / bán kính bánh (lấy từ bounds node `wheel-*`) |
| Rung màn hình | Chỉ khi va | 0,04 đơn vị, 0,1 s, tắt dần |
| Cổng nước | Tia nước + hơi | 14 hạt `circle_*`/`smoke_*`, `#4FC3F7` + trắng, 0,4 s, hình nón lên |
| Cổng màu | Bọt màu xe + lấp lánh | 10 bọt `circle_05` màu xe + 4 `star_*` khi ra, 0,5 s |
| Va | Bụi nhỏ | 6 hạt `smoke_01` xám, 0,25 s |
| Thắng | Pháo giấy 6 màu xe | 30 hạt từ trên bãi, 1,0 s, phát cùng tiếng thắng |

## Thứ tự (P1 bắt buộc)
1. P1 `CarBody` + số trong `FeelConfig`: khởi hành, phanh/va, ra cổng. Test EditMode cho hàm lò xo (về 0, không NaN, không vượt biên).
2. P1 Bánh quay.
3. P1 Hạt cổng nước, cổng màu, va; rung màn hình nhỏ.
4. P2 Lún bùn, rũ bùn, bùn mờ dần; xe bị đụng giật nhẹ.
5. P2 Pháo giấy khi thắng.

## Kiểm
- Run All + Console sạch. Undo/Chơi lại giữa lúc xe đang nhún: xe về tư thế 0 ngay, không kẹt nghiêng.
- Pause: mọi chuyển động và hạt dừng hoặc ẩn.
- **Video 20–30 giây** gửi Claude (quay Game view hoặc Simulator): màn có bùn → cổng nước → cổng màu (ví dụ 05 hoặc 11), một lần va xe, một màn có xe tải (20), một lần thắng.
- Stats màn 16 so với 011 (156 batches); Profiler: không GC Alloc mỗi frame từ `CarBody`/hạt.

## Điều dễ sai
Nghiêng root thay vì `modelRoot` (lệch collider, chạm sai) · `Instantiate`/`Destroy` hạt mỗi sự kiện · tạo material lúc chạy · lò xo dùng `Time.deltaTime` khi game Pause (`timeScale` không dùng trong dự án, nhưng phải dừng theo `ActionAnimator.Paused`) · Undo không reset tư thế · số viết cứng trong code.

## Cắt nếu trễ
5 → 4 → hạt va. **Không cắt** 1, 2, hạt cổng nước/cổng màu.

Báo cáo ~15 dòng; Trao đổi chỉ `[BÀN GIAO]`; không tự commit.

## Báo cáo Codex (25/11, bàn giao WIP theo yêu cầu người làm)
Đã viết P1 1→3 và P2 4→5; chưa cắt mục nào. Luật/Core/JSON/CameraConfig/HUD/package/settings không đổi.
Code mới: `BodySpring` (Game, C# thuần), `CarBody`, `FeelEffects`, `CoreFeelTools`, `CoreFeelTests`; nối FeelConfig/CarView/ActionAnimator/GameBootstrap.
Thân/mũi/ép/rướn chỉ sửa modelRoot và bùn con; root/collider giữ luồng cũ. Lò xo nghiệm theo thời gian, bật ngược 3/8, reset khi Undo/Chơi lại/disable, dừng theo animator.Paused.
Bánh cache wheel-* và bán kính từ mesh bounds quy về hệ root; quay theo Step/Push/Water/Exit, bù pivot FBX để không quay vòng quanh xe; Tele/Rotate/nhịp Stop không tính quãng lăn.
Giữ sự kiện âm/rung cũ; thêm CarEventStarted mang CarView + MoveEvent và Cancelled. Xe bị chặn ngay cũng diễn Stop đã có, không thêm lượt/đổi state Core.
Hạt: CoreFeel prefab 5 ParticleSystem, dùng Emit + Simulate chung đồng hồ Pause; tổng ≤60 hạt sống; không Instantiate/material mới theo sự kiện. Camera cộng/trả offset, không sửa CameraConfig.
Config: thêm nhóm bật/tắt thân/bánh/bùn/nước/cổng/bụi/pháo giấy/rung, số theo bảng Claude; các số hạt phụ là số tạm chờ video. Material bùn alpha dùng asset chung + MaterialPropertyBlock.
Người làm đã chạy menu: có5 PNG circle_01/circle_05/smoke_01/star_05/spark_05 (~351KiB nguồn) + License CC0,6 material/CoreFeel prefab; GameRoot.feelEffectsPrefab đã gán,12 prefab xe thêm CarBody; mọi asset mới đủ.meta. Backup Logs/012-asset-backups/20261125-090112-292.
Dữ liệu: số feel mới theo bảng, mudFadeMaterial đã gán; winDialogDelaySeconds thực tế0.5→0.5s (giữ giá trị cũ, menu chỉ đổi nếu0). Không sửa scene/GUID/meta cũ; GameRoot ghi thêm các field serialize null cũ, scene override giữ nguyên.
Đã kiểm bằng Roslyn: Game/Presentation/Editor/Tests/Presentation Android biên dịch0 lỗi; Game/Editor/Tests0 warning, Presentation39 CS0649 đều ở field serialize sẵn có. Chạy độc lập BodySpring24.012 mẫu qua (biên/không NaN/về0/bật ngược3/8); chưa Unity Run All hay xác nhận Console sạch.
Test mới (15 ca): spring ổn định/biên/thời gian lỗi; root/collider không nghiêng + Pause/Undo; bánh xe thường/tải tới-lùi/tâm/reset; metadata Stop; bùn fade/Undo; trần hạt/Pause/Cancel; OnValidate.
Ảnh mới: Run All538 passed/1 failed/1 skipped; test đỏ `MechanicCatalogTests.OcclusionUsesSharedMaterialAssetsAndRestoresOnDisable` (011, code/test không đổi trong012). Chưa có nội dung assertion, chưa kết luận nguyên nhân; không sửa đoán hoặc làm yếu test. Đây là điều kiện CHẶN merge.
Người làm báo rung/cảm giác tạm ổn và yêu cầu commit để chuyển Claude. Chưa xác nhận riêng đủ danh sách Play/Console0, video20–30s, Stats16/Profiler GC; mốc011:156 batches. Font TMP fallback phát sinh khi Play nằm ngoài phạm vi, để nguyên ngoài commit.
Bạn cần hiểu: Core tính luật trước, hình diễn sự kiện sau; nhún con giữ vùng chạm đúng; quãng lăn là từng đoạn trượt nên portal không làm bánh quay vọt; material/particle tạo sẵn để tránh cấp phát theo sự kiện.
Tự tin code ~80%; V2 còn1test đỏ, V3 mới có xác nhận người làm, Claude chưa review. Commit WIP theo yêu cầu chat, không coi đã hoàn tất/đủ merge. Claude review code/feeling; Codex hoặc người làm cần lấy assertion + Run All xanh/Console sạch trước merge. APK đầu tiên ở013, tối26/11.

## Verdict (Claude, 25/11, commit adfc4c1): CHANGES — code ổn, chặn merge vì Run All còn đỏ
Đã soi: phạm vi; Core không đổi, `BodySpring` thuần C#; số khớp bảng; không cấp phát mỗi frame (đọc code); input không đổi; Undo/Cancel/OnDisable reset tư thế và hạt; không static; 12 prefab chỉ thêm `CarBody`, GameRoot thêm `feelEffectsPrefab`. Hạt: `Simulate` tự pause hệ nên Pause đóng băng được (đọc code, chưa thấy trên máy).
Chưa soi: chi tiết `CoreFeelTools`, YAML `CoreFeel.prefab`, `FeedbackRouter` khi chạm xe bị chặn ngay.
- [CHẶN] `MechanicCatalogTests.OcclusionUsesSharedMaterialAssetsAndRestoresOnDisable` đỏ. 012 không đụng `MechanicOcclusion`/test này, `Editor.log` không ghi assertion. Nghi do `runInEditMode` phụ thuộc trạng thái Editor/thứ tự test (chưa kiểm). Cần nguyên văn assertion + kết quả chạy lẻ test này; không làm yếu test.
- [NÊN] `CarBody.cs:137` cú giật khi bị đụng nằm trong `if (feel.MudBodyEnabled)`: tắt nhóm bùn là mất luôn cú giật. Chuyển dòng 137 ra ngoài khối bùn.
- [NÊN] `winDialogDelaySeconds` 0,5 (CHỐT 23/11) < pháo giấy 1,0 s: bảng thắng che nửa sau pháo giấy. Người làm chọn: giữ 0,5 hoặc tăng ~0,9. → **Đã xử lý 25/11:** người làm chốt 0,9; Claude sửa `FeelConfig.asset` `winDialogDelaySeconds` 0.5 → 0.9 (sửa YAML, chưa Play lại).
- [GỢI Ý] Chạm xe bị chặn ngay giờ có nhịp Stop + âm/rung theo lý do (trước chỉ `StopWall`): đổi hành vi có chủ đích, ổn. Chạm dồn nhiều lần sẽ xếp hàng nhịp Stop: thử trên máy.
- [GỢI Ý] `CarBody.Advance` ghi transform mọi xe mỗi frame cả khi đứng yên; chỉ tối ưu nếu Profiler/Stats16 xấu. `smoke` dùng chung cho hơi nước và bụi, `gravityModifier` đặt lúc Emit đổi cả hạt đang sống (hiện cùng 0,2 nên chưa sao).
### V3 theo video 09:12 (30 s, Simulator iPhone 12 Pro Max, màn 26)
Claude tách khung hình 20 khung/s để soi. Thấy: xe chạy, ra cổng dưới (hồng 12,9 s, xanh 18,0 s), va dừng (vàng 19,8 s); Console 0/0/0 suốt lúc Play. **Chưa thấy:** bùn → cổng nước, thắng/pháo giấy, xe tải đẩy, Pause/Undo (màn 26 không có các thứ này).
- [NÊN] **Hạt cổng màu gần như vô hình**: bọt/sao chỉ là vài điểm 1–2 px trên cổng. Lý do: hạt 0,17–0,2 ô trong khi xe chỉ ~35 px; bọt cùng màu với chính cổng; vận tốc hướng lên trời nên ở góc 70° hầu như không tỏa trên màn hình.
- [NÊN] **Bụi va không thấy**: xám 0,5 trên mặt nhựa đường tối, cỡ 0,18.
- [NÊN] **Nhún/chúi rất khó thấy**: camera gần như nhìn từ trên xuống, nên chúi mũi ±8° gần như không đổi hình; chỉ co/giãn **theo chiều dài và chiều ngang** là đọc được. Ép thân 0,06 s chỉ ~4 khung hình. Bãi to hơn ở 013 sẽ giúp, nhưng chưa đủ.
- Ghi nhận cho 013 (không sửa ở 012): tiêu đề chọn màn còn "New Text", nút "Button", banner "Kẹt xe" là ô trắng trống, bãi chỉ ~50% bề ngang.

### Số mới cho Codex (`FeelConfig.asset`, Claude chốt; người làm chỉnh lại sau APK nếu cần)
| Field | Cũ → mới | Vì sao |
|---|---|---|
| `brakeLength` | 0.94 → 0.90 | co thân thấy được từ trên xuống |
| `squashSeconds` | 0.06 → 0.10 | đủ ~6 khung hình |
| `exitStretch` | 1.06 → 1.10 | rướn khi ra cổng đọc được |
| `hitDistance` | 0.05 → 0.08 | xe bị đụng giật thấy được |
| `particleSpread` | 0.7 → 1.2 | hạt tỏa ngang trên màn hình |
| `bubbles` size · count | 0.17 → 0.34 · 10 → 12 | thấy bọt quanh cổng |
| `sparkles` size · count | 0.2 → 0.4 · 4 → 6 | sao trắng tách khỏi màu cổng |
| `impact` size | 0.18 → 0.30 | bụi va thấy được |
| `impactColor` | (0.5, 0.5, 0.5) → `#D9D4C7` | bụi sáng nổi trên nhựa đường tối |
| `water` size | 0.13 → 0.25 | cùng lý do cỡ hạt (chưa xem) |
| `confetti` size | 0.14 → 0.25 | cùng lý do cỡ hạt (chưa xem) |
- [GỢI Ý, cắt được] **Phình ngang khi phanh**: field mới `brakeWidth` = 1.06 (Tooltip/Range/OnValidate), `CarBody` đặt `scale.z = Lerp(1, brakeWidth, squash)` như `scale.y`. Từ trên xuống, "ngắn lại + phình ra" là thứ đọc được nhất.
- Giữ nguyên: pitch, rung màn hình 0,04 (CHỐT "rung nhỏ"), trần 60 hạt.

Merge khi: Run All xanh + Console sạch + video ngắn **màn có bùn → cổng nước** (05 hoặc 11) **và một lần thắng** sau khi đổi số. Ảnh Stats16 nếu kịp, không thì đo ở APK 013.

## Cập nhật Codex sau review (25/11, chưa commit)
Đính chính: assertion người làm gửi là `CoreFeelTests.ConfettiSharesFiveSystemsCapsAllParticlesAndCancelClears`, Expected60/But was0 tại dòng196. Tên test011 chống che trong báo cáo/verdict trước được nhận diện nhầm từ ảnh; không có bằng chứng assertion chống che đỏ, không sửa MechanicOcclusion/test011.
Đã đổi default C# theo toàn bộ bảng số mới; thêm brakeWidth1.06 (Range/Tooltip/OnValidate), phình ngang khi phanh; cú giật khi bị đụng độc lập MudBodyEnabled, vẫn thuộc BodyEnabled.
Menu mới `Tools/Squeaky Lane/Core Feel/Apply Reviewed Tuning` chỉ ghi số đã duyệt vào FeelConfig bằng SerializedObject/SaveAssetIfDirty, backup Logs/012-asset-backups, in field cũ→mới; giữ winDialogDelaySeconds0.9/pitch/rung/trần60. Asset vẫn số cũ tới khi bấm menu, không cần Setup lại prefab.
Sửa pool hạt: bật emission với rateOverTime/rateOverDistance/burstCount=0 để chỉ Emit bằng code; Clear dừng/xóa rồi Simulate(0,restart:true) khởi tạo trạng thái mô phỏng và pause trước Emit. Ảnh mới xác nhận test pháo giấy đã xanh; chưa tách riêng tác động của từng thay đổi khởi tạo nên không kết luận lỗi do riêng emission hay reset.
Giữ assertions60/Pause/Cancel, thêm assertion30 ngay burst đầu để bắt pool không phát; thêm6 ca cho hit khi tắt bùn, co-dài/phình-ngang/reset, brakeWidth âm/quá lớn/NaN/Infinity.
Đã biên dịch Roslyn5 lượt0 lỗi; Game/Editor/Tests0 warning, Presentation39 CS0649 ở field serialize sẵn có. Ảnh người làm09:24: Run All545 passed/0 failed/1 skipped, gồm test pháo giấy và6 ca mới. Log có cảnh báo fallback từ2 test chủ đích; chưa xác nhận Console0 trong Play mới.
Đối chiếu asset09:24: winDialogDelaySeconds0.9 đúng; brakeLength0.94, bubbles.size0.17 và các số cũ còn nguyên, chưa có log Apply Reviewed Tuning. Còn bấm menu riêng → Clear Console/Play05 hoặc11 + thắng, Pause/Undo/Restart; video cho Claude, Stats16 đo APK013 nếu chưa kịp. Chưa commit phần sửa review.
Bạn cần hiểu: đổi default C# không ghi đè số asset cũ nên cần bấm menu; tắt bùn không nên mất phản hồi va; hạt to phủ nhiều pixel hơn dù trần vẫn60, đo mobile ở013 trước tối ưu. Tự tin code~80%, cảm giác chờ video.

## Trao đổi
2026-11-25 02:24 · Codex → Người làm · [BÀN GIAO]: Nhánh012-core-feel/HEADbaa6f56, chưa stage/commit; code P1/P2 và15 ca test đã viết, Roslyn5 lượt0 lỗi, BodySpring24.012 mẫu qua. Theo chat người làm sẽ thao tác sau khi Codex sửa xong: thoát Play/Prefab Mode → Refresh → Tools/Squeaky Lane/Core Feel/Setup Assets and GameRoot → Run All → Clear Console/Play05 hoặc11,20, va xe, Pause/Undo/Restart/portal/bàn xoay/reload/Play lại; báo Passed/Failed/Skipped + Console. Menu mới tạo5 PNG/CC0/6 material/CoreFeel, nối GameRoot và12 CarBody; chưa có asset/meta đầy đủ tới khi Unity chạy menu/Refresh. Chưa xác nhận Unity/V3, chưa chuyển Claude. Còn video20–30s cho Claude, Stats16 so156 batches và Profiler GC; APK đầu tiên ở013 tối26/11.
2026-11-25 09:05 · Codex → Claude, Người làm · [BÀN GIAO]: Người làm báo rung/cảm giác tạm ổn, yêu cầu commit chuyển Claude. Trao gậy Claude trên012-core-feel, commit mốcWIP từbaa6f56 (xem git log); code/menu/asset P1+P2 đủ, không đổi scene/Core/package/settings. Ảnh Run All538/1/1: test011 OcclusionUsesSharedMaterialAssetsAndRestoresOnDisable còn đỏ, chưa có assertion nên không sửa đoán; CHẶN merge tới Run All xanh + Console sạch. Chưa video/Stats16/Profiler và danh sách Play chi tiết. Giữ font TMP fallback phát sinh ngoài commit, không hoàn tác. Claude review một lần; chưa push/merge.
2026-11-25 09:10 · Claude → Người làm, Codex · [BÀN GIAO]: Review adfc4c1: CHANGES (xem Verdict). Code đọc ổn, không sửa code. Trao gậy Người làm: chụp nguyên văn assertion test Occlusion đỏ + chạy lẻ test đó, báo qua/đỏ; quyết winDialogDelaySeconds; hoàn lại font TMP fallback, không commit. Sau đó Codex: sửa theo assertion (không làm yếu test) + dời dòng hit ra khỏi khối bùn (CarBody.cs:137). Claude sửa PROJECT.md §7.5/§13 dòng 012 (V0). Chưa push/merge.
2026-11-25 09:17 · Claude → Codex, Người làm · [BÀN GIAO]: Đã xem video 09:12 (màn 26): Console 0/0/0; hạt cổng màu/bụi va gần như vô hình, nhún/chúi khó thấy ở góc gần thẳng đứng. Trao gậy Codex, làm theo thứ tự: (1) đổi 11 số theo bảng "Số mới cho Codex", báo field · cũ → mới; (2) dời dòng hit ra khỏi khối bùn (CarBody.cs:137); (3) nếu kịp: brakeWidth 1.06 + test OnValidate; (4) test Occlusion đỏ: chờ người làm gửi nguyên văn assertion + kết quả chạy lẻ, không làm yếu test. Đã có trong working tree, chưa commit: FeelConfig.winDialogDelaySeconds 0.5 → 0.9 (người làm chốt), verdict, PROJECT.md. Người làm: Run All + video bùn → cổng nước + thắng sau khi Codex xong. Chưa push/merge.
2026-11-25 09:24 · Codex → Người làm · [BÀN GIAO]: Đã sửa review trên012-core-feel/HEADbbf4e09: default số mới + menu Apply Reviewed Tuning, brakeWidth1.06/validation, hit độc lập bùn, sửa khởi tạo/reset pool hạt,6 ca test mới và assertion burst đầu. Đính chính từ assertion: test đỏ thực tế là ConfettiSharesFiveSystemsCapsAllParticlesAndCancelClears Expected60/But was0, không phải chống che011; không sửa test011. Ảnh mới Run All545/0/1, confetti đã xanh; Roslyn5 lượt0 lỗi. Asset còn số cũ tới khi người làm bấm Tools/Squeaky Lane/Core Feel/Apply Reviewed Tuning (giữ delay0.9). Còn Clear Console/Play05 hoặc11 bùn→nước + thắng/Pause/Undo/Restart, gửi videoClaude; Stats16 ởAPK013 nếu chưa kịp. Chưa commit/stage/push/merge phần sửa review.
