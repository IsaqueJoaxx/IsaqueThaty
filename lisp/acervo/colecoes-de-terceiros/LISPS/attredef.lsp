; *******************************************************************
;                          ATTREDEF.LSP
;
; This program allows you to redefine a Block and update the
; Attributes associated with any previous insertions of that Block.
; All new Attributes are added to the old Blocks and given their
; default values. All old Attributes with equal tag values to the new
; Attributes are redefined but retain their old value. And all old
; Attributes not included in the new Block are deleted.
;
; Note that if handles are enabled, new handles will be assigned to
; each redefined block.
;
; Written by Karry Layden - May 1988
; *******************************************************************


; Oldatts sets "old" to the list of old Attributes for each Block.
; The list does not include constant Attributes.

(defun oldatts (/ an e)
   (setq an (entnext b1))
   (setq e (entget an))
   (while (and (= (cdr (assoc 0 e)) "ATTRIB")
               (member (cdr (assoc 70 e)) '(0 1 4 5 8 9 12 13)))
      (if old
         (setq old (cons e old))
         (setq old (list e))
      )
      (setq an (entnext an))
      (if an
         (setq e (entget an))
      )
      (setq count (1+ count))         ; count the number of old atts
   )
)

; Newatts sets "new" to the list of new Attributes in the new Block.
; The list does not include constant Attributes.

(defun newatts (ssetn l / i an e)
   (setq i 0)
   (while (<= i l)
      (setq an (ssname ssetn i))
      (setq e (entget an))
      (if (and (= (cdr (assoc 0 e)) "ATTDEF")
               (member (cdr (assoc 70 e)) '(0 1 4 5 8 9 12 13)))
            (progn
               (if new
                  (setq new (cons e new))
                  (setq new (list e))
               )
               (setq n (1+ n))        ; count the number of new atts
            )
      )
      (setq i (1+ i))
   )
)

; Compare the list of "old" to the list of "new" Attributes and make
; the two lists "same" and "preset". "Same" contains the old values of
; all the Attributes in "old" with equal tag values to some Attribute
; in "new" and the default values of all the other Attributes. "Preset"
; contains the preset Attributes in old with equal tag values to some
; Attribute in new.

(defun compare (/ i j)
   (setq i 0
         j 0
         eds 0
         same nil
         amount 0
         preset nil)
   (while (<= i (1- n))
      (cond ((= (cdr (assoc 2 (nth j old))) (cdr (assoc 2 (nth i new))))
                (if (member (cdr (assoc 70 (nth i new))) '(8 9 12 13))
                   (progn
                      (if preset
                         (setq preset (cons (nth j old) preset))
                         (setq preset (list (nth j old)))
                      )
                      (setq eds (1+ eds)) ; count equal preset atts
                   )
                   (if same
                      (setq same (cons (cdr (assoc 1 (nth j old))) same))
                      (setq same (list (cdr (assoc 1 (nth j old)))))
                   )
                )
                (if (member (cdr (assoc 70 (nth i new))) '(4 5))
                   (setq amount (+ 1 amount))
                )
                (setq i (1+ i))
                (setq j 0)
             )
             ((= j (1- count))
                (if (not (member (cdr (assoc 70 (nth i new))) '(8 9 12 13)))
                   (if same
                      (setq same (cons (cdr (assoc 1 (nth i new))) same))
                      (setq same (list (cdr (assoc 1 (nth i new)))))
                   )
                )
                (if (member (cdr (assoc 70 (nth i new))) '(4 5))
                   (setq amount (+ 1 amount))
                )
                (setq i (1+ i))
                (setq j 0)
             )
             (t
                (setq j (1+ j))
             )
      )
   )
)

; Find the entity for each of the "preset" Attributes in the newly
; inserted Block.

(defun findpt ()
   (setq test T)
   (setq en (entnext e1))
   (setq e (entget en))
   (while test
      (if (and (= (cdr (assoc 0 e)) "ATTRIB") (= (cdr (assoc 2 e)) tag))
         (setq test nil)
         (progn
            (setq ex en)
            (setq en (entnext ex))
            (if e
               (setq e (entget en))
            )
         )
      )
   )
)

; Insert a new Block on top of each old Block and set its new Attributes
; to their values in the list "same". Then replace each of the "preset"
; Attributes with its old value.

(defun redef (/ xsf ysf zsf ls i e1 v)
   (command "ucs" "e" b1)             ; define the block's UCS
   (setq xsf (cdr (assoc 41 (entget b1)))) ; find x scale factor
   (setq ysf (cdr (assoc 42 (entget b1)))) ; find y scale factor
   (setq zsf (cdr (assoc 43 (entget b1)))) ; find z scale factor
   (setq ls (1- (length same)))
   (setq i 0)
   (command "insert" bn "0.0,0.0,0.0" "XYZ" xsf ysf zsf "0.0")
   (while (<= i ls)                   ; set attributes to their values
      (command (nth i same))
      (setq i (1+ i))
   )
   (while (< 0 amount)
      (command "")                    ; at prompts, verify attributes
      (setq amount (1- amount))
   )
   (setq i 0)
   (setq e1 (entlast))
   (while (< 0 eds)                   ; edit each of the "preset" attributes
      (setq tag (cdr (assoc 2 (nth i preset))))
      (setq v (cdr (assoc 1 (nth i preset))))
      (findpt)                        ; find the entity to modify
      (setq e (subst (cons 1 v) (assoc 1 e) e))
      (entmod e)                      ; modify the entity's value
      (setq i (1+ i))
      (setq eds (1- eds))
   )
   (command "ucs" "p")                ; restore the previous UCS
)

; System variable save

(defun modes (a)
   (setq mlst '())
   (repeat (length a)
      (setq mlst (append mlst (list (list (car a) (getvar (car a))))))
      (setq a (cdr a)))
)

; System variable restore

(defun moder ()
   (repeat (length mlst)
      (setvar (caar mlst) (cadar mlst))
      (setq mlst (cdr mlst))
   )
)

; Internal error handler

(defun attrerr (s)                    ; If an error (such as CTRL-C) occurs
                                      ; while this command is active...
   (if (/= s "Function cancelled")
      (princ (strcat "\nError: " s))
   )
   (moder)                            ; restore saved modes
   (setq *error* olderr)              ; restore old *error* handler
   (princ)
)

; Main program

(defun C:ATTREDEF (/ k n olderr bn sseto ssetn pt l new
                     old same presets b1 count amount)
   (setq k 0
         n 0
         test T
         olderr *error*
         *error* attrerr)

   (modes '("CMDECHO" "ATTDIA" "ATTREQ" "GRIDMODE" "UCSFOLLOW"))
   (setvar "cmdecho" 0)               ; turn cmdecho off
   (setvar "attdia" 0)                ; turn attdia off
   (setvar "attreq" 1)                ; turn attreq on
   (setvar "gridmode" 0)              ; turn gridmode off
   (setvar "ucsfollow" 0)             ; turn ucsfollow off

   (while test
      (setq bn (strcase (getstring "\nName of Block you wish to redefine: ")))
      (if (null (setq sseto (ssget "X" (list (cons 2 bn)))))
         (progn
            (princ "\nBlock ")
            (princ bn)
            (princ " is not defined. Please try again.")
         )
         (setq test nil)
      )
   )
   (setq test T)
   (while test
      (princ "\nSelect new Block... ")
      (if (null (setq ssetn (ssget)))
         (princ "\nNo new Block selected. Please try again.")
         (setq test nil)
      )
   )
   (initget 17)
   (setq pt (getpoint "\nInsertion base point of new Block: "))
   (setq l (1- (sslength sseto)))
   (newatts ssetn (1- (sslength ssetn))) ; find the list of new attributes
   (command "block" bn "Y" pt ssetn "")  ; redefine the block
   (while (<= k l)
      (setq b1 (ssname sseto k))      ; For each old block...
      (setq old nil)
      (setq count 0)
      (oldatts)                       ; find the list of old attributes,
      (compare)                       ; compare the old list with the new,
      (redef)                         ; and redefine its attributes.
      (entdel b1)                     ; delete the old block.
      (setq k (1+ k))
   )
   (moder)                            ; restore saved modes
   (command "regenall")
   (setq *error* olderr)              ; restore old *error* handler
   (princ)
)
