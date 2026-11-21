// Chấm 1 hoặc nhiều màn khó trong tools/hard.js:
//   node tools/check.js [chỉ số]
// In lời giải ngắn nhất (dạng dễ đọc), tỉ lệ bẫy, tỉ lệ bế tắc, và các nước đi bẫy ở đầu màn.
const CW = require('../src/logic.js');
const { analyze } = require('./gen.js');
const HARD = require('./hard.js');

const fmt = (L, lv, a) => {
  const car = lv.cars[a[1]], name = `${car.color}${car.mud ? '(bùn' + (car.mud === 2 ? ' dày' : '') + ')' : ''}#${a[1]}`;
  if (a[0] === 'rot') return `xoay ${name}`;
  const dir = car.axis === 'h' ? (a[2] > 0 ? '→' : '←') : (a[2] > 0 ? '↓' : '↑');
  return `${name}${dir}`;
};

const only = process.argv[2] != null ? +process.argv[2] : null;
HARD.forEach((lv, idx) => {
  if (only !== null && idx !== only) return;
  const a = analyze(lv, 120000);
  if (!a) { console.log(`#${idx} ${lv.name}: quá lớn`); return; }
  if (!a.solvable) { console.log(`#${idx} ${lv.name}: KHÔNG GIẢI ĐƯỢC`); return; }
  const L = CW.compile(lv);
  // nước đi đầu tiên: nước nào dẫn vào bế tắc
  const s0 = CW.initState(L), firsts = [];
  for (let i = 0; i < L.cars.length; i++) for (const m of [['move', i, 1], ['move', i, -1], ['rot', i, 0]]) {
    const r = m[0] === 'rot' ? (L.tables.length ? CW.rotate(L, s0, i) : null) : CW.move(L, s0, i, m[2]);
    if (!r || !r.moved) continue;
    const s = CW.solve(L, r.state, 60000);
    firsts.push(`${fmt(L, lv, m)}${s.moves ? '' : s.exhausted ? ' ✗' : ' ?'}`);
  }
  console.log(`#${idx} ${lv.name} | ${lv.cars.length} xe | ${a.solLen} lượt (thừa ${a.solLen - lv.cars.length}) | bẫy ${(a.trap * 100).toFixed(0)}% | bế tắc ${(a.deadRatio * 100).toFixed(0)}% | ${a.states} trạng thái`);
  console.log('   giải:', a.path.map((m) => fmt(L, lv, m)).join('  '));
  console.log('   nước đầu:', firsts.join(' · '));
  console.log('   dùng:', [...a.used].join(','));
});
