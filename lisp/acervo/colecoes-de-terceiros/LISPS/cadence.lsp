
;;;; These variables are defined globally  
(setq RAD2DEG 57.29578)  ; degrees per radian
(setq LINESET (ssadd))   ; "block create" selection set
(setq COUNTER 0)         ; "block create" name sequencing device

;;;
;;; name: C:DEFBLOCK
;;;
;;; synopsis: Function implemented as an AutoCAD command.  This function
;;;           validates the "tee" geonmetry of two selected lines.  If valid
;;;           creates and inserts a block based on the relationship of the 
;;;           selected lines layer names.
;;;
;;; input: Queries the user for a mainline and a branch line. 
;;;
;;; return value: none - implemented as an AutoCAD commnad.
;;;
(defun C:DEFBLOCK ( / namlist mainline branchline intersection symbol-def)
                      ;inpt wpt1 wpt2 wpt3 wpt4 a1 a2 a3 a4 a5 a6 a7 a8)

  ;; Query uset to select main line
  (setq mainline (getline "\nSelect main leg: "))

  (setvar "cmdecho" 0)
  (setvar "blipmode" 0)
  ;;If a valid line, get the branch line
  (if (/= mainline nil)
    (progn
      (setq branchline (getline "\nSelect branch leg: "))

      ;;If a valid line, continue
      (if (/= branchline nil)
        (progn

          ;;Calculate the intersection of the mainline and the branch line 
          (setq intersection (calc-inters mainline branchline))

          ;;If intersection exists, continue
          (if (/= intersection nil)
            (progn

              ;;Calculate the symbol definition points
              (setq inpt (car intersection))
              (setq wpt1 (polar inpt (cadr intersection) 0.1875))
              (setq wpt2 (polar inpt (+ (cadr intersection) pi) 0.1875))
              (setq wpt3 (polar inpt (caddr intersection) 0.1875))
              (setq wpt4 (polar inpt (+ (caddr intersection) pi) 0.1875))
              (setq a1 (polar wpt1 (+ (caddr intersection) pi) 0.15625))
              (setq a2 (polar a1 (caddr intersection) 0.3125))
              (setq a3 (polar wpt2 (+ (caddr intersection) pi) 0.15625))
              (setq a4 (polar a3 (caddr intersection) 0.3125))
              (setq a5 (polar wpt3 (+ (cadr intersection) pi) 0.15625))
              (setq a6 (polar a5 (cadr intersection) 0.3125))
              (setq a7 (polar wpt4 (+ (cadr intersection) pi) 0.15625))
              (setq a8 (polar a7 (cadr intersection) 0.3125))

              ;;"BREAK" the main and branch lines
              (break-line (car mainline) wpt1 wpt2)
              (break-line (car branchline) wpt3 wpt4)

              ;;Determine which symbol is to be drawn
              (setq COUNTER (1+ COUNTER))
              (setq symbol-def (apply-tee-rules mainline branchline))

              ;;Draw the symbol, make it a block, insert the new block
              (setq namlist (build-block (car symbol-def)))
              (command "BLOCK" (cadr symbol-def) inpt LINESET "")
              (command "INSERT" (cadr symbol-def) inpt "1" "1" "0")
            );progn
            (prompt "\nSelected line segments are invalid.");bad intersection
          );if
        );progn
        (prompt "\nBranch line selection invalid.");bad branch line
      );if
    );progn
    (prompt "\nMain line selection invalid.");bad main line
  );if

  ;;empties the selection set so it can be reused
  (clean-ss namlist)
  (graphscr)
  (prompt "\nCommand")
  ':
);end C:DEFBLOCK

