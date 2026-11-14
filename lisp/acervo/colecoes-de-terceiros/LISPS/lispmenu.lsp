; LISPMENU.LSP
; Copyright (c) Barry R. Bowen 1993
; __________________________________________________________
; Variables:
; CNT      = Counter
; FILE     = DCL file pointer
; FN       = DCL file name
; FNAME    = Complete DCL file name with path
; NKEY     = Keyword form key_word list
; KEY      = Keyword for program button label
; KEY_LIST = Keyword list from DCL file
; ----------------------------------------------------------

(defun C:LISPMENU (/ CNT FN FNAME FILE NKEY KEY KEY_LIST)
  (defun WLF (LINE) (write-line LINE FILE))
  (setq old_cmd (getvar "cmdecho")
        old_error *error*
        *error* ai_error)
  (setvar "cmdecho" 0)
  (while (not FN)
    (setq FN (strcase (getstring "\nDCL Filename: "))
       FNAME (strcat FN ".DCL"))
    (if (not (findfile FNAME))
      (progn
        (setq FN nil)
        (alert (strcat "File " FNAME " Does Not Exist!"))
  ) ) )
  (prompt "\nCreating LISP Program File....")
  (setq FILE (open FNAME "r"))
  (setq LINE (read-line FILE))
  (setq key_list (list '()))
  (while LINE
    (if (wcmatch LINE "?*key*")
      (progn
         (get_key)
         (setq key_list (append (list NKEY) key_list))
    ) )
    (setq LINE (read-line FILE))
  )
  (setq key_list (cdr (reverse key_list)))
  (close FILE)
  (setq FNAME (strcat FN ".LSP"))
  (setq FILE (open FNAME "w"))
  (WLF (strcat ";; " FNAME))
  (WLF ";; Program For AutoLISP Dialog Box Menu")
  (WLF "")
  (WLF "")
  (WLF (strcat "(defun C:" FN " (/ ai_defaults dcl_id old_cmd
                       old_error PRG TILE what_next)"))
  (WLF (strcat "  (defun " FN "_MAIN ()"))
  (WLF 
(strcat "    (if (not (new_dialog " (chr 34) FN (chr 34)
                                        " dcl_id)) (exit))"))
  (WLF 
(strcat "      (action_tile " (chr 34) "accept" (chr 34)
                     (chr 34) " (done_dialog)" (chr 34) ")"))
  (WLF 
(strcat "      (action_tile " (chr 34) "cancel" (chr 34)
                     (chr 34) " (done_dialog)" (chr 34) ")"))
  (setq CNT 0)
  (setq KEY (nth CNT KEY_LIST))
  (while (/= KEY nil)
      (WLF 
(strcat "      (action_tile " (chr 34) KEY (chr 34) " "
                    (chr 34) "(setq PRG $key)" (chr 34) ")"))
      (setq CNT (1+ CNT))
      (setq KEY (nth CNT KEY_LIST))
  )
  (WLF "")
  (WLF "  (setq what_next (start_dialog))")
  (WLF "   (if (= 1 what_next)")
  (WLF "    (progn")
  (WLF 
(strcat "  (if (assoc " (chr 34) FN (chr 34) " ai_defults)"))
  (WLF (strcat "       (setq ai_defults (subst (list "
                         (chr 34) FN (chr 34) " on_screen)"))
  (WLF 
(strcat "     (assoc " (chr 34) FN (chr 34) " ai_defaults)"))
  (WLF "               ai_defaults")
  (WLF "  )))))")
  (WLF "  (do_action PRG)")
  (WLF "  );end defun main")
  (WLF "")
  (WLF ";; Setup Error Function")
  (WLF 
(strcat "(setq old_cmd (getvar " (chr 34) "cmdecho" (chr 34) )"))
  (WLF "     old_error  *error*")
  (WLF "      *error* ai_error")
  (WLF ")")
  (WLF (strcat "(setvar " (chr 34) "cmdecho" (chr 34) " 0)"))
  (WLF "(cond")
  (WLF "   ((not (ai_notrans)))")
  (WLF "   ((not (ai_acadapp)))")
  (WLF 
(strcat "   ((not (setq dcl_id (ai_dcl "(chr 34) FN (chr 34) ))))"))
  (WLF (strcat "   (t (" FN "_MAIN))"))
  (WLF ")")
  (WLF "(setq *error* old_error)")
  (WLF 
(strcat "(setvar " (chr 34) "cmdecho" (chr 34) " old_cmd)"))
  (WLF "(done_dialog dcl_id)")
  (WLF "(princ)")
  (WLF ")")
  (WLF "")
  (WLF "(defun do_action (PRG)")
  (WLF "  (cond")
  (setq CNT 0)
  (setq KEY (nth CNT KEY_LIST))
  (while (/= KEY nil)
    (WLF 
(strcat "    ((= PRG " (chr 34) KEY (chr 34) ") (load "
                     (chr 34) KEY (chr 34) ") (c:" KEY "))"))
    (setq CNT (1+ CNT))
    (setq KEY (nth CNT KEY_LIST))
  )
  (WLF "))")
  (WLF "")
  (close FILE)
  (setvar "cmdecho" old_cmd)
  (setq *error* old_error)
  (princ)
)

(defun get_key (/ CK CNT SLS)
  (setq SLS (- (strlen LINE) 3)
        CK (substr LINE SLS 1)
        CNT 1)
  (while (/= CK (chr 34))
     (setq SLS (- SLS 1)
            CK (substr LINE SLS 1)
           CNT (1+ CNT))
  )
  (setq SLS (1+ SLS))
  (setq NKEY (substr LINE SLS CNT))
)
(prompt "\nAutoLISP Dialog Box Program Menu Loaded...")
(prompt "\nType LISPMENU To Run.")
(princ)
