class GameService {
    constructor() {
        this._collision = new CollisionManager(); this._turnManager = null;
        this._state = GameStateType.Lobby; this._players = []; this._teams = [];
        this._mode = GameMode.Deathmatch; this._worldWidth = 1200; this._worldHeight = 600;
        this.activeProjectile = null;
        this.onProjectileFired = null; this.onPlayerDamaged = null; this.onPlayerDied = null;
        this.onGameOver = null; this.onTurnChanged = null; this.onTimerTick = null;
    }
    get state() { return this._state; }
    get players() { return this._players; }
    get teams() { return this._teams; }
    get currentPlayer() { return this._turnManager ? this._turnManager.currentPlayer : null; }
    setWorldBounds(w, h) { this._worldWidth = w; this._worldHeight = h; this._collision.worldBounds = { x: 0, y: 0, width: w, height: h }; }
    initializeGame(players, teams, mode) {
        this._players = players; this._teams = teams; this._mode = mode; this._state = GameStateType.Playing;
        this._turnManager = new TurnManager(this._players, this._teams, this._mode);
        this._turnManager.onTurnChanged = p => { if (this.onTurnChanged) this.onTurnChanged(p); };
        this._turnManager.onTimerTick = t => { if (this.onTimerTick) this.onTimerTick(t); };
        this._turnManager.onGameOver = () => { this._state = GameStateType.GameOver; if (this.onGameOver) this.onGameOver(); };
        this._arrange(); this._turnManager.startGame();
    }
    _arrange() { const n = this._players.length; for (let i = 0; i < n; i++) { this._players[i].x = (this._worldWidth / (n + 1)) * (i + 1); this._players[i].y = this._worldHeight - 60; } }
    fire(formula, direction) {
        if (this._state !== GameStateType.Playing || this.activeProjectile || !this._turnManager?.currentPlayer) return;
        const p = this._turnManager.currentPlayer;
        this.activeProjectile = new Projectile(p.id, formula, p.x, p.y, direction);
        if (this.onProjectileFired) this.onProjectileFired(this.activeProjectile);
    }
    update() {
        if (this._state !== GameStateType.Playing || !this.activeProjectile?.isActive) return;
        const p = this.activeProjectile; p.x += p.stepX * p.direction;
        try { p.y = p.startY + MathParser.evaluate(p.formula, p.x - p.startX); } catch { p.y += 5; }
        if (this._collision.isOutOfBounds(p) || this._collision.hitsObstacle(p) || this._collision.hitPlayer(p, this._players) || p.hasTraveledMaxRange) this._explode(p.x, p.y, p);
    }
    _explode(x, y, p) {
        p.isActive = false;
        for (const pl of this._collision.getPlayersInRadius(x, y, p.explosionRadius, this._players)) {
            const d = Math.hypot(pl.x - x, pl.y - y), dmg = p.damage * (1 - (d / p.explosionRadius) * 0.5);
            pl.health -= dmg; if (this.onPlayerDamaged) this.onPlayerDamaged(pl, dmg, x); if (!pl.isAlive && this.onPlayerDied) this.onPlayerDied(pl);
        }
        this.activeProjectile = null; if (this._turnManager) this._turnManager.endTurn();
    }
    getSnapshot() {
        const s = new GameSnapshot();
        s.players = this._players.map(p => ({ id: p.id, name: p.name, teamId: p.teamId, x: p.x, y: p.y, health: p.health, isAlive: p.isAlive }));
        s.currentPlayerId = this._turnManager?.currentPlayer?.id ?? 0; s.turnTimeRemaining = this._turnManager?._currentTurnSeconds ?? 0; s.state = this._state; return s;
    }
    applySnapshot(s) { for (const ps of s.players) { const p = this._players.find(x => x.id === ps.id); if (p) { p.x = ps.x; p.y = ps.y; p.health = ps.health; p.isAlive = ps.isAlive; } } this._turnManager?.setCurrentPlayer(s.currentPlayerId); }
    tickTurnTimer() { this._turnManager?.tick(); }
}
