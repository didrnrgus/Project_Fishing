// ez-tree(MIT, Daniel Greenheck)로 나무를 만들어 형상만 JSON으로 뽑는다. tools/blender/build_vegetation.py가 부른다.
//   node tools/eztree/dump.mjs <출력 폴더> 이름:프리셋[:씨앗] ...
// 브라우저 없이: 텍스처 로더가 만드는 <img>만 흉내 낸다 (텍스처는 쓰지 않는다).
import fs from 'fs';
import path from 'path';
import { fileURLToPath, pathToFileURL } from 'url';

const el = () => ({ addEventListener() {}, removeEventListener() {}, setAttribute() {}, style: {}, getContext: () => null });
globalThis.document = { createElementNS: el, createElement: el };
globalThis.self = globalThis;

const here = path.dirname(fileURLToPath(import.meta.url));
const { Tree } = await import(pathToFileURL(path.join(here, 'node_modules/@dgreenheck/ez-tree/build/ez-tree.es.js')).href);

const [, , outDir, ...jobs] = process.argv;
fs.mkdirSync(outDir, { recursive: true });
for (const job of jobs) {
  const [name, preset, seed] = job.split(':');
  const t = new Tree();
  t.loadPreset(preset);
  if (seed) t.options.seed = Number(seed);
  t.generate();
  const dump = {};
  for (const [key, mesh] of [['bark', t.branchesMesh], ['leaves', t.leavesMesh]]) {
    const g = mesh.geometry;
    const a = (n) => (g.attributes[n] ? Array.from(g.attributes[n].array) : []);
    dump[key] = { position: a('position'), normal: a('normal'), uv: a('uv'), index: g.index ? Array.from(g.index.array) : [] };
  }
  dump.options = { preset, seed: t.options.seed, bark: t.options.bark.type, leaves: t.options.leaves.type, type: t.options.type };
  fs.writeFileSync(path.join(outDir, `${name}.json`), JSON.stringify(dump));
  console.log(`[eztree] ${name} (${preset} #${t.options.seed}) 줄기 ${dump.bark.index.length / 3}, 잎 ${dump.leaves.index.length / 3}`);
}
