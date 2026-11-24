# 010 — Xe Kenney · L ≤ 1 ngày · Đang làm
Đang cầm: Codex · AI: Codex (code), Claude (visual/review) · Nhánh: 010-kenney-cars từ main e290e2d
Mục tiêu: xe Kenney mỗi màu một dáng, đọc rõ màu cổng; giữ luật/input/animation. [CHỐT chat] Qua sơn đổi cả dáng theo màu mới, giữ root/collider lưới.
Kế hoạch: kiểm FBX/UV/CC0 và chuyển asset đã xong → menu tạo colormap/material → prefab bọc → catalog → Run All/Console → Play/ảnh/Stats/APK.
Dáng: pink hatchback-sports; blue sedan; yellow taxi; green suv; orange van; purple sedan-sports; truck/delivery cho xe tải.
P1: chỉ FBX dùng tới + colormap tại Art/Cars/Kenney; root pivot giữa thân, +X, rotation 0/scale 1; footprint 2×1 hoặc 3×1 có khe; collider/CarView ở root.
P1: 6 material + 6 colormap đổi riêng ô thân theo VisualPalette; CarCatalog màu/loại → prefab, fallback khối/cảnh báo một lần; bùn hiện/rửa sạch.
Tệp: CarView, BoardView, GameBootstrap/ConfigProvider, CarCatalog mới, menu Editor/test; [Editor] model/texture/material/prefab/catalog và gán tham chiếu.
Không đổi Core/FeelConfig/package/settings/VFX/tutorial; không dọn code. Cắt P3 chống che → P2 ánh sáng (config), không cắt P1.
Chờ chốt trước commit mốc A: dáng hiện tại Blue→van, Green→truck, Orange→sedan-sports, Purple→suv khác bảng; Green hình xe tải nhưng loại thường dễ gây hiểu nhầm. Scene có 3 anchor vùng chọn màn đổi về 0 ngoài gán catalog; đã hỏi có chủ ý không, chưa sửa/hoàn lại.
Kiểm V1–V3 chưa hoàn tất: người làm muốn commit mốc WIP trước, không có kết quả Run All để ghi đã qua. Còn Play/cổng/Undo/màn08/portal/tải/bùn/Play lại, Console sạch/ảnh Claude duyệt/nhắc build APK. Stats TRƯỚC: 248 Batches, 88 SetPass, Tris 3.1k, Verts 6.3k, 59.3 FPS, 780×439/16:9; chưa có số SAU.
Đã có 6 prefab thường, 6 material/colormap, catalog và GameRoot đã gán; kiểm pixel 6/6 đúng/chỉ đổi ô thân. Kiểm prefab phát hiện sai bảng dáng, chưa kết luận qua; 6 truckPrefab còn trống. C# biên dịch qua nhưng chưa Run All/Play Kenney/P2/P3. Ngoài phạm vi: 2 .meta từ 009-01 và TMP fallback thêm glyph; giữ nguyên, chưa tự gom vào mốc commit.
Bạn cần hiểu: model con bù hướng/cỡ, root theo lưới; dùng sharedMaterial có sẵn, không tạo material runtime. Kết quả/chưa kiểm sẽ cập nhật tại đây.
Verdict: chưa review; vẫn Đang làm. Commit WIP do người làm thực hiện, không đồng nghĩa 010 đã xong/đã test. Scene chỉ chọn hunk gán catalog cho mốc này; các anchor chưa rõ và font fallback/2 .meta 009-01 để ngoài. Báo cáo khoảng 15 dòng, Trao đổi chỉ [BÀN GIAO].
## Trao đổi
