# 016 — Thế giới đồ chơi: trạm rửa giữa phố, xe mảng đậm nhạt, cơ chế vẽ lại, chọn màn, main menu     Cỡ: L (chia 6 phần)     Trạng thái: Đang làm
Đang cầm: Claude        AI tham gia: Claude (code, nối, kiểm), Codex (016-a…d: shader, cơ chế, phố, ảnh IP)
Liên quan: 015 (hầm rửa, Toy Lit, khay bo góc). Nhánh: `016-toy-world` (rẽ từ `015-toy-look` vì 015 chưa merge).

## Yêu cầu người làm (26/11, ảnh màn 7 + ảnh game đỗ xe + ảnh menu IP)
1. Theme khó hiểu ("đồng không mông quạnh có mỗi chỗ rửa"): bãi là **trạm rửa nằm giữa khu phố**, xe ra cổng **chạy theo đường tới lối ra gần nhất** rồi khuất (người làm chọn 26/11).
2. Xe: ngoài viền tối, muốn **mảng màu đậm nhạt** (nóc/mui đậm hơn…), tham chiếu video YouTube `--AF6LwQntQ` và ảnh game đỗ xe.
3. Vẽ lại **toàn bộ cơ chế** theo kiểu game Rollic/Voodoo.
4. Sửa UI **màn chọn màn** cho đẹp (Claude tự chỉnh).
5. **Main menu** đầu game: bố cục học ảnh menu mẫu, **chỉ nút chạy được** (LEVEL n, Chọn màn, Cài đặt; người làm chọn 26/11); nút lấy từ project cũ (`Sand Puzzle Clone/_Project.rar`); ảnh trung tâm là **ảnh IP xe được rửa**, Codex thử tạo ảnh. **Không vẽ mặt ở đâu cả** (giữ `[CHỐT]` cũ; người làm xác nhận 26/11).

## Phân việc
| Phần | Ai | Phiếu |
|---|---|---|
| Shader mảng đậm nhạt | Codex viết, Claude áp | 016-a |
| Cơ chế vẽ lại (prefab mới, giữ cấu trúc) | Codex viết tool, Claude chạy + đổi catalog | 016-b |
| Props phố + vật liệu đường | Codex | 016-c |
| Ảnh IP main menu | Codex thử tạo ảnh | 016-d |
| Đường vòng quanh bãi (mesh theo cỡ màn) + xe ra cổng chạy theo đường | Claude | — |
| Màn chọn màn + main menu | Claude | — |

## Điều dễ làm sai
Đường vòng ăn mất bề ngang màn dọc (bãi nhỏ đi) · xe đi đường đặt chỗ sai làm xe khác chờ vô cớ · xe chạy đường xuyên qua hầm rửa · prefab cơ chế mới thiếu phần bắt buộc (đèn đỏ/xanh, phần quay của trụ, che mờ) · màu cổng/xe khó phân biệt sau khi đổi tông · menu thêm nút giả · số hard-code.

## Cắt nếu trễ (theo thứ tự)
Props phố phụ → ảnh IP (dùng nền tĩnh có sẵn) → vẽ lại cơ chế ít gặp (portal, trụ) → đường vòng (giữ xe ra thẳng như cũ). **Không cắt:** APK thử máy, main menu tối thiểu.

## Trao đổi
2026-11-26 10:08 · Claude → Người làm, Codex · [BÁO]: mở nhánh `016-toy-world` từ `015-toy-look`; giao Codex 016-a…d chạy song song (vùng file tách riêng).
