(defun texto^estilo^romans ()
  (if (= (tblsearch "style" "ROMANS") nil)
    (command "style" "romans" "romans.shx" 0 0.85 "" "" "" "")
    (progn
      (if (= (getvar "textstyle") "ROMANS")
	()
	(setvar "textstyle" "romans")
      )
    )
  )
)

(princ)