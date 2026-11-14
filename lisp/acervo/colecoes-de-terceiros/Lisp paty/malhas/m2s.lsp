;;    M2S  (Mesh-to-Solid)
;;    Creates an ACIS solid from an open 3d polygon mesh.
;;
;;    Take 2 - Updated 7/7/1998
;;       - Works with REVSURF'd meshes that touch or cross axis of revolution.
;;       - Works even if solid being constructed is not fully visible on screen.
;;       - Works with all open meshes created with REVSURF, RULESURF,
;;          EDGESURF, TABSURF, AI_MESH, and 3DMESH. Most of the stock 3D
;;          surfaces will work if you use DDMODIFY to open them in the M
;;          and N directions.
;;       - Does not work with polyface entities.
;;
;;    (c) Copyright 1998 Bill Gilliss.  
;;        All rights reserved... such as they are.
;;
;;    bill.gilliss@aya.yale.edu    gilliss@iglou.com
;;
;;       I wrote this to create sculptable ACIS terrain models
;;    for architectural site renderings. It could also be used
;;    to create thin shells from meshes, by subtracting a moved
;;    copy of the solid from the original solid. Let me know of
;;    other uses you find for it, or problems you encounter.
;;
;;       The solid is created by projecting each mesh facet "down"
;;    the current z-axis to a plane a user-specified distance below
;;    the lowest vertex. To assure that all parts of the mesh are
;;    generated as solids, this distance can not be zero, but the
;;    solid can be SLICEd later if need be.
;;
;;       The solid will match the displayed mesh: if the mesh has
;;    been smoothed and SPLFRAME is set to 0, the solid will be
;;    smoothed. Otherwise, it will not be. The mesh itself is not
;;    changed at all.
;;


