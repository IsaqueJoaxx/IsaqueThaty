;;;---------------------------------------------------------------------------
;;;
;;;   dbview.lsp
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
;;;   by Frumkin A.
;;;   April 20 1992
;;;
;;;--------------------------------------------------------------------------
;;;  DESCRIPTION
;;;
;;;  Test ASI. Allows customers to view and edit database tables.
;;;
;;;----------------------------------------------------------------------------

;;;----------------------------------------------------------------------------
;;; Defined c: so that it can be used at the Command Line..
;;;----------------------------------------------------------------------------
  (defun c:dbview()
    (dbview)
  )

;;;
;;; Drive initialization.
;;;
  (defun initdrv ( / drvname hdrv)
      (setq drvname (getstring "\nEnter SQL driver name: "))
      (if (not (= "" drvname))
          (if (setq hdrv (asi_initdrv drvname))
              (princ "\nDrive loaded")
              (princ (strcat "\nCannot load " drvname))
          )
          (setq hdrv nil)
      )
      (setq hdrv hdrv)
  )
  
  ;;;
  ;;; Logon to the data base.
  ;;;
  (defun logon (hdrv / basename username password hcon)
      (setq basename (getstring "\n\nDatabase name ->"))
      (setq username (getstring "\nUser name ->"))
      (setq password (getstring "\nPassword ->"))
      (if (setq hcon (asi_lon hdrv basename username password))
          (princ "OK")
          (princ (strcat "\nCannot connect to database " basename))
      )
      (setq hcon hcon)
  )
  
  ;;;
  ;;; Fetching commands.
  ;;;
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
  
  ;;;
  ;;; Prints row from database.
  ;;;
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
  
  ;;; 
  ;;; Prints table.
  ;;;
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
  
  ;;;
  ;;; Prints names of columns.
  ;;;
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
  
  ;;;
  ;;; Prints contents of table.
  ;;;
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
  
  ;;;
  ;;; Adds spaces to string while its length leth then defined one.
  ;;;
  (defun addlist (str len / l)
     (setq l (strlen str)) 
     (while (< l len)
        (setq l (1+ l) str (strcat str " "))
     )
     (setq str str)
  )
  
  ;;;
  ;;; Updates row.
  ;;; 
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
  
  ;;;
  ;;; Error handle.
  ;;;
  (defun my_err (s)                  ; If an error (such as CTRL-C) occurs
                                     ; while this command is active...
  
          (if hddrv (asi_termdrv hddrv))
          (setq hddrv nil)
          (if (/= (substr s 1 4) QUIT)
             (princ s)
          )
          (setq *error* older)      ; restore old *error* handler
          (prin1)
  )
  
  ;;;
  ;;; External command
  ;;;
  (defun dbview ( / hdcon hdcom)
      (if asi_initdrv
          (progn    
              (setq olderr *error* *error* my_err)
              (if (and 
                      (setq hddrv (initdrv))
                      (setq hdcon (logon hddrv))
                      (setq hdcom (asi_ohdl hdcon))
                      (not (= "" (setq name (getstring "\nTable name: "))))
                  )
                  (if (asi_cex hdcom (strcat "select * from " name))
                      (scan hdcom)
                      (princ (strcat "\nTable " name " not found."))
                  )
              )
              (if hddrv (asi_termdrv hddrv))
              (setq *error* older)      ; restore old *error* handler
          )
          (princ "\nLoad 'LISPSQL.EXP' before execution.")      
      )
      (prin1)
  )
;;;----------------------------------------------------------------------------

(princ "C:DBVIEW loaded. Start command with (DBVIEW) or DBVIEW.")
(princ)
  
  
  
  

