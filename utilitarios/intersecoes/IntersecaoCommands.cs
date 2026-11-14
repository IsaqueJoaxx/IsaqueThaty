using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Civil.DatabaseServices;

using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;
using CivilAlignment = Autodesk.Civil.DatabaseServices.Alignment;

// Plugin de "interseção" para Civil 3D 2025+
// -----------------------------------------------------------------------------
// IMPORTANTE (limitação da plataforma):
//   A API .NET do Civil 3D NÃO possui método para criar o objeto nativo
//   "Intersection" (confirmado pela Autodesk). Este comando reproduz o
//   resultado na prática: cria as curvas de concordância (raios), monta o
//   corredor a partir dos alinhamentos + perfis + assembly, e gera a
//   superfície do corredor. Ajuste os pontos marcados com  >>> AJUSTE <<<
//   conforme os nomes/códigos do seu template.
// -----------------------------------------------------------------------------

[assembly: CommandClass(typeof(IntersecaoPlugin.IntersecaoCommands))]

namespace IntersecaoPlugin
{
    public class IntersecaoCommands
    {
        [CommandMethod("CRIARINTERSECAO")]
        public void CriarIntersecao()
        {
            Document doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            CivilDocument civilDoc = CivilApplication.ActiveDocument;

            try
            {
                // ---------- 1) Seleção dos dois alinhamentos ----------
                ObjectId almPrincipalId = SelecionarAlinhamento(ed, "\nSelecione o ALINHAMENTO PRINCIPAL: ");
                if (almPrincipalId.IsNull) return;
                ObjectId almSecundarioId = SelecionarAlinhamento(ed, "\nSelecione o ALINHAMENTO SECUNDARIO: ");
                if (almSecundarioId.IsNull) return;

                // ---------- 2) Raios das curvas (4 quadrantes) ----------
                double raioPadrao = PedirDouble(ed, "\nRaio padrao das curvas de concordancia (m)", 9.0, 0.001);
                double[] raios = new double[4];
                bool iguais = PedirSimNao(ed, "\nUsar o mesmo raio para as 4 esquinas?", true);
                if (iguais)
                {
                    for (int i = 0; i < 4; i++) raios[i] = raioPadrao;
                }
                else
                {
                    raios[0] = PedirDouble(ed, "\nRaio esquina 1 (Principal+ / Secundario+)", raioPadrao, 0.001);
                    raios[1] = PedirDouble(ed, "\nRaio esquina 2 (Secundario+ / Principal-)", raioPadrao, 0.001);
                    raios[2] = PedirDouble(ed, "\nRaio esquina 3 (Principal- / Secundario-)", raioPadrao, 0.001);
                    raios[3] = PedirDouble(ed, "\nRaio esquina 4 (Secundario- / Principal+)", raioPadrao, 0.001);
                }

                // ---------- 3) Cota da intersecao ----------
                double cota = PedirDouble(ed, "\nCota (elevacao) da intersecao (m)", 0.0, double.MinValue);

                // ---------- 4) Largura de pavimento (meia-pista) ----------
                double meiaPista = PedirDouble(ed, "\nMeia-largura da pista / offset da borda (m)", 3.5, 0.0);

                // ---------- 5) Assembly do corredor ----------
                ObjectId assemblyId = SelecionarAssembly(ed, civilDoc);
                if (assemblyId.IsNull) return;

                // ---------- 6) Nomes e opcoes ----------
                string nomeCorredor = PedirString(ed, "\nNome do corredor", "Corredor_Intersecao");
                string nomeSuperficie = PedirString(ed, "\nNome da superficie", "Sup_Intersecao");
                bool criarOffsets = PedirSimNao(ed, "\nCriar alinhamentos de offset das bordas?", true);

                // ---------- 7) Calcular ponto de cruzamento ----------
                Point3d pontoCruz;
                Vector2d dirPrincipal, dirSecundario;
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    CivilAlignment almP = (CivilAlignment)tr.GetObject(almPrincipalId, OpenMode.ForRead);
                    CivilAlignment almS = (CivilAlignment)tr.GetObject(almSecundarioId, OpenMode.ForRead);

                    if (!CalcularCruzamento(almP, almS, out Point2d pc2d, out dirPrincipal, out dirSecundario))
                    {
                        ed.WriteMessage("\nNao foi possivel encontrar o cruzamento dos alinhamentos.");
                        tr.Commit();
                        return;
                    }
                    pontoCruz = new Point3d(pc2d.X, pc2d.Y, cota);
                    tr.Commit();
                }

                ed.WriteMessage("\nCruzamento em X={0:F3}  Y={1:F3}  Cota={2:F3}",
                    pontoCruz.X, pontoCruz.Y, cota);

                // ---------- 8) Desenhar as 4 curvas de concordancia ----------
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    BlockTableRecord ms = (BlockTableRecord)tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForWrite);

