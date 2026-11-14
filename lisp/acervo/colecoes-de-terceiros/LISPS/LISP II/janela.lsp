(defun c:janela  (/  p1  p2)
		(setq p1  (getpoint  “\nPrimeiro  canto:”)
			p2  (getcorner  p1  “\nSegunto canto:”) )
		(ssget  “W”  p1  p2) )
