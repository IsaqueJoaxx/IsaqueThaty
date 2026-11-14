; Steven Jarvis
; Master-Bilt Products
;
; AUTOCAD TEXT EDITOR
;
;      Key Definitions
;	     F2 = Rotate      Ctrl ->
;	     F3 = Move	      Ctrl <-
;	     F4 = Copy	      Delete
;	     F5 = New Ht.     Insert (press twice to toggle)
;	     Backspace
;
;PConsulting added the error correction the rest of the credit goes to
;the above

;------------------------- Error Function ---------------------------------

(defun PCONERR (st)
 (if (/= st "Function cancelled")
    (princ (strcat "\nError: "s))
 )
 (moder)                 ;  If prog. fails reset varibles
 (setq *error* olderr)
 (princ)
)

;-------- Mode Save ----- Saves variables in a list that you specify

(defun MODES (a)
   (setq MLST '())
   (repeat (length a)
      (setq MLST (append MLST (list (list (car a) (getvar (car a))))))
      (setq a (cdr a)))
)
;-------- Mode Reset ----- Resets saved system variables
(defun MODER ()
   (repeat (length MLST)
      (setvar (caar MLST) (cadar MLST))
      (setq MLST (cdr MLST))
   )
)
;---------------------------------------------------------------------------
 (setq olderr *error* *error* PCONERR)
 (modes '("cmdecho"))                     ; Change these as needed
  (mapcar 'setvar
          '("cmdecho")                    ;make these match the above
          '(0) 
  )
;---------------------------------------------------------------------------
(defun C:EDITT ( / Pointer Pb Ins Back Del Text Ht NewHt OldHt
		  Pick Epick Put Col Oldtext En Ip)
 (setvar "cmdecho" 0)
 (while (not En) (terpri)
   (setq En (entget (car (setq Ip (entsel))))
	 Ip (cadr Ip)))
 (cond
   ((= (cdr (assoc 0 En)) "INSERT")
     (command "ddatte" Ip) )
   ((= (cdr (assoc 0 En)) "TEXT")
       (setq Back '(setq Text (strcat (substr Text
				   (if (= Col 1) 2 1)
				   (if (= Col 1)(1- (strlen Text))(- Col 2) ))
			 (if (= Col 1) "" (substr Text Col))))
	     Del '(setq Text (strcat (substr Text
				   (if (= Col 1) 2 1)
				   (if (= Col 1)(1- (strlen Text))(1- Col) ))
			 (if (= Col 1) "" (substr Text (1+ Col)))))
	     Put '(setq Text (if (= Col 1)
			  (strcat (chr Pick)(substr Text (if (= Ins 1) 1 2)))
			  (strcat (substr Text 1 (1- Col))(chr Pick)
				  (substr Text (if (= Ins 1) Col (1+ Col))))))
	     Ht '(progn (setq En (subst (cons 40
		   (if (= nil (setq NewHt (getdist (cdr (assoc 10 En))
		     (strcat "\nNew Height <"
		     (rtos (setq OldHt (cdr (assoc 40 En))) 2 2) "> : \n"))))
		   OldHt NewHt))(assoc 40 En) En))(entmod en))
	     Col 1 pick 0 Ins 0 tpick '(1)
	     OldText (setq Text (cdr (assoc 1 En)))
	     OldEn En)
      (while (and (/= 13 Pick)(/= 27 Pick))
	(redraw (cdr (assoc -1 En)) 3)
	(princ (strcat "\n" (if (= 1 Ins) "Inssert" "Replace") " Mode\n"))
	(princ Text)(terpri)
	(princ
	  (strcat
	    (if (= Col 1) ""
	      (repeat (1- Col)
		(setq Pointer (strcat (if Pointer Pointer "") " ")))) "^"))
	(while (not (member (car Tpick) '(2 3 4 6)))
	  (setq Pick (if (= 4 (car (setq Tpick (grread)))) 300 (cadr Tpick)))
	  (if (or (= Pick 188)(= Pick 189)(= Pick 190))(progn
	    (setq Pb (getvar "pickbox"))
	    (command (cond ((= Pick 188) "rotate") ;determine command
			   ((= Pick 189) "move")
			   ((= Pick 190) "copy"))
	      (cdr (assoc -1 En)) ""               ;select objects
	      (cdr (assoc (if (= 0 (cdr (assoc 72 En))) 10 11) En));base pt.
	      pause
	    );command
	    (setvar "pickbox" 10)
	    (setq En (entget (ssname (ssget
	      (if (= Pick 188) Ip (setq Ip (getvar "lastpoint")))) 0)))
	    (setvar "pickbox" Pb)
	  ));if,progn
	);while
	(setq Pointer nil Tpick nil)
	(setq Col
	  (cond
	    ((= Pick 243) (max 1 (1- Col)))		    ; <- key
	    ((= Pick 8) (eval Back)(max 1 (1- Col)))	    ; Backspace
	    ((= Pick 244) (min (1+ Col)(1+ (strlen Text)))) ; -> key
	    ((= Pick 211) (eval Del) Col)		    ; Delete
	    ((= Pick 191) (eval Ht) Col)		    ; New Height
	    ((and (< Pick 127)(> Pick 31))		    ; Ascii keystroke
	      (eval Put)(min (1+ Col)(1+ (strlen Text))))
	    ((= Pick 27) (setq Text OldText En OldEn) Col)  ; Esc
	    ((and (= (type Pick) 'LIST)(not (null (ssget Pick)))) ; Text pick
	      (setq Epick (entget (ssname (ssget Pick) 0))
		    Text (cdr (assoc 1 Epick))
		    Pick 13)
	      Col)
	    ((= Pick 300) (setq Ins (abs (1- Ins))) Col) ; Insert toggle
	    ((= Pick 0) (setq Pick 13) Col)		 ; Buttons return
	    (t Col)
	) );cond,setq
      );while
    (setq En (subst (cons 1 Text)(assoc 1 En) En))
    (entmod En)
    (princ)
  );cond TEXT
  (t (prompt "\nInvalid Entity Selection... ")(terpri))
;---------------
 (moder)                                                 ;resets varibles 
 (setq *error* olderr) 
 (princ)
 );opening cond
);Edit
