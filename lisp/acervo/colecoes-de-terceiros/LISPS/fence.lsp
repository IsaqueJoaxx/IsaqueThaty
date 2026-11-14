;;===========================================================================;;
;;;;Routine Name: FENCE.LSP- Version 2
;;;;Decription: C:FENCE -This routine will cut out entities enclosed within
;;;;            a FENCE placed by the user.  These entities will be
;;;;            returned in a selection set and processed according
;;;;            to the Fence Command specified at the first prompt.
;;;;                                 - - -
;;;;Notes:      FENCE requires that blocks and hatch patterns be exploded
;;;;            before running prog.  Also, FENCE will not cut text, arcs or
;;;;            circles, but it will include them in the selection set if
;;;;            their insertion points are within the fence.
;--
;;;;Programmer: Andy Higgins- City of Austin Electric Bldg., 301 West Av
;;;;Date      : 4/18/91
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;<<<<<<*ERROR*>>>>>>>>>>>>>>>>>>
(defun *ERROR* (s)
        (prompt "\n*ERROR*:") (princ s)
        (prin1)
);end defun
;<<<<<<<<<<<<MINTER>>>>>>>>>>>>>>
;
;This routine returns multiple intersections in a list between a line
;from starting point ST to ending point EN and a closed polyline whose
;vertices are defined in PLIST.
;
(defun minter (st en plist)
(setq
 brkcnt 0
 pt3 (car plist)
 blist1 ()
 blist ()
 blist0 ()
 bdist 0)
 ;
(foreach a plist
(if (/= (setq b (inters pt3 a st en)) nil)
 (progn
    (if (or (and (= (car b) (car st)) (= (cadr b) (cadr st)))
            (and (= (car b) (car en)) (= (cadr b) (cadr en))))
(progn (prompt "\rLine Start at Fence: ") (setq lscnt (1+ lscnt) problm t)(princ lscnt))
    );end if
    (setq blist (append blist (list b)) brkcnt (1+ brkcnt) brkflg t)
 );end progn
);end if
(setq pt3 a)
);end foreach
;----sort blist by distance
(while (> (length blist) 0)
 (setq closds (distance st en))
 (foreach a blist 
     (if (< (setq b (distance st a)) closds) (setq closbp a closds b))
 );end foreach
 ;delete closbp from blist
 (setq blist1 (append blist1 (list closbp)))
 (setq blista (cdr (member closbp blist)))
 (setq blistb (reverse (cdr (member closbp (reverse blist))))) 
 (setq blist (append blistb blista))
);end while
(setq blist1 (cons st blist1))
);end defun
;
;<<<<<<<<INSIDE>>>>>>>>>>>>
;
;
;This routine determines whether or not IPT is inside the polyline
;described by plist or not. SPT is the reference point used to determine 
;this and it is normally set to pt1.  Returns -1 if outside and 1 if
;IPT is inside
;
(defun inside (ipt spt plist)
        (setq pt3 (car plist) iflag -1)
        (foreach a plist
                (if (/= (inters pt3 a ipt spt) nil)
                        (setq iflag (* iflag -1))
                );end if
        (setq pt3 a)
        );end foreach
(eval iflag)
);end defun
;
;(defun c:fndpt ()
;(while (/= (setq g (car (cdr (grread 5)))) nil)
;        (if (= (inside g pt1 listv) 1) (command "point" g))
;);end while
;);end defun
;
;
;<<<<<<<<<<<<PLTRIM>>>>>>>>>>>>>>>>.
;
;FORMAT:(pltrim <polyline name>)
(defun pltrim (pnam1)
        (setq listv3 (reverse (getlst pnam1)))
        (setq lncmd "pline")
        (setq pstwid (cdr (assoc 40 elist2)))
        (setq penwid (cdr (assoc 41 elist2)))
        (mkplst listv3 listv enam2)
);end defun mkplst
;        
;<<<<<<<<<<<<LNTRIM>>>>>>>>>>>>>>>>
;
;FORMAT:(lntrim <start pt> <end pt>)
(defun lntrim (lnpt1 lnpt2)
        (setq listv3 (list lnpt1 lnpt2))
        (setq lncmd "line")
        (mkplst listv3 listv enam2)
);end defun
;
;<<<<<<<<<<<<<<<MKPLST>>>>>>>>>>>>>>>>>>>>>>>>>>>
;
;FORMAT:(mkplst <vertex list1> <vertex list2> <entity name>)
;
;This routine will cut the polyline whose verticies are defined in 
;LISTV2 when it falls inside the closed polyline whose verticies are
; defined in LISTVC.
;This routine returns two lists.  The first list INLIST is a list of
;points defining all lines that are inside the closed polyline 
;defined in listv.  The second list OUTLST is all those outside.
; Then the original polyline is erased and redrawn broken up
;with all segments inside added to SS1.
;
(defun mkplst (listv2 listvc pnam3)
;(setq oldcol (atoi (getvar "cecolor")))
;(setq oldlnt (getvar "celtype"))
(if (= color2 0) (setq color2 "BYBLOCK"))
(if (= color2 nil) (setq color3 "BYLAYER") (setq color3 color2))
(if (= lntyp2 nil) (setq lntyp3 "BYLAYER") (setq lntyp3 lntyp2))
(command "color" color3)
(command "linetype" "s" lntyp3 "")
;(command "layer" "s" laynm2 "")
  (setq inlist ()
        outlst ()
        problm nil
        brkflg nil
        alist ()
        lstver (car listv2)
        addflg (inside lstver pt1 listvc)
        lstflg addflg
        frsver lstver)
        (setq listv2 (append (cdr listv2) (list (last listv2))))
        (setq cnt 0)
        (foreach c listv2
            (setq cnt (1+ cnt))
            (setq st lstver lstver c tlist (minter st lstver listvc))
            (if (= cnt (length listv2)) (setq addflg (* addflg -1)))
            (mklist)
        );end while
        ;
        (if (and (/= brkflg nil) (= problm nil))
        ;-----Redraw Entity inside of fence---
        (progn
        (command "erase" pnam3 "")
        (setq plist3 (entget pnam3))
        (foreach lnlist inlist (command lncmd)
        (foreach b lnlist (command b));end foreach
        (command "")
        (setdwg);modify new entities color, linetype and width to original's
        (ssadd newent ss1)
        );end foreach
        ;----Redraw Entity outside of Fence
        (foreach lnlist outlst (command lncmd)
        (foreach b lnlist (command b));end foreach
        (command "")
        (setdwg)
        );end foreach
        );end progn then
        ;
        (progn ;else
        (if (= problm nil)
        (if (= (inside frsver pt1 listvc) 1) (ssadd pnam3 ss1))
        (ssadd pnam3 ss1)
        );end if
        );end progn
        );end if
);end defun
;
;<<<<<<<<MKLIST>>>>>>>>>>
;
(defun mklist ()
     (foreach k tlist
        (setq alist (append alist (list k)))
        (if (/= addflg lstflg)
          (progn
           (setq alist (append alist))
            (if (= lstflg 1)
              (setq inlist (append inlist (list alist)))
              (setq outlst (append outlst (list alist)))
            );
            (setq alist (list k) lstflg addflg)
          );end progn
        );end if
        ;
        (setq addflg (* addflg -1))
     );end foreach
     (setq addflg (* addflg -1))
);end defun
;
;<<<<<<<GETLST>>>>>>>>>>>>
;
;
;Routine returns a list of (x,y) of all verticies in polyline PNAM
;for both closed and open polylines
;
(defun getlst (pnam / listx listy alist a)
       (setq listx ()
              listy ()
              listq ()
              alist ()
              a (entnext pnam)) ;gets first vertex
        (while (/= (cdr (assoc 0 alist)) "SEQEND")
           (setq alist (entget a)
           a (entnext a)
           listq (cons (cdr (assoc 10 alist)) listq))
        )  ;end WHILE seqend
        (if (= (logand (cdr (assoc 70 (entget pnam))) 1) 1) 
                (setq closed t listq (subst (last listq) nil listq))
                (setq closed nil listq (cdr listq))
        );end if
);end defun
;
;<<<<<<<SETDWG>>>>>>>>>>>.
(defun setdwg ()
        (setq newent (entlast))
        (setq newlst (entget newent))

           (if (= lncmd "pline")
              (progn
                (setq newlst (subst (cons 40 pstwid) (assoc 40 newlst) newlst))
                (setq newlst (subst (cons 41 penwid) (assoc 41 newlst) newlst))
              );end progn
           );end if pline

        (setq newlst (subst (cons 8 laynm2) (assoc 8 newlst) newlst))
        ;(if (= color2 nil) (setq color3 "BYLAYER") (setq color3 color2))
        ;(setq newlst (subst (cons 62 color3) (assoc 62 newlst) newlst))
        ;(if (= lntyp2 nil) (setq lntyp3 "BYLAYER") (setq lntyp3 lntyp2))
        ;(setq newlst (subst (cons 6 lntyp3) (assoc 6 newlst) newlst))
        (entmod newlst)
        (entupd newent)
);end defun
;
;
;<<<<<<<<<<<<UPDCLK>>>>>>>>>
;
;
(defun updclk ()
	(setq pntang (* (/ fcnt (sslength ss)) (* pi 2.0)))
(while (< lstpag pntang)
       (setq pntr2 (polar crstpt (- ninety lstpag) linlen))
       (setq pntls2 (subst (cons 10 pntr2) (assoc 10 pntls2) pntls2))
       (entmod pntls2) (entupd pnten2)	
       (setq lstpag (+ lstpag incrad))
);end while
(setq lstpag pntang)
);end defun

