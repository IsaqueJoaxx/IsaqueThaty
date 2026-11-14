; UNDER.LSP

; Last Changed 4/5/89

; Written by: Bob Thomas
;             AutoCAD Consulting - AutoLISP Programming
;             Thomas Enterprises
;             1030 "B" Street, Suite 201
;             San Rafael, CA  94901
;             415/453-8712
;
; This short AutoLISP routine creates a function called UND, which performs
; like an object-snap override; it selects objects covered by other objects 
; with a single point pick.
;
; For example, if you wanted to erase a line segment that is hidden 
; underneath a polyline, you could pick the line segment with the UND 
; function, as shown in the following example dialog:

; -------------------------------------------------

; Command: ERASE
; Select objects: (UND) UNDERNEATH Pick object: 
; (pick a point where the line segment is -- the line segment underneath 
; is highlighted, the polyline above is ignored)
; 1 found.
; Select objects: (continue normally)
;
; (This works with objects behind other objects in 3-D views, as well.)
; 
; NOTE: As written, if several objects are underneath the top object, 
; all the objects underneath the top object will be selected by UND.
;
; This function may be freely distributed, but please leave this header
; attached so people know what this function does, and I get a plug in
; for my consulting services.
; 
(defun und( / x ss1)
    (if (not (setq x (cadr (entsel "UNDERNEATH Pick object: "))))
        (progn (prompt "\nNo object found.") (princ))
        (progn
          (setq ss1 (ssdel (ssname (ssget x) 0) 
                           (ssget "C" (polar x 3.92699 0.15)
                                      (polar x 0.785398 0.15)
                           )
                    )
          )
          (if (= (sslength ss1) 0)
              (progn (prompt "\nNothing underneath.") (princ))
              (eval ss1)
          )
        )
    )
)
; EOF  UNDER.LSP    Bob Thomas  50 lines
