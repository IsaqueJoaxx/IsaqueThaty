# Gera GeoTIFFs de teste com o GDAL (varias compressoes/preditores/endianness) e os valores de referencia.
import json, os
import numpy as np
from osgeo import gdal, osr
gdal.UseExceptions()
pasta = r"C:\Temp\geo\tif"
os.makedirs(pasta, exist_ok=True)
rng = np.random.default_rng(1)
h, w = 700, 900
yy, xx = np.mgrid[0:h, 0:w]
z = (800 + 30 * np.sin(xx / 50.0) + 20 * np.cos(yy / 40.0) + rng.normal(0, 0.5, (h, w)))
gt = (-44.0, 1 / 3600, 0, -19.5, 0, -1 / 3600)
srs = osr.SpatialReference(); srs.ImportFromEPSG(4326)
casos = [
    ("f32_deflate_p3_tiled.tif", gdal.GDT_Float32, ["TILED=YES", "BLOCKXSIZE=256", "BLOCKYSIZE=256", "COMPRESS=DEFLATE", "PREDICTOR=3"], None, "Area"),
    ("f32_lzw_p3_big.tif", gdal.GDT_Float32, ["TILED=YES", "COMPRESS=LZW", "PREDICTOR=3", "ENDIANNESS=BIG"], None, "Area"),
    ("i16_lzw_p2_strips.tif", gdal.GDT_Int16, ["COMPRESS=LZW", "PREDICTOR=2", "BLOCKYSIZE=37"], -32768, "Area"),
    ("f64_none_bigtiff.tif", gdal.GDT_Float64, ["BIGTIFF=YES", "TILED=YES"], None, "Point"),
    ("u16_deflate_p2_be.tif", gdal.GDT_UInt16, ["TILED=YES", "COMPRESS=DEFLATE", "PREDICTOR=2", "ENDIANNESS=BIG"], None, "Point"),
]
ref = {}
pts = [(0, 0), (1, 0), (0, 1), (255, 255), (256, 256), (899, 699), (450, 333), (37, 36), (37, 37), (512, 1), (13, 600)]
for nome, tipo, opcoes, nd, aop in casos:
    arq = os.path.join(pasta, nome)
    d = gdal.GetDriverByName("GTiff").Create(arq, w, h, 1, tipo, opcoes)
    d.SetGeoTransform(gt); d.SetProjection(srs.ExportToWkt())
    d.SetMetadataItem("AREA_OR_POINT", aop)
    b = d.GetRasterBand(1)
    dados = z.copy()
    if tipo == gdal.GDT_UInt16: dados = dados * 10
    if nd is not None:
        dados = np.round(dados)
        dados[100:120, 200:230] = nd
        b.SetNoDataValue(nd)
    b.WriteArray(dados)
    d.FlushCache(); d = None
    r = gdal.Open(arq)
    g = r.GetGeoTransform()
    a = r.GetRasterBand(1).ReadAsArray().astype(float)
    nodata = r.GetRasterBand(1).GetNoDataValue()
    amostras = []
    for c, l in pts + [(210, 110)]:
        v = a[l, c]
        amostras.append([c, l, None if (nodata is not None and v == nodata) else float(v)])
    ref[nome] = {"cx0": g[0] + 0.5 * g[1], "cy0": g[3] + 0.5 * g[5], "dx": g[1], "dy": -g[5],
                 "largura": r.RasterXSize, "altura": r.RasterYSize, "amostras": amostras,
                 "soma": float(np.nansum(np.where(a == (nodata if nodata is not None else np.nan), 0, a)))}
    r = None
# COG (como o Copernicus): via driver COG
src = os.path.join(pasta, "f32_deflate_p3_tiled.tif")
cog = os.path.join(pasta, "f32_cog.tif")
gdal.Translate(cog, src, format="COG", creationOptions=["COMPRESS=DEFLATE", "PREDICTOR=YES", "BLOCKSIZE=512"])
ref["f32_cog.tif"] = dict(ref["f32_deflate_p3_tiled.tif"])
with open(os.path.join(pasta, "ref.json"), "w") as f:
    json.dump(ref, f, indent=1)
print("ok", list(ref))
