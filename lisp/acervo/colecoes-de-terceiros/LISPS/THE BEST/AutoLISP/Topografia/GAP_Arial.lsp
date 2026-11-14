(defun GAP_Arial ()
  (if (= (tblsearch "style" "GAP_Arial") nil)
    (command "style" "GAP_Arial" "arial.ttf" 0 1 "" "" "")
    (progn
      (if (= (getvar "textstyle") "GAP_Arial")
	(alert "El estilo ya existe")
	(setvar "textstyle" "GAP_Arial")
      )
    )
  )
)

(princ)