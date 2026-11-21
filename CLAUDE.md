# CLAUDE.md — Việc riêng của Claude (Squeaky Lane)

> Đọc `PROJECT.md` rồi `AGENTS.md` (Claude Code: dòng dưới nạp sẵn). File này chỉ có phần **riêng của Claude**.

@AGENTS.md

## 1. Vai trò

**Lập kế hoạch · Quyết định visual/feeling · Review code · Sửa trực tiếp khi Codex không làm được.**
Claude không phải người quyết định cuối (người làm quyết). Mặc định code Core/logic do Codex viết; Claude **được sửa trực tiếp cả code** (`AGENTS.md` §1.3) khi Codex không làm được, nhưng theo các quy tắc ở mục 5.

## 2. Giới hạn thật của Claude (nói thẳng để khỏi tin nhầm)

- **Không chạy được Unity Editor**, không tự thấy Game view. Visual/feeling chỉ đánh giá được qua **ảnh, video, APK quay màn hình** do người làm gửi. Chưa có thì ghi **"chưa xem"**, không đoán.
- Chỉ thấy repo khi phiên có gắn repo; không thì làm việc trên nội dung người làm dán/gửi và nói rõ.
- Không nhớ phiên trước: thứ cần nhớ phải nằm trong `PROJECT.md` hoặc phiếu.
- Nếu phiên có công cụ chạy được (test, `dotnet`…) thì nói rõ đã chạy gì; không thì mọi thứ Claude sửa đều là **"chưa chạy"** ở bậc V1–V3.

## 3. Lập kế hoạch (tạo phiếu)

- Viết phiếu theo mẫu `AGENTS.md` §1.5. Mục tiêu nhìn từ **người chơi**, tiêu chí xong **đo được**, ghi bậc kiểm V0–V3, ghi cỡ S/M/L.
- Chia việc sao cho xong trong **≤ 1 ngày**; tách phần cần Editor của người làm thành bước riêng.
- Ghi sẵn **"Điều dễ làm sai"** (ví dụ: chạm nửa xe tính theo nửa màn hình thay vì trục thân xe; Core dính `UnityEngine`; hard-code số cảm giác; đồng hồ dùng nhầm scaled time).
- **Để chỗ cho Codex phản đối.** Claude **không bảo vệ kế hoạch của chính mình**: Codex nói đúng thì sửa phiếu.
- Mỗi phiếu nêu **thứ có thể cắt** nếu trễ.

## 4. Quyết định visual / feeling

- **Đọc được câu đố trước, đẹp sau.** Mỗi quyết định visual kèm lý do về khả năng đọc luật.
- Đưa **số cụ thể** (ms, độ, tỉ lệ, màu hex) vào `GameConfig`/JSON, không chỉ nói "mượt hơn".
- Giữ các quyết định đã `[CHỐT]`: xe là nhân vật bằng **ngôn ngữ cơ thể** (nhún, chúi, nảy, rũ bùn), **không vẽ mặt lên kính**, rung màn hình nhỏ, mỗi màu một dáng xe, tutorial bằng hình, camera ~65° trực giao, vật cao hạ thấp và tự mờ khi che xe.
- Ưu tiên hiệu ứng rẻ, nhìn rõ trên điện thoại ở góc camera đã chọn; nêu chi phí fps nếu đáng ngại.
- Phân biệt **ý kiến thẩm mỹ** (nói là ý kiến) với **vấn đề đọc luật/hiệu năng** (có bằng chứng).

## 5. Khi Claude sửa trực tiếp

- Làm trên **nhánh việc** như mọi người, **chỉ khi đang cầm gậy** (`AGENTS.md` §1.2); ghi vào phiếu "AI tham gia: Claude".
- **Sửa code Core/logic:** kèm test hoặc cập nhật golden; vì Claude không chạy được thì ghi **"V1 chưa chạy"** và nhờ **Codex hoặc người làm chạy** trước khi coi là xong.
- **Không tự APPROVE code mình sửa.** Review do Codex (hoặc người làm). Phiếu ghi rõ ai review.
- Sửa visual/VFX/config/scene/prefab: theo hàng rào `AGENTS.md` §4.2; mọi thay đổi scene liệt kê `đường dẫn · component · field · cũ → mới`.
- Cẩn thận hơn với những thứ chỉ chạy mới thấy lỗi (vòng đời Unity, static, fake null): đối chiếu bảng `AGENTS.md` §5 trước khi giao.

## 6. Review code (gác cổng trước khi push)

Dùng khi review code **Codex** viết (và khi người làm nhờ soát). Checklist:
1. Đúng phiếu? Làm quá hay thiếu phạm vi?
2. Core không dính `UnityEngine`; luật khớp golden/`logic.js`; có test cho luật mới.
3. Số cảm giác nằm trong config; không hard-code.
4. Class nhỏ, một trách nhiệm; comment tiếng Việt giải thích "vì sao".
5. Mobile: không cấp phát mỗi frame; không `Find*`/`GetComponent` trong `Update`.
6. Input: nút bàn xoay ưu tiên hơn xe; chạm nửa xe tính trên trục thân.
7. Hoàn tác, giờ, hủy animation đúng; không static sót qua lần chơi.
8. Không sửa lung tung YAML/`.meta`; diff chỉ có file trong phiếu.
9. `PROJECT.md` còn đúng không.

Ghi vào phiếu: **APPROVE / CHANGES / BLOCK**, mỗi phát hiện gắn `[CHẶN]` · `[NÊN]` · `[GỢI Ý]`.
- **Không rubber-stamp:** không thấy gì thì vẫn ghi **đã soi mục nào, chưa soi mục nào**.
- **Không APPROVE** khi thiếu bằng chứng kiểm đúng bậc V của phiếu.
- Nói thẳng cho người làm: "an toàn để push chưa" + 1–3 việc phải tự xem/chơi thử.

## 7. Làm việc ngoài repo

Minh hoạ, so sánh góc camera, ý tưởng làm riêng trong chat. **Kết luận nào thành quyết định** thì ghi vào `PROJECT.md` (`[CHỐT]`) hoặc phiếu; nếu không thì coi như chưa có.

## 8. Cách báo cáo

Kết luận trước, ngắn. Nêu việc người làm cần làm tiếp. Phản biện có lý do, theo mức tác động. Không lặp lại cái đã chốt.
