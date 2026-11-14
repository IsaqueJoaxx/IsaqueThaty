using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace Projeta.Core.Expressoes;

/// <summary>Valor de uma expressão: número ou texto.</summary>
public readonly struct Valor : IEquatable<Valor>
{
    public readonly double Num;
    public readonly string? Str;
    public bool EhTexto => Str is not null;

    public Valor(double n) { Num = n; Str = null; }
    public Valor(string s) { Num = double.NaN; Str = s; }

    public static implicit operator Valor(double d) => new(d);
    public static implicit operator Valor(string s) => new(s);
    public static implicit operator Valor(bool b) => new(b ? 1.0 : 0.0);

    public double ComoNumero(string contexto = "")
    {
        if (!EhTexto) return Num;
        if (Comum.Num.TentarLer(Str, out var v)) return v;
        throw new ErroExpressao($"Esperado número mas veio texto '{Str}'{(contexto.Length > 0 ? " em " + contexto : "")}");
    }

    public bool ComoBool() => EhTexto ? !string.IsNullOrEmpty(Str) && Str != "0" && !Str!.Equals("false", StringComparison.OrdinalIgnoreCase) : Math.Abs(Num) > 1e-12 && !double.IsNaN(Num);
    public override string ToString() => EhTexto ? Str! : Num.ToString("G10", CultureInfo.InvariantCulture);
    public bool Equals(Valor o) => EhTexto || o.EhTexto ? string.Equals(ToString(), o.ToString(), StringComparison.OrdinalIgnoreCase) : Math.Abs(Num - o.Num) <= 1e-9 * Math.Max(1, Math.Abs(Num));
    public override bool Equals(object? obj) => obj is Valor v && Equals(v);
    public override int GetHashCode() => ToString().GetHashCode();
}

public sealed class ErroExpressao : Exception
{
    public ErroExpressao(string msg) : base(msg) { }
}

/// <summary>Fornece valores de variáveis e funções extras para a avaliação.</summary>
public interface IContextoExpressao
{
    bool TentarVariavel(string nome, out Valor valor);
    bool TentarFuncao(string nome, Valor[] args, out Valor valor) { valor = default; return false; }
}

/// <summary>Contexto baseado em dicionário (nomes sem distinção de maiúsculas).</summary>
public sealed class ContextoDicionario : IContextoExpressao
{
    public Dictionary<string, Valor> Variaveis { get; } = new(StringComparer.OrdinalIgnoreCase);
    public IContextoExpressao? Pai { get; set; }
    public Func<string, Valor[], Valor?>? Funcoes { get; set; }

    public ContextoDicionario() { }
    public ContextoDicionario(IEnumerable<KeyValuePair<string, double>> vars) { foreach (var kv in vars) Variaveis[kv.Key] = kv.Value; }

    public Valor this[string nome] { get => Variaveis[nome]; set => Variaveis[nome] = value; }

    public bool TentarVariavel(string nome, out Valor valor)
    {
        if (Variaveis.TryGetValue(nome, out valor)) return true;
        if (Pai is not null) return Pai.TentarVariavel(nome, out valor);
        valor = default; return false;
    }

    public bool TentarFuncao(string nome, Valor[] args, out Valor valor)
    {
        var r = Funcoes?.Invoke(nome, args);
        if (r.HasValue) { valor = r.Value; return true; }
        if (Pai is not null) return Pai.TentarFuncao(nome, args, out valor);
        valor = default; return false;
    }
}

/// <summary>
/// Avaliador de expressões matemáticas próprio do PROJETA.
/// Operadores: + - * / ^ %, comparações (&lt; &lt;= &gt; &gt;= == !=), lógicos (&amp;&amp; || !), ternário (c ? a : b).
/// Funções: min, max, abs, sqrt, pow, exp, ln, log10, sin, cos, tan, asin, acos, atan, atan2, rad, deg,
/// round(x[,n]), floor, ceil, clamp(x,a,b), se(c,a,b) / if(c,a,b), interp(x, x1,y1, x2,y2, ...).
/// Ângulos das funções trigonométricas em radianos (use rad()/deg() para converter).
/// </summary>
public sealed class Expressao
{
    private static readonly ConcurrentDictionary<string, Expressao> Cache = new();
    private readonly No _raiz;
    public string Fonte { get; }

    private Expressao(string fonte, No raiz) { Fonte = fonte; _raiz = raiz; }

    public static Expressao Compilar(string fonte)
    {
        if (fonte is null) throw new ErroExpressao("Expressão vazia");
        return Cache.GetOrAdd(fonte, f =>
        {
            var p = new Parser(f);
            var raiz = p.ParseTudo();
            return new Expressao(f, raiz);
        });
    }

