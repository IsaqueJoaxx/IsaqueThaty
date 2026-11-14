;================================Table.lsp===================================
;Description of variables use
;Globals: _space;real,spacing between rows
;Locals;  rows;integer, rows in table beyond heading section
;         space:real,prompted spacing between rows
;         high:real,height of the table
;         pt1-pt4:point,points at the four corners of the table
;         omode:integer,stores the current value of ORTHO
;         pp1,pp2:point,points along side and bottom of table
;         p:point, temporary point for constructing lines
;         d1:real,width of the table
;         title:string,table's title value
;============================================================================
(defun C:TABLE
        (/ rows space high pt1 pt2 pt3 pt4 omode p pp1 pp2 d1 title)
;Turn off command line echo.
        (setvar "cmdecho" 0)
;Prompt for number of rows in the table        
        (initget 7)
        (setq rows (getint"\nHow many ROWS(--) in table: "))
;Prompt for the spacing between the rows. Allow for default.        
        (if (not_space)(setq_space 0.5))
        (initget 6)
        (setq space (getreal (strcat
                "\nSpacing between rows<"(rtos _space) ">:")))
        (if (not space)(setq _space 0.5)(setq _space space))
;Calculate the height of the table.
        (setq high (+ (* rows _space) 0.625))
;Prompt for 1st and 2nd points.  These determine origin and width of table.
        (initget 1)
        (setq pt1 (getpoint "\nEnter upper-left corner: "))
        (setq omode (getvar "orthomode"))
        (setvar "orthomode" 1)
        (initget 1)
        (setq pt4 (getpoint pt1 "\nEnter upper-right corner: "))
        (setvar "orthomode" omode)
;Calculate the other points.
        (setq pt2 (polar pt1 (* 1.5 pi) high)
            pt3 (polar pt4 (* 1.5 pi) high)
            pp1 (polar pt4 (* 1.5 pi) 0.25)
            pp2 (polar pt2 0.0 0.125))
;Draw the lines for table outline and rows.
        (setvar "blipmode" 0)
        (command "pline" pt1 "w" 0.02 0.02 pt2 pt3 pt4 "c"
           "pline" (polar pt1 (* 1.5 pi) 0.375)(osnap pp1 "perpend") ""
           "pline" (polar pt1 (* 1.5 pi) 0.625)(osnap pp1 "perpend") ""
        )
        (setq p (polar pt1 (* 1.5)(+ 0.625 _space)))
        (repeat (1-rows)
           (command "line" p (osnap pp1 "perpend")"")
           (setq p (polar p (* 1.5 pi _space)))
;Enter points to form column lines.           
        (setq p (getpoint "\nEnter COLUMN upper, start points: "))
        (while p
                (command "pline" p (osnap pp2 "PERPEND")"")
                (setq p (getpoint "\n\tNext column <RETURN to quit>: "))
        )
;Prompt for table's title and insert text.
        (setq title (getstring t "\nEnter table's TITLE: "))
        (if (/=title"")(progn
                (setq d1 (distance pt1 pt4))
                (command "text" "m" (polar (polar pt1 0 (/ d1 2))
                        (* 1.5 pi) 0.1875) 0.175 0 title)))
        (setvar "blipmode" 1)
(princ))
