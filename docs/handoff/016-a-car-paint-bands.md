# 016-a · Xe có mảng màu đậm nhạt (phiếu nhỏ cho Codex)     Cỡ: S–M     Trạng thái: Xong (Claude review)
Đang cầm: Claude        AI tham gia: Claude (giao, áp, review), Codex (shader)
Liên quan: 015-b (`Art/Shaders/ToyLit.shader`, `ToyLitOutline.shader`, `ToyLookTools.cs`). Nhánh `016-toy-world`.

## Mục tiêu (người chơi)
Xe hiện đã có 2 tông sáng/tối + viền tối nhưng **thân một màu phẳng**. Người làm muốn như game đỗ xe đồ chơi (ảnh mẫu: xe đỏ/xanh/vàng bóng): **nóc/mui (mặt hướng lên) đậm hơn hoặc nhạt hơn thân bên**, có **mảng màu tách bạch** theo hướng mặt, đọc ra khối 3D rõ trên điện thoại ở camera trực giao nghiêng 58°. Màu xe vẫn nhận ra ngay để chọn cổng.

## Phạm vi — Làm (vùng `AGENTS.md` §1.3; chỉ viết code, kiểm tĩnh; **không gọi Unity MCP/CLI**)
- Sửa `Assets/_Project/Art/Shaders/ToyLit.shader` (và giữ `ToyLitOutline.shader` dùng chung pass qua `UsePass`, cùng Properties):
  1. **Mặt nạ sơn thân**: chỉ tô mảng trên các texel **ô sơn thân** của colormap Kenney (kính, lốp, đèn, viền đen giữ nguyên). Đọc `Scripts/Editor/KenneyCarsWindow.cs` (hàm `RecolorBody`, lưới 8×4, 4 ô thân) để biết ô thân; truyền thành Property (ví dụ `_BodyCells` hoặc dải UV) hoặc tự suy ra mặt nạ từ UV. Material hiện có (`Art/Cars/Toy/M_ToyCar*_<màu>.mat`) phải dùng được không cần gán thêm (giá trị mặc định đúng cho Car Kit).
  2. **Mảng theo hướng mặt** (không gian thế giới, normal mượt hay phẳng đều được): mặt hướng lên (`N.y > _TopThreshold` ~0,6) nhân `_TopTint` (mặc định đậm hơn ~15%, hơi bão hòa hơn, ví dụ HSV V×0,85 S×1,1); mặt hướng camera/đầu xe giữ màu gốc; mặt bên hướng xa đèn đã có ramp. Chuyển mảng **sắc nét** (smoothstep hẹp `_TopSmooth` .04), không gradient loang.
  3. **Dải thấp tối** (tùy chọn): phần thân thấp (gần gầm, theo chiều cao object `_LowerHeight`) tối thêm `_LowerShade` .85 để xe "đứng" trên mặt đất.
  4. Mọi số là Property có `[Header("016 Paint bands")]` + `Range`, chỉnh trong Inspector.
- Sửa `Assets/_Project/Scripts/Editor/ToyLookTools.cs` nếu cần để ảnh preview `Logs/016-car-bands-preview.png` render 6 màu xe (Car_*.prefab, material `M_ToyCarOutline_*`), góc 58°, đèn (50,−30,0) cường độ 1, ambient `#9AA9C2`, kèm một xe material cũ để so. Menu mới hoặc tham số mới: `Tools/Squeaky Lane/016 Toy/Car Bands Preview`. Không đổi GUID/material đã có.

**Không làm:** đổi CarCatalog/prefab xe/scene; texture mới cho từng xe; package/settings; commit; gọi Unity.

## Tiêu chí xong (Claude đo) · V2/V3
1. Shader biên dịch sạch trong Editor (Claude kiểm `ShaderUtil`), không khai báo trùng biến Unity (bài học 015-b: `_SpecColor` đã có sẵn trong Lighting.cginc).
2. Ảnh preview: nóc/mui và thân bên **khác tông rõ** ở cả 6 màu; kính/lốp không bị nhuộm; xe vàng/cam vẫn phân biệt được.
3. Không thêm pass mới (chi phí giữ nguyên).

## Điều dễ sai
Nhuộm cả kính/lốp · mảng loang gradient · tối quá làm xanh dương/tím lẫn nhau · normal object-space (xe xoay 90° thì mảng sai) · khai báo trùng biến built-in.
**Cắt nếu trễ:** dải thấp tối.

