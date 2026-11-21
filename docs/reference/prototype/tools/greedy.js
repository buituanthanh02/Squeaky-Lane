// Thêm xe dần vào một màn nền (giữ màn luôn giải được), ưu tiên bản làm bot kẹt nhiều hơn.
//   node tools/greedy.js <file.json> <chỉ số> <số xe đích> <giây> <seed>
const CW = require('../src/logic.js');
const { botStats, winnable, findPath } = require('./big.js');
const { makeRng } = require('./gen.js');
const [file, idx, target, sec, seed] = [process.argv[2], +process.argv[3], +process.argv[4], +process.argv[5], +(process.argv[6] || 1)];
const all = JSON.parse(require('fs').readFileSync(file)), rng = makeRng(seed);
const COLORS = ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'];
const clone = (o) => JSON.parse(JSON.stringify(o));
function addCar(lv0) {
  const lv = clone(lv0), occ = new Set(), k = (r, c) => r + ',' + c;
  lv.cars.forEach((car) => { for (let n = 0; n < 2; n++) occ.add(car.axis === 'h' ? k(car.r, car.c + n) : k(car.r + n, car.c)); });
  for (const [r, c] of [...(lv.muds || []), ...(lv.sprays || []), ...(lv.cones || [])]) occ.add(k(r, c));
  for (const p of [...(lv.paints || []), ...(lv.barriers || [])]) occ.add(k(p.r, p.c));
  const inT = (r, c) => (lv.tables || []).some(([tr, tc]) => r >= tr && r <= tr + 1 && c >= tc && c <= tc + 1);
  const gm = {}; for (const g of lv.gates) gm[g.side + ':' + g.i] = g.color;
  for (let t = 0; t < 60; t++) {
    const axis = rng() < 0.5 ? 'h' : 'v', r = Math.floor(rng() * (lv.rows - (axis === 'v' ? 1 : 0))), c = Math.floor(rng() * (lv.cols - (axis === 'h' ? 1 : 0)));
    const cells = [[r, c], axis === 'h' ? [r, c + 1] : [r + 1, c]];
    if (cells.some(([a, b]) => occ.has(k(a, b)))) continue;
    if (cells.filter(([a, b]) => inT(a, b)).length === 1) continue;
    const ends = (axis === 'h' ? ['l:' + r, 'r:' + r] : ['t:' + c, 'b:' + c]).map((e) => gm[e]).filter((x) => x && x !== 'water');
    const color = ends.length && rng() < 0.75 ? ends[Math.floor(rng() * ends.length)] : COLORS[Math.floor(rng() * 6)];
    const mud = rng() < (lv.mudRate == null ? 0.35 : lv.mudRate) ? (rng() < (lv.thickRate || 0) ? 2 : 1) : 0;
    lv.cars.push({ axis, r, c, color, mud });
    return lv;
  }
  return null;
}
const ev = (lv) => { const L = CW.compile(lv); if (winnable(L, CW.initState(L), 30000) !== true) return null; return botStats(L, 24, rng); };
let cur = all[idx], cb = ev(cur), t0 = Date.now(), it = 0;
if (!cb) { console.log('màn nền không giải được'); process.exit(0); }
while (Date.now() - t0 < sec * 1000 && cur.cars.length < target) {
  it++;
  const cand = addCar(cur); if (!cand) continue;
  const b = ev(cand); if (!b) continue;
  if (b.stuck + 0.08 >= cb.stuck) { cur = cand; cb = b; console.error(`+xe → ${cur.cars.length} xe, bot kẹt ${(b.stuck * 100).toFixed(0)}% ` + JSON.stringify(cur)); }
}
const out = clone(cur); delete out.mudRate; delete out.thickRate;
const L = CW.compile(out), p = findPath(L, CW.initState(L), 200000), fin = botStats(L, 80, rng);
console.log(`${out.name}: ${out.cars.length} xe | lời giải ${p ? p.length : '?'} | bot kẹt ${(fin.stuck * 100).toFixed(0)}% thắng ${(fin.wins * 100).toFixed(0)}% | ${it} lần thử`);
console.log(JSON.stringify(out));
