; *********************************************************
; IST.LSP  Indication of Surface Texture program
; Copyright (c) Barry R. Bowen 1993
; All rights reserved
; This program uses the blocks BST, MST, and MRST
; ---------------------------------------------------------
; Variables
; CK    = Variable to check for no value
; IN    = Index variable
; OM    = Current orthomode setting
; RMAX  = Maximum Roughness value
; RMIN  = Minimum Roughness value
; RU    = Roughness unit
; RVLST = List of selected R values/Grade Numbers
; STYP  = Symbol Type
; TH    = Text Height (current)
; VC    = Point at View Center
; ---------------------------------------------------------

(defun C:IST (/ CK IN RMAX RMIN STYP TH RVLST VC)
  (V3)
  (LS "SURFACE" "7" "")
  (if (= RU nil) (C:RU))
  (cond
    ((= RU "M") (setq RVLST
       (list " " "Micro-" "meters" "--------" " "
             "50" "25" "12.5" "6.3" "3.2" "1.6"
             "0.8" "0.4" "0.2" "0.1" "0.05" "0.025")))
    ((= RU "I") (setq RVLST
       (list " " " Micro-" "inches" "--------" " "
             "2000" "1000" "500" "250" "125" "63"
             "32" "16" "8" "4" "2" "1")))
    ((= RU "G") (setq RVLST
       (list " " "Grade" "Numbers" "--------" " "
             "N12" "N11" "N10" "N9" "N8" "N7" "N6"
             "N5" "N4" "N3" "N2" "N1")))
  )
  (setq TH (getvar "textsize") VC (getvar "viewctr") IN 0)
  (setq OM (getvar "orthomode")
        OSM (getvar "osmode"))
  (setvar "orthomode" 0)
  (setvar "cmdecho" 0)
  (initget 1 "B M R")
  (setq STYP (getkword "\n<B>asic/<M>achining/Material <R>emoval: "))
  (foreach N RVLST (grtext IN N) (setq IN (1+ IN)))
  (repeat 3 (grtext IN "        ") (setq IN (1+ IN)))
  (prompt "\nSelect MAX Roughness From Screen: ")
  (setq RMAX (nth (nth 1 (grread)) RVLST))
  (grtext 19 "None")
  (prompt "\nSelect MIN Roughness From Screen: ")
  (setq CK (nth 1 (grread)))
  (if (/= CK 19) (setq RMIN (nth CK RVLST)) (setq RMIN ""))
  (grtext)
  (if (= RMIN "") (progn (setq RMIN RMAX RMAX "")))
  (setvar "osmode" 512)
  (cond
    ((= STYP "B") (command "insert" "BST" VC TH TH 0 RMAX RMIN))
    ((= STYP "M") (command "insert" "MST" VC TH TH 0 RMAX RMIN))
    ((= STYP "R") (command "insert" "MRST" VC TH TH 0 RMAX RMIN))
  )
  (prompt "\nSelect Location: ")
  (command "move" (entlast) "" VC pause)
  (prompt "\nRotation Angle: ")
  (command "rotate" (entlast) "" "@" pause)
  (RL)
  (setvar "OSMODE" OSM)
  (setvar "orthomode" OM)
  (V4)
)

; ----------------------- RU --------------------------------
(defun C:RU ()
  (initget 1 "M I G")
  (setq RU
    (getkword "\n<M>icrometers/Micro<I>nches/<G>rade Numbers: "))
)

; -------------------------- V3 ----------------------------
(defun V3 ()
  (setq BM (getvar "blipmode"))
  (setvar "blipmode" 0)
  (setvar "cmdecho" 0)
  (command "undo" "group")
)

; -------------------------- V4 ----------------------------
(defun V4 (/ BA)
  (setvar "blipmode" BM)
  (command "undo" "end")
  (prompt "\n")
  (setq BA "Program Completed. . . . .")
)

; ------------------------- LS -----------------------------
; Layer search command for creating new layers.
; Variables:
; CL   = Current layer
; CLR  = Color for new layer
; FRZ  = Frozen layer
; NLAY = New layer name
; LAY  = Requested layer name
; LT   = Linetype for new layer
;
; SAMPLE CALL: (LS "BEAM" "3" "HIDDEN")
;              Where "BEAM" is the new layer name, "3" is the
;              layer color and "HIDDEN" is the linetype. An
;              empty "" can be used for a continuous
;              linetype.

(defun LS (NLAY CLR LT)
 (setq CL (getvar "clayer"))
 (setq LAY (tblsearch "layer" NLAY))
 (if (not LAY)
   (command "layer" "m" NLAY "c" CLR "" "lt" LT "" "");True
    (progn
      (setq FRZ (cdr (assoc 70 LAY)))
      (if (= FRZ 65)
        (progn
         (command "layer" "t" NLAY "")
         (command "layer" "s" NLAY "")
        )
         (command "layer" "s" NLAY "") ;False
))  ) )

; --------------------------- RL ---------------------------
(defun RL () (command "layer" "s" CL ""))

(prompt "\nIndication of Surface Texture Loaded....")
(prompt "\nType IST to execute or RU to set Values.")
(princ)
