const GameMode = Object.freeze({ Deathmatch: 0, TeamDeathmatch: 1 });
const ConnectionType = Object.freeze({ Local: 0, Host: 1, Client: 2 });
const GameStateType = Object.freeze({ Lobby: 0, Playing: 1, GameOver: 2 });

class Player {
    constructor(id, name, teamId = 0) {
        this.id = id; this.name = name; this.teamId = teamId;
        this.x = 0; this.y = 0; this._health = 100; this.isAlive = true;
        this.hitboxWidth = 30; this.hitboxHeight = 40;
    }
    set health(val) { this._health = Math.max(0, Math.min(100, val)); this.isAlive = this._health > 0; }
    get health() { return this._health; }
}

class Projectile {
    constructor(ownerId, formula, x, y, direction) {
        this.ownerId = ownerId; this.formula = formula;
        this.x = x; this.y = y; this.startX = x; this.startY = y;
        this.direction = direction; this.stepX = 3.0; this.maxRange = 800;
        this.damage = 25; this.explosionRadius = 40; this.isActive = true;
    }
    get hasTraveledMaxRange() { return Math.abs(this.x - this.startX) >= this.maxRange; }
}

class Team {
    constructor(id, name) { this.id = id; this.name = name; this.players = []; }
    get isDefeated() { return this.players.every(p => !p.isAlive); }
    nextAlivePlayer(current) {
        const alive = this.players.filter(p => p.isAlive);
        if (!alive.length) return null;
        if (!current) return alive[0];
        const idx = alive.findIndex(p => p.id === current.id);
        return alive[(idx + 1) % alive.length];
    }
}

class GameSnapshot { constructor() { this.players = []; this.currentPlayerId = 0; this.turnTimeRemaining = 0; this.state = GameStateType.Playing; } }
class PlayerSnapshot { constructor() { this.id = 0; this.name = ''; this.teamId = 0; this.x = 0; this.y = 0; this.health = 100; this.isAlive = true; } }
