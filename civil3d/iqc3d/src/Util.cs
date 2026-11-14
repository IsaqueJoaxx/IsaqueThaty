using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace IQC3D
{
    /// <summary>Funções de apoio usadas por todos os comandos.</summary>
    internal static class U
    {
        public static readonly CultureInfo BR = new CultureInfo("pt-BR");

        public static Document Doc => AcApp.DocumentManager.MdiActiveDocument;
        public static Editor Ed => Doc.Editor;
        public static Database Db => Doc.Database;
        public static CivilDocument Civil => CivilDocument.GetCivilDocument(Db);

        public static void Msg(string s) => Ed.WriteMessage("\n" + s);

        /// <summary>Executa o corpo de um comando mostrando o erro na linha de comando em vez de travar.</summary>
        public static void Rodar(Action corpo)
        {
            try { corpo(); }
            catch (System.Exception ex)
            {
                try
                {
                    Msg("ERRO: " + ex.Message);
                    var pilha = (ex.StackTrace ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Contains("IQC3D")).Take(3);
                    foreach (var l in pilha) Msg("   " + l);
                }
                catch { }
            }
        }

        /// <summary>Estaca brasileira: 20 m por estaca. 245.3 -> "12+5,30".</summary>
        public static string Estaca(double station, double passo = 20.0)
        {
            double cm = Math.Round(station * 100.0);           // trabalha em centimetros
            if (cm < 0) cm = 0;
            long passoCm = (long)Math.Round(passo * 100.0);
            long est = (long)cm / passoCm;
            double resto = (cm - est * passoCm) / 100.0;
            return est.ToString(BR) + "+" + resto.ToString("0.00", BR);
        }

        public static ObjectId GarantirLayer(Transaction tr, string nome, short cor)
        {
            var lt = (LayerTable)tr.GetObject(Db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(nome)) return lt[nome];
            lt.UpgradeOpen();
            var ltr = new LayerTableRecord { Name = nome, Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(Autodesk.AutoCAD.Colors.ColorMethod.ByAci, cor) };
            var id = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            return id;
        }

        public static BlockTableRecord ModelSpace(Transaction tr, OpenMode modo = OpenMode.ForWrite)
        {
            var bt = (BlockTable)tr.GetObject(Db.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], modo);
        }

        public static ObjectId Adicionar(Transaction tr, Entity ent)
        {
            var ms = ModelSpace(tr);
            var id = ms.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);
            return id;
        }

        /// <summary>Seleção com filtro por DXF name (ex.: "AECC_COGO_POINT").</summary>
        public static ObjectId[] Selecionar(string mensagem, params string[] dxfNames)
        {
            var opts = new PromptSelectionOptions { MessageForAdding = "\n" + mensagem };
            PromptSelectionResult r;
            if (dxfNames != null && dxfNames.Length > 0)
            {
                var filtro = new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, string.Join(",", dxfNames)) });
                r = Ed.GetSelection(opts, filtro);
            }
            else r = Ed.GetSelection(opts);
            return r.Status == PromptStatus.OK ? r.Value.GetObjectIds() : null;
        }

        /// <summary>Pede um objeto de um tipo específico (classe .NET).</summary>
        public static ObjectId PegarObjeto<T>(string mensagem, string rejeita) where T : Entity
        {
            var o = new PromptEntityOptions("\n" + mensagem);
            o.SetRejectMessage("\n" + rejeita);
            o.AddAllowedClass(typeof(T), false);
            var r = Ed.GetEntity(o);
            return r.Status == PromptStatus.OK ? r.ObjectId : ObjectId.Null;
        }

        public static double? PedirDouble(string msg, double padrao, bool permitirZero = true, bool permitirNegativo = false)
        {
            var o = new PromptDoubleOptions("\n" + msg) { DefaultValue = padrao, UseDefaultValue = true, AllowZero = permitirZero, AllowNegative = permitirNegativo };
            var r = Ed.GetDouble(o);
            return r.Status == PromptStatus.OK ? r.Value : (double?)null;
        }

        public static string PedirTexto(string msg, string padrao, bool espacos = true)
        {
            var o = new PromptStringOptions("\n" + msg + (string.IsNullOrEmpty(padrao) ? "" : $" <{padrao}>")) { AllowSpaces = espacos };
            var r = Ed.GetString(o);
            if (r.Status != PromptStatus.OK) return null;
            return string.IsNullOrWhiteSpace(r.StringResult) ? padrao : r.StringResult.Trim();
        }

        /// <summary>Palavra-chave. Retorna null se cancelado.</summary>
        public static string PedirOpcao(string msg, string padrao, params string[] opcoes)
        {
            msg = System.Text.RegularExpressions.Regex.Replace(msg, @"\s*\[[^\]]*\]\s*$", "");
            var o = new PromptKeywordOptions("\n" + msg);
            foreach (var k in opcoes) o.Keywords.Add(k);
            o.Keywords.Default = padrao;
            o.AllowNone = true;
            var r = Ed.GetKeywords(o);
            if (r.Status == PromptStatus.None) return padrao;
            return r.Status == PromptStatus.OK ? r.StringResult : null;
        }

        /// <summary>Escolher uma superfície: clicar ou digitar o nome (Enter lista).</summary>
        public static ObjectId EscolherSuperficie(string msg)
        {
            var civ = Civil;
            var ids = civ.GetSurfaceIds().Cast<ObjectId>().ToList();
            if (ids.Count == 0) { Msg("Nenhuma superfície no desenho."); return ObjectId.Null; }
            var o = new PromptEntityOptions("\n" + msg + " (clique) ou [Nome]");
            o.SetRejectMessage("\nIsso não é uma superfície.");
            o.AddAllowedClass(typeof(Autodesk.Civil.DatabaseServices.Surface), false);
            o.Keywords.Add("Nome");
            var r = Ed.GetEntity(o);
            if (r.Status == PromptStatus.OK) return r.ObjectId;
            if (r.Status != PromptStatus.Keyword) return ObjectId.Null;
            using (var tr = Db.TransactionManager.StartOpenCloseTransaction())
            {
                var nomes = ids.Select(i => ((Autodesk.Civil.DatabaseServices.Surface)tr.GetObject(i, OpenMode.ForRead)).Name).ToList();
                for (int i = 0; i < nomes.Count; i++) Msg($"  {i + 1} - {nomes[i]}");
                var n = Ed.GetInteger(new PromptIntegerOptions("\nNúmero da superfície: ") { LowerLimit = 1, UpperLimit = nomes.Count });
                return n.Status == PromptStatus.OK ? ids[n.Value - 1] : ObjectId.Null;
            }
        }

        public static string F(double v, int casas = 3) => v.ToString("F" + casas, BR);

        /// <summary>Intersecção de duas retas infinitas (p1 + t d1, p2 + s d2) no plano XY.</summary>
        public static Point2d? IntersecaoRetas(Point2d p1, Vector2d d1, Point2d p2, Vector2d d2)
        {
            double den = d1.X * d2.Y - d1.Y * d2.X;
            if (Math.Abs(den) < 1e-12) return null;
            var w = p2 - p1;
            double t = (w.X * d2.Y - w.Y * d2.X) / den;
            return p1 + d1 * t;
        }
    }

    /// <summary>Gravador mínimo de .xlsx (sem dependências), só texto e números.</summary>
    internal sealed class Xlsx
    {
        private readonly List<(string nome, List<object[]> linhas)> _abas = new List<(string, List<object[]>)>();

        public List<object[]> NovaAba(string nome)
        {
            var l = new List<object[]>();
            var limpo = new string(nome.Where(c => "[]:*?/\\".IndexOf(c) < 0).ToArray());
            if (limpo.Length > 31) limpo = limpo.Substring(0, 31);
            if (limpo.Length == 0) limpo = "Aba" + (_abas.Count + 1);
            while (_abas.Any(a => a.nome == limpo)) limpo = (limpo.Length > 28 ? limpo.Substring(0, 28) : limpo) + "_" + _abas.Count;
            _abas.Add((limpo, l));
            return l;
        }

        private static string Col(int i) { var s = ""; i++; while (i > 0) { int m = (i - 1) % 26; s = (char)('A' + m) + s; i = (i - 1) / 26; } return s; }
        private static string Esc(string s) => System.Security.SecurityElement.Escape(s ?? "");

        public void Salvar(string caminho)
        {
            if (File.Exists(caminho)) File.Delete(caminho);
            using (var zip = ZipFile.Open(caminho, ZipArchiveMode.Create))
            {
                void Put(string n, string conteudo)
                {
                    var e = zip.CreateEntry(n, CompressionLevel.Optimal);
                    using (var w = new StreamWriter(e.Open(), new UTF8Encoding(false))) w.Write(conteudo);
                }
                var ct = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
                for (int i = 0; i < _abas.Count; i++) ct.Append($"<Override PartName=\"/xl/worksheets/sheet{i + 1}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                ct.Append("</Types>");
                Put("[Content_Types].xml", ct.ToString());
                Put("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
                var wb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
                var rels = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                for (int i = 0; i < _abas.Count; i++)
                {
                    wb.Append($"<sheet name=\"{Esc(_abas[i].nome)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
                    rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
                }
                wb.Append("</sheets></workbook>");
                rels.Append($"<Relationship Id=\"rId{_abas.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                Put("xl/workbook.xml", wb.ToString());
                Put("xl/_rels/workbook.xml.rels", rels.ToString());
                // estilo 0 = normal; 1 = negrito (cabeçalho); 2 = número 0,000
                Put("xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"0.000\"/></numFmts><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border/></borders><cellStyleXfs count=\"1\"><xf/></cellStyleXfs><cellXfs count=\"3\"><xf/><xf fontId=\"1\" applyFont=\"1\"/><xf numFmtId=\"164\" applyNumberFormat=\"1\"/></cellXfs></styleSheet>");
                for (int i = 0; i < _abas.Count; i++)
                {
                    var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetData>");
                    var linhas = _abas[i].linhas;
                    for (int r = 0; r < linhas.Count; r++)
                    {
                        sb.Append($"<row r=\"{r + 1}\">");
                        var lin = linhas[r];
                        for (int c = 0; c < lin.Length; c++)
                        {
                            var v = lin[c];
                            string refc = Col(c) + (r + 1);
                            if (v == null) continue;
                            if (v is double d)
                            {
                                if (double.IsNaN(d)) continue;
                                sb.Append($"<c r=\"{refc}\" s=\"2\"><v>{d.ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                            }
                            else if (v is int n) sb.Append($"<c r=\"{refc}\"><v>{n}</v></c>");
                            else sb.Append($"<c r=\"{refc}\" t=\"inlineStr\"{(r == 0 ? " s=\"1\"" : "")}><is><t>{Esc(v.ToString())}</t></is></c>");
                        }
                        sb.Append("</row>");
                    }
                    sb.Append("</sheetData></worksheet>");
                    Put($"xl/worksheets/sheet{i + 1}.xml", sb.ToString());
                }
            }
        }
    }
}
