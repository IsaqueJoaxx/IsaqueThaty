(vmon)
(defun dtr (a) (* pi (/ a 180.0)))
(defun rtd (a) (/ (* a 180.0) pi))
(defun c:symline (/ ll ticker j count numxs linang d firstx rotang k brkf brk1 brk2 e1 opnt)
    (graphscr)
    (setvar "cmdecho" 0)

; IF NOT USED SINCE LOADED, DO THIS IS "if" FOR INITIALIZATION
    (if (= initvar nil)
      (progn
        (setq blknm nil)
        (setq blksz nil)
        (setq blksp nil)
        (setq brkvar "ON")
        (prompt "\n\"SYMLINE.LSP\",\(version 1.0\)\; by Duane Witherwax")
        (terpri)
        (setq h 1)
        (while h
          (prompt "Block name <>: ")
          (initget 1)
          (setq blknm (strcase (getstring)))
          (if (= blknm "")
            (progn
              (prompt "You must respond with name of block!") (terpri)
            )
          )
          (setq tbdata (tblsearch "BLOCK" blknm))
          (if (= tbdata nil) (setq tbdata (findfile (strcat blknm ".DWG"))))
          (if (/= tbdata nil) (setq h nil)
            (progn
              (prompt "\nBlock ")
              (prompt blknm)
              (prompt " does not exist.") (terpri)
              (prompt "Please try again.") (terpri)
              (setq blknm nil)
              (terpri)
            );End progn
          );End if
        )
        
        (prompt "Enter size for symbols <>: ")
        (initget 1)
        (setq blksz (getreal)) (terpri)
        
        (prompt "Enter spacing for symbols <>: ")
        (initget 1)
        (setq blksp (getreal)) (terpri)
        
        (setq op " <")
        (setq cl ">: ")
        (setq brkvarsav brkvar)
        (setq brkvardef (strcat op brkvar cl))
        (prompt "Break Line ON/OFF") (prompt brkvardef)
        (initget "ON OFF")
        (setq brkvar (getstring)) (terpri)
        (if (= brkvar "") (setq brkvar brkvarsav))
        
      )
    )

; HERE IS ACTUAL START OF WORK
; LOOP UNTIL WE GET A FIRSTPOINT
    (setq j 1)
    (while j
      (setq firstptsav firstpt)
      (initget "P B")
      (setq firstpt (getpoint "Properties/Block_last/<From point>: "))
        (terpri)
      
      (if (/= firstpt nil) (setq j nil))
      
      (if (= firstpt nil)
        (progn
          (setq firstpt endpt)
          (setq j nil)
        );End progn
      );End if
      
      (if (= firstpt "P")
        (progn
          (setq op " <")
          (setq cl ">: ")
          (setq blknmsav blknm)
          (setq blknmdef (strcat op blknm cl))
          (setq h 1)
          (while h
            (prompt "Block name") (prompt blknmdef)
            (initget " ")
            (setq blknm (strcase (getstring)))
            (if (= blknm "")
              (progn
                (setq blknm blknmsav)
                (setq h nil)
              )
            )
            (setq tbdata (tblsearch "BLOCK" blknm))
           (if (= tbdata nil) (setq tbdata (findfile (strcat blknm ".DWG"))))
            (if (/= tbdata nil) (setq h nil)
              (progn
                (prompt "\nBlock ")
                (prompt blknm)
                (prompt " does not exist!") (terpri)
                (prompt "Please try again.") (terpri)
                (setq blknm blknmsav)
                (terpri)
              );End progn
            );End if
          )
          
          (setq blkszsav blksz)
          (setq blksztmp (rtos blksz))
          (setq blkszdef (strcat op blksztmp cl))
          (prompt "Enter size for symbols") (prompt blkszdef)
          (initget " ")
          (setq blksz (getreal)) (terpri)
          (if (= blksz nil) (setq blksz blkszsav))
          
          (setq blkspsav blksp)
          (setq blksptmp (rtos blksp))
          (setq blkspdef (strcat op blksptmp cl))
          (prompt "Enter spacing for symbols") (prompt blkspdef)
          (initget " ")
          (setq blksp (getreal)) (terpri)
          (if (= blksp nil) (setq blksp blkspsav))
          
          (setq op " <")
          (setq cl ">: ")
          (setq brkvarsav brkvar)
          (setq brkvardef (strcat op brkvar cl))
          (prompt "Break Line ON/OFF") (prompt brkvardef)
          (initget "ON OFF")
          (setq brkvar (getstring)) (terpri)
          (if (= brkvar "") (setq brkvar brkvarsav))
          
          (setq j 1)
        );End progn
      );End if
      
      (if (= firstpt "B")
        (progn
          (setq firstpt firstptsav)
          (setq bfl 1)
          (setq j nil)
        );End progn
      );End if
    );End while

; LOOP FOR DRAWING SYMLINE
; LOOPS UNTIL ENDPOINT IS "Close" OR "NIL"
    (setq m 1)
    (setq strtpt firstpt)
    (if (/= bfl 1) (setq blklist nil) (setq m nil))
    (while m
;     (if (= bfl 1) (setq m nil)); Checks for last option. Skips below.
      (initget "C")
      (setq endpt (getpoint strtpt "To point: ")) (terpri)
      
      (if (= endpt nil)
        (progn
          (setq endpt strtpt)
          (setq m nil)
        );End progn
      );End if
      
      (if (= endpt "C")
        (progn
          (setq endpt firstpt)
          (setq m nil)
        );End progn
      );End if
      
      (if (/= endpt strtpt)
        (progn
          (setq ll (distance strtpt endpt))
          (setvar "highlight" 0)
          
          (setq ticker 0)
          (setq j 1)
          (setq count 0)
          (while j
            (setq ticker (+ ticker blksp))
            (setq count (+ count 1))
            (if (> ticker ll) (setq j nil))
          );End while j
          (setq numxs count)
          (setq linang (angle strtpt endpt))
          (setq d (/ (- ll (* (- numxs 1) blksp)) 2))
          (if (< d blksp)
            (progn
              (setq numxs (- numxs 1))
              (setq d (/ (- ll (* (- numxs 1) blksp)) 2))
            );End progn
          );End if
          
          (setq firstx (polar strtpt linang d))
          (setq rotang (rtd linang))
          
          (if (<= numxs 0)
            (progn
              (command "LINE" strtpt endpt "")
              (if (= blklist nil)
                (progn
                  (setq e1 (entlast))
                  (setq blklist (list e1))
                );End progn Then
                (progn
                  (setq e1 (entlast))
                  (setq blklist (append blklist (list e1)))
                );End progn Else
              );End if
            );End progn then
            (progn
              (command "LINE" strtpt endpt "")
              (setq k 0)
              (if (= blklist nil)
                (progn
                  (setq e1 (entlast))
                  (setq blklist (list e1))
                );End progn Then
                (progn
                  (setq e1 (entlast))
                  (setq blklist (append blklist (list e1)))
                );End progn Else
              );End if
              (setq brkf firstx)
              (while (< k numxs)
                (progn
                   (command "INSERT" blknm brkf blksz blksz rotang)
                   (setq e2 (entlast))
                   (setq blklist (append blklist (list e2)))
                   (setq brk1 (polar brkf (dtr (- rotang 180)) (/ blksz 2)))
                   (setq brk2 (polar brkf (dtr rotang) (/ blksz 2)))
                   (if (or (= brkvar "ON") (= brkvar "on"))
                     (progn
                       (command "BREAK" e1 brk1 brk2)
                       (setq e1 (entlast))
                       (setq blklist (append blklist (list e1)))
                     );End progn
                   );End if
                   (setq k (+ k 1))
                   (setq brkf (polar brkf (dtr rotang) blksp))
                );End progn
              );End while
            );End progn else
          );End if
          (setq strtpt endpt)
        );End progn
      );End if
    );End while m

; ROUTINE THAT BLOCKS WHOLE OF LAST USE
    (setq op " <")
    (setq cl ">: ")
    (setq blkyn "NO")
    (setq blkynsav blkyn)
    (setq blkyndef (strcat op blkyn cl))
    (prompt "Create block? \(Yes/No\)") (prompt blkyndef)
    (initget "YES NO yes no Y N y n")
    (setq blkyn (getstring)) (terpri)
    (if (= blkyn "") (setq blkyn blkynsav))

    (if (or (= blkyn "Y") (= blkyn "y") (= blkyn "YES") (= blkyn "yes"))
      (progn
        (setvar "highlight" 0)
        (setq j 1)
        (while j
          (setq newblknm (strcase (getstring "\nBlock name: ")))
          (setq tbdata (tblsearch "BLOCK" newblknm))
          (if (= tbdata nil) (setq j nil)
            (progn
              (prompt "\nBlock ")
              (prompt newblknm)
              (prompt " already exists.") (terpri)
              (prompt "Please try again.") (terpri)
            );End progn
          );End if
        )
        (prompt "\nCreating block ") (prompt newblknm) (prompt " ...")
        (terpri)
        (command "BLOCK" newblknm firstpt)
        (foreach item blklist (command item))
        (command "")
        (command "INSERT" newblknm firstpt 1 1 0)
        (setq blklist nil)
      )
    )
    (setq bfl nil); Sets Break For Last to nil.
    (setq initvar 1); Sets use as having been initialized.
    (setvar "highlight" 1)
    (setvar "cmdecho" 1)
(princ)
);Close defun
