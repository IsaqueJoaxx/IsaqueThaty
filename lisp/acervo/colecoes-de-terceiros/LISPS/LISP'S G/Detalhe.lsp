(defun c:det (/	osm blip ent alist cir1	pt1 raio1 cir2 pt2 raio2 fator
	      ss pt)
  (setq p1 (getpoint "\nSelecione o centro do círculo de ampliação:"))
  (command "circle" p1 pause "")
  (command "undo" "be")
  (setq osm (getvar "osmode"))
  (setq blip (getvar "blipmode"))
  (setvar "blipmode" 0)
  (setvar "osmode" 0)
  (setvar "cmdecho" 1)
  (while (not cir1)
    (setq ent (car (entsel "\nSelecione o cÌrculo a ampliar: ")))
    (if	(and ent
	     (setq alist (entget ent))
	     (= (cdr (assoc 0 alist)) "CIRCLE")
	)
      (setq cir1  ent
	    pt1	  (cdr (assoc 10 alist))
	    raio1 (cdr (assoc 40 alist))
      )
    )
  )
  (command "copy" cir1 "" pt1 pause)
  (setq	cir2  (entlast)
	alist (entget cir2)
	pt2   (cdr (assoc 10 alist))
  )
  (command "scale" cir2 "" pt2 pause)
  (setvar "cmdecho" 0)
  (setq	alist (entget cir2)
	raio2 (cdr (assoc 40 alist))
	fator (/ raio2 raio1)
  )
  (command "copy"
	   "cp"
	   (foreach pt (pt_circulo pt1 raio1 T)
	     (command pt)
	     ""
	   )
	   "r"
	   cir1
	   ""
	   pt1
	   pt1
  )
  (setq ss (ssget "p"))
  (command "move" ss "" pt1 pt2)
  (command "scale" ss "" pt2 fator)
  (repeat 2
    (command "trim"
	     cir2
	     ""
	     "f"
	     (foreach pt (pt_circulo pt2 raio2 nil)
	       (command pt)
	       ""
	     )
	     ""
    )
  )
  (setvar "blipmode" blip)
  (setvar "osmode" osm)
  (setvar "cmdecho" 1)
  (command "undo" "e")
  (princ)
)


(defun pt_circulo (centro raio inscrito / ang ndiv delta pt resp)
  (setq	ang   0
	ndiv  36
	delta (dtor (/ 360 ndiv))
  )
  (if (not inscrito)
    (setq raio (* 1.0001 (/ raio (cos (/ delta 2.0)))))
  )
  (repeat (1+ ndiv)
    (setq ang (+ ang delta))
    (setq pt (polar centro ang raio))
    (setq resp (append resp (list pt)))
  )
)


(defun dtor (a)
  (* pi (/ a 180.00))
)

(princ)