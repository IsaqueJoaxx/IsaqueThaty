; TIP854B: MLO.LSP (c)1993, Bernd Hoffman

(Defun C:MLO (/ SS EN ED LA)
   (Setvar "CMDECHO" 0)
   (Princ
      "Turn multiple layers off."
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
                  ".LAYER" "OFF" LA ""
               )
            (Prompt
               "\nCurrent layer will not be turned off!"
            )
         )
         (SsDel EN SS)
   )
   (Setvar "CMDECHO" 1)
   (Princ)
)
