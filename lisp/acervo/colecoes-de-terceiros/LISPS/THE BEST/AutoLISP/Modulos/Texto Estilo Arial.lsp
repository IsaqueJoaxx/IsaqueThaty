(defun texto^estilo^arial ()
  (if (= (tblsearch "style" "ARIAL") nil)
    (command "style" "arial" "arial.ttf" 0 0.85 "" "" "")
    (progn
      (if (= (getvar "textstyle") "ARIAL") nil
	(setvar "textstyle" "ARIAL")
      )
    )
  )
)

(princ)