    public Valor Avaliar(IContextoExpressao ctx) => _raiz.Avaliar(ctx);

    public double AvaliarNumero(IContextoExpressao ctx) => Avaliar(ctx).ComoNumero(Fonte);

    public static double Numero(string fonte, IContextoExpressao ctx) => Compilar(fonte).AvaliarNumero(ctx);

    /// <summary>Nomes de variáveis referenciadas (útil para ordenar dependências).</summary>
    public IReadOnlyCollection<string> Variaveis()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _raiz.ColetarVariaveis(set);
        return set;
    }

    public override string ToString() => Fonte;

    // ------------------------------------------------------------------ AST
    private abstract class No
    {
        public abstract Valor Avaliar(IContextoExpressao c);
        public virtual void ColetarVariaveis(HashSet<string> s) { }
    }

    private sealed class Constante : No
    {
        private readonly Valor _v; public Constante(Valor v) => _v = v;
        public override Valor Avaliar(IContextoExpressao c) => _v;
    }

    private sealed class Variavel : No
    {
        private readonly string _n; public Variavel(string n) => _n = n;
        public override Valor Avaliar(IContextoExpressao c)
        {
            if (c.TentarVariavel(_n, out var v)) return v;
            switch (_n.ToLowerInvariant())
            {
                case "pi": return Math.PI;
                case "e": return Math.E;
                case "g": return 9.80665;
                case "true": case "verdadeiro": return 1.0;
                case "false": case "falso": return 0.0;
            }
            throw new ErroExpressao($"Variável desconhecida: '{_n}'");
        }
        public override void ColetarVariaveis(HashSet<string> s) => s.Add(_n);
    }

    private sealed class Unario : No
    {
        private readonly char _op; private readonly No _a;
        public Unario(char op, No a) { _op = op; _a = a; }
        public override Valor Avaliar(IContextoExpressao c)
        {
            var v = _a.Avaliar(c);
            return _op == '-' ? -v.ComoNumero() : (Valor)!v.ComoBool();
        }
        public override void ColetarVariaveis(HashSet<string> s) => _a.ColetarVariaveis(s);
    }

    private sealed class Binario : No
    {
        private readonly string _op; private readonly No _a, _b;
        public Binario(string op, No a, No b) { _op = op; _a = a; _b = b; }
        public override Valor Avaliar(IContextoExpressao c)
        {
            if (_op == "&&") return _a.Avaliar(c).ComoBool() && _b.Avaliar(c).ComoBool();
            if (_op == "||") return _a.Avaliar(c).ComoBool() || _b.Avaliar(c).ComoBool();
            var x = _a.Avaliar(c); var y = _b.Avaliar(c);
            switch (_op)
            {
                case "==": return x.Equals(y);
                case "!=": return !x.Equals(y);
                case "+":
                    if (x.EhTexto || y.EhTexto)
                    {
                        if (Comum.Num.TentarLer(x.Str ?? x.ToString(), out var nx) && Comum.Num.TentarLer(y.Str ?? y.ToString(), out var ny) && !(x.EhTexto && y.EhTexto))
                            return nx + ny;
                        return x.ToString() + y.ToString();
                    }
                    return x.Num + y.Num;
            }
            double a = x.ComoNumero(), b = y.ComoNumero();
            return _op switch
            {
                "-" => a - b,
                "*" => a * b,
                "/" => b == 0 ? throw new ErroExpressao("Divisão por zero") : a / b,
                "%" => a % b,
                "^" => Math.Pow(a, b),
                "<" => a < b,
                "<=" => a <= b,
                ">" => a > b,
                ">=" => a >= b,
                _ => throw new ErroExpressao("Operador inválido " + _op)
            };
        }
        public override void ColetarVariaveis(HashSet<string> s) { _a.ColetarVariaveis(s); _b.ColetarVariaveis(s); }
    }

    private sealed class Ternario : No
    {
        private readonly No _c, _a, _b;
        public Ternario(No c, No a, No b) { _c = c; _a = a; _b = b; }
        public override Valor Avaliar(IContextoExpressao c) => _c.Avaliar(c).ComoBool() ? _a.Avaliar(c) : _b.Avaliar(c);
        public override void ColetarVariaveis(HashSet<string> s) { _c.ColetarVariaveis(s); _a.ColetarVariaveis(s); _b.ColetarVariaveis(s); }
    }

    private sealed class Chamada : No
    {
        private readonly string _nome; private readonly No[] _args;
        public Chamada(string nome, No[] args) { _nome = nome; _args = args; }

        public override Valor Avaliar(IContextoExpressao c)
        {
            string n = _nome.ToLowerInvariant();
            // funções com avaliação preguiçosa
            if (n is "se" or "if" or "iif")
            {
                Exigir(3);
                return _args[0].Avaliar(c).ComoBool() ? _args[1].Avaliar(c) : _args[2].Avaliar(c);
            }
            var v = new Valor[_args.Length];
            for (int i = 0; i < v.Length; i++) v[i] = _args[i].Avaliar(c);
            if (c.TentarFuncao(_nome, v, out var r)) return r;
            double A(int i) => v[i].ComoNumero(_nome);
            switch (n)
            {
                case "min": ExigirMin(1); return v.Select(x => x.ComoNumero(_nome)).Min();
                case "max": ExigirMin(1); return v.Select(x => x.ComoNumero(_nome)).Max();
                case "abs": Exigir(1); return Math.Abs(A(0));
                case "sqrt": case "raiz": Exigir(1); return Math.Sqrt(A(0));
                case "pow": case "pot": Exigir(2); return Math.Pow(A(0), A(1));
                case "exp": Exigir(1); return Math.Exp(A(0));
                case "ln": Exigir(1); return Math.Log(A(0));
                case "log": case "log10": Exigir(1); return Math.Log10(A(0));
                case "sin": case "sen": Exigir(1); return Math.Sin(A(0));
                case "cos": Exigir(1); return Math.Cos(A(0));
                case "tan": case "tg": Exigir(1); return Math.Tan(A(0));
                case "asin": Exigir(1); return Math.Asin(A(0));
                case "acos": Exigir(1); return Math.Acos(A(0));
                case "atan": Exigir(1); return Math.Atan(A(0));
                case "atan2": Exigir(2); return Math.Atan2(A(0), A(1));
                case "rad": Exigir(1); return A(0) * Math.PI / 180.0;
                case "deg": case "graus": Exigir(1); return A(0) * 180.0 / Math.PI;
                case "round": case "arred":
                    ExigirMin(1);
                    return v.Length > 1 ? Math.Round(A(0), (int)A(1), MidpointRounding.AwayFromZero) : Math.Round(A(0), MidpointRounding.AwayFromZero);
                case "floor": Exigir(1); return Math.Floor(A(0));
                case "ceil": case "ceiling": Exigir(1); return Math.Ceiling(A(0));
                case "clamp": Exigir(3); return Math.Clamp(A(0), A(1), A(2));
                case "sign": Exigir(1); return (double)Math.Sign(A(0));
                case "interp":
                    {
                        // interp(x, x1, y1, x2, y2, ...) — interpolação linear por tabela
                        if (v.Length < 5 || (v.Length - 1) % 2 != 0) throw new ErroExpressao("interp(x, x1, y1, x2, y2, ...)");
                        double x = A(0);
                        int m = (v.Length - 1) / 2;
                        double X(int i) => A(1 + 2 * i);
                        double Y(int i) => A(2 + 2 * i);
                        if (x <= X(0)) return Y(0);
                        for (int i = 1; i < m; i++)
                            if (x <= X(i)) return Y(i - 1) + (Y(i) - Y(i - 1)) * (x - X(i - 1)) / (X(i) - X(i - 1));
                        return Y(m - 1);
                    }
                case "texto": case "str": Exigir(1); return v[0].ToString();
            }
            throw new ErroExpressao($"Função desconhecida: '{_nome}'");
        }

        private void Exigir(int n) { if (_args.Length != n) throw new ErroExpressao($"{_nome}() espera {n} argumento(s)"); }
        private void ExigirMin(int n) { if (_args.Length < n) throw new ErroExpressao($"{_nome}() espera ao menos {n} argumento(s)"); }
        public override void ColetarVariaveis(HashSet<string> s) { foreach (var a in _args) a.ColetarVariaveis(s); }
    }

    // ------------------------------------------------------------------ Parser
    private sealed class Parser
    {
        private readonly string _s; private int _i;
        public Parser(string s) { _s = s; }

        public No ParseTudo()
        {
            var n = Ternario();
            Espacos();
            if (_i < _s.Length) throw new ErroExpressao($"Caractere inesperado '{_s[_i]}' na posição {_i + 1} de \"{_s}\"");
            return n;
        }

        private void Espacos() { while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++; }

        private bool Ver(string tok)
        {
            Espacos();
            if (string.CompareOrdinal(_s, _i, tok, 0, tok.Length) == 0)
            {
                // evita confundir "<" com "<=" etc.
                _i += tok.Length; return true;
            }
            return false;
        }

        private bool VerPalavra(string w)
        {
            Espacos();
            if (_i + w.Length <= _s.Length && string.Compare(_s, _i, w, 0, w.Length, StringComparison.OrdinalIgnoreCase) == 0
                && (_i + w.Length == _s.Length || !EhIdent(_s[_i + w.Length])))
            { _i += w.Length; return true; }
            return false;
        }

        private static bool EhIdentInicio(char c) => char.IsLetter(c) || c == '_';
        private static bool EhIdent(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '.';

        private No Ternario()
        {
            var c = Ou();
            if (Ver("?"))
            {
                var a = Ternario();
                if (!Ver(":")) throw new ErroExpressao("Esperado ':' no operador ternário");
                var b = Ternario();
                return new Ternario(c, a, b);
            }
            return c;
        }

        private No Ou()
        {
            var a = E();
            while (Ver("||") || VerPalavra("or") || VerPalavra("ou")) a = new Binario("||", a, E());
            return a;
        }

        private No E()
        {
            var a = Comparacao();
            while (Ver("&&") || VerPalavra("and")) a = new Binario("&&", a, Comparacao());
            return a;
        }

        private No Comparacao()
        {
            var a = Soma();
            while (true)
            {
                if (Ver("==")) a = new Binario("==", a, Soma());
                else if (Ver("!=") || Ver("<>")) a = new Binario("!=", a, Soma());
                else if (Ver("<=")) a = new Binario("<=", a, Soma());
                else if (Ver(">=")) a = new Binario(">=", a, Soma());
                else if (Ver("<")) a = new Binario("<", a, Soma());
                else if (Ver(">")) a = new Binario(">", a, Soma());
                else if (Ver("=")) a = new Binario("==", a, Soma());
                else return a;
            }
        }

        private No Soma()
        {
            var a = Produto();
            while (true)
            {
                if (Ver("+")) a = new Binario("+", a, Produto());
                else if (Ver("-")) a = new Binario("-", a, Produto());
                else return a;
            }
        }

        private No Produto()
        {
            var a = Unario();
            while (true)
            {
                if (Ver("*")) a = new Binario("*", a, Unario());
                else if (Ver("/")) a = new Binario("/", a, Unario());
                else if (Ver("%")) a = new Binario("%", a, Unario());
                else return a;
            }
        }

        private No Unario()
        {
            if (Ver("-")) return new Unario('-', Unario());
            if (Ver("+")) return Unario();
            if (Ver("!") || VerPalavra("not") || VerPalavra("nao")) return new Unario('!', Unario());
            return Potencia();
        }

        private No Potencia()
        {
            var b = Primario();
            if (Ver("^")) return new Binario("^", b, Unario()); // associativo à direita
            return b;
        }

        private No Primario()
        {
            Espacos();
            if (_i >= _s.Length) throw new ErroExpressao($"Fim inesperado da expressão \"{_s}\"");
            char c = _s[_i];
            if (c == '(')
            {
                _i++;
                var n = Ternario();
                if (!Ver(")")) throw new ErroExpressao("Esperado ')'");
                return n;
            }
            if (c == '\'' || c == '"')
            {
                char q = c; _i++;
                var sb = new StringBuilder();
                while (_i < _s.Length && _s[_i] != q) sb.Append(_s[_i++]);
                if (_i >= _s.Length) throw new ErroExpressao("Texto sem aspas de fechamento");
                _i++;
                return new Constante(sb.ToString());
            }
            if (char.IsDigit(c) || (c == '.' && _i + 1 < _s.Length && char.IsDigit(_s[_i + 1])))
            {
                int ini = _i;
                while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.')) _i++;
                if (_i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E') && _i + 1 < _s.Length && (char.IsDigit(_s[_i + 1]) || ((_s[_i + 1] == '-' || _s[_i + 1] == '+') && _i + 2 < _s.Length && char.IsDigit(_s[_i + 2]))))
                {
                    _i += 2;
                    while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
                }
                var txt = _s[ini.._i];
                if (!double.TryParse(txt, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    throw new ErroExpressao($"Número inválido '{txt}'");
                return new Constante(d);
            }
            if (EhIdentInicio(c))
            {
                int ini = _i;
                while (_i < _s.Length && EhIdent(_s[_i])) _i++;
                var nome = _s[ini.._i];
                if (Ver("("))
                {
                    var args = new List<No>();
                    if (!Ver(")"))
                    {
                        do { args.Add(Ternario()); } while (Ver(",") || Ver(";"));
                        if (!Ver(")")) throw new ErroExpressao($"Esperado ')' na chamada de {nome}()");
                    }
                    return new Chamada(nome, args.ToArray());
                }
                return new Variavel(nome);
            }
            throw new ErroExpressao($"Caractere inesperado '{c}' na posição {_i + 1} de \"{_s}\"");
        }
    }
}
