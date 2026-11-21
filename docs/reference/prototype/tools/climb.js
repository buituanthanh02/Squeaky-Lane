// =====================================================================
// LEO ĐỒI: bắt đầu từ 1 màn giải được, đột biến dần (dời xe, đổi màu,
// thêm/bớt bùn, đổi cổng, dời ô đặc biệt) và giữ bản "khó hơn".
// Độ khó = số lượt thừa ngoài việc mỗi xe ra 1 lần + tỉ lệ bẫy ở đầu màn.
//   node tools/climb.js <spec> <seed> <giây>
// =====================================================================
const CW = require('../src/logic.js');
const { genLevel, analyze, SPECS, makeRng } = require('./gen.js');

const clone = (o) => JSON.parse(JSON.stringify(o));
const ri = (rng, a, b) => a + Math.floor(rng() * (b - a + 1));
const pickR = (rng, arr) => arr[Math.floor(rng() * arr.length)];

/** Ô có hợp lệ để đặt xe/ô đặc biệt không (không chồng lấn) */
function occupiedCells(lv, skipCar = -1) {
  const s = new Set(), k = (r, c) => r + ',' + c;
  lv.cars.forEach((car, i) => { if (i === skipCar) return; for (let n = 0; n < (car.len || 2); n++) s.add(car.axis === 'h' ? k(car.r, car.c + n) : k(car.r + n, car.c)); });
  for (const [r, c] of [...(lv.muds || []), ...(lv.sprays || [])]) s.add(k(r, c));
  for (const p of lv.paints || []) s.add(k(p.r, p.c));
  for (const b of lv.barriers || []) s.add(k(b.r, b.c));
  return s;
}
const inTable = (lv, r, c) => (lv.tables || []).some(([tr, tc]) => r >= tr && r <= tr + 1 && c >= tc && c <= tc + 1);

function mutate(rng, lv0, sp) {
  const lv = clone(lv0);
  const op = rng();
  if (op < 0.35) {
    // dời 1 xe
    const i = ri(rng, 0, lv.cars.length - 1), car = lv.cars[i];
    const occ = occupiedCells(lv, i);
    for (let t = 0; t < 30; t++) {
      const axis = rng() < 0.7 ? car.axis : car.axis === 'h' ? 'v' : 'h';
      const r = ri(rng, 0, lv.rows - (axis === 'v' ? 2 : 1)), c = ri(rng, 0, lv.cols - (axis === 'h' ? 2 : 1));
      const cells = [[r, c], axis === 'h' ? [r, c + 1] : [r + 1, c]];
      if (cells.some(([a, b]) => occ.has(a + ',' + b))) continue;
      const tc = cells.filter(([a, b]) => inTable(lv, a, b)).length;
      if (tc === 1) continue;
      Object.assign(car, { axis, r, c }); break;
    }
  } else if (op < 0.5) {
    const car = pickR(rng, lv.cars); car.color = pickR(rng, sp.colors);
  } else if (op < 0.62) {
    const car = pickR(rng, lv.cars);
    car.mud = rng() < 0.45 ? 0 : rng() < (sp.thickRate || 0) ? 2 : 1;
  } else if (op < 0.85) {
    // đổi / thêm / bỏ cổng
    const side = pickR(rng, ['t', 'b', 'l', 'r']), i = ri(rng, 0, (side === 't' || side === 'b' ? lv.cols : lv.rows) - 1);
    lv.gates = lv.gates.filter((g) => !(g.side === side && g.i === i));
    const r = rng();
    if (r < 0.75) lv.gates.push({ side, i, color: rng() < 0.3 ? 'water' : pickR(rng, sp.colors) });
  } else {
    // dời 1 ô đặc biệt
    const lists = ['muds', 'sprays'].filter((n) => (lv[n] || []).length);
    if (lists.length) {
      const n = pickR(rng, lists), idx = ri(rng, 0, lv[n].length - 1);
      const occ = occupiedCells(lv);
      for (let t = 0; t < 20; t++) { const r = ri(rng, 0, lv.rows - 1), c = ri(rng, 0, lv.cols - 1); if (!occ.has(r + ',' + c) && !inTable(lv, r, c)) { lv[n][idx] = [r, c]; break; } }
    } else if ((lv.paints || []).length) {
      const p = pickR(rng, lv.paints); p.color = pickR(rng, sp.colors);
    }
  }
  return lv;
}

/** Điểm nhanh: phải giải được trong giới hạn; điểm = lượt thừa */
function quickScore(lv) {
  const L = CW.compile(lv), q = CW.solve(L, CW.initState(L), 20000);
  if (!q.moves) return null;
  return { extra: q.moves.length - lv.cars.length, len: q.moves.length, moves: q.moves };
}

function climb(spName, seed, seconds) {
  const sp = SPECS[spName], rng = makeRng(seed);
  // khởi đầu: lấy màn sinh ngẫu nhiên đầu tiên giải được
  let cur, cs;
  for (let t = 0; t < 500; t++) { const lv = genLevel(rng, sp); const q = quickScore(lv); if (q) { cur = lv; cs = q; break; } }
  if (!cur) return null;
  const t0 = Date.now();
  let best = { lv: cur, s: cs }, iters = 0;
  while (Date.now() - t0 < seconds * 1000) {
    iters++;
    const cand = mutate(rng, cur, sp);
    const q = quickScore(cand);
    if (!q) continue;
    // chấp nhận nếu không tệ hơn (cho phép đi ngang để thoát cực trị địa phương)
    if (q.extra >= cs.extra || rng() < 0.05) { cur = cand; cs = q; }
    if (q.extra > best.s.extra) best = { lv: cand, s: q };
  }
  const a = analyze(best.lv, 60000);
  return { best, a, iters };
}

if (require.main === module) {
  const [spName, seed, sec] = [process.argv[2], +(process.argv[3] || 1), +(process.argv[4] || 60)];
  const r = climb(spName, seed, sec);
  if (!r) { console.log('không khởi tạo được'); process.exit(0); }
  const { best, a, iters } = r;
  const need = SPECS[spName].need;
  console.log(`${spName} seed ${seed}: ${iters} vòng | ${best.s.len} lượt (thừa ${best.s.extra}) | ` +
    (a ? `bẫy ${(a.trap * 100).toFixed(0)}% | bế tắc ${(a.deadRatio * 100).toFixed(0)}% | ${a.states} tt | dùng ${[...a.used].join(',')} | đủ cơ chế: ${need.every((m) => a.used.has(m))}` : 'quá lớn để phân tích'));
  console.log(JSON.stringify(best.lv));
}
module.exports = { climb, mutate };
