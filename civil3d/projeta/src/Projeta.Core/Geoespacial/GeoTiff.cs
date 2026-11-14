using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Projeta.Core.Geoespacial;

/// <summary>Origem dos bytes de um arquivo (disco, memória ou faixas HTTP de um COG na internet).</summary>
public interface IFonteBytes : IDisposable
{
    /// <summary>Lê até <paramref name="n"/> bytes a partir de <paramref name="pos"/> (menos, perto do fim do arquivo).</summary>
    byte[] Ler(long pos, int n);
}

public sealed class FonteArquivo : IFonteBytes
{
    private readonly FileStream _f;
    public FonteArquivo(string caminho) => _f = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.Read);

    public byte[] Ler(long pos, int n)
    {
        lock (_f)
        {
            if (pos >= _f.Length || n <= 0) return Array.Empty<byte>();
            n = (int)Math.Min(n, _f.Length - pos);
            var b = new byte[n];
            _f.Position = pos;
            _f.ReadExactly(b, 0, n);
            return b;
        }
    }

    public void Dispose() => _f.Dispose();
}

public sealed class FonteMemoria : IFonteBytes
{
    private readonly byte[] _b;
    public FonteMemoria(byte[] b) => _b = b;
    public byte[] Ler(long pos, int n)
    {
        if (pos >= _b.Length || n <= 0) return Array.Empty<byte>();
        n = (int)Math.Min(n, _b.Length - pos);
        var r = new byte[n];
        Array.Copy(_b, pos, r, 0, n);
        return r;
    }
    public void Dispose() { }
}

/// <summary>
/// Leitor de GeoTIFF (TIFF clássico e BigTIFF; ladrilhado ou em faixas; sem compressão, LZW ou Deflate; preditores
/// horizontal e de ponto flutuante; amostras inteiras ou float). Lê só os blocos que cobrem a janela pedida, o que
/// permite ler um Cloud Optimized GeoTIFF pela internet baixando poucos kB.
/// </summary>
public sealed class GeoTiff : IDisposable
{
    private readonly IFonteBytes _f;
    private bool _le = true, _big;
    private byte[] _cab = Array.Empty<byte>();
    private long[] _offsets = Array.Empty<long>(), _contagens = Array.Empty<long>();
    private readonly Dictionary<int, float[]> _blocos = new();

    public int Largura { get; private set; }
    public int Altura { get; private set; }
    public int LarguraBloco { get; private set; }
    public int AlturaBloco { get; private set; }
    public bool Ladrilhado { get; private set; }
    public int Compressao { get; private set; } = 1;
    public int Preditor { get; private set; } = 1;
    public int Bits { get; private set; } = 8;
    /// <summary>1 inteiro sem sinal, 2 inteiro com sinal, 3 ponto flutuante.</summary>
    public int Formato { get; private set; } = 1;
    public int Amostras { get; private set; } = 1;
    public int Planar { get; private set; } = 1;

    /// <summary>Coordenada do centro do pixel (0, 0).</summary>
    public double CentroX0 { get; private set; }
    public double CentroY0 { get; private set; }
    /// <summary>Tamanho do pixel em X e em Y (positivo; as linhas descem).</summary>
    public double Dx { get; private set; } = 1;
    public double Dy { get; private set; } = 1;
    public bool PixelPonto { get; private set; }
    public bool Georreferenciado { get; private set; }
    public double? SemDados { get; private set; }
    /// <summary>EPSG do sistema (geográfico ou projetado), quando o arquivo informa.</summary>
    public int? Epsg { get; private set; }
    public bool Geografico { get; private set; }
    /// <summary>Quantos bytes foram lidos da fonte (para saber o que veio da internet).</summary>
    public long BytesLidos { get; private set; }

    private GeoTiff(IFonteBytes f) => _f = f;

    public void Dispose() => _f.Dispose();

    public static GeoTiff Abrir(IFonteBytes f, int bytesCabecalho = 65536)
    {
        var g = new GeoTiff(f);
        g.LerCabecalho(bytesCabecalho);
        return g;
    }

