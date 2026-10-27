import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {validate,compile} from '../tools/validate-chronicle-content.mjs';
const source=JSON.parse(fs.readFileSync(new URL('../content/chronicles.json',import.meta.url),'utf8'));
test('authoring catalog compiles reproducibly and matches runtime',()=>{
 const generated=compile(source),actual=fs.readFileSync(new URL('../Game/Assets/ForgeEmpire/Core/ChronicleContent.cs',import.meta.url),'utf8').replaceAll('\r\n','\n');assert.equal(actual,generated);
});
for(const [name,mutate]of [
 ['duplicate IDs',c=>c.enemies[1].id=0],['missing enemy',c=>c.enemies.pop()],['invalid region',c=>c.recipes[0].region=9],['negative cost',c=>c.recipes[0].ingots=-1],['unknown effect',c=>c.recipes[0].effect='Fake'],['missing boss counter',c=>c.bosses[0].counter=''],['mechanic reuse',c=>c.bosses[1].mechanic=0],['bad timing',c=>c.enemies[0].attackTicks=0],['nonfinite damage',c=>c.enemies[0].power=Infinity]])test(`authoring rejects ${name}`,()=>{const copy=structuredClone(source);mutate(copy);assert.throws(()=>validate(copy));});
