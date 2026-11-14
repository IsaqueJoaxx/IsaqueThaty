
;********************************************************************
;                             MACRO
;  Lets a user define a sequence of commands which can be executed
;  by pressing a single key or a combination of keys. For example,
;  a user could define the key combination EL to execute the commands
;  erase last redraw. The program actually uses the user's input
;  to create a new AUTOCAD command in the form of a C: function.
;            K. Funk       Version 1.0          May 10,1986.
;********************************************************************

(setq MacList ())

;FUNCTION MACRO - creates keyboard macros.
(defun C:MACRO ()
   (setq Key (getstring "\nMacro Key(s): "))
   (setq Key (strcat "C:" Key))
   (cond ((/= Key "C:")
      (setq Mac (list 'Command))
      (setq Cmd "")
      (While (/= Cmd ".")
         (setq Cmd (getstring "\nMacro Def (. to end): "))
         (If (/= Cmd ".") (setq Mac (cons Cmd Mac)))
      )
      (setq Ans "")
      (While (NOT (member Ans '("Y" "y" "N" "n")))
         (setq Ans (getstring "\nCreate Macro (Y/N): ")))
      (cond ((member Ans '("Y" "y"))
         (setq Mac (reverse Mac))
         (setq MacDef (list 'defun (read Key) '() Mac))
         (setq MacList (cons MacDef MacList))
         (eval MacDef))
      ))
   )
)

;FUNCTION C:MACSAVE - saves macros defined in this session to disk.
(defun C:MACSAVE ()
   (setq Exist "T")
   (While (= Exist "T")
      (setq FN "")
      (While (= FN "")
         (setq FN (getstring "\nFilename: ")))
      (setq FN (strcat FN ".LSP"))
      (setq FU (open FN "r"))      ;check for existing file
      (cond ((NULL FU)
               (setq Exist "N"))
            (T
               (close FU)
               (setq Ans "")
               (While (NOT (member Ans '("Y" "y" "N" "n")))

                 (cond  (null FN))
                 (while (member ans ("QUIT" "Quit" "quit")))
                 )
  
                (setq Ans (getstring "\nFile Already Exists...ADD A MACRO(Y/N): ")))
               (If (member Ans '("Y" "y")) (setq Exist "N")))
      )  ;end cond
   ) ;end while
   (setq FU (open FN "a"))
   (foreach Macrow MacList (print Macrow FU))
   (close FU)
)