;;;
;;; name: getline
;;;
;;; synopsis: Patterned after the AutoLISP "get" functions. Prompts the user
;;;           to select a line.
;;;
;;; syntax: (getline <prompt>)
;;;         <prompt> - A string to be used as a prompt.
;;;
;;; return value: A list containing the line's database name, the start point 
;;;               of the line, the endpoint of the line, and the line's layer.
;;;
(defun getline (querry / objname lname llist)

  ;;Retrieve selected entity name from database
  (setq objname (entsel querry))

  ;;If no entity selected prompt error message and return nil, Else continue
  (if (= objname nil)
    (progn
      (prompt "\nNo object selected")
      (eval nil)
    );progn
    (progn

      ;;Retrieve selected entity association list from database 
      (setq lname (car objname))
      (redraw lname 3)
      (setq llist (entget lname))

      ;;If entity is a line continue, Else prompt error and return nil.
      (if (= (cdr (assoc '0 llist)) "LINE")
        (progn

          ;;Build the return list.
          (redraw lname 4)
          (list lname 
                (cdr (assoc '10 llist)) 
                (cdr (assoc '11 llist))
                (cdr (assoc '8 llist)) 
          );list
        );progn
        (progn
          (prompt "\nSelected object is not a line.")
          (redraw lname 4)
          (eval nil)
        );progn
      );if
    );progn
  );if
); end getline

;;;
;;; name: calc-inters
;;;
;;; synopsis: Computes the intersection of lines and calculates the direction
;;;           of the lines based on that intersection.
;;;
;;; syntax: (calc-inters <getline-list1> <getline-list2>)
;;;  <getline-list1> - the main line {see getline for order of getline list
;;;  <getline-list2> - the branch line {see getline for order of getline list
;;;
;;; return value: A list consisting of a point (the intersection point), the 
;;;               direction of the mainline from the point of intersection, 
;;;               and the direction of the branch line from the point of 
;;;               intersection.
;;;
(defun calc-inters (mainline branchline / xpt mainhead brchhead)

  ;;Calculate the point of intersection
  (setq xpt (inters (cadr mainline) (caddr mainline) 
                    (cadr branchline) (caddr branchline) nil))

  ;;Calculate direction of the lines.
  (if (< (distance xpt (cadr mainline)) 0.025)
    (setq mainhead (angle xpt (caddr mainline)))
    (setq mainhead (angle xpt (cadr mainline)))
  );if
  (if (< (distance xpt (cadr branchline)) 0.025)
    (setq brchhead (angle xpt (caddr branchline)))
    (setq brchhead (angle xpt (cadr branchline)))
  );if

  ;;Build the return list
  (list xpt mainhead brchhead)
); end calc-inters

;;;
;;; name: break-line
;;;
;;; synopsis: BREAK's a line
;;;
;;; syntax: (break-line <ename> <point1> <point2>)
;;;         <ename> - database name on entity to break
;;;         <point1> - a point list representing a point on the "BREAK".
;;;         <point2> - a point list representing a point on the "BREAK".
;;;
;;; return value: nil
;;;
;;; side effect - Specified line is broken
;;;
(defun break-line (ename pt1 pt2 / editset elist c1 c2 pt1 pt2)

  ;;Calculate the points of a window using the first point
  (setq c1 (list (+ (car pt1) 0.05) (+ (cadr pt1) 0.05)))
  (setq c2 (list (- (car pt1) 0.05) (- (cadr pt1) 0.05)))

  ;;Check to see if there are lines to edit
  (setq editset (ssget "C" c1 c2))

  ;;If lines are not present, try again with the second point
  (if (= editset nil 0)
    (progn
      (setq c1 (list (+ (car pt2) 0.05) (+ (cadr pt2) 0.05)))
      (setq c2 (list (- (car pt2) 0.05) (- (cadr pt2) 0.05)))
      (setq editset (ssget "C" c1 c2))

      ;;If still no lines prompt error and return, Else execute break.
      (if (= editset nil)
        (prompt "\nInvalid edit points. Break not preformed")
        (progn

          ;;Build list like that returned by entsel using the first point
          ;;and select objects with this list
          (setq elist (list ename pt2))
          (command "BREAK" elist pt1)
        );progn
      );if
    );progn
    (progn

      ;;Build list like that returned by entsel using the first point
      ;;and select objects with this list
      (setq elist (list ename pt1))
      (command "BREAK" elist pt2)
    );progn
  );if
);end break-line

;;;
;;; name: apply-tee-rules
;;;
;;; synopsis: Computes the relationship between two intersecting lines and based
;;;           on that relationship creates the correct block.
;;;
;;; syntax: (apply-tee-rules <getline-list1> <getline-list2>)
;;;         <getline-list1> - the mainline
;;;         <getline-list1> - the branchline
;;;
;;; return value: A list consisting of the order of point insertion and the computed
;;;         block name.
;;;
(defun apply-tee-rules (main branch)
  (cond

    ;;If both layer names are equal to "0"
    ( (and (equal (cadddr main) "0") (equal (cadddr branch) "0"))
      (list '(wpt1 wpt3 wpt2 wpt4 wpt1)
             (strcat "TYPE0" (itoa COUNTER)))
    );

    ;;If both layer names are equal to "1"
    ( (and (equal (cadddr main) "1") (equal (cadddr branch) "1"))
      (list '(a1 a2 a6 a5 a4 a3 a7 a8 a1)
             (strcat "TYPE1" (itoa COUNTER)))
    );
    
    ;;Default case
    ( T
      (list '(a1 a2 inpt a6 a5 inpt a4 a3 a1)
             (strcat "TYPET" (itoa COUNTER)))
    );
  );cond
);

;;;
;;; name: build-block
;;;
;;; synopsis: Executes the "LINE" command on the list of points returned by
;;;           apply-tee-rules.
;;;
;;; syntax: (build-block <list>)
;;;         <list> - list of point names
;;;
;;; return value: a list of line entity names drawn
;;;         
;;; side-effect: LINESET the global selection set filled with line entity
;;;              drawn
;;; 
(defun build-block (linlst / apt bpt namlist ename)

  ;;Get first point and trim the list
  (setq apt (car linlst))
  (setq linlst (cdr linlst))

  ;;while the first point of the linlst is not nil, continue
  (while (setq bpt (car linlst))

    ;;The elements of linlst are actually point names and must be evaluated
    ;;first to access thier values.
    (command "LINE" (eval apt) (eval bpt))
    (command "")
    
    ;;Get the entity name of the line segment drawn and add it to the selection
    ;;list and the auxillary name list.
    (setq ename (entlast))
    (ssadd ename LINESET)
    (setq namlist (cons ename namlist) apt bpt linlst (cdr linlst))
  );while

  ;;Return the list of entity names
  (eval 'namlist)
);end build-block

;;;
;;; name: clean-ss
;;;
;;; synopsis: Resets the global "create block" selection set to empty.
;;;
;;; syntax: (clean-ss <list>)
;;;          <list> - list of entity names to be removed from LINESET.
;;;
;;; return value: nil
;;;
;;; side effect: LINESET empty.
;;;
(defun clean-ss (namlist / curname)
  (if (/= namlist nil)
    (progn

      ;;Get first entity name
      (setq curname (car namlist))
      (while (setq namlist (cdr namlist))

        ;;Using entity name from namlist, delete that entity from the selection
        ;;set LINESET
        (ssdel curname LINESET)
        (setq curname (car namlist))
      );while
      (ssdel curname LINESET)
    );progn
  );if
);end clean-ss

(prompt "Sample design envrionment loaded")
':
