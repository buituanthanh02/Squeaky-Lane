// Kiểm tra từng màn: lời giải ngắn nhất + số trạng thái bế tắc có thể rơi vào
const CW = require('../src/logic.js');
const LV = require('../src/levels.js');
const only = process.argv[2] ? +process.argv[2] : null;
LV.forEach((lv, idx) => {
  if (only !== null && idx !== only) return;
  const L = CW.compile(lv);
  const s0 = CW.initState(L);
  const sol = CW.solve(L, s0, 200000);
  // khám phá toàn bộ để đếm trạng thái bế tắc (không thắng được)
  const seen = new Map(); const q = [s0]; seen.set(CW.stateKey(L, s0), s0);
  while (q.length && seen.size < 30000) {
    const st = q.shift();
    for (let i = 0; i < L.cars.length; i++) {
      const cand = [CW.move(L, st, i, 1), CW.move(L, st, i, -1), L.tables.length ? CW.rotate(L, st, i) : null];
      for (const r of cand) if (r && r.moved) { const k = CW.stateKey(L, r.state); if (!seen.has(k)) { seen.set(k, r.state); q.push(r.state); } }
    }
  }
  let dead = 0;
  for (const st of seen.values()) { if (CW.isWin(st)) continue; const r = CW.solve(L, st, 20000); if (!r.moves && r.exhausted) dead++; }
  const fmt = (m) => m[0] === 'rot' ? `xoay${m[1]}` : `${m[1]}${m[2] > 0 ? '+' : '-'}`;
  console.log(`#${idx + 1} ${lv.name.padEnd(18)} ${sol.moves ? 'OK ' + sol.moves.length + ' lượt: ' + sol.moves.map(fmt).join(' ') : 'KHÔNG GIẢI ĐƯỢC'} | states ${seen.size}, bế tắc ${dead}`);
});
