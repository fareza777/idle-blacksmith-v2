import { Client } from 'file:///C:/Users/FAJAR/.claude/plugins/cache/aseprite-ai-artist/aseprite/0.8.2/node_modules/@modelcontextprotocol/sdk/dist/esm/client/index.js';
import { StdioClientTransport } from 'file:///C:/Users/FAJAR/.claude/plugins/cache/aseprite-ai-artist/aseprite/0.8.2/node_modules/@modelcontextprotocol/sdk/dist/esm/client/stdio.js';
import { writeFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

export const root = 'D:/Idle Blacksmith';
export async function withAseprite(work) {
  const client = new Client({ name: 'forge-empire-art', version: '0.1.0' });
  const transport = new StdioClientTransport({
    command: process.execPath,
    args: ['C:/Users/FAJAR/.claude/plugins/cache/aseprite-ai-artist/aseprite/0.8.2/dist/cli.js', 'serve', '--headless', '--aseprite', 'D:/Tools/aseprite/aseprite.exe'],
    env: { ...process.env, ASEPRITE_AI_ALLOW_LUA: '0', ASEPRITE_AI_PROMPTS: '0' },
    stderr: 'pipe', cwd: root,
  });
  try {
    await client.connect(transport);
    const call = async (name, args = {}, capture) => {
      const result = await client.callTool({ name, arguments: args }, undefined, { timeout: 120000 });
      if (result.isError || result.structuredContent?.error) throw new Error(`${name}: ${JSON.stringify(result)}`);
      if (capture) {
        const image = result.content?.find(c => c.type === 'image');
        if (image) await writeFile(`${root}/QA/${capture}.png`, Buffer.from(image.data, 'base64'));
      }
      return result.structuredContent ?? result;
    };
    const ready = await call('preflight');
    if (!ready.ready || ready.mode !== 'headless') throw new Error(`Aseprite unavailable: ${JSON.stringify(ready)}`);
    console.log(`Aseprite MCP READY ${ready.asepriteVersion} (isolated headless)`);
    return await work(call, client);
  } finally { await client.close(); }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await withAseprite(async (_, client) => {
    const { tools } = await client.listTools();
    const selected = tools.filter(t => ['sprite_manage', 'draw', 'frame', 'layer', 'tag', 'palette', 'export', 'look', 'validate'].includes(t.name));
    await writeFile(`${root}/QA/aseprite-schemas.json`, JSON.stringify(selected.map(t => ({ name: t.name, input: t.inputSchema })), null, 2));
    console.log(selected.map(t => t.name).join(', '));
  });
}
