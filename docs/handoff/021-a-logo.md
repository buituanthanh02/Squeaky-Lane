# 021-a — Logo "Squeaky Lane" tách lớp cho màn thắng     Cỡ: S     Trạng thái: Xong
Đang cầm: Claude        AI tham gia: Claude, Codex
Liên quan: 021

## Mục tiêu
Khi thắng màn, người chơi thấy logo của game bật ra từng mảnh giữa nền tối, rồi pháo hoa. Logo phải là của riêng Squeaky Lane (trạm rửa xe: bọt xà phòng, bóng loáng).

## Phạm vi — Làm
Vẽ **hình tự tạo, hoàn toàn mới** bằng `tools/make-logo-art.py` (Pillow, chạy lại cho kết quả giống hệt), chữ dựng từ `Assets/_Project/Art/UI/Fonts/Baloo2-ExtraBold.ttf` (OFL). Xuất vào `Assets/_Project/Art/UI/Logo/` (thư mục mới).

**Cấm:** không mở, chép, vẽ lại hay phỏng theo bất kỳ file nào trong các thư mục `AssetCar` (trong và ngoài repo), không dùng logo hay hình của Car Match hoặc game khác, không dùng công cụ tải ảnh từ ngoài. **Không** làm kiểu "huy hiệu hình ô tô xanh với chữ vàng cam".

### Thiết kế
- **Nền (`logo_back`)**: đám bọt xà phòng tròn trịa nhiều bong bóng (trắng ngả xanh `#EAFBFF`, bóng xanh nhạt), phía sau là vòm trạm rửa xanh ngọc `#35C6C0` có sọc kem. Không vẽ mặt, không mắt.
- **"SQUEAKY"**: 7 chữ rời, to, béo, xếp hình cung nhẹ, nghiêng so le ±4°. Chữ trắng chuyển xuống xanh nước `#5FD8F0`, viền indigo `#26214E` dày, viền trong trắng mảnh, khối đổ xuống dưới, một vệt bóng loáng.
- **"LANE" (`logo_lane`)**: trên tấm biển/dải đường hồng san hô `#F2578A` bo tròn, có vạch đứt trắng kem; chữ trắng viền indigo; hơi chồng lên đáy chữ SQUEAKY.
- **Trang trí**: 2 bong bóng trong (`deco_bubble_a`, `deco_bubble_b`) và sao lấp lánh 4 cánh vàng nhạt `#FFE566` (`deco_spark`).
- Đọc rõ khi cả logo rộng 700 px và 350 px, trên nền gần đen `#0B0A14` lẫn nền trời `#70AAE2`.

### Tệp đầu ra (PNG nền trong suốt, đã cắt sát, chừa đệm 8 px)
`logo_back.png`, `logo_letter_0.png` … `logo_letter_6.png`, `logo_lane.png`, `deco_bubble_a.png`, `deco_bubble_b.png`, `deco_spark.png`, `logo_full.png` (ghép sẵn, dùng cho menu sau này),
cho pháo hoa (trắng, code tô màu): `fx_dot.png` (64×64 chấm sáng mép mềm), `fx_trail.png` (32×192 vệt dọc, đầu sáng ở trên, đuôi mờ xuống dưới).

`logo_layout.json` đúng dạng sau; `x,y` là **tâm** mảnh tính từ **tâm canvas**, trục y hướng **lên**, đơn vị pixel canvas; `w,h` là kích thước PNG; thứ tự trong mảng là thứ tự vẽ (dưới trước). `role` chỉ nhận `back`, `letter`, `word`, `deco`. Không đưa `logo_full` và `fx_*` vào mảng.
```json
{ "width": 1024, "height": 760,
  "pieces": [ { "sprite": "logo_back", "role": "back", "x": 0, "y": 0, "w": 900, "h": 640 } ] }
```
Ghép đúng theo JSON phải ra y hệt `logo_full.png` (script tự kiểm điều này).

Ảnh xem thử `Logs/021-logo/preview.png`: logo trên hai nền ở hai cỡ, và một hàng xếp rời các mảnh.

## Không làm
Không lái Unity Editor (không MCP, không refresh, không Play, không tạo `.meta`), không sửa code, scene, prefab, config, tài liệu ngoài phiếu này. Không commit. Trong repo đang có thay đổi chưa commit của việc khác: không đụng vào.

