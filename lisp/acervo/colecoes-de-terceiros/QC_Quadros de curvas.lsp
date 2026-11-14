(defun c:qc ()
  (setq	osm (getvar "osmode")
	cla (getvar "clayer")
  )
  (setvar "cmdecho" 0)
  (setvar "osmode" 0)
  (setvar "orthomode" 0)
  (camada)
  (command "_style" "romans" "romans" "0.0" "1.0" "0.0"	"n" "n"	"n")
  (setq op (getint "\n 1 - Curva Simples 2 - Espiral :"))
  (if (= op 1)
    (simples)
    (espiral)
  )
)

(defun simples (/    esc  er_or	    l1	 l2   l3   l4	l5   l6	  l7
		l8   l9	  l10  l11  l12	 l13  l14  l15	l16  l17  l18
		l19  l20  l21  l22  l23	 l24  l25  l26	l27  l28  l29
		l30  l31  l32  t1   t2	 t3   t4   t5	t6   t7	  t8
		t9   t10  t11  t12  t13	 t14  t15  t16	t17  t18  t19
		t20  t21  t22  t23  t24	 t25  t26  t27	t28  t29  c1
		c2   c3	  c4   c5   c6	 c7   c8   c9	c10  c11  c12
		c13  c14  c15  c16  c17	 c18  c19  c20	c21  con1 cor1
		txt1
	       )
  (setq	esc	(getvar "dimscale")
	er_or	*error*
	*error*	erro
	l1	(getpoint "\nPonto de Inserção :")
	l2	(list (+ (car l1) (* 175.0 esc)) (cadr l1))
	l3	(list (car l1) (- (cadr l1) (* 6.0 esc)))
	l4	(list (+ (car l1) (* 175.0 esc)) (- (cadr l1) (* 6.0 esc)))
	l5	(list (car l1) (- (cadr l1) (* 25.5 esc)))
	l6	(list (+ (car l1) (* 175.0 esc)) (- (cadr l1) (* 25.5 esc)))
	l7	(list (+ (car l1) (* 20.0 esc)) (- (cadr l1) (* 6.0 esc)))
	l8	(list (+ (car l1) (* 100.0 esc)) (- (cadr l1) (* 6.0 esc)))
	l9	(list (+ (car l1) (* 20.0 esc)) (- (cadr l1) (* 12.0 esc)))
	l10	(list (+ (car l1) (* 100.0 esc)) (- (cadr l1) (* 12.0 esc)))
	l11	(list (+ (car l1) (* 20.0 esc)) (- (cadr l1) (* 18.0 esc)))
	l12	(list (+ (car l1) (* 175.0 esc)) (- (cadr l1) (* 18.0 esc)))
	l13	(list (+ (car l1) (* 10.0 esc)) (- (cadr l1) (* 25.5 esc)))
	l14	(list (+ (car l1) (* 46.667 esc))
		      (- (cadr l1) (* 18.0 esc))
		)
	l15	(list (+ (car l1) (* 73.333 esc))
		      (- (cadr l1) (* 18.0 esc))
		)
	l16	(list (+ (car l1) (* 118.75 esc))
		      (- (cadr l1) (* 18.0 esc))
		)
	l17	(list (+ (car l1) (* 137.5 esc)) (- (cadr l1) (* 18.0 esc)))
	l18	(list (+ (car l1) (* 156.25 esc))
		      (- (cadr l1) (* 18.0 esc))
		)
	l21	(list (car l1) (- (cadr l1) (* 40.5 esc)))
	l22	(list (+ (car l1) (* 175.0 esc)) (- (cadr l1) (* 40.5 esc)))
	t1	(list (+ (car l1) (* 87.5 esc)) (- (cadr l1) (* 3.0 esc)))
	t2	(list (+ (car l1) (* 10.0 esc)) (- (cadr l1) (* 15.75 esc)))
	t3	(list (+ (car l1) (* 60.0 esc)) (- (cadr l1) (* 9.0 esc)))
	t4	(list (+ (car l1) (* 60.0 esc)) (- (cadr l1) (* 15.0 esc)))
	t5	(list (+ (car l1) (* 137.5 esc)) (- (cadr l1) (* 12.0 esc)))
	t6	(list (+ (car l1) (* 33.333 esc))
		      (- (cadr l1) (* 21.75 esc))
		)
	t7	(list (+ (car l1) (* 60.0 esc)) (- (cadr l1) (* 21.75 esc)))
	t8	(list (+ (car l1) (* 86.667 esc))
		      (- (cadr l1) (* 21.75 esc))
		)
	t9	(list (+ (car l1) (* 109.375 esc))
		      (- (cadr l1) (* 21.75 esc))
		)
	t10	(list (+ (car l1) (* 128.125 esc))
		      (- (cadr l1) (* 21.75 esc))
		)
	t11	(list (+ (car l1) (* 146.875 esc))
		      (- (cadr l1) (* 21.75 esc))
		)
	t12	(list (+ (car l1) (* 165.625 esc))
		      (- (cadr l1) (* 21.75 esc))
		)
	h1	(* 3.0 esc)
	h2	(* 2.5 esc)
	con1	0
  )
  (command "_clayer" "IFL3"    "_line"	 l1	   l2	     ""
	   "_clayer" "IFL3"    "_line"	 l3	   l4	     ""
	   "_clayer" "IFL2"    "_line"	 l9	   l10	     ""
	   "_clayer" "IFL3"    "_line"	 l11	   l12	     ""
	   "_clayer" "IFL3"    "_line"	 l5	   l6	     ""
	   "_clayer" "IFL3"    "_line"	 l21	   l22	     ""
	   "_clayer" "IFR120"
	   "_text"   "_j"      "_mc"     t1	   h1	     0	   "Q U A D R O   D E   C U R V A S"
	   "_text"   "_j"      "_mc"	 t2	   h1	     0     "CURVA"
	   "_text"   "_j"      "_mc"     t3	   h1        0     "PONTOS NOTÁVEIS"
	   "_text"   "_j"      "_mc"     t4        h1        0     "ESTACAS-COORDENADAS"
	   "_text"   "_j"      "_mc"	 t5	   h1	     0	   "ELEMENTOS"
	   "_text"   "_j"      "_mc"     t6	   h1	     0     "PC"
	   "_text"   "_j"      "_mc"     t7        h1	     0     "PI"
	   "_text"   "_j"      "_mc"     t8	   h1        0	   "PT"
	   "_text"   "_j"      "_mc"     t9	   h1        0     "R"
	   "_text"   "_j"      "_mc"	 t10	   h1	     0	   "AC"
	   "_text"   "_j"      "_mc"     t11       h1        0     "T"
	   "_text"   "_j"      "_mc"     t12	   h1	     0     "D"
	   "_style"  "romans"  "romans" "0.0"     "0.9"     "0.0"	 "n"	   "n"	     "n"
	  )
  (while (= nil)
    (setvar "osmode" 1)
    (setq p1 (getpoint "\nIndique o PC :"))
    (if	(= p1 nil)
      (erro)
      (setq p2	 (getpoint "\nIndique o PI :")
	    p3	 (getpoint "\nIndique o PT :")
	    l19	 (list (+ (car l21) (* 10.0 esc))
		       (+ (cadr l21) (* 10.0 esc))
		 )
	    l20	 (list (+ (car l21) (* 100.0 esc))
		       (+ (cadr l21) (* 10.0 esc))
		 )
	    l23	 (list (+ (car l21) (* 10.0 esc))
		       (+ (cadr l21) (* 5.0 esc))
		 )
	    l24	 (list (+ (car l21) (* 100.0 esc))
		       (+ (cadr l21) (* 5.0 esc))
		 )
	    l25	 (list (+ (car l21) (* 10.0 esc)) (cadr l21))
	    l26	 (list (+ (car l21) (* 20.0 esc)) (cadr l21))
	    l27	 (list (+ (car l21) (* 46.667 esc)) (cadr l21))
	    l28	 (list (+ (car l21) (* 73.333 esc)) (cadr l21))
	    l29	 (list (+ (car l21) (* 100.0 esc)) (cadr l21))
	    l30	 (list (+ (car l21) (* 118.75 esc)) (cadr l21))
	    l31	 (list (+ (car l21) (* 137.5 esc)) (cadr l21))
	    l32	 (list (+ (car l21) (* 156.25 esc)) (cadr l21))
	    t13	 (list (+ (car l21) (* 5.0 esc)) (+ (cadr l21) (* 7.5 esc)))
	    t14	 (list (+ (car l21) (* 15.0 esc))
		       (+ (cadr l21) (* 12.5 esc))
		 )
	    t15	 (list (+ (car l21) (* 15.0 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    t16	 (list (+ (car l21) (* 15.0 esc))
		       (+ (cadr l21) (* 2.5 esc))
		 )
	    t17	 (list (+ (car l21) (* 33.333 esc))
		       (+ (cadr l21) (* 12.5 esc))
		 )
	    t18	 (list (+ (car l21) (* 33.333 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    t19	 (list (+ (car l21) (* 33.333 esc))
		       (+ (cadr l21) (* 2.5 esc))
		 )
	    t20	 (list (+ (car l21) (* 60.0 esc))
		       (+ (cadr l21) (* 12.5 esc))
		 )
	    t21	 (list (+ (car l21) (* 60.0 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    t22	 (list (+ (car l21) (* 60.0 esc))
		       (+ (cadr l21) (* 2.5 esc))
		 )
	    t23	 (list (+ (car l21) (* 86.667 esc))
		       (+ (cadr l21) (* 12.5 esc))
		 )
	    t24	 (list (+ (car l21) (* 86.667 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    t25	 (list (+ (car l21) (* 86.667 esc))
		       (+ (cadr l21) (* 2.5 esc))
		 )
	    t26	 (list (+ (car l21) (* 109.375 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    t27	 (list (+ (car l21) (* 128.125 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    t28	 (list (+ (car l21) (* 146.875 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    t29	 (list (+ (car l21) (* 165.625 esc))
		       (+ (cadr l21) (* 7.5 esc))
		 )
	    l21	 (list (car l21) (- (cadr l21) (* 15.0 esc)))
	    l22	 (list (car l22) (- (cadr l22) (* 15.0 esc)))
	    c1	 (car p1)
	    c2	 (cadr p1)
	    c3	 (rtos c1 2 3)
	    c4	 (rtos c2 2 3)
	    c5	 (car p2)
	    c6	 (cadr p2)
	    c7	 (rtos c5 2 3)
	    c8	 (rtos c6 2 3)
	    c9	 (car p3)
	    c10	 (cadr p3)
	    c11	 (rtos c9 2 3)
	    c12	 (rtos c10 2 3)
	    c13	 (/ (distance p1 p3) 2)
	    c14	 (/ c13 (distance p3 p2))
	    c15	 (* (asin c14) 2)
	    c16	 (- 3.1415926 c15)
	    c17	 (angtos c16 1 3)
	    c18	 (distance p2 p3)
	    c19	 (rtos c18 2 3)
	    c20	 (gra c17)
	    con1 (+ 1 con1)
	    cor1 (rtos con1 2 0)
	    txt1 (strcat "C" cor1)
      )
    )
    (arc_cur_simples)
    (setvar "osmode" 0)
    (command "_clayer"	     "IFR120"	     "_text" "_j"    "_mc"
	     t13     h1	     0	     txt1    "_clayer"	     "IFR100"
	     "_text" "_j"    "_mc"   t14     h2	     0	     "EST."
	     "_text" "_j"    "_mc"   t15     h2	     0	     "N"
	     "_text" "_j"    "_mc"   t16     h2	     0	     "E"
	     "_text" "_j"    "_mc"   t17     h2	     0	     "-"
	     "_text" "_j"    "_mc"   t18     h2	     0	     C4
	     "_text" "_j"    "_mc"   t19     h2	     0	     C3
	     "_text" "_j"    "_mc"   t20     h2	     0	     "-"
	     "_text" "_j"    "_mc"   t21     h2	     0	     C8
	     "_text" "_j"    "_mc"   t22     h2	     0	     C7
	     "_text" "_j"    "_mc"   t23     h2	     0	     "-"
	     "_text" "_j"    "_mc"   t24     h2	     0	     C12
	     "_text" "_j"    "_mc"   t25     h2	     0	     C11
	     "_text" "_j"    "_mc"   t27     h2	     0	     C20
	     "_text" "_j"    "_mc"   t28     h2	     0	     C19
	     "_clayer"	     "IFL2"  "_line" L19     L20     ""
	     "_clayer"	     "IFL2"  "_line" L23     L24     ""
	     "_clayer"	     "IFL3"  "_line" L21     L22     ""
	    )
    (princ "\n")
    (princ txt)
  )
)


(defun espiral (/      osm    cla    esc    er_or  ln1	  ln2	 ln3
		ln4    ln5    ln6    ln7    ln8	   ln9	  ln10	 ln11
		ln12   ln13   ln14   ln15   ln16   ln17	  ln18	 ln19
		ln20   ln21   ln22   ln23   ln24   ln25	  ln26	 ln27
		ln28   ln29   ln30   ln31   ln32   ln33	  ln34	 ln35
		ln36   ln37   ln38   ln39   ln40   ln41	  ln42	 ln43
		ln44   te1    te2    te3    te4	   te5	  te6	 te7
		te8    te9    te10   te11   te12   te13	  te14	 te15
		te16   te17   te18   te19   te20   te21	  te22	 te23
		te24   te25   te26   te27   te28   te29	  te30	 te31
		te32   te33   te34   te35   te36   te37	  te38	 te39
		te40   te41   te42   te43   te44   te45	  ce1	 ce2
		ce3    ce4    ce5    ce6    ce7	   ce7a	  ce8	 ce8a
		ce9    ce10   ce11   ce12   ce13   ce14	  ce15	 ce15a
		ce16   ce16a  ce17   ce18   ce19   ce20	  ce21	 ce22
		ce23   ce23a  ce24   ce25   ce26   ce25a  ce26a	 ce27
		ce28   ce29   ce30   ce31   ce32   ce33	  con	 cor
		txt    cv1    cv2
	       )
  (setq	esc	(getvar "dimscale")
	er_or	*error*
	*error*	erro
	ln1	(getpoint "\nPonto de Inserção :")
	ln2	(list (+ (car ln1) (* 265.217 esc)) (cadr ln1))
	ln3	(list (car ln1) (- (cadr ln1) (* 6.0 esc)))
	ln4	(list (+ (car ln1) (* 265.217 esc))
		      (- (cadr ln1) (* 6.0 esc))
		)
	ln5	(list (+ (car ln1) (* 25.0 esc)) (- (cadr ln1) (* 6.0 esc)))
	ln6	(list (+ (car ln1) (* 135.0 esc))
		      (- (cadr ln1) (* 6.0 esc))
		)
	ln7	(list (+ (car ln1) (* 25.0 esc))
		      (- (cadr ln1) (* 12.0 esc))
		)
	ln8	(list (+ (car ln1) (* 135.0 esc))
		      (- (cadr ln1) (* 12.0 esc))
		)
	ln9	(list (+ (car ln1) (* 25.0 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln10	(list (+ (car ln1) (* 265.217 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln11	(list (+ (car ln1) (* 47.0 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln12	(list (+ (car ln1) (* 69.0 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln13	(list (+ (car ln1) (* 91.0 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln14	(list (+ (car ln1) (* 113.0 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln15	(list (+ (car ln1) (* 150.0 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln16	(list (+ (car ln1) (* 171.141 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln17	(list (+ (car ln1) (* 186.821 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln18	(list (+ (car ln1) (* 202.5 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln19	(list (+ (car ln1) (* 218.179 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln20	(list (+ (car ln1) (* 233.859 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln21	(list (+ (car ln1) (* 249.538 esc))
		      (- (cadr ln1) (* 18.0 esc))
		)
	ln22	(list (car ln1) (- (cadr ln1) (* 25.5 esc)))
	ln23	(list (+ (car ln1) (* 265.217 esc))
		      (- (cadr ln1) (* 25.5 esc))
		)
	ln24	(list (car ln1) (- (cadr ln1) (* 40.5 esc)))
	ln25	(list (+ (car ln1) (* 265.217 esc))
		      (- (cadr ln1) (* 40.50 esc))
		)
	ln26	(list (+ (car ln1) (* 15.0 esc))
		      (- (cadr ln1) (* 25.5 esc))
		)
	te1	(list (+ (car ln1) (* 132.609 esc))
		      (- (cadr ln1) (* 3.0 esc))
		)
	te2	(list (+ (car ln1) (* 12.5 esc))
		      (- (cadr ln1) (* 16.75 esc))
		)
	te3	(list (+ (car ln1) (* 80.0 esc)) (- (cadr ln1) (* 9.0 esc)))
	te4	(list (+ (car ln1) (* 80.0 esc))
		      (- (cadr ln1) (* 15.08 esc))
		)
	te5	(list (+ (car ln1) (* 200.109 esc))
		      (- (cadr ln1) (* 12.0 esc))
		)
	te6	(list (+ (car ln1) (* 36.0 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te7	(list (+ (car ln1) (* 58.0 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te8	(list (+ (car ln1) (* 80.0 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te9	(list (+ (car ln1) (* 102.0 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te10	(list (+ (car ln1) (* 124.0 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te11	(list (+ (car ln1) (* 142.5 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te12	(list (+ (car ln1) (* 160.571 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te13	(list (+ (car ln1) (* 178.981 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te14	(list (+ (car ln1) (* 194.660 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te15	(list (+ (car ln1) (* 210.340 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te16	(list (+ (car ln1) (* 226.019 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te17	(list (+ (car ln1) (* 241.698 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	te18	(list (+ (car ln1) (* 257.378 esc))
		      (- (cadr ln1) (* 21.75 esc))
		)
	h1	(* 3.0 esc)
	h2	(* 2.5 esc)
	con	0
  )
  (command "_clayer" "IFL3"    "_line"	 ln1	   ln2	     ""
	   "_clayer" "IFL3"    "_line"	 ln3	   ln4	     ""
	   "_clayer" "IFL2"    "_line"	 ln7	   ln8	     ""
	   "_clayer" "IFL3"    "_line"	 ln9	   ln10	     ""
	   "_clayer" "IFL3"    "_line"	 ln22	   ln23	     ""
	   "_clayer" "IFL3"    "_line"	 ln24	   ln25	     ""
	   "_clayer" "IFR120"
	   "_text"   "_j"      "_mc"     te1	   h1	     0     "Q U A D R O   D E   C U R V A S"
	   "_text"   "_j"      "_mc"	 te2	   h1	     0     "CURVA"
	   "_text"   "_j"      "_mc"     te3       h1        0     "PONTOS NOTÁVEIS"
	   "_text"   "_j"      "_mc"     te4       h1        0     "ESTACAS - COORDENADAS"
	   "_text"   "_j"      "_mc"	 te5	   h1	     0     "ELEMENTOS"
	   "_text"   "_j"      "_mc"     te6	   h1	     0     "TE/PC"
	   "_text"   "_j"      "_mc"     te7       h1	     0     "EC"
	   "_text"   "_j"      "_mc"     te8       h1        0	   "PI"
	   "_text"   "_j"      "_mc"     te9       h1        0	   "CE"
	   "_text"   "_j"      "_mc"	 te10	   h1	     0	   "ET/PT"
	   "_text"   "_j"      "_mc"     te11      h1        0     "R"
	   "_text"   "_j"      "_mc"     te12	   h1	     0     "AC"
	   "_text"   "_j"      "_mc"     te13      h1        0     "LC1"
	   "_text"   "_j"      "_mc"     te14      h1        0	   "LC2"
	   "_text"   "_j"      "_mc"     te15      h1        0     "TS1"
	   "_text"   "_j"      "_mc"	 te16	   h1	     0	   "TS2"
	   "_text"   "_j"      "_mc"     te17      h1        0     "T"
	   "_text"   "_j"      "_mc"     te18	   h1	     0     "D"
	   "_style"  "romans"  "romans" "0.0"     "0.9"     "0.0"	 "n"	   "n"	     "n"
	  )
  (while (= nil)
    (setvar "osmode" 1)
    (setq p1 (getpoint "\nIndique o TE/PC :"))
    (if	(= p1 nil)
      (erro)
      (setq p2	  (getpoint "\nIndique o EC/PC :")
	    p3	  (getpoint "\nIndique o PI :")
	    p4	  (getpoint "\nIndique o CE/PT :")
	    p5	  (getpoint "\nIndique o ET/PT :")
	    ln27  (list	(+ (car ln24) (* 15.0 esc))
			(+ (cadr ln24) (* 10.0 esc))
		  )
	    ln28  (list	(+ (car ln24) (* 135.0 esc))
			(+ (cadr ln24) (* 10.0 esc))
		  )
	    ln29  (list	(+ (car ln24) (* 15.0 esc))
			(+ (cadr ln24) (* 5.0 esc))
		  )
	    ln30  (list	(+ (car ln24) (* 135.0 esc))
			(+ (cadr ln24) (* 5.0 esc))
		  )
	    ln31  (list (+ (car ln24) (* 15.0 esc)) (cadr ln24))
	    ln32  (list (+ (car ln24) (* 25.0 esc)) (cadr ln24))
	    ln33  (list (+ (car ln24) (* 47.0 esc)) (cadr ln24))
	    ln34  (list (+ (car ln24) (* 69.0 esc)) (cadr ln24))
	    ln35  (list (+ (car ln24) (* 91.0 esc)) (cadr ln24))
	    ln36  (list (+ (car ln24) (* 113.0 esc)) (cadr ln24))
	    ln37  (list (+ (car ln24) (* 135.0 esc)) (cadr ln24))
	    ln38  (list (+ (car ln24) (* 150.0 esc)) (cadr ln24))
	    ln39  (list (+ (car ln24) (* 171.141 esc)) (cadr ln24))
	    ln40  (list (+ (car ln24) (* 186.821 esc)) (cadr ln24))
	    ln41  (list (+ (car ln24) (* 202.5 esc)) (cadr ln24))
	    ln42  (list (+ (car ln24) (* 218.179 esc)) (cadr ln24))
	    ln43  (list (+ (car ln24) (* 233.859 esc)) (cadr ln24))
	    ln44  (list (+ (car ln24) (* 249.538 esc)) (cadr ln24))
	    te19  (list	(+ (car ln24) (* 7.5 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te20  (list	(+ (car ln24) (* 20.0 esc))
			(+ (cadr ln24) (* 12.5 esc))
		  )
	    te21  (list	(+ (car ln24) (* 20.0 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te22  (list	(+ (car ln24) (* 20.0 esc))
			(+ (cadr ln24) (* 2.5 esc))
		  )
	    te23  (list	(+ (car ln24) (* 36.0 esc))
			(+ (cadr ln24) (* 12.5 esc))
		  )
	    te24  (list	(+ (car ln24) (* 36.0 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te25  (list	(+ (car ln24) (* 36.0 esc))
			(+ (cadr ln24) (* 2.5 esc))
		  )
	    te26  (list	(+ (car ln24) (* 58.0 esc))
			(+ (cadr ln24) (* 12.5 esc))
		  )
	    te27  (list	(+ (car ln24) (* 58.0 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te28  (list	(+ (car ln24) (* 58.0 esc))
			(+ (cadr ln24) (* 2.5 esc))
		  )
	    te29  (list	(+ (car ln24) (* 80.0 esc))
			(+ (cadr ln24) (* 12.5 esc))
		  )
	    te30  (list	(+ (car ln24) (* 80.0 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te31  (list	(+ (car ln24) (* 80.0 esc))
			(+ (cadr ln24) (* 2.5 esc))
		  )
	    te32  (list	(+ (car ln24) (* 102.0 esc))
			(+ (cadr ln24) (* 12.5 esc))
		  )
	    te33  (list	(+ (car ln24) (* 102.0 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te34  (list	(+ (car ln24) (* 102.0 esc))
			(+ (cadr ln24) (* 2.5 esc))
		  )
	    te35  (list	(+ (car ln24) (* 124.0 esc))
			(+ (cadr ln24) (* 12.5 esc))
		  )
	    te36  (list	(+ (car ln24) (* 124.0 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te37  (list	(+ (car ln24) (* 124.0 esc))
			(+ (cadr ln24) (* 2.5 esc))
		  )
	    te38  (list	(+ (car ln24) (* 142.5 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te39  (list	(+ (car ln24) (* 160.571 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te40  (list	(+ (car ln24) (* 178.981 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te41  (list	(+ (car ln24) (* 194.660 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te42  (list	(+ (car ln24) (* 210.34 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te43  (list	(+ (car ln24) (* 226.019 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te44  (list	(+ (car ln24) (* 241.698 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    te45  (list	(+ (car ln24) (* 257.378 esc))
			(+ (cadr ln24) (* 7.5 esc))
		  )
	    ln24  (list (car ln24) (- (cadr ln24) (* 15.0 esc)))
	    ln25  (list (car ln25) (- (cadr ln25) (* 15.0 esc)))
	    ;CALCULO DOS DADOS
	    ce1	  (car p1)
	    ce2	  (cadr p1)
	    ce3	  (rtos ce1 2 3)
	    ce4	  (rtos ce2 2 3)
	    ce5	  (car p2)
	    ce6	  (cadr p2)
	    ce7	  (rtos ce5 2 3)
	    ce8	  (rtos ce6 2 3)
	    ce9	  (car p3)
	    ce10  (cadr p3)
	    ce11  (rtos ce9 2 3)
	    ce12  (rtos ce10 2 3)
	    ce13  (car p4)
	    ce14  (cadr p4)
	    ce15  (rtos ce13 2 3)
	    ce16  (rtos ce14 2 3)
	    ce17  (car p5)
	    ce18  (cadr p5)
	    ce19  (rtos ce17 2 3)
	    ce20  (rtos ce18 2 3)
	    ce21  (/ (distance p2 p4) 2)
	    ce22  (/ ce21 (distance p4 p3))
	    ce23  (* (asin ce22) 2)
	    ce23a (- 3.1415926 ce23)
	    ce24  (angtos ce23a 1 3)
	    ce25  (distance p3 p5)
	    ce26  (rtos ce25 2 3)
	    ce25a (distance p1 p3)
	    ce26a (rtos ce25a 2 3)
	    ce27  (gra ce24)
	    ce30  (distance p1 p2)
	    ce31  (rtos ce30 2 0)
	    ce32  (distance p4 p5)
	    ce33  (rtos ce32 2 0)
	    con	  (+ 1 con)
	    cor	  (rtos con 2 0)
	    txt	  (strcat "C" cor)
      )
    )
    (arc_cur_espiral)
    (setvar "osmode" 0)
    (command "_clayer"	     "ifr120"
	     "_text" "_j"    "_mc"   te19    h1	     0	     txt
	     "_clayer"	     "ifr100"
	     "_text" "_j"    "_mc"   te20    h2	     0	     "EST"
	     "_text" "_j"    "_mc"   te21    h2	     0	     "N"
	     "_text" "_j"    "_mc"   te22    h2	     0	     "E"
	     "_text" "_j"    "_mc"   te23    h2	     0	     "-"
	     "_text" "_j"    "_mc"   te24    h2	     0	     ce4;TE/PC-NORTE
	     "_text" "_j"    "_mc"   te25    h2	     0	     ce3;TE/PC-ESTE
	     "_text" "_j"    "_mc"   te26    h2	     0	     "-"
	     "_text" "_j"    "_mc"   te29    h2	     0	     "-"
	     "_text" "_j"    "_mc"   te30    h2	     0	     ce12;PI-NORTE
	     "_text" "_j"    "_mc"   te31    h2	     0	     ce11;PI-ESTE
	     "_text" "_j"    "_mc"   te32    h2	     0	     "-"
	     "_text" "_j"    "_mc"   te35    h2	     0	     "-"
	     "_text" "_j"    "_mc"   te36    h2	     0	     ce20;ET/PT-NORTE
	     "_text" "_j"    "_mc"   te37    h2	     0	     ce19;ET/PT-ESTE
	     "_text" "_j"    "_mc"   te39    h2	     0	     ce27;AC
	     "_clayer"	     "IFL2"  "_line" ln27    ln28    ""
	     "_clayer"	     "IFL2"  "_line" ln29    ln30    ""
	     "_clayer"	     "IFL3"  "_line" ln24    ln25    ""
	    )
    (if (= ce8 ce4)
      (command "_text" "_j"    "_mc"   te27    h2     0	     "-"
	       "_text" "_j"    "_mc"   te28    h2     0	     "-"
	       )
      (command "_text" "_j"    "_mc"   te27    h2     0	     ce8;EC/PC-NORTE
	       "_text" "_j"    "_mc"   te28    h2     0	     ce7;EC/PC-ESTE
	       )
      )
    (if (= ce20 ce16)
      (command "_text" "_j"    "_mc"   te33    h2     0	     "-"
	       "_text" "_j"    "_mc"   te34    h2     0	     "-"
	       )
      (command "_text" "_j"    "_mc"   te33    h2     0	     ce16;CE/PT-NORTE
	       "_text" "_j"    "_mc"   te34    h2     0	     ce15;CE/PT-ESTE
	       )
      )
    (if (= ce8 ce4)
      (command "_text" "_j"    "_mc"   te40    h2     0	     "-"
	       )
      (command "_text" "_j"    "_mc"   te40    h2     0	     ce31;LC1
	       )
      )
    (if (= ce20 ce16)
      (command "_text" "_j"    "_mc"   te41    h2     0	     "-"
	       )
      (command "_text" "_j"    "_mc"   te41    h2     0	     ce33;LC2
	       )
      )
    (if (= ce8 ce4)
      (command "_text" "_j"    "_mc"   te42    h2     0	     "-"
	       )
      (command "_text" "_j"    "_mc"   te42    h2     0	     ce26a;TS1
	       )
      )
    (if (= ce20 ce16)
      (command "_text" "_j"    "_mc"   te43    h2     0	     "-"
	       )
      (command "_text" "_j"    "_mc"   te43    h2     0	     ce26;TS2
	       )
      )
    (if (= ce8 ce4)
      (command "_text" "_j"    "_mc"   te44    h2     0	     ce26;T
	       )
      (command "_text" "_j"    "_mc"   te44    h2     0	     "-"
	       )
      )
    (princ "\n")
    (princ txt)
  )
)


(defun asin (a)
  (atan
    (/ a (sqrt (- 1.0 (* a a))))
  )
)

(defun gra (s / cnt tmp)
  (setq	cnt 0
	tmp ""
  )
  (while (< cnt (strlen s))
    (setq cnt (1+ cnt))
    (if	(= (substr s cnt 1) "d")
      (setq s	(strcat tmp "%%" (substr s cnt))
	    cnt	(strlen s)
      )
      (setq tmp (strcat tmp (substr s cnt 1)))
    )
  )
  (setq s s)
)

(defun arc_cur_simples ()
  (setq	ent  (car (entsel "\nSelecione a curva :"))
	entd (entget ent)
	raio (cdr (assoc 40 entd))
	angi (cdr (assoc 50 entd))
	angf (cdr (assoc 51 entd))
  )
  (if (> angi angf)
    (setq ang (- (* 2.0 pi) (abs (- angi angf))))
    (setq ang (abs (- angi angf)))
  )
  (setq	arcd (* raio ang)
	C21  (rtos arcd 2 3)
	C22  (rtos raio 2 3)
  )
  (setvar "osmode" 0)
  (command "_clayer"	   "IFR100"
	   "_text" "_j"	   "_mc"   T29	   h2	   0	   C21;DESENVOLVIMENTO
	   "_text" "_j"	   "_mc"   T26	   h2	   0	   C22;RAIO
	  )
)

(defun arc_cur_espiral ()
  (setq	ent  (car (entsel "\nSelecione a curva :"))
	entd (entget ent)
	raio (cdr (assoc 40 entd))
	angi (cdr (assoc 50 entd))
	angf (cdr (assoc 51 entd))
  )
  (if (> angi angf)
    (setq ang (- (* 2.0 pi) (abs (- angi angf))))
    (setq ang (abs (- angi angf)))
  )
  (setq	arcd (* raio ang)
	ce28 (rtos arcd 2 3)
	ce29 (rtos raio 2 3)
  )
  (setvar "osmode" 0)
  (command "_clayer"	   "ifr100"
	   "_text" "_j"	   "_mc"   te45    h2	   0	   ce28;DESENVOLVIMENTO
	   "_text" "_j"	   "_mc"   te38	   h2	   0	   ce29;RAIO
	  )
)

(defun camada ()
  (setq	la1 (tblsearch "layer" "IFL1")
	la2 (tblsearch "layer" "IFL2")
	la3 (tblsearch "layer" "IFL3")
	la4 (tblsearch "layer" "IFL4")
	la5 (tblsearch "layer" "IFR100")
	la6 (tblsearch "layer" "IFR120")
  )
  (if (= la1 nil)
    (command "_layer" "_new" "IFL1" "_color" "_white" "IFL1" "")
  )
  (if (= la2 nil)
    (command "_layer" "_new" "IFL2" "_color" "_yellow" "IFL2" "")
  )
  (if (= la3 nil)
    (command "_layer" "_new" "IFL3" "_color" "_green" "IFL3" "")
  )
  (if (= la4 nil)
    (command "_layer" "_new" "IFL4" "_color" "_cyan" "IFL4" "")
  )
  (if (= la5 nil)
    (command "_layer" "_new" "IFR100" "_color" "_green"	"IFR100" "")
  )
  (if (= la6 nil)
    (command "_layer" "_new" "IFR120" "_color" "_cyan" "IFR120"	"")
  )
)

(defun erro (msg)
  (prompt "\nFim da Lista:")
  (setvar "osmode" 0)
  (setq *error* er_or)
  (if (= op 1)
    (command "_clayer"	     "IFL3"
	     "_line" l21     l1	     ""
	     "_line" l26     l7	     ""
	     "_line" l29     l8	     ""
	     "_line" l22     l2	     ""
	     "_clayer"	     "IFL2"
	     "_line" l25     l13     ""
	     "_line" l27     l14     ""
	     "_line" l28     l15     ""
	     "_line" l30     l16     ""
	     "_line" l31     l17     ""
	     "_line" l32     l18     ""
	    )
    (command "_clayer"	     "ifl3"
	     "_line" ln24    ln1     ""
	     "_line" ln31    ln26    ""
	     "_line" ln32    ln5     ""
	     "_line" ln37    ln6     ""
	     "_line" ln25    ln2     ""
	     "_clayer"	     "ifl2"
	     "_line" ln33    ln11    ""
	     "_line" ln34    ln12    ""
	     "_line" ln35    ln13    ""
	     "_line" ln36    ln14    ""
	     "_line" ln38    ln15    ""
	     "_line" ln39    ln16    ""
	     "_line" ln40    ln17    ""
	     "_line" ln41    ln18    ""
	     "_line" ln42    ln19    ""
	     "_line" ln43    ln20    ""
	     "_line" ln44    ln21    ""
	    )
  )
  (command "_style" "romans" "romans" "0.0" "1.0" "0.0"	"n" "n"	"n")
  (setvar "osmode" 35)
  (setvar "clayer" "IFL2")
)