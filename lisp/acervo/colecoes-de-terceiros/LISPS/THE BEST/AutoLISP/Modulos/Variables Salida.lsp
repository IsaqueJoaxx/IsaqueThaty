(defun variables^salida ()
  (if osmode^old
    (setvar "osmode" osmode^old)
  )
  (if luprec^old
    (setvar "luprec" luprec^old)
  )
  (if cmdecho^old
    (setvar "cmdecho" cmdecho^old)
  )
  (if attdia^old
    (setvar  "attdia" attdia^old)
  )
  (if aunits^old
    (setvar  "aunits" aunits^old)
  )
  (command "clayer" "0")
  (princ)
)