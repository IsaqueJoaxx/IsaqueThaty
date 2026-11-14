; SeCAD Tool Box by Omar Melo        4:10:01  8/13/1986
; 4707 SW 72 AVE
; Miami, Fl  33155
;
;
;
; Please note the short coming of the following program
; It need a calibration routine to set user (more programming & thinking)
; It is not useful to those who use slow machine or work on drawing
;   that a redraw time on 1 minute or more...
; This can be overcome by a function that merges a slide
;   over drawing file. It would also be nice if it exclusive-or
;   on top of existing graphic so it can be cleared off graphic page
;   without a redraw and nominal visual damage to user's drawing
;
;
;
(command "vslide" "icon")
(setq pt (getpoint "\nSelect Icon: ")
      a  (getvar "viewctr")
      vy (getvar "viewsize")
      c  (getvar "screensize") ;use to get ratio to calculate view width
      x  (car c)
      y  (last c)
      vx (* x (/ vy y) 0.905)  ; * 0.905 use to calibrate i my have to check aspect
                               ; ratio on my display.. ye old fudge factor
      b  (list (- (car a) (/ vx 2.)) (- (last a) (/ vy 2.)))
      ix 10
      iy 10
      dx (- (car pt) (car b))
      dy (- (last pt) (last b))
      rx (fix (/ dx (/ vx ix)))
      ry (fix (/ dy (/ vy iy)))
      c  (+ rx (* ry iy))
)
; now to implement command associated with icon
(command "redraw" "script" (strcat "\\scr\\icon" (itoa c)))
; you might instead of calling a script call a lisp file
;(command "redraw")
;(load (strcat "\scr\icon" (itoa c)))
;....how ever there is no way that I know of telling AutoLISP
;....to execute a screen menu line. For exaple: (menucmd "s=icons")
;....                                           (menucmddo  c)
;....   where c contains menu row to execute.
22290D0A202020202020612020281C
:1802B80067657476617220