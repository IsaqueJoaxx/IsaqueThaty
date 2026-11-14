;; KJOIST.LSP
;; Program For K-Series Bar Joists Dialog Box Menu
;; Copyright (c) Barry R. Bowen 1989-94
;; All rights reserved
;;

(prompt "\nLoading KJOIST...")
(defun JOIST (BNAME DEPTH / DIST EN EN1 EN2 JBPT JLGTH
              LGTH PT PT1 PT2 PT3 PT4 PT5 PT6 PT7 PT8 PT9
              PT10 PT1A PT1B PT5A PT6A PT6B PT9A PT10A PT10B
              DIST SL1)
  (ZC DEPTH)
  (setq SL1 nil)
  (cond
   ((= KVW "E")
    (if (= KBR "S") (setq LGTH 2.5) (setq LGTH 4))
    (setq JBPT (getpoint "\nEdge of Bearing Point: ")
         JLGTH (getdist JBPT "\nPartial Length of Joist: "))
    (if (< JLGTH (* 1.75 DEPTH)) (setq JLGTH (* 2 DEPTH)))
    (ZC JLGTH)
    (if (= KXT "N")
      (progn (setq PT4 (polar JBPT pi LGTH)
                  DIST (distance JBPT PT4)))
      (progn
        (setq DIST (getdist JBPT "\nEnd Extension Length: ")
               PT4 (polar JBPT pi DIST)))
    )
; ---------------------------- Required Points For Top Cord
    (setq JLGTH (+ DIST JLGTH)
            PT3 (polar JBPT 0 0.5)
           PT3A (polar PT3 (D90) 0.25)
            PT2 (polar PT3 (D90) 1.25)
           PT4A (polar PT4 (D90) 0.25)
           PT4B (polar PT4 (D90) 1.25)
            PT5 (polar PT4 (D90) 2.5)
           PT5A (polar PT5 (D270) 0.25)
            PT1 (polar PT4B 0 JLGTH)
            PT6 (polar PT5 0 JLGTH)
           PT6A (polar PT6 (D270) 0.25)
; ---------------------------- Required Points For Bottom Cord
           PT7A (polar PT5 (D270) (- DEPTH 1.25))
            PT7 (polar PT7A 0 JLGTH)
           TPT1 (polar PT2 pi 2.0)
           TPT2 (polar TPT1 5.764688 (* 1.5 DEPTH))
            PT8 (inters TPT1 TPT2 PT7A PT7 nil)
           PT8A (inters TPT1 TPT2 PT3 PT2 nil)
            PT9 (polar PT8 pi 2.0)
           PT10 (polar PT9 (D270) 1.25)
          PT10A (polar PT10 (D90) 0.25)
           PT11 (polar PT7 (D270) 1.25)
          PT11A (polar PT11 (D90) 0.25)
; --------------------------- Required Points For Web
           PT12 (inters TPT1 PT8 PT3 PT2 nil)
           PT13 (polar PT8 0 0.7567)
           PT14 (polar PT12 (D90) 0.4317)
           PT15 (polar PT8 0 1.4375)
           PT16 (polar PT15 (D135) 12)
           PT16 (inters PT15 PT16 PT1 PT2 nil)
           PT17 (polar PT15 0 0.55)
           PT18 (polar PT16 0 0.55)
           PT19 (polar PT17 0 0.5250)
           PT20 (polar PT19 (D45) 12)
           PT20 (inters PT19 PT20 PT1 PT7 nil)
           PT21 (polar PT19 0 0.55)
           PT22 (polar PT21 (D45) 12)
           PT22 (inters PT21 PT22 PT1 PT7 nil))
; ------------------------------------------ Draw Top Cord
    (command "pline" PT1 "W" 0 0 PT2 PT3 PT4 PT5 PT6 ""
             "line" PT5A PT6A ""
             "line" PT4B PT2 ""
             "line" PT4A PT3A ""
; ------------------------------------------ Draw Bottom Cord
             "pline" PT7 PT9 PT10 PT11 ""
             "line" PT10A PT11A ""
; ------------------------------------------ Draw Web Lines
             "line" PT8 PT8A ""
             "line" PT13 PT14 ""
             "line" PT15 PT16 ""
             "line" PT17 PT18 ""
             "line" PT19 PT20 "")
    (CKEXT PT1 PT2 T)
    (command "line" PT21 PT22 "")
    (CKEXT PT1 PT2 T)
   ) ;end first cond
   (T  (JSEC))
  ) ;end cond
)

