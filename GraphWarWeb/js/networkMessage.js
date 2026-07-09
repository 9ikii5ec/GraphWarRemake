const MessageType = Object.freeze({ Welcome:'Welcome',GameStart:'GameStart',TurnChanged:'TurnChanged',ProjectileFired:'ProjectileFired',DamageDealt:'DamageDealt',PlayerDied:'PlayerDied',GameOver:'GameOver',Snapshot:'Snapshot',JoinRequest:'JoinRequest',FireCommand:'FireCommand',Ping:'Ping',Pong:'Pong',Error:'Error' });

class NetworkMessage {
    constructor(type, senderId = 0, payload = '') { this.type = type; this.senderId = senderId; this.payload = payload; this.timestamp = Date.now(); }
    serialize() { return JSON.stringify(this); }
    static deserialize(json) { try { return JSON.parse(json); } catch { return null; } }
}

class FirePayload { constructor(f = '', d = 1, x = 0, y = 0) { this.formula = f; this.direction = d; this.playerX = x; this.playerY = y; } }
class DamagePayload { constructor(t = 0, d = 0, x = 0, y = 0) { this.targetId = t; this.damage = d; this.explosionX = x; this.explosionY = y; } }
