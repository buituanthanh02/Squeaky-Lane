// Tính độ dài lời giải ngắn nhất của từng màn → src/levelmeta_data.js (dùng cho chấm độ khó ở màn chọn màn).
// Chạy: node tools/meta.js   (màn quá lớn không giải nổi trong giới hạn thì ghi null)
const fs = require('fs'), path = require('path'), CW = require('../src/logic.js'), LV = require('../src/levels.js');
const out = LV.map((lv, i) => {
  const L = CW.compile(lv), r = CW.solve(L, CW.initState(L), 400000);
  process.stderr.write(`${i + 1} ${lv.name}: ${r.moves ? r.moves.length : 'null'}\n`);
  return r.moves ? r.moves.length : null;
});
// Màn quá lớn để giải khi chơi: lấy lời giải đã tìm sẵn (test/big_paths.json, khoá = số màn - 1) cho nút Gợi ý, có kiểm lại là thắng
const big = JSON.parse(fs.readFileSync(path.join(__dirname, '../test/big_paths.json'), 'utf8')), hint = {};
LV.forEach((lv, i) => {
  if (out[i] != null || !big[i]) return;
  const L = CW.compile(lv); let st = CW.initState(L);
  for (const m of big[i]) st = (m[0] === 'move' ? CW.move(L, st, m[1], m[2]) : m[0] === 'rot' ? CW.rotate(L, st, m[1]) : CW.rotatePylon(L, st, m[1])).state;
  if (!CW.isWin(L, st)) throw new Error('lời giải lưu sẵn của màn ' + (i + 1) + ' không thắng');
  hint[i] = big[i];
});
fs.writeFileSync(path.join(__dirname, '../src/levelmeta_data.js'),
  '// Sinh tự động bởi tools/meta.js — độ dài lời giải ngắn nhất từng màn (null = quá lớn để giải trong giới hạn)\nconst CW_SOLLEN = ' + JSON.stringify(out) + ';\n' +
  '// Lời giải tìm sẵn cho các màn quá lớn (nút Gợi ý dùng ngay, khỏi tìm): { chỉ số màn: [nước...] }\nconst CW_HINTPATH = ' + JSON.stringify(hint) + ';\n');
