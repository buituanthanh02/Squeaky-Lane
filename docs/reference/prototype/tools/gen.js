// =====================================================================
// BỘ SINH MÀN KHÓ: sinh ngẫu nhiên theo bộ cơ chế, rồi chấm điểm bằng
// cách khám phá TOÀN BỘ không gian trạng thái.
//   node tools/gen.js <tên-spec> <số-ứng-viên> <seed>
// =====================================================================
const CW = require('../src/logic.js');

/** Bộ sinh số ngẫu nhiên có seed (mulberry32) */
function makeRng(seed) {
  let a = seed >>> 0;
  return () => { a |= 0; a = (a + 0x6D2B79F5) | 0; let t = Math.imul(a ^ (a >>> 15), 1 | a); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
}
const ri = (rng, a, b) => a + Math.floor(rng() * (b - a + 1));
const pickR = (rng, arr) => arr[Math.floor(rng() * arr.length)];

// ---------------------------------------------------------------------
// CÁC BỘ CƠ CHẾ cho màn khó
// ---------------------------------------------------------------------
const SPECS = {
  nuoc_van: { cols: 5, rows: 6, cars: [7, 8], colors: ['pink', 'blue', 'yellow', 'green'], mudRate: 0.5, splash: true, extraWater: 2, need: ['water', 'splashTrap'] },
  bun_xit: { cols: 5, rows: 6, cars: [6, 8], colors: ['pink', 'blue', 'yellow', 'green'], mudRate: 0.35, thickRate: 0.4, splash: true, muds: 2, sprays: 1, extraWater: 1, need: ['mud', 'spray'] },
  xoay_nuoc: { cols: 5, rows: 6, cars: [6, 7], colors: ['pink', 'blue', 'yellow', 'green'], mudRate: 0.4, splash: true, tables: 1, extraWater: 1, need: ['rot', 'water'] },
  son_xoay: { cols: 5, rows: 6, cars: [6, 7], colors: ['pink', 'blue', 'yellow', 'green', 'purple'], mudRate: 0.25, splash: true, tables: 1, paints: 2, barrier: 1, need: ['rot', 'paint'] },
  tat_ca: { cols: 6, rows: 6, cars: [8, 9], colors: ['pink', 'blue', 'yellow', 'green', 'orange'], mudRate: 0.45, thickRate: 0.25, splash: true, muds: 1, sprays: 1, tables: 1, paints: 1, extraWater: 1, need: ['water', 'rot', 'mud', 'splashTrap'] },
};

// ---------------------------------------------------------------------
// SINH 1 MÀN
// ---------------------------------------------------------------------
function genLevel(rng, sp) {
  const { cols, rows } = sp;
  const taken = new Set(), key = (r, c) => r + ',' + c;
  const tables = [];
  for (let t = 0; t < (sp.tables || 0); t++) {
    const r = ri(rng, 1, rows - 3), c = ri(rng, 1, cols - 3);
    tables.push([r, c]);
  }
  const inTable = (r, c) => tables.some(([tr, tc]) => r >= tr && r <= tr + 1 && c >= tc && c <= tc + 1);

  // xe
  const cars = [];
  const nCars = ri(rng, sp.cars[0], sp.cars[1]);
  for (let tries = 0; tries < 400 && cars.length < nCars; tries++) {
    const axis = rng() < 0.5 ? 'h' : 'v', len = 2;
    const r = ri(rng, 0, rows - (axis === 'v' ? len : 1)), c = ri(rng, 0, cols - (axis === 'h' ? len : 1));
    const cells = []; for (let k = 0; k < len; k++) cells.push(axis === 'h' ? [r, c + k] : [r + k, c]);
    if (cells.some(([a, b]) => taken.has(key(a, b)))) continue;
    const tcount = cells.filter(([a, b]) => inTable(a, b)).length;
    if (tcount !== 0 && tcount !== len) continue; // không đè nửa bàn xoay
    cells.forEach(([a, b]) => taken.add(key(a, b)));
    let mud = 0;
    if (rng() < (sp.mudRate || 0)) mud = rng() < (sp.thickRate || 0) ? 2 : 1;
    cars.push({ axis, r, c, color: pickR(rng, sp.colors), mud });
  }

  // cổng: mỗi xe có 1 cổng cùng màu ở một đầu làn (nếu còn chỗ)
  const gates = {};
  const slotsOf = (car) => (car.axis === 'h' ? [['l', car.r], ['r', car.r]] : [['t', car.c], ['b', car.c]]);
  for (const car of cars) {
    const ends = slotsOf(car); if (rng() < 0.5) ends.reverse();
    let ok = false;
    for (const [s, i] of ends) { const k = s + ':' + i; if (!gates[k]) { gates[k] = car.color; ok = true; break; } if (gates[k] === car.color) { ok = true; break; } }
    void ok;
  }
  // vũng sơn: đổi đích của vài xe sang màu khác, đặt vũng trong làn
  const paints = [];
  for (let p = 0; p < (sp.paints || 0); p++) {
    const car = pickR(rng, cars);
    const lane = [];
    for (let k = 0; k < (car.axis === 'h' ? cols : rows); k++) { const [a, b] = car.axis === 'h' ? [car.r, k] : [k, car.c]; if (!taken.has(key(a, b)) && !inTable(a, b)) lane.push([a, b]); }
    if (!lane.length) continue;
    const [pr, pc] = pickR(rng, lane), newColor = pickR(rng, sp.colors.filter((x) => x !== car.color));
    const free = slotsOf(car).find(([s, i]) => !gates[s + ':' + i] || gates[s + ':' + i] === car.color);
    if (!free) continue;
    gates[free[0] + ':' + free[1]] = newColor;
    paints.push({ r: pr, c: pc, color: newColor }); taken.add(key(pr, pc));
  }
  // cổng nước: ưu tiên đầu làn còn trống của xe bùn, rồi thêm vài cổng ngẫu nhiên
  for (const car of cars.filter((x) => x.mud)) {
    const free = slotsOf(car).filter(([s, i]) => !gates[s + ':' + i]);
    if (free.length && rng() < 0.75) { const [s, i] = pickR(rng, free); gates[s + ':' + i] = 'water'; }
  }
  const allSlots = [];
  for (let i = 0; i < cols; i++) allSlots.push(['t', i], ['b', i]);
  for (let i = 0; i < rows; i++) allSlots.push(['l', i], ['r', i]);
  for (let w = 0; w < (sp.extraWater || 0); w++) {
    const free = allSlots.filter(([s, i]) => !gates[s + ':' + i]);
    if (free.length) { const [s, i] = pickR(rng, free); gates[s + ':' + i] = 'water'; }
  }
  // các ô đặc biệt trên ô trống
  const emptyCells = () => { const out = []; for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) if (!taken.has(key(r, c)) && !inTable(r, c)) out.push([r, c]); return out; };
  const place = (n) => { const out = []; for (let k = 0; k < n; k++) { const e = emptyCells(); if (!e.length) break; const p = pickR(rng, e); taken.add(key(p[0], p[1])); out.push(p); } return out; };
  const muds = place(sp.muds || 0), sprays = place(sp.sprays || 0);
  const barriers = place(sp.barrier || 0).map(([r, c]) => ({ r, c, need: ri(rng, 1, 3) }));

  return {
    cols, rows, splash: !!sp.splash,
    gates: Object.entries(gates).map(([k, color]) => { const [side, i] = k.split(':'); return { side, i: +i, color }; }),
    cars, muds, sprays, paints, barriers, tables,
  };
}

