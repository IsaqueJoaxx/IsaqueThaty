;From CADENCE Magazine November , 1991
;Page 122

(defun C:Z ()
  (setq lpt (getpoint "\nOrigin <Last Point>  "))
  (if (/= nil lpt) (command "Id" lpt))
  (command "UCS" "O" "@")
  (setq e 1)
  (while e
  (setq b (getstring))
  (if (= b "") (setq b b1))
  (setq b (strcase b))
  (if (= b "Y") (command "UCS" "X" "-90"))
  (if (= b "X")  (command "UCS" "Y" "90"))
  (if (= b "T")
    (progn
      (command "UCS" "Z" "90")
      (setq b b1)
    )
  )
  (if (= b "S") (setq e nil))
  (setq b1 b)
  )
)

