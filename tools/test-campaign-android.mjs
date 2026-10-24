// Run only after the approved campaign APK is installed. This file never builds or installs it.
// node tools/test-campaign-android.mjs --fresh
// node tools/test-campaign-android.mjs --fixtures-only
// Without either flag, only an already empty, manual profile is accepted.
import {execFileSync} from 'node:child_process';
import {existsSync, readFileSync, writeFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {inflateSync} from 'node:zlib';
import assert from 'node:assert/strict';

const adbPath = 'C:/Users/FAJAR/AppData/Local/Android/Sdk/platform-tools/adb.exe';
const serial = 'emulator-5562';
const pkg = 'com.emberoak.forgeempire';
const qa = 'D:/Idle Blacksmith/QA';
const deviceSave = `/sdcard/Android/data/${pkg}/files/workshop.json`;
const localApk = 'D:/Idle Blacksmith/Builds/Android/ForgeEmpire.apk';
const prefix = 'campaign-android-';
const args = new Set(process.argv.slice(2));
for (const arg of args) assert.ok(['--fresh', '--fixtures-only', '--help'].includes(arg), `Unknown option: ${arg}`);
assert.ok(!(args.has('--fresh') && args.has('--fixtures-only')), 'Choose --fresh OR --fixtures-only');
if (args.has('--help')) {
  console.log('Campaign QA on emulator-5562 / com.emberoak.forgeempire only.\n--fresh: preserve the current save, atomically install literal empty defaults, run golden + fixtures.\n--fixtures-only: restore the preserved, hash-verified golden profile, then run fixtures.\nNo flag: require an already empty manual profile. No pm clear, APK install, or AVD changes.');
  process.exit(0);
}
assert.ok(existsSync(qa), `QA output directory must already exist: ${qa}`);

const checks = [], screenshots = [], fixtures = [], notes = [], skillEvidence = new Set(), skillAttempts = new Set();
const startedAt = new Date().toISOString();
const runId = startedAt.replace(/[^0-9]/g, '');
let stage = 'preflight', fixtureUsedAfterGoldenPath = false, goldenCompleted = false;
let scale = 1, rootY = NaN, rootH = NaN, worldOffset = 0;
let apk = null, logStart = null, originalSave = null, goldenHash = null, goldenApk = null, goldenResumed = false;
const appPids = new Set();
const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
const file = (name, extension = 'json') => {
  assert.match(name, /^[a-z0-9-]+$/);
  return `${qa}/${prefix}${name}.${extension}`;
};
const hash = bytes => createHash('sha256').update(bytes).digest('hex');
const clone = value => JSON.parse(JSON.stringify(value));
const tickNow = () => Number(BigInt(Date.now()) * 10000n + 621355968000000000n);
const writeJSON = (name, value) => writeFileSync(file(name), JSON.stringify(value, null, 2));
const adb = (...values) => execFileSync(adbPath, ['-s', serial, ...values], {
  timeout: 30000, maxBuffer: 48 * 1024 * 1024, stdio: ['ignore', 'pipe', 'pipe']
});
const shell = (...values) => adb('shell', ...values);
function check(condition, label, detail = undefined) {
  assert.ok(condition, label);
  checks.push({stage, label, ...(detail === undefined ? {} : {detail})});
  console.log('PASS', stage, label);
}
function rememberPid() {
  const value = shell('pidof', pkg).toString().trim();
  for (const pid of value.split(/\s+/)) if (/^\d+$/.test(pid)) appPids.add(pid);
}
function launch() {
  shell('monkey', '-p', pkg, '-c', 'android.intent.category.LAUNCHER', '1');
}
function physicalTap(x, y) {
  assert.ok(Number.isFinite(x) && Number.isFinite(y), 'Touch needs measured coordinates');
  shell('input', 'tap', String(Math.round(x)), String(Math.round(y)));
}
function tap(x, y) {
  assert.ok(x >= 0 && x < 1080 && y >= 0 && y < rootH, `Touch outside measured root: ${x}, ${y}; height ${rootH}`);
  physicalTap(x * scale, rootY + y * scale);
}
function swipe(x, fromY, toY, duration = 650) {
  assert.ok(fromY > 0 && fromY < rootH && toY > 0 && toY < rootH, 'Swipe remains inside safe root');
  shell('input', 'swipe', String(Math.round(x * scale)), String(Math.round(rootY + fromY * scale)),
    String(Math.round(x * scale)), String(Math.round(rootY + toY * scale)), String(duration));
}

// Native screencap PNGs are RGB or RGBA; reconstruct every PNG filter, not raw scanline guesses.
function png(bytes) {
  assert.ok(bytes.subarray(0, 8).equals(Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])), 'Expected a PNG screenshot');
  const width = bytes.readUInt32BE(16), height = bytes.readUInt32BE(20), colorType = bytes[25];
  assert.equal(bytes[24], 8, 'Expected 8-bit PNG');
  assert.ok(colorType === 2 || colorType === 6, `Unsupported PNG color type ${colorType}`);
  assert.equal(bytes[28], 0, 'Expected noninterlaced PNG');
  const channels = colorType === 6 ? 4 : 3, blocks = [];
  for (let p = 8; p < bytes.length;) {
    const length = bytes.readUInt32BE(p);
    assert.ok(p + length + 12 <= bytes.length, 'Truncated PNG chunk');
    if (bytes.toString('ascii', p + 4, p + 8) === 'IDAT') blocks.push(bytes.subarray(p + 8, p + 8 + length));
    p += length + 12;
  }
  const raw = inflateSync(Buffer.concat(blocks)), stride = width * channels;
  assert.equal(raw.length, (stride + 1) * height, 'Unexpected PNG scanline size');
  const data = Buffer.alloc(stride * height);
  for (let y = 0; y < height; y++) {
    const filter = raw[y * (stride + 1)];
    assert.ok(filter <= 4, `Invalid PNG filter ${filter}`);
    for (let x = 0; x < stride; x++) {
      const a = x >= channels ? data[y * stride + x - channels] : 0;
      const b = y ? data[(y - 1) * stride + x] : 0;
      const c = y && x >= channels ? data[(y - 1) * stride + x - channels] : 0;
      let predictor = 0;
      if (filter === 1) predictor = a;
      if (filter === 2) predictor = b;
      if (filter === 3) predictor = (a + b) >> 1;
      if (filter === 4) {
        const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
        predictor = pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
      }
      data[y * stride + x] = (raw[y * (stride + 1) + 1 + x] + predictor) & 255;
    }
  }
  return {width, height, channels, data, pixel(x, y) {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || y < 0 || x >= width || y >= height) return Buffer.alloc(0);
    const offset = (y * width + x) * channels;
    return data.subarray(offset, offset + 3);
  }};
}
function pause(milliseconds) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, milliseconds);
}
function screenshot(name) {
  const path = file(name, 'png');
  let bytes = null;
  for (let attempt = 0; attempt < 6; attempt++) {
    bytes = adb('exec-out', 'screencap', '-p');
    if (bytes.length > 8 && bytes.subarray(1, 4).toString() === 'PNG') break;
    bytes = null;
    pause(400);
  }
  assert.ok(bytes, 'screencap did not return a PNG after retries');
  writeFileSync(path, bytes);
  if (!screenshots.includes(path)) screenshots.push(path);
  const image = png(bytes);
  // Android's system ANR dialog is white; dismiss it so emulator load cannot invalidate a real result.
  const s = image.width / 1080, chip = image.pixel(540 * s, 1100 * s);
  if (chip.length === 3 && chip.every(value => value > 244)) {
    notes.push(`Dismissed emulator system ANR during ${name}`);
    physicalTap(320 * s, 1360 * s);
  }
  return image;
}
const rgb = hex => Buffer.from(hex, 'hex');
function color(pixel, hex, tolerance = 3) {
  const target = rgb(hex);
  return pixel.length === 3 && target.every((value, i) => Math.abs(value - pixel[i]) <= tolerance);
}
const pixelAt = (image, x, y) => image.pixel(x * scale, rootY + y * scale);
function enabledFill(image, x, y, hex) {
  const actual = pixelAt(image, x, y), expected = rgb(hex);
  // Handles Unity's highlighted tint. Disabled buttons are much darker and alpha-composited.
  return actual.length === 3 && expected.every((v, i) => actual[i] >= v * .85 && actual[i] <= Math.min(255, v * 1.18) + 3);
}
function runsAt(image, x, hex) {
  const runs = []; let start = -1;
  for (let y = 0; y <= image.height; y++) {
    if (y < image.height && color(image.pixel(x, y), hex, 1)) { if (start < 0) start = y; }
    else if (start >= 0) { runs.push({start, length: y - start}); start = -1; }
  }
  return runs;
}
function calibrate(image) {
  const s = image.width / 1080;
  const hud = runsAt(image, Math.round(268 * s), '1c2932')
    .find(run => run.start < image.height * .3 && Math.abs(run.length / s - 55) <= 4);
  if (!hud) return false;
  const y = hud.start - 124 * s;
  const nav = runsAt(image, Math.round(30 * s), '243b3d')
    .find(run => run.start > image.height * .55 && Math.abs(run.length / s - 104) <= 4);
  if (!nav) return false;
  const height = (nav.start - y) / s + 118;
  assert.ok(y >= -2 && height > 1000 && y + height * s <= image.height + 3, 'Invalid safe-area calibration');
  scale = s; rootY = y; rootH = height;
  return true;
}
function dialog(kind) {
  const requested = {welcome: 1040, offline: 893, inventory: 1370, crew: 1220, heroes: 1570,
    region: 1590, battle: 1550, 'recipe-detail': 1260, 'quest-detail': 1260,
    recipes: Math.min(1760, rootH - 100), quests: Math.min(1740, rootH - 100), world: Math.min(1750, rootH - 80)}[kind];
  assert.ok(Number.isFinite(requested), `Unknown dialog ${kind}`);
  const height = Math.min(requested, rootH - 90);
  return {x: 56, y: (rootH - height) / 2, height};
}
function hasDialog(image, kind) {
  const d = dialog(kind);
  // Safe-area and canvas rounding move the exact border by a pixel or two; scan a small band.
  const band = (y, hex) => {
    for (let offset = -6; offset <= 6; offset++)
      if (color(pixelAt(image, 70, y + offset), hex, 6)
        && color(pixelAt(image, 1000, y + offset), hex, 6)) return true;
    return false;
  };
  return band(d.y, 'e4bb78')
    && color(pixelAt(image, 951, d.y + 45), '293b43', 6)
    && band(d.y + d.height, '35464b');
}
function dialogTap(kind, x, y) {
  const d = dialog(kind);
  assert.ok(y >= 0 && y < d.height, `${kind} control is clipped on this viewport: y=${y}, h=${d.height}`);
  tap(d.x + x, d.y + y);
}
// Locate a welcome/offline panel before HUD calibration; do not guess a safe-area origin.
function dismissStartup(image) {
  const s = image.width / 1080;
  for (let y = 0; y < image.height; y++) {
    if (![70, 540, 1000].every(x => color(image.pixel(x * s, y), 'e4bb78', 3))) continue;
    if (color(image.pixel(50 * s, y), 'e4bb78', 3)) continue; // Context dock is not a dialog.
    for (const [height, buttonY] of [[1040, 875], [893, 732]]) {
      if (!color(image.pixel(70 * s, y + (height - 1) * s), '35464b', 6)) continue;
      if (!color(image.pixel(145 * s, y + (buttonY + 12) * s), 'e4bb78', 6)) continue;
      physicalTap(540 * s, y + (buttonY + 44) * s);
      return true;
    }
  }
  return false;
}
async function until(label, probe, timeout = 20000, interval = 350) {
  const end = Date.now() + timeout;
  do {
    const value = await probe();
    if (value) return value;
    await wait(interval);
  } while (Date.now() < end);
  throw new Error(`Timed out: ${label}`);
}
async function waitHUD(name = 'workshop', allowStartup = false) {
  const image = await until('visible resource HUD AND measured bottom navigation', () => {
    const frame = screenshot(name);
    if (allowStartup && dismissStartup(frame)) return false;
    return calibrate(frame) ? frame : false;
  }, 120000, 700);
  rememberPid();
  return image;
}
async function waitDialog(kind, name = kind) {
  return until(`visible ${kind} dialog`, () => {
    const frame = screenshot(name);
    return hasDialog(frame, kind) && frame;
  }, 30000, 350);
}
async function closeDialog() {
  shell('input', 'keyevent', '4');
  return waitHUD('back-to-workshop');
}
async function openTab(index, kind, name = kind) {
  tap([114, 328, 542, 756, 970][index], rootH - 66);
  return waitDialog(kind, name);
}
async function inventory(name = 'inventory') { tap(885, 63); return waitDialog('inventory', name); }
async function crew(name = 'crew') { tap(773, 63); return waitDialog('crew', name); }
function crop(image, x, y, width, height) {
  const left = Math.round(x * scale), top = Math.round(rootY + y * scale);
  const w = Math.round(width * scale), h = Math.round(height * scale);
  assert.ok(left >= 0 && top >= 0 && left + w <= image.width && top + h <= image.height, 'Screenshot crop is in bounds');
  const rows = [];
  for (let row = top; row < top + h; row++) {
    const begin = (row * image.width + left) * image.channels;
    rows.push(image.data.subarray(begin, begin + w * image.channels));
  }
  return Buffer.concat(rows);
}
async function scrollEvidence(kind, top, height, label) {
  const d = dialog(kind), region = [210, d.y + top + 20, 430, Math.min(240, height - 40)];
  const before = screenshot(`${label}-top`), beforeCrop = crop(before, ...region);
  swipe(540, d.y + top + height - 60, d.y + top + 80);
  let previous = null;
  const after = await until(`${label} scroll settling`, () => {
    const image = screenshot(`${label}-scrolled`), current = crop(image, ...region);
    const settled = previous && previous.equals(current);
    previous = current;
    return settled && image;
  }, 10000, 400);
  check(!beforeCrop.equals(crop(after, ...region)), `${label}: native drag changes list text, not workshop animation`);
}

