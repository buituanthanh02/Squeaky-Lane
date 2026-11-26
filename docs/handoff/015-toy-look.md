# 015 — Hình "đồ chơi" + cổng nước + UI nét     Cỡ: L (chia 5 bước)     Trạng thái: Chờ người làm xem (5 bước đã làm một lượt)
Đang cầm: Claude        AI tham gia: Claude (lập kế hoạch), Codex (asset khi cần)
Liên quan: 013 (theme/UI, S4 chưa áp), 014 (xe chạy, bùn, trạm rửa). Nhánh: `015-toy-look` (rẽ từ `014-car-feel` vì 014 chưa merge).

## Yêu cầu người làm (26/11)
Cổng nước (xe đi qua được rửa rồi quay vào bãi): dùng hình trạm rửa đã vẽ, đi chậm + rửa, **phải thấy xe đi qua cổng rồi mới quay lại**. Xe bớt đơn điệu: có độ bóng/đổ bóng (chỉ học cách đổ bóng, không chép hình). Camera nghiêng thêm. Chữ trên nút không nét. Theme nền vẽ lại, cảm giác 3D, nhộn, có đường nét/viền bóng. Tham chiếu: Loop Sort, video Color Bus Trip (Voodoo).

## Chẩn đoán (Claude đọc/đo 26/11)
- Cổng nước: `ActionAnimator` chỉ cho xe thụt 0,6 ô trong 0,25 s rồi lùi: không thấy đi qua.
- Camera `tiltDegrees` = 70. Ảnh so sánh `Logs/015-shots/` (58°, 50°). Ở 50° cổng phía dưới che ô của bãi.
- Xe: material colormap Standard gloss 0; **013 S4 chưa áp vào `Play.unity`** (ambient còn Skybox, Light còn Soft Shadows, chưa có khay/props/bóng giả).
- Chữ: font SDF 64 pt, padding 10, `HudOutline` dilate .25 + outline .20. Ảnh người làm là Simulator thu nhỏ: chưa tách được lỗi do thu nhỏ hay do viền dày.
- Video Color Bus Trip: cỏ xanh sáng, đường màu kem có mép đậm, xe bóng loáng với đốm sáng và bóng đổ mềm, nút be có viền; **không có viền đen**.

## Các bước (thứ tự đề xuất; mỗi bước có ảnh cho người làm)
1. **Cổng nước (M):** dựng cổng rửa từ asset trạm rửa (chổi + trụ + bọt) ở mép bãi; xe chạy xuyên hẳn ra ngoài, bò chậm trong cổng, lùi thẳng vào bãi và sạch bùn. Không đổi Core.
2. **Camera 58° (S)** và kiểm chạm nửa xe, che khuất, nhãn.
3. **Chữ nét (S):** render ở độ phân giải thật để xác định nguyên nhân, rồi chỉnh viền / tạo lại font atlas.
4. **Look đồ chơi (M):** shader mobile cho xe và vật: đổ bóng 2 tông mềm, đốm bóng, viền sáng mép, viền tối mảnh; áp phần ánh sáng + bóng giả của S4. Đo batches trước/sau (ngân sách ~200 ở màn 16).
5. **Theme nền (M, giới hạn một buổi):** cỏ sáng, mặt bãi và mép bo có bóng, vài vật trang trí ngoài bãi.

## Rủi ro / cắt nếu trễ
Cắt theo thứ tự: bước 5 props → viền tối → bước 5. **Không cắt:** bước 1, APK thử trên máy. Viền kiểu mesh phóng to thêm 1 draw mỗi mesh. Nền bãi sáng làm giảm tương phản xe vàng/cam: phải kiểm. Chưa có APK, chưa chốt 25 màn, hạn 30/11.

--- AI làm điền khi xong ---
## Bước 1 — Cổng rửa (Claude + Codex 015-a)
- **Đã làm:** cổng nước có hầm rửa `WashGate_0` (Codex vẽ, phiếu 015-a): xe bò `washGateCrawlSeconds` 1,1 s xuyên hầm ra thảm cho tới khi đuôi qua mép bãi thêm `washGateExtraCells` 0,3 ô (chổi quay + phun bọt, bùn trôi), đứng 0,15 s, lùi về mép bãi 0,45 s rồi lùi tiếp theo luật. Thiếu prefab thì giữ cổng nước cũ. Camera chừa thêm (xe dài nhất + 0,4 ô) ở trục có hầm rửa.
- **Tệp:** `FeelConfig.cs` (+4 field nhóm "015 — Cổng rửa"), `ActionAnimator.cs`, `BoardView.cs` (`WashGateAt`, `SpawnWashGate`, lề camera), `WashStationView.cs` (chổi `Roller L/R/Top` quay quanh X local của Spin), `MechanicCatalog.cs` (enum thêm cuối `WashGate`), `Config/MechanicCatalog.asset` (+1 dòng), test mới `WashGate015Tests` (3), asset Codex. Không đổi Core/luật/scene.
- **Đã chạy (MCP):** biên dịch sạch; EditMode **580 · 579 Passed · 0 Failed · 1 Skipped**; Console sau Run All chỉ còn cảnh báo khối dự phòng do test cố ý (1 cái mới từ `WashGate015Tests`, catalog test không có Surface). Play màn 03 (Editor dừng, bước animator tay): ảnh `Logs/015-shots/01-gate-crawl`, `02-gate-out`, `03-gate-back`.
- **Chưa kiểm:** hạt bọt khi Play chạy liền, Hoàn tác giữa lúc rửa trong Play (có test), cổng cạnh trái/phải trên khung dọc điện thoại, máy thật.

