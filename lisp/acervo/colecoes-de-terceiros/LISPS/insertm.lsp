;======================================================
; INSERTM.LSP Copyright 1992 by Looking Glass Microproducts
;======================================================
; Insert Mulltiple Files.
(defun C:INSERTM (/ BLOCKNAME DIR DIRNAME ERROR GETCORN GETXYZ
                  GETY GETZ GET_DWGSPEC GET_EXPLODED GET_NONEXPLODED
                  GET_PARAMS INSERTM INSERT_FILE NOTRANS OLD-ERROR
                  POPVARS PUSHVARS RTOD SYSVARS XGETANGLE XGETREAL
                 )
   ;======================================================
   ; Error Handler
   (defun ERROR (S)
      (if (not
             (member
                S
                '("Function cancelled" "console break")
             )
          )
         (princ S)
      )
      (command "_undo" "end")
      (command "_undo" "1")
      (if FHAND
         (progn (close FHAND) (setq FHAND nil))
      )
      (POPVARS)
   )
   ;======================================================
   ; Set and Save System Variables
   (defun PUSHVARS (VLIST)
      (foreach PAIR VLIST
         (setq
            SYSVARS (cons
                       (cons
                          (strcase (car PAIR))
                          (getvar
                             (car PAIR)
                          )
                       )
                       SYSVARS
                    )
         )
         (if (cdr PAIR) (setvar (car PAIR) (cdr PAIR)))
      )
      t
   )
   ;======================================================
   ; Restore System Variables
   (defun POPVARS ()
      (foreach PAIR SYSVARS (setvar (car PAIR) (cdr PAIR)))
      (setq
         *error* OLD-ERROR
      )
      (princ)
   )
   ;======================================================
   ; Disallow transparent invocation of routine.
   (defun NOTRANS ()
      (cond
         ((zerop (logand (getvar "cmdactive") (+ 1 2 4 8))))
         ((alert
             "This command may not be invoked transparently."
          )
         )
      )
   )
   ;======================================================
   ; Get File specification
   (defun GET_DWGSPEC (/ FILESPEC)
      (setq
         FILESPEC (strcase
                     (getstring
                        "\n Drawing specification: "
                     )
                  )
      )
      (cond
         ((= "" FILESPEC) nil)
         ((wcmatch FILESPEC "~*.DWG")
            (strcat FILESPEC ".DWG")
         )
         (FILESPEC)
      )
   )
   ;======================================================
   ; Get extract directory name from pathname
   (defun DIRNAME (PATHNAME / I J)
      (setq I 1)
      (repeat
         (strlen PATHNAME)
         (if (member (substr PATHNAME I 1) '("/" "\\" ":"))
            (setq
               J I
            )
         )
         (setq I (1+ I))
      )
      (if J (substr PATHNAME 1 J) "")
   )
   ;======================================================
   ; Get list of files matching filespec
   (defun DIR (FILESPEC / CMD FNAME FHAND LINE FLIST)
      (setq
         PREFIX (DIRNAME FILESPEC)
         FNAME  (strcat (getvar "tempprefix") "$TEMP$.AC$")
      )
      (setq
         CMD (strcat
                "dir "
                FILESPEC
                " /-p /-w /-a /o-n /b /-l >"
                FNAME
             )
      )
      (command "shell" CMD)
      (setq FHAND (open FNAME "r"))
      (if (not FHAND)
         (alert
            (strcat FNAME "\nCan't read file.")
         )
         (progn
            (while (setq LINE (read-line FHAND))
               (setq
                  FLIST (cons
                           (strcat
                              PREFIX
                              (substr
                                 LINE
                                 1
                                 (- (strlen LINE) 4)
                              )
                           )
                           FLIST
                        )
               )
            )
            (close FHAND)
            FLIST
         )
      )
   )
   ;======================================================
   ; Extract blockname from pathname
   (defun BLOCKNAME (PATHNAME / I J)
      (setq I 1)
      (repeat
         (strlen PATHNAME)
         (if (member (substr PATHNAME I 1) '("/" "\\" ":"))
            (setq
               J I
            )
         )
         (setq I (1+ I))
      )
      (if J (substr PATHNAME (1+ J)) PATHNAME)
   )

   ;======================================================
   ; Insert filename
   (defun INSERT_FILE (FILENAME / INS_NAME BLK_NAME REDEFINE)
      (if (car PARAMS)
         (setq INS_NAME (strcat "*" FILENAME))
         (progn
            (setq BLK_NAME (BLOCKNAME FILENAME))
            (if (setq REDEFINE (tblsearch "block" BLK_NAME))
               (setq
                  INS_NAME     (strcat
                                  (BLOCKNAME FILENAME)
                                  "="
                                  FILENAME
                               )
                  REGENPENDING t
               )
               (setq INS_NAME FILENAME)
            )
         )
      )
      (prompt (strcat "\n Inserting " FILENAME "... "))
      (command
         "_insert" INS_NAME
      )
      (apply 'command (cdr PARAMS))
      (if REDEFINE
         (prompt
            (strcat "Block " BLK_NAME " redefined.")
         )
      )
   )
   ;======================================================
   ; Radians to degrees
   (defun RTOD (X) (/ (* 180.0 X) pi))
   ;======================================================
   ; Get real with default
   (defun XGETREAL (PRMPT DEFAULT)
      (cond ((getreal PRMPT)) (DEFAULT))
   )
   ;======================================================
   ; Get angle with default
   (defun XGETANGLE (PRMPT BASE DEFAULT)
      (cond ((getangle BASE PRMPT)) (DEFAULT))
   )
   ;======================================================
   ; Get parameters for exploded blocks
   (defun GET_EXPLODED ()
      (initget 6) ; disallow zero, negative
      (setq
         SCALE (XGETREAL "\n Scale factor <1>: " 1)
         ANG   (XGETANGLE INSPNT "\n Rotation angle <0>: " 0)
      )
   )
   ;======================================================
   ; Get corner for xy
   (defun GETCORN (/ AGAIN CORNER)
      (setq AGAIN t)
      (while AGAIN
         (initget 1)
         (setq
            CORNER (getcorner INSPNT "\nOther corner: ")
            XSCALE (- (car CORNER) (car INSPNT))
            YSCALE (- (cadr CORNER) (cadr INSPNT))
         )
         (if (or (zerop XSCALE) (zerop YSCALE))
            (prompt
               "\nValue must be nonzero."
            )
            (setq AGAIN nil)
         )
      )
   )
   ;======================================================
   ; Get Y scale
   (defun GETY ()
      (initget 2) ; disallow zero
      (setq
         YSCALE (XGETREAL
                   "\n Y scale factor (default=X): "
                   XSCALE
                )
      )
   )
   ;======================================================
   ; Get Z scale
   (defun GETZ ()
      (initget 2) ; disallow zero
      (setq
         ZSCALE (abs
                   (XGETREAL
                      "\n Z scale factor (default=X): "
                      XSCALE
                   )
                )
      )
   )
   ;=====================================================
   ; Get X, Y, and Z scales
   (defun GETXYZ ()
      (initget 2 "Corner") ; disallow zero
      (setq
         XSCALE (XGETREAL
                   "\n X scale factor <1> / Corner: "
                   1
                )
      )
      (if (= XSCALE "Corner") (GETCORN) (GETY))
      (GETZ)
   )
   ;======================================================
   (defun GET_NONEXPLODED ()
      (initget 2 "Corner Xyz") ; disallow zero
      (setq
         XSCALE (XGETREAL
                   "\n X scale factor <1> / Corner / XYZ: "
                   1
                )
      )
      (cond
         ((= XSCALE "Corner")
            (GETCORN)
            (setq ZSCALE (abs XSCALE))
         )
         ((= XSCALE "Xyz") (GETXYZ))
         (t
            (GETY)
            (setq ZSCALE (abs XSCALE))
         )
      )
      (setq
         ANG (XGETANGLE INSPNT "\n Rotation angle <0>: " 0)
      )
   )
   ;======================================================
   ; Get Insertion Parameters
   (defun GET_PARAMS (/ EXPLODE INSPNT SCALE ANG)
      (initget "Yes No")
      (setq
         EXPLODE (=
                    "Yes"
                    (getkword
                       "\n Explode drawings? <No> "
                    )
                 )
      )
      (initget 1) ; disallow nil
      (setq INSPNT (getpoint "\n Insertion point: "))
      (if EXPLODE
         (progn
            (GET_EXPLODED)
            (list
               EXPLODE
               INSPNT
               SCALE
               (RTOD ANG)
            )
         )
         (progn
            (GET_NONEXPLODED)
            (list
               EXPLODE
               INSPNT
               "XYZ"
               XSCALE
               YSCALE
               ZSCALE
               (RTOD ANG)
            )
         )
      )
   )
   ;======================================================
   ; Search main routine
   (defun INSERTM (/ FILESPEC FILELIST FILENAME PARAMS REGENPENDING
   )
      (cond
         ((not (setq FILESPEC (GET_DWGSPEC))))
         ((not (setq FILELIST (DIR FILESPEC)))
            (alert
               (strcat FILESPEC "\nFile not found.")
            )
         )
         (t
            (setq PARAMS (GET_PARAMS))
            (foreach FILENAME FILELIST
               (INSERT_FILE FILENAME)
            )
            (if REGENPENDING
               (progn
                  (prompt "\n Regenerating drawing.")
                  (command
                     "_regenall"
                  )
               )
            )
         )
      )
   )

   ;======================================================
   ; Body of INSERTM Command 
   (if (NOTRANS)
      (progn
         (setq OLD-ERROR *error* *error* ERROR)
         (PUSHVARS
            '(("cmdecho" . 0)
               ("blipmode" . 0)
               ("osmode" . 0)
               ("attdia" . 1)
               ("regenmode" . 0)
            )
         )
         (command "_undo" "group")
         (INSERTM)
         (command "_undo" "end")
         (POPVARS)
      )
      (princ)
   )
)
(princ
   "  INSERTM.LSP (Copyright 1992 by Looking Glass Microproducts) loaded."
)
(princ)

