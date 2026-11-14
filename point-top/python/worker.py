"""Ponte de cálculo POINT TOP — IFM; protocolo JSON por linha."""
import json
import sys
from pathlib import Path
from volume_ifm import calculate


def emit(kind, **data):
    print(json.dumps(dict(kind=kind, **data), ensure_ascii=False, allow_nan=False), flush=True)


def demo(folder):
    import numpy as np
    import rasterio
    from rasterio.transform import from_origin
    folder = Path(folder)
    folder.mkdir(parents=True, exist_ok=True)
    a = np.full((4, 4), 100.0)
    b = np.full((4, 4), 102.0)
    b[0, :] = 97
    b[1, 0] = -9999
    paths = []
    for name, data in [('1_Primitivo.tif', a), ('2_Projeto.tif', b)]:
        path = folder / name
        with rasterio.open(path, 'w', driver='GTiff', width=4, height=4,
                           count=1, dtype='float64', crs='EPSG:31983',
                           transform=from_origin(500000, 7800000, 2, 2), nodata=-9999) as dst:
            dst.write(data, 1)
            dst.set_band_unit(1, 'm')
        paths.append(str(path))
    return paths


if __name__ == '__main__':
    try:
        request = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
        if request.get('demo'):
            request['base'], request['comparison'] = demo(request['demoFolder'])
        folder, result = calculate(request['base'], request['comparison'], request['output'],
                                   request.get('boundary'), request['tolerance'],
                                   lambda value: emit('progress', value=round(value, 2)))
        emit('done', folder=str(folder), result=result)
    except Exception as exc:
        emit('error', message=str(exc))
        sys.exit(1)
