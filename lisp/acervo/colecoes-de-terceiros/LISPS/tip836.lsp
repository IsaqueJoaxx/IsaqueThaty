;TIP836:  PERPL.LSP (c)1993, Bernd Hoffmann

(Defun C:PERPL (/ SA SB SNP OM OS PT1 PT2)
  (setvar "cmdecho" 0)
  (setq
    SA  (getvar "snapang")
    SB  (getvar "snapbase")
    SNP (getvar "snapmode")
    OM  (getvar "orthomode")
    OS  (getvar "osmode")
    PT1 (osnap (getpoint
        "\nPick point on line to draw perpendicular from: "
      )
      "nea"
    )
  )
  (setvar "osmode" 0)
  (setq PT2 (osnap PT1 "end"))
  (if (equal PT1 PT2)
    (setq PT2 (osnap PT1 "MID"))
  )
  (command ".snap" "r" PT1 PT2)
  (setvar "snapmode" 0)
  (setvar "orthomodE" 1)
  (prompt "\nto point:")
  (command ".line" PT1 pause "")
  (setvar "snapang" SA)
  (setvar "snapbase" SB)
  (setvar "snapmode" SNP)
  (setvar "orthomode" OM)
  (setvar "osmode" OS)
  (setvar "cmdecho" 1)
  (princ)
); end perpl.lsp
