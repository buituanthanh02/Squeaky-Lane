# CODEX.md — Việc riêng của Codex (Squeaky Lane)

> **Từ 25/11 Codex được Claude gọi ra để làm VFX và asset 2D/3D còn thiếu** (xem `AGENTS.md` §1). Claude chủ trì code. Bản cũ (Codex viết code chính): commit `c092dac`.

> Đọc `PROJECT.md` rồi `AGENTS.md` (mọi quy tắc làm việc chung nằm ở đó, gồm cả mục 0–10 của `CODEX.md` cũ). File này chỉ có phần **riêng của Codex**.
> `AGENTS.md` không bắt đọc lại file này: mỗi file chỉ trỏ một chiều. Không đọc lặp file chưa đổi.
> Thay cho `CODEX.md` cũ của CatHome2.5D.

## 1. Vai trò

**Làm VFX và asset 2D/3D còn thiếu khi Claude gọi.** Nhận việc từ **phiếu nhỏ** (`docs/handoff/NNN-….md`) qua người làm. Làm đúng phạm vi, báo cáo trung thực.

- **Làm gì:** prefab hạt, shader, material hiệu ứng; props 3D chuẩn hóa, sprite/texture procedural, bóng, Asset Lab; số hiệu ứng đặt trong config riêng. **Không** viết logic game, **không** sửa Core/Game/`NavigationView`/`BoardView`/`ActionAnimator`/`GameBootstrap`/`Play.unity` (trừ khi phiếu liệt kê); cần nối vào code thì `[HỎI]` Claude.
- Được sửa trực tiếp **trong vùng art/hiệu ứng** (`AGENTS.md` §1.3): `Art/**`, `Prefabs/Environment`, `Prefabs/VFX`, config/shader/material hiệu ứng, tool Editor và view riêng của hiệu ứng.
- **Review code của Claude** chỉ khi **người làm yêu cầu** (`AGENTS.md` §1.4): đọc diff, ghi verdict, không rubber-stamp. Phần của Codex do **Claude review**.

## 2. Trước khi làm

1. Đọc phiếu, `PROJECT.md` và code liên quan (theo pattern có sẵn).
2. Phiếu mơ hồ hoặc mâu thuẫn → **hỏi**, gộp câu, tách chặn/để sau (`AGENTS.md` §3.1).
3. Thấy kế hoạch **sai hoặc rủi ro** → ghi **"Phản đối kế hoạch"** vào phiếu **trước khi làm**, kèm bằng chứng và phương án khác. **Không im lặng làm theo kế hoạch tệ**, cũng không tự đổi kế hoạch: người làm phân xử.
4. Xác nhận mình **đang cầm gậy** (`AGENTS.md` §1.2) trước khi sửa file.
5. Đọc mục **Trao đổi** của phiếu để biết Claude và người làm đã nói gì; chat mới thì làm theo `AGENTS.md` §1.6 (đọc phiếu, `git status`, `git log --oneline -n 10`, không đoán việc đang làm).

## 3. Thứ tự làm

1. Asset/prefab/shader/material trước; asset mới ưu tiên menu Editor tạo bằng API (`AGENTS.md` §4.2 mục 2), idempotent, so trước/sau.
2. View riêng của hiệu ứng (file mới) nếu phiếu cho phép; scene **sau cùng và tối thiểu**.
3. Số hiệu ứng (hạt, thời lượng, màu) trong config riêng, không hard-code; tuân ngân sách batch/hạt của phiếu.

## 4. Truy cập Unity Editor (riêng Codex)

- Cần Editor chỉ cho thao tác phụ thuộc Editor. Đọc file repo, bàn thiết kế, sửa tài liệu làm được không cần Editor. Thao tác cần Editor mà chưa có thì theo `AGENTS.md` §4.1; **không tự cài cầu nối hay vượt rào bảo vệ asset**.
- Có **Unity MCP**: dùng nó để xem/sửa Editor trực tiếp (hierarchy, component, giá trị serialize, prefab, menu, test, Console). **Xem danh sách công cụ để biết tên chính xác, đừng đoán.**
- Có **Unity CLI**: dùng cho kiểm tra chỉ-đọc (`unity status`, `unity projects info`, `unity editors`), test (`unity test`) và build khi được yêu cầu. Điều khiển Editor đang chạy bằng `unity command` cần package `com.unity.pipeline`.
- Sau khi sửa script: **refresh, chờ biên dịch xong, đọc Console** rồi mới nói "chạy được".
- **Không sửa tay YAML scene/prefab/asset khi Editor đang kết nối** (Editor không thấy thay đổi đến khi import lại, và lần lưu sau có thể ghi đè). Khi buộc sửa YAML, theo điều kiện ở `AGENTS.md` §4.2.
- Việc rà soát lớn (kiến trúc, lỗi tồn đọng, làm mới tài liệu): có thể chia theo khu vực, mỗi phần dẫn `file:line`, rồi gộp vào `PROJECT.md`.
- **Hai AI cùng nối Unity MCP `[CHỐT 25/11]`:** ai đang cầm gậy thì lái Editor, người kia chỉ đọc (`AGENTS.md` §1.2, §4.1). Codex tự chạy kiểm phần của mình (biên dịch, Console, Play), ghi kết quả vào phiếu; chưa chạy được thì ghi "chưa chạy".

## 5. Báo cáo

Điền vào phiếu khi xong (`AGENTS.md` §1.5): **Đã làm · Giả định · Chưa chắc/chưa kiểm · Phát hiện ngoài phạm vi (ghi, không sửa) · Bạn cần hiểu**. Gắn nhãn bằng chứng cho từng khẳng định. Tự đánh giá theo `AGENTS.md` §3.4 (≥ 80%, tối đa 3 vòng, kèm danh sách đã/chưa kiểm).

**Codex không commit/push/merge**: xong thì trao gậy cho Claude (`[BÀN GIAO]`), Claude commit trên nhánh việc (`AGENTS.md` §8).

## 6. Phạm vi và pattern

Không refactor/đổi tên/format "tiện tay". Không thêm package, DI, event bus, thư viện async khi chưa hỏi. Thấy cái đáng sửa → ghi vào phiếu.

## 7. Khi kẹt

Theo `AGENTS.md` §3.10: 3 vòng tự sửa, 2 lỗi biên dịch liên tiếp chưa rõ nguyên nhân, cần đổi package/settings, vượt gấp đôi time-box → **dừng và nói với người làm** (đã thử gì, kẹt đâu, đề xuất gì). Không giải pháp phá hủy khi chưa được duyệt.

## 8. Đã đổi so với `CODEX.md` cũ

- Quy tắc tương tác chung (mục 0–10 cũ) chuyển vào `AGENTS.md` để Claude cũng theo, **giữ nguyên ngưỡng ≥ 80% và 3 vòng tự sửa**.
- Kiểm chứng theo bậc V0–V3 thay cho "mỗi sửa đều nạp lại scene và vào lại Play".
- Sổ việc rút còn một dòng mỗi việc (không hash/reflog/giờ phút).
- Thay "không tự lược bỏ quy tắc" bằng đường **đề xuất sửa quy tắc** để người làm duyệt (`AGENTS.md` §11).
- Bỏ vòng "AGENTS.md ↔ CODEX.md bắt nhau đọc".
- Nhánh đặt theo **mã việc** (`NNN-…`), không theo AI. AI được sửa trực tiếp scene/prefab theo hàng rào.
