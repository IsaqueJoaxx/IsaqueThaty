; SETPROP.LSP

; Written by Sri Vaidyanathan
; Bechtel Corp.
; 50 Beale St., PO Box 3965,
; San Francisco, CA 94119-3965, USA
; 415-768-1234
; 11/18/91

; Set element creation properties (such as color, linetype, etc.)
; to match a specified entity.  This routine displays the entity's
; properties and asks the user to confirm that these are the settings
; that the user wants to make current.

; -------------------------------------------------

(defun *error* (s)
(setvar "cmdecho" 1)
(if (and (/= s "") (/= s "Function cancelled"))
    (princ (strcat "\nError: " s))
)
(princ)
)

;e - elem list
;p,p1 - pts
;s,c - string
;f - font
;ce,os - sys var

(defun setprop (/ e p p1 s c f ce os)
(setq ce (getvar "cmdecho"))
(setq os (getvar "osmode"))
(setvar "cmdecho" 0)
(setvar "osmode" 0)
(setvar "highlight" 1)
(setq e nil)

(while (null e)
 (setq e (tblsearch "style" (getvar "textstyle")))      ;get current style
 (if e (progn
        (setq p (cdr(assoc 40 e))) ;height
        (setq p1 (cdr(assoc 41 e))) ;width factor
        (setq s (cdr(assoc 3 e))) ;font
       )
       (progn   ;default
        (setq p 0)
        (setq p1 1)
        (setq s "")
       )
 )
 (if (= p 0)  (setq p (getvar "textsize"))) ;var ht
 (setq p1 (* p p1))
 (prompt (strcat "\nCurrent Layer: " (getvar "clayer") "  Linetype: "
         (getvar "celtype") "  Color: " (getvar "cecolor") "\nTxt style: "
         (getvar "textstyle") "/" s "  height: " (rtos p) "  width: " (rtos p1)
 ))

 (prompt "\nMatch settings to element <select>/cancel to exit")
 (setq e (entsel ""))
 (if e (progn
        (setq p (cadr e))  ;pick pt
        (setq e (entget(car e))) ;elem list
        (if (= (cdr(assoc 0 e)) "INSERT") (progn
            (command "explode" (cdr(assoc -1 e)))
            (setq e (ssget p))
            (if e (setq e (entget (ssname e 0))) ) ;elem list
            (command "u") ;undo
        )) ;end if ins progn

        (if e (progn
            (setq s (cdr (assoc 6 e)))          ;linetype
            (if (null s) (setq s "BYLAYER"))
            (setq c (cdr (assoc 62 e)))         ;color
            (if (null c) (setq c "BYLAYER") 
                (if (= c 0) (setq c "BYBLOCK") (setq c (itoa c)))
            )
            (prompt (strcat "\nEntity: " (cdr(assoc 0 e)) "  Layer: " (cdr(assoc 8 e))
                         "  Linetype: " s "  Color: " c))

            (setq p1 (cdr(assoc 0 e)))
            (if (or (= p1 "TEXT") (= p1 "ATTRIB") (= p1 "ATTDEF")) (progn
                (setq p (cdr(assoc 40 e))) ;height
                (setq p1 (* p (cdr(assoc 41 e)))) ;width
                (setq f (cdr(assoc 7 e)))  ;style
                (setq f (tblsearch "style" f))
                (if f (setq f (cdr(assoc 3 f))) (setq f "")) ;font
                (prompt (strcat "\nText style: " (cdr(assoc 7 e)) "/" f "  height: "
                                 (rtos p) "  width: " (rtos p1)))
            )) ;end if txt

            (prompt "\nClick to accept, else reject")
            (setq p (grread))
            (cond ((= (car p) 3) p)  ;click
                  ((and (= (car p) 2) (= (cadr p) 13)) p)  ;RETURN key
                  ((and (= (car p) 2) (= (cadr p) 32)) p)  ;SPACE bar
                  ((= (car p) 6) (grread) (setq p nil e nil)) ;ignore Button menu click
                  (T (setq p nil e nil))  ;reject
            )

            (if p (progn
                  (command "layer" "s" (cdr(assoc 8 e)) "") ;chg layer
                  (if (/= s "BYBLOCK") (command "linetype" "s" s ""))
                  (if (/= c "BYBLOCK") (command "color" c))
                  (setq p1 (cdr(assoc 0 e))) ;elem type

                  (if (or (= p1 "TEXT") (= p1 "ATTRIB") (= p1 "ATTDEF")) (progn
                    (setq p1 (cdr(assoc 7 e)))  ;cur style
                    (setq p (cdr(assoc 40 e))) ;height

                    (setq f (tblnext "style" T))  ;get first style
                    (while f  ;place dummy txt to set size for each style
                     (if (= (cdr(assoc 40 f)) 0) (progn  ;vary height
                         (command "text" "s" (cdr(assoc 2 f)) (list 0 0 0) p "" ".")
                         (entdel (entlast))  ;delete dummy txt
                     ))
                     (setq f (tblnext "style"))
                    )

                    (command "style" p1 "" "" "" "" "" "" "") ;set cur style
                  )) ;end if txt

                  (prompt "\ndone!\007")  
            )) ;end if p progn

        )) ;end if e progn

 )) ;end if e progn
) ;end while

(setvar "cmdecho" ce)
(setvar "osmode" os)
(princ))        
