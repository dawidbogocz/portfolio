// platformer.js -- Canvas-based procedural platformer game
// ES module with proper exports for Blazor JS interop

var canvas, ctx;
var gameRunning = false;
var animFrameId = null;
var keys = {};
var camera = { x: 0, y: 0 };
var player, platforms, coins;
var levelData = null;
var currentScore = 0;
var currentSeed = 0;
var gameOver = false;
var reachedEnd = false;
var collectedCoinIds = {};
var displayLeaderboard = false;
var leaderboardData = null;

// Physics constants
var GRAVITY = 0.5;
var JUMP_SPEED = -10;
var MOVE_SPEED = 4;
var FRICTION = 0.8;
var CANVAS_W = 800;
var CANVAS_H = 450;

// === PUBLIC API (called from Blazor) ===

export function start(canvasId) {
  canvas = document.getElementById(canvasId);
  if (!canvas) { console.error('Canvas not found:', canvasId); return; }
  canvas.width = CANVAS_W;
  canvas.height = CANVAS_H;
  ctx = canvas.getContext('2d');
  gameRunning = true;
  document.addEventListener('keydown', onKeyDown);
  document.addEventListener('keyup', onKeyUp);
}

export function stop() {
  gameRunning = false;
  if (animFrameId) cancelAnimationFrame(animFrameId);
  document.removeEventListener('keydown', onKeyDown);
  document.removeEventListener('keyup', onKeyUp);
}

export function loadLevel(canvasId, dataStr) {
  if (!canvas || canvas.id !== canvasId) {
    canvas = document.getElementById(canvasId);
    if (!canvas) { console.error('Canvas not found:', canvasId); return; }
    canvas.width = CANVAS_W;
    canvas.height = CANVAS_H;
    ctx = canvas.getContext('2d');
  }

  levelData = typeof dataStr === 'string' ? JSON.parse(dataStr) : dataStr;
  currentSeed = levelData.seed || 0;
  platforms = levelData.platforms ? levelData.platforms.slice() : [];
  coins = levelData.coins ? levelData.coins.slice() : [];
  collectedCoinIds = {};

  // Add ground as a platform (if not already present)
  var hasGround = platforms.some(function(p) { return p.isGround; });
  if (!hasGround) {
    platforms.push({ x: 0, y: levelData.height - 40, w: levelData.width || 4000, h: 40, isGround: true });
  }

  player = {
    x: levelData.startX || 50,
    y: levelData.startY || (levelData.height - 80),
    w: 24,
    h: 32,
    vx: 0,
    vy: 0,
    onGround: false
  };

  camera = { x: 0, y: 0 };
  currentScore = 0;
  gameOver = false;
  reachedEnd = false;
  displayLeaderboard = false;
  leaderboardData = null;
  gameRunning = true;

  if (animFrameId) cancelAnimationFrame(animFrameId);
  gameLoop();
}

export function restart() {
  if (levelData) {
    loadLevel(canvas ? canvas.id : '', JSON.stringify(levelData));
  }
}

export function getScore() { return currentScore; }

export function getSeed() { return currentSeed; }

export function isAlive() { return !gameOver && !reachedEnd; }

export function showLeaderboard(dataStr) {
  leaderboardData = typeof dataStr === 'string' ? JSON.parse(dataStr) : dataStr;
  displayLeaderboard = true;
}

// === INPUT ===

function onKeyDown(e) {
  keys[e.key] = true;
  if (e.key === 'r' || e.key === 'R') restart();
}

function onKeyUp(e) {
  keys[e.key] = false;
}

// === GAME LOOP ===

function gameLoop() {
  if (!gameRunning || !ctx) return;

  update();
  render();

  animFrameId = requestAnimationFrame(gameLoop);
}

