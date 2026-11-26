# 015 — Hình "đồ chơi" + cổng nước + UI nét     Cỡ: L (chia 5 bước)     Trạng thái: Plan (chờ người làm duyệt)
Đang cầm: Claude        AI tham gia: Claude (lập kế hoạch), Codex (asset khi cần)
Liên quan: 013 (theme/UI, S4 chưa áp), 014 (xe chạy, bùn, trạm rửa). Nhánh: chưa mở (đề xuất `015-toy-look` từ `main` sau khi merge 014).

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

## Trao đổi
2026-11-26 · Claude → Người làm · [HỎI]: duyệt độ nghiêng, kiểu quay lại ở cổng nước, thứ tự bước, và việc merge 014 trước.
