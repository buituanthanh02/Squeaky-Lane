// Kiểm mọi màn: giải được không, lời giải dài bao nhiêu. node tools/checkall.js [từ] [đến]
const CW = require('../src/logic.js'), LV = require('../src/levels.js');
const a = +(process.argv[2] || 1), b = +(process.argv[3] || LV.length);
for (let i = a; i <= b; i++) {
  const lv = LV[i - 1]; let r;
  try { const L = CW.compile(lv); r = CW.solve(L, CW.initState(L), 300000); } catch (e) { console.log(i, lv.name, 'LỖI', e.message); continue; }
  console.log(i, lv.name, r.moves ? 'giải được ' + r.moves.length : 'KHÔNG GIẢI ĐƯỢC (exhausted=' + r.exhausted + ')');
}
