(defun c:ZE()(command "_.zoom" "_e")(princ))
(defun c:Za()(command "_.zoom" "_a")(princ))
(defun c:Zp()(command "_.zoom" "_p")(princ))
(defun c:Zd()(command "_.zoom" "_d")(princ))
(defun c:Zc()(command "_.zoom" "_c")(princ))
(defun c:Zv()(command "_.zoom" "_v")(princ))



 ;======================================================================
 ;         Funçoes trigonométricas                             
 ;======================================================================
(DEFUN ACOS (X) ;Inverse cosine
    (- (/ PI 2.0) (ATAN (/ X (SQRT (- 1.0 (* X X))))))
) ;_  Fim do defun
(DEFUN ASIN (X) ; Inverse sine
    (ATAN (/ X (SQRT (- 1.0 (* X X)))))
) ;_  Fim do defun
(DEFUN ACSC (X) ; Inverse cosecant
    (+ (ATAN (/ 1.0 (SQRT (- (* X X) 1.0))))
       (* (/ PI 2.0)
	  (- (IF (< X 0)
		 (- 1.0)
		 (+ 1.0)
	     ) ;_  Fim do if
	     1.0
	  ) ;_  Fim do -
       ) ;_  Fim do *
    ) ;_  Fim do +
) ;_  Fim do defun
(DEFUN ASEC (X) ; Inverse secant
    (+ (ATAN (SQRT (- (* X X) 1.0)))
       (* (/ PI 2.0)
	  (- (IF (< X 0)
		 (- 1.0)
		 (+ 1.0)
	     ) ;_  Fim do if
	     1.0
	  ) ;_  Fim do -
       ) ;_  Fim do *
    ) ;_  Fim do +
) ;_  Fim do defun
(DEFUN ACOT (X) ; Inverse cotangent
    (- (/ PI 2.0) (ATAN X))
) ;_  Fim do defun
(DEFUN SEC (X) ; Secant
    (/ 1.0 (COS X))
) ;_  Fim do defun
(DEFUN CSC (X) ; Cosecant
    (/ 1.0 (SIN X))
) ;_  Fim do defun
(DEFUN DTR (X) ; Degree to Radian conversion
    (/ (* X PI) 180.0)
) ;_  Fim do defun
(DEFUN RTD (X) ; Radian to Degree conversion
    (/ (* X 180.0) PI)
) ;_  Fim do defun
(DEFUN TAN (X) ; Tangent
    (/ (SIN X) (COS X))
) ;_  Fim do defun
(DEFUN COT (X) ; Cotangent
    (/ (COS X) (SIN X))
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================








 ;======================================================================
 ;         Converte Maiuscula p/ minuscula                              
 ;======================================================================
(DEFUN C:XCASE ()
    (SETVAR "cmdecho" 0)
    (sf)
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (sf)
    (COMMAND "_.undo" "_Group")
    (PROMPT " Troca textos selecionados de maiuscula para minusculo e vice-versa  ")
    (SETQ Selecao (SSGET)
	  cont	  0
    ) ;_  Fim do SETQ
    (IF	Selecao
	(PROGN
	    (INITGET 1 "N M")
	    (SETQ caso (GETKWORD "\nTransforma para (n/M)   :  "))
	    (REPEAT (SSLENGTH Selecao)
		(SETQ Nome (SSNAME selecao cont)
		      dxf  (ENTGET Nome)
		      tipo (CDR (ASSOC 0 dxf))
		) ;_  Fim do SETQ
		(IF (= tipo "TEXT")
		    (PROGN
			(SETQ texto   (CDR (ASSOC 1 dxf))
			      lntexto (STRLEN texto)
			      o_chr   (ASCII (SUBSTR texto 1 1))
			) ;_  Fim do setq
			(COND
			    ((= caso "N")
			     (IF (OR (AND (<= o_chr 90)
					  (>= o_chr 65)
				     ) ;_  Fim do AND
				     (AND
					 (<= o_chr 223)
					 (>= o_chr 192)
				     ) ;_  Fim do and
				 ) ;_  Fim do or
				 (SETQ n_chr (CHR (+ o_chr 32)))
				 (SETQ n_chr (CHR o_chr))
			     ) ;_  Fim do IF
			    )
			    ((= caso "M")
			     (IF (OR (AND (<= o_chr 122)
					  (>= o_chr 97)
				     ) ;_  Fim do AND
				     (AND
					 (<= o_chr 253)
					 (>= o_chr 224)
				     ) ;_  Fim do OR
				 ) ;_  Fim do AND
				 (SETQ n_chr (CHR (- o_chr 32)))
				 (SETQ n_chr (CHR o_chr))
			     ) ;_  Fim do IF
			    ) ;_  Fim do COND
			) ;_  Fim do COND
			(SETQ n_str n_chr
			      cnt   2
			) ;_  Fim do setq

			(REPEAT	(- lntexto 1)
			    (SETQ o_chr (ASCII (SUBSTR texto cnt 1)))
			    (COND
				((= caso "N")
				 (IF (OR (AND (<= o_chr 90)
					      (>= o_chr 65)
					 ) ;_  Fim do AND
					 (AND
					     (<= o_chr 223)
					     (>= o_chr 192)
					 ) ;_  Fim do OR
				     ) ;_  Fim do OR
				     (SETQ n_chr (CHR (+ o_chr 32)))
				     (SETQ n_chr (CHR o_chr))
				 ) ;_  Fim do IF
				)
				((= caso "M")
				 (IF (OR (AND (<= o_chr 122)
					      (>= o_chr 97)
					 ) ;_  Fim do AND
					 (AND
					     (<= o_chr 253)
					     (>= o_chr 224)
					 ) ;_  Fim do OR
				     ) ;_  Fim do OR
				     (SETQ n_chr (CHR (- o_chr 32)))
				     (SETQ n_chr (CHR o_chr))
				 ) ;_  Fim do if
				) ;_  Fim do IF
			    ) ;_  Fim do cond
			    (SETQ n_str	(STRCAT n_str n_chr)
				  cnt	(+ cnt 1)
			    ) ;_  Fim do setq
			) ;_  Fim do repeat
			(SETQ dxf (SUBST (CONS 1 n_str) (ASSOC 1 dxf) dxf))
			(ENTMOD dxf)
		    ) ;_  Fim do progn
		) ;_  Fim do IF
		(SETQ cont (1+ cont))
	    ) ;_  Fim do PROGN
	) ;_  Fim do IF
	(PRINC "\n Nada foi selecionado")
    ) ;_  Fim do REPEAT
    (SETQ *error* errorot)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do PROGN
 ;==================================================================================
 ;==================================================================================




 ;======================================================================
 ;            Quebra texto com espaços                                  
 ;======================================================================
(DEFUN c:qbt ()
    (SETVAR "CMDECHO" 0)
    (SETQ olderro *error*
	  *error* errotxt
    ) ;_  Fim do SETQ
    (COMMAND "._UNDO" "_Group")
    (SETQ oldangdir  (GETVAR "angdir")
	  oldangbase (GETVAR "angbase")
	  oldosmode  (GETVAR "osmode")
	  olducs     (GETVAR "ucsname")
	  oldcolor   (GETVAR "cecolor")
	  oldestilo  (GETVAR "textstyle")
    ) ;_  Fim do setq
    (sf)
    (SETVAR "angdir" 0)
    (SETVAR "angbase" 0)
    (COMMAND "_.ucs" "")
    (PRINC "\n Quebra um texto com espaços em varios textos")
    (GRAPHSCR)
    (SETQ sel (SSGET)
	  cont 0
	  contxt 0
    ) ;_  Fim do setq
    (REPEAT (SSLENGTH sel)
	(SETQ objTxt (SSNAME sel cont)
	      elem   (ENTGET objTxt)
	      nome   (CDR (ASSOC 0 elem))
	      cont   (+ 1 cont)
	) ;_  Fim do setq
	(SETVAR "osmode" 0)
	(IF (= nome "TEXT")
	    (PROGN
		(SETQ texto (CDR (ASSOC 1 elem))
		      tamt  (STRLEN texto)
		      ind   1
		      lst   nil
		) ;_  Fim do setq
		(REPEAT	tamt
		    (SETQ txt (SUBSTR texto ind 1))
		    (IF	(= txt " ")
			(SETQ lst (APPEND lst (LIST ind)))
		    ) ;_  Fim do IF
		    (SETQ ind (1+ ind))
		) ;_  Fim do REPEAT
		(IF lst
		    (PROGN
			(SETQ ind 1
			      ind1 0
			      lista nil
			) ;_  Fim do setq
			(REPEAT	(LENGTH lst)
			    (SETQ ind1 (+ (NTH (- ind 1) lst) 1)
				  indb (NTH ind lst)
			    ) ;_  Fim do setq
			    (IF	(AND (/= ind1 indb) (<= ind1 tamt))
				(SETQ lista (APPEND lista (LIST ind1)))
			    ) ;_  Fim do IF
			    (SETQ ind (1+ ind))
			) ;_  Fim do REPEAT
			(SETQ lista (APPEND lista (LIST (1+ tamt))))
			(SETQ ind      0
			      pt1      (CDR (ASSOC 10 elem))
			      alttexto (CDR (ASSOC 40 elem))
			      layer    (CDR (ASSOC 8 elem))
			      estilo   (CDR (ASSOC 7 elem))
			      ang      (CDR (ASSOC 50 elem))
			      cor      nil
			      cor      (CDR (ASSOC 62 elem))
			      Entd     (TBLSEARCH "style" Estilo)
			      altT     (CDR (ASSOC '40 entd))
			      ind1     1
			      ptt      (TEXTBOX elem)
			      x1       (CAR (CAR ptt))
			      x2       (CAAR (CDR ptt))
			      dist     (- x2 x1)
			      fat      (/ dist tamt)
			) ;_  Fim do PROGN
			(REPEAT	(LENGTH lista)
			    (SETQ fin  (NTH ind lista)
				  i    (- fin ind1)
				  txt  (SUBSTR texto ind1 i)
				  ang1 (* ang 57.2957795131)
			    ) ;_  Fim do setq
			    (COMMAND "_.color" cor)
			    (IF	(= 0 altT)
				(COMMAND "_.text" "_s" estilo pt1 alttexto ang1 txt)
				(PROGN
				    (SETVAR "textsize" alttexto)
				    (COMMAND "_.text" "_s" estilo pt1 ang1 txt)
				) ;_  Fim do progn
			    ) ;_  Fim do if
			    (SETQ pt1  (POLAR pt1 ang (* fat i))
				  ind1 fin
				  ind  (+ ind 1)
			    ) ;_  Fim do SETQ
			    (SETQ ult (ENTLAST)
				  obj (ENTGET ult)
				  obj (SUBST (CONS 8 layer) (ASSOC 8 obj) obj)
				  obj (SUBST (CONS 62 cor) (ASSOC 62 obj) obj)
				  obj (SUBST (CONS 40 alttexto) (ASSOC 40 obj) obj)
			    ) ;_  Fim do setq
			    (ENTMOD obj)
			    (ENTUPD ult)
			    (COMMAND "_.erase" objTxt "")
			) ;_  Fim do REPEAT
		    ) ;_  Fim do progn
		) ;_  Fim do if
	    ) ;_  Fim do PROGN
	) ;_  Fim do IF
    ) ;_  Fim do REPEAT
    (SETQ *error* olderro)
    (SETVAR "textstyle" oldestilo)
    (SETVAR "osmode" oldosmode)
    (SETVAR "angdir" oldangdir)
    (SETVAR "angbase" oldangbase)
    (SETVAR "cecolor" oldcolor)
    (SETVAR "textstyle" oldestilo)
    (COMMAND "_.ucs" "_p")
    (COMMAND "._UNDO" "_End")
    (PRINC)
) ;_  Fim do DEFUN
 ;==================================================================================
 ;==================================================================================


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(DEFUN extraiDadosBloco	(selecao /)
    (SETQ entidade (ENTGET (CAR Selecao))
	  blPt	   nil
    ) ;_  Fim do SETQ
    (SETQ blPtx (CADR (ASSOC '10 entidade)))
    (SETQ blPty (CADDR (ASSOC '10 entidade)))
    (SETQ blPtz (CADDDR (ASSOC '10 entidade)))
    (SETQ blScx (CDR (ASSOC '41 entidade)))
    (SETQ blScy (CDR (ASSOC '42 entidade)))
    (SETQ blScz (CDR (ASSOC '43 entidade)))
    (SETQ blAng (CDR (ASSOC '50 entidade)))
    (SETQ blPt (APPEND blPt (LIST (LIST blPtx blPty blPtz))))
) ;_  Fim do defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;





 ;======================================================================
 ;=            Rotina de tratamento de erros                            
 ;======================================================================
(DEFUN errorot (msg)
    (PRINC "\n******  Comando cancelado ****\n")
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================



 ;======================================================================
 ;======================================================================
(DEFUN sf ()
    (PRINC
	"\n      ***** Rotina do Programa SFTopo  www.sftopo.com.br *****"
    ) ;_  Fim do princ
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================






 ;=======================================================================
 ;                      Transforma radianos em grau decimal              
 ;=======================================================================
(DEFUN RadDEG (angR /)
    (SETQ angD (/ (* angR 180) PI))
) ;_  Fim do defun
 ;=======================================================================
 ;=======================================================================


 ;=======================================================================
 ;               Transforma  radiano em Graus minutos e segundos         
 ;=======================================================================
(DEFUN RadDMS (angR /)
    (RadDEG angR)
    (DegDms angD)
) ;_  Fim do defin
 ;=======================================================================
 ;=======================================================================




 ;=======================================================================
 ;             Transforma Angulo decimal em angulo minutos e segundos    
 ;=======================================================================
(DEFUN DegDms (angD /)
    (SETQ grau (FIX angD))
    (SETQ a (* (- angD grau) 60))
    (SETQ minuto (FIX a))
    (SETQ b (* (- a minuto) 60))
    (SETQ segundo (FIX (+ b 0.5)))
    (IF	(> segundo 59)
	(SETQ minuto  (+ minuto 1)
	      segundo 0
	) ;_  Fim do setq
    ) ;_  Fim do if
    (IF	(> minuto 59)
	(SETQ grau   (+ grau 1)
	      minuto 0
	) ;_  Fim do setq
    ) ;_  Fim do if
    (IF	(> grau 359)
	(SETQ grau 0)
    ) ;_  Fim do if
    (SETQ grau (RTOS grau 2 0))
    (IF	(< minuto 10)
	(SETQ minuto (STRCAT "0" (RTOS minuto 2 0)))
	(SETQ minuto (RTOS minuto 2 0))
    ) ;_  Fim do if
    (IF	(< segundo 10)
	(SETQ segundo (STRCAT "0" (RTOS segundo 2 0)))
	(SETQ segundo (RTOS segundo 2 0))
    ) ;_  Fim do if
) ;_  Fim do DEFUN
 ;=======================================================================
 ;=======================================================================






 ;======================================================================
 ;            Rotina para Rotular Arcos R, AC e D                       
 ;======================================================================
(DEFUN C:Rta ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (sf)
    (SETVAR "cmdecho" 0)
    (SETVAR "blipmode" 0)
    (COMMAND "_.undo" "_Group")
    (PRINC
	"\n  **** Rotula Arcos com Raio Angulo Central e Desenvolvida  ****\n"
    ) ;_  Fim do princ (inicio)
    (SETQ Estilo (GETVAR "textstyle"))
    (SETQ Entidade (TBLSEARCH "style" Estilo))
    (SETQ altTexto (CDR (ASSOC '40 Entidade)))
    (WHILE (SETQ Sel (ENTSEL "\nSelecone um Arco para Rotular!"))
	(SETQ Entidade (ENTGET (CAR Sel)))
	(IF (EQUAL "ARC" (CDR (ASSOC '0 Entidade)))
	    (PROGN
		(SETQ raio (CDR (ASSOC '40 entidade)))
		(SETQ ang1 (CDR (ASSOC '50 entidade)))
		(SETQ ang2 (CDR (ASSOC '51 entidade)))
		(IF (< ang1 ang2)
		    (SETQ angR (- ang2 ang1))
		    (SETQ angR (+ (* 2 PI) (- ang2 ang1)))
		) ;_  Fim do IF
		(SETQ desenv (* angR raio))
		(RadDms angR)
		(SETQ pt1 (GETPOINT "\n Ponto para inserção dos dados"))
		(IF (/= PT1 NIL)
		    (PROGN
			(SETQ txt1 (STRCAT "R  = " (RTOS raio 2 2) " m"))
			(SETQ
			    txt2 (STRCAT "AC = " grau "°" minuto "'" segundo (CHR 34))
			) ;_  Fim do SETQ
			(SETQ txt3 (STRCAT "D  = " (RTOS desenv 2 2) " m"))
			(IF (/= altTexto 0)
			    (COMMAND "_text" pt1 "" txt1)
			    (COMMAND "_text" pt1 "" "" txt1)
			) ;_  Fim do if
			(COMMAND "_text" "" txt2)
			(COMMAND "_text" "" txt3)
		    ) ;_  Fim do PROGN
		) ;_  Fim do IF
	    ) ;_  Fim do PROGN
	    (ALERT "A seleção não é Arco")
	) ;_  Fim do IF
    ) ;_  Fim do while
    (SETQ *error* errorot)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;======================================================================
 ;======================================================================






 ;======================================================================
 ;            Rotula as coordenadas E e N de um ponto                   
 ;======================================================================
(DEFUN c:Rtc ()
    (sf)
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do SETQ
    (SETQ oldosm (GETVAR "osmode"))
    (SETVAR "cmdecho" 0)
    (SETVAR "blipmode" 0)
    (COMMAND "_.undo" "_Group")
    (PRINC
	"\n       **** Rotula Coordenadas E e N de  ponto   ****\n"
    ) ;_  Fim do princ
    (IF	(= escala nil)
	(mscl)
    ) ;_  Fim do if
    (SETQ pt1 (LIST 1 1 0))
    (SETQ p0 (LIST 0 0 0))
    (SETQ a (FLOAT escala))
    (SETQ dt1 (/ a 333.33333333))
    (SETQ of  (* dt1 0.20)
	  alt (* 0.50 dt1)
    ) ;_  Fim do setq
    (SETQ pt1 (GETPOINT "\nClique no ponto p/ Rotular:  "))
    (WHILE (/= pt1 nil)
	(SETQ pt2 (GETPOINT pt1 "\nPonto p/ inserir texto:  "))
	(IF (/= pt2 nil)
	    (PROGN
		(SETVAR "osmode" 0)
		(COMMAND "_.line" pt1 pt2 "")
		(SETQ pt3 (GETORIENT pt2 "\nAngulo p/ inserir o Texto:  "))
		(COMMAND "_.erase" "_last" "")
		(IF (NOT pt3)
		    (PROGN
			(SETQ p1 (GETVAR "ucsydir"))
			(SETQ ang (ANGLE p0 p1))
			(SETQ pt5 (TRANS pt1 1 0))
			(SETQ pt6 (TRANS pt2 1 0))
			(SETQ e	(CAR pt5)
			      x	(CAR pt6)
			) ;_  Fim do setq
			(IF (< x e)
			    (SETQ ang (+ PI ang))
			) ;_  Fim do if
		    ) ;_  Fim do progn
		    (SETQ ang (- (/ PI 2) pt3))
		) ;_  Fim do if
		(SETQ e	(CAR pt1)
		      n	(CADR pt1)
		) ;_  Fim do setq
		(SETQ se (RTOS e 2 3))
		(SETQ sn (RTOS n 2 3))
		(WHILE (< (STRLEN se) (STRLEN sn))
		    (SETQ se (STRCAT " " se))
		) ;_  Fim do while
		(WHILE (< (STRLEN sn) (STRLEN se))
		    (SETQ sn (STRCAT " " sn))
		) ;_  Fim do while
		(IF (> n 1000000)
		    (SETQ ce (STRCAT " E=" se)
			  cn (STRCAT " N=" sn)
		    ) ;_  Fim do setq
		    (SETQ ce (STRCAT " X=" se)
			  cn (STRCAT " Y=" sn)
		    ) ;_  Fim do setq
		) ;_  Fim do if
		(SETQ fx (STRLEN ce))
		(SETQ fx (* (+ 0.3 fx) alt))
		(SETQ ang1 (- (/ PI 2) (ANGLE pt1 pt2)))
		(SETQ x (+ e (* dt1 (SIN ang1))))
		(SETQ y (+ n (* dt1 (COS ang1))))
		(SETQ pt4 (LIST x y 0))
		(SETQ e	(CAR pt2)
		      n	(CADR pt2)
		) ;_  Fim do setq
		(SETQ x (+ e (* fx (SIN ang))))
		(SETQ y (+ n (* fx (COS ang))))
		(SETQ pt3 (LIST x y 0))
		(SETQ pt5 (TRANS pt2 1 0))
		(SETQ pt6 (TRANS pt3 1 0))
		(SETQ e	(CAR pt5)
		      x	(CAR pt6)
		) ;_  Fim do setq
		(SETQ est nil)
		(SETQ est (TBLSEARCH "STYLE" "Mono"))
		(IF (= est nil)
		    (COMMAND "_.style" "Mono" "courier new" "0" "1.0" "" "" "")
		) ;_  Fim do if
		(PROGN
		    (SETVAR "osmode" 0)
		    (COMMAND "_.pline" pt1 "_w" 0 of pt4 "_w" 0 0 pt2 pt3 "")
		    (IF	(>= x e)
			(PROGN
			    (COMMAND "_.text" "s" "MONO" "j" "bl" pt2 alt pt3 ce)
			    (COMMAND "_.text" "" cn)
			) ;_  Fim do progn
			(PROGN
			    (SETQ e (STRLEN ce))
			    (SETQ ce (SUBSTR ce 2 (- e 1)))
			    (SETQ cn (SUBSTR cn 2 (- e 1)))
			    (COMMAND "_.text" "_s" "MONO" "_j" "bl" pt3 alt pt2 ce)
			    (COMMAND "_.text" "" cn)
			) ;_  Fim do progn
		    ) ;_  Fim do if
		    (SETVAR "osmode" oldosm)
		) ;_  Fim do progn
		(PRINC "\n------------")
		(SETQ pt1 (GETPOINT "\nNovo ponto p/ Rotular (<ENTER> finaliza): "))
	    ) ;_  Fim do progn
	) ;_  Fim do if
    ) ;_  Fim do while
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================



 ;====================================================================
 ;          Rotina para rotular somente area                          
 ;====================================================================
(DEFUN c:areaM ()
    (sf)
    (PRINC "\n  Rotula área em m2")
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (SETQ Estilo (GETVAR "textstyle"))
    (SETQ Entidade (TBLSEARCH "style" Estilo))
    (SETQ altTexto (CDR (ASSOC '40 Entidade)))
    (SETQ p1 (GETPOINT "\nClique no centro da Area <ENTER> p/ terminar "))
    (WHILE p1
	(COMMAND "_.boundary" p1 "")
	(COMMAND "_.area" "_e" "_last")
	(COMMAND "_.erase" "_last" "")
	(SETQ str (GETVAR "area"))
	(SETQ t1 (RTOS str 2 2))
	(SETQ s "ÁREA= ")
	(SETQ a " m2")
	(SETQ r (STRCAT s t1 a))
	(IF (/= altTexto 0)
	    (COMMAND "_.text" p1 "" r)
	    (COMMAND "_.text" p1 "" "" r)
	) ;_  Fim do IF
	(SETQ p1 (GETPOINT "\nClique no centro da Area <ESC> p/ terminar "))
    ) ;_  Fim do while
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;====================================================================
 ;====================================================================



 ;====================================================================
 ;            Rotina para rotular somente area                        
 ;====================================================================
(DEFUN c:areaH ()
    (sf)
    (PRINC "\n  Rotula área em m2")
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (SETQ Estilo (GETVAR "textstyle"))
    (SETQ Entidade (TBLSEARCH "style" Estilo))
    (SETQ altTexto (CDR (ASSOC '40 Entidade)))
    (SETQ p1 (GETPOINT "\nClique no centro da Area <ENTER> p/ terminar "))
    (WHILE p1
	(COMMAND "_.boundary" p1 "")
	(COMMAND "_.area" "_e" "_last")
	(COMMAND "_.erase" "_last" "")
	(SETQ str (GETVAR "area"))
	(SETQ str (/ str 10000))
	(SETQ t1 (RTOS str 2 5))
	(SETQ s "ÁREA= ")
	(SETQ a " Ha")
	(SETQ r (STRCAT s t1 a))
	(IF (/= altTexto 0)
	    (COMMAND "_.text" p1 "" r)
	    (COMMAND "_.text" p1 "" "" r)
	) ;_  Fim do IF
	(SETQ p1 (GETPOINT "\nClique no centro da Area <ESC> p/ terminar "))
    ) ;_  Fim do while
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;====================================================================
 ;====================================================================






 ;====================================================================
 ;             Lisp para desenhar casa                                
 ;====================================================================
(DEFUN c:ret ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (sf)
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (PRINC "\n  Desenha retangulos dados 3 pontos")
    (SETQ pt1	 "1 1 0"
	  oldosm (GETVAR "osmode")
    ) ;_  Fim do SETQ
    (WHILE pt1
	(SETQ pt1 (GETPOINT "\nPrimeiro ponto: "))
	(IF (= pt1 nil)
	    (EXIT)
	) ;_  Fim do if
	(SETQ pt2 (GETPOINT pt1 "\nSegundo ponto: "))
	(SETQ pt3 (GETPOINT pt2 "\nPonto Offset: "))
	(SETQ dt1 (DISTANCE pt3 pt2))
	(SETQ ang (ANGLE pt1 pt2))
	(SETQ ang1 (ANGLE pt2 pt3))
	(SETQ amblng2 (+ PI ang))
	(IF (>= ang2 (* PI 2))
	    (SETQ ang2 (- ang2 (* PI 2)))
	) ;_  Fim do if
	(IF (> ang2 ang)
	    (IF	(AND (> ang1 ang) (< ang1 ang2))
		(SETQ ang3 (+ ang (/ PI 2)))
		(SETQ ang3 (+ ang (/ PI 2) PI))
	    ) ;_  Fim do if
	    (IF	(AND (< ang1 ang) (> ang1 ang2))
		(SETQ ang3 (+ ang (/ PI 2) PI))
		(SETQ ang3 (+ ang (/ PI 2)))
	    ) ;_  Fim do if
	) ;_  Fim do if
	(IF (>= ang3 (* PI 2))
	    (SETQ ang3 (- ang3 (* PI 2)))
	) ;_  Fim do if
	(SETQ dif (ABS (- ang3 ang1)))
	(SETQ dt1 (* dt1 (COS dif)))
	(SETQ pt4 (POLAR pt2 ang3 dt1))
	(SETQ pt5 (POLAR pt1 ang3 dt1))
	(COMMAND "_.osnap" "_none")
	(COMMAND "_.pline" pt1 pt2 pt4 pt5 "_c")
	(SETVAR "osmode" oldosm)
    ) ;_  Fim do while
    (SETQ *error* errorot)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;====================================================================
 ;====================================================================









 ;======================================================================
 ;=            Controla esclas no desenho                              =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:SFe ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (mscl)
    (PRINC)
) ;_  Fim do DEFUN
 ;======================================================================
 ;======================================================================





 ;======================================================================
 ;=            Cria estilos de textos                                  =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:SFs (/ fsc)
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (sf)
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (PRINC "\n**********  Criando estilos - Aguarde! *********")
    (SETQ Escala (GETVAR "USERR1"))
    (IF	(> 1 Escala)
	(mscl)
    ) ;_  Fim do if
    (SETQ fsc (/ Escala 1000))
 ;------estilo romans simples
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS120"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS120"
		 "romans"
		 (* fsc 1.20)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS150"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS150"
		 "romans"
		 (* fsc 1.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS200"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS200"
		 "romans"
		 (* fsc 2.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS250"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS250"
		 "romans"
		 (* fsc 2.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS300"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS300"
		 "romans"
		 (* fsc 3.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS350"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS350"
		 "romans"
		 (* fsc 3.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS400"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS400"
		 "romans"
		 (* fsc 4.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS450"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS450"
		 "romans"
		 (* fsc 4.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS500"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS500"
		 "romans"
		 (* fsc 5.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS600"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS600"
		 "romans"
		 (* fsc 6.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS700"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS700"
		 "romans"
		 (* fsc 7.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS800"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS800"
		 "romans"
		 (* fsc 8.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS900"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS900"
		 "romans"
		 (* fsc 9.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RS1000"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RS1000"
		 "romans"
		 (* fsc 10.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "Romans"))
    (IF	(= est nil)
	(COMMAND "style" "ROMANS" "romans" "0" "1.0" "" "" "" "")
    ) ;_  Fim do if
 ;------estilo romans duplo
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD120"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD120"
		 "romanD"
		 (* fsc 1.20)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD150"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD150"
		 "romanD"
		 (* fsc 1.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD200"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD200"
		 "romanD"
		 (* fsc 2.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD250"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD250"
		 "romanD"
		 (* fsc 2.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD300"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD300"
		 "romanD"
		 (* fsc 3.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD350"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD350"
		 "romanD"
		 (* fsc 3.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD400"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD400"
		 "romanD"
		 (* fsc 4.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD450"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD450"
		 "romanD"
		 (* fsc 4.50)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD500"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD500"
		 "romanD"
		 (* fsc 5.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD600"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD600"
		 "romanD"
		 (* fsc 6.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD700"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD700"
		 "romanD"
		 (* fsc 7.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD800"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD800"
		 "romanD"
		 (* fsc 8.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD900"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD900"
		 "romanD"
		 (* fsc 9.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RD1000"))
    (IF	(= est nil)
	(COMMAND "style"
		 "RD1000"
		 "romanD"
		 (* fsc 10.00)
		 "1.0"
		 ""
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "RomanD"))
    (IF	(= est nil)
	(COMMAND "style" "ROMAND" "romanD" "0" "1.0" "" "" "" "")
    ) ;_  Fim do if
 ;------estilo Arial
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR120"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR120"
		 "arial"
		 (* fsc 1.20)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR150"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR150"
		 "arial"
		 (* fsc 1.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR200"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR200"
		 "arial"
		 (* fsc 2.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR250"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR250"
		 "arial"
		 (* fsc 2.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR300"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR300"
		 "arial"
		 (* fsc 3.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR350"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR350"
		 "arial"
		 (* fsc 3.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR400"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR400"
		 "arial"
		 (* fsc 4.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR450"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR450"
		 "arial"
		 (* fsc 4.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR500"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR500"
		 "arial"
		 (* fsc 5.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR600"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR600"
		 "arial"
		 (* fsc 6.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR700"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR700"
		 "arial"
		 (* fsc 7.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR800"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR800"
		 "arial"
		 (* fsc 8.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR900"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR900"
		 "arial"
		 (* fsc 9.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "AR1000"))
    (IF	(= est nil)
	(COMMAND "style"
		 "AR1000"
		 "arial"
		 (* fsc 10.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "arial"))
    (IF	(= est nil)
	(COMMAND "style" "ARIAL" "arial" "0" "1.0" "" "" "")
    ) ;_  Fim do if
 ;------estilo mono texto
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT120"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT120"
		 "courier new"
		 (* fsc 1.20)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT150"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT150"
		 "courier new"
		 (* fsc 1.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT200"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT200"
		 "courier new"
		 (* fsc 2.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT250"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT250"
		 "courier new"
		 (* fsc 2.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT300"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT300"
		 "courier new"
		 (* fsc 3.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT350"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT350"
		 "courier new"
		 (* fsc 3.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT400"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT400"
		 "courier new"
		 (* fsc 4.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT450"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT450"
		 "courier new"
		 (* fsc 4.50)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT500"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT500"
		 "courier new"
		 (* fsc 5.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT600"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT600"
		 "courier new"
		 (* fsc 6.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT700"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT700"
		 "courier new"
		 (* fsc 7.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT800"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT800"
		 "courier new"
		 (* fsc 8.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT900"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT900"
		 "courier new"
		 (* fsc 9.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "MT1000"))
    (IF	(= est nil)
	(COMMAND "style"
		 "MT1000"
		 "courier new"
		 (* fsc 10.00)
		 "1.0"
		 ""
		 ""
		 ""
	) ;_  Fim do COMMAND
    ) ;_  Fim do if
    (SETQ est nil)
    (SETQ est (TBLSEARCH "STYLE" "Mono"))
    (IF	(= est nil)
	(COMMAND "style" "Mono" "courier new" "0" "1.0" "" "" "")
    ) ;_  Fim do if
    (COMMAND "_.undo" "_end")
    (PRINC "\n---------------------------------------------------------")
    (PRINC "\n        Estilos carregados!!!!!!!")
    (PRINC "\n---------------------------------------------------------")
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================





 ;======================================================================
 ;=                                                                    =
 ;=            Controle de escala nos desenhos                         =
 ;=                                                                    =
 ;======================================================================
(DEFUN mscl ()
    (SETQ fsc (GETVAR "USERR1"))
    (IF	(= fsc 0)
	(PROGN
	    (PRINC "\nDesenho sem escala definida")
	    (INITGET 6)
	    (SETQ fsc (GETREAL "\nNova escala para Carregar estilos <1000>: "))
	    (IF	(NOT fsc)
		(SETQ fsc 1000)
	    ) ;_  Fim do if
	    (SETVAR "userr1" fsc)
	) ;_  Fim do progn
	(PROGN
	    (PRINC "\nEscala Atual 1: ")
	    (PRINC (FIX fsc))
	    (INITGET 6)
	    (SETQ fsc1 (GETREAL "\n***Digite a Nova Escala 1: "))
	    (IF	fsc1
		(SETQ fsc fsc1)
	    ) ;_  Fim do if
	    (SETVAR "userr1" fsc)
	) ;_  Fim do progn
    ) ;_  Fim do if
    (IF	(> fsc 0)
	(PROGN
	    (SETQ escala fsc)
	    (WHILE (SETQ gr1 (TBLNEXT "STYLE" (NOT gr1)))
		(SETQ estilo (CDR (ASSOC 2 gr1)))
		(SETQ gr2 (TBLOBJNAME "style" estilo))
		(SETQ gr (ENTGET gr2))
		(SETQ comp (STRLEN estilo))
		(IF (> comp 2)
		    (PROGN
			(SETQ inic (- comp 2))
			(SETQ a (ATOI (SUBSTR estilo inic 3)))
			(SETQ a (* a fsc))
			(SETQ a (FLOAT a))
			(SETQ alt1 (/ a 100000))
			(SETQ gr (SUBST (CONS 40 alt1) (ASSOC 40 gr1) gr))
			(ENTMOD gr)
		    ) ;_  Fim do progn
		) ;_  Fim do if
	    ) ;_  Fim do while
	) ;_  Fim do progn

    ) ;_  Fim do if
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================





 ;============================================================================
 ;                     Rotina para desennhar malha de coordenadas             
 ;============================================================================
(DEFUN c:mcoord	()
    (SETQ olderro   *error*
	  *error*   erromcoord
	  oldosmode (GETVAR "osmode")
    ) ;_  Fim do setq
    (DEFUN erromcoord (msg)
	(PRINC "\n******  Comando cancelado ****\n")
	(SETQ *error* olderro)
	(SETVAR "osmode" oldosmode)
	(COMMAND "_.undo" "_end")
	(PRINC)
    ) ;_  Fim do defun
    (sf)
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (SETQ oldosmode (GETVAR "osmode"))
    (verescala)
    (PRINC
	"\n*** Desenha malha de coordenadas dentro de uma polilinha ***"
    ) ;_  Fim do princ
    (SETQ selent nil)
    (SETQ selent (SSGET "X" '((0 . "lwpolyline") (8 . "quadricula"))))
    (IF	(/= selent nil)
	(PROGN
	    (SETQ ofml (GETDIST "\nDistancia entre as malhas <100> : "))
	    (COMMAND "_.color" "_bylayer")
	    (COMMAND "osmode" 0)
	    (COMMAND "_.layer" "_s" "quadricula" "")
	    (COMMAND "_.layer" "_off" "*" "" "")
	    (INITGET 6)
	    (IF	(= ofml nil)
		(SETQ ofml 100)
	    ) ;_ end of if
;;;;;;;;;;;;;;;;;;incio dos calculos
;;;;;
;;;;;;;;;;;;;Lista de pontos da polilinha
	    (SETQ selecao (SSNAME selent 0))
	    (SETQ vertlw nil)
	    (extraipontos selecao)
	    (SETQ lista	vertlw
		  de	0
		  para	1
	    ) ;_  Fim do setq
	    (translista)
	    (SETQ vertlw nlista)
 ;       Verificar pontos maximos e minimos da polilinha
	    (SETQ xmax -10000000
		  ymax -10000000
		  xmin 10000000
		  ymin 10000000
		  cont 0
		  num  (LENGTH vertlw)
	    ) ;_  Fim do setq
	    (REPEAT num
		(SETQ x	   (CAR (NTH cont vertlw))
		      y	   (CADR (NTH cont vertlw))
		      xmax (MAX xmax x)
		      ymax (MAX ymax y)
		      xmin (MIN xmin x)
		      ymin (MIN ymin y)
		      cont (+ 1 cont)
		) ;_  Fim do setq
	    ) ;_  Fim do repeat
	    (IF	(< xmax 0)
		(SETQ xmax (+ xmax ofml))
	    ) ;_  Fim do if
	    (IF	(< ymax 0)
		(SETQ ymax (+ ymax ofml))
	    ) ;_  Fim do if
	    (IF	(< xmin 0)
		(SETQ xmin (- xmin ofml))
	    ) ;_  Fim do if
	    (IF	(< ymin 0)
		(SETQ ymin (- ymin ofml))
	    ) ;_  Fim do if
	    (SETQ pmin (LIST xmin ymin)
		  pmax (LIST xmax ymax)
	    ) ;_  Fim do setq
	    (SETQ xmax (+ (* (FIX (/ xmax ofml)) ofml) ofml))
	    (SETQ ymax (+ (* (FIX (/ ymax ofml)) ofml) ofml))
	    (SETQ xmin (* (FIX (/ xmin ofml)) ofml))
	    (SETQ ymin (* (FIX (/ ymin ofml)) ofml))

;;;;;;;;;;;;;;;desenha as linhas de coordenadas

	    (SETQ xi (+ ofml xmin)
		  y1 ymin
		  y2 ymax
	    ) ;_  Fim do setq
	    (WHILE (< xi xmax)
		(SETQ p1 (LIST xi y1)
		      p2 (LIST xi y2)
		      xi (+ xi ofml)
		) ;_  Fim do setq
		(COMMAND "_.line" p1 p2 "")
	    ) ;_  Fim do while
	    (SETQ yi (+ ofml ymin)
		  x1 xmin
		  x2 xmax
	    ) ;_  Fim do setq
	    (WHILE (< yi ymax)
		(SETQ p1 (LIST x1 yi)
		      p2 (LIST x2 yi)
		      yi (+ yi ofml)
		) ;_  Fim do setq
		(COMMAND "_.line" p1 p2 "")
	    ) ;_  Fim do while

 ;-----------------fileta as linhas de coordenadas
	    (SETQ pm (LIST (- xmin 20) (- ymin 20)))
	    (COMMAND "_offset" ".05" selecao pm "")
	    (SETQ selecao1 (ENTLAST))
	    (SETQ selec (ENTLAST))
	    (SETQ vertlw nil)
	    (extraipontos selecao1)
	    (SETQ lista	vertlw
		  de	0
		  para	1
	    ) ;_  Fim do setq
	    (translista)
	    (SETQ vertlw nlista)
	    (SETQ num (LENGTH vertlw))
	    (REPEAT 5
		(COMMAND "_.trim" selecao "" "_f")
		(SETQ cont 0)
		(REPEAT	num
		    (SETQ pt   (NTH cont vertlw)
			  cont (+ 1 cont)
		    ) ;_  Fim do repeat
		    (COMMAND pt)
		) ;_  Fim do PROGN
		(COMMAND "" "")
	    ) ;_  Fim do repeat
;;;;;;;;;;;;;Selecao das linhas
	    (SETQ selecao nil)
	    (SETQ selecao (SSGET "X" '((0 . "line") (8 . "quadricula"))))
	    (IF	selecao
		(PROGN
		    (SETQ tem nil
			  tem (TBLSEARCH "style" "RS150")
		    ) ;_  Fim do setq
		    (IF	(NOT tem)
			(COMMAND "_.style"
				 "RS150"
				 "romans"
				 (/ (* fsc 1.50) 1000)
				 "1.0"
				 ""
				 ""
				 ""
				 ""
			) ;_  Fim do COMMAND
		    ) ;_  Fim do if

		    (SETQ num  (SSLENGTH selecao)
			  cont 0
		    ) ;_  Fim do setq
		    (REPEAT num
			(SETQ entidade (ENTGET (SSNAME selecao cont))
			      cont     (+ 1 cont)
			      pt1      (CDR (ASSOC 10 entidade))
			      pt2      (CDR (ASSOC 11 entidade))
			      pt1      (TRANS pt1 0 1)
			      pt2      (TRANS pt2 0 1)
			      x1       (CAR pt1)
			      y1       (CADR pt1)
			      x2       (CAR pt2)
			      y2       (CADR pt2)
			) ;_  Fim do setq
			(IF (EQUAL x1 x2 0.001)
			    (PROGN
				(IF (> x1 200000)
				    (SETQ texto (STRCAT "   E =" (RTOS x1 2 0) "  "))
				    (SETQ texto (STRCAT "   X =" (RTOS x1 2 0) "  "))
				) ;_  Fim do IF
			    ) ;_  Fim do PROGN
			    (PROGN
				(IF (> y1 1000000)
				    (SETQ texto (STRCAT "   N =" (RTOS y1 2 0) " "))
				    (SETQ texto (STRCAT "   Y =" (RTOS y1 2 0) " "))
				) ;_  Fim do IF
			    ) ;_  Fim do PROGN
			) ;_  Fim do IF
;;;;;;;;;;;;;ver ponto x menor transformatdo
			(SETQ pt1t (TRANS pt1 1 0)
			      pt2t (TRANS pt2 1 0)
			      x1t  (CAR pt1t)
			      x2t  (CAR pt2t)
			      y1t  (CADR pt1t)
			      y2t  (CADR pt2t)
			) ;_  Fim do setq
			(IF (< x1t x2t)
			    (SETQ ponto1 pt1
				  ponto2 pt2
			    ) ;_  Fim do setq
			    (PROGN
				(IF (= x1t x2t)
				    (PROGN
					(IF (< y1t y2t)
					    (SETQ ponto1 pt1
						  ponto2 pt2
					    ) ;_  Fim do setq
					    (SETQ ponto1 pt2
						  ponto2 pt1
					    ) ;_  Fim do setq
					) ;_  Fim do if
					p
				    ) ;_  Fim do progn
				    (SETQ ponto1 pt2
					  ponto2 pt1
				    ) ;_  Fim do setq

				) ;_  Fim do if

			    ) ;_  Fim do progn
			) ;_  Fim do IF
			(COMMAND "_.text" "s" "rs150" "j" "bl" ponto1 ponto2 texto)
			(COMMAND "_.text" "s" "rs150" "j" "br" ponto2 "" texto)
		    ) ;_  Fim do REPEAT
		    (ENTDEL selec)
		) ;_  Fim do PROGN
	    ) ;_  Fim do IF

	) ;_  Fim do PROGN
	(ALERT
	    "Desenhe uma polilinha no layer QUADRICULA p/ fazer a malha"
	) ;_  Fim do alert
    ) ;_  Fim do IF
    (COMMAND "_.layer" "_on" "*" "")
    (SETVAR "osmode" oldosmode)
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUNP
 ;============================================================================
 ;============================================================================







 ;=============================================================
 ;= Rotina elaborada pelo Eng. Cartógrafo Santo Fachin        =
 ;=          Todos os direitos reservados                     =
 ;=                                                           =
 ;=           Rotaciona os textos invetidos                   =
 ;=============================================================
(DEFUN c:rott ()
    (SETQ olderro   *error*
	  *error*   errorot
	  oldosmode (GETVAR "osmode")
    ) ;_  Fim do setq
    (SF)
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (COMMAND "_.view" "_s" "atual")
    (SETVAR "ucsicon" 0)
    (SETVAR "osmode" 0)
    (PRINC
	"\n**** Rotaciona textos invertidos  \n"
    ) ;_  Fim do princ
    (INITGET 1 "Individual Todos  ")
    (SETQ var (GETKWORD
		  "        Rotação dos textos (Individual/Todos) <<Todos>> "
	      ) ;_  Fim do getkword
    ) ;_  Fim do setq
    (IF	(= var "")
	(SETQ var "Todos")
    ) ;_  Fim do if
    (SETQ sel  (SSGET "X" '((0 . "TEXT")))
	  num  (SSLENGTH sel)
	  ind  0
	  maxi (/ PI 2)
	  maxi (+ maxi 0.01)
	  mini (+ maxi PI)
	  cont 0
    ) ;_  Fim do DEFUN
    (REPEAT num
	(SETQ nome  (SSNAME sel ind)
	      lista (ENTGET nome)
	      dir   (CDR (ASSOC 50 lista))
	) ;_  Fim do SETQ
	(IF (> dir maxi)
	    (IF	(<= dir mini)
		(IF (= var "Todos")
		    (PROGN
			(SETQ cont (+ cont 1))
			(COMMAND "_.ucs" "entity" nome)
			(SETQ tb (TEXTBOX (LIST (CONS -1 nome)))
			      x1 (CAAR tb)
			      x2 (CAADR tb)
			      y1 (CADAR tb)
			      y2 (CADADR tb)
			) ;_  Fim do setq
			(SETQ x	 (/ (+ x1 x2) 2)
			      y	 (/ (+ y1 y2) 2)
			      p1 (LIST x y)
			) ;_  Fim do SETQ
			(COMMAND "_.rotate" nome "" p1 "180")
		    ) ;_  Fim do progn
		    (PROGN
			(COMMAND "_.ucs" "entity" nome)
			(SETQ tb (TEXTBOX (LIST (CONS -1 nome)))
			      x1 (CAAR tb)
			      x2 (CAADR tb)
			      y1 (CADAR tb)
			      y2 (CADADR tb)
			) ;_  Fim do setq
			(SETQ x	  (/ (+ x1 x2) 2)
			      y	  (/ (+ y1 y2) 2)
			      p1  (LIST x y)
			      dif (ABS (* (- x2 x1) 3))
			) ;_  Fim do SETQ
			(IF dif
			    (COMMAND "_.zoom" "_c" p1 dif)
			    (COMMAND "_.zoom" "_c" p1 50)
			) ;_  Fim do IF
			(rpisca nome)
			(INITGET 1 "Sim Não")
			(SETQ esc
				 (GETKWORD "\n    Rotaciona este Texto (Sim/Não)===>> ")
			) ;_  Fim do SETQ
			(IF (= esc "Sim")
			    (PROGN
				(SETQ cont (+ cont 1))
				(COMMAND "_.rotate" nome "" p1 "180")
			    ) ;_  Fim do progn
			) ;_  Fim do if
		    ) ;_  Fim do progn
		) ;_  Fim do if
	    ) ;_  Fim do if
	) ;_  Fim do if
	(SETQ ind (+ ind 1))
    ) ;_  Fim do repeat
    (COMMAND "_.UCS" "")
    (COMMAND "_.view" "_r" "atual")
    (PRINC "\n-----------------------------------------------------")
    (PRINC "\n          Foram encontrados ")
    (PRINC cont)
    (PRINC " textos invertidos")
    (PRINC "\n-----------------------------------------------------")
    (SETQ *error* olderro)
    (SETVAR "osmode" oldosmode)
    (COMMAND "_.undo" "_end")
    (GC)
    (PRINC)
) ;_  Fim do defun
 ;==================================================================================
 ;==================================================================================





;;;;;;;;====================================================================
;;;;;;;;---------Muda largura polilinha, linha e arcos                      
;;;;;;;;====================================================================
(DEFUN c:EXPl ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (COMMAND "_.undo" "_group")
    (SETVAR "cmdecho" 0)
    (sf)
    (PRINC "\n Transforma linhas em polilinhas e da uma expessura a elas")
    (SETQ sSel
	     (SSGET '((-4 . "<OR")
		      (0 . "LINE")
		      (0 . "LWPOLYLINE")
		      (0 . "MTC")
		      (-4 . "OR>")
		     )
	     ) ;_  Fim do ssget
    ) ;_  Fim do setq
    (IF	(/= sSel nil)
	(PROGN
	    (IF	(NOT expAnt)
		(SETQ expAnt 0.0)
	    ) ;_  Fim do if
	    (SETQ expNova (GETREAL (STRCAT "\nEntre expessura das linhas <"
					   (RTOS expAnt 2 2)
					   ">: "
				   ) ;_  Fim do strcat
			  ) ;_  Fim do getreal
	    ) ;_  Fim do setq
	    (IF	expNova
		(SETQ expAnt ExpNova)
	    ) ;_  Fim do if
	    (SETQ cont 0)
	    (WHILE (< cont (SSLENGTH sSel))
		(SETQ Elemento (SSNAME sSel cont))
		(IF (= "LWPOLYLINE" (CDR (ASSOC 0 (ENTGET elemento))))
		    (COMMAND "_.pedit" elemento "_w" expAnt "")
		    (COMMAND "_.pedit" elemento "_y" "_w" expAnt "")
		) ;_  Fim do if
		(SETQ cont (+ cont 1))
	    ) ;_  Fim do while
	) ;_  Fim do progn
    ) ;_  Fim do if
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
;;;;===================================================================
;;;;===================================================================


 ;======================================================================
 ;=                                                                    =
 ;=            Rotina pisca texto selecionado                           =
 ;=                                                                    =
 ;======================================================================
(DEFUN rpisca (nome)
    (REPEAT 200
	(ENTDEL nome)
	(REPEAT	200
	    (PROMPT "")
	) ;_  Fim do repeat
    ) ;_  Fim do repeat
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================




 ;======================================================================
 ;----------Rotina para verificar se escala esta definida               
 ;======================================================================
(DEFUN verescala ()
    (SETQ fsc (GETVAR "userr1"))
    (IF	(= fsc 0)
	(mscl)
    ) ;_  Fim do if
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================







 ;======================================================================
;;;;;;;Rotina para transformar uma lista de pontos de um ucs para outro
 ;======================================================================
(DEFUN translista ()
    (SETQ num	 (LENGTH lista)
	  nlista nil
	  cont	 0
    ) ;_  Fim do setq
    (REPEAT num
	(SETQ pt     (NTH cont lista)
	      cont   (+ 1 cont)
	      ptt    (TRANS pt de para)
	      nlista (APPEND nlista (LIST ptt))
	) ;_  Fim do setq
    ) ;_  Fim do repeat
) ;_  Fim do DEFUN
 ;======================================================================
 ;======================================================================




 ;======================================================================
 ;--------rotina para extrair dados de uma selecao simples              
 ;======================================================================
(DEFUN extrai (selecao)
    (SETQ entidade  (CAR selecao)
	  elementos (ENTGET entidade)
	  ptins	    (CADR selecao)
	  nome	    (CDR (ASSOC '0 elementos))
    ) ;_  Fim do SETQ
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================


 ;======================================================================
 ;=                                                                    =
 ;=            Rotina para extrair lista de pontos de uma seleção      =
 ;=                                                                    =
 ;======================================================================
(DEFUN extraipontos (selecao /)
    (SETQ vertlw   nil
	  angs	   nil
	  elemento (ENTGET selecao)
	  nome	   (CDR (ASSOC '0 (ENTGET selecao)))
    ) ;_  Fim do SETQ
    (IF	(= "ARC" nome)
	(PROGN
	    (SETQ pt1arc (CDR (ASSOC '10 elemento))
		  Raio	 (CDR (ASSOC '40 elemento))
		  ang1	 (CDR (ASSOC '50 elemento))
		  ang2	 (CDR (ASSOC '51 elemento))
	    ) ;_  Fim do SETQ
	    (IF	(< ang1 ang2)
		(SETQ angR (- ang2 ang1)
		      angR (+ (* 2 PI) (- ang2 ang1))
		) ;_  Fim do SETQ
	    ) ;_  Fim do IF
	    (SETQ desenv (* angR raio)
		  pt2arc (POLAR pt1arc ang1 raio)
		  pt3arc (POLAR pt1arc ang2 raio)
	    ) ;_  Fim do SETQ
	) ;_  Fim do progn
    ) ;_  Fim do if
    (IF	(= "LINE" nome)
	(PROGN
	    (SETQ ptl1 (CDR (ASSOC '10 elemento))
		  ptl2 (CDR (ASSOC '11 elemento))
	    ) ;_  Fim do SETQ
	) ;_  Fim do progn
    ) ;_  Fim do if
    (IF	(= "LWPOLYLINE" nome)
	(PROGN
	    (SETQ nvert	(CDR (ASSOC '90 elemento))
		  ptlw	(CDR (ASSOC '10 elemento))
	    ) ;_  Fim do SETQ
	    (IF	(= (LENGTH ptlw) 2)
		(SETQ ptlw (LIST (NTH 0 ptlw) (NTH 1 ptlw) 0.00))
	    ) ;_  Fim do if
	    (SETQ fim	ptlw
		  fecha	(CDR (ASSOC '70 elemento))
		  n	-1
		  ik	1
	    ) ;_  Fim do SETQ
	    (WHILE ik
		(SETQ n	 (+ 1 n)
		      qq (CAR (NTH n elemento))
		) ;_  Fim do SETQ
		(IF (= qq 10)
		    (SETQ ik nil)
		) ;_  Fim do if
	    ) ;_  Fim do while
	    (SETQ cont 1)
	    (WHILE (<= cont nvert)
		(SETQ ptlw (CDR (NTH n elemento))
		      ang1 (CDR (NTH (+ n 3) elemento))
		) ;_  Fim do SETQ
		(IF (= (LENGTH ptlw) 2)
		    (SETQ ptlw (LIST (NTH 0 ptlw) (NTH 1 ptlw) 0.00))
		) ;_  Fim do if
		(SETQ vertlw (APPEND vertlw (LIST ptlw))
		      angs   (APPEND angs (LIST ang1))
		      n	     (+ 4 n)
		      cont   (+ 1 cont)
		) ;_  Fim do SETQ
	    ) ;_  Fim do while
	    (IF	(= fecha 1)
		(PROGN
		    (SETQ vertlw (APPEND vertlw (LIST fim))
			  angs	 (APPEND angs (LIST 0.0))
		    ) ;_  Fim do SETQ
		) ;_  Fim do progn
	    ) ;_  Fim do if
	) ;_  Fim do progn
    ) ;_  Fim do if
    (IF	(= "POLYLINE" nome)
	(PROGN
	    (SETQ fecha	 (CDR (ASSOC '70 (ENTGET selecao)))
		  ptlw	 (ENTNEXT selecao)
		  vt2	 (ENTGET ptlw)
		  vt3	 (CDR (ASSOC '10 vt2))
		  ang1	 (CDR (ASSOC '42 vt2))
		  fim	 vt3
		  vertlw (APPEND vertlw (LIST vt3))
		  angs	 (APPEND angs (LIST ang1))
		  simbt	 1
	    ) ;_  Fim do PROGN
	    (WHILE simbt
		(SETQ ptlw (ENTNEXT ptlw)
		      vt2  (ENTGET ptlw)
		      tt   (CDR (ASSOC '0 vt2))
		      vt3  (CDR (ASSOC '10 vt2))
		      ang1 (CDR (ASSOC '42 vt2))
		) ;_  Fim do SETQ
		(IF (= 'LIST (TYPE vt3))
		    (PROGN
			(SETQ vertlw (APPEND vertlw (LIST vt3))
			      angs   (APPEND angs (LIST ang1))
			) ;_  Fim do SETQ
		    ) ;_  Fim do progn
		) ;_  Fim do if
		(IF (= tt "SEQEND")
		    (SETQ simbt nil)
		) ;_  Fim do if
	    ) ;_  Fim do while
	    (IF	(= fecha 1)
		(PROGN
		    (SETQ vertlw (APPEND vertlw (LIST fim))
			  angs	 (APPEND angs (LIST 0.0))
		    ) ;_  Fim do SETQ
		) ;_  Fim do PROGN
	    ) ;_  Fim do PROGN
	) ;_  Fim do IF
    ) ;_  Fim do IF
    (IF	(= "SPLINE" nome)
	(PROGN
	    (SETQ ptlw (LENGTH elemento)
		  vt0  0
	    ) ;_  Fim do SETQ
	    (REPEAT ptlw
		(IF (= 11 (CAR (NTH vt0 elemento)))
		    (PROGN
			(SETQ vt3    (CDR (NTH vt0 elemento))
			      vertlw (APPEND vertlw (LIST vt3))
			      angs   (APPEND angs (LIST 0.0))
			) ;_  Fim do SETQ
		    ) ;_  Fim do progn
		) ;_  Fim do if
		(SETQ vt0 (+ 1 vt0))
	    ) ;_  Fim do repeat
	    (IF	(= vertlw nil)
		(PROGN
		    (SETQ ptlw (LENGTH elemento)
			  vt0  0
		    ) ;_  Fim do SETQ
		    (REPEAT ptlw
			(IF (= 10 (CAR (NTH vt0 elemento)))
			    (PROGN
				(SETQ vt3    (CDR (NTH vt0 elemento))
				      vertlw (APPEND vertlw (LIST vt3))
				      angs   (APPEND angs (LIST 0.0))
				) ;_  Fim do SETQ
			    ) ;_  Fim do progn
			) ;_  Fim do if
			(SETQ vt0 (+ 1 vt0))
		    ) ;_  Fim do repeat
		) ;_  Fim do progn
	    ) ;_  Fim do if
	) ;_  Fim do progn
    ) ;_  Fim do if
    (SETQ vert2 vert)
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================




 ;=============================================================
 ;=                                                           =
 ;=            Funcao para entrada de strings                 =
 ;=                                                           =
 ;=============================================================
(DEFUN strget (prompt_str default / inp)
    (PRINC prompt_str)
    (COND
	((= nil default)
	 (PRINC " :")
	)
	(T (PRINC (STRCAT " <" default "> :")))
    ) ;_  Fim do cond
    (SETQ inp (GETSTRING))
    (COND
	((= "" inp) ; enter pressed
	 (COND
	     ((= nil default) inp)
	     (T default)
	 ) ;_  Fim do cond
	)
	(T inp)
    ) ;_  Fim do cond
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================




 ;=============================================================
 ;=                                                           =
 ;=            Retorna a hora separada com espaco             =
 ;=                                                           =
 ;=============================================================
(DEFUN shora (/ t-spec temp)
    (SETQ t-spec (GETVAR "CDATE"))
    (LIST
	(FIX (SETQ temp (* 100 (- t-spec (FIX t-spec)))))
	(FIX (SETQ temp (* 100 (- temp (FIX temp)))))
	(FIX (SETQ temp (* 100 (- temp (FIX temp)))))
    ) ;_  Fim do list
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================




 ;=============================================================
 ;=                                                           =
 ;=            Retorna a hora separada com :                  =
 ;=                                                           =
 ;=============================================================

(DEFUN hora (/ t-spec hrs mins secs temp)
    (SETQ t-spec (GETVAR "CDATE"))
    (SETQ
	hrs  (ITOA (FIX (SETQ temp (* 100 (- t-spec (FIX t-spec))))))
	mins (ITOA (FIX (SETQ temp (* 100 (- temp (FIX temp))))))
	secs (ITOA (FIX (SETQ temp (* 100 (- temp (FIX temp))))))
    ) ;_  Fim do setq
    (IF	(< (STRLEN mins) 2)
	(SETQ mins (STRCAT "0" mins))
    ) ;_  Fim do if
    (IF	(< (STRLEN secs) 2)
	(SETQ secs (STRCAT "0" secs))
    ) ;_  Fim do if
    (STRCAT hrs ":" mins ":" secs)
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================




 ;=============================================================
 ;=                                                           =
 ;=            Retorna a data no formado dia mes e ano        =
 ;=                                                           =
 ;=============================================================
(DEFUN data (/ t-spec temp meses mo dia yr)
    (SETQ meses
	     '("Janeiro" "Fevereiro" "Março" "Abril" "Maio" "Junho" "Julho" "Agosto" "Setembro"
	       "Outubro" "Novembro" "Dezembro"
	      )
    ) ;_  Fim do setq
    (SETQ t-spec (GETVAR "CDATE"))
    (SETQ t-spec (/ t-spec 10000))
    (SETQ
	YR  (SUBSTR (ITOA (FIX t-spec)) 3)
	MO  (NTH (1- (FIX (SETQ temp (* 100 (- t-spec (FIX t-spec))))))
		 meses
	    ) ;_  Fim do nth
	dia (ITOA (FIX (* 100 (- temp (FIX temp)))))
    ) ;_  Fim do setq
 ; append leading zeros if required
    (IF	(< (STRLEN MO) 2)
	(SETQ MO (STRCAT "0" MO))
    ) ;_  Fim do if
    (IF	(< (STRLEN dia) 2)
	(SETQ dia (STRCAT "0" dia))
    ) ;
    (IF	(< (STRLEN YR) 2)
	(SETQ YR (STRCAT "0" YR))
    ) ; good past year 2000

    (STRCAT " " dia " de " mo " de 20" yr)
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================

 ;=============================================================
 ;=                                                           =
 ;=           Apaga todas as entidades do layer selecinado    =
 ;=                                                           =
 ;=============================================================
(DEFUN c:Dlay ()
    (SETQ olderro *error*
	  *error* erroRot
    ) ;_  Fim do setq
    (sf)
    (SETVAR "CMDECHO" 0)
    (COMMAND "._UNDO" "_group")
    (PRINC "\n Apaga todas as entidades do layer do objeto selecionado")
    (SETQ sel (ENTSEL "\n Selecione um objeto pertencente a Layer"))
    (IF	sel
	(PROGN
	    (SETQ lay (CDR (ASSOC 8 (ENTGET (CAR sel)))))
	    (SETQ ss (SSGET "X" (LIST (CONS 8 lay))))
	    (COMMAND "_.erase" ss "")
	    (SETQ txt (STRCAT "\n    Todos os objetos do layer "
			      lay
			      " foram apagados!!!!"
		      ) ;_  Fim do STRCAT
	    ) ;_  Fim do SETQ
	    (PRINC "\n-------------------------------------------------------------")
	    (PRINC txt)
	    (PRINC "\n-------------------------------------------------------------")
	) ;_  Fim do progn
	(PRINC "\n ****** Nenhuma entidade foi selecionada  *******")
    ) ;_  Fim do if
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;=============================================================
 ;=============================================================




 ;=============================================================
 ;=                                                           =
 ;=          Rotina de erro para linhas e textos              =
 ;=                                                           =
 ;=============================================================
(DEFUN errotxt (msg)
    (IF	(= msg "quit / exit abort")
	(PRINC)
	(PRINC (STRCAT "error: " msg))
    ) ;_  Fim do if
    (SETQ *error* olderro)
    (COMMAND "._UNDO" "_End")
    (SETVAR "osmode" oldosmode)
    (SETVAR "angdir" oldangdir)
    (SETVAR "angbase" oldangbase)
    (SETVAR "cecolor" oldcolor)
    (SETVAR "textstyle" oldestilo)
    (COMMAND "_.ucs" "_p")
    (PRINC)
) ;_  Fim do defun
 ;=============================================================
 ;=============================================================







 ;=============================================================
 ;=                                                           =
 ;=          Insere um texto dentro de um balão               =
 ;=                                                           =
 ;=============================================================
(DEFUN C:Bal ()
    (SETQ olderro *error*
	  *error* errotxt
    ) ;_  Fim do SETQ
    (COMMAND "._UNDO" "_Group")
    (SETQ oldangdir  (GETVAR "angdir")
	  oldangbase (GETVAR "angbase")
	  oldosmode  (GETVAR "osmode")
	  olducs     (GETVAR "ucsname")
	  oldcolor   (GETVAR "cecolor")
	  oldestilo  (GETVAR "textstyle")
    ) ;_  Fim do setq
    (sf)
    (SETVAR "CMDECHO" 0)
    (SETVAR "angdir" 0)
    (SETVAR "angbase" 0)
    (COMMAND "_.ucs" "")
    (PRINC "\n Insere um texto dentro de um balão(circulo)")
    (GRAPHSCR)
    (SETQ p1	   (GETPOINT "\nClique no primeiro ponto ")
	  ptc	   (GETPOINT p1 "\nClique no cento do balão: ")
	  TXT	   (GETSTRING 1 "\nDigite o texto para inserir no Balão: ")
	  Estilo   (GETVAR "textstyle")
	  Entidade (TBLSEARCH "style" Estilo)
	  altTexto (CDR (ASSOC '40 Entidade))
    ) ;_  Fim do SETQ
    (SETVAR "osmode" 0)
    (IF	(= ptc nil)
	(SETQ ptc p1)
    ) ;_  Fim do if
    (IF	(= txt "")
	(SETQ txt "0")
    ) ;_  Fim do if
    (IF	(= altTexto 0)
	(COMMAND "_.TEXT" "_j" "_mc" ptc "" "0" Txt)
	(COMMAND "_.TEXT" "_j" "_mc" ptc "0" Txt)
    ) ;_  Fim do IF
    (IF	(= 0 altTexto)
	(SETQ tam (GETVAR "TEXTSIZE"))
	(SETQ tam altTexto)
    ) ;_  Fim do if
    (SETQ ent  (ENTLAST)
	  ptt  (TEXTBOX (ENTGET ent))
	  x1   (CAR (CAR ptt))
	  x2   (CAAR (CDR ptt))
	  diam (+ tam (- x2 x1))
	  diam (* 0.5 diam)
	  dist (DISTANCE p1 ptc)
	  ANG  (ANGLE p1 ptc)
	  p2   (POLAR p1 ANG (- DIST diam))
    ) ;_  Fim do setq
    (SETQ tamtexto (+ 0.5 (STRLEN txt))
    ) ;_  Fim do SETQ
    (IF	(> dist diam)
	(COMMAND "_.LINE" p1 p2 "")
    ) ;_  Fim do if
    (COMMAND "_.CIRCLE" ptc diam)
    (SETQ *error* olderro)
    (SETVAR "osmode" oldosmode)
    (SETVAR "angdir" oldangdir)
    (SETVAR "angbase" oldangbase)
    (COMMAND "_.ucs" "_p")
    (COMMAND "._UNDO" "_End")
    (PRINC)
) ;_  Fim do DEFUN
 ;=============================================================
 ;=============================================================






 ;=============================================================
 ;=                                                           =
 ;=           Purga desenho                                   =
 ;=                                                           =
 ;=============================================================
(DEFUN c:Pur ()
    (sf)
    (SETQ olderro *error*
	  *error* erroRot
    ) ;_  Fim do setq
    (SETVAR "CMDECHO" 0)
    (COMMAND "._UNDO" "_group")
    (PRINC "\n Purga o desenho de blocos e outros não utilizados")
    (REPEAT 3
	(COMMAND "_.PURGE" "_ALL" "*" "N")
	(COMMAND "_.PURGE" "_B" "*" "N")
	(COMMAND "_.PURGE" "_D" "*" "N")
	(COMMAND "_.PURGE" "_LA" "*" "N")
	(COMMAND "_.PURGE" "_LT" "*" "N")
	(COMMAND "_.PURGE" "_SH" "*" "N")
	(COMMAND "_.PURGE" "_M" "*" "N")
	(COMMAND "_.PURGE" "_ST" "*" "N")
    ) ;_  Fim do repeat
    (PRINC "\n-------------------------------------------------------------")
    (PRINC "\n       Purgue completo!!!!!")
    (PRINC "\n-------------------------------------------------------------")
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;=============================================================
 ;=============================================================






 ;=============================================================
 ;=                                                           =
 ;=           Zera a coordenada Z de algumas entidades        =
 ;=                                                           =
 ;=============================================================

(DEFUN C:Zera (/ olderr	oldcmd zeroz ss1 ss1len	i numchg numnot	numno0 ssno0 ename elist etype yorn
	       vrt crz
	      )
    (SETQ olderr *error*)
    (DEFUN *error* (msg)
	(IF (= msg "quit / exit abort")
	    (PRINC)
	    (PRINC (STRCAT "error: " msg))
	) ;_  Fim do if
	(SETQ *error* olderr)
	(COMMAND "._UNDO" "_End")
	(PRINC)
    ) ;_  Fim do defun
    (DEFUN zeroz (key zelist / oplist nplist)
	(SETQ oplist (ASSOC key zelist)
	      nplist (REVERSE (APPEND '(0.0) (CDR (REVERSE oplist))))
	      zelist (SUBST nplist oplist zelist)
	) ;_  Fim do setq
	(ENTMOD zelist)
    ) ;_  Fim do defun
    (SETQ oldcmd (GETVAR "CMDECHO"))
    (SETVAR "CMDECHO" 0)
    (COMMAND "._UNDO" "_Group")
    (sf)
    (PRINC "\n  Torna todas as entidades selecinadas com Coordenada Z=0")
    (GRAPHSCR)
    (SETQ ss1 (SSGET))
    (IF	(= nil ss1)
	(SETQ ss1 (SSGET "X"))
    ) ;_  Fim do if
    (SETQ ss1len (SSLENGTH ss1)
	  i	 0
	  numchg 0
	  numnot 0
	  numno0 0
	  ssno0	 (SSADD)
    ) ;_  Fim do setq
    (REPEAT ss1len
	(SETQ ename (SSNAME ss1 i)
	      elist (ENTGET ename)
	      etype (CDR (ASSOC 0 elist))
	) ;_  Fim do setq
	(IF (NOT (MEMBER etype
			 '("3DFACE" "ARC" "ATTDEF" "CIRCLE" "DIMENSION"	"ELLIPSE" "HATCH" "INSERT"
			   "LINE" "LWPOLYLINE" "MTEXT" "POINT" "POLYLINE" "SOLID" "TEXT"
			  )
		 ) ;_  Fim do MEMBER
	    ) ;_  Fim do NOT
	    (PROGN
		(SETQ numnot (1+ numnot))
		(IF (/= 0.0 (CAR (REVERSE (ASSOC 10 elist))))
		    (PROGN ;add it to special list if Z /= 0
			(SETQ numno0 (1+ numno0))
			(SSADD ename ssno0)
		    ) ;_  Fim do progn
		) ;_  Fim do if
	    ) ;_  Fim do progn
	) ;_  Fim do if
	(IF (MEMBER etype
		    '("3DFACE" "ARC" "ATTDEF" "CIRCLE" "DIMENSION" "ELLIPSE" "HATCH" "INSERT" "LINE"
		      "MTEXT" "POINT" "POLYLINE" "SOLID" "TEXT"
		     )
	    ) ;_  Fim do MEMBER
	    (SETQ elist	 (zeroz 10 elist)
		  numchg (1+ numchg)
	    ) ;_  Fim do setq
	) ;_  Fim do if
	(IF (MEMBER etype
		    '("3DFACE" "ATTDEF" "DIMENSION" "LINE" "TEXT" "SOLID")
	    ) ;_  Fim do MEMBER
	    (SETQ elist (zeroz 11 elist))
	) ;_  Fim do if
	(IF (MEMBER etype '("3DFACE" "SOLID"))
	    (PROGN
		(SETQ elist (zeroz 12 elist))
		(SETQ elist (zeroz 13 elist))
	    ) ;_  Fim do progn
	) ;_  Fim do if
	(IF (MEMBER etype '("DIMENSION"))
	    (PROGN
		(SETQ elist (zeroz 13 elist))
		(SETQ elist (zeroz 14 elist))
		(SETQ elist (zeroz 15 elist))
		(SETQ elist (zeroz 16 elist))
	    ) ;_  Fim do progn
	) ;_  Fim do if
	(IF (= etype "POLYLINE")
	    (PROGN
		(SETQ vrt ename)
		(WHILE (NOT (EQUAL (CDR (ASSOC 0 (ENTGET vrt))) "SEQEND"))
		    (SETQ elist (ENTGET (ENTNEXT vrt)))
		    (SETQ crz (CADDDR (ASSOC 10 elist)))
		    (IF	(/= crz 0)
			(PROGN
			    (zeroz 10 elist)
			    (ENTUPD ename)
			) ;_  Fim do progn
		    ) ;_  Fim do if
		    (SETQ vrt (CDR (ASSOC -1 elist)))
		) ;_  Fim do while
	    ) ;_  Fim do progn
	) ;_  Fim do if
	(IF (MEMBER etype '("LWPOLYLINE"))
	    (PROGN
		(SETQ elist  (SUBST (CONS 38 0.0) (ASSOC 38 elist) elist)
		      numchg (1+ numchg)
		) ;_  Fim do setq
		(ENTMOD elist)
	    ) ;_  Fim do progn
	) ;_  Fim do if
	(SETQ i (1+ i)) ;next entity
    ) ;_  Fim do repeat
    (PROMPT (STRCAT "\n" (ITOA numchg) " objetos zerados."))
    (PROMPT
	(STRCAT "\n" (ITOA numnot) " objetos não zerados.")
    ) ;_  Fim do prompt
    (COMMAND "._UNDO" "_End")
    (SETVAR "CMDECHO" oldcmd)
    (SETQ *error* olderr)
    (PRINC)
) ;_  Fim do DEFUN
 ;=============================================================
 ;=============================================================



 ;=============================================================
 ;=                                                           =
 ;=           Soma um vertice a linha ou polilinha            =
 ;=                                                           =
 ;=============================================================
(DEFUN C:SVERT ()
    (DEFUN errosvert (msg)
	(SETQ *error* olderro)
	(IF (= fec 1)
	    (PROGN
		(SETQ Entidade (ENTLAST)
		      dxfEnt   (ENTGET Entidade)
		) ;_  Fim do setq
		(SETQ dxfEnt (SUBST (CONS 70 1) (ASSOC 70 dxfEnt) dxfEnt))
		(ENTMOD dxfEnt)
		(ENTUPD Entidade)
	    ) ;_  Fim do progn
	) ;_  Fim do if
	(SETVAR "osmode" oldosm)
	(COMMAND "_.undo" "_end")
	(PRINC)
    ) ;_  Fim do defun
    (DEFUN insertvert ()
	(COMMAND "_.break" Entidade pickpoint pickpoint)
	(SETQ ultimo (ENTLAST)
	      contv  (+ 1 contv)
	) ;_  Fim do SETQ
	(IF (= conv "S")
	    (COMMAND "_.PEDIT" ultimo "_join" ultimo Entidade "" "x")
	) ;_  Fim do if
	(SETVAR "osmode" OLDOSM)
	(PRINC "\nMove p/ novo vertice: ")
	(SETQ NEWVERT (OSNAP pickpoint "int,near"))
	(COMMAND "_.STRETCH" "_C" NEWVERT NEWVERT "" pickpoint pause)
    ) ;_  Fim do defun
    (sf)
    (SETQ olderro *error*
	  *error* errosvert
    ) ;_  Fim do setq
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (SETQ oldosm (GETVAR "osmode"))
    (PRINC "\n*** Soma Vertice a linha ou polilinha ***")
    (SETVAR "osmode" 512)
    (SETQ contv 0)
    (WHILE (SETQ Selecao (ENTSEL "\n  Selecione uma linha ou polilinha."))
	(SETVAR "osmode" 512)
	(SETQ Entidade	(CAR Selecao)
	      pickpoint	(OSNAP (CADR Selecao) "nearest")
	      dxfEnt	(ENTGET Entidade)
	      Nome	(CDR (ASSOC 0 dxfEnt))
	) ;_  Fim do setq
	(IF (= Nome "LWPOLYLINE")
	    (PROGN
		(SETQ conv "S"
		      fec  (CDR (ASSOC 70 dxfEnt))
		) ;_  Fim do SETQ
		(IF (= fec 1)
		    (PROGN
			(SETQ dxfEnt (SUBST (CONS 70 0) (ASSOC 70 dxfEnt) dxfEnt))
			(ENTMOD dxfEnt)
			(ENTUPD Entidade)
		    ) ;_  Fim do progn
		) ;_  Fim do if
		(insertvert)
		(IF (= fec 1)
		    (PROGN
			(SETQ Entidade (ENTLAST)
			      dxfEnt   (ENTGET Entidade)
			) ;_  Fim do setq
			(SETQ dxfEnt (SUBST (CONS 70 1) (ASSOC 70 dxfEnt) dxfEnt))
			(ENTMOD dxfEnt)
			(ENTUPD Entidade)
		    ) ;_  Fim do progn
		) ;_  Fim do if
	    ) ;_  Fim do progn
	    (IF	(= Nome "LINE")
		(PROGN
		    (INITGET 1 "S N  ")
		    (SETQ
			conv (GETKWORD "\nTransforma linha em polilinha (S/N) <N> ")
		    ) ;_  Fim do setq
		    (IF	(= conv "s")
			(SETQ conv "S")
		    ) ;_  Fim do if
		    (IF	(= conv "")
			(SETQ conv "N")
		    ) ;_  Fim do if
		    (IF	(= conv "S")
			(PROGN
			    (COMMAND "_.pedit" Entidade "_y" "")
			    (SETQ Entidade (ENTLAST))
			    (insertvert)
			) ;progn
			(insertvert)
		    ) ;if
		) ;progn
		(PRINC "\n    A seleção não é uma Linha ou Polilinha válida.")
	    ) ;if
	) ;if
    ) ;while
    (SETQ
	txt (STRCAT "\n    Foram somados " (ITOA contv) " novos vértices a polilinha")
    ) ;_  Fim do SETQ
    (PRINC "\n--------------------------------------------------------------------")
    (PRINC txt)
    (PRINC "\n--------------------------------------------------------------------")
    (SETQ *error* olderro)
    (SETVAR "osmode" oldosm)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;=============================================================================
 ;=============================================================================




 ;======================================================================
 ;=                                                                    =
 ;=  Rotina que Coloca os Sólidos e Hachuras no fundo do desenho       =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:dwb ()
    (sf)
    (PRINC "\n Coloca Sólidos e Hachuras no fundo do desenho")
    (SETQ Sel (SSGET "X"
		     (LIST (CONS -4 "<OR")
			   (CONS 0 "SOLID")
			   (CONS 0 "HATCH")
			   (CONS -4 "OR>")
		     ) ;_  Fim do LIST
	      ) ;_  Fim do list
    ) ;_  Fim do setq
    (IF	sel
	(PROGN
	    (COMMAND "_.draworder" Sel "" "_b")
	    (PRINC "\n-----------------------------------------------------")
	    (PRINC "\n Hachuras e sólidos do desenho colocados no fundo")
	    (PRINC "\n-----------------------------------------------------")
	) ;_  Fim do PRINC
	(PRINC "\n   Nenhuma entidade encontrada no desenho!!!!!")
    ) ;_  Fim do progn
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================





 ;======================================================================
 ;=                                                                    =
 ;=  Rotina para ver códigos dxf de entidades                          =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:dxf ()
    (sf)
    (SETQ ent
	     (ENTGET (CAR (ENTSEL "\n  Selecione a entidade para ver os códigos DXF: ")))
    ) ;_  Fim do SETQ
    (PRINC
	"\n------------------------------------------------------------------------\n"
    ) ;_  Fim do princ
    (PRINC ent)
    (PRINC
	"\n------------------------------------------------------------------------"
    ) ;_  Fim do princ
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================





 ;======================================================================
 ;=                                                                    =
 ;=  Rotina para verificar se o texto é um numero válido               =
 ;=                                                                    =
 ;======================================================================
(DEFUN verificaTxt (txt)
    (SETQ cont1	1
	  str	""
	  tam	(STRLEN txt)
	  num	nil
    ) ;_  Fim do setq
    (REPEAT tam
	(SETQ str (SUBSTR txt cont1 1))
	(IF (OR (= str ",") (= str "."))
	    (SETQ num (APPEND num (LIST cont1)))
	) ;_  Fim do if
	(SETQ cont1 (1+ cont1))
    ) ;_  Fim do repeat
    (SETQ sep	   (LENGTH num)
	  numtexto ""
    ) ;_  Fim do SETQ
    (IF	(= 0 sep)
	(SETQ numtexto txt)
    ) ;_  Fim do if
    (IF	(= 1 sep)
	(PROGN
	    (SETQ ind	   (NTH 0 num)
		  tam1	   (FIX (- ind 1))
		  tam2	   (FIX (- tam ind))
		  inicio2  (FIX (+ ind 1))
		  numTexto (STRCAT (SUBSTR txt 1 tam1) "." (SUBSTR txt inicio2 tam2))
	    ) ;_  Fim do setq
	) ;_  Fim do PROGN
    ) ;_  Fim do if
    (IF	(= 2 sep)
	(PROGN
	    (SETQ ind1	   (NTH 0 num)
		  ind2	   (NTH 1 num)
		  tam1	   (FIX (- ind1 1))
		  tam2	   (FIX (- tam ind2))
		  inicio2  (FIX (+ ind1 1))
		  inicio3  (FIX (+ ind2 1))
		  numTexto (STRCAT (SUBSTR txt 1 tam1)
				   (SUBSTR txt inicio2 3)
				   "."
				   (SUBSTR txt inicio3 tam2)
			   ) ;_  Fim do STRCAT
	    ) ;_  Fim do setq
	) ;_  Fim do PROGN
    ) ;_  Fim do if
    (IF	(= 3 sep)
	(PROGN
	    (SETQ ind1	   (NTH 0 num)
		  ind2	   (NTH 1 num)
		  ind3	   (NTH	2z
				e
				num
			   ) ;_  Fim do nth
		  tam1	   (FIX (- ind1 1))
		  tam2	   (FIX (- tam ind3))
		  inicio2  (FIX (+ ind1 1))
		  inicio3  (FIX (+ ind2 1))
		  inicio4  (FIX (+ ind3 1))
		  numTexto (STRCAT (SUBSTR txt 1 tam1)
				   (SUBSTR txt inicio2 3)
				   (SUBSTR txt inicio3 3)
				   "."
				   (SUBSTR txt inicio4 tam2)
			   ) ;_  Fim do STRCAT
	    ) ;_  Fim do setq
	) ;_  Fim do PROGN
    ) ;_  Fim do if
    (SETQ soma1 (ATOF numTexto))
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================





 ;======================================================================
 ;=                                                                    =
 ;=  Rotina para somar uma seleção de textos numéricos                 =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:soma ()
    (SETVAR "cmdecho" 0)
    (sf)
    (PRINC
	"\n    **** Soma textos numéricos    ****\n"
    ) ;_  Fim do princ
    (SETQ textos (SSGET (LIST (CONS 0 "TEXT")))
	  cont	 0
	  soma	 0
    ) ;_  Fim do setq
    (IF	(/= textos nil)
	(PROGN
	    (REPEAT (SSLENGTH textos)
		(SETQ txt (CDR (ASSOC 1 (ENTGET (SSNAME textos cont)))))
		(verificaTxt txt)
		(SETQ soma (+ soma soma1)
		      cont (1+ cont)
		) ;_  Fim do setq
	    ) ;_  Fim do repeat
	    (PRINC "\n-----------------------------------------------------------")
	    (SETQ txt (STRCAT "\n   A soma dos textos é: " (RTOS soma 2 5)))
	    (PRINC txt)
	) ;_  Fim do progn
	(PRINC "\n   Nenhum texto foi selecionado!!!!!!!!!!!!")
    ) ;_  Fim do if
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================




 ;======================================================================
 ;=                                                                    =
 ;=  Rotina para eliminar vertice  duplicados de todas as polilinhas   =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:evdp ()
    (SETQ olderro *error*
	  *error* erropl
    ) ;_  Fim do setq
    (COMMAND "_.undo" "_group")
    (SETVAR "cmdecho" 0)
    (COMMAND "_.ucs" "")
    (sf)
    (PRINC
	"\n    **** Elimina vértice duplicados  de todas polilinha 2d    ****\n"
    ) ;_  Fim do princ
    (SETQ selec	nil
	  selec	(SSGET "X" '((0 . "LWPOLYLINE")))
	  contS	0
	  dupl	0
	  num	0
	  fec	300
    ) ;_  Fim do SETQ
    (REPEAT (SSLENGTH selec)
	(SETQ selecao nil
	      selecao (SSNAME selec contS)
	      ent     nil
	      ent     (ENTGET selecao)
	      num     (LENGTH ent)
	      sel     nil
	) ;_  Fim do SETQ
 ;_  Fim do SETQ
	(REDRAW selecao 3)
	(extraipontos selecao)
	(SETQ fec  (CDR (ASSOC 70 ent))
	      cont 0
	) ;_  Fim do SETQ
	(WHILE (/= (CAR (NTH cont ent)) 10)
	    (SETQ sel  (APPEND sel (LIST (NTH cont ent)))
		  cont (+ 1 cont)
	    ) ;_  Fim do SETQ
	) ;_  Fim do WHILE
	(SETQ vert    (/ (- num cont) 4)
	      cont1   0
	      p1      nil
	      p2      nil
	      dist    0
	      numvert 0
	) ;_  Fim do SETQ
 ;verificar numero do vertice que será o primeiro
	(SETQ numVert (LENGTH vertlw)
	) ;_  Fim do SETQ
	(IF (= fec 1)
	    (SETQ numVert (- numVert 1))
	) ;_  Fim do if
	(SETQ cont1 cont
	      p1 (LIST 0 0 0)
	      contvert 0
	) ;_  Fim do SETQ
	(REPEAT	numVert
	    (SETQ p2   (NTH contvert vertlw)
		  dist (DISTANCE p1 p2)
	    ) ;_  Fim do setq
	    (IF	(> dist 0)
		(PROGN
		    (SETQ sel (APPEND sel (LIST (NTH cont1 ent)))
			  sel (APPEND sel (LIST (NTH (+ 1 cont1) ent)))
			  sel (APPEND sel (LIST (NTH (+ 2 cont1) ent)))
			  sel (APPEND sel (LIST (NTH (+ 3 cont1) ent)))
		    ) ;_  Fim do SETQ
		) ;_  Fim do progn
		(SETQ dupl (+ 1 dupl))
	    ) ;_  Fim do IF
	    (SETQ cont1	   (+ 4 cont1)
		  contvert (+ contvert 1)
		  p1	   p2
	    ) ;_  Fim do SETQ
	) ;_  Fim do REPEAT
	(SETQ sel (APPEND sel (LIST (NTH (- num 1) ent))))
	(ENTMOD sel)
	(ENTUPD selecao)
	(COMMAND "_.redraw")
	(REDRAW selecao 4)
	(COMMAND "_.redraw")
	(REDRAW selecao 4)
	(SETQ contS (+ 1 contS))
    ) ;_  Fim do REPEAT
    (PRINC "\n-------------------------------------------------------------------")
    (SETQ txt (STRCAT "\n Encontrados " (ITOA dupl) " vértices duplicados"))
    (PRINC txt)
    (COMMAND "_.ucs" "P")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==========================================================================================
 ;==========================================================================================






 ;======================================================================
 ;=                                                                    =
 ;=  Rotina para eliminar Textos em branco                             =
 ;=                                                                    =
 ;======================================================================
(DEFUN C:Apt ()
    (sf)
    (SETVAR "cmdecho" 0)
    (PRINC "\n *****  Apaga todos os textos em branco de um desenho  *****")
    (SETQ selecao (SSGET "X" '((0 . "TEXT")))
	  cont	  0
	  seldel  (SSADD)
	  del	  0
    ) ;_  Fim do SETQ
    (IF	selecao
	(PROGN
	    (REPEAT (SSLENGTH selecao)
		(SETQ entidade (SSNAME selecao cont)
		      elemento (ENTGET entidade)
		      txt      (CDR (ASSOC 1 elemento))
		) ;_  Fim do SETQ
		(EliminaCaracter txt " ")
		(IF (= texto "")
		    (PROGN
			(SSADD entidade seldel)
			(SETQ del (1+ del))
		    ) ;_  Fim do progn
		) ;_  Fim do if
		(SETQ cont (1+ cont))
	    ) ;_  Fim do repeat
	) ;_  Fim do progn
    ) ;_  Fim do IF
    (COMMAND "_.erase" seldel "")
    (PRINC "\n-----------------------------------------------------")
    (PRINC (STRCAT "\n      Foram apagados " (ITOA del) " textos em branco."))
    (PRINC "\n-----------------------------------------------------")
    (PRINC)
) ;_  Fim do defun
 ;---------------------------------------------------------------------------
 ;---------------------------------------------------------------------------





 ;---------------------------------------------------------------------------
 ;funcao que retira todos os espacos a esquerda de uma string
 ;---------------------------------------------------------------------------
(DEFUN Eliminacaracter (txt caracter)
    (SETQ tam	(STRLEN txt)
	  texto	txt
    ) ;_  Fim do SETQ
    (WHILE (AND	(> tam 0)
		(= (SUBSTR txt tam 1) caracter)
	   ) ;_  Fim do and
	(SETQ texto (SUBSTR txt 1 (1- tam))
	      tam   (1- tam)
	) ;_  Fim do SETQ
    ) ;_  Fim do while
) ;_  Fim do defun
 ;---------------------------------------------------------------------------
 ;---------------------------------------------------------------------------







 ;======================================================================
 ;=                                                                    =
 ;=  Rotina para converter 3dPoly e spline em Lwpolilinha              =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:cvsp ()
    (COMMAND "_.undo" "_group")
    (SETVAR "cmdecho" 0)
    (sf)
    (PRINC "\n****  Converte 3dPoly e Spline em Lwpolyline ****")
    (SETQ sel	 nil
	  oldosm (GETVAR "osmode")
    ) ;_  Fim do setq
    (SETQ sel (SSGET))
    (SETVAR "osmode" 0)
    (IF	sel
	(PROGN
	    (SETQ num  (SSLENGTH sel)
		  cont 0
	    ) ;_  Fim do setq
	    (REPEAT num
		(SETQ selecao (SSNAME sel cont)
		      cont    (+ 1 cont)
		) ;_  Fim do SETQ
		(extraipontos selecao)
		(IF (OR (= "SPLINE" NOME) (= "POLYLINE" nome))
		    (PROGN
			(SETQ tam   (LENGTH vertlw)
			      cont1 0
			) ;_  Fim do setq
			(COMMAND "_.pline")
			(REPEAT	tam
			    (COMMAND (NTH cont1 vertlw))
			    (SETQ cont1 (+ 1 cont1))
			) ;_  Fim do repeat
			(COMMAND "")
		    ) ;_  Fim do progn
		) ;_  Fim do if
	    ) ;_  Fim do repeat
	) ;_  Fim do progn
	(PRINC "\n Nada Selecionado")
    ) ;_  Fim do if
    (INITGET "S N")
    (SETQ resp (GETKWORD "\n Apaga a Seleção??  (S/N) <N>: "))
    (IF	(= nil resp)
	(SETQ resp "N")
    ) ;_  Fim do if
    (IF	(OR (= resp "S") (= resp "s"))
	(COMMAND "_.erase" sel "")
    ) ;_  Fim do if
    (SETVAR "osmode" oldosm)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================






 ;======================================================================
 ;=                                                                    =
 ;=  Rotina para eliminar vertice  duplicados de uma polilinha         =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:evd ()
    (SETQ olderro *error*
	  *error* erropl
    ) ;_  Fim do setq
    (COMMAND "_.undo" "_group")
    (SETVAR "cmdecho" 0)
    (COMMAND "_.ucs" "")
    (sf)
    (PRINC
	"\n    **** Elimina vértice duplicados  de uma polilinha 2d    ****\n"
    ) ;_  Fim do princ 
    (WHILE (SETQ selecao
		    (ENTSEL "\n>>>>  Selecione  Polilinha  (<ENTER> para Sair): ")
	   ) ;_  Fim do SETQ
	(extrai selecao)
	(IF (= nome "LWPOLYLINE")
	    (PROGN
		(SETQ ent (ENTGET (CAR selecao))
		      num (LENGTH ent)
		) ;_  Fim do SETQ
		(SETQ ptIns (CADR selecao)
		      ptIns (OSNAP ptIns "endpoint")
		      sel   nil
		      cont  0
		) ;_  Fim do SETQ
		(REDRAW (CAR selecao) 3)
		(extraipontos (CAR selecao))
		(SETQ cont 0
		      fec  (CDR (ASSOC 70 ent))
		) ;_  Fim do SETQ
		(WHILE (/= (CAR (NTH cont ent)) 10)
		    (SETQ sel  (APPEND sel (LIST (NTH cont ent)))
			  cont (+ 1 cont)
		    ) ;_  Fim do SETQ
		) ;_  Fim do WHILE
		(SETQ vert  (/ (- num cont) 4)
		      cont1 0
		) ;_  Fim do SETQ
 ;verificar numero do vertice que será o primeiro
		(SETQ numVert (LENGTH vertlw)
		) ;_  Fim do SETQ
		(IF (= fec 1)
		    (SETQ numVert (- numVert 1))
		) ;_  Fim do if
		(SETQ cont1 cont
		      dupl 0
		      p1 (LIST 0 0 0)
		      contvert 0
		) ;_  Fim do SETQ
		(REPEAT	numVert
		    (SETQ p2   (NTH contvert vertlw)
			  dist (DISTANCE p1 p2)
		    ) ;_  Fim do setq
		    (IF	(> dist 0)
			(PROGN
			    (SETQ sel (APPEND sel (LIST (NTH cont1 ent)))
				  sel (APPEND sel (LIST (NTH (+ 1 cont1) ent)))
				  sel (APPEND sel (LIST (NTH (+ 2 cont1) ent)))
				  sel (APPEND sel (LIST (NTH (+ 3 cont1) ent)))
			    ) ;_  Fim do SETQ
			) ;_  Fim do progn
			(SETQ dupl (+ 1 dupl))
		    ) ;_  Fim do IF
		    (SETQ cont1	   (+ 4 cont1)
			  contvert (+ contvert 1)
			  p1	   p2
		    ) ;_  Fim do SETQ
		) ;_  Fim do REPEAT

		(SETQ sel (APPEND sel (LIST (NTH (- num 1) ent))))
		(ENTMOD sel)
		(ENTUPD (CAR selecao))
		(COMMAND "_.redraw")
		(REDRAW (CAR selecao) 4)
		(COMMAND "_.redraw")
		(REDRAW (CAR selecao) 4)
		(PRINC
		    "\n-------------------------------------------------------------------"
		) ;_  Fim do PRINC
		(SETQ txt (STRCAT "\n Encontrados " (ITOA dupl) " vértices duplicados"))
		(PRINC txt)

	    ) ;_  Fim do progn
	    (PROGN
		(PRINC "\n----------------------------------------------")
		(PRINC "\n**   Seleção não é uma polilinha 2d!!!!!!!! **")
		(PRINC "\n----------------------------------------------")
	    ) ;_  Fim do PROGN
	) ;_  Fim do IF
    ) ;_  Fim do while
    (COMMAND "_.ucs" "P")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==========================================================================================
 ;==========================================================================================







 ;======================================================================
 ;=                                                                    =
 ;=            Rotina para eliminar vertice  de uma polilinha          =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:evp ()
    (SETQ olderro *error*
	  *error* erropl
    ) ;_  Fim do setq
    (COMMAND "_.undo" "_group")
    (SETVAR "cmdecho" 0)
    (COMMAND "_.ucs" "")
    (sf)
    (PRINC
	"\n    **** Elimina vértice  de uma polilinha 2d    ****\n"
    ) ;_  Fim do princ 
    (WHILE (SETQ selecao
		    (ENTSEL
			"\n>>>>  Selecione  Polilinha próximo do vértice (<ENTER> para Sair): "
		    ) ;_  Fim do ENTSEL
	   ) ;_  Fim do SETQ
	(extrai selecao)
	(IF (= nome "LWPOLYLINE")
	    (PROGN
		(SETQ ent (ENTGET (CAR selecao))
		      num (LENGTH ent)
		) ;_  Fim do SETQ
		(SETQ ptIns (CADR selecao)
		      ptIns (OSNAP ptIns "endpoint")
		      sel   nil
		      cont  0
		) ;_  Fim do SETQ
		(REDRAW (CAR selecao) 3)
		(extraipontos (CAR selecao))
		(SETQ cont 0)
		(WHILE (/= (CAR (NTH cont ent)) 10)
		    (SETQ sel  (APPEND sel (LIST (NTH cont ent)))
			  cont (+ 1 cont)
		    ) ;_  Fim do SETQ
		) ;_  Fim do WHILE
		(SETQ vert  (/ (- num cont) 4)
		      cont1 0
		) ;_  Fim do SETQ
 ;verificar numero do vertice que será o primeiro
		(SETQ numVert (- (LENGTH vertlw) 1)
		      vertins nil
		) ;_  Fim do SETQ
		(REPEAT	numVert
		    (SETQ p1   (NTH cont1 vertlw)
			  dist (DISTANCE p1 ptIns)
		    ) ;_  Fim do SETQ
		    (IF	(= dist 0)
			(SETQ vertins cont1)
		    ) ;_  Fim do IF
		    (SETQ cont1 (+ 1 cont1))
		) ;_  Fim do REPEAT
		(SETQ vertant  vertins
		      vertpost (- vert vertins)
		) ;_  Fim do SETQ
		(SETQ cont1 cont)
		(REPEAT	vertant
		    (SETQ sel	(APPEND sel (LIST (NTH cont1 ent)))
			  sel	(APPEND sel (LIST (NTH (+ 1 cont1) ent)))
			  sel	(APPEND sel (LIST (NTH (+ 2 cont1) ent)))
			  sel	(APPEND sel (LIST (NTH (+ 3 cont1) ent)))
			  cont1	(+ 4 cont1)
		    ) ;_  Fim do SETQ
		) ;_  Fim do REPEAT
		(SETQ cont1    (+ cont (* 4 vertins))
		      cont1    (+ 1 cont1)
		      vertpost (- vertpost 1)
		) ;_  Fim do SETQ
		(REPEAT	vertpost
		    (SETQ sel	(APPEND sel (LIST (NTH cont1 ent)))
			  sel	(APPEND sel (LIST (NTH (+ 1 cont1) ent)))
			  sel	(APPEND sel (LIST (NTH (+ 2 cont1) ent)))
			  sel	(APPEND sel (LIST (NTH (+ 3 cont1) ent)))
			  cont1	(+ 4 cont1)
		    ) ;_  Fim do SETQ
		) ;_  Fim do REPEAT
		(SETQ sel (APPEND sel (LIST (NTH (- num 1) ent))))
		(ENTMOD sel)
		(ENTUPD (CAR selecao))
		(COMMAND "_.redraw")
		(REDRAW (CAR selecao) 4)
		(COMMAND "_.redraw")
		(REDRAW (CAR selecao) 4)
	    ) ;_  Fim do progn
	    (PROGN
		(PRINC "\n----------------------------------------------")
		(PRINC "\n**   Seleção não é uma polilinha 2d!!!!!!!! **")
		(PRINC "\n----------------------------------------------")
	    ) ;_  Fim do PROGN
	) ;_  Fim do IF
    ) ;_  Fim do while
    (COMMAND "_.ucs" "P")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==========================================================================================
 ;==========================================================================================









 ;======================================================================
 ;=            Rotina de tratamento de erros                            
 ;======================================================================
(DEFUN erropl (msg)
    (PRINC "\n******  Comando cancelado ****\n")
    (SETQ *error* olderro)
    (COMMAND "_ucs" "P")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;======================================================================
 ;======================================================================





 ;======================================================================
 ;=                                                                    =
 ;=            Rotina para vertice inicial de uma polilinha            =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:mvi ()
    (SETQ olderro *error*
	  *error* erropl
    ) ;_  Fim do setq
    (COMMAND "_.undo" "_group")
    (SETVAR "cmdecho" 0)
    (COMMAND "_.ucs" "")
    (sf)
    (PRINC
	"\n    **** Muda vértice inicial de uma polilinhac 2d    ****\n"
    ) ;_  Fim do princ 
    (SETQ selecao (ENTSEL "\n>>>>  Selecione uma Polilinha (<ENTER> para Sair): "))
    (extrai selecao)
    (IF	(= nome "LWPOLYLINE")
	(PROGN
	    (SETQ ent (ENTGET (CAR selecao))
		  fec (CDR (ASSOC 70 ent))
		  num (LENGTH ent)
	    ) ;_  Fim do SETQ
	    (IF	(= fec 1)
		(PROGN
		    (SETQ ptIns	(CADR selecao)
			  ptIns	(OSNAP ptIns "endpoint")
			  sel	nil
			  cont	0
		    ) ;_  Fim do SETQ
		    (REDRAW (CAR selecao) 3)
		    (extraipontos (CAR selecao))
		    (SETQ cont 0)
		    (WHILE (/= (CAR (NTH cont ent)) 10)
			(SETQ sel  (APPEND sel (LIST (NTH cont ent)))
			      cont (+ 1 cont)
			) ;_  Fim do SETQ
		    ) ;_  Fim do WHILE
		    (SETQ vert	(/ (- num cont) 4)
			  cont1	0
		    ) ;_  Fim do SETQ
 ;verificar numero do vertice que será o primeiro
		    (SETQ numVert (- (LENGTH vertlw) 1)
			  vertins nil
		    ) ;_  Fim do SETQ
		    (REPEAT numVert
			(SETQ p1   (NTH cont1 vertlw)
			      dist (DISTANCE p1 ptIns)
			) ;_  Fim do SETQ
			(IF (= dist 0)
			    (SETQ vertins cont1)
			) ;_  Fim do IF
			(SETQ cont1 (+ 1 cont1))
		    ) ;_  Fim do REPEAT
		    (SETQ vertant  vertins
			  vertpost (- vert vertins)
		    ) ;_  Fim do SETQ
		    (SETQ cont1 (+ cont (* 4 vertins)))
		    (REPEAT vertpost
			(SETQ sel   (APPEND sel (LIST (NTH cont1 ent)))
			      sel   (APPEND sel (LIST (NTH (+ 1 cont1) ent)))
			      sel   (APPEND sel (LIST (NTH (+ 2 cont1) ent)))
			      sel   (APPEND sel (LIST (NTH (+ 3 cont1) ent)))
			      cont1 (+ 4 cont1)
			) ;_  Fim do SETQ
		    ) ;_  Fim do REPEAT
		    (SETQ cont1 cont)
		    (REPEAT vertant
			(SETQ sel   (APPEND sel (LIST (NTH cont1 ent)))
			      sel   (APPEND sel (LIST (NTH (+ 1 cont1) ent)))
			      sel   (APPEND sel (LIST (NTH (+ 2 cont1) ent)))
			      sel   (APPEND sel (LIST (NTH (+ 3 cont1) ent)))
			      cont1 (+ 4 cont1)
			) ;_  Fim do SETQ
		    ) ;_  Fim do REPEAT
		    (SETQ sel (APPEND sel (LIST (NTH (- num 1) ent))))
		    (ENTMOD sel)
		    (ENTUPD (CAR selecao))
		    (COMMAND "_.redraw")
		    (REDRAW (CAR selecao) 4)
		    (COMMAND "_.redraw")
		    (REDRAW (CAR selecao) 4)
		) ;_  Fim do PROGN
		(PROGN
		    (PRINC "\n-------------------------------------------")
		    (PRINC "\n**   Não é uma polilinha fechada!!!!!!!! **")
		    (PRINC "\n-------------------------------------------")
		) ;_  Fim do PROGN
	    ) ;_  Fim do PROGN
	) ;_  Fim do progn
	(PROGN
	    (PRINC "\n----------------------------------------------")
	    (PRINC "\n**   Seleção não é uma polilinha 2d!!!!!!!! **")
	    (PRINC "\n----------------------------------------------")
	) ;_  Fim do PROGN
    ) ;_  Fim do IF
    (COMMAND "_.ucs" "P")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==========================================================================================
 ;==========================================================================================




 ;======================================================================
 ;=                                                                    =
 ;=            Rotina para inverter uma polilinha                      =
 ;=                                                                    =
 ;======================================================================
(DEFUN PtAlinhamento (vertlw /)
    (SETQ numVert (LENGTH vertlw)
	  cont	  0
	  ang1	  500
	  ang2	  500
	  ptins	  (OSNAP ptins "near")
	  ptant	  nil
	  ptpost  nil
    ) ;_  Fim do setq
    (REPEAT (- numVert 1)
	(SETQ p1   (NTH cont vertlw)
	      p2   (NTH (+ 1 cont) vertlw)
	      ang1 (ANGLE p1 ptins)
	      ang2 (ANGLE ptins p2)
	      angz (- ang1 ang2)
	) ;_  Fim do setq
	(IF (AND (> angz -0.000001) (< angz 0.000001))
	    (PROGN
		(SETQ ptant p1
		      ptpost p2
		) ;_  Fim do SETQ
	    ) ;_  Fim do progn
	) ;_  Fim do if
	(SETQ cont (+ 1 cont))
    ) ;_  Fim do repeat
) ;_  Fim do defun
 ;=====================================================
(DEFUN c:invP ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (COMMAND "_.undo" "_group")
    (SETVAR "cmdecho" 0)
    (sf)
    (COMMAND "_.ucs" "")
    (PRINC
	"\n    **** Inverte o Sentido de uma polilinha 2d    ****\n"
    ) ;_  Fim do PRINC
    (WHILE (SETQ selecao (ENTSEL "\n  Selecione uma Polilinha (<ENTER> para Sair): "))
	(extrai selecao)
	(IF (= nome "LWPOLYLINE")
	    (PROGN
		(SETQ ptIns (CADR selecao))
		(REDRAW (CAR selecao) 3)
		(extraipontos (CAR selecao))
		(PtAlinhamento vertlw)
		(IF NOT
		    ptant
		    (*error*)
		) ;_  Fim do if
		(SETQ flecha (GETVAR "viewsize"))
		(SETQ flecha  (/ flecha 5)
		      flecha1 (/ flecha 5)
		      flecha2 (/ flecha1 2)
		      ang1    (ANGLE ptins ptpost)
		      ang2    (ANGLE ptins ptant)
		      dist    (DISTANCE ptins ptpost)
		) ;_  Fim do SETQ
		(IF (> flecha dist)
		    (SETQ flecha dist)
		) ;_  Fim do if
		(SETQ
		    p1 (POLAR ptins ang1 flecha)
		    p2 (POLAR p1 ang2 flecha1)
		    p3 (POLAR p2 (+ ang2 0.785) flecha2)
		    p4 (POLAR p2 (+ ang2 5.498) flecha2)
		) ;_  Fim do SETQ
		(GRVECS (LIST 1 p1 p3 1 p3 p2 1 p2 p4 1 p4 p1 4 p1 ptins))
		(INITGET "S N")
		(SETQ
		    opcao
		       (GETKWORD
			   "\n         Inverte a Polilinha selecionada? (S/N) <N>:"
		       ) ;_  Fim do getkword
		) ;_  Fim do setq
		(IF (= opcao nil)
		    (SETQ opcao "N")
		) ;_  Fim do if
		(IF (= "S" opcao)
		    (PROGN
			(PROMPT "\nInvertendo Polilinha . . .\n")
			(SETQ ent  (ENTGET (CAR selecao))
			      num  (LENGTH ent)
			      sel  nil
			      cont 0
			) ;_  Fim do setq
			(WHILE (/= (CAR (NTH cont ent)) 10)
			    (SETQ sel  (APPEND sel (LIST (NTH cont ent)))
				  cont (+ 1 cont)
			    ) ;_  Fim do setq
			) ;_  Fim do WHILE
			(SETQ vert  (/ (- num cont) 4)
			      cont1 1
			) ;_  Fim do setq
			(REPEAT	vert
			    (SETQ cont (- (- num 1) (* cont1 4)))
			    (SETQ sel (APPEND sel (LIST (NTH cont ent)))
				  sel (APPEND sel (LIST (NTH (+ 1 cont) ent)))
				  sel (APPEND sel (LIST (NTH (+ 2 cont) ent)))
			    ) ;_  Fim do SETQ
			    (IF	(> (- vert cont1) 0)
				(PROGN
				    (SETQ lista (CDR (NTH (- cont 1) ent)))
				    (IF	(/= lista 0)
					(PROGN
					    (SETQ lista	 (* lista -1)
						  listal (CONS 42 lista)
					    ) ;_  Fim do SETQ
					) ;_  Fim do PROGN
					(SETQ listal (NTH (- cont 1) ent))
				    ) ;_  Fim do if
				    (SETQ sel (APPEND sel (LIST listal)))
				) ;_  Fim do progn
			    ) ;_  Fim do if
			    (SETQ cont1 (+ 1 cont1))
			) ;_  Fim do REPEAT
			(SETQ sel (APPEND sel (LIST (NTH (- num 2) ent)))
			      sel (APPEND sel (LIST (NTH (- num 1) ent)))
			) ;_  Fim do SETQ
			(ENTMOD sel)
			(ENTUPD (CAR selecao))
			(COMMAND "_.redraw")
			(REDRAW (CAR selecao) 4)
		    ) ;_  Fim do PROGN
		) ;_  Fim do IF
		(COMMAND "_.redraw")
		(REDRAW (CAR selecao) 4)
	    ) ;_  Fim do PROGN
	    (PRINC "\nSeleção não é uma polilinha!!!!!!!!")
	) ;_  Fim do IF
    ) ;_  Fim do WHILE
    (COMMAND "_.ucs" "P")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==========================================================================================
 ;==========================================================================================


 ;================================================================================
 ;         Rotina para alinhar textos e blocos e linhas a um alinhamento definido 
 ;================================================================================
(DEFUN c:alin ()
    (SETVAR "cmdecho" 0)
    (DEFUN *error* (msg)
	(COMMAND "_.ucs" "_p")
	(COMMAND "_.undo" "_end")
	(PRINC)
    ) ;_  Fim do DEFUN
    (COMMAND "_.undo" "_group")
    (COMMAND "_.ucs" "")
    (PRINC "\n *** Selecione blocos, textos e linhas para alinhar")
    (SETQ selecao (SSGET))
    (IF	selecao
	(PROGN
	    (SETQ tam	 (SSLENGTH selecao)
		  cont	 0
		  angulo 5000
		  linha	 (ENTSEL "\n Clique no objeto para alinhar os textos")
		  pSel	 (CADR linha)
		  ent	 (ENTGET (CAR linha))
	    ) ;_  Fim do setq
	    (IF	(= (CDR (ASSOC 0 ent)) "TEXT")
		(SETQ angulo (CDR (ASSOC 50 ent)))
	    ) ;_  Fim do IF
	    (IF	(= (CDR (ASSOC 0 ent)) "LINE")
		(PROGN
		    (SETQ p1	 (CDR (ASSOC 10 ent))
			  p2	 (CDR (ASSOC 11 ent))
			  angulo (ANGLE p1 p2)
		    ) ;_  Fim do setq
		) ;_  Fim do PROGN
	    ) ;_  Fim do IF
	    (IF	(= (CDR (ASSOC 0 ent)) "INSERT")
		(SETQ angulo (CDR (ASSOC 50 ent)))
	    ) ;_  Fim do IF
	    (IF	(= (CDR (ASSOC 0 ent)) "LWPOLYLINE")
		(PROGN
		    (SETQ ptm	 (OSNAP pSel "mid")
			  ptf	 (OSNAP pSel "end")
			  angulo (ANGLE ptm ptf)
		    ) ;_  Fim do setq
		) ;_  Fim do PROGN
	    ) ;_  Fim do IF
	    (IF	(= angulo 5000)
		(PROGN
		    (PRINC "\n   Seleção para alinhamento não é válida!!!!")
		    (EXIT)
		) ;_  Fim do progn
	    ) ;_  Fim do if
	    (REPEAT tam
		(SETQ nome (SSNAME selecao cont)
		      ent  (ENTGET nome)
		) ;_  Fim do setq
		(IF (= (CDR (ASSOC 0 ent)) "TEXT")
		    (PROGN
			(IF (AND (> angulo 1.5707963268) (< angulo 4.712388904))
			    (SETQ angulo (+ angulo 3.1415926536))
			) ;_  Fim do IF
			(SETQ ent (SUBST (CONS 50 angulo) (ASSOC 50 ent) ent))
		    ) ;_  Fim do IF
		) ;_  Fim do IF
		(IF (= (CDR (ASSOC 0 ent)) "INSERT")
		    (SETQ ent (SUBST (CONS 50 angulo) (ASSOC 50 ent) ent))
		) ;_  Fim do IF
		(IF (= (CDR (ASSOC 0 ent)) "LINE")
		    (PROGN
			(SETQ p1   (CDR (ASSOC 10 ent))
			      p2   (CDR (ASSOC 11 ent))
			      dist (DISTANCE p1 p2)
			      pt2  (POLAR p1 angulo dist)
			) ;_  Fim do setq
			(SETQ ent (SUBST (CONS 11 pt2) (ASSOC 11 ent) ent))
		    ) ;_  Fim do progn
		) ;_  Fim do IF
		(SETQ cont (+ 1 cont))
		(ENTMOD ent)
		(ENTUPD nome)
	    ) ;_  Fim do REPEAT
	) ;_  Fim do PROGN
	(PRINC "\n  Nada foi selecionado - Função cancelada")
    ) ;_  Fim do if
    (COMMAND "_.ucs" "_p")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;================================================================================
 ;================================================================================



 ;=============================================================
 ;        Remove linhas duplicadas                             
 ;=============================================================
(DEFUN c:rml (/	s1 s2 ent p10 p11 a6 a8	lista sn cn new_error old_error	roda
	     )
    (SETVAR "CMDECHO" 0)
    (SETQ roda "\\|/-")
    (SF)
    (PRINC "\n Remove linhas duplicadas - Rotina escrita por E. Fernal")
    (DEFUN rodaroda (/ calota)
	(SETQ calota (SUBSTR roda 1 1)
	      roda   (SUBSTR roda 2)
	) ;_  Fim do SETQ
	(PRINC (STRCAT "\r" calota))
	(SETQ roda (STRCAT roda calota))
    ) ;_  Fim do DEFUN
    (DEFUN new_error (error)
	(COMMAND "_.UNDO" "E")
	(COMMAND "_.U")
	(PRINC "\n-> Erro : Situação permanece inalterada...")
	(PRINC)
	(IF old_error
	    (SETQ *error* old_error)
	) ;_  Fim do IF
    ) ;_  Fim do DEFUN
    (SETQ old_error *error*
	  *error* new_error
    ) ;_  Fim do SETQ
    (PRINC "\n  : Trabalhando : ")
    (IF	(SETQ s1 (SSGET "X" '((0 . "LINE"))))
	(PROGN (COMMAND "_.UNDO" "G")
	       (WHILE (> (SSLENGTH s1) 0)
		   (SETQ ent   (SSNAME s1 0)
			 s2    nil
			 s1    (SSDEL ent s1)
			 a8    (CDR (ASSOC 8 (ENTGET ent)))
			 a6    (CDR (ASSOC 6 (ENTGET ent)))
			 p10   (CDR (ASSOC 10 (ENTGET ent)))
			 p11   (CDR (ASSOC 11 (ENTGET ent)))
			 lista nil
			 lista (COND ((AND (> (SSLENGTH s1) 1) a6)
				      (LIST '(0 . "LINE")
					    (CONS 6 a6)
					    (CONS 8 a8)
					    (CONS 10 p10)
					    (CONS 11 p11)
				      ) ;_  Fim do LIST
				     )
				     ((> (SSLENGTH s1) 1)
				      (LIST '(0 . "LINE")
					    (CONS 8 a8)
					    (CONS 10 p10)
					    (CONS 11 p11)
				      ) ;_  Fim do LIST
				     )
				     (T nil)
			       ) ;_  Fim do COND
		   ) ;_  Fim do SETQ
		   (rodaroda)
		   (IF lista
		       (IF (SETQ s2 (SSGET "x" lista))
			   (IF (> (SSLENGTH s2) 0)
			       (PROGN (SETQ sn (SSLENGTH s2)
					    cn 0
				      ) ;_  Fim do SETQ
				      (REPEAT sn
					  (IF (SSMEMB (SSNAME s2 cn) s1)
					      (SETQ s1 (SSDEL (SSNAME s2 cn) s1))
					  ) ;_  Fim do IF
					  (SETQ cn (1+ cn))
					  (rodaroda)
				      ) ;_  Fim do REPEAT
				      (COMMAND "_.ERASE" s2 "")
				      (GC)
				      (ENTDEL ent)
			       ) ;_  Fim do PROGN
			       nil
			   ) ;_  Fim do IF
			   nil
		       ) ;_  Fim do IF
		       nil
		   ) ;_  Fim do IF
	       ) ;_  Fim do WHILE
	       (COMMAND "_.UNDO" "E")
	) ;_  Fim do PROGN
	(ALERT "Linhas não encontradas em seu desenho...")
    ) ;_  Fim do IF
    (IF	old_error
	(SETQ *error* old_error)
    ) ;_  Fim do IF
    (PRINC)
    (PRINC)
) ;_  Fim do DEFUN
 ;=============================================================
 ;=============================================================





 ;=============================================================
 ;=                                                           =
 ;=            Converte e junta linhas em polilinhas          =
 ;=                                                           =
 ;=============================================================
(DEFUN C:CvP (/ nome en num ind cl la tt tem)
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (sf)
    (COMMAND "_.undo" "_group")
    (SETVAR "CMDECHO" 0)
    (PRINC
	"\n******** Converte linhas e arcos de um determinado Layer em Polilinhas ******"
    ) ;_  Fim do PRINC
    (SETQ CL (GETVAR "CLAYER"))
    (SETVAR "HIGHLIGHT" 1)
    (SETQ LA (GETSTRING
		 (STRCAT "\nQual o layer que deseja efetuar a convercao <"
			 CL
			 "> : "
		 ) ;_  Fim do STRCAT
	     ) ;_  Fim do GETSTRING
    ) ;_  Fim do SETQ
    (IF	(= LA "")
	(SETQ LA CL)
    ) ;_  Fim do IF
    (SETQ EN (SSGET "X" (LIST (CONS 8 LA) (CONS 0 "LINE"))))
    (IF	en
	(PROGN
	    (SETQ NA (SSLENGTH EN)
		  TT 0
	    ) ;_  Fim do SETQ
	    (REPEAT NA
		(COMMAND ".PEDIT" (SSNAME EN TT) "Y" "X")
		(SETQ TT (+ TT 1))
	    ) ;_  Fim do REPEAT
	) ;_  Fim do progn
    ) ;_  Fim do if
    (SETQ en (SSGET "X" (LIST (CONS 8 LA) (CONS 0 "arc"))))
    (IF	en
	(PROGN
	    (SETQ NA (SSLENGTH EN)
		  TT 0
	    ) ;_  Fim do SETQ
	    (REPEAT NA
		(COMMAND "_.PEDIT" (SSNAME EN TT) "Y" "X")
		(SETQ TT (+ TT 1))
	    ) ;_  Fim do REPEAT
	) ;_  Fim do progn
    ) ;_  Fim do if
    (SETQ en (SSGET "X" (LIST (CONS 8 LA) (CONS 0 "Lwpolyline"))))
    (IF	en
	(PROGN
	    (SETVAR "HIGHLIGHT" 1)
	    (SETQ QUES
		     (GETSTRING "\nJuntar as polylines quando possivel ? <N> :")
	    ) ;_  Fim do SETQ
	    (IF	(= nil QUES)
		(SETQ QUES "N")
	    ) ;_  Fim do IF
	    (SETQ QUES (STRCASE (SUBSTR QUES 1 1)))
	    (IF	(= QUES "S")
		(PROGN
		    (SETQ num (- (SSLENGTH en) 1))
		    (SETQ ind 0)
		    (REPEAT num
			(SETQ nome (SSNAME en ind))
			(SETQ tem (ENTGET nome))
			(IF (/= tem nil)
			    (COMMAND "_.PEDIT" nome "_J" nome en "" "_X")
			) ;_  Fim do if
			(SETQ ind (+ ind 1))

		    ) ;_  Fim do repeat
		) ;_  Fim do progn
	    ) ; end if
	) ;_  Fim do progn
	(PROMPT
	    (STRCAT "\n!!!!!!!!!!! Nenhuma linha arco ou polilinha no Layer: "
		    la
		    " !!!!!!!!!"
	    ) ;_  Fim do strcat
	) ;_  Fim do prompt
    ) ;_  Fim do if
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
;;;;;;;;====================================================================
;;;;;;;;====================================================================




 ;=============================================================
 ;=                                                           =
 ;=           Calcula a Distancia de caminhamento             =
 ;=                                                           =
 ;=============================================================
(DEFUN c:dtt (/ pt1 pt2 d)
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (SETVAR "CMDECHO" 0)
    (sf)
    (PRINC
	"\n ***** Calcula a distancia de caminhamento  ******"
    ) ;_  Fim do princ
    (SETQ pt1 (GETPOINT "\nPrimeiro Ponto: ")
	  d   0
    ) ;_  Fim do SETQ
    (WHILE
	(SETQ pt2 (GETPOINT pt1 "\nProximo Ponto: "))
	   (SETQ d (+ (DISTANCE pt1 pt2) d))
	   (PRINC (STRCAT "\n***  Distância: "
			  (RTOS (DISTANCE pt1 pt2) 2 2)
			  " metros  --- Acumulado: "
			  (RTOS d 2 2)
			  " metros   \n-------------------------------------------------------------"
		  ) ;_  Fim do PRINC
	   ) ;_  Fim do WHILE
	   (SETQ pt1 pt2)
    ) ;_  Fim do while
    (SETQ *error* errorot)
    (PRINC)
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================





 ;======================================================================
 ;=                                                                    =
 ;=            Exibe / rodando                                         =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:moinho ()
    (SETQ erro_rot *error*)
    (SETQ *error* erro)
    (SETVAR "cmdecho" 0)
    (SETQ numctr 1
	  ctrole 1
    ) ;_  Fim do setq
    (WHILE (= 1 ctrole)
	(PRINC (STRCAT (CHR 008) (CHR 008) (CHR 008)))
	(IF (= numctr 1)
	    (PRINC " |")
	) ;_  Fim do if
	(IF (= numctr 2)
	    (PRINC " /")
	) ;_  Fim do if
	(IF (= numctr 3)
	    (PRINC " -")
	) ;_  Fim do if
	(IF (= numctr 4)
	    (PRINC " \\")
	) ;_  Fim do if
	(SETQ numctr (+ 1 numctr))
	(IF (= numctr 5)
	    (SETQ numctr 1)
	) ;_  Fim do if
    ) ;_  Fim do while
    (PRINC)
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================




 ;======================================================================
 ;=                                                                    =
 ;=            Encontra lista de dados                                 =
 ;=                                                                    =
 ;======================================================================
(DEFUN c:lista (tbna / a lst)
    (IF	(AND (EQUAL (TYPE tbna) 'LIST)
	     (EQUAL (CADR tbna) 16)
	) ;and
	(PROGN
	    (SETQ tbna (CAR tbna)) ;setq
	    (WHILE (SETQ a (TBLNEXT tbna (NOT a)))
 ; a acts as a rewind first time
		(IF (NOT (EQUAL 16 (LOGAND 16 (CDR (ASSOC 70 a)))))
		    (SETQ lst (APPEND lst
				      (LIST (CDR (ASSOC 2 a)))
			      ) ;append
		    ) ;setq
		) ;if
	    ) ;while
	) ;progn then local only
	(PROGN
	    (WHILE (SETQ a (TBLNEXT tbna (NOT a)))
 ; a acts as a rewind first time
		(SETQ lst (APPEND lst
				  (LIST (CDR (ASSOC 2 a)))
			  ) ;append
		) ;setq
	    ) ;while
	) ;progn else
    ) ;if 
    lst
) ;defun tnlist
;;;;;;;;====================================================================
;;;;;;;;====================================================================







;;;;;;;;====================================================================
 ;                      funcao tangente                                      
;;;;;;;;====================================================================
(DEFUN tan (an)
    (/ (SIN an) (COS an))
) ;_  Fim do defun
;;;;;;;;====================================================================
;;;;;;;;====================================================================






;;;;===================================================================
;;;;;;----------------Break linhas na intersecao                       
;;;;===================================================================
(DEFUN c:SEPL (/ ss point olderr)
    (SETQ olderr  *error*
	  *error* errorot
	  oldosm  (GETVAR "osmode")
    ) ;_  Fim do SETQ
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_group")
    (sf)
    (PRINC "\nSepara linha\polilinha em duas")
    (SETQ ss nil
	  point	nil
    ) ;_  Fim do SETQ
    (WHILE (NOT ss)
	(SETQ ss (ENTSEL "\nSelecione linha/polilinha para cortar: "))
    ) ;while
    (WHILE (NOT point)
	(SETQ point
		 (GETPOINT "\nSelecione intersecção para cortar: ")
	) ;_  Fim do setq
    ) ;while
    (COMMAND "_.break" ss "f" point "@")
    (COMMAND "_.undo" "_end")
    (SETQ *error* olderr)
    (PRINC)
) ;_  Fim do defun
;;;;===================================================================
(DEFUN break_error (msg)
    (PRINC (STRCAT "\nError: " msg))
    (SETVAR "osmode" 0)
    (SETVAR "cmdecho" 1)
    (PRINC)
) ;_  Fim do defun
 ;==========================================================================
 ;==========================================================================





 ;==========================================================================
;;;;---------Obtem imformacoes de objetos                                  
 ;==========================================================================
(DEFUN C:ssobj (/ ENT EDATA ETYPE P1 P1X P1Y P2 D ANG TBL)
    (SETQ ENT (CAR (ENTSEL "\nClique na Entidade: ")))
    (IF	ENT
	(PROGN
	    (SETQ EDATA	(ENTGET ENT)
		  ETYPE	(CDR (ASSOC 0 EDATA))
		  P1	(CDR (ASSOC 10 EDATA))
		  P1X	(CADR (ASSOC 10 EDATA))
		  P1Y	(CADDR (ASSOC 10 EDATA))
	    ) ;_  Fim do setq
	    (PROMPT "    Type: ")
	    (PRIN1 (READ ETYPE))
	    (PROMPT "    Layer name: ")
	    (PRIN1 (READ (CDR (ASSOC 8 EDATA))))
	    (IF	(EQ ETYPE "LINE")
		(PROGN
		    (SETQ P2  (CDR (ASSOC 11 EDATA))
			  D   (DISTANCE P1 P2)
			  ANG (/ (* (ANGLE P1 P2) 180) PI)
		    ) ;_  Fim do setq
		    (PROMPT "    Length = ")
		    (PRIN1 D)
		    (PROMPT "    Angle = ")
		    (PRIN1 ANG)
		    (PROMPT " degrees")
		) ;_  Fim do progn
	    ) ;_  Fim do if
	    (IF	(EQ ETYPE "CIRCLE")
		(PROGN
		    (PROMPT "    Circle center: x = ")
		    (PRIN1 P1X)
		    (PROMPT "  y = ")
		    (PRIN1 p1y)
		    (PROMPT "    Circle radius = ")
		    (PRIN1 (CDR (ASSOC 40 EDATA)))
		) ;_  Fim do progn
	    ) ;_  Fim do if
	    (IF	(EQ ETYPE "MTC")
		(PROGN
		    (PROMPT "    Arc center: x = ")
		    (PRIN1 P1X)
		    (PROMPT "  y = ")
		    (PRIN1 P1Y)
		    (PROMPT "    Arc radius = ")
		    (PRIN1 (CDR (ASSOC 40 EDATA)))
		) ;_  Fim do progn
	    ) ;_  Fim do if
	    (IF	(EQ ETYPE "INSERT")
		(PROGN
		    (PROMPT "    Block name: ")
		    (PRIN1 (READ (CDR (ASSOC 2 EDATA))))
		    (PROMPT "    Block insertion point: x = ")
		    (PRIN1 P1X)
		    (PROMPT "  y = ")
		    (PRIN1 P1Y)
		) ;_  Fim do progn
	    ) ;_  Fim do if
	    (IF	(EQ ETYPE "TEXT")
		(PROGN
		    (SETQ TBL (TBLSEARCH "style" (CDR (ASSOC 7 EDATA))))
		    (PROMPT "    Style: ")
		    (PRIN1 (READ (CDR (ASSOC 7 EDATA))))
		    (PROMPT "    Font: ")
		    (PRIN1 (READ (CDR (ASSOC 3 TBL))))
		    (PROMPT "    Text height: ")
		    (PRIN1 (CDR (ASSOC 40 EDATA)))
		) ;_  Fim do progn
	    ) ;_  Fim do if
	    (IF	(EQ ETYPE "POLYLINE")
		(PROGN
		    (PROMPT "    Starting width: ")
		    (PRIN1 (CDR (ASSOC 40 EDATA)))
		    (PROMPT "    Ending width: ")
		    (PRIN1 (CDR (ASSOC 41 EDATA)))
		) ;_  Fim do progn
	    ) ;_  Fim do if
	) ;_  Fim do progn
	(PROMPT "  ** Entidade nao Selecionada! **  ")
    ) ;_  Fim do if
    (PRINC)
) ; end lq.lsp   
 ;==========================================================================
 ;==========================================================================





 ;==========================================================================
 ;            Desenha uma linha na bissetriz de duas linhas                 
 ;==========================================================================
(DEFUN getln (PR)
    (SETQ TYPE	"nil"
	  PRMPT	(STRCAT "\nClick na " PR " linha: ")
    ) ;_  Fim do setq
    (WHILE (/= TYPE "LINE")
	(IF (/= (SETQ TEMP (ENTSEL PRMPT)) nil)
	    (PROGN
		(SETQ LN1  (ENTGET (CAR TEMP))
		      TYPE (CDR (ASSOC 0 LN1))
		) ;_  Fim do setq
		(IF (/= TYPE "LINE")
		    (PRINT (STRCAT "Nao encontro bissetriz para a " TYPE))
		) ;_  Fim do if
	    ) ;_  Fim do progn
	    (PRINT "Ponto invalido")
	) ;_  Fim do if
    ) ;end while
) ;end getln
 ;==========================================================================
(DEFUN C:BISS ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (sf)
    (COMMAND "_undo" "_group")
    (SETQ oldosm (GETVAR "osmode"))
    (getln "Primeira")
    (SETQ P1   (CDR (ASSOC 10 LN1))
	  P2   (CDR (ASSOC 11 LN1))
	  PIK1 (OSNAP (CADR TEMP) "near")
    ) ;_  Fim do setq
    (getln "Segunda")
    (SETQ p3   (CDR (ASSOC 10 LN1))
	  P4   (CDR (ASSOC 11 LN1))
	  PIK2
	       (OSNAP (CADR TEMP) "near")
    ) ;_  Fim do setq
 ; get intersection and angles
    (SETQ P5   (INTERS P1 P2 P3 P4 nil)
	  ANGA (ANGLE P5 PIK1)
	  ANGB (ANGLE P5 PIK2)
    ) ;_  Fim do setq
    (IF	(> ANGA ANGB)
	(SETQ ANGC (+ (/ (+ (- (* 2 PI) ANGA) ANGB) 2) ANGA))
	(SETQ ANGC (+ (/ (- ANGB ANGA) 2) ANGA))
    ) ;_  Fim do if
    (SETVAR "osmode" 0)
    (COMMAND "LINE" P5 (POLAR P5 ANGC (DISTANCE P5 PIK1)) "")
    (SETVAR "osmode" oldosm)
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRIN1)
) ; end bisect.lsp
 ;==========================================================================
 ;==========================================================================



 ;==========================================================================
 ;                          Copia e rotaciona um objeto                     
 ;==========================================================================
(DEFUN c:copyrot ()
    (sf)
    (PROMPT
	"\nSelecione os itens para ser copiados e rotacionados: "
    ) ;_  Fim do prompt
    (SETQ ss1 (SSGET))
    (SETQ pt1 (GETPOINT "\nClick no ponto para rotacionar: "))
    (COMMAND "copy" ss1 "" "0,0" "0,0")
    (COMMAND "rotate" ss1 "" (LIST (CAR pt1) (CADR pt1)) pause)
    (COMMAND "redraw")
    (PRINC)
) ;_  Fim do defun
 ;==========================================================================
 ;==========================================================================




 ;==========================================================================
 ;  Troca ocorrencias de blocos selecionados por um outro bloco fornecido   
 ;==========================================================================
(DEFUN c:trblS ()
    (SETVAR "cmdecho" 0)
    (SETQ olderro *error*
	  *error* errorot
	  msg	  "\n  ***** Comando Cancelado   ****"
    ) ;_  Fim do setq
    (sf)
    (COMMAND "_.undo" "_group")
    (PROMPT "\n   Selecione os blocos p/ mudar: ")
    (SETQ ENT1 (SSGET))
    (SETQ NEWBL (GETSTRING "\nNome do Bloco para Substituir: "))
    (SETQ tem (TBLSEARCH "block" newbl))
    (IF	tem
	(PROGN
	    (COMMAND "_.insert" NEWBL nil)
	    (SETQ N (SSLENGTH ENT1))
	    (SETQ I 0)
	    (REPEAT N
		(SETQ BL1 (ENTGET (SSNAME ENT1 I)))
		(SETQ NWNM (CONS 2 NEWBL))
		(SETQ OLD (ASSOC 2 BL1))
		(SETQ ODNM (CDR OLD))
		(ENTMOD (SUBST NWNM OLD BL1))
		(SETQ I (1+ I))
	    ) ;_  Fim do repeat
	) ;_  Fim do progn
	(PRINC "\n   Bloco não encontrado!!!!!")
    ) ;_  Fim do if

    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;==========================================================================
 ;==========================================================================




 ;==========================================================================
 ;              troca a ocorerrencia de um bloco por outro bloco            
 ;==========================================================================
(DEFUN c:TrBl ()
    (SETVAR "cmdecho" 0)
    (SETQ olderro *error*
	  *error* errorot
	  msg	  "\n  ***** Comando Cancelado   ****"
    ) ;_  Fim do setq
    (sf)
    (COMMAND "_.undo" "_group")
    (PROMPT "\nSelecione um dos blocos que desejas trocar: ")
    (SETQ Sel (ENTSEL))
    (IF	Sel
	(PROGN
	    (SETQ E1 (ENTGET (CAR Sel)))
	    (SETQ E2N (CDR (ASSOC 2 E1)))
	    (SETQ E2L (CDR (ASSOC 8 E1)))
	    (SETQ tiposel (CDR (ASSOC 0 E1)))
	    (IF	(= tiposel "INSERT")
		(PROGN
		    (SETQ ENT1 (SSGET "X" (LIST (CONS 0 "INSERT") (CONS 2 E2N))))
		    (SETQ NEWBL (GETSTRING "\nEntre com o nome do bloco para trocar: "))
		    (SETQ tem (TBLSEARCH "block" newbl))
		    (IF	tem
			(PROGN
			    (COMMAND "_.insert" NEWBL nil)
			    (SETQ N (SSLENGTH ENT1))
			    (SETQ I 0)
			    (REPEAT N
				(SETQ BL1 (ENTGET (SSNAME ENT1 I)))
				(SETQ NWNM (CONS 2 NEWBL))
				(SETQ OLD (ASSOC 2 BL1))
				(SETQ OLDBL (CDR OLD))
				(SETQ OLDL (ASSOC 8 BL1))
				(SETQ OLDLL (CDR OLDL))
				(IF (AND (EQUAL E2N OLDBL) (EQUAL E2L OLDLL))
				    (PROGN
					(SETQ ODNM (CDR OLD))
					(ENTMOD (SUBST NWNM OLD BL1))
				    ) ;_  Fim do progn
				) ;_  Fim do if
				(SETQ I (1+ I))
			    ) ;_  Fim do repeat
			) ;_  Fim do progn
			(PRINC "\n Bloco não encontrado")
		    ) ;_  Fim do if
		) ;_  Fim do progn
		(PRINC "\n A seleção não é um bloco válido")
	    ) ;_  Fim do if
	) ;_  Fim do progn
	(PRINC "\n  Nada foi selecionado")
    ) ;_  Fim do if
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==========================================================================
 ;==========================================================================



 ;=======================================================================
 ;--------------Congelar Layers                                          
 ;=======================================================================
(DEFUN C:FL ()
    (SETVAR "cmdecho" 0)
    (SETQ olderro *error*
	  *error* errorot
	  msg	  "\n  ***** Comando Cancelado   ****"
    ) ;_  Fim do setq
    (SF)
    (COMMAND "_.undo" "_group")
    (PROMPT
	"\nClique Entidade para congelar Layer: "
    ) ;_  Fim do prompt
    (WHILE
	(SETQ name (CDR (ASSOC 8 (ENTGET (CAR (ENTSEL))))))
	   (COMMAND "_.layer" "_f" name "")
    ) ;_  Fim do while
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;=======================================================================
 ;=======================================================================



 ;=======================================================================
 ;----------muda varias entidades p/ layer da ultima entidade            
 ;=======================================================================
(DEFUN C:ML ()
    (SETVAR "cmdecho" 0)
    (SETQ olderro *error*
	  *error* errorot
	  msg	  "\n  ***** Comando Cancelado   ****"
    ) ;_  Fim do setq
    (sf)
    (COMMAND "_.undo" "_group")
    (PROMPT
	"\nSelecine entidades para mudar Layer <ENTER> p/ final da seleção: "
    ) ;_  Fim do prompt
    (SETQ ss (SSGET))
    (PROMPT "\nClick na entidade para atualizar o Layer: ")
    (SETQ name (CDR (ASSOC 8 (ENTGET (CAR (ENTSEL))))))
    (COMMAND "_.chprop" ss "" "_la" name "")
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;=======================================================================
 ;=======================================================================



 ;=======================================================================
 ;       Rotina que conta os blocos dos desenhos                         
 ;=======================================================================
(DEFUN C:Contb ()
    (SETVAR "cmdecho" 0)
    (sf)
    (PRINC "\n Conta todos os blocos do desenho e emite relatório")
    (PRINT)
    (PRINT)
    (PRINT)
    (PRINT)
    (SETQ Sel (SSGET "x" '((0 . "INSERT"))))
    (IF	Sel
	(PROGN
	    (TEXTPAGE)
	    (PRINC "\n                  RELATÓRIO")
	    (PRINC "\n         NOME BLOCO     QUANTIDADE")
	    (SETQ total	 (SSLENGTH Sel)
		  selb	 nil
		  blocos (TBLNEXT "block" T)
	    ) ;_  Fim do SETQ
	    (WHILE blocos
		(SETQ nbloco (CDR (ASSOC 2 blocos))
		      selb   (APPEND selb (LIST nbloco))
		      blocos (TBLNEXT "block")
		) ;_  Fim do setq
	    ) ;_  Fim do while
	    (SETQ numb (LENGTH selb)
		  cont 0
	    ) ;_  Fim do SETQ
	    (REPEAT numb
		(SETQ contbl 0
		      cont1  0
		      nome   (NTH cont selb)
		) ;_  Fim do setq
		(REPEAT	total
		    (SETQ nomeb (CDR (ASSOC 2 (ENTGET (SSNAME Sel cont1)))))
		    (IF	(= nomeb nome)
			(SETQ contbl (+ 1 contbl))
		    ) ;_  Fim do if
		    (SETQ cont1 (+ 1 cont1))
		) ;_  Fim do repeat
		(IF (> contbl 0)
		    (PROGN
			(SETQ tam   (STRLEN nome)
			      numbl (ITOA contbl)
			      tam1  (STRLEN numbl)
			      nome  (STRCAT "         " nome)
			      dif   (- 35 (+ 10 (+ tam tam1)))
			) ;_  Fim do setq
			(REPEAT	dif
			    (SETQ nome (STRCAT nome " "))
			) ;_  Fim do repeat
			(SETQ nome (STRCAT nome numbl))
			(PRINC "\n")
			(PRINC nome)
		    ) ;_  Fim do progn
		) ;_  Fim do if
		(SETQ cont (+ 1 cont))
	    ) ;_  Fim do repeat
	) ;_  Fim do PROGN
	(PRINC "\nNão há blocos inseridos no desenho atual")
    ) ;_  Fim do if
    (TERPRI)
    (TERPRI)
    (TERPRI)
    (PRINC)
) ;_  Fim do DEFUN
 ;=======================================================================
 ;=======================================================================



 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
(DEFUN c:zp ()
    (STARTAPP "c:\\sftopo\\sfzoomnoponto.exe")
    (PRINC)
) ;_  Fim do defun
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>



 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
(DEFUN c:lp ()
    (STARTAPP "c:\\sftopo\\sfligarpontos.exe")
    (PRINC)
) ;_  Fim do defun
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>



 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
(DEFUN c:ip ()
    (STARTAPP "c:\\sftopo\\sfinserirpontos.exe")
    (PRINC)
) ;_  Fim do defun
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>




 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
(DEFUN c:exp ()
    (STARTAPP "c:\\sftopo\\sfexportapolilinha.exe")
    (PRINC)
) ;_  Fim do defun
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
 ;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>



 ;==============================================================================
 ;                 Rotina deixa elementos selecionados invisivel                
 ;==============================================================================
(DEFUN c:Nvis (/ SS1 EN	LOOK_FOR lOOK_FOR_DXF LOOK_FOR_LAYER LAYER_LOCK_STATUS TOMODIFY	TOCON
	       COUNTER
	      )
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_group")
    (sf)
    (PRINC "\n            Rotina criada por Frank Zander")
    (PRINC "\nSelecione as entidades para deixar invisivel!!!!!!¨")
    (SETQ ss1 (SSGET))
    (IF	ss1
	(PROGN
	    (SETQ counter 0)
	    (WHILE (<= counter (- (SSLENGTH ss1) 1))

		(SETQ en (SSNAME ss1 counter))
		(SETQ look_FOR en)
		(SETQ LOOK_FOR_DXF (ENTGET LOOK_FOR))
		(SETQ LOOK_FOR_LAYER (CDR (ASSOC 8 LOOK_FOR_DXF)))
		(SETQ LAYER_LOCK_STATUS
			 (CDR
			     (ASSOC 70
				    (TBLSEARCH "LAYER" LOOK_FOR_LAYER)
			     ) ;_  Fim do ASSOC
			 ) ;_  Fim do CDR
		) ;_  Fim do setq
		(IF (= LAYER_LOCK_STATUS 4)
		    (PROGN
			(COMMAND "_.layer" "_unlock" LOOK_FOR_LAYER "")
		    ) ;_  Fim do progn

		) ;_  Fim do If

		(SETQ LOOK_FOR_DXF
			 (SUBST	(CONS 60 1)
				(ASSOC 60 LOOK_FOR_DXF)
				LOOK_FOR_DXF
			 ) ;_  Fim do subst
		) ;_  Fim do setq
		(SETQ tocon (CONS 60 1))
		(SETQ ToModify (APPEND LOOK_FOR_DXF (LIST tocon)))
		(ENTMOD ToModify)
		(IF (= LAYER_LOCK_STATUS 4)
		    (PROGN
			(COMMAND "_.layer" "_lock" LOOK_FOR_LAYER "")

		    ) ;_  Fim do progn
		) ;_  Fim do If
		(SETQ counter (+ counter 1))
	    ) ;_  Fim do while

	) ;_  Fim do progn
    ) ;_  Fim do if
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;==============================================================================
 ;==============================================================================





 ;==============================================================================
 ;                    Torna as selecoes visisveis                               
 ;==============================================================================

(DEFUN C:Vis (/	SS1 EN LOOK_FOR	lOOK_FOR_DXF LOOK_FOR_LAYER LAYER_LOCK_STATUS TOMODIFY TOCON
	     )
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_group")
    (sf)
    (PRINC "\n            Rotina criada por Frank Zander")
    (PRINC "\n     Torna todas as entidades invisiveis em visiveis")
    (SETQ ss1 (SSGET "x" '((60 . 1))))
    (IF	ss1
	(PROGN
	    (SETQ counter 0)
	    (WHILE (<= counter (- (SSLENGTH ss1) 1))
		(SETQ en (SSNAME ss1 counter))
		(SETQ look_for en)
		(SETQ LOOK_FOR_DXF (ENTGET LOOK_FOR))
		(SETQ LOOK_FOR_LAYER (CDR (ASSOC 8 LOOK_FOR_DXF)))
		(SETQ LAYER_LOCK_STATUS
			 (CDR
			     (ASSOC 70
				    (TBLSEARCH "LAYER" LOOK_FOR_LAYER)
			     ) ;_  Fim do ASSOC
			 ) ;_  Fim do CDR
		) ;_  Fim do setq
		(IF (= LAYER_LOCK_STATUS 4)
		    (PROGN
			(COMMAND "_.layer" "_unlock" LOOK_FOR_LAYER "")

		    ) ;_  Fim do progn

		) ;_  Fim do If

		(SETQ LOOK_FOR_DXF
			 (SUBST	(CONS 60 0)
				(ASSOC 60 LOOK_FOR_DXF)
				LOOK_FOR_DXF
			 ) ;_  Fim do subst
		) ;_  Fim do setq
		(ENTMOD LOOK_FOR_DXF)
		(IF (= LAYER_LOCK_STATUS 4)
		    (PROGN
			(COMMAND "_.layer" "_lock" LOOK_FOR_LAYER "")

		    ) ;_  Fim do progn
		) ; End of while
		(SETQ counter (+ counter 1))
	    ) ;_  Fim do while
	) ;_  Fim do PROGN
    ) ;_  Fim do IF
    (COMMAND "_.redrawall")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==============================================================================
 ;==============================================================================





 ;==============================================================================
 ;=====================Define senha para acesso                                 
 ;==============================================================================
(DEFUN c:SenhaSF (/ pass msg inputvalue tmp arq_tmp1 dcl_id)
    (sf)
    (SETQ pass "autolisp")
;;;;;; ALTERE AQUI A SENHA  
    (WHILE (/= pass T)
	(PROGN
	    (DEFUN errdia (msg)
		(SET_TILE "error" msg)
		nil
	    ) ;_  Fim do defun
	    (DEFUN xxx ()
		(IF (= pass inputvalue)

		    (PROGN (DONE_DIALOG)
			   (SETQ pass T)
			   (PRINC "\nSenha Correta, Bom Trabalho!!!")
		    ) ;_  Fim do progn
		    (PROGN
			(SET_TILE "Password2" "")
			(MODE_TILE "Password2" 2)
			(errdia "Senha incorreta, tente novamente")
		    ) ;_  Fim do progn
		) ;_  Fim do if
	    ) ;_  Fim do defun
	    (PRINC
		"\r*********************************************************************"
	    ) ;_  Fim do princ
	    (DEFUN gravar ()
		(WRITE-LINE
		    "pass : dialog {label = \"Senha de acesso...\";
    : edit_box {
    key = \"Password2\";
    password_char = \"*\";
    is_default = true;}
ok_only;
: errtile {
  width = 30;
  alignment = right ;
  
  }   }"
		    arq_tmp2
		) ;_  Fim do write-line
	    ) ;_  Fim do defun
	    (SETQ tmp (STRCAT (GETENV "temp") "\\_.dcl"))
	    (SETQ arq_tmp2 (OPEN tmp "w"))
	    (WRITE-LINE "" arq_tmp2)
	    (CLOSE arq_tmp2)
	    (SETQ arq_tmp2 (OPEN tmp "a"))
	    (gravar)
	    (CLOSE arq_tmp2)
	    (SETQ dcl_id (LOAD_DIALOG tmp))
	    (IF	(NOT (NEW_DIALOG "pass" dcl_id))
		(EXIT)
	    ) ;_  Fim do if
	    (MODE_TILE "Password2" 2)
	    (ACTION_TILE
		"Password2"
		"(setq inputvalue (get_tile \"Password2\"))
     (xxx)
     "
	    ) ;_  Fim do action_tile
	    (ACTION_TILE
		"accept"
		"(setq inputvalue (get_tile \"Password2\"))
     (xxx)
     "
	    ) ;_  Fim do action_tile
	    (ACTION_TILE
		"cancel"
		"(setq inputvalue nil)
     (xxx)
     "
	    ) ;_  Fim do action_tile
	    (errdia "Digite a senha para desbloquear.")
	    (START_DIALOG)
	    (UNLOAD_DIALOG dcl_id)

	) ;_  Fim do progn
    ) ;_  Fim do while
    (PRINC)
) ;_  Fim do defun
 ;==============================================================================
 ;==============================================================================



 ;==============================================================================
 ;        Rotina para apagar tudo dentor  de um retangulo                       
 ;==============================================================================
(DEFUN c:ADR ()
    (sf)
    (PRINC "n                Rotina original escrita por Bob Jones")
    (SETQ oldosm  (GETVAR "osmode")
	  olducs  (GETVAR "ucsname")
	  oldlts  (GETVAR "ltscale")
	  olderro *error*
	  *error* erroAdr
    ) ;_  Fim do setq
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_group")
    (GRAPHSCR)
    (COMMAND "_.ucs" "")
    (SETVAR "ltscale" 0.0001)
    (PRINC "\n Apaga todas as entidades dentro do retangulo")
    (SETQ pt1 (GETPOINT
		  "\n Clique no canto inferior esquerdo do retangulo"
	      ) ;_  Fim do getpoint
    ) ;_  Fim do SETQ
    (IF	pt1
	(PROGN
	    (SETQ pt3 (GETCORNER pt1
				 "\n Clique no canto superior direito do retangulo"
		      ) ;_  Fim do getcorner
	    ) ;_  Fim do setq
	    (COMMAND "_.osnap" "_none")
	    (IF	pt3
		(PROGN
		    (SETQ pt1ax	(+ (CAR pt1) 0.25)
			  pt1ay	(+ (CADR pt1) 0.25)
			  pt3ax	(- (CAR pt3) 0.25)
			  pt3ay	(- (CADR pt3) 0.25)
			  pt1a	(LIST pt1ax pt1ay 0)
			  pt2a	(LIST pt1ax pt3ay 0)
			  pt3a	(LIST pt3ax pt3ay 0)
			  pt4a	(LIST pt3ax pt1ay 0)
			  pt2	(LIST (CAR pt1) (CADR pt3) 0)
			  pt4	(LIST (CAR pt3) (CADR pt1) 0)
		    ) ;_  Fim do setq
		    (COMMAND "_.pline" pt1 pt2 pt3 pt4 "_c")
		    (SETQ Sel (ENTLAST))
		    (COMMAND "_.erase" "_w" pt1 pt3 "_r" Sel "")
		    (REPEAT 5
			(COMMAND "_.trim" Sel "" "_f" pt1a pt2a	"" "_f"	pt2a pt3a "" "_f" pt3a pt4a
				 "" "_f" pt4a pt1a "" ""
			) ;_  Fim do COMMAND
		    ) ;_  Fim do repeat
		    (COMMAND "_.erase" "_w" pt1 pt3 "_r" Sel "")
		) ;_  Fim do PROGN
	    ) ;_  Fim do IF
	) ;_  Fim do progn
    ) ;_  Fim do if
    (SETQ *error* olderro)
    (SETVAR "ltscale" oldlts)
    (SETVAR "osmode" oldosm)
    (COMMAND "_.ucs" "_p")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==============================================================================
 ;==============================================================================





;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(DEFUN erroAdr (s)
    (PRINC "\n *************   Função Cancelada   **************")
    (SETQ *error* olderro)
    (SETVAR "ltscale" oldlts)
    (SETVAR "osmode" oldosm)
    (COMMAND "_.ucs" "_p")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;




 ;==============================================================================
 ;        Rotina para apagar tudo fora de um retangulo                          
 ;==============================================================================
(DEFUN c:AFR ()
    (sf)
    (PRINC "n                Rotina original escrita por Bob Jones")
    (SETQ oldosm  (GETVAR "osmode")
	  olducs  (GETVAR "ucsname")
	  oldlts  (GETVAR "ltscale")
	  olderro *error*
	  *error* erroAdr
    ) ;_  Fim do setq
    (SETVAR "cmdecho" 0)
    (SETVAR "blipmode" 0)
    (COMMAND "_.undo" "_Group")
    (GRAPHSCR)
    (COMMAND "_.ucs" "")
    (SETVAR "ltscale" 0.0001)
    (PRINC "\n Apaga todas as entidades fora do retangulo")
    (SETQ pt1 (GETPOINT
		  "\n Clique no canto inferior esquerdo do retangulo"
	      ) ;_  Fim do getpoint
    ) ;_  Fim do SETQ
    (IF	pt1
	(PROGN
	    (SETQ pt3 (GETCORNER pt1
				 "\n Clique no canto superior direito do retangulo"
		      ) ;_  Fim do getcorner
	    ) ;_  Fim do setq
	    (COMMAND "_.osnap" "_none")
	    (IF	pt3
		(PROGN
		    (SETQ pt1ax	(- (CAR pt1) 0.25)
			  pt1ay	(- (CADR pt1) 0.25)
			  pt3ax	(+ (CAR pt3) 0.25)
			  pt3ay	(+ (CADR pt3) 0.25)
			  pt1a	(LIST pt1ax pt1ay 0)
			  pt2a	(LIST pt1ax pt3ay 0)
			  pt3a	(LIST pt3ax pt3ay 0)
			  pt4a	(LIST pt3ax pt1ay 0)
			  pt2	(LIST (CAR pt1) (CADR pt3) 0)
			  pt4	(LIST (CAR pt3) (CADR pt1) 0)
		    ) ;_  Fim do setq
		    (COMMAND "_.pline" pt1 pt2 pt3 pt4 "_c")
		    (SETQ Sel (ENTLAST))
		    (COMMAND "_.erase" "_all" "_r" "_c" pt1 pt3 "")
		    (REPEAT 5
			(COMMAND "_.trim" Sel "" "_f" pt1a pt2a	"" "_f"	pt2a pt3a "" "_f" pt3a pt4a
				 "" "_f" pt4a pt1a "" ""
			) ;_  Fim do COMMAND
		    ) ;_  Fim do REPEAT
		    (COMMAND "_.erase" "_all" "_r" "_c" pt1 pt3 "")
		) ;_  Fim do PROGN
	    ) ;_  Fim do IF
	) ;_  Fim do progn
    ) ;_  Fim do if
    (SETQ *error* olderro)
    (SETVAR "ltscale" oldlts)
    (SETVAR "osmode" oldosm)
    (COMMAND "_.ucs" "_p")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do DEFUN
 ;==============================================================================
 ;==============================================================================




 ;==============================================================================
 ;        Rotina  para alterar blocos (menos atributos                          
 ;==============================================================================
(DEFUN c:Mbl ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do SETQ

    (sf)
    (SETVAR "cmdecho" 0)
    (SETVAR "blipmode" 0)
    (COMMAND "_.undo" "_Group")
    (GRAPHSCR)
    (COMMAND "ucs" "")
    (PRINC
	"\nAltera todos os blocos(sem atributos) com parametros do Selecionado"
    ) ;_  Fim do PRINC
    (SETQ selecao (ENTSEL "\n Selecione o bloco padrão ====>>"))
    (IF	(/= selecao nil)
	(PROGN
	    (SETQ entidade (ENTGET (CAR selecao)))
	    (SETQ nome (CDR (ASSOC '0 entidade)))
	    (IF	(= nome "INSERT")
		(PROGN
		    (INITGET 1 "Sim Não")
		    (SETQ rot (GETKWORD "Alterar rotação? (S/N)"))
		    (extraiDadosBloco selecao)
		    (SETQ nome1 (CDR (ASSOC '2 entidade)))
		    (SETQ bllayer (CDR (ASSOC '8 entidade)))
		    (SETQ Selblocos (SSGET "X" (LIST (CONS 0 "INSERT") (CONS 2 nome1))))
		    (SETQ tam  (SSLENGTH selblocos)
			  cont 0
		    ) ;_  Fim do setq
		    (REPEAT tam
			(SETQ lista (ENTGET (SSNAME selblocos cont)))
			(SETQ lista (SUBST (CONS 8 bllayer) (ASSOC 8 lista) lista))
			(SETQ lista (SUBST (CONS 41 blScx) (ASSOC 41 lista) lista))
			(SETQ lista (SUBST (CONS 42 blScy) (ASSOC 42 lista) lista))
			(IF (= rot "Sim")
			    (SETQ lista (SUBST (CONS 50 blAng) (ASSOC 50 lista) lista))
			) ;_  Fim do if
			(ENTMOD lista)
			(SETQ cont (+ 1 cont))
		    ) ;_  Fim do repeat
		) ;_  Fim do progn
		(ALERT "***  Entidade selecianada não e um bloco!!!!  ***")
	    ) ;_  Fim do if
	) ;_  Fim do progn
    ) ;_  Fim do if
    (COMMAND "_.ucs" "_p")
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do dfun
 ;==============================================================================
 ;==============================================================================


 ;======================================================================
 ;             Rotina para desenho de taludes.                          
 ;======================================================================
(DEFUN c:tal ()
    (SETQ olderro *error*
	  *error* errorot
    ) ;_  Fim do setq
    (sf)
    (SETVAR "cmdecho" 0)
    (COMMAND "_.undo" "_Group")
    (SETQ crista (ENTSEL "\nSelecione a crista do talude : "))
    (SETQ w 1)
    (WHILE w
	(IF (NOT crista)
	    (SETQ crista (ENTSEL "\nSelecao vazia. Selecione novamente : "))
	    (SETQ w nil)
	) ;_  Fim do if
    ) ;_  Fim do while
    (IF	(/= (CDR (ASSOC 0 (ENTGET (CAR crista)))) "LWPOLYLINE")
	(PROGN
	    (ALERT
		"A seleção não é uma Polilinha!!!!!!!!!!!!"
	    ) ;_  Fim do alert
	    (EXIT)
	) ;_  Fim do progn
    ) ;_  Fim do if
    (COMMAND "chprop" crista "" "c" "4" "")
    (SETQ pe (ENTSEL "\nSelecione o pé do talude: "))
    (SETQ w 1)
    (WHILE w
	(IF (NOT pe)
	    (SETQ pe (ENTSEL "\nSelecao vazia. Selecione novamente o pé do talude : "))
	    (SETQ w nil)
	) ;_  Fim do if
    ) ;_  Fim do while
    (IF	(/= (CDR (ASSOC 0 (ENTGET (CAR pe)))) "LWPOLYLINE")
	(PROGN
	    (ALERT
		"A seleção não é uma Polilinha!!!!!!!!!!!!!"
	    ) ;_  Fim do alert
	    (EXIT)
	) ;_  Fim do progn
    ) ;_  Fim do if
    (COMMAND "chprop" pe "" "C" "1" "")
    (SETQ dist (GETDIST "\nIndique  o espaçamento <2.00>: "))
    (IF	(NOT dist)
	(SETQ dist 2.00)
    ) ;_  Fim do if
    (COMMAND "chprop" crista "" "c" "bylayer" "")
    (COMMAND "chprop" pe "" "c" "bylayer" "")
    (COMMAND "_.area" "_e" crista)
    (SETQ lcrista (GETVAR "perimeter"))
    (SETQ nsurf (FIX (/ lcrista dist)))
    (SETVAR "surftab1" nsurf)
    (COMMAND "rulesurf" (NTH 1 crista) (NTH 1 pe))
    (SETQ malha (ENTLAST))
    (SETQ vt1 malha)
    (SETQ vt2 malha)
    (SETQ sel1 (SSADD))

    (REPEAT (+ 1 nsurf)
	(SETQ vt2 (ENTNEXT vt2))
    ) ;_  Fim do repeat
    (SETQ cont 1.0)
    (REPEAT (+ 1 nsurf)
	(SETQ vt1 (ENTNEXT vt1))
	(SETQ vt2 (ENTNEXT vt2))
	(COMMAND "_line"
		 (CDR (ASSOC 10 (ENTGET vt1)))
		 (CDR (ASSOC 10 (ENTGET vt2)))
		 ""
	) ;_  Fim do COMMAND
	(IF (= cont -1.0)
	    (COMMAND "_.scale"
		     (ENTLAST)
		     ""
		     (CDR (ASSOC 10 (ENTGET vt1)))
		     "0.35"
	    ) ;_  Fim do COMMAND
	) ;_  Fim do if
	(SETQ cont (* cont -1.0))
    ) ;_  Fim do repeat
    (COMMAND "_.erase" malha "")
    (COMMAND "_.redraw")
    (SETQ *error* olderro)
    (COMMAND "_.undo" "_end")
    (PRINC)
) ;_  Fim do defun
 ;====================================================================
 ;====================================================================


;|«Visual LISP© Format Options» (100 4 1 2 T " Fim do " 80 15 2 0 1 T T nil T)
 ***Don't add text below the comment!***|;