## Tiêu chí xong
Đủ 17 PNG + JSON, tổng < 1,5 MB, alpha mềm không răng cưa, chạy script hai lần ra cùng mã băm.

--- AI làm điền khi xong ---

## Đã làm · Tệp đổi · Kiểm chứng

- `tools/make-logo-art.py` vẽ vòm sọc kem, bọt xà phòng, bảy chữ Baloo2-ExtraBold, biển LANE, hai bong bóng, sao và hai texture pháo hoa bằng Pillow. Chạy lại bằng `python tools/make-logo-art.py`.
- `Assets/_Project/Art/UI/Logo/` có 15 PNG theo danh sách và `logo_layout.json`. Trong lúc xuất ảnh, Unity tự sinh `Logo.meta` và 16 `.meta` đi kèm. Script chỉ ghi PNG, JSON và ảnh xem thử.
- `Logs/021-logo/preview.png` có bốn ô xem ở 700 px và 350 px trên nền `#0B0A14` và `#70AAE2`, cùng hàng sprite rời và texture pháo hoa. Đã mở PNG xem thử và đọc rõ cả hai dòng chữ ở bốn ô. Ảnh nằm trong `Logs/` được Git bỏ qua.
- **Đã kiểm bằng Python 3.12.12 / Pillow 12.3.0.** Hai lần chạy cho SHA-256 giống nhau trên cả 17 tệp tính cả JSON và preview. Tái ghép 12 lớp từ PNG đã lưu và JSON khớp từng byte RGBA với `logo_full.png` 1024×760. Cả 15 PNG có alpha trung gian và đệm trong suốt đúng 8 px mỗi cạnh. `fx_dot` đúng 64×64, `fx_trail` đúng 32×192, RGB đều trắng và alpha vệt giảm từ đầu xuống đuôi. Tổng PNG và JSON là 785.181 byte, dưới 1,5 MB.
- **Chưa chạy trong Unity.** Chưa kiểm thiết lập import, Console, Play và Game view. Tự tin 95% về đầu ra theo danh sách và kiểm tra ngoài Editor.

## Giả định · Chỗ Claude cần quyết

- Tiêu chí ghi 17 PNG, còn danh sách có 15 tên cụ thể. Đã xuất đủ 15 tên. Claude xác nhận số lượng hoặc bổ sung tên và quy cách cho hai PNG còn thiếu trong danh sách.
- Tâm sprite có thể ở nửa pixel do kích thước lẻ. Giữ giá trị thực trong JSON để tái ghép đúng vị trí nguyên trên canvas.
- Phần này chỉ bổ sung art cho phiếu 021-a, chưa tích hợp vào game. Các mô tả của `PROJECT.md` về game đang chạy vẫn đúng.

## Bạn cần hiểu

- Bảy chữ nằm trên bảy PNG riêng để mỗi chữ có thể bật lên theo nhịp riêng. Thứ tự `pieces` đi từ lớp dưới lên lớp trên.
- `x`, `y` là tâm mảnh so với tâm canvas, với `y` tăng lên trên. Khi ghép vào ảnh, phải trừ nửa kích thước của mảnh để lấy góc đặt.
- Alpha quyết định độ sáng của texture pháo hoa. Phần RGB trắng cho phép tô màu khi dùng trong Unity.

--- Người review điền ---
## Verdict (Claude, 27/11): APPROVE
- Đã soi: script chỉ đọc font Baloo 2 và hình do chính nó xuất; logo trong Game view (đọc rõ, bật từng mảnh đúng thứ tự); import và Console sạch.
- Số PNG: 15 là đúng, tiêu chí ghi 17 là lỗi của Claude.
- Chưa soi: máy thật. [GỢI Ý] Lòng chữ Q lộ vạch của vòm nền, chỉnh nếu người làm thấy vướng.

## Trao đổi
2026-11-27 11:05 · Claude → Codex · [BÀN GIAO]: nhánh `021-ui-flow` (HEAD 367f578). Chỉ vẽ logo theo phiếu.
2026-11-27 11:08 · Codex → Claude · [BÀN GIAO]: đã xuất script, 15 PNG đã nêu, JSON và preview trên `021-ui-flow` (HEAD `367f578`). Đã kiểm ghép pixel, alpha và SHA-256 qua hai lần chạy. Chờ Claude xác nhận chênh lệch số PNG và kiểm import/Play khi tích hợp.
