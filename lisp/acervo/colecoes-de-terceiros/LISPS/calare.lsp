;;; Calarea
;;;
;;; Guido M Haarmans, Neuchatel Switzerland, July 4, 1994.
;;; CIS account 73324,1133
;;;
;;; Calarea, calculates the total area of a selection set
;;; of closed polylines (non-polylines and open polylines
;;; are automatically rejected)
;;;
;;; Freeware, permission to modify and distribute as long as you
;;; leave my credit in and don't charge for it.

;calarea error handler
   (defun aesc_calarea_myerror (msg)
    (princ (strcat "\nCalarea: " msg))
    (setq *error* aesc_calarea_olderror) 
    (list (setvar "cmdecho" aesc_calarea_cmdo))   
    (princ "\n")(princ) 
   )

; ---------------------------------------------------------------
(defun c:calarea ( / sset setlen totarea 
                     totper reject cnt ent entlist)
   (graphscr)

   'get cmdecho and set to 0
   (setq aesc_calarea_cmdo (getvar "cmdecho"))
   (setvar "cmdecho" 0)
   
   ;error handler
   (setq aesc_calarea_olderror *error*)
   (setq *error* aesc_calarea_myerror)
   
   ;get selection set of polylines
   (prompt "\nCalculate area:")
   (setq sset (ssget))

   ;number of entities in polyline
   (setq setlen 0)               ;initiate setlen
   (if sset (setq setlen (sslength sset))) 
    
   (setq totarea 0.0) ;total area
   (setq totper 0.0)  ;total perimeter
   (setq reject 0)    ;non polyline entities
   (setq cnt 0)       ;counter for selection set

   (princ "\nCalculating area ")
   (while (< cnt setlen)
       ;get next entity in selection set
       (setq ent (ssname sset cnt))
       (setq cnt (1+ cnt))
       ;reject non-polylines and open polylines
       (setq entlist (entget ent))
       (if (and 
           (= (cdr (assoc 0 entlist)) "POLYLINE") ; is a pline
           (= (rem (cdr (assoc 70 entlist)) 2) 1) ; and closed
           )
             (progn                              ;calculate area
             ;area command
             (command "_.area" "_e" ent)
             ;add area to totarea
             (setq totarea (+ totarea (getvar "area")))
             (setq totper  (+ totper (getvar "perimeter")))
             )
             ;else
             (setq reject (1+ reject))          ; add 1 to reject
         );end if
   );end while
   
   (textpage)
   (princ (strcat
           "\n\nCalarea statistics:\n"
           "\nNumber of entities selected = " 
            (itoa setlen)
           "\nEntities rejected           = " 
            (itoa reject) 
           "\nTotal area                  = " 
            (rtos totarea 2 3)
           "\nTotal perimeter             = "
            (rtos totper 2 3)
          )
   )
    (setvar "cmdecho" aesc_calarea_cmdo)
    (prompt "\n")
    (setq *error* aesc_calarea_olderror)
    (princ)
)
(prompt "\nCalarea loaded, type CALAREA to start command.")(princ)