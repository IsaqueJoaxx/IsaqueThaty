; DrafTools   [Version 1.00a] 4/6/94       
;
; ***************************************
; ****  Author:  Owen Wengerd        ****
; ****                               ****
; ****  Manu-Soft Computer Services  ****
; ****  P.O. Box 84                  ****
; ****  Fredericksburg, OH  44627    ****
; ****  (216) 695-5903               ****
; ****  Compu-Serve ID:  71324,3252  ****
; ***************************************


(defun new_acad_helpdlg
               (filename item / dcl_id path hlist ilist hdisp item err)

;Local Functions...

  (defun dt_gethelp (filename / path fh hlist fl hi)
    (and
      (setq path (dt_fpath filename))
      (setq fh (open path "r"))
      (progn
        (while (setq fl (read-line fh))
          (cond
            ( (= "\\ " (substr fl 1 2)))
            ( (= "\\" (substr fl 1 1))
              (setq hlist (append hlist (list (reverse hi))))
              (setq hi (list (substr fl 2)))
            )
            (1 (setq hi (cons fl hi)))
          )
        )
        (if hi (setq hlist (append hlist (list (reverse hi)))))
        (close fh)
      )
    )
    hlist
  )

  (defun dt_help_act (key value why / hi)
    (cond
      ( (= key "helptext")
        (set_tile key "")
        (mode_tile "helpitem" 2)
      )
      ( (= key "helpitem")
        (if (and (= why 1) (/= value ""))
          (if (setq hi (h_search value))
            (h_show (setq item (nth hi ilist)))
            (error (strcat "No help found for \"" value "\" in " filename))
          )
        )
      )
      ( (= key "index")
        (clear_err)
        (setq hi (h_index hdisp))
        (if hi (h_show (setq item hi)) (mode_tile "helpitem" 2))
      )
      ( (= key "top")
        (h_show (setq item ""))
      )
      ( (= key "next")
        (h_show
          (setq item
            (if hdisp (nth (1+ (h_search hdisp)) ilist) (car ilist))
          )
        )
      )
      ( (= key "previous")
        (h_show
          (setq item
            (if (= hdisp (car ilist)) "" (nth (1- (h_search hdisp)) ilist))
          )
        )
      )
    )
  )

  (defun h_show (item / hs)
    (start_list "helptext")
    (mapcar 'add_list
      (if (setq hdisp (htxt_search item))
        (progn
          (setq hdisp (car (setq hs (nth hdisp hlist))))
          (cdr hs)
        )
        (car hlist)
      )
    )
    (end_list)
    (set_tile "helpitem" (if item (strcase item) ""))
    (mode_tile "helpitem" 2)
    (mode_tile "previous" (if (and item (/= item "")) 0 1))
    (mode_tile "next" (if (= 1 (member item ilist)) 1 0))
    (clear_err)
  )

  (defun htxt_search (item / cnt pattern)
    (if (and item (/= item ""))
      (progn
        (setq cnt     0
              pattern (strcase item)
        )
        (while
          (and
            (not (wcmatch (car (nth cnt hlist)) pattern))
            (< (setq cnt (1+ cnt)) (length hlist))
          )
        )
        (if (< cnt (length hlist)) cnt)
      )
    )
  )

  (defun h_search (item / cnt pattern)
    (if (and item (/= item ""))
      (progn
        (setq cnt     0
              pattern (strcase item)
        )
        (while
          (and
            (not (wcmatch (nth cnt ilist) pattern))
            (< (setq cnt (1+ cnt)) (length ilist))
          )
        )
        (if (< cnt (length ilist)) cnt)
      )
    )
  )

  (defun h_index (hdisp / ii err track)
    (defun hi_showlist ()
      (setq pattern (strcase (get_tile "pattern"))
            track   nil
      )
      (start_list "helpindex")
      (foreach i (reverse ilist)
        (if (wcmatch (strcase i) pattern) (setq track (cons i track)))
      )
      (if track
        (mapcar 'add_list track)
        (progn
          (setq track '(nil))
          (add_list "No entries")
        )
      )
      (end_list)
      (clear_err)
    )
    (defun hi_hilist (value why)
      (if (= why 1)
        (setq ii (nth (atoi value) track))
        (if ii (done_dialog 1) (error "Nothing selected"))
      )
    )
    (defun hi_pattern ()
      (hi_showlist)
      (set_tile "helpindex" "0")
      (setq ii (car track))
    )
    (if (new_dialog "DT_INDEX" dcl_id)
      (progn
        (action_tile "pattern" "(if (= $reason 1) (hi_pattern))")
        (action_tile "helpindex" "(hi_hilist $value $reason)") 
        (action_tile "accept"
          "(if ii (done_dialog 1) (error \"Nothing selected\"))"
        )
        (action_tile "cancel" "(done_dialog 0)")
        (hi_showlist)
        (if hdisp (set_tile "helpindex" (itoa (h_search hdisp))))
        (if (> (start_dialog) 0) ii)
      )
      (alert
        (strcat "Index Dialog Definition is Corrupted."
                "\n     Cannot Display Index!"
        )
      )
    )
  )

  (defun error (msg)
    (set_tile "error" (setq err msg))
  )

  (defun clear_err ()
    (set_tile "error" "")
    (setq err nil)
  )


;Main Program...

  (if
    (and
      (setq filename
        (if (wcmatch filename "*`.*") filename (strcat filename ".hlp"))
      )
      (setq hlist (dt_gethelp filename))
      (setq ilist (acad_strlsort (mapcar 'car (cdr hlist))))
      (setq path (dt_fpath "DT_HELP.DCL"))
      (setq dcl_id (load_dialog path))
      (setq err  nil
            item ""
      )
      (new_dialog "DT_HELP" dcl_id "(dt_help_act $key $value $reason)")
    )
    (progn
      (h_show item)
      (start_dialog)
      (unload_dialog dcl_id)
    )
    (alert "Online help files not found!")
  )
)