## Sửa sau video người làm (26/11 09:19) — xe vọt tốc sau khi rửa
- Video: đứng trên thảm → lùi chậm 0,45 s → 9,51 s vọt ~2,5 ô/0,25 s. Nguyên nhân: các ô lùi sau đặt "đủ đà" tốc tối đa; thêm nữa bứt tốc 014 chạy đều trong từng ô nên nhảy tốc ở ranh giới ô.
- Sửa: lùi khỏi thảm là bứt tốc từ đứng yên, các ô lùi nối tiếp đường cong (`accelBase`); vị trí trong ô theo `FeelConfig.CellsAtTime` (áp cho mọi Step/Push). Bỏ field `washGateBackSeconds`. Test `BackingOutAcceleratesSmoothlyWithoutSpeedJump`.

## Bước 2 — Camera 58°
- `CameraConfig.tiltDegrees` 70 → 58. Raycast 846 điểm (0,15/0,3/0,45 thân × 2 nửa × mọi xe × 25 màn) trên Simulator 1284×2778: 846/846 đúng xe, đúng nửa. Màn 16 không vật nào che xe; số trên nóc đọc được.
- Phát hiện ngoài phạm vi (không sửa): nút "XOAY" bàn xoay là ô vuông mờ to che xe đứng trên bàn.

## Bước 3 — Chữ "gãy"
- Ảnh `ScreenCapture` 1284×2778: chữ HUD/nút nét. Thu nhỏ 21% kiểu nearest (như Simulator ở Scale 21) tái hiện đúng chữ gãy (dấu tiếng Việt, nét mảnh); thu nhỏ có lọc thì ổn (`Logs/015-shots/14-text-compare.png`). **Kết luận: do Simulator hiển thị thu nhỏ, không sửa font/viền.** Chờ APK xác nhận.

## Bước 4 — Xe "đồ chơi" + S4
- Áp `S4 Apply Scenery` (013): ambient Flat `#9AA9C2`, Light Shadows None, khay bãi, bóng giả. Phần props Kenney của S4 **hỏng** (FBX không Read/Write ⇒ 8 prefab 0 đỉnh): không dùng, không commit (để lại chưa theo dõi trong `Prefabs/Environment`, `Art/UI/City/Meshes|Materials`).
- Codex 015-b: shader `Toy Lit` (2 tông, đốm bóng, viền sáng mép), `Toy Lit Outline` (+ viền tối clip-space), material `M_ToyCar(Outline)_*`. `CarCatalog.material` → `M_ToyCarOutline_*`.
- Đo màn 16 (đếm renderer×pass, chưa tính UI): trước 80 renderer/90 lượt vẽ/32 material; sau 108/167/39 (xe 46 → 90, viền 37). UI ~25 batch ⇒ ~192, dưới ngân sách 200 nhưng sát. `UnityStats` trong Simulator không đáng tin nên không dùng.

## Bước 5 — Theme nền
- `UiTheme.groundMaterial` → `M_ToyGround` (cỏ lấm tấm chữ V + hoa theo tọa độ thế giới); props Bắc: cây tròn/bụi/cây nón/hoa; Nam: rào/hoa/đá/bụi; bỏ `parkedCarPrefab`.
- Khay **bo góc** (`UiTheme.trayCornerRadius` 0,45 ô): mặt trên quạt tam giác, thành bên theo chu vi, viền sáng chạy theo góc; vẫn 1 mesh/1 material. Props đè lên thảm hầm rửa thì bỏ (`SceneThemeView.OnWashGatePad`).
- Test: `RoundedTrayKeepsOneSubmeshHeightAndCutsCorners`, `PropsSkipWashGatePads`; test 013 khay vuông chạy với bán kính 0.

## Kết quả kiểm cả lượt (MCP)
- Biên dịch sạch; EditMode **583 · 582 Passed · 0 Failed · 1 Skipped**; Console chỉ có cảnh báo khối dự phòng do test cố ý.
- Ảnh: `Logs/015-shots/34-board.png` (màn 16 toy), `45-wash-seq.jpg` (màn 03 Play thật: trong hầm → trên thảm → lùi về sạch bùn), `53-final-trio.jpg`.
- Scene `Play.unity`: chỉ 3 thay đổi S4 — `RenderSettings.m_AmbientMode` 0 → 3 (Flat) · `m_AmbientSkyColor` #363A42 → #9AA9C2 · `Directional Light/Light.m_Shadows.m_Type` 2 (Soft) → 0 (None). Nhiễu bố cục của Test Runner đã bỏ (đóng scene, dựng lại từ git + 3 dòng, mở lại).
- **Chưa kiểm:** máy thật/fps, bọt hạt khi rửa nhìn rõ chưa, hầm cạnh trái/phải làm bãi nhỏ đi (màn 16 bãi chỉ ~55% bề ngang).

## Trao đổi
2026-11-26 · Claude → Người làm · [HỎI]: duyệt độ nghiêng, kiểu quay lại ở cổng nước, thứ tự bước, và việc merge 014 trước.
2026-11-26 09:13 · Claude → Người làm · [BÁO]: bước 1 xong trên `015-toy-look`, chờ người làm xem ảnh trước khi sang bước 2.
2026-11-26 09:47 · Claude → Người làm · [BÁO]: 5 bước đã làm một lượt trên `015-toy-look`, chờ người làm chơi thử và chỉ chỗ cần sửa.
