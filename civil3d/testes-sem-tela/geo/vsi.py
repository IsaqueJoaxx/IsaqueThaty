# Confere o ladrilho Copernicus pelo GDAL (leitura parcial via /vsicurl/) e grava pontos de referencia E N Z.
from osgeo import gdal, osr
gdal.UseExceptions()
gdal.SetConfigOption("GDAL_DISABLE_READDIR_ON_OPEN", "EMPTY_DIR")
gdal.SetConfigOption("CPL_VSIL_CURL_ALLOWED_EXTENSIONS", ".tif")
nome = "Copernicus_DSM_COG_10_S20_00_W044_00_DEM"
ds = gdal.Open(f"/vsicurl/https://copernicus-dem-30m.s3.amazonaws.com/{nome}/{nome}.tif")
gt = ds.GetGeoTransform()
b = ds.GetRasterBand(1)
print("tamanho", ds.RasterXSize, ds.RasterYSize, "bloco", b.GetBlockSize(), "tipo", gdal.GetDataTypeName(b.DataType))
print("gt", gt)
print("area_or_point", ds.GetMetadataItem("AREA_OR_POINT"), "estrutura", ds.GetMetadata("IMAGE_STRUCTURE"), "nodata", b.GetNoDataValue())
print("cobre lat", gt[3] + ds.RasterYSize * gt[5], "a", gt[3], "lon", gt[0], "a", gt[0] + ds.RasterXSize * gt[1])
geo = osr.SpatialReference(); geo.ImportFromEPSG(4674)
utm = osr.SpatialReference(); utm.ImportFromEPSG(31983)
for s in (geo, utm): s.SetAxisMappingStrategy(osr.OAMS_TRADITIONAL_GIS_ORDER)
u2g = osr.CoordinateTransformation(utm, geo); g2u = osr.CoordinateTransformation(geo, utm)
linhas = []
for e, n in [(611300, 7796300), (611900, 7796800), (612400, 7797200), (612700, 7796400), (611172, 7796932)]:
    lon, lat, _ = u2g.TransformPoint(e, n)
    c = int((lon - gt[0]) / gt[1]); l = int((lat - gt[3]) / gt[5])
    v = float(b.ReadAsArray(c, l, 1, 1)[0, 0])
    clon = gt[0] + (c + 0.5) * gt[1]; clat = gt[3] + (l + 0.5) * gt[5]
    ee, nn, _ = g2u.TransformPoint(clon, clat)
    linhas.append(f"{ee:.4f} {nn:.4f} {v:.4f}")
    print("pixel", c, l, "centro", round(clat, 7), round(clon, 7), "z", v, "utm", round(ee, 3), round(nn, 3))
open(r"C:\Temp\geo\pontos_ref.txt", "w").write("\n".join(linhas) + "\n")
