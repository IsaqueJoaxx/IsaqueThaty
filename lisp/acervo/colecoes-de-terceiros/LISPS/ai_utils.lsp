;;;----------------------------------------------------------------------------
;;;
;;;   AI_UTILS.LSP   Version 0.5
;;;
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
;;;----------------------------------------------------------------------------
;;; (ai_abort <appname> [<error message>] )
;;;
;;; Displays critical error message in alert box and terminates all
;;; running applications.
;;;
;;; If <errmsg> is nil, no alert box or error message is displayed.

  (defun ai_abort (app msg)
     (defun *error* (s)
        (if old_error (setq *error* old_error))
        (princ)
     )
     (if msg
       (alert (strcat " Application error: "
                      app
                      " \n\n  "
                      msg
                      "  \n"
              )
       )
     )
     (exit)
  )


(defun ai_return (value) value)  ; Make act of returning value explicit

;;; Beep function conditional on user-preferred setting.

    (defun ai_beep ( / f)
       (write-line "\007" (setq f (open "CON" "w")))
       (setq f (close f))
    )

;;; (ai_alert <message> )
;;;
;;; Shell for (alert)

    (defun ai_alert (msg)
       (if ai_beep? (ai_beep))
       (alert (strcat " " msg "  "))
    )

;;;  (ai_acadapp)
;;;
;;;  Check to see if acadapp is loaded (and load if necessary).
;;;
;;;  If ACADAPP is not loaded, then display a message indicating
;;;  such in an alert box, and return NIL to the caller.  This
;;;  function does not generate an error condition, so if that is
;;;  appropriate, then the caller must test the result and generate
;;;  the error condition itself.
;;;

