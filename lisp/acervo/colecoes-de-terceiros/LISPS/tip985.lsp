; TIP985.LSP: LAYER OFF   Turn Layers Off   (c)1994, Paul Cazier

; This routine turns off a selected object's layer
; or, multiple selected object's layers,
; but was not designed to turn off the current layer.

(defun layr ()
  (while (/= OBJ nil)
    (setq OBJ (entsel "/Select Object, to turn layer off: "))
   (if
      (= OBJ nil)
        (prompt "\No Object Selected.")
          (progn
           (setq OBJ1 (entget (car OBJ)))
           (setq LAY (cdr(assoc 8 OBJ1)))
           (setq LYR (strcat LYR LAY))
           (setq LYR (strcat LYR ","))
          )
   )
  )
)
(defun C:LAYEROFF()
  (setq LYR "")
  (setq OBJ "T")
  (LAYR)
  (command "LAYER" "OFF" LYR "" "")
)
