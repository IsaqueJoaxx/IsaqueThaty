# Inspeciona o ladrilho ANADEM pelo GDAL (leitura parcial via /vsicurl/) e grava pontos de referencia E N Z.
from osgeo import gdal, osr
gdal.UseExceptions()
gdal.SetConfigOption("GDAL_DISABLE_READDIR_ON_OPEN", "EMPTY_DIR")
gdal.SetConfigOption("CPL_VSIL_CURL_ALLOWED_EXTENSIONS", ".tif")
gdal.SetConfigOption("CPL_CURL_VERBOSE", "NO")
url = "/vsicurl/https://metadados.snirh.gov.br/files/anadem_v1_tiles/anadem_v1_23K.tif"
ds = gdal.Open(url)
b = ds.GetRasterBand(1)
gt = ds.GetGeoTransform()
print("tamanho", ds.RasterXSize, ds.RasterYSize, "bloco", b.GetBlockSize(), "tipo", gdal.GetDataTypeName(b.DataType), "bandas", ds.RasterCount)
print("gt", gt)
print("area_or_point", ds.GetMetadataItem("AREA_OR_POINT"), "estrutura", ds.GetMetadata("IMAGE_STRUCTURE"), "nodata", b.GetNoDataValue(), "overviews", b.GetOverviewCount())
srs = osr.SpatialReference(wkt=ds.GetProjection())
print("srs", srs.GetName(), srs.GetAuthorityCode(None))
print("cobre lat", gt[3] + ds.RasterYSize * gt[5], "a", gt[3], "lon", gt[0], "a", gt[0] + ds.RasterXSize * gt[1])
geo = osr.SpatialReference(); geo.ImportFromEPSG(4674)
utm = osr.SpatialReference(); utm.ImportFromEPSG(31983)
for s in (geo, utm, srs): s.SetAxisMappingStrategy(osr.OAMS_TRADITIONAL_GIS_ORDER)
u2r = osr.CoordinateTransformation(utm, srs); r2u = osr.CoordinateTransformation(srs, utm)
linhas = []
for e, n in [(611300, 7796300), (611900, 7796800), (612400, 7797200), (612700, 7796400), (611172, 7796932)]:
    x, y, _ = u2r.TransformPoint(e, n)
    c = int((x - gt[0]) / gt[1]); l = int((y - gt[3]) / gt[5])
    v = float(b.ReadAsArray(c, l, 1, 1)[0, 0])
    cx = gt[0] + (c + 0.5) * gt[1]; cy = gt[3] + (l + 0.5) * gt[5]
    ee, nn, _ = r2u.TransformPoint(cx, cy)
    linhas.append(f"{ee:.4f} {nn:.4f} {v:.4f}")
    print("pixel", c, l, "centro", round(cy, 7), round(cx, 7), "z", v, "utm", round(ee, 3), round(nn, 3))
open(r"C:\Temp\geo\pontos_ref_anadem.txt", "w").write("\n".join(linhas) + "\n")
