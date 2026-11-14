;;;QDIM v1.0    01.20.93
;;;by Raymond Bradley, CIS 71165,2764
;;;c/o Fitschen & Associates
;;;1855 Gateway Blvd., Ste 370
;;;Concord, CA  94520
;;;510 686 2400

(defun C:qdim ( / pt1 pt2 pt3 pt4 ftxt ptm ptp pth ptl ang num pt_c snlist
     angp angl gd lalist la box aforc oaforc dlist oerr ed sd osnlist)

     (setup)
     (if (not dcl_q) (setq dcl_q (load_dialog "QDIM")))

     (if (setq ss (ssget "I"))
	(if (and (= (sslength ss) 1)
	    (= (dxf 0 (setq ed (entget (ssname ss 0)))) "DIMENSION"))
	    (setq P_QBSN T
		  P_QBPT (dxf 10 ed)
		  P_QBAN (angle P_QBPT (dxf 14 ed))
	    );setq
	);if
     );if

     (while (not (setq pt1 (getpoint "\nSelect first point: ")))
      (setq sd 4)
      (while (> sd 3)
       	(if (not (new_dialog "qdlist" dcl_q)) (exit))
	(prep_tiles)
        (setq sd (start_dialog))
	(if (= sd 5) (base_pick))
      );while
      (grtext -1 (strcat "Layer: " (nth P_LIND lalist) "  Style: " (nth P_DIND dlist)))
    );while

     (while (not (setq pt2 (getpoint pt1 "\nSelect second point: ")))
      (setq sd 4)
      (while (> sd 3)
       	(if (not (new_dialog "qdlist" dcl_q)) (exit))
	(prep_tiles)
        (setq sd (start_dialog))
	(if (= sd 5) (base_pick))
      );while
      (grtext -1 (strcat "Layer: " (nth P_LIND lalist) "  Style: " (nth P_DIND dlist)))
    );while

     (setq ptm (polar pt1 (angle pt1 pt2) (/ (distance pt1 pt2) 2))
	   pt_c pt2
          angp (angle pt1 pt2)
          angl (+ angp (* pi 0.5))
          opt pt1 opta pt1 optb pt1
          box 1
     );setq

     (menucmd "p0=")

     (grdraw pt1 pt2 -1 3)

     (if (> (car pt1) (car pt2))
          (setq ptp pt2 pt2 pt1 pt1 ptp)
     );if

     (if (> (cadr pt2) (cadr pt1))
          (setq  pth pt2 ptl pt1);setq
          (setq  pth pt1 ptl pt2);setq
     );if

     (setq pt3 (list (car pt1) (cadr pt2))
           pt4 (list (car pt2) (cadr pt1))
           gd (grread T 4 box)
	   oerr *error*
     );setq

     (defun *error* (st)
	(dmdraw pt1 opta ptb pt2)
	(setq *error* oerr)
	(grtext)
	(princ)
     );defun

     (prompt "\nWHITE button changes parameters")
     (prompt "\nBLUE button to snap")
     (prompt "\nLocate third point: ")

     (while (or (= (car gd) 5) (= (car gd) 2) (= (car gd) 6))
          (cond
               ((= (car gd) 6)
                    (cond
                         ((= (cadr gd) 0)
			      (setq sd 4)
			      (while (> sd 3)
                              	(if (not (new_dialog "qdlist" dcl_q)) (exit))
			      	(prep_tiles)
                              	(setq sd (start_dialog))
				(if (= sd 5) (base_pick))
			      );while
                              (grtext -1 (strcat "Layer: " (nth P_LIND lalist) "  Style: " (nth P_DIND dlist) "  Mode: " mode ftxt))
                              (setq gd (grread T 4 box)) 
                         );cond BUTTON 2

                         ((= (cadr gd) 1)
                              (menucmd "p0=POP0")
                              (menucmd "p0=*")
                              (setq gd (grread))

                              (if (= (car gd) 4)
                                   (progn
					(setq tx (nth (- (cadr gd) 500) snlist))
					(cond 
					    ((wcmatch tx "*NEA*,*PER*,*TAN*")
						(prompt (strcat tx " to "))
						(setq box 2)
					    )
					    ((wcmatch tx "*CEN*,*ENDP*,*INS*,*INT*,*MID*,*NOD*,*QUA*")
						(prompt (strcat tx " of "))
						(setq box 2)
					    )
					    (T (prompt tx) (setq box 1))
					);cond
                                   );progn
                              );if
                              (setq gd (grread T 4 box))
                         );cond BUTTON 3

                    );cond within button pick
               );cond menu button pick

               ((= (car gd) 2)
                    (cond
                         ((or (= (cadr gd) 13) (= (cadr gd) 32))
                              (snapto)
                         );cond

                         (T
                              (setq tx (strcat tx (chr (cadr gd))))
                              (prompt (chr (cadr gd)))
                         );other keypress

                    );cond within in keypress
               );cond keypress

               ((= (car gd) 5)     
                    (setq ptp (cadr gd)
                         ang (angle ptm ptp)
                    );setq
               );cond GET POINT
          );cond overall

          (orient)
          (qddraw)
          (setq gd (grread T 4 box))
     );while main

     (setq tx (strcase tx) *error* oerr)
     (if (member tx snlist)
          (progn
               (setq ptm (osnap ptp tx))
               (if ptm (setq ptp ptm))
          );progn
     );if


     (cond
          ((= mode "ALIGN")
               (setq optb (distance pt1 pt2))
          )

          ((= mode "HOR")
               (setq optb (distance pt1 (list (car pt2) (cadr pt1))))
          )

          ((= mode "VERT")
               (setq optb (distance pt1 (list (car pt1) (cadr pt2))))
          )
     );cond

     (if (<= optb 48)
          (setq optb (strcat (rtos optb 2 0) (chr 34)))
          (setq optb (rtos optb))
     );if

     (if (and (= (substr optb 1 2) "44") (= (ascii (substr optb 3 1)) 34))
          (setq optb (strcat optb " CLR."))
     );if

     (if (not (new_dialog "edit" dcl_q)) (exit))
     (set_tile "word" optb)
     (action_tile "plusminus" "(set_tile \"word\" (strcat \"%%P\" (get_tile \"word\")))")
     (action_tile "min" "(set_tile \"word\" (strcat (get_tile \"word\") \" MIN.\"))")
     (action_tile "clr" "(set_tile \"word\" (strcat (get_tile \"word\") \" CLR.\"))")
     (action_tile "eq" "(set_tile \"word\" \"EQ.\")")
     (action_tile "accept" "(setq optb (strcase (get_tile \"word\")))(done_dialog)")
     (action_tile "word" "(mode_tile \"accept\" 2)")
     (mode_tile "accept" 2)
     (start_dialog)

     (dmdraw pt1 pta ptb pt2)
     (setq  opt (getvar "CLAYER"))

     (command "LAYER" "S" (nth P_LIND lalist) "")
;     (if (not (tblsearch "STYLE" "ARCH"))
;          (command ".STYLE" "ARCH" "ARCHITXT" "" "" "" "" "" "")
;     );if

     (command "DIM")
     (if (/= (nth P_DIND dlist) "*UNNAMED")
	(command "RESTORE" (nth P_DIND dlist))
     );if
     (command "STYLE")
;    (command "ARCH")
     (command "DIMSCALE" C_SCAL mode pt1 pt2)
     (command ptp optb "E")
     (if P_BFOL
      (progn
	(setq ed (entget (entlast))
	      P_QBPT (dxf 10 ed)
	      P_QBAN (angle P_QBPT (dxf 14 ed))
	      P_QBSN T
	)
      );progn
     );if
     (command "LAYER" "S" opt "")
     (setvar "TEXTEVAL" te)
     (setvar "LASTPOINT" pt_c)
     (grtext)
     (princ)
);defun

