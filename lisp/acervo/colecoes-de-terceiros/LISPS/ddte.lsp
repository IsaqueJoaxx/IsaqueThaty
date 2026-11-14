;;ษออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออป
;;บProgram name:       DDTE.LSP                                              บ
;;บInitial Author:     Michael Jenkins                                       บ
;;บDescription:        This is a dialog box for editing multiple lines of    บ
;;บ                    text. The selections will automatically be filtered.  บ
;;บ                    Pressing <Enter> will automatically bring up the next บ
;;บ                    piece of text if it's not the last piece, otherwise itบ
;;บ                    exits the dialog and updates the pieces of text.      บ
;;ศออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออออผ


;;; ===================== load-time error checking ============================
;;;

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
      (ai_abort "DDTE"
         (strcat "Can't locate file AI_UTILS.LSP."
   "\n Check support directory.")))

   (  (eq "failed" (load "ai_utils" "failed"))            ; load it
   (ai_abort "DDTE" "Can't load file AI_UTILS.LSP"))
)

(if (not (ai_acadapp))               ; defined in AI_UTILS.LSP
   (ai_abort "DDTE" nil)        ; a Nil <msg> supresses
)                                    ; ai_abort's alert box dialog.

;;; ==================== end load-time operations ===========================
;;
;;The main function...
;;
(defun C:DDTE
   (
      / _update _previous te_list _next te_done
      te_cancel index te_old sset set_len _undo _build
   )

   ;build list of text
   (defun _build (/ tb_index)
      ;preset variables
      (setq tb_index 0)
      ;initialize list
      (setq te_list nil)
      ;build the list
      (while (/= tb_index (sslength sset))
         (if (= te_list nil)
            (setq te_list
               (list
                  (cons tb_index
                     (cdr
                        (assoc 1
                           (entget
                              (ssname sset tb_index)
                           )
                        )
                     )
                  )
               )
            )
            (setq te_list
               (cons
                  (cons tb_index
                     (cdr
                        (assoc 1
                           (entget
                              (ssname sset tb_index)
                           )
                        )
                     )
                  )
               te_list)
            )
         )
         (setq tb_index (1+ tb_index))
      )
   )

   ;update the list
   (defun _update ()
      (setq te_list
         (subst
            (cons index
               (get_tile "te")
            )
            (assoc index te_list)
            te_list
         )
      )
   )

   ;go to the previous piece of text
   (defun _previous ()
      (_clear)
      (_update)
      (setq index (1- index))
      (set_tile "te" (cdr (assoc index te_list)))
      (if (= index 0)
         (mode_tile "previous" 1)
      )
      (_index)
   )

   ;go to the next piece of text
   (defun _next ()
      ;check to see if this is the last
      (if (< (1+ index) sset_len)
         ;go to next piece if not last
         (progn
            (_clear)
            (_update)
            (setq index (1+ index))
            (set_tile "te" (cdr (assoc index te_list)))
            (if (= index (- sset_len 1))
               (mode_tile "next" 1)
            )
         )
         ;leave if they pressed enter on last piece
         (progn
            (_update)
            (setq #te_loc (done_dialog 1))
         )
      )
      (_index)
   )

   ;clear the grayed out tiles
   (defun _clear ()
      (if (/= sset_len 1)
         (progn
            (mode_tile "next" 0)
            (mode_tile "previous" 0)
         )
      )
   )

   ;pull up the help screen
   (defun _help ()
      (acad_helpdlg "heaven" "ddte")
   )

   ;restore current text string
   (defun _undo ()
      (set_tile "te" (cdr (assoc 1 (entget (ssname sset index)))))
      (_update)
   )

   ;go back to all of the original text
   (defun _undoall ()
      (_build)
      (set_tile "te" (cdr (assoc index te_list)))
   )

   ;convert current string to upper case
   (defun _upper ()
      (set_tile "te" (strcase (get_tile "te")))
      (_update)
   )

   ;convert current string to lower case
   (defun _lower ()
      (set_tile "te" (strcase (get_tile "te") T))
      (_update)
   )

   (defun _index ()
      (set_tile "error"
         (strcat (itoa (1+ index)) " of " (itoa sset_len))
      )
   )

   (setq
      te_cancel nil
      index 0
   )

   ;get the selection set
   (if (= sset nil)
      (while (= (setq sset (ssget (list (cons 0 "TEXT")))) nil))
   )

   ;set up the original list
   (_build)

   ;set up the dialog identification
   (setq id (load_dialog "ddte.dcl"))
   (if
      (setq file
         (open "c:/ddte.loc" "r")
      )
      (progn
         (setq #te_loc
            (list
               (atoi
                  (read-line file)
               )
               (atoi
                  (read-line file)
               )
            )
         )
         (close file)
      )
   )

   (new_dialog "ddte" id "" #te_loc)

   ;preset variables
   (setq sset_len (sslength sset))
   ;(setq index 0)

   ;set up the dialog box
   (set_tile "te" (cdr (assoc 1 (entget (ssname sset index)))))

   ;gray out appropriate tiles
   (if (/= sset_len 1)
      (mode_tile "previous" 1)
      (progn
         (mode_tile "previous" 1)
         (mode_tile "next" 1)
         (mode_tile "undoall" 1)
      )
   )

   ;tell them where they are
   (_index)

   ;setup callbacks
   (action_tile "cancel" "(setq #te_loc (done_dialog 0))")
   (action_tile "accept" "(_update)(setq #te_loc (done_dialog 1))")
   (mapcar
      '(lambda (x)
         (action_tile x (strcat "(_" x ")"))
      )
      (list "help" "undoall" "upper" "lower" "undo" "previous" "next")
   )

   ;go until they quit
   (setq te_cancel (start_dialog))
   (unload_dialog id)

   (setq file (open "c:/ddte.loc" "w"))
   (write-line (itoa (car #te_loc)) file)
   (write-line (itoa (cadr #te_loc)) file)
   (close file)

   ;process the result
   (if (/= te_cancel 0)
      (progn
         ;preset variables
         ;(setq sset_len (sslength sset))
         (setq index 0)
         ;build list of text items
         (while (/= index sset_len)
            (entmod
               (subst
                  (cons 1 (cdr (assoc index te_list)))
                  (assoc 1 (entget (ssname sset index)))
                  (entget (ssname sset index))
               )
            )
            (setq index (1+ index))
         )
      )
   )
   (princ)
)
;;============================== The End =====================================
