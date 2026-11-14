;--- listb -------------------------------------------------

; Here's a routine that will walk though a block defintion, displaying the 
; contents.

; list the entities in a block definition <bname>
; if entity <etype> is not nil, then list only that type
; example: (listb "MYBLOCK" nil) for all entities, or
;          (listb "MYBLOCK" "ATTDEF") for only attdefs
;
;
(defun listb (bname etype / dxf data wait current)

   (defun dxf (x)(cdr (assoc x data)))

   (defun wait ()
      (print data)
      (setq current data)
      (terpri)
      (if (and 
             (setq data (entnext (dxf -1)))   
             (= 13 (cadr (grread)))
          )
          (setq data (entget data))
          (setq data nil)
      )
   );wait
   
   (prompt "\nPress RETURN for next entity or any key to stop.")
   (terpri)
   (print (setq data (tblsearch "block" bname)))
   (terpri)
   (setq data (dxf -2)               ; get first entity
         data (entget data)          ; get assoc list
   )
   (if etype (setq etype (strcase etype)))
   (while data
      (cond
         (etype 
            (if (= etype (dxf 0))
                (wait)      
                (if (setq data (entnext (dxf -1)))   
                    (setq data (entget data))
                    (setq data nil)
                )
            )
         )
         (T (wait))
      );cond
   );while
   current
)
;
;-- atts ---------------------------------------------------
; get only attribute defintions for all blocks
;
(defun c:atts (/ bname etype)
   (setq bname (tblnext "block" T))
   (while bname
      (listb (cdr (assoc 2 bname)) "ATTDEF")
      (setq bname (tblnext "block"))
   )      
   (princ)
)
;
;--- c:listb ----------------------------------------------
; pick a block (or DIM or hatch) to list
;
(defun c:listb (/ ent dxf data)

   (defun dxf (x)(cdr (assoc x data)))

   (cond
   ((setq ent (entsel "Pick a block INSERT: "))
      
       (setq data (entget (car ent)))
       (textscr)
       (if (or
              (= "INSERT" (dxf 0))
              (= "DIMENSION" (dxf 0))
              (= "HATCH" (dxf 0))
           )
          (progn
             (print data)
             (terpri)
             (initget "ATtdef Insert Circle Arc Line Polyline") 
             (setq etype (getkword 
                "\nEntity type  ATtdef/Insert/Circle/Arc/Line/Polyline: "
             ))
             (if (or (null etype)(= etype ""))
                (setq etype nil)
                (setq etype (strcase etype)); make capitals
             )
             (listb (dxf 2) etype)
          )
          (prompt " ... not a block definition.")
       )
   )
   (T (prompt "... no entity found."))
   )
   (princ)
)
;--- end of file ---------------------------------------------
