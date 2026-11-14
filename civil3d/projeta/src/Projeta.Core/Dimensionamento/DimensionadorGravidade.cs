using Projeta.Core.Comum;
using Projeta.Core.Hidraulica;
using Projeta.Core.Hidrologia;
using Projeta.Core.Redes;

namespace Projeta.Core.Dimensionamento;

/// <summary>
/// Dimensionamento de redes de gravidade (drenagem pelo método racional; esgoto por contribuição em marcha — NBR 9649).
/// Percorre os trechos de montante para jusante, escolhendo a menor seção que atende às restrições
/// de recobrimento, declividade, lâmina e velocidade, respeitando o critério de concordância nos PVs.
/// </summary>
public static class DimensionadorGravidade
{
    private sealed record Candidato(FormaSecao Forma, double B, double H, int Celulas)
    {
        public ISecao Secao => Forma == FormaSecao.Circular ? new SecaoCircular(B) : new SecaoRetangularFechada(B, H);
        public double AreaPlena => Celulas * (Forma == FormaSecao.Circular ? Math.PI * B * B / 4 : B * H);
    }

    private sealed class Cfg
    {
        public double Manning, LaminaMax, VelMin, VelMax, Recobrimento, ProfMax, DeclMin, DeclMax, DegrauMin, DegrauTuboQueda = double.MaxValue;
        public CriterioConcordancia Criterio;
        public Func<double, double> Espessura = d => 0.05;
        public List<Candidato> Candidatos = new();
        public bool Esgoto;
        public double TensaoMin;
    }