(defun prep_tiles ()
   (start_list "llist")
   (mapcar 'add_list lalist)
   (end_list)
   (start_list "dlist")
    (mapcar 'add_list dlist)
    (end_list)
    (set_tile "dlist" (itoa P_DIND))
    (set_tile "llist" (itoa P_LIND))
    (cond
	((= aforc 1)
         (set_tile "aligned" "1")
        )
        ((= aforc 2)
         (set_tile "horizontal" "1")
        )
        ((= aforc 3)
         (set_tile "vertical" "1")
        )
       (T (set_tile "none" "1"))
    );cond
    (action_tile "accept" "(progn (force) (ddvals))")
    (action_tile "cancel" "(done_dialog)")
    (action_tile "pick" "(done_dialog 5)")
    (if P_BFOL
	(set_tile "follow" "1")
    );if
    (if P_QBSN
	(set_tile "base" "1")
    );if
    (if (not P_QBPT)
	(mode_tile "base" 1)
    );if
);defun

(defun ddvals ()
     (setq d (get_tile "dlist")
          l (get_tile "llist")
     );setq
     (if (/= d "")
          (setq P_DIND (atoi d))
     );if
     (if (/= l "")
          (setq P_LIND (atoi l))
     );if
     (if (= (get_tile "base") "1")
	(setq P_QBSN T)
	(setq P_QBSN nil)
     );if
     (if (= (get_tile "follow") "1")
	(setq P_BFOL T)
	(setq P_BFOL nil)
     );if
     (done_dialog)
);defun

;;;DMDRAW temporarily draws the guidelines on the screen
;;;issuing DMDRAW a second time with the same corrdinates will erase it
(defun dmdraw (pt1 pt2 pt3 pt4)
     (grdraw pt1 pt2 -1 3)
     (grdraw pt2 pt3 -1)
     (grdraw pt3 pt4 -1 3)
);defun

(defun setup ( / num)
     (setq te (getvar "TEXTEVAL")
	  C_SCAL (getvar "DIMSCALE")
          aforc 0
          tx ""
          ftxt""
	  osnlist (list "NEA" "ENDP" "MID" "INTE" "PER" "CEN" "INS" "NOD" "QUA" "TAN")
	  fname (findfile (strcat (getvar "MENUNAME") ".MNU"))
	  fp (open fname "r")
     );setq
     (while (/= (read-line fp) "***POP0"))
     (while (not (wcmatch (setq line (read-line fp)) "`**"))
	(setq count 0 flag nil)
	(repeat (length osnlist)
	    (setq word (nth count osnlist)
		  line (strcase line)
		  count (1+ count)
	    );setq
	    (if (wcmatch line (strcat "*" word "*"))
		(setq snlist (append snlist (list word)) flag T)
	    );if
	);repeat
	(if (not flag) (setq snlist (append snlist (list ""))))
     );while

     (if (not dcl_q)
          (setq dcl_q (load_dialog "QDIM.DCL"))
     );setq
     (setvar "CMDECHO" 0)
     (setvar "UNITMODE" 0)
     (setvar "TEXTEVAL" 1)

     (setq num 0)
     (if (not (wcmatch (getvar "CLAYER") "*DIM*"))
          (setq lalist (list (getvar "CLAYER")))
     );if

     (tblnext "LAYER" T)
     (while (setq opt (tblnext "LAYER"))
          (setq opta (dxf 2 opt))
          (if (wcmatch opta "*DIM*")
               (setq lalist (append lalist (list opta))
                    num (1+ num)
               );setq
          );if
          (if (and (= opta "FLR_DIM") (not P_LIND))
               (setq P_LIND num)
          );if
     );while
     (if (not P_LIND) (setq P_LIND 0))
     (if (> P_LIND (- (length lalist) 1)) (setq P_LIND (prompt "PL overflow") P_LIND 0))

     (if (not P_DIND) (setq P_DIND 0))
     (setq num nil)
     (while (setq num (tblnext "DIMSTYLE" (not num)))
          (setq dlist (append dlist (list (dxf 2 num))))
     );while

     (if (not (member (getvar "DIMSTYLE") dlist))
	(setq dlist (append (list (getvar "DIMSTYLE")) dlist))
     );if
     (if (> P_DIND (1- (length dlist))) (setq P_DIND (1- (length dlist))))
     (grtext -1 (strcat "Layer: " (nth P_LIND lalist) "  Style: " (nth P_DIND dlist)))
);defun


(defun base_pick ( / en ed)
    (setq en (car (entsel)))
    (if en (setq ed (entget en)))
    (if (= (dxf 0 ed) "DIMENSION")
	(setq   P_QBPT (dxf 10 ed)
		P_QBAN (angle (dxf 10 ed) (dxf 14 ed))
		P_QBSN T
	);setq
    );if
);defun

;;;SNAPTO is invoked when a line of text is completed within the QDIM loop.
;;;It determines whether it is valid input, then sets the BOX to 2
;;;which causes the grread to draw a pick box
(defun snapto ()
     (if     (or (= (strcase tx) "NEA")
               (= (strcase tx) "INT")
               (= (strcase tx) "PER")
          );or

          (progn
               (prompt " to: ")
               (setq box 2)
          );progn

          (progn
               (prompt "\nInvalid option")
               (prompt "\nLocate third point: ")
               (setq  tx ""
                    gd (grread T)
               );setq
          );progn
     );if
);defun

;;;ORIENT determines where to draw guidelines based on the mode (HOR, VERT or
;;;ALIgned) that QDIM is in
(defun orient ()
     (if P_QBSN
	(progn
	    (setq ptz (inters P_QBPT (polar P_QBPT (+ (* pi 0.5) P_QBAN) 1)
				ptp (polar ptp P_QBAN 1) nil
			);inters
	    );setq
	    (if (< (distance ptp ptz) 20.0) (setq ptp ptz))
	);progn
    );if

     (cond
          ((or (and     (> (car ptp) (car pt1))
                         (< (car ptp) (car pt2))
                         (> (cadr ptp) (cadr ptl))
                         (< (cadr ptp) (cadr pth))
                         (= aforc 0)
                    );and
                    (= aforc 1)
               );or

               (setq pta (inters  pt1 (polar pt1 angl 1)
                         ptp (polar ptp angp 1)
                         nil
                    );inters
                    ptb (inters   pt2 (polar pt2 angl 1)
                         ptp (polar ptp angp 1)
                         nil
                    );inters
                    mode "ALIGN"
               );setq
          );align mode

          ((or   (and (> ang (angle ptm pt4)) (< ang (angle ptm pt1)) (= aforc 0))
                    (and (> ang (angle ptm pt3)) (< ang (angle ptm pt2)) (= aforc 0))
                    (and (> ang (angle ptm pt2)) (< ang (angle ptm pt3)) (= aforc 0))
                    (and (> ang (angle ptm pt1)) (< ang (angle ptm pt4)) (= aforc 0))
                    (and (= (cadr pt1) (cadr pt2)) (= aforc 0))
                    (= aforc 2)
               );or

               (setq pta (list (car pt1) (cadr ptp))
                    ptb (list (car pt2) (cadr ptp))
                    mode "HOR"
               );setq
          );horizontal mode 

          (ptp (setq pta (list (car ptp) (cadr pt1))
                    ptb (list (car ptp) (cadr pt2))
                    mode "VERT"
               );setq
          );vertical mode
     );cond OVERALL
);defun orient

;;;QDDRAW determines whether DMDRAW needs to be called.  If so
;;;it resets the old variables (which begin with o) for future erasure
(defun qddraw ()
     (if (or (not (equal opt ptp)) (/= aforc oaforc))
          (progn
               (dmdraw pt1 opta optb pt2)
               (dmdraw pt1 pta ptb pt2)
               (grtext -1 (strcat "Layer: " (nth P_LIND lalist) "  Style: " (nth P_DIND dlist) "  Mode: " mode ftxt))
               (setq opt ptp              
                    opta pta
                    optb ptb
                    oaforc aforc
               );setq
          );progn
     );if
);defun

(defun force ()
     (cond
          ((= (get_tile "aligned") "1")
               (setq aforc 1)
          )

          ((= (get_tile "horizontal") "1")
               (setq aforc 2)
          )

          ((= (get_tile "vertical") "1")
               (setq aforc 3)
          )

          (T (setq aforc 0))
     );cond
     (if (> aforc 0)
          (setq ftxt "<--F")
          (setq ftxt "")
     );if
);defun

(defun dxf (code elist)
	(cdr (assoc code elist))
);defun

(if AUTO_RUN
  (progn
    (setq AUTO_RUN nil)
    (C:qdim)
  );progn
);if