;;; -*-  Mode: LISP -*- (C) Benjamin Olasov 1990
;;;  Linework Economizer v. 3.0

;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; File: ECONO.LSP     Copyright (C) Ben Olasov 1990                       ;;;
;;; Inquiries:                                                              ;;;
;;;                                                                         ;;;
;;;       Ben Olasov     LISPenard Technologies                             ;;;
;;;                      New York, NY                                       ;;;
;;;                                                                         ;;;
;;;                      Voice:    (212) 274-8506     (212) 979-3732        ;;;
;;;                      FAX:      (212) 979-3686     (212) 979-3611        ;;;
;;;                      Arpanet:  olasov@cs.columbia.edu                   ;;;
;;;                      Internet: ben@syska.com                            ;;;
;;;                                                                         ;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

;; Lispenard Technologies provides this program 'as is', without warranty of 
;; any kind, either expressed or implied, including, but not limited to the 
;; implied warranties of merchantability and fitness for a particular purpose. 
;; This program remains the intellectual property of Lispenard Technologies, 
;; and is not to be resold or distributed without the written consent of 
;; Lispenard Technologies. 
;;
;; In no event shall Lispenard Technologies be liable to anyone for special, 
;; collateral, incidental, or consequential damages in connection with or 
;; arising out of purchase or use of these materials.  The entire risk as to
;; the quality and performance of the program is with the user.  Should the 
;; program prove defective, the user assumes the entire cost of all necessary 
;; servicing, repair or correction.
;; 
;; Inquiries regarding conditions of use, and requests for modification of 
;; this code for use in other than the English language, should be directed 
;; to Lispenard Technologies, 33 Lispenard Street, New York, NY  10013.
;; 
;; Lispenard Technologies reserves the right to revise and improve its 
;; products as it sees fit.  Any comments contained in this code describe the
;; state of this product at the time of its publication, and may not reflect
;; the product at all times in the future. 
;;
;; AutoLisp and AutoCad are registered trademarks of AutoDesk, Inc.

(VMON)
(gc)

(princ "\nLoading- please wait...")

(expand 100)

(defun C:ECONO ()
       (start_timer)
       (create_layer_table)
       (if (= explode_plines? "Y")
           (explode_plines))
       (economize_by_layer)
       (if (= compress? "Y")
           (compress_by_layer))
       (restore_layers)
       (if (= compress? "Y")
           (explode_1segment_plines))
       (stop_timer))

(defun start_timer ()
       (setq deleted 0
             c_date (getvar "cdate")
             s_date (getvar "tdusrtimer")
             dwg (getvar "dwgname")
             explode_plines? (strcase (userstr (if explode_plines? explode_plines? "Y")
                                               "Explode polylines before beginning?"))
             compress? (strcase (userstr (if compress? compress? "Y")
                                         "Join touching lines into multi-segment polylines?")))
       (princ (strcat "\nStarting to process drawing " dwg " on " (parse_time c_date))))
(defun stop_timer ()
       (setq e_date (getvar "tdusrtimer")
             t_secs (* 86400.0 (- e_date s_date))
             hrs (fix (/ t_secs 3600.0))
             mns (fix (/ (- t_secs (* hrs 3600.0)) 60.0))
             secs (- t_secs (+ (* hrs 3600.0) (* mns 60.0))))
       (if (null (setq fil (open (strcat dwg ".eco") "a")))
           (progn (princ (strcat "\nCouldn't open " dwg ".eco for writing.
Writing to current directory instead."))
                  (setq fil (open (strcat dwg ".eco") "a"))))
       (princ "\nECONOMIZE active for ")
       (princ (strcat "\nStarted processing drawing " dwg " on " (parse_time c_date)) fil)
       (princ "\nECONOMIZE v. 2.1 active for " fil)
       (if (> hrs 0.0)
           (princ (strcat (itoa hrs) " hour" (if (> hrs 1) "s" "") ", ") fil))
       (if (> mns 0.0)
           (princ (strcat (itoa mns) " minute" (if (> mns 1) "s" "") ", ") fil))
       (princ (strcat (rtos secs 2 3) " seconds.") fil)
       (princ (strcat "\nFile: " dwg ": Deleted a total of " (itoa deleted) " redundant lines.") fil)
       (princ "\n--------" fil)
       (close fil)
       (if (> hrs 0.0)
           (princ (strcat (itoa hrs) " hour" (if (> hrs 1) "s" "") ", ")))
       (if (> mns 0.0)
           (princ (strcat (itoa mns) " minute" (if (> mns 1) "s" "") ", ")))
       (princ (strcat (rtos secs 2 3) " seconds."))
       (princ (strcat "\nFile: " dwg ": Deleted a total of " (itoa deleted) " redundant lines."))
       (princ))

(defun economize_by_layer ()
       (setq c_lay (getvar "clayer"))
;       (setvar "regenmode" 0)
       (setvar "cmdecho" 0)
       (setvar "blipmode" 0)
       (setvar "osmode" 0)
       (foreach lyr (mapcar 'car lyrs)
                (if (and (setq lines (ssget "x" (list (cons 0 "LINE")
                                               (cons 8 lyr)))
                               *lines* lines)
                          (setq lines_l (sslength lines)))
                    (process_lines lyr)))
        (command "layer" "t" "*" "on" "*" "s" c_lay ""))

(defun create_layer_table ()
       (setq c_lay (getvar"clayer")
             lyr_data (tblnext "layer" t)
             lyr_nm (cdr (assoc 2 lyr_data))
             lyr_thawed? (cdr (assoc 70 lyr_data))
             lyr_on? (cdr (assoc 62 lyr_data))
             lyrs (list (list lyr_nm lyr_thawed? lyr_thawed?)))
       (while (setq lyr_data (tblnext "layer"))
              (setq lyr_nm (cdr (assoc 2 lyr_data))
                    lyr_thawed? (cdr (assoc 70 lyr_data))
                    lyr_on? (cdr (assoc 62 lyr_data))
                    lyrs (cons (list lyr_nm lyr_thawed? lyr_on?) lyrs))))

(defun freeze_all_but (layr)
       (command "layer" "t" layr "on" layr "s" layr)   ;; Thaw working layer
       (foreach l (aux_remove layr (mapcar 'car lyrs)) ;; Freeze all others
                (command "f" l))
       (command ""))

; (70 . 64) thawed
; (70 . 65) frozen
; (62 . 7)  on 
; (62 . -7) off 

(defun restore_layers ()
       (command "layer")
       (setq c_lay_data (assoc c_lay lyrs)
             lyr_thawed? (cadr c_lay_data)
             lyr_on? (caddr c_lay_data))
       (if (= lyr_thawed? 65)
           (command "f" c_lay)
           (command "t" c_lay))
       (if (> lyr_on? 0)
           (command "on" c_lay)
           (command "off" c_lay))
       (command "s" c_lay)
       (foreach lr (aux_remove c_lay_data lyrs);; read layer data 
                (setq lyr_nm (car lr)          ;; from layer property table
                      lyr_thawed? (cadr lr)
                      lyr_on? (caddr lr))
                (if (= lyr_thawed? 65)
                    (command "f" lyr_nm)
                    (command "t" lyr_nm))
                (if (> lyr_on? 0)
                    (command "on" lyr_nm)
                    (command "off" lyr_nm)))
       (command ""))

(defun process_lines (layr / incr)
       (freeze_all_but layr)
       (if lines (progn (terpri)
                        (setq incr 0
                              ssl (sslength lines)
                              l_deleted 0)
                        (repeat ssl
                                (setq ln (ssname lines incr))
                                (princ (strcat "\rProcessing line "
                                               (itoa (1+ incr)) " of "
                                               (itoa lines_l)
                                               " on layer " layr))
                                (if (and ln (ssmemb ln *lines*))
                                    (compile ln))
                                (setq incr (1+ incr)))))
       (princ (strcat "\t\tDeleted " (itoa l_deleted) " redundant lines.")))

(defun compile (lin / ld *lin_ss ptlst ext_pts i sl)
       (if lin
           (progn (setq lin* lin
                        ld (get_line_data lin)
                        lin_ss (ssget "c" *p1* *p2*)
                        *lin_ss* (ss2enamlist lin_ss)
                        *lin_ss (filter_non-colinear_segments lin *lin_ss*)
                        ptlst (create_ptlst *lin_ss))
                  (if (and *lin_ss
                           (> (sslength *lin_ss) 1))
                      (progn (setq ext_pts (extreme_pts ptlst)
                                   lin1 (ssname *lin_ss 0)
                                   *lin1 (entget lin1)
                                   lyr (cdr (assoc 8 *lin1)))
                             (if (and *lin_ss 
                                      (setq *ssl (sslength *lin_ss)))   
                                 (progn (setq deleted (+ deleted *ssl)
                                              l_deleted (+ l_deleted *ssl))
                                        (command "erase" *lin_ss "")
                                        (command "layer" "m" lyr "")
                                        (command "line" (car ext_pts)
                                                        (cadr ext_pts) ""))) 
                             T)))))

(defun create_ptlst (ss / i sl l1 *l1 n1 n2 pts)
       (cond ((null ss) nil)
             ((/= (type ss) 'PICKSET) nil)
             ((< (setq sl (sslength ss)) 2) nil)
             (T (setq i 1
                      sl (sslength ss)
                      l1 (ssname ss 0)
                      *l1 (entget l1)
                      n1 (cdr (assoc 10 *l1))
                      n2 (cdr (assoc 11 *l1))
                      pts (list n1 n2))
                (repeat (1- sl)
                        (setq l1 (ssname ss i)
                              *l1 (entget l1)
                              n1 (cdr (assoc 10 *l1))
                              n2 (cdr (assoc 11 *l1)))
                        (if (null (member n1 pts))
                            (setq pts (append pts (list n1))))
                        (if (null (member n2 pts))
                            (setq pts (append pts (list n2))))
                        (setq i (1+ i)))
                pts)))

(defun filter_non-colinear_segments (lin enamlst / l sl)
       (cond ((or (null enamlst)
                  (null lin)) nil)
             (T (foreach l enamlst
                         (if (and l  ;; if line isn't parallel to test line, 
                                  (not (colinear lin l))) ;; delete it from set
                             (ssdel l lin_ss)    ;; of lines to be processed
                             (ssdel l *lines*))) ;; else, assume it will be erased.
       lin_ss)))

(defun extreme_pts (pt_list)
       (cond ((or (null pt_list)
                  (< (length pt_list) 2)) nil) ;; termination condition
             ((= (length pt_list) 2) pt_list)  ;; only 2 pts in list
             (T (setq n1 (car pt_list)         ;; find extreme points
                      n2 (cadr pt_list))
                (cond ((v-orient n1 n2)
                       (setq plst (mapcar 'xy pt_list)
                             rev_p (mapcar 'reverse plst)
                             y_coords (mapcar 'car rev_p)
                             min_y (apply 'min y_coords)
                             max_y (apply 'max y_coords)
                             _n1 (assoc min_y rev_p)
                             _n2 (assoc max_y rev_p)
                             *n1 (reverse _n1)
                             *n2 (reverse _n2)))
                      ((h-orient n1 n2)
                       (setq plst (mapcar 'xy pt_list)
                             x_coords (mapcar 'car plst)
                             min_x (apply 'min x_coords)
                             max_x (apply 'max x_coords)
                             *n1 (assoc min_x plst)
                             *n2 (assoc max_x plst)))
                      ((setq direct (diagonal n1 n2))
                       (setq plst (mapcar 'xy pt_list)
                             rev_p (mapcar 'reverse plst)
                             x_coords (mapcar 'car plst)
                             y_coords (mapcar 'car rev_p)
                             min_x (apply 'min x_coords)
                             max_x (apply 'max x_coords)
                             min_y (apply 'min y_coords)
                             max_y (apply 'max y_coords))
                       (if (= direct 'LLUR) ; if we got this far, DIRECT is non-nil
                           (setq  *n1 (list min_x min_y)
                                  *n2 (list max_x max_y))
                           (setq  *n1 (list max_x min_y)  
                                  *n2 (list min_x max_y)))))
                     (list *n1 *n2))))

(defun get_line_data (line)
       (setq elist (entget line)
             *p1* (cdr (assoc 10 elist))
             *p2* (cdr (assoc 11 elist))
             *ang1* (angle *p1* *p2*)
             h_pi* (/ pi 2.0)))

(defun colinear (lin1 lin2 / line1 line2)
       (if (and lin1 lin2
                (setq line1 (entget lin1))
                (setq line2 (entget lin2))
                (setq l1p1 (cdr (assoc 10 line1)))
                (setq l1p2 (cdr (assoc 11 line1)))
                (setq l2p1 (cdr (assoc 10 line2)))
                (setq l2p2 (cdr (assoc 11 line2)))
                (setq ang1 (rad2deg (angle l1p1 l1p2)))
                (setq ang2a (rad2deg (angle l2p1 l2p2)))
                (setq ang2b (rad2deg (angle l2p2 l2p1))))
           (progn (if (not (equal l1p1 l2p1))
                      (setq ang3 (rad2deg (angle l1p1 l2p1)))
                      (setq ang3 nil))
                  (if (not (equal l1p1 l2p2))
                      (setq ang4 (rad2deg (angle l1p1 l2p2)))
                      (setq ang3 nil))
                  (and (or (= ang1 ang2a) ; pass the test for parallelism
                           (= ang1 ang2b))
                       (or (= ang2a ang3) ; pass the test that one point
                           (= ang2b ang3) ; on the segment is colinear with
                           (= ang2a ang4) ; the test segment
                           (= ang2b ang4))))))

(defun ~= (actual_value test_value tolerance)  ;;fuzzy equality
       (if (and actual_value test_value tolerance)
           (<= (abs (- actual_value test_value)) tolerance)))

(defun DEG2RAD (ang)
       (* pi (/ ang 180.000000)))

(defun RAD2DEG (ang)
       (* ang (/ 360 (* pi 2.000000))))

(defun pos-in-list (item lst)
        (if (null (member item lst))
            nil
            (- (length lst) (length (cdr (member item lst))))))

(defun 2D-TO-3D (pt elev)   ;; Construct 3D point with elev as Z coordinate
       (if pt (append (xy pt) (list elev))
              (append (getpoint "\nFirst point: ") (list elev))))

(defun XY (pt) ;; convert 3D point to 2D
       (list (car pt) (cadr pt)))

;; find closest point in node list "nodes" to point "pt"'
(defun closest (pt nodes)
       (nth
          (1- (pos-in-list
                 (apply 'min (mapcar '(lambda (node) (distance pt node)) nodes))
                             (mapcar '(lambda (node) (distance pt node)) nodes)))
        nodes))

(defun v-orient (p1 p2) ;;are two points in a basically vertical relationship?
        (> (abs (- (cadr p1) (cadr p2))) 
           (abs (- (car p1) (car p2))))) 

(defun vertical (p1 p2)
       (= (car p1) (car p2)))

(defun horizontal (p1 p2)
       (= (cadr p1) (cadr p2)))

(defun h-orient (p1 p2) ;;are two points in a horizontal relationship?
        (< (abs (- (cadr p1) (cadr p2))) 
           (abs (- (car p1) (car p2))))) 

(defun diagonal (p1 p2 / ang1)
       (setq ang1 (rad2deg (angle p1 p2)))
       (cond ((or (= ang1 45.0)
                  (= ang1 225.0)) 'LLUR)  ;; return direction of vector
             ((or (= ang1 135.0)
                  (= ang1 315.0)) 'LRUL)  ;; return direction of vector
             (T nil)))                    ;; else, nil

(defun left-to-right (p1 p2) ;;is vector P1 P2 pointing to right?
          (and (h-orient p1 p2)
               (<= (car p1) (car p2))))

(defun right-to-left (p1 p2)  ;;is vector P1 P2 pointing to left?
          (and (h-orient p1 p2)
               (> (car p1) (car p2))))

(defun top-to-bottom (p1 p2) ;;is vector P1 P2 pointing down?
          (and (v-orient p1 p2)
               (> (cadr p1) (cadr p2))))

(defun bottom-to-top (p1 p2)  ;;is vector P1 P2 pointing up?
          (and (v-orient p1 p2)
               (<= (cadr p1) (cadr p2))))

;; convert a selection set to a list of entity lists
(defun ss2enamlist (ss / entlist ctr)
       (if ss (progn
           (setq ctr 0)
           (repeat (sslength ss)
                   (progn (setq entlist (cons (ssname ss ctr) entlist))
                          (setq ctr (1+ ctr)))))) (if entlist entlist))

;(defun ~= (actual_value test_value tolerance)  ;;fuzzy equality
;       (and (<= actual_value (+ test_value tolerance))
;            (>= actual_value (- test_value tolerance))))

(defun aux_remove (atm lst) 
       (cond ((null lst) nil) 
             ((null (member atm lst)) lst)
             ((equal atm (car lst)) (cdr lst))
             (t (append (reverse (cdr (member atm (reverse lst))))
                        (cdr (member atm lst))))))

(defun parse_time (cdate / date_str year month day hour min secs date)
       (if cdate
           (setq date_str (rtos cdate 2 6)
                 year (substr date_str 3 2)
                 month (substr date_str 5 2) 
                 day (substr date_str 7 2)
                 hour (substr date_str 10 2)
                 min (substr date_str 12 2)
                 secs (substr date_str 14 2)
                 date (strcat month "/" day "/" year "  " hour ":" min ":" secs))))

(defun explode (str / firstchr *str*)  ;; iterative text explosion
      (if (null str) nil
          (repeat (strlen str)
                  (progn
                      (setq *str* (cons (setq firstchr (substr str 1 1)) *str*))
                      (setq str (substr str 2))))) (reverse *str*))

(defun concat (lst / str)
       (if (or (null lst)
               (/= (type lst) 'LIST)) nil
           (apply 'strcat lst)))

;;;  Compresser v. 2.0

(defun explode_plines ()
       (setvar "cmdecho" 0)
       (setq plns (ssget "x" '((0 . "POLYLINE"))))
       (if plns (progn (setq lngth (sslength plns)
                             i 0)
                       (terpri)
                       (repeat lngth
                               (setq pln (ssname plns i))
                               (princ (strcat "\rExploding polyline "
                                      (itoa (1+ i))
                                      " of " (itoa lngth)))
                               (command "explode" pln)
                               (setq i (1+ i)))))
       (princ))

(defun explode_1segment_plines ()
       (setvar "cmdecho" 0)
       (setq plns (ssget "x" '((0 . "POLYLINE"))))
       (if plns (progn (setq lngth (sslength plns)
                             i 0)
                       (terpri)
                       (repeat lngth
                               (setq pln (ssname plns i))
                               (princ (strcat "\rAnalyzing polyline "
                                      (itoa (1+ i))
                                      " of " (itoa lngth)))
                               (setq num_verts (length (collect_vertices pln)))
                               (if (< num_verts 3)
                                   (progn (princ "\rExploding")
                                          (command "explode" pln)))
                               (setq i (1+ i)))))
       (princ))

(defun compress_by_layer ()
       (foreach lyr (mapcar 'car lyrs)
                (if (and (setq lines (ssget "x" (list (cons 0 "LINE")
                                               (cons 8 lyr)))
                               *lines* lines)
                          (setq lines_l (sslength lines)))
                    (compress_lines lyr))))

(defun compress_lines (layr)
       (freeze_all_but layr)
       (princ (strcat "\nCompiling lines on layer " layr "\n"))
       (while (and (setq lines (ssget "x" (list (cons 0 "LINE") (cons 8 layr))))
                   (> (setq ssl (sslength lines)) 0)
                   (setq line1 (ssname lines 0)))
               (princ "\rProcessing ")
               (princ line1)
               (command "pedit" line1 "y" "j" lines "" "x")))

(defun collect_vertices (ent / *ent* pt pts)
       (if (= (cdr (assoc 0 (setq *ent* (entget ent)))) "POLYLINE")
           (progn (setq ent (entnext ent))
                  (while (setq *ent* (entget ent) pt (cdr (assoc 10 *ent*)))
                         (setq pts (cons pt pts)
                               ent (entnext ent))))
           (princ "\ncollect_vertices: not a POLYLINE."))
       (if pts pts))

(defun userstr (dflt prmpt / var) ;;DFLT and PRMPT are strings
       (setq var (getstring (if (and dflt (/= dflt ""))
                                (strcat prmpt " <" dflt ">: ")
                                (strcat prmpt ": "))))
       (cond ((/= var "") var)
             ((and dflt (= var "")) dflt)
             (T (*error* "no default given"))))

(princ "\nC:ECONO loaded - type ECONO to use.")
(princ)