    public static ResultadoRede Calcular(Rede rede, Projeto projeto)
    {
        var res = new ResultadoRede { Tipo = rede.Tipo };
        var top = new Topologia(rede);
        res.Mensagens.AddRange(top.Mensagens);
        if (top.TemCiclo || top.Mensagens.Any(m => m.Gravidade == Gravidade.Erro))
        {
            res.Mensagens.Add(new(Gravidade.Erro, "Corrija a topologia da rede antes de dimensionar."));
            return res;
        }
        if (rede.Tipo == TipoRede.Agua)
        {
            res.Mensagens.Add(new(Gravidade.Erro, "Redes de água são calculadas pelo EPANET (comando PRJEPANETCALCULAR)."));
            return res;
        }

        foreach (var n in rede.Nos)
            if (double.IsNaN(n.CotaTampa))
                res.Mensagens.Add(new(Gravidade.Erro, "Nó sem cota de terreno/tampa. Associe uma superfície ou force a cota da tampa.", n.Nome));
        if (res.Mensagens.Any(m => m.Gravidade == Gravidade.Erro)) return res;

        var cfg = MontarConfig(rede);
        bool esgoto = rede.Tipo == TipoRede.Esgoto;
        EquacaoChuva? eq = null;
        if (!esgoto)
        {
            eq = projeto.Equacao(rede.Drenagem.EquacaoId);
            if (eq is null && rede.Tipo == TipoRede.Drenagem)
            {
                res.Mensagens.Add(new(Gravidade.Erro, $"Equação de chuva '{rede.Drenagem.EquacaoId}' não encontrada."));
                return res;
            }
            res.Metodo = eq is null ? "Geometria (rede genérica)" : $"Método racional — {eq.Nome}, T = {rede.Drenagem.TempoRetorno:0.#} anos";
        }
        else res.Metodo = "Contribuição em marcha (NBR 9649)";

        // ------------------------------------------------ acumuladores por nó
        var caLocal = rede.Nos.ToDictionary(n => n.Id, _ => 0.0);
        var areaLocal = rede.Nos.ToDictionary(n => n.Id, _ => 0.0);
        var tcLocal = rede.Nos.ToDictionary(n => n.Id, _ => rede.Drenagem.TcMinimo);
        foreach (var b in rede.Bacias)
        {
            if (b.NoId is null || !caLocal.ContainsKey(b.NoId))
            {
                res.Mensagens.Add(new(Gravidade.Aviso, "Bacia sem nó de lançamento — ignorada", b.Nome));
                continue;
            }
            caLocal[b.NoId] += b.C * b.Area;
            areaLocal[b.NoId] += b.Area;
            tcLocal[b.NoId] = Math.Max(tcLocal[b.NoId], Math.Max(b.Tc(), rede.Drenagem.TcMinimo));
        }

        var caChega = rede.Nos.ToDictionary(n => n.Id, _ => 0.0);
        var areaChega = rede.Nos.ToDictionary(n => n.Id, _ => 0.0);
        var tcChega = rede.Nos.ToDictionary(n => n.Id, _ => 0.0);
        var qAdicChega = rede.Nos.ToDictionary(n => n.Id, _ => 0.0);
        var qiChega = rede.Nos.ToDictionary(n => n.Id, _ => 0.0); // L/s
        var qfChega = rede.Nos.ToDictionary(n => n.Id, _ => 0.0);

        // esgoto: taxas de contribuição linear
        double txI = 0, txF = 0;
        if (esgoto)
        {
            var p = rede.Esgoto;
            double lTotal = rede.Trechos.Where(t => t.Contribui).Sum(rede.ComprimentoHorizontal);
            double ti = p.TaxaInfiltracao / 1000.0; // L/s.m
            txI = (lTotal > 0 ? p.VazaoDomesticaInicial() / lTotal : 0) + ti;
            txF = (lTotal > 0 ? p.VazaoDomesticaFinal() / lTotal : 0) + ti;
            res.Mensagens.Add(new(Gravidade.Info, $"Extensão contribuinte {Num.F(lTotal, 1)} m; taxa inicial {Num.F(txI * 1000, 3)} L/s.km; taxa final {Num.F(txF * 1000, 3)} L/s.km"));
        }

        // ------------------------------------------------ trechos
        var porTrecho = new Dictionary<string, ResultadoTrecho>();
        foreach (var t in top.OrdemMontanteJusante)
        {
            var a = rede.NoPorId(t.NoMontanteId)!;
            var b = rede.NoPorId(t.NoJusanteId)!;
            double L = rede.ComprimentoHorizontal(t);
            var r = new ResultadoTrecho
            {
                TrechoId = t.Id, Nome = t.Nome, Montante = a.Nome, Jusante = b.Nome, Comprimento = L,
                CotaTerrenoMontante = a.CotaTampa, CotaTerrenoJusante = b.CotaTampa,
            };
            if (L <= 0.01)
            {
                r.Erros.Add("Comprimento nulo");
                porTrecho[t.Id] = r; res.Trechos.Add(r);
                continue;
            }

            double q, qi = 0;
            if (!esgoto)
            {
                double sca = caLocal[a.Id] + caChega[a.Id];
                double tc = Math.Max(tcLocal[a.Id], tcChega[a.Id]);
                double qAdic = qAdicChega[a.Id] + a.VazaoAdicional / 1000.0;
                double i = eq is null ? 0 : eq.Intensidade(rede.Drenagem.TempoRetorno, tc);
                r.SomaCA = sca; r.Tc = tc; r.Intensidade = i;
                r.AreaLocalHa = areaLocal[a.Id];
                r.AreaAcumuladaHa = areaLocal[a.Id] + areaChega[a.Id];
                q = MetodoRacional.VazaoCA(sca, i) + qAdic;
                if (eq is not null && (tc < eq.DuracaoMin || tc > eq.DuracaoMax))
                    r.Avisos.Add($"tc = {tc:0.0} min fora da validade da equação ({eq.DuracaoMin:0}–{eq.DuracaoMax:0} min)");
            }
            else
            {
                double qiM = qiChega[a.Id] + a.ContribuicaoSingular, qfM = qfChega[a.Id] + a.ContribuicaoSingular;
                double qiJ = qiM + (t.Contribui ? txI * L : 0), qfJ = qfM + (t.Contribui ? txF * L : 0);
                r.VazaoInicialLs = qiJ; r.VazaoFinalLs = qfJ; r.TaxaInicial = txI; r.TaxaFinal = txF;
                double qmin = rede.Esgoto.VazaoMinima;
                qi = Math.Max(qiJ, qmin) / 1000.0;
                q = Math.Max(qfJ, qmin) / 1000.0;
            }
            r.Vazao = q;

            var chegam = top.Chegam(a.Id).Select(x => porTrecho.TryGetValue(x.Id, out var rr) ? rr : null).Where(x => x is not null).Cast<ResultadoTrecho>().ToList();
            DimensionarTrecho(t, r, a, b, L, q, qi, chegam, cfg);
            porTrecho[t.Id] = r;
            res.Trechos.Add(r);

            // passa para jusante
            if (!esgoto)
            {
                caChega[b.Id] += r.SomaCA;
                areaChega[b.Id] += r.AreaAcumuladaHa;
                double tcFim = r.Tc + r.TempoPercurso;
                tcChega[b.Id] = Math.Max(tcChega[b.Id], tcFim);
                qAdicChega[b.Id] += qAdicChega[a.Id] + a.VazaoAdicional / 1000.0;
            }
            else
            {
                qiChega[b.Id] += r.VazaoInicialLs;
                qfChega[b.Id] += r.VazaoFinalLs;
            }
        }

        // ------------------------------------------------ nós
        foreach (var n in rede.Nos)
        {
            var rn = new ResultadoNo { NoId = n.Id, Nome = n.Nome, Tipo = n.Tipo, X = n.X, Y = n.Y, CotaTampa = n.CotaTampa, Estaca = n.Estaca, Afastamento = n.Afastamento };
            var ent = top.Chegam(n.Id).Select(t => porTrecho.GetValueOrDefault(t.Id)).Where(x => x is not null).Cast<ResultadoTrecho>().ToList();
            var sai = top.Saem(n.Id).Select(t => porTrecho.GetValueOrDefault(t.Id)).Where(x => x is not null).Cast<ResultadoTrecho>().ToList();
            var cotas = ent.Select(e => e.CotaFundoJusante).Concat(sai.Select(s => s.CotaFundoMontante)).Where(z => !double.IsNaN(z)).ToList();
            rn.CotaFundo = cotas.Count > 0 ? cotas.Min() - n.Rebaixo : (n.CotaFundoForcada ?? n.CotaTampa - 1.0);
            rn.MaiorSecao = ent.Concat(sai).Select(x => Math.Max(x.Diametro, x.Altura)).DefaultIfEmpty(0).Max();
            if (sai.Count > 0)
            {
                var zs = sai[0].CotaFundoMontante;
                rn.DegrauMaximo = ent.Select(e => e.CotaFundoJusante - zs).DefaultIfEmpty(0).Max();
                rn.VazaoSaida = sai[0].Vazao;
            }
            else rn.VazaoSaida = ent.Sum(e => e.Vazao);
            if (rn.DegrauMaximo > cfg.DegrauTuboQueda + 1e-6)
            {
                rn.ExigeTuboQueda = true;
                rn.Avisos.Add($"Degrau de {rn.DegrauMaximo:0.00} m — prever tubo de queda");
            }
            if (rn.Profundidade > cfg.ProfMax) rn.Avisos.Add($"Profundidade {rn.Profundidade:0.00} m acima do limite de {cfg.ProfMax:0.00} m");
            if (n.Tipo == TipoNo.Exutorio && n.CotaFundoForcada is double zl && ent.Count > 0 && ent.Min(e => e.CotaFundoJusante) < zl - 1e-3)
                rn.Avisos.Add($"Rede chega em {ent.Min(e => e.CotaFundoJusante):0.000}, abaixo da cota de lançamento {zl:0.000}");
            res.Nos.Add(rn);
        }

        if (res.Trechos.Any(t => t.Erros.Count > 0))
            res.Mensagens.Add(new(Gravidade.Erro, $"{res.Trechos.Count(t => t.Erros.Count > 0)} trecho(s) com erro — veja a planilha."));
        res.Mensagens.Add(new(Gravidade.Info, $"{res.Trechos.Count} trechos e {res.Nos.Count} nós calculados."));
        return res;
    }

