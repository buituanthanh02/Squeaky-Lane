// =====================================================================
// SINH MÀN LỚN KIỂU "KẸT CỨNG": xếp xe dày đặc, chỉ giữ màn mà lúc đầu gần như
// không xe nào ra thẳng được (≤ maxFree), rồi lọc màn giải được + chấm bằng bot ngẫu nhiên.
//   node tools/dense.js <spec> <seed> <giây>
// =====================================================================
const CW = require('../src/logic.js');
const { makeRng } = require('./gen.js');
const { findPath, botStats, winnable } = require('./big.js');

const SPECS = {
  dong: { cols: 7, rows: 8, fill: 0.62, cars: [11, 14], colors: ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'], mudRate: 0.4, water: 3, maxFree: 3, splash: true },
  xoay_son: { cols: 7, rows: 7, fill: 0.58, cars: [10, 13], colors: ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'], mudRate: 0.25, water: 2, tables: 2, paints: 2, maxFree: 2, splash: true },
  tong_luc: { cols: 7, rows: 8, fill: 0.6, cars: [12, 14], colors: ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'], mudRate: 0.4, thickRate: 0.3, water: 2, tables: 1, paints: 1, sprays: 1, muds: 1, maxFree: 2, splash: true },
};

function gen(rng, sp) {
  const { cols, rows } = sp, ri = (a, b) => a + Math.floor(rng() * (b - a + 1)), pick = (a) => a[Math.floor(rng() * a.length)];
  const k = (r, c) => r + ',' + c, taken = new Set();
  const tables = [];
  for (let t = 0; t < (sp.tables || 0); t++) { const r = ri(1, rows - 3), c = ri(1, cols - 3); if (tables.some(([a, b]) => Math.abs(a - r) < 3 && Math.abs(b - c) < 3)) continue; tables.push([r, c]); }
  const inT = (r, c) => tables.some(([tr, tc]) => r >= tr && r <= tr + 1 && c >= tc && c <= tc + 1);
  // cổng dựng dần theo xe: mỗi xe có đường hầm cùng màu ở một đầu làn; xe bùn có vòi phun ở đầu kia (nếu còn chỗ)
  const gates = {}, count = {};
  const cars = [], nTarget = ri(sp.cars[0], sp.cars[1]);
  for (let t = 0; t < 800 && cars.length < nTarget; t++) {
    const axis = rng() < 0.5 ? 'h' : 'v', r = ri(0, rows - (axis === 'v' ? 2 : 1)), c = ri(0, cols - (axis === 'h' ? 2 : 1));
    const cells = [[r, c], axis === 'h' ? [r, c + 1] : [r + 1, c]];
    if (cells.some(([a, b]) => taken.has(k(a, b)))) continue;
    if (cells.filter(([a, b]) => inT(a, b)).length === 1) continue;
    const ends = (axis === 'h' ? [['l', r], ['r', r]] : [['t', c], ['b', c]]).map(([a, b]) => a + ':' + b);
    if (rng() < 0.5) ends.reverse();
    let mud = 0; if (rng() < sp.mudRate) mud = rng() < (sp.thickRate || 0) ? 2 : 1;
    let color = null;
    const tun = ends.find((e) => gates[e] && gates[e] !== 'water');
    if (tun && rng() < 0.85) color = gates[tun];
    else { const fe = ends.find((e) => !gates[e]); if (!fe) continue; color = pick(sp.colors.filter((x) => (count[x] || 0) < 2)) || pick(sp.colors); gates[fe] = color; count[color] = (count[color] || 0) + 1; }
    if (mud) { const other = ends.find((e) => !gates[e]); if (other && rng() < 0.8) gates[other] = 'water'; }
    cars.push({ axis, r, c, color, mud }); cells.forEach(([a, b]) => taken.add(k(a, b)));
  }
  const empty = () => { const o = []; for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) if (!taken.has(k(r, c)) && !inT(r, c)) o.push([r, c]); return o; };
  const place = (n) => { const o = []; for (let q = 0; q < n; q++) { const e = empty(); if (!e.length) break; const p = pick(e); taken.add(k(p[0], p[1])); o.push(p); } return o; };
  const lv = { cols, rows, splash: !!sp.splash, tables,
    gates: Object.entries(gates).map(([kk, color]) => { const [side, i] = kk.split(':'); return { side, i: +i, color }; }), cars,
    muds: place(sp.muds || 0), sprays: place(sp.sprays || 0), paints: place(sp.paints || 0).map(([r, c]) => ({ r, c, color: pick(sp.colors) })), barriers: [] };
  return lv;
}

/** Số xe ra thẳng được ngay lượt đầu */
function freeExits(L, st) { let n = 0; st.cars.forEach((c, i) => { for (const s of [1, -1]) { const r = CW.move(L, st, i, s); if (r && r.exited) { n++; break; } } }); return n; }

if (require.main === module && process.argv[2] !== 'climb') {
  const [name, seed, sec] = [process.argv[2], +(process.argv[3] || 1), +(process.argv[4] || 120)];
  const sp = SPECS[name], rng = makeRng(seed), t0 = Date.now(), best = [];
  let n = 0, solv = 0;
  while (Date.now() - t0 < sec * 1000) {
    n++;
    const lv = gen(rng, sp);
    if (lv.cars.length < sp.cars[0]) continue;
    const L = CW.compile(lv), s0 = CW.initState(L);
    if (freeExits(L, s0) > sp.maxFree) continue;
    const path = findPath(L, s0, 60000);
    if (!path) continue;
    solv++;
    const bot = botStats(L, 40, rng);
    const used = new Set(); let st = s0;
    for (const a of path) { const r = a[0] === 'rot' ? CW.rotate(L, st, a[1]) : CW.move(L, st, a[1], a[2]); r.events.forEach((e) => used.add(e.t)); if (a[0] === 'rot') used.add('rot'); st = r.state; }
    const sc = bot.stuck * 100 + (1 - bot.wins) * 30 + (path.length - lv.cars.length) * 3 + lv.cars.length;
    best.push({ sc, lv, path, bot, used: [...used] }); best.sort((a, b) => b.sc - a.sc); best.length = Math.min(best.length, 3);
  }
  console.error(`${name}: ${n} ứng viên, ${solv} giải được`);
  for (const b of best) {
    console.log(`score ${b.sc.toFixed(0)} | ${b.lv.cars.length} xe | lời giải ${b.path.length} (thừa ${b.path.length - b.lv.cars.length}) | bot kẹt ${(b.bot.stuck * 100).toFixed(0)}% thắng ${(b.bot.wins * 100).toFixed(0)}% | ${b.used.join(',')}`);
    console.log(JSON.stringify(b.lv));
  }
}
module.exports = { gen, SPECS, freeExits };

// ---------------------------------------------------------------------
// LEO ĐỒI từ một màn kẹt cứng giải được: đột biến (dời xe, đổi màu/bùn, đổi cổng, thêm xe)
//   node tools/dense.js climb <spec> <seed> <giây>
// ---------------------------------------------------------------------
if (require.main === module && process.argv[2] === 'climb') {
  const { mutate } = require('./climb.js');
  const { addCar } = require('./big.js');
  const [, , , name, seed, sec] = process.argv;
  const sp = SPECS[name], rng = makeRng(+seed), t0 = Date.now();
  const evalLv = (lv, n = 24) => {
    const L = CW.compile(lv), s0 = CW.initState(L);
    if (freeExits(L, s0) > sp.maxFree + 1) return null;
    const path = findPath(L, s0, 50000); if (!path) return null;
    const bot = botStats(L, n, rng), used = new Set(); let st = s0;
    for (const a of path) { const r = a[0] === 'rot' ? CW.rotate(L, st, a[1]) : CW.move(L, st, a[1], a[2]); r.events.forEach((e) => used.add(e.t)); if (a[0] === 'rot') used.add('rot'); st = r.state; }
    return { s: bot.stuck * 100 + (1 - bot.wins) * 30 + (path.length - lv.cars.length) * 4 + lv.cars.length * 2, path, bot, used };
  };
  let cur = null, tries = 0;
  while (!cur && Date.now() - t0 < sec * 500) { tries++; const lv = gen(rng, sp); if (lv.cars.length < sp.cars[0]) continue; const q = evalLv(lv); if (q) cur = { lv, q }; }
  if (!cur) { console.log(name, 'không tìm được màn khởi đầu', tries); process.exit(0); }
  let best = cur, it = 0;
  const spm = { ...sp, need: [] };
  while (Date.now() - t0 < sec * 1000) {
    it++;
    const cand = rng() < 0.25 && cur.lv.cars.length < sp.cars[1] ? addCar(rng, cur.lv, sp) : mutate(rng, cur.lv, spm);
    const q = evalLv(cand); if (!q) continue;
    if (q.s >= cur.q.s || rng() < 0.03) cur = { lv: cand, q };
    if (q.s > best.q.s) best = { lv: cand, q };
  }
  const L = CW.compile(best.lv), fin = botStats(L, 150, rng);
  console.log(`${name} seed ${seed}: khởi đầu sau ${tries} màn, ${it} vòng | ${best.lv.cars.length} xe | lời giải ${best.q.path.length} (thừa ${best.q.path.length - best.lv.cars.length}) | bot kẹt ${(fin.stuck * 100).toFixed(0)}% thắng ${(fin.wins * 100).toFixed(0)}% | ${[...best.q.used].join(',')}`);
  console.log(JSON.stringify(best.lv));
  console.log('PATH ' + JSON.stringify(best.q.path));
}
