;***** DEFTABLE.LSP *****;
;
;++Last Update 05/28/93++
  
;{{/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\}}
;{{{{ Send Comments or Questions (or Donations!) }}}}
;{{{{                                            }}}}
;{{{{ To:                                        }}}}
;{{{{     Owen Wengerd                           }}}}
;{{{{     P.O. Box 84                            }}}}
;{{{{     Fredericksburg, OH  44627              }}}}
;{{{{     (216) 695-5903                         }}}}
;{{{{     71324,3252 (Compu-Serve ID)            }}}}
;{{\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/\/}}


(defun errexit (s)
  (princ "\nError:  ")
  (princ s)
  (restore)
)

(defun deftablex ()
  (setq     errexit   nil
            deftablex nil
            savetable nil
            DEFTABLE  nil
  )
  (setvar "EXPERT" (nth 1 oldvar))
  (setvar "CMDECHO" (car oldvar))
  (setq *error* olderr)
  (princ)
)

(defun savetable (table / fh t3 t1)
  (setq fh (open table "w"))
  (setq table (append rows columns center width height
                      (list textheight upright rowbyrow)
                      contents))
  (setq t3 0)
  (while
    (< t3 (length table))
    (princ (nth t3 table) fh)
    (princ "\n" fh)
    (setq t3 (1+ t3))
  )
  (close fh)
)


(defun DEFTABLE (/ rows columns center width height textheight 
                   upright rowbyrow contents t1 t2 t3 
                   ct ct1 oldvar olderr restore)
  (setq oldvar
    (list
      (getvar "CMDECHO")
      (getvar "EXPERT")
    )
  )
  (setq olderr  *error*
      restore deftablex
      *error* errexit
  )
  (setvar "CMDECHO" 0)
  (setvar "EXPERT" 0)
  (terpri)
  (initget 0)
  (if
    (setq table
      (getfiled "Enter Name Of New Table:"
        (strcat (if *LISPPATH *LISPPATH "") "*")
        "tbl"
        259
      )
    )
    (progn
      (initget 4)
      (setq rows
        (list
          (if (setq t3 (getint "\nHow Many Predefined Rows? <1>:  "))
            t3
            1
          )
          (if (setq t3 (getint "How Many Undefined Rows? <variable>:  "))
            t3
            -1
          )
        )
      )
      (setq height nil)
      (setq t1 1)
      (setq ct (+ (apply '+ rows) (if (= -1 (last rows)) 2 0)))
      (repeat
        ct
        (setq height
          (cons
            (if
              (setq t3
                (getreal
                  (strcat
                    "Enter Height of "
                    (if (and (= -1 (last rows)) (= ct t1))
                      "Undefined Rows"
                      (strcat "Row " (rtos t1 2 0))
                    )
                    (if height
                      (strcat " <" (rtos (car height) 2) ">:  ")
                      " <0.18>:  "
                    )
                  )
                )
              )
              t3
              (if height (car height) 0.18)
            )
            height
          )
        )
        (setq t1 (1+ t1))
      )
      (setq height (reverse height))
      (setq columns
        (list
          (if (setq t3 (getint "\nHow Many Predefined Columns?  <1>: "))
            t3
            1
          )
          (if 
            (setq t3
              (getint "How Many Undefined Columns? <variable>:  ")
            )
            t3
            -1
          )
        )
      )
      (setq width nil)
      (setq t1 1)
      (setq ct (+ (apply '+ columns) (if (= -1 (last columns)) 2 0)))
      (repeat
        ct
        (setq width
          (cons
            (if
              (setq t3
                (getreal
                  (strcat
                    "Enter Width of "
                    (if (and (= -1 (last columns)) (= ct t1))
                      "Undefined Columns"
                      (strcat "Column " (rtos t1 2 0))
                    )
                    (strcat " <" (rtos (if width (car width) 1) 2) ">:  ")
                  )
                )
              )
              t3
              (if width (car width) 1)
            )
            width
          )
        )
        (setq t1 (1+ t1))
      )
      (setq width (reverse width))
      (setq center nil)
      (setq t1 1)
      (repeat
        ct
        (setq center
          (cons
            (if
              (setq t3
                (progn
                  (initget 6 "Left Right Center")
                  (getkword
                    (strcat
                      "Specify Justification for "
                      (if
                        (and (= -1 (last columns)) (= ct t1))
                        "Undefined Columns"
                        (strcat "Column " (rtos t1 2 0))
                      )
                      " (Left/Right/<C>enter):  "
                    )
                  )
                )
              )
              (strcase (substr t3 1 1))
              "C"
            )
            center
          )
        )
        (setq t1 (1+ t1))
      )
      (setq center (reverse center))
      (initget 6)
      (setq textheight
        (if
          (setq t3
            (getreal
              (strcat
                "\nEnter Text Height <"
                (rtos (* 0.5 (car height)) 2)
                ">:  "
              )
            )
          )
          t3
          (* 0.5 (car height))
        )
      )
      (initget 6 "Yes No")
      (setq upright
        (if
          (= 0 (car rows))
          (not nil)
          (/=
            "N"
            (if
              (setq t3 (getkword
      "Will Predefined Rows Be at the Top of the Table? (<Y>/N):  "))
              (strcase (substr t3 1 1))
              "Y"
            )
          )
        )
      )
      (initget 6 "Column Row")
      (setq rowbyrow
        (if
          (or
            (and (/= -1 (last columns)) (= 1 (apply '+ columns)))
            (and (/= -1 (last rows)) (= 1 (apply '+ rows)))
          )
          nil
          (=
            "R"
            (if
              (setq t3 (getkword
      "Enter Variable Cells Row by row or Column by column? (<R>/C):  "))
              (strcase (substr t3 1 1))
              "R"
            )
          )
        )
      )
      (setq contents nil)
      (terpri)
      (setq t1 1)
      (setq ct1 (+ (apply '+ columns) (if (= -1 (last columns)) 2 0)))
      (setq ct (+ (apply '+ rows) (if (= -1 (last rows)) 2 0)))
      (if rowbyrow
        (progn
          (setq t3 ct1)
          (setq ct1 ct)
          (setq ct t3)
        )
      )
      (initget 0)
      (repeat
        ct1
        (princ
          (if
            (and (= ct1 t1) (= -1 (last (if rowbyrow rows columns))))
            (strcat
              "\nUndefined "
              (if rowbyrow "Rows:\n" "Columns:\n")
            )
            (strcat
              (if rowbyrow "\nRow " "\nColumn ")
              (rtos t1 2 0)
              ":\n"
            )
          )
        )
        (setq t2 1)
        (repeat
          ct
          (setq contents
            (cons
              (setq t3
                (getstring ct
                  (strcat
                    "    Enter Contents of "
                    (if
                      (and
                        (= -1 (last (if rowbyrow columns rows)))
                        (= ct t2)
                      )
                      (strcat
                        "Undefined "
                        (if rowbyrow "Columns" "Rows")
                      )
                      (strcat
                        (if rowbyrow "Column " "Row ")
                        (rtos t2 2 0)
                      )
                    )
                    " <variable>: "
                  )
                )
              )
              contents
            )
          )
          (setq t2 (1+ t2))
        )
        (setq t1 (1+ t1))
      )
      (setq contents (reverse contents))
      (savetable table)
    )
  )

  (restore)
)

(DEFTABLE)
