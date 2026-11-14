; VERTEX.LSP
; 1/12/87
; by Tony Tanzillo
;
; These are miscellaneous routines that make use
; of the AutoLISP (inters) function, and can be used
; in a manner similar to AutoCAD's Object snap mechanism.
;
; (vertex)  - Returns the intersection of 2 line entities.
;             Can be used as an OPTION KEYWORD at any AutoCAD
;             point prompt. It is a kind of "virtual" intersection
;             OBJECT SNAP (meaning that the lines do not have to
;             physically intersect)
;
;     Usage:  Assuming you have several non-parallel lines on the
;             display already, enter the LINE command, and pick
;             a starting point, draw a few segments, and then at
;             any "To point: " prompt, enter "(VERTEX) <CR>"
;             (w/o the quotes) and select 2 lines. The endpoint
;             of the current line segment you are drawing should
;             Anchor itself to the vertex (virtual intersection)
;             of the 2 lines you selected. There are other uses,
;             which is limited only by ones imagination.
;
;  (extend) - This is another OPTION KEYWORD that can be entered
;             at any point prompt. If used with the line command
;             It will let you draw a line by "EXENDING IT" to
;             another line (just like AutoCAD's EXTEND command
;             would extend an existing line). The line that you
;             are extending to is treated as though it were of
;             infinite length, And the resulting line will maintain
;             the specified angle regardless of length.
;
;     Usage - Assuming there are some lines on the display, Enter
;             the LINE command and specify a starting point, draw
;             a few segments, and then enter (EXTEND) at any
;             "To point: " prompt. Next, enter or pick the angle
;             that the next segment is to be drawn at, and then
;             select a line that the segment is to be EXTENDED to.
;             Again, don't assume this function is only useful with
;             the LINE command which I only site here as an example.
;             Since this these functions return a point, they can be
;             used at ANY AutoCAD POINT PROMPT, and thier uses are
;             limited only by ones imagination.
;
;
;    Tony Tanzillo, A/E Automation Systems 70307,2556

(defun pfetch(e p)
  (cond
    ((atom p)
       (cdr
         (assoc p
           (entget (cond ((eq (type e) 'ENAME) e) ((car e))))
         )
       )
    )
    ((mapcar '(lambda(x) (pfetch e x )) p))
  )
)


(defun orr(l) (eval (cons 'cond (mapcar 'list l))))


(defun ends(e)
  (list
    (pfetch e 10)
    (pfetch e 11)
  )
)

(defun land(l) (if (not (member nil l)) (last l)))

(defun vertex( / a)
  (inters
    (car (setq a (ends (entsel "Vertex of line: "))))
    (cadr a)
    (car (setq a (ends (entsel "And line: "))))
    (cadr a) nil))

(defun extend( / a b c d e)
  (setq a (getvar "lastpoint"))
  (setq b (getpoint a "Angle: "))
  (setq c (entsel "\nLine to extend to, or ENTER for 2 points: "))
  (setq e
    (if c
      (ends c)
      (progn
        (list
          (setq d (getpoint "\nFrom point: "))
          (getpoint d "To point: ")
        )
      )
    )
  )
  (if (and a b (car e) (cdr e))
    (apply 'inters
      (list a b (car e) (cadr e) nil)
    )
  )
)
)
;
;   There may be