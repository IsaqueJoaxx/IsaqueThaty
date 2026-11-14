"""VOLUME ENTRE SUPERFÍCIES — IFM. Integração raster por janelas."""
import csv
import html
import json
import math
import os
from pathlib import Path
from datetime import datetime
from contextlib import ExitStack

import numpy as np
import rasterio
from rasterio.enums import Resampling, ColorInterp
from rasterio.features import geometry_mask, bounds as geometry_bounds
from rasterio.vrt import WarpedVRT
from rasterio.windows import Window, from_bounds, transform as window_transform
from rasterio.warp import transform_geom


def load_boundary(path, target_crs):
    if not path:
        return None
    obj = json.loads(Path(path).read_text(encoding='utf-8-sig'))
    source_crs = obj.get('crs', {}).get('properties', {}).get('name', 'EPSG:4326')
    features = obj['features'] if obj.get('type') == 'FeatureCollection' else [obj]
    geometries = []
    for feat in features:
        geom = feat.get('geometry') if feat.get('type') == 'Feature' else feat
        if not geom or geom.get('type') not in ('Polygon', 'MultiPolygon'):
            raise ValueError('O limite GeoJSON deve conter somente polígonos.')
        geometries.append(transform_geom(source_crs, target_crs, geom))
    if not geometries:
        raise ValueError('Limite vazio.')
    return geometries


def validate(src):
    if src.count != 1 or src.colorinterp[0] == ColorInterp.palette:
        raise ValueError(f'{src.name}: use um DEM/MDT de uma banda, sem paleta, com cotas.')
    if not src.crs or not src.crs.is_projected:
        raise ValueError('Use superfícies com sistema projetado em metros, por exemplo SIRGAS 2000 / UTM.')
    if not math.isclose(src.crs.linear_units_factor[1], 1.0, abs_tol=1e-9):
        raise ValueError('As coordenadas horizontais devem estar em metros.')
    t = src.transform
    if t.b != 0 or t.d != 0 or t.a <= 0 or t.e >= 0:
        raise ValueError('Reexporte o TIF com grade norte acima, sem rotação.')
    if src.units[0] and src.units[0].lower() not in ('m', 'meter', 'meters', 'metre', 'metres'):
        raise ValueError('A unidade vertical declarada no TIF não é metro. Converta antes.')


def calculate(base, comparison, output, boundary=None, tolerance=0.0, progress=lambda v: None):
    if not math.isfinite(tolerance) or tolerance < 0:
        raise ValueError('A tolerância deve ser finita e maior ou igual a zero.')
    if Path(base).resolve() == Path(comparison).resolve():
        raise ValueError('Selecione duas superfícies diferentes.')
    output = Path(output)
    output.mkdir(parents=True, exist_ok=True)
    # Pasta exclusiva: nunca sobrescrever medições anteriores.
    from tempfile import mkdtemp
    run = Path(mkdtemp(prefix=datetime.now().strftime('Medicao_%Y%m%d_%H%M%S_'), dir=output))
    try:
        with ExitStack() as stack:
            stack.enter_context(rasterio.Env(GDAL_CACHEMAX=128*1024*1024, GDAL_NUM_THREADS='1'))
            a = stack.enter_context(rasterio.open(base))
            b = stack.enter_context(rasterio.open(comparison))
            validate(a)
            validate(b)
            if a.crs != b.crs:
                raise ValueError('Os sistemas de coordenadas diferem. Reexporte ambos no mesmo CRS antes de medir.')
            geometries = load_boundary(boundary, a.crs)
            left, bottom = max(a.bounds.left, b.bounds.left), max(a.bounds.bottom, b.bounds.bottom)
            right, top = min(a.bounds.right, b.bounds.right), min(a.bounds.top, b.bounds.top)
            if geometries:
                boxes = [geometry_bounds(g) for g in geometries]
                left, bottom = max(left, min(t[0] for t in boxes)), max(bottom, min(t[1] for t in boxes))
                right, top = min(right, max(t[2] for t in boxes)), min(top, max(t[3] for t in boxes))
            if left >= right or bottom >= top:
                raise ValueError('As superfícies e o limite não têm sobreposição.')
            fw = from_bounds(left, bottom, right, top, a.transform)
            # Só células inteiras dentro da interseção das extensões.
            c0, r0 = math.ceil(fw.col_off-1e-8), math.ceil(fw.row_off-1e-8)
            c1, r1 = math.floor(fw.col_off+fw.width+1e-8), math.floor(fw.row_off+fw.height+1e-8)
            width, height = c1-c0, r1-r0
            if width < 1 or height < 1:
                raise ValueError('A sobreposição é menor que uma célula da superfície 1.')
            grid = window_transform(Window(c0, r0, width, height), a.transform)
            area = abs(grid.a*grid.e)
            same_grid = a.transform == b.transform and a.width == b.width and a.height == b.height
            vrt = None if same_grid else stack.enter_context(WarpedVRT(
                b, crs=a.crs, transform=grid, width=width, height=height,
                dtype='float64', nodata=float('nan'), resampling=Resampling.bilinear,
                warp_mem_limit=64))
            dest = stack.enter_context(rasterio.open(run/'diferenca_2_menos_1.tif', 'w',
                driver='GTiff', width=width, height=height, count=1, dtype='float64',
                crs=a.crs, transform=grid, nodata=float('nan'), tiled=True,
                blockxsize=512, blockysize=512, compress='deflate', predictor=3, BIGTIFF='IF_SAFER'))
            dest.set_band_description(1, 'Superfície 2 menos superfície 1, em metros; sem filtro de tolerância')
            n = eligible = ignored = 0
            cut_parts, fill_parts, raw_parts = [], [], []
            min_d, max_d = float('inf'), float('-inf')
            total = math.ceil(width/512)*math.ceil(height/512)
            done = 0
            for row in range(0, height, 512):
                for col in range(0, width, 512):
                    w = Window(col, row, min(512, width-col), min(512, height-row))
                    wa = Window(c0+col, r0+row, w.width, w.height)
                    av = a.read(1, window=wa, masked=True).astype('float64')
                    bv = (b.read(1, window=wa, masked=True) if same_grid else vrt.read(1, window=w, masked=True)).astype('float64')
                    av = av*a.scales[0]+a.offsets[0]
                    bv = bv*b.scales[0]+b.offsets[0]
                    valid = ~(np.ma.getmaskarray(av) | np.ma.getmaskarray(bv))
                    valid &= np.isfinite(av.data) & np.isfinite(bv.data)
                    inside = np.ones(valid.shape, dtype=bool) if not geometries else geometry_mask(
                        geometries, out_shape=valid.shape, transform=window_transform(w, grid),
                        invert=True, all_touched=False)
                    eligible += int(inside.sum())
                    valid &= inside
                    delta = bv.data-av.data
                    d = delta[valid]
                    n += d.size
                    if d.size:
                        min_d, max_d = min(min_d, float(d.min())), max(max_d, float(d.max()))
                        raw_parts.append(float(d.sum(dtype='float64'))*area)
                        cut_parts.append(float(-d[d < -tolerance].sum(dtype='float64'))*area)
                        fill_parts.append(float(d[d > tolerance].sum(dtype='float64'))*area)
                        ignored += int((abs(d) <= tolerance).sum())
                    dest.write(np.where(valid, delta, np.nan), 1, window=w)
                    done += 1
                    progress(done/total*100)
            if n == 0:
                raise ValueError('Não existem células com cotas válidas nas duas superfícies dentro do limite.')
            cut, fill = math.fsum(cut_parts), math.fsum(fill_parts)
            result = dict(assistente='VOLUME ENTRE SUPERFÍCIES — IFM', versao='0.1.0',
                data=datetime.now().isoformat(timespec='seconds'), superficie_1=str(Path(base).resolve()),
                superficie_2=str(Path(comparison).resolve()), limite=str(boundary or 'Sem limite: interseção válida dos TIFs'),
                crs=a.crs.to_string(), resolucao_x_m=grid.a, resolucao_y_m=-grid.e,
                area_valida_m2=n*area, area_sem_dados_m2=(eligible-n)*area,
                corte_m3=cut, aterro_m3=fill, saldo_aterro_menos_corte_m3=fill-cut,
                saldo_sem_tolerancia_m3=math.fsum(raw_parts), tolerancia_m=tolerance,
                area_faixa_tolerancia_m2=ignored*area, diferenca_min_m=min_d, diferenca_max_m=max_d,
                alinhamento='Grade original' if same_grid else 'Superfície 2 reamostrada por bilinear na grade da superfície 1',
                unidade_vertical='Metros, confirmados pelo operador; metadados de escala/offset aplicados',
                convencao='2 - 1: negativo = corte/remoção; positivo = aterro/acréscimo',
                metodo='Soma de diferenças de cota vezes área de célula; limite pelo centro da célula',
                ressalva='Volumes geométricos, sem empolamento/compactação. Mesmo datum vertical confirmado pelo operador. Área calculada na projeção. Bordas parciais não fracionadas.')
        (run/'resultado.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
        with (run/'resultado.csv').open('w', newline='', encoding='utf-8-sig') as f:
            writer = csv.writer(f, delimiter=';')
            writer.writerow(['Campo', 'Valor'])
            writer.writerows((k, str(v).replace('.', ',') if isinstance(v, float) else v) for k,v in result.items())
        rows = ''.join(f'<tr><th>{html.escape(k)}</th><td>{html.escape(str(v))}</td></tr>' for k,v in result.items())
        (run/'relatorio.html').write_text('<!doctype html><meta charset="utf-8"><title>VOLUME ENTRE SUPERFÍCIES — IFM</title><style>body{font:15px Arial;margin:40px;color:#18334a}table{border-collapse:collapse;width:100%}th,td{border:1px solid #ccd;padding:9px;text-align:left;overflow-wrap:anywhere}th{width:30%}h1{font-size:24px}@media print{body{margin:10px}}</style><h1>VOLUME ENTRE SUPERFÍCIES — IFM</h1><p>Relatório de volume geométrico. Use Imprimir / Salvar como PDF no navegador.</p><table>'+rows+'</table>', encoding='utf-8')
        return run, result
    except Exception as exc:
        (run/'ERRO.txt').write_text(str(exc)+'\nProcessamento incompleto. Não utilizar o TIF desta pasta para medição.', encoding='utf-8')
        raise


def gui():
    import tkinter as tk
    from tkinter import ttk, filedialog, messagebox
    import queue
    import threading
    root = tk.Tk()
    root.title('VOLUME ENTRE SUPERFÍCIES — IFM')
    root.geometry('820x560')
    root.minsize(760, 530)
    frame = ttk.Frame(root, padding=20)
    frame.pack(fill='both', expand=True)
    frame.columnconfigure(1, weight=1)
    ttk.Label(frame, text='VOLUME ENTRE SUPERFÍCIES — IFM', font=('Segoe UI',16,'bold')).grid(row=0,column=0,columnspan=3,sticky='w',pady=(0,12))
    ttk.Label(frame,text='Cálculo por blocos de 512 × 512 células • saída em m³').grid(row=1,column=0,columnspan=3,sticky='w')
    values = [tk.StringVar() for _ in range(4)]
    values[3].set(str(Path(__file__).resolve().parent/'Resultados'))
    def browse(i):
        path = filedialog.askdirectory() if i == 3 else filedialog.askopenfilename(filetypes=[('Limite GeoJSON','*.geojson *.json')] if i == 2 else [('Superfície de elevação','*.tif *.tiff')])
        if path: values[i].set(path)
    for i,label in enumerate(['1 — Primitivo / medição anterior','2 — Projeto / medição atual','Limite GeoJSON (opcional)','Pasta dos resultados']):
        ttk.Label(frame,text=label).grid(row=i+2,column=0,sticky='w',pady=9)
        ttk.Entry(frame,textvariable=values[i]).grid(row=i+2,column=1,sticky='ew',padx=8)
        ttk.Button(frame,text='Selecionar',command=lambda i=i:browse(i)).grid(row=i+2,column=2)
    tol = tk.StringVar(value='0')
    ttk.Label(frame,text='Tolerância vertical (m)').grid(row=6,column=0,sticky='w',pady=8)
    ttk.Entry(frame,textvariable=tol,width=15).grid(row=6,column=1,sticky='w',padx=8)
    ttk.Label(frame,text='Padrão 0: sem descarte. |ΔZ| ≤ tolerância não entra em corte/aterro.').grid(row=7,column=0,columnspan=3,sticky='w')
    checked = tk.BooleanVar()
    ttk.Checkbutton(frame,variable=checked,text='Confirmo: os dois TIFs são modelos de elevação, com cotas em metros e mesmo datum vertical.').grid(row=8,column=0,columnspan=3,sticky='w',pady=12)
    ttk.Label(frame,text='2 − 1: negativo = corte / remoção; positivo = aterro / acréscimo.\nSem limite, toda a interseção válida será medida. Use o mesmo limite nas medições semanais.\nO limite usa o centro das células. GeoJSON sem CRS é interpretado como longitude/latitude WGS84.',wraplength=760).grid(row=9,column=0,columnspan=3,sticky='w')
    bar = ttk.Progressbar(frame,maximum=100)
    bar.grid(row=10,column=0,columnspan=3,sticky='ew',pady=12)
    status = tk.StringVar(value='Pronto para selecionar as superfícies.')
    ttk.Label(frame,textvariable=status,wraplength=760).grid(row=12,column=0,columnspan=3,sticky='w',pady=8)
    events = queue.Queue()
    running = False
    def start():
        nonlocal running
        if not checked.get():
            messagebox.showerror('Conferência necessária','Confirme as cotas, unidades e o datum vertical.'); return
        try:
            args = [v.get().strip() for v in values]
            if not all(args[i] for i in (0,1,3)): raise ValueError('Selecione as duas superfícies e a pasta de resultados.')
            tolerance = float(tol.get().replace(',','.'))
            if not math.isfinite(tolerance) or tolerance < 0: raise ValueError('Tolerância inválida.')
        except ValueError as exc:
            messagebox.showerror('Dados inválidos',str(exc)); return
        running = True
        button.state(['disabled'])
        status.set('Processando. Aguarde a conclusão...')
        bar['value'] = 0
        def worker():
            try: events.put(('done',calculate(args[0],args[1],args[3],args[2] or None,tolerance,lambda p:events.put(('progress',p)))))
            except Exception as exc: events.put(('error',str(exc)))
        threading.Thread(target=worker,daemon=True).start()
    def poll():
        nonlocal running
        try:
            while True:
                kind, value = events.get_nowait()
                if kind == 'progress': bar['value'] = value
                else:
                    running = False
                    button.state(['!disabled'])
                    if kind == 'error': status.set('Falha: '+value); messagebox.showerror('Falha no cálculo',value)
                    else:
                        folder, result = value
                        status.set(f"Corte: {result['corte_m3']:,.3f} m³ | Aterro: {result['aterro_m3']:,.3f} m³\nResultados: {folder}")
                        os.startfile(str(folder/'relatorio.html'))
        except queue.Empty: pass
        root.after(150,poll)
    def close():
        if running: messagebox.showinfo('Cálculo em andamento','Aguarde a conclusão antes de fechar.'); return
        root.destroy()
    button = ttk.Button(frame,text='CALCULAR VOLUMES',command=start)
    button.grid(row=11,column=0,columnspan=3,sticky='ew')
    root.protocol('WM_DELETE_WINDOW',close)
    poll()
    root.mainloop()


if __name__ == '__main__':
    gui()