;<<<<<<<<<<<<MAKESS>>>>>>>>>>>
;
(defun makess ()
(setq pt1 (getpoint "\nChoose Side to Fence"))
	(setq ss (ssadd))
	(setq lstver (car listv))
	(setq plist (cdr listv))	
	(foreach a plist
		(addss ss (ssget "c" lstver a))
		(setq lstver a)
	);end foreach
);end defun
;
;
;<<<<<<<<<ADDSS>>>>>>>>>>>
;
;
;
(defun addss (ssa ssb) ;add ssb to ssa
        (setq scnt 0)
        (while (< scnt (sslength ssb))
               (ssadd (ssname ssb scnt) ssa)
                (setq scnt (1+ scnt))
         );end while
);end defun
;<<<<<<<<<<<<<<<<<<< FENCE >>>>>>>>>>>>>>>>>>>>>>>>>>>>>
;
(defun c:fence ( / type type2 icnt xpt1 pt2 pt3 a ss alist pcnt pcnt2
                              elay enam flag listx listy xlistv minx miny maxx 
                              maxy cnt etype vtxnam vtxlst strpnt plist)
                           
(setvar "cmdecho" 0)
(setvar "blipmode" 0)
;--------
(while (= (substr (setq fncmd (strcase (getstring "\nTimer/Fence Command <ERASE>: ")))
 1 1) "T")
 (initget 0 "Yes No")
 (setq ans (getkword "\nDisplay Timer? <N>"))
  (if (or (= and "No") (= ans "") (= ans nil))
    (setq shwclk nil)
    (setq shwclk t)
  );end if
);end while
;----
(if (= fncmd "") (setq fncmd "ERASE"))
;(command "viewres" "n" 100)
(setq cirad 0.405)
(setq ss1 (ssadd))
(setq vewctr (getvar "viewctr"))
(setq scnsiz (getvar "screensize"))
(setq scrato (/ (car scnsiz) (cadr scnsiz)))
(setq schght (getvar "viewsize"))
(setq scwidt (* scrato schght))
(setq scwid2 (/ scwidt 2.0))
(setq schght2 (/ schght 2.0))
(setq ckscale (/ scwidt 10.0)) 
(setq clkwid2 (/ ckscale 2))
(setq linlen (* ckscale cirad))
(setq crstpt (polar vewctr (/ pi 2) schght2))
(setq crstpt (polar crstpt 0 scwid2))
(setq crstpt (polar crstpt pi clkwid2)) 
(setq crstpt (polar crstpt (* 1.5 pi) clkwid2))
;--------PLACE FENCE----
(setq eflag t)
(while (/= eflag nil)
(setq lstpt (getpoint "\nStarting Point of Fence."))
(setq ptlist (list lstpt))
 (while (/= (setq pt (getpoint lstpt "\nNext Point\<Close>")) nil)
(setq ptlist (append ptlist (list pt)))
	(grdraw lstpt pt 7)
	(setq lstpt pt)
 );end while getpoint
 ;
 (command "pline")
 (foreach b ptlist (command b))
 (command "c")
 ;
;------------------
(setq enum (entlast))
(if (= enum nil)
 (setq eflag t)
 (if (= (cdr (assoc 0 (entget enum))) "POLYLINE") (setq eflag nil) (setq eflag t))
);end if
);end while
;(prompt "\n Found Polyline")
;-----find min and max points
        (setq listv (getlst enum))
	(if (= closed t)
	  (progn	

        (setq minx (car (last listv))
              miny (cadr (last listv))
              maxx minx
              maxy miny
        ;
        ); end setq
        ;
        (foreach a listv
         (setq minx (min minx (car a))
               miny (min miny (cadr a))
               maxx (max maxx (car a))
               maxy (max maxy (cadr a))
         )
        )  ;end FOREACH a
        ;(prompt "\n Getting Selection Set... ")
        (setq  pt1 (list minx miny)
               pt2 (list maxx maxy) 
               other pt2
               ss (ssget "c" pt1 pt2)
		slength (sslength ss)
        );end setq
      );end progn
	(makess)
    );end if
;(prompt "\n Found ") (princ (sslength ss)) (prompt " objects.")
;----Adjust anchor Point if necessary---
(foreach a listv
(if (and (= (car pt1) (car a)) (= (cadr pt1) (cadr a))) 
        (progn
        ;(prompt "\nAdjusting Anchor Point. ")
(setq pt1 (polar pt1 4.71 (* 2 (/ (getvar "viewsize") (cadr (getvar "screensize"))))))
        );end progn
);end if
);end foreach
;-------
(if shwclk
(progn
;----PLACE CLOCK-----
(command "color" 1)
(setq fdrlisp "c:\\acad\\")
(if (findfile (strcat fdrlisp "clock.dwg"))
(command "insert" (strcat fdrlisp "clock") crstpt ckscale "" "")
(command "donut" 0 (* ckscale 0.8) crstpt "")
);end if
;---------
(setq cirent (entlast))
(command "color" 7)
(setq incr 1.5) 
(setq ninety (/ pi 2.0))
(setq incrad (* pi (/ incr 180.0)))
(setq endwid (* linlen incrad))
;(command "line" crstpt (polar crstpt ninety linlen))
(command "pline" crstpt (polar crstpt ninety linlen) "")  
(setq pntent (entlast))
(setq b (entnext pntent))
(setq pnten2 (entnext b))
(setq pntlst (entget pntent))
(setq pntls2 (entget pnten2))
;set polyline width
(setq pntlst (subst (cons 40 0) (assoc 40 pntlst) pntlst))
(setq pntlst (subst (cons 41 endwid) (assoc 41 pntlst) pntlst))
(entmod pntlst) (entupd pntent)
);end progn
);end if
;---------------------------------
 (setq fcnt 0.0)
        (setq lstpag 0.0)
        (ssdel enum ss)
        (setq ss1 (ssadd) lscnt 0)
        ;(ssadd enum ss1)
        (while (< fcnt (sslength ss))
        (if shwclk (updclk))
;
;        (prompt "\r Selection Set Size:") (princ (- (sslength ss) fcnt)) 
;        (prompt "      ")
         (setq enam2 (ssname ss fcnt)
	              elist2 (entget enam2)
               type2 (cdr (assoc 0 elist2))
               laynm2 (cdr (assoc 8 elist2))
               lntyp2 (cdr (assoc 6 elist2))
               color2 (cdr (assoc 62 elist2))
               pt3 (car listv)
               icnt 0
               modflg 0
               brkcnt 0
               pt2 (cdr (assoc 10 (entget (ssname ss fcnt))))
               pt5 (cdr (assoc 11 (entget enam2))) 
          );end setq enam2
  ;--------------
  (if (and (= type2 "POLYLINE") (/= laynm2 "QGRID"))
   (pltrim enam2)      ;(pexplod enam2)
   ;-------------
   (progn ;else not polyline
    (if (/= type2 "LINE")
	(progn 
	   (if (/= laynm2 "QNAME")
	      (if (= (inside pt2 pt1 listv) 1) (ssadd enam2 ss1));not a line
	   );end if
	);end progn
       (lntrim pt2 pt5)
    );end if "LINE"
   );end progn "then not a polyline"
  );end if "polyline"
  ;--------------
 (setq fcnt (1+ fcnt))
) ;end WHILE fcnt
;---
(if shwclk 
(progn
(updclk)
(command "erase" cirent pntent "")
);end progn
);end if
;------
(setvar "highlight" 1)
;
(entdel enum)
(setq fncmd (strcase fncmd))
(princ "\n")
;----BLOCK----
(if (= fncmd "BLOCK")
(progn ;1
(setq bkname (getstring "\nBlock Name: "))
(setq inspt (getpoint "\nInsertion Base Point: "))
(princ "\n")
(command fncmd bkname inspt ss1 "")
);end progn 1
;----ERASE
(progn ;else not "BLOCK" 2
  (if (= fncmd "ERASE")
    (command "erase" ss1 "")
    (progn ;3
     (if (= fncmd "WBLOCK")
       (progn ;4
        (setq filnam (getstring "\nFile Name: "))
        (setq bkname (getstring "\nBlock Name: "))
         (if (= bkname "")
          (progn
          (setq inspt (setq pt (getpoint "\nInsertion Base Point: ")))
          (command "wblock" filnam "" pt ss1 "")
          );end progn
          (command "wblock" filnam bkname)
         );end if
       );end progn ;4
       ;-else not erase,wblock or block
       (progn ;5
         (setvar "cmdecho" 1)
         (command fncmd ss1 "" pause pause)
       );end progn ;5
     );end if wblock
    );end progn else not erase
  );end if erase
);end progn ;2
);end if
;-----
(prin1)
)         ;end DEFUN FENCE
;

