;This program will take a ployline, 2D or 3D, and use it as the centerline
;for a tube.
;The number of segments for the tube are supplied by "Surftab1" and
;"Surftab2". You input the Radius and pick the centerline. The program then
;makes a 3D Mesh Tube. You can fine tune it with Pedit.
;I got the idea for this program from using Jamie Clay's "Mfit" program.
;I was drawing a 1.5 inch power cable in 3D to examine how it fit in a
;cubicale. The procedure I followed was the same as in this program,
;only I did it manually. When I had to do subsequent cables I decided to
;incorperate it into a Lisp program. Since "Mfit" did the job in the first
;place, I decided to build on it. I included credits to Jamie for the part
;he did. Also you may notice that the defuns he did are not taylor made for
;this program, I get paid for drawing, not programing, and didn't have the 
;time to customize everything for optimum performence. None the less I think
;it is a usefull program and decided to upload it for you guys.
;As far as I can tell everything works right, let me know if anything is
;screwed up. Jay Parisi CIS ID 76526,3640  03/13/89

(vmon) 
(defun c:3dtube ()
  (setvar "cmdecho" 0)
  (setq radius (getdist "\nInput radius of tube: "))
  (setq tab1 (getvar "surftab1")) ;set tabs to current surftabs
  (setq tab2 (getvar "surftab2"))
  (setq center nil)
;get centerline and make sure it a poly
  (while (not center) 
    (setq center (entsel "\nSelect centerline of tube: "))
    (if center
      (progn
        (setq pltest (cdr (assoc 0 (entget (car center)))))
        (if (/= pltest "POLYLINE")
          (progn
            (setq center nil )
            (prompt "\nCenterline of tube must be a polyline")
          )
        )
      )
    )
  )
;set a undo mark to come back to
  (command "undo" "mark")
  (command "ucs" "w")
  (command "layer" "m" "$$3dcirs$$" "")
  (setq cen (car center))
  (setq divtemp tab2)
;divide the center line according to surftab2
  (divcenter)
;find out how much of the arc to leave out so we can use the same function
  (setq circum (* 2 pi radius))
  (setq sector (/ circum tab1))
  (setq arcang (/ sector radius))
  (setq plang (- 360 (rtd arcang)))
  (setq index 0)  
  (command "ucs" "w" )
  (setq XYZpts nil)
  (setq pts nil)
  (setq leth (1+ tab2))
;get the points to make the segments
  (getpoints)
  (setq divtemp (1- tab1))
;draw the segments and divide them like the centerline
  (repeat tab2
    (command "ucs" "za" (nth index XYZpts) (nth (1+ index) XYZpts) )
    (command "pline" (list radius 0) "a" "ce" "0,0" "a" plang "")
    (command "ucs" "w" )
    (setq center (entlast))
    (setq center (list center (list radius 0)))
    (setq cen (car center))
    (divcenter)
    (setq index (1+ index))
  )
  (command "ucs" "za" (nth index XYZpts) (nth (1- index) XYZpts) )
  (setq plang (- 0 plang))
;'cuz we changed the ucs, we gotta reverse the last segment
  (command "pline" (list (- 0 radius) 0) "a" "ce" "0,0" "a" plang "")
  (command "ucs" "w" )
  (setq center (entlast))
  (setq center (list center (list radius 0)))
  (setq cen (car center))
;divide the last segment
  (divcenter)
  (setq XYZpts nil)
  (setq pts nil)
  (setq leth (* tab1 (1+ tab2)))
  (command "ucs" "w")
;get the points for the mesh
  (getpoints)
  (command "undo" "back")
;now make the mesh.
  (command "ucs" "w")
  (command "3dmesh" (1+ tab2) tab1)  ; Start 3dmesh command
  (setq pt# 0)
  (repeat (length XYZpts)
    (setq pt1 (nth pt# XYZpts)) ; pull out a point
    (command pt1)               ; send it to the mesh command
    (setq pt# (1+ pt#))         ; move on
  )
;close it cuz its a tube
  (command "pedit" "last" "n" "")
  (command "ucs" "p")
)
;thats it!!! 
; Divide plines by Jamie Clay
(defun divcenter ()
  (setq lst (entlast))     ; Set last entity
  (setq vtx (entnext cen)) ; Find first vertex
  (setq plbit (cdr (assoc 70 (entget cen)))) ; What kind of pline is this?
  (setq lptA (cdr (assoc 10 (entget vtx))))  ; Set up a last point
  (if (= (logand plbit 1 ) 1 )       ; If open place a first vertex point
    (command "divide" center divtemp)
    (progn
      (command "point" (trans lptA cen 0))         ; first vertex point
      (command "divide" center divtemp)
    )
  )

;Walk to the end of the pline to find the last vertex point
  (while (/= (cdr (assoc 0 (entget (entnext vtx)))) "SEQEND")
    (setq vtx (entnext vtx))
    (setq vrtx (cdr (assoc 0 (entget vtx))))
    (setq lptB (cdr (assoc 10 (entget vtx))))
  )
  (if (= (logand plbit 5 ) 5)  ; if open and spline place last vertex point
    (command "point" (cdr (assoc 10 (entget (entnext lst))))) ; if closed
    (if (= (logand plbit 1) 1)
      (command "point" (trans lptA cen 0)) ; closed pline point
      (command "point" (trans lptB cen 0)) ; last vertex point
    )
  )
  (setq refpt (getass 10 (entlast)))
)
;get the points for the centerline segments and the mesh. By Jamie Clay
(defun getpoints ()
  (setq pts (ssget "x" '((8 . "$$3dcirs$$") (0 . "POINT")))) ; collect points
  (setq indx 0)                                ; set index
  (repeat leth
    (setq 10pt (get10 (ssname pts indx)))    ; get point value
    (setq indx (1+ indx))                      ; move index
    (if XYZpts
      (setq XYZpts (append XYZpts (list 10pt))) ; add point to list
      (setq XYZpts (list 10pt))                 ; create list if not
    )
  )               
)                                               
; Get the 3dpoints and trans them if needed
(defun get10 (x)
  (if (= (getvar "worlducs") 0)
    (trans (cdr (assoc 10 (entget x))) 0 1)
    (cdr (assoc 10 (entget x)))
  )
)

; Get as assoc member of an entity
(defun getass(x y)
    (cdr (assoc x  (entget y)))
)
; turn radians to degrees
(defun rtd (a)
  (/ (* a 180.0) pi)
)