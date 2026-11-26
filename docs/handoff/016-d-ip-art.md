# 016-d · Ảnh IP trung tâm main menu: các chiếc xe được rửa (phiếu nhỏ cho Codex)     Cỡ: S–M     Trạng thái: Đang làm
Đang cầm: Codex        AI tham gia: Claude (giao, review, đặt vào menu), Codex (phân tích ảnh mẫu, tạo ảnh)
Liên quan: 016 phần main menu. Nhánh `016-toy-world`.

## Mục tiêu (người chơi)
Mở game thấy ngay **một bức tranh "có hồn"** về thế giới Squeaky Lane: các chiếc xe đồ chơi màu kẹo đang được rửa ở trạm rửa vui nhộn (bọt xà phòng, chổi lăn sọc kẹo, vòi nước, cầu vồng nước), phong cách tranh minh họa game mobile cao cấp như ảnh mẫu người làm gửi (`docs/handoff/016-ref/menu-reference.png`: nhân vật hoạt hình 3D mềm, ánh sáng ấm, trời xanh mây trắng, cảnh vật chi tiết phía sau, màu bão hòa, viền mềm).

## Ràng buộc `[CHỐT]`
- **Không vẽ mặt/mắt/miệng lên xe hay kính xe** (người làm xác nhận lại 26/11). "Có hồn" bằng **dáng**: xe nhún nảy, nghiêng như đang vui khi bọt phủ, một xe vừa rửa xong bóng loáng lấp lánh, một xe còn lấm bùn đang chờ; có thể có người rửa xe/linh vật **không phải xe** nếu cần.
- Không chép nhân vật/IP của game khác (ảnh mẫu chỉ để học phong cách: ánh sáng, độ chi tiết, bố cục).
- Màu xe theo bảng của game: hồng `#E84A8C`, xanh `#35C3FF`, vàng `#FFD23F`, lá `#63CC40`, cam `#FF9629`, tím `#914DFF`.

## Việc
1. **Phân tích ảnh mẫu** `docs/handoff/016-ref/menu-reference.png` (Codex mở ảnh): ghi vào phiếu 5–8 gạch về phong cách (ánh sáng, góc máy, tỉ lệ khung, độ bão hòa, cách tách tiền/hậu cảnh, khoảng trống cho UI trên/dưới).
2. **Viết prompt** tạo ảnh (tiếng Anh) dựa trên phân tích, khung dọc 9:16 (1080×1920), chừa ~18% trên cho thanh UI và ~30% dưới cho nút LEVEL + thanh điều hướng (vùng đó nền đơn giản).
3. **Tạo ảnh** nếu công cụ tạo ảnh có sẵn trong phiên Codex của bạn (không dùng dịch vụ trả phí ngoài, không dùng key của người làm). Lưu `Assets/_Project/Art/UI/Menu/menu_ip.png` (PNG, ≥ 1080×1920). Tạo 2–3 phương án nếu được, đặt `menu_ip_a.png`, `_b`…
4. **Không tạo được** thì ghi rõ lý do vào phiếu và để lại **prompt hoàn chỉnh** trong phiếu (Claude chuyển người làm tự tạo).

**Không làm:** dựng UI menu/scene (Claude), commit, gọi Unity.

## Tiêu chí xong
Có ảnh đúng khung + không mặt trên xe + đọc ra "xe đồ chơi được rửa vui vẻ"; hoặc báo không tạo được kèm prompt.

--- AI làm điền khi xong ---

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-d (phân tích ảnh mẫu + tạo ảnh IP).
