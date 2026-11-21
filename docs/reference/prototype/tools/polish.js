// =====================================================================
// MÀI MÀN: lấy 1 màn thiết kế tay làm gốc, đột biến nhẹ (dời xe, bật/tắt bùn,
// đổi cổng, dời ô đặc biệt) và giữ bản có điểm khó cao hơn.
// Điểm = độ dài lời giải + 15×tỉ lệ bẫy + 3×tỉ lệ bế tắc, buộc giữ các cơ chế yêu cầu.
//   node tools/polish.js <chỉ-số-trong-hard.js> <seed> <giây> <cơ chế bắt buộc, cách nhau dấu phẩy>
// =====================================================================
const CW = require('../src/logic.js');
const { analyze, makeRng } = require('./gen.js');
const HARD = require('./hard.js');
const clone = (o) => JSON.parse(JSON.stringify(o));
const ri = (rng, a, b) => a + Math.floor(rng() * (b - a + 1));
const pickR = (rng, arr) => arr[Math.floor(rng() * arr.length)];

function cellsOf(car) { const out = []; for (let n = 0; n < (car.len || 2); n++) out.push(car.axis === 'h' ? [car.r, car.c + n] : [car.r + n, car.c]); return out; }
function occupied(lv, skip = -1) {
  const s = new Set(); const k = (r, c) => r + ',' + c;
  lv.cars.forEach((car, i) => { if (i !== skip) cellsOf(car).forEach(([r, c]) => s.add(k(r, c))); });
  for (const [r, c] of [...(lv.muds || []), ...(lv.sprays || []), ...(lv.cones || [])]) s.add(k(r, c));
  for (const p of [...(lv.paints || []), ...(lv.barriers || [])]) s.add(k(p.r, p.c));
  return s;
}
const inTable = (lv, r, c) => (lv.tables || []).some(([tr, tc]) => r >= tr && r <= tr + 1 && c >= tc && c <= tc + 1);

function mutate(rng, lv0) {
  const lv = clone(lv0), op = rng();
  if (op < 0.45) {
    const i = ri(rng, 0, lv.cars.length - 1), car = lv.cars[i], occ = occupied(lv, i);
    for (let t = 0; t < 40; t++) {
      const axis = rng() < 0.75 ? car.axis : car.axis === 'h' ? 'v' : 'h';
      const r = ri(rng, 0, lv.rows - (axis === 'v' ? 2 : 1)), c = ri(rng, 0, lv.cols - (axis === 'h' ? 2 : 1));
      const cells = cellsOf({ axis, r, c, len: car.len });
      if (cells.some(([a, b]) => occ.has(a + ',' + b))) continue;
      const tc = cells.filter(([a, b]) => inTable(lv, a, b)).length;
      if (tc === 1) continue;
      Object.assign(car, { axis, r, c }); break;
    }
  } else if (op < 0.6) {
    const car = pickR(rng, lv.cars); car.mud = car.mud ? 0 : 1;
  } else if (op < 0.85) {
    const side = pickR(rng, ['t', 'b', 'l', 'r']), i = ri(rng, 0, (side === 't' || side === 'b' ? lv.cols : lv.rows) - 1);
    const had = lv.gates.find((g) => g.side === side && g.i === i);
    lv.gates = lv.gates.filter((g) => g !== had);
    const colors = [...new Set(lv.cars.map((c) => c.color))];
    if (!had || rng() < 0.5) lv.gates.push({ side, i, color: rng() < 0.4 ? 'water' : pickR(rng, colors) });
  } else {
    const lists = ['muds', 'sprays', 'paints', 'barriers'].filter((n) => (lv[n] || []).length);
    if (lists.length) {
      const n = pickR(rng, lists), idx = ri(rng, 0, lv[n].length - 1), occ = occupied(lv);
      for (let t = 0; t < 30; t++) {
        const r = ri(rng, 0, lv.rows - 1), c = ri(rng, 0, lv.cols - 1);
        if (occ.has(r + ',' + c) || inTable(lv, r, c)) continue;
        if (Array.isArray(lv[n][idx])) lv[n][idx] = [r, c]; else Object.assign(lv[n][idx], { r, c });
        break;
      }
    }
  }
  return lv;
}

function score(lv, need) {
  const a = analyze(lv, 25000);
  if (!a || !a.solvable) return null;
  if (!need.every((m) => a.used.has(m))) return null;
  return { a, s: a.solLen + 15 * a.trap + 3 * a.deadRatio };
}

const [idx, seed, sec] = [+process.argv[2], +(process.argv[3] || 1), +(process.argv[4] || 60)];
const need = (process.argv[5] || '').split(',').filter(Boolean);
const rng = makeRng(seed);
let cur = clone(HARD[idx]), cs = score(cur, need);
if (!cs) { console.log('màn gốc không đạt yêu cầu'); process.exit(0); }
let best = { lv: cur, sc: cs }, it = 0;
const t0 = Date.now();
while (Date.now() - t0 < sec * 1000) {
  it++;
  const cand = mutate(rng, cur), sc = score(cand, need);
  if (!sc) continue;
  if (sc.s >= cs.s - 0.3 || rng() < 0.03) { cur = cand; cs = sc; }
  if (sc.s > best.sc.s) best = { lv: cand, sc };
}
const a = best.sc.a;
console.log(`seed ${seed}: ${it} vòng | ${a.solLen} lượt | bẫy ${(a.trap * 100).toFixed(0)}% | bế tắc ${(a.deadRatio * 100).toFixed(0)}% | điểm ${best.sc.s.toFixed(2)}`);
console.log(JSON.stringify(best.lv));
