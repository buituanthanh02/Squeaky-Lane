// =====================================================================
// tools/mech.js — sinh màn có cơ chế mới (xe tải đẩy / ống cống / chốt barie / bàn xoay / đèn)
// Cách làm: ngẫu nhiên bố trí → giải BFS → chỉ giữ màn:
//   1. giải được;
//   2. MỖI cơ chế đã bật đều BẮT BUỘC (tắt nó đi thì không còn giải được — kiểm bằng BFS vét hết);
//   3. lời giải ngắn nhất càng dài càng tốt.
// Chạy: node tools/mech.js <spec-json> <số-lần-thử> <seed>
// =====================================================================
const CW = require('../src/logic.js');

function rng(seed) { let s = seed >>> 0; return () => { s = (s * 1664525 + 1013904223) >>> 0; return s / 4294967296; }; }
const COLORS = ['pink', 'blue', 'yellow', 'green', 'orange', 'purple'];

/** Dựng một màn ngẫu nhiên theo spec */
function randomLevel(spec, R) {
  const { cols, rows } = spec;
  const used = new Set();   // ô đã dùng
  const k = (r, c) => r + ',' + c;
  const free = (cells) => cells.every(([r, c]) => r >= 0 && c >= 0 && r < rows && c < cols && !used.has(k(r, c)));
  const take = (cells) => cells.forEach(([r, c]) => used.add(k(r, c)));
  const pickInt = (n) => Math.floor(R() * n);
  const lv = { cols, rows, gates: [], cars: [], name: 'gen' };
  if (spec.tables) {
    lv.tables = [];
    for (let n = 0; n < spec.tables; n++) for (let t = 0; t < 40; t++) {
      const r = pickInt(rows - 1), c = pickInt(cols - 1), cells = [[r, c], [r + 1, c], [r, c + 1], [r + 1, c + 1]];
      if (free(cells)) { take(cells); lv.tables.push([r, c]); break; }
    }
  }
  // cổng gắn tường: mặt cổng nằm trên mép bãi. borderFaces: mọi cặp (ô, cạnh) trên mép
  const borderFaces = [];
  for (let c = 0; c < cols; c++) { borderFaces.push([0, c, 'N']); borderFaces.push([rows - 1, c, 'S']); }
  for (let r = 0; r < rows; r++) { borderFaces.push([r, 0, 'W']); borderFaces.push([r, cols - 1, 'E']); }
  const usedFaces = new Set();
  const takeFace = () => {
    for (let t = 0; t < 60; t++) { const f = borderFaces[pickInt(borderFaces.length)]; const kk = f.join(','); if (!usedFaces.has(kk)) { usedFaces.add(kk); return f; } }
    return null;
  };
  if (spec.portals) {
    lv.portals = [];
    for (let n = 0; n < spec.portals; n++) { const a = takeFace(), b = takeFace(); if (a && b && (a[0] !== b[0] || a[1] !== b[1])) lv.portals.push([a, b]); }
  }
  if (spec.pylons) {
    lv.pylons = [];
    for (let n = 0; n < spec.pylons; n++) for (let t = 0; t < 40; t++) {
      const r = 1 + pickInt(rows - 2), c = 1 + pickInt(cols - 2);
      if (!free([[r, c]])) continue;
      const a = takeFace(), b = takeFace();
      if (!a || !b) break;
      take([[r, c]]); lv.pylons.push({ r, c, o: R() < 0.5 ? 'h' : 'v', to: [a, b] }); break;
    }
  }
  if (spec.switches) {
    lv.switches = [];
    for (let n = 0; n < spec.switches; n++) for (let t = 0; t < 60; t++) {
      const pad = [pickInt(rows), pickInt(cols)], a = [pickInt(rows), pickInt(cols)], b = [pickInt(rows), pickInt(cols)];
      if (free([pad, a, b]) && new Set([k(...pad), k(...a), k(...b)]).size === 3) { take([pad, a, b]); lv.switches.push({ r: pad[0], c: pad[1], a: [a], b: [b] }); break; }
    }
  }
  if (spec.lights) {
    lv.lights = [];
    for (let n = 0; n < spec.lights; n++) for (let t = 0; t < 40; t++) {
      const r = pickInt(rows), c = pickInt(cols);
      if (free([[r, c]])) { take([[r, c]]); lv.lights.push({ r, c, phase: pickInt(2) }); break; }
    }
  }
  if (spec.cones) {
    lv.cones = [];
    for (let n = 0; n < spec.cones; n++) for (let t = 0; t < 40; t++) {
      const r = pickInt(rows), c = pickInt(cols);
      if (free([[r, c]])) { take([[r, c]]); lv.cones.push([r, c]); break; }
    }
  }
  // xe
  const nColors = spec.colors || 4;
  for (let n = 0; n < spec.cars; n++) {
    const truck = n < (spec.trucks || 0), len = truck ? 3 : 2;
    for (let t = 0; t < 80; t++) {
      const axis = R() < 0.5 ? 'h' : 'v', r = pickInt(rows), c = pickInt(cols);
      const cells = Array.from({ length: len }, (_, q) => (axis === 'h' ? [r, c + q] : [r + q, c]));
      if (!free(cells)) continue;
      take(cells);
      lv.cars.push({ axis, r, c, len, color: COLORS[pickInt(nColors)], mud: 0, ...(truck ? { truck: true } : {}) });
      break;
    }
  }
  // cổng: mỗi màu 1 cổng (đôi khi 2) ở mép, mỗi vị trí mép tối đa 1 cổng
  const slots = [];
  for (let i = 0; i < rows; i++) { slots.push(['l', i]); slots.push(['r', i]); }
  for (let i = 0; i < cols; i++) { slots.push(['t', i]); slots.push(['b', i]); }
  for (let i = slots.length - 1; i > 0; i--) { const j = pickInt(i + 1); [slots[i], slots[j]] = [slots[j], slots[i]]; }
  const colorsUsed = [...new Set(lv.cars.map((c) => c.color))];
  let si = 0;
  for (const col of colorsUsed) { const n = R() < 0.35 ? 2 : 1; for (let q = 0; q < n && si < slots.length; q++) { const [side, i] = slots[si++]; lv.gates.push({ side, i, color: col }); } }
  return lv;
}

