# 011-01 — Làm đẹp nhanh để visual tạm ổn · Bỏ (gộp vào 013)
Đang cầm: Codex (sau khi 011 merge, nhánh `011-01-visual-hand-pass` từ main) · AI: Codex (code + hướng dẫn bước Editor), Claude (visual/review)
Liên quan: 011

> **[CHỐT 25/11 — người làm] Không mở phiếu này.** Bảng V1–V7 được dùng lại trong việc 013 "Khung nhìn" (play-plan §4). Mục "Học sau" vẫn giữ ở đây. Trước đó: hoãn cả phiếu, làm sau 012 nếu còn giờ (ngày 28–29/11). Ba việc rẻ đã tách ra làm ngay: V7 màu portal (điều kiện merge 011), V1 `padding` 0,1 (một số), V3 chữ nút HUD (nợ từ 009).

## Vì sao có việc này
Ảnh 25/11 (màn 28, 29, 30): luật đã đọc được nhưng nhìn còn "hình của lập trình viên". Bãi chỉ chiếm ~65% bề ngang, xung quanh là nền xanh đậm trống, chữ nổi `1A/2B/1/số xe` chồng nhau, nút HUD còn chữ `Button`, cổng ở cạnh trái/phải nhìn từ trên chỉ còn một vạch mỏng.

## Cách làm `[CHỐT 25/11 — người làm]`
Vì sát hạn: **Codex làm nhanh bằng code/tool như 011**; người làm chỉ làm các bước Editor ngắn theo hướng dẫn (gán ô, gõ chữ nút). Phần **học làm tay trong Editor dời sang sau** khi visual tạm ổn (mục cuối). Tool vẫn được dựng lại prefab.

## Việc (Codex; số do Claude chốt)
| # | Việc | Số |
|---|---|---|
| V1 | Camera: `CameraConfig.padding` | 0,5 → 0,1 (bãi ~75% bề ngang) |
| V2 | Ánh sáng: góc Directional Light, ambient | (50°, −30°), cường độ 1,1 · ambient `#4A5568` |
| V3 | HUD: nút Pause/Màn kế còn chữ `Button` → chữ thật (người làm gõ theo hướng dẫn) | nền nút `#F5F7FA`, chữ `#1E2633`, nút chính `#FFB300` |
| V4 | Cổng màu/nước: thêm **tấm sơn màu trên mặt đất** ngay trước cổng để cổng trái/phải đọc được từ trên | 0,9 × 0,35 ô, cùng màu cổng, cao 0,003 |
| V5 | **Bỏ chữ nổi trên bãi:** gợi ý chuyển từ "xe 3 →" sang viền sáng + mũi tên trên chính chiếc xe, rồi bỏ số trên mui xe. Nhãn `1A/2B` của portal/trụ đổi thành vòng màu cùng cặp. Chỉ giữ chữ ở barie | viền `#FFE082`, mũi tên trắng |
| V6 | Cảnh quanh bãi: nền vỉa hè + cỏ, vài cây/hàng rào/đèn đường (Racing `treeSmall`, `fenceStraight`; City `light-square`), sinh theo cỡ bãi | vỉa hè `#9AA3AE`, cỏ `#6FA86B`, không vật nào trong 1 ô quanh mép bãi |
| V7 | `portalColors` → `#FFFFFF`, `#C6FF00`, `#00E5FF` (nếu chưa làm trước khi merge 011) | — |

## Kiểm
Run All + Console sạch. Ảnh màn 01, 28, 30 gửi Claude, so với ảnh 25/11. Stats màn 16 không vượt ~200 batches.
**Build APK đầu tiên khi xong 011-01** (play-plan: muộn nhất 26/11).

## Cắt nếu trễ
V6 phần cây/đèn (giữ nền vỉa hè) → V5 phần nhãn portal. **Không cắt** V1, V3, V4, V5 phần số xe.

## Học sau (khi visual tạm ổn; Codex hướng dẫn từng bước)
Đúng là thao tác chủ yếu là dựng tay, nhập số và kéo file vào ô. Phần đáng học là **hiểu vì sao**, vì đó là thứ các anh sẽ hỏi:
1. Prefab, Prefab Variant, override trên instance: khi nào sửa prefab gốc, khi nào sửa variant (làm lại cổng màu thành 1 gốc + 6 variant).
2. Material dùng chung và draw call: vì sao 6 màu = 6 material chứ không đổi màu từng xe lúc chạy.
3. Pivot, scale, trục: vì sao root xe để scale 1, model con bù hướng.
4. Camera trực giao và ánh sáng: `orthographicSize`, góc nghiêng 70°, bóng.
5. uGUI: Canvas, anchor, safe area, TextMeshPro có dấu.
Mỗi mục ~30 phút: mở prefab tương ứng trong game, Codex giải thích, người làm tự sửa một chỗ nhỏ.
**Tối thiểu trước khi nộp:** tự giải thích được 5 mục trên bằng chính game này.

## Trao đổi
