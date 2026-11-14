(Defun C:talude ( / CurvaDiv Curva2 DisLinT) 
 (vl-load-com) 
 (setq CurvaDiv (car (entsel "\nSeleccione Curva de crista: "))) 
 (setq Curva2 (car (entsel "\nSeleccione Curva de pé: "))) 
 (initget 6) 
 (setq DisLinT (getdist "\nDistancia entre as linhas de talude: ")) 
 (DrawTalud CurvaDiv Curva2 DisLinT) 
 (princ) 
);c.defun 