/** Bản "tắt cơ chế" để kiểm tra cơ chế có bắt buộc không */
function ablations(lv, spec) {
  const out = [];
  const clone = () => JSON.parse(JSON.stringify(lv));
  if (spec.trucks) { const x = clone(); x.cars.forEach((c) => delete c.truck); out.push(['truck', x]); }
  if (spec.portals) { const x = clone(); delete x.portals; out.push(['portal', x]); }
  if (spec.pylons) {   // trụ không xoay được: thử mọi tổ hợp hướng cố định, tất cả đều phải vô nghiệm
    const n = lv.pylons.length;
    for (let m = 0; m < (1 << (2 * n)); m++) { const x = clone(); x.pylons.forEach((p, i) => { p.o = (m >> (2 * i)) & 3; p.fixed = true; }); out.push(['pylon' + m, x]); }
  }
  if (spec.switches) {
    const x = clone(); x.cones = (x.cones || []).concat(x.switches.flatMap((s) => s.b)); delete x.switches; out.push(['switch', x]);   // chốt không bao giờ được đè
  }
  if (spec.tables) { const x = clone(); x.tables = []; out.push(['table', x]); }
  if (spec.lights) { const x = clone(); x.cones = (x.cones || []).concat(x.lights.map((l) => [l.r, l.c])); delete x.lights; out.push(['light', x]); }
  return out;
}

function evaluate(lv, spec, maxStates = 60000) {
  const L = CW.compile(lv), st = CW.initState(L);
  const sol = CW.solve(L, st, maxStates);
  if (!sol.moves) return null;
  // cơ chế nào tắt đi vẫn giải được → không bắt buộc
  const need = {};
  for (const [name, x] of ablations(lv, spec)) {
    const r = CW.solve(CW.compile(x), CW.initState(CW.compile(x)), maxStates);
    const ok = !r.moves && r.exhausted;
    if (name.startsWith('pylon')) need.pylon = (need.pylon === undefined ? true : need.pylon) && ok;
    else need[name] = ok;
  }
  return { moves: sol.moves, len: sol.moves.length, explored: sol.explored, need };
}

/** Mọi ô tile cố định đã chiếm (để không xếp xe đè lên) */
function tileCells(lv) {
  const out = new Set(), add = (r, c) => out.add(r + ',' + c);
  for (const [r, c] of lv.cones || []) add(r, c);
  for (const p of lv.pylons || []) add(p.r, p.c);
  for (const s of lv.switches || []) { add(s.r, s.c); s.a.forEach((x) => add(...x)); s.b.forEach((x) => add(...x)); }
  for (const l of lv.lights || []) add(l.r, l.c);
  for (const [r, c] of lv.tables || []) { add(r, c); add(r + 1, c); add(r, c + 1); add(r + 1, c + 1); }
  return out;
}

/** Đặt thử một xe ngẫu nhiên (không đè tile/xe khác). null nếu không đặt được */
function placeCar(lv, spec, R, truck, colors) {
  const pickInt = (n) => Math.floor(R() * n), len = truck ? 3 : 2, used = tileCells(lv);
  for (const c of lv.cars) for (let q = 0; q < c.len; q++) used.add((c.axis === 'h' ? c.r : c.r + q) + ',' + (c.axis === 'h' ? c.c + q : c.c));
  for (let t = 0; t < 80; t++) {
    const axis = R() < 0.5 ? 'h' : 'v', r = pickInt(lv.rows), c = pickInt(lv.cols);
    const cells = Array.from({ length: len }, (_, q) => (axis === 'h' ? [r, c + q] : [r + q, c]));
    if (cells.some(([rr, cc]) => rr >= lv.rows || cc >= lv.cols || used.has(rr + ',' + cc))) continue;
    return { axis, r, c, len, color: colors[pickInt(colors.length)], mud: 0, ...(truck ? { truck: true } : {}) };
  }
  return null;
}