// ---------------------------------------------------------------------
// CHẤM ĐIỂM: khám phá toàn bộ đồ thị trạng thái
// ---------------------------------------------------------------------
function analyze(level, cap = 40000) {
  const L = CW.compile(level);
  const s0 = CW.initState(L), k0 = CW.stateKey(L, s0);
  const nodes = new Map([[k0, { st: s0, out: [] }]]);
  const q = [k0];
  while (q.length) {
    const k = q.shift(), nd = nodes.get(k);
    for (let i = 0; i < L.cars.length; i++) {
      if (!nd.st.cars[i].alive) continue;
      const acts = [['move', i, 1], ['move', i, -1]];
      if (L.tables.length) acts.push(['rot', i, 0]);
      for (const a of acts) {
        const res = a[0] === 'rot' ? CW.rotate(L, nd.st, i) : CW.move(L, nd.st, i, a[2]);
        if (!res || !res.moved) continue;
        const k2 = CW.stateKey(L, res.state);
        nd.out.push({ a, k: k2, ev: res.events.map((e) => e.t) });
        if (!nodes.has(k2)) { nodes.set(k2, { st: res.state, out: [] }); q.push(k2); if (nodes.size > cap) return null; }
      }
    }
  }
  // lan ngược từ trạng thái thắng: tìm mọi trạng thái còn thắng được + khoảng cách tới đích
  const rev = new Map();
  for (const [k, nd] of nodes) for (const o of nd.out) { if (!rev.has(o.k)) rev.set(o.k, []); rev.get(o.k).push(k); }
  const dist = new Map(), bq = [];
  for (const [k, nd] of nodes) if (CW.isWin(nd.st)) { dist.set(k, 0); bq.push(k); }
  while (bq.length) { const k = bq.shift(); for (const p of rev.get(k) || []) if (!dist.has(p)) { dist.set(p, dist.get(k) + 1); bq.push(p); } }
  if (!dist.has(k0)) return { solvable: false };

  // đi theo lời giải ngắn nhất, đo tỉ lệ nước đi dẫn vào bế tắc ở mỗi bước
  const path = [], used = new Set();
  let k = k0, trapSum = 0, steps = 0, splashTrap = false;
  while (dist.get(k) > 0) {
    const nd = nodes.get(k);
    const good = nd.out.filter((o) => dist.has(o.k) && dist.get(o.k) === dist.get(k) - 1);
    const bad = nd.out.filter((o) => !dist.has(o.k));
    if (bad.some((o) => o.ev.includes('splash'))) splashTrap = true;
    trapSum += nd.out.length ? bad.length / nd.out.length : 0; steps++;
    const o = good[0];
    o.ev.forEach((e) => used.add(e)); if (o.a[0] === 'rot') used.add('rot');
    path.push(o.a); k = o.k;
  }
  let dead = 0; for (const kk of nodes.keys()) if (!dist.has(kk)) dead++;
  if (splashTrap) used.add('splashTrap');
  return { solvable: true, solLen: path.length, path, trap: trapSum / steps, deadRatio: dead / nodes.size, states: nodes.size, used };
}