                    ObjectId layerCurvas = GarantirLayer(tr, db, "INT-CURVAS-CONCORDANCIA", 1);

                    // As 4 direcoes que saem do cruzamento (sentido +/- de cada eixo)
                    Vector2d[] dirs =
                    {
                        dirPrincipal,          // 0  Principal +
                        dirSecundario,         // 1  Secundario +
                        dirPrincipal.Negate(), // 2  Principal -
                        dirSecundario.Negate() // 3  Secundario -
                    };
                    Point2d centro = new Point2d(pontoCruz.X, pontoCruz.Y);

                    // Quadrantes formados por pares de direcoes adjacentes
                    (int a, int b)[] quadrantes = { (0, 1), (1, 2), (2, 3), (3, 0) };

                    for (int q = 0; q < 4; q++)
                    {
                        Arc arco = CriarConcordancia(
                            centro, dirs[quadrantes[q].a], dirs[quadrantes[q].b], raios[q], meiaPista, cota);
                        if (arco != null)
                        {
                            arco.LayerId = layerCurvas;
                            ms.AppendEntity(arco);
                            tr.AddNewlyCreatedDBObject(arco, true);
                        }
                    }
                    tr.Commit();
                }
                ed.WriteMessage("\nCurvas de concordancia criadas.");

                // ---------- 8.1) Garantir perfil de projeto em cada alinhamento ----------
                GarantirPerfil(doc, civilDoc, almPrincipalId, cota, ed);
                GarantirPerfil(doc, civilDoc, almSecundarioId, cota, ed);

                // ---------- 8.2) Alinhamentos de offset das bordas ----------
                if (criarOffsets)
                {
                    CriarOffsets(doc, almPrincipalId, meiaPista, ed);
                    CriarOffsets(doc, almSecundarioId, meiaPista, ed);
                }

                // ---------- 9) Criar o corredor ----------
                ObjectId corridorId = CriarCorredor(
                    doc, civilDoc, nomeCorredor, almPrincipalId, almSecundarioId, assemblyId, ed);

                if (corridorId.IsNull)
                {
                    ed.WriteMessage("\nCorredor nao pode ser criado (verifique se os alinhamentos tem perfil).");
                    return;
                }
                ed.WriteMessage("\nCorredor '{0}' criado.", nomeCorredor);

                // ---------- 10) Criar a superficie do corredor ----------
                CriarSuperficieCorredor(doc, corridorId, nomeSuperficie, ed);

                ed.WriteMessage("\n\nIntersecao concluida: {0} curvas, corredor e superficie gerados.", 4);
            }
            catch (Autodesk.AutoCAD.Runtime.Exception acEx)
            {
                ed.WriteMessage("\nErro AutoCAD: {0}", acEx.Message);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\nErro: {0}", ex.Message);
            }
        }

        // ============================================================
        //  HELPERS DE ENTRADA
        // ============================================================
        private ObjectId SelecionarAlinhamento(Editor ed, string prompt)
        {
            var opts = new PromptEntityOptions(prompt);
            opts.SetRejectMessage("\nSelecione um ALINHAMENTO do Civil 3D.");
            opts.AddAllowedClass(typeof(CivilAlignment), true);
            PromptEntityResult res = ed.GetEntity(opts);
            return res.Status == PromptStatus.OK ? res.ObjectId : ObjectId.Null;
        }

        private double PedirDouble(Editor ed, string msg, double def, double minimo)
        {
            var opts = new PromptDoubleOptions($"{msg} <{def}>: ")
            {
                DefaultValue = def,
                UseDefaultValue = true,
                AllowNegative = minimo < 0,
                AllowZero = minimo <= 0
            };
            PromptDoubleResult r = ed.GetDouble(opts);
            if (r.Status != PromptStatus.OK) return def;
            if (r.Value < minimo) return def;
            return r.Value;
        }

        private string PedirString(Editor ed, string msg, string def)
        {
            var opts = new PromptStringOptions($"{msg} <{def}>: ")
            {
                AllowSpaces = false,
                DefaultValue = def,
                UseDefaultValue = true
            };
            PromptResult r = ed.GetString(opts);
            return (r.Status == PromptStatus.OK && !string.IsNullOrWhiteSpace(r.StringResult))
                ? r.StringResult : def;
        }

        private bool PedirSimNao(Editor ed, string msg, bool def)
        {
            var opts = new PromptKeywordOptions($"{msg} ")
            {
                AllowNone = true
            };
            opts.Keywords.Add("Sim");
            opts.Keywords.Add("Nao");
            opts.Keywords.Default = def ? "Sim" : "Nao";
            PromptResult r = ed.GetKeywords(opts);
            if (r.Status != PromptStatus.OK) return def;
            return r.StringResult == "Sim";
        }

        private ObjectId SelecionarAssembly(Editor ed, CivilDocument civilDoc)
        {
            var opts = new PromptEntityOptions("\nSelecione o ASSEMBLY do corredor: ");
            opts.SetRejectMessage("\nSelecione um Assembly do Civil 3D.");
            opts.AddAllowedClass(typeof(Autodesk.Civil.DatabaseServices.Assembly), true);
            PromptEntityResult res = ed.GetEntity(opts);
            return res.Status == PromptStatus.OK ? res.ObjectId : ObjectId.Null;
        }

        // ============================================================
        //  GEOMETRIA
        // ============================================================

        // Encontra o cruzamento amostrando os dois alinhamentos como polilinhas.
        private bool CalcularCruzamento(CivilAlignment almP, CivilAlignment almS,
            out Point2d cruz, out Vector2d dirP, out Vector2d dirS)
        {
            cruz = new Point2d();
            dirP = new Vector2d();
            dirS = new Vector2d();

            using (Polyline plP = AlinhamentoParaPolyline(almP))
            using (Polyline plS = AlinhamentoParaPolyline(almS))
            {
                var pts = new Point3dCollection();
                plP.IntersectWith(plS, Intersect.OnBothOperands, pts, IntPtr.Zero, IntPtr.Zero);
                if (pts.Count == 0) return false;

                Point3d p = pts[0];
                cruz = new Point2d(p.X, p.Y);

                dirP = DirecaoNoPonto(almP, cruz);
                dirS = DirecaoNoPonto(almS, cruz);
                return true;
            }
        }

        // Amostra o alinhamento em passos curtos, gerando uma polilinha 2D.
        private Polyline AlinhamentoParaPolyline(CivilAlignment alm)
        {
            var pl = new Polyline();
            double comprimento = alm.EndingStation - alm.StartingStation;
            int n = Math.Max(50, (int)(comprimento / 1.0)); // passo ~1 m
            double passo = comprimento / n;
            int idx = 0;
            for (int i = 0; i <= n; i++)
            {
                double est = alm.StartingStation + passo * i;
                double x = 0, y = 0;
                try { alm.PointLocation(est, 0.0, ref x, ref y); }
                catch { continue; }
                pl.AddVertexAt(idx++, new Point2d(x, y), 0, 0, 0);
            }
            return pl;
        }

        // Direcao tangente (unitaria) do alinhamento no ponto informado.
        private Vector2d DirecaoNoPonto(CivilAlignment alm, Point2d p)
        {
            double est = 0, off = 0;
            alm.StationOffset(p.X, p.Y, ref est, ref off);
            double d = 0.5;
            double est2 = Math.Min(est + d, alm.EndingStation);
            double est1 = Math.Max(est - d, alm.StartingStation);
            double x1 = 0, y1 = 0, x2 = 0, y2 = 0;
            alm.PointLocation(est1, 0.0, ref x1, ref y1);
            alm.PointLocation(est2, 0.0, ref x2, ref y2);
            var v = new Vector2d(x2 - x1, y2 - y1);
            return v.Length > 1e-9 ? v.GetNormal() : new Vector2d(1, 0);
        }

        // Cria o arco de concordancia (raio) tangente as duas bordas de pista.
        // As bordas sao as retas paralelas aos eixos, deslocadas de "meiaPista".
        private Arc CriarConcordancia(Point2d centroCruz, Vector2d dirA, Vector2d dirB,
            double raio, double meiaPista, double cota)
        {
            // Perpendiculares (para a esquerda de cada direcao)
            Vector2d perpA = new Vector2d(-dirA.Y, dirA.X);
            Vector2d perpB = new Vector2d(-dirB.Y, dirB.X);

            // A quina do quadrante fica entre dirA e dirB; a borda de cada eixo
            // eh deslocada para o lado interno do quadrante.
            Vector2d bissetriz = (dirA + dirB);
            if (bissetriz.Length < 1e-9) return null;
            bissetriz = bissetriz.GetNormal();

            // Escolhe o sinal do offset que joga a borda para dentro do quadrante.
            double sA = (perpA.DotProduct(bissetriz) >= 0) ? 1.0 : -1.0;
            double sB = (perpB.DotProduct(bissetriz) >= 0) ? 1.0 : -1.0;

            // Retas das bordas: ponto + direcao
            Point2d pontoBordaA = centroCruz + perpA * (sA * meiaPista);
            Point2d pontoBordaB = centroCruz + perpB * (sB * meiaPista);

            // Interseccao das duas retas de borda = quina real do meio-fio
            if (!InterseccaoRetas(pontoBordaA, dirA, pontoBordaB, dirB, out Point2d quina))
                return null;

            // Angulo entre as duas direcoes (apontando para longe da quina)
            Vector2d uA = dirA.GetNormal();
            Vector2d uB = dirB.GetNormal();
            // Garante que as direcoes apontem para "fora" a partir da quina
            if (uA.DotProduct(bissetriz) < 0) uA = uA.Negate();
            if (uB.DotProduct(bissetriz) < 0) uB = uB.Negate();

            double ang = uA.GetAngleTo(uB);
            if (ang < 1e-6 || Math.Abs(ang - Math.PI) < 1e-6) return null;

            double tanDist = raio / Math.Tan(ang / 2.0);   // quina -> ponto de tangencia
            double centDist = raio / Math.Sin(ang / 2.0);  // quina -> centro do arco

            Point2d tangA = quina + uA * tanDist;
            Point2d tangB = quina + uB * tanDist;
            Point2d centroArco = quina + bissetriz * centDist;

            Point3d c3 = new Point3d(centroArco.X, centroArco.Y, cota);
            Point3d tA3 = new Point3d(tangA.X, tangA.Y, cota);
            Point3d tB3 = new Point3d(tangB.X, tangB.Y, cota);

            double a0 = Math.Atan2(tA3.Y - c3.Y, tA3.X - c3.X);
            double a1 = Math.Atan2(tB3.Y - c3.Y, tB3.X - c3.X);

            // Normaliza para o menor arco
            var arco = new Arc(c3, raio, a0, a1);
            if (arco.Length > Math.PI * raio) // pegou o arco maior; inverte
                arco = new Arc(c3, raio, a1, a0);
            return arco;
        }

        private bool InterseccaoRetas(Point2d p1, Vector2d d1, Point2d p2, Vector2d d2, out Point2d inter)
        {
            inter = new Point2d();
            double det = d1.X * d2.Y - d1.Y * d2.X;
            if (Math.Abs(det) < 1e-9) return false; // paralelas
            double t = ((p2.X - p1.X) * d2.Y - (p2.Y - p1.Y) * d2.X) / det;
            inter = p1 + d1 * t;
            return true;
        }

        private ObjectId GarantirLayer(Transaction tr, Database db, string nome, short cor)
        {
            LayerTable lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            if (lt.Has(nome)) return lt[nome];
            lt.UpgradeOpen();
            var ltr = new LayerTableRecord
            {
                Name = nome,
                Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                    Autodesk.AutoCAD.Colors.ColorMethod.ByAci, cor)
            };
            ObjectId id = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            return id;
        }

        // ============================================================
        //  CORREDOR E SUPERFICIE
        // ============================================================
        private ObjectId CriarCorredor(Document doc, CivilDocument civilDoc, string nome,
            ObjectId almPId, ObjectId almSId, ObjectId assemblyId, Editor ed)
        {
            ObjectId corridorId = ObjectId.Null;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Obtem o primeiro perfil de cada alinhamento (perfil de projeto).
                ObjectId perfilP = PrimeiroPerfil(tr, almPId);
                ObjectId perfilS = PrimeiroPerfil(tr, almSId);
                if (perfilP.IsNull || perfilS.IsNull)
                {
                    ed.WriteMessage("\nCada alinhamento precisa ter ao menos um PERFIL.");
                    tr.Commit();
                    return ObjectId.Null;
                }

                corridorId = civilDoc.CorridorCollection.Add(nome);
                Corridor corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForWrite);

                // Baseline do alinhamento principal
                Baseline blP = corridor.Baselines.Add("BL_Principal", almPId, perfilP);
                blP.BaselineRegions.Add("Reg_Principal", assemblyId);

                // Baseline do alinhamento secundario
                Baseline blS = corridor.Baselines.Add("BL_Secundario", almSId, perfilS);
                blS.BaselineRegions.Add("Reg_Secundario", assemblyId);

                tr.Commit();
            }

            // Rebuild fora da transacao de escrita
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Corridor corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForWrite);
                corridor.Rebuild();
                tr.Commit();
            }
            return corridorId;
        }

        private ObjectId PrimeiroPerfil(Transaction tr, ObjectId alinhamentoId)
        {
            CivilAlignment alm = (CivilAlignment)tr.GetObject(alinhamentoId, OpenMode.ForRead);
            foreach (ObjectId pid in alm.GetProfileIds())
            {
                Profile prof = tr.GetObject(pid, OpenMode.ForRead) as Profile;
                // >>> AJUSTE <<< se quiser filtrar por tipo (ProfileType.FG = greide de projeto)
                if (prof != null && prof.ProfileType == ProfileType.FG)
                    return pid;
            }
            // fallback: primeiro perfil qualquer
            var ids = alm.GetProfileIds();
            return ids.Count > 0 ? ids[0] : ObjectId.Null;
        }

        // Se o alinhamento nao tiver perfil de projeto, cria um greide PLANO na cota.
        private void GarantirPerfil(Document doc, CivilDocument civilDoc,
            ObjectId alinhamentoId, double cota, Editor ed)
        {
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                CivilAlignment alm = (CivilAlignment)tr.GetObject(alinhamentoId, OpenMode.ForRead);

                // Ja existe perfil FG? entao nao faz nada.
                foreach (ObjectId pid in alm.GetProfileIds())
                {
                    Profile p = tr.GetObject(pid, OpenMode.ForRead) as Profile;
                    if (p != null && p.ProfileType == ProfileType.FG) { tr.Commit(); return; }
                }

                // Estilos (usa o primeiro disponivel)
                ObjectId estiloPerfil = PrimeiroEstiloPerfil(civilDoc);
                ObjectId labelSet = PrimeiroLabelSetPerfil(civilDoc);
                ObjectId layerId = GarantirLayer(tr, db, "INT-PERFIL-PROJETO", 3);

                string nomePerfil = "Greide_" + alm.Name;
                ObjectId profId = Profile.CreateByLayout(
                    nomePerfil, alinhamentoId, layerId, estiloPerfil, labelSet);

                Profile prof = (Profile)tr.GetObject(profId, OpenMode.ForWrite);

                double staIni = alm.StartingStation;
                double staFim = alm.EndingStation;
                // Tangente unica e plana: mesma cota do inicio ao fim.
                prof.Entities.AddFixedTangent(
                    new Point2d(staIni, cota),
                    new Point2d(staFim, cota));

                tr.Commit();
                ed.WriteMessage("\nPerfil plano '{0}' criado na cota {1:F3}.", nomePerfil, cota);
            }
        }

        private ObjectId PrimeiroEstiloPerfil(CivilDocument civilDoc)
        {
            var estilos = civilDoc.Styles.ProfileStyles;
            foreach (ObjectId id in estilos) return id;
            return ObjectId.Null;
        }

        private ObjectId PrimeiroLabelSetPerfil(CivilDocument civilDoc)
        {
            var sets = civilDoc.Styles.LabelSetStyles.ProfileLabelSetStyles;
            foreach (ObjectId id in sets) return id;
            return ObjectId.Null;
        }

        // Cria dois alinhamentos de offset (bordas +/- meiaPista) a partir do eixo.
        private void CriarOffsets(Document doc, ObjectId alinhamentoId, double meiaPista, Editor ed)
        {
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                CivilAlignment alm = (CivilAlignment)tr.GetObject(alinhamentoId, OpenMode.ForRead);
                try
                {
                    // Offset positivo e negativo geram as duas bordas da pista.
                    alm.CreateOffsetAlignment(meiaPista);
                    alm.CreateOffsetAlignment(-meiaPista);
                    ed.WriteMessage("\nOffsets (+/-{0:F2} m) criados para '{1}'.", meiaPista, alm.Name);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("\nNao foi possivel criar offset de '{0}': {1}", alm.Name, ex.Message);
                }
                tr.Commit();
            }
        }

        private void CriarSuperficieCorredor(Document doc, ObjectId corridorId, string nomeSup, Editor ed)
        {
            Database db = doc.Database;
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Corridor corridor = (Corridor)tr.GetObject(corridorId, OpenMode.ForWrite);

                CorridorSurface sup = corridor.CorridorSurfaces.Add(nomeSup);
                // >>> AJUSTE <<< o codigo de link conforme seu assembly.
                // "Top" eh o mais comum para a superficie de acabamento.
                try { sup.AddLinkCode("Top", true); }
                catch { sup.AddLinkCode("Datum", true); } // fallback

                corridor.Rebuild();
                tr.Commit();
            }
            ed.WriteMessage("\nSuperficie '{0}' criada a partir do corredor.", nomeSup);
        }
    }
}