(defun ai_acadapp ( / fname)
   (setq fname (ai_acadapp_fn))
   (cond
      (  (= (type acad_colordlg) 'EXSUBR))              ; it's already loaded.

      (  (not (findfile fname))                         ; find it
         (ai_alert (strcat "Can't find " fname "."))
         (ai_return nil))

      (  (eq "failed" (xload fname "failed"))           ; load it
         (ai_alert (strcat "Can't load " fname "."))
         (ai_return nil))
     (t)
   )
)

;;; (ai_acadapp_fn)
;;;
;;; This function returns the filename & extension of the ADS
;;; ACADAPP executable for every platform.

;;; Default filename is "acadapp" (in lower-case).

(defun ai_acadapp_fn ( / platform)
   (setq platform (getvar "platform"))
   (cond
      (  (eq platform "Windows")           "ACADAPP.EXE")
      (  (eq platform "386 DOS Extender")  "ACADAPP.EXP")

      ;;;
      ;;; insert other cases as required.
      ;;;

      (t "acadapp")             ; Default extension 
   )
)

;;; (ai_table <table name> <bit> )
;;;
;;; Returns a list of items in the specified table.  The bit values have the
;;; following meaning:
;;;  0  List all items in the specified table.
;;;  1  Do not list Layer 0 and Linetype CONTINUOUS.
;;;  2  Do not list anonymous blocks.
;;;         A check against the 70 flag for the following bit:
;;;                  1  anonymous block
;;;  4  Do not list externally dependant items.
;;;         A check against the 70 flag is made for any of the following 
;;;         bits, which add up to 48:
;;;                 16  externally dependant
;;;                 32  resolved external or dependant
;;;  8  Do not list Xrefs.
;;;         A check against the 70 flag for the following bit:
;;;                  4  external reference
;;;  16 Add BYBLOCK and BYLAYER items to list.
;;;
(defun ai_table (table_name bit / tbldata table_list just_name)
  (setq tbldata nil)
  (setq table_list '())
  (setq table_name (strcase table_name))
  (while (setq tbldata (tblnext table_name (not tbldata)))
    (setq just_name (cdr (assoc 2 tbldata)))
    (cond 
      ((= "" just_name))               ; Never return null Shape names.
      ((and (= 1 (logand bit 1))
            (or (and (= table_name "LAYER") (= just_name "0"))
                (and (= table_name "LTYPE")
                     (= just_name "CONTINUOUS")
                )
            )
      ))
      ((and (= 2 (logand bit 2))
            (= table_name "BLOCK")
            (= 1 (logand 1 (cdr (assoc 70 tbldata))))
      )) 
      ((and (= 4 (logand bit 4))
            ;; Check for Xref dependents only. 
            (zerop (logand 4 (cdr (assoc 70 tbldata)))) 
            (not (zerop (logand 48 (cdr (assoc 70 tbldata)))))
            
      ))
      ((and (= 8 (logand bit 8))
            (not (zerop (logand 4 (cdr (assoc 70 tbldata)))))
      ))
      ;; Vports tables can have similar names, only display one.
      ((member just_name table_list)
      )
      (T (setq table_list (cons just_name table_list)))
    )
  )
  (cond
    ((and (= 16 (logand bit 16))
          (= table_name "LTYPE") ) (setq table_list (cons "BYBLOCK" 
     (cons "BYLAYER" table_list))) ) 
    (t) 
  ) 
  (ai_return table_list) 
)

;;;
;;; (ai_strtrim <string> )
;;;
;;; Trims leading and trailing spaces from strings.
(defun ai_strtrim (s)
  (cond 
    ((/= (type s) 'str) nil)
    (t (ai_strltrim (ai_strrtrim s)))
  )
)
(defun ai_strltrim (s)
  (cond 
    ((eq s "") s)
    ((/= " " (substr s 1 1)) s)
    (t (ai_strltrim (substr s 2)))
  )
)
(defun ai_strrtrim (s)
  (cond 
    ((eq s "") s)
    ((/= " " (substr s (strlen s) 1)) s)
    (t (ai_strrtrim (substr s 1 (1- (strlen s)))))
  )
)

;;;
;;; Pass a number, an error message, and a range.  If the value is good, it is
;;; returned, else an error is displayed.  
;;;  Range values:
;;;                 0 - any numeric input OK
;;;                 1 - reject positive
;;;                 2 - reject negative
;;;                 4 - reject zero
;;;                 
(defun ai_num (value error_msg range / good_value)
  (cond
    ;; is it a number
    ((not (setq good_value (distof value)))
      (set_tile "error" error_msg)
      nil
    )
    ;; is it positive
    ((and (= 1 (logand 1 range))
       (= (abs good_value) good_value)
     )
      (set_tile "error" error_msg)
      nil
    )
    ;; is it zero
    ((and (= 2 (logand 2 range))
       (= 0.0 good_value)
     )
      (set_tile "error" error_msg)
      nil
    )
    ;; is it negative
    ((and (= 4 (logand 4 range))
       (/= (abs good_value) good_value)
     )
      (set_tile "error" error_msg)
      nil
    )
    (T good_value)
  )
)

;;;
;;; Pass an angle and an error message.  If good, the angle is returned else
;;; nil and an error message displayed.
;;;
(defun ai_angle(value error_msg / good_value)
  (cond
    ((and (setq good_value (angtof value))
     )
      (set_tile "error" "")
      (atof (angtos good_value))
    )
    (T (set_tile "error" error_msg) nil)
  )
)

;;;
;;;  Error routine.
;;;
(defun ai_error (s)              ; If an error (such as CTRL-C) occurs
  (if (not (member s '("Function cancelled" "console break")))
    (princ (strcat "\nError: " s))
  )
  (if undo_init (ai_undo_pop))              ; Deal with UNDO
  (if old_error (setq *error* old_error))   ; Restore old *error* handler
  (if old_cmd (setvar "cmdecho" old_cmd))   ; Restore cmdecho value
  (princ)
)

;;;
;;; Routines that check CMDACTIVE and post an alert if the calling routine
;;; should not be called in the current CMDACTIVE state.  The calling 
;;; routine calls (ai_trans) if it can be called transparently or 
;;; (ai_notrans) if it cannot.
;;;
;;;           1 - Ordinary command active.
;;;           2 - Ordinary and transparent command active.
;;;           4 - Script file active.
;;;           8 - Dialogue box active.
;;;
(defun ai_trans ()
  (if (zerop (logand (getvar "cmdactive") (+ 2 4 8) ))
    T
    (progn 
      (alert "This command may not be invoked transparently.")
      nil
    )
  )
)

(defun ai_transd ()
  (if (zerop (logand (getvar "cmdactive") (+ 2 4) ))
    T
    (progn 
      (alert "This command may not be invoked transparently.")
      nil
    )
  )
)

(defun ai_notrans ()
  (if (zerop (logand (getvar "cmdactive") (+ 1 2 4 8) ))
    T
    (progn 
      (alert "This command may not be invoked transparently.")
      nil
    )
  )
)

;;; (ai_aselect)
;;;
;;; Looks for a current selection set, and returns it if found,
;;; or throws user into interactive multiple object selection,
;;; and returns the resulting selection set if one was selected.
;;;
;;; Sets the value of ai_seltype to:
;;;
;;;    1 = resulting selection set was autoselected
;;;    2 = resulting selection set was prompted for.

   (defun ai_aselect ( / ss)
      (cond
         (  (and (eq 1 (logand 1 (getvar "pickfirst")))
                 (setq ss (ssget "i")))
            (setq ai_seltype 1)
            (ai_return ss))
         (  (setq ss (ssget))
            (setq ai_seltype 2)
            (ai_return ss))
      )
   )

;;; (ai_aselect1 <msg> )
;;;
;;; Looks for ONE autoselected entity, or throws the user into
;;; interactive entity selection (one entity, where a selection
;;; point is insignificant).  <msg> is the prompt generated if
;;; interactive selection is invoked.
;;;
;;; Sets the value of ai_seltype to:
;;;
;;;    1 = resulting entity was autoselected
;;;    2 = resulting entity was prompted for.


   (defun ai_aselect1 (msg / ent)
      (cond
         (  (and (eq 1 (logand 1 (getvar "pickfirst")))
                 (setq ent (ssget "i"))
                 (eq 1 (sslength ent)))
            (setq ai_seltype 1)
            (ai_return (ssname ent 0)))

         (  (setq ent (entsel msg))
            (setq ai_seltype 2)
            (ai_return (car ent)))
      )
   )

;;;
;;; A function that turns on UNDO so that some existing routines will work.
;;; Do not use with new routines as they should be designed to operate with
;;; any UNDO setting.
;;;
(defun ai_undo_on ()
  (setq undo_setting (getvar "undoctl"))
  (cond
    ((= 2 (logand undo_setting 2))     ; Undo is one
      (command "_.undo" "control" "_all" "_.undo" "_auto" "_off")
    )
    ((/= 1 (logand undo_setting 1))    ; Undo is disabled
      (command "_.undo" "_all" "_.undo" "_auto" "_off")
    )
  )
)

;;;
;;; Return UNDO to the initial setting.  Do not use with new routines as they 
;;; should be designed to operate with any UNDO setting.
;;;
(defun ai_undo_off ()
  (cond 
    ((/= 1 (logand undo_setting 1))
      (command "_.undo" "_control" "_none")
    )
    ((= 2 (logand undo_setting 2))
      (command "_.undo" "_control" "_one")
    )
  )
)

;;;
;;; UNDO handlers.  When UNDO ALL is enabled, Auto must be turned off and 
;;; GROUP and END added as needed. 
;;;
(defun ai_undo_push()
  (setq undo_init (getvar "undoctl"))
  (cond
    ((and (= 1 (logand undo_init 1))   ; enabled
          (/= 2 (logand undo_init 2))  ; not ONE (ie ALL is ON)
          (/= 8 (logand undo_init 8))   ; no GROUP active
     )
      (command "_.undo" "_group")
    )
    (T)
  )  
  ;; If Auto is ON, turn it off.
  (if (= 4 (logand 4 undo_init))
      (command "_.undo" "_auto" "_off")
  )
)

;;;
;;; Add an END to UNDO and return to initial state.
;;;
(defun ai_undo_pop()
  (cond 
    ((and (= 1 (logand undo_init 1))   ; enabled
          (/= 2 (logand undo_init 2))  ; not ONE (ie ALL is ON)
          (/= 8 (logand undo_init 8))   ; no GROUP active
     )
      (command "_.undo" "_end")
    )
    (T)
  )  
  ;; If it has been forced off, turn it back on.
  (if (= 4 (logand undo_init 4))
    (command "_.undo" "_auto" "_on")
  )  
)
;;;
;;; (get_dcl "FILTER")
;;;
;;; Checks for the existence of, and loads the specified .DCL file,
;;; or aborts with an appropriate error message, causing the initial
;;; load of the associated application's .LSP file to be aborted as
;;; well, disabling the application.
;;;
;;; If the load is successful, the handle of the .DCL file is then
;;; added to the ASSOCIATION LIST ai_support, which would have the
;;; following structure:
;;;
;;;
;;;   (("DCLFILE1" . 1) ("DCLFILE2" . 2)...)
;;;
;;; If result of (ai_dcl) is NIL, then .DCL file is not avalable,
;;; or cannot be loaded (the latter can result from a DCL audit).
;;;
;;; Applications that call (ai_dcl) should test its result, and
;;; terminate or abort if it is nil.  Normal termination rather
;;; than aborting with an error condition, is desirable if the
;;; application can be invoked transparently.
;;;
(defun ai_dcl (dcl_file / dcl_handle)
  (cond
    ;; If the specified .DCL is already loaded then
    ;; just return its handle to the caller.
    ((ai_return (cdr (assoc dcl_file ai_support))))

    ;; Otherwise, try to FIND the .DCL file, and display a
    ;; an appropriate message if it can't be located, and
    ;; return Nil to the caller:
    ((not (findfile (strcat dcl_file ".dcl")))
      (ai_alert
        (strcat
          "Can't locate dialog definition file " dcl_file
          ".dcl\n Check your support directory."))
      (ai_return nil)
    )
    ;; The file has been found.  Now try to load it.  If it
    ;; can't be succesfully loaded, then indicate such, and
    ;; abort the caller:
    ((or (not (setq dcl_handle (load_dialog dcl_file)))
         (> 1 dcl_handle))
      (ai_alert
        (strcat
          "Can't load dialog control file " dcl_file ".dcl"
          "\n Check your support directory."))
      (ai_return nil)
    )
    ;; Otherwise, the file has been loaded, so add it's handle
    ;; to the FILE->HANDLE association list AI_SUPPORT, and
    ;; return the handle to the caller:
    (t (setq ai_support (cons (cons dcl_file dcl_handle) ai_support))
      (ai_return dcl_handle)
    )
  )
)