    public static GeoTiff Abrir(string arquivo) => Abrir(new FonteArquivo(arquivo));

    // ------------------------------------------------------------------ cabeçalho e IFD

    private sealed record Entrada(int Tag, int Tipo, long Contagem, byte[] Dados);

    private void LerCabecalho(int bytesCabecalho)
    {
        _cab = Ler(0, bytesCabecalho);
        if (_cab.Length < 16) throw new InvalidDataException("Arquivo curto demais para ser um TIFF.");
        if (_cab[0] == 'I' && _cab[1] == 'I') _le = true;
        else if (_cab[0] == 'M' && _cab[1] == 'M') _le = false;
        else throw new InvalidDataException("Não é um arquivo TIFF.");
        int versao = (int)U(_cab, 2, 2);
        long ifd;
        if (versao == 42) { _big = false; ifd = (long)U(_cab, 4, 4); }
        else if (versao == 43) { _big = true; ifd = (long)U(_cab, 8, 8); }
        else throw new InvalidDataException("Versão de TIFF desconhecida.");

        var e = LerIfd(ifd);
        Entrada? Tag(int t) => e.TryGetValue(t, out var x) ? x : null;
        double N1(int t, double padrao) => Tag(t) is { } x ? Numeros(x)[0] : padrao;

        Largura = (int)N1(256, 0);
        Altura = (int)N1(257, 0);
        if (Largura <= 0 || Altura <= 0) throw new InvalidDataException("TIFF sem dimensões.");
        Bits = (int)N1(258, 8);
        Compressao = (int)N1(259, 1);
        Amostras = (int)N1(277, 1);
        Planar = (int)N1(284, 1);
        Preditor = (int)N1(317, 1);
        Formato = (int)N1(339, 1);
        if (Bits is not (8 or 16 or 32 or 64)) throw new NotSupportedException($"TIFF com {Bits} bits por amostra não é suportado.");
        if (Formato == 3 && Bits is not (32 or 64)) throw new NotSupportedException("Ponto flutuante só em 32 ou 64 bits.");
        if (Compressao is not (1 or 5 or 8 or 32946))
            throw new NotSupportedException($"Compressão TIFF {Compressao} não suportada (use sem compressão, LZW ou Deflate).");

        if (Tag(322) is not null)
        {
            Ladrilhado = true;
            LarguraBloco = (int)N1(322, 256);
            AlturaBloco = (int)N1(323, 256);
            _offsets = Numeros(Tag(324) ?? throw new InvalidDataException("TIFF ladrilhado sem TileOffsets.")).Select(v => (long)v).ToArray();
            _contagens = Numeros(Tag(325) ?? throw new InvalidDataException("TIFF ladrilhado sem TileByteCounts.")).Select(v => (long)v).ToArray();
        }
        else
        {
            LarguraBloco = Largura;
            AlturaBloco = (int)Math.Min(N1(278, Altura), Altura);
            _offsets = Numeros(Tag(273) ?? throw new InvalidDataException("TIFF sem StripOffsets.")).Select(v => (long)v).ToArray();
            _contagens = Numeros(Tag(279) ?? throw new InvalidDataException("TIFF sem StripByteCounts.")).Select(v => (long)v).ToArray();
        }

        // georreferência
        int tipoRaster = 1;
        if (Tag(34735) is { } chaves)
        {
            var k = Numeros(chaves);
            int nk = k.Length >= 4 ? (int)k[3] : 0;
            for (int i = 0; i < nk && 4 + i * 4 + 3 < k.Length; i++)
            {
                int id = (int)k[4 + i * 4], local = (int)k[5 + i * 4];
                int valor = (int)k[7 + i * 4];
                if (local != 0) continue;
                switch (id)
                {
                    case 1024: Geografico = valor == 2; break;
                    case 1025: tipoRaster = valor; break;
                    case 2048: if (valor is > 0 and < 32767) Epsg ??= valor; break;
                    case 3072: if (valor is > 0 and < 32767) Epsg = valor; break;
                }
            }
        }
        PixelPonto = tipoRaster == 2;
        double meio = PixelPonto ? 0 : 0.5;
        if (Tag(33550) is { } escala && Tag(33922) is { } amarra)
        {
            var s = Numeros(escala);
            var tp = Numeros(amarra);
            Dx = s[0]; Dy = s[1];
            CentroX0 = tp[3] + (0 - tp[0] + meio) * Dx;
            CentroY0 = tp[4] - (0 - tp[1] + meio) * Dy;
            Georreferenciado = true;
        }
        else if (Tag(34264) is { } mt)
        {
            var m = Numeros(mt);
            if (Math.Abs(m[1]) > 1e-12 || Math.Abs(m[4]) > 1e-12) throw new NotSupportedException("GeoTIFF girado (ModelTransformation com rotação) não é suportado.");
            Dx = m[0]; Dy = -m[5];
            CentroX0 = m[3] + meio * Dx;
            CentroY0 = m[7] - meio * Dy;
            Georreferenciado = true;
        }
        if (Tag(42113) is { } nd)
        {
            var txt = Encoding.ASCII.GetString(nd.Dados).Trim('\0', ' ');
            if (double.TryParse(txt, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) SemDados = v;
            else if (txt.Equals("nan", StringComparison.OrdinalIgnoreCase)) SemDados = double.NaN;
        }
    }

    private Dictionary<int, Entrada> LerIfd(long pos)
    {
        var d = new Dictionary<int, Entrada>();
        int tamCont = _big ? 8 : 2, tamEnt = _big ? 20 : 12, tamVal = _big ? 8 : 4;
        long n = (long)U(Bytes(pos, tamCont), 0, tamCont);
        var bloco = Bytes(pos + tamCont, (int)(n * tamEnt));
        for (int i = 0; i < n; i++)
        {
            int o = i * tamEnt;
            int tag = (int)U(bloco, o, 2), tipo = (int)U(bloco, o + 2, 2);
            long cont = (long)U(bloco, o + 4, _big ? 8 : 4);
            int vo = o + (_big ? 12 : 8);
            long tam = cont * TamanhoTipo(tipo);
            byte[] dados;
            if (tam <= tamVal) { dados = new byte[tam]; Array.Copy(bloco, vo, dados, 0, tam); }
            else dados = Bytes((long)U(bloco, vo, tamVal), (int)tam);
            d[tag] = new Entrada(tag, tipo, cont, dados);
        }
        return d;
    }

    private static int TamanhoTipo(int t) => t switch { 1 or 2 or 6 or 7 => 1, 3 or 8 => 2, 4 or 9 or 11 or 13 => 4, 5 or 10 or 12 or 16 or 17 or 18 => 8, _ => 1 };

    private double[] Numeros(Entrada e)
    {
        int ts = TamanhoTipo(e.Tipo);
        var r = new double[e.Contagem];
        for (int i = 0; i < e.Contagem; i++)
        {
            int o = i * ts;
            r[i] = e.Tipo switch
            {
                1 or 7 => e.Dados[o],
                6 => (sbyte)e.Dados[o],
                3 => U(e.Dados, o, 2),
                8 => (short)U(e.Dados, o, 2),
                4 or 13 => U(e.Dados, o, 4),
                9 => (int)U(e.Dados, o, 4),
                5 => U(e.Dados, o, 4) / (double)Math.Max(1, U(e.Dados, o + 4, 4)),
                10 => (int)U(e.Dados, o, 4) / (double)Math.Max(1, (int)U(e.Dados, o + 4, 4)),
                11 => BitConverter.Int32BitsToSingle((int)U(e.Dados, o, 4)),
                12 => BitConverter.Int64BitsToDouble((long)U(e.Dados, o, 8)),
                16 or 18 => U(e.Dados, o, 8),
                17 => (long)U(e.Dados, o, 8),
                _ => e.Dados[o],
            };
        }
        return r;
    }

    private ulong U(byte[] b, int o, int n) => n switch
    {
        1 => b[o],
        2 => _le ? BinaryPrimitives.ReadUInt16LittleEndian(b.AsSpan(o)) : BinaryPrimitives.ReadUInt16BigEndian(b.AsSpan(o)),
        4 => _le ? BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(o)) : BinaryPrimitives.ReadUInt32BigEndian(b.AsSpan(o)),
        8 => _le ? BinaryPrimitives.ReadUInt64LittleEndian(b.AsSpan(o)) : BinaryPrimitives.ReadUInt64BigEndian(b.AsSpan(o)),
        _ => throw new ArgumentOutOfRangeException(nameof(n)),
    };

