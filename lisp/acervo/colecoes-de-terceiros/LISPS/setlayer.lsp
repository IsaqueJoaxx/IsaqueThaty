(defun c:SETLAYER (/ LAYER)
(setq LAYER
	(cdr
		(assoc 8
			(entget
				(car
					(entsel "Pick item for new layer "
))))))
(command "LAYER" "SET" layer "")
)