const quick = (lv, max = 30000) => { const L = CW.compile(lv); const r = CW.solve(L, CW.initState(L), max); return r.moves ? r.moves.length : -1; };

/** Chọn cổng cho một xe: hoặc dùng cổng sẵn có cùng màu, hoặc mở cổng mới (55% ngay trên đường xe chạy, 45% chỗ khác → buộc dùng cơ chế) */
function gateFor(lv, car, R) {
  const pickInt = (n) => Math.floor(R() * n);
  const taken = new Set(lv.gates.map((g) => g.side + ':' + g.i));
  // vị trí mép đã có mặt cổng thì không đặt cổng xe ở đó
  const faceGate = (f) => (f[2] === 'W' ? 'l:' + f[0] : f[2] === 'E' ? 'r:' + f[0] : f[2] === 'N' ? 't:' + f[1] : 'b:' + f[1]);
  for (const [a, b] of lv.portals || []) { taken.add(faceGate(a)); taken.add(faceGate(b)); }
  for (const p of lv.pylons || []) for (const f of p.to) taken.add(faceGate(f));
  const same = lv.gates.filter((g) => g.color === car.color);
  if (same.length && R() < 0.4) return null;
  for (let t = 0; t < 10; t++) {
    let side, i;
    if (R() < 0.55) {
      if (car.axis === 'h') { side = R() < 0.5 ? 'l' : 'r'; i = car.r; } else { side = R() < 0.5 ? 't' : 'b'; i = car.c; }
    } else {
      const k = pickInt(2 * lv.rows + 2 * lv.cols);
      if (k < lv.rows) { side = 'l'; i = k; } else if (k < 2 * lv.rows) { side = 'r'; i = k - lv.rows; }
      else if (k < 2 * lv.rows + lv.cols) { side = 't'; i = k - 2 * lv.rows; } else { side = 'b'; i = k - 2 * lv.rows - lv.cols; }
    }
    if (!taken.has(side + ':' + i)) return { side, i, color: car.color };
  }
  return null;
}

/** Dựng màn: đặt tile → thêm xe + cổng từng chiếc (giữ giải được) → leo đồi tăng độ dài lời giải + bắt buộc cơ chế */
function run(spec, tries, seed) {
  const R = rng(seed), pickInt = (n) => Math.floor(R() * n);
  const colors = COLORS.slice(0, spec.colors || 4);
  let best = null;
  for (let n = 0; n < tries; n++) {
    const lv = randomLevel({ ...spec, cars: 0 }, R);
    let ok = true;
    for (let q = 0; q < spec.cars; q++) {
      let placed = false;
      for (let t = 0; t < 30 && !placed; t++) {
        const car = placeCar(lv, spec, R, q < (spec.trucks || 0), colors);
        if (!car) continue;
        const g = gateFor(lv, car, R);
        lv.cars.push(car); if (g) lv.gates.push(g);
        if (lv.gates.some((x) => x.color === car.color) && quick(lv, 8000) >= 0) placed = true;
        else { lv.cars.pop(); if (g) lv.gates.pop(); }
      }
      if (!placed) { ok = false; break; }
    }
    if (!ok) continue;
    // leo đồi: dời một xe (giữ màu + cổng), giữ lại nếu lời giải không ngắn đi
    let cur = evaluate(lv, spec);
    if (!cur) continue;
    const scoreOf = (e) => e.len + Object.values(e.need).filter(Boolean).length * 12;
    let curScore = scoreOf(cur);
    for (let it = 0; it < (spec.climb || 60); it++) {
      const i = pickInt(lv.cars.length), old = lv.cars[i];
      lv.cars.splice(i, 1);
      const car = placeCar(lv, spec, R, !!old.truck, [old.color]);
      if (!car) { lv.cars.splice(i, 0, old); continue; }
      lv.cars.splice(i, 0, car);
      const e = evaluate(lv, spec);
      const sc = e ? scoreOf(e) : -1;
      if (e && sc >= curScore) { cur = e; curScore = sc; } else { lv.cars.splice(i, 1, old); }
    }
    const allNeed = Object.values(cur.need).every(Boolean);
    const total = curScore + (allNeed ? 100 : 0);
    if (!best || total > best.score) best = { score: total, lv: JSON.parse(JSON.stringify(lv)), e: cur };
  }
  return best;
}

module.exports = { randomLevel, evaluate, run, ablations, rng };

if (require.main === module) {
  const spec = JSON.parse(process.argv[2]), tries = +process.argv[3] || 200, seed = +process.argv[4] || 1;
  const t0 = Date.now();
  const b = run(spec, tries, seed);
  if (!b) { console.log('không có màn giải được'); process.exit(0); }
  console.log(JSON.stringify({ score: b.score, len: b.e.len, need: b.e.need, ms: Date.now() - t0 }));
  console.log(JSON.stringify(b.lv));
}
