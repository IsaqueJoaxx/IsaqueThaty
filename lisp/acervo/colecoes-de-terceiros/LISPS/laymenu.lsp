;laymenu.lsp - by c.l.smith, *.lsp, houston,TX
;new version of laymenu, providing a dynamic screen menu for layer selection.
;reads layer table (on first call or after a "reset") and displays layers on
;the screen menu. i use this as part of my acad.lsp file, as i do work on
;clients' drawings often without a copy of their non-existent layer naming
;convention or standard, not one of them using the same layers or meaningful
;(to me) names for the ones they use. the last folks had so many layers that
;i had to add the page up/down option to the code (i have 39 lines in the
;screen menu when in hi-res mode), jeezis.
;
;options:     ?     -issues normal "layer ?" command
;             Set   -allows one layer pick (not "*") from listing
;             On    -allows multiple layer picks from listing to turn on
;             Off   -allows multiple layer picks from listing to turn off
;             Freeze-allows multiple layer picks from listing to freeze
;             Thaw  -allows multiple layer picks from listing to thaw
;             ??    -lists layers to screen menu
;             Reset -to add newly created layers to the listing
;             Answer-allows a single layer pick as a reply to a command in
;                         progress (such as "change" or "layer") which requires
;                         a layer name as input.
;
;notes  - multiple pick and "??" options require that you pick the line
;             "*-DONE-*" to let the program know you are through before normal
;             keyboard intercourse can resume. picking a blank line will
;             terminate the command.
;       - use the page up/page down options if you've got to many layers.
;       -add the line "[laymenu](laymenu);" to your tablet or screen menu.
;             if you're not using this in your acad.lsp, you can add a separate
;             line "[loadlmnu](if (null listemlayers) (load "laymenu"));" or
;             a similar statement to the menu. putting the load statement on
;             the same line as the function call will disable the "Answer"
;             option.
;       -renaming laymenu to c:laymenu will disable the "Answer" option.
;       -separate these comments from the code for faster loading.
;

(setvar "expert" 1)       ;please remember, this is part of my acad.lsp, so if
(setvar "cmdecho" 0)      ;you use different settings, move these statements
(setvar "menuecho" 2)     ;to "laymenu" function with a save and reset for old
                          ;values. expert value less than 1 cause errors.

(DEFUN AVAL (N L / )      ;returns value from an association list
        (CDR (ASSOC N L)))

(DEFUN MELT ( / L )       ;MEnu Line Test, quick count of screen menu lines.
  (SETQ L 0) (WHILE (/= L 50) (GRTEXT L (ITOA L)) (SETQ L (+ 1 L))))

(DEFUN C:P20 () (SETQ PAGE 20)) ;these two commands will adjust the page length
(DEFUN C:P38 () (SETQ PAGE 38)) ;and can be deleted if not required, as laymenu
                                ;defaults to 20. use the last number visible on
                                ;menu after using melt function.

(DEFUN SHOWME ( PG / CT NDEX)         ;displays pages (PG is # from 0 to ?)
  (SETQ CT 0 NDEX (+ (* PG (- PAGE 3)) PG))
  (WHILE (AND (< CT (- PAGE 2)) (< NDEX (LENGTH LAYRLIST)))
        (APPLY 'GRTEXT (LIST CT (AVAL NDEX LAYRLIST)))
        (SETQ CT (+ 1 CT) NDEX (+ 1 NDEX)))
  (WHILE (< CT (- PAGE 2)) (GRTEXT CT " ") (SETQ CT (+ 1 CT)))
  (GRTEXT CT "*-DONE-*")
  (GRTEXT (+ 1 CT) "PAGE UP")
  (GRTEXT (+ 2 CT) "PAGE DN"))

(DEFUN LAYMEN (M / TRIPS PIKT DONE TMPIKT ) ;passed a command mode M
  (SHOWME (SETQ TRIPS 0))
  (SETQ PIKT "" DONE NIL)
  (WHILE (NOT DONE)
        (SETQ TMPIKT (GRREAD))
        (WHILE (NOT (MEMBER (CAR TMPIKT) '(4 13) )) (SETQ TMPIKT (GRREAD)))
        (SETQ NDEX (+ (CADR TMPIKT) (* TRIPS (- PAGE 2))))
        (COND ((AND (< (CADR TMPIKT) (- PAGE 2)) (< NDEX (LENGTH LAYRLIST)))
                    (PRIN1 (SETQ TMPIKT (AVAL NDEX LAYRLIST)))
                    (IF (MEMBER M '("Set" "Answer") )
                         (SETQ DONE 1 PIKT (IF (= TMPIKT "*") "AWSHIT" TMPIKT))
                         (SETQ PIKT
                            (IF (= PIKT "") TMPIKT (STRCAT TMPIKT "," PIKT)))))
              ((= (CADR TMPIKT) (- PAGE 1))
                    (SETQ TRIPS (IF (= TRIPS 0) 0 (- TRIPS 1)))
                    (SHOWME TRIPS))
              ((= (CADR TMPIKT) PAGE)
                    (SETQ TRIPS
                          (IF (> (+ (* (+ 1 TRIPS) (- PAGE 3)) TRIPS 1)
                                      (LENGTH LAYRLIST))
                                TRIPS (+ 1 TRIPS)))
                    (SHOWME TRIPS))
              (T (SETQ DONE 1))))
  (COND ((= M "Set") (COMMAND "LAYER" "S" PIKT ""))
        ((= M "Answer") (COMMAND PIKT))
        ((/= M "??") (COMMAND "LAYER" M PIKT ""))
        (T))
  (GRTEXT) (PRINC))

(DEFUN LISTEMLAYERS ( / NAME CT )     ;builds menu lists
  (SETQ MLIST '((0 . "CHOOSE") (1 . "LAYER") (2 . "SUB-") (3 . "COMMAND")
        (4 . "?") (5 . "Set") (6 . "ON") (7 . "OFF") (8 . "Freeze")
        (9 . "Thaw") (10 . "??") (11 . "Reset") (12 . "Answer"))
        LAYRLIST (LIST '(0 . "*") (CONS 1 (AVAL 2 (TBLNEXT "LAYER" T))))
        CT 1)
  (WHILE (SETQ NAME (AVAL 2 (TBLNEXT "LAYER")))
        (SETQ LAYRLIST (CONS (CONS (SETQ CT (1+ CT)) NAME) LAYRLIST))))

(DEFUN LAYMENU ( / N PIKT)      ;a layer command for the microwave
  (SETQ PAGE (IF PAGE PAGE 20)) ;set page length
  (IF MLIST 'OK (LISTEMLAYERS)) ;check for menu lists, create them if missing
  (FOREACH L MLIST (GRTEXT (CAR L) (CDR L)))      ;display menu options
  (SETQ N 12)                                     ;blank out the rest of the
  (REPEAT (- PAGE 12) (GRTEXT (SETQ N (+ 1 N)) " "))    ;menu lines
  (SETQ PIKT (GRREAD))    ;get option pick
  (WHILE                  ;until option picked.
     (NOT (APPLY 'AND (MAPCAR 'MEMBER PIKT '((4 13) (4 5 6 7 8 9 10 11 12)) )))
     (SETQ PIKT (GRREAD)))
  (GRTEXT)
  (COND ((= (CADR PIKT) 11) (SETQ MLIST NIL))     ;reset option
        ((= (CADR PIKT) 4) (COMMAND "LAYER" "?" ""))    ;? option
        (T (LAYMEN (AVAL (CADR PIKT) MLIST)))))   ; other options
