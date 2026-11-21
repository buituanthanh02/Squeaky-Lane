// In màn ra chữ + lời giải để xem nhanh: node tools/show.js file.txt
const fs = require('fs'), CW = require('../src/logic.js');
const lines = fs.readFileSync(process.argv[2], 'utf8').trim().split('\n');
const lv = JSON.parse(lines[lines.length - 1]);
const g = Array.from({ length: lv.rows }, () => Array(lv.cols).fill(' . '));
const put = (r, c, s) => { g[r][c] = s; };
lv.cars.forEach((c, i) => { for (let k = 0; k < (c.len || 2); k++) put(c.axis === 'h' ? c.r : c.r + k, c.axis === 'h' ? c.c + k : c.c, (c.truck ? 'T' : c.color[0].toUpperCase()) + i + (c.axis === 'h' ? '-' : '|')); });
(lv.portals || []).forEach(([a, b], n) => { for (const f of [a, b]) g[f[0]][f[1]] = (g[f[0]][f[1]].trim() + '@' + f[2] + n).padEnd(4); });
(lv.pylons || []).forEach((p, n) => { put(p.r, p.c, ' P' + n + p.o); (p.to || []).forEach((f, q) => { g[f[0]][f[1]] = (g[f[0]][f[1]].trim() + '@' + f[2] + 'p' + n + q).padEnd(4); }); });
(lv.switches || []).forEach((s) => { put(s.r, s.c, ' ## '); s.a.forEach(([r, c]) => put(r, c, ' A+ ')); s.b.forEach(([r, c]) => put(r, c, ' B+ ')); });
(lv.lights || []).forEach((l) => put(l.r, l.c, l.phase ? ' L1 ' : ' L0 '));
(lv.tables || []).forEach(([r, c]) => { for (const [dr, dc] of [[0, 0], [0, 1], [1, 0], [1, 1]]) if (g[r + dr][c + dc] === ' . ') put(r + dr, c + dc, ' ~~ '); });
(lv.cones || []).forEach(([r, c]) => put(r, c, ' XX '));
console.log('gates:', lv.gates.map((x) => x.side + x.i + ':' + x.color).join(' '));
for (const row of g) console.log(row.map((s) => s.padEnd(9)).join(''));
const L = CW.compile(lv), r = CW.solve(L, CW.initState(L), 200000);
console.log('solution', r.moves && r.moves.length, JSON.stringify(r.moves));
