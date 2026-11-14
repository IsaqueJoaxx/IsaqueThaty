; TIP999.LSP: BOM.LSP    Bill of Materials   (c)1994, Jimmy Dobbs

(defun C:BOM ()
   (setvar "cmdecho" 0)
   (setq N (* 0.5 PI) W PI S (* 1.5 PI) E 0 LOOP1 1 REP 1 QTY 1)
   (setq SCALE (getvar "dimscale"))           ;;;model space only
   (setq SIZE (getint "\nNumber of lines (items): "))
   (setq NUM (getint "\nReference number to begin with: "))
   (initget 1 "Y N")
   (setq ASSY (getkword "\nMultiple assemblies? (Y)es or (N)o: "))
   (if (= ASSY "Y") (setq QTY (getint "\nNumber of assemblies: ") LOOP2 nil) 
   (setq LOOP1 nil LOOP2 1))
   (setq PT1 (getpoint "\nSelect insertion point: "))
   (setq PT2 (polar PT1 e (* scale 0.4375)))  ;;;sets first column width
   (setq PT3 (polar PT2 e (* scale 0.4375)))  ;;;sets second (same as first)
   (setq PT4 (polar PT3 e (* scale 1.8125)))  ;;;sets third column width
   (setq PT5 (polar PT4 e (* scale 2.3125)))  ;;;sets fourth column width
   (setq PT6 (polar PT5 e (* scale 3)))       ;;;sets fifth column width
   (setq TXTSZ (* SCALE 0.125))               ;;;sets text height
   (setq HT (* SCALE 0.25))                   ;;;sets row height
   (command "line" PT1 PT6 "")
   (command "array" "l" "" "r" (+ 1 SIZE) "1" HT)
   (setq DIST (* HT SIZE))
   (command "line" PT1 (polar PT1 N DIST) "")
   (command "line" PT2 (polar PT2 N DIST) "")
   (command "line" PT3 (polar PT3 N DIST) "")
   (command "line" PT6 (polar PT6 N DIST) "")
   (setq ANG (angle PT2 (polar PT1 N HT)))
   (setq LONG (distance PT2 (polar PT1 N HT)))

   ;;;loop draws multiple assy section
   (while LOOP1       
      (command "line" PT1 (polar PT1 (- W ANG) LONG) "")
      (repeat (- QTY 1)
         (setq PT1 (polar PT1 ANG LONG))
         (setq DIST2 (- DIST (* HT REP)))
         (setq DIST3 (* SCALE 0.4375 REP))  ;;;same as first column
         (command "line" PT1 (polar PT1 (- W ANG) LONG) "")
         (command "line" PT1 (polar PT1 N DIST2) "")
         (command "line" PT1 (polar PT1 E DIST3) "")
         (setq REP (+ REP 1))
      )
      (command "array" "l" "" "r" (+ 2 (- SIZE QTY)) "1" HT)
      (command "line" PT2 (polar PT2 ANG (* LONG QTY)) "")
      (setq LOOP1 Nil)
   )

   ;;;draws an X in box
   (while LOOP2      
      (command "line" PT2 (polar PT2 ANG LONG) "")
      (command "line" PT1 (polar PT1 (- W ANG) LONG) "")
      (setq LOOP2 nil)
   )

   ;;;fills in numbers and dashes
   (setq PT7 (polar PT3 ANG (/ LONG 2)) PT8 PT7 SIZE1 (- SIZE 1))
   (repeat SIZE
      (command "text" "j" "m" PT7 TXTSZ "0" (itoa NUM))
      (setq NUM (+ NUM 1) PT7 (polar PT7 N HT))
   )
   (repeat QTY
      (command "text" "j" "m" (polar PT8 ANG LONG) TXTSZ "0" "-")
      (command "array" "l" "" "r" SIZE1 "1" HT)
      (setq PT8 (polar PT8 ANG LONG) SIZE1 (- SIZE1 1))
   )
   (command "text" (polar PT3 (- W ANG) TXTSZ) TXTSZ "0" "-")
   (command "array" "l" "" "r" SIZE "1" HT)
   (command "copy" "w" PT3 (polar PT4 N DIST) "" "M" PT3 PT4 PT5 "")
   (setvar "cmdecho" 1)
); end bom.lsp