(defun C:KJOIST (/ reset show_j KJ814 KJ16 KJ18 KJ20
     KJ22 KJ24 KJ26 KJ28 KJ30 get_ksz ck_out getimg get_sze
     KLIST JX FILE dcl_id old_cmd KSZCK old_error what_next)

 (setq KJDR# "c:/cadalyst/94apr/")

(defun reset () (set_tile "error" ""))

(defun jsec1 ()
  (foreach N '("eenor" "eext" "bconc" "bstl") (mode_tile N 1)))

(defun jelv ()
  (foreach N '("eenor" "eext" "bconc" "bstl") (mode_tile N 0)))

(defun show_j ()
  (set_tile "ksize" "")
  (start_list "ksize")
  (mapcar 'add_list JLIST)
  (end_list)
)

 (defun KJ814 ()
   (setq JLIST (list "8K1" "10K1" "12K1" "12K3" "12K5"
                     "14K1" "14K3" "14K4" "14K6"))
 )

 (defun KJ16 ()
   (setq JLIST (list "16K2" "16K3" "16K4" "16K5" "16K6"
                     "16K7" "16K9"))
 )

(defun get_ksz ()
  (setq ksz (nth (atoi X) KLIST))
  (cond
    ((= ksz "8-14") (KJ814) (show_j))
    ((= ksz "  16") (KJ16) (show_j))
  )
)

(defun ck_out ()
  (if (/= KSZCK nil) (progn
  (cond
    ((= (get_tile "velv") "1") (setq KVW "E"))
    ((= (get_tile "vsec") "1") (setq KVW "S"))
  )
  (cond
    ((= (get_tile "bconc") "1") (setq KBR "C"))
    ((= (get_tile "bstl") "1") (setq KBR "S"))
  )
  (cond
    ((= (get_tile "eenor") "1") (setq KXT "N"))
    ((= (get_tile "eext") "1") (setq KXT "E"))
  )
  (done_dialog 4)
 );end first progn
 (progn
   (set_tile "error" "You Must Select A Size!")
   (mode_tile "ksize" 2)
 ))
)

(defun getimg ()
  (if (= JX 1) (setq KV "KJELV") (setq KV "KJSEC"))
  (set_tile "kjimg" "")
  (setq x (dimx_tile "kjimg"))
  (setq y (dimy_tile "kjimg"))
  (start_image "kjimg")
  (slide_image 0 0 x y (strcat KJDR# KV))
  (end_image)
)

(defun get_sze ()
  (setq JSZ (nth (atoi X) JLIST) JL (strlen JSZ))
  (if (= JL 3) (setq JSIZE (substr JSZ 1 1))
               (setq JSIZE (substr JSZ 1 2))
  )
  (setq JSIZE (distof JSIZE))
)

  (setvar "cmdecho" 0)
  (setq what_next 4 start_1 nil)
  (setq dcl_id (load_dialog (strcat KJDR# "kjoist.dcl")))
  (if (not (new_dialog "kjoist" dcl_id)) (exit))

  (setq KLIST (list "8-14" "  16"))

  (start_list "ksiz")
  (mapcar 'add_list KLIST)
  (end_list)

  (action_tile "velv"  "(jelv) (setq JX 1) (getimg)")
  (action_tile "vsec"  "(jsec1) (setq JX 2) (getimg)")
  (action_tile "ksiz"  "(reset)  (setq X $value) (get_ksz)")
  (action_tile "ksize" "(reset) (setq X $value KSZCK 1) (get_sze)")
  (action_tile "accept"   "(ck_out)")
  (action_tile "cancel"   "(done_dialog 0)")

  (set_tile "eenor" "2")
  (set_tile "bstl"  "2")
  (set_tile "velv"  "2")

  (setq x (dimx_tile "kjimg"))
  (setq y (dimy_tile "kjimg"))
  (start_image "kjimg")
  (slide_image 0 0 x y (strcat KJDR# "KJELV"))
  (end_image)

  (KJ814)
  (start_list "ksize")
  (mapcar 'add_list JLIST)
  (end_list)

  (setq what_next (start_dialog))
  (if (= what_next 4) (joist jsz jsize))
  (unload_dialog dcl_id)

  (princ)
);end main defun



(defun CKEXT (X1 X2 X3 / C D E)
 (setq ELIST (entget (entlast))
           C (E4 10 ELIST)
           D (E4 11 ELIST)
           E (inters C D X1 X2 X3))
  (if (/= E nil)
    (progn
      (command "erase" (entlast) ""
               "line" C E "")
  ) )
)

(defun JSEC (/ DIST PT1 PT2 PT3)
  (setq PT1 (getpoint "\nTop of Joist: ")
        PT2 (polar PT1 (D270) DEPTH))
  (if (/= (tblsearch "block" BNAME) nil)
    (command "insert" BNAME PT1 1 1 0)
    (progn
      (command "insert" (strcat KJDR# "joistsec") PT1 "" "" "")
      (setq SL1 (ssadd (entlast)))
      (command "insert" "" PT2 "1" "-1" "")
      (SSET)
      (setq PT3 (polar (polar PT1 0 0.125) (D270) 1.25)
           DIST (- DEPTH 2.5))
      (command "insert" (strcat KJDR# "WEB") PT3 "1" DIST "")
      (SSET)
      (BLOCKIT)
  ) )
)

(defun D45 () (* pi 0.25))
(defun D90 () (* pi 0.5))
(defun D135 () (* pi 0.75))
(defun D270 () (* pi 1.5))

(defun BLOCKIT ()
  (command "block" BNAME PT1 SL1 ""
           "insert" BNAME PT1 "1" "1" "0")
  (setq SL1 (ssget "L"))
)

(defun ZC (X) (if (> X (VS)) (command "zoom" "c" (VC) (* 1.5 X))))

(defun VC () (getvar "viewctr"))

(defun VS () (getvar "viewsize"))

(defun SSET () (setq SL1 (ssadd (entlast) SL1)))

(defun E4 (NO EVAR1) (cdr (assoc NO EVAR1)))

(prompt "K-Series Bar Joist Module Loaded....")
(prompt "\nCopyright Barry R. Bowen 1989-94")
(princ)

