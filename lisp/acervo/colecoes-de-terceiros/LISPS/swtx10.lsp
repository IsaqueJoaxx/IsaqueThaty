; **********************************************************
; SWTX10.LSP
; Copyright (c) Barry R. Bowen 1993-1994
; ----------------------------------------------------------

(defun SWTX10 (/ RESET CKOUT DCL_ID CLIST FILE CKLED V3 V4
    LD_CLR CLYRLT XL YL DO_NEXT GETLIST LINE CK DO_CAT
    CKTSTR NTE TXTS1 TXTS2 TXTS2A TXTS3 STRCK DO_LED NUM
    XXX EN DO_NTE TS HTS MLL ELIST PT ANG PT1 PT2 LP FNAME LS
    RESET D90 D270 RLAY)

  (defun RESET () (set_tile "error" "")
   (set_tile "error" "Copyright (c) Barry R. Bowen 1993-94"))

  (defun RLAY () (command "layer" "s" OLAY ""))
  (defun D90 () (* pi 0.5))
  (defun D270 () (* pi 1.5))
  (defun *error* (E) (V4) (princ "error: ") (princ E) (tepri))

; -------------------------- V3 ---------------------------
  (defun V3 ()
    (setq BM (getvar "blipmode"))
    (setvar "blipmode" 0) (setvar "cmdecho" 0)
    (command "undo" "group")
  )

; -------------------------- V4 ---------------------------
  (defun V4 ()
    (setvar "blipmode" BM) (command "undo" "end")
    (prompt "\nProgram Completed. . . . .")
  )

; ----------------------- CKLED -----------------------------
  (defun CKLED ()
    (if (not LX) (setq LX "1"))
    (if (= LX "0")
      (progn (setq LX1 1 CKLD nil))
      (progn (setq LX1 0 CKLD 1))
    )
    (foreach N '("slled" "mulled" "arrow" "dot" "txtclr" "lclr")
         (mode_tile N LX1))
  )

; ----------------------- LD_CLR -----------------------------
  (defun LD_CLR ()
    (if (not LDCLR) (setq LDCLR 5))
    (cond
     ((= LDCLR 1) (set_tile "txtclr" "RED"))
     ((= LDCLR 2) (set_tile "txtclr" "YELLOW"))
     ((= LDCLR 3) (set_tile "txtclr" "GREEN"))
     ((= LDCLR 4) (set_tile "txtclr" "CYAN"))
     ((= LDCLR 5) (set_tile "txtclr" "BLUE"))
     ((= LDCLR 6) (set_tile "txtclr" "MAGENTA"))
    ((= LDCLR 7) (set_tile "txtclr" "WHITE"))
      (T (set_tile "txtclr" (itoa LDCLR)))
    )
    (if (= LDCLR 256) (progn
      (setq CLYRLT (tblsearch "layer" (getvar "CLAYER"))
             LDCLR (cdr (assoc 62 CLYRLT)))
      (set_tile "txtclr" "BYLAYER")
    ))
    (start_image "lclr")
    (setq XL (dimx_tile "lclr") YL (dimy_tile "lclr"))
    (fill_image 0 0 XL YL LDCLR)
   (end_image)
  )

; --------------------------- CKOUT -----------------------
  (defun ckout ()
    (cond ((and (= (get_tile "nte") "")
        (set_tile "error"
          "You Must Select a Note From The Notes Tile!")
        (mode_tile "nte" 2)))
      (T (done_dialog 7) (DO_CAT))
 )  )

;---------------------------- LS --------------------
(defun LS (NLAY CLR LT / LAY FRZ)
  (if (= (tblsearch "layer" NLAY) nil)
    (command "layer" "m" NLAY "c" CLR "" "lt" LT "" "")
    (progn
      (setq LAY (tblsearch "layer" NLAY))
      (setq FRZ (cdr (assoc 70 LAY)))
      (if (= FRZ 65)
         (progn (command "layer" "t" NLAY "")
          (command "layer" "s" NLAY ""));progn
          (command "layer" "s" NLAY "")
) ) ) )

; --------------------------- DO_CAT ----------------------
(defun DO_CAT (/ FNMAE CK LINE)
  (setq FNAME (findfile "SWTX10.TXT"))
  (setq FILE (open FNAME "r") LINE (read-line FILE)
          CK (substr LINE 1))
  (while (/= CK (strcat "/" (nth (atoi XX) CLIST)))
     (setq LINE (read-LINE FILE) CK (substr LINE 1))
) )

; -------------------------- DO_NLIST ----------------------
(defun DO_NLIST (/ LINE)
  (setq LINE (read-line FILE))
  (set_tile "title" (strcat "General Notes For " TITLE))
  (start_list "nte")
  (while (/= (substr LINE 1 1) "/")
    (add_list LINE) (setq LINE (read-line FILE)))
  (end_list)
  (close FILE)
)

; --------------------------- DO_LED ----------------------
(defun DO_LED (/ NUM EN)
  (setq NUM (atoi XXX))
  (repeat NUM (setq NTE (read-line FILE)))
  (setq NTE (read-line FILE))
  (close FILE)
  (CKTSTR)
     (setq EN (tblsearch "style" "NOTES"))
     (if (= EN nil)
       (prompt "\nNote: A Text Style Named <NOTES> Must Be
                         Defined With A Specific Text Height!")
       (DO_NTE)
     )
  (princ)
)

; --------------------------- DO_NTE ----------------------
(defun DO_NTE (/ ORGTS HTS TS LP PT1 PT PT2)
   (LS "NOTES" "7" "")
   (setq ORGTS (getvar "textstyle"))
   (setvar "textstyle" "notes")
   (command "ortho" "off")
   (setq TS (cdr (assoc 40 EN)))
   (setq HTS (/ TS 2.0))
   (if (/= CKLD nil) (progn
     (SML)
     (setq LP (getvar "lastpoint")
          PT1 (polar LP ANG (* ts 0.66666))
           PT (list (car PT1) (- (cadr LP) HTS))
          PT2 (polar PT1 0 HTS))
     )
     (progn (setq PT (getpoint "\nNote Location: ") ANG 0))
   )
   (cond ((= ANG (+ pi (* pi 0.5)))
           (command "text" "r" PT2 "90" TXTS1))
         ((= ANG (* pi 0.5)) (command "text" PT2 "90" TXTS1))
         ((= ANG pi)
           (command "text" "r" PT "0" TXTS1)
           (setq ELIST (entget (entlast)) PT (cdr (assoc 10 ELIST)))
            (cond
              ((= STRCK 1)
               (setq PT (polar PT (D270) (* TS 1.6666)))
               (command "text" PT "0" TXTS2))
              ((= STRCK 3)
               (setq PT (polar PT (D270) (* TS 1.6666)))
               (command "text" PT "0" TXTS2A)
               (setq PT (polar PT (D270) (* TS 1.6666)))
               (command "text" PT "0" TXTS3))
            )
         ); pi condition
         (t (command "text" PT "0" TXTS1)
            (cond
              ((= STRCK 1)
               (setq PT (polar PT (D270) (* TS 1.6666)))
               (command "text" PT "0" TXTS2))
              ((= STRCK 3)
               (setq PT (polar PT (D270) (* TS 1.6666)))
               (command "text" PT "0" TXTS2A)
               (setq PT (polar PT (D270) (* TS 1.6666)))
               (command "text" PT "0" TXTS3))
            )
         );end T cond
    );cond
   (setvar "textstyle" ORGTS)
   (RLAY)
)

; ---------------------------- SML -------------------------
(defun SML (/ LGTH PT1 PT2 PT2A PT2B PT3 WDTH)
  (if (= (getvar "dimscale") 48)
    (setq LGTH (* 0.0625 48.0))
    (setq LGTH (* (getvar "dimasz") (getvar "dimscale")))
  )
  (setq WDTH (/ LGTH 2.999)
        PT1 (getpoint "\nLeader Begin: ")
        PT3 (getpoint PT1 "\nNext Point: ")
        ANG (angle PT1 PT3)
        PT2 (polar PT1 ANG LGTH)
       PT2A (polar PT1 0 (/ WDTH 2.0))
       PT2B (polar PT1 pi (/ WDTH 2.0)))
  (if (= LTER 0)
    (command "pline" PT1 "w" "0" WDTH PT2 "w" "0" "0" PT3)
    (progn
     (command "donut" 0 (* WDTH 2.0) PT1 "")
     (setq EL (entlast))
     (command "pline" PT1 "w" "0" "0" PT3)
    )
  )
  (setq ANG (angle PT1 PT3))
  (if (= LRCK 0)
    (progn
      (setq PT1 PT3)
      (while PT2
        (command (setq PT2 (getpoint PT1 "\nNext Point: ")))
        (if (/= PT2 NIL) (progn
          (setq ANG (angle PT1 PT2))
          (setq PT1 PT2)))
    ) )
    (progn
      (setq LGTH (* 1.5 LGTH))
      (cond
       ((< ANG 1.5) (setq ANG 0 PT4 (polar PT3 0 LGTH)))
       ((and (< ANG 3.1) (> ANG 1.5)) (setq ANG pi PT4 (polar PT3 pi LGTH)))
       ((and (< ANG 4.7) (> ANG 3.1)) (setq ANG pi PT4 (polar PT3 pi LGTH)))
       ((> ANG 4.7) (setq ANG 0 PT4 (polar PT3 0 LGTH)))
      )
      (command PT4 "")
    )
  )
  (if (/= LDCLR 256) (command "change" (entlast) "" "p" "c" LDCLR ""))
  (if (= LTER 1) (command "change" EL "" "p" "c" LDCLR ""))
)

; Check String For ^ character
(defun CKTSTR (/ IN CK CNT)
  (setq CNT (strlen NTE) STRCK nil)
  (setq IN 1 CK nil)
  (while (/= CK "^")
    (setq CK (substr NTE IN 1))
    (setq IN (1+ IN))
    (if (= CK "^") (setq STRCK 1))
    (if (= CNT IN) (setq CK "^"))
  );while
  (if (= STRCK 1) (progn
    (setq TXTS1 (substr NTE 1 (- IN 2)))
    (setq TXTS2 (substr NTE IN))
    (setq IN 1 CK nil CNT (strlen TXTS2))
    (while (/= CK "^")
      (setq CK (substr TXTS2 IN 1))
      (setq IN (1+ IN))
      (if (= CK "^") (setq STRCK 3))
      (if (= CNT IN) (setq CK "^"))
    );while
    (if (= STRCK 3) (progn
      (setq TXTS2A (substr TXTS2 1 (- IN 2)))
      (setq TXTS3 (substr TXTS2 IN))
    ));if/progn
   );first progn
   (setq TXTS1 NTE)
  );if
)

; ------------------- Main Program Code ---------------------
  (V3)

  (setq DCL_ID (load_dialog "SWTX10.dcl"))
  (if (not (new_dialog "swtx10" DCL_ID)) (exit))
  (if (not TITLE)
     (setq TITLE "General Notes Insertion Dialog Box")
     (set_tile "title" (strcat "General Notes For " TITLE))
  )
  (setq FNAME (findfile "SWTX10.TXT")
         FILE (open FNAME "r")
        CLIST (read (read-line FILE)))
  (close FILE)
  (start_list "clist")
  (mapcar 'add_list CLIST)
  (end_list)

  (if (/= XX nil) (progn (DO_CAT) (DO_NLIST)))
  (if (not LRCK) (setq LRCK 1)) ;0=multiple 1=single
  (if (= LRCK 0) (set_tile "mulled" "2") (set_tile "slled" "2"))
  (if (not LTER) (setq LTER 0)) ;0=arrow 1=dot
  (if (= LTER 0) (set_tile "arrow" "2") (set_tile "dot" "2"))

  (LD_CLR);Check current color for leader
  (CKLED) ;Check the status of the leader
  (if (= CKLD nil) (set_tile "aled" "0") (set_tile "aled" "1"))
  (set_tile "error" "Copyright (c) Barry R. Bowen 1993-94")
  (action_tile "clist"  "(reset) (setq XX $value)
         (setq TITLE (nth (atoi XX) CLIST)) (DO_CAT) (DO_NLIST)")
  (action_tile "nte"    "(reset) (setq XXX $value)")
  (action_tile "aled"   "(setq LX $value) (CKLED)");leader
  (action_tile "slled"  "(setq LRCK 1)");single leader
  (action_tile "mulled" "(setq LRCK 0)");multiple leaders
  (action_tile "arrow"  "(setq LTER 0)")
  (action_tile "dot"    "(setq LTER 1)")
  (action_tile "lclr"   "(setq LDCLR (acad_colordlg LDCLR)) (LD_CLR)")
  (action_tile "accept" "(CKOUT)")
  (action_tile "cancel" "(done_dialog)")

  (setq DO_NEXT (start_dialog))

  (unload_dialog dcl_id)
  (if (= 7 DO_NEXT) (DO_LED))
  (V4)
)

(prompt "\nNOTES for AutoCAD Program Loaded.....")
(prompt "\nCopyright Barry R. Bowen 1993-94")
(princ)
