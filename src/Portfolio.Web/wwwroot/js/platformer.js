// platformer.js -- Canvas-based procedural platformer game
// ES module with proper exports for Blazor JS interop

var canvas, ctx;
var gameRunning = false;
var animFrameId = null;
var keys = {};
var camera = { x: 0, y: 0 };
var player, platforms, coins, enemies, traps;
var levelData = null;
var currentScore = 0;
var currentSeed = 0;
var gameOver = false;
var reachedEnd = false;
var collectedCoinIds = {};
var displayLeaderboard = false;
var leaderboardData = null;
var killedCount = 0;
var stars = [];
var parallaxOffset = 0;

// Physics constants
var GRAVITY = 0.5;
var JUMP_SPEED = -10;
var MOVE_SPEED = 4;
var CANVAS_W = 800;
var CANVAS_H = 450;

// === STOMP BOUNCE ===
var stompMomentum = 0;
var STOMP_BOUNCE = -7;

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
  generateStars();
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
    generateStars();
  }

  levelData = typeof dataStr === 'string' ? JSON.parse(dataStr) : dataStr;
  currentSeed = levelData.seed || 0;
  platforms = levelData.platforms ? levelData.platforms.slice() : [];
  coins = levelData.coins ? levelData.coins.slice() : [];
  enemies = levelData.enemies ? levelData.enemies.slice() : [];
  traps = levelData.traps ? levelData.traps.slice() : [];
  collectedCoinIds = {};
  killedCount = 0;

  // Ensure ground is marked
  for (var i = 0; i < platforms.length; i++) {
    if (platforms[i].theme === 'ground') platforms[i].isGround = true;
  }

  player = {
    x: levelData.startX || 50,
    y: levelData.startY || 370,
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

export function getKills() { return killedCount; }

export function getSeed() { return currentSeed; }

export function isAlive() { return !gameOver && !reachedEnd; }

export function showLeaderboard(dataStr) {
  leaderboardData = typeof dataStr === 'string' ? JSON.parse(dataStr) : dataStr;
  displayLeaderboard = true;
}

// === STARS (parallax background) ===

function generateStars() {
  stars = [];
  for (var i = 0; i < 60; i++) {
    stars.push({
      x: Math.random() * 3000,
      y: Math.random() * CANVAS_H * 0.6,
      r: 0.5 + Math.random() * 1.5,
      brightness: 0.3 + Math.random() * 0.7
    });
  }
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

  // Horizontal movement (WASD only: A/D, no arrows)
  var moveX = 0;
  if (keys['a'] || keys['A']) moveX = -MOVE_SPEED;
  if (keys['d'] || keys['D']) moveX = MOVE_SPEED;

  player.vx = moveX * 0.8;

  // Jump (W or Space, no Up arrow)
  if ((keys['w'] || keys['W'] || keys[' ']) && player.onGround) {
    player.vy = stompMomentum !== 0 ? stompMomentum : JUMP_SPEED;
    player.onGround = false;
    stompMomentum = 0;
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

  // Collision Y with platforms
  for (var j = 0; j < platforms.length; j++) {
    var pl = platforms[j];
    if (rectCollide(player, pl)) {
      if (player.vy > 0) {
        player.y = pl.y - player.h;
        player.vy = 0;
        player.onGround = true;
      } else if (player.vy < 0) {
        player.y = pl.y + pl.h;
        player.vy = 0;
      }
    }
  }

  // Enemy AI and collision
  for (var e = 0; e < enemies.length; e++) {
    var enemy = enemies[e];
    if (enemy.dead) continue;

    // Patrol movement
    enemy.x += enemy.speed || 1;
    if (enemy.x >= (enemy.patrolEnd || enemy.x)) {
      enemy.speed = -Math.abs(enemy.speed);
    } else if (enemy.x <= (enemy.patrolStart || enemy.x - 40)) {
      enemy.speed = Math.abs(enemy.speed);
    }

    // Collision with player
    var eRect = { x: enemy.x, y: enemy.y, w: enemy.w || 28, h: enemy.h || 24 };
    if (rectCollide(player, eRect)) {
      // Stomp check: player falling and above enemy center
      if (player.vy > 0 && player.y + player.h - 8 < enemy.y + (enemy.h || 24) / 2) {
        // STOMP!
        enemy.dead = true;
        enemy.deathTimer = 0;
        killedCount++;
        currentScore += 25;
        stompMomentum = STOMP_BOUNCE;
        player.vy = STOMP_BOUNCE;
        player.onGround = false;
      } else {
        // Hit by enemy = game over
        gameOver = true;
        return;
      }
    }

    // Death animation timer
    if (enemy.dead) {
      enemy.deathTimer = (enemy.deathTimer || 0) + 1;
    }
  }

  // Trap collision
  for (var t = 0; t < traps.length; t++) {
    var trap = traps[t];
    var tRect = { x: trap.x, y: trap.y, w: trap.w, h: trap.h };
    if (rectCollide(player, tRect)) {
      gameOver = true;
      return;
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
  parallaxOffset = camera.x * 0.3;

  if (camera.x < 0) camera.x = 0;
  if (camera.y < 0) camera.y = 0;
  var maxY = (levelData ? levelData.height : 450) - CANVAS_H;
  if (camera.y > maxY) camera.y = maxY;

  // Fall off screen = game over
  if (player.y > (levelData ? levelData.height : 450) + 100) {
    gameOver = true;
  }

  // Reached end
  if (levelData && player.x >= levelData.endX - 20) {
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

  // Background gradient
  var grad = ctx.createLinearGradient(0, 0, 0, h);
  grad.addColorStop(0, '#0a0e17');
  grad.addColorStop(0.6, '#0d1117');
  grad.addColorStop(1, '#131a24');
  ctx.fillStyle = grad;
  ctx.fillRect(0, 0, w, h);

  // Parallax stars
  ctx.save();
  for (var s = 0; s < stars.length; s++) {
    var star = stars[s];
    var sx = (star.x - parallaxOffset) % (w + 100);
    if (sx < -10) sx += w + 100;
    ctx.globalAlpha = star.brightness * 0.6;
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(sx, star.y, star.r, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.restore();

  if (!levelData) return;

  ctx.save();
  ctx.translate(-camera.x, -camera.y);

  // Theme colors for platforms
  function getThemeColor(theme) {
    switch (theme) {
      case 'starter': return '#1b3a4b';
      case 'normal':  return '#2d4059';
      case 'challenge': return '#3d2a3a';
      case 'rest':    return '#1a3328';
      case 'end':     return '#1a3a3a';
      case 'ground':  return '#131d2e';
      default:        return '#1e2a3a';
    }
  }
  function getThemeAccent(theme) {
    switch (theme) {
      case 'starter': return '#4a9eff';
      case 'normal':  return '#58a6ff';
      case 'challenge': return '#ff4a6a';
      case 'rest':    return '#3dd68c';
      case 'end':     return '#ffcc00';
      case 'ground':  return '#2a3a55';
      default:        return '#58a6ff';
    }
  }

  // Draw platforms
  for (var i = 0; i < platforms.length; i++) {
    var p = platforms[i];
    var theme = p.theme || 'normal';
    ctx.fillStyle = getThemeColor(theme);
    ctx.fillRect(p.x, p.y, p.w, p.h);
    ctx.fillStyle = getThemeAccent(theme);
    ctx.fillRect(p.x, p.y, p.w, 3);
  }

  // Draw traps (spikes)
  for (var t = 0; t < traps.length; t++) {
    var trap = traps[t];
    var spikeCount = Math.floor(trap.w / 16);
    var spikeW = trap.w / spikeCount;
    ctx.fillStyle = '#e03a3a';
    for (var s = 0; s < spikeCount; s++) {
      ctx.beginPath();
      ctx.moveTo(trap.x + s * spikeW, trap.y + trap.h);
      ctx.lineTo(trap.x + s * spikeW + spikeW / 2, trap.y);
      ctx.lineTo(trap.x + (s + 1) * spikeW, trap.y + trap.h);
      ctx.closePath();
      ctx.fill();
    }
    ctx.fillStyle = '#ff6b6b';
    for (var s = 0; s < spikeCount; s++) {
      ctx.beginPath();
      ctx.moveTo(trap.x + s * spikeW + 2, trap.y + trap.h);
      ctx.lineTo(trap.x + s * spikeW + spikeW / 2, trap.y + 4);
      ctx.lineTo(trap.x + (s + 1) * spikeW - 2, trap.y + trap.h);
      ctx.closePath();
      ctx.fill();
    }
  }

  // Draw enemies
  for (var e = 0; e < enemies.length; e++) {
    var enemy = enemies[e];
    if (enemy.dead) {
      // Death squish animation (2 frames = ~33ms)
      var dt = enemy.deathTimer || 0;
      if (dt > 4) continue; // disappear after ~70ms
      ctx.fillStyle = '#8b0000';
      var squishH = (enemy.h || 24) * (1 - dt * 0.3);
      var squishY = (enemy.y || 0) + (enemy.h || 24) - squishH;
      ctx.fillRect(enemy.x, squishY, enemy.w || 28, squishH);
      continue;
    }

    var eW = enemy.w || 28;
    var eH = enemy.h || 24;

    // Body
    ctx.fillStyle = '#c0392b';
    ctx.fillRect(enemy.x, enemy.y, eW, eH);

    // Darker bottom
    ctx.fillStyle = '#922b21';
    ctx.fillRect(enemy.x, enemy.y + eH - 6, eW, 6);

    // Eyes (looking in direction of movement)
    ctx.fillStyle = '#f1c40f';
    var eyeDir = (enemy.speed || 0) > 0 ? 4 : 0;
    ctx.fillRect(enemy.x + 4 + eyeDir, enemy.y + 6, 5, 5);
    ctx.fillRect(enemy.x + eW - 9 + eyeDir, enemy.y + 6, 5, 5);
    ctx.fillStyle = '#000';
    ctx.fillRect(enemy.x + 5 + eyeDir, enemy.y + 7, 3, 3);
    ctx.fillRect(enemy.x + eW - 8 + eyeDir, enemy.y + 7, 3, 3);

    // Feet
    ctx.fillStyle = '#7b241c';
    ctx.fillRect(enemy.x + 2, enemy.y + eH - 2, 8, 2);
    ctx.fillRect(enemy.x + eW - 10, enemy.y + eH - 2, 8, 2);
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

  // End zone
  if (levelData.endX) {
    var pulse = Math.sin(Date.now() / 400) * 0.08 + 0.12;
    ctx.fillStyle = 'rgba(255, 204, 0, ' + pulse + ')';
    ctx.fillRect(levelData.endX - 10, 0, 40, levelData.height);
    ctx.fillStyle = '#ffcc00';
    ctx.font = '12px JetBrains Mono, monospace';
    ctx.fillText('GOAL', levelData.endX - 5, 30);
  }

  // Draw player
  ctx.fillStyle = '#58a6ff';
  ctx.fillRect(player.x, player.y, player.w, player.h);
  ctx.fillStyle = '#0d1117';
  ctx.fillRect(player.x + 4, player.y + 8, 5, 5);
  ctx.fillRect(player.x + 14, player.y + 8, 5, 5);

  ctx.restore();

  // HUD (screen-space)
  ctx.fillStyle = '#8b949e';
  ctx.font = '13px JetBrains Mono, monospace';
  ctx.fillText('Score: ' + currentScore, 10, 22);
  ctx.fillText('Kills: ' + killedCount, 10, 40);

  var totalCoins = coins.length;
  var collected = countKeys(collectedCoinIds);
  ctx.fillText('Coins: ' + collected + '/' + totalCoins, 10, 58);

  // Game over screen
  if (gameOver) {
    ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#ff4a6a';
    ctx.font = '28px JetBrains Mono, monospace';
    ctx.textAlign = 'center';
    ctx.fillText('GAME OVER', w / 2, h / 2 - 30);
    ctx.fillStyle = '#8b949e';
    ctx.font = '16px JetBrains Mono, monospace';
    ctx.fillText('Score: ' + currentScore + '  Kills: ' + killedCount, w / 2, h / 2 + 5);
    ctx.fillText('[R]etry  /  [N]ew Level', w / 2, h / 2 + 35);
    ctx.textAlign = 'left';
  } else if (reachedEnd) {
    ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#3dd68c';
    ctx.font = '28px JetBrains Mono, monospace';
    ctx.textAlign = 'center';
    ctx.fillText('LEVEL COMPLETE', w / 2, h / 2 - 30);
    ctx.fillStyle = '#8b949e';
    ctx.font = '16px JetBrains Mono, monospace';
    ctx.fillText('Score: ' + currentScore + '  Kills: ' + killedCount, w / 2, h / 2 + 5);
    ctx.fillText('[R]etry  /  [N]ew Level', w / 2, h / 2 + 35);
    ctx.textAlign = 'left';
  }

  // Leaderboard overlay
  if (displayLeaderboard && leaderboardData) {
    ctx.fillStyle = 'rgba(0, 0, 0, 0.85)';
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#58a6ff';
    ctx.font = '18px JetBrains Mono, monospace';
    ctx.textAlign = 'center';
    ctx.fillText('TOP SCORES', w / 2, 35);

    for (var i = 0; i < Math.min(leaderboardData.length, 5); i++) {
      var entry = leaderboardData[i];
      ctx.fillStyle = i === 0 ? '#ffcc00' : (i === 1 ? '#c0c0c0' : (i === 2 ? '#cd7f32' : '#8b949e'));
      ctx.font = '15px JetBrains Mono, monospace';
      ctx.fillText('#' + (i + 1) + '  ' + entry.playerName, w / 2 - 120, 80 + i * 28);
      ctx.fillStyle = '#8b949e';
      ctx.fillText(String(entry.score) + ' pts', w / 2 + 80, 80 + i * 28);
    }

    ctx.fillStyle = '#555';
    ctx.font = '12px JetBrains Mono, monospace';
    ctx.fillText('[R]etry', w / 2, 80 + 5 * 28 + 20);
    ctx.textAlign = 'left';
  }
}

function countKeys(obj) {
  var n = 0;
  for (var k in obj) { if (obj.hasOwnProperty(k)) n++; }
  return n;
}