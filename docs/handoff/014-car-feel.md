# 014 — Cảm giác xe chạy: bứt tốc, vệt bánh, đèn, lắc khi đâm     Cỡ: M     Trạng thái: Chờ người làm chơi thử
Đang cầm: Claude        AI tham gia: Claude (code, nối, kiểm), Codex (asset VFX: phiếu `014-a-car-vfx-assets.md`)
Liên quan: 012 (ngôn ngữ cơ thể xe), 013 (theme, bóng giả). Thuộc dòng 014 "VFX và juice" trong sổ việc; phiếu này chỉ làm phần **xe chạy**, phần UI/cổng/pháo giấy của 014 để sau.

## Mục tiêu (người chơi)
Chạm hoặc vuốt thì xe **bứt tốc** ra khỏi chỗ đỗ (chậm → nhanh), để lại **vệt khói trắng mờ** sau đuôi, **đèn chiếu** ở đầu đang chạy; khi đâm vào xe khác/tường thì thân **lắc lư** vài nhịp rồi đứng yên. Tham chiếu: video Crowd Express (người làm gửi 26/11), chỉ học cách làm, không lấy asset.

## Ghi chép từ video (Claude xem 26/11, đã tắt tiếng)
- Vệt sau xe: một dải khói trắng mềm, hơi lượn, rộng ~1/3 thân, mờ dần trong ~0,5–1 s; nằm sát mặt đường.
- Đèn: 2 tia trắng ngắn phía trước đầu xe khi đang chạy, mất khi xe dừng.
- Xe bứt tốc từ chỗ đứng yên, không trượt đều ngay từ đầu.
- Video không có pha đâm rõ; phần lắc khi đâm là yêu cầu riêng của người làm.

## Phạm vi
**Làm:**
1. **Bứt tốc** (ActionAnimator): thời lượng từng ô theo gia tốc đều tới tốc độ tối đa `1/stepSeconds` sau `accelCells` ô; xe tải đẩy hàng dùng cùng nhịp. Không đổi luật/Core.
2. **Lắc khi đâm** (CarBody): dao động tắt dần quanh trục thân (roll) + lắc đầu (yaw) khi `Stop`; xe bị đụng (`Hit`) lắc nhẹ hơn. Biên độ nhân hệ số va 012 (trượt dài → lắc mạnh).
3. **Vệt bánh**: `TrailRenderer` gắn root xe (không theo nhún thân), sát mặt bãi, material do Codex làm; xóa vệt khi Hoàn tác/Chơi lại/portal.
4. **Đèn**: tia đèn ở đầu xe theo hướng chạy (trắng khi chạy tới, đỏ khi lùi), dài ra theo tốc độ, thu lại khi dừng. Prefab/material do Codex làm.
5. Mọi số trong `FeelConfig` (nhóm "014 — Xe chạy"); bật/tắt từng món.
**Không làm:** VFX UI/cổng/pháo giấy (phần còn lại của 014), âm thanh mới, đổi Core/luật, Project Settings, package.

## Tệp dự kiến
Claude: `Scripts/Game/FeelConfig.cs`, `Scripts/Presentation/ActionAnimator.cs`, `CarBody.cs`, `CarView.cs`, view mới `CarTrailView.cs`, test EditMode mới, `Config/FeelConfig.asset` (gán asset Codex, cần Editor), `PROJECT.md`.
Codex (phiếu 014-a): `Art/VFX/Textures/*`, `Art/VFX/Materials/*`, `Prefabs/VFX/*`, tool Editor mới.

## Tiêu chí xong + bậc kiểm
- V1/V2 (Claude qua MCP): biên dịch sạch, Console 0 lỗi/cảnh báo mới, EditMode Run All không đỏ (mốc 561/560/0/1), test mới cho thời lượng bứt tốc (tổng thời gian, đơn điệu, không đổi khi `accelCells`=0).
- V2: Play màn 01 và một màn có xe tải/portal: vệt không kéo vạch ngang bãi khi portal/Hoàn tác; đèn đổi màu khi lùi; Pause dừng lắc.
- V3: ảnh Game view qua MCP lúc xe đang chạy và lúc vừa đâm; người làm chơi thử chốt feeling.
- Hiệu năng: tối đa 12 TrailRenderer + 24 quad đèn; ghi số batches màn 16 trước/sau.

## Điều dễ sai
Vệt gắn vào `modelRoot` (lắc theo thân) · vệt kéo vạch khi xe nhảy chỗ (Undo/portal) · tạo material lúc chạy · đèn hiện khi xe đứng yên · bứt tốc làm lệch đặt chỗ đường đi giữa các xe (clip dài hơn) · xe tải đẩy hàng lệch nhịp với xe bị đẩy · số hard-code.

## Cắt nếu trễ
Đèn đỏ khi lùi → đèn → vệt bánh. **Không cắt:** bứt tốc, lắc khi đâm.

