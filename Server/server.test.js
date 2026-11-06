'use strict';

const test = require('node:test');
const assert = require('node:assert');
const http = require('node:http');
const { createServer, createLimiter, cleanReply } = require('./server.js');

function listen(server) {
  return new Promise((resolve) => server.listen(0, '127.0.0.1', () => resolve(server.address().port)));
}

function fakeAi(handler) {
  return http.createServer((req, res) => {
    let body = '';
    req.on('data', (chunk) => { body += chunk; });
    req.on('end', () => handler(JSON.parse(body), req, res));
  });
}

function answer(res, content) {
  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify({ choices: [{ message: { content } }] }));
}

async function withServer(handler, overrides, run) {
  const ai = fakeAi(handler);
  const aiPort = await listen(ai);
  const server = createServer({
    apiKey: 'test-key', model: 'test-model', upstreamUrl: `http://127.0.0.1:${aiPort}/`,
    maxPerDay: 100, timeoutMs: 300, ...overrides,
  });
  const port = await listen(server);
  const post = (body) => fetch(`http://127.0.0.1:${port}/marlow`, { method: 'POST', body: JSON.stringify(body) });
  try {
    await run(post, port);
  } finally {
    server.close();
    ai.closeAllConnections();
    ai.close();
  }
}

test('returns a checked reply and sends the game state and key to the AI service', async () => {
  let sent;
  let authorization;
  await withServer((body, req, res) => {
    sent = body;
    authorization = req.headers.authorization;
    answer(res, JSON.stringify({ reply: 'Stones are on the beach.', mood: 'cheerful', action: 'point_stone' }));
  }, {}, async (post) => {
    const res = await post({ message: 'Where is stone?', hunger: 50, inventory: ['Wood: 1'], events: ['Built a campfire'], quest: 'Lens: found' });
    assert.strictEqual(res.status, 200);
    assert.deepStrictEqual(await res.json(), { reply: 'Stones are on the beach.', mood: 'cheerful', action: 'point_stone' });
  });
  assert.strictEqual(authorization, 'Bearer test-key');
  const user = JSON.parse(sent.messages[1].content);
  assert.strictEqual(user.player_says, 'Where is stone?');
  assert.deepStrictEqual(user.game_state.events, ['Built a campfire']);
  assert.strictEqual(user.game_state.quest, 'Lens: found');
});

test('accepts the quest marker actions', () => {
  assert.strictEqual(cleanReply(JSON.stringify({ reply: 'Marked.', action: 'point_wreck' })).action, 'point_wreck');
});

test('replaces an invented action and mood with safe values', () => {
  assert.deepStrictEqual(
    cleanReply(JSON.stringify({ reply: 'Done!', mood: 'evil', action: 'give_axe' })),
    { reply: 'Done!', mood: 'neutral', action: 'none' });
});

test('rejects replies that are not valid', async () => {
  assert.strictEqual(cleanReply('not json'), null);
  assert.strictEqual(cleanReply(JSON.stringify({ reply: '' })), null);
  await withServer((body, req, res) => answer(res, 'oops'), {}, async (post) => {
    assert.strictEqual((await post({ message: 'hi' })).status, 502);
  });
});

test('cuts over-long replies and messages', async () => {
  let sent;
  await withServer((body, req, res) => {
    sent = JSON.parse(body.messages[1].content);
    answer(res, JSON.stringify({ reply: 'x'.repeat(1000), action: 'none' }));
  }, {}, async (post) => {
    const res = await post({ message: 'y'.repeat(1000) });
    assert.strictEqual((await res.json()).reply.length, 280);
    assert.strictEqual(sent.player_says.length, 300);
  });
});

test('fails cleanly when the AI service errors or times out', async () => {
  await withServer((body, req, res) => { res.writeHead(500); res.end('{}'); }, {}, async (post) => {
    assert.strictEqual((await post({ message: 'hi' })).status, 502);
  });
  await withServer(() => {}, { timeoutMs: 50 }, async (post) => {
    assert.strictEqual((await post({ message: 'hi' })).status, 502);
  });
});

test('needs a key, a message and valid JSON', async () => {
  await withServer(() => assert.fail('should not call the AI'), { apiKey: '' }, async (post) => {
    assert.strictEqual((await post({ message: 'hi' })).status, 503);
  });
  await withServer(() => assert.fail('should not call the AI'), {}, async (post, port) => {
    assert.strictEqual((await post({ message: '   ' })).status, 400);
    const bad = await fetch(`http://127.0.0.1:${port}/marlow`, { method: 'POST', body: '{nope' });
    assert.strictEqual(bad.status, 400);
    const big = await fetch(`http://127.0.0.1:${port}/marlow`, { method: 'POST', body: 'x'.repeat(20000) }).catch(() => null);
    assert.ok(big === null || big.status === 400);
  });
});

test('limits each player per minute and everyone per day', () => {
  let time = Date.UTC(2026, 10, 6, 12);
  const allow = createLimiter({ perMinute: 2, perDay: 3, now: () => time });
  assert.ok(allow('a'));
  assert.ok(allow('a'));
  assert.ok(!allow('a'));
  assert.ok(allow('b'));
  assert.ok(!allow('c'));
  time += 24 * 60 * 60 * 1000;
  assert.ok(allow('c'));
});

test('the health check works and never reveals the key', async () => {
  await withServer(() => {}, {}, async (post, port) => {
    const res = await fetch(`http://127.0.0.1:${port}/health`);
    const body = await res.text();
    assert.ok(!body.includes('test-key'));
    assert.deepStrictEqual(JSON.parse(body), { ok: true, aiConfigured: true });
  });
});
