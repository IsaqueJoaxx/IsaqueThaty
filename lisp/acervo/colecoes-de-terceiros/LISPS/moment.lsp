;This function calculates the centroid and the moment of inertia
;  for a profile.  The profile should be "selected" immediately
;  before invoking this routine.  The function creates creates a
;  layer named "Moment" and creates a red point at the centroid
;  and prints the x and y moment of inertia values.
;  The function averages exploded cross hatching to calculate the
;  centroid and integrates each points delta from the centroid to
;  determine the moment.  (see Cadence Vol.1 No.3, pg 51)
;
;  Note: Scale and the resulting accuracy can be adjusted by altering
;        the size of the view at invokation or the "setq scale"
;        assignment. The default setting is half of current view
;        size.  Smaller scales increase accuracy.
;================================================================
; CHAZ HENRY         NORTH CAROLINA STATE UNIVERSITY, RALEIGH, NC
;================================================================
; 919-365-6660 ext. 213 SIEMENS ENERGY AND AUTOMATION, RALEIGH NC
;================================================================

(defun moment ( / l xcord ycord temp momentx momenty cnt oldlay centroidx
                   centroiidy scale ss e scalesqterm cellsize)
  (if (ssget "p")
      (progn
         (setq cnt 0  centroidx 0 centroidy 0 oldlay (getvar "CLAYER"))

         ;create new layer cross hatch section and select exploded
         ;  cross hatching (zero length lines) using the drawing
         ;  limits as window corners.
         (setq scale (* (getvar "VIEWSIZE") 0.4))

         (command "LAYER" "m" "moment" "c" "red" "moment" "")
         (command "HATCH" "*DOTS" scale "45" "p" "")
         (command "LAYER" "off" "*" "n" "")
         (setq ss (ssget "W" (getvar "LIMMAX") (getvar "LIMMIN")))

         (setq l (sslength ss))
         (princ "calculating centroid.....")
         (while (< cnt l)
                (progn
                   (setq e (entget (ssname ss cnt)))
                   (setq centroidx (+ centroidx (car (cdr (assoc 10 e)))))
                   (setq centroidy (+ centroidy (nth 2 (assoc 10 e))))
                   (setq cnt (1+ cnt))
                   ))
         (setq centroidx (/ centroidx cnt) centroidy (/ centroidy cnt))

         (princ "done")
         (terpri)

         (setq cnt 0
               cellsize (* (/ scale 16) (/ scale 16))
               momentx 0
               momenty 0
               cellsqterm (/ (* cellsize cellsize) 12))

         (princ "calculating moment of inertia.....")

         (while (< cnt l)
                (progn
                   (setq e (entget (ssname ss cnt)))
                   (setq xcord (car (cdr (setq temp (assoc 10 e)))))
                   (setq ycord (nth 2 temp))
                   (setq momentx (+ momentx
                                    cellsqterm
                                    (* cellsize (* (setq temp (- centroidx xcord)) temp))))
                   (setq momenty (+ momenty
                                    cellsqterm
                                    (* cellsize (* (setq temp (- centroidy ycord)) temp))))
                   (setq cnt (1+ cnt))))

         ;erase construction points draw centroid and moment and
         ; restore the original layer setting.
         (princ "done")
         (terpri)
         (command "ERASE" "w" (getvar "LIMMAX") (getvar "LIMMIN") "")
         (command "POINT" (list centroidx  centroidy))
         (command "LAYER" "s" oldlay "")         ;restore old layer
         (terpri)
         (princ "X moment = ")(princ momentx)
         (princ "    ")
         (princ "Y moment = ")(princ momenty)
         (terpri)
         )
      "No items preselected..select profile and retry")

  )

