class NetworkManager {
    constructor() {
        this.isHost = false; this.myId = 0; this._ws = null; this._clients = new Map(); this._nextClientId = 1; this._server = null;
        this.onMessageReceived = null; this.onClientConnected = null; this.onClientDisconnected = null; this.onLog = null;
    }
    get isConnected() { return this.isHost || (this._ws && this._ws.readyState === WebSocket.OPEN); }

    async startHost(port) {
        this.isHost = true; this.myId = 0;
        return new Promise((resolve, reject) => {
            try {
                this._server = new WebSocket.Server({ port }, () => { this._log(`Server on port ${port}`); resolve(); });
                this._server.on('connection', ws => this._handleClient(ws));
                this._server.on('error', err => { this._log(`Error: ${err.message}`); reject(err); });
            } catch (err) { reject(err); }
        });
    }
    _handleClient(ws) {
        const id = this._nextClientId++;
        this._clients.set(id, ws);
        this._log(`Client connected: ID=${id}`);
        if (this.onClientConnected) this.onClientConnected(id);
        ws.send(JSON.stringify({ type: 'Welcome', senderId: 0, payload: id.toString(), timestamp: Date.now() }) + '\n');
        ws.on('message', data => {
            for (const line of data.toString().split('\n')) {
                if (!line.trim()) continue;
                const msg = NetworkMessage.deserialize(line);
                if (msg) { msg.senderId = id; if (this.onMessageReceived) this.onMessageReceived(msg, id); }
            }
        });
        ws.on('close', () => { this._clients.delete(id); this._log(`Client disconnected: ID=${id}`); if (this.onClientDisconnected) this.onClientDisconnected(id); });
    }
    async broadcast(msg) { if (!this.isHost) return; for (const [, ws] of this._clients) { try { this._send(ws, msg); } catch {} } }
    async sendTo(clientId, msg) { const ws = this._clients.get(clientId); if (ws) this._send(ws, msg); }
    _send(ws, msg) { if (ws.readyState === WebSocket.OPEN) ws.send(msg.serialize() + '\n'); }

    async connectToHost(host, port) {
        this.isHost = false;
        return new Promise((resolve, reject) => {
            this._ws = new WebSocket(`ws://${host}:${port}`);
            this._ws.onopen = () => { this._log(`Connected to ${host}:${port}`); resolve(); };
            this._ws.onmessage = e => {
                for (const line of e.data.split('\n')) {
                    if (!line.trim()) continue;
                    const msg = NetworkMessage.deserialize(line);
                    if (msg) { if (msg.type === MessageType.Welcome) this.myId = parseInt(msg.payload, 10); if (this.onMessageReceived) this.onMessageReceived(msg, 0); }
                }
            };
            this._ws.onclose = () => this._log('Connection lost.');
            this._ws.onerror = err => { this._log(`Error: ${err.message||'unknown'}`); reject(err); };
        });
    }
    async sendToHost(msg) { if (this.isHost || !this._ws) return; this._send(this._ws, msg); }
    _log(msg) { if (this.onLog) this.onLog(`[Network] ${msg}`); }
    dispose() { this._server?.close(); this._ws?.close(); for (const [, ws] of this._clients) ws.close(); this._clients.clear(); }
}
