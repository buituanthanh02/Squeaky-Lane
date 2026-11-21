// Kiểm tra kịch bản tutorial: mọi bước "do" phải là nước đi hợp lệ, sau tutorial màn vẫn giải được,
// và mọi mục tiêu focus phải trỏ tới thứ có thật trong màn.
const CW = require('../src/logic.js');
const LV = require('../src/levels.js');
let bad = 0;
LV.forEach((lv, idx) => {
  const L = CW.compile(lv);
  let st = CW.initState(L);
  const issues = [];
  for (const [k, s] of (lv.tutorial || []).entries()) {
    for (const f of s.focus || []) {
      if (f.car != null && !lv.cars[f.car]) issues.push(`bước ${k + 1}: không có xe ${f.car}`);
      if (f.gate && !lv.gates.some((g) => g.side + ':' + g.i === f.gate)) issues.push(`bước ${k + 1}: không có cổng ${f.gate}`);
    }
    if (!s.do) continue;
    const r = s.do[0] === 'rot' ? CW.rotate(L, st, s.do[1]) : CW.move(L, st, s.do[1], s.do[2]);
    if (!r || !r.moved) { issues.push(`bước ${k + 1}: nước đi không hợp lệ`); break; }
    st = r.state;
  }
  const sol = CW.solve(L, st, 400000);
  if (!sol.moves) issues.push('sau tutorial KHÔNG giải được');
  if (issues.length) bad++;
  console.log(`#${idx + 1} ${lv.name.padEnd(18)} tutorial ${(lv.tutorial || []).length} bước | còn ${sol.moves ? sol.moves.length : '?'} lượt ${issues.length ? '⚠ ' + issues.join('; ') : '✓'}`);
});
process.exit(bad ? 1 : 0);