function peekState() {
  return JSON.parse(adb('exec-out', 'cat', deviceSave).toString('utf8').replace(/^\uFEFF/, ''));
}
function readState(name = 'observed-save') {
  adb('pull', deviceSave, file(name));
  return JSON.parse(readFileSync(file(name), 'utf8').replace(/^\uFEFF/, ''));
}
async function waitState(label, predicate, timeout = 22000) {
  let last = null, readError = null;
  try {
    return await until(label, () => {
      try { last = peekState(); readError = null; }
      catch (error) { readError = error.message; return false; }
      writeJSON('observed-save', last);
      return predicate(last) && last;
    }, timeout, 300);
  } catch (error) {
    if (last) writeJSON('timeout-save', last);
    throw new Error(`${error.message}${readError ? `; save read: ${readError}` : ''}`);
  }
}
async function savedAfter(before, label, predicate = () => true) {
  return waitState(label, state => state.LastSavedUtc > before.LastSavedUtc && predicate(state));
}
async function pauseSave(label) {
  const previous = peekState();
  shell('input', 'keyevent', '3');
  // Unity pause/focus callbacks can take several seconds on ARM translation.
  await savedAfter(previous, `${label}: pause callback saved`);
  return readState(label); // Pull only AFTER observing LastSavedUtc advance.
}
async function checkpoint(label) {
  const saved = await pauseSave(label);
  launch();
  await waitHUD(`${label}-resumed`, true);
  return saved;
}
function pristine(state) {
  const c = state.Campaign;
  return state.Gold === 40 && state.Ore === 0 && state.Ingots === 0 && state.Inventory?.length === 0
    && state.TotalCrafted === 0 && state.TotalMined === 0 && state.TotalSold === 0 && state.OrdersCompleted === 0
    && state.LifetimeGold === 0 && state.BestCombo === 0 && state.Combo === 0
    && state.Levels?.every(n => n === 1) && state.Automated?.every(n => !n) && state.Running?.every(n => !n)
    && (!c || (c.Chapter === 0 && c.ExpeditionsWon === 0 && c.Reputation === 0 && c.TownLevel === 0
      && c.SelectedHero === 0 && !c.Battle?.Active && !c.Battle?.Finished && c.QuestClaims.every(n => !n)
      && ['CraftedCounts', 'Materials', 'RegionWins', 'EliteWins', 'BossKills'].every(key => c[key].every(n => n === 0))
      && c.Heroes.every((h, i) => !h || ((!h.Weapon || (!h.Weapon.Name && h.Weapon.Value===0)) && (!h.Armor || (!h.Armor.Name && h.Armor.Value===0)) && h.Level === 1 && h.Experience === 0 && (i === 0 || !h.Recruited)))));
}
function freshState() {
  // Literal WorkshopState defaults only; campaign/hero/battle defaults come from NormalizeCampaign.
  return {Gold: 40, Ore: 0, Ingots: 0, Inventory: [], Levels: [1, 1, 1, 1],
    Automated: [false, false, false, false], AutomationPaused: [false, false, false, false],
    Running: [false, false, false, false], Progress: [0, 0, 0, 0], TotalMined: 0, TotalCrafted: 0,
    TotalSold: 0, OrdersCompleted: 0, Combo: 0, BestCombo: 0, TutorialStep: 0, LastSavedUtc: 0,
    SoundEnabled: true, MusicEnabled: true, HapticsEnabled: true, LifetimeGold: 0, SimulationTime: 0,
    PendingQuality: 0, LastStrikeTime: -100, PendingSaleValue: 0, Campaign: null, SelectedRecipe: 0, PendingRecipe: 0};
}
async function installState(state, label, ageSeconds = 0) {
  const installed = clone(state);
  installed.LastSavedUtc = tickNow() - ageSeconds * 10000000;
  shell('am', 'force-stop', pkg);
  writeJSON(label, installed);
  // Rename on the same device filesystem, never clear app data or change the AVD.
  const temporary = `${deviceSave}.campaign-qa.tmp`;
  adb('push', file(label), temporary);
  shell('mv', '-f', temporary, deviceSave);
  worldOffset = 0;
  launch();
  await waitState(`${label}: app normalized and saved`, s => s.LastSavedUtc > installed.LastSavedUtc, 120000);
  await waitHUD(`${label}-loaded`, true); // A save update alone is NOT a rendered first frame.
  return readState(`${label}-observed`);
}
function protectedProgress(state) {
  const copy = clone(state);
  delete copy.LastSavedUtc; delete copy.Gold; delete copy.Ore; delete copy.Ingots; delete copy.Inventory;
  const hero = copy.Campaign.Heroes[copy.Campaign.SelectedHero];
  delete hero.Weapon.Quality; delete hero.Armor.Quality;
  return copy;
}
function assertManual(state) {
  assert.ok(state.Automated.every(n => !n), 'Golden opening must be entirely manual');
  assert.ok(state.AutomationPaused?.length === 4, 'Installed APK must include the approved campaign/pause save contract');
}
function worldTap(station) {
  // WorkshopView.StationAreas, native 1080x1665 scene, viewport starts at root y=186.
  const areas = [[17, 142, 126, 139], [235, 147, 131, 145], [113, 311, 159, 117]];
  const [x, y, w, h] = areas[station].map(n => n * 2.8125);
  const low = Math.max(351, 186 + y - worldOffset + 12);
  const high = Math.min(rootH - 347 - 12, 186 + y + h - worldOffset - 12);
  assert.ok(high > low, `Station ${station} is outside the visible world viewport`);
  let touchY = (low + high) / 2;
  if (station === 1 && touchY >= 436 && touchY <= 515) touchY = high > 531 ? 531 : Math.min(high, 424);
  tap(x + w / 2, touchY);
  focused(station);
}
function focused(station) {
  const viewportHeight = rootH - 486, anchorY = [263, 272, 383][station] * 2.8125;
  worldOffset = Math.max(0, Math.min(1665 - viewportHeight, anchorY - viewportHeight * .55));
}
async function manualMine(label) {
  const before = peekState();
  assert.ok(!before.Automated[0] || before.AutomationPaused[0], 'Manual mine requires unowned or paused crew');
  assert.ok(!before.Running[0]);
  worldTap(0);
  const after = await savedAfter(before, label, s => s.TotalMined === before.TotalMined + 2 && !s.Running[0]);
  assert.equal(after.Ore, before.Ore + 2, 'A manual mine cycle produces exactly two ore');
  return after;
}
async function smeltOne(label) {
  const before = peekState();
  assert.ok(before.Ore >= 2 && !before.Running[1], 'Smelting needs two real ore');
  worldTap(1);
  return savedAfter(before, label, s => s.Ingots === before.Ingots + 1 && !s.Running[1]);
}
async function selectRecipe(id, label = `recipe-${id}`) {
  const before = peekState();
  await openTab(2, 'recipes', `${label}-ledger`);
  const d = dialog('recipes'), rowY = 287 + id * 187;
  assert.ok(rowY + 138 < d.height - 37, `Recipe ${id} needs further scrolling on this viewport`);
  dialogTap('recipes', 32 + 695 + 93, rowY + 53 + 42);
  await waitHUD(`${label}-selected`);
  focused(2);
  return savedAfter(before, label, s => s.SelectedRecipe === id);
}
async function craftSelected(label) {
  const before = peekState(), recipe = before.SelectedRecipe;
  tap(696, rootH - 234); // ContextAction, never a direct simulation call.
  const after = await savedAfter(before, label, s => s.TotalCrafted === before.TotalCrafted + 1 && !s.Running[2]);
  assert.equal(after.Campaign.CraftedCounts[recipe], before.Campaign.CraftedCounts[recipe] + 1);
  assert.equal(after.Inventory.length, before.Inventory.length + 1);
  return after;
}
async function claimQuest(id, label) {
  const before = peekState(), side = id >= 15, row = side ? id - 15 : id;
  assert.ok(!before.Campaign.QuestClaims[id], `Quest ${id} was not already claimed`);
  await openTab(1, 'quests', `${label}-main-tab`);
  if (side) {
    dialogTap('quests', 488 + 223, 173 + 39); // Actual SideQuestTab, not the board or main-story row.
    await until('selected Town Requests tab', () => {
      const image = screenshot(`${label}-town-tab`), d = dialog('quests');
      return hasDialog(image, 'quests') && enabledFill(image, 56 + 500, d.y + 185, 'e4bb78');
    });
  }
  const claimY = 275 + row * 202 + 65 + 42;
  assert.ok(claimY + 43 < dialog('quests').height - 131, 'Quest claim row must be inside its list viewport');
  dialogTap('quests', 32 + 695 + 93, claimY);
  const after = await savedAfter(before, `QuestClaims[${id}]`, s => s.Campaign.QuestClaims[id]);
  const reward = {0: [30, 5, 0, 0], 1: [45, 8, 0, 0], 2: [80, 15, 0, 0], 16: [55, 10, 3, 1], 17: [30, 6, 2, 0]}[id];
  assert.equal(after.Gold - before.Gold, reward[0]);
  assert.equal(after.Campaign.Reputation - before.Campaign.Reputation, reward[1]);
  assert.equal(after.Campaign.Materials[0] - before.Campaign.Materials[0], reward[2]);
  assert.equal(after.Campaign.TownLevel - before.Campaign.TownLevel, reward[3]);
  if (id === 17) assert.equal(after.Ore, before.Ore - 8, 'Delivery consumes exactly eight real ore');
  screenshot(`${label}-claimed`);
  // The same actual claim target is disabled, and a second touch must not pay again.
  dialogTap('quests', 32 + 695 + 93, claimY);
  await closeDialog();
  const persisted = await checkpoint(`${label}-persisted`);
  assert.equal(persisted.Gold, after.Gold);
  assert.deepEqual(persisted.Campaign.QuestClaims, after.Campaign.QuestClaims);
  check(true, `Quest ${id} pays once through ${side ? 'SideQuestTab' : 'Quest tab'} row ${row}`, {save: file(`${label}-persisted`)});
  return persisted;
}
function mapPoint(region) {
  const d = dialog('world'), height = Math.min(1140, d.height - 440), width = height * .8;
  const [x, y] = [[77, 383], [261, 322], [94, 223], [275, 155], [188, 63]][region];
  return [56 + (968 - width) / 2 + x / 384 * width, d.y + 174 + y / 480 * height - 2];
}
async function depart(region, route, label) {
  const before = peekState();
  assert.ok(!before.Campaign.Battle.Active && !before.Campaign.Battle.Finished, 'Collect the previous battle first');
  await openTab(4, 'world', `${label}-map`);
  tap(...mapPoint(region));
  await waitDialog('region', `${label}-routes`);
  dialogTap('region', 34 + 668 + 104, 740 + route * 205 + 55 + 45);
  const active = await savedAfter(before, `${label}: departure saved`, s => s.Campaign.Battle.Active
    && s.Campaign.Battle.Region === region && s.Campaign.Battle.Route === route);
  await waitDialog('battle', `${label}-battle-active`);
  return active;
}
const skillXs = [39 + 143, 341 + 143, 643 + 143];
function observeSkills(state) {
  const battle = state.Campaign.Battle;
  for (const skill of skillAttempts) {
    if (!skillEvidence.has(skill) && ((skill === 0 && battle.StrikeCooldown > 0)
      || (skill === 1 && battle.GuardCooldown > 0) || (skill === 2 && battle.HealsUsed === 1))) {
      skillEvidence.add(skill);
      writeJSON(`skill-${skill}-saved-effect`, state);
    }
  }
}
async function fight(label) {
  let lastState = peekState();
  const end = Date.now() + 90000;
  while (Date.now() < end) {
    const frame = screenshot(`${label}-battle-live`);
    assert.ok(hasDialog(frame, 'battle'), 'Skills are only tapped in the visible current battle dialog');
    lastState = peekState();
    const b = lastState.Campaign.Battle, d = dialog('battle');
    observeSkills(lastState);
    const collectEnabled = enabledFill(frame, 56 + 60, d.y + 1338, 'e4bb78');
    if (b.Finished && collectEnabled) {
      check(b.Won && !b.Active && b.EnemyHP === 0 && b.HeroHP > 0, `${label}: real battle victory`);
      writeJSON(`${label}-finished`, lastState);
      screenshot(`${label}-victory`);
      return lastState;
    }
    if (b.Active && !collectEnabled) {
      // Brace first, recover after damage, then strike. Early strike can end the two-wave supply
      // before its first incoming hit, so defer it until healing has been observed in the golden path.
      const order = skillEvidence.has(2) ? [0, 1, 2] : [1, 2, 0];
      for (const skill of order) {
        if (skill === 0 && stage === 'golden' && !skillEvidence.has(2)) continue;
        const current = peekState().Campaign.Battle;
        if (!current.Active || current.Finished) break;
        if (skill === 2 && current.HealsUsed > 0) continue;
        // The saved cooldown can be stale until autosave: trust the current button tint instead.
        // One native frame per iteration keeps short supply fights interactive. No modal can change
        // here: only skills are touched, and a finished battle disables them in the game itself.
        if (!enabledFill(frame, 56 + 49 + skill * 302, d.y + 950, skill === 0 ? 'e4bb78' : '33534e')) continue;
        dialogTap('battle', skillXs[skill], 937 + 56);
        skillAttempts.add(skill);
        observeSkills(peekState());
      }
    }
    // Completion itself need not save immediately: wait up to the 10s autosave, not a hasty tap loop.
    await wait(400);
  }
  writeJSON(`${label}-battle-timeout`, lastState);
  throw new Error(`${label}: battle did not reach a saved, visible terminal state within 90 seconds`);
}
function totalXP(hero) { return hero.Experience + 15 * hero.Level * (hero.Level - 1); }
function lootSnapshot(s) {
  return {Gold: s.Gold, LifetimeGold: s.LifetimeGold, Campaign: clone(s.Campaign), Inventory: clone(s.Inventory), TotalSold: s.TotalSold};
}
async function collectBattle(finished, label) {
  const b = finished.Campaign.Battle;
  assert.ok(b.Finished && b.Won && !b.Active);
  await waitDialog('battle', `${label}-collect-ready`);
  dialogTap('battle', 45 + 439, 1324 + 51);
  const after = await savedAfter(finished, `${label}: collection saved`, s => !s.Campaign.Battle.Active && !s.Campaign.Battle.Finished);
  await waitDialog('world', `${label}-map-after-loot`);
  assert.equal(after.Gold - finished.Gold, b.RewardGold);
  assert.equal(after.LifetimeGold - finished.LifetimeGold, b.RewardGold);
  assert.equal(after.Campaign.Materials[b.Region] - finished.Campaign.Materials[b.Region], b.RewardMaterial);
  assert.equal(totalXP(after.Campaign.Heroes[b.HeroIndex]) - totalXP(finished.Campaign.Heroes[b.HeroIndex]), b.RewardExperience);
  assert.equal(after.Campaign.ExpeditionsWon, finished.Campaign.ExpeditionsWon + 1);
  assert.equal(after.Campaign.RegionWins[b.Region], finished.Campaign.RegionWins[b.Region] + 1);
  assert.equal(after.Campaign.EliteWins[b.Region], finished.Campaign.EliteWins[b.Region] + (b.Route === 1 ? 1 : 0));
  assert.equal(after.Campaign.BossKills[b.Region], finished.Campaign.BossKills[b.Region] + (b.Route === 2 ? 1 : 0));
  assert.deepEqual(after.Campaign.QuestClaims, finished.Campaign.QuestClaims, 'Loot never auto-claims a quest');
  await closeDialog();
  // Reopening WORLD must show the map, not an old collectible result.
  await openTab(4, 'world', `${label}-no-second-loot`);
  await closeDialog();
  const persisted = await checkpoint(`${label}-loot-persisted`);
  assert.deepEqual(lootSnapshot(persisted), lootSnapshot(after), 'Reopening and saving cannot award loot twice');
  check(true, `${label}: one material/gold/XP payment and persisted cleared battle`, {save: file(`${label}-loot-persisted`)});
  return persisted;
}
async function crewToggle(before, paused, label) {
  dialogTap('crew', 32 + 657 + 111, 301 + 50 + 36);
  return savedAfter(before, label, s => s.Automated[0] && s.AutomationPaused[0] === paused);
}

