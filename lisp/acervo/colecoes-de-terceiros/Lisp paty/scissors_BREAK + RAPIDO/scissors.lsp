(defun C:n( / pt1 bpt1 bpt2 ss1 dist1 ent1 centrecercle)
  ;;make it quiet
  (setvar "cmdecho" 0)

  ;;first, get the breaking point
  (setq pt1 (getpoint "Give me the break point... ")) ;;point where to break

  ;;now, check if there's more than one object under that point
  (setq ss1 (selectfrompoint pt1))

  ;;if there's more than 1 object under point, ask to select, else use existing point
  (if (> (sslength ss1) 1)
   (progn ;;then
   (princ "DISAMBIGUATION : pick the object to break...")
   (setq ent1 (entsel))
   (setq ent1n (car ent1)) ;; e-name of ent1
   (setq bpt1 (cadr ent1)) ;; point on ent to break
   )
   (progn ;;else
   (setq ent1n (ssname ss1 0));;store into an entity (can be useful further)
   (setq bpt1 pt1) ;; point on ent to break
   )
  )

  ;;but if it's a circle (thus usually unbreakable)...
  (if
    (= (cdr (assoc 0 (entget ent1n))) "CIRCLE")
    ;;then replace the circle by 2 arcs joining at the break point
    (progn
     (princ "It's a circle !")
     (setq centrecercle (cdr (assoc 10 (entget ent1n))))
     (command "_arc" "_c" centrecercle pt1 "_a" "180")
     (command "_arc" "_c" centrecercle pt1 "_a" "-180")
     (entdel ent1n)
    )
    ;;else perform a normal break
    (progn
    (command "_break" bpt1 "_f" pt1 pt1)
    )
)

  ;;restore cmdecho
  (setvar "cmdecho" 1)
  ;;end quietly
  (princ)
)

;;--- this function makes a selection set of entities beneath a point 
(defun selectfrompoint (bpt1 / dist1 ss1)
  (setq dist1 (/ (getvar "viewsize") 200)) ;; set a distance equal to 1/200-th of view height
  (setq ss1 ;;select by fence around point
   (ssget "_F"
    (list
    (list (+ (car bpt1) dist1) (+ (cadr bpt1) dist1) 0)
    (list (- (car bpt1) dist1) (- (cadr bpt1) dist1) 0)
    )
   )
  );; setq ss1
  ss1
)
