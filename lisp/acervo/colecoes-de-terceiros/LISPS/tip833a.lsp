;TIP #833A: CMLINE.LSP (c)1993, Bernd Hoffmann

(DEFUN C:CMLINE (/ PT1 ANG CM LEN)
  (setvar "cmdecho" 0)
  (setq
    PT1 (getpoint
      "\nStart point of line: "
    )
    ANG (getangle "\nAngle of line: ")
    CM (getreal "\nLength of line in cm: ")
    LEN (* CM 0.393701)
  )
  (Command
    ".line" PT1
    (polar PT1 ANG LEN) ""
  )
  (setvar "cmdecho" 1)
  (princ)
); end cmline.lsp