function update() {
  if (gameOver || reachedEnd) return;

  // Horizontal movement
  var moveX = 0;
  if (keys['ArrowLeft'] || keys['a'] || keys['A']) moveX = -MOVE_SPEED;
  if (keys['ArrowRight'] || keys['d'] || keys['D']) moveX = MOVE_SPEED;

  player.vx = moveX * FRICTION;

  // Jump
  if ((keys['ArrowUp'] || keys['w'] || keys['W'] || keys[' ']) && player.onGround) {
    player.vy = JUMP_SPEED;
    player.onGround = false;
  }

  // Gravity
  player.vy += GRAVITY;
  if (player.vy > 15) player.vy = 15;

  // Move X
  player.x += player.vx;

  // Collision X
  for (var i = 0; i < platforms.length; i++) {
    var p = platforms[i];
    if (rectCollide(player, p)) {
      if (player.vx > 0) {
        player.x = p.x - player.w;
      } else if (player.vx < 0) {
        player.x = p.x + p.w;
      }
      player.vx = 0;
    }
  }

  // Move Y
  player.y += player.vy;
  player.onGround = false;

  // Collision Y
  for (var j = 0; j < platforms.length; j++) {
    var pl = platforms[j];
    if (rectCollide(player, pl)) {
      if (player.vy > 0) {
        // Landing on platform
        player.y = pl.y - player.h;
        player.vy = 0;
        player.onGround = true;
      } else if (player.vy < 0) {
        // Hitting head
        player.y = pl.y + pl.h;
        player.vy = 0;
      }
    }
  }

  // Coin collection
  for (var k = 0; k < coins.length; k++) {
    var coin = coins[k];
    var coinId = coin.id || k;
    if (!collectedCoinIds[coinId]) {
      var coinRect = { x: coin.x, y: coin.y, w: 16, h: 16 };
      if (rectCollide(player, coinRect)) {
        collectedCoinIds[coinId] = true;
        currentScore += 10;
      }
    }
  }

  // Camera follows player
  camera.x = player.x - CANVAS_W / 3;
  camera.y = player.y - CANVAS_H / 2;

  // Clamp camera
  if (camera.x < 0) camera.x = 0;
  if (camera.y < 0) camera.y = 0;
  var maxY = (levelData ? levelData.height : 450) - CANVAS_H;
  if (camera.y > maxY) camera.y = maxY;

  // Fall off screen = game over
  if (player.y > (levelData ? levelData.height : 450) + 100) {
    gameOver = true;
  }

  // Reached end
  if (levelData && player.x >= levelData.endX - 20 && player.x <= levelData.endX + 40) {
    reachedEnd = true;
  }
}

function rectCollide(a, b) {
  return a.x < b.x + b.w &&
         a.x + a.w > b.x &&
         a.y < b.y + b.h &&
         a.y + a.h > b.y;
}

// === RENDERING ===

