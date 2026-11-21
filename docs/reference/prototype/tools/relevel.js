// Sắp lại danh sách màn: thêm màn "Bể rửa", đổi tên/gợi ý theo cổng mới,
// gắn kịch bản tutorial nền tối. Chạy: node tools/relevel.js  → ghi đè src/levels.js
const fs = require('fs');
const OLD = require('../src/levels.js');
const by = (name) => JSON.parse(JSON.stringify(OLD.find((l) => l.name === name)));

const lv = [];
const add = (l) => { delete l.hand; lv.push(l); };

// 1. Vuốt cho xe chạy
add(Object.assign(by('Vuốt cho xe chạy'), {
  hint: 'Vuốt dọc thân xe để chạy. Xe ra ở <b>đường hầm cùng màu</b>.',
  tutorial: [
    { say: 'Mỗi xe phải ra đúng đường hầm cùng màu với nó.', focus: [{ car: 1 }, { gate: 't:1' }], tap: true },
    { say: 'Xe xanh đang chắn đường. Vuốt xe xanh sang phải.', focus: [{ car: 0 }, { gate: 'r:1' }], do: ['move', 0, 1] },
    { say: 'Giờ vuốt xe hồng lên.', focus: [{ car: 1 }, { gate: 't:1' }], do: ['move', 1, -1] },
  ],
}));

// 2. Bể rửa (mới) — luật "trong bãi = chỗ rửa"
add({
  name: 'Bể rửa', tag: 'Mới', cols: 4, rows: 6,
  hint: 'Xe còn bùn phải lăn qua <b>bể rửa</b> trước khi vào đường hầm.',
  gates: [{ side: 't', i: 1, color: 'pink' }, { side: 'b', i: 3, color: 'blue' }, { side: 'r', i: 2, color: 'green' }],
  sprays: [[1, 1], [4, 3]],
  cars: [
    { axis: 'v', r: 3, c: 1, color: 'pink', mud: 1 },
    { axis: 'v', r: 0, c: 3, color: 'blue', mud: 1 },
    { axis: 'h', r: 2, c: 2, color: 'green' },
  ],
  tutorial: [
    { say: 'Xe này còn bùn. Đường hầm không nhận xe bẩn.', focus: [{ car: 0 }], tap: true },
    { say: 'Đây là bể rửa. Xe lăn qua là sạch bùn.', focus: [{ cell: [1, 1] }], tap: true },
    { say: 'Vuốt xe hồng lên: qua bể rửa rồi ra đường hầm hồng.', focus: [{ car: 0 }, { cell: [1, 1] }, { gate: 't:1' }], do: ['move', 0, -1] },
  ],
});

// 3. Vòi phun (trước đây là "Cổng nước")
add(Object.assign(by('Cổng nước'), {
  name: 'Vòi phun', tag: 'Mới',
  hint: '<b>Vòi phun</b> ở mép bãi xịt sạch bùn rồi <b>đẩy xe lại</b> — không phải lối ra.',
  tutorial: [
    { say: 'Đây là vòi phun: xịt sạch bùn rồi đẩy xe ngược lại. Sau lưng nó là tường, không có đường ra.', focus: [{ gate: 't:1' }], tap: true },
    { say: 'Dọn đường trước: vuốt xe vàng sang trái.', focus: [{ car: 3 }, { gate: 'l:4' }], do: ['move', 3, -1] },
    { say: 'Vuốt xe hồng lên vòi phun. Sạch bùn, bị đẩy xuống và ra luôn đường hầm hồng.', focus: [{ car: 0 }, { gate: 't:1' }, { gate: 'b:1' }], do: ['move', 0, -1] },
  ],
}));

// 4. Bùn văng
add(Object.assign(by('Bùn văng'), {
  hint: 'Xe bùn đâm vào xe sạch là <b>văng bùn</b> sang xe kia.',
  tutorial: [
    { say: 'Xe còn bùn lao vào xe sạch thì bùn văng sang xe kia!', focus: [{ car: 4 }, { car: 3 }], tap: true },
    { say: 'Làn của xe vàng không có vòi phun hay bể rửa: dính bùn là kẹt. Tính thứ tự cho khéo.', focus: [{ car: 3 }, { gate: 'r:4' }], tap: true },
  ],
}));

// 5. Vũng bùn
add(Object.assign(by('Vũng bùn'), {
  hint: 'Xe đầu tiên qua <b>vũng bùn</b> sẽ dính hết bùn, rồi vũng biến mất.',
  tutorial: [
    { say: 'Vũng bùn: xe đầu tiên lăn qua sẽ dính hết bùn, rồi vũng biến mất.', focus: [{ cell: [2, 2] }], tap: true },
    { say: 'Cho xe xanh lá hứng bùn: làn của nó có vòi phun để rửa lại.', focus: [{ car: 0 }, { cell: [2, 2] }, { gate: 't:2' }], do: ['move', 0, -1] },
  ],
}));

