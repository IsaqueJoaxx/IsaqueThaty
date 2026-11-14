;*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*
;s  Elaborado por: Soraia Adaniya                    Em:  18/07/2001        s
;o                                                                          o
;*  SB - PROJETOS                                                           *
;s  Rotina para substituir arcos por polylines                              s
;o  Versão 1.0 - Comando para iniciar: ARPO                                 o
;*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*
(defun c:arpo ( / verifica_polyline
	          arc_polyline
	          extrai_coordenadas
	          desenha_pline
	          spline_polyline
	          qp selpl cp pl resp laypl pol qpol l layarc plnova)
  
  ;(load "\\\\servermof01\\autolisp\\soraia\\auxiliar\\soutils.lsp")
  ;(inicializar)
  (setq cm (getvar "cmdecho"))
  (setq os (getvar "osmode"))
  (setvar "osmode" 0)
  (setvar "cmdecho" 0)
  
;-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
  (defun verifica_polyline ( / selepoly lipl)
    (command "explode" pl)
    (setq selepoly (ssget "p" '((0 . "ARC")) ))
    (if (= selepoly nil)
      (setq resp 0)
      (setq resp 1) )
    (command "undo" ""))
;-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
  (defun desenha_pline (l)
    (setvar "clayer" layarc)
    (command "pline")
    (while l
      (command (car l))
      (setq l (cdr l)) )
    (command "")
    (setq plnova (entlast)))
;-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
  (defun extrai_coordenadas ( / aux pta)
    (setq lipto (list ptf))
    (setq c -1)
    (repeat (sslength selaux)
      (setq c (1+ c))
      (setq aux (ssname selaux c))
      (setq pta (list (cdr (assoc 10 (entget aux)))))
      (setq lipto (append lipto pta)) )
    (setq lipto (append lipto (list pti))) )
;-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
  (defun spline_polyline ( / selarc n qa arc liarc pto raio pti ptf selaux)
    (setq selarc (ssget "x" '((0 . "SPLINE")) ))
    (setq n 0)
    (if (= selarc nil)
      (setq qa 0)
      (setq qa (sslength selarc)) )
    (if (/= qa 0)
      (progn
	(command "chprop" selarc "" "c" 5 "")
	(command "layer" "m" "AUXARC" "") ))
    (repeat qa
      (setq arc (ssname selarc n))
      (setq n (1+ n))
      (setq liarc (entget arc))
      (setq layarc (cdr (assoc 8 liarc)))
      (setq pti (cdr (assoc 10 liarc)))
      (setq nt 0)
      (setq li (nth nt liarc))
      (while (/= li nil)
	(setq nt (1+ nt))
	(setq tp (car li))
	(if (= tp 10)
	  (setq ptf (cdr li)))
	(setq li (nth nt liarc)) )
      (command "lengthen" arc "")
      (setq peri (fix (getvar "perimeter")))
      (if (<= peri 10) (setq peri 10))
      (setvar "clayer" "AUXARC")
      (command "divide" arc peri)
      (setq selaux (ssget "x" '((0 . "POINT") (8 . "AUXARC")) ))
      (extrai_coordenadas)
      (entdel arc)
      (command "erase" selaux "")
      (desenha_pline lipto)
      (if (/= pol nil) (ssadd plnova pol)) ))
;-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
  (defun arc_polyline ( / selarc n qa arc liarc pto raio pti ptf selaux)
    (setq selarc (ssget "x" '((0 . "ARC")) ))
    (setq n 0)
    (if (= selarc nil)
      (setq qa 0)
      (setq qa (sslength selarc)) )
    (if (/= qa 0)
      (progn
	(command "chprop" selarc "" "c" 5 "")
	(command "layer" "m" "AUXARC" "") ))
    (repeat qa
      (setq arc (ssname selarc n))
      (setq n (1+ n))
      (setq liarc (entget arc))
      (setq pto (cdr (assoc 10 liarc)))
      (setq raio (cdr (assoc 40 liarc)))
      (setq layarc (cdr (assoc 8 liarc)))
      (setq pti (polar pto (cdr (assoc 50 liarc)) raio))
      (setq ptf (polar pto (cdr (assoc 51 liarc)) raio))
      (command "lengthen" arc "")
      (setq peri (fix (getvar "perimeter")))
      (if (<= peri 20) (setq peri 20))
      (setvar "clayer" "AUXARC")
      (command "divide" arc (fix (/ peri 10)))
      (setq selaux (ssget "x" '((0 . "POINT") (8 . "AUXARC")) ))
      (extrai_coordenadas)
      (entdel arc)
      (command "erase" selaux "")
      (desenha_pline lipto)
      (if (/= pol nil) (ssadd plnova pol)) ))
;-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
  
; MAIN PROGRAM
  
  (setvar "plinewid" 0)
  (arc_polyline)
  (spline_polyline)
  (setq qp 0)
  (setq selpl (ssget "x" '((0 . "LWPOLYLINE")) ))
  (if (= selpl nil)
    (setq qp 0)
    (setq qp (sslength selpl)) )
  (setq cp -1)
  (repeat qp
    (setq cp (1+ cp))
    (setq pl (ssname selpl cp))
    (verifica_polyline)
    (repeat resp
      (setq laypl (cdr (assoc 8 (entget pl))))
      (command "explode" pl)
      (setq pol (ssadd))
      (setq pol (ssget "p" '((-4 . "<NOT") (0 . "ARC") (-4 . "NOT>")) ))
      (arc_polyline)
      (if (= pol nil)
	(setq qpol 0)
	(setq qpol (sslength pol)) )
      (if (and (/= qpol 0) (>= qpol 2))
	(command "pedit" pol "" "j" pol "" "")) ))
  (setvar "cmdecho" cm)
  (setvar "osmode" os)
;;;  (finalizar)
  (princ) )
;*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*so*

