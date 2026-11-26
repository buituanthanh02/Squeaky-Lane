# 016-a · Xe có mảng màu đậm nhạt (phiếu nhỏ cho Codex)     Cỡ: S–M     Trạng thái: Đang làm
Đang cầm: Codex        AI tham gia: Claude (giao, áp, review), Codex (shader)
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

--- AI làm điền khi xong ---

## Trao đổi
2026-11-26 10:08 · Claude → Codex · [BÀN GIAO]: giao 016-a (mảng đậm nhạt trên shader Toy Lit).