    private byte[] Ler(long pos, int n)
    {
        var b = _f.Ler(pos, n);
        BytesLidos += b.Length;
        return b;
    }

    /// <summary>Bytes de uma posição: do cabeçalho já lido ou da fonte.</summary>
    private byte[] Bytes(long pos, int n)
    {
        if (pos >= 0 && pos + n <= _cab.Length)
        {
            var r = new byte[n];
            Array.Copy(_cab, pos, r, 0, n);
            return r;
        }
        var b = Ler(pos, n);
        if (b.Length < n) throw new InvalidDataException("TIFF truncado.");
        return b;
    }

    // ------------------------------------------------------------------ coordenadas

    /// <summary>Centro do pixel (coluna, linha) nas coordenadas do arquivo.</summary>
    public (double X, double Y) Centro(int col, int lin) => (CentroX0 + col * Dx, CentroY0 - lin * Dy);

    /// <summary>Posição fracionária (coluna, linha) de um ponto, contada a partir do centro do pixel (0, 0).</summary>
    public (double Col, double Lin) Pixel(double x, double y) => ((x - CentroX0) / Dx, (CentroY0 - y) / Dy);

    // ------------------------------------------------------------------ dados

    /// <summary>Valores de uma janela da banda 1 (NaN fora da imagem ou sem dados).</summary>
    public float[,] LerJanela(int col0, int lin0, int colunas, int linhas)
    {
        var r = new float[linhas, colunas];
        int porLinha = (Largura + LarguraBloco - 1) / LarguraBloco;
        for (int l = 0; l < linhas; l++)
            for (int c = 0; c < colunas; c++) r[l, c] = float.NaN;
        int c0 = Math.Max(col0, 0), c1 = Math.Min(col0 + colunas - 1, Largura - 1);
        int l0 = Math.Max(lin0, 0), l1 = Math.Min(lin0 + linhas - 1, Altura - 1);
        if (c0 > c1 || l0 > l1) return r;
        for (int bl = l0 / AlturaBloco; bl <= l1 / AlturaBloco; bl++)
            for (int bc = c0 / LarguraBloco; bc <= c1 / LarguraBloco; bc++)
            {
                int idx = Ladrilhado ? bl * porLinha + bc : bl;
                var v = Bloco(idx, bl);
                int la = Math.Max(l0, bl * AlturaBloco), lb = Math.Min(l1, (bl + 1) * AlturaBloco - 1);
                int ca = Math.Max(c0, bc * LarguraBloco), cb = Math.Min(c1, (bc + 1) * LarguraBloco - 1);
                for (int l = la; l <= lb; l++)
                    for (int c = ca; c <= cb; c++)
                        r[l - lin0, c - col0] = v[(l - bl * AlturaBloco) * LarguraBloco + (c - bc * LarguraBloco)];
            }
        return r;
    }

