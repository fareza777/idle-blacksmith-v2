import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const data = 'C:/Program Files/Unity/Hub/Editor/6000.3.20f1/Editor/Data';
const dotnet = path.join(data, 'NetCoreRuntime', 'dotnet.exe');
const compiler = path.join(data, 'DotNetSdkRoslyn', 'csc.dll');
const shared = path.join(data, 'NetCoreRuntime', 'shared', 'Microsoft.NETCore.App');
const versions = fs.readdirSync(shared).filter(name => /^\d+\.\d+\.\d+$/.test(name));
versions.sort((a, b) => {
  const left = a.split('.').map(Number);
  const right = b.split('.').map(Number);
  for (let i = 0; i < 3; i++) if (left[i] !== right[i]) return right[i] - left[i];
  return 0;
});
if (!versions.length) throw new Error('No bundled .NET runtime found');
const version = versions[0];
const runtime = path.join(shared, version);
const qa = path.join(root, 'QA');
const label = process.argv[2] || 'latest';
if (!/^[a-zA-Z0-9-]+$/.test(label)) throw new Error('Artifact label must be alphanumeric or hyphenated');
const output = path.join(qa, `core-test-${label}.dll`);
const response = path.join(qa, `core-test-${label}.rsp`);
const logFile = path.join(qa, `core-test-${label}.log`);
const sources = [path.join(root, 'Game', 'Assets', 'ForgeEmpire', 'Core'), path.join(root, 'tests')]
  .flatMap(directory => fs.readdirSync(directory).filter(name => name.endsWith('.cs')).sort().map(name => path.join(directory, name)));
// Runtime folders also contain native DLLs, which cannot be compiler references.
const references = fs.readdirSync(runtime)
  .filter(name => /^(System\..*|netstandard|mscorlib)\.dll$/.test(name) && !name.includes('.Native.')).sort()
  .map(name => `/reference:"${path.join(runtime, name)}"`);
fs.writeFileSync(response, [
  '/nologo', '/nostdlib+', '/target:exe', '/langversion:7.3',
  '/warn:4', '/warnaserror+', '/optimize+', '/deterministic+', `/out:"${output}"`,
  ...references, ...sources.map(source => `"${source}"`),
].join('\n'));
fs.writeFileSync(path.join(qa, `core-test-${label}.runtimeconfig.json`), JSON.stringify({
  runtimeOptions: { tfm: `net${version.split('.').slice(0, 2).join('.')}`, framework: { name: 'Microsoft.NETCore.App', version } },
}, null, 2));
let log = `Runtime: ${version}\nCompiler: ${compiler}\nLanguage: C# 7.3 (warnings as errors)\n`;
process.stdout.write(log);
function execute(args) {
  const result = spawnSync(dotnet, args, {
    cwd: root, encoding: 'utf8', windowsHide: true, timeout: 120000,
    env: { ...process.env, DOTNET_CLI_TELEMETRY_OPTOUT: '1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE: '1' },
  });
  const text = (result.stdout || '') + (result.stderr || '') + (result.error ? `${result.error.message}\n` : '');
  log += text;
  process.stdout.write(text);
  return result.status === null ? 1 : result.status;
}
let status = execute([compiler, '/noconfig', `@${response}`]);
if (status === 0) status = execute([output]);
log += `Exit code: ${status}\n`;
fs.writeFileSync(logFile, log);
process.exitCode = status;
