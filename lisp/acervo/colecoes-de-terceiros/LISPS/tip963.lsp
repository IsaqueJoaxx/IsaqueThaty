; TIP963.LSP: ISOA.LSP   Divide Isometric Ellipse   (c)1994, R. McDowell

(defun  C:ISOA  (/ a1 a2 a3 b1 r1 p1 p2 p3 l1 l2 index x y )
   (setq a1 (getint "Enter Number of Spaces: "))
   (setq l1 (getint "Enter Plane: 1=TOP 2=RIGHT 3=LEFT "))
   (if (= l1 1) (setq l2 (* (* 2.0 pi )  0.0 )))
   (if (= l1 2) (setq l2 (/ (* 2.0 pi )  6.0 )))
   (if (= l1 3) (setq l2 (/ (* 2.0 pi ) -6.0 )))
   (setq r1 (getreal "Enter Radius: "))
   (setq r1 (* r1 1.224733))
   (setq p1 (getpoint "Enter Center of ISO Array"))
   (setq a2 (/ (* 2.0 pi) a1))
   (setq b1 (* (sin (atan (/ 1.0 (sqrt 2.0)))) r1 ))
   (setq index 0)
   (repeat a1
      (setq x (* r1
            (cos
               (+ (/ (* 2.0 pi ) 8.0 )
                  (setq a3 (* a2 index))
               )
            )
      ))
      (setq y
         (sqrt
            (- (expt b1 2.0)
               (/ 
                  (* (expt x 2.0) (expt b1 2.0))
                  (expt r1 2.0)
               )
            )
         )
      )
      (setq index (1+ index))
      (if (> a3 (* 0.75 pi )) (setq y (* y -1)))
      (if (> a3 (* 1.75 pi )) (setq y (* y -1)))
      (setq p2
         (list 
            (- (* x (cos l2)) (* y (sin l2)))
            (+ (* x (sin l2)) (* y (cos l2)))
         )
      )
      (setq p3
         (list
            (+ (car  p1) (car  p2))
            (+ (cadr p1) (cadr p2))
         )
      )
      (command "point" p3)
   )
); end isoa.lsp
