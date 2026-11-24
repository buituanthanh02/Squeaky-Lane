# 011 — Bãi xe và cơ chế ra hình · L ≤ 1,5 ngày · Plan
Đang cầm: Codex (sau khi 010 merge và mở nhánh `011-lot-mechanics` từ main) · AI: Codex (code), Claude (visual/review)
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

## Thứ tự (P1 bắt buộc)
1. P1 `MechanicCatalog` + fallback + test (thiếu prefab → khối, cảnh báo 1 lần).
2. P1 Mặt bãi + bó vỉa (không cần bộ mới).
3. P1 Cổng màu, cổng nước, vũng bùn, cọc, bàn xoay (dựng khối + `cone.fbx`).
4. P1 Đèn (+ vạch dừng), trụ xoay, portal.
5. P2 Thay bằng model City Kit/Racing Kit khi Claude đã chốt danh sách.
6. P3 Nhà nền quanh bãi (cắt đầu tiên).

## Kiểm
Run All + Console sạch. Play: màn có đèn (đổi pha sau mỗi lượt), 08 bàn xoay, một màn portal, một màn trụ xoay, một màn bùn → cổng nước → cổng màu. Ảnh Game view 3 màn gửi Claude. Stats so với 010 (màn 16: 302 batches). Nếu batches tăng mạnh: gộp mặt bãi thành một mesh, bật GPU instancing.

## Điều dễ sai
Mỗi ô một GameObject mặt đất (hàng trăm draw call) · tạo material mới lúc chạy · vật cao che xe ở góc 70° (giữ cao ≤ 0,9) · màu portal trùng màu xe làm người chơi tưởng là cổng màu · prefab tự đọc luật thay vì nhận trạng thái từ `BoardView`.

Báo cáo ~15 dòng; Trao đổi chỉ `[BÀN GIAO]`; không tự commit.

## Trao đổi
