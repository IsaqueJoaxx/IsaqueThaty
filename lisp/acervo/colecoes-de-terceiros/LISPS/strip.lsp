; STRIP.LSP Version 1.0                                        November 1, 1988
; By Jamie Clay
;
; This routine will examine a drawing for redundant LINEs and proceed to
; to remove them. This is designed to reduce the size of drawings created
; by AutoCAD and AutoShade's DXB output.  
;
; The process is simple, STRIP takes a LINE and adds the start and end points 
; to create a match value.  It will then create a selection set based on a
; window defined by the start and end points.  Using the match value it will
; find other identical lines in the selection set and erase them when found.
;
;
(defun c:STRIP (/ a b c erased count sum1 sum2 index)
  (setq count 0)
  (setq erased 0)
  (setq a (entnext))
  (while (/= (cdr (assoc 0 (entget a))) "LINE") ;WHILE #1 - Find first line
    (setq a (entnext a))
  )                                             ;END WHILE #1
  (setvar "cmdecho" 0)
  (while a                           ; WHILE #2 - While A is true
    (setq pass (strcat "Pass: " (rtos count 2 0)))
    (grtext -2 pass)
    (setq sum1 (sum a))              ; Create window points and verification sum
    (setq b (ssget "w" ptA ptB))     ; Get all entities in this window
    (ssdel a b)                      ; Remove first line from selection set
    (if (> (sslength b) 0)           ; IF #1  - If more than one line 
      (progn                           ; PROGN #1
        (if (= (sslength b) 1)           ; IF #2
          (progn                           ; PROGN #2
            (if (= sum1 (sum (ssname b 0)))  ;IF #3
              (progn                           ;PROGN #3
                (command "erase" b "")
                (setq erased (1+ erased))
                (prompt (strcat "\rRemoved lines :" (rtos erased 2 0)))  
              )                                ; END PROGN #3
            )                                ; END IF #3
          )                                ; END PROGN #2
          (progn                           ; PROGN #4
            (setq index (1- (sslength b)))
            (setq c 0)
            (repeat (sslength b)           ; REPEAT #1
              (setq c (ssname b index))
              (setq sum2 (sum c))
              (if (= sum1 sum2)            ; IF #4
                (progn                     ; PROGN #5
                  (entdel c)
                  (setq erased (1+ erased))
                  (prompt (strcat "\rRemoved lines :" (rtos erased 2 0)))  
                  (setq index (1- index))
                )                          ; END PROGN #5
                (setq index (1- index))
              )                            ; END IF #4
            )                              ; END REPEAT #1
          )                                ; END PROGN #4
        )                                  ; END IF #2
        (setq a (next a))
      )                                    ; END PROGN #2
      (setq a (next a))
    )                                      ; END IF #1
    (setq count (1+ count))
  )                                        ; END WHILE #2
  (command "redraw")
  (setvar "cmdecho" 1)
  (prompt "\nFinished!")
  (princ)
)                                          ; END DEFUN

;======================= SUPPORTING DEFUNS =====================
;
; Find the next Line
(defun NeXT (x)
   (setq b nil)
   (setq x (entnext x))
   (if x
     (if (/= (cdr (assoc 0 (entget x))) "LINE")
       (while (/= (if x (cdr (assoc 0 (entget x))) "LINE") "LINE")
         (setq x (entnext x))
       )
     )

   )
   (eval x)
)

; Create a match value based on the lines start and end points
(defun sum (x)
  (setq ptA (cdr (assoc 10 (entget x))))
  (if (assoc 11 (entget x)) 
    (progn
      (setq ptB (cdr (assoc 11 (entget x))))
      (setq sumX (+ (+ (car ptA) (cadr ptA)) (+ (car ptB) (cadr ptB))))
    )
  )
) 

; All done we is loaded
(prompt "\nC:STRIP - Loaded!")
(princ)