// ---------------------------------------------------------------------
// CHẠY: sinh N ứng viên, giữ những màn tốt nhất
// ---------------------------------------------------------------------
if (require.main === module) {
  const name = process.argv[2], N = +(process.argv[3] || 2000), seed = +(process.argv[4] || 1), budget = +(process.argv[5] || 100) * 1000;
  const sp = SPECS[name], rng = makeRng(seed), best = [];
  const t0 = Date.now();
  let n = 0;
  for (; n < N && Date.now() - t0 < budget; n++) {
    const lv = genLevel(rng, sp);
    if (lv.cars.length < sp.cars[0]) continue;
    // lọc nhanh: phải giải được và đủ dài trước khi khám phá toàn bộ
    const Lq = CW.compile(lv), quick = CW.solve(Lq, CW.initState(Lq), 25000);
    if (!quick.moves || quick.moves.length < lv.cars.length + 3) continue;
    const a = analyze(lv);
    if (!a || !a.solvable) continue;
    if (!sp.need.every((m) => a.used.has(m))) continue;
    // mọi xe phải "đáng giá": lời giải dài hơn số xe ít nhất 3 lượt
    if (a.solLen < lv.cars.length + 3) continue;
    const score = a.solLen + 10 * a.trap + 4 * a.deadRatio;
    best.push({ score, a, lv });
    best.sort((x, y) => y.score - x.score); best.length = Math.min(best.length, 5);
  }
  for (const b of best) {
    console.log(`score ${b.score.toFixed(2)} | ${b.a.solLen} lượt | bẫy ${(b.a.trap * 100).toFixed(0)}% | bế tắc ${(b.a.deadRatio * 100).toFixed(0)}% | ${b.a.states} trạng thái | dùng: ${[...b.a.used].join(',')}`);
    console.log(JSON.stringify(b.lv));
  }
  console.error(`(${n} ứng viên, ${((Date.now() - t0) / 1000).toFixed(1)}s)`);
}
module.exports = { genLevel, analyze, SPECS, makeRng };