--- AI làm điền khi xong ---
## Đã làm · Tệp đổi · Test và kết quả · Lệch kế hoạch
- **Bứt tốc** (`FeelConfig.TimeToCell/StepDuration`, dùng trong `ActionAnimator` cho Step và Push): gia tốc đều tới `1/stepSeconds` sau `accelCells` = 1,2 ô. Ô đầu 0,22 s (trước 0,1 s), từ ô thứ 3 như cũ; cả lượt chậm thêm đúng 0,12 s. Xe tải và hàng bị đẩy cùng nhịp.
- **Lắc khi đâm** (`CarBody`): sin tắt dần (1−t)² trong 0,5 s, 2,5 nhịp; nghiêng 8° × hệ số va 012 (1 → 1,8 khi trượt ≥ 5 ô), lắc đầu = 0,8 × nghiêng; xe bị đụng lắc 0,6 lần, ngược chiều. Đo trong Play (đâm ngay ô đầu, số cũ 7°/0,45): xe đâm nghiêng ±3,8°/lắc đầu ±1,7°, xe bị đụng ±2,3°, về 0 sau ~0,5 s → tăng lên 8°/0,8 để nhìn từ trên xuống thấy rõ (ý kiến thẩm mỹ, chờ người làm chơi thử).
- **Vệt bánh + đèn** (`CarRoadFx` mới, class thường do `CarView` giữ): TrailRenderer gắn root xe, sát mặt bãi (+0,015), 0,6 s, rộng 0,4 ô, đầu vệt alpha 0,8; tia đèn trắng ấm ở đầu đang chạy, đỏ khi lùi, dài từ 55% tới 100% theo tốc độ, thu lại khi dừng. Vệt xóa khi Hoàn tác/Chơi lại (`CancelAndSnap`) và khi xe nhảy chỗ tức thời (portal). Không tạo material lúc chạy; object con tạo một lần mỗi xe.
- **Config:** FeelConfig nhóm "014 — Xe chạy" (17 field mới, `OnValidate` chặn giá trị sai): `accelCells` 1,2 · `crashWobbleDegrees` 8 · `crashWobbleYawRatio` 0,8 · `crashWobbleSeconds` 0,5 · `crashWobbleCycles` 2,5 · `hitWobbleRatio` 0,6 · `trailSeconds` 0,6 · `trailWidth` 0,4 · `trailAlpha` 0,8 · `lightResponse` 10 · `roadFxLift` 0,015 + 3 tham chiếu asset Codex.
- **Tệp:** `FeelConfig.cs`, `ActionAnimator.cs`, `CarBody.cs`, `CarView.cs`, mới `CarRoadFx.cs`, `CarFeel014Tests.cs`; `Config/FeelConfig.asset` (gán qua MCP); asset Codex (phiếu 014-a) + sửa hằng alpha/độ dài đèn trong `CarVfxTools.cs`. Không đổi Core/luật/scene/prefab game/package/settings.
- **Đã chạy (MCP, Editor chính):** biên dịch sạch, Console 0 lỗi/cảnh báo sau biên dịch cuối; EditMode **571 tổng · 570 Passed · 0 Failed · 1 Skipped** (thêm 4 test: bứt tốc đơn điệu/không vượt tốc tối đa, tổng thời gian = (ô + accelCells)·stepSeconds, accelCells = 0 giữ nhịp cũ, lắc bị chặn trong ±1 và về 0). Play màn 01 (bước từng khung 0,02 s, chụp `Logs/014-shots/`): xe xanh ra cổng có vệt + 2 tia đèn, xe hồng chạy 3 ô có vệt; Hoàn tác giữa đường: vệt 13 điểm → 0, đèn tắt.
- **Lệch kế hoạch:** Claude chỉnh số asset của Codex (alpha/độ dài đèn) thay vì gọi vòng 2 vì là sửa hằng một dòng; không đo Stats/batches màn 16.

## Phần 2 — Bùn kiểu D + vệt bùn (người làm chốt 26/11 01:27)
Người làm xem các kiểu bùn (game rửa xe ASMR, PowerWash, MudRunner, Forza) và chọn **D + E**: cục bùn khối trên nóc có giọt chảy xuống mép, giọt bùn rơi khi chạy, vệt bánh nâu khi dính bùn. Không phủ cả thân (màu xe phải đọc được để chọn cổng).
- **Codex (phiếu 014-b):** mesh `MudBlob` + `M_MudBlob` (Standard Opaque `#5A3418`, smoothness .65) + prefab `MudBlob` + `M_MudTrail`, thêm vào menu `CarVfxTools`.
- **Claude:** `CarView.SwapMudBlob` thay tấm bùn phẳng của prefab xe bằng cục bùn lúc dựng hình (không sửa 12 prefab xe); bùn đặc dùng material Opaque, đang rửa mới đổi sang `MudFade` cùng màu; `CarRoadFx.SetMud` đổi vệt trắng ↔ nâu; `FeelEffects`: mỗi ô xe bẩn chạy rơi 1 giọt bùn sau đuôi, lội vũng bùn thì bắn 8 hạt bùn (chung pool, chịu trần 60 hạt). FeelConfig nhóm "014 — Bùn kiểu D" (5 field). Test `MudBlob014Tests`.

