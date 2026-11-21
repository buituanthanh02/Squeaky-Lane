// Xuất lời giải từng bước (dạng chữ) cho các màn khó: node tools/walk.js > /tmp/walk.json
// Mỗi xe được gán một chữ cái (A, B, C… theo thứ tự trong màn) để người đọc đối chiếu với ảnh minh hoạ.
const CW = require('../src/logic.js'), LV = require('../src/levels.js'), big = require('../test/big_paths.json');
const SOL = require('fs').readFileSync(__dirname + '/../src/levelmeta_data.js', 'utf8').match(/\[([^\]]*)\]/)[1].split(',').map((x) => (x === 'null' ? null : +x));
const COL = { blue: 'xanh dương', pink: 'hồng', yellow: 'vàng', green: 'xanh lá', red: 'đỏ', orange: 'cam', purple: 'tím', water: 'nước', white: 'trắng' };
const END = { h: { '-1': 'TRÁI', '1': 'PHẢI' }, v: { '-1': 'TRÊN', '1': 'DƯỚI' } };
const letter = (i) => String.fromCharCode(65 + i);
const out = [];
LV.forEach((lv, n) => {
  const hard = (SOL[n] == null || SOL[n] >= 12) || /^(rất )?khó( nhất)?$/i.test(lv.tag || '');
  if (!hard) return;
  const L = CW.compile(lv); let st = CW.initState(L);
  const moves = big[String(n)] || CW.solve(L, st, 400000).moves;
  const name = (i) => `xe ${letter(i)} (${COL[lv.cars[i].color] || lv.cars[i].color}${lv.cars[i].truck ? ', xe tải' : ''})`;
  const steps = [];
  for (const m of moves) {
    let text, notes = [];
    const axis0 = m[0] === 'move' ? st.cars[m[1]].axis : null;
    let res;
    if (m[0] === 'move') { res = CW.move(L, st, m[1], m[2]); text = `Chạm đầu ${END[axis0][m[2]]} của ${name(m[1])}`; }
    else if (m[0] === 'rot') { res = CW.rotate(L, st, m[1]); text = `Vuốt ngang thân ${name(m[1])} để xoay 90° (đang đứng trên bàn xoay)`; }
    else { res = CW.rotatePylon(L, st, m[1]); text = `Chạm trụ xoay ${m[1] + 1} (xoay thêm 90°)`; }
    for (const e of res.events) {
      if (e.t === 'exit') notes.push('ra cổng, xe sạch');
      else if (e.t === 'tele') notes.push(`chui cổng, trồi ra ở hàng ${e.r + 1}, cột ${e.c + 1}`);
      else if (e.t === 'push') notes.push('húc đẩy xe phía trước');
      else if (e.t === 'splash') notes.push('bùn văng ra');
    }
    steps.push({ text, notes: [...new Set(notes)], raw: m });
    st = res.state;
  }
  out.push({ level: n + 1, name: lv.name, tag: lv.tag, cols: lv.cols, rows: lv.rows, len: moves.length, win: CW.isWin(L, st),
    cars: lv.cars.map((c, i) => ({ i, label: letter(i), color: COL[c.color] || c.color, axis: c.axis, truck: !!c.truck, mud: c.mud })), steps });
});
console.log(JSON.stringify(out));
