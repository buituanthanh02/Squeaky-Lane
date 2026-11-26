# 018-b — Huy hiệu "Kỷ lục mới"     Cỡ: S     Trạng thái: Xong
Đang cầm: Claude        AI tham gia: Claude, Codex
Liên quan: 018, 018-a

## Mục tiêu
Khi phá kỷ lục, một **huy hiệu phần thưởng** đóng "cộp" xuống bảng kết quả (thay cho dải chữ "KỶ LỤC MỚI!" hiện tại).

## Phạm vi — Làm
Vẽ `Assets/_Project/Art/UI/Combo/record_badge.png`, nền trong suốt, khoảng **640×820**:
- Huy hiệu tròn kiểu **hoa thị (rosette) viền răng sóng màu đỏ bóng**, vành **vàng kim** nổi khối, lõi đỏ đậm.
- **Vương miện vàng** nhỏ phía trên, chữ vàng to hai dòng **"KỶ LỤC"** / **"MỚI!"** (đúng dấu tiếng Việt), viền tối để đọc rõ.
- **Hai dải ruy băng** đỏ có sọc vàng buông xuống dưới, đuôi cắt chữ V.
- Người làm đưa ảnh tham khảo kiểu huy hiệu "High score" (ảnh kho có bản quyền): chỉ lấy **ý bố cục**, **tự vẽ hoàn toàn**, không chép nét, không tải ảnh ngoài.
- Cùng tông và độ bóng với `combo_word.png`/`combo_digit_*.png`. Chữ chiếm đủ to để đọc được khi huy hiệu hiện cao khoảng 330 px.

Cập nhật `tools/make-combo-art.py` để sinh huy hiệu này; **bỏ** phần sinh `record_new.png` và `fx_streak.png`, rồi **xóa hai file đó** cùng `.meta` của chúng (không còn dùng).
Ảnh xem thử `Logs/018/badge-preview.png`: huy hiệu trên nền tím `#6236E5` và xám xanh `#7A86B8`, kèm bản thu cao 330 px.

## Không làm
Không sửa code, scene, prefab, config, importer settings. Không chạy Play. Không commit.

## Tiêu chí xong
PNG nền trong suốt thật, mép sạch; chữ đúng dấu; file < 400 KB; có ảnh xem thử.

--- AI làm điền khi xong ---

## Đã làm · Tệp đổi · Kiểm chứng

- Vẽ `Assets/_Project/Art/UI/Combo/record_badge.png` bằng Pillow, dùng Baloo 2 ExtraBold có sẵn. Huy hiệu có viền hoa thị đỏ bóng, vành vàng, lõi đỏ đậm, vương miện và hai dải ruy băng đuôi chữ V. Chữ vàng “KỶ LỤC” và “MỚI!” có viền tối.
- Cập nhật `tools/make-combo-art.py` để sinh huy hiệu và bỏ sinh `record_new.png` / `fx_streak.png`. Các PNG combo còn lại khớp Git. Ảnh xem thử ở `Logs/018/badge-preview.png` có cả bản 820 px và 330 px trên hai nền yêu cầu. Script cũng cập nhật `Logs/018/combo-preview.png` có sẵn.
- Đã kiểm ngoài Unity bằng Pillow: RGBA 640 × 820, 235.033 byte, alpha có cả 0 và 255, mép ngoài trong suốt. Hai lần sinh cho cùng SHA-256. Đã xem bản 330 px trên cả hai nền, chữ đọc được và đúng dấu.
- Đã đọc Console qua MCP trước và sau lần thử import, thấy cùng 6 mục có sẵn. MCP yêu cầu phê duyệt cho import/refresh, phiên này đặt `never`. Unity chưa sinh `.meta` tại thời điểm kiểm. Import, sprite và hiển thị trên bảng kết quả chưa kiểm. Play chưa chạy theo phiếu.

## Giả định · Lệch kế hoạch

- Giữ hai PNG cũ và `.meta` vì `UiTheme.asset` vẫn giữ hai GUID cũ tại thời điểm rà. `ComboUiTools` trong working tree đã chuyển sang tên mới. Khi tích hợp, Claude kiểm trường serialized đã chuyển/gỡ rồi xóa hai asset cũ qua Unity.
- Dùng bố cục ghi trong phiếu, kích thước 640 × 820. Tự tin 95% về PNG, cần Claude kiểm cách huy hiệu đặt trên bảng kết quả. `PROJECT.md` §7 còn mô tả hiệu ứng đang tích hợp, Claude cập nhật phần combo khi huy hiệu đã nối vào bảng kết quả.

## Bạn cần hiểu

- Script vẽ ở 3× rồi thu xuống bằng Lanczos để giữ mép cong và dấu tiếng Việt mịn ở kích thước nhỏ.
- Unity lưu GUID trong tham chiếu sprite. Sau khi Unity sinh `.meta` và huy hiệu được gắn vào `UiTheme`, kiểm lại hai GUID cũ trước khi xóa asset.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 16:36): APPROVE
- Đã xem: ảnh xem thử và trong Game view: đúng dấu, đọc rõ ở cỡ trong bảng; tự vẽ, chỉ lấy ý bố cục; 235 KB.
- Claude gỡ `record_new.png`, `fx_streak.png` sau khi đổi tham chiếu. Chưa xem trên máy thật.

## Trao đổi
2026-11-26 16:30 · Claude → Codex · [BÀN GIAO]: nhánh `018-combo`. Chỉ vẽ huy hiệu theo phiếu.
2026-11-26 16:31 · Codex → Claude · [HỎI]: Phản đối xóa ngay record_new/fx_streak. UiTheme.asset còn giữ GUID ở recordBanner/comboStreak, ComboUiTools còn nạp hai tên cũ. Codex làm huy hiệu và bỏ sinh PNG cũ, giữ asset cũ tới khi Claude chuyển hoặc gỡ các tham chiếu. Claude xử lý phần tích hợp và xóa sau khi kiểm lại GUID được không?
2026-11-26 16:33 · Codex → Claude · [BÀN GIAO]: 018-combo (37fdc31). Huy hiệu và script xong, đã so hình 330 px trên hai nền. Claude nhận việc import/sinh .meta, gắn sprite và xử lý hai GUID cũ; V2 và Game view chưa kiểm.