    // ==================================================================================

    private static Cfg MontarConfig(Rede rede)
    {
        var c = new Cfg();
        if (rede.Tipo == TipoRede.Esgoto)
        {
            var p = rede.Esgoto;
            c.Esgoto = true;
            c.Manning = p.Manning; c.LaminaMax = p.LaminaMaxima; c.VelMin = 0; c.VelMax = p.VelocidadeMaxima;
            c.Recobrimento = p.RecobrimentoMinimo; c.ProfMax = p.ProfundidadeMaxima;
            c.DeclMin = p.DeclividadeMinimaAbsoluta; c.DeclMax = 0.15;
            c.Criterio = p.Criterio; c.DegrauMin = p.DegrauMinimo; c.DegrauTuboQueda = p.DegrauTuboQueda;
            c.Espessura = p.Espessura; c.TensaoMin = p.TensaoTrativaMinima;
            foreach (var d in p.Diametros.Where(d => d >= p.DiametroMinimo - 1e-9).OrderBy(d => d))
                c.Candidatos.Add(new Candidato(FormaSecao.Circular, d, d, 1));
        }
        else
        {
            var p = rede.Drenagem;
            c.Manning = p.Manning; c.LaminaMax = p.LaminaMaxima; c.VelMin = p.VelocidadeMinima; c.VelMax = p.VelocidadeMaxima;
            c.Recobrimento = p.RecobrimentoMinimo; c.ProfMax = p.ProfundidadeMaxima;
            c.DeclMin = p.DeclividadeMinima; c.DeclMax = p.DeclividadeMaxima;
            c.Criterio = p.Criterio; c.DegrauMin = p.DegrauMinimo;
            c.Espessura = p.Espessura;
            foreach (var d in p.Diametros.Where(d => d >= p.DiametroMinimo - 1e-9).OrderBy(d => d))
                c.Candidatos.Add(new Candidato(FormaSecao.Circular, d, d, 1));
            if (p.PermitirGalerias)
                foreach (var g in p.Galerias)
                {
                    var partes = g.ToLowerInvariant().Split('x', '×');
                    if (partes.Length == 2 && Num.TentarLer(partes[0], out var bb) && Num.TentarLer(partes[1], out var hh) && bb > 0 && hh > 0)
                        c.Candidatos.Add(new Candidato(FormaSecao.Retangular, bb, hh, 1));
                }
            // ordena por capacidade aproximada (área)
            c.Candidatos = c.Candidatos.OrderBy(k => k.AreaPlena).ToList();
        }
        return c;
    }

    private sealed class Tentativa
    {
        public bool Ok;
        public double ZA, ZB, S, Y, V, YI, Tensao, Vc;
        public List<string> Avisos = new(), Erros = new();
    }

    private static void DimensionarTrecho(Trecho t, ResultadoTrecho r, No a, No b, double L, double q, double qi,
        List<ResultadoTrecho> chegam, Cfg cfg)
    {
        // ---------------- candidatos
        List<Candidato> candidatos;
        bool secaoForcada = t.DiametroForcado is > 0;
        if (secaoForcada)
        {
            double bf = t.DiametroForcado!.Value;
            double hf = t.Forma == FormaSecao.Circular ? bf : (t.AlturaForcada ?? bf);
            candidatos = new() { new Candidato(t.Forma, bf, hf, Math.Max(1, t.Celulas)) };
        }
        else
        {
            double areaMin = chegam.Count == 0 ? 0 : chegam.Max(c => c.Celulas * (c.Forma == FormaSecao.Circular ? Math.PI * c.Diametro * c.Diametro / 4 : c.Diametro * c.Altura));
            bool chegaGaleria = chegam.Any(c => c.Forma == FormaSecao.Retangular);
            double hMin = chegam.Count == 0 ? 0 : chegam.Max(c => c.Altura);
            candidatos = cfg.Candidatos
                .Where(k => k.AreaPlena >= areaMin - 1e-9)
                .Where(k => !chegaGaleria || k.Forma == FormaSecao.Retangular)
                .Where(k => k.Forma != FormaSecao.Circular || k.B >= hMin - 1e-9)
                .Select(k => k with { Celulas = Math.Max(1, t.Celulas) })
                .ToList();
            if (candidatos.Count == 0)
            {
                // nenhuma seção comercial >= a de montante: repete a maior de montante
                var m = chegam.OrderByDescending(c => c.Diametro * c.Altura).First();
                candidatos = new() { new Candidato(m.Forma, m.Diametro, m.Altura, m.Celulas) };
            }
        }

        double n = t.Manning ?? cfg.Manning;
        Tentativa? escolhida = null; Candidato? cand = null;
        for (int k = 0; k < candidatos.Count; k++)
        {
            bool ultima = k == candidatos.Count - 1;
            var tent = Tentar(t, a, b, L, q, qi, chegam, cfg, candidatos[k], n, ultima);
            escolhida = tent; cand = candidatos[k];
            if (tent.Ok) break;
        }
        if (escolhida is null || cand is null) { r.Erros.Add("Sem seções candidatas"); return; }
        if (!escolhida.Ok && !secaoForcada) escolhida.Erros.Insert(0, "Seção insuficiente mesmo com a maior seção disponível");

        var sec = cand.Secao;
        r.Forma = cand.Forma; r.Diametro = cand.B; r.Altura = cand.H; r.Celulas = cand.Celulas;
        r.Espessura = cfg.Espessura(cand.Forma == FormaSecao.Circular ? cand.B : Math.Max(cand.B, cand.H));
        r.Declividade = escolhida.S;
        r.CotaFundoMontante = escolhida.ZA; r.CotaFundoJusante = escolhida.ZB;
        r.AlturaLamina = escolhida.Y; r.Velocidade = escolhida.V;
        r.VazaoPlena = Manning.VazaoPlena(sec, n, Math.Max(escolhida.S, 1e-9), cand.Celulas);
        r.VelocidadePlena = r.VazaoPlena / cand.AreaPlena;
        r.TempoPercurso = escolhida.V > 0.01 ? L / escolhida.V / 60.0 : L / Math.Max(r.VelocidadePlena, 0.3) / 60.0;
        r.TensaoTrativa = escolhida.Tensao; r.VelocidadeCritica = escolhida.Vc; r.LaminaInicial = escolhida.YI;
        r.Avisos.AddRange(escolhida.Avisos);
        r.Erros.AddRange(escolhida.Erros);

        // verificações finais de recobrimento/profundidade
        if (r.RecobrimentoMontante < cfg.Recobrimento - 0.005) r.Avisos.Add($"Recobrimento a montante {r.RecobrimentoMontante:0.00} m < {cfg.Recobrimento:0.00} m");
        if (r.RecobrimentoJusante < cfg.Recobrimento - 0.005) r.Avisos.Add($"Recobrimento a jusante {r.RecobrimentoJusante:0.00} m < {cfg.Recobrimento:0.00} m");
        if (r.ProfundidadeMontante > cfg.ProfMax || r.ProfundidadeJusante > cfg.ProfMax) r.Avisos.Add($"Profundidade acima de {cfg.ProfMax:0.00} m");
    }

