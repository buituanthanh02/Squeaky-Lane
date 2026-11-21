// In lời giải từng nước + sự kiện chính (tele/push/exit/pyl...) để chọn bước hướng dẫn: node tools/sol.js file.txt
const fs = require('fs'), CW = require('../src/logic.js');
// đối số có dạng L20 → lấy màn số 20 trong levels.js; không thì đọc file sinh màn
const lv = /^L\d+$/.test(process.argv[2]) ? require('../src/levels.js')[+process.argv[2].slice(1) - 1] : JSON.parse(fs.readFileSync(process.argv[2], 'utf8').trim().split('\n').pop());
const L = CW.compile(lv); let st = CW.initState(L);
const sol = CW.solve(L, st, 300000);
if (!sol.moves) { console.log('không giải được'); process.exit(0); }
sol.moves.forEach((m, n) => {
  let ev = [];
  if (m[0] === 'move') { const r = CW.move(L, st, m[1], m[2]); st = r.state; ev = r.events.filter((e) => ['tele', 'push', 'exit', 'water', 'stop'].includes(e.t)).map((e) => e.t + (e.why ? ':' + e.why : '')); }
  else if (m[0] === 'rot') st = CW.rotate(L, st, m[1]).state;
  else st = CW.rotatePylon(L, st, m[1]).state;
  const sw = L.switches.map((s) => (CW.switchPressed(L, st, s) ? 'P' : '-')).join('');
  console.log(n + 1, JSON.stringify(m), ev.join(','), sw);
});
