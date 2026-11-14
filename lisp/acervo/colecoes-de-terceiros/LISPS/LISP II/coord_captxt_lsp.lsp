<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.0 Transitional//EN">
<!-- saved from url=(0052)http://www.autolisp.com.br/aaa1/aa7/coord_captxt.lsp -->
<HTML><HEAD>
<META http-equiv=Content-Type content="text/html; charset=windows-1252">
<META content="MSHTML 6.00.2900.2722" name=GENERATOR></HEAD>
<BODY><PRE>;|
#########################################################################
#  www.autolisp.com.br                                                  #
#  Copyright (c)2002 Marcos Mendes                                      #
#  URL: http://www.autolisp.com.br                                      #
#  Email: mmal@estadao.com.br ou marcos@autolisp.com.br                 #
#                                                                       #
# Esta rotina pode ser distribuida livremente!                          #
# Não remova este cabeçalho!                                            #
#                                                                       #
#########################################################################
|;
;; retorna uma lista contendo as coordenadas que encapsulam um texto 
(defun captxt (ent   /	   entl	 box   boxl  boxh  ang	 ang90 evec
	       veclist	   p1	 p2    p3    p4	   lista p1a   p2a
	       p3a   p4a
	      )
  (setq entl (entget ent))
  (setq box (cadr (textbox entl)))
  (setq boxl (car box))
  (setq boxh (cadr box))
  (setq ang (cdr (assoc 50 entl)))
  (setq ang90 (+ ang (/ pi 2)))
  (setq evec (cdr (assoc 210 entl)))
  (setq	p1 (cdr (assoc 10 entl))
	p2 (polar p1 ang boxl)
	p3 (polar p2 ang90 boxh)
	p4 (polar p1 ang90 boxh)
  )
  (setq	p1a (trans p1 evec 1)
	p2a (trans p2 evec 1)
	p3a (trans p3 evec 1)
	p4a (trans p4 evec 1)
  )
  (setq lista (list p1a p2a p3a p4a))
)
</PRE></BODY></HTML>
