// ===== carwash2/logic.js =====
// =====================================================================
// TRẠM RỬA XE v2 — luật chơi thuần (không vẽ), dùng chung cho game + bộ giải.
//
// LUẬT GỐC
// - Bãi lưới cols × rows. Xe dài 2 (hoặc 3), nằm ngang 'h' / dọc 'v',
//   chỉ chạy dọc theo thân (tới/lùi), chạy tới khi vướng.
// - Mép bãi có cổng. Cổng màu X: xe màu X và SẠCH BÙN thì chạy xuyên qua
//   (được rửa, rời bãi). Không thì xe dừng sát mép.
//
// CƠ CHẾ MỚI
// - mud (bùn): 0 sạch · 1 bám bùn (vẫn thấy màu) · 2 bùn dày (che màu, hiện "?").
//   Xe còn bùn thì cổng màu không nhận.
// - Cổng nước ('water'): xe chui vào, được xịt sạch bùn, rồi bị ĐẨY NGƯỢC
//   vào bãi và trượt tiếp theo chiều ngược lại (mỗi lượt chỉ dội 1 lần).
// - Bùn văng (level.splash): xe còn bùn đâm vào xe sạch → xe kia bám bùn.
// - Vũng bùn (muds): xe đầu tiên lăn qua bị bám bùn, vũng biến mất.
// - Vũng sơn (paints): xe lăn qua đổi sang màu của vũng.
// - Ô vòi xịt (sprays): xe lăn qua được xịt sạch bùn (không dừng lại).
// - Bàn xoay (tables, 2×2): xe dài 2 đứng gọn trên bàn → xoay 90° theo
//   chiều kim đồng hồ (tính là 1 lượt).
// - Barie (barriers): chắn ô cho tới khi rửa đủ `need` xe.
// - Đèn giao thông (lights): sau MỖI lượt đổi xanh ↔ đỏ. Đèn đỏ chắn ô.
// - Cọc (cones): vật cản cố định.
// - Xe tải (car.truck, dài 3): gặp xe khác thì ĐẨY cả hàng xe phía trước đi 1 ô (nếu phía sau hàng còn chỗ trống),
//   xe tải tiến vào ô vừa trống rồi dừng. Xe bị đẩy vẫn nhận hiệu ứng ô mới (sơn, bùn, vòi xịt).
// - Cổng dịch chuyển kiểu portal gun (portals): GẮN LÊN MẶT TƯỜNG (mép bãi), mỗi cổng là [r, c, cạnh] với cạnh 'N'|'E'|'S'|'W' của ô (r,c).
//   Đầu xe tới sát cổng A rồi chạy tiếp thì chui vào, trồi ra ở cổng B theo hướng VUÔNG GÓC mặt tường B (nên xe có thể đổi ngang ↔ dọc).
//   Xe trồi ra nguyên cả thân: đuôi sát tường, các ô thân phải trống, nếu không thì cổng như bức tường. Mỗi lượt chui tối đa 1 lần.
// - Trụ xoay (pylons): {r,c,o:'h'|'v',to:[cổng0,cổng1]}. Là một ô tường có HAI MẶT cổng. 'h': mặt trên + mặt dưới; 'v': mặt trái + mặt phải. Chạm = xoay thêm 90° cùng một chiều (4 hướng: mặt 0 đi trên → trái → dưới → phải).
//   Mặt thứ nhất (trên/trái) nối với cổng to[0], mặt thứ hai (dưới/phải) nối với to[1]. CHẠM trụ để xoay 90° (tính 1 lượt) → đổi xe nào vào được.
// - Barie liên động (switches): {r,c,a:[[r,c]..],b:[[r,c]..]}. Chốt ở ô (r,c). Không có xe đè chốt: barie A MỞ, barie B ĐÓNG.
//   Có xe đè chốt (một ô thân xe nằm trên chốt): A ĐÓNG, B MỞ. Trạng thái tính theo vị trí xe ở ĐẦU lượt đi.
// =====================================================================
const CW = (() => {
  const key = (r, c) => r + ',' + c;

  /** Chuẩn hoá màn chơi: dựng bảng tra nhanh cho cổng và các loại ô */
  function compile(level) {
    const L = Object.assign({}, level);
    L.gateMap = {};
    for (const g of level.gates || []) L.gateMap[g.side + ':' + g.i] = g;
    L.coneSet = new Set((level.cones || []).map(([r, c]) => key(r, c)));
    L.barriers = level.barriers || [];
    L.paintMap = {};
    for (const p of level.paints || []) L.paintMap[key(p.r, p.c)] = p.color;
    L.mudList = (level.muds || []).map(([r, c]) => ({ r, c }));
    L.mudIndex = {};
    L.mudList.forEach((m, i) => (L.mudIndex[key(m.r, m.c)] = i));
    L.spraySet = new Set((level.sprays || []).map(([r, c]) => key(r, c)));
    L.lights = (level.lights || []).map((l) => ({ phase: 0, ...l }));
    L.tables = (level.tables || []).map(([r, c]) => ({ r, c }));
    L.splash = !!level.splash;
    // cổng gắn tường + trụ xoay: danh sách "mặt cổng" tra theo hướng của từng trụ (xem faceList)
    L.portals = (level.portals || []).map(([a, b]) => [{ r: a[0], c: a[1], side: a[2] }, { r: b[0], c: b[1], side: b[2] }]);
    L.pylons = (level.pylons || []).map((p) => ({ r: p.r, c: p.c, o: pylQ(p.o), fixed: !!p.fixed, to: p.to.map(([r, c, side]) => ({ r, c, side })) }));
    L.pylonSet = new Set(L.pylons.map((p) => key(p.r, p.c)));
    L.faceCache = {};
    L.switches = level.switches || [];
    L.cars = level.cars.map((c, i) => Object.assign({ id: i, len: 2, mud: 0 }, c));
    return L;
  }

  function initState(L) {
    return {
      cars: L.cars.map((c) => ({ r: c.r, c: c.c, axis: c.axis, color: c.color, mud: c.mud, alive: true })),
      muds: L.mudList.map(() => true), // vũng bùn còn hay đã bị hút
      pyl: L.pylons.map((p) => p.o), // hướng từng trụ xoay: số lần đã xoay 90° ngược chiều kim đồng hồ (0..3)
      moves: 0,
    };
  }

  const washedCount = (st) => st.cars.reduce((n, c) => n + (c.alive ? 0 : 1), 0);

  /** Đèn đỏ hay xanh ở lượt hiện tại (phase 0: lượt đầu là xanh) */
  const lightRed = (st, l) => (st.moves + l.phase) % 2 === 1;

  function carCells(len, s) {
    const out = [];
    for (let k = 0; k < len; k++) out.push(s.axis === 'h' ? [s.r, s.c + k] : [s.r + k, s.c]);
    return out;
  }

  /** Có xe nào đang đè lên chốt sw không */
  function switchPressed(L, st, sw) {
    for (let i = 0; i < st.cars.length; i++) {
      const s = st.cars[i];
      if (!s.alive) continue;
      for (const [r, c] of carCells(L.cars[i].len, s)) if (r === sw.r && c === sw.c) return true;
    }
    return false;
  }

  /** Bản đồ chiếm chỗ: -1 trống, -2 vật cản, >=0 chỉ số xe */
  function occupancy(L, st, skip = -1) {
    const g = new Array(L.rows * L.cols).fill(-1);
    const put = (r, c, v) => { if (r >= 0 && c >= 0 && r < L.rows && c < L.cols) g[r * L.cols + c] = v; };
    for (const k of L.coneSet) { const [r, c] = k.split(',').map(Number); put(r, c, -2); }
    for (const p of L.pylons) put(p.r, p.c, -2);
    const washed = washedCount(st);
    for (const b of L.barriers) if (washed < b.need) put(b.r, b.c, -2);
    for (const l of L.lights) if (lightRed(st, l)) put(l.r, l.c, -2);
    for (const sw of L.switches) for (const [r, c] of switchPressed(L, st, sw) ? sw.a : sw.b) put(r, c, -2);
    st.cars.forEach((s, i) => {
      if (s.alive && i !== skip) for (const [r, c] of carCells(L.cars[i].len, s)) put(r, c, i);
    });
    return g;
  }

  const cloneState = (st) => ({ cars: st.cars.map((x) => ({ ...x })), muds: st.muds.slice(), pyl: st.pyl.slice(), moves: st.moves });

  /**
   * Xe tải i (chạy theo chiều dir) đẩy xe firstJ ở ô đầu xe. Trả về danh sách xe bị đẩy (cả chuỗi xe nằm sau xe đó),
   * hoặc null nếu không đẩy được (bị tường / cọc / barie / đèn đỏ / mép bãi chặn).
   * Mọi xe bị đẩy dịch đúng 1 ô theo chiều dir, kể cả xe nằm vuông góc (bị hất sang ngang).
   */
  function tryPush(L, st, i, dir, firstJ) {
    const vec = st.cars[i].axis === 'h' ? [0, dir] : [dir, 0];
    const occ = occupancy(L, st);
    const pushed = [firstJ], inSet = new Set(pushed);
    for (let n = 0; n < pushed.length; n++) {
      const j = pushed[n];
      for (const [r, c] of carCells(L.cars[j].len, st.cars[j])) {
        const tr = r + vec[0], tc = c + vec[1];
        if (tr < 0 || tc < 0 || tr >= L.rows || tc >= L.cols) return null;
        const v = occ[tr * L.cols + tc];
        if (v === -2 || v === i) return null;
        if (v >= 0 && !inSet.has(v)) { inSet.add(v); pushed.push(v); }
      }
    }
    return pushed;
  }

  // ---- Cổng gắn tường ----
  const SIDE_VEC = { N: [-1, 0], S: [1, 0], W: [0, -1], E: [0, 1] };
  const OPP = { N: 'S', S: 'N', W: 'E', E: 'W' };
  const vecSide = (vr, vc) => (vr < 0 ? 'N' : vr > 0 ? 'S' : vc < 0 ? 'W' : 'E');

  /** Đổi hướng ghi trong màn ('h' | 'v' | số 0..3) thành số lần xoay 90° (0 = nằm ngang, 1 = dựng đứng, 2 = ngang đảo mặt, 3 = dựng đứng đảo mặt) */
  const pylQ = (o) => (typeof o === 'number' ? ((o % 4) + 4) % 4 : o === 'v' ? 1 : 0);
  /** Thứ tự cạnh khi xoay ngược chiều kim đồng hồ: mặt 0 đi N → W → S → E, mặt 1 luôn ở cạnh đối diện */
  const PYL_SIDES = ['N', 'W', 'S', 'E'];
  const PYL_FACE = { N: (p) => ({ r: p.r - 1, c: p.c, side: 'S' }), S: (p) => ({ r: p.r + 1, c: p.c, side: 'N' }), W: (p) => ({ r: p.r, c: p.c - 1, side: 'E' }), E: (p) => ({ r: p.r, c: p.c + 1, side: 'W' }) };

  /** Hai mặt cổng của trụ p sau q lần xoay: mỗi mặt = {r,c,side}: ô đứng trước mặt + cạnh của ô đó chạm vào trụ */
  function pylonFaces(p, q) {
    const s0 = PYL_SIDES[q & 3];
    return [PYL_FACE[s0](p), PYL_FACE[OPP[s0]](p)];
  }

  /** Mọi mặt cổng đang hoạt động (kèm mặt đối diện `to`), tuỳ hướng các trụ. Có nhớ đệm theo hướng trụ. */
  function faceList(L, st) {
    const sig = st.pyl.join('');
    if (L.faceCache[sig]) return L.faceCache[sig];
    const list = [];
    const link = (a, b) => { list.push({ ...a, to: b }); list.push({ ...b, to: a }); };
    for (const [a, b] of L.portals) link(a, b);
    L.pylons.forEach((p, k) => { const f = pylonFaces(p, st.pyl[k]); link(f[0], p.to[0]); link(f[1], p.to[1]); });
    return (L.faceCache[sig] = list);
  }

  /**
   * Cho xe i chạy theo chiều sign (+1 phải/xuống, −1 trái/lên).
   * Trả về { state, events, moved, exited } — events là chuỗi sự kiện để phần vẽ diễn lại:
   *   step  {r,c}          tiến 1 ô
   *   paint {color}        vừa lăn qua vũng sơn
   *   mud   {idx}          vừa lăn qua vũng bùn (vũng biến mất)
   *   spray {}             vừa lăn qua ô vòi xịt
   *   water {gate,sign}    chui vào cổng nước, được xịt sạch, bị đẩy ngược lại
   *   exit  {gate}         ra cổng đúng màu
   *   stop  {why,gate,j}   dừng lại (edge | wrongGate | muddy | car | wall)
   *   splash{j}            bùn văng sang xe j
   *   push  {sign,cars:[{j,r,c,fx}]}  xe tải đẩy các xe j dịch 1 ô (r,c = vị trí mới)
   *   tele  {fr,fc,fside,tr,tc,tside,r,c,axis,sign}  chui vào cổng (ô fr,fc · cạnh fside) → trồi ra cổng (ô tr,tc · cạnh tside);
   *                                   (r,c) = góc thân xe sau khi trồi ra, axis/sign = hướng mới của xe
   */
  function move(L, st0, i, sign) {
    const d = L.cars[i];
    if (!st0.cars[i].alive) return null;
    const st = cloneState(st0);
    const s = st.cars[i];
    const occ = occupancy(L, st0, i);
    const events = [];
    let dir = sign, bounced = false, exited = false, steps = 0, teleported = false;

    /** Đầu xe sát tường: có cổng ở đó không? 'none' | 'blocked' (cổng không dùng được: như tường) | 'ok' (đã chui, xe đã được đặt lại) */
    const portalTry = () => {
      const vec = s.axis === 'h' ? [0, dir] : [dir, 0];
      const hr = s.axis === 'h' ? s.r : dir > 0 ? s.r + d.len - 1 : s.r, hc = s.axis === 'h' ? (dir > 0 ? s.c + d.len - 1 : s.c) : s.c;
      const f = faceList(L, st).find((x) => x.r === hr && x.c === hc && x.side === vecSide(vec[0], vec[1]));
      if (!f) return 'none';
      if (teleported) return 'blocked';   // mỗi lượt chui tối đa 1 lần (tránh chạy vòng mãi giữa hai cổng)
      const ov = SIDE_VEC[OPP[f.to.side]], cells = [];
      for (let k = 0; k < d.len; k++) cells.push([f.to.r + ov[0] * k, f.to.c + ov[1] * k]);
      if (!cells.every(([r, c]) => r >= 0 && c >= 0 && r < L.rows && c < L.cols && occ[r * L.cols + c] === -1)) return 'blocked';
      s.axis = ov[0] === 0 ? 'h' : 'v';
      s.r = Math.min(...cells.map((x) => x[0])); s.c = Math.min(...cells.map((x) => x[1]));
      dir = ov[0] + ov[1] > 0 ? 1 : -1; teleported = true; steps++;
      events.push({ t: 'tele', fr: hr, fc: hc, fside: f.side, tr: f.to.r, tc: f.to.c, tside: f.to.side, r: s.r, c: s.c, axis: s.axis, sign: dir });
      for (const [r, c] of cells) {   // hiệu ứng của các ô vừa phủ (từ đuôi tới đầu)
        const kk = key(r, c);
        const pc = L.paintMap[kk];
        if (pc && pc !== s.color) { s.color = pc; events.push({ t: 'paint', color: pc }); }
        const mi = L.mudIndex[kk];
        if (mi != null && st.muds[mi]) { st.muds[mi] = false; s.mud = Math.max(s.mud, 1); events.push({ t: 'mud', idx: mi }); }
        if (L.spraySet.has(kk) && s.mud > 0) { s.mud = 0; events.push({ t: 'spray' }); }
      }
      return 'ok';
    };

    for (let guard = 0; guard < 80; guard++) {
      // ô "đầu xe" kế tiếp theo chiều chạy
      let nr, nc;
      if (s.axis === 'h') { nr = s.r; nc = dir > 0 ? s.c + d.len : s.c - 1; }
      else { nc = s.c; nr = dir > 0 ? s.r + d.len : s.r - 1; }

      if (nr < 0 || nc < 0 || nr >= L.rows || nc >= L.cols) {
        const pt = portalTry();
        if (pt === 'ok') continue;
        if (pt === 'blocked') { events.push({ t: 'stop', why: 'wall', sign: dir }); break; }
        const side = s.axis === 'h' ? (dir > 0 ? 'r' : 'l') : (dir > 0 ? 'b' : 't');
        const g = L.gateMap[side + ':' + (s.axis === 'h' ? s.r : s.c)];
        if (g && g.color === 'water' && !bounced) {
          // cổng nước: xịt sạch, đẩy ngược lại rồi trượt tiếp
          events.push({ t: 'water', gate: g, sign: dir, hadMud: s.mud });
          s.mud = 0; bounced = true; dir = -dir;
          continue;
        }
        if (g && g.color !== 'water' && g.color === s.color && s.mud === 0) {
          events.push({ t: 'exit', gate: g, sign: dir });
          exited = true; s.alive = false;
          break;
        }
        const why = !g || g.color === 'water' ? 'edge' : s.mud > 0 ? 'muddy' : 'wrongGate';
        events.push({ t: 'stop', why, gate: g || null, sign: dir });
        break;
      }

      const o = occ[nr * L.cols + nc];
      let pushedNow = false;
      if (o >= 0) {
        // xe tải: đẩy cả hàng xe phía trước đi 1 ô (nếu đẩy được) rồi tiến vào ô vừa trống
        const pushed = d.truck ? tryPush(L, st, i, dir, o) : null;
        if (pushed) {
          pushedNow = true;
          const vec = s.axis === 'h' ? [0, dir] : [dir, 0];
          const ev = { t: 'push', sign: dir, cars: [] };
          for (const j of pushed) {
            const sj = st.cars[j], dj = L.cars[j];
            const old = carCells(dj.len, sj).map(([r, c]) => key(r, c));
            sj.r += vec[0]; sj.c += vec[1];
            const fx = [];
            // ô mới bị phủ thêm (không tính ô đã phủ từ trước) → áp hiệu ứng
            for (const [r, c] of carCells(dj.len, sj)) {
              const kk = key(r, c);
              if (old.includes(kk)) continue;
              const pc = L.paintMap[kk];
              if (pc && pc !== sj.color) { sj.color = pc; fx.push({ k: 'paint', color: pc }); }
              const mi = L.mudIndex[kk];
              if (mi != null && st.muds[mi]) { st.muds[mi] = false; sj.mud = Math.max(sj.mud, 1); fx.push({ k: 'mud', idx: mi }); }
              if (L.spraySet.has(kk) && sj.mud > 0) { sj.mud = 0; fx.push({ k: 'spray' }); }
            }
            ev.cars.push({ j, r: sj.r, c: sj.c, fx });
          }
          events.push(ev);
          // bùn văng: xe tải còn bùn húc vào xe sạch
          if (L.splash && s.mud > 0 && st.cars[o].mud === 0) { st.cars[o].mud = 1; events.push({ t: 'splash', j: o, sign: dir }); }
        } else {
          // đâm vào xe khác (phải đang lao tới, không tính đứng sát sẵn):
          // nếu mình còn bùn mà xe kia sạch → bùn văng
          if (L.splash && steps > 0 && s.mud > 0 && st.cars[o].mud === 0) {
            st.cars[o].mud = 1;
            events.push({ t: 'splash', j: o, sign: dir });
          }
          events.push({ t: 'stop', why: 'car', j: o, sign: dir });
          break;
        }
      }
      if (o === -2) {
        // trụ xoay: nếu mình đang đứng trước một mặt cổng của trụ thì chui vào, không thì trụ là tường
        const pt = L.pylonSet.has(key(nr, nc)) ? portalTry() : 'none';
        if (pt === 'ok') continue;
        events.push({ t: 'stop', why: 'wall', sign: dir });
        break;
      }

      // tiến một ô, rồi áp hiệu ứng của ô vừa lăn vào
      if (s.axis === 'h') s.c += dir; else s.r += dir;
      steps++;
      events.push({ t: 'step', r: s.r, c: s.c, sign: dir });
      const k = key(nr, nc);
      const p = L.paintMap[k];
      if (p && p !== s.color) { s.color = p; events.push({ t: 'paint', color: p }); }
      const mi = L.mudIndex[k];
      if (mi != null && st.muds[mi]) { st.muds[mi] = false; s.mud = Math.max(s.mud, 1); events.push({ t: 'mud', idx: mi }); }
      if (L.spraySet.has(k) && s.mud > 0) { s.mud = 0; events.push({ t: 'spray' }); }
      if (pushedNow) { events.push({ t: 'stop', why: 'push', sign: dir }); break; }
    }

    const moved = steps > 0 || bounced || exited;
    if (!moved) return { state: st0, events, moved: false, exited: false };
    st.moves++;
    return { state: st, events, moved: true, exited };
  }

  /** Bàn xoay chứa trọn xe i (nếu có) */
  function tableUnder(L, s, len) {
    if (len !== 2) return null;
    const cells = carCells(len, s);
    return L.tables.find((t) => cells.every(([r, c]) => r >= t.r && r <= t.r + 1 && c >= t.c && c <= t.c + 1)) || null;
  }

  /** Xoay xe i trên bàn xoay 90° theo chiều kim đồng hồ */
  function rotate(L, st0, i) {
    const s0 = st0.cars[i];
    if (!s0.alive) return null;
    const t = tableUnder(L, s0, L.cars[i].len);
    if (!t) return null;
    // toạ độ tương đối trong bàn 2×2: (dr,dc) → (dc, 1−dr)
    const cells = carCells(2, s0).map(([r, c]) => [c - t.c, 1 - (r - t.r)]).map(([dr, dc]) => [t.r + dr, t.c + dc]);
    const occ = occupancy(L, st0, i);
    if (cells.some(([r, c]) => occ[r * L.cols + c] !== -1)) return { state: st0, moved: false, blocked: true, table: t };
    const st = cloneState(st0);
    const rs = Math.min(cells[0][0], cells[1][0]), cs = Math.min(cells[0][1], cells[1][1]);
    st.cars[i] = { ...s0, r: rs, c: cs, axis: s0.axis === 'h' ? 'v' : 'h' };
    st.moves++;
    return { state: st, moved: true, table: t, events: [{ t: 'rotate' }] };
  }

  /** Xoay trụ k thêm 90° (luôn cùng một chiều, 4 lần là về chỗ cũ). Tính 1 lượt. Trụ mỏng nên xe đứng cạnh không cản. */
  function rotatePylon(L, st0, k) {
    if (!L.pylons[k]) return null;
    const st = cloneState(st0);
    st.pyl[k] = (st.pyl[k] + 1) & 3;
    st.moves++;
    return { state: st, moved: true, events: [{ t: 'pyl', k }] };
  }

  /** Các barie vừa mở khi số xe đã rửa tăng từ a lên b */
  const barriersOpened = (L, a, b) => L.barriers.filter((x) => a < x.need && b >= x.need);

  const isWin = (st) => st.cars.every((c) => !c.alive);

  function stateKey(L, st) {
    let k = st.cars.map((c) => (c.alive ? `${c.r}.${c.c}${c.axis}${c.color[0]}${c.color.length}${c.mud}` : 'x')).join('|');
    if (st.muds.length) k += '#' + st.muds.map((m) => (m ? 1 : 0)).join('');
    if (st.pyl.length) k += '^' + st.pyl.join('');
    if (L.lights.length) k += '@' + (st.moves % 2);
    return k;
  }

  /** Mọi nước đi hợp lệ từ một trạng thái: [loại, xe, chiều] */
  function* nextMoves(L, st) {
    for (let i = 0; i < L.cars.length; i++) {
      if (!st.cars[i].alive) continue;
      for (const sg of [1, -1]) {
        const res = move(L, st, i, sg);
        if (res && res.moved) yield { m: ['move', i, sg], st: res.state };
      }
      if (L.tables.length) {
        const res = rotate(L, st, i);
        if (res && res.moved) yield { m: ['rot', i, 0], st: res.state };
      }
    }
    for (let k = 0; k < L.pylons.length; k++) if (!L.pylons[k].fixed) yield { m: ['pyl', k, 0], st: rotatePylon(L, st, k).state };
  }

  /**
   * BFS tìm lời giải ít lượt nhất từ trạng thái st0.
   * Trả về { moves } nếu giải được, { moves: null, exhausted: true } nếu chắc chắn bế tắc,
   * { moves: null, exhausted: false } nếu vượt giới hạn trạng thái (không kết luận).
   */
  function solve(L, st0, maxStates = 60000) {
    const k0 = stateKey(L, st0);
    const parent = new Map([[k0, null]]);
    let frontier = [st0], explored = 0;
    while (frontier.length) {
      const next = [];
      for (const st of frontier) {
        if (isWin(st)) {
          const moves = [];
          let k = stateKey(L, st);
          while (parent.get(k)) { const p = parent.get(k); moves.push(p.m); k = p.k; }
          return { moves: moves.reverse(), explored, exhausted: true };
        }
        const k = stateKey(L, st);
        for (const n of nextMoves(L, st)) {
          const k2 = stateKey(L, n.st);
          if (parent.has(k2)) continue;
          parent.set(k2, { k, m: n.m });
          next.push(n.st);
          if (++explored > maxStates) return { moves: null, explored, exhausted: false };
        }
      }
      frontier = next;
    }
    return { moves: null, explored, exhausted: true };
  }

  /**
   * "Còn đường thắng không?" — dùng cho báo Kẹt xe (không cần lời giải ngắn nhất nên khác solve).
   * Tìm ưu tiên trạng thái còn ÍT xe nhất (cùng mức thì đào sâu trước): lúc chưa kẹt thường chạm đích sau vài chục trạng thái,
   * thay vì phải duyệt cả tầng như BFS. Kết luận giống solve: win=true / (win=false, exhausted=true: chắc chắn kẹt) /
   * (win=false, exhausted=false: quá giới hạn, không kết luận).
   * Là generator: cứ vài trạng thái lại yield để bên gọi chia nhỏ thời gian chạy (không làm đứng khung hình).
   */
  function* winSearch(L, st0, maxStates = 200000) {
    const seen = new Set([stateKey(L, st0)]);
    const buckets = [];   // buckets[số xe còn lại] = ngăn xếp trạng thái
    const alive = (s) => { let n = 0; for (const c of s.cars) if (c.alive) n++; return n; };
    const push = (s) => { const a = alive(s); (buckets[a] || (buckets[a] = [])).push(s); };
    push(st0);
    let explored = 0, expanded = 0;
    for (;;) {
      let b = 0;
      while (b < buckets.length && !(buckets[b] && buckets[b].length)) b++;
      if (b >= buckets.length) return { win: false, exhausted: true, explored };
      const st = buckets[b].pop();
      if (isWin(st)) return { win: true, exhausted: true, explored };
      for (const n of nextMoves(L, st)) {
        const k = stateKey(L, n.st);
        if (seen.has(k)) continue;
        seen.add(k); push(n.st);
        if (++explored > maxStates) return { win: false, exhausted: false, explored };
      }
      if (++expanded % 8 === 0) yield;
    }
  }

  /**
   * Tìm MỘT đường thắng (không nhất thiết ngắn nhất) cho nút Gợi ý khi bộ giải tối ưu quá lớn. Cùng kiểu ưu tiên với winSearch
   * (ngăn xếp theo số xe còn lại) nhưng nhớ nút cha để dựng lại chuỗi nước. Là generator: cứ ~8 lần mở rộng thì nhường (yield).
   * Trả về { moves: [...] | null, exhausted, explored }.
   */
  function* pathSearch(L, st0, maxStates = 300000) {
    const k0 = stateKey(L, st0), parent = new Map([[k0, null]]), buckets = [];
    const alive = (s) => { let n = 0; for (const c of s.cars) if (c.alive) n++; return n; };
    const push = (s, k) => { const a = alive(s); (buckets[a] || (buckets[a] = [])).push([s, k]); };
    push(st0, k0);
    let explored = 0, expanded = 0;
    for (;;) {
      let b = 0;
      while (b < buckets.length && !(buckets[b] && buckets[b].length)) b++;
      if (b >= buckets.length) return { moves: null, exhausted: true, explored };
      const [st, k] = buckets[b].pop();
      if (isWin(st)) {
        const moves = [];
        for (let cur = k; parent.get(cur); cur = parent.get(cur).k) moves.push(parent.get(cur).m);
        return { moves: moves.reverse(), exhausted: true, explored };
      }
      for (const n of nextMoves(L, st)) {
        const kk = stateKey(L, n.st);
        if (parent.has(kk)) continue;
        parent.set(kk, { k, m: n.m }); push(n.st, kk);
        if (++explored > maxStates) return { moves: null, exhausted: false, explored };
      }
      if (++expanded % 8 === 0) yield;
    }
  }

  return { compile, initState, carCells, occupancy, move, rotate, rotatePylon, nextMoves, faceList, pylonFaces, SIDE_VEC, OPP, tableUnder, barriersOpened, switchPressed, isWin, solve, winSearch, pathSearch, stateKey, washedCount, lightRed, key };
})();
if (typeof module !== 'undefined') module.exports = CW;