- **Kết quả phần 2 (Claude, 26/11 02:00):** EditMode **572 · 571 Passed · 0 Failed · 1 Skipped** (thêm `MudBlob014Tests`); Console không lỗi. Play màn 02 (ảnh `Logs/014-shots/11…14`): xe dính bùn có cục bùn khối + giọt, vệt nâu và giọt rơi khi chạy, qua vòi xịt bùn hết và vệt về trắng; Hoàn tác trả lại cục bùn + vệt nâu, xóa vệt cũ. Số: giọt rơi 2 hạt/0,5 s/cỡ 0,2; bắn khi lội vũng 8 hạt; vệt bùn `#96622F` alpha .95.
- **Lỗi quy trình tự gặp:** sau khi MCP `manage_scriptable_object` ghi `FeelConfig.asset`, bản trong bộ nhớ Unity có lúc cũ hơn đĩa; lần `SaveAssets` sau đó ghi đè mất số đã chỉnh (vệt 0,4→0,3, lắc 8→7). Đã ghi lại đủ và đối chiếu `git diff` trên đĩa. Lần sau: chỉnh asset bằng `SerializedObject` trong `execute_code` rồi kiểm lại file YAML, không tin kết quả "ok" của patch.

## Giả định · Chưa kiểm · Ngoài phạm vi
- **Chưa kiểm:** portal (vệt có bị kéo vạch không; code xóa vệt ở đoạn tức thời), xe tải đẩy hàng, đèn đỏ khi lùi trong ảnh, Stats màn 16 (thêm tối đa 12 trail + 24 quad), ảnh có HUD (ảnh chụp bằng camera, không có UI), máy thật/fps.
- **Ngoài phạm vi (ghi, không sửa):** (1) sửa C# khi đang Play → Unity biên dịch lại giữa Play → `GameBootstrap.OnEnable/OnDisable` NullReference (`tableButtons`, `feedback` chỉ tạo ở `Awake`). Không xảy ra trong build; tránh bằng cách thoát Play trước khi sửa code hoặc đặt Preferences → Script Changes While Playing = Recompile After Finished Playing. (2) Chạy Test Runner làm `Play.unity` bị lưu kèm nhiễu layout (giá trị scrollbar, cỡ chữ auto-size): không commit, người làm bỏ thay đổi đó (`git restore Assets/_Project/Scenes/Play.unity`) khi Editor không mở dở. (3) `Play.unity` chưa có cảnh S4 (khay bãi/props) của 013.

## Bạn cần hiểu
- **Bứt tốc mà không đổi luật:** Core vẫn trả từng bước `Step`; chỉ thời lượng hoạt hình mỗi bước khác nhau. Công thức gia tốc đều: thời điểm tới ô s là `2√(s·a)·step` khi còn tăng tốc, `(s + a)·step` sau đó; nhờ vậy test kiểm được tổng thời gian chính xác.
- **Vì sao vệt/đèn gắn root xe chứ không gắn `modelRoot`:** `modelRoot` nhún, chúi, lắc theo CarBody; vệt và đèn phải nằm trên mặt đường nên đi theo root (chỉ có vị trí + hướng), rồi chiếu xuống mặt bãi trong `Project()`.
- **TrailRenderer tự vẽ theo chuyển động của transform**, nên khi xe nhảy chỗ (Hoàn tác, portal) phải gọi `Clear()`, nếu không sẽ có một vạch thẳng nối chỗ cũ và chỗ mới.
- **Số nằm trong `FeelConfig.asset`:** chỉnh trong Inspector ngoài Play là đổi ngay, không cần sửa code; sửa ScriptableObject trong lúc Play cũng bị lưu lại.

## Trao đổi
2026-11-26 00:58 · Claude → Người làm, Codex · [BÁO]: Mở nhánh `014-car-feel` từ main (b7cbc2b). Claude cầm phiếu, làm code; giao Codex phiếu nhỏ 014-a (texture/material/prefab vệt bánh + đèn).
2026-11-26 01:18 · Claude → Người làm · [BÁO]: Xong code + asset 014 trên nhánh 014-car-feel; biên dịch sạch, EditMode 571/570/0/1, ảnh Play màn 01 ở Logs/014-shots. Người làm chơi thử feeling (bứt tốc, vệt, đèn, lắc) và chỉnh số trong FeelConfig nếu cần; Play.unity có nhiễu layout do Test Runner, không commit.
