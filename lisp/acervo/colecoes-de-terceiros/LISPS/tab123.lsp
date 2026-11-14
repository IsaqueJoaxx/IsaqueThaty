;;; *========================[ Tab123.LSP ]==============================*
;;;
;;; Tab123 creates a table in AutoCAD from a Lotus 123 file.
;;; Read_WKS.EXE must be on the DOS path, and SHELL must provide
;;; at least 150K of free memory for Read_WKS.EXE to execute.
;;;
;;; by Jerry Workman, CopyRight (c) Mountain Software, 1991,92
;;; version 1.32C
;;; *====================================================================*

;;; Initalize Globals

(If (Not rsf)
  (SetQ rsf 2.0
        cof 1.0))

;;; Our error routine

(defun AtErr(s)
  (If (/= s "Function cancelled")
      (Princ (StrCat "\nError: " s))
  )
  (moder)                             ; Restore modified modes
  (If (= (Type fp) 'FILE) (SetQ fp (Close fp)))
  (setq *error* olderr)               ; Restore old *error* handler
  (princ)
)

;;; swiped this from ADESK

(defun Modes (a)
   (setq MLST '())
   (repeat (length a)
      (setq MLST (append MLST (list (list (car a) (getvar (car a))))))
      (setq a (cdr a)))
)

;;; and this

(defun Moder ()
   (repeat (length MLST)
      (setvar (caar MLST) (cadar MLST))
      (setq MLST (cdr MLST))
   )
)

;;; Draw a grid around the table

(Defun DrawGrid ( / gx gy r c lyr clayer)
  (SetQ lyr(GetString "\nGrid Layer<GRID>: "))
  (If(= lyr "") (SetQ lyr "GRID"))
  (SetQ gx sx chars 0 r 0 c 0 clayer (getvar "CLAYER"))
  (command ".LAYER" "M" "GRID" "")
  ; Calculate horizontal line length
  (princ(strcat "\nDrawing a " (itoa cols) " column by "
    (itoa rows) " row grid..."))
  (ForEach c colwid (SetQ chars (+ chars c)))
  ; Draw Horizontal Lines
  (SetQ x2 (+ sx (* cols (* 2 CharOS)) (* chars CharSiz)))
  (Command ".LINE" (List sx sy) (List x2 sy) "")
  (Command ".ARRAY" "L" "" "R" (1+ rows) 1 (* -1 rc))
  ; Draw Vertical lines
  (SetQ y2 (- sy (* rows rc)))
  (While (<= c cols) (Progn
    (If(> c 0)
      (SetQ cw  (Nth (1- c) colwid)
            gx  (+ gx (* 2 CharOS) (* cw CharSiz)))
    )
    (SetQ pt1 (List gx sy)
          pt2 (List gx y2)
    )
    (Command ".LINE" pt1 pt2 "")
    (SetQ c (1+ c))
  ))
  (command ".LAYER" "S" clayer "")
)

;;; Draw the text entities from ACAD.123

(Defun c:DrawTab ()
  (setq olderr  *error*
        *error* AtErr)
  (Modes '("BLIPMODE" "CMDECHO"))
  (SetVar "BLIPMODE" 0)
  (SetVar "CMDECHO" 0)
  (If (Null (SetQ fp (open "ACAD.123" "r")))
    (Princ "\nError: Can't open file \"ACAD.123\"")
  ;else
    (Progn
      (SetQ line (Read-Line fp))          ; size of table
      (If line (Progn
        (SetQ size (read line))
        (graphscr)
        (prompt "\nTable file \"ACAD.123\" opened...")
        (SetQ pt (getpoint "\nTable Insertion point: "))

        ;*----Prompt for a text height
        (SetQ ts (tblsearch "STYLE" (getvar "TEXTSTYLE"))
              h nil
        )
        (If (= (Cdr (Assoc 40 ts)) 0.0)
          (Progn
            (InitGet 6)
            (SetQ h (GetDist pt (strcat "\nText Height <"
                                        (rtos (getvar "TEXTSIZE"))
                                        ">: "
                                )
                    )
            )
            (If (Null h)
              (SetQ h (getvar "TEXTSIZE")))
            (SetQ hmode nil)
          ) ;else
          (SetQ h (Cdr (Assoc 40 ts))
                hmode 1)
        )
        (SetQ sx  (Car pt)
              sy  (Cadr pt)
              rc  (* rsf h)                       ; row centers
              wf  (Cdr (Assoc 41 ts))             ; character width factor
              CharSiz (* wf h)                    ; character width
              CharOS (* CharSiz cof)              ; character offset
              x   (+ sx CharOS)
              y   (- sy (- rc (/ (* (1- rsf) h) 2)))
              cols (Car size)
              rows (Cadr size)
              colwid (read (Read-Line fp))        ; column width List
              row 1
        )
        (InitGet "Yes No")
        (If (/= "No" (getkword "\nCreate table grid?<Yes>: "))
	  (DrawGrid))
	(princ "\nLoading text entities...")
        (While (<= row rows) (Progn
          (SetQ line (Read-Line fp))
          (SetQ col 0
                tx x)
          (If (> row 1)
            (SetQ y (- y rc)))
          (If line
            (SetQ cells (read line))
          ;else
            (SetQ cells nil)
          )
          (ForEach cell cells
            (If(> col 0)
              (SetQ lcw (Nth (1- col) colwid)
                    tx  (+ tx (* 2 CharOS) (* lcw CharSiz)))
            )
            (SetQ Just (Car cell)
                  cw   (Nth col colwid))
            (Cond ((= Just 1)
                   (SetQ pt (List tx y))
                  )
                  ((= Just 2)
                   (SetQ pt (List (+ tx (/ (* cw CharSiz) 2)) y)
                         j "c")
                  )
                  ((= Just 3)
                   (SetQ pt (List (- (+ tx (* cw CharSiz))(* 0.7 CharSiz)) y)
                         j "r")
                  )
                  ((= Just 4)
                   (SetQ pt (List (+ tx (* cw CharSiz)) y)
                         j "r")
                  )
            )
            (Command ".text")
            (If (> Just 1)
              (Command j))
            (Command pt)
            (If (Not hmode)
              (Command h))
            (Command 0 (Cadr cell))
            (SetQ col (1+ col))
          )
          (SetQ row (1+ row))
        ))
        (Princ "\nTab123 finished...")
      )
        (Princ"\nNo Table Loaded...")
      )
    (SetQ fp (Close fp))
    )
  )
  (Moder)
  (setq *error* olderr)               ; Restore old *error* handler
  (Princ)
)

;;; Execute Read_WKS

(Defun c:LoadTab ()
  (SetQ fp (Close(Open "ACAD.123" "w")))        ;just as good as erasing it
  (Command "shell" "READ_WKS.EXE /A /I")
  (Princ)
)

;;; Get a floating point value

(Defun GtReal( txt dflt / val )
  (SetQ val (GetReal (strcat txt "<" (rtos dflt 2 2) ">:")))
  (If val
    val
    dflt)
)

;;; Prompt / report parameters

(Defun GetParms ( / cmd )
  (Princ(strcat "\nParameters: Row Scale Factor[" (rtos rsf 2 2)
                "] Character Offset Factor[" (rtos cof 2 2) "]"
  ))
  (InitGet "RowScaleFactor CharOffsetFactor Exit")
  (SetQ cmd (getkword "\nRowScaleFactor/CharOffsetFactor/Exit/<Exit>:"))
  (Cond ((= cmd "RowScaleFactor")
        (SetQ rsf (GtReal "\nEnter Row Scale Factor" rsf)))
        ((= cmd "CharOffsetFactor")
        (SetQ cof (GtReal "\nEnter Character Offset Factor" cof)))
  )
  (Princ)
)

;;; The Main program

(Defun c:Tab123 ( / cmd )
  (InitGet "Load Draw All Parms Exit")
  (SetQ cmd (getkword "\nLoad/Draw/All/Parms/Exit/<All>:"))
  (If (Not cmd)
    (SetQ cmd "All"))
  (Cond ((= cmd "All")
          (c:LoadTab)
          (c:DrawTab)
        )
        ((= cmd "Load")
          (c:LoadTab)
        )
        ((= cmd "Draw")
          (c:DrawTab)
        )
        ((= cmd "Parms")
          (GetParms)
        )
  )
  (Princ)
)

(Princ "\nTab123 version 1.32C loaded\nEnter TAB123 to execute.")
(Princ)

