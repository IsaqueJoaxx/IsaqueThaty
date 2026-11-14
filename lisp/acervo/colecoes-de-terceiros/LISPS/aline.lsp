;
;  ALINE.lsp  Line with an attribute.
;
;  By: Roger S. Behymer     02-10-88
;                         BSI Consultants, Inc.
;                         16880 W. Bernardo Dr.
;                         San Diego, CA  92127
;
;                         (619) 451-6100
;
(defun D-LINE ()
(setvar "BLIPMODE" 0)
    (setq DIST-T (distance PT1 PT2))
    (if (null DISTDEF) (setq DISTDEF 50))
    (setq DISTDEFS (itoa DISTDEF))
    (initget (+ 2 4))
    (setq DIST (getint 
               (strcat "\nDistance between attributes <" DISTDEFS ">: ")))
    (if (= DIST nil) (setq DIST DISTDEF) (setq DISTDEF DIST))
    (if (> DIST (* DIST-T 0.5)) (progn
         (princ "\n** ERROR **   Distance is too great for length of ALINE.")
         (princ "\n              Please try again.\n")
         (D-LINE)))
    (setq ANG-R (angle PT1 PT2))
    (setq ANG-D (rtd ANG-R))
    (setq NUM (fix (/ DIST-T DIST)))
    (setq H-BRK (* BRK 0.5))
    (setq T-PT1 PT1)
    (setq COUNT 1)
    (while (<= COUNT NUM)
         (setq T-PT2 (polar T-PT1 ANG-R DIST))
         (setq T-PT3 (polar T-PT2 (- ANG-R pi) H-BRK))
         (command "LINE" T-PT1 T-PT3 "")
         (ATT)
         (setq T-PT1 (polar T-PT2 ANG-R H-BRK))
         (setq COUNT (1+ COUNT))
    )
    (command "LINE" T-PT1 PT2 "")
    (setvar "BLIPMODE" 1)
)

(defun ATT ()
    (if (= TORB "T") 
         (command "TEXT" "M" T-PT2 HT ANG-D TXT)
         (command "INSERT" BLK T-PT2 "" "" ANG-D))
)

(defun TEXT ()
    (if (= HTDEF nil) (setq HTDEF 1.5))
    (setq HTDEFST (rtos HTDEF 2 2))
    (initget (+ 2 4))
    (setq HT (getreal (strcat "\nText Height <" HTDEFST ">: ")))
    (if (= HT nil) (setq HT HTDEF) (setq HTDEF HT))
    (if (= TXTDEF nil) (setq TXTDEF "W"))
    (setq TXT (getstring (strcat "\nText <" TXTDEF ">: ")))
    (if (= TXT "") (setq TXT TXTDEF) (setq TXTDEF TXT))
    (if (= BRKDEF nil) (setq BRKDEF (* HT 1.5)))
    (setq BRKDEFST (rtos BRKDEF 2 2))
    (setq BRK (getreal (strcat "\nBreak Width <" BRKDEFST ">: ")))
    (if (= BRK nil) (setq BRK BRKDEF) (setq BRKDEF BRK))
    (command "TEXT" "S" "STANDARD")
    (command)
)

(defun BLOCK ()
    (if (= BLKDEF nil) (setq BLKDEF "CL")) ; Replace "CL" with any block name.
    (setq BLK (getstring (strcat "\nBlock to Insert <" BLKDEF ">: ")))
    (if (= BLK "") (setq BLK BLKDEF) (setq BLKDEF BLK))
    (if (= BRKDEF nil) (setq BRKDEF 2.0))
    (setq BRKDEFST (rtos BRKDEF 2 2))
    (setq BRK (getreal (strcat "\nBreak Width <" BRKDEFST ">: ")))
    (if (= BRK nil) (setq BRK BRKDEF) (setq BRKDEF BRK))
)
;  Radians to degrees conversion.
(defun rtd (a) (/ (* a 360) (* 2 pi)))
;
;  Main Function
;
(defun C:ALINE ()
    (setvar "CMDECHO" 0)
    (setvar "HIGHLIGHT" 0)
    (setq PT1 (getpoint "\nFrom point: "))
    (setq PT2 (getpoint PT1 "\nTo point: "))
    (if (= TORB nil) (setq TORBDEF "T"))
    (setq TORB (strcase (getstring 
           (strcat "\n[T]ext attribute or [B]lock <" TORBDEF ">: "))))
    (if (= TORB "") (setq TORB TORBDEF) (setq TORBDEF TORB))
    (if (= TORB "T") (TEXT) (BLOCK))
    (D-LINE)
    (setvar "CMDECHO" 1)
    (setvar "HIGHLIGHT" 1)
(princ)
)
