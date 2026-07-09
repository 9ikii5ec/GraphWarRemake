class CollisionManager {
    constructor() { this.obstacles = []; this.worldBounds = { x: 0, y: 0, width: 1200, height: 600 }; }
    _rectsOverlap(a, b) { return a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y; }
    isOutOfBounds(p) { return p.x < this.worldBounds.x || p.x > this.worldBounds.x + this.worldBounds.width || p.y < this.worldBounds.y || p.y > this.worldBounds.y + this.worldBounds.height; }
    hitsObstacle(p) { const r = { x: p.x - 3, y: p.y - 3, width: 6, height: 6 }; return this.obstacles.some(o => this._rectsOverlap(r, o)); }
    hitPlayer(p, players) {
        const pr = { x: p.x - 3, y: p.y - 3, width: 6, height: 6 };
        for (const pl of players) {
            if (pl.id === p.ownerId || !pl.isAlive) continue;
            if (this._rectsOverlap(pr, { x: pl.x - pl.hitboxWidth / 2, y: pl.y - pl.hitboxHeight / 2, width: pl.hitboxWidth, height: pl.hitboxHeight })) return pl;
        }
        return null;
    }
    getPlayersInRadius(x, y, radius, players) { return players.filter(p => p.isAlive && Math.hypot(p.x - x, p.y - y) <= radius); }
}
