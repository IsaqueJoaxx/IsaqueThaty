; TIP854A: MLF.LSP (c)1993, Bernd Hoffman

(Defun C:MLF (/ SS EN ED LA)
   (Setvar "CMDECHO" 0)
   (Princ
      "Freeze multiple layers."
   )
   (Setq SS (SsGet))
   (While (> (SsLength SS) 0)
         (Setq
            EN (SsName SS 0)
            ED (EntGet EN)
            LA (CDR (Assoc 8 ED))
         )
         (If (/= LA (GetVar "CLAYER"))
               (Command
                  ".LAYER" "FREEZE" LA ""
               )
            (Prompt
               "\nCan not freeze current layer!"
            )
         )
         (SsDel EN SS)
   )
   (Setvar "CMDECHO" 1)
   (Princ)
)
