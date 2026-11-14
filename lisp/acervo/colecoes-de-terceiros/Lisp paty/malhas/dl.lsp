(defun c:dl ()
  (setvar "cmdecho" 0)
  (setq sspoint1 (getpoint "\nClique o primeiro ponto"))
  (setq sspoint2 (getpoint "\nClique o segundo ponto"))
  (command "line" sspoint1 sspoint2 "")
  (command "erase" "l" "")
  (command "dist" sspoint1 sspoint2)
  (setq ssdist (rtos (getvar "distance")))
  (setq ssang (angtos (getvar "lastangle")))
  (setq
    sspointe1 (getpoint "\n\Clique onde deseja inserir a distancia")
  )
  (command "text"
	   "j"
	   "bc"
	   sspointe1
	   "3.0"
	   ssang
	   (strcat ssdist ".")
  )
  (setq	sspointe2
	 (getpoint "\n\Clique onde deseja inserir o rumo ou azimute"
	 )
  )
  (command "text"
	   "j"
	   "tc"
	   sspointe2
	   "3.0"
	   ssang
	   (strcat ssang ".")
  )
  (setvar "cmdecho" 1)
  (princ)
)
