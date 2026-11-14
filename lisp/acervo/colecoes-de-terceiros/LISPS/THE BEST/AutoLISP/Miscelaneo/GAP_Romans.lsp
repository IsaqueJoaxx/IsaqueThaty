(defun GAP_Romans ()
  (if (= (tblsearch "style" "GAP_Romans") nil)
    (command "style" "GAP_Romans" "romans.shx" 0 1 "" "" "" "")
    (progn
      (if (= (getvar "textstyle") "GAP_Romans")
	(alert "El estilo ya existe")
	(setvar "textstyle" "GAP_Romans")
      )
    )
  )
)

(princ)