(defun c:NA (/ SP EP CE BM ANG LN1
    LN2 LN3 P1 P2 P3 P4 P5 P6 P7 P8 N1
    N2 N3 N4 T1 T2)
  (setq
    SP (getpoint "\nArrow's south end:")
    EP (getpoint "\nNorthern end: ")
    CE (getvar "CMDECHO")
    BM (getvar "BLIPMODE")
  )
  (setvar "CMDECHO" 0)
  (setvar "BLIPMODE" 0)
  (setq
    ANG (angle SP EP)
    LN1 (distance SP EP)
    LN2 (/ LN1 16)
    LN3 (/ LN2 2)
    P1 (polar SP ANG (* LN2 3))
    P2 (polar p1 ANG LN3)
    P3 (polar sp ANG (/ LN1 3))
    P4 (polar p3 ANG LN3)
    P5 (polar p4 ANG LN2)
    P6 (polar p5 ANG LN3)
    P7 (polar p6 ANG LN2)
    P8 (polar p7 ANG LN3)
    P9 (polar p7 (+ ANG (/ pi 2)) LN3)
    N1 (polar p4 (+ ANG (/ pi 2))
      (* LN3 0.7))
    N2 (polar N1 ANG LN2)
    N3 (polar p4 (+ ANG (* pi 1.5))
      (* LN3 0.7))
    N4 (polar P5 (+ ANG (* pi 1.5))
      (* LN3 0.7))
    T1 (polar P1 (+ ANG (/ pi 2)) LN3)
    T2 (polar T1 (+ ANG pi) (* LN2 3.5))
  )
  (command
    "LINE" SP P3 ""
    "LINE" P6 EP ""
    "LINE" N1 N2 N3 N4 "")
    (command
      "line" EP P8 P9 EP "" ""
      "line" T2 T1 P2 SP T2 "" "")
    (setvar "BLIPMODE" BM)
  (setvar "CMDECHO" CE)
  (princ)
)
¯†÷îﬂíz'ºÇπ}G8üÒìL$π∆e˘¶†˝oµõÎ`wÅ&˜∫3˜Ûò»æ(0]B)≈ºMû¨}Y—ÀRBÓ'Â¸ûNo‹—|Ü
züØ¿?0Ro1ÛŒÛ<Ç44km>sæ≈ßˇªÀ‚È