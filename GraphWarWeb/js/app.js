const GameUI = {
    _gameService: null, _networkManager: null, _canvas: null, _ctx: null,
    _connectionType: ConnectionType.Local, _isMyTurn: true, _gameLoopId: null, _turnTimerInterval: null,
    _playerNameBoxes: [], _players: [], _logVisible: false,

    init() {
        this._canvas = document.getElementById('gameCanvas');
        this._ctx = this._canvas.getContext('2d');
        this._resizeCanvas();
        window.addEventListener('resize', () => this._resizeCanvas());
    },
    _resizeCanvas() { const c = this._canvas.parentElement; this._canvas.width = c.clientWidth; this._canvas.height = c.clientHeight; },

    addPlayerEntry(name = '') {
        const idx = this._players.length + 1, n = name || `Player ${idx}`;
        this._players.push({ name: n, teamId: 0 });
        const container = document.getElementById('playerList'), row = document.createElement('div');
        row.className = 'player-entry';
        row.innerHTML = `<input type="text" value="${n}" class="player-name-input" data-idx="${this._players.length - 1}"/><button class="btn-remove" onclick="GameUI.removePlayer(${this._players.length - 1})">X</button>`;
        container.appendChild(row);
        const input = row.querySelector('.player-name-input');
        input.addEventListener('input', e => { const i = +e.target.dataset.idx; if (i < this._players.length) this._players[i].name = e.target.value; });
        this._playerNameBoxes.push(input);
    },
    removePlayer(idx) { if (this._players.length <= 2) return; this._players.splice(idx, 1); this._rebuildPlayerList(); },
    _rebuildPlayerList() {
        const c = document.getElementById('playerList'); c.innerHTML = ''; this._playerNameBoxes = [];
        for (let i = 0; i < this._players.length; i++) {
            const p = this._players[i], row = document.createElement('div'); row.className = 'player-entry';
            row.innerHTML = `<input type="text" value="${p.name}" class="player-name-input" data-idx="${i}"/><button class="btn-remove" onclick="GameUI.removePlayer(${i})">X</button>`;
            c.appendChild(row);
            const input = row.querySelector('.player-name-input');
            input.addEventListener('input', e => { const j = +e.target.dataset.idx; if (j < this._players.length) this._players[j].name = e.target.value; });
            this._playerNameBoxes.push(input);
        }
    },
    onConnectionTypeChanged() { document.getElementById('networkPanel').style.display = document.getElementById('connectionCombo').selectedIndex > 0 ? 'flex' : 'none'; },

    async startGame() {
        for (let i = 0; i < this._playerNameBoxes.length && i < this._players.length; i++) this._players[i].name = this._playerNameBoxes[i].value;
        const mode = document.getElementById('modeCombo').selectedIndex === 0 ? GameMode.Deathmatch : GameMode.TeamDeathmatch;
        const connIdx = document.getElementById('connectionCombo').selectedIndex;
        this._connectionType = connIdx === 0 ? ConnectionType.Local : connIdx === 1 ? ConnectionType.Host : ConnectionType.Client;
        const players = this._players.map((p, i) => new Player(i + 1, p.name, p.teamId));
        let teams = [];
        if (mode === GameMode.TeamDeathmatch) { const g = {}; for (const p of players) { if (!g[p.teamId]) g[p.teamId] = new Team(p.teamId, `Team ${p.teamId + 1}`); g[p.teamId].players.push(p); } teams = Object.values(g); }
        document.getElementById('lobbyScreen').style.display = 'none';
        document.getElementById('gameScreen').style.display = 'flex';
        this._resizeCanvas();
        this._gameService = new GameService(); this._setEvents();
        this._gameService.setWorldBounds(this._canvas.width, this._canvas.height);
        if (this._connectionType === ConnectionType.Local) { this._gameService.initializeGame(players, teams, mode); this._startLoop(); }
        else if (this._connectionType === ConnectionType.Host) {
            const port = parseInt(document.getElementById('portInput').value, 10) || 54000;
            this._networkManager = new NetworkManager(); this._networkManager.onMessageReceived = (m, s) => this._handleNet(m, s); this._networkManager.onLog = m => this._updateStatus(m);
            await this._networkManager.startHost(port); this._gameService.initializeGame(players, teams, mode); this._startLoop(); this._updateStatus('Host started.');
        } else {
            const host = document.getElementById('hostAddressInput').value || '127.0.0.1', port = parseInt(document.getElementById('portInput').value, 10) || 54000;
            this._networkManager = new NetworkManager(); this._networkManager.onMessageReceived = (m, s) => this._handleNet(m, s); this._networkManager.onLog = m => this._updateStatus(m);
            try { await this._networkManager.connectToHost(host, port); this._updateStatus(`Connected. ID: ${this._networkManager.myId}`); } catch (e) { this._updateStatus(`Error: ${e.message}`); return; }
        }
    },

    _setEvents() {
        const gs = this._gameService;
        gs.onPlayerDamaged = p => this._log(`${p.name} took damage!`);
        gs.onPlayerDied = p => this._log(`${p.name} destroyed!`);
        gs.onGameOver = () => { document.getElementById('fireButton').disabled = true; const w = gs.players.find(p => p.isAlive); this._updateStatus(w ? `Winner: ${w.name}` : 'Draw!'); this._log('=== GAME OVER ==='); };
        gs.onTurnChanged = p => {
            document.getElementById('playerNameText').textContent = p.name;
            document.getElementById('healthText').textContent = Math.round(p.health);
            document.getElementById('healthBar').value = p.health;
            document.getElementById('fireButton').disabled = false;
            document.getElementById('turnTimer').textContent = '60';
            if (this._connectionType === ConnectionType.Client) { this._isMyTurn = this._networkManager?.myId === p.id; document.getElementById('fireButton').disabled = !this._isMyTurn; }
            this._log(`Turn: ${p.name}`);
        };
        gs.onTimerTick = s => { document.getElementById('turnTimer').textContent = s.toString(); };
    },
    _startLoop() {
        const loop = () => { this._gameService.update(); this._draw(); this._gameLoopId = requestAnimationFrame(loop); };
        this._gameLoopId = requestAnimationFrame(loop);
        this._turnTimerInterval = setInterval(() => { if (this._gameService.state === GameStateType.Playing) this._gameService.tickTurnTimer(); }, 1000);
    },

    fire() {
        if (!this._isMyTurn || this._gameService.state !== GameStateType.Playing) return;
        const input = document.getElementById('formulaInput'), formula = input.value.trim();
        if (!formula) return;
        const v = MathParser.tryValidate(formula); if (!v.valid) { this._updateStatus(`Error: ${v.error}`); return; }
        const cur = this._gameService.currentPlayer; if (!cur) return;
        const dir = cur.x < this._canvas.width / 2 ? 1 : -1;
        if (this._connectionType === ConnectionType.Local || this._connectionType === ConnectionType.Host) {
            this._gameService.fire(formula, dir);
            if (this._connectionType === ConnectionType.Host && this._networkManager) this._networkManager.broadcast(new NetworkMessage(MessageType.ProjectileFired, 0, JSON.stringify(new FirePayload(formula, dir, cur.x, cur.y))));
        } else { this._networkManager?.sendToHost(new NetworkMessage(MessageType.FireCommand, this._networkManager.myId, JSON.stringify(new FirePayload(formula, dir, cur.x, cur.y)))); }
        input.value = ''; document.getElementById('fireButton').disabled = true;
    },

    _handleNet(msg) {
        switch (msg.type) {
            case MessageType.Welcome: this._updateStatus(`ID: ${msg.payload}`); break;
            case MessageType.FireCommand: if (this._connectionType === ConnectionType.Host) { const p = JSON.parse(msg.payload), pl = this._gameService.players.find(x => x.id === msg.senderId); if (pl && this._gameService.currentPlayer?.id === pl.id) { this._gameService.fire(p.formula, p.direction); this._networkManager?.broadcast(new NetworkMessage(MessageType.ProjectileFired, 0, msg.payload)); } } break;
            case MessageType.ProjectileFired: if (this._connectionType === ConnectionType.Client) { const p = JSON.parse(msg.payload); this._gameService.fire(p.formula, p.direction); } break;
            case MessageType.Snapshot: if (this._connectionType === ConnectionType.Client) this._gameService.applySnapshot(JSON.parse(msg.payload)); break;
        }
    },

    _draw() {
        const ctx = this._ctx, w = this._canvas.width, h = this._canvas.height;
        if (w <= 0 || h <= 0) return;
        ctx.clearRect(0, 0, w, h); ctx.fillStyle = '#181825'; ctx.fillRect(0, 0, w, h);
        ctx.fillStyle = '#696969'; ctx.fillRect(0, h - 60, w, 60);
        const obs = [{ x: w * .25, y: h - 160, w: 80, h: 100 }, { x: w * .5, y: h - 200, w: 60, h: 140 }, { x: w * .75, y: h - 140, w: 100, h: 80 }];
        for (const o of obs) { ctx.fillStyle = '#808080'; ctx.fillRect(o.x, o.y, o.w, o.h); }
        for (const p of this._gameService.players) {
            const dy = h - 60 - p.y - 40;
            ctx.fillStyle = !p.isAlive ? '#CD5C5C' : this._gameService.currentPlayer?.id === p.id ? '#90EE90' : '#6495ED';
            ctx.fillRect(p.x - 15, dy, 30, 40);
            ctx.fillStyle = '#CDD6F4'; ctx.font = '10px sans-serif'; ctx.textAlign = 'center'; ctx.fillText(p.name, p.x, dy - 18);
            if (p.isAlive) { const r = p.health / 100; ctx.fillStyle = '#404040'; ctx.fillRect(p.x - 15, dy - 6, 30, 4); ctx.fillStyle = r > .5 ? '#90EE90' : r > .25 ? '#FFFF00' : '#FF0000'; ctx.fillRect(p.x - 15, dy - 6, 30 * r, 4); }
        }
        if (this._gameService.activeProjectile?.isActive) { const p = this._gameService.activeProjectile; ctx.fillStyle = '#FFD700'; ctx.beginPath(); ctx.arc(p.x, h - 60 - p.y, 4, 0, Math.PI * 2); ctx.fill(); }
    },

    _updateStatus(t) { document.getElementById('statusOverlay').textContent = t; },
    _log(t) { const el = document.getElementById('logText'); el.textContent += t + '\n'; el.parentElement.scrollTop = el.parentElement.scrollHeight; },
    toggleLog() { this._logVisible = !this._logVisible; document.getElementById('logPanel').style.display = this._logVisible ? 'block' : 'none'; },
    goToLobby() { if (this._gameLoopId) cancelAnimationFrame(this._gameLoopId); if (this._turnTimerInterval) clearInterval(this._turnTimerInterval); this._networkManager?.dispose(); document.getElementById('gameScreen').style.display = 'none'; document.getElementById('lobbyScreen').style.display = 'flex'; }
};