    /// <summary>Valor no pixel (NaN fora).</summary>
    public float Valor(int col, int lin) => LerJanela(col, lin, 1, 1)[0, 0];

    private float[] Bloco(int idx, int linhaBloco)
    {
        if (_blocos.TryGetValue(idx, out var pronto)) return pronto;
        if (idx >= _offsets.Length) throw new InvalidDataException("Bloco do TIFF fora da tabela de offsets.");
        int bps = Bits / 8;
        int linhas = Ladrilhado ? AlturaBloco : Math.Min(AlturaBloco, Altura - linhaBloco * AlturaBloco);
        int amostrasLinha = LarguraBloco * (Planar == 1 ? Amostras : 1);
        int esperado = amostrasLinha * linhas * bps;
        var comp = _contagens[idx] > 0 ? Ler(_offsets[idx], (int)_contagens[idx]) : Array.Empty<byte>();
        var cru = Compressao switch
        {
            1 => comp,
            5 => Lzw(comp, esperado),
            _ => Inflar(comp, esperado),
        };
        if (cru.Length < esperado) Array.Resize(ref cru, esperado);
        int passo = Planar == 1 ? Amostras : 1;
        var saida = new float[LarguraBloco * linhas];
        int bytesLinha = amostrasLinha * bps;
        var tmp = new byte[bytesLinha];
        for (int l = 0; l < linhas; l++)
        {
            int o = l * bytesLinha;
            if (Preditor == 3)
            {
                for (int i = passo; i < bytesLinha; i++) cru[o + i] = (byte)(cru[o + i] + cru[o + i - passo]);
                Array.Copy(cru, o, tmp, 0, bytesLinha);
                for (int c = 0; c < LarguraBloco; c++)
                {
                    int k = c * passo;
                    if (bps == 4)
                    {
                        int bits = (tmp[k] << 24) | (tmp[amostrasLinha + k] << 16) | (tmp[2 * amostrasLinha + k] << 8) | tmp[3 * amostrasLinha + k];
                        saida[l * LarguraBloco + c] = Filtrar(BitConverter.Int32BitsToSingle(bits));
                    }
                    else
                    {
                        long bits = 0;
                        for (int b = 0; b < 8; b++) bits = (bits << 8) | tmp[b * amostrasLinha + k];
                        saida[l * LarguraBloco + c] = Filtrar(BitConverter.Int64BitsToDouble(bits));
                    }
                }
                continue;
            }
            if (Preditor == 2) DesfazerHorizontal(cru, o, amostrasLinha, passo, bps);
            for (int c = 0; c < LarguraBloco; c++)
                saida[l * LarguraBloco + c] = Filtrar(Amostra(cru, o + c * passo * bps, bps));
        }
        _blocos[idx] = saida;
        return saida;
    }

