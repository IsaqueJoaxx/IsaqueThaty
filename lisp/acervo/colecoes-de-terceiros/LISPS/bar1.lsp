;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; ADVANCED AUTOLISP      MAY 1993   CADENCE        Bill Kramer
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Listing 1:  BLOCK ATTRIBUTE EXTRACT FUNCTION
; Parameter EN:
;   if STRING, returns block definitions ((tag prompt)...)
;   if ENAME, returns insert definitions ((tag value)...)
;
(defun GET_BK_ATTRS (EN / EL EX RES CD)
  (cond
    ((= (type EN) 'ENAME) ;; EN is entity name, get inserted data
       (setq EL (entget EN)
             EX '(/= (cdr (assoc 0 EL)) "SEQEND") ;; loop condition
             CD 1
             EN  ;; EN is set to result of nested conditional
          (if (= (cdr (assoc 0 EL)) "INSERT") ;; block insert?
            (if (= (cdr (assoc 66 EL)) 1)     ;; with attributes?
              (entnext EN) ;;set EN to first attrib entity name
              (prompt "\nGET_BK_ATTRS error, no attributes") ;;EN is nil
            )
            (prompt "\nGET_BK_ATTRS error, entity is not a block insert")
          )
       )
    )
    ((= (type EN) 'STR)   ;; EN is string, get block def data
       (setq EN (tblsearch "BLOCK" EN)
             EX '(boundp (quote EL))
             CD 3
       )
       (if EN ;; found block definition
         (setq EN (cdr (assoc -2 EN)))
       )
    )
    (t
       (setq EN nil
             EX nil)
       (prompt "\nGET_BK_ATTRS error, parameter invalid")
    )
  )
  (if EN
    (progn  ;; walk through block def/insert attribs
       (setq EL (entget EN))
       (while (eval EX)
          (if (= (substr (cdr (assoc 0 EL)) 1 3) "ATT")
             (setq RES (cons (list (cdr (assoc 2 EL))
                                   (cdr (assoc CD EL)))
                             RES))
          )
          (setq EN (entnext EN)
                EL (if EN (entget EN) nil)
          )
       )
    )
  )
  (reverse RES)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Listing 2:  Block Attribute Report type 1
;
(defun C:BAR1 ( / BLK SS1 TABS S ITM CNT TAB DAT)
  (setq BLK (strcase (getstring "\nBlock name containing attributes: ")))
  (if (/= BLK "") (progn
     (setq TITLES (GET_BK_ATTRS BLK) ;; list of attribute tags/defs
           SS1 (ssget "X" (list '(0 . "INSERT") (cons 2 BLK))))
     (if (and TITLES SS1)
       (progn
         (setq S "")
         (foreach ITM TITLES
           (setq TABS (if TABS (cons (+ 3 (strlen S)) TABS) (list 0))
                 S (strcat S 
                           (if (= (strlen S) 0) "" "   ")
                           (cadr ITM))))
         (write-line S)
         (setq ITM (strlen S)
               TABS (reverse TABS)
               S "")
         (repeat ITM
            (setq S (strcat S "-")))
         (write-line S)
         (setq CNT 0)
         (repeat (sslength SS1)
           (setq TITLES (GET_BK_ATTRS (ssname SS1 CNT))
                 CNT (1+ CNT)
                 S "")
           (mapcar
             '(lambda (DAT TAB)
                 (while (< (strlen S) TAB)
                    (setq S (strcat S " ")))
                 (if (> (strlen S) TAB)
                    (setq S (substr S 1 TAB)))
                 (setq S (strcat S (cadr DAT)))
             )
             TITLES TABS)
           (write-line S)
         ) ;; end REPEAT loop
       ) ;; end of PROGN
       (cond ;;failures
         ((null TITLES) (prompt "\nBlock definition not valid"))
         ((null SS1) (prompt "\nNo insertions of block found"))
       )
     )
  )) ;; end BLK not blank if test
  (princ)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
; Listing 3:  Block Attribute Report type 2
;
(defun C:BAR2 ( / TAG BLKS T1 T2 T3 BLK SS1 CNT)
  (setq TAG (strcase (getstring "\nEnter tag name to count: ")))
  (if (/= TAG "")
    (progn
       (prompt "\nBlock definitions scan...")
       (setq BLKS nil
             T1 (tblnext "BLOCK" 'T))
       (while T1
         (setq T2 (GET_BK_ATTRS (cdr (assoc 2 T1))))
         (if (assoc TAG T2)
           (setq BLKS (cons (cdr (assoc 2 T1)) BLKS)))
         (setq T1 (tblnext "BLOCK"))
       )
       (prompt (strcat "\nFound " (itoa (length BLKS))
                       " block definitions with attrbiute tag "
                       TAG))
       (foreach BLK BLKS
         (prompt (strcat "\n\nBLOCK: " BLK))
         (setq SS1 (ssget "X" (list '(0 . "INSERT") (cons 2 BLK))))
         (if SS1
           (progn
              (prompt (strcat " inserted " (itoa (sslength SS1)) " time[s]"))
              (setq T1 (ssname SS1 (1- (sslength SS1))))
              (while (and SS1 T1) ;;work selection set of inserts
                (setq T2 (GET_BK_ATTRS T1) ;;get values for ename T1
                      T3 (strcase (cadr (assoc TAG T2))) ;;value in T3
                      CNT (- (sslength SS1) 2) ;;set backwards ss index
                      INC 1) ;;counter for tag value usage
                (ssdel T1 SS1) ;;remove last entity from SS
                (repeat (if SS1 (sslength SS1) 0)
                  (setq T1 (ssname SS1 CNT)  ;;get ename
                        T2 (GET_BK_ATTRS T1) ;;and attrib list
                  )
                  (if (= (strcase (cadr (assoc TAG T2))) T3) ;;values equal
                    (progn
                      (ssdel (ssname SS1 CNT) SS1) ;;yes, remove from SS
                      (setq INC (1+ INC))))        ;;increment INC
                  (setq CNT (1- CNT)) ;;go to next member in SS, backwards
                );; end repeat
                (prompt 
                   (strcat "\n  Value: " T3 " ["
                           (itoa INC) "]"))
                (if SS1 
                   (setq T1 (ssname SS1 (1- (sslength SS1))))
                   (setq T1 nil))
              );;end while SS1
           );;end progn
           (prompt " not inserted")
         );; end if ss1
       ) ;; end foreach BLK
    ) ;; end progn
  ) ;; end if
  (princ)
)
