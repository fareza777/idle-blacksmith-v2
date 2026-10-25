import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const mode = process.argv[2];
const serial = process.argv[3];
if (!['progression', 'regression', 'illustrated','chronicles'].includes(mode) || !/^(127\.0\.0\.1:\d+|emulator-\d+)$/.test(serial || '')) {
  throw new Error('Usage: node tools/test-native-android.mjs progression|regression|illustrated|chronicles explicit-emulator-serial');
}
const adb = path.join(process.env.LOCALAPPDATA, 'Android', 'Sdk', 'platform-tools', 'adb.exe');
const output = path.join(root, 'QA',mode==='chronicles'?'chronicles-20261025':'illustrated-20261025', 'android', mode);
fs.mkdirSync(output, { recursive: true });
function call(args, required = true, timeout = 30000) {
  const result = spawnSync(adb, args, { encoding: 'utf8', windowsHide: true, timeout });
  if (required && (result.error || result.status !== 0)) {
    throw new Error(`adb ${args.join(' ')}: ${result.error?.message || result.stderr || result.stdout}`);
  }
  return (result.stdout || '').trim();
}
const device = (...args) => call(['-s', serial, ...args]);
if (serial.startsWith('127.')) call(['connect', serial]);
if (device('get-state') !== 'device') throw new Error('The requested emulator is not ready');
console.log(device('install', '-r', path.join(root, 'Builds', 'Android', 'ForgeEmpire.apk')));
const folder = mode==='chronicles'?'chronicles-qa':mode === 'illustrated' ? 'illustrated-qa' : mode === 'progression' ? 'progression-qa' : 'redesign-qa';
const filename = mode==='chronicles'?'chronicles-result.json':mode === 'illustrated' ? 'illustrated-result.json' : mode === 'progression' ? 'progression-result.json' : 'native-result.json';
const remote = `/sdcard/Android/data/com.emberoak.forgeempire/files/${folder}`;
device('shell', 'rm', '-f', `${remote}/${filename}`);
device('shell', 'am', 'start', '-S', '-n', 'com.emberoak.forgeempire/com.unity3d.player.UnityPlayerGameActivity',
  '--es', 'unity',mode==='chronicles'?'--chronicles-smoke': mode === 'illustrated' ? '--illustrated-smoke' : mode === 'progression' ? '--progression-smoke' : '--redesign-smoke');
console.log(`RUNNING ${mode} on ${serial}`);
const deadline = Date.now() + 240000;
let result;
while (Date.now() < deadline) {
  const json = call(['-s', serial, 'shell', 'cat', `${remote}/${filename}`], false, 10000);
  if (json.startsWith('{')) { result = JSON.parse(json); break; }
  await new Promise(resolve => setTimeout(resolve, 2000));
}
call(['-s', serial, 'pull', `${remote}/.`, output], false, 60000);
fs.writeFileSync(path.join(output, 'runtime.log'), call(['-s', serial, 'logcat', '-d', '-s', 'Unity'], false));
if (!result) throw new Error(`Timed out waiting for ${filename}; logs saved in ${output}`);
console.log(JSON.stringify(result));
if (!result.passed) process.exitCode = 1;
