;;;   LLoad.lsp
;;;   Copyright (C) 1990 by Autodesk, Inc.
;;;  
;;;   THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED WARRANTY. 
;;;   ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR PURPOSE AND OF 
;;;   MERCHANTABILITY ARE HEREBY DISCLAIMED.
;;; 
;;;   by Jan S. Yoder
;;;   01 February 1990
;;;
;;;--------------------------------------------------------------------------;
;;; DESCRIPTION
;;;   
;;;   This routine allows you to create a list of AutoLisp file names that
;;;   you load frequently, and allows you to load any of them by typing 
;;;   the number associated with the file name.  This file name can be any
;;;   legal pathname with d4rive letters, etc. that is acceptable to the 
;;;   platform or machine on which AutoCAD is running.  This can be very
;;;   helpful in a networking situation where the file you wish to load is
;;;   on a path such as n:\acad\ourstuff\lsp\etc\foo.lsp.  Typing
;;;   
;;;     (load "n:\acad\ourstuff\lsp\etc\foo")
;;;     
;;;   with the correct syntax is something best left alone.
;;;   
;;;   By using Lload, you can reduce the number of times that you need to 
;;;   type long path names, and remember the exact syntax to a single time,
;;;   and you don't even need to remember the syntax.
;;;   
;;;   LLoad
;;;   
;;;   The first time you run Lload.lsp, you will be asked whether or not you
;;;   want a default file built.  If you answer No, then you can type the
;;;   name of a file you want loaded.  However, if you answer Yes, a new,
;;;   blank file called lload.dfs is created for you, and you may begin 
;;;   adding file names to it.
;;;   
;;;     Build a new default file?  <Y>: 
;;;   
;;;     LispLoad  Version 1.00
;;;     Available Lisp files: 
;;;   
;;;   
;;;     Add/Remove an entry/<Number to load>:   (a)
;;;     Lisp routine name to load <No default>:
;;;           
;;;   Type Add to add a file name.  When you do this, the routine checks to
;;;   see that the file does exist, and if it does, it is loaded into 
;;;   memory and added to the list.  The list is then displayed again, and
;;;   you are prompted as before.  You may add as many routines to the list 
;;;   as you wnat, as long as AutoCAD has the memory to load them. 
;;;   
;;;   You may also remove items from the menu by typing the number associated 
;;;   with it.  However, this does not remove the routine from memory;  you
;;;   must leave the current AutoCAD drawing session to do that.
;;;   
;;;     Number of entry to remove from list:
;;;   
;;;   After you have several items in the list, you may load or reload the 
;;;   routine simply by typing its number.
;;;   
;;;   Pressing RETURN at the Add/Remove prompt exits you from the routine
;;;   without doing anything.
;;;   
;;;   
;;;   XLoad/XULoad
;;;   
;;;   There is a parallel routine called XLoad which allows you to maintain
;;;   a similar list of external functions written in ADS.  The prompts and
;;;   structure are the same.  XULoad allows you to unload ADS functions
;;;   from the same list.
;;;
;;;
;;;--------------------------------------------------------------------------;
;;;
;;; Function main
;;;
(defun l_load (xld unload / a ll_ver ll_oe ll_oer ll_err ll_oc xld deffi I_LIST)

  (setq ll_ver "1.00")                ; Reset this local if you make a change.
  (setq ll_xpf (ll_cpf (getvar "acadprefix" )))
  (setq ll_llf "lload.dfs")           ; Reset this local if you make a change.
  (setq ll_xlf "xload.dfs")           ; Reset this local if you make a change.
  
  (if ll_err                          ; Set our new error handler
    (setq ll_oer ll_err) 
  )
  ;;
  ;; Internal error handler defined locally
  ;;

  (defun ll_err (s)                   ; If an error (such as CTRL-C) occurs
                                      ; while this command is active...
    (if (/= s "Function cancelled")
      (if (= s "quit / exit abort")
        (princ)
        (princ (strcat "\nError: " s))
      )
    )
    (command "undo" "end")
    (if ll_oe                         ; If an old error routine exists
      (setq *error* ll_oe)            ; then, reset it 
    )
    (setvar "cmdecho" ll_oc)          ; Reset command echoing on error
    (princ)
  )
  
  ;;
  ;; Body of LLOAD function
  ;;
  
  (if *error*                         ; Set our new error handler
    (setq ll_oe *error* *error* ll_err) 
    (setq *error* ll_err) 
  )
  (setq ll_oc (getvar "cmdecho"))     ; Save current state of command echoing
  (setvar "cmdecho" 0)                ; Turn off command echoing
  (command "undo" "group")            ; Start an UNDO group

  ;;
  ;; Look for the default file.
  ;;

  (setq deffi (ll_lfx (if xld ll_xlf ll_llf) "r"))

  ;;
  ;; If found, then process, else create one and process
  ;;

  (if deffi
    (ll_gos)                          ; LLoad_Get_OptS
    (progn
      (ll_bdf)                        ; LLoad_Build_Default_File
      (setq deffi (ll_lfx (if xld ll_xlf ll_llf) "r"))
      (if deffi
        (ll_gos)                      ; LLoad_Get_OptS
        (progn
          (princ "\n\tCouldn't open the default file for reading. ")
          (exit)
        )
      )
    )
  )
  (command "undo" "end")              ; End the UNDO group
  (if ll_oe                           ; If an old error routine exists
    (setq *error* ll_oe)              ; then, reset it
  )
  (if ll_oer                          ; Reset the old error handler
    (setq ll_err ll_oer) 
  )
  (setvar "cmdecho" ll_oc)            ; Reset command echoing
  (princ)
)
;;;
;;; Look for an external definition file in AutoCAD's search path
;;; ll_lfx == LLoad_Look_For_Xfile
;;;
(defun ll_lfx (f_name r_or_w / lfile temp)
  ;; Look for f_name in AutoCAD's search paths.
  (if (= r_or_w "w")
    (if (setq temp (open f_name r_or_w))
      temp                            ; Return file descriptor
      (progn
        (princ (strcat "\n\tCouldn't open " f_name " for writing. "))
        (exit)
      )
    )
    (if (setq lfile (findfile f_name))
      (if (setq temp (open lfile r_or_w))
        temp                          ; Return file descriptor
        (progn
          (princ (strcat "\n\tCouldn't open " f_name " for reading. "))
          (exit)
        )
      )
      nil                             ; or nil
    )
  )
)
;;;
;;; Get the user's options
;;; ll_gos == LLoad_Get_OptS
;;;
(defun ll_gos (/ d_item max_ls ans)
  (if textpage (textpage) (textscr))  ; For Release 10
  (setq ans T)
  (while ans
    ;;
    ;; LLoad_Look_For_Xfile
    ;;
    (setq deffi (ll_lfx (if xld ll_xlf ll_llf) "r"))
    (if (null deffi)
      (setq ans nil)
      (progn
        (if xld
          (ll_rux ";;; XLOAD Default Files" 1 23)
          (ll_rux ";;; LISP Default Files" 1 22)
        )
        (if xld
          (if unload
            (princ (strcat "\n\tXUnLoad  Version " ll_ver 
                           "\n\tAvailable ADS programs: \n"))
            (princ (strcat "\n\tXLoad  Version " ll_ver 
                           "\n\tAvailable ADS programs: \n"))
          )
          (princ (strcat "\n\tLispLoad  Version " ll_ver 
                         "\n\tAvailable Lisp files: \n"))
        )
        (setq I_LIST nil)
        (setq max_ls (ll_lns "" 1 1))
        (setq ans (strcat 
          "\n\n\tAdd/Remove an entry/<Number to " (if unload "un" "") "load>: "))
        (setq d_item (ll_pfl max_ls 6 "Add Remove" ans))
        (cond
          ((= d_item nil)
            ;; No file selected.  Exiting. 
            (exit)
          )
          ((= d_item 0)
            (princ)
          )
          (T
            (if xld 
              (setq j:xa (cadr d_item))
              (setq j:a (cadr d_item))
            )
            (ll_lox nil)
            (setq ans nil)
          )
        )
        (close deffi)
      )
    )
  )
)
;;;
;;; Read lines from a file until the argument matches the given sub-string
;;; Returns the last line read as a string.
;;; ll_rux == LLoad_Read_Until_X
;;;
(defun ll_rux (str j k / l cont line)
  (setq cont T l 0)
  (while cont
    (setq line (read-line deffi))
    ;;
    ;; Seek to the start of the default file definition
    ;;
    (if line
      (if (= (substr line j k) str)
        (setq cont nil)
        (setq l (1+ l))
      )
      (progn
        (setq cont nil)
      )
    )
  )
  line                                ; Return line as a string
)
;;;
;;; List names on the screen until an end of list marker is found.
;;; Store the items found into a list, I_LIST, a global
;;; Ignore blank lines and commented lines. Return number of lines.
;;; ll_lns == LLoad_List_Names_on_Screen
;;;
(defun ll_lns (str j k / l cont line)
  (setq cont T l 0)
  (while cont
    (if (setq line (read-line deffi))
      ;; Seek to the end of the section delimited by "str"
      ;; Else print the line to the screen preceded by an integer
      (if (= (substr line j k) str)
        (setq cont nil)
        (progn
          (setq l         (1+ l)
                item      (ll_tok line)
                I_LIST (if I_LIST
                            (append I_LIST (list item))
                            (list item)
                          )
          )
          (if (and (> l 1) (= (rem l 10) 1))
            (if (= (rem l 20) 1)
              (progn
                (princ "\n\t<more> ")
                (grread)
                (repeat 8 (progn (princ (chr 8))   ; back one char
                                 (princ (chr 32))  ; space
                                 (princ (chr 8)))) ; back one char
              )
              (terpri)
            )
          )
          (princ (strcat "\n\t" (itoa l) ":\t " line))
        )
      )
      (setq cont nil)
    )
  )
  l
)
;;;
;;; Tokenize the line, removing any trailing blanks.
;;; Return the tokenized string
;;; ll_tok == LLoad_TOKenize
;;;
(defun ll_tok (str / sl j)
  (setq sl (strlen str)
        j  0
  )
  (while (= (substr str (- sl j) 1) " ")
    (setq j (1+ j))
  )
  (substr str 1 (- sl j))
)
;;;
;;; Pick from the list by typing an integer, returns the item, zero or nil.
;;; ll_pfl == LLoad_Pick_From_List
;;;
(defun ll_pfl (max_l ig_b ig_str prmpt / OK ans return)
  (while (null OK)
    (initget ig_b ig_str)
    (setq ans (getint prmpt))
    (cond 
      ((= ans "Remove")
        (setq str "\n\tNumber of entry to remove from list: ")
        (setq d_item (ll_pfl max_ls 6 "" str))
        (if (/= d_item nil)
          (progn
            (princ (strcat "\n\tRemoving " (cadr d_item) " from list. "))
            (ll_chl d_item nil)
          )
        )
        (setq OK T return 0)
      )
      ((= ans "Add")
        (setq d_item (list 0 (ll_lox T)))
        (if (nth 1 d_item) (ll_chl d_item T))
        (setq OK     T
              return 0
        ) 
      )
      ((or (= ans "") (null ans))
        (setq OK     T
              return nil
        ) 
      )
      (T
        (cond
          ((and (> ans 0) (<= ans max_l))
            (setq return (list ans (nth (1- ans) I_LIST))
                  OK     T
            )
          )
          (T
            (cond 
              ((= max_l 0)
                (princ "\n\tNo files to load.")
                (setq OK nil)
              )
              ((= max_l 1)
                (princ "\n\tOnly one file to load.")
                (setq OK nil)
              )
              (T
                (princ (strcat 
                  "\n\tNumber must be between 1 and " (itoa max_l) "."))
                (setq OK nil)
              )
            )
          )
        )
      )
    )
  )
  return
)
;;;
;;; Load or Xload the selected file.  Returns a file name.
;;; ll_lox == LLoad_Load_Or_Xload
;;;
(defun ll_lox (typeit / dflt ans lfile )  
  (if typeit
    (progn
      (if (null (if xld j:xa j:a))
        (setq dflt "No default")
        (setq dflt (if xld j:xa j:a))
      )
      (setq ans (getstring (strcat 
        "\n\t" (if xld
                 "External program"
                 "Lisp routine"
               )
               " name to "
               (if unload "un" "")
               "load <" 
               dflt ">: \n\t")))
               
      (if (not (or (eq ans "") (eq ans nil)))
        (progn
          (if (= (substr ans (- (strlen ans) 3)) ".lsp")
            (setq ans (substr ans 1 (- (strlen ans) 4)))
          )
          (set (if xld (read "j:xa") (read "j:a")) ans)
        )
      )
      (if (= (if xld j:xa j:a) "No default")
        (princ "\nNo file specified. ")
      )
    )
  )
  (setq lfile (if xld j:xa (strcat j:a ".lsp")))
  (if (not (open lfile "r"))
    (progn
      (setq lfile (findfile (if xld j:xa (strcat j:a ".lsp"))))
    )
    ;else just read it directly from the given path
  )
  (if lfile
    (progn
      (if unload (princ "\n\tUnloading ") (princ "\n\tLoading "))
      (princ (if xld j:xa (strcat j:a ".lsp... ")))
      (if xld
        (if unload 
          (xunload j:xa)
          (xload j:xa)
        )
        (load j:a)
      )
      (princ " Done. ")
    )
    (progn
      (princ "\n\t")
      (princ (if xld j:xa (strcat j:a ".lsp ")))
      (princ " -- Invalid filename or file not found.\n")
      (setq lfile nil)
    )
  )
  (if lfile (if xld j:xa j:a) nil)
)
;;;
;;; Add or remove the item from the default file.
;;; If A_OR_R is T then add, else remove
;;; ll_chl == LLoad_CHange_List
;;;
(defun ll_chl (item a_or_r / deffi k temp1 temp2 temp3)
  (if a_or_r
    ;;
    ;; Adding an item to the default list.
    ;;
    (progn
      (if xld
        (setq deffi (ll_lfx ll_xlf "a"))
        (setq deffi (ll_lfx ll_llf "a"))
      )
      (princ (strcat "\n\tWriting " (cadr item) " to default file. "))
      (write-line (cadr item) deffi)
    )
    ;;
    ;; Removing an item from the default list.
    ;;
    (progn
      (if xld
        (setq deffi (ll_lfx ll_xlf "r"))
        (setq deffi (ll_lfx ll_llf "r"))
      )
      (setq temp1 (read-line deffi))
      (setq temp2 (read-line deffi))
      (setq temp3 (read-line deffi))
      (close deffi)

      (if xld
        (setq deffi (ll_lfx ll_xlf "w"))
        (setq deffi (ll_lfx ll_llf "w"))
      )

      (write-line temp1 deffi)
      (write-line temp2 deffi)
      (write-line temp3 deffi)
      (setq k 0 l (length I_LIST))

      (while (and (< k l) (/= k (1- (car item))))
        (write-line (nth k I_LIST) deffi)
        (setq k (1+ k))
      )
      (while (< (setq k (1+ k)) l)
        (write-line (nth k I_LIST) deffi)
      )
    )
  )
  (close deffi)
)
;;;
;;; Build the default file from this file.
;;; ll_bdf == LLoad_Build_Default_File
;;;
(defun ll_bdf (/ ans deffi)
      
  (initget "Yes No")
  (setq ans (getkword "\nBuild a new default file?  <Y>: "))
  (if (= ans "No")
    (ll_lox T)
    (progn
      (if xld
        (if (setq deffi (open (strcat ll_xpf ll_xlf) "w"))
          (progn
            (princ ";;; Do NOT erase or change the first three lines\n" deffi)
            (princ (strcat ";;; Version " ll_ver 
                           " -- (c) Autodesk, Inc  February 1990\n") deffi)
            (princ ";;; XLOAD Default Files\n" deffi)
            (if j:a (write-line j:a deffi))
            (close deffi)
          )
          (princ "\nError opening XLOAD.DFS for writing. ")
        )
        (if (setq deffi (open (strcat ll_xpf ll_llf) "w"))
          (progn
            (princ ";;; Do NOT erase or change the first three lines\n" deffi)
            (princ (strcat ";;; Version " ll_ver 
                           " -- (c) Autodesk, Inc  February 1990\n") deffi)
            (princ ";;; LISP Default Files\n" deffi)
            (if j:a (write-line j:a deffi))
            (close deffi)
          )
          (princ "\nError opening LISP.DFS for writing. ")
        )
      )
    )
  )
)
;;;
;;; Return the first path in ACADPREFIX delimited by ";".
;;;
;;; ll_cpf == LLoad_Check_acadPreFix
;;;
(defun ll_cpf (pf / temp)
  (setq j 1
        l (strlen pf)
  )
  (while (<= j l)
    (if (= (substr pf j 1) ";")
      (progn
        (setq temp (substr pf 1 (1- j)))
        (setq j (1+ l))
      )
      (setq j (1+ j))
    )
  )
  (if temp
    temp
    pf
  )
)
;;;
;;; These are the C: function definitions
;;;
(defun c:ll () (l_load nil nil))
(defun c:xl () (l_load T nil))
(defun c:xul () (l_load T T))
;;; (defun c:load () (l_load nil nil))
;;; (defun c:xload () (l_load T nil))
;;; (defun c:xunload () (l_load T T))
(princ "\n\tLLoad loaded.  Type LL, XL or XUL to start program. \t")
(princ)
