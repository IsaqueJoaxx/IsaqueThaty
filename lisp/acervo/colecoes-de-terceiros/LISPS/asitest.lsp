;;;---------------------------------------------------------------------------
;;;
;;;   asitest.lsp
;;;   Copyright (C) 1991-1992 by Autodesk, Inc.
;;;      
;;;   Permission to use, copy, modify, and distribute this software 
;;;   for any purpose and without fee is hereby granted, provided 
;;;   that the above copyright notice appears in all copies and that 
;;;   both that copyright notice and this permission notice appear in 
;;;   all supporting documentation.
;;;
;;;   THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED
;;;   WARRANTY.  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR
;;;   PURPOSE AND OF MERCHANTABILITY ARE HEREBY DISCLAIMED.
;;;
;;;   by Frumkin A.
;;;   April 20 1992
;;;
;;;--------------------------------------------------------------------------
;;;  DESCRIPTION
;;;
;;;  Test ASI. Allows customers to enter SQL statements and edit database
;;;  tables.
;;;
;;;----------------------------------------------------------------------------

;;;----------------------------------------------------------------------------
;;; Defined c: so that it can be used at the Command Line..
;;;----------------------------------------------------------------------------
  (defun c:sqldrv()
    (sqldrv)
  )
  (defun c:sqlcnc()
    (sqlcnc)
  )
  (defun c:sqlterm()
    (sqlterm)
  )
  (defun c:sql()
    (sql)
  )
  (defun c:testbind()
    (testbind)
  )
  (defun c:sqlfile()
    (sqlfile)
  )
  (defun c:sqldis()
    (sqldis)
  )
  
  ;;
  ;; Compilation of SQL statement.
  ;;
  (defun compile (hcon stm /
                  hcom
                  )
    (if (setq hcom (asi_ohdl hcon))
       (progn
        (if (and (asi_com hcom stm) (asi_exe hcom))
                        (if (eq (asi_stm hcom) "ASI_CURSOR") 
                        (scan hcom)
                (princ "\nOK\n")
                        )
        )
            (asi_chdl hcom)
       )
    )
  )

  ;;
  ;; Fetching table.
  ;;
  (defun scan (hcom / flag com prev prompt)
     (setq prev "Exit")
     (while (not flag)
        (print_row hcom)
        (setq prompt
                (strcat "\nFirst/Last/Next/Previous/Delete/Update/Show/Exit/<"
                        prev ">: "))
        (initget 0 "First Last Next Previous Delete Update Show Exit")
        (setq com (getkword prompt))
        (if (= com nil)(setq com prev))
        (cond 
                ((eq com "First")
              (progn 
                 (princ "\nTop")
                 (asi_ftr hcom)
              )
          )
                ((eq com "Last")  
              (progn 
                 (princ "\nBottom")
                 (asi_fbr hcom)
              )
          )
                ((eq com "Next")        (asi_fet hcom))
                ((eq com "Previous")   (asi_fbk hcom)) 
                ((eq com "Delete") 
                        (if (asi_del hcom)      (princ "\nCurrent line deleted"))
                )
                ((eq com "Update")              (update_row hcom))
                ((eq com "Show")                (print_set hcom))
                ((eq com "Exit")                   (setq flag T))
        )
        (if (not (= com nil)) (setq prev com))
     )
  )

  ;;
  ;; Prints row from table
  ;;
  (defun print_row (hcom)
     (print_header hcom)
     (if (= (fix (asi_currow hcom)) -2)
        (princ "\nEOS")
            (if (= (fix (asi_currow hcom)) -1) 
               (princ "\nTOS")
           (print_data hcom)
            )
     )  
  )
  
  ;;
  ;; Print data from table.
  ;;
  (defun print_set (hcom / rows flag)
     (print_header hcom)
     (setq rows 0)
     (asi_ftr hcom)
     (if (= (fix (asi_currow hcom)) -2)
        (princ "\nEOS")
        (if (= (fix (asi_currow hcom)) -1) 
           (princ "\nTOS")
                (while (not flag)
                        (print_data hcom)
                        (setq rows (1+ rows))
              (if (null (asi_fet hcom)) (setq flag T))
                )
        )
     )  
     (asi_ftr hcom)
     (princ (strcat "\n" (itoa rows) " rows selected"))
     (getstring "\nPress RETURN...")
  )
  
  ;;
  ;; Print column names.
  ;; 
  (defun print_header (hcom / str jj lst len l)
     (setq str "\n    |" jj  0)
     (while (setq lst (asi_cds hcom jj))
         (setq jj (1+ jj))
         (setq len (strlen (nth 0 lst)))
         (if (< len (nth 1 lst)) (setq l (nth 1 lst)) (setq l len))
           (setq str (strcat str (addlist (nth 0 lst) l) " | "))
     )
     (princ str)
     (princ "\n    |--------------------")
  )
  
  ;;
  ;; Prints table.
  ;;
  (defun print_data (hcom / l lst len val jj tp str)
      (setq str (strcat "\n" (addlist (itoa (+ 1 (fix (asi_currow hcom)))) 4) "|")
            jj 0)
      (while (setq val (asi_cvl hcom jj))
          (setq lst (asi_cds hcom jj)
                tp (type val)
                len (strlen (nth 0 lst))
          )
          (if (< len (nth 1 lst)) (setq l (nth 1 lst)) (setq l len))
        (cond 
                    ((= tp 'INT)        
                            (setq str 
                                    (strcat str (addlist (itoa val) l) " | "))
                    )
                    ((= tp 'REAL) 
                            (setq str 
                                    (strcat str (addlist (rtos val 2 (nth 2 lst)) l) " | "))
                    )
                    (T (setq str (strcat str (addlist val l) " | ")))
            )
            (setq jj (1+ jj))
      )
      (princ str)
      (terpri)
  )
  
  ;; 
  ;; Adds space for sting while it length leth then defined.
  ;;
  (defun addlist (str len / l)
     (setq l (strlen str)) 
     (while (< l len)
        (setq l (1+ l) str (strcat str " "))
     )
     (setq str str)
  )
  
  ;;
  ;; Updates current row.
  ;;
  (defun update_row (hcom / ii flag cds prompt val newval tp)
     (if (>= (fix (asi_currow hcom)) 0 )
        (progn
           (princ "\n -------Update current row --------------\n")
         (setq ii 0 flag T)
         (while (and flag (setq cds (asi_cds hcom ii)))
                 (setq val (asi_cvl hcom ii) 
                      prompt (strcat "\n" (nth 0 cds) "<")
                          tp (type val)
                 )
                 (cond 
                    ((= tp 'INT)        
                            (setq prompt (strcat prompt (itoa val) ">: "))
                    )
                    ((= tp 'REAL) 
                            (setq prompt (strcat prompt (rtos val 2 (nth 2 cds)) ">: "))
                    )
                    (T 
                            (setq prompt (strcat prompt val ">: "))
                         )
                 )
           (setq newval (getstring prompt))
                (if (not (= newval ""))
                  (if (= newval "NULL")  
                     (setq flag (asi_upd hcom (nth 0 cds) ""))
                     (setq flag (asi_upd hcom (nth 0 cds) newval))
                  )) 
             (if (not flag) (princ "  error") (setq ii (1+ ii)))
         )
        )
     )
  )
  
  ;;
  ;; Reads SQL statements from file and exequtes them.
  ;;
  (defun readFile (hcon fname / pt hcom)
     (if (setq hcom (asi_ohdl hcon))
        (if (setq pt (open fname "r"))
           (while (setq str (strRead pt))
              (if (not (= str ""))
                  (if (asi_cex hcom str)
                     (progn
                         (princ "\nOK\n")       
                         (if (eq (asi_stm hcom) "ASI_CURSOR") (scan hcom))
                     ) 
                       (progn
                        (princ "\nError")
                          (asi_errmsg hcom)
                       )
                  )
              )
           )
        )
        (asi_chdl hcom)
     )
  )

  ;;
  ;; Reads one SQL statement from the file.
  ;;   
  (defun strRead (pt / flag flag1 str workstr l i j)
     (setq str "" flag T)
     (while flag
        (setq workstr (read-line pt)  flag1 T) 
        (if (not workstr)
           (setq flag nil str nil)
           (progn
              (if (= "$" (substr workstr 1 1))
                  (princ (strcat "\nComment: " (substr workstr 2)))
                  (progn
                     (setq l (strlen workstr) i 1)
                     (while (and (<= i l) (= (substr workstr i 1) " "))
                          (setq i (1+ i))
                     ) 
                     (setq j i)
                     (while (and flag1 (<= j l))
                         (if (= "&" (substr workstr j 1)) 
                             (setq flag1 nil)
                             (setq j (1+ j))
                         )
                     )
                     (if flag1
                         (setq str (strcat str (substr workstr i))
                               flag nil
                         ) 
                         (setq str (strcat str (substr workstr i (- j i))))
                     )
                  )     
              )
           )                  
        )
     )
     (terpri)
     (if str (princ str))
     (setq str str)
  )
  
  ;;
  ;; Error handle.
  ;;
  (defun my_err (s)                  ; If an error (such as CTRL-C) occurs
                                     ; while this command is active...
  
          (if (/= (substr s 1 4) QUIT)
             (princ s)
          )
          (setq *error* older)      ; restore old *error* handler
          (prin1)
  )
  
  (defun sqlcnc ()
      (if hdrv
          (progn
              (setq olderr *error* *error* my_err)  
              (if hcon (asi_lof hcon))
              (setq basename (getstring "\nDatabase name ->"))
              (setq username (getstring "\nUser name ->"))
              (setq password (getstring "\nPassword ->"))
              (if (setq hcon (asi_lon hdrv basename username password))
                  (princ "OK")
                  (princ (strcat "\nCannot connect to database " basename))
              )
              (setq *error* older)      ; restore old *error* handler
          )
          (progn
                    (princ "No active drivers detected.")
              (setq hcon nil)
          )
      )
      (prin1)
  )
  
  (defun sqldis ()
        (if hcon 
            (if (asi_lof hcon) (setq hcon nil))
                (princ "No active data base detected.")
        )
      (prin1)
  )
  
  (defun sql ( / statement prompt)
      (if hcon
          (progn
              (setq olderr *error* *error* my_err)  
              (setq prompt 
    (strcat "\nEnter SQL statement.\n" drvname "\\" basename "\\" username ">")
              )
              (while (not (eq (setq statement (getstring T prompt)) ""))
                  (compile hcon statement)
              )
              (setq *error* older)      ; restore old *error* handler
          )
                (princ "No active tables detected.")
      )
      (prin1)
  )
  
  (defun sqlterm ()
      (if hdrv (if (asi_termdrv  hdrv) (setq hdrv nil hcon nil)))
      (prin1)
  )
  
  (defun testbind ( / com hcom val name htype length)
      (if hcon
          (progn
          (setq olderr *error* *error* my_err)  
          (while (not (eq "" (setq com (getstring T "\nSQL STATEMENT>"))))
              (if (and (setq hcom (asi_ohdl hcon)) (asi_com hcom com))
                  (progn
                            (while (not (= "" (setq name 
                              (getstring "\nHost variable name: "))))
                          (initget "Char Int Real Short Long Float")
                          (setq htype (strcat "ASI_H" 
          (getkword "\nVariable type Char/Int/Real/Short/Long/Float: ")))
                            (setq val (getstring T "\nEnter host variable value: "))
                          (initget 1)
                          (setq length (getint "Length: "))    
                            (if (asi_bnd hcom name val htype length)
                                    (princ "\nOK\n")
                                    (princ (strcat "\nBind Error: "
                                   (asi_errmsg hcom) "\n"))
                          )
                      )
                        (if (asi_exe hcom)
                          (progn 
                              (princ "\nOK\n")       
                                        (if (eq (asi_stm hcom) "ASI_CURSOR") (scan hcom))
                          ) 
                            (progn
                              (princ "\nError")
                                (asi_errmsg hcom)
                            )
                        )
                      (asi_chdl hcom)
                  )
              )
          )
          (setq *error* older)      ; restore old *error* handler
          )
        (princ "\nNo active data base")
      )
      (prin1)
  )
  
  (defun sqlfile ( / fname)
      (if hcon
          (progn
              (setq olderr *error* *error* my_err)  
              (if (not (= "" (setq fname (getstring "Enter file name: "))))
                  (if (setq fname (findfile fname))
                      (readFile hcon fname)
                      (princ "\nBad file name")
                  )
              )
              (setq *error* older)      ; restore old *error* handler
          )
            (princ "No active tables detected.")
      )
      (prin1)
  )
  
  (defun sqldrv ()
      (if asi_initdrv
          (progn    
              (setq olderr *error* *error* my_err)
              (if hdrv (asi_termdrv hdrv))
              (setq hcon nil)
              (initget 1)
              (setq drvname (getstring "\nEnter SQL driver name: "))
              (if (setq hdrv (asi_initdrv drvname))
                  (princ "\nDrive loaded")
                  (princ (strcat "\nCannot load " drvname))
              )
              (setq *error* older)      ; restore old *error* handler
          )
          (princ "\nLoad 'LISPSQL.EXP' before execution.")      
      )
      (prin1)
  )

;;;--------------------------------------------------------------------------

(princ "ASITEST loaded:\n") 
(princ "\nSQLDRV or (SQLDRV)     - Driver Initialization")
(princ "\nSQLCNC or (SQLCNC)     - Open a Handle to a Database")
(princ "\nSQLDIS or (SQLDIS)     - Release the Connection to a Database")
(princ "\nSQLFILE or (SQLFILE)   - Execute SQL Statements from a file")
(princ "\nSQL or (SQL)           - Execute SQL Statements defined in the")
(princ "\n                         dialogue and Fetching the results of")
(princ "\n                         'cursor' commands")
(princ "\nTESTBIND or (TESTBIND) - Execute SQL Statements with Host Variables")
(princ "\nSQLTERM or (SQLTERM)   - Release the Driver.")
(princ)

  
  
  
  
  
  

