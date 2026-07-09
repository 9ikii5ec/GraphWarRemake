const MathParser = {
    _pos: 0, _expr: '', _xValue: 0,

    evaluate(formula, x) {
        this._expr = formula.replace(/\s+/g, '').toLowerCase();
        this._xValue = x;
        this._pos = 0;
        const result = this._parseExpression();
        if (this._pos < this._expr.length) throw new Error('Unexpected: ' + this._expr.slice(this._pos));
        return result;
    },

    _parseExpression() {
        let r = this._parseTerm();
        while (this._pos < this._expr.length) {
            const op = this._expr[this._pos];
            if (op === '+' || op === '-') { this._pos++; r = op === '+' ? r + this._parseTerm() : r - this._parseTerm(); }
            else break;
        }
        return r;
    },

    _parseTerm() {
        let r = this._parsePower();
        while (this._pos < this._expr.length) {
            const op = this._expr[this._pos];
            if (op === '*' || op === '/') { this._pos++; const right = this._parsePower(); r = op === '/' ? r / right : r * right; }
            else break;
        }
        return r;
    },

    _parsePower() {
        let r = this._parseUnary();
        if (this._pos < this._expr.length && this._expr[this._pos] === '^') {
            this._pos++; r = Math.pow(r, this._parseUnary());
        }
        return r;
    },

    _parseUnary() {
        if (this._pos < this._expr.length && this._expr[this._pos] === '-') { this._pos++; return -this._parseUnary(); }
        if (this._pos < this._expr.length && this._expr[this._pos] === '+') { this._pos++; return this._parseUnary(); }
        return this._parsePrimary();
    },

    _parsePrimary() {
        if (this._pos >= this._expr.length) throw new Error('Unexpected end');
        if (this._expr[this._pos] === '(') { this._pos++; const r = this._parseExpression(); this._expect(')'); return r; }
        if (this._isDigit(this._expr[this._pos]) || this._expr[this._pos] === '.') return this._parseNumber();
        if (this._expr[this._pos] === 'x') { this._pos++; return this._xValue; }
        if (this._matchKeyword('pi')) return Math.PI;
        if (this._matchKeyword('e')) return Math.E;
        const f = this._tryParseFunction();
        if (f !== null) return f;
        throw new Error('Unexpected: ' + this._expr[this._pos]);
    },

    _parseNumber() {
        const s = this._pos;
        while (this._pos < this._expr.length && (this._isDigit(this._expr[this._pos]) || this._expr[this._pos] === '.')) this._pos++;
        return parseFloat(this._expr.slice(s, this._pos));
    },

    _matchKeyword(kw) {
        if (this._pos + kw.length > this._expr.length) return false;
        if (this._expr.slice(this._pos, this._pos + kw.length) !== kw) return false;
        if (this._pos + kw.length < this._expr.length && this._isLetter(this._expr[this._pos + kw.length])) return false;
        this._pos += kw.length;
        return true;
    },

    _tryParseFunction() {
        const funcs = ['sin','cos','tan','sqrt','abs','log','ln'];
        for (const fn of funcs) {
            if (this._pos + fn.length > this._expr.length) continue;
            if (this._expr.slice(this._pos, this._pos + fn.length) !== fn) continue;
            this._pos += fn.length; this._expect('('); const a = this._parseExpression(); this._expect(')');
            if (fn === 'sin') return Math.sin(a);
            if (fn === 'cos') return Math.cos(a);
            if (fn === 'tan') return Math.tan(a);
            if (fn === 'sqrt') return Math.sqrt(a);
            if (fn === 'abs') return Math.abs(a);
            if (fn === 'log') return Math.log10(a);
            if (fn === 'ln') return Math.log(a);
        }
        return null;
    },

    _expect(c) {
        if (this._pos >= this._expr.length || this._expr[this._pos] !== c) throw new Error('Expected ' + c);
        this._pos++;
    },

    tryValidate(formula) {
        try { this.evaluate(formula, 0); return { valid: true, error: null }; }
        catch (ex) { return { valid: false, error: ex.message }; }
    },

    _isDigit(ch) { return ch >= '0' && ch <= '9'; },
    _isLetter(ch) { return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z'); }
};
