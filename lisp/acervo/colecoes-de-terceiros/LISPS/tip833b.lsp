;TIP833B:  MMLINE.LSP (c)1993, Bernd Hoffmann

(Defun C:MMLINE (/ PT1 ANG MM LEN)
  (setvar "cmdecho" 0)
  (setq
    PT1 (getpoint
      "\nStart point of line: "
    )
    ANG (getangle "\nAngle of line: ")
    MM (getreal "\nLength of line in mm: ")
    LEN (* MM 0.0393701)
  )
  (command
    ".line" PT1
    (polar PT1 ANG LEN) ""
  )
  (setvar "cmdecho" 1)
  (princ)
); end mmline.lsp
