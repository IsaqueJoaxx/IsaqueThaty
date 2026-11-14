; TIP990.LSP: LS.LSP   Set Layer   (c)1994, C. Carr

;;;******************
;;; This program allows you to set your layer
;;; by just typing the name. If the layer doesn't exist you can create it
;;; by specifying the color and linetype, without preloading the linetype
;;; C.CARR - 44W 191 Main - Elburn, IL - 60119
;;;******************
;;;************

(defun c:LS () 
  (setvar "cmdecho" 0)
  (setq RNLR 1 
        ls:err *error*)
  (defun *error* () 
    (msg) 
    (setq *error* ls:err)
    (setvar "cmdecho" 1)
  ) 
  (if (= (setq LYR (getstring "\nPress RETURN to select entity <Layer Name> "))
       ""
      ) 
    (progn
      (while RNLR 
        (setq LAYRSET (entsel "\nSelect enity on target layer: "))
        (if (/= LAYRSET nil) 
          (setq RNLR nil)
          (princ "\nNo enity found")
        )
      ) 
      (setq LYR (cdr (assoc 8 (entget (car LAYRSET)))))
    )                                ;END PROGN 
    (if (= (tblsearch "LAYER" LYR) nil) 
      (progn
        (princ (strcat "LAYER '" LYR "' doesn't exist")) 
        (initget 1 "C S") 
        (setq LSASK (getkword (strcat "\n(C)reate new layer<" LYR 
                                    ">/(S)earch for existing layer: "
                              )
                    ))
        (if (= LSASK "C") 
          (progn
            (if (= (setq LYRCLR (getstring (strcat "\nColor of Layer " LYR 
                                                 " <white>: "
                                           )
                                ))
                 ""
                ) 
              (setq LYRCLR "WHITE")
            ) 
            (if (= (setq LYRLTP (getstring (strcat "\nLinetype for Layer " LYR 
                                                 " <continuous>: "
                                           )
                                ))
                 ""
                ) 
              (setq LYRLTP "CONTINUOUS")
            ) 
            (command "LAYER" "N" LYR "C" LYRCLR LYR "LT" LYRLTP LYR "")
          )                          ; END "c"
          (progn
            (command "LAYER" "?" "" "") 
            (setq LYR (getstring "\n Choose existing Layer Name: "))
          )
        )                            ; END "s" END IF LSASK
      )
    )
  )                                  ; END PROGN END IF TBLSEARCH ;END IF LYR
  (command "layer" "s" LYR "") 
  (terpri) 
  (setq *error* ls:err)
  (setvar "cmdecho" 1)
  (princ)
) 
;;;**************