    private static Tentativa Tentar(Trecho t, No a, No b, double L, double q, double qi, List<ResultadoTrecho> chegam, Cfg cfg,
        Candidato cand, double n, bool ultima)
    {
        var sec = cand.Secao;
        int cel = cand.Celulas;
        double H = cand.H;
        double e = cfg.Espessura(cand.Forma == FormaSecao.Circular ? cand.B : Math.Max(cand.B, cand.H));
        double tA = a.CotaTampa, tB = b.CotaTampa;

        double qiLs = qi * 1000, qLs = q * 1000;
        double sMin = cfg.DeclMin, sMax = cfg.DeclMax;
        if (cfg.Esgoto)
        {
            // NBR 9649: Imín = 0,0055·Qi^-0,47 (Qi em L/s, mínimo 1,5) e Imáx = 4,65·Qf^-0,67
            sMin = Math.Max(sMin, 0.0055 * Math.Pow(Math.Max(qiLs, 1.5), -0.47));
            sMax = Math.Max(sMin, Math.Min(sMax, 4.65 * Math.Pow(Math.Max(qLs, 1.5), -0.67)));
        }

        bool fZA = t.CotaMontanteForcada.HasValue, fZB = t.CotaJusanteForcada.HasValue, fS = t.DeclividadeForcada.HasValue;
        var tent = new Tentativa();

        double yEst = 0;
        for (int iter = 0; iter < 4; iter++)
        {
            tent = new Tentativa();
            // ---------- cota de fundo a montante
            double zA = tA - cfg.Recobrimento - H - e;
            if (chegam.Count > 0)
            {
                double lim = cfg.Criterio switch
                {
                    CriterioConcordancia.GeratrizSuperior => chegam.Min(c => c.CotaFundoJusante + c.Altura) - H,
                    CriterioConcordancia.GeratrizInferior => chegam.Min(c => c.CotaFundoJusante),
                    _ => chegam.Min(c => c.CotaFundoJusante + c.AlturaLamina) - yEst,
                } - cfg.DegrauMin;
                zA = Math.Min(zA, lim);
            }
            if (fZA) zA = t.CotaMontanteForcada!.Value;

            // ---------- declividade
            double zBcob = tB - cfg.Recobrimento - H - e;
            double s;
            if (fS) s = t.DeclividadeForcada!.Value;
            else if (fZB) s = (zA - t.CotaJusanteForcada!.Value) / L;
            else s = Math.Clamp((zA - zBcob) / L, sMin, sMax);

            void CoberturaJusante()
            {
                if (!fZA && !fZB && zA - s * L > zBcob + 1e-6) zA = zBcob + s * L;
            }
            CoberturaJusante();

            if (s <= 0)
            {
                tent.Erros.Add("Declividade nula ou negativa");
                tent.ZA = zA; tent.ZB = zA - s * L; tent.S = s;
                return tent;
            }

            // ---------- hidráulica
            double y = Manning.AlturaNormal(sec, q, n, s, cel);
            bool lamOk = !double.IsNaN(y) && y <= cfg.LaminaMax * H + 1e-9;
            if (!lamOk && ultima && !fS && !fZB)
            {
                // última seção: tenta aumentar a declividade para caber a vazão
                double sLam = Manning.DeclividadeParaLamina(sec, q, n, cfg.LaminaMax, cel);
                if (!double.IsNaN(sLam) && sLam <= sMax * 1.5)
                {
                    s = Math.Max(s, sLam * 1.0001);
                    y = Manning.AlturaNormal(sec, q, n, s, cel);
                    lamOk = !double.IsNaN(y) && y <= cfg.LaminaMax * H + 1e-9;
                    if (lamOk) tent.Avisos.Add("Declividade aumentada para comportar a vazão");
                }
            }
            if (double.IsNaN(y)) y = H;
            double v = Manning.Velocidade(sec, q, y, cel);

            if (q > 0 && cfg.VelMin > 0 && v < cfg.VelMin - 1e-6 && !fS && !fZB)
            {
                double sv = Manning.DeclividadeParaVelocidade(sec, q, n, cfg.VelMin, cel);
                if (!double.IsNaN(sv) && sv <= sMax)
                {
                    s = Math.Max(s, sv);
                    y = Manning.AlturaNormal(sec, q, n, s, cel);
                    v = Manning.Velocidade(sec, q, y, cel);
                }
            }
            if (q > 0 && v > cfg.VelMax + 1e-6 && !fS && !fZB)
            {
                double sv = Manning.DeclividadeParaVelocidade(sec, q, n, cfg.VelMax, cel);
                if (!double.IsNaN(sv))
                {
                    s = Math.Max(sMin, Math.Min(s, sv * 0.999));
                    CoberturaJusante();
                    y = Manning.AlturaNormal(sec, q, n, s, cel);
                    if (double.IsNaN(y)) y = H;
                    v = Manning.Velocidade(sec, q, y, cel);
                    lamOk = y <= cfg.LaminaMax * H + 1e-9;
                }
            }

            tent.ZA = zA; tent.S = s; tent.ZB = fZB ? t.CotaJusanteForcada!.Value : zA - s * L;
            if (fZB && !fS) tent.S = s = (zA - tent.ZB) / L;
            tent.Y = y; tent.V = v;
            tent.Ok = lamOk && v <= cfg.VelMax + 1e-6;

            if (!lamOk) tent.Erros.Add($"Lâmina y/D = {(y / H):0.00} acima de {cfg.LaminaMax:0.00}");
            if (v > cfg.VelMax + 1e-6) (tent.Ok ? tent.Avisos : tent.Erros).Add($"Velocidade {v:0.00} m/s acima da máxima {cfg.VelMax:0.00}");
            if (q > 0 && cfg.VelMin > 0 && v < cfg.VelMin - 1e-6) tent.Avisos.Add($"Velocidade {v:0.00} m/s abaixo da mínima {cfg.VelMin:0.00}");
            if (q <= 0) tent.Avisos.Add("Trecho sem vazão de contribuição");

            if (cfg.Esgoto)
            {
                double yi = Manning.AlturaNormal(sec, qi, n, s, cel);
                if (double.IsNaN(yi)) yi = H;
                tent.YI = yi;
                tent.Tensao = Manning.TensaoTrativa(sec, yi, s);
                tent.Vc = Manning.VelocidadeCritica(sec, y);
                if (tent.Tensao < cfg.TensaoMin - 1e-6) tent.Avisos.Add($"Tensão trativa {tent.Tensao:0.00} Pa < {cfg.TensaoMin:0.0} Pa");
                if (v > tent.Vc && y > 0.5 * H + 1e-9)
                {
                    tent.Ok = false;
                    tent.Erros.Add($"Velocidade final {v:0.00} m/s > crítica {tent.Vc:0.00} m/s: lâmina deve ser ≤ 0,50");
                }
            }

            if (cfg.Criterio != CriterioConcordancia.LaminaDagua || chegam.Count == 0 || Math.Abs(y - yEst) < 0.001) break;
            yEst = y;
        }
        return tent;
    }
}
