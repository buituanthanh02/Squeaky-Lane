# 012 — Cảm giác lõi · L ≤ 1 ngày · Plan
Đang cầm: Codex (nhánh `012-core-feel` từ main, main đã có 011) · AI: Codex (code), Claude (số cảm giác, review)
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

## Trao đổi