function render() {
  var w = CANVAS_W, h = CANVAS_H;

  // Background
  ctx.fillStyle = '#0d1117';
  ctx.fillRect(0, 0, w, h);

  if (!levelData) return;

  ctx.save();
  ctx.translate(-camera.x, -camera.y);

  // Draw platforms
  for (var i = 0; i < platforms.length; i++) {
    var p = platforms[i];
    if (p.isGround) {
      ctx.fillStyle = '#1a2332';
      ctx.fillRect(p.x, p.y, p.w, p.h);
      ctx.fillStyle = '#2d4059';
      ctx.fillRect(p.x, p.y, p.w, 4);
    } else {
      ctx.fillStyle = '#2d4059';
      ctx.fillRect(p.x, p.y, p.w, p.h);
      ctx.fillStyle = '#58a6ff';
      ctx.fillRect(p.x, p.y, p.w, 3);
      ctx.strokeStyle = '#3b5998';
      ctx.lineWidth = 1;
      ctx.strokeRect(p.x, p.y, p.w, p.h);
    }
  }

  // Draw coins
  for (var k = 0; k < coins.length; k++) {
    var coin = coins[k];
    var coinId = coin.id || k;
    if (collectedCoinIds[coinId]) continue;

    ctx.fillStyle = '#e3b341';
    ctx.beginPath();
    ctx.arc(coin.x + 8, coin.y + 8, 7, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = '#f0d060';
    ctx.beginPath();
    ctx.arc(coin.x + 7, coin.y + 7, 4, 0, Math.PI * 2);
    ctx.fill();
  }

  // Draw end zone
  if (levelData.endX) {
    ctx.fillStyle = 'rgba(88, 166, 255, 0.15)';
    ctx.fillRect(levelData.endX - 10, 0, 40, levelData.height);
    ctx.fillStyle = '#58a6ff';
    ctx.font = '12px JetBrains Mono, monospace';
    ctx.fillText('GOAL', levelData.endX - 5, 30);
  }

  // Draw player
  ctx.fillStyle = '#58a6ff';
  ctx.fillRect(player.x, player.y, player.w, player.h);

  // Eyes
  ctx.fillStyle = '#0d1117';
  ctx.fillRect(player.x + 4, player.y + 8, 5, 5);
  ctx.fillRect(player.x + 14, player.y + 8, 5, 5);

  ctx.restore();

  // HUD (screen-space)
  ctx.fillStyle = '#8b949e';
  ctx.font = '14px JetBrains Mono, monospace';
  ctx.fillText('Score: ' + currentScore, 10, 25);
  ctx.fillText('Seed: ' + currentSeed, 10, 45);

  var totalCoins = coins.length;
  var collected = countKeys(collectedCoinIds);
  ctx.fillText('Coins: ' + collected + '/' + totalCoins, 10, 65);

  // Game over screen
  if (gameOver) {
    ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#ff6b6b';
    ctx.font = '32px JetBrains Mono, monospace';
    ctx.textAlign = 'center';
    ctx.fillText('GAME OVER', w / 2, h / 2 - 20);
    ctx.fillStyle = '#8b949e';
    ctx.font = '16px JetBrains Mono, monospace';
    ctx.fillText('Final Score: ' + currentScore, w / 2, h / 2 + 20);
    ctx.fillText('Press R to retry', w / 2, h / 2 + 50);
    ctx.textAlign = 'left';
  } else if (reachedEnd) {
    ctx.fillStyle = 'rgba(0, 0, 0, 0.7)';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#a6e22e';
    ctx.font = '32px JetBrains Mono, monospace';
    ctx.textAlign = 'center';
    ctx.fillText('LEVEL COMPLETE', w / 2, h / 2 - 20);
    ctx.fillStyle = '#8b949e';
    ctx.font = '16px JetBrains Mono, monospace';
    ctx.fillText('Score: ' + currentScore, w / 2, h / 2 + 20);
    ctx.fillText('Press R for new level', w / 2, h / 2 + 50);
    ctx.textAlign = 'left';
  }

  // Leaderboard overlay
  if (displayLeaderboard && leaderboardData) {
    ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#58a6ff';
    ctx.font = '20px JetBrains Mono, monospace';
    ctx.textAlign = 'center';
    ctx.fillText('-- LEADERBOARD --', w / 2, 40);
    ctx.fillStyle = '#8b949e';
    ctx.font = '14px JetBrains Mono, monospace';

    for (var i = 0; i < Math.min(leaderboardData.length, 5); i++) {
      var entry = leaderboardData[i];
      var date = entry.dateAchieved ? new Date(entry.dateAchieved).toLocaleDateString() : '--';
      ctx.fillText((i + 1) + '.  ' + entry.playerName + '  --  ' + entry.score + ' pts', w / 2, 70 + i * 25);
      ctx.font = '11px JetBrains Mono, monospace';
      ctx.fillText('seed: ' + entry.seed + '  (' + date + ')', w / 2, 85 + i * 25);
      ctx.font = '14px JetBrains Mono, monospace';
    }
    ctx.textAlign = 'left';
  }
}

function countKeys(obj) {
  var n = 0;
  for (var k in obj) { if (obj.hasOwnProperty(k)) n++; }
  return n;
}