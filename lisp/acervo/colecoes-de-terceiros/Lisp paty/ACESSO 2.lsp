;;; ***** PROGRESIVAS ****
;;;------------------------------------------------------------------------------
;;; (Txt2PK txt dec)
;;; Subrutina
;;; Valor de entrada: String de numeros, Cantidad Decimales ("12.32" 2  "254369.111" 3 "-123" 1)
;;; Valor de retorno: Mismo numero con "+"                  ("0+012.32" "254+369.111"  "-0+123.0")
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;;------------------------------------------------------------------------------
;;; (prog-txt prog) ;;;NO FUNCIONA BIEN!
;;; uebergabe: progresiva (valor real)
;;; Beispiel: (prog-txt 23.81) => 0+023.810
;;;------------------------------------------------------------------------------

;;; **** FECHAS ******
;;; convierte string a lista, dado el String y un Caracter de separacion.
;;; subrutina desvuelve Lista
;;; (fechahoy2str) => "25/03/2003"
;;;------------------------------------------------------------------------------
;;; sub-subrutina para str2lst: 
;;; separa el 1° elemento de un string a lista
;;; (str-div str c)
;;;------------------------------------------------------------------------------
;;; (fechahoy2lst) => (2002 1 29)
;;;------------------------------------------------------------------------------
;;; (mes2xstr num) => "Januar"
;;;------------------------------------------------------------------------------
;;; (str-addnleftchar "String" 10 "_") => "____String"
;;;------------------------------------------------------------------------------
;;; (str-addnrightchar "String" 10 "_") => "String____"
;;;------------------------------------------------------------------------------
;;; (str-addncentrochar "String" 10 "_") => "__String__"
;;;------------------------------------------------------------------------------
;;; (fecha2ilst) Formato Übergabe: "29/01/2002" "/"
;;;                             => (2002 1 29)
;;;------------------------------------------------------------------------------
;;; sub-subrutina para str2lst: "25/03/2003" -> "25" "03" "2003"
;;; separa el 1° elemento de un string a lista
;;;------------------------------------------------------------------------------
;;; convierte string a lista com numeros interger, 
;;; dado uma String e um Caracter de separacao.
;;; subrutina. 
;;; Ejemplo Input:   (str-intlist "25/04/2003" "/")
;;; desenvolve Lista:         => (25 4 2003)
;;;------------------------------------------------------------------------------
;;; getcantmes
;;; desvuelve cantidad meses entre 2 fechas 
;;; Input: Lista_Fecha_ini Lista_Fecha_fin
;;; (getcantmes (23 1 2003) (25 3 2003)) => (1 2) ; 1=Enero 2=diferencia meses
;;; (getcantmes (23 1 2002) (25 3 2003)) => (13 2) ; 1=Enero 13=diferencia meses
;;;------------------------------------------------------------------------------
;;; (defun getdifmes (f_ini f_fin /)
;;;------------------------------------------------------------------------------
;;; (defun getdifdia (f_ini f_fin /)
;;;------------------------------------------------------------------------------
;;; (defun mtxt (capa color pt txt ang h)
;;;------------------------------------------------------------------------------

;;; ************** STRING LISTS *****************
;;;------------------------------------------------------------------------------
;;; convierte string a lista, dados da String e um Caracter de separacao.
;;; subrutina
;;; desenvolve Lista
;;; (str2lst str char) 
;;; (str2lst "12,34,aaa" ",") => ("12" "34" "aaa")
;;;------------------------------------------------------------------------------
;;; Converte lista de floats a lista de strings.
;;; input: titulo, lista de valores float, quantidade de decimais
;;; return: lista com string incluido carreagem returns
;;; --->>> string listo para MTEXT
;;; (ptLst2str "Titulo" (3.0 2.0 0.0) 2) => "Titulo\P3.0\P2.0"
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (STRISNUM STR) ; => T / NIL
;;; AVERIGUA Se TEXTO E Um NUMERO VALIDO
;;; TEXTO DEVE ESTAR LIMPO ( SIN "+" "," ETC.)
;;; -------------------------------
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (DEFUN GETCHAR (CHARLST TXT / )
;;; INPUT: LISTA DE CARACTERES (LIST "J" "4" "+" ...ETC) TEXTSTRING
;;; RETORNO: CARACTER, P.E.: "+" O NIL
;;; EFECTO: PEDE AO USUARIO APERTAR UMA TECLA DESDE QUE SEJA 
;;;		TECLA ESPECIFICA DENTRO DE UM DADO
;;; EJEMPLO: (SETQ CHAR (GETCHAR (LIST "+" "-" "\R") "INGRESA O <+> O <->: " ))
;;; --------------------------------------------------------------
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; GETTXT->NUM
;;; ENTRADA: TEXTSTRING, STRINGNUMERO
;;; RETORNO: NUMERO FLOAT
;;; PERGUNTA POR NUMERO INSERIDO POR TECLADO, 
;;;                         SIM ENTER, PODE SELECIONAR UM TEXTO
;;; (SETQ NUM (GETTXT->NUM "TEXTO:" "6+333.222")) ;;; => 6333.222
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; ************** PLINES *****************
;;; INPUT: LISTA DE SEGMENTOS DE UNA POLYLINHA
;;; RETURN: LARGURA TOTAL DE UMA LISTA EM REAL
;;; (POLY-LENGTH plist) => 2658.2356
;;;------------------------------------------------------------------------------
;;;;;;;;;; SELECT LWPOLYLINEA
;;; - ENTRADA: STRING (MENSAJE)
;;; - RETORNO: ELE
;;; - EFECTO: NENHUM
;;; - EJEMPLO: (getplin "Selecione LWpolylinha: ")
;;;------------------------------------------------------------------------------
;;; STDJM-GetLenPliniPt
;;; Valor de Ingreso:
;;;   - Lista segmentos de LW-Polylinha (pt1 pt2 [bulge])
;;;   - Punto sobre LW-Polylinea
;;;   - PK-Inicio da LW-Polilinha
;;; Valor de Retorno:
;;;   - Largo (desde 1°VerticePline Basta Ponto) 
;;;   - nil, se Pto no esta sobre Pline
;;;>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
;;; nota: falta definir si pt no esta sobre linea
;;; EXEMPLO: (setq erg (stdjm-GetLenPliniPt ptpk plist 0))
;;;------------------------------------------------------------------------------
;;; (gather '(x1 y1 z1 x2 y2 z2 x3 y3 z3 ...))
;;;  => ((x1 y1 z1)(x2 y2 z2)(x3 y3 z3) ...)
;;;------------------------------------------------------------------------------
;;; (intersect ename1 ename2) => intpt o nil
;;;------------------------------------------------------------------------------
;;; Busca Vertice com a mesma Coordenada e elemina da lista
;;; Input: Lista de Polilinha
;;; Return: Lista modificada de Polilinha
;;; Nota: guarda os Valores.
;;;(ElimDobleVert lstpl)
;;;------------------------------------------------------------------------------


;;; ************* SELECAO SETS *************

;;;------------------------------------------------------------------------------
;;; -->>> e-Lista con SUBENTIDADES do bloco
;;; (jm-allsubents blkname)
;;;------------------------------------------------------------------------------
;;;; -->>> e-Lista con ATTE'S del bloque
;;; (jm-attsubs blkname)
;;;------------------------------------------------------------------------------
(defun jmf ()
(if (> (std-date-diff (std-datlst->cdate (list 2003 5 5 23 59 30)) 20040507.0) 0)
nil T)

);;;;fin func
(defun GetAzimutSeg ( SEG / )
(vl-string-subst "°" "d" 
(angtos (+ (- (* 2 PI)(std-seg-angle SEG )) (/ pi 2)) 1 4))
);;; END FUNC
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; SELECIONA TODAS LINHAS OU POLYLINHAS DE UM LAYER ESPECIFICADO
;;; ENTRADA: LAYER
;;; SAIDA: SELECTION SET
;;; (selAll_L-Pl_onLay "LAYERNAME")
;;;------------------------------------------------------------------------------
;;;; seleciona todos Blocos on Layers que comecem com C_* o CN_*
;;; (selAll_BLKs_onLays) => LISTA ENAMES
;;;------------------------------------------------------------------------------
;;;; select all Lins o plines on Layer especificado
;;; (selAll_Lins_on_Lay lay) => LISTA ENAMES de LINES O PLINES bajo "LAY"
;;;;-----------------------------------------------------------------
;;; (lastent) ;;; => Returns last main entity 
;;;------------------------------------------------------------------------------
;;; Desenvolve todo os elementos da tabela seguindo condicoes
;;; (por ejemplo: tabla de bloques)
;;; >>> (all-table-entries-if table condition)
;;;------------------------------------------------------------------------------
;;; busca no blocoe todas as subentidades,
;;; que coincidem da condicion especificada:
;;; >>> (gather-subents-if blockname condicion)
;;;------------------------------------------------------------------------------









;;; ************* BLOQUES **************
;;;------------------------------------------------------------------------------
;;; ROTINA PARA TROCARR LAYER AO MESMO LAYER QUE ELEMENTOS DO BLOCO:
;;; BUSCAR BLOQUE ESPECIFICO, LEER CAPA DEL 1° ELEMENTO
;;; MOVER SELECAO A ESTE LAYER
;;; desenvolve nada
;;; (CHLAYBLK nombreblk lay)
;;;------------------------------------------------------------------------------



;;; ************* FILES **************
;;; UBICACION ARCHIVOS
(defun getruta (/)
"c:/sertodem/EXPRESS"
)
;;;------------------------------------------------------------------------------


;;; ************* TEXTOS **************
;;; (txtJM LAYER color pt txt ang h) >>>> ENTMAKE TEXTO
;;;------------------------------------------------------------------------------
;;; (mtxtJM LAYER color pt txt ang h) >>>> ENTMAKE MTEXT
;;;------------------------------------------------------------------------------

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;***************************************************************************;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;



;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (fechahoy2lst) => (2002 1 29)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun fechahoy2lst (/ cd)
  (setq cd (rtos (getvar "cdate") 2 6))
  (append
    (list (atoi (substr cd 1 4)))
    (list (atoi (substr cd 5 2)))
    (list (atoi (substr cd 7 2)))
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (mes2xstr num) => "Januar"
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun mes2xstr	(d /)
  (nth (1- d)
       '("Enero"      "Febrero"	   "Marzo"	"Abril"
	 "Mayo"	      "Junio"	   "Julio"	"Agosto"
	 "Setiembre"  "Oktubre"	   "Noviembre"	"Diciembre"
	)
  )
)            
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun fechahoy2str (/ cd)
  (setq cd (rtos (getvar "cdate") 2 6))
  (strcat
    (str-lpadz (substr cd 7 2) 2)
    "."
    (str-lpadz (substr cd 5 2) 2)
    "."
    (substr cd 1 4)
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (str-addnleftchar "String" 10 "_") => "____String"
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun str-addnleftchar	(str nl ch /)
  (cond
    ((= (strlen str) nl) str)
    ((> (strlen str) nl) (substr str 1 nl))
    (1 (str-addnleftchar (strcat ch str) nl ch))
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (str-addnrightchar "String" 10 "_") => "String____"
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun str-addnrightchar (str nl ch /)
  (cond
    ((= (strlen str) nl) str)
    ((> (strlen str) nl) (substr str 1 nl))
    (1 (str-addnrightchar (strcat str ch) nl ch))
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (str-addncentrochar "String" 10 "_") => "__String__"
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun str-addncentrochar (str nl ch / len)
  (setq len (strlen str))
  (cond
    ((= len nl) str)
    ((> len nl) (substr str (/ (- len nl) 2) nl))
    (1
     (str-addnrightchar
       (str-addnleftchar str (+ len (/ (- len nl) 2)) ch)
       nl
       ch
     )
    )
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (fecha2ilst) Formato Übergabe: "29/01/2002" "/"
;;;                             => (2002 1 29)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun fecha2ilst (fecha / cd)
  (setq cd (rtos fecha 2 6))
  (append
    (list (atoi (substr cd 1 4)))
    (list (atoi (substr cd 5 2)))
    (list (atoi (substr cd 7 2)))
  )
)

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; sub-subrotina para str2lst:"
;;; separa o 1° elemento de uma string a lista
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun str-div (str c / i l)
  (setq i 1)
  (setq l (strlen str))
  (while (and (<= i l) (/= (substr str i 1) c))
    (setq i (1+ i))
  )
  (list (substr str 1 (1- i)) (substr str (1+ i)))
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; convierte string a lista com numeros interger, 
;;; dado a String e um Caracter de separacion.
;;; subrotina. Exemplo Input:   "25/04/2003" "/"
;;; desenvolve Lista:         => (25 4 2003)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun str-intlst (str char / hlp lst)
  (if (/= str "")
    (progn
      (setq hlp (str-div str char))
      (setq lst (list (atoi (car hlp))))
      (while (/= (cadr hlp) "")
	(setq hlp (str-div (cadr hlp) char))
	(setq lst (cons (atoi (car hlp)) lst))
      )
    )
  )
  (setq lst (reverse lst))
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; getcantmes
;;; desenvolve cantidad meses entre 2 fechas 
;;; Input: Lista_Fecha_ini Lista_Fecha_fin
;;; (getcantmes (23 1 2003) (25 3 2003)) => (1 2) ; 1=Enero 2=diferencia meses
;;; (getcantmes (23 1 2002) (25 3 2003)) => (13 2) ; 1=Enero 13=diferencia meses
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun getdifmes (f_ini f_fin /)
  (+ (- (cadr f_fin) (cadr f_ini))
     (* 12 (- (caddr f_fin) (caddr f_ini)))
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun getdifdia (f_ini f_fin /)
  (+ (* (- (caddr f_fin) (caddr f_ini)) 366)
     (-
       (+ (* (cadr f_fin) 30.5) (car f_fin))
       (+ (* (cadr f_ini) 30.5) (car f_ini))
     )
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;



;;; ************** STRING LISTS *****************



;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (str2lst str char)
;;; converte string a lista, dado a String e um Caracter de separacao.
;;; subrutina desvuelve Lista
;;; (fechahoy2str) => "25/03/2003"
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun str2lst (str char / hlp lst)
  (if (and (/= str "")(/= str nil))
    (progn
      (setq hlp (str-div str char))
      (setq lst (list (car hlp)))
      (while (/= (cadr hlp) "")
	(setq hlp (str-div (cadr hlp) char))
	(setq lst (cons (car hlp) lst))
      )
    )
  )
  (if (and (/= lst "")(/= lst nil))
  (setq lst (reverse lst))
  )
)

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (txt2pk txtOnum dec simb)
;;; troca de entrada de <txt dec> a <txt dec comasimb>
;;; entrada: txt o numero, quantidade decimais, simbolo de virgula
;;; Valor de retorno: Mesmo numero com "+"                  
;;; (txt2pk "-12,32" 2 ".") => "-0+012.32" 
;;; (txt2pk 254369.1117 3 ",") => "254+369,112" 
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun Txt2PK (txt dec comasimb / aux2 auxtxt minu txt valordecs valorinteger )
  (setq minu 1)
  (if (not (STRINGP txt)) (setq txt (rtos txt 2 dec)) )
  (if (not (strisnum txt)) (std-error "txt2pk->texto esperado!?!!!"))
  (setq txt (std-strchg txt "," "." ))
  (if (< (atof txt) 0) (progn (setq minu -1)(setq txt (std-strchg txt "-" ""))))
  (setq auxtxt (std-strsplit txt "."))
  (if (> (length auxtxt) 2) (std-error "Txt2PK: String nao e um numero valido..." ))
  (if (= (length auxtxt) 2)
  (setq valordecs (rtos (atof (strcat "0." (cadr auxtxt)) ) 2 dec))
  (setq valordecs (rtos 0.0 2 dec)) );endif
  (if (= (std-firstchar valordecs) "0") (setq valordecs (std-string-subst "" "0" valordecs)))
  (setq valorinteger (rtos (atof (car auxtxt)) 2 0))
  (cond
   ((= valorinteger "") 
   			(setq valorinteger "0+000") )
   ((= (strlen valorinteger) 1 ) 
   			(setq valorinteger (strcat "0+00" valorinteger)))
   ((= (strlen valorinteger) 2 )
   			(setq valorinteger (strcat "0+0" valorinteger)))			
   ((= (strlen valorinteger) 3 )
   			(setq valorinteger (strcat "0+" valorinteger)))
   ((= (strlen valorinteger) 4 )
   			(setq valorinteger (strcat (std-firstchar 	valorinteger) "+" 
   			(std-lastchars 3 valorinteger))))	
   ((> (strlen valorinteger) 4 )
   			(setq valorinteger (strcat (std-str-n 3 valorinteger)	"+" (std-lastchars 3 valorinteger))) )
   ((T) (print "txt2pk: deu...?...merda....")
   )
	);;;End cond
(if (= minu -1) (strcat "-" valorinteger (std-strchg valordecs "." comasimb))
(strcat valorinteger (std-strchg valordecs "." comasimb)))
)	;;;FIN FUNC			
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (E2Pk numtxt incr) ; 11.12.2004 Rev1
;;; 2530				->Estaca (valorpkreal 0 nil)
;;; 2530+12,5		->Estaca (valorpkreal 1 ",")
;;; 2530+2.55		->Estaca (valorpkreal 2 ".")
;;; 253+025.5   -> (nil "errmsg")
;;; 253025.5 	-> (nil "errmsg")
;;; Return: (valorpkreal decs simdec)
;;;   o 		(nil "errmsg")
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun e2pk (numtxt incr / decs dif erg len minu numtxt posmas pospunto simdec strdiv symdec)
(setq minu 1)
(setq erg (list T T))
(setq numtxt (std-strchg	numtxt " " ""))
(if (std-stripos "," numtxt) 
	(setq simdec ",")
	(setq simdec "."))
(setq len (strlen numtxt))
(setq numtxt (std-strchg numtxt "," "."))
(if (std-stripos "." numtxt)
 (setq decs (- (1- len) (std-stripos "." numtxt))) (setq decs 0))
(cond
   ((std-stricmp "PI=" numtxt) (setq numtxt (std-string-subst "" "PI=" numtxt)) ) 
   ((std-stricmp "PC=" numtxt) (setq numtxt (std-string-subst "" "PC=" numtxt))  ) 
   ((std-stricmp "PC." numtxt) (setq numtxt (std-string-subst "" "PC." numtxt))  )
   ((std-stricmp "PT=" numtxt) (setq numtxt (std-string-subst "" "PT=" numtxt))  ) 
   ((std-stricmp "PT." numtxt) (setq numtxt (std-string-subst "" "PT." numtxt))  )
   ((std-stricmp "ESTACA" numtxt) (setq numtxt (std-string-subst "" "ESTACA" numtxt)) ) 
   ((std-stricmp "EST." numtxt) (setq numtxt (std-string-subst "" "EST." numtxt)) ) 
   ((std-stricmp "EST" numtxt) (setq numtxt (std-string-subst "" "EST" numtxt)) )    
   ((std-stricmp "E-" numtxt) (setq numtxt (std-string-subst "" "E-" numtxt)) )
   ((std-stricmp "E=" numtxt) (setq numtxt (std-string-subst "" "E=" numtxt)) )
   ((std-stricmp "E" numtxt) (setq numtxt (std-string-subst "" "E" numtxt)) )
   ((std-stricmp "E." numtxt) (setq numtxt (std-string-subst "" "E." numtxt)) )
	);end cond
(if (std-stricmp "-" numtxt) (progn 
   (setq numtxt (std-string-subst "" "-" numtxt)) (setq minu -1)))
(setq posmas (std-stripos 	"+" numtxt))
(setq pospunto (std-stripos 	"." numtxt))
(if (and posmas (not pospunto)) (progn 
   (setq pospunto (1+ (strlen numtxt))) ))
(cond 
	((and posmas pospunto) (setq dif (- pospunto posmas )) 
		(if (not (and (> (- pospunto posmas 1) 0) (< (- pospunto posmas 1) 3)))
   		(setq erg (list nil "\n<+> NAO TEM A POSICAO CORRETA"))));endif
((and (not pospunto) posmas)
	(if (not (and (> (- len posmas 1) 0) (< (- len posmas 1) 3))) 
       (setq erg (list nil "\n<+> NAO TEM A POSICAO CORRETA")))) ;endif
((and (not posmas) pospunto) 
       (setq erg (list nil "\nVALOR COM DECIMAIS DEVE TER UM <+> !"))) ;
);end cond
(if (car erg) (progn
(setq strdiv (std-strsplit numtxt "+"))
(setq erg (* (atof (car strdiv)) incr))
(if (cadr strdiv) (setq erg (+ (atof (cadr strdiv)) erg) ))
(setq erg (* erg minu))
(list erg decs simdec)
) erg)
);;;End Defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (pk2e pk incr decs simb)
;;; Return: texto estaca (con 1000 de incr -> pk) 
(defun pk2e ( pk incr decs simb / adelante atraz count pk )
(cond 
((<= incr 10) (setq count 1))
((<= incr 100) (setq count 2))
((<= incr 1000) (setq count 3))
);end cond
(setq pk (atof (rtos pk 2 6)))
(if (and (= (* (fix (/ pk incr)) incr) pk) (= decs 0))
 (if (= incr 1000)
   (strcat (rtos (fix (/ pk incr)) 2 0) "+000")
   (rtos (fix (/ pk incr)) 2 0))
(progn ;else:
(setq adelante (rtos (fix (/ pk incr)) 2 0))
(setq atraz (std-strsplit (rtos (- pk (* (fix (/ pk incr)) incr) ) 2 decs) "."))
(while (< (strlen (car atraz)) count)
  (setq atraz (list (strcat "0" (car atraz)) (cadr atraz))));end while
 (if (cadr atraz)
 (strcat adelante "+" (car atraz) simb (cadr atraz))
 (strcat adelante "+" (car atraz)));end if
));endif
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;(prog-txt prog) ;;;<<< anda mal! >>neu: (txt2pk txtOnum dec simb)
;;; uebergabe: progresiva (valor real)
;;; Beispiel: (prog-txt 23.81) => 0+023.810
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun prog-txt (prog / hilftxt txt i z largo)  		
(setq txt (rtos prog 2 0))
(setq largo (strlen txt))
(setq hilftxt (substr txt (- largo 2 ))) ;cadena atraz
(setq txt (substr txt 1 (- largo 3 )))	;cadena adelante
(setq txt (strcat txt "+" hilftxt))
);;;end func prog-txt
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;; ************* PLINES *************
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; Busca Vertice com a mesma Coordenada e a elemina da lista
;;; Input: Lista de Polilinha
;;; Return: Lista modificada de Polilinha
;;; Nota: guarda os Valores de Bulge, sem hay.
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun ElimDobleVert (lstpl / i)
(setq i 0)
(while (nth i lstpl)
    (if ( std-set-equal-p (car (nth i lstpl)) (cadr (nth i lstpl)) )
    ;;;->OK (setq lstpl (std-remove (nth i lstpl) lstpl))
    (setq lstpl (STD-DELPOS i lstpl))
    (setq i (1+ i))
    );;; End If
);;; End While
lstpl
);;; End Func
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; INPUT: POLYLINHA (ENAMELIST!)
;;; RETURN: LARGO TOTAL DE LA LISTA
(defun POLY-LENGTH (plist /)
(apply '+ ; suma de todo los largos de los segmentos
(mapcar 'std-seg-length plist; recto o curvado
))
)

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; MAKE-SEG-3P
;;; CRIA DE 3 PONTOS UM SEGMENTO ARCO (PT1 PT2 BULGE)
;;; INPUT: 3 PONTOS
;;; RETURN: SEGMENTO ARCO (PT1 PT2 BULGE)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun make-seg-3p (p1 p2 p3 / ctr)
  (if (setq ctr
	     ;; circumference center
	     (apply 'inters
		    (append (vec-midnorm p1 p2)
			    (vec-midnorm p2 p3)
			    '(nil)
		    )
	     )
      )
    (std-make-seg
      p1
      p3
      (std-arc->bul
	p1
	p3
	(list ctr)
	(std-ccw p1 p2 p3)
      )
    )
    (std-make-seg p1 p3 nil)
  )
)
(defun vec-midnorm (p1 p2 / mid ang)
  (setq	mid (std-midpt p1 p2)
	ang (+ *pi/2* (angle p1 p2))
  )
  (list	(polar mid ang 0.5)
	(polar mid ang -0.5)
  )
);;;ENF FUNCAO
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; STDJM-GetLenPliniPt
;;; Valor de Ingreso:
;;;   - Lista segmentos de LW-Polylinha (pt1 pt2 [bulge])
;;;   - Ponto sobre LW-Polylinha
;;;   - PK-Inicio da LW-Polilinha
;;; Valor de Retorno:
;;;   - Largura (desde 1°VerticePline falta Ponto) 
;;;   - nil, se Pto nao esta sobre Pline
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; nota: falta definir se pt nao esta sobre linha
;;; EXEMPLO: (setq erg (stdjm-GetLenPliniPt ptpk plist 0))
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

(defun stdjm-GetLenPliniPt ( ptpk plist pkini / ang auxbulge i pkini)
 
;;;---------

  (setq i 0)


(if (NOT (EQUAL (CAR(nth i plist)) ptpk 0.000001) ) ;;; 1° IF
;;; >>> 1° IF
(progn
;;;--- WHILE HASTA FINAL P-LISTA O PTPK=NIL:
  (while (and (nth i plist) ptpk)

    (if (STD-SEG-STRAIGHT-P (nth i plist))
      ;;; >>> SI LINEA:
      (progn
	(setq ang (- (angle (car (nth i plist)) (cadr (nth i plist)))
		     (angle (car (nth i plist)) ptpk)
		  )
	)
       ;;; >>> Se PONTO SOBRE SEGMENTO:
	(if
	  (and (<= ang 0.0000001)
	       (<= (distance (car (nth i plist)) ptpk)
		   (distance (car (nth i plist)) (cadr (nth i plist)))
	       )
	  )
       ;;; >>> SE PONTO SOBRE SEGMENTO, ENTAO INSERE ULTIMA LARGURA:
	   (progn
	     (setq pkini (+ pkini (distance (car (nth i plist)) ptpk)))
	     (setq ptpk nil)
	   ) ;;; >>> SE NAO, SEGUI INSERINDO LARGURA AO SEGMENTO:
	   (progn
	     (setq pkini (+ pkini
			    (distance (car (nth i plist))
				      (cadr (nth i plist))
			    )
			 )
	     )
	   )
	)
	

      ) 
      ;;; <<< END SIM LINHA
      ;;; >>> BEGIN SINO BULGE:
      (progn

	(setq auxBulge (- (caddr (nth i plist))
			  (STD-SEG-BULGE
			    (make-seg-3p
			      (car (nth i plist))
			      ptpk
			      (cadr (nth i plist))
			    )
			  )
		       )
	)
       ;;; >>> IF PONTO SOBRE SEGMENTO:
	(if (and (<= auxBulge 0.000001)
		 (< (distance (car (nth i plist)) ptpk)
		    (distance (car (nth i plist)) (cadr (nth i plist)))
		 )
	    )
	  (progn
		 (setq pkini (+	pkini
				(STD-SEG-LENGTH
				  (list	(car (nth i plist))
					ptpk
					(STD-ARC->BUL
					  (car (nth i plist))
					  ptpk
					  (STD-SEG->CIR (nth i plist))
					  -0.1
					)
					-1
				  )
				)
			     )
		 )
		 (setq ptpk nil)
	  )
	  ;;; >>> IF PONTO NAO SOBRE SEGMENTO:
	  (progn
		 ;(print "no")
		 (setq pkini (+ pkini (STD-SEG-LENGTH (nth i plist))))
	  )
	)
      )
    )
    ;;; <<< SE LINHA O BULGE
    (setq i (1+ i))
  )
;;;; END WHILE FALTA FINAL P-LISTA O PTPK=NIL:
)
;;;<<< END 1° IF 
) 
pkini
) ;;; END FUNCTION
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun gather (ls len / tmp rl i)
  (setq i 0)
  (foreach item	ls
    (setq tmp (append tmp (list item)))
    (setq i (1+ i))
    (if	(zerop (rem i len))
      (setq rl	(cons tmp rl)
	    tmp	nil
      )
    )
  )
  (reverse rl)
)

;;;(gather '(x1 y1 z1 x2 y2 z2 x3 y3 z3 ...))
;;;  => ((x1 y1 z1)(x2 y2 z2)(x3 y3 z3) ...)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun intersect (ent1 ent2 / ar)
  (vl-load-com)
  (setq	ar
	 (vlax-invoke-method
	   (vlax-ename->vla-object ent1)
	   'IntersectWith
	   (vlax-ename->vla-object ent2)
	   acExtendNone
	 )
  )
  (if
    (/=	-1
	(vlax-safearray-get-u-bound
	  (vlax-variant-value ar)
	  1
	)
    )
     (gather
       (vlax-safearray->list
	 (vlax-variant-value ar)
       )
       3
     )
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; Convierte lista de floats a lista de strings.
;;; input: titulo, lista de valores float, cantidad de decimales
;;; return: lista con string incluido carriage returns
;;; --->>> string listo para MTEXT
;;; (ptLst2str "Titulo" (3.0 2.0 0.0) 2) => "Titulo\P3.0\P2.0"

(defun ptLst2str (tit li decs / i str)
(setq str (strcat tit "\\PN: "))
(setq str (strcat str (rtos (nth 1 li) 2 decs) "\\PE: " ))
(setq str (strcat str (rtos (nth 0 li) 2 decs) "\\P" ))
(if (/= (nth 2 li) 0)
  (setq str (strcat str "Z: "(rtos (nth 2 li) 2 decs)  ))  )
str
) ;;; End ptLst2str
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;


;;; *********** SELECCION SETS ************


;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;-->>> e-Lista con SUBENTIDADES del bloque
(defun jm-allsubents (blkname / )
(gather-subents-if blkname
 '(lambda(data / ) 
      (wcmatch (cdr(assoc 0 data)) "*" )
  )    
)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;-->>> e-Lista con ATTE'S del bloque
(defun jm-attsubs (blkname / )

(gather-subents-if blkname
 '(lambda(data / ) 
      (wcmatch (cdr(assoc 0 data)) "ATTDEF" ) 
  )    
)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;



;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; SELECCIONA TODAS LINEAS O POLYLINEAS DE UNA CAPA ESPECIFICADA
;;; ENTRADA: LAYER
;;; SALIDA: SELECTION SET
;;; (selAll_L-Pl_onLay "LAYERNAME")
;;;------------------------------------------------------------------------------
(defun selAll_L-Pl_onLay (Lay / eles)
  (if (setq eles
	     (std-sslist
	       (ssget "x"
		      (list
			(cons -4 "<and")
			(cons -4 "<or")
			(cons 0 "LINE")
			(cons 0 "LWPOLYLINE")
			(cons -4 "or>")
			(cons 8 Lay)
			(cons -4 "and>")


		      )
	       )
	     )
      )
    eles
  )
;;; end if
)
;;; END selAll_L-Pl_onLay
;;;------------------------------------------------------------------------------
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;;; select all Bloques on Layers que empiezen con C_* o CN_*

(defun selAll_BLKs_onLays ( / eles)
  (if (setq eles
	     (std-sslist
	       (ssget "x"
		      (list
			(cons -4 "<and")
			(cons 0 "INSERT")
			(cons -4 "<OR")
			(cons 8 "C_*") 
			(cons 8 "CN_*")
			(cons -4 "OR>")
			(cons -4 "and>")
		      )
	       )
	     )
      )
    eles
  )
;;; end if
)
;;; END FUNC
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;; select all Lins o plines on Layer especificado
;;; (selAll_Lins_on_Lay lay) => LISTA ENAMES de LINES O PLINES bajo "LAY"
;;;;----------------------------------------------------------------- 
(defun selAll_Lins_on_Lay (lay / eles)
  (if (setq eles
	     (std-sslist
	       (ssget "x"
		      (list
			(cons -4 "<and")
			(cons 8  lay)
			(cons -4 "<OR")
			(cons 0 "LINE")
			(cons 0 "LWPOLYLINE")
			(cons -4 "OR>")
			(cons -4 "and>")
		      )
	       )
	     )
      )
    eles
  )
;;; end if


);;;END FUNC



;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun lastent (/ a b) ;;;Returns last main entity 
  (if (setq a (entlast))         ;;;Gets last main entity 
    (while (setq b (entnext a))  ;;;If subentities follow, loops

                                 ;;;until there are no more 

      (setq a b)                 ;;;subentities 

    ) 
  ) 
  a                              ;;;Returns last main entity 

)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Desvuelve todo los entries del table segun condicion
; (por ejemplo: tabla de bloques)
(defun all-table-entries-if
  (table condition / rover list-of-entries)
  (setq rover(tblnext table 'T))
  (while rover
    (if(apply condition(list rover))
      (setq list-of-entries
        (cons(cdr(assoc 2 rover))list-of-entries)
      )
    )
    (setq rover(tblnext table))
  )
  list-of-entries
)
                  
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; busca en bloque todo los subentidades,
; que coinciden de la condicion especificada:
(defun gather-subents-if (blockname condicion / rover subents)
  (setq rover (cdr(assoc -2(tblsearch"block"blockname))) )
  (while rover
    (if(apply condicion(list(entget rover)))
      (setq subents(cons rover subents))
    )
    (setq rover(entnext rover))
  )
  subents
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;


;;; ************* BLOQUES **************
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; RUTINA PARA CAMBIAR CAPA AL MISMO LAYER QUE ELEMNTOS DEL BLOQUE:
;;; BUSCAR BLOQUE ESPECIFICO, LEER CAPA DEL 1° ELEMENTO
;;; MOVER SELECTIONSET A ESE LAYER
;;; J.MEYER, 11/06/2003 
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun CHLAYBLK (nombreblk lay / elem i)
(if (setq elem
                (std-sslist 
                (ssget "x" 
                 (list
                      (cons -4  "<and")
                        (cons 0  "INSERT")
                        (cons 2  nombreblk)
                      (cons -4  "and>")
                
                  )
                )
                )
                )
(progn
(foreach i elem (std-entchg i nil 8 lay)) 
));;; end if
) ;;; END CHLAYBLK           
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;




;;; ************* TEXTOS **************
(defun txtJM (capa color pt txt ang h /)
  (entmake
    (list (cons 0 "TEXT")
	  (cons 1 txt)
	  (cons 7 "Padrao")
	  (cons 8 capa)
	  (cons 10 pt)
	  (cons 11 pt)
	  (cons 40 h)
	  (cons 50 ang)
         (cons 62 color)
	  (cons 72 4)
	  (cons 73 1)
    )
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun mtxtJM (capa color pt txt ang h /)
    (entmake
     (list
       (cons 0 "MTEXT") ;generate initial MTEXT property list
              (CONS 8 CAPA)
                     (cons 62 COLOR) ;color
       (cons 100 "AcDbEntity")

       (cons 100 "AcDbMText") ;generic extrusion direction
              (cons 10 pt) ;loc (translate if in UCS)
       (cons 11 PT) ;X tilt for current ucs
       (cons 40 h) ;text height
       (cons 1 TXT) ;initial value
       (cons 7 "Padrao") ;style pointer

       (cons 50 ang) ;angle

     ) ;end MTEXT properties list
    )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun mtxtJMMIDDLE (capa color pt txt ang h /)
    (entmake
     (list
       (cons 0 "MTEXT") ;generate initial MTEXT property list
        (CONS 8 CAPA)
        (cons 62 COLOR) ;color

       (cons 100 "AcDbEntity")
       (cons 100 "AcDbMText") ;generic extrusion direction
               (cons 10 pt) ;loc (translate if in UCS)
       ;(cons 11 PT) ;X tilt for current ucs
       (cons 40 h) ;text height
       (cons 1 TXT) ;initial value
       (cons 7 "Padrao") ;style pointer
       (CONS 71 5)

       (cons 50 ang) ;angle

     ) ;end MTEXT properties list
    )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; GETTXT->NUM
;;; ENTRADA: TEXTSTRING, STRINGNUMERO
;;; RETORNO: NUMERO FLOAT
;;; PREGUNTA POR NUMERO INGRESADO POR EL TECLADO, 
;;;                         SI ENTER, PIDE SELECCIONAR UN TEXTO
;;; (SETQ NUM (GETTXT->NUM "TEXTO:" "6+333.222")) ;;; => 6333.222
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun gettxt->num (txt1 txt2 / str )
  (setq str (getstring (strcat "\n" txt1 )))
;;;;---STR SELECT TEXTO:
  (if (/= str "")
    (progn       
       (setq str (std-string-subst "" "+" str))
       (setq str (std-string-subst "." "," str))
    )
    (progn
      (setq str (std-entsel (strcat "\n" txt2 )))
      (while (not (std-entity-type-p str "TEXT") )
	(print "Intentelo de nuevo o ESC para salir!")
	(setq str (std-entsel (strcat "\nOtra vez: " txt2 )))
      )
       (SETQ STR (std-gettextval STR) )
       (setq str (std-string-subst "" "+" str))
       (setq str (std-string-subst "." "," str))
    )
  ) ;;; end if
  (print str)
(if (not (strisnum str))
 (std-error "Valor ingresado no es numerico")
)
(atof str)
);;; end func
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;; SELECT LWPOLYLINEA
;;; - ENTRADA: STRING (MENSAJE)
;;; - RETORNO: ELE
;;; - EFECTO: NINGUNO
;;; - EJEMPLO: (getplin "Selecione LWpolylinea: ")
;;; -------------------------
(defun getplin ( str / eleCL)

  (setq eleCL (std-entsel (strcat "\n\n" str)))
  (while (not (std-entity-type-p eleCL "LWPOLYLINE"))
    (prinC "\n\nIntentelo de nuevo o ESC para salir!")
    (setq eleCL (std-entsel (strcat "\n\n" str)))
  )
eleCL
) ;;; end func
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;; SELECT LWPOLYLINEA POLYLINE o LINE
;;; - ENTRADA: STRING (MENSAJE)
;;; - RETORNO: ELE
;;; - EFECTO: NINGUNO
;;; - EJEMPLO: (getplinolin "Selecione LWpolylinea: ")
;;; -------------------------
(defun getplinOlin ( str / eleCL)
  (setq eleCL (std-entsel (strcat "\n\n" str)))
  (while (not (or (std-entity-type-p eleCL "LWPOLYLINE")
  								(std-entity-type-p eleCL "LINE")
  								(std-entity-type-p eleCL "POLYLINE")))
    (prinC "\n\nIntentelo de nuevo o ESC para salir!")
    (setq eleCL (std-entsel (strcat "\n\n" str)))
  )
eleCL
) ;;; end func
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (DEFUN GETCHAR (CHARLST TXT / )
;;; INPUT: LISTA DE CARACTERES (LIST "J" "4" "+" ...ETC) TEXTSTRING
;;; RETORNO: CARACTER, P.E.: "+" O NIL
;;; EFECTO: PIDE AL USUARIO APRETAR UNA TECLA HASTA QUE ES LA 
;;;		TECLA ESPECIFICA DENTRO DE UN RANGO DADO
;;; EJEMPLO: (SETQ CHAR (GETCHAR (LIST "+" "-" "\R") "INGRESA O <+> O <->: " ))
;;; --------------------------------------------------------------
(DEFUN GETCHAR (charlst TXT / OK ) ;;; charlist = ("j" "4" "+" ...etc)
(PRINC (STRCAT "\n\n" TXT))
(SETQ OK NIL)
   (WHILE (NOT OK)  
   (SETQ CHAR (grread nil 10)) ;;; -> (2 43) = (TECLADO ASCIVALOR)
      (FOREACH I CHARLST 
      (IF (= (ASCII I) (CADR CHAR)) (SETQ OK T))
      );;; END FOREACH
   ) ;;; END WHILE

CHAR      
) ;;; END FUNC
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun geom:make-seg-3p (p1 p2 p3 / ctr)
  (if (setq ctr
        ;; circumference center
        (apply 'inters (append (geom:vec-midnorm p1 p2)
                               (geom:vec-midnorm p2 p3)
                               '(nil))))
     (std-make-seg p1 p3
       (std-arc->bul p1 p3 (list ctr)
                     (std-ccw p1 p2 p3)))
     (std-make-seg p1 p3 nil)))

(defun geom:vec-midnorm (p1 p2 / mid ang)
  (setq mid (std-midpt p1 p2)
        ang (+ *pi/2* (angle p1 p2)))
  (list (polar mid ang 0.5)(polar mid ang -0.5)))
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (SelectTxtEstaca increment)
;;; 2530				->Estaca
;;; 2530+12,5		->Estaca
;;; 2530+2.5		->Estaca
;;; 253+025.5   -> da ERROR
;;; 253025.5 	-> da ERROR
;;; Aceita Valor teclado o seleccion de un texto.
;;; Lo convierte en float de pk real
(defun SelectTxtEstaca ( incr / decs erg len numtxt poscoma posmas pospunto simdec strdiv )
  (setq
    numtxt    (getstring
	  "\nPK / Estaca inicial o ENTER para selecionar Texto: "	))
;;;;
  (if (= numtxt "")
    (progn
     (setq numtxt (std-entsel "\nSelecione TEXTO Estaca: "))
          (while (not (or (std-entity-type-p numtxt "TEXT") (= numtxt "\e")))
        	(print "\nIntentelo de nuevo o ESC para salir!")
        	(setq numtxt (std-entsel "\nSelecione TEXTO Estaca: ")));end while
;;; IF NIL: QUIT
  		(if (or (= numtxt nil) (= numtxt "\e"))
    	(STD-ERROR "\nCANCELADO POR USUARIO") );enf if
    (setq numtxt (std-getval 1 numtxt )) 
   ) );end if else 
(E2Pk numtxt incr)
);;;End Defun

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (SelAlignSelEst incr)
;;; Selecionar Alineamiento,
;;; Selecionar Estaca <Clic sobre texto o escribiendo>, 
;;; Definir punto sobre Alineamiento que corresponde a estaca
;;; Return: Lista (enameCL pt Pkini pkpt)
;;; ((<Entity name: 428092b8> (-837.514 -143.927 0.0)) 
;;; 														(-842.473 -145.988 0.0) 154604.0 154620.0)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun SelAlignSelEst (incr / elecl lenini pk pt )
;;;;---ELECL - SELECT ALINEAMIENTO: (entidad clic-pto)
(setq eleCL (std-entsel "\nSelecione Alinhamento: "))			
(while (not (std-entity-type-p eleCL "LWPOLYLINE"))
  (print "\nTente-o de novo ou ESC para sair! ")
  (setq eleCL (std-entsel "\nSelecione Alinhamento: "))  )
;; select text estaca o valor  
(if (not (setq pk (SelectTxtEstaca incr))) (std-error "\nSelAlignSelEst->SelectTxtEstaca: ReturnValue!"))
;select punto sobre pline q corresponde a estaca dada:
(if (not (setq pt (std-get1point	"\nSelecione Ponto que corresponde a Estaca selecionada: ") )) 
			(std-error "\nRequer Ponto arriba o Pline "))
;;; fin selection
(setq lenini (getPkOnPoly eleCL pt))
(if (not lenini) 
		(std-error "\nSelAlignSelEst->getPkOnPoly: ReturnValue!\nProvavelmente Ponto não esta se localizada corretamente arriba Alinhamento "))
(list eleCL (- (car pk) lenini) (car pk))
);end defun selalign
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (SelPerfSelEst incr)
(defun SelPerfSelEst (incr / elecl lenini pk pt )
;;;;---ELECL - SELECT ALINEAMIENTO: (entidad clic-pto)
(setq eleCL (std-entsel "\nSelecione Perfil: "))			
(while (not (std-entity-type-p eleCL "LWPOLYLINE"))
  (print "\nTente-o de novo ou ESC para sair! ")
  (setq eleCL (std-entsel "\nSelecione Perfil: "))  )
;; select text estaca o valor  
(if (not (setq pk (SelectTxtEstaca incr))) (std-error "\nSelAlignSelEst->SelectTxtEstaca: ReturnValue!"))
;select punto sobre pline q corresponde a estaca dada:
(if (not (setq pt (std-get1point	"\nSelecione Ponto que corresponde a Estaca selecionada: ") )) 
			(std-error "\nRequer Ponto arriba o Pline "))
;;; fin selection
(setq lenini (getPkOnPoly eleCL pt))
(if (not lenini) 
		(std-error "\nSelAlignSelEst->getPkOnPoly: ReturnValue!\nProvavelmente Ponto não esta se localizada corretamente arriba Alinhamento"))
(list eleCL pt (- (car pk) lenini) (car pk))
);end defun selalign
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (std-ptonseg-p <seg> <testpt>) ; point on segment?
;;; Input: segmento, punto
;;; Input: ((130.815 256.842 0.0) (450.443 425.495 0.0)), (529.186 400.41 0.0)
;;; Return: Largo segmentino hasta Punto, o NIL si Punto no esta sobre segmento
(defun std-ptonseg-p (seg testpt / ang1 ang2 ccv ccw ctrrad onseg segaux ) ; point on segment?
(setq onseg nil)
(cond
((std-seg-bulge seg)  ;;if seg = arco
          (setq ctrrad (std-seg->cir seg));;; (->midpt radio) 
          (if (> (std-seg-bulge-num seg) 0)
            (setq ccw 1) (setq ccv -1));endif
          (if (equal (distance  (car seg) (std-new-z (car ctrrad) 0.0)) ; si es el mismo radio
            					 (distance  testpt (std-new-z (car ctrrad) 0.0)) 1E-6)
            						(progn 
            							(setq ccw (std-seg-bulge-num seg))
            							(setq ang1 (angle (car ctrrad) (car seg)))
            							(setq ang2 (angle (car ctrrad) testpt))
            							(setq segaux (std-arc->seg ctrrad ang1 ang2))
													  (if (or (< ; si punto esta sobre arco:
													  			(std-seg-length (std-arc->seg ctrrad ang1 ang2))
													        (std-seg-length seg))
													        (equal ; si punto esta sobre arco:
													  			(std-seg-length (std-arc->seg ctrrad ang1 ang2))
													        (std-seg-length seg) 1E-6) )
															  	    (setq onseg (std-seg-length (std-arc->seg ctrrad ang1 ang2)))
														)));endifif
); end case if arco 
((not (std-seg-bulge seg))  ;; if seg = linea recta:
  (if (and (equal (std-seg-angle seg) (std-seg-angle (list (car seg) testpt)) 1E-6) 
  					(<= (distance (car seg) testpt) (std-seg-pts-dist seg)) )
  (progn (setq onseg (distance (car seg) testpt)))
  (if (equal testpt (car seg)) (setq onseg 0) 1E-6)
  );endif
);end case linea recta
(T (std-error "\nstd-ptonseg-p: case T!")
);end case otros
);end cond
onseg
);;end defun
;;;===================================================
;;; (GetCVdata segmento)
;;; Return: Lista 
;;; (pkini angparcial radio longCurva longTangente ctrPt itsPI 
;;;						arcmidpt arcinipt arcfinpt ang1 ang2 bulge)
;;; (0.0 -5.49144 25.0 19.7936 10.4484 (335.212 394.998 0.0) (308.376 398.737 0.0) 
;;;   (310.451 398.448 0.0) (311.036 388.633 0.0) (313.696 407.729 0.0) 6.02573 0.534291 -20156)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun getCVdata ( seg / )
(setq midRad (std-seg->cir seg))
(if (not (car midrad))(std-error "\ngetCVdata: segmento de entrada no es curva!"))
(setq a1 (+ (angle (xy-of (car midRad)) (xy-of (car seg))) (/ pi 2)))
(setq a2 (+ (angle (xy-of (car midRad)) (xy-of (cadr seg))) (/ pi 2)))
(setq its (inters (car seg) (polar (car seg) a1 100.0) 
				          (cadr seg) (polar (cadr seg) a2 100.0) nil ))
(setq azim1 (angle (xy-of (car seg)) (xy-of its)) )
(setq azim2 (angle (xy-of its) (xy-of (cadr seg))) )
(setq lentan (distance (xy-of (car seg)) (xy-of its) ))
(setq arcmidpt (car (IntersectSegAux seg (list (caar midrad)(cadar midrad) 0.0) its)))
(setq angparc (- azim2 azim1))
(if (< angparc 0) (setq angparc (+ angparc (* 2 pi))))
(if (> angparc pi) (setq angparc (- (* 2 pi) angparc)))
(setq cvd (list 0.0
			angparc
			(cadr midRad) 
			(std-seg-length	 seg) 
			(distance (xy-of (car seg)) (xy-of its))
			(list (caar midrad)(cadar midrad) 0.0) 	
			its 	
			arcmidpt
			(car seg)
			(cadr seg)
			azim1 
			azim2
			(std-seg-bulge seg)	))
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (IntersectSegAux eleCLoSegLst Pt1 Pt2)
;;; +++ MAKE SEGMENTO AUXILIAR DE DOS PUNTOS Y 
;;; INTERSECTALA CON OTRO ELEMENTO +++
;;; Input: EnameElemento o 1Segmento, Punto1, Punto2
;;; Return: Lista de Pontos de interseccao
(defun IntersectSegAux (enameCL Pt1 Pt2 / ename ptintlst elenew  )
(std-init-entget)		
(if (std-seg-p  enameCL) (progn
(std-entmake-lwpolyline (list (cons 0 "LWPOLYLINE" )) (list enamecl))
(setq enameCL (entlast))
(setq elenew T)
));;end if not ename
(if (listp enameCL)  (if (std-enamep (car enamecl)) (setq enamecl (car enamecl))))
(std-entmake-line (list (cons 10 pt1) (cons 11 pt2) ))
(setq ename (entlast))
(setq PtIntlst (getinters enameCL ename));;; <<<Get Inters. (pointlist)
(std-entdel ename);;; Borrar elemento auxiliar
(if elenew (std-entdel enameCL))
PtIntLst
);;; end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;;; (GETINTERS enameCL enameInt)
;;; +++ INTERSECTA 2 ELEMENTOS Y DESVUELVE POINTLIST +++
;;; Input: Ename 1°Elemento, Ename 2°Elemento
;;; Return: Lista de Puntos de interseccion o NIL si no hay interseccion
(defun getinters (enameCL enameInt / erg)
(setq erg (list "PTLST")) 
    (foreach p (intersect enameCL enameInt )
		(setq erg (std-adjoin p erg))
    );;;End Foreach
(setq erg (std-remove "PTLST" erg))
);;; end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (getPkOnPoly_R1 eleCL pto) 
;;; Returns (pto PK Offs Ptoint ang)
;;; -------++++++++--------++++++++---------++++++++----------
(defun getPkOnPoly_R1 (eleCL pto / ptoInt pk lastent)
  (vl-load-com)
  (setq ptoInt (vlax-curve-getClosestPointTo (vlax-ename->vla-object eleCL) pto))
  (entmake
    (list'(0 . "LINE")
      (cons 10 pto)
      (cons 11 ptoInt)
    '(62 . 1)))
  (setq lastent (entlast))
  (setq pk (vlax-curve-getDistAtPoint (vlax-ename->vla-object eleCL) ptoInt))
  (entdel lastent)
  (list pto pk (distance (list (car ptoint)(cadr ptoint))
  										 (list (car pto)(cadr pto))) ptoint )
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (getPkOnPoly eleCL pt) 
;;; +++ DESVUELVE LARGO POLYLINEA HASTA INTERSECCION +++
;;; (Interseccion debe existir)
;;; Input: Ename Polylinea, Punto
;;; Input: <Entity name: 4009be20>, (529.186 400.41 0.0)
;;; Return: largo Polylinea Node0 hasta Interseccion
;;;         o nil
;;; Revision: 04-05-05
(defun getPkOnPoly (psegs pt / ipos psegs seglen  )
(if (std-enamep psegs)(progn
	(setq psegs (std-pline-segs psegs)) );end if ename
(progn ;else:
(if (listp psegs)  
	(if (std-enamep (car psegs)) 
		(setq psegs (std-pline-segs (car psegs)))
		(if (not (std-seg-p (car psegs)))
				(std-error "\n\nGetPkOnPoly: revisar")) ))));end ififif
				(print pt)
(setq ipos 0) (setq seglen nil)
(while (and (nth ipos psegs) (not seglen) )
(setq seglen (std-ptonseg-p (nth ipos psegs) pt))
(setq ipos (1+ ipos))   
);endwhile
(setq ipos (1- ipos))
;;;Return:
(if (and (> ipos 0) seglen)
(+ (apply '+ (mapcar 'std-seg-length (car (std-split-list ipos psegs)))) seglen)  
seglen)
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; funcciona - desvuelve dist parc desde seg! 
;;; (GETCLICSEG PSEGS PT) => (N°Vertex, TIPO, LENSEG-HASTA-PTO, DISTMINperp-inputPT-SEG, PT) 
;;; TIPO 0 = DISTANCIA MAS CERCANA = PERPENDICULAR PT - SEGMENTO
;;; TIPO 1 = DISTANCIA MAS CERCANO = PT - (CADR SEG) 
;;; TIPO 2 = DISTANCIA PT - (CADR SEG) (PERP FUERA DE LA LINEA)
;;;---------------------------------------------------------------
(defun getclicseg (psegs pt / distmin erg i ii len )
(setq pt (std-new-z pt 0.0))
(setq i 0)
(setq distmin (list 0 1 0.0 (distance (car (nth 0 psegs)) pt)))
(while (nth i psegs)
(if (std-seg-BULGE (nth i psegs))(progn
  (setq erg (cons i (getclicsegcurva (nth i psegs) pt)))
  )(progn
  (setq erg (cons i (getclicsegrecto (nth i psegs) pt)))
   ));;; END IF
(if (> (nth 3 distmin) (nth 3 erg)) (setq distmin erg) );->conseguir distancia minima
(setq i (1+ i))
);;; end while psegs
;(setq i (nth 0 distmin))
;(setq len 0.0)
;(setq ii 0)
;(while (< ii i)
;(setq len (+ len (std-seg-length (nth ii psegs))))
;(setq ii (1+ ii))
;);;;end while 
;(list i len)
distmin
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (getclicsegcurv seg pt3) => (N°Vertex, TIPO, SEG-LEN-HASTA-PTO, DISTMINperp-inputPT-SEG, PT) 
;;; TIPO 0 = DISTANCIA MAS CERCANA = PERPENDICULAR PT - SEGMENTO
;;; TIPO 1 = DISTANCIA MAS CERCANO = PT - (CADR SEG) 
;;; TIPO 2 = DISTANCIA PT - (CADR SEG) (PERP FUERA DE LA LINEA)
;;;--------------------------------------------------------------
(defun getclicsegcurva (seg pt3 / ang ang1 ang2 cir di-perp erg pt segaux ) 
(setq cir (std-seg->cir seg)) ;(midpt rad)
(setq ang1 (angle (car cir) (car seg)))
(setq ang2 (angle (car cir) (cadr seg)))
(setq ang (angle (car cir) pt3))
(cond 
   (  (or (and (<= ang ang1) (>= ang ang2)) (and (>= ang ang1) (<= ang ang2)) )
   (setq pt (polar (car cir) ang (cadr cir)));;;(polar pt ang dist) 
   (setq segaux (std-arc->seg cir ang ang1))
   (setq di-perp (distance pt pt3));;;
     (if (< di-perp (distance (car cir) (cadr seg)))
     (setq erg (list 0 (std-seg-length segaux) di-perp pt)) 				;;; TIPO 0
     (setq erg (list 1 (std-seg-length seg) (distance pt3 (cadr seg)) (cadr seg)))	;;; TIPO 1
     )
   )
   (  (or  (and (>= ang (+ pi ang1)) (<= ang (+ pi ang2))) (and (<= ang (+ pi ang1)) (>= ang (+ pi ang2)))  )
   (setq pt (polar (car cir) (+ pi ang) (cadr cir)))
   (setq segaux (std-arc->seg cir ang1 ang))
   (setq di-perp (distance pt pt3) )
     (if (< di-perp (distance (car cir) (cadr seg)))
     (setq erg (list 0 (std-seg-length segaux) di-perp pt))				;;; TIPO 0
     (setq erg (list 1 (std-seg-length seg) (distance pt3 (cadr seg)) (cadr seg)))	;;; TIPO 1
     )
   )
   ( (= T T)   
     (setq erg (list 2 (std-seg-length seg) (distance pt3 (cadr seg)) (cadr seg))) 	;;; TIPO 2
   )
);;;end cond
erg
);;;end func

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (GETCLICSEGRECTO SEG PT3) => (N°Vertex, TIPO, SEG-LEN-HASTA-PTO, DISTMINperp-inputPT-SEG, PT)
;;; TIPO 0 = DISTANCIA MAS CERCANA = PERPENDICULAR PT - SEGMENTO
;;; TIPO 1 = DISTANCIA MAS CERCANO = PT - (CADR SEG) 
;;; TIPO 2 = DISTANCIA PT - (CADR SEG) (PERP FUERA DE LA LINEA)
;;;---------------------------------------------------------------
(defun getclicsegrecto (seg pt3 / a ang1 ang2 b1 b2 dist_pt-seg erg ) 

(SETQ ANG1 (- (angle (car seg) pt3) (std-seg-angle seg)))
(setq b1 (* (DISTANCE (CAR SEG) PT3) (COS ANG1)))
(if (< b1 0) (setq b1 (* b1 -1 )) )
(SETQ ANG2 (- (angle pt3 (cadr seg)) (std-seg-angle seg) ) )
(setq b2 (* (DISTANCE (CAdR SEG) PT3) (COS ANG2)))
(if (< b2 0) (setq b2 (* b2 -1 )) )
(setq a (* (sin ang1) (DISTANCE (CAR SEG) PT3))) 
(if (< a 0.0) (setq a (* a -1)) )
(setq dist_pt-seg (distance pt3 (cadr seg)) )
   (if (<= (+ b1 b2) (+ (std-seg-length seg) 0.0000001) )
   (progn
     (if (<= dist_pt-seg a)
      (setq erg (list 1 (std-seg-length seg) dist_pt-seg 
     			(polar (car seg) (std-seg-angle seg) (std-seg-length seg)) )  
      ) 							;;; 1 -  if - pt3 mas cerca al 2º nodo  
      (setq erg (list 0 b1 a 
     			(polar (car seg) (std-seg-angle seg) b1) )
      )	       							;;; 0 -  else - dist perpendicular
     );end if
   );;; 
   (progn (setq erg  (list 2 (std-seg-length seg) dist_pt-seg 
   			(polar (car seg) (std-seg-angle seg) (std-seg-length seg)) )
   )   )   );;; end ifelse 					;;; 2  - fuera de la zona perp

erg 
);;;end defun

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; siguientes para testear
;;; return: lista de strings: ("3 876.90 786.98..." "1 ...." "2 ....")
(defun openAlignH ( / alignlst arch fp1 i nomproy  )
(if (setq arch (getfiled "Select Nombre Alineamiento" (strcat (getruta) "Proy/") "alh" 8))
	(setq fp1 (std-fopen arch "r"))
	(Std-error "\nCancelado por el usuario...chau\nNo se abrió el Alineamiento"))
(setq nomproy (std-strcase(std-filename-base arch)))
(setq alignlst (list (list nomproy )))
(while (setq i (read-line fp1))
(if (strisnum (std-firstchar i))	
 (setq alignlst (cons i alignlst) )));end while
(std-fclose fp1)
(setq alignlst (reverse alignlst))
;(setq alignlst (mapcar '(lambda (x) (if (strisnum (car x)) x)) alignlst))
alignlst
);end defun
;(setq fp1 (open (getfiled "nombre archivo csv (TN)"  
;								"D:/Lapsus LISPuli/Proy_CO/" "csv" 0) "r") ) 
;;; Formato archivo txt:
;;; 690+11.535      EIXO.VALA.PT49   7484844.8810   293172.9840   578.098
;(setq tnlst (list (str2lst (read-line fp1) " ")))
;(while (setq i (str2lst (read-line fp1) " ")) (setq tnlst (cons i tnlst)))
;(foreach i (str2lst (read-line fp1) " ")(setq tnlst (cons i tnlst)))
;(close fp1) (setq tnlst (reverse tnlst))
;(std-fopen "TEST.INI" "w")
;(setq dirs (std-glob-dirs (strcat (getruta) "proy/")))
;(if (< (length dirs) 2) (std-error "\nFaltan Directorios: "))
;(if (not (std-fopen "TEST.INI" "r")))

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;(defun mkcfg ( NomArch ProyDir /  )
;(if (not (std-file-directory-p (strcat (getruta) "Proy/"))) (print "xxx")) ;  => T or nil	
;);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;(asli symb #JM:ALIGNLST)
(defun asli ( symb lst / )
(cadr (assoc symb lst)))
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;(substasli symb lst newval)
(defun substasli (symb lst newval / new old )
(setq old (assoc symb lst) new (list symb newval)) 
(subst new old lst))
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; ojo: quita todo los nil
;;; para mantener nil use std-flatten
(defun flatten (liste / )
  (cond
    ((null liste)nil)
    ((atom liste)(list liste))
    (1(append(flatten(car liste))(flatten(cdr liste))))
  )
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (lst2str lst sep dec)
;;; ignora symbols y flatten lista.
;;; desvuelve string
(defun lst2str (lst sep dec / lst str )
(setq str "")
(setq lst (std-flatten lst))
(foreach i lst (cond
((std-symbolp i) )
((std-integerp i) (setq str (strcat str (rtos i 2 0) sep)))
((std-realp i) (setq str (strcat str (rtos i 2 16) sep)))
((not i) (setq str (strcat str "NIL" sep)))
((stringp i) (setq str (strcat str i sep)))
((listp i) (std-error "\nSTDJMLIB->lst2str: ne puede entrar aqui...???!"))
(T (std-error "\nSTDJMLIB->lst2str: no deberia entrar aqui..."))
));end cond, foreach

(std-strchg str "." dec)
) ;end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (strl2asliPIs strl sep)
(defun strl2asliPIs (strl sep / ctrl itsl rad strl  )
(if (stringp strl)
 (setq strl (std-strsplit	(std-strchg strl "," ".") sep)) )
(setq strl (mapcar '(lambda (x) 
										(if (std-wcimatch x "*_*") x
										(if (= (strlen x) 1) (atoi x) 
										   (if (= x "NIL") (null x) (atof x))))  ) strl))
(cond 
((= (length strl) 14) (setq itsl nil)(setq rad nil)(setq ctrl nil))
((= (length strl) 16) (setq itsl (list (nth 11 strl)(nth 12 strl)(nth 13 strl)))
											(setq rad nil)(setq ctrl nil))
((= (length strl) 18) (setq itsl (list (nth 11 strl)(nth 12 strl)(nth 13 strl)))
											(setq rad (nth 14 strl))
											(setq ctrl (list (nth 15 strl)(nth 16 strl)(nth 17 strl))))
(T (std-error "\nstrl2aslipis: condicion inesperada.")));end cond
(list (list 'TYP (nth 0 strl))
			(list 'ISEGPKR (nth 1 strl))
      (list 'ISEGEQ  (nth 2 strl))
      (list 'SEG  (list (list (nth 3 strl)(nth 4 strl)(nth 5 strl))
      						(list (nth 6 strl)(nth 7 strl)(nth 8 strl))(nth 9 strl) ))
      (list 'LONGSEG  (nth 10 strl))
      (list 'ITS  itsl)
      (list 'RADIO  rad)
      (list 'CTR  ctrl))
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (lst2asliPIs lst )
(defun lst2asliPIs (lst / sb )
(list (list 'TYP (nth 0 lst))
			(list 'ISEGPKR (nth 1 lst))
      (list 'ISEGEQ  (nth 2 lst))
      (list 'SEG  (nth 3 lst))
      (list 'LONGSEG  (nth 4 lst))
      (list 'ITS  (nth 5 lst))
      (list 'RADIO  (nth 6 lst))
      (list 'CTR  (nth 7 lst)))
      ;(list 'V (nth 8 lst))
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (strisnum str)
;;; desvuelve T o nil
(defun strisnum ( str / ok str strl  )
(setq ok T)
(if (or (= str ".")(= str ",")) (setq ok nil))
(if (= str nil )(setq ok nil)(progn
(setq str (std-strchg str "," "."))
(setq strl (vl-string->list str) )
(foreach i strl
(if (not (or (and (>= i 48) (<= i  58)) (= i 46) (= i 45))) 
(setq ok nil));;; end if
);;;end foreach
(if (> (std-strcount "." str) 1) (setq ok nil))
(if (> (std-strcount "-" str) 1) (setq ok nil))
(if (and (= (std-strcount "-" str) 1) (/= (car strl) 45)) (setq ok nil))
));end ifelse str=nil
ok
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (writeAlignH alignlst eqlst)
(defun writeAlignH (alignlst eqlst / alignlst arch eqlst fecha fp1   )
(if (not (std-file-directory-p (strcat (getruta) "Proy/")))(progn 
(std-dcl-yesno	"Directorio ....\Lapsus Lispuli\Proy\ no existe!" "Yes")	
(std-error) ));End if not ruta
(setq fecha (std-date->datlst  (getvar "DATE")))
(setq fecha (strcat "Fecha ultima modificación: " (itoa (nth 0 fecha)) "/"
										(itoa (nth 1 fecha)) "/"
										(itoa (nth 2 fecha)) " - "
										(itoa (nth 3 fecha)) "."
										(itoa (nth 4 fecha))))
(if (setq arch (getfiled "Nombre Alineamiento" (strcat (getruta) "Proy/") "alh" 1))
	(setq fp1 (std-fopen arch "w"))
	(Std-error "\nCancelado por el usuario...chau\nNo se guardó el Alineamiento"))
(setq #JM:ALIGNPROY (std-strcase(std-filename-base arch)))
(write-line (strcat "Nombre Alineamiento: " #JM:ALIGNPROY) fp1)
(write-line Fecha fp1)
(setq eqlst (mapcar '(lambda (x) (write-line (lst2str x " " ".") fp1)) eqlst))
(setq alignlst (mapcar '(lambda (x) (write-line (lst2str x " " ".") fp1)) alignlst))	
(prompt "\nEl Alineamiento fue guardado. ")
(prompt (strcat "\nEl alineamiento actual es - " #JM:ALIGNPROY " - "))
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun closealignh ( / )
(setq  #JM:ALIGNLST nil #JM:ALIGNPROY nil #JM:EQLST nil)
(prompt "\nSe cerró el Alineamiento.")(princ)
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (geteq pkreal)
;;; input: valor pkreal
;;; return: valor-equacion o valor-pkreal si no hay ecuacion
;;;-----------------------------------------------------------
(defun geteq (pkreal / dif eqlst equ i )
(if #JM:EQLST (progn 
(setq i 0)(setq equ nil)
(setq eqlst #JM:EQLST)
(while (and (< pkreal (cadr (nth i eqlst))) (not (equal pkreal (cadr (nth i eqlst)) 1e-6)))
(setq i (1+ i)) );end while
(while (and (nth i eqlst)(or (> pkreal (cadr (nth i eqlst)))(equal pkreal (cadr (nth i eqlst)) 1e-6)))
(setq equ T)
(setq dif (- (caddr (nth i #JM:EQLST))(cadr (nth i #JM:EQLST))))
(setq i (1+ i))
);end while
(if equ  (+ pkreal dif) (atof (rtos pkreal 2 6)))
 ) pkreal);end if
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (revisarahlst eqlst ahlst) 
;;; setzt alle eqs in ahlst segun eqlst
;;; return: ahlst
;;;---------------------------------------------------
(defun revisarahlst ( eqlst ahlst / dif pkr  )
(if eqlst (progn 
(foreach i eqlst
(setq pkr (cadr i))
(setq dif (- (caddr i) pkr))
(mapcar '(lambda (x)
					(if (< (asli 'isegpkr x) pkr) 
						x 
  					(substasli 'isegeq x (+ dif (asli 'isegpkr x)))
  				))  ahlst)
);end foreach
);else if not eqlst - eqs en ahlst gleichsetzen mit pks;
(mapcar '(lambda (x)
 (substasli 'isegeq x (asli 'isegpkr x)) ) ahlst)
);end if
);end defun
;;;(substasli symb lst newval)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (getfixprox pkini incr)
;;; Input: Pk-inicial Increment
;;; Return: siguiente o mismo Valor redondo 
;;; 12.2 20 -> 20      125.2 20 -> 140    120.0 20 -> 120 etc.
(defun getfixprox (pkini incr / )
(if (= pkini (* (fix (/ pkini incr)) incr)) pkini
(+ (* (fix (/ pkini incr)) incr) incr));endif
); end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (openeqlst lst)
(defun openeqlst ( lst / eqlst i weiter  )
(setq i 0 weiter T eqlst (list "qqq"))
(while (and (nth i lst) weiter)
(if (= (std-firstchar (nth i lst)) "3")
(setq eqlst (cons (nth i lst) eqlst)) (setq weiter nil) );end if
(setq i (1+ i)));end while
(setq eqlst (vl-remove "qqq" (reverse eqlst)))
(setq eqlst (mapcar '(lambda (x) (str2lst x " ")) eqlst))
(setq eqlst (mapcar '(lambda (xx) 
										(list 
										(atoi (nth 0 xx))
										(atof (nth 1 xx))
										(atof (nth 2 xx))
										(list
										(atof (nth 3 xx))
										(atof (nth 4 xx))
										(atof (nth 5 xx)))
										(atoi (nth 6 xx)));end lst
										) eqlst))
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (geteqantprox pkreal)
(defun geteqantprox (pkreal / i pkant pkprox   )
(setq i 0 pkant nil pkprox nil)
(while (nth i #JM:EQLST)
(if (equal (cadr (nth i #JM:EQLST)) pkreal 1e-6)(progn
(setq pkant (+ (geteq (- pkreal 0.1))0.1))
(setq pkprox (geteq pkreal))
));end if
(setq i (1+ i))
);end while
(list pkant pkprox)
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;===================================================
;;; (AlignH-MkData (ENAMECL_o_PSEGS PKINI PKPT) NoPiIni v );pkpt no se usa
;;; NoPiIni = "" o para variantes el PI de referencia
;;; v = variante: T = variante.  nil = not variante
;;; Return: Lista 
;;; 'TYP 'ISEGPKR 'ISEGEQ 'SEG 'LONGSEG 'RADIO 'ITS 'CTR
;;; "35_0" -5.49144 25.0 19.7936 10.4484 (335.212 394.998 0.0) (308.376 398.737 0.0) 
;;;   (310.451 398.448 0.0) (311.036 388.633 0.0) (313.696 407.729 0.0) 6.02573 0.534291 -20156)
;;; NOTA: Uso para 	- definir alineamiento, 
;;;									- agregar al final mas plines 
;;;									- agregar una variante
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
(defun AlignH-MkData ( iniLst NoPiIni v / a1 a2 ahlst i its mid midrad nopi 
																						nopiini pkactual psegs seg seglst ubipi   )
(setq Pkactual (cadr inilst))
(setq psegs (car inilst))
(cond
((std-enamep psegs)	(setq psegs (std-pline-segs psegs)))
((listp psegs) (if (std-enamep (car psegs)) (setq psegs (std-pline-segs (car psegs)))) )
);end cond
(if v (progn		;;; ---------------- si variante:
  (if (not (stringp NoPiIni)) (setq NoPiIni (itoa NoPiIni)))
  (setq NoPiIni (strcat NoPiIni "-"))
  (setq NoPi 1));else:
  (progn  ;;; ---------------- si align comun
  (if (stringp NoPiIni) (setq NoPi (atoi NoPiIni)))
  (setq NoPi NoPiIni)
  (setq NoPiIni ""))) ;endifelse
;;; averiguar 1°segmento
(setq seg (nth 0 psegs))
(if (= (length seg) 2) (setq seg (append seg '(nil))))
(if (= (length (car seg)) 2)(setq seg (list (std-add-z (car seg) 0.0)(std-add-z (cadr seg) 0.0)(caddr seg))))
(cond 
((std-seg-straight-p seg)	;if seg=recto
  (setq seglst (list (strcat NoPiIni (itoa NoPi) "_0") pkactual pkactual seg 
  													(std-seg-length seg) (car seg) nil nil));;<<<1/4
  (setq NoPi (1+ NoPi))
  );end case recto
(T	;if seg = curva

  (setq midRad (std-seg->cir seg))
  (setq mid (std-add-z (car midrad) 0.0))
  (setq a1 (+ (angle (xy-of (car midRad)) (xy-of (car seg))) (/ pi 2)))
  (setq a2 (+ (angle (xy-of (car midRad)) (xy-of (cadr seg))) (/ pi 2)))
  (setq its (inters (car seg) (polar (car seg) a1 100.0) 
  				          (cadr seg) (polar (cadr seg) a2 100.0) nil ))
  (if v (progn 
  (setq seglst (list (strcat "0_" NoPiIni (itoa NoPi) ) 
  					pkactual pkactual seg (std-seg-length seg) its (cadr midrad) mid));;<<<2/4
  (setq NoPi (+ NoPi 1)))(progn ;end if variante
  (setq seglst (list (strcat noPiIni (itoa NoPi) "_" NoPiIni (itoa (1+ NoPi)) ) 
  					pkactual pkactual seg (std-seg-length seg) its (cadr midrad) mid));;<<<2/4
  (setq NoPi (+ NoPi 2))));endifelse

));end case curva, end cond 1° seg
(setq ahlst (list seglst))
(setq pkactual (+ pkactual (std-seg-length seg)))
(setq i 1)
(while (setq seg (nth i psegs))
(if (= (length seg) 2) (setq seg (append seg '(nil))))
(if (= (length (car seg)) 2)(setq seg (list (std-add-z (car seg) 0.0)(std-add-z (cadr seg) 0.0)(caddr seg))))
(cond 
((std-seg-straight-p seg)	;if seg=recto
  (if (std-seg-straight-p (nth (1- i) psegs)) (progn ;if segant=recto
  	(setq its (car seg))(setq ubiPI (strcat NoPiIni (itoa NoPi) "_0"))(setq NoPi (1+ NoPi)) )
  	(progn (setq its nil) (setq ubiPi "0_0")) );end if recto recto
  (setq seglst (list ubiPi pkactual pkactual seg (std-seg-length seg) its nil nil));;<<<3/4
);end case recto
(T	;if seg = curva
  (setq midRad (std-seg->cir seg))
  (setq mid (std-add-z (car midrad) 0.0))
  (if (= (length (car midrad)) 2)(setq midrad (list (std-add-z (car midrad) 0.0)(cadr midrad))))
  (setq a1 (+ (angle (xy-of (car midRad)) (xy-of (car seg))) (/ pi 2)))
  (setq a2 (+ (angle (xy-of (car midRad)) (xy-of (cadr seg))) (/ pi 2)))
  (setq its (inters (car seg) (polar (car seg) a1 100.0) 
  				          (cadr seg) (polar (cadr seg) a2 100.0) nil ))
  (setq seglst (list (strcat "0_" NoPiIni (itoa NoPi) ) 
  										pkactual pkactual seg (std-seg-length seg) 
  										its (cadr midrad) mid));;<<<4/4
  (setq NoPi (1+ NoPi))
));end case curva, end cond
(setq ahlst (cons seglst ahlst))
(setq pkactual (+ pkactual (std-seg-length seg)))
(setq i (1+ i))
);end while
(if (not v) (progn
(setq seglst (car ahlst))
(setq ahlst (cdr ahlst))
(setq seglst (list 
							(strcat (car seglst) "_" (itoa NoPi))
							(nth 1 seglst)(nth 2 seglst)(nth 3 seglst)
							(nth 4 seglst)(nth 5 seglst)(nth 6 seglst)(nth 7 seglst)))
(setq ahlst (cons seglst ahlst))
));end if not variante
(mapcar 'lst2asliPIs (reverse ahlst))
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (GetPointSobreCL Segs pkini len)
;;; len = largo buscado
;;; pk = pk inicial o 0.0
;;; -------------------------
(defun getPointSobreCL (Segs pk len / i erg pk )
(setq erg nil i 0)
(while (and (nth i segs) (not erg))
(setq pk (+ (std-seg-length (nth i segs)) pk) )
(if (< len pk) (setq erg (getPointSobreSeg (nth i segs) 
									(- (std-seg-length (nth i segs)) (- pk len)))))
(setq i (1+ i))
);end while
erg
);end defun
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; (getPointSobreSeg seg len)
;;; busca punto segun largo
;;; largo debe ser menor que seglen!!!!
;;;-------------------------------------------------------------
(defun getPointSobreSeg (seg len / ang1 ang2 ccv ctrrad ) 
(if (> len (std-seg-length seg))(std-error "\ngetPointSobreSeg; len mayor q seglen!!!"))
(if (std-seg-straight-p seg) ; if recto
(polar (car seg) (std-seg-angle seg) len)
(progn ; case curva
   (setq ctrrad (std-seg->cir seg))	;midpt, radio
    (if (> (caddr seg) 0)(setq ccv 1) (setq ccv -1));endif
		(setq ang1 (angle (car ctrrad) (car seg)))
		(if (= ccv 1)
		(setq ang2 (+ ang1 (/ len (cadr ctrrad))))
		(setq ang2 (- ang1 (/ len (cadr ctrrad)))));endif
		(cadr (std-arc->seg (list (car ctrrad)(cadr ctrrad)) ang1 ang2))
));end ifelse
);end defun