# 016-d · Ảnh IP trung tâm main menu: các chiếc xe được rửa (phiếu nhỏ cho Codex)     Cỡ: S–M     Trạng thái: Xong (Claude review)
Đang cầm: Claude        AI tham gia: Claude (giao, review, đặt vào menu), Codex (phân tích ảnh mẫu, tạo ảnh)
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

## Phân tích ảnh mẫu

- Khung gần 9:16. Cụm chủ thể lớn nằm ở nửa giữa và thấp hơn tâm ảnh, với ba điểm nhìn tạo thành tam giác.
- Máy nhìn ba phần tư, hơi từ trên xuống. Mặt trên tảng đá, mặt nước và làng phía xa cùng hiện rõ, tạo chiều sâu dù khung hẹp.
- Nắng ấm bắt mạnh trên bề mặt hướng lên. Bóng đổ và vùng khuất mềm, còn mảng trời và mặt nước xanh cân lại sắc cam vàng của tiền cảnh.
- Hình khối bo tròn, tỉ lệ hoạt hình và chất liệu có độ nổi. Viền khối mềm, vùng chủ thể có độ nét cao hơn cảnh xa.
- Tiền cảnh có đá và cây ôm mép khung. Mặt nước xanh làm lớp đệm tương phản sau chủ thể, rồi đến nhà, đảo và mây có màu nhẹ hơn, ít cạnh sắc hơn.
- Màu bão hòa tập trung ở chủ thể và vài điểm nhấn. Hậu cảnh vẫn có chi tiết nhỏ như mái nhà và thuyền.
- Thanh trên nằm chủ yếu trên nền trời. Nút LEVEL đè lên nền đá và cây khá nhiều chi tiết. Ảnh Squeaky Lane giữ ~18% trên là trời đơn giản và dành ~30% dưới cho một mặt sân liền mạch, ít tương phản.

## Prompt ImageGen (tiếng Anh)

Prompt chung đi cùng đoạn bổ sung của từng tệp. Hai tệp dùng ImageGen tích hợp.

```text
Create a finished, full-bleed 9:16 portrait PNG illustration for the main menu of Squeaky Lane, an original mobile toy-car puzzle game. Target 1080 × 1920 or higher, an edge-to-edge vertical composition. Render a sunny miniature neighborhood car wash as polished stylized 3D storybook game art: soft bevels, chunky toy-scale proportions, satin-painted plastic, warm sun from upper left, soft contact shadows, clean cool sky and a gentle ocean of atmospheric depth between foreground and background. Use a slightly elevated three-quarter camera with moderate perspective, showing roofs, side panels and wheels. Make the action readable at phone thumbnail size. Keep the upper 18 percent almost empty with pale blue sky and a soft cloud wisp at an edge for UI. Concentrate the vehicles and the wash equipment entirely in the middle band from 18 to 70 percent down the frame, leaving a margin on both sides. Keep the bottom 30 percent a naturally continuous, even, low-contrast wash forecourt in soft blue-gray, with minimal texture, for a LEVEL button and navigation overlay. Avoid abrupt bands or stripes where these zones meet. Use subtly softened low-rise buildings and rounded trees behind, with quieter, cooler colors and lower contrast than the subjects.
Show exactly three original rounded toy cars, each with a distinct silhouette and a different stage of the wash. A candy pink compact hatchback (#E84A8C) tilts joyfully on its suspension under white soap foam and a candy-striped cylindrical wash roller. A glossy sky-blue coupe (#35C3FF) has rolled out clean, with one or two tiny sun glints, and a mustard-yellow mini pickup (#FFD23F) waits with a few clearly readable brown mud splashes. Add an arcing water jet with subtle rainbow refraction concentrated near the wash arch and a few bubbles around the foam. Keep colored main body panels saturated and readable. Secondary accent colors may be leaf green #63CC40, orange #FF9629 and violet #914DFF. Give the vehicles personality solely through silhouette, body tilt, wheel stance and the visual story of mud, foam and clean paint. Every vehicle is an ordinary faceless toy: smooth, undivided dark blue tinted windscreens with soft uniform reflections, narrow restrained headlight strips and simple unbroken body-colored front panels. No eyes, eyebrows, pupils, mouth, grin, face-like patterns or expressions anywhere on the vehicles. Original shapes and staging. No people, mascots, copied characters, title, signs with writing, logo, UI, buttons, panels, watermark, text or border.
```

**Bổ sung cho `menu_ip.png`**

```text
Stage the sky-blue coupe leaving at left front in a near-pure side profile facing left. Show the candy-pink hatchback directly side-on under the roller, facing left, so its front fascia is hidden by the angle. Let one of its near-side wheels rise slightly on a gentle suspension tilt with soap on its roof. Show the muddy yellow pickup from a rear-side view, facing left, on a lane behind and to the right. Keep all vehicle bodies, tire contact patches and equipment above 68 percent of frame height, with quiet low-contrast matte blue-gray pavement below. Shape all visible bumpers as plain uninterrupted pieces of solid body-colored plastic, with flush, tiny side lamp details. Absolutely no grilles, inset seams, curved slots, lower air intakes, or bright paired features on any bumper that could evoke a mouth or eyes. Frame a substantial, rounded pastel teal and cream wash arch and bright violet-striped roller to clearly tell the wash story.
```

