;-----------------------------------------------------------------
; TXTEND.LSP -- Determines Endpoint of any Text Entity
;            -- Brought to you by Looking Glass Microproducts
;
; (txtend elist) returns endpoint of a text entity passed to
;                the function as an entity list
;
;----------------------------------------------------------
(defun txtend (textent / just tempent p0 p1 p2 item)
   (defun item (n e)
      (cdr (assoc n e))
   )
   (if (= "TEXT" (item 0 textent))
      (progn
         (setq
             just    (item 72 textent)              ; justification flag
             p0      (item 10 textent)              ; left hand text point
             tempent (subst (cons 72 2)             ; right justified text entity
                            (assoc 72 textent)
                            textent)
         )
         (entmod tempent)                           ; right justify text
         (setq
             tempent (entget (item -1 tempent))     ; retrieve it
         )
         (entmod textent)                           ; restore the text
         (setq
             p1      (item 10 tempent)              ; left text point
             p2      (item 11 tempent)              ; right text point
         )
         (polar p0 (angle  p1 p2) (distance p1 p2)) ; right hand text point
      )
   )
)
