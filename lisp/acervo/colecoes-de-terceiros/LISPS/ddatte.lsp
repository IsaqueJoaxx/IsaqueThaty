;;;------------------------------------------------------------------------
;;;  DDATTEXT.LSP
;;;  Copyright (C) 1993 by Autodesk, Inc.
;;;
;;; Permission to use, copy, modify, and distribute this software
;;; for any purpose and without fee is hereby granted, provided
;;; that the above copyright notice appears in all copies and that 
;;; both that copyright notice and this permission notice appear in 
;;; all supporting documentation.
;;; 
;;; THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED
;;; WARRANTY.  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR
;;; PURPOSE AND OF MERCHANTABILITY ARE HEREBY DISCLAIMED.
;;;
;;;------------------------------------------------------------------------
;;;  DESCRIPTION
;;;
;;;  This is an enhancement to the ATTEXT command. It loads up a dialogue box
;;;  which presents to the user all the prompts and options that he/she
;;;  might encounter during the extraction of attributes.
;;;
;;;------------------------------------------------------------------------
;;;  Warning
;;;
;;;  The filenames are not turned into uppercase because unix files are 
;;;  case sensitive.
;;;
;;;------------------------------------------------------------------------
;;;   Prefixes in command and keyword strings: 
;;;      "."  specifies the built-in AutoCAD command in case it has been        
;;;           redefined.
;;;      "_"  denotes an AutoCAD command or keyword in the native language
;;;           version, English.
;;;------------------------------------------------------------------------
;;;
;;;  Revisions: 
;;; 
;;;    (Not sanctioned by Autodesk, Inc.)
;;;
;;;    1.  9/22/93  TonyT.
;;;    
;;;        Fixed bug that causes explicit selection set to be ignored.
;;;        
;;;
;;;    2.  9/22/93  TonyT.
;;;
;;;        Added support for Noun-Verb object selection.
;;;        
;;;
;;;====================== load-time error checking ========================

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

;;; Check to see if AI_UTILS is loaded, If not, try to find it,
;;; and then try to load it.
;;;
;;; If it can't be found or it can't be loaded, then abort the
;;; loading of this file immediately, preserving the (autoload)
;;; stub function.

  (cond
     (  (and ai_dcl (listp ai_dcl)))          ; it's already loaded.

     (  (not (findfile "ai_utils.lsp"))                     ; find it
        (ai_abort "DDATTEXT"
                  (strcat "Can't locate file AI_UTILS.LSP."
                          "\n Check support directory.")))

     (  (eq "failed" (load "ai_utils" "failed"))            ; load it
        (ai_abort "DDATTEXT" "Can't load file AI_UTILS.LSP"))
  )

  (if (not (ai_acadapp))               ; defined in AI_UTILS.LSP
      (ai_abort "DDATTEXT" nil)         ; a Nil <msg> supresses
  )                                    ; ai_abort's alert box dialog.

;;;===================== end load-time operations =========================