async function goldenPath() {
  stage = 'golden';
  await waitState('tutorial accepted through Begin', s => s.TutorialStep === 1);
  let start = readState('golden-start');
  check(pristine(start) && start.TutorialStep === 1, 'Known empty profile entered through the rendered workshop/Begin');
  assertManual(start);
  for (let i = 0; i < 5; i++) {
    await manualMine(`opening-mine-${i}`);
    await smeltOne(`opening-smelt-${i}`);
  }
  let s = readState('five-manual-ingots');
  check(s.Ingots === 5 && s.Ore === 0 && s.TotalMined === 10 && s.TotalCrafted === 0, 'Five ingots earned exclusively by manual mine/smelt touches');
  await selectRecipe(0);
  s = await craftSelected('first-sword');
  const sword = clone(s.Inventory[0]);
  await selectRecipe(2);
  s = await craftSelected('first-mail');
  const mail = clone(s.Inventory[1]);
  check(sword.RecipeId === 0 && mail.RecipeId === 2 && s.Ingots === 0, 'Recipe 0 sword and recipe 2 mail consume the five earned ingots');
  await inventory('earned-sword-and-mail');
  dialogTap('inventory', 32 + 654 + 112, 235 + 29 + 34);
  s = await savedAfter(s, 'InventoryEquip0 weapon', a => a.Campaign.Heroes[a.Campaign.SelectedHero].Weapon?.RecipeId === 0 && a.Inventory.length === 1);
  await waitDialog('inventory', 'weapon-equipped');
  dialogTap('inventory', 32 + 654 + 112, 235 + 29 + 34); // Mail becomes index zero after removal.
  s = await savedAfter(s, 'InventoryEquip0 armor', a => a.Campaign.Heroes[a.Campaign.SelectedHero].Armor?.RecipeId === 2 && a.Inventory.length === 0);
  await closeDialog();
  s = await checkpoint('earned-equipment');
  assert.deepEqual(s.Campaign.Heroes[s.Campaign.SelectedHero].Weapon, sword);
  assert.deepEqual(s.Campaign.Heroes[s.Campaign.SelectedHero].Armor, mail);
  check(s.Campaign.SelectedHero === 0 && s.TotalSold === 0, 'Selected Rowan retains the exact UI-equipped weapon/armor in the save');
  await openTab(3, 'heroes', 'golden-hero-loadout');
  await closeDialog();
  await claimQuest(0, 'main-quest-0');

  await openTab(4, 'world', 'golden-world-locked');
  const beforeLocked = peekState(), point = mapPoint(1);
  const lockedFrame = screenshot('locked-region-1');
  check(!enabledFill(lockedFrame, point[0] - 32, point[1] - 30, 'e4bb78'), 'Region 1 map tile is visibly unavailable in chapter 0');
  tap(...point);
  await waitDialog('world', 'locked-region-stays-map');
  await closeDialog();
  const lockedState = await checkpoint('locked-region-save');
  assert.deepEqual(lockedState.Campaign.Battle, beforeLocked.Campaign.Battle);
  check(lockedState.Campaign.Chapter === 0, 'Locked region tap cannot open departure or start a battle');

  await openTab(2, 'recipes', 'golden-recipes');
  let d = dialog('recipes'), lockedY = 287 + 4 * 187 + 53;
  assert.ok(lockedY + 85 < d.height - 37, 'Locked recipe 4 is visible within the ledger');
  check(!enabledFill(screenshot('locked-recipe-4'), 56 + 32 + 705, d.y + lockedY + 12, 'e4bb78'), 'Recipe 4 select button is disabled before chapter 1');
  const recipeBefore = peekState();
  dialogTap('recipes', 32 + 695 + 93, lockedY + 42);
  await waitDialog('recipes', 'locked-recipe-stays-ledger');
  await scrollEvidence('recipes', 287, d.height - 324, 'golden-recipe-scroll');
  await closeDialog();
  s = await checkpoint('locked-recipe-save');
  check(s.SelectedRecipe === recipeBefore.SelectedRecipe && !s.Running[2] && s.TotalCrafted === recipeBefore.TotalCrafted, 'Locked recipe touch changes neither selection nor production');

  await depart(0, 0, 'golden-supply');
  await collectBattle(await fight('golden-supply'), 'golden-supply');
  // A very fast device may finish the first supply between recover and strike. A real second
  // patrol is allowed, but never inject a battle or silently skip skill coverage.
  if (skillEvidence.size < 3) {
    await depart(0, 0, 'golden-skill-patrol');
    await collectBattle(await fight('golden-skill-patrol'), 'golden-skill-patrol');
  }
  check([0, 1, 2].every(skill => skillEvidence.has(skill)), 'Power Strike, Brace, and Recover used through visible Android buttons and persisted effects');
  await claimQuest(1, 'main-quest-1');
  const oreBefore = peekState();
  for (let i = 0; i < 4; i++) await manualMine(`delivery-mine-${i}`);
  s = peekState();
  check(s.Ore === oreBefore.Ore + 8 && s.TotalMined === oreBefore.TotalMined + 8, 'Four manual mine cycles supply the eight-ore delivery');
  await claimQuest(17, 'side-quest-17');

  await crew('golden-crew-before-hire');
  let before = peekState();
  dialogTap('crew', 32 + 657 + 111, 301 + 50 + 36);
  s = await savedAfter(before, 'hire miner', a => a.Automated[0] && !a.AutomationPaused[0]);
  check(s.Gold === before.Gold - 60, 'Miner hire charges once and persists ownership');
  s = await crewToggle(s, true, 'pause owned miner');
  const paused = s;
  await closeDialog();
  s = await waitState('paused miner completes only its in-flight job', a => a.LastSavedUtc > paused.LastSavedUtc && !a.Running[0]);
  assert.ok(s.TotalMined >= paused.TotalMined && s.TotalMined <= paused.TotalMined + (paused.Running[0] ? 2 : 0));
  const settled = s;
  s = await savedAfter(settled, 'later autosave while paused', a => a.SimulationTime >= settled.SimulationTime + 3);
  check(s.TotalMined === settled.TotalMined && s.Ore === settled.Ore && s.Automated[0] && s.AutomationPaused[0], 'Pause preserves ownership and prevents future mine cycles');
  await crew('golden-crew-paused');
  const resumeBefore = peekState();
  await crewToggle(resumeBefore, false, 'resume owned miner');
  await closeDialog();
  s = await waitState('resumed miner produces new ore', a => a.TotalMined > resumeBefore.TotalMined && a.LastSavedUtc > resumeBefore.LastSavedUtc);
  check(s.Gold === resumeBefore.Gold && s.Automated[0] && !s.AutomationPaused[0], 'Resume restarts work without a second hire charge');

  await crew('golden-crew-final-pause');
  await crewToggle(peekState(), true, 'pause miner for deterministic reward checks');
  await closeDialog();
  await waitState('final miner job settled', a => !a.Running[0] && a.AutomationPaused[0]);
  const stock = peekState();
  s = await checkpoint('golden-complete');
  assert.deepEqual(s.Inventory, stock.Inventory);
  assert.deepEqual(s.Campaign.Heroes[0].Weapon, sword);
  assert.deepEqual(s.Campaign.Heroes[0].Armor, mail);
  check(s.Inventory.length === 0 && s.TotalSold === 0 && !s.Automated[3], 'Both earned items remain equipped; no merchant sold the hero loadout');
  assert.ok([0, 1, 17].every(id => s.Campaign.QuestClaims[id]) && !s.Campaign.QuestClaims[2]);
  assert.ok(s.Campaign.BossKills.every(n => n === 0) && s.Campaign.EliteWins.every(n => n === 0));
  assert.ok(s.Running.every(n => !n));
  writeJSON('golden-profile', s);
  goldenHash = hash(readFileSync(file('golden-profile')));
  goldenCompleted = true;
  goldenApk = apk;
  screenshot('golden-workshop-complete');
  writeJSON('golden-evidence', {completed: true, goldenHash, pkg, serial, apk, checks: checks.filter(c => c.stage === 'golden'),
    screenshots: [...screenshots], skillEvidence: [...skillEvidence], completedAt: new Date().toISOString()});
  return s;
}

