#!/usr/bin/env bash
# Claude giao việc cho Codex (subagent) rồi nhận kết quả, không cần người làm mở chat Codex.
#   tools/codex-run.sh <tên-việc> <file-phiếu.md> [lời nhắn thêm]
# Ví dụ: tools/codex-run.sh bubble docs/handoff/013-s4-bubble-sprite.md "Chỉ làm bước 1."
# Kết quả: Logs/codex/<tên-việc>.log (toàn bộ), Logs/codex/<tên-việc>.last.txt (câu trả lời cuối của Codex).
# Chạy nền (Claude dùng run_in_background) để vẫn làm việc khác trong lúc Codex làm.
# Quy tắc: AGENTS.md §1 + CODEX.md. Codex chỉ làm trong vùng art/VFX, không commit; Claude review rồi commit.
set -u
NAME="${1:?thiếu <tên-việc>}"
TICKET="${2:?thiếu <file-phiếu.md>}"
EXTRA="${3:-}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="$ROOT/Logs/codex"
mkdir -p "$OUT"
PROMPT="$OUT/$NAME.prompt.md"

cat > "$PROMPT" <<EOF
Bạn là Codex, subagent do Claude (chủ trì code) gọi ra cho dự án Squeaky Lane. Người làm không ngồi cạnh: mọi thứ bạn làm Claude sẽ đọc và review.
1. Đọc theo thứ tự: PROJECT.md, AGENTS.md, CODEX.md, rồi phiếu \`$TICKET\` (cả mục Trao đổi).
2. Làm đúng phạm vi phiếu. Chỉ sửa trong vùng của Codex (AGENTS.md §1.3: Art/**, Prefabs/Environment, Prefabs/VFX, shader/material hiệu ứng, tool Editor riêng của hiệu ứng). Cần chạm code ngoài vùng thì ghi [HỎI] vào phiếu, không tự sửa.
3. Bạn được lái Unity Editor qua Unity MCP (server http://127.0.0.1:8080). Claude chỉ đọc trong lúc bạn làm. Không Play, không lưu scene trừ khi phiếu ghi.
4. KHÔNG commit, không push. Không đổi package/Project Settings/Core/Game.
5. Xong: điền phần "AI làm điền" của phiếu (đã làm, tệp đổi, kiểm gì và kết quả, chưa kiểm gì), đổi \`Đang cầm: Claude\`, thêm một dòng [BÀN GIAO] ở mục Trao đổi (giờ lấy từ đồng hồ máy).
6. Câu trả lời cuối: tối đa 10 dòng tiếng Việt: kết luận trước, danh sách tệp đổi, thứ chưa kiểm, và mọi điều bạn phản đối hoặc muốn Claude quyết.
$EXTRA
EOF

# Sandbox workspace-write: ghi được trong repo, không đụng ngoài repo. Lời nhắc đi qua stdin để giữ nguyên UTF-8.
cd "$ROOT" || exit 1
codex exec -C "$ROOT" -s workspace-write --color never --skip-git-repo-check \
  -o "$OUT/$NAME.last.txt" - < "$PROMPT" > "$OUT/$NAME.log" 2>&1
CODE=$?
echo "codex exit=$CODE · log: Logs/codex/$NAME.log · trả lời cuối: Logs/codex/$NAME.last.txt"
exit $CODE
