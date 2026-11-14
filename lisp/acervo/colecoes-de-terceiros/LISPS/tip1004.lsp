; TIP1004.LSP: WS.LSP   Draw Waterstop   (c)1994, Andrew Sloss

(defun *error* (msg)
 (command "layer" "s" LA1 "")
 (setvar "OSMODE" OSMODE1)
 (setvar "ORTHOMODE" ORTHO1)
 (setvar "SNAPANG" 0)
 (redraw)
 (princ MSG)
 (princ)
)
(DEFUN C:WS ( / LA1 OSMODE1 ORTHO1 PT1 RUN PT2 D1 A1 A1A D1B PT1B PT1A A2 D2
                PT3 PT4 PT5 PT6 PT7 PT8 PT9 PT10 PT11 PT12 PT13)
 (setq LA1 (getvar "CLAYER"))
 (setq OSMODE1 (getvar "OSMODE"))
 (setq ORTHO1 (getvar "ORTHOMODE"))
 (setvar "ORTHOMODE" 0)
 (if (not (tblsearch "layer" "joint"))
  (command "layer" "n" "str0101" "c" "r" "joint" "")
 );end if
 (if (not (tblsearch "layer" "wstop"))
  (command "layer" "n" "wstop" "c" "g" "str0301" "")
 );end if
 (if (not *ASLOPE) (setq *ASLOPE "1"))
 (princ "\nEnter <1> for straight key - <2> for sloped key < ")
 (princ *ASLOPE)
 (initget "1 2")
 (setq ASLOPE (getkword " >: "))
 (if (equal ASLOPE "") (setq ASLOPE NIL))
 (if (not ASLOPE) (setq ASLOPE *ASLOPE) (setq *ASLOPE ASLOPE))
 (initget 1)
 (setq PT1 (GETPOINT "\nPick joint start pt. on one side of wall using correct osnap: "))
 (setq RUN T)
 (while RUN
  (setvar "OSMODE" 128)
  (initget 1)
  (setq PT2 (getpoint PT1 "\nPick pt. on opp. side of wall: "))
  (setvar "OSMODE" 0)
  (setq D1 (distance PT1 PT2))
  (setq A1 (angle PT1 PT2))
  (setq A1A (+ (/ PI 2) A1))
  (setq D1B (/ D1 2.0))
  (setq PT1B (polar PT1 A1 D1B))
  (grdraw PT1 PT2 -1 1)
  (setvar "SNAPANG" A1A)
  (setvar "ORTHOMODE" 1)
  (initget 1)
  (setq PT1A (getpoint PT1B "\nPick side of joint for waterstop: "))
  (setvar "SNAPANG" 0)
  (setvar "ORTHOMODE" 0)
  (redraw)
  (setq A2 (angle PT1B PT1A))
  (if (< D1 24.0)
   (progn
    (setq D2 (/ D1 3.0))
    (setq PT3 (polar PT1 A1 D2))
    (setq PT4 (polar PT3 A2 2.0))
    (setq PT5 (polar PT4 A1 D2))
    (if (equal ASLOPE "2")
     (progn
      (setq PT4 (polar PT4 A1 0.50))
      (setq PT5 (polar PT5 (+ A1 pi) 0.50))
      (setq D2A ( / (distance PT4 PT5) 2.0))
     );end prOGN
    );end if
    (setq PT6 (polar PT3 A1 D2))
    (if (equal ASLOPE "2")
     (setq PT7 (polar PT4 A1 D2A))
     (setq PT7 (polar PT4 A1 (/ D2 2.0)))
    );end if
    (setq PT8 (polar PT7 (+ A1 (/ PI 2)) 3.0))
    (setq PT9 (polar PT7 (- A1 (/ PI 2)) 3.0))
   );end progn
   (progn
    (setq D2 (/ (- D1 8.0) 3.0))
    (setq PT3 (polar PT1 A1 D2))
    (setq PT4 (polar PT3 A2 2.0))
    (setq PT5 (polar PT4 A1 4.0))
    (if (equal ASLOPE "2")
     (progn
      (setq PT4 (polar PT4 A1 0.50))
      (setq PT5 (polar PT5 (+ A1 pi) 0.50))
     );end progn
    );end if
    (setq PT6 (polar PT3 A1 4.0))
    (setq PT7 (polar PT6 A1 D2))
    (setq PT8 (polar PT7 A2 2.0))
    (setq PT9 (polar PT8 A1 4.0))
    (if (equal ASLOPE "2")
     (progn
      (setq PT8 (polar PT8 A1 0.50))
      (setq PT9 (polar PT9 (+ A1 pi) 0.50))
     );end progn
    );end if
    (setq PT10 (polar PT7 A1 4.0))
   );end progn
  );end if
  (command "layer" "s" "joint" "")
  (if (< D1 24.0)
   (command "pline" pt1 "w" "0" "0" PT3 PT4 PT5 PT6 PT2 "")
   (command "pline" pt1 "w" "0" "0" PT3 PT4 PT5 PT6 PT7 PT8 PT9 PT10 PT2 "")
  );end if
  (command "layer" "s" "wstop" "")
  (if (< D1 24.0)
   (command "pline" pt8 pt9 "") 
   (progn
    (setvar "OSMODE" 2)
    (initget 1)
    (setq PT11 (getpoint "\nPick key with waterstop: "))
    (setvar "OSMODE" 0)
    (setq PT12 (polar PT11 (+ A1 (/ PI 2.0)) 3.0))
    (setq PT13 (polar PT11 (- A1 (/ PI 2.0)) 3.0))
    (command "pline" PT12 PT13 "")
   );end progn
  );end if
  (setq PT1 (getpoint "\nPick joint start pt. on one side of wall using correct osnap or  return to exit: "))
  (if (not PT1) (setq RUN nil))
 );end while
 (setvar "ORTHOMODE" ORTHO1)
 (setvar "OSMODE" OSMODE1)
 (command "layer" "s" LA1 "")
 (princ)); end ws.lsp