async function fixturePath(golden) {
  assert.ok(goldenCompleted, 'Resource/quality fixtures are forbidden before golden completion');
  stage = 'fixture';
  fixtureUsedAfterGoldenPath = true;
  const boosted = clone(golden), hero = boosted.Campaign.Heroes[boosted.Campaign.SelectedHero];
  boosted.Gold = 1000; boosted.Ore = 80; boosted.Ingots = 80;
  hero.Weapon.Quality = 3; hero.Armor.Quality = 3;
  boosted.Inventory=Array.from({length:8},(_,i)=>clone(i%2===0?hero.Weapon:hero.Armor));
  assert.deepEqual(protectedProgress(boosted), protectedProgress(golden), 'Fixture may change only resources, inventory copies and equipped quality');
  fixtures.push({path: file('resource-quality-fixture'), allowedChanges: ['Gold', 'Ore', 'Ingots', 'Inventory: eight copies of the earned gear for scrolling', 'Hero.Weapon.Quality', 'Hero.Armor.Quality', 'LastSavedUtc']});
  let s = await installState(boosted, 'resource-quality-fixture');
  assert.deepEqual(s.Campaign.QuestClaims, golden.Campaign.QuestClaims);
  assert.deepEqual(s.Campaign.RegionWins, golden.Campaign.RegionWins);
  assert.deepEqual(s.Campaign.BossKills, golden.Campaign.BossKills);
  assert.deepEqual(s.Campaign.EliteWins, golden.Campaign.EliteWins);
  check(s.Campaign.Heroes[0].Weapon.Quality === 3 && s.Campaign.Heroes[0].Armor.Quality === 3,
    'Labeled resource/quality-only fixture loaded after preserving golden profile');
  await inventory('fixture-inventory');
  await scrollEvidence('inventory',235,924,'fixture-inventory-scroll');
  await closeDialog();
  await openTab(3, 'heroes', 'fixture-hero-masterwork');
  await closeDialog();
  await depart(0, 1, 'fixture-elite');
  await collectBattle(await fight('fixture-elite'), 'fixture-elite');
  screenshot('town-before-rescue');
  s = await claimQuest(16, 'side-rescue-16');
  screenshot('town-restored-level-1');
  check(s.Campaign.TownLevel === 1 && s.Campaign.QuestClaims[16], 'Elite rescue 16 activates visible town restoration (before/after screenshots)');
  await depart(0, 2, 'fixture-cinderhart');
  await collectBattle(await fight('fixture-cinderhart'), 'fixture-cinderhart');
  s = await claimQuest(2, 'main-quest-2');
  check(s.Campaign.Chapter === 1 && s.Campaign.BossKills[0] === 1, 'Earned Cinderhart boss claim and quest 2 unlock chapter 1');
  await openTab(4, 'world', 'chapter-1-world-unlocked');
  tap(...mapPoint(1));
  await waitDialog('region', 'chapter-1-marsh-routes');
  check(true, 'Region 1 opens its actual route dialog after chapter unlock');
  await closeDialog();
  await selectRecipe(4, 'chapter-1-recipe-4');
  const beforeCraft = peekState();
  s = await craftSelected('chapter-1-reedfire-sword');
  check(s.SelectedRecipe === 4 && s.Campaign.CraftedCounts[4] === beforeCraft.Campaign.CraftedCounts[4] + 1
    && s.Ingots === beforeCraft.Ingots - 4 && s.Campaign.Materials[0] === beforeCraft.Campaign.Materials[0] - 2
    && s.Campaign.Materials[1] === beforeCraft.Campaign.Materials[1], 'Unlocked recipe 4 crafts using four ingots and Materials[0], not marsh material');

  // Region 1 elite lasts long enough to preserve a live battle across the asynchronous pause
  // callback, and is survivable with legitimately levelled Rowan's quality-three starter gear.
  await depart(1, 1, 'cold-restore');
  const background = await pauseSave('background-active-battle');
  check(background.Campaign.Battle.Active && !background.Campaign.Battle.Finished && !background.Campaign.Battle.Won,
    'Background callback persists a genuinely active expedition before cold restart');
  shell('am', 'force-stop', pkg);
  const old = clone(background);
  // installState changes ONLY this timestamp; no battle outcome, snapshots, XP, or flags are forged.
  fixtures.push({path: file('offline-clock-fixture'), allowedChanges: ['LastSavedUtc'], secondsOffline: 120});
  const loaded = await installState(old, 'offline-clock-fixture', 120);
  const installedClock = JSON.parse(readFileSync(file('offline-clock-fixture'), 'utf8'));
  const withoutClock = value => { const copy = clone(value); delete copy.LastSavedUtc; return copy; };
  assert.deepEqual(withoutClock(installedClock), withoutClock(background));
  assert.ok(installedClock.Campaign.Battle.Active && !installedClock.Campaign.Battle.Finished && !installedClock.Campaign.Battle.Won);
  assert.equal(loaded.Gold, background.Gold, 'Offline completion does not auto-pay expedition gold');
  assert.deepEqual(loaded.Campaign.Materials, background.Campaign.Materials);
  assert.equal(loaded.Campaign.ExpeditionsWon, background.Campaign.ExpeditionsWon);
  assert.deepEqual(loaded.Campaign.QuestClaims, background.Campaign.QuestClaims);
  await openTab(4, 'battle', 'cold-restored-expedition');
  const finished = await fight('cold-restored');
  check(finished.Campaign.Battle.Elapsed > background.Campaign.Battle.Elapsed, 'Cold load advances the real active battle from LastSavedUtc');
  s = await collectBattle(finished, 'cold-restored');
  const autoBefore = peekState();
  const automatic = await savedAfter(autoBefore, 'untouched 10-second autosave', a => a.SimulationTime >= autoBefore.SimulationTime + 1);
  assert.deepEqual(lootSnapshot(automatic), lootSnapshot(s));
  check(automatic.LastSavedUtc > autoBefore.LastSavedUtc, 'Foreground autosave persists stable rewards without another touch');
  await pauseSave('final-before-cold-check');
  shell('am', 'force-stop', pkg);
  worldOffset = 0;
  launch();
  await waitHUD('final-cold-workshop', true);
  const final = readState('final-save');
  assert.deepEqual(lootSnapshot(final), lootSnapshot(s));
  check(!final.Campaign.Battle.Active && !final.Campaign.Battle.Finished, 'Another cold launch cannot replay collected loot or lose campaign/equipment');
  assert.equal(hash(readFileSync(file('golden-profile'))), goldenHash, 'Fixture stages never overwrite the golden restoration profile');
}

