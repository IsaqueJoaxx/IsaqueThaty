;    This routine changes all block entities to layer 0 and color bylayer
;    then reblock the enties.  A regen is required to view the changes.

(setq setvs (list (cons "limcheck" (getvar "limcheck"))
				  (cons "expert" (getvar "expert"))
				  (cons "regenmode" (getvar "regenmode"))
				  (cons "osmode" (getvar "osmode"))
			)
)
(setvar "limcheck" 0)
(setvar "cmdecho" 0)
(setvar "regenmode" 0)
(setvar "osmode" 0)


(defun c:blk20 ( / tbldata bnam ex ss1 ent last1)
(prompt "\n\n\nWorking!!!")
	(setq tbldata (tblnext "block" T))
	(setq bnam (cdr (assoc 2 tbldata)))
	(if (= (substr bnam 1 1) "*")
		(progn
		(setq tbldata (tblnext "block"))
		(setq bnam (cdr (assoc 2 tbldata)))
		)
		(progn
		(setq ex (strcat "*"bnam))
		(SETQ LAST1 (ENTLAST) SS1 (SSADD))
		(command "insert" ex (GETVAR "VIEWCTR") 1 0) 
		(setq ent (entnext last1))
			(while ent
				(setq ss1 (ssadd ent ss1))
				(setq ent (entnext ent))
			)
	 	(command "change" SS1 "" "p" "c" "bylayer" "la" "0" "")
	 	(command "block" bnam (GETVAR "VIEWCTR") SS1 "")
		)
	)
	(while (setq tbldata (tblnext "block"))
		   (setq bnam (cdr (assoc 2 tbldata)))
			(if (= (substr bnam 1 1) "*")
				(progn
				(setq tbldata (tblnext "block"))
				(setq bnam (cdr (assoc 2 tbldata)))
				)
				(progn
				(setq ex (strcat "*"bnam))
				(setq last1 (entlast) ss1 (ssadd))
				(command "insert" ex (getvar "viewctr") 1 0) 
				(setq ent (entnext last1))
					(while ent
						(setq ss1 (ssadd ent ss1))
						(setq ent (entnext ent))
					)
	 			(command "change" SS1 "" "p" "c" "bylayer" "la" "0" "")
	 			(command "block" bnam (GETVAR "VIEWCTR") SS1 "")
				)
			)
	)
(foreach cnt setvs (setvar (car cnt)(cdr cnt)))
(prompt "\n\nUntil next time!")
(princ)
)
