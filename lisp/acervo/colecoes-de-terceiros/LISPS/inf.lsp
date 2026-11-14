;*******************************INFO.LSP******************************
;* By:    HEINZ D. BREHM
;* Date:  11-20-92
;* Utility to interact with attributes
;*********************************************************************
(defun myerror (s)                    ; If an error (such as CTRL-C) occurs
                                      ; while this command is active...
  (princ "\nINFO completed")
  (entdel a)
  (entdel b)
  (setvar "GRIDMODE" $gr)
  (command "redraw")
  (setq *error* olderr)               ; Restore old *error* handler
  (princ)
)
(setq olderr *error*)
(setq *error* myerror)

(defun ssdxf (code n ss) (cdr (assoc code (entget (ssname ss n)))))

(defun c:info ( / $gr $vc $vs a alpha ang b dlist dmin dpos dpt eda
edb ifact ilist iplist ipt k len n xpt)

; Create 'INFO'
  (if (not (tblsearch "BLOCK" "INFO"))
  (progn
  (entmake '((0 . "BLOCK") (2 . "INFO") (70 . 0) (10 0.0 0.0 0.0)))
  (entmake '((0 . "CIRCLE")(8 . "0")(10 0.0 0.0 0.0) (62 . 5)
            (40 . 0.5)))
  (entmake '((0 . "POLYLINE")(8 . "0")(62 . 7)(40 . 0.15)(41 . 0.15)))
  (entmake '((0 . "VERTEX")(10 0.0 0.0625)))
  (entmake '((0 . "VERTEX")(10 0.0 -0.3125)))
  (entmake '((0 . "SEQEND")))
  (entmake '((0 . "POLYLINE")(8 . "0")(62 . 7)(70 . 1)(40 . 0.075)
            (41 . 0.075)))
  (entmake '((0 . "VERTEX")(10 -0.0375 0.25)(42 . 1)))
  (entmake '((0 . "VERTEX")(10 0.0375 0.25)(42 . 1)))
  (entmake '((0 . "SEQEND")))
  (entmake '((0 . "ENDBLK")))
  ));progn,if

; Create 'POINTER'
  (if (not (tblsearch "BLOCK" "POINTER"))
  (progn
  (entmake '((0 . "BLOCK") (2 . "POINTER") (70 . 0) (10 0.0 0.0 0.0)))
  (entmake '((0 . "POLYLINE")(8 . "0")(62 . 5)(40 . 0.8666)
            (41 . 0.000)))
  (entmake '((0 . "VERTEX")(10 -0.625 0.2165)(40 . 0.8666)
            (41 . 0.000)))
  (entmake '((0 . "VERTEX")(10 -0.5 0.0)(40 . 0.000)(41 . 0.8666)))
  (entmake '((0 . "VERTEX")(10 -0.625 -0.2165)(40 . 0.8666)
            (41 . 0.8666)))
  (entmake '((0 . "SEQEND")))
  (entmake '((0 . "ENDBLK")))
  ));progn,if

; Initial Settings
  (setq $gr (getvar "GRIDMODE") 
        $vs (getvar "VIEWSIZE")
        $vc (getvar "VIEWCTR")
        ilist (ssget "X" (list (cons 0 "INSERT") (cons 66 1))) 
        n 0 
        ifact (/ $vs 20)
        len (sslength ilist)
  );setq
  (repeat len
  (setq iplist (cons (ssdxf 10 n ilist) iplist) 
        n (1+ n)
  );setq
  );repeat
  (setq ipt (car (cdr (grread T))))
 



; Insert 'INFO' and 'POINTER'
  (command "insert" "INFO" ipt ifact "" 0)
  (setq a (entlast))
  (command "insert" "POINTER" ipt ifact "" 0)
  (setq b (entlast))
  (prompt 
  "\nUse pick button to edit, press Enter to exit")
  (setq eda (entget a) 
        edb (entget b)
  );setq
  (if (= $gr 1)
  (progn
  (setvar "GRIDMODE" 0)
  (command "redraw")
  );progn
  );if
  (setq alpha (setq xpt (grread T)))

; Start while loop
  (while alpha
  (setq ipt (car (cdr xpt)) 
        k 0)
  (repeat len
  (setq dlist (cons (distance ipt (nth k iplist)) dlist) 
        k (1+ k)
  );setq
  );repeat
  (setq dmin (eval (cons min dlist))
        dpos (- len (length (member dmin dlist)))
        dpt (nth (- len dpos 1) iplist) 
        ang (angle dpt ipt)
  );setq

; Update location and orientation
  (entmod (setq eda (subst (cons 10 ipt)(assoc 10 eda) eda)))
  (entmod (setq edb (subst (cons 10 ipt)(assoc 10 edb) edb)))
  (entmod (setq edb (subst (cons 50 ang)(assoc 50 edb) edb)))
  (setq dlist nil)
  (setq xpt (grread T))
  (cond
  (
  (equal (car (cadr xpt))(- (car $vc)(/ (* 1.4 $vs) 2.0)) 0.25)
  (setq alpha nil))
  ((= (car xpt) 3)(if (< (abs(- (cadr dpt)(cadr $vc)))(/ $vs 2.0))
  (command "ddatte" (ssname ilist dpos) "redraw")
  (prompt "\nObject is not in current view")))
  );cond
  );while

; Reset environment
)
  (entdel a)
  (entdel b)
  (setvar "GRIDMODE" $gr)
  (command "redraw")
  (princ)

