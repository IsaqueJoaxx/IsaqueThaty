;;Edit Text enbedded in a Block using a dialog box interface.
; (c) 1994 - Wilsoft Development, James R. Wilson
(defun reset ()
  (setq flg 1)
  (done_dialog)
)

(setq dcl_id (load_dialog "MODBKTXT.DCL"))
(while
  (setq bltxt (nentsel "\nSelect Block Text to modify: \n<Return> to end."))
  (setq entdata (entget (car bltxt)) btxt (cdr (assoc 1 entdata)) ostr btxt)
  (setq flg 0)
  (if (not (new_dialog "MODBKTXT" dcl_id)) (exit))
  (set_tile "btxt" btxt)
  (mode_tile "btxt" 2)
  (action_tile "btxt" "(setq btxt $value)")
  (action_tile "accept" "(done_dialog)")
  (action_tile "cancel" "(reset)")
  (start_dialog)
  (if (/= flg 1)
    (progn
      (entmod (subst (cons 1 btxt)(assoc 1 entdata) entdata))
      (if (setq bltxt (car (reverse (cadddr bltxt)))) (entupd bltxt))
    )
  )
)
(unload_dialog dcl_id)
(setq bltxt nil entdata nil btxt nil flg nil dcl_id nil)
(princ "(c) 1994 - Wilsoft Development, James R. Wilson")
