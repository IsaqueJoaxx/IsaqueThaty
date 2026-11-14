from osgeo import osr
casos = [
    (-19.9208, -43.9378, 31983),
    (-15.0, -48.5, 31983),
    (2.8, -60.67, 31975),
    (-19.9208, -43.9378, 32723),
    (-33.7, -53.4, 31982),
    (-5.0, -42.0, 31984),
    (-23.55, -46.63, 31983),
]
for lat, lon, epsg in casos:
    geo = osr.SpatialReference(); geo.ImportFromEPSG(4326 if epsg > 32000 else 4674)
    utm = osr.SpatialReference(); utm.ImportFromEPSG(epsg)
    for s in (geo, utm):
        s.SetAxisMappingStrategy(osr.OAMS_TRADITIONAL_GIS_ORDER)
    t = osr.CoordinateTransformation(geo, utm)
    e, n, _ = t.TransformPoint(lon, lat)
    print(f"({lat}, {lon}, {epsg}, {e:.4f}, {n:.4f}),")