(defun c:m2s (/  ent ename entlst M N MN SN SM ST smooth oldecho vtx d1
                 low vtxcnt vtxmax bot bottom p1 p2 p3 p4 c1 c2 c3 c4 
                 b1 b2 b3 b4 soldepth ssall ssrow)

(setq oldecho (getvar "cmdecho"))
(setq oldsnap (getvar "osmode"))
(setq oldblip (getvar "blipmode"))
(setvar "cmdecho" 0)
(setvar "osmode" 0)
(setvar "blipmode" 0)
(command "undo" "begin")

;;select the mesh
  (setq ent (entsel "Select a polygon mesh to solidify: "))
  (setq ename (car ent))
  (setq entlst (entget ename))

  (if (not (= (cdr (assoc 0 entlst)) "POLYLINE"))
    (progn
      (alert "That is not a polygon mesh.")
      (exit)
      (princ)
    );progn
  );endif

  (if
    (not
      (or
       (= (cdr (assoc 70 entlst)) 16) ;open 3d polygon mesh
       (= (cdr (assoc 70 entlst)) 20) ;open mesh w/ spline-fit vertices
        );or
       );not
     (progn
       (alert "That is not an *open* polygon mesh.")
       (exit)
       (princ)
     );progn
  );endif

;; decide whether to use smoothed or unsmoothed vertices
  (setq M (cdr (assoc 71 entlst)))   ;M vertices
  (setq N (cdr (assoc 72 entlst)))   ;N vertices
  (setq SM (cdr (assoc 73 entlst)))  ;smoothed M vertices
  (setq SN (cdr (assoc 74 entlst)))  ;smoothed N vertices
  (setq ST (cdr (assoc 75 entlst)))  ;surface type
  (if
    (or
      (= (getvar "splframe") 1)      ;use MxN vertices when splframe = 1
      (= ST 0)                       ;or mesh has not been smoothed
      )
    (setq smooth 0
         MN (* M N))
    (setq smooth 1                   ;use SMxSN vertices when mesh is smoothed 
          MN (* SM SN)               ;and SPLFRAME = 0
          M SM
          N SN)
    );if

;; determine lowest vertex
  (grtext -2 "Checking out the mesh...")
  (setq vtx ename)
  (setq vtx (entnext vtx))
  (setq d1 (entget vtx))
  (setq bottom (caddr (trans (cdr (assoc 10 d1)) 0 1)))
  
  (repeat (1- MN)   ;compare with each vertex's z coord
    (setq vtx (entnext vtx))
    (setq d1 (entget vtx))
    (setq low (caddr (trans (cdr (assoc 10 d1)) 0 1)))
    (setq bottom (min bottom low))
    );repeat

;; get desired thickness of solid
  (setq soldepth 0)
  (while
     (zerop soldepth)
     (progn
       (setq soldepth 
          (getdist "\nEnter desired thickness of solid below lowest vertex <1>: "))
       (if (not soldepth) (setq soldepth 1.0))
       (if (zerop soldepth)
          (princ "\nThickness can be small, but not zero. (Slice it later, if need be.)"))
        );progn
     );while
  (setq bot (- bottom (abs soldepth)))
   
  (setq p1 ename)
  (if (= smooth 1)
      (setq p1 (entnext p1))) ;skip 1st vtx of smoothed mesh - not true vtx
  (setq ssrow (ssadd))        ;initialize set of extruded segments to be unioned as a row
  (setq ssall (ssadd))        ;initialize set of rows to be unioned into the whole
  (grtext -2 "Creating row...")
  (setq vtxmax (- MN N))  
  (setq vtxcnt 1)

;;create row of solid segments
  (while (< vtxcnt vtxmax)

    (if (= 0 (rem vtxcnt N))  ;at end of each row...
        (progn
          (setq rowmsg (strcat "Unioning row "
                       (itoa (/ vtxcnt N)) " of "
                       (itoa (1- M)) "... "))
          (grtext -2 rowmsg)
          (command "union" ssrow "")
          (setq row (entlast))
          (ssadd row ssall)
          (setq ssrow (ssadd))
          (setq p1 (entnext p1)         ;skip to the next vertex
                vtxcnt (1+ vtxcnt))
          );progn
        );if
        
    (grtext -2 "Creating row...")
    (setq p1 (entnext p1)                  ;first vertex of mesh square
          p2 (entnext p1)                  ;second vertex
          p3 p2)
    (repeat (1- n) (setq p3 (entnext p3))) ;walk along to 3rd (p1 + N) vertex
    (setq p4 (entnext p3))                 ;4th vertex of mesh square

    (setq c1 (trans (cdr (assoc 10 (entget p1))) 0 1) ;top coordinates
          c2 (trans (cdr (assoc 10 (entget p2))) 0 1)
          c3 (trans (cdr (assoc 10 (entget p3))) 0 1)
          c4 (trans (cdr (assoc 10 (entget p4))) 0 1)
          b1 (list (car c1) (cadr c1) bot)            ;bottom coordinates
          b2 (list (car c2) (cadr c2) bot)
          b3 (list (car c3) (cadr c3) bot)
          b4 (list (car c4) (cadr c4) bot))
          (LOFT c1 c2 c3 b1 b2 b3)
          (LOFT c2 c3 c4 b2 b3 b4)

    (setq vtxcnt (1+ vtxcnt))
  );while

 (grtext -2 "Unioning last row...")
  (command "union" ssrow "")
  (setq row (entlast))
  (ssadd row ssall)
  (if (> M 2)       ;bypass final union for N x 1 meshes (i.e., RULESURF)
    (progn
      (grtext -2 "Unioning all rows...")
       (command "union" ssall "")
        );progn
     );if

;;cleanup
  (command "undo" "end")
  (setvar "cmdecho" oldecho)
  (setvar "osmode" oldsnap)
  (setvar "blipmode" oldblip)
  (setq ssall nil ssrow nil)
  (princ)

);defun

;;============== SUBROUTINES ====================
;(defun *error* (msg)
;  (command)
;  (command "undo" "end")
;  (setvar "cmdecho" oldecho)
;  (setvar "osmode" oldsnap)
;  (setvar "blipmode" oldblip)
;  (princ (strcat "\nError: " msg))
;  );defun

(defun LOFT (r1 r2 r3 s1 s2 s3 / e1 extr highest)
  (command "area" s1 s2 s3 "")
  (if (not (equal (getvar "area") 0.0 0.00000001))
    (progn
      (command "pline" s1 s2 s3 "c")
      (setq highest (max (caddr r1) (caddr r2) (caddr r3)))
      (setq extr (- highest bot))
      (command "extrude" (entlast) "" extr 0.0)
      (command "slice" (entlast) "" "3points" r1 r2 r3 s1)
      (setq e1 (entlast))
      (ssadd e1 ssrow)
      );progn
    );if
  );defun

(princ "M2S loaded.")

