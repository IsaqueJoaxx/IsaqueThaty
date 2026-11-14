

(defun wait (s)
; waits s milleseconds
       (command "delay" (setq z s))
)
(defun graphwait ()
; goes to graphics screen and waits 1/4 second
       (graphscr)
       (wait 250)
)
(defun starttest (s)
; tell the user something, and wait for his RETURN. 
; i.e., print the statement s, tell them to Hit RETURN when ready
;  then go to graphics and wait 1/4 second
;
;  !!!! save the starting prompt in the test file?
;  !!!! number the tests for restart purposes
;  
       (if (= "N" comnd) (textscr))
       (print s)
       (print "Hit RETURN when ready to proceed")
       (getstring T)
       (graphwait)
       (repeat 3 (print "  "))
)
(defun getrslt (q)
; wait a little for the user to look at the result...then
; ask the user question q and do something with his answer
; always ask from the text screen...so we don't have problems with
; command prompt area off or dual screens with only one line cmmnd prompts
; 
; also save the result in the test result file
; and only accept Y y or N n   ask again if not
; 
;       
       (wait wtime)
       (if (= comnd "N")
           (command "textscr"))
       (setq z " ")
       (while (and (/= z "Y") (/= z "N"))
          (terpri)
          (setq z (strcase (getstring q)))
       )
       (if (null testf) (terpri)
         (progn
         (write-line q testf)
         (if (= "Y" z)
             (write-line  " PASS" testf)
             (write-line  " FAIL" testf))
         (write-line "    " testf)
         ))
)

(defun slowpick  ()
; returns "l" after awhile
       (repeat 800 (setq x "l"))
)
(defun userpick ()
       (terpri)
       (command "erase") (getstring t "Move the pick box, hit RETURN")
       (command)  
)
(defun dssetup ()
       (setvar "cmdecho" 1)
       (textscr) 
       ; run dscfg to update ascii.cfg file for status, prompt, menu config
       (command "shell" "dscfg")
       (setvar "cmdecho" 0)
       (setvar "blipmode" 0)
       (setq cfgf (open "ascii.cfg" "r"))
       (setq disp (read-line cfgf))
       (setq cfg (read-line cfgf))
       (setq sfgf (close cfgf))
       (setq status (substr cfg 1 1))
       (setq comnd (substr cfg 3 1))
       (setq menu (substr cfg 5 1))
       (terpri)
       (print disp)    
       (print "                             ")
       (print (strcat "Status line    " status))
       (print (strcat "Command prompt " comnd))
       (print (strcat "Menu area      " menu))
       (print "                             ")
       (setq x 0)
       (setq disp8 "")
       (repeat 8 (if (= " " (substr disp (setq x (1+ x)) 1))
                     (setq disp8 (strcat disp8 "-"))
                     (setq disp8 (strcat disp8 (substr disp x 1))))
       )
       (setq disp8 (strcase disp8 T))
)
(defun getver ()
       (setq midf (open "acad4.mid" "r"))
       (if (null midf) (setq ver "")
       (progn
       (setq x "  ")
         (while (and (/= "VERSION" x) (/= nil x))
               (setq x (strcase (substr (setq ver (read-line midf)) 1 7)))
         )
       ))
)
(defun startestout (testfn)
        (if (/= nil testf)
            (setq testf (close testf)))

        (setq testf (open testfn "w"))

        (write-line disp testf)
        (getver)
        (if (/= "" ver)
            (write-line ver testf))
        (write-line "      " testf)
        (write-line (strcat "Status line " status) testf)
        (write-line (strcat "Command prompt " comnd) testf)
        (write-line (strcat "Menu area " menu) testf)
        (write-line "      " testf)
        (write-line (rtos (getvar "cdate") 2) testf)
        (write-line "      " testf)

        (print (strcat "Output test result file: " testfn))
)

(defun C:SINGLE () 
       (load "single")
)
(defun C:DUAL ()
       (load "dual")
)
(defun C:COLORS ()
       (load "colors") 
)
(defun C:NCFG ()
; figure out current yyy cfg and change it to the next
       (setq cfglist (list
                     '(yyy "Y Y N")
                     '(yyn "Y N Y")
                     '(yny "Y N N")
                     '(ynn "N Y Y")
                     '(nyy "N Y N")
                     '(nyn "N N Y")
                     '(nny "N N N")
                     '(nnn "Y Y Y")
                          ))
       (if (or (null status) (null comnd) (null menu))
           (dssetup) 
           (progn
            (textscr)
            (terpri)
            (print (strcat "Current status, command, menu: " status " " comnd " " menu))
            (wait 2000)
           ))
       (setq x (read (strcat status comnd menu)))
       (setq nxtcfg (assoc x cfglist))
       (setq nxtcfg (car (cdr nxtcfg)))
       (print (strcat "Change status, command, menu to " nxtcfg))
       (command "shell" (strcat "dscfg " nxtcfg))
       (command "script" "outin")
)

