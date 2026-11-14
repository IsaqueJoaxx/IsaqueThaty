using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Projeta.Sinalizacao.Civil;
using Projeta.Sinalizacao.Core.Comum;
using Projeta.Sinalizacao.Core.Dados;
using Projeta.Sinalizacao.Core.Quantitativos;
using Projeta.Sinalizacao.Infra;

namespace Projeta.Sinalizacao.Comandos;

/// <summary>Consulta, edição, correção, localização, numeração, quantitativos, cadastro e 3D.</summary>
public sealed class CmdGestao
{
    internal static string Resumo(ObjetoSin o)
    {
        var l = new List<string> { $"{o.Id} — {o.Descricao} [{o.Codigo}] — {ObjetoSin.NomeSituacao(o.Situacao)}" };
        foreach (var kv in o.Quant.Where(k => k.Key != "cor")) l.Add($"   {kv.Key}: {Num.F(kv.Value, 3)}");
        if (!string.IsNullOrWhiteSpace(o.Obs)) l.Add("   obs.: " + o.Obs);
        return string.Join("\n", l);
    }

    [CommandMethod("PROJETA_SIN", "SINCONSULTA", CommandFlags.Modal)]
    public void Consulta()
    {
        try
        {
            using var s = Sessao.Abrir();
            var sel = s.PedirObjeto("Selecione um objeto da sinalização");
            if (sel is null) return;
            var (id, o) = sel.Value;
            Ctx.Msg(Resumo(o));
            if (Ctx.ComInterface && UI.Janelas.Editar(o, s.Normas))
            {
                s.Atualizar(id, o);
                Ctx.Msg("Objeto atualizado:\n" + Resumo(o));
            }
            s.Confirmar();
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINSITUACAO", CommandFlags.Modal | CommandFlags.UsePickSet)]
    public void SituacaoCmd()
    {
        try
        {
            using var s = Sessao.Abrir();
            var kw = Selecao.Palavra(s.Ed, "Nova situação", new[] { "Implantar", "Manter", "Remover", "Recuperar" }, "Implantar");
            if (kw is null) return;
            var objs = s.SelecionarObjetos("Selecione os objetos", false);
            var sit = Enum.Parse<Situacao>(kw);
            foreach (var (id, o) in objs) { o.Situacao = sit; s.Atualizar(id, o); }
            s.Confirmar();
            Ctx.Msg($"{objs.Count} objeto(s) marcados como {ObjetoSin.NomeSituacao(sit)}.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINCORRIGE", CommandFlags.Modal)]
    public void Corrige()
    {
        try
        {
            using var s = Sessao.Abrir();
            var objs = s.SelecionarObjetos("Selecione os objetos a regenerar");
            int rep = Operacoes.CorrigirIdsRepetidos(s, objs);
            int n = 0;
            foreach (var (id, _) in objs) if (s.Regenerar(id)) n++;
            s.Confirmar();
            Ctx.Msg($"{n} objeto(s) regenerado(s) com os padrões e normas atuais; {rep} ID(s) repetido(s) corrigido(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINLOCALIZAR", CommandFlags.Modal)]
    public void Localizar()
    {
        try
        {
            using var s = Sessao.Abrir();
            var t = Selecao.Texto(s.Ed, "ID ou código a localizar (ex.: PL-012, R-19, FTP-1)", "", false);
            if (string.IsNullOrWhiteSpace(t)) return;
            var todos = Armazenamento.Todos(s.Tr, s.Db)
                .Where(x => x.Obj.Id.Equals(t, StringComparison.OrdinalIgnoreCase) || x.Obj.Codigo.Contains(t, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Obj.Id).ToList();
            if (todos.Count == 0) { Ctx.Msg("Nada encontrado."); return; }
            int i = todos.Count == 1 ? 0 : Selecao.Lista(s.Ed, "Encontrados", todos.Select(x => $"{x.Obj.Id} {x.Obj.Codigo} — {x.Obj.Descricao}").ToList());
            if (i < 0) return;
            var br = (BlockReference)s.Tr.GetObject(todos[i].Id, OpenMode.ForRead);
            var ext = br.GeometricExtents;
            Zoom(s.Ed, ext, 1.5);
            s.Ed.SetImpliedSelection(new[] { todos[i].Id });
            Ctx.Msg(Resumo(todos[i].Obj));
            s.Confirmar();
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    internal static void Zoom(Editor ed, Extents3d ext, double margem)
    {
        using var v = ed.GetCurrentView();
        var c = new Point2d((ext.MinPoint.X + ext.MaxPoint.X) / 2, (ext.MinPoint.Y + ext.MaxPoint.Y) / 2);
        double w = Math.Max(5, (ext.MaxPoint.X - ext.MinPoint.X) * margem), h = Math.Max(5, (ext.MaxPoint.Y - ext.MinPoint.Y) * margem);
        double r = v.Width / Math.Max(1e-9, v.Height);
        if (w / h > r) h = w / r; else w = h * r;
        var dcs = Matrix3d.PlaneToWorld(v.ViewDirection);
        dcs = Matrix3d.Displacement(v.Target - Point3d.Origin) * dcs;
        dcs = Matrix3d.Rotation(-v.ViewTwist, v.ViewDirection, v.Target) * dcs;
        dcs = dcs.Inverse();
        var cw = new Point3d(c.X, c.Y, 0).TransformBy(dcs);
        v.CenterPoint = new Point2d(cw.X, cw.Y);
        v.Width = w; v.Height = h;
        ed.SetCurrentView(v);
    }

    [CommandMethod("PROJETA_SIN", "SINNUMERAR", CommandFlags.Modal)]
    public void Numerar()
    {
        try
        {
            using var s = Sessao.Abrir();
            var todos = Armazenamento.Todos(s.Tr, s.Db);
            var eixos = Eixos.CivilDisponivel ? Eixos.Alinhamentos(s.Tr) : new();
            var ordem = todos.Select(x => (x.Id, x.Obj, loc: Operacoes.Localizar(s.Tr, x.Obj, eixos)))
                .OrderBy(x => x.Obj.Categoria).ThenBy(x => x.loc.Eixo).ThenBy(x => x.loc.Estaca ?? double.MaxValue).ThenBy(x => x.loc.E).ThenBy(x => x.loc.N).ToList();
            s.Cfg.Contadores.Clear();
            foreach (var (id, o, _) in ordem)
            {
                o.Id = s.Cfg.ProximoId(o.Categoria);
                s.Atualizar(id, o);
            }
            s.Confirmar();
            Ctx.Msg($"{ordem.Count} objeto(s) renumerado(s)" + (eixos.Count > 0 ? " pela ordem de estaqueamento." : " pela posição."));
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    private static List<(ObjectId, string)> EscolherEixos(Sessao s)
    {
        if (!Eixos.CivilDisponivel) return new();
        var al = Eixos.Alinhamentos(s.Tr);
        if (al.Count == 0) { Ctx.Msg("Nenhum alinhamento no desenho: a planilha sai só com coordenadas."); return new(); }
        var itens = new List<string> { "Automático (alinhamento mais próximo de cada objeto)", "Nenhum (só coordenadas)" };
        itens.AddRange(al.Select(a => a.Nome));
        int i = Selecao.Lista(s.Ed, "Alinhamento para estaca e lado", itens, 0);
        if (i < 0) return new();
        return i == 0 ? al : i == 1 ? new() : new() { al[i - 2] };
    }

    [CommandMethod("PROJETA_SIN", "SINQUANT", CommandFlags.Modal)]
    public void Quantitativos()
    {
        try
        {
            using var s = Sessao.Abrir();
            var eixos = EscolherEixos(s);
            var objs = s.SelecionarObjetos("Selecione os objetos para o quantitativo");
            if (objs.Count == 0) { Ctx.Msg("Nenhum objeto de sinalização no desenho."); return; }
            Operacoes.CorrigirIdsRepetidos(s, objs);
            foreach (var (id, _) in objs) s.Regenerar(id);
            objs = objs.Select(x => (x.Id, s.Ler(x.Id)!)).ToList();
            string dwg = s.Db.Filename;
            string pasta = string.IsNullOrWhiteSpace(dwg) || !Path.IsPathRooted(dwg) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Path.GetDirectoryName(dwg)!;
            string nome = (string.IsNullOrWhiteSpace(dwg) ? "Desenho" : Path.GetFileNameWithoutExtension(dwg)) + "_quantitativos.xlsx";
            string? arq = Ctx.ComInterface ? UI.Janelas.SalvarComo("Planilha de quantitativos", "Excel (*.xlsx)|*.xlsx", Path.Combine(pasta, nome)) : Path.Combine(pasta, nome);
            if (arq is null) return;
            string titulo = "Quantitativos de sinalização — " + Path.GetFileNameWithoutExtension(nome).Replace("_quantitativos", "");
            try { Operacoes.ExportarQuantitativos(s, objs, eixos, arq, titulo); }
            catch (IOException ex) { Ctx.Msg($"Não foi possível gravar {arq}: {ex.Message} (a planilha está aberta no Excel?)"); return; }
            s.Confirmar();
            Ctx.Msg($"Quantitativos de {objs.Count} objeto(s) gravados em {arq}");
            if (Ctx.ComInterface) try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(arq) { UseShellExecute = true }); } catch { }
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINIMPORTAR", CommandFlags.Modal)]
    public void Importar()
    {
        try
        {
            using var s = Sessao.Abrir();
            string? arq = Ctx.ComInterface ? UI.Janelas.Abrir("Cadastro de placas", "Planilha (*.xlsx;*.csv)|*.xlsx;*.csv") : Selecao.Texto(s.Ed, "Arquivo CSV/XLSX do cadastro", "");
            if (string.IsNullOrWhiteSpace(arq) || !File.Exists(arq)) return;
            var linhas = Cadastro.Ler(arq, out var avisos);
            foreach (var a in avisos) Ctx.Msg("[CADASTRO] " + a);
            int n = Operacoes.ImportarCadastro(s, linhas);
            s.Confirmar();
            Ctx.Msg($"{n} conjunto(s) de placas importado(s) de {Path.GetFileName(arq)}.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SINMODELOCADASTRO", CommandFlags.Modal)]
    public void ModeloCadastro()
    {
        try
        {
            string dwg = Ctx.Db.Filename;
            string pasta = string.IsNullOrWhiteSpace(dwg) || !Path.IsPathRooted(dwg) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : Path.GetDirectoryName(dwg)!;
            string arq = Path.Combine(pasta, "modelo_cadastro_placas.csv");
            File.WriteAllText(arq, Cadastro.ModeloCsv(), new System.Text.UTF8Encoding(true));
            Ctx.Msg($"Modelo gravado em {arq}. Colunas: COD; SUP; X; Y; ROT (graus, para onde a face olha); SIT; DIM; VALOR.");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SIN3D", CommandFlags.Modal)]
    public void Gerar3D()
    {
        try
        {
            using var s = Sessao.Abrir();
            ObjectId sup = ObjectId.Null;
            if (Eixos.CivilDisponivel)
            {
                var sups = Eixos.Superficies(s.Tr);
                if (sups.Count > 0)
                {
                    var itens = new List<string> { "Nenhuma (cota zero)" };
                    itens.AddRange(sups.Select(x => x.Nome));
                    int i = Selecao.Lista(s.Ed, "Superfície do terreno para as cotas", itens, sups.Count == 1 ? 1 : 0);
                    if (i < 0) return;
                    if (i > 0) sup = sups[i - 1].Id;
                }
            }
            var objs = s.SelecionarObjetos("Selecione os objetos");
            int n = Bim.Gerar(s, objs, sup);
            s.Confirmar();
            Ctx.Msg($"{n} sólido(s) 3D gerados nos layers SIN-3D-* (placas, suportes, semafórica, obras e defensas).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }

    [CommandMethod("PROJETA_SIN", "SIN3DAPAGAR", CommandFlags.Modal)]
    public void Apagar3D()
    {
        try
        {
            using var s = Sessao.Abrir();
            int n = 0;
            foreach (ObjectId id in Ctx.EspacoModelo(s.Tr, s.Db, OpenMode.ForRead))
            {
                var e = s.Tr.GetObject(id, OpenMode.ForRead) as Entity;
                if (e is null || e.ExtensionDictionary.IsNull) continue;
                if (Armazenamento.LerTexto(s.Tr, e, Armazenamento.Chave3D) is null) continue;
                e.UpgradeOpen(); e.Erase(); n++;
            }
            s.Confirmar();
            Ctx.Msg($"{n} sólido(s) 3D apagado(s).");
        }
        catch (System.Exception ex) { Infra.Erros.Mostrar(ex); }
    }
}
