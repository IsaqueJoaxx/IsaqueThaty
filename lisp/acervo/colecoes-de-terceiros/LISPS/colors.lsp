

(dssetup)
(print "                                ")
(print "* * * COLOR handling tests * * *")
(print "                                ")

; open test result output file...name NNNdisp.lay
(setq testfn (strcat disp8 ".clr"))
(startestout testfn)
(terpri)
(print "When the command prompt is only one line or is configured OFF,")
(print "test questions will be asked from the TEXT screen.")
(print "So, if you need to look at the graphics screen again, ")
(print "or the text screen, use flipscreen BEFORE answering the question")
(terpri)
(print "Yes/No questions must be answered with 'y' 'Y' 'n' or 'N'")
(print "Any other response will result in the prompt being repeated")
(terpri)
(print "When a prompt says Hit RETURN, you can only continue by")
(print "depressing the RETURN key")
(terpri)
(print "* * Hit RETURN when ready to go * *")
(getstring cr)

; the first test should check that config of status, comnd and menu worked


; some tests specifically for color...

(starttest "We will now insert CHROMA to check the 256 COLORS")
(command "insert"  "chroma" "0,0" "1" "1" "0")
(command)
(command "zoom" "e")
(getrslt "Did ALL 256 COLORS show up properly? ")

; check highlighting w erase
(starttest "Now we'll HIGHLIGHT everything")    
(command "erase" "w" (getvar "extmin") (getvar "extmax") (slowpick))
(command)
(getrslt "Did ALL the COLORS HIGHLIGHT correctly? ")

; check erase
(starttest "Now we'll ERASE ALL the colors")
(command "erase"  "w" (getvar "extmin") (getvar "extmax")  "")
(getrslt "Did ALL the COLORS ERASE correctly? (i.e. is the dwg area blank)")

; check oops
(starttest "Make sure OOPS re-creates ALL COLORS correctly")
(command "oops")
(getrslt "Did ALL the COLORS come back (OOPS) properly? ")

; check flip screens after status command
(starttest "Check for correct COLORS after STATUS, GRAPHSCR")
(command "status")
(graphscr)
(wait 1000)
(getrslt "Are ALL COLORS correct after STATUS, GRAPHSCR? ")

; check SHELL and colors 
(starttest "Check for correct COLORS after SHELL")
(command "SHELL" "DIR *.")
(graphscr)
(wait 1000)
(getrslt "Are ALL COLORS correct after SHELL? ")

; get rid of all those colors....and zoom a
(command "erase" "l" "")
(command "zoom" "a")

; check axis ticks not drawn in last color
; have tester input a supported color
(starttest "Check that AXIS ticks are NOT LAST COLOR")
;change layer 0 color 
(setq x 0)
(while (= 0 x)
       (setq x (getint "Enter a color # (other than 0): ")) 
       (if (null x) (setq x 0)))
(command "layer" "color")
(command x "0" "")
(command)
;draw something then turn axis on
(graphwait)
(command "line" (getvar "viewctr") "@2<0" "")
(command "axis" "on")
(command "erase" "l" "")
(getrslt "Did the AXIS ticks draw in the PROPER COLOR? (NOT LAST COLOR)")

; check xhair colors?
(starttest "Check that CROSS-HAIRS are NOT LAST COLOR")
(command "line" (getvar "viewctr") "@2<0" "")
(graphwait)
(command "erase" "l" "")
(wait 1000)
(getrslt "Are the CROSS-HAIRS the correct COLOR (NOT LAST COLOR)? ")

; check grid dots not drawn in last color
(starttest "Check that GRID dots are NOT LAST COLOR")
(command "line" (getvar "viewctr") "@2<0" "")
(wait 250)
(command "grid" "on")
(command "erase" "l" "")
(getrslt "Did the GRID dots draw in the PROPER COLOR? (NOT LAST COLOR)")


; check drawing something in the background color
; is this needed...as all 256 colors are drawn w insert chroma?

(starttest "Now we'll DRAW using the BACKGROUND COLOR")

;define layer 0 color as background color - and set it
(setq x 0)
(while (= 0 x)
       (setq x (getint "Enter a color # (other than 0): "))
       (if (null x) (setq x 0)))
(command "layer" "color")
(command x "0" "")

; now draw something fairly large with it - wide pline half height of screen
(setq w (/ (getvar "viewsize")  2) )
(command "pline" (getvar "viewctr") "w" w w "@2<0" "")
(getrslt "Did a wide pline appear in a COLOR other than the BACKGROUND COLOR? ")
                
;make sure highlighting and erase work ok
(starttest "Now make sure this PLINE HIGHLIGHTS and ERASE properly")
(command "erase" "l" (slowpick)  "")
(getrslt "Did the PLINE HIGHLIGHT and ERASE properly? ")



(write-line (rtos (getvar "cdate") 2) testf)
(write-line (strcat "end " disp) testf)
(setq testf (close testf))
(print "* * * END * * *")        
