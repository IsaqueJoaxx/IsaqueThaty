;;  Makefont.lsp                                                                  
;;  by Brad Halls [76300,32] 
;;  Ruby & Associates, P.C.                               
;;  20245 West 12 Mile Road                                           
;;  Southfield, Michigan 48076                                        
;;  (313) 350-2400                                                    
;;                                                                    
;;  Note: The author has just been LAID OFF, and may now be
;;        reached at: 1237 Heitsch St., Waterford, MI 48328.
;;        (313) 673-1680. Happy day. Anybody hiring?
;;
;;  Prompts user for input from either digitizer or keyboard to       
;;  create a single character description of a customized font,       
;;  then appends that description to a file of the user's choice.     
;;  This program was published in CADENCE magazine in May, 1991.
;;  Freeware. Comments & suggestions welcome.
;;                                                                 
;;  
;;  VARIABLE              DATA TYPE        DESCRIPTION
;;   
;;  p1                    various          intget varible for program
;;  lastcommand           string           contains last command given
;;  newstring             string           string to be added to desc
;;  bytecount             int              # of bytes to describe char
;;  newpoint              list             most recent point selected
;;  oldpoint              list             point selected before newpoint
;;  x1                    int              x value of point vector
;;  y1                    int              y value of point vector
;;  charstring            string           character description for font
;;  filestrg              char             y or n to write to a file
;;  namestrg              string           name of file to append desc
;;  code                  int              ASCII code for header
;;  shapedesc             string           shape desc for header
;;  textfile              string           name of text file + ".shp"              
;;  file                  file             file to append desc
;;  bytes                 string           string for bytecount
;;  header                string           header line for shape
;;  thischar              char             char to write to file
;;  stringcount           int              position in charstring
;;  linecount             int              # of characters per line              
;;  cpd_mode              boolean          CPD mode descriptor
;; 
;; ------------------------------------------------------------------------;

   (defun pen_up ()
     (if 
       (or 
         (= lastcommand "Up")   ;test1
         (= lastcommand "Down") ;test2
         (= cpd_mode 1)         ;test3
       ) ;or
           (prompt "\n\n*ERROR* Invalid pen command!\n") ;then

          (progn ;else
            (prompt "\nThe pen is now up.")
            (setq newstring "2,8,")
            (setq charstring (strcat charstring newstring))
            (setq bytecount (+ 2 bytecount)) 
            (setq lastcommand "Up")
          ) ;progn
      ) ;if
   ) ;defun

  ;----------------------------------------------------------------------;

   (defun pen_down ()
     (if 
       (or 
         (= lastcommand "Up")   ;test1
         (= lastcommand "Down") ;test2
         (= cpd_mode 1)         ;test3
       ) ;or
           (prompt "\n\n*ERROR* Invalid pen command!\n") ;then
  
          (progn ;else
            (prompt "\nThe pen is now down.")
            (setq newstring "1,8,")
            (setq charstring (strcat charstring newstring))
            (setq bytecount (+ 2 bytecount)) 
            (setq lastcommand "Down")
          ) ;progn
     ) ;if
   ) ;defun

  ;----------------------------------------------------------------------;

   (defun start_cpd ()
     (if (= cpd_mode 1)
       (progn ;then
         (prompt "\n")
         (prompt "\n*ERROR* Already in CPD mode.")
         (prompt "\n")
        )
        (progn ;else
          (setq newstring "1,9,")
          (setq charstring (strcat charstring newstring))
          (prompt "\n\nContinuous points now being recorded.")
          (prompt "\nEnter 'e' to end pen down mode.\n")
          (setq bytecount (+ 2 bytecount))
          (setq lastcommand "Start")
          (setq cpd_mode 1)
        )
     );if
   ); defun

  ;----------------------------------------------------------------------;

   (defun end_cpd ()
     (if (= cpd_mode 0)
       (progn ;then
         (prompt "\n")
         (prompt "\n*ERROR* Not in CPD mode!")
         (prompt "\n")
       )
       (progn ;else
         (setq newstring "(0,0),")
         (setq charstring (strcat charstring newstring))
         (prompt "\nPen down mode terminated.")
         (setq bytecount (+ 2 bytecount))
         (setq lastcommand "End")
         (setq cpd_mode 0)
       )
     );if
   ); defun

  ;----------------------------------------------------------------------;

   (defun quit_desc ()
     (if (= cpd_mode 1)
       (progn ;then
         (prompt "\n")
         (prompt "\n*ERROR* Still in CPD mode.")
         (prompt "\n")
       )
       (progn ;else
         (prompt "\n")
         (prompt "\nCharacter definition complete.")
         (setq charstring (strcat charstring "0"))
         (setq bytecount (+ 1 bytecount))
       )
     );if
   ); defun

  ;----------------------------------------------------------------------;

   (defun add_point ()
     (if 
       (or 
         (= lastcommand "End") 
         (= lastcommand "")
         (and (= lastcommand "Point") (= cpd_mode 0))
       ); or

       (progn ;then
         (prompt "\n")
         (prompt "\n*ERROR* Must indicate pen position!")
         (prompt "\n")
       )
       (progn ;else
         (setq newpoint p1)
         (setq x1 (rtos (- (car newpoint) (car oldpoint))))
         (setq y1 (rtos (- (cadr newpoint) (cadr oldpoint))))
         (setq newstring (strcat "(" x1 "," y1 ")" "," )) 
         (setq charstring (strcat charstring newstring))
         (setq oldpoint p1)
         (prompt "\nPoint recorded.")
         (setq bytecount (+ 2 bytecount))
         (setq lastcommand "Point")
       )
     );if
   ); defun

  ;--------------------------------------------------------------;

   (defun write_file ()
     (setq namestrg (getstring "\nName of file <no extension>: "))
     (setq textfile (strcat namestrg ".shp"))
     (prompt "\nASCII code for character <See AutoCAD manual p.510>: ")
     (setq code (getstring))
     (prompt "\nShape description <No spaces allowed>: ")
     (setq shapedesc (getstring)) 
     (setq file (open textfile "a"))
     (setq bytes (rtos bytecount))
     (setq header (strcat "*" code "," bytes "," shapedesc))
     (write-line header file)

     (while (/= thischar "")
       (setq thischar (substr charstring stringcount 1))
       (if 
         (and 
           (> linecount 65) 
           (= (substr charstring (+ 1 stringcount) 1) "(" )
         )
         (progn ;then
           (write-char (ascii thischar) file)
           (setq stringcount (+ 1 stringcount))
           (write-char 10 file)
           (setq linecount 0)
         )
         (progn ;else
           (write-char (ascii thischar) file)
           (setq stringcount (+ 1 stringcount))
           (setq linecount (+ 1 linecount))
         )
       );if
     );while

     (write-char 10 file)
     (close file)
   ); defun

  ;----------------------------------------------------------------------;

   (defun display_exit ()
     (prompt "\nOK. Now end your drawing and select option number 7 from the")
     (prompt "\nAutoCAD main menu to compile your font. If you don't already")
     (prompt "\nhave one, you will need a header of the form <*0,4,font name>,")
     (prompt "\n<above, below, modes,0> as the first 2 lines in your font file.")
     (prompt "\nSee appendix B of the AutoCAD reference manual for further")
     (prompt "\ninformation on custimizing shapes & fonts. Good luck!")
     (prompt "\n")
   ); defun

  ;----------------------------------------------------------------------;

   (defun display_intro ()

     (repeat 35 (prompt "\n")); clear screen
     (prompt "\n--------------------------------------------------------------")
     (prompt "\nWelcome to the Makefont.lsp character description program")
     (prompt "\nCopyright (c) July 1990 by Brad Halls, Ruby & Associates P.C.")
     (prompt "\nBefore you begin, be sure to set up your grid, snap, and units")
     (prompt "\nso you get only integer values for point coordinates.")
     (prompt "\nSuggested settings are as follows:")
     (prompt "\n")
     (prompt "\n  1. LIMITS: 50,50")
     (prompt "\n  2. UNITS:  Decimal, set to 0 places after the decimal point.")
     (prompt "\n  3. GRID:   ON, spacing set to one")
     (prompt "\n  4. SNAP:   ON, spacing set to one")
     (prompt "\n")
     (prompt "\nYou should now see integer coordinates in the upper right hand")
     (prompt "\nportion of your screen. If you do not, you must correct this or")
     (prompt "\nthe font will not compile!")
     (prompt "\n")
     (prompt "\nThe following are available pen commands: ")
     (prompt "\n")
     (prompt "\n       Up...........Acivate pen up mode")
     (prompt "\n       Down.........Activate pen down mode")
     (prompt "\n       Start........Activate continuous pen down (CPD) mode")
     (prompt "\n       End..........Terminate continuous pen down (CPD) mode")
     (prompt "\n       Quit.........Terminate character description")
     (prompt "\n")
     (setq p1 (getpoint "\nStarting point? (It is a good idea to start at 0,0): "))
     (prompt "\n")
     (prompt "\nOK.")
     (prompt "\nNow digitize each point along the letter path, and")
     (prompt "\nbe sure to idicate pen position as you go.")
     (prompt "\n")
   ); defun

  ;----------------------------------------------------------------------;

   (defun c:go ()

     (setq p1 nil) (setq ptlist nil) (setq bytecount 0) (setq stringcount 1)
     (setq linecount 0) (setq charstring "") (setq newstring "")
     (setq lastcommand "") (setq thischar "empty") (setq cpd_mode 0)
     (display_intro)
     (setq oldpoint p1)
     (while (/=  p1 "Quit")
       (initget "Up Down Start End Quit")
       (setq p1 (getpoint oldpoint "\nUp/Down/Start/End/Quit/<next point>: "))

       (cond 
         ((= p1 "Up"   ) (pen_up   ))
         ((= p1 "Down" ) (pen_down ))
         ((= p1 "Start") (start_cpd))
         ((= p1 "End"  ) (end_cpd  ))
         ((= p1 "Quit" ) (quit_desc))
         (T              (add_point))
       ) ;cond

     ) ;while
     (prompt (setq filestrg (getstring " Write to a file? ")))(terpri)
     (if (or (= filestrg "y") (= filestrg "Y"))
       (write_file)
     ) ;if
   (display_exit)
   (princ)
   ) ;program

  ;----------------------------------------------------------------------;