class TurnManager {
    constructor(players, teams, mode) {
        this._players = players; this._teams = teams; this._mode = mode;
        this._currentPlayerIndex = 0; this._currentTurnSeconds = 0; this._isGameStarted = false;
        this.TURN_TIME_LIMIT = 60;
        this.currentPlayer = players.find(p => p.isAlive) || players[0];
        this.onTurnChanged = null; this.onTimerTick = null; this.onGameOver = null;
    }
    startGame() { this._isGameStarted = true; this._currentPlayerIndex = 0; this.currentPlayer = this._players.find(p => p.isAlive); this._currentTurnSeconds = this.TURN_TIME_LIMIT; if (this.onTurnChanged) this.onTurnChanged(this.currentPlayer); }
    tick() { if (!this._isGameStarted) return; this._currentTurnSeconds--; if (this.onTimerTick) this.onTimerTick(this._currentTurnSeconds); if (this._currentTurnSeconds <= 0) this.endTurn(); }
    endTurn() {
        if (!this._isGameStarted) return;
        if (this.checkGameOver()) { if (this.onGameOver) this.onGameOver(); return; }
        this._advance(); this._currentTurnSeconds = this.TURN_TIME_LIMIT;
        if (this.onTurnChanged) this.onTurnChanged(this.currentPlayer);
    }
    _advance() { this._mode === GameMode.TeamDeathmatch ? this._advanceTeam() : this._advanceDM(); }
    _advanceDM() {
        const s = this._currentPlayerIndex;
        do { this._currentPlayerIndex = (this._currentPlayerIndex + 1) % this._players.length; }
        while (!this._players[this._currentPlayerIndex].isAlive && this._currentPlayerIndex !== s);
        this.currentPlayer = this._players[this._currentPlayerIndex];
    }
    _advanceTeam() {
        const ct = this._teams.find(t => t.players.some(p => p.id === this.currentPlayer.id));
        if (!ct) { this._advanceDM(); return; }
        const next = ct.nextAlivePlayer(this.currentPlayer);
        if (next) { this.currentPlayer = next; this._currentPlayerIndex = this._players.findIndex(p => p.id === next.id); return; }
        const ti = this._teams.indexOf(ct);
        for (let i = 1; i <= this._teams.length; i++) {
            const nt = this._teams[(ti + i) % this._teams.length];
            if (nt.isDefeated) continue;
            const n = nt.nextAlivePlayer(null);
            if (n) { this.currentPlayer = n; this._currentPlayerIndex = this._players.findIndex(p => p.id === n.id); return; }
        }
    }
    checkGameOver() { return this._mode === GameMode.Deathmatch ? this._players.filter(p => p.isAlive).length <= 1 : this._teams.filter(t => !t.isDefeated).length <= 1; }
    setCurrentPlayer(id) { const p = this._players.find(p => p.id === id); if (p && p.isAlive) { this.currentPlayer = p; this._currentPlayerIndex = this._players.findIndex(x => x.id === id); this._currentTurnSeconds = this.TURN_TIME_LIMIT; if (this.onTurnChanged) this.onTurnChanged(this.currentPlayer); } }
}
