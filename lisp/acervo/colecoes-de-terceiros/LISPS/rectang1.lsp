;  RECTANG1.LSP program to draw a rectangle with lower left and
;  upper right points.
;       (c)1994, Anthony Hotchkiss and CADalyst
;
(defun c:rectang1  ()
   (setq  pt1  (getpoint  "\nLower left point:")
         pt3  (getpoint  "\nUpper right point:")
             p2x  (car  pt3)
             p2y  (cadr  pt1)
             pt2  (list  p2x  p2y)
             pt4  (list  (car  pt1)  (cadr  pt3))
   ) ;  end of point assignments
   (command  "pline"  pt1  pt2  pt3  pt4  "c")
);  end of defun rectang1

