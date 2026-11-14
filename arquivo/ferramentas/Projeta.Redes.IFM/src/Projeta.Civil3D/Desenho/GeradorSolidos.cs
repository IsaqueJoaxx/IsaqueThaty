using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Projeta.Core.Comum;
using Projeta.Core.Modelador;

namespace Projeta.Civil3D.Desenho;

/// <summary>Converte a árvore de sólidos do modelador (núcleo) em Solid3d do AutoCAD.</summary>
internal static class GeradorSolidos
{
    public static Solid3d? Criar(Solido s, List<string> erros)
    {
        try { return CriarInterno(s, erros); }
        catch (System.Exception ex)
        {
            erros.Add($"{s.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static Solid3d Novo()
    {
        var so = new Solid3d();
        so.SetDatabaseDefaults();
        return so;
    }

    private static Solid3d? CriarInterno(Solido s, List<string> erros)
    {
        switch (s)
        {
            case SolCaixa c:
                {
                    var so = Novo();
                    so.CreateBox(c.X, c.Y, c.Z);
                    so.TransformBy(Matrix3d.Displacement(new Vector3d(c.Base.X, c.Base.Y, c.Base.Z + c.Z / 2)));
                    return so;
                }
            case SolCilindro cy:
                {
                    var so = Novo();
                    so.CreateFrustum(cy.Altura, cy.RaioBase, cy.RaioBase, cy.RaioTopo);
                    var eixo = new Vector3d(cy.Eixo.X, cy.Eixo.Y, cy.Eixo.Z).GetNormal();
                    var centro = new Point3d(cy.Base.X, cy.Base.Y, cy.Base.Z) + eixo * (cy.Altura / 2);
                    so.TransformBy(Matrix3d.Displacement(centro - Point3d.Origin) * AlinharZ(eixo));
                    return so;
                }
            case SolEsfera e:
                {
                    var so = Novo();
                    so.CreateSphere(e.Raio);
                    so.TransformBy(Matrix3d.Displacement(new Vector3d(e.Centro.X, e.Centro.Y, e.Centro.Z)));
                    return so;
                }
            case SolExtrusao ex:
                {
                    using var reg = Regiao(ex.Perfil);
                    var so = Novo();
                    so.Extrude(reg, ex.Altura, 0);
                    switch (ex.Plano)
                    {
                        case "XZ":
                            // (u, v, w) -> (u, w, v): rotação de 90° em X seguida de deslocamento em Y
                            so.TransformBy(Matrix3d.Displacement(new Vector3d(0, ex.Altura, 0)) * Matrix3d.Rotation(Math.PI / 2, Vector3d.XAxis, Point3d.Origin));
                            break;
                        case "YZ":
                            // (u, v, w) -> (w, u, v)
                            so.TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.ZAxis, Point3d.Origin) * Matrix3d.Rotation(Math.PI / 2, Vector3d.XAxis, Point3d.Origin));
                            break;
                    }
                    return so;
                }
            case SolRevolucao rv:
                {
                    using var reg = Regiao(rv.Perfil);
                    reg.TransformBy(Matrix3d.Rotation(Math.PI / 2, Vector3d.XAxis, Point3d.Origin)); // (r, z) no plano XZ
                    var so = Novo();
                    so.Revolve(reg, Point3d.Origin, Vector3d.ZAxis, rv.AnguloGraus * Math.PI / 180.0);
                    return so;
                }
            case SolBooleano b:
                {
                    var baseSol = CriarInterno(b.Base, erros);
                    if (baseSol is null) return null;
                    var tipo = b.Tipo switch
                    {
                        "uniao" => BooleanOperationType.BoolUnite,
                        "subtrair" => BooleanOperationType.BoolSubtract,
                        _ => BooleanOperationType.BoolIntersect,
                    };
                    foreach (var o in b.Outros)
                    {
                        var outro = Criar(o, erros);
                        if (outro is null) continue;
                        try { baseSol.BooleanOperation(tipo, outro); }
                        catch (System.Exception ex) { erros.Add($"Operação {b.Tipo}: {ex.Message}"); }
                        finally { outro.Dispose(); }
                    }
                    return baseSol;
                }
            case SolTransformado t:
                {
                    var so = CriarInterno(t.Base, erros);
                    so?.TransformBy(new Matrix3d(t.Matriz.M));
                    return so;
                }
        }
        erros.Add("Tipo de sólido não suportado: " + s.GetType().Name);
        return null;
    }

    /// <summary>Região a partir de um polígono 2D no plano XY.</summary>
    public static Region Regiao(IReadOnlyList<P2> pts)
    {
        using var pl = new Polyline();
        for (int i = 0; i < pts.Count; i++) pl.AddVertexAt(i, new Point2d(pts[i].X, pts[i].Y), 0, 0, 0);
        pl.Closed = true;
        var curvas = new DBObjectCollection { pl };
        var regs = Region.CreateFromCurves(curvas);
        if (regs.Count == 0) throw new InvalidOperationException("Perfil não forma uma região fechada");
        var reg = (Region)regs[0];
        for (int i = 1; i < regs.Count; i++) regs[i].Dispose();
        return reg;
    }

    /// <summary>Matriz que leva o eixo Z ao vetor informado.</summary>
    public static Matrix3d AlinharZ(Vector3d eixo)
    {
        var z = Vector3d.ZAxis;
        double dot = z.DotProduct(eixo);
        if (dot > 1 - 1e-12) return Matrix3d.Identity;
        if (dot < -1 + 1e-12) return Matrix3d.Rotation(Math.PI, Vector3d.XAxis, Point3d.Origin);
        var eixoRot = z.CrossProduct(eixo).GetNormal();
        return Matrix3d.Rotation(Math.Acos(dot), eixoRot, Point3d.Origin);
    }

    /// <summary>Tubo (cilindro oco ou galeria) entre dois pontos de fundo, com espessura de parede.</summary>
    public static List<Solid3d> Tubo(Point3d fundoIni, Point3d fundoFim, bool circular, double largura, double altura, double espessura, int celulas, List<string> erros)
    {
        var lista = new List<Solid3d>();
        var dir = fundoFim - fundoIni;
        double comp = dir.Length;
        if (comp < 0.01) return lista;
        var u = dir.GetNormal();
        var lateral = new Vector3d(-u.Y, u.X, 0);
        if (lateral.Length < 1e-9) lateral = Vector3d.XAxis; else lateral = lateral.GetNormal();
        var acima = u.CrossProduct(lateral).GetNormal(); // sempre com Z >= 0; (lateral, acima, u) é destro
        double passo = largura + 2 * espessura + 0.15;
        for (int k = 0; k < Math.Max(1, celulas); k++)
        {
            double desl = (k - (celulas - 1) / 2.0) * passo;
            var meio = fundoIni + dir * 0.5 + lateral * desl + Vector3d.ZAxis * (altura / 2);
            try
            {
                var ext = Novo(); var inte = Novo();
                if (circular)
                {
                    ext.CreateFrustum(comp, largura / 2 + espessura, largura / 2 + espessura, largura / 2 + espessura);
                    inte.CreateFrustum(comp + 0.02, largura / 2, largura / 2, largura / 2);
                }
                else
                {
                    ext.CreateBox(largura + 2 * espessura, altura + 2 * espessura, comp);
                    inte.CreateBox(largura, altura, comp + 0.02);
                }
                ext.BooleanOperation(BooleanOperationType.BoolSubtract, inte);
                inte.Dispose();
                // Z local -> direção do tubo; X local -> lateral; Y local -> "acima"
                var m = Matrix3d.AlignCoordinateSystem(Point3d.Origin, Vector3d.XAxis, Vector3d.YAxis, Vector3d.ZAxis,
                    meio, lateral, acima, u);
                ext.TransformBy(m);
                lista.Add(ext);
            }
            catch (System.Exception ex) { erros.Add("Tubo: " + ex.Message); }
        }
        return lista;
    }
}
