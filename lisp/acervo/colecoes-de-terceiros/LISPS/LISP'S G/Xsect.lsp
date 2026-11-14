;============= XSECT.LSP =========================== Rev: 1999/10/04 ========
;
;  Copyright 1999 by:  Norman F. Taylor
;                      5546 8B Ave., Delta, BC   V4M 1V6
;                      Tel: (604)948-1198   Fax: (604)948-9642
;                      Email: nandp_taylor@bc.sympatico.ca
;
;  Permission to use, copy, modify, and distribute this software and its
;  documentation for any purpose and without fee is hereby granted.
;
;  THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED WARRANTY.
;  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR PURPOSE AND OF
;  MERCHANTABILITY ARE HEREBY DISCLAIMED.
;
;============= DOCUMENTATION ================================================
;
;  This routine generates a cross-section from a defined <cutting plane> and
;  a <selection of objects>.
;
;  The <cutting plane> is defined by a line, 2D-polyline or 3D-polyline.  It
;  is assumed to extend normal to the XY-plane of the current UCS, and to be
;  of infinite extent in the Z-direction.  Polylines may have multiple
;  segments.
;
;  The <selection of objects> can consist of lines, 2D-polylines,
;  3D-polylines, meshes, 3Dfaces and solids.  The routine picks off the
;  Z-coordinates (in the current UCS) of their intersections with the
;  <cutting plane>, and orders them by 2D-distance from the initial vertex
;  of the object defining the <cutting plane>.  The objects in the
;  <selection of objects> do not have to be parallel to the XY-plane of the
;  current UCS.
;
;  The section is drawn using the current layer, color and linetype settings.
;  Axis text is in the current style.  If the current text style uses a fixed
;  height, then the text is scaled to the specified height.  When prompted
;  for the section location point, remember that the axis text and ticks will
;  be placed both to the left and below the pick point.  Vertical lines,
;  representing (cutting plane> endpoints, are also drawn and labelled.
;
;  Note 1:  In many contour drawings, the contours are all at the same
;  elevation.  The contours used for the section must be at the elevation
;  they are representing.
;
;  Note 2:  The routine analyzes polylines and meshes for vertices as they
;  appear on screen: control points are not used.  Arc segments in polylines
;  are ignored, but the arc endpoints are used.
;
;  Note 3:  The files "xsect.lsp" and "xsect.dcl" should both be located in a
;  directory that is on AutoCAD's support file search path.  If AutoCAD can't
;  find "xsect.dcl", then the command line interface is used for input to the
;  routine.
;
;============= LIBRARY FUNCTIONS ============================================

;=== Sets modes of list A
;
(defun ModNu (A)
   (foreach X A (setvar (car X) (cdr X)))
)

;=== Begins command
;
(defun StartNFT (S V / J)
   (setq
      OldErr  *error*
      *error* StdErrNFT
      UNDOIT  nil
      ~MODLST (mapcar '(lambda (X) (cons X (getvar X)))
                 (append (list "CMDECHO" "BLIPMODE") S)
              )
      ~LSPVER (atof (substr (ver) 18))          ;AutoLISP version as real
      ~UCSINI (if (>= ~LSPVER 10.0)             ;UCS at start
                 (if (= (getvar "WORLDUCS") 1)  ;  restore with (UcsBk)
                    (list "W")
                    (list
                       "3P" (getvar "UCSORG")
                       (getvar "UCSXDIR") (getvar "UCSYDIR")
                    )
                 )
              )
      ~FILLST nil   ;list of open file handles
   )
   (setvar "CMDECHO" 0)
   (command ".UNDO" "G")
   (setq J 0)
   (foreach X V (if X (setvar (nth J S) X)) (setq J (1+ J)))
)

;=== Terminates command and error handler
;
(defun EndNFT (F)
   (command ".UNDO" "E")
   (if F (command ".U"))
   (setq
      *error* OldErr
      OldErr  nil
      UNDOIT  nil
      ~LSPVER nil
      ~UCSINI nil
      ~FILLST nil
   )
   (ModNu ~MODLST)   ;restore sysvars
   (princ)
)

;=== Error handler while command active
;
(defun StdErrNFT (X / L)
   (foreach L ~FILLST (close L))
   (setq L (list "Function cancelled" "console break" "quit / exit abort"))
   (if (not (member X L)) (prompt (strcat "\n** Error: " X)))
   (command nil nil)
   (EndNFT UNDOIT)
)

;=== Determines if bits of B are set in datum A
;
(defun BitSet (A B)
   (= (logand A B) B)
)

;=== Returns data from item N of assoc list E
;
(defun Item (N E)
   (cdr (assoc N E))
)

;=== Returns list of N elements S
;
(defun MkLst (S N / L)
   (setq L nil)
   (repeat N (setq L (cons S L)))
)

;=== As-drawn 2D-lwpolyline vertex list function
;
;  argument: N - entity name of 2D-lwpolyline
;  returns:  list of 2D-vertices (as drawn) in ECS coords
;
(defun Pl1Lst (N / L P V Z)
   (setq
      L (entget N)
      Z (list (Item 38 L))    ;elevation as list
      V nil                   ;init vertex list
   )
   (while (setq P (assoc 10 L))    ;find vertices...
      (setq
         V (cons (append (cdr P) Z) V)    ;inc ECS vertex list
         L (cdr (member P L))             ;dec ent assoc list
      )
   )
   (reverse V)
)

;=== As-drawn 2D-polyline vertex list function
;
;  argument: N - entity name of 2D-polyline
;  returns:  list of 2D-vertices (as drawn) in ECS coords
;
(defun Pl2Lst (N / F L V)
   (setq
      F (BitSet (Item 70 (entget N)) 4)   ;splined flag
      N (if F (entnext N) N)              ;init first entity
      V nil                               ;init vertex list
   )
   (while
      (and          ;loop condition...
         (setq
            N (entnext N)
            L (entget N)
            F (Item 70 L)    ;exit if nil for SEQEND
         )
         (<= F 8)            ;exit if flag value not allowed
      )
      (setq V (cons (Item 10 L) V))    ;inc ECS vertex list
   )
   (reverse V)
)

;=== As-drawn 3D-polyline vertex list function
;
;  argument: E - entity name of 3D-polyline
;  returns:  list of 3D-vertices (as drawn) in WCS coords
;
(defun Pl3Lst (E / F G L V)
   (setq
      F (Item 70 (entget E))
      E (if (BitSet F 4) (entnext E) E)
      G (if (BitSet F 4) 40 32)
      V nil
   )
   (while (= (Item 70 (setq E (entnext E) L (entget E))) G)
      (setq V (cons (Item 10 L) V))
   )
   (reverse V)
)

;=== As-drawn 3D polygon mesh vertex list function
;
;  argument: E - entity name of polygon mesh
;  returns:  list - M lists of N 3D-vertices (as drawn)
;
(defun MeshLst2 (E / L N S V W)
   (setq
      L (entget E)
      S (BitSet (Item 70 L) 4)  ;smoothed flag
      N (Item (if S 74 72) L)   ;vertices in N-direction
      E (if S (entnext E) E)    ;skip if smoothed
      V nil
   )
   (repeat (Item (if S 73 71) L)   ;vertices in M-direction
      (setq W nil)
      (repeat N (setq W (cons (Item 10 (entget (setq E (entnext E)))) W)))
      (setq V (cons (reverse W) V))
   )
   (reverse V)
)

;=== As-drawn line/polyline vertex list function
;
;  arguments:  EN - entity name of line or polyline: LW/2D/3D/mesh
;              EZ - returned coordinate system transform code:
;                      nil - no transform
;                      0   - to WCS if LW/2D
;                      1   - to current UCS
;                      TN  - to ECS defined by entity name TN
;                      TZ  - to ECS defined by 3D extrusion vector TZ
;              AC - bit-coded allowed-entity code:
;                      1 - line
;                      2 - LW/2D polyline
;                      4 - 3D polyline
;                      8 - polygon mesh
;
;  returns:  (list
;               list of command name & closed bits code
;               list of 3D-vertices (as drawn) in ECS/ECS/WCS/WCS coords
;            )
;            nil if entity not line or polyline type
;
(defun Pl0Lst (EN EZ AC / EC EF EL ET TF TT VL)
   (setq
      EL (entget EN)    ;entity assoc list
      ET (Item 0 EL)    ;entity type
      EF (Item 70 EL)   ;entity flag
      TF 0              ;transform-from code
      TT (or            ;transform-to flag
            (= EZ 1)
            (= (type EZ) 'ENAME)
            (and (listp EZ) (= (length EZ) 3))
         )
      VL nil
   )
   (if EF
      (setq
         EF (rem EF 128)    ;del linegen & higher bits
         EC (logand EF 1)   ;flag: 1 if closed, 0 if open
      )
   )
   (cond
      ((and (= ET "LWPOLYLINE") (BitSet AC 2))
         (setq
            TF EN
            TT (or TT (zerop EZ))
            EC (list "PLINE" EC)    ;list cmd-name closed-flag
            VL (Pl1Lst EN)          ;ECS coords list
         )
      )
      ((= ET "POLYLINE")
         (cond
            ((and (BitSet EF 16) (BitSet AC 8))
               (setq
                  EC (list "3DMESH" (logand EF 33))   ;flag: 0/1/32/33
                  VL (MeshLst2 EN)                    ;list of WCS coords lists
               )
            )
            ((and (BitSet EF 8) (BitSet AC 4))
               (setq
                  EC (list "3DPOLY" EC)
                  VL (Pl3Lst EN)          ;WCS coords list
               )
            )
            ((and (< EF 8) (BitSet AC 2))
               (setq
                  TF EN
                  TT (or TT (zerop EZ))
                  EC (list "PLINE" EC)
                  VL (Pl2Lst EN)          ;ECS coords list
               )
            )
            (T nil)
         )
      )
      ((and (= ET "LINE") (BitSet AC 1))
         (setq
            EC (list ET 0)
            VL (list (Item 10 EL) (Item 11 EL))
         )
      )
      (T nil)
   )
   (if VL
      (if TT
         (cons EC (mapcar '(lambda (P) (trans P TF EZ)) VL))
         (cons EC VL)
      )
   )
)

;=== Returns 2D point from input 2D/3D point
;
(defun Pt2D (P)
   (list (car P) (cadr P))
)

;=== Returns vector from P to Q, divided by D
;
(defun VecDiv (P Q D / X Y)
   (mapcar '(lambda (X Y) (/ (- X Y) D)) Q P)
)

;=== Returns bubblesorted list SL, ascending/descending if AD T/nil
;
;  SL = (a b c d ...)
;  LP = nil:  a,b,c,... are atoms (all numbers or all strings)
;  LP = int:  a,b,c,... are lists, with sorting done on the LP'th element
;
(defun BublSort (SL AD LP / J K L N X Y)
   (setq
      AD (if AD < >)       ;ascending/descending operator
      LP (if LP (1+ LP))   ;inc sub-list position
      N  (length SL)
      J  -1
      SL (mapcar '(lambda (X) (cons (setq J (1+ J)) X)) SL)   ;index list
      J  0
   )
   (repeat (1- N)
      (setq
         X (if LP (nth LP (nth J SL)) (cdr (nth J SL)))
         L J
         K (1+ J)
      )
      (repeat (- N K)
         (setq Y (if LP (nth LP (nth K SL)) (cdr (nth K SL))))
         (if (AD Y X)
            (setq
               X Y
               L K
            )
         )
         (setq K (1+ K))
      )
      (if (/= L J)
         (setq
            X  (nth J SL)
            Y  (nth L SL)
            SL (subst (cons J (cdr Y)) X (subst (cons L (cdr X)) Y SL))
         )
      )
      (setq J (1+ J))
   )
   (mapcar 'cdr SL)    ;de-index sorted list
)

;=== Builds strings, sets initget, returns prompt for XGet* funcs
;
(defun XGetKey (L P D F / K Q S)
   (setq
      K ""
      Q ""
      S 0
   )
   (foreach X L
      (if (= (type X) 'STR)
         (progn
            (if (/= X D) (setq Q (strcat Q X "/")))
            (setq K (strcat K " " X))
         )
         (if (numberp X) (setq S (+ S X)))
      )
   )
   (initget S (substr K 2))
   (strcat
      "\n** " P (if (= P "") "" (if (= Q "") " " " - ")) Q "<"
      (cond
         ((= (setq S (type D)) 'REAL) ((if F rtos angtos) D))
         ((= S 'INT) ((if F itoa angtos) D))
         ((= S 'LIST)
            (setq K "")
            (substr (foreach X D (setq K (strcat K "," (rtos X)))) 2)
         )
         (T D)
      )
      ">: "
   )
)

;=== Returns dist (from pt P0 if P0), list OL key, or dflt D if null input
;
(defun XGetDist (P0 OL P D)
   (setq P (XGetKey OL P D T))
   (if P0 (cond ((getdist P0 P)) (D)) (cond ((getdist P)) (D)))
)

;=== Returns entity assoc list L with group N changed to D, or adds group
;
(defun SubEnt (N D L)
   (setq D (cons N D))
   (if (setq N (assoc N L)) (subst D N L) (cons D L))
)

;============= PROGRAM-SPECIFIC FUNCTIONS ===================================

;=== Filter Zero Lengths function
;
;  argument:  VL - list of vertices, 2D or 3D
;
;  returns:  vertex list with coincident points made single
;
(defun XSect_FZL (VL / P0 P1 TL)
   (setq
      P0 (car VL)
      TL (list P0)
   )
   (foreach P1 (cdr VL)                       ;filter out zero-length segs...
      (if (not (zerop (distance P0 P1)))
         (setq TL (cons P1 TL))
      )
      (setq P0 P1)
   )
   (reverse TL)
)

;=== Expand Data List function
;
;  argument:  DL - object data list as returned by (Pl0Lst)
;
;  returns:  (list
;               (list [vertex] [vertex]...)    ;sublist
;            )
;            nil if DL not as required
;
;  note:  function treats 3dmesh DL as crossed arrays of linear polylines,
;         returning multiple sublists
;
(defun XSect_EDL (DL / DC DT DV MC ML MV XV)
   (setq
      DT (caar DL)     ;command name
      DC (cadar DL)    ;closed code
      DV (cdr DL)      ;vertex list
   )
   (cond
      ((member DT (list "PLINE" "3DPOLY" "3DFACE" "SOLID"))
         (if (and (= DC 1) (> (length DV) 2))                ;if closed
            (list (append DV (list (car DV))))               ;add pt to end
            (list DV)                                        ;else same
         )
      )
      ((= DT "3DMESH")        ;convert mesh array data to polyline data...
         (setq
            MC (and (BitSet DC 1) (> (length (car DV)) 2))   ;closed flag
            ML nil                                           ;init list
         )
         (foreach MV DV
            (if MC (setq MV (append MV (list (car MV)))))    ;add pt to end
            (setq ML (cons MV ML))                           ;inc list
         )
         (setq
            MC (and (BitSet DC 32) (> (length DV) 2))        ;closed flag
            XV (MkLst nil (length DV))                       ;init array list
         )
         (foreach MV DV (setq XV (mapcar 'cons MV XV)))      ;flip array
         (foreach MV XV
            (if MC (setq MV (append MV (list (car MV)))))    ;add pt to end
            (setq ML (cons MV ML))                           ;inc list
         )
         ML
      )
      ((= DT "LINE") (list DV))
      (T nil)
   )
)

;=== Get Data List function
;
;  acquires and pre-processes cross-section and contour data
;
(defun XSect_GDL (/ EL EN ET JS PR SS)
   (setq PR "\n** Pick line or polyline cutting section...")
   (while
      (not
         (and
            (setq EN (car (entsel PR)))
            (setq XSL (Pl0Lst EN 1 7))     ;line/nonmesh-poly in UCS coords
         )
      )
   )
   (setq
      PR  (car (XSect_EDL XSL))                ;process vertex list
      SS  (mapcar '(lambda (P) (Pt2D P)) PR)   ;2D section pts
      XSL (XSect_FZL SS)                       ;filter null segments
   )
   (prompt "\n** Select objects to be sectioned...")
   (setq
      SS  (ssget)                              ;get selset
      CDL nil                                  ;init contours data list
      JS  0                                    ;init counter
   )
   (while (and SS (setq EN (ssname SS JS)))    ;process selset...
      (setq
         EL (entget EN)
         ET (Item 0 EL)
         PR (cond
               ((= ET "3DFACE")
                  (cons
                     (list ET 1)
                     (mapcar '(lambda (P) (trans P 0 1))
                        (list
                           (Item 10 EL) (Item 11 EL)
                           (Item 12 EL) (Item 13 EL)
                        )
                     )
                  )
               )
               ((= ET "SOLID")
                  (cons
                     (list ET 1)
                     (mapcar '(lambda (P) (trans P EN 1))
                        (list
                           (Item 10 EL) (Item 11 EL)
                           (Item 13 EL) (Item 12 EL)
                        )
                     )
                  )
               )
               (T (Pl0Lst EN 1 15))    ;line/polyline data list
            )
         JS (1+ JS)                    ;inc counter
      )
      (if PR (setq CDL (append CDL (XSect_EDL PR))))    ;inc contour data
   )
)

;=== Make Section Lists function
;
(defun XSect_MSL (/ C0 C1 CL DC DX DZ PL TD X0 X1)
   (setq
      IPL nil          ;list of all intersect pts
      TD  0.0          ;total distance along cutting segments
      SDL (list TD)    ;total distance list
      X0  (car XSL)    ;start pt of cutting segment
   )
   (foreach X1 (cdr XSL)           ;for all next pts of cut-seg...
      (setq DX (distance X0 X1))   ;2D length of cut-seg
      (foreach CL CDL              ;process all contour data lists...
         (setq C0 (car CL))        ;start pt of contour data list
         (foreach C1 (cdr CL)                     ;for all next pts...
            (if (setq IP (inters X0 X1 C0 C1))    ;if onseg 2D intersect...
               (setq
                  DC  (distance X0 IP)                  ;2D dist from seg start
                  DZ  (- (caddr C1) (caddr C0))         ;Z offset from C0
                  IP  (+ (/ (* DZ DC) DX) (caddr C0))   ;Z of intersect pt
                  IPL (cons (list (+ TD DC) IP) IPL)    ;inc intersect pt list
               )
            )
            (setq C0 C1)     ;update start pt of contour
         )
      )
      (setq
         X0  X1                       ;update start pt of cut-seg
         TD  (+ TD DX)                ;inc total dist of cut-segs
         SDL (append SDL (list TD))   ;inc list
      )
   )
   (if IPL
      (setq IPL (XSect_FZL (BublSort IPL T 0)))    ;sort ascending & filter
   )
)

;=== Edit-Box Action function
;
;  parameters:  TK - tile key
;               TV - tile value
;               TR - tile reason
;
(defun XSect_EBA (TK TV TR / AF ES JT)
   (if (or (= TR 1) (= TR 2))
      (progn
         (cond
            ((= TK "xsect_top")
               (setq
                  JT 0
                  AF (< (atof TV) Y1)
                  ES (strcat "Value cannot be less than " (rtos Y1))
               )
            )
            ((= TK "xsect_bot")
               (setq
                  JT 1
                  AF (> (atof TV) Y0)
                  ES (strcat "Value cannot be greater than " (rtos Y0))
               )
            )
            (T (setq
                  AF (list "xsect_scl" "xsect_txt" "xsect_tik" 0)
                  JT (length (member TK AF))
                  AF (<= (atof TV) 0.0)
                  ES (strcat "Value must be greater than 0")
               )
            )
         )
         (if AF
            (progn
               (set_tile TK (rtos (Item JT DV)))
               (alert ES)
               (mode_tile TK 3)
            )
            (setq DV (SubEnt JT (atof TV) DV))
         )
      )
   )
)

;==== Draw Cross Section function
;
(defun XSect_DCS (/ DB DV JL PT SF SP TF TG TH TS Y0 Y1 YB YT)
   (setq
      DV ~XSECTDV
      JL (list 0 1 2 3 4)
      PT (and                 ;check validity...
            (listp DV)
            (equal (mapcar 'car DV) JL)
            (apply 'and (mapcar 'numberp (mapcar 'cdr DV)))
         )
   )
   (if (not PT)               ;initialize
      (setq
         TS (getvar "TEXTSIZE")
         DV (mapcar 'cons JL (list 0.0 0.0 1.0 TS 1.0))
      )
   )
   (setq
      PT (mapcar 'cadr IPL)               ;list of section elevations
      Y0 (apply 'min PT)                  ;min elevation
      Y1 (apply 'max PT)                  ;max elevation
      DV (SubEnt 0 Y1 (SubEnt 1 Y0 DV))
      DB (load_dialog "xsect.dcl")        ;load DCL file
   )
   (if (new_dialog "xsect" DB)            ;initialize dialog
      (progn
         (setq TS (list "top" "bot" "tik" "txt" "scl"))
         (foreach PT JL
            (setq SF (strcat "xsect_" (nth PT TS)))
            (action_tile SF "(XSect_EBA $key $value $reason)")
            (set_tile SF (rtos (Item PT DV)))
         )
         (set_tile "xsect_max" (strcat "Elevation maximum  " (rtos Y1)))
         (set_tile "xsect_min" (strcat "Elevation minimum  " (rtos Y0)))
         (mode_tile "xsect_top" 3)
         (start_dialog)              ;display dialog
         (unload_dialog DB)          ;unload dialog
         (setq
            YT (Item 0 DV)           ;vertical axis top
            YB (Item 1 DV)           ;vertical axis bottom
            TS (Item 2 DV)           ;vertical tick interval
            TH (Item 3 DV)           ;axis label text height
            SF (Item 4 DV)           ;vertical exaggeration
         )
      )
      (progn                         ;command line...
         (prompt
            (strcat
               "\n** Elevation of section ranges from "
               (rtos Y1) " to " (rtos Y0)
            )
         )
         (setq PR "Vertical axis top elevation")
         (while (< (setq YT (XGetDist nil nil PR Y1)) Y1))
         (setq PR "Vertical axis bottom elevation")
         (while (> (setq YB (XGetDist nil nil PR Y0)) Y0))
         (setq PR "Vertical axis tick interval")
         (while (<= (setq TS (XGetDist nil nil PR (Item 2 DV))) 0.0))
         (setq PR "Axis label text height")
         (while (<= (setq TH (XGetDist nil nil PR (Item 3 DV))) 0.0))
         (setq PR "Section vertical exaggeration factor")
         (while (<= (setq SF (XGetDist nil nil PR (Item 4 DV))) 0.0))
         (setq DV (mapcar 'cons JL (list YT YB TS TH SF)))
      )
   )
   (setq
      YT (* YT SF)    ;scale to draw...
      YB (* YB SF)
      TS (* TS SF)
      TG (- TH)                                               ;text gap as neg
      PT (Item 40 (tblsearch "STYLE" (getvar "TEXTSTYLE")))   ;height
   )
   (if (zerop PT)
      (setq
         TH (- TH)
         TF 1.0
      )
      (setq TF (/ TH PT))
   )
   (initget 1)
   (setq
      SP (Pt2D (getpoint "\n** Pick point to place drawn section: "))
      UNDOIT T
   )
   (setvar "BLIPMODE" 0)
   (setvar "OSMODE" 0)
   (setvar "ORTHOMODE" 0)
   (setvar "HIGHLIGHT" 0)
   (command
      ".UCS" "O" SP
      ".LINE" (list 0 0) (list (last SDL) 0) ""   ;horizontal axis
   )
   (foreach PT SDL                                ;vertical lines...
      (setq
         Y0 (list PT 0)                           ;common line pt
         Y1 (list PT (+ TG TG))                   ;text insertion pt
      )
      (command
         ".LINE" Y0 (list PT (- YT YB)) ""        ;section break line
         ".LINE" Y0 (list PT TG) ""               ;tick line
         ".TEXT" "J" "TC" Y1                      ;tick text...
      )
      (if (minusp TH) (command (- TH)))
      (command 0.0 (rtos PT))
      (if (/= TF 1.0)
         (command ".SCALE" (entlast) "" Y1 TF)    ;scale fixed-height text
      )
   )
   (setq PT 0)                                    ;init tick y-coord
   (while (<= PT (- YT YB))
      (setq Y1 (list (+ TG TG) PT))               ;text insertion pt
      (command
         ".LINE" (list 0 PT) (list TG PT) ""      ;tick line
         ".TEXT" "J" "MR" Y1                      ;tick text...
      )
      (if (minusp TH) (command (- TH)))
      (command 0.0 (rtos (/ (+ PT YB) SF)))
      (if (/= TF 1.0)
         (command ".SCALE" (entlast) "" Y1 TF)    ;scale fixed-height text
      )
      (setq PT (+ PT TS))                         ;inc tick y-coord
   )
   (command
      ".UCS" "P"                                     ;restore prev UCS
      ".UCS" "O" (list (car SP) (- (cadr SP) YB))    ;section origin
      ".PLINE"                                       ;draw section...
   )
   (foreach PT IPL (command (list (car PT) (* (cadr PT) SF))))
   (command
      ""                ;end pline
      ".UCS" "P"        ;restore prev UCS
   )
   (setq ~XSECTDV DV)   ;save default values
)

;=== Command function
;
(defun C:XSECT (/ CDL IPL SDL XSL)
   (StartNFT
      (list "HIGHLIGHT" "ORTHOMODE" "OSMODE" "FLATLAND" "UCSFOLLOW" "DIMZIN")
      (list nil nil nil 0 0 0)
   )
   (XSect_GDL)          ;get data lists
   (if CDL              ;if contour data...
      (progn
         (XSect_MSL)             ;make section list
         (if IPL (XSect_DCS))    ;if section data, draw cross-section
      )
   )
   (EndNFT nil)
)

;============= RUN ON LOAD ==================================================

(princ "\n** Type XSECT at the command prompt...\n")
(C:XSECT)

;============= END OF FILE ==================================================
