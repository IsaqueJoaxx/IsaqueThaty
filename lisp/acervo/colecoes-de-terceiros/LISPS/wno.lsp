(defun c:WNO () ;writes wire numbers at wire corners
;(setvar "cmdecho" 0)
(setq wireno (getstring "\nEnter wire number or symbol: "))
(setq tht 0.1)
(prompt "\nEnter text height <0.1>: ")
(setq tht (getreal))
(if (null tht) (setq tht 0.1))
(setq wirelst nil)
(while
  (setq wireint (getpoint "\nSelect wire intersection: "))
  (setq wirelst (cons wireint wirelst))
)
(foreach wireint1 wirelst
  (setq wireint1 (osnap wireint1 "int"))
  (setq wiremp1 (osnap wireint1 "midpoint"))
  (command "erase" wiremp1 "")
  (setq wiremp2 (osnap wireint1 "midpoint"))
  (command "oops")
    (setq wirex (car wireint1))
    (setq wirey (- (cadr wireint1) tht))
    (setq wireint2 (list wirex wirey))
  (if (or
       (and
  (< (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp1) 2 5)))
  (> (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp2) 2 5))))
       (and
  (> (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp1) 2 5)))
  (< (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp2) 2 5))))
      )
    (command "text" "r" (polar wireint1 (* 3 (/ pi 4)) 0.07) 
      tht 0 wireno))
  (if (or
       (and
  (< (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp1) 2 5)))
  (< (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp2) 2 5))))
       (and
  (< (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp2) 2 5)))
  (< (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp1) 2 5))))
      )
    (command "text" (polar wireint1 (/ pi 4) 0.07) tht 0 wireno))
  (if (or
       (and
  (> (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp1) 2 5)))
  (> (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp2) 2 5))))
       (and
  (> (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp1) 2 5)))
  (> (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp2) 2 5))))
      )
    (command "text" "r" (polar wireint2 (* 5 (/ pi 4)) 0.07)
      tht 0 wireno))
  (if (or
       (and
  (< (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp1) 2 5)))
  (> (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp2) 2 5))))
       (and
  (< (read(rtos(car wireint1) 2 5))(read(rtos(car wiremp2) 2 5)))
  (> (read(rtos(cadr wireint1) 2 5))(read(rtos(cadr wiremp1) 2 5))))
      )
    (command "text" (polar wireint2 (- (/ pi 4)) 0.07)
      tht 0 wireno))
  )
)
;
;  C:WNO writes "wire numbers" on an electrical wiring type
;shcematic.  On such a schematic it is often required to label
;wires with wire numbers, either for an aid in reading the 
;drawing or as designators to be attached to each wire during
;the assembly pprocess or both.  When this is the case, often
;the numbers are written at places on the drawing where the
;wire "turns a corner" or makes a rigtht angle bend.
;
;  The function first prompts the user to input the symbol
;to be used for a specific wire.  The symbol can be any 
;combination of numbers and/or letters.  Next input for the
;height of the text to be used.  Then WNO prompts the user to
;input the "wire corners" to be labeled with the specified
;symbol.  Any number of corners may be chosen, the program
;stores them in a list.  Each corner is then analyzed to
;determine which direction in is pointing so the symbol may
;be written in the proper place.  All the corners in the list
;are operated on untill the list is empty and all the corners
;picked are labeled.
;
;  WNO can used with the drawing "zoomed out" so that all
;corners of a particular wire may be labeled at the same time.
;
;  A note about the program: four tests are used to determine
;which way a wire corner faces.  The tests compare the appropriate
;x and y coordinates of the midpoints of each wire in the 
;corner.  Because I had trouble in comparing two real numbers,
;sometimes two different reals would not evaluate as being
;different, I had to convert the reals to strings with "rtos",
;then use "read" to evaluate the strings with < or >. 
