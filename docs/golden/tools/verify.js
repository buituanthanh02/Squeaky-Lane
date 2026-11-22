'use strict';

const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { CW, sourceLevels, goldenDirectory, fileName, readJson,
  levelToJson, levelToPrototype, compiledRules } = require('./export.js');
const { copyJson, makeTrace, applyCommand, snapshotEvents } = require('./make-traces.js');

const MAX_STATES = 300000;

function verifyLevelData(data, id) {
  assert.ok(Number.isInteger(id) && id >= 1 && id <= sourceLevels.length, 'Id màn ngoài bộ nguồn');
  assert.deepEqual(data, levelToJson(sourceLevels[id - 1], id), `Màn ${id}: JSON object khác nguồn/metadata`);
  const original = CW.compile(sourceLevels[id - 1]), level = CW.compile(levelToPrototype(data));
  assert.deepEqual(compiledRules(level), compiledRules(original), `Màn ${id}: mất dữ liệu luật khi round-trip`);
  return { level, original };
}

function verifyTrace(level, originalLevel, trace, id) {
  // Tạo kỳ vọng từ nguồn gốc, rồi phát lại độc lập trên bản đã round-trip qua JSON object.
  const expected = makeTrace(originalLevel, id);
  for (const field of ['schemaVersion', 'id', 'baseSeed', 'maxSteps'])
    assert.equal(trace[field], expected[field], `Màn ${id}: ${field}`);
  assert.deepEqual(trace.initialState, expected.initialState, `Màn ${id}: initialState từ nguồn`);
  assert.deepEqual(CW.initState(level), trace.initialState, `Màn ${id}: initialState sau round-trip`);
  assert.equal(trace.sequences.length, expected.sequences.length, `Màn ${id}: số chuỗi`);
  let steps = 0;
  for (let sequence = 0; sequence < expected.sequences.length; sequence++) {
    const actualSequence = trace.sequences[sequence], expectedSequence = expected.sequences[sequence];
    const prefix = `Màn ${id} · chuỗi ${sequence + 1}`;
    assert.equal(actualSequence.kind, expectedSequence.kind, `${prefix}: loại chuỗi`);
    assert.equal(actualSequence.seed, expectedSequence.seed, `${prefix}: seed`);
    assert.equal(actualSequence.moves.length, expectedSequence.moves.length, `${prefix}: số nước`);
    let state = CW.initState(level);
    for (let step = 0; step < actualSequence.moves.length; step++) {
      const actual = actualSequence.moves[step], reference = expectedSequence.moves[step];
      const context = `${prefix} · nước ${step + 1}`;
      for (const field of ['kind', 'car', 'sign']) assert.equal(actual[field], reference[field], `${context}: ${field}`);
      const before = copyJson(state), result = applyCommand(level, state, actual);
      assert.ok(result, `${context}: kết quả null`);
      assert.deepEqual(state, before, `${context}: state đầu vào bị sửa`);
      for (const field of ['moved', 'exited']) {
        assert.equal(actual[field], reference[field], `${context}: nguồn ${field}`);
        assert.equal(!!result[field], actual[field], `${context}: phát lại ${field}`);
      }
      assert.deepEqual(actual.events, reference.events, `${context}: events nguồn`);
      assert.deepEqual(snapshotEvents(result.events), actual.events, `${context}: events phát lại`);
      assert.deepEqual(actual.state, reference.state, `${context}: state nguồn`);
      assert.deepEqual(result.state, actual.state, `${context}: state phát lại`);
      state = result.state;
      steps++;
    }
  }
  return steps;
}

function checkFileNames(directory, prefix) {
  const actual = fs.readdirSync(directory).sort();
  const expected = sourceLevels.map((_, i) => fileName(prefix, i + 1)).sort();
  assert.deepEqual(actual, expected, `${directory}: phải đủ đúng 31 file`);
}

function directoryBytes(directory) {
  let bytes = 0;
  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const file = path.join(directory, entry.name);
    bytes += entry.isDirectory() ? directoryBytes(file) : fs.statSync(file).size;
  }
  return bytes;
}

function verify() {
  assert.equal(sourceLevels.length, 31);
  checkFileNames(path.join(goldenDirectory, 'levels'), 'level');
  checkFileNames(path.join(goldenDirectory, 'traces'), 'trace');
  let known = 0, unknown = 0, steps = 0, sequences = 0;
  for (let i = 0; i < sourceLevels.length; i++) {
    const id = i + 1, data = readJson(path.join(goldenDirectory, 'levels', fileName('level', id)));
    const { level, original } = verifyLevelData(data, id);
    let minimum;
    if (data.minMoves === -1) {
      assert.equal(data.solveExhausted, false, `Màn ${id}: trạng thái chưa kết luận`);
      unknown++;
      minimum = 'minMoves=-1 (chưa kết luận, chỉ kiểm nạp/trace)';
    } else {
      const solution = CW.solve(level, CW.initState(level), MAX_STATES);
      assert.ok(solution.moves && solution.exhausted, `Màn ${id}: solve chưa có kết luận trong ${MAX_STATES} trạng thái`);
      assert.equal(solution.moves.length, data.minMoves, `Màn ${id}: minMoves`);
      assert.equal(solution.exhausted, data.solveExhausted, `Màn ${id}: solveExhausted`);
      let state = CW.initState(level);
      for (const [kind, car, sign] of solution.moves) {
        const result = applyCommand(level, state, { kind, car, sign });
        assert.ok(result && result.moved, `Màn ${id}: lời giải chứa lệnh không hợp lệ`);
        state = result.state;
      }
      assert.ok(CW.isWin(state), `Màn ${id}: phát lại lời giải không thắng`);
      known++;
      minimum = `minMoves=${data.minMoves} khớp (${solution.explored} trạng thái)`;
    }
    const trace = readJson(path.join(goldenDirectory, 'traces', fileName('trace', id)));
    const count = verifyTrace(level, original, trace, id);
    steps += count;
    sequences += trace.sequences.length;
    console.log(`QUA ${String(id).padStart(2, '0')}/31: ${minimum}; ${trace.sequences.length} chuỗi/${count} nước khớp.`);
  }
  const bytes = directoryBytes(goldenDirectory);
  assert.ok(bytes <= 3600000, `Dữ liệu vượt 3,6 MB: ${bytes} byte`);
  console.log(`QUA 31/31: ${known} minMoves đã kiểm; ${unknown} chưa kết luận; ${sequences} chuỗi/${steps} nước khớp; ${bytes} byte.`);
  return { known, unknown, sequences, steps, bytes };
}

if (require.main === module) {
  try { verify(); } catch (error) { console.error(error.stack); process.exitCode = 1; }
}
module.exports = { MAX_STATES, verifyLevelData, verifyTrace, verify, directoryBytes };
