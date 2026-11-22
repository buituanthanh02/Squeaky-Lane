'use strict';

const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');

const goldenDirectory = path.resolve(__dirname, '..');
const referenceDirectory = path.resolve(goldenDirectory, '../reference/prototype');
const CW = require(path.join(referenceDirectory, 'src/logic.js'));
const sourceLevels = require(path.join(referenceDirectory, 'src/levels.js'));

const cellObject = ([r, c]) => ({ r, c });
const faceObject = ([r, c, side]) => ({ r, c, side });
const cellTuple = ({ r, c }) => [r, c];
const faceTuple = ({ r, c, side }) => [r, c, side];
const fileName = (prefix, id) => `${prefix}_${String(id).padStart(2, '0')}.json`;
const readJson = file => JSON.parse(fs.readFileSync(file, 'utf8'));

function writeJson(file, data, pretty = true) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  // LF và không ghi giờ máy: cùng nguồn phải sinh cùng byte trên mọi máy.
  fs.writeFileSync(file, JSON.stringify(data, null, pretty ? 2 : undefined) + '\n', 'utf8');
}

function levelToJson(level, id) {
  const metadata = readJson(path.join(referenceDirectory, 'levels-export', fileName('level', id)));
  assert.equal(metadata.id, id, `Metadata sai id ${id}`);
  assert.equal(metadata.name, level.name, `Metadata sai tên màn ${id}`);
  assert.ok(metadata.minMoves === null || (Number.isInteger(metadata.minMoves) && metadata.minMoves >= 0));
  assert.equal(typeof metadata.solveExhausted, 'boolean');
  return {
    id, name: level.name, cols: level.cols, rows: level.rows,
    gates: (level.gates ?? []).map(({ side, i, color }) => ({ side, i, color })),
    cars: level.cars.map(c => ({ axis: c.axis, r: c.r, c: c.c, color: c.color,
      len: c.len ?? 2, truck: !!c.truck, mud: c.mud ?? 0 })),
    cones: (level.cones ?? []).map(cellObject),
    muds: (level.muds ?? []).map(cellObject),
    sprays: (level.sprays ?? []).map(cellObject),
    tables: (level.tables ?? []).map(cellObject),
    paints: (level.paints ?? []).map(({ r, c, color }) => ({ r, c, color })),
    lights: (level.lights ?? []).map(l => ({ r: l.r, c: l.c, phase: l.phase ?? 0 })),
    barriers: (level.barriers ?? []).map(({ r, c, need }) => ({ r, c, need })),
    splash: !!level.splash,
    portals: (level.portals ?? []).map(([a, b]) => ({ a: faceObject(a), b: faceObject(b) })),
    pylons: (level.pylons ?? []).map(p => ({ r: p.r, c: p.c, o: p.o ?? 'h',
      fixed: !!p.fixed, to: p.to.map(faceObject) })),
    switches: (level.switches ?? []).map(s => ({ r: s.r, c: s.c,
      a: (s.a ?? []).map(cellObject), b: (s.b ?? []).map(cellObject) })),
    minMoves: metadata.minMoves === null ? -1 : metadata.minMoves,
    solveExhausted: metadata.solveExhausted,
  };
}

function levelToPrototype(data) {
  // Chiều chuyển ngược chỉ dùng để kiểm dữ liệu, không thay nguồn prototype.
  return {
    name: data.name, cols: data.cols, rows: data.rows,
    gates: data.gates.map(({ side, i, color }) => ({ side, i, color })),
    cars: data.cars.map(({ axis, r, c, color, len, truck, mud }) => ({ axis, r, c, color, len, truck, mud })),
    cones: data.cones.map(cellTuple), muds: data.muds.map(cellTuple),
    sprays: data.sprays.map(cellTuple), tables: data.tables.map(cellTuple),
    paints: data.paints.map(({ r, c, color }) => ({ r, c, color })),
    lights: data.lights.map(({ r, c, phase }) => ({ r, c, phase })),
    barriers: data.barriers.map(({ r, c, need }) => ({ r, c, need })),
    splash: data.splash,
    portals: data.portals.map(p => [faceTuple(p.a), faceTuple(p.b)]),
    pylons: data.pylons.map(p => ({ r: p.r, c: p.c, o: p.o, fixed: p.fixed, to: p.to.map(faceTuple) })),
    switches: data.switches.map(s => ({ r: s.r, c: s.c, a: s.a.map(cellTuple), b: s.b.map(cellTuple) })),
  };
}

function compiledRules(level) {
  // So toàn bộ dữ liệu luật sau compile; Set phải đổi ra mảng, JSON.stringify(Set) sẽ mất nội dung.
  return {
    cols: level.cols, rows: level.rows, splash: level.splash,
    cars: level.cars.map(c => ({ id: c.id, axis: c.axis, r: c.r, c: c.c, color: c.color,
      len: c.len, truck: !!c.truck, mud: c.mud })),
    gateMap: level.gateMap, cones: Array.from(level.coneSet),
    muds: level.mudList, paints: level.paintMap, sprays: Array.from(level.spraySet),
    lights: level.lights, tables: level.tables, barriers: level.barriers,
    switches: level.switches, portals: level.portals, pylons: level.pylons,
  };
}

function exportLevels() {
  assert.equal(sourceLevels.length, 31, 'Bộ nguồn phải có đúng 31 màn');
  for (let i = 0; i < sourceLevels.length; i++) {
    const id = i + 1;
    writeJson(path.join(goldenDirectory, 'levels', fileName('level', id)), levelToJson(sourceLevels[i], id));
  }
  console.log('Đã xuất 31/31 level JSON object (minMoves chưa biết: 17, 18, 19 = -1).');
}

if (require.main === module) exportLevels();
module.exports = { CW, sourceLevels, goldenDirectory, fileName, readJson, writeJson,
  levelToJson, levelToPrototype, compiledRules, exportLevels };
