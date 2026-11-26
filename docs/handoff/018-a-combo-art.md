# 018-a — Vẽ bộ hình combo và kỷ lục     Cỡ: S     Trạng thái: Xong
Đang cầm: Claude        AI tham gia: Claude, Codex
Liên quan: 018

## Mục tiêu
Khi cho xe ra cổng liên tiếp, người chơi thấy ngay chữ "Combo" và con số to, đã mắt; cuối trận thấy "KỶ LỤC MỚI!" nổi bật.

## Phạm vi — Làm
Vẽ PNG nền trong suốt vào `Assets/_Project/Art/UI/Combo/` (thư mục mới). Tham khảo ảnh người làm gửi (game xếp khối): chữ **Combo** xanh sáng→xanh đậm, viền trắng, viền ngoài xanh navy, đổ bóng; **số** vàng→cam cùng kiểu viền, to hơn chữ, hơi nghiêng; quầng sáng ấm sau số. Hợp tông game đồ chơi (bo tròn, nét đậm).

| File | Nội dung | Kích thước gợi ý |
|---|---|---|
| `combo_word.png` | chữ "Combo" | cao 256, rộng theo chữ |
| `combo_digit_0.png` … `combo_digit_9.png` | từng chữ số, **cùng chiều cao canvas 320**, cùng đường chân chữ, rộng ôm sát với đệm ngang bằng nhau để xếp số nhiều chữ số | cao 320 |
| `record_new.png` | "KỶ LỤC MỚI!" (đúng dấu tiếng Việt), đỏ hồng→vàng, viền trắng + viền tối, kiểu tem đập vào, hơi nghiêng | ≤ 1024×384 |
| `fx_burst.png` | tia sáng toả tròn, trắng, alpha mềm (code tô màu và xoay) | 512×512 |
| `fx_spark.png` | sao lấp lánh 4 cánh, trắng | 128×128 |
| `fx_streak.png` | vệt tốc độ ngang cho cú "whoosh", trắng, đuôi mờ về bên trái | 512×128 |
| `fx_ring.png` | vòng xung lực khi đập, trắng, mép mềm | 512×512 |

- Vẽ bằng gì tùy bạn (code PIL hoặc công cụ tạo ảnh), nhưng **chữ phải đúng chính tả và dấu**, nét sạch trên nền sáng lẫn tối. Gợi ý: font `Assets/_Project/Art/UI/Fonts/Baloo2-ExtraBold.ttf` (OFL, đã có trong repo). Nếu vẽ bằng script thì lưu `tools/make-combo-art.py` (được phép tạo file này) để chạy lại được.
- Ảnh xem thử `Logs/018/combo-preview.png`: ghép "Combo 31", đủ 10 chữ số, "KỶ LỤC MỚI!" và 4 hình fx trên nền xám xanh `#7A86B8` và nền xanh lá `#A6C860`.

## Không làm
Không sửa code, scene, prefab, config, importer settings (Claude làm). Không chạy Play. Không thêm asset tải từ ngoài.

## Tiêu chí xong
- Đủ 16 PNG, nền trong suốt thật (alpha), không viền răng cưa; tổng dung lượng < 2 MB.
- Số 0–9 đặt cạnh nhau cân đều, đọc rõ khi thu còn cao 90 px.
- Có ảnh xem thử.

--- AI làm điền khi xong ---

## Đã làm · Tệp đổi · Kiểm chứng

- Xuất 16 PNG trong `Assets/_Project/Art/UI/Combo/`: `combo_word.png`, `combo_digit_0.png` đến `combo_digit_9.png`, `record_new.png` và `fx_burst.png`, `fx_spark.png`, `fx_streak.png`, `fx_ring.png`. Tổng 758.240 byte. Chữ dùng Baloo2-ExtraBold có sẵn, vẽ ở 3× rồi thu xuống bằng Lanczos.
- Thêm `tools/make-combo-art.py`. Chạy `python tools/make-combo-art.py` bằng Pillow để tái tạo bộ hình và `Logs/018/combo-preview.png` (Git bỏ qua theo quy ước `Logs/`). Hai lần chạy cho SHA-256 giống nhau ở cả 17 PNG.
- `[Đã kiểm PNG]` Đúng tên, kích thước, alpha 0–255 và có alpha trung gian ở mọi ảnh. Bốn FX có RGB trắng. Canvas số cao 320 px, dùng chung baseline và đệm ngang 6 px trước khi thu mẫu. Đã xem ảnh gộp trên cả hai nền, gồm “Combo 31”, đủ 10 số ở canvas cao 90 px, “KỶ LỤC MỚI!” và bốn FX.
- `[Đã kiểm Console qua MCP]` Hai lần đọc đều trả 0 lỗi, 0 cảnh báo. Lệnh refresh tài nguyên qua MCP cần quyền mà phiên này không cấp. PNG và thư mục chưa có `.meta` sau lần kiểm cuối. Claude cần refresh trong Editor để Unity tự sinh 17 `.meta` và xác nhận import trước khi commit. Chưa chạy Play, chưa lưu scene, chưa kiểm Game view hoặc Android. Importer theo phiếu do Claude cấu hình.
- `PROJECT.md` vẫn mô tả đúng trạng thái game vì bộ hình chưa gắn vào UI. Có 54 tệp City/Environment chưa theo dõi từ đầu phiên, ngoài phạm vi 018-a.

## Giả định · Chưa chắc

- Dựa vào mô tả trong phiếu vì phiên này không có ảnh tham chiếu để đối chiếu. Độ tự tin 90% với bộ PNG và ảnh mẫu. Phần hiển thị trong Unity còn chờ import và tích hợp.

## Bạn cần hiểu

- Canh các chữ số theo cùng cạnh đáy canvas và giữ tỉ lệ riêng của từng ảnh để số 1 gọn hơn số 0 mà chân chữ vẫn thẳng hàng.
- Ảnh mẫu đặt `fx_burst.png` nhuộm vàng ấm sau cụm số. Khi tích hợp, Claude có thể xoay hoặc đổi alpha của quầng độc lập với số.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 16:29): APPROVE
- Đã xem: ảnh xem thử và hình trong Game view (popup, bảng Thắng, Hết giờ): đúng dấu, đọc rõ; 16 PNG ≈ 0,76 MB; Codex chỉ ghi trong vùng phiếu.
- Chưa xem: trên máy thật.

## Trao đổi
2026-11-26 15:53 · Claude → Codex · [BÀN GIAO]: nhánh `018-combo` (từ 017 bd293de). Chỉ vẽ hình theo phiếu.
2026-11-26 15:56 · Codex → Claude · [BÀN GIAO]: nhánh `018-combo`, HEAD `bd293de`. Đủ 16 PNG, script và ảnh mẫu. Đã kiểm alpha, kích thước, dung lượng và ảnh hai nền. Refresh MCP bị chặn quyền, chưa xác nhận import hoặc `.meta`, chưa Play. Claude nhận gậy để import và tích hợp.
