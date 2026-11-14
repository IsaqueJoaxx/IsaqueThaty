;
;       Fractal Trees
;
;       Designed and implemented by Kelvin R. Throop in January of 1985
;
;       This command expands the most recently drawn LINE entity into a
;       fractal tree.  The angle of branching is given by the constant
;       BRANCH set below, and the factor by which each generation of
;       branches is shorter than the preceding is set by the constant
;       LFAC.  The number of cycles of branching is solicited from the
;       user.
;
(defun c:tree ()

        (setq branch (/ pi 3))
        (setq lfac 2.5)
        (setq zoomc 10)

        (if (= (cdr (assoc 0 (entget (setq cent (entlast))))) "LINE")
           (progn
              (setq sbm (getvar "blipmode"))
              (setq sce (getvar "cmdecho"))
              (setvar "blipmode" 0)
              (setvar "cmdecho" 0)
              (if (> zoomc 0)
                 (command "view" "save" "fractree")
              )
              (setq zoomd nil)
              (setq nbran 3)
              (setq ipow 1 iter 1)
              (repeat (1- (getint "Number of iterations: "))
                 (setq iter (+ iter (setq ipow (* ipow nbran))))
              )

              (setq e (entget cent))       ; Duplicated to make loop faster
              (setq p1 (cdr (assoc 10 e)))
              (setq p2 (cdr (assoc 11 e)))
              (setq d (/ (distance p1 p2) lfac))
              (setq zoomd (/ d zoomc))

              (repeat iter
                 (setq e (entget cent))
                 (setq p1 (cdr (assoc 10 e)))
                 (setq p2 (cdr (assoc 11 e)))
                 (setq d (/ (distance p1 p2) lfac))
                 (setq a (angle p1 p2))
                 (command "line" p2 (polar p2 (+ a branch) d) "")
                 (command "line" p2 (polar p2 a d) "")
                 (command "line" p2 (polar p2 (- a branch) d) "")
                 (if (< d zoomd)
                    (progn
                       (command "zoom" "c" p2 (* d 10))
                       (setq zoomd (/ d zoomc))
                    )
                 )
                 (setq cent (entnext cent))
              )
              (if (> zoomc 0)
                 (command "view" "restore" "fractree")
              )
              (setvar "blipmode" sbm)
              (setvar "cmdecho" sce)
           )
           (princ "\nLast entity is not a line!           ")
        )
        nil
)