**Bổ sung cho `menu_ip_a.png`**

```text
Alternate camera and composition: look diagonally into the car wash from the rear of the cars, at a gentle downward angle. Put the wash arch left of center with two peach-and-cream vertical car wash rollers. Let the clean blue coupe exit toward the right in a perfectly broad side profile facing right, with plain bodywork, its side window and doors prominent, and its front fascia fully out of view. The lively pink compact hatchback is centered between the rollers facing into the wash away from camera; show its back and side only, one wheel slightly up in the suds. The muddy yellow pickup waits at the left edge behind the hatchback in back three-quarter view, all three fully in frame. The pink back has an uninterrupted smooth panel and inconspicuous taillamp strips at its outer corners. No visible paired front lamps, dark bumper cutouts, grilles, or upturned fascia lines. Make the setting a small toy-town plaza with the yellow pickup and the arch slightly further back than the clean blue coupe. Limit finer detail to the arch and the cars. Keep the lower 30 percent entirely empty below all car and plant silhouettes, softly textured blue-gray matte pavement. Keep the upper 18 percent quiet blue sky.
```

## Đã làm · Tệp đổi · Kiểm chứng

- `Assets/_Project/Art/UI/Menu/menu_ip.png` — bản chính. Xe hồng nhìn ngang dưới chổi tím, xe lam sạch ở tiền cảnh và xe vàng lấm bùn đang chờ. Chọn bản này vì các trạng thái rửa đọc rõ qua hình dáng nhìn ngang và bọt.
- `Assets/_Project/Art/UI/Menu/menu_ip_a.png` — bản thay thế với góc nhìn từ sau, chổi đứng và xe sạch ở bên phải. Cả hai giữ kính trơn và chỉ có đèn xe thông thường.
- `Assets/_Project/Art/UI/Menu.meta` và hai `.png.meta` — Unity tự sinh. Đọc tĩnh thấy hai ảnh đang dùng `TextureImporter` kiểu mặc định (`textureType: 0`).
- `docs/handoff/016-d-ip-art.md` — phân tích, prompt và bàn giao. Đây là ảnh nguồn nên chưa làm thay đổi mô tả tính năng trong `PROJECT.md`.
- **Đã kiểm tệp và nhìn ảnh.** Hai ảnh gốc 941×1672 được nội suy lên 1080×1920 đúng 9:16. Đọc lại hai PNG thành công, RGB 24-bit, lần lượt 3,16 và 3,11 MB. Quan sát được ba trạng thái bùn, bọt và sạch, vùng trời trên và mặt sân dưới đủ rộng theo bố cục dự kiến. Bỏ hai bản nháp góc chính diện vì chi tiết cản có thể gợi hình miệng.
- **Chưa chạy trong Unity.** Claude cần cấu hình import kiểu Sprite nếu ghép ảnh bằng UI Image, rồi kiểm Console và ảnh Game view sau khi đặt nút LEVEL, thanh trên và thanh điều hướng. Kiểm cắt ảnh trên màn hẹp vì các xe nằm gần mép ngang. Màu là màu mục tiêu của thân xe, thay đổi theo ánh sáng của tranh.
- **Tự đánh giá 90%** cho đầu ra ảnh và phạm vi phiếu. Phần còn lại phụ thuộc cách menu đặt và co ảnh khi chạy.

## Bạn cần hiểu

- Ba thân xe có hình dáng và độ nghiêng khác nhau. Bùn, bọt và điểm lóe sáng kể lại thứ tự chờ, rửa và ra khỏi trạm.
- Vùng trống là một phần của ảnh để UI đặt đè. Khi khung màn hẹp hơn 9:16, chế độ phủ toàn màn có thể cắt mép ngang và che một phần xe.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 10:30): APPROVE
- Đã soi: `menu_ip.png` 1080×1920, 3 xe ở 3 giai đoạn rửa, không mặt, đúng tông ảnh mẫu; đặt vào main menu và nền Chọn màn.
- `menu_ip_a.png` là phương án dự phòng, chưa dùng.

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-d (phân tích ảnh mẫu + tạo ảnh IP).
2026-11-26 10:15 · Codex → Claude · [BÀN GIAO]: đã phân tích ảnh mẫu, lưu hai PNG 1080×1920 và kiểm nội dung/định dạng tệp trên nhánh 016-toy-world, HEAD 55c3d85, chưa commit. Claude nhận để kiểm import/.meta và ghép ảnh vào menu. Unity và Game view chưa chạy.
2026-11-26 10:30 · Claude → Người làm · [BÁO]: đã nối vào game trên `016-toy-world`.
