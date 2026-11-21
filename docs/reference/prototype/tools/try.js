// Kiểm tra màn thiết kế tay: node tools/try.js <file.json> [chỉ số]
// In: số xe ra thẳng được lúc đầu, lời giải, tỉ lệ bẫy từng bước, bot ngẫu nhiên.
const CW = require('../src/logic.js');
const { findPath, botStats, winnable, legal } = require('./big.js');
const { makeRng } = require('./gen.js');
const { freeExits } = require('./dense.js');
const lvls = JSON.parse(require('fs').readFileSync(process.argv[2], 'utf8'));
const only = process.argv[3] != null ? [+process.argv[3]] : lvls.map((_, i) => i);
for (const idx of only) {
  const lv = lvls[idx], L = CW.compile(lv), s0 = CW.initState(L), t0 = Date.now();
  const path = findPath(L, s0, 400000);
  if (!path) { console.log(`#${idx} ${lv.name}: KHÔNG GIẢI ĐƯỢC (hoặc quá lớn) ${Date.now() - t0}ms`); continue; }
  let st = s0, traps = [];
  for (const a of path) {
    const ms = legal(L, st); let bad = 0;
    for (const m of ms) if (winnable(L, m.r.state, 2500) === false) bad++;
    traps.push(`${bad}/${ms.length}`);
    const r = a[0] === 'rot' ? CW.rotate(L, st, a[1]) : CW.move(L, st, a[1], a[2]); st = r.state;
  }
  const bot = botStats(L, 40, makeRng(1));
  console.log(`#${idx} ${lv.name}: ${lv.cars.length} xe | ra thẳng lúc đầu ${freeExits(L, s0)} | lời giải ${path.length} nước (thừa ${path.length - lv.cars.length}) | bẫy/bước ${traps.join(' ')} | bot kẹt ${(bot.stuck * 100).toFixed(0)}% thắng ${(bot.wins * 100).toFixed(0)}% | ${Date.now() - t0}ms`);
  console.log('   ' + JSON.stringify(path));
}
