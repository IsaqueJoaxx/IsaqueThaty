;***** INSTABLE.LSP *****;
;
;++Last Update 05/28/93++
  
;{{/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\}}
;{{{{ Send Comments or Questions (or Donations!) }}}}
;{{{{                                            }}}}
;{{{{ To:                                        }}}}
;{{{{     Owen Wengerd                           }}}}
;{{{{     P.O. Box 84                            }}}}
;{{{{     Fredericksburg, OH  44627              }}}}
;{{{{     (216) 695-5903                         }}}}
;{{{{     71324,3252 (Compu-Serve ID)            }}}}
;{{\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/}}


(defun errexit (s)
  (princ "\nError:  ")
  (princ s)
  (restore)
)

(defun instablex ()
  (setq  errexit   nil
	 instablex nil
	 ipoffset  nil
	 gettable  nil
	 INSTABLE  nil
  )
  (setvar "TEXTSIZE" (nth 1 oldvar))
  (setvar "REGENMODE" (nth 2 oldvar))
  (setvar "EXPERT" (nth 3 oldvar))
  (setvar "BLIPMODE" (nth 4 oldvar))
  (setvar "CMDECHO" (car oldvar))
  (setq *error* olderr)
  (princ)
)

(defun ipoffset (ox oy)
  (list
    (apply '+ (cons (* scale ox) (list (car ip))))
    (apply '+ (cons (* scale oy) (cdr ip)))
  )
)

(defun gettable (/ fh table t2)
  (if
    (setq table
      (getfiled
	"Select Table To Insert"
	(strcat (if *LISPPATH *LISPPATH "") "*")
	"tbl"
	258
      )
    )
    (progn
      (setq fh (open table "r"))
      (setq table nil)
      (while
	(setq t2 (read-line fh))
	(setq table (cons t2 table))
      )
      (close fh)
      (setq table (reverse table))
      (setq rows (list (atoi (car table)) (atoi (cadr table))))
      (setq columns (list (atoi (nth 2 table)) (atoi (nth 3 table))))
      (setq table (cddddr table))
      (setq t2 (+ (car columns) (if (= -1 (cadr columns)) 1 (cadr columns))))
      (setq center nil)
      (repeat t2
	(progn
	  (setq center (cons (car table) center))
	  (setq table (cdr table))
	)
      )
      (setq center (reverse center))
      (setq width nil)
      (repeat t2
	(progn
	  (setq width (cons (atof (car table)) width))
	  (setq table (cdr table))
	)
      )
      (setq width (reverse width))
      (setq t2 (+ (car rows) (if (= -1 (cadr rows)) 1 (cadr rows))))
      (setq height nil)
      (repeat t2
	(progn
	  (setq height (cons (atof (car table)) height))
	  (setq table (cdr table))
	)
      )
      (setq height (reverse height))
      (setq textheight (atof (car table)))
      (setq upright (/= "nil" (cadr table)))
      (setq rowbyrow (/= "nil" (caddr table)))
      (setq contents (cdddr table))
      (setq table nil)
      (setq t2 (not nil))
    )
    (setq table nil)
  )
)


(defun INSTABLE (/ rows columns center width height textheight ph upright txt
		   rowbyrow contents cnt pos nr nc tw th d1 d2 n t1 t2 t3 t4
		   x y alin sc scale scin oldvar s ip olderr restore BORDER)

  (setq oldvar
    (list
      (getvar "CMDECHO")
      (getvar "TEXTSIZE")
      (getvar "REGENMODE")
      (getvar "EXPERT")
      (getvar "BLIPMODE")
    )
  )
  (setq olderr  *error*
	restore instablex
	*error* errexit
  )
  (setvar "CMDECHO" 0)
  (setvar "REGENMODE" 1)
  (setvar "EXPERT" 0)
  (setvar "BLIPMODE" 0)

  (setq BORDER '1)      ;Change ['1] to [nil] for partial border around table

  (terpri)
  (if (gettable)
    (progn
      (graphscr)
      (if
	(not (setq ip (getpoint "\nInsertion Point: ")))
	(setq ip (getvar "LASTPOINT"))
      )
      (setq ip (list (car ip) (cadr ip)))
      (terpri)
      (initget 0 "LL LR UL UR ll lr ul ur")
      (if
	(not (setq alin (getkword "\nAlignment Corner (<LL>/LR/UL/UR): ")))
	(setq alin "LL")
      )
      (setq alin (strcase alin))
      (if (> *DWGSCALE 0) (setq scale (/ 1 *DWGSCALE)) (setq scale 1))
      (initget 6)
      (if
	(and
	  (setq scin
	    (getreal
	      (strcat
		"Enter Drawing Scale  <"
		(rtos (/ 1 scale) 2 3)
		">:  "
	      )
	    )
	  )
	  (/= 0 scin)
	)
	(setq scale (/ 1 scin))
      )
      (initget 4)
      (if (= -1 (last columns))
	(if
	  (not (setq nc (getint "Enter No. of Data Columns  <1>:  ")))
	  (setq nc 1)
	)
	(setq nc 0)
      )
      (if (= -1 (last rows))
	(if
	  (not (setq nr (getint "Enter No. of Data Rows  <1>:  ")))
	  (setq nr 1)
	)
	(setq nr 0)
      )
      (setq tw
	(+
	  (apply '+ (cdr (reverse width)))
	  (if (= -1 (cadr columns)) (* (last width) nc) (last width))
	)
      )
      (setq th
	(+
	  (apply '+ (cdr (reverse height)))
	  (if (= -1 (cadr rows)) (* (last height) nr) (last height))
	)
      )
      (if
	(= "R" (substr alin 2))
	(setq ip (ipoffset (- 0 tw) 0))
      )
      (if
	(= (substr alin 1 1) "U")
	(setq ip (ipoffset 0 (- 0 th)))
      )
      (if BORDER
	(command "LINE"
	  ip (ipoffset tw 0) (ipoffset tw th) (ipoffset 0 th) ip ""
	)
	(progn
	  (if
	    (= (substr alin 1 1) "U")
	    (command "LINE"
	      ip
	      (ipoffset tw 0)
	      ""
	    )
	    (command "LINE"
	      (ipoffset 0 th)
	      (ipoffset tw th)
	      ""
	    )
	  )
	  (if
	    (= (substr alin 2 1) "L")
	    (command "LINE"
	      (ipoffset tw 0)
	      (ipoffset tw th)
	      ""
	    )
	    (command "LINE"
	      (ipoffset 0 th)
	      ip
	      ""
	    )
	  )
	)
      )
      (setq nr (+ (car rows) (if (= -1 (cadr rows)) nr (cadr rows))))
      (setq nc
	(+ (car columns) (if (= -1 (cadr columns)) nc (cadr columns)))
      )
      (setq ct 0)
      (setq t1 0)
      (while (< ct (1- nr))
	(setq t1
	  (+ t1 (if (< ct (length height)) (nth ct height) (last height)))
	)
	(command "LINE"
	  (ipoffset 0 (setq n (if upright (- th t1) t1)))
	  (ipoffset tw n)
	  ""
	)
	(setq ct (1+ ct))
      )
      (setq ct 0)
      (setq t1 0)
      (while (< ct (1- nc))
	(setq t1
	  (+ t1 (if (< ct (length width)) (nth ct width) (last width)))
	)
	(command "LINE"
	  (ipoffset t1 th)
	  (ipoffset t1 0)
	  ""
	)
	(setq ct (1+ ct))
      )
      (setq ct 0)
      (setq t3 0)
      (if
	rowbyrow
	(setq t1 height t2 width)
	(setq t1 width t2 height)
      )
      (setq pos 0)
      (setq cnt nil)
      (if
	(= 0
	  (setq ph
	    (cdr (assoc '40 (tblsearch "style" (getvar "textstyle"))))
	  )
	)
	(setq ph nil)
	(setq textheight (/ ph scale))
      )
      (while (< ct (if rowbyrow nr nc))
	(setq t3
	  (+ t3 (setq d1 (if (< ct (length t1)) (nth ct t1) (last t1))))
	)
	(setq d1 (/ d1 2))
	(setq ct1 0)
	(setq n 0)
	(setq sc pos)
	(while (< ct1 (if rowbyrow nc nr))
	  (setq n
	    (+ n (setq d2 (if (< ct1 (length t2)) (nth ct1 t2) (last t2))))
	  )
	  (setq d2 (/ d2 2))
	  (setq x (if rowbyrow (- n d2) (- t3 d1)))
	  (setq y
	    (if upright
	      (- th (if rowbyrow (- t3 d1) (- n d2)))
	      (if rowbyrow (- t3 d1) (- n d2))
	    )
	  )
	  (setq txt (nth pos contents))
	  (setq t4
	    (if
	      (and
		(/= "" txt)
		(/= "@" txt)
		(= "@" (substr txt 1 1))
		(member
		  (setq t4 (strcase (substr txt 2 1)))
		  (list "L" "C" "R")
		)
	      )
	      (progn (setq txt (substr txt 3 (- (strlen txt) 2))) t4)
	      (progn
		(if (< (if rowbyrow ct1 ct) (length (if rowbyrow t2 t1)))
		  (nth (if rowbyrow ct1 ct) center)
		  (last center)
		)
	      )
	    )
	  )
	  (if
	    (and (< 1 (strlen txt)) (= "+" (substr txt (strlen txt) 1)))
	    (setq cnt
	      (if (assoc (setq s (substr txt 1 (1- (strlen txt)))) cnt)
		(subst 
		  (cons
		    s
		    (setq txt
		      (if (or (= "0" s) (< 0 (atoi s)))
			(itoa (1+ (atoi (cdr (assoc s cnt)))))
			(chr (1+ (ascii (cdr (assoc s cnt)))))
		      )
		    )
		  )
		  (assoc s cnt)
		  cnt
		)
		(append cnt (list (cons (setq txt s) s)))
	      )
	    )
	  )
	  (if
	    (= txt "")
	    (progn
	      (grdraw
		(ipoffset (- x 0.1) (- y 0.1))
		(ipoffset (+ x 0.1) (+ y 0.1))
		-1
	      )
	      (grdraw
		(ipoffset (- x 0.1) (+ y 0.1))
		(ipoffset (+ x 0.1) (- y 0.1))
		-1
	      )
	      (initget 0)
	      (setq txt (getstring x "Enter Text For Marked Cell:  "))
	      (grdraw
		(ipoffset (- x 0.1) (+ y 0.1))
		(ipoffset (+ x 0.1) (- y 0.1))
		-1
	      )
	      (grdraw
		(ipoffset (- x 0.1) (- y 0.1))
		(ipoffset (+ x 0.1) (+ y 0.1))
		-1
	      )
	    )
	  )
	  (if
	    (and
	      (/= "" txt)
	      (/= "@" txt)
	      (= "@" (substr txt 1 1))
	      (member
		(strcase (substr txt 2 1))
		(list "L" "C" "R")
	      )
	    )
	    (setq
	      t4  (strcase (substr txt 2 1))
	      txt (substr txt 3 (- (strlen txt) 2))
	    )
	  )
	  (if
	    (= t4 "L")
	    (command "TEXT"
	      (ipoffset
		(- x (- (if rowbyrow d2 d1) textheight))
		(- y (/ textheight 2))
	      )
	    )
	    (command "TEXT"
	      (if
		(= t4 "R")
		"R"
		"M"
	      )
	      (if
		(= t4 "R")
		(ipoffset
		  (+ x (- (if rowbyrow d2 d1) textheight))
		  (- y (/ textheight 2))
		)
		(ipoffset x y)
	      )
	    )
	  )
	  (if ph (command "0" txt) (command (* scale textheight) "0" txt))
	  (setq ct1 (1+ ct1))
	  (if
	    (or
	      (>= ct1 (if rowbyrow nc nr))
	      (<
		ct1
		(+
		  (car (if rowbyrow columns rows))
		  (if
		    (=
		      -1
		     (cadr (if rowbyrow columns rows))
		    )
		    1
		    (cadr (if rowbyrow columns rows))
		  )
		)
	      )
	    )
	    (setq pos (1+ pos))
	  )
	  (if (>= pos (length contents)) (setq pos sc))
	)
	(setq ct (1+ ct))
      )
    )
  )
  (restore)
)

(INSTABLE)
