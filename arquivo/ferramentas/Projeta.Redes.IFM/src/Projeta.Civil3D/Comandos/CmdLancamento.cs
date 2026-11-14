using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Civil3D.Desenho;
using Projeta.Civil3D.Infra;
using Projeta.Civil3D.UI;
using Projeta.Core.Comum;
using Projeta.Core.Hidrologia;
using Projeta.Core.Modelador;
using Projeta.Core.Redes;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Projeta.Civil3D.Comandos;

public sealed class CmdLancamento
{
    private static TipoNo? LerTipoNo(string? s) => s switch
    {
        "PV" => TipoNo.PV, "BL" => TipoNo.BocaDeLobo, "Caixa" => TipoNo.Caixa, "Exutorio" => TipoNo.Exutorio,
        "Juncao" => TipoNo.Juncao, "Reservatorio" => TipoNo.Reservatorio, "Tanque" => TipoNo.Tanque, _ => null,
    };

    private static string[] TiposPermitidos(Rede r) => r.Tipo == TipoRede.Agua
        ? new[] { "Juncao", "Reservatorio", "Tanque" }
        : new[] { "PV", "BL", "Caixa", "Exutorio" };

    private static No? NoProximo(Rede r, Point3d p, double tol) =>
        r.Nos.Select(n => (n, d: new P2(n.X, n.Y).Distancia(new P2(p.X, p.Y)))).Where(x => x.d <= tol).OrderBy(x => x.d).Select(x => x.n).FirstOrDefault();

    /// <summary>PRJLANCAR — lança nós e tubos clicando os pontos de montante para jusante.</summary>
    [CommandMethod("PRJLANCAR", CommandFlags.Modal)]
    public static void Lancar() => Operacoes.Rodar("PRJLANCAR", s =>
    {
        var rede = s.EscolherRede();
        if (rede is null) return;
        var tipos = TiposPermitidos(rede);
        var tipoAtual = rede.Tipo == TipoRede.Agua ? TipoNo.Juncao : TipoNo.PV;
        double tol = Math.Max(0.5, rede.AlturaTexto);
        No? anterior = null;
        var criados = new Stack<(No? no, Trecho? trecho)>();
        Ctx.Msg("Clique os nós de MONTANTE para JUSANTE. Clicar sobre um nó existente conecta a ele. Enter termina.");
        while (true)
        {
            string desc = tipoAtual.ToString();
            var ppo = new PromptPointOptions($"\nPróximo nó ({desc}){(anterior is null ? "" : " a jusante de " + anterior.Nome)} ou [Tipo/Novo/Desfazer] <terminar>: ", "Tipo Novo Desfazer")
            {
                AllowNone = true, AppendKeywordsToMessage = false,
            };
            if (anterior is not null) { ppo.UseBasePoint = true; ppo.BasePoint = new Point3d(anterior.X, anterior.Y, 0); }
            var r = s.Ed.GetPoint(ppo);
            if (r.Status == PromptStatus.Keyword)
            {
                switch (r.StringResult)
                {
                    case "Tipo":
                        var t = LerTipoNo(s.PedirPalavra("Tipo dos próximos nós", tipos, tipos.Contains(desc) ? desc : tipos[0]));
                        if (t.HasValue) tipoAtual = t.Value;
                        break;
                    case "Novo":
                        anterior = null;
                        Ctx.Msg("Novo ramal: clique o primeiro nó (pode ser um nó existente).");
                        break;
                    case "Desfazer":
                        if (criados.Count == 0) break;
                        var (no, tr) = criados.Pop();
                        if (tr is not null) rede.Trechos.Remove(tr);
                        if (no is not null) rede.RemoverNo(no.Id);
                        anterior = tr is not null ? rede.NoPorId(tr.NoMontanteId) : null;
                        Ctx.Msg("Último lançamento desfeito.");
                        break;
                }
                continue;
            }
            if (r.Status != PromptStatus.OK) break;
            var existente = NoProximo(rede, r.Value, tol);
            No atual;
            bool novo = existente is null;
            if (existente is not null) { atual = existente; Ctx.Msg($"Conectado ao nó existente {existente.Nome}."); }
            else atual = rede.NovoNo(tipoAtual, r.Value.X, r.Value.Y);
            Trecho? trecho = null;
            if (anterior is not null && anterior != atual && !rede.Trechos.Any(t => t.NoMontanteId == anterior.Id && t.NoJusanteId == atual.Id))
                trecho = rede.NovoTrecho(anterior, atual);
            criados.Push((novo ? atual : null, trecho));
            anterior = atual;
            if (atual.Tipo == TipoNo.Exutorio) { anterior = null; Ctx.Msg("Exutório lançado — clique para iniciar outro ramal ou Enter."); }
        }
        if (criados.Count == 0) return;
        Operacoes.Atualizar(s, rede, sincronizar: false);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJNO — insere um nó avulso.</summary>
    [CommandMethod("PRJNO", CommandFlags.Modal)]
    public static void InserirNo() => Operacoes.Rodar("PRJNO", s =>
    {
        var rede = s.EscolherRede();
        if (rede is null) return;
        var tipos = TiposPermitidos(rede);
        var tipo = LerTipoNo(s.PedirPalavra("Tipo do nó", tipos, tipos[0]));
        if (tipo is null) return;
        int n = 0;
        while (true)
        {
            var r = s.Ed.GetPoint(new PromptPointOptions("\nPosição do nó <terminar>: ") { AllowNone = true });
            if (r.Status != PromptStatus.OK) break;
            var no = rede.NovoNo(tipo.Value, r.Value.X, r.Value.Y);
            if (tipo == TipoNo.Reservatorio) no.Carga = s.PedirNumero($"Nível d'água do reservatório {no.Nome} (m)", 0) ?? 0;
            n++;
        }
        if (n == 0) return;
        Operacoes.Atualizar(s, rede, sincronizar: false);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJTUBO — liga dois nós existentes (montante → jusante).</summary>
    [CommandMethod("PRJTUBO", CommandFlags.Modal)]
    public static void LigarTubo() => Operacoes.Rodar("PRJTUBO", s =>
    {
        var a = s.PedirItem("Nó de montante");
        if (a?.No is null || a.Rede is null) { Ctx.Msg("Selecione um nó."); return; }
        var b = s.PedirItem("Nó de jusante");
        if (b?.No is null || b.Rede != a.Rede) { Ctx.Msg("O nó de jusante deve ser da mesma rede."); return; }
        if (a.No == b.No) return;
        var t = a.Rede.NovoTrecho(a.No, b.No);
        Ctx.Msg($"Trecho {t.Nome}: {a.No.Nome} → {b.No.Nome}");
        Operacoes.Atualizar(s, a.Rede);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJBACIA — transforma polilinhas fechadas em bacias de contribuição.</summary>
    [CommandMethod("PRJBACIA", CommandFlags.Modal)]
    public static void CriarBacia() => Operacoes.Rodar("PRJBACIA", s =>
    {
        var rede = s.EscolherRede("Selecione um elemento da rede de drenagem", r => r.Tipo == TipoRede.Drenagem);
        if (rede is null) return;
        var filtro = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE,POLYLINE,CIRCLE") });
        var sel = s.Ed.GetSelection(new PromptSelectionOptions { MessageForAdding = "\nSelecione as polilinhas fechadas das bacias: " }, filtro);
        if (sel.Status != PromptStatus.OK) return;
        double cPadrao = rede.Bacias.LastOrDefault()?.C ?? 0.70;
        int criadas = 0;
        foreach (var id in sel.Value.GetObjectIds())
        {
            var c = (Curve)s.Tr.GetObject(id, OpenMode.ForRead);
            if (!c.Closed) { Ctx.Msg("Polilinha aberta ignorada."); continue; }
            string h = id.Handle.ToString();
            var bac = rede.Bacias.FirstOrDefault(b => b.HandleContorno == h);
            bool nova = bac is null;
            bac ??= new Bacia { Nome = rede.ProximoNome("B-", rede.Bacias.Select(x => x.Nome)), HandleContorno = h };
            bac.Contorno = DesenhoRede.PontosCurva(c);
            bac.AreaHa = Math.Abs(c.Area) / 10000.0;
            var cen = Geo.Centroide(bac.Contorno);
            var prox = rede.Nos.OrderBy(n => new P2(n.X, n.Y).Distancia(cen)).FirstOrDefault();
            if (prox is null) { Ctx.Msg("A rede não tem nós."); return; }
            Ctx.Msg($"Bacia {bac.Nome}: {Num.F(bac.AreaHa, 3)} ha. Nó mais próximo do centroide: {prox.Nome}.");
            var peo = new PromptEntityOptions($"\nNó de lançamento da bacia {bac.Nome} ou [Proximo] <Proximo>: ", "Proximo") { AllowNone = true };
            var pr = s.Ed.GetEntity(peo);
            No no = prox;
            if (pr.Status == PromptStatus.OK)
            {
                var it = s.Localizar(pr.ObjectId);
                if (it?.No is not null && it.Rede == rede) no = it.No; else Ctx.Msg("Não é nó desta rede — usando o mais próximo.");
            }
            else if (pr.Status == PromptStatus.Cancel) return;
            bac.NoId = no.Id;
            var cc = s.PedirNumero($"Coeficiente de escoamento C da bacia {bac.Nome}", bac.C > 0 && !nova ? bac.C : cPadrao);
            if (cc is null) return;
            bac.C = cc.Value; cPadrao = cc.Value;
            var peoT = new PromptEntityOptions($"\nTalvegue da bacia {bac.Nome} (polilinha, para Kirpich) <usar tc informado>: ") { AllowNone = true };
            peoT.SetRejectMessage("\nSelecione uma curva.");
            peoT.AddAllowedClass(typeof(Curve), false);
            var rt = s.Ed.GetEntity(peoT);
            if (rt.Status == PromptStatus.OK) { bac.HandleTalvegue = rt.ObjectId.Handle.ToString(); bac.MetodoTc = MetodoTc.Kirpich; }
            else if (nova) { bac.MetodoTc = MetodoTc.Informado; bac.TcInformado = rede.Drenagem.TcMinimo; }
            if (nova) rede.Bacias.Add(bac);
            criadas++;
        }
        if (criadas == 0) return;
        Operacoes.Atualizar(s, rede);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJREMOVER — remove nós, tubos, bacias ou dispositivos longitudinais.</summary>
    [CommandMethod("PRJREMOVER", CommandFlags.Modal | CommandFlags.UsePickSet)]
    public static void Remover() => Operacoes.Rodar("PRJREMOVER", s =>
    {
        var itens = s.SelecionarItens("Selecione os elementos a remover");
        if (itens.Count == 0) return;
        var redes = new HashSet<Rede>();
        foreach (var it in itens)
        {
            if (it.Longitudinal is not null)
            {
                foreach (var h in it.Longitudinal.HandlesDesenho) Ctx.Apagar(s.Tr, s.Db, h);
                s.Projeto.Longitudinais.Remove(it.Longitudinal);
                continue;
            }
            if (it.Rede is null) continue;
            redes.Add(it.Rede);
            if (it.No is not null)
            {
                foreach (var t in it.Rede.Trechos.Where(t => t.NoMontanteId == it.No.Id || t.NoJusanteId == it.No.Id).ToList())
                { Ctx.Apagar(s.Tr, s.Db, t.HandleLinha); Ctx.Apagar(s.Tr, s.Db, t.HandleRotulo); Ctx.Apagar(s.Tr, s.Db, t.HandleSolido); }
                Ctx.Apagar(s.Tr, s.Db, it.No.HandleBloco); Ctx.Apagar(s.Tr, s.Db, it.No.HandleRotulo);
                it.Rede.RemoverNo(it.No.Id);
            }
            else if (it.Trecho is not null)
            {
                Ctx.Apagar(s.Tr, s.Db, it.Trecho.HandleLinha); Ctx.Apagar(s.Tr, s.Db, it.Trecho.HandleRotulo); Ctx.Apagar(s.Tr, s.Db, it.Trecho.HandleSolido);
                it.Rede.Trechos.Remove(it.Trecho);
            }
            else if (it.Bacia is not null)
            {
                Ctx.Apagar(s.Tr, s.Db, it.Bacia.HandleRotulo);
                it.Rede.Bacias.Remove(it.Bacia);
            }
        }
        foreach (var r in redes) Operacoes.Atualizar(s, r);
        s.SalvarEConfirmar();
        Ctx.Msg($"{itens.Count} elemento(s) removido(s).");
    });

    /// <summary>PRJCONECTAR — muda o nó de jusante (ou montante) de um tubo.</summary>
    [CommandMethod("PRJCONECTAR", CommandFlags.Modal)]
    public static void Conectar() => Operacoes.Rodar("PRJCONECTAR", s =>
    {
        var it = s.PedirItem("Selecione o tubo");
        if (it?.Trecho is null || it.Rede is null) { Ctx.Msg("Selecione um tubo."); return; }
        var lado = s.PedirPalavra("Reconectar qual extremidade", new[] { "Jusante", "Montante" }, "Jusante");
        if (lado is null) return;
        var no = s.PedirItem("Novo nó");
        if (no?.No is null || no.Rede != it.Rede) { Ctx.Msg("Selecione um nó da mesma rede."); return; }
        if (lado == "Jusante") { if (no.No.Id == it.Trecho.NoMontanteId) return; it.Trecho.NoJusanteId = no.No.Id; }
        else { if (no.No.Id == it.Trecho.NoJusanteId) return; it.Trecho.NoMontanteId = no.No.Id; }
        Operacoes.Atualizar(s, it.Rede);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJINVERTER — inverte o sentido dos tubos selecionados.</summary>
    [CommandMethod("PRJINVERTER", CommandFlags.Modal | CommandFlags.UsePickSet)]
    public static void Inverter() => Operacoes.Rodar("PRJINVERTER", s =>
    {
        var itens = s.SelecionarItens("Selecione os tubos").Where(i => i.Trecho is not null).ToList();
        foreach (var it in itens)
        {
            (it.Trecho!.NoMontanteId, it.Trecho.NoJusanteId) = (it.Trecho.NoJusanteId, it.Trecho.NoMontanteId);
            (it.Trecho.CotaMontanteForcada, it.Trecho.CotaJusanteForcada) = (it.Trecho.CotaJusanteForcada, it.Trecho.CotaMontanteForcada);
        }
        foreach (var r in itens.Select(i => i.Rede!).Distinct()) Operacoes.Atualizar(s, r);
        s.SalvarEConfirmar();
        Ctx.Msg($"{itens.Count} tubo(s) invertido(s).");
    });

    /// <summary>PRJDIVIDIR — insere um nó sobre um tubo, dividindo-o em dois.</summary>
    [CommandMethod("PRJDIVIDIR", CommandFlags.Modal)]
    public static void Dividir() => Operacoes.Rodar("PRJDIVIDIR", s =>
    {
        var it = s.PedirItem("Selecione o tubo a dividir");
        if (it?.Trecho is null || it.Rede is null) return;
        var rede = it.Rede; var t = it.Trecho;
        var a = rede.NoPorId(t.NoMontanteId)!; var b = rede.NoPorId(t.NoJusanteId)!;
        var r = s.Ed.GetPoint("\nPonto do novo nó sobre o tubo: ");
        if (r.Status != PromptStatus.OK) return;
        var ab = new P2(b.X - a.X, b.Y - a.Y);
        double u = Math.Clamp(new P2(r.Value.X - a.X, r.Value.Y - a.Y).Dot(ab) / Math.Max(ab.Dot(ab), 1e-9), 0.02, 0.98);
        var novo = rede.NovoNo(rede.Tipo == TipoRede.Agua ? TipoNo.Juncao : TipoNo.PV, a.X + ab.X * u, a.Y + ab.Y * u);
        var t2 = rede.NovoTrecho(novo, b);
        t2.Forma = t.Forma; t2.DiametroForcado = t.DiametroForcado; t2.AlturaForcada = t.AlturaForcada; t2.Celulas = t.Celulas; t2.Material = t.Material; t2.Manning = t.Manning; t2.RugosidadeHW = t.RugosidadeHW;
        t2.CotaJusanteForcada = t.CotaJusanteForcada;
        t.NoJusanteId = novo.Id; t.CotaJusanteForcada = null; t.ComprimentoForcado = null; t.DeclividadeForcada = null;
        Ctx.Msg($"Novo nó {novo.Nome}; trechos {t.Nome} e {t2.Nome}.");
        Operacoes.Atualizar(s, rede);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJTROCAR — troca o dispositivo (modelo) dos nós selecionados.</summary>
    [CommandMethod("PRJTROCAR", CommandFlags.Modal | CommandFlags.UsePickSet)]
    public static void Trocar() => Operacoes.Rodar("PRJTROCAR", s =>
    {
        var itens = s.SelecionarItens("Selecione os nós").Where(i => i.No is not null).ToList();
        if (itens.Count == 0) return;
        var defs = Sessao.Bib.Dispositivos.Values.OrderBy(d => d.Categoria).ThenBy(d => d.Id).ToList();
        int i = s.EscolherDaLista("Dispositivos da biblioteca", defs.Select(d => $"{d.Id} — {d.Nome} [{d.Categoria}]").ToList());
        if (i < 0) return;
        foreach (var it in itens)
        {
            it.No!.DispositivoId = defs[i].Id;
            foreach (var k in it.No.ParametrosDispositivo.Keys.Where(k => !k.StartsWith("__")).ToList()) it.No.ParametrosDispositivo.Remove(k);
        }
        foreach (var r in itens.Select(x => x.Rede!).Distinct()) Operacoes.Atualizar(s, r, calcular: false);
        s.SalvarEConfirmar();
    });

    /// <summary>PRJPROPRIEDADES — edita um nó, tubo ou bacia (e os parâmetros do dispositivo do nó).</summary>
    [CommandMethod("PRJPROPRIEDADES", CommandFlags.Modal)]
    public static void Propriedades() => Operacoes.Rodar("PRJPROPRIEDADES", s =>
    {
        var it = s.PedirItem("Selecione nó, tubo, bacia ou dispositivo longitudinal");
        if (it is null) return;
        if (it.Longitudinal is not null) { Ctx.Msg("Use PRJLONGEDITAR para dispositivos longitudinais."); return; }
        object obj = (object?)it.No ?? (object?)it.Trecho ?? it.Bacia!;
        using (var f = new FormPropriedades("PROJETA — IFM — Propriedades", ("Propriedades", obj)))
            if (AcApp.ShowModalDialog(f) != System.Windows.Forms.DialogResult.OK) return;
        if (it.No is not null && Sessao.Bib.Dispositivos.TryGetValue(it.No.DispositivoId, out var def)
            && s.PedirPalavra("Editar os parâmetros do dispositivo", new[] { "Sim", "Nao" }, "Nao") == "Sim")
        {
            var rn = it.Rede?.Resultado?.No(it.No.Id);
            var valoresRede = new Dictionary<string, double>(it.No.ParametrosDispositivo, StringComparer.OrdinalIgnoreCase);
            if (rn is not null) { valoresRede["Profundidade"] = rn.Profundidade; valoresRede.TryAdd("DiametroMaximo", rn.MaiorSecao); }
            var mod = AvaliadorDispositivo.Avaliar(def, valoresRede);
            using var fp = new FormParametros($"Parâmetros de {it.No.Nome} ({def.Id})", def.Parametros.Where(p => p.Origem != "rede"),
                it.No.ParametrosDispositivo, mod.Valores);
            if (AcApp.ShowModalDialog(fp) == System.Windows.Forms.DialogResult.OK)
            {
                foreach (var k in it.No.ParametrosDispositivo.Keys.Where(k => !k.StartsWith("__")).ToList()) it.No.ParametrosDispositivo.Remove(k);
                foreach (var kv in fp.Valores) it.No.ParametrosDispositivo[kv.Key] = kv.Value;
            }
        }
        if (it.Rede is not null) Operacoes.Atualizar(s, it.Rede);
        s.SalvarEConfirmar();
    });
}