;;;
;;; The main routine.
;;;
(defun c:ddattext (/ 
                  bit         ftype          out_ext        tile_name
                  data        mod            out_file       title
                  dcl_id      mode           out_or_temp    what_next
                  ext         n              ss             which_file
                  file        old_cmd        temp_file      fl
                  file_name   old_error      temp_var       undo_init
                  )
  ;;
  ;;  Get template file from file dialogue box.
  ;;
  (defun get_tfile (/ temp_var)
    (if (setq temp_var 
          (getfiled "Template File" temp_file "txt" 2)
      )
      (progn
        (setq temp_file temp_var)
        (set_tile "temp_file" temp_file)
      )
    )
  )
  ;;
  ;;  Gets output file from file dialogue box.
  ;;
  (defun get_ofile (/ temp_var)
    (if (= ftype "DXF")
      (setq out_ext "dxx")
      (setq out_ext "txt")
    )
    (if (setq temp_var 
          (getfiled "Output File" out_file out_ext 3)
        )
      (progn
        (setq out_file temp_var)
        (set_tile "out_file" out_file)
      )
    )
  )
  ;;
  ;;  Change the extension of the output file to the new extension
  ;;  (either .TXT or .DXX).
  ;;
  (defun new_ext (file_name ext mode / n ch)
    (setq n        2
          out_file (substr file_name 1 1)
    )
    (mode_tile "temp_file" mode)
    (mode_tile "select_temp_file" mode)
    (while (and (/= ch ".") (/= ch ""))
      (setq ch       (substr file_name n 1)
            n        (1+ n)
      )
      (if (or (= ch ".") (= ch ""))
        (setq out_file (strcat out_file "." ext))
        (setq out_file (strcat out_file ch))
      )
    )
    (set_tile "out_file" out_file)
  )

  ;;
  ;;  Remove extension of the output file (it's guaranteed to have one).
  ;;
  (defun rem_ext (which_file / n ch)
    (setq n    2
          file (substr which_file 1 1)
    )
    (while (/= ch ".")
      (setq ch (substr which_file n 1)          
            n  (1+ n)
      )
      (if (= ch ".")
        (setq file file)
        (setq file (strcat file ch))
      )
    )
  )
  ;;
  ;;  Test name of file for invalid name or extension.
  ;;
  (defun file_test (file_name tile_name)
    (if (= tile_name "temp_file")
      (setq ext "txt")
      (if (= ftype "DXF")
        (setq ext "dxx")
        (setq ext "txt")
      )
    )
    (cond
      ((wcmatch file_name "*`.")
        (setq file_name (strcat file_name ext))
      )
      ((or (= file_name "")(= file_name nil)) ; looks for empty file name
        (set_tile "error" "Invalid file name.")
        (mode_tile tile_name 2)
      )
      ((and (wcmatch file_name "*`.*")
           (not (wcmatch (strcase file_name) (strcat "*`." (strcase ext))))
       )
        (set_tile "error" (strcat "Extension changed to ." ext))
        (rem_ext file_name)
        (setq file_name (strcat file "." ext))
      )
      ((not (wcmatch file_name "*`.*"))
        (setq file_name (strcat file_name "." ext))
      )
      (T (set_tile "error" " "))
    )
    (set_tile tile_name file_name)
    file_name                         ; return the file name 
  )
  ;;
  ;; Is the output file name valid.
  ;; 
  (defun check_out()
    (if (findfile out_file)
      (if (not (out_exists))
        (progn (mode_tile "out_file" 2) nil)
        T
      )
      (progn
        (setq data (open out_file "w"))
        (if (not data)
          (progn
            (set_tile "error" "Invalid output file name.")
            (mode_tile "out_file" 2)
            nil                      ; return nil on error
          )
          (progn
            (close data)
            T
          )
        )
      )
    )
  )

  ;;
  ;;  Upon hitting Ok, checks validity of template file as well as output
  ;;  file. Also checks that the template file and output file don't have
  ;;  the same name. The ATTEXT command normally allows user to overwrite
  ;;  the template file thus rendering the template file useless. This
  ;;  will not let the user overwrite the template file under any
  ;;  circumstance.
  ;;
  (defun accept () 
    (cond
      ;; Check the output file name.
      ((= "" (setq out_file (file_test (get_tile "out_file") "out_file"))))

      ;; Check output file name for invalid characters.
      ((wcmatch out_file "*[] `#`@`?`*`~`[`,`'!%^&()+={}|;\"<>]*")
        (set_tile "error" "Invalid character in file name.")
        (mode_tile "out_file" 2)
      )

      ;; Check template file if not DXF.
      ((not (check_template)))

      ;; Check if the output file name is valid.
      ((and (= 1 fl)(not (check_out))))

      ;; If all the above is legit then quit.
      (T (done_dialog 1))
    )
  )

  ;;
  ;; Check fo the template file if not DXF.
  ;;
  (defun check_template()     
    (if (= ftype "DXF")
      T                              ;  DXF does not care about the template.
      (progn
        (if (/= (strcase out_file) (strcase temp_file))
          (progn
            (setq temp_file (file_test (get_tile "temp_file") "temp_file"))
            (if (not (findfile temp_file))
              (progn
                (set_tile "error" "File not found.")
                (mode_tile "temp_file" 2)
                nil
              )
              T
            )
          )
          (progn
            (out_temp)
            (mode_tile "out_file" 2)
            nil
          )
        )
      )
    )
  )

  ;;
  ;; Reset the error tile.
  ;;
  (defun rs_error()
    (set_tile "error" "")
  )

  ;;
  ;;  Alert dialogue, called on OK to get confirmation of overwriting File.
  ;;  Return T if Overwrite and nil if Cancel.
  ;;
  (defun out_exists()
    (if (not (new_dialog "out_exists" dcl_id)) (exit))
    (action_tile "yes" "(done_dialog 2)")
    (action_tile "cancel" "(done_dialog 0)")
    (if (= (start_dialog) 2)  T (setq redefine nil))
  )
  ;;
  ;;  Alert dialogue, called on OK to alert user that template file is about
  ;;  to be overwritten by output file.
  ;;
  (defun out_temp()
    (if (not (new_dialog "out_temp" dcl_id)) (exit))
    (action_tile "yes" "(done_dialog 2)")
    (if (= (start_dialog) 2)  T)
  )
  ;; 
  ;; Pop up the dialogue.
  ;;
  (defun ddattext_main()

    (setq what_next 2
          ftype "CDF"
          temp_file ""
          ss (ssget "i")
    )

    ;; main loop
    ;;
    (while (> what_next 1)
      (if (not (new_dialog "ddattext" dcl_id))
        (exit)
      )

      (set_tile "cdf" "1")

      (if (not temp_file) (setq temp_file ""))
      (set_tile "temp_file" temp_file)

      (if (not out_file)
        (setq out_file (strcat (getvar "dwgname") ".txt"))
      )
      (set_tile "out_file" out_file)

      (set_tile "how_many" (if ss 
                             (itoa (sslength ss))
                             (eval "0")
                           )
      )

      (action_tile "cdf" "(setq ftype \"CDF\")(new_ext out_file \"txt\" 0)")
      (action_tile "sdf" "(setq ftype \"SDF\")(new_ext out_file \"txt\" 0)")
      (action_tile "dxf" "(setq ftype \"DXF\")(new_ext out_file \"dxx\" 1)")
      (action_tile "selobjs" "(done_dialog 2)")
      (action_tile "temp_file" "(rs_error)(setq temp_file $value)")
      (action_tile "select_temp_file" "(rs_error)(get_tfile)")
      (action_tile "out_file" "(rs_error)(setq fl 1)(setq out_file $value)")
      (action_tile "select_out_file" "(rs_error)(setq fl 0)(get_ofile)")
      (action_tile "accept" "(accept)")
      (action_tile "cancel" "(done_dialog 0)")
      (action_tile "help" "(acad_helpdlg \"acad.hlp\" \"ddattext\")")
      (setq what_next (start_dialog))
      (cond
        ((= what_next 2)
          (prompt "\nSelect objects: ")
          (setq ss (ssget))
        )
      )
    )      ; end while loop

    (if (= what_next 1)
      (progn
        (rem_ext out_file)
        (command "_.attext")
        (if (and ss (not (zerop (sslength ss))))
            (command "_e" ss "")
        )
        (if (= ftype "DXF")        
          (command (substr ftype 1 1) file)
          (command (substr ftype 1 1) temp_file file)
        )
      )
    )
  )

  ;; Set up error function.
  (setq old_cmd (getvar "cmdecho")    ; save current setting of cmdecho
        old_error  *error*            ; save current error function
        *error* ai_error              ; new error function
  )

  (setvar "cmdecho" 0)

  (cond
     (  (not (ai_notrans)))                        ; transparent not OK
     (  (not (ai_acadapp)))                      ; ACADAPP.EXP xloaded?
     (  (not (setq dcl_id (ai_dcl "ddattext"))))  ; is .DCL file loaded?
     (T (ai_undo_push)
        (ddattext_main)                          ; proceed!
        (ai_undo_pop)
     )
  )

  (setq *error* old_error) 
  (setvar "cmdecho" old_cmd)

  (princ)
)

;;;-----------------------------------------------------------------------
(princ "  DDATTEXT loaded.")
(princ)