// 6. Vũng sơn
add(Object.assign(by('Vũng sơn'), {
  hint: 'Lăn qua <b>vũng sơn</b> thì xe đổi màu. Có xe phải qua vũng rồi quay lại.',
  tutorial: [
    { say: 'Lăn qua vũng sơn thì xe đổi sang màu của vũng.', focus: [{ car: 0 }, { cell: [2, 0] }], tap: true },
    { say: 'Vuốt xe hồng sang trái để nhúng sơn xanh.', focus: [{ car: 0 }, { cell: [2, 0] }], do: ['move', 0, -1] },
    { say: 'Xe đã thành màu xanh. Vuốt sang phải ra đường hầm xanh.', focus: [{ car: 0 }, { gate: 'r:2' }], do: ['move', 0, 1] },
  ],
}));

// 7. Bùn dày
add(Object.assign(by('Bùn dày & vòi xịt'), {
  name: 'Bùn dày',
  hint: '<b>Bùn dày</b> che mất màu xe. Lăn qua <b>bể rửa</b> để lộ màu.',
  tutorial: [
    { say: 'Bùn dày che mất màu xe, chưa biết xe màu gì.', focus: [{ car: 0 }], tap: true },
    { say: 'Vuốt lên qua bể rửa để lộ màu.', focus: [{ car: 0 }, { cell: [1, 2] }, { gate: 't:2' }], do: ['move', 0, -1] },
  ],
}));

// 8. Bàn xoay
add(Object.assign(by('Bàn xoay'), {
  hint: 'Xe đứng gọn trên <b>bàn xoay</b>: vuốt ngang thân xe để xoay 90°.',
  tutorial: [
    { say: 'Bàn xoay: xe đứng gọn trên bàn thì xoay được.', focus: [{ table: [2, 1] }], tap: true },
    { say: 'Vuốt ngang thân xe để xoay 90°.', focus: [{ car: 0 }], do: ['rot', 0] },
    { say: 'Giờ vuốt lên, ra đường hầm vàng.', focus: [{ car: 0 }, { gate: 't:2' }], do: ['move', 0, -1] },
  ],
}));

// 9. Barie
add(Object.assign(by('Barie'), {
  hint: '<b>Barie</b> mở khi rửa đủ số xe ghi trên biển.',
  tutorial: [{ say: 'Barie mở khi rửa đủ số xe ghi trên biển.', focus: [{ cell: [1, 2] }], tap: true }],
}));

// 10. Đèn giao thông
add(Object.assign(by('Đèn giao thông'), {
  hint: '<b>Đèn đỏ</b> chặn đường. Sau mỗi lượt đèn đổi màu.',
  tutorial: [{ say: 'Đèn đổi màu sau mỗi lượt. Đèn đỏ chặn đường.', focus: [{ cell: [1, 0] }, { cell: [3, 3] }], tap: true }],
}));

// 11. Tổng hợp
add(Object.assign(by('Tổng hợp'), { hint: 'Gộp vòi phun, bùn văng, vũng bùn và bể rửa. Mẹo: để xe <b>đã bẩn</b> hút vũng bùn.' }));

// 12–16. Màn khó
for (const n of ['Thứ tự rửa', 'Xe hy sinh', 'Xoay sau khi rửa', 'Đổi màu đúng lúc', 'Tổng lực']) {
  const l = by(n);
  l.hint = l.hint.replace('Cổng nước', 'Vòi phun').replace('cổng nước', 'vòi phun');
  add(l);
}

// ---- ghi file: mỗi màn một khối, có chú thích ----
const body = lv.map((l, i) => `  // ${i + 1}. ${l.name}\n  ${JSON.stringify(l, null, 2).replace(/\n/g, '\n  ')}`).join(',\n');
const out = `// ===== carwash2/levels.js =====
// Mỗi màn giới thiệu đúng 1 thứ (màn 1–10), rồi màn tổng hợp và 5 màn khó.
// Xe: axis 'h'/'v', (r,c) ô trên-trái, len 2 (mặc định). mud: 0 sạch · 1 bám bùn · 2 bùn dày.
// Cổng: side t/b/l/r + chỉ số hàng/cột; color là màu xe (đường hầm) hoặc 'water' (vòi phun).
// sprays = bể rửa trong bãi. tutorial: các bước hướng dẫn nền tối (xem class Tutorial trong game.js):
//   { say, focus: [{car}|{gate:'t:1'}|{cell:[r,c]}|{table:[r,c]}], tap: true }  — chạm để tiếp
//   { say, focus, do: ['move', xe, chiều] | ['rot', xe] }                         — chỉ cho làm đúng nước này
const CW_LEVELS = [
${body},
];
if (typeof module !== 'undefined') module.exports = CW_LEVELS;
`;
fs.writeFileSync(__dirname + '/../src/levels.js', out);
console.log('đã ghi', lv.length, 'màn');