    private float Filtrar(double v)
    {
        if (SemDados is { } nd && (v == nd || (double.IsNaN(nd) && double.IsNaN(v)))) return float.NaN;
        return (float)v;
    }

    private double Amostra(byte[] b, int o, int bps) => (Formato, bps) switch
    {
        (3, 4) => BitConverter.Int32BitsToSingle((int)U(b, o, 4)),
        (3, 8) => BitConverter.Int64BitsToDouble((long)U(b, o, 8)),
        (2, 1) => (sbyte)b[o],
        (2, 2) => (short)U(b, o, 2),
        (2, 4) => (int)U(b, o, 4),
        (2, 8) => (long)U(b, o, 8),
        (_, 1) => b[o],
        (_, 2) => U(b, o, 2),
        (_, 4) => U(b, o, 4),
        _ => U(b, o, 8),
    };

    private void DesfazerHorizontal(byte[] b, int o, int n, int passo, int bps)
    {
        for (int i = passo; i < n; i++)
        {
            int p = o + i * bps, q = o + (i - passo) * bps;
            switch (bps)
            {
                case 1: b[p] = (byte)(b[p] + b[q]); break;
                case 2:
                    {
                        ushort v = (ushort)(U(b, p, 2) + U(b, q, 2));
                        if (_le) BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(p), v); else BinaryPrimitives.WriteUInt16BigEndian(b.AsSpan(p), v);
                        break;
                    }
                case 4:
                    {
                        uint v = (uint)(U(b, p, 4) + U(b, q, 4));
                        if (_le) BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(p), v); else BinaryPrimitives.WriteUInt32BigEndian(b.AsSpan(p), v);
                        break;
                    }
                default:
                    {
                        ulong v = U(b, p, 8) + U(b, q, 8);
                        if (_le) BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(p), v); else BinaryPrimitives.WriteUInt64BigEndian(b.AsSpan(p), v);
                        break;
                    }
            }
        }
    }

    private static byte[] Inflar(byte[] comp, int esperado)
    {
        var r = new byte[esperado];
        if (comp.Length == 0) return r;
        using var ms = new MemoryStream(comp);
        using var z = new ZLibStream(ms, CompressionMode.Decompress);
        int lidos = z.ReadAtLeast(r, esperado, throwOnEndOfStream: false);
        return r;
    }

    /// <summary>Descompressão LZW do TIFF (códigos de 9 a 12 bits, MSB primeiro, troca de largura antecipada).</summary>
    public static byte[] Lzw(byte[] src, int esperado)
    {
        var saida = new byte[esperado];
        var prefixo = new int[4096];
        var sufixo = new byte[4096];
        var tamanho = new int[4096];
        var primeiro = new byte[4096];
        for (int i = 0; i < 256; i++) { prefixo[i] = -1; sufixo[i] = (byte)i; tamanho[i] = 1; primeiro[i] = (byte)i; }
        int proximo = 258, largura = 9, anterior = -1, pos = 0;
        long bit = 0, totalBits = src.Length * 8L;

        void Escrever(int codigo)
        {
            int t = tamanho[codigo];
            int c = codigo;
            for (int k = t - 1; k >= 0; k--)
            {
                if (pos + k < esperado) saida[pos + k] = sufixo[c];
                c = prefixo[c];
            }
            pos += t;
        }

        while (bit + largura <= totalBits && pos < esperado)
        {
            int codigo = 0;
            for (int k = 0; k < largura; k++)
            {
                long b = bit + k;
                codigo = (codigo << 1) | ((src[b >> 3] >> (7 - (int)(b & 7))) & 1);
            }
            bit += largura;
            if (codigo == 256) { proximo = 258; largura = 9; anterior = -1; continue; }
            if (codigo == 257) break;
            if (anterior < 0)
            {
                if (codigo > 255) throw new InvalidDataException("LZW inválido.");
                Escrever(codigo);
                anterior = codigo;
                continue;
            }
            if (proximo < 4096)
            {
                if (codigo < proximo)
                {
                    prefixo[proximo] = anterior; sufixo[proximo] = primeiro[codigo];
                }
                else if (codigo == proximo)
                {
                    prefixo[proximo] = anterior; sufixo[proximo] = primeiro[anterior];
                }
                else throw new InvalidDataException("LZW inválido.");
                tamanho[proximo] = tamanho[anterior] + 1;
                primeiro[proximo] = primeiro[anterior];
                proximo++;
            }
            Escrever(codigo);
            anterior = codigo;
            if (proximo >= (1 << largura) - 1 && largura < 12) largura++;
        }
        return saida;
    }

    // ------------------------------------------------------------------ gravação

    /// <summary>
    /// Grava uma grade float32 como GeoTIFF ladrilhado (256 × 256), Deflate com preditor de ponto flutuante,
    /// pixel como área. <paramref name="x0"/>/<paramref name="y0"/> = canto superior esquerdo da grade.
    /// </summary>
    public static byte[] Gravar(float[,] z, double x0, double y0, double dx, double dy, int epsg, bool geografico, float? semDados = null, int ladrilho = 256)
    {
        int linhas = z.GetLength(0), colunas = z.GetLength(1);
        int nx = (colunas + ladrilho - 1) / ladrilho, ny = (linhas + ladrilho - 1) / ladrilho;
        var blocos = new List<byte[]>();
        int wc = ladrilho;
        for (int by = 0; by < ny; by++)
            for (int bx = 0; bx < nx; bx++)
            {
                var cru = new byte[ladrilho * ladrilho * 4];
                for (int l = 0; l < ladrilho; l++)
                {
                    var linha = new byte[wc * 4];
                    for (int c = 0; c < ladrilho; c++)
                    {
                        int gl = by * ladrilho + l, gc = bx * ladrilho + c;
                        float v = gl < linhas && gc < colunas ? z[gl, gc] : (semDados ?? 0);
                        int bits = BitConverter.SingleToInt32Bits(v);
                        linha[c] = (byte)(bits >> 24); linha[wc + c] = (byte)(bits >> 16);
                        linha[2 * wc + c] = (byte)(bits >> 8); linha[3 * wc + c] = (byte)bits;
                    }
                    for (int i = linha.Length - 1; i >= 1; i--) linha[i] = (byte)(linha[i] - linha[i - 1]);
                    Array.Copy(linha, 0, cru, l * wc * 4, linha.Length);
                }
                using var ms = new MemoryStream();
                using (var zs = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) zs.Write(cru);
                blocos.Add(ms.ToArray());
            }

        var tags = new SortedDictionary<int, (int Tipo, byte[] Dados, int Cont)>();
        static byte[] Shorts(params int[] v) { var b = new byte[v.Length * 2]; for (int i = 0; i < v.Length; i++) BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(i * 2), (ushort)v[i]); return b; }
        static byte[] Longs(params long[] v) { var b = new byte[v.Length * 4]; for (int i = 0; i < v.Length; i++) BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(i * 4), (uint)v[i]); return b; }
        static byte[] Doubles(params double[] v) { var b = new byte[v.Length * 8]; for (int i = 0; i < v.Length; i++) BinaryPrimitives.WriteInt64LittleEndian(b.AsSpan(i * 8), BitConverter.DoubleToInt64Bits(v[i])); return b; }
        tags[256] = (3, Shorts(colunas), 1);
        tags[257] = (3, Shorts(linhas), 1);
        tags[258] = (3, Shorts(32), 1);
        tags[259] = (3, Shorts(8), 1);
        tags[262] = (3, Shorts(1), 1);
        tags[277] = (3, Shorts(1), 1);
        tags[284] = (3, Shorts(1), 1);
        tags[317] = (3, Shorts(3), 1);
        tags[322] = (3, Shorts(ladrilho), 1);
        tags[323] = (3, Shorts(ladrilho), 1);
        tags[339] = (3, Shorts(3), 1);
        tags[33550] = (12, Doubles(dx, dy, 0), 3);
        tags[33922] = (12, Doubles(0, 0, 0, x0, y0, 0), 6);
        var chaves = geografico
            ? Shorts(1, 1, 0, 3, 1024, 0, 1, 2, 1025, 0, 1, 1, 2048, 0, 1, epsg)
            : Shorts(1, 1, 0, 3, 1024, 0, 1, 1, 1025, 0, 1, 1, 3072, 0, 1, epsg);
        tags[34735] = (3, chaves, chaves.Length / 2);
        if (semDados is { } nd)
        {
            var s = Encoding.ASCII.GetBytes(nd.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "\0");
            tags[42113] = (2, s, s.Length);
        }
        // offsets e contagens entram depois que o layout é conhecido
        tags[324] = (4, Longs(new long[blocos.Count]), blocos.Count);
        tags[325] = (4, Longs(blocos.Select(b => (long)b.Length).ToArray()), blocos.Count);

        int nTags = tags.Count;
        long ifdTam = 2 + nTags * 12 + 4;
        long extra = tags.Values.Where(t => t.Dados.Length > 4).Sum(t => (long)(t.Dados.Length + (t.Dados.Length & 1)));
        long dadosIni = 8 + ifdTam + extra;
        var offs = new long[blocos.Count];
        long p = dadosIni;
        for (int i = 0; i < blocos.Count; i++) { offs[i] = p; p += blocos[i].Length; }
        tags[324] = (4, Longs(offs), blocos.Count);

        using var outp = new MemoryStream();
        var w = new BinaryWriter(outp);
        w.Write((byte)'I'); w.Write((byte)'I'); w.Write((ushort)42); w.Write((uint)8);
        w.Write((ushort)nTags);
        long extraPos = 8 + ifdTam;
        var extras = new List<byte[]>();
        foreach (var (tag, (tipo, dados, cont)) in tags)
        {
            w.Write((ushort)tag); w.Write((ushort)tipo); w.Write((uint)cont);
            if (dados.Length <= 4) { var v = new byte[4]; Array.Copy(dados, v, dados.Length); w.Write(v); }
            else { w.Write((uint)extraPos); extras.Add(dados); extraPos += dados.Length + (dados.Length & 1); }
        }
        w.Write((uint)0);
        foreach (var d in extras) { w.Write(d); if ((d.Length & 1) == 1) w.Write((byte)0); }
        foreach (var b in blocos) w.Write(b);
        w.Flush();
        return outp.ToArray();
    }
}