## Phản đối kế hoạch
`Toy Lit` cũng dùng cho cây/đạo cụ. UV của mesh đạo cụ có `V=0,5`, đúng biên hàng sơn Kenney. Mặt nạ lấy cả đường biên có thể làm tối mặt trên của đạo cụ. Lùi miền mặt nạ nửa texel theo hai trục để chỉ nhận mẫu bilinear nằm trọn trong ô sơn.

--- AI làm điền khi xong ---

## Đã làm · Tệp đổi · Kiểm tra

- `Assets/_Project/Art/Shaders/ToyLit.shader` nhận diện các ô thân theo UV colormap, lấy normal thế giới để phân mảng trên mui và áp tint trước ánh sáng. Giá trị mặc định gồm `_TopTint = 0,85` (hệ số sRGB), `_TopSaturation = 1,1`, `_TopThreshold = 0,6` và `_TopSmooth = 0,04`. Mặt bên tiếp tục dùng ramp 015.
- `Assets/_Project/Art/Shaders/ToyLitOutline.shader` có cùng bốn Properties trong Inspector và dùng lại pass `FORWARD`. Số pass giữ nguyên. Material hiện có nhận giá trị mặc định từ shader.
- `Assets/_Project/Scripts/Editor/ToyLookTools.cs` thêm menu **Tools → Squeaky Lane → 016 Toy → Car Bands Preview**, lưu `Logs/016-car-bands-preview.png`. Hàng trên là hồng, xanh dương, vàng và vàng đối chứng 015. Hàng dưới là xanh lá, cam, tím. Xe đối chứng dùng bản sao material tạm với hai hệ số trung tính để so cùng xe, góc và ánh sáng.
- **Đã kiểm tĩnh.** Mặt nạ nhận đúng tâm 4/32 ô atlas. Đọc UV gốc của cả 7 FBX Kenney, gồm xe tải, và đối chiếu thấy tất cả UV thuộc ô sơn vẫn qua mặt nạ sau khi lùi biên. Đã kiểm UV đạo cụ ở V=0,5 bị loại, bốn Properties trùng nhau, pass `FORWARD` dùng chung, đủ prefab và material cho preview, và `git diff --check` sạch ở bốn tệp thuộc phiếu.

## Giả định · Chưa kiểm

- Bỏ dải gầm tùy chọn. Thang và gốc Y local phụ thuộc mesh, còn mục tiêu chính là tách mảng mui bằng normal. `PROJECT.md` §7.5 vẫn mô tả đúng shader và vật liệu mà `CarCatalog` dùng.
- Chưa chạy Unity, biên dịch shader, Console, menu preview, Play hoặc đo GPU. Claude cần kiểm `ShaderUtil`, ảnh đủ sáu màu, ranh giới kính/lốp và độ phân biệt vàng–cam trước khi duyệt V2/V3. Việc giữ số pass xác nhận qua mã, còn phép tính mỗi pixel có tăng.
- Tự đánh giá khoảng 90% đáp ứng phạm vi và quy tắc ở mức kiểm tĩnh. Mức tối của mui dưới ánh sáng thực tế còn chờ xem ảnh.

## Bạn cần hiểu

- UV trỏ tới ô màu trong colormap và xác định phần nào là sơn. Normal thế giới xác định phần sơn nào hướng lên. Xoay xe quanh trục Y giữ nguyên thành phần Y của normal.
- Đổi hệ số tint và bão hòa về 1 trả lại màu nền 015 cho phép so trực tiếp, đồng thời giữ ánh sáng, bóng nhựa và viền trên cả hai bản.

--- Người review điền ---
## Verdict (Claude, 2026-11-26 10:30): APPROVE có sửa
- Đã soi: shader biên dịch sạch; ảnh preview 6 màu; trong game màn 03/08/16.
- [CHẶN → Claude đã sửa] mảng sơn tô nhầm lên vật khác dùng chung shader: UV mặt đường rơi vào ô sơn thân nên có 2 ô vuông tối giữa mỗi phố. Mặc định shader đổi thành tắt (`_TopTint` 1, `_TopSaturation` 1); chỉ material xe đặt 0,88 / 1,08.
- Chưa soi: máy thật.

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-a (mảng đậm nhạt trên shader Toy Lit).
2026-11-26 10:13 · Codex → Claude · [BÀN GIAO]: Đã làm mảng sơn và menu preview trên 016-toy-world (HEAD 55c3d85, thay đổi chưa commit). Kiểm tĩnh mask/UV 7 FBX, pass và đầu vào preview đạt. Chờ Claude biên dịch, kiểm Console và render ảnh V2/V3.
2026-11-26 10:30 · Claude → Người làm · [BÁO]: đã nối vào game trên `016-toy-world`.
