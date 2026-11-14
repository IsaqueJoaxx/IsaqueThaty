; TIP989.LSP: COPY2L.LSP    Copy Selections to a Layer
;                            (c)1994, David O. Storvick

; A simple program to copy entities to a layer
; for drafting purposes. Leaves linework on original layer
; Type COPY2L to execute program.
;
   (defun C:COPY2L ( / SS1 NLAYER PT1 PT2)
      (setq PT1 '(0.0 0.0 0.0))
      (setq PT2 '(0.0 0.0 0.0))
      (setvar "cmdecho" 0)
      (prompt "Objects to Copy:")
      (setq SS1(ssget))
      (setq NLAYER(getstring "Copy Objects to What Layer?"))
      (command "copy" SS1 "" PT1 "" PT2)
      (command "change" SS1 "" "P" "LAYER" NLAYER "")
      (princ)
   )

