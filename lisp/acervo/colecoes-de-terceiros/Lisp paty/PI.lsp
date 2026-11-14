(defun c:pi (/	   plist n     point teste x	 y     x1    x2
	     x3	   y1	 y2    y3    y4	   piw	 nome  xt    piname
	    )

  (defun *error* (msg)
    (if
      (or
	(= msg "Function cancelled")
	(= msg "quit / exit abort")
      )
       (progn
	 (command "ucs" "delete" "pi")
	 (princ)
	 (setq *error* nil)
       )
    )
  )

  (setvar "cmdecho" 0)
  (setq nome 0)
  (command "ucs" "save" "pi")
  (defun selpol	()
    (setq plist (entget (car (entsel "Selecione a Polilinha:"))))
    (setq n 0)
    (setq point nil)
    (while (= teste nil)
      (if (= 10 (car (nth n plist)))
;;;	(progn
;;;	  (setq n (1+ n))
	  (setq teste 1)
;;;	)
	(setq n (1+ n))
      )
    )
    (setq teste nil)
    (while (/= 210 (car (nth (+ 4 n) plist)))
      (if (= 10 (car (nth n plist)))
	(progn
	  (setq point (trans (cdr (nth n plist)) 0 1))
	  (setq n (1+ n))
	  (if (/= 210 (car (nth (+ 4 n) plist)))
	    (setq xt 1)
	  )
	  (inspi)
	)
	(setq n (1+ n))
      )
    )
  )

  (defun menu1 ()
    (initget "Polilinha")
    (setq point (getpoint "\nIndique um Ponto ou [Polilinha]"))
    (if	(= point nil)
      (selpol)
      (progn
	(setq xt 1)
	(inspi)
      )
    )
  )

  (defun inspi ()
    (terpri)
    (if	(= xt 1)
      (progn
	(setq dz (getvar "dimzin"))
	(setvar "dimzin" 1) 
	(setq x (rtos (nth 0 point) 2 3))
	(setq y (rtos (nth 1 point) 2 3))
	(setvar "dimzin" dz)
	(setq piw (trans point 1 0))
;;;	(if (/= nome nil)
;;;	  (setq
;;;	    piname
;;;	     (getstring
;;;	       (strcat "\nDigite o nome do PI <" (itoa (+ 1 nome)) ">:")
;;;	     )
;;;	  )
;;;	  (setq piname (getstring (strcat "\nDigite o nome do PI:")))
;;;	)
;;;	(if (/= piname "")
;;;	  (setq nome (atoi piname))
	  (setq nome (+ 1 nome))
;;;	)
	(command "ucs" "")
	(command "insert"
		 "coord.dwg"
		 piw
		 ""
		 ""
;;;		 pause
		 ""
		 (strcat "PI " (rtos nome 2 0))
		 (strcat "N=" y)
		 (strcat "E=" x)
	)
	(command "ucs" "restore" "pi")
      )
    )
  )
  (menu1)
  (princ)
  (command "ucs" "del" "pi")
  (setvar "cmdecho" 1)
  (princ)
)

(princ "\n.")
(princ
  "\n* * * Digite \"PI\" para executar o programa * * *"
)
(princ)