(DEFUN c:rect ()
   ;
   ; Mike Pillers, P.E.     July 12, 1987
   ; Machine/ Mechanism Design Consultant
   ; MJP Mechanical Engineering & CAD Service
   ; San Jose, CA          (408) 280-7959
   ;
   ; This command will allow the user to draw a simple rectangle
   ; while being able to watch it "rubber band" on the screen.
   ; Since the resulting rectangle is a PLINE, the user will
   ; be able to STRETCH, BREAK, & EXPLODE it.  This is much more
   ; convenient than than using the RECTANG block that comes 
   ; with AutoCAD that can't even be exploded.
   ;
   ;                     AutoCAD version 2.6 is required.
   ;
  (setq p1 (getpoint "\nEnter first point: "))
  (setq p3 (getcorner p1 "\nEnter second point: "))
  (setq x1 (car p1))
  (setq x2 (car p3))
  (setq y1 (cadr p1))
  (setq y2 (cadr p3))
  (setq p2 (list x1 y2))
  (setq p4 (list x2 y1))
  (command "pline" p1 p2 p3 p4 "c")
)
  
  