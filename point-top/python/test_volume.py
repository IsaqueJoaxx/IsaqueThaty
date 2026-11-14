import json
import tempfile
import unittest
from pathlib import Path
import numpy as np
import rasterio
from rasterio.transform import from_origin
from volume_ifm import calculate


class VolumeTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.folder = Path(self.tmp.name)

    def tearDown(self):
        self.tmp.cleanup()

    def tif(self, name, data, transform=None, crs='EPSG:31983', nodata=-9999, mask=None):
        path = self.folder/name
        with rasterio.open(path,'w',driver='GTiff',width=data.shape[1],height=data.shape[0],count=1,
                           dtype='float64',crs=crs,transform=transform or from_origin(500000,7800000,2,2),nodata=nodata) as dst:
            dst.write(data.astype('float64'),1)
            if mask is not None: dst.write_mask(mask)
        return path

    def run_calc(self,a,b,**kw):
        return calculate(a,b,self.folder/'out',**kw)

    def test_cut_fill_nodata_and_sign(self):
        a=self.tif('a.tif',np.full((4,4),100.))
        data=np.full((4,4),102.); data[0,:]=97; data[1,0]=-9999
        b=self.tif('b.tif',data)
        folder,r=self.run_calc(a,b)
        self.assertEqual(r['corte_m3'],48)
        self.assertEqual(r['aterro_m3'],88)
        self.assertEqual(r['area_valida_m2'],60)
        self.assertEqual(r['area_sem_dados_m2'],4)
        with rasterio.open(folder/'diferenca_2_menos_1.tif') as d:
            self.assertTrue(d.read(1,masked=True).mask[1,0])
        self.assertTrue((folder/'relatorio.html').exists())
        _,reverse=self.run_calc(b,a)
        self.assertEqual(reverse['corte_m3'],88)
        self.assertEqual(reverse['aterro_m3'],48)

    def test_resolution_and_outside_coverage(self):
        a=self.tif('a.tif',np.full((6,6),100.))
        b=self.tif('b.tif',np.full((8,8),102.),from_origin(500002,7799998,1,1),nodata=None)
        _,r=self.run_calc(a,b)
        self.assertEqual(r['area_valida_m2'],64)
        self.assertAlmostEqual(r['aterro_m3'],128)

    def test_boundary_and_tolerance(self):
        a=self.tif('a.tif',np.full((4,4),100.))
        b=self.tif('b.tif',np.full((4,4),100.1))
        limit=self.folder/'limite.geojson'
        limit.write_text(json.dumps({'type':'FeatureCollection','crs':{'type':'name','properties':{'name':'EPSG:31983'}},'features':[{'type':'Feature','geometry':{'type':'Polygon','coordinates':[[[500000,7800000],[500004,7800000],[500004,7799996],[500000,7799996],[500000,7800000]]]}}]}))
        _,r=self.run_calc(a,b,boundary=limit,tolerance=.2)
        self.assertEqual(r['area_valida_m2'],16)
        self.assertEqual(r['aterro_m3'],0)
        self.assertAlmostEqual(r['saldo_sem_tolerancia_m3'],1.6)

    def test_warp_preserves_holes(self):
        a=self.tif('a.tif',np.full((4,4),100.))
        data=np.full((8,8),102.); data[:4,:4]=-9999
        b=self.tif('b.tif',data,from_origin(500000,7800000,1,1))
        _,r=self.run_calc(a,b)
        self.assertLess(r['area_valida_m2'],64)
        self.assertGreater(r['area_sem_dados_m2'],0)
        self.assertAlmostEqual(r['aterro_m3'],r['area_valida_m2']*2)

    def test_multiple_windows(self):
        a=self.tif('a.tif',np.full((520,520),100.))
        b=self.tif('b.tif',np.full((520,520),101.))
        progress=[]
        _,r=self.run_calc(a,b,progress=progress.append)
        self.assertEqual(r['aterro_m3'],520*520*4)
        self.assertEqual(len(progress),4)
        self.assertEqual(progress[-1],100)

    def test_reject_incompatible_and_empty(self):
        a=self.tif('a.tif',np.full((4,4),100.))
        b=self.tif('b.tif',np.full((4,4),100.),crs='EPSG:4326')
        with self.assertRaises(ValueError): self.run_calc(a,b)
        c=self.tif('c.tif',np.full((4,4),-9999.))
        with self.assertRaises(ValueError): self.run_calc(a,c)
        d=self.tif('d.tif',np.full((4,4),100.),from_origin(600000,7900000,2,2))
        with self.assertRaises(ValueError): self.run_calc(a,d)

    def test_plane_on_different_grid(self):
        # Superfície inclinada em grade mais fina e deslocada: confere a reamostragem com cota variável.
        a=self.tif('a.tif',np.full((40,40),100.))
        x0,y0=500000.5,7800000.5
        cols,rows=np.meshgrid(np.arange(90),np.arange(90))
        xs,ys=x0+(cols+.5),y0-(rows+.5)
        z=lambda x,y:100+.01*(x-500000)+.02*(7800000-y)
        b=self.tif('b.tif',z(xs,ys),from_origin(x0,y0,1,1),nodata=None)
        limit=self.folder/'limite.geojson'
        limit.write_text(json.dumps({'type':'FeatureCollection','crs':{'type':'name','properties':{'name':'EPSG:31983'}},'features':[{'type':'Feature','geometry':{'type':'Polygon','coordinates':[[[500010,7799990],[500070,7799990],[500070,7799930],[500010,7799930],[500010,7799990]]]}}]}))
        _,r=self.run_calc(a,b,boundary=limit)
        xc=500010+1+2*np.arange(30)
        yc=7799990-1-2*np.arange(30)
        esperado=sum(z(x,y)-100 for x in xc for y in yc)*4
        self.assertEqual(r['area_valida_m2'],3600)
        self.assertAlmostEqual(r['aterro_m3'],esperado,places=3)
        self.assertEqual(r['corte_m3'],0)

    def test_scale_and_offset(self):
        # Cotas gravadas como inteiro com escala e deslocamento, na mesma grade e em grade diferente.
        def escalado(name,transform,shape):
            path=self.folder/name
            with rasterio.open(path,'w',driver='GTiff',width=shape,height=shape,count=1,dtype='int32',crs='EPSG:31983',transform=transform,nodata=-1) as dst:
                dst.write(np.full((shape,shape),1000,dtype='int32'),1)
                dst.scales=(.01,)
                dst.offsets=(100.,)
            return path
        a=self.tif('a.tif',np.full((4,4),108.))
        _,r=self.run_calc(a,escalado('b.tif',from_origin(500000,7800000,2,2),4))
        self.assertAlmostEqual(r['aterro_m3'],2*16*4)
        _,r=self.run_calc(a,escalado('c.tif',from_origin(500000,7800000,1,1),8))
        self.assertAlmostEqual(r['aterro_m3'],2*r['area_valida_m2'])

if __name__=='__main__': unittest.main()
