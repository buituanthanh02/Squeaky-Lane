// =====================================================================
// BỘ THIẾT KẾ MÀN LỚN (7×8, 10–13 xe): không duyệt hết được không gian trạng thái,
// nên đo độ khó bằng NGƯỜI CHƠI NGẪU NHIÊN: cho bot đi ngẫu nhiên nhiều lần,
// sau mỗi nước kiểm tra "còn đường thắng không" — tỉ lệ bot rơi vào bế tắc = độ khó.
//   node tools/big.js <spec> <seed> <giây>
// =====================================================================
const CW = require('../src/logic.js');
const { genLevel, makeRng } = require('./gen.js');
const { mutate } = require('./climb.js');

const BIG = {
  dong: { cols: 7, rows: 8, cars: [11, 13], colors: ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'], mudRate: 0.55, splash: true, extraWater: 2, need: ['water', 'splash'] },
  xoay_son: { cols: 7, rows: 7, cars: [10, 12], colors: ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'], mudRate: 0.35, splash: true, tables: 2, paints: 2, barrier: 1, extraWater: 1, need: ['rot', 'paint'] },
  tong_luc: { cols: 7, rows: 8, cars: [11, 13], colors: ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'], mudRate: 0.45, thickRate: 0.3, splash: true, muds: 2, sprays: 1, tables: 1, paints: 1, extraWater: 2, need: ['water', 'rot', 'splash'] },
};

/** Mọi nước đi hợp lệ từ trạng thái st */
function legal(L, st) {
  const out = [];
  st.cars.forEach((c, i) => {
    if (!c.alive) return;
    for (const sg of [1, -1]) { const r = CW.move(L, st, i, sg); if (r && r.moved) out.push({ a: ['move', i, sg], r }); }
    if (L.tables.length) { const r = CW.rotate(L, st, i); if (r && r.moved) out.push({ a: ['rot', i, 1], r }); }
  });
  return out;
}

/** Còn thắng được không: true / false / null (quá giới hạn) */
function winnable(L, st, cap = 30000) {
  const it = CW.winSearch(L, st, cap); let r;
  do r = it.next(); while (!r.done);
  return r.value.win ? true : r.value.exhausted ? false : null;
}

/** Hàng đợi ưu tiên (heap nhị phân) theo khoá số */
class Heap {
  constructor() { this.a = []; }
  get size() { return this.a.length; }
  push(k, v) { const a = this.a; a.push([k, v]); let i = a.length - 1; while (i > 0) { const p = (i - 1) >> 1; if (a[p][0] <= a[i][0]) break; [a[p], a[i]] = [a[i], a[p]]; i = p; } }
  pop() { const a = this.a, top = a[0], last = a.pop(); if (a.length) { a[0] = last; let i = 0; for (;;) { const l = 2 * i + 1, r = l + 1; let m = i; if (l < a.length && a[l][0] < a[m][0]) m = l; if (r < a.length && a[r][0] < a[m][0]) m = r; if (m === i) break; [a[m], a[i]] = [a[i], a[m]]; i = m; } } return top[1]; }
}

/** Tìm một lời giải (ưu tiên ít xe còn lại, rồi ít nước) — trả về danh sách nước đi */
function findPath(L, st0, cap = 150000) {
  const key = (s) => CW.stateKey(L, s), k0 = key(st0), seen = new Map([[k0, null]]);
  const alive = (s) => { let n = 0; for (const c of s.cars) if (c.alive) n++; return n; };
  const h = new Heap(); h.push(alive(st0) * 1000, [st0, k0, 0]);
  while (h.size) {
    const [st, k, d] = h.pop();
    if (CW.isWin(st)) { const path = []; let kk = k; while (seen.get(kk)) { const [pk, a] = seen.get(kk); path.unshift(a); kk = pk; } return path; }
    for (const { a, r } of legal(L, st)) {
      const k2 = key(r.state); if (seen.has(k2)) continue;
      seen.set(k2, [k, a]); h.push(alive(r.state) * 1000 + d + 1, [r.state, k2, d + 1]);
      if (seen.size > cap) return null;
    }
  }
  return null;
}

/**
 * Độ khó bằng bot ngẫu nhiên: n ván, mỗi ván tối đa 80 nước.
 * Trả về tỉ lệ ván bị kẹt + số nước trung bình trước khi kẹt.
 */
function botStats(L, n, rng) {
  let stuck = 0, wins = 0, stepSum = 0;
  for (let g = 0; g < n; g++) {
    let st = CW.initState(L);
    for (let step = 0; step < 80; step++) {
      if (CW.isWin(st)) { wins++; break; }
      const ms = legal(L, st);
      if (!ms.length) { stuck++; stepSum += step; break; }
      st = ms[Math.floor(rng() * ms.length)].r.state;
      if (winnable(L, st, 2500) === false) { stuck++; stepSum += step; break; }
    }
  }
  return { stuck: stuck / n, wins: wins / n, avgStep: stuck ? stepSum / stuck : 0 };
}

/** Điểm khó: bot kẹt càng nhiều càng tốt; lời giải phải có "nước thừa" (đi dọn đường, đi rửa) */
function score(lv, rng, n = 60) {
  const L = CW.compile(lv);
  const path = findPath(L, CW.initState(L), 80000);
  if (!path) return null;
  // phải dùng đủ cơ chế yêu cầu (xét các sự kiện dọc lời giải)
  let st = CW.initState(L); const used = new Set();
  for (const a of path) { const r = a[0] === 'rot' ? CW.rotate(L, st, a[1]) : CW.move(L, st, a[1], a[2]); r.events.forEach((e) => used.add(e.t)); if (a[0] === 'rot') used.add('rot'); st = r.state; }
  const b = botStats(L, n, rng);
  return { s: b.stuck * 100 + (path.length - lv.cars.length) * 3, path, used, bot: b };
}

/**
 * Sinh màn lớn KIỂU DỒN DẦN: đặt cổng trước, rồi thêm từng xe một;
 * xe nào thêm vào làm màn hết giải được (hoặc không kết luận được) thì bỏ.
 * Nhờ vậy màn luôn giải được dù không gian trạng thái quá lớn để duyệt hết.
 */
function buildLevel(rng, sp, nTarget) {
  const { cols, rows } = sp, ri = (a, b) => a + Math.floor(rng() * (b - a + 1)), pick = (a) => a[Math.floor(rng() * a.length)];
  const slots = []; for (let i = 0; i < cols; i++) slots.push(['t', i], ['b', i]); for (let i = 0; i < rows; i++) slots.push(['l', i], ['r', i]);
  const gates = {};
  // mỗi màu 2 đường hầm, vài vòi phun
  for (const color of sp.colors) for (let k = 0; k < 2; k++) { const f = slots.filter(([a, b]) => !gates[a + ':' + b]); const [a, b] = pick(f); gates[a + ':' + b] = color; }
  for (let k = 0; k < (sp.extraWater || 0) + 1; k++) { const f = slots.filter(([a, b]) => !gates[a + ':' + b]); const [a, b] = pick(f); gates[a + ':' + b] = 'water'; }
  const lv = { cols, rows, splash: !!sp.splash, gates: [], cars: [], muds: [], sprays: [], paints: [], barriers: [], tables: [] };
  const sync = () => { lv.gates = Object.entries(gates).map(([k, color]) => { const [side, i] = k.split(':'); return { side, i: +i, color }; }); };
  sync();
  const taken = new Set(), key = (r, c) => r + ',' + c;
  for (let t = 0; t < (sp.tables || 0); t++) { const r = ri(1, rows - 3), c = ri(1, cols - 3); if ([0, 1].some((a) => [0, 1].some((b) => taken.has(key(r + a, c + b))))) continue; lv.tables.push([r, c]); }
  const inTable = (r, c) => lv.tables.some(([tr, tc]) => r >= tr && r <= tr + 1 && c >= tc && c <= tc + 1);
  const empty = () => { const o = []; for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) if (!taken.has(key(r, c)) && !inTable(r, c)) o.push([r, c]); return o; };
  const ok = () => { const L = CW.compile(lv); return winnable(L, CW.initState(L), 3000) === true; };
  for (let tries = 0; tries < 70 && lv.cars.length < nTarget; tries++) {
    const axis = rng() < 0.5 ? 'h' : 'v';
    const r = ri(0, rows - (axis === 'v' ? 2 : 1)), c = ri(0, cols - (axis === 'h' ? 2 : 1));
    const cells = [[r, c], axis === 'h' ? [r, c + 1] : [r + 1, c]];
    if (cells.some(([a, b]) => taken.has(key(a, b)))) continue;
    const tc = cells.filter(([a, b]) => inTable(a, b)).length; if (tc === 1) continue;
    const ends = axis === 'h' ? [['l', r], ['r', r]] : [['t', c], ['b', c]];
    const endColors = ends.map(([a, b]) => gates[a + ':' + b]).filter((x) => x && x !== 'water');
    const color = endColors.length && rng() < 0.4 ? pick(endColors) : pick(sp.colors);
    let mud = 0; if (rng() < (sp.mudRate || 0)) mud = rng() < (sp.thickRate || 0) ? 2 : 1;
    const car = { axis, r, c, color, mud };
    lv.cars.push(car); cells.forEach(([a, b]) => taken.add(key(a, b)));
    if (!ok()) { lv.cars.pop(); cells.forEach(([a, b]) => taken.delete(key(a, b))); }
  }
  // ô đặc biệt: chỉ giữ cái nào không phá thế giải được
  const addCell = (list, mk) => { for (let t = 0; t < 12; t++) { const e = empty(); if (!e.length) return; const [r, c] = pick(e); list.push(mk(r, c)); taken.add(key(r, c)); if (ok()) return; list.pop(); taken.delete(key(r, c)); } };
  for (let k = 0; k < (sp.muds || 0); k++) addCell(lv.muds, (r, c) => [r, c]);
  for (let k = 0; k < (sp.sprays || 0); k++) addCell(lv.sprays, (r, c) => [r, c]);
  for (let k = 0; k < (sp.paints || 0); k++) addCell(lv.paints, (r, c) => ({ r, c, color: pick(sp.colors) }));
  for (let k = 0; k < (sp.barrier || 0); k++) addCell(lv.barriers, (r, c) => ({ r, c, need: ri(1, 3) }));
  return lv;
}

/** Đột biến thêm: thêm 1 xe vào ô trống (màn lớn cần đông xe) */
function addCar(rng, lv0, sp) {
  const lv = JSON.parse(JSON.stringify(lv0)), occ = new Set(), k = (r, c) => r + ',' + c;
  lv.cars.forEach((car) => { for (let n = 0; n < (car.len || 2); n++) occ.add(car.axis === 'h' ? k(car.r, car.c + n) : k(car.r + n, car.c)); });
  for (const [r, c] of [...(lv.muds || []), ...(lv.sprays || [])]) occ.add(k(r, c));
  for (const p of [...(lv.paints || []), ...(lv.barriers || [])]) occ.add(k(p.r, p.c));
  const inT = (r, c) => (lv.tables || []).some(([tr, tc]) => r >= tr && r <= tr + 1 && c >= tc && c <= tc + 1);
  for (let t = 0; t < 40; t++) {
    const axis = rng() < 0.5 ? 'h' : 'v', r = Math.floor(rng() * (lv.rows - (axis === 'v' ? 1 : 0))), c = Math.floor(rng() * (lv.cols - (axis === 'h' ? 1 : 0)));
    const cells = [[r, c], axis === 'h' ? [r, c + 1] : [r + 1, c]];
    if (cells.some(([a, b]) => a >= lv.rows || b >= lv.cols || occ.has(k(a, b)))) continue;
    if (cells.filter(([a, b]) => inT(a, b)).length === 1) continue;
    let mud = 0; if (rng() < (sp.mudRate || 0)) mud = rng() < (sp.thickRate || 0) ? 2 : 1;
    lv.cars.push({ axis, r, c, color: sp.colors[Math.floor(rng() * sp.colors.length)], mud });
    return lv;
  }
  return lv;
}

/** Điểm khó (bản nhẹ cho leo đồi) */
function score2(lv, rng, n) {
  const L = CW.compile(lv), path = findPath(L, CW.initState(L), 40000);
  if (!path) return null;
  let st = CW.initState(L); const used = new Set();
  for (const a of path) { const r = a[0] === 'rot' ? CW.rotate(L, st, a[1]) : CW.move(L, st, a[1], a[2]); r.events.forEach((e) => used.add(e.t)); if (a[0] === 'rot') used.add('rot'); st = r.state; }
  const b = botStats(L, n, rng);
  return { s: b.stuck * 100 + (1 - b.wins) * 40 + (path.length - lv.cars.length) * 4, path, used, bot: b };
}

if (require.main === module) {
  const [name, seed, sec] = [process.argv[2], +(process.argv[3] || 1), +(process.argv[4] || 120)];
  const sp = BIG[name], rng = makeRng(seed), t0 = Date.now();
  let cur = null;
  while (!cur) { const lv = buildLevel(rng, sp, 9); const q = score2(lv, rng, 20); if (q) cur = { lv, q }; }
  let best = cur, it = 0;
  while (Date.now() - t0 < sec * 1000) {
    it++;
    const cand = (cur.lv.cars.length < sp.cars[1] && rng() < 0.3) ? addCar(rng, cur.lv, sp) : mutate(rng, cur.lv, sp);
    const q = score2(cand, rng, 20);
    if (!q) continue;
    const ok = sp.need.every((m) => q.used.has(m)) && cand.cars.length >= sp.cars[0];
    const bonus = (lv) => Math.min(lv.cars.length, sp.cars[1]) * 3;   // ưu tiên màn đông xe
    if (q.s + bonus(cand) >= cur.q.s + bonus(cur.lv) || rng() < 0.03) cur = { lv: cand, q };
    if (ok && (!best.ok || q.s > best.q.s)) best = { lv: cand, q, ok };
  }
  const L = CW.compile(best.lv), fin = botStats(L, 150, rng);
  console.log(`${name} seed ${seed}: ${it} vòng | đạt=${!!best.ok} | ${best.lv.cars.length} xe | lời giải ${best.q.path.length} nước (thừa ${best.q.path.length - best.lv.cars.length}) | bot kẹt ${(fin.stuck * 100).toFixed(0)}% thắng ${(fin.wins * 100).toFixed(0)}% | dùng ${[...best.q.used].join(',')}`);
  console.log(JSON.stringify(best.lv));
  console.log('PATH ' + JSON.stringify(best.q.path));
}
function ri0(rng, a, b) { return a + Math.floor(rng() * (b - a + 1)); }
module.exports = { BIG, findPath, botStats, winnable, legal, buildLevel, score, addCar };
