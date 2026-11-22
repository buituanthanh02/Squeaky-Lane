'use strict';

const path = require('node:path');
const assert = require('node:assert/strict');
const { CW, sourceLevels, goldenDirectory, fileName, writeJson, levelToJson } = require('./export.js');

const BASE_SEED = 0x20261122;
const SEQUENCES = 8;
const MAX_STEPS = 25;
const copyJson = value => JSON.parse(JSON.stringify(value));

function sequenceSeed(id, sequence) {
  return ((BASE_SEED ^ Math.imul(id, 0x9e3779b9) ^ Math.imul(sequence + 1, 0x85ebca6b)) >>> 0) || 1;
}

function makeRandom(seed) {
  let state = seed >>> 0;
  // xorshift32: chỉ phép toán số nguyên 32 bit, không phụ thuộc Math.random/giờ máy.
  return () => {
    state ^= state << 13; state ^= state >>> 17; state ^= state << 5;
    return (state >>> 0) / 0x100000000;
  };
}

function applyCommand(level, state, command) {
  switch (command.kind) {
    case 'move': return CW.move(level, state, command.car, command.sign);
    case 'rot': return CW.rotate(level, state, command.car);
    case 'pyl': return CW.rotatePylon(level, state, command.car);
    default: throw new Error(`Loại nước đi lạ: ${command.kind}`);
  }
}

function snapshotEvents(events = []) {
  return events.map(event => {
    const copy = copyJson(event);
    if (Object.hasOwn(copy, 'gate') && copy.gate !== null) {
      const { side, i, color } = copy.gate;
      copy.gate = { side, i, color };
    }
    // Giữ nguyên trường k của push.cars[].fx, không đổi thành t và không bỏ sự kiện con.
    return copy;
  });
}

function recordStep(level, state, command, id) {
  const before = copyJson(state), result = applyCommand(level, state, command);
  assert.ok(result && result.moved, `Màn ${id}: lệnh không hợp lệ`);
  assert.deepEqual(state, before, `Màn ${id}: prototype sửa state đầu vào`);
  return { nextState: result.state, record: { ...command, moved: !!result.moved, exited: !!result.exited,
    events: snapshotEvents(result.events), state: copyJson(result.state) } };
}

function makeTrace(level, id) {
  const trace = { schemaVersion: 2, id, baseSeed: BASE_SEED, maxSteps: MAX_STEPS,
    initialState: copyJson(CW.initState(level)), sequences: [] };
  for (let sequence = 0; sequence < SEQUENCES; sequence++) {
    const seed = sequenceSeed(id, sequence), random = makeRandom(seed);
    const moves = [];
    let state = CW.initState(level);
    for (let step = 0; step < MAX_STEPS && !CW.isWin(state); step++) {
      const choices = Array.from(CW.nextMoves(level, state));
      if (choices.length === 0) break;
      const selected = choices[Math.floor(random() * choices.length)];
      const command = { kind: selected.m[0], car: selected.m[1], sign: selected.m[2] };
      const stepResult = recordStep(level, state, command, id);
      assert.deepEqual(stepResult.nextState, selected.st, `Màn ${id}: phát lại lệnh khác nextMoves`);
      moves.push(stepResult.record);
      state = stepResult.nextState;
    }
    trace.sequences.push({ kind: 'random', seed, moves });
  }
  const minimum = levelToJson(sourceLevels[id - 1], id).minMoves;
  if (minimum >= 0) {
    // Chuỗi lời giải bổ sung theo review 005: chạm cơ chế mà random có thể bỏ qua.
    const solution = CW.solve(level, CW.initState(level), 300000);
    assert.ok(solution.moves && solution.exhausted, `Màn ${id}: chưa tìm được lời giải chuẩn`);
    assert.equal(solution.moves.length, minimum, `Màn ${id}: độ dài lời giải chuẩn`);
    let state = CW.initState(level);
    const moves = [];
    for (const [kind, car, sign] of solution.moves) {
      const stepResult = recordStep(level, state, { kind, car, sign }, id);
      moves.push(stepResult.record);
      state = stepResult.nextState;
    }
    assert.ok(CW.isWin(state), `Màn ${id}: chuỗi lời giải không thắng`);
    trace.sequences.push({ kind: 'solution', seed: 0, moves });
  }
  return trace;
}

function makeTraces() {
  assert.equal(sourceLevels.length, 31);
  let steps = 0, sequences = 0;
  for (let i = 0; i < sourceLevels.length; i++) {
    const id = i + 1, trace = makeTrace(CW.compile(sourceLevels[i]), id);
    // Trace compact để không vượt ngân sách dữ liệu; vẫn giữ đủ mọi trường state/event.
    writeJson(path.join(goldenDirectory, 'traces', fileName('trace', id)), trace, false);
    steps += trace.sequences.reduce((sum, sequence) => sum + sequence.moves.length, 0);
    sequences += trace.sequences.length;
  }
  console.log(`Đã xuất 31/31 trace, ${sequences} chuỗi, ${steps} nước (random tối đa ${MAX_STEPS}/chuỗi; thêm 28 solution).`);
}

if (require.main === module) makeTraces();
module.exports = { BASE_SEED, SEQUENCES, MAX_STEPS, copyJson, makeRandom, sequenceSeed,
  applyCommand, snapshotEvents, makeTrace, makeTraces };
