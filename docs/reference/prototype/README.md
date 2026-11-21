# prototype-reference (CHỈ ĐỂ ĐỐI CHIẾU, không nằm trong Assets)
- src/logic.js: luật game v10 (module CW). src/levels.js: dữ liệu 31 màn.
- tools/: bộ sinh và kiểm màn (node tools/checkall.js). test/run.js, tut.js: test JS của prototype.
- levels-export/level_NN.json: 31 màn xuất ra JSON kèm minMoves (solve, maxStates=300000). _summary.json: bảng tổng hợp.
- Màn 17, 18, 19: solve không tìm ra trong 300000 trạng thái (minMoves = null), cần xử lý riêng.
- Màn 16 cần > 60000 trạng thái (mặc định solve), nên golden test phải truyền maxStates lớn.
- test/big_paths.json: lời giải đã biết cho màn 17, 18, 19 (khóa là chỉ số 0 = màn 17/18/19) với độ dài 17, 15, 15. Đây là cận trên tìm bằng pathSearch, CHƯA chứng minh là ngắn nhất. winSearch xác nhận cả ba màn thắng được.
