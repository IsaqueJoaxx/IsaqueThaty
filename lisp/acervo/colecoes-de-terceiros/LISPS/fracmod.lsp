
; FRACMOD command - swaps a single char. fraction for 4 char. fraction.
;                   It's a modified CHGTEXT command and REQUIRES Rel. 9.
;
;        *** This command requires that text style on the selected  ***
;        *** layer be defined in the .dwg using the fonts "DIMPLEX" ***
;        *** or "DOMPLEX" (or another font w/ a - g setup as frac-  ***
;        *** tions). DIMPLEX & DOMPLEX are incl'd in the .arc file. ***
; 
;                   The fonts are modified ACAD ver.2.6 SIMPLEX and
;                   COMPLEX. The fractions 1/8 to 7/8 (in 1/8 increments)
;                   have been substituted for the lowercase letters a - g.
;                   We use all CAP's in our dimensioning and consider it
;                   a minor miracle if the contractors can hold 1/8 inch
;                   tolerences. You can modify the .shp files and the 
;                   look-up table in FRACMOD to suit your needs.
;
;                   Run the command CHGSTYLE first if you need to change
;                   the text style to one that uses a font w/fractions.
;                   
;
; SWITCH - swaps NewString for OldString in the FRACMOD command.

(defun SWITCH (ns)
  (setq s (strcat (substr s 1 (1- si)) ns
                  (substr s (+ si osl))))
  (setq chf t)    ; Found old string
  (setq si (+ si nsl))
)

(defun C:FRACMOD (/ p cl n e os as ns st s nsl osl sl si chf chm)
  (terpri)
  (if (setq osl (entsel "\n  Select an item on the layer to be modified : "))
    ;then#1
     (progn                               ;progn#t1
     (setq osl (cdr (assoc 8 (entget (car osl)))))
     (setq chm 0 p (ssget "X" (list (cons 0 "TEXT")(cons 8 osl))))
                                          ;Selects all text on layer ARDIM
     (if p                                ;if#2
        (progn              ;progn#t2      If any objects selected
        (setq osl 4 nsl 1)                ;Recycles the variable osl
        (setq cl 0 n (sslength p))        ;Set the counters for size of SSet
        (while (< cl n)                   ;while#1  For each selected object...
           (progn                         ;progn#w1
           (setq e (entget (ssname p cl)))         ;Get next entity in SSet
           (setq chf nil si 1)                     ;Set a marker and a counter
           (setq s (cdr (setq as (assoc 1 e))))    ; Get the text
           (while (= osl (setq sl (strlen (setq st (substr s si osl)))));while#2
              (cond                       ;cond#1   Tests for cond of
              ((= st " 1/8")(switch "a"))          ;several common arch.
              ((= st " 1/4")(switch "b"))          ;fractions used by the
              ((= st " 3/8")(switch "c"))          ;dim cmd and calls SWITCH
              ((= st " 1/2")(switch "d"))          ;to substitute a single
              ((= st " 5/8")(switch "e"))          ;char frac for dim's
              ((= st " 3/4")(switch "f"))          ;4 char frac's
              ((= st " 7/8")(switch "g"))
              )                                    ;end cond#1
              (setq si (1+ si))
           )                                       ;end while#2
           (if chf                                 ;if#3 If there was a SWITCH
              (progn                 ;progn#t3           make it permanent.
              (setq e (subst (cons 1 s) as e))
              (entmod e)                           ; Modify the TEXT entity
              (setq chm (1+ chm))
              )                                    ;end progn#t3
           )                                       ;end if#3
           (setq cl (1+ cl))
           )                                       ;end progn#w1
        )                                          ;end while#1
      )                                            ;end progn #t2
     ))                                            ;end if#2 & progn#t1
     ;else#1
     (progn (terpri)(prompt "\nMissed!")(C:fracmod))
  )                                                ;end if#1
   (princ)                                         ;exit cleanly
)

; CHGSTYLE -  changes the text style of all text on a user selected layer
;             to a user spcified text style. This requires that the text
;             style be defined.
;
; Variables Schedule
;    dl      - layer selected to be modified.
;    ts      - new text style
;    c       - selection set of text ent's on dimlyr.
;    d       - index marker for position in sel. set.
;    e       - # of entities in the selection set.
;    f       - variable assigned to each entity in sel. set.

(defun C:CHGSTYLE (/ dl ts c d e f)
  (terpri)
  (setq dl (entsel "\nselect item on layer to be modified :"))
  (terpri)
  (if dl                                ;if#1
     ;then#1
     (progn                             ;progn#t1
     (setq dl (cdr (assoc 8 (entget (car dl)))))
     (if (tblsearch "STYLE" (setq ts (strcase (getstring "\nNew style : "))))
        ;then#2
        (progn                          ;progn#t2
        (setq c (ssget "X" (list (cons 0 "TEXT")(cons 8 dl))))
        (setq d 0 e (sslength c))
        (while (< d e)               ;while#1
           (progn                    ;progn#w1
           (setq f (entget (ssname c d)))
           (setq f (subst (cons 7 ts) (assoc 7 f) f))
           (entmod f)
           (setq d (1+ d))
           )                         ;end progn#w1
        )                            ;end while#1
        (prompt "\nAll done.")
        )                              ;end progn#t2
       ;else#2
        (progn
        (terpri)
        (prompt (strcat "\nText style " ts " is not defined in this drawing."))
        )
     ))                                 ;end if#2 & progn#t1
     ;else#1
     (progn (prompt "\nMissed!")(C:chgstyle))
  )                                     ;end if#1
  (princ)                               ;exit cleanly
)