function captureLog() {
  if (!logStart) return;
  const full = adb('logcat', '-d', '-v', 'threadtime', '-T', logStart, 'Unity:V', 'AndroidRuntime:V', 'libc:F', 'DEBUG:F', '*:S').toString();
  writeFileSync(file('runtime', 'log'), full);
  const relevant = full.split(/\r?\n/).filter(line => {
    const match = line.match(/^\d\d-\d\d\s+\S+\s+(\d+)\s+/);
    return match && appPids.has(match[1]);
  }).join('\n');
  assert.ok(appPids.size > 0 && relevant.length > 0, 'Runtime log must contain this run\'s Unity process output');
  check(!/FATAL EXCEPTION|Fatal signal|\b\w*(?:Exception|ErrorException)\b|ExecutionEngineException|SIGSEGV|SIGABRT/i.test(relevant),
    'No fatal or managed exceptions in this run\'s Unity/AndroidRuntime logs', {log: file('runtime', 'log'), pids: [...appPids]});
}
function result(passed, error = null) {
  writeJSON('result', {passed, startedAt, finishedAt: new Date().toISOString(), stage, checks,
    nativeTouch: true, device: serial, package: pkg, viewport: {scale, rootY, rootH},
    goldenCompleted, goldenProfile: file('golden-profile'), goldenHash, goldenApk, goldenResumed, originalSave,
    fixtureUsedAfterGoldenPath, fixtures, apk, skillEvidence: [...skillEvidence], screenshots, notes,
    ...(error ? {error: error.message, stack: error.stack} : {})});
}

try {
  assert.equal(adb('get-state').toString().trim(), 'device', 'The explicitly authorized device must be connected');
  const packagePath = shell('pm', 'path', pkg).toString().trim().split(/\r?\n/).find(line => line.endsWith('/base.apk'));
  assert.ok(packagePath, `Install the approved campaign APK first: ${pkg}`);
  const installedApk = packagePath.slice('package:'.length);
  try {
    const sha256 = shell('sha256sum', installedApk).toString().match(/^([a-f0-9]{64})\s/i)?.[1];
    assert.ok(sha256);
    apk = {sha256, source: 'installed-base-apk', devicePath: installedApk};
  } catch {
    apk = existsSync(localApk) ? {sha256: hash(readFileSync(localApk)), source: 'local-build-not-verified-installed', path: localApk} : {sha256: null};
    notes.push('Installed APK hashing unavailable; any local build hash is explicitly not an installed-APK verification.');
  }
  logStart = shell("date '+%m-%d %H:%M:%S.000'").toString().trim();
  assert.match(logStart, /^\d\d-\d\d \d\d:\d\d:\d\d\.000$/);
  // Preserve existing data before a launch, reset, or fixture restore. Never overwrite a prior run's backup.
  const hasSave = shell('sh', '-c', `"if [ -f '${deviceSave}' ]; then printf present; fi"`).toString().trim() === 'present';
  let oldState = null;
  if (hasSave) {
    originalSave = file(`pre-run-${runId}`);
    adb('pull', deviceSave, originalSave);
    oldState = JSON.parse(readFileSync(originalSave, 'utf8').replace(/^\uFEFF/, ''));
  }
  let golden;
  if (args.has('--fixtures-only')) {
    const evidence = JSON.parse(readFileSync(file('golden-evidence'), 'utf8'));
    goldenHash = hash(readFileSync(file('golden-profile')));
    assert.ok(evidence.completed && evidence.pkg === pkg && evidence.serial === serial && evidence.goldenHash === goldenHash,
      '--fixtures-only requires this script\'s completed, unmodified golden profile/evidence');
    golden = JSON.parse(readFileSync(file('golden-profile'), 'utf8'));
    goldenCompleted = true;
    goldenResumed = true;
    goldenApk = evidence.apk;
    if (goldenApk?.sha256 !== apk?.sha256) notes.push('Golden evidence belongs to a different APK hash; this invocation validates only the resumed fixture stages on the current APK.');
    checks.push(...evidence.checks);
    screenshots.push(...evidence.screenshots.filter(path => !screenshots.includes(path)));
    for (const skill of evidence.skillEvidence) skillEvidence.add(skill);
    notes.push('Golden path evidence resumed from campaign-android-golden-evidence.json; golden APK metadata is preserved there.');
    stage = 'golden-restore';
    await installState(golden, 'golden-resume');
  } else {
    if (args.has('--fresh')) {
      if (!hasSave) {
        launch();
        await waitHUD('initial-install-workshop', true);
        await waitState('initial persistent data directory exists', s => s.LastSavedUtc > 0);
      }
      await installState(freshState(), 'generated-fresh-state');
    } else {
      assert.ok(!oldState || pristine(oldState), 'Existing progress was preserved. Use --fresh to reset this QA app, or --fixtures-only to resume golden evidence.');
      launch();
      await waitHUD('initial-workshop', true);
    }
    golden = await goldenPath();
  }
  await fixturePath(golden);
  stage = 'runtime-log';
  captureLog();
  result(true);
  console.log('CAMPAIGN_ANDROID_PASS', checks.length, file('result'));
} catch (error) {
  try { screenshot('failure'); } catch (captureError) { notes.push(`Failure screenshot unavailable: ${captureError.message}`); }
  try { captureLog(); } catch (logError) { notes.push(`Runtime log check: ${logError.message}`); }
  result(false, error);
  console.error('CAMPAIGN_ANDROID_FAIL', error.message, file('result'));
  process.exitCode = 1;
}
