;;;
;;;        FFFFFF  OOOOO   NN   N TTTTTTT     
;;;        F      O     O  N N  N    T    aaaaa    ss  mmm mm
;;;        FFFFF  O     O  N  N N    T   a    a   s  s m  m  m
;;;        F      O     O  N   NN    T   a    a  s   s m  m  m
;;;        F       OOOOO   N    N    T    aaaaa sssss  m  m  m.lsp
;;;
;;; *************************************************************************
;;;
;;; Name: FONTasm! *.SHP Font Character Tabulator 
;;;
;;; NOTE: Supports AutoCAD release 10 or higher
;;;       --> not tested under ACAD rel <10
;;;
;;; File: fa.lsp
;;;
;;; Description: This routine will plot a table of each character in an
;;;              AutoCAD *.SHX font file. After the user responds to
;;;              the prompt for an .SHX filename, it will proceed to build
;;;              an ASCII table containing each character.  The resulting
;;;              drawing is designed to be suitable for plotting or printing.
;;;
;;;              This routine is a companion to FONTasm! [FA.exe] released by:
;;;                Gordon Gibson              G. Gibson & Associates
;;;                Vancouver, B.C. Canada     tel/fax (604) 732-0218
;;;                                           CIS     76310,340 
;;;
;;; Author:        J.David Williams, P.Eng.   INTEGREX ENGINEERING
;;;                Vancouver, B.C. Canada     tel/fax (604) 684-2854
;;;                                           CIS     73500,3036  
;;;
;;; ************************************************************************
;;; *                                                                      *
;;; *  Unlimited use of this software is granted by J David Williams.      *
;;; *  Permission to copy and distribute this software is allowed provided *
;;; *  it is unaltered except where specified by the author.               *
;;; *                                                                      *
;;; ************************************************************************
;;;
;;; Program Notes:
;;; --------------
;;;  
;;; Load this as you would any other lisp routine. Run it as FA or FONTasm.
;;;  When prompted, type the file name of the font you want displayed
;;;  then sit back and watch.  It is not necessary to provide the path
;;;  to the font if it is in a dir visible to FA.lsp.  Nor is it
;;;  necessary to add the .SHX extension.
;;; Print or plot the resulting drawing as you would any other;
;;;  Recommend using A-size media @ scale 1"=1" rotate 90deg & 
;;;  adjust offset for optimum position.
;;; ASCII characters <#032 cannot be plotted by ACAD unless preceded by
;;;  an ASCII char>=032.  This routine will plot #s <032 only when #032 is 
;;;  space char.
;;; AutoCAD substitutes ASCII 063 (normally a ques mark) for undefined
;;;  chars >ASCII 032.  If #063 is also undefined, AutoCAD cannot perform
;;;  the substitution and issues a "Undefined Character" message for each
;;;  undefined character it tries to draw.  This condition should not 
;;;  arise often.  It is safe to ignore this warning.
;;;
;;; ************************************************************************
;;; *                                                                      *
;;; *   Revision History:                                                  *
;;; *                                                                      *
;;; ************************************************************************
;;;
;;; Revision history:
;;;  ver 0.00  16Aug91  alpha version
;;;  ver 0.90  20Aug91  original beta release
;;;  ver 0.91  12Sep91  DEMO code added
;;;  ver 0.92  20Oct91  compatiblility to rel10 added
;;;  ver 0.93  13Nov91  char <ASCII32 plotted provided #032 SPC/undef
;;;                     docs changed to approx conform to A'DESK standard
;;;  ver 0.94  28Nov91  problem recognizing demo ver of font resolved
;;;                     program description revised
;;;  ver 1.00  06Feb92  release version
;;;  ver 1.01  28Feb92  works for 'Q' versions of ACAD
;;;                     picks fonts from outside subdirs
;;;  ver 1.02  01Mar92  improved operation for fonts from outside subdirs
;;;  ver 1.03  13Mar92  revised get_ver() to support ACAD rel >=10 only
;;;                     
;;;---------------------------------------------------------------------------
;;;
(prompt "loading FONTasm...  ")
(prompt "supports ACAD rel 10 or greater [incl ACADWIN]")

;;;
;;; ERR  Redefine error handler
;;;
(defun err (st / )

  (if (/= st "Function cancelled")
    (princ (strcat "\nError: " st))
    (princ         "\nFONTasm cancelled") )
  (setq *error* olderr)
  (princ)
) 

;;;
;;; GET_VER  Get ACAD version
;;;
;;; ver - ACAD version# -- ACAD rel 10 or greater supported
;;;       if   rel10 detected, graphics are drawn w/ COMMAND statements
;;;       else rel>11 assumed, graphics drawn using 'entmake'
;;;       ACADWIN versions also supported.
;;;
(defun get_ver ( / )

  (setq ver 11)                                     ;assume ver 11 or greater

  (if (= (substr (getvar "ACADVER") 1 2) "10")      ;1st chars of ACADVER="10"
    (setq ver 10) )                                 ; then version 10
)

;;;
;;; GET_FONT  Font file name
;;;
;;; fstr - complete file name including .SHX extension
;;; fshx - file handle
;;;
(defun get_font ( / flen fhdr ftyp)
                                                    ;prompt for file name
  (setq fstr (getstring "\nFont file name [include path if necessary]: ")
        fstr (strcase fstr)                         ;make string upper case
        flen (strlen fstr) )                        ;no chars in string

  (if (or (< flen 4)                                ;add '.SHX' extension
          (/= (substr fstr (- flen 4)) ".SHX") )    ;  if necessary
    (setq fstr (strcat fstr ".SHX")) )

  (if (not (findfile fstr))                         ;if font file not visible
    (progn                                          ; to routine, ask again
      (princ "Font file ")(princ fstr)(princ " not found -- try again.")
      (get_font) )
    (progn
      (setq fshx (open fstr "r"))                   ;open .SHX
      (if fshx                                      
        (setq fhdr (read-line fshx))                ;read sentinel to EOF 
        (progn                                      ;redo if path not specific
          (princ "Font file ")(princ fstr)(princ " needs path -- try again")
          (get_font) )
      )
      ;; Char in col 21 of .SHX sentinel either "0" or "!"
      ;;  "0" == legitimate font -- non demo
      ;;  "!" == demo font only
      ;; Note to spelunkers -- you can change this, 
      ;;  but it won't do you any good.
      (setq ftyp (substr fhdr 21 1))
      (if (= ftyp "!")
        (setq demo 1)
        (setq demo 0) )
  ) )
)

;;;
;;; PARSE_FONT  Extract from file name, font name
;;;
;;; font - font name string
;;;
(defun parse_font ( / ch flen)

  (setq flen (- (strlen fstr) 4)  font "")             ;throw away extension

  (while (and (>  flen 0)                              ;look at each char
              (/= (setq ch (substr fstr flen 1)) ".")  ;  for legal letter
              (/= ch "/") (/= ch ":") (/= ch "\\") )   ;  in font name
    (setq font (strcat ch font)                        ;build font name
          flen (1- flen) )
  )
  (prompt (strcat "\nPreparing font " font " found in file " fstr))
  (if (= demo 1) (prompt " *** DEMO version ***"))
)

;;;
;;; SET_UP  Organize drawing file for ASCII table
;;;
(defun set_up ( / )

  (prompt "\nSetting-up drawing to display font characters")

  (setvar "EXPERT"    1)
  (setvar "BLIPMODE"  0)
  (setvar "REGENMODE" 0)
  (setvar "GRIDUNIT" (list  1.0 1.0))
  (setvar "GRIDMODE"  1)
  (setvar "LIMMIN"   (list  0.0 0.0))
  (setvar "LIMMAX"   (list 10.0 7.5)) 
  (setvar "SNAPMODE"  1)
  (setvar "SNAPUNIT" (list  0.1 0.1))

  (if (tblsearch "LAYER" "logo")
    (command "LAYER" "off" "logo" "") )

  (if (tblsearch "LAYER" "table")
    (command "LAYER" "off" "table" "") )

  (if (tblsearch "LAYER" "demo")
    (command "LAYER" "off" "demo" "") )

  (if (not (tblsearch "LAYER" "font"))
    (command "LAYER" "n" "font" "c" "w" "font" "") )

  (if (not (tblsearch "LAYER" "frame"))
    (command "LAYER" "m" "frame" "c" "w" "frame" "") )

  (command "ZOOM" "w" "0.0,0.0" "10.0,7.5")         ;zoom page size 11.0x8.5
  (setq sset (ssadd))                               ;erase existing entites?
  (if (/= (setq sset (ssget "C" '(0.0 0.0) '(10.0 7.5))) nil)
    (command "ERASE" sset "") )
  (setq sset nil)
)

;;;
;;; SCAN_FONT  Examine each char in font for maximum horiz size
;;;
;;; c32 - global boolean for ASCII032  0 - char SPC or undef
;;;                                    1 - some other char
;;; wd  - max width of chars in font file (global)
;;;
(defun scan_font ( / ch dd i )

  (command "STYLE" "font" fstr "1.0" "1.0" "0.0" "n" "n" "n")
  (command "LAYER" "s" "font" "")

  (prompt "\nScanning font to find maximum character width")
  (setq cc 0 i 60 wd 0.0)

  (command "TEXT" "m" "5.0,4.0" "0.0" "%%032")      ;place ASCII 32 char
  (setq ch (entget (entlast)))
  (if (equal (cdr (assoc 10 ch)) (cdr (assoc 11 ch))) ;if ins=align pt
    (progn                                          ; then assume char is SPC  
      (setq c32 0  i 33) )                          ;  or undef, start scan @33
    (progn                                          ; else char not SPC or undef
      (setq c32 1  i 32)                            ;  start scan @ASCII 32
      (prompt "\nUnable to draw chars <ASCII#32 for this font")
      (entdel (entlast)) )                          ; remove temp placement
  )

  (while (< i 256)                                  ;write each char to midpt
    (command "TEXT" "m" "5.0,4.0" "0.0" (strcat "%%" (itoa i)))
    (setq ch (entget (entlast))                     ;examine entity '11' coord
          dd (- (car (cdr (assoc 11 ch))) (car (cdr (assoc 10 ch))))
          i  (1+ i) )                               ;compute char half-width
    (if (> dd wd) (setq wd dd))                     ;check against greatest wd
    (entdel (entlast))
  ) 
  (setq wd (* wd 2.0 1.20))                         ;~double half-width 
)

;;;
;;; TABLE_FRAME  Draw ASCII table & headers
;;;
(defun table_frame ( / cen col_lbl dn hcell i lt org rt up)

  (setq cell 0.35                                   ;>> max char size
        origin (list 0.65 6.00) )                   ;>> upr left coords table
  
  (prompt "\nPreparing table")
  (command "LAYER" "m" "table" "c" "c" "table" "")

  (setq boxpt (list                                 ;corners ASCII table border
          (coadd origin (list (* cell (- 1.0)) (* cell (- 10.0))))
          (coadd origin (list (* cell (- 1.0)) (* cell     1.0)))
          (coadd origin (list (* cell   26.0)  (* cell     1.0)))
          (coadd origin (list (* cell   26.0)  (* cell (- 10.0)))) ))
  (make_pline boxpt "table" 0.01 7 1)               ;draw ASCII table border

  (setq i 0)
  (while (< i 26)                                   ;draw col dividers
    (setq up (list (* cell i) cell)
          dn (list (* cell i) (* cell -10.0)) )
    (make_line (coadd origin up) (coadd origin dn) "table" 7)
    (setq i (1+ i)) 
  )
  (setq i 0)
  (while (< i 10)                                   ;draw row dividers
    (setq lt (list (- cell)      (* cell (- i)))
          rt (list (* cell 26.0) (* cell (- i))) )
    (make_line (coadd origin lt) (coadd origin rt) "table" 7)
    (setq i (1+ i))
  )

  (setq col_lbl (list                               ;list of col headers
    "00X" "01X" "02X" "03X" "04X" "05X" "06X" "07X" "08X" "09X"
    "10X" "11X" "12X" "13X" "14X" "15X" "16X" "17X" "18X" "19X"
    "20X" "21X" "22X" "23X" "24X" "25X"))
  (command "STYLE" "headr" "romand" "0.15" "0.6" "0.0" "n" "n" "n")

  (setq hcell (/ cell 2.0)  i 0                     ;draw col headers
        org   (coadd origin (list hcell hcell)) )
  (foreach lbl col_lbl
    (setq cen (coadd org (list (* cell i) 0.0)))
    (command "TEXT" "m" cen "0.0" lbl)
    (setq i (1+ i))
  )
                                                    ;draw row headers
  (setq org (coadd origin (list (- hcell) (- hcell)))  i 0)
  (while (< i 10)
    (setq cen (coadd org (list 0.0 (* cell (- i)))))
    (command "TEXT" "m" cen "0.0" (rtos i 2 0))
    (setq i (1+ i))
  )
) 

;;;
;;; TABLE_FONT  Draw characters in ASCII table
;;;
(defun table_font ( / cen chr col i org row)

  (prompt "\nWriting font characters to table")
  (command "LAYER" "s" "font" "")

  (setq flimit 0.25                                 ;**** font size limit ****
        fhite (/ (fix (* (/ flimit wd) 1000.0)) 1000.0) ;hite to nearest 0.001
        i (/ cell 2.0)                              
        org (coadd origin (list i (- i)))           ;move origin down half-cell
        i 1 )

  (command "STYLE" "font" fstr fhite "1.0" "0.0" "n" "n" "n")
  
  (if (= c32 0)                                     ;if ASCII 32 is SPC/undef
    (setq i 1)                                      ; start at char ASCII 1
    (setq i 32)                                     ; else start at ASCII 32
  )

  (while (< i 256)                                  ;for all 256 chars...
    (setq col (fix (/ i 10))                        ;integer column
          row (- i (* col 10))                      ;integer row
          cen (coadd org (list (* col cell)         ;coords co centre of cell
                               (- (* row cell)))) ) ;  to draw char
    (if (and (> i 0) (< i 256)) ())                 ;NOP statement to prevent
                                                    ; chars placed randomly!
    (if (/= i 10)                                   ; ASCII 10 == LineFeed
      (progn
        (if (< i 32)                                  
          (setq chr (strcat "%%032%%" (itoa i) "%%032"))  ;pad w/ spaces
          (setq chr (strcat "%%"      (itoa i)        )) );char only
        (command "TEXT" "m" cen "0.0" chr)
    ) )
    (setq i (1+ i))                                 ;i+1th char
  )
)

;;;
;;; DRAW_FRAME  Draw borders & titles
;;;
(defun draw_frame ( / cen lm mx str twd )

  (prompt "\nFinishing drawing with borders & titles")
  (command "LAYER" "s" "frame" "")

  (setq boxpt (list (list  0.0 0.0) (list  0.0 7.5) ;corners inside border
                    (list 10.0 7.5) (list 10.0 0.0) )) 
  (make_pline boxpt "frame" 0.02 7 1)               ;draw interior border

  ;; Find width title string by drawing it and parsing its (assoc 10)...
  (setq cen (list 5.50 1.50))                       ;coords to flash font str
  (command "STYLE" "title" "romand" "0.50" "1.0" "0.0" "n" "n" "n")
  (command "TEXT" "m" cen "0.0" font)               ;draw title string
  (setq twd (distance cen (cdr (assoc 10 (entget (entlast)))))
        twd (+ (/ (fix (* twd 10.0)) 5.0) 0.5) )    ;calc text width
  (if (< twd 2.5) (setq twd 2.5))                   ; but not < min value
  (setq lm  (- 9.75 twd) )                          ;x-coord left margin box
  (command "ERASE" "l" "")                          ;erase title string

  ;; Draw title box containing font name...
  (setq boxpt (list (list 9.75 1.85) (list 9.75 0.25)
                    (list lm   0.25) (list  lm  1.85) ))
  (make_pline boxpt "frame" 0.01 7 1)               ;draw title box
  (make_line (list lm 1.05) (list 9.75 1.05) "frame" 7)
  (setq mx  (- 9.75 (/ twd 2.0))                    ;x-coord of middle of box
        cen (list mx 1.45) )                        ;center coords of text
  (command "TEXT" "m" cen "0.0" font)               ;draw font name in box

  ;; Draw hight & width of font as text...
  (command "STYLE" "note" "romand" "0.15" "1.0" "0.0" "n" "n" "n")
  (setq str (strcat "Text Height " (rtos fhite 2 3)))
  (command "TEXT" "m" (list mx 0.75) "0.0" str)     ;draw text of font hite
  (setq str "Text Width 1.00")                      ;>>font width always 1.0<<
  (command "TEXT" "m" (list mx 0.45) "0.0" str)     ;draw text of font width
)

;;;
;;; DRAW_LOGO  Draw logo to font sheet
;;;
(defun draw_logo ( / i l loline logorg logosc logoff l_xoff p1 pt)

  (setq l 0
        logorg (list 0.40  0.60)                    ;ref coord of lowrleft logo
        logosc (list 0.0012 0.0012) )               ;scale logo size
  (setq l_xoff (list 0 416 416 913 1453             ;x-coord char offsets 
                     1719 2125 2439 3107 3107))     ;  (from FONTasm.C)

  (prompt "\nDrawing FONTasm logo")
  (command "LAYER" "m" "logo" "c" "b" "logo" "")    ;make new layer "logo"

  (draw_chars logorg logosc l_xoff)                 ;draw chars
  (command "HATCH" "line" "0.10" "0.0" chsset "")   ;hatch all chars
  (setq chsset nil)                                 ;make char Pline sset nil
)

;;;
;;; DRAW_DEMO  Draw demo stamp
;;;
(defun draw_demo ( / d deline demorg demosc demoff d_xoff i p1 pt)

  (setq d 0
        demorg (list 0.40  0.60)                    ;ref coord of lowrleft
        demosc (list 0.0012 0.0012) )               ;scale 'demo' size
  (setq d_xoff (list 2750 2750 3090 3345 3764 3764));x-coord char offsets
                                                    ;  (from FONTasm.C)
  (prompt "\nDrawing DEMO stamp")
  (command "LAYER" "m" "demo" "c" "r" "demo" "")    ;make new layer "demo"

  (draw_chars demorg demosc d_xoff)                 ;draw all chars
  (setq chsset nil)                                 ;make char Pline sset nil
  (draw_blocks)
)

;;;
;;; DRAW_CHARS  Draw characters of FONTasm logo or DEMO stamp
;;;
;;; ch_org  xycoord of origin for all chars
;;; ch_scl  scale of chars to be applied to x&y coords
;;; ch_x0   list of char x-offsets taken from FONTasm.C 
;;;
(defun draw_chars (ch_org ch_scl ch_x0 / ch chline ch_off i)

  (setq ch 0  chsset (ssadd))
  (while (< ch (length ch_x0))                      ;draw 'ch' # of chars
    (setq chline '()  i 0                           ;'chline'-pline vertecies
          ch_off (list (nth ch ch_x0) 0.0) )        ;offset vector
    (if (= (getvar "CLAYER") "LOGO")                ;get vertex coord componets
      (def_logo ch)                                 ; of logo if layer='logo'
      (def_demo ch) )                               ; of demo if layer='demo'
    (while (nth (+ i 1) chdat)                      ;build array of vertecies
       (setq p1 (list (nth i chdat) (nth (+ i 1) chdat)))
       (setq pt (mapcar '(lambda (pos scl org xof)
                                 (+ (* (+ pos xof) scl) org))
                         p1 ch_scl ch_org ch_off)   
             chline (append (list pt) chline)       ;list of vertecies
             i  (+ i 2) )                           ;increment to next coord
    )
    (make_pline chline (getvar "CLAYER") 0.0 4 1)   ;draw char as Pline
    (ssadd (entlast) chsset)                        ;build list of entities
    (setq ch (1+ ch))                               ;increment to next char
  )
)

;;;
;;; DRAW_BLOCKS  Draw blocks around blanked chars [DEMO ver]
;;;
(defun draw_blocks ( / cen cha col dd half i org p0 p1 p2 p3 pp row)

  (setq half (/ cell 2.0)  i 0
        dd   (* half 0.90)
        org  (coadd origin (list half (- half))) )
  (setq chx  (list 49 51 53 55 57 65 69 73 79 85 97 101 105 111 117)) ;ASCIIs
        
  (while (< i (length chx))
    (setq cha (nth i chx)
          col (fix (/ cha 10.0))
          row (- cha (* col 10.0))
          cen (coadd org (list (* col cell) (- (* row cell))))
          p0  (coadd cen (list (- dd) (- dd)))      ;corner coords
          p1  (coadd cen (list (- dd)    dd ))
          p2  (coadd cen (list    dd     dd ))
          p3  (coadd cen (list    dd  (- dd))) 
          pp  (list p0 p1 p2 p3)
          i   (1+ i) )
    (make_pline pp "demo" 0.0 1 1)                  ;place highlights
) )

;;;
;;; DEF_LOGO  Define logo coord components
;;;  
;;; n - component array describing letter
;;;
(defun def_logo ( n / )

  (setq chdat '())                                  ; list of verticies that
                                                    ;  describe character
(cond 
  ((= n 0)             ; "F" 
    (setq chdat (list      
     84 561  100 594  120 623  143 645  150 652  158 661  166 670  195 688
    224 699  266 707  284 707  303 708  322 709  345 711  376 707  395 688
    398 641  390 608  375 594  361 592  346 596  332 602  300 607  263 604
    230 598  209 585  193 564  178 535  174 519  170 499  168 482  157 441
    144 396  141 352  178 363  211 372  246 381  290 390  334 392  375 385
    384 381  392 376  398 365  396 342  392 311  387 293  374 288  351 292
    309 303  295 303  278 299  266 297  237 295  194 289  146 279  126 256
    116 213  109 164  105 126  106 106  105  80  102  62   99  44   96  27
     96   0   99 -34   98 -63   85 -83   66 -89   47 -82   36 -51   33   0
     33  41   33  65   35  89   37 109   38 139   39 172   41 199   41 225
     42 248   39 262   27 259    9 253   -2 258    1 277   17 297   33 311
     51 344   60 400   65 463   72 516   75 531   79 547   84 561) ))

  ((= n 1)             ; "O" outside perimeter 
    (setq chdat (list
    311  45  295  33  281  21  270  10  228  -3  187 -17  146 -23   90  -6
     52  33   31  90   24 158   31 234   33 242   36 248   39 254   39 264
     39 274   37 289   34 291   30 289   29 285   19 267    5 265   -2 275
      0 298    3 324   18 350   40 393   59 426   75 463   92 518  112 567
    132 612  156 656  169 665  178 671  184 676  199 666  200 642  186 607
    184 586  193 588  201 598  214 612  232 624  252 635  274 659  295 668
    314 672  322 674  331 680  340 684  387 670  425 644  463 607  485 572
    510 518  525 453  519 356  491 270  448 195  398 131  350  81  311  45) ))

  ((= n 2)             ; "O" inside perimeter
    (setq chdat (list
    316 125  374 187  402 232  414 264  426 287  439 360  432 436  405 503
    360 551  297 568  239 549  203 527  177 500  152 467  143 425  130 375
    120 321  119 268  118 226  118 185  123 150  153  84  198  62  254  78
    316 125) ))

  ((= n 3)             ; "N"
    (setq chdat (list
     -2 -21    3  13   15  92   29 173   39 217   51 296   62 377   70 450
     79 508   88 543   88 582   97 621  113 635  138 639  160 636  170 627
    177 625  188 634  207 658  227 671  241 663  248 648  232 607  220 567
    215 529  231 470  252 413  273 357  289 305  319 232  344 167  367 116
    391  92  398 117  408 168  420 236  432 313  436 370  438 428  441 489
    445 557  457 604  468 649  484 691  513 714  529 716  533 682  525 627
    520 573  520 523  517 466  511 393  500 309  488 222  473 139  457  66
    452  41  441  18  432   2  409  -7  388 -16  365 -23  338  -3  305  50
    271 123  240 197  217 254  198 297  178 341  162 369  146 339  127 277
    109 213  101 173   93 140   80 105   67  55   54   6   37 -51   28 -58
     16 -60    6 -68   -4 -53   -4 -34   -2 -21) ))

  ((= n 4)             ; "T"  
    (setq chdat (list
    184 -31  167 -25  160  -6  158  20  156  44  156  68  156  88  151 102
    151 125  156 150  154 183  153 212  156 244  162 305  168 375  174 436
    182 473  186 522  191 562  193 602  187 608  178 612  170 613  157 607
    147 605  129 604  109 600   81 592   51 580   27 577   19 588   27 617
     29 642   36 665   45 680   71 701  109 708  143 713  154 710  165 710
    189 717  227 719  265 719  303 715  328 716  355 710  381 707  389 689
    389 668  389 646  390 632  389 616  383 613  360 615  331 620  311 625
    284 625  264 621  256 613  271 598  285 577  293 551  284 491  273 422
    259 355  246 303  237 233  230 186  221 145  216  75  216  27  215  -4
    205 -14  194 -20  184 -31) ))

  ((= n 5)             ; "a"  
    (setq chdat (list
     -4 102    4 154   14 170   35 188   66 215   77 231   99 254  133 283 
    153 298  181 320  215 350  267 385  311 401  346 379  339 362  325 346 
    301 330  289 315  272 299  256 283  239 265  225 250  211 242  203 230 
    190 215  174 203  152 180  131 154  119 139  114 130  109 121  107 113 
    126 120  146 135  164 146  175 162  193 178  221 197  239 218  269 241 
    297 250  317 247  324 226  330 205  327 155  350 129  385 139  395 144 
    402 151  410 160  413 157  414 145  416 129  413 117  414 109  416 102 
    409  93  399  83  393  76  373  63  352  50  328  49  276  58  248 101 
    240 145  203 114  171  82  139  61  122  41  103  23   82  16   60   3 
     36   3   20  16    7  25    4  40    4  63    9  78    7  90   -4 102) ))

  ((= n 6)             ; "s"  
    (setq chdat (list
     -2  90   -2 115   -4 143   10 162   28 178   64 214   98 254  111 265 
    130 285  152 309  170 327  182 339  191 348  220 357  249 349  264 332 
    266 309  266 286  262 270  261 249  258 231  256 209  252 193  247 175 
    242 156  230 121  221  90  221  70  241  80  265 103  283 119  297 137 
    311 155  322 162  326 159  324 152  322 141  327 114  322  95  301  70 
    282  49  263  33  248  25  234  16  214  -1  201 -16  188 -22  178 -29 
    170 -41  147 -52  123 -58  113 -39  101 -12   97  17   96  49   96  80 
    114  98  131 104  141  96  148  86  156  96  161 135  165 178  166 213 
    150 224  134 211  111 195   66 156   24 113   -2  90) ))

  ((= n 7)             ; "m"  
    (setq chdat (list
     96   2   80  -2   63   2   55  18   68  67   90 136   92 176   62 160
     28 118   -4  92   -4 114   -5 132   -6 150   20 184   51 215   82 246
    101 269  123 292  143 307  183 321  203 306  207 273  195 223  190 190
    215 203  248 229  269 253  303 285  355 331  393 337  410 297  407 277
    403 250  400 219  395 193  390 170  389 150  417 180  445 207  480 232
    520 242  552 225  572 186  581 171  589 158  602 139  631 130  656 149
    676 164  675 138  675 120  676 100  649  70  609  50  555  64  525  91
    495 126  461 143  444 133  428 117  408 102  381  73  345  31  318   2
    290 -13  273  -9  264   6  269  34  282  76  299 137  309 192  287 196
    234 154  207 124  169  85  137  45  120  29  106  10   96   2) ))

  ((= n 8)             ; "!" upper stroke
    (setq chdat (list
    176 174  157 147  130 121  107 115  104 128  106 148  109 182  106 202
    108 234  117 268  119 321  125 371  127 404  135 425  142 466  139 502
    121 512  126 544  133 596  139 647  153 672  178 684  197 703  223 720
    250 727  264 705  256 657  246 607  242 568  237 525  230 496  225 454
    218 394  205 344  200 325  195 296  193 270  183 241  177 205  176 174) ))

  ((= n 9)             ; "!" dot
    (setq chdat (list
     84 -27   77 -14   70   7   72  23   82  39   98  56  119  72  140  74
    167  65  174  33  171  15  171  -3  170 -14  164 -17  158 -19  154 -20
    134 -22  105 -26   84 -27) ))
))

;;;
;;; DEF_DEMO  Define demo coord components
;;;
;;; n - component array describing letter
;;;
(defun def_demo ( n / )

  (setq chdat '())

(cond 
  ((= n 0)             ; "D" outside perimeter 
    (setq chdat (list      
     28 816  238 816  323 742  323 379  244 300   28 300   28 816) ))

  ((= n 1)             ; "D" inside perimeter 
    (setq chdat (list      
    199 726  148 726  148 402  199 402  199 726) ))

  ((= n 2)             ; "E" 
    (setq chdat (list      
     28 816  249 816  249 720  153 720  153 623  233 623  233 527  153 527
    153 396  249 396  249 300   28 300   28 816) ))

  ((= n 3)             ; "M" 
    (setq chdat (list      
    221 566  261 816  402 816  402 300  289 300  289 635  238 300  187 300 
    141 629  142 300   28 300   28 816  170 816  209 566  221 566) ))

  ((= n 4)             ; "0" outside perimeter
    (setq chdat (list      
    102 816  238 816  318 736  318 379  238 300   96 300   22 379   22 736
    102 816) ))
 
  ((= n 5)             ; "0" inside perimeter
    (setq chdat (list      
    148 714  148 396  193 396  193 714  148 714) ))
))

;;;
;;; MAKE_LINE  Entmake line
;;;
;;; p0 p1 - endpoints
;;; layer - layer of entity
;;; color - color of entity
;;;
(defun make_line (p0 p1 layer color / )

  (if (= ver 10)
    (progn
      (command "LAYER" "set" layer "")
      (command "LINE" p0 p1 "")
    )
    (progn
      (entmake (list (cons '0 "LINE")
                     (cons '8  layer)
                     (cons '62 color)
                     (cons '10 p0)
                     (cons '11 p1) ))
  ) )
)
 
;;;
;;; MAKE_PLINE  Entmake pline 
;;;
;;; plist - list of verticies
;;; layer - drawing layer
;;; width - width of pline
;;; color - color of entity
;;; close - 0 No, 1 Yes
;;;
(defun make_pline (plist layer width color close / vrtx )

  (if (= ver 10)
    (progn
      (command "PLINE")
      (foreach vrtx plist
        (command vrtx) )
      (if (= close 1)
        (command "c")
        (command "") )
    )
    (progn
      (entmake (list (cons '0 "POLYLINE")
                     (cons '8  layer)
                     (cons '66 1)
                     (cons '70 close)
                     (cons '40 width)
                     (cons '41 width) ))
      (foreach vrtx plist
        (entmake (list (cons '0 "VERTEX")
                       (cons '8 layer) 
                       (cons '10 vrtx) )) )
      (entmake (list (cons '0 "SEQEND")))
  ) )
)

;;;
;;; COADD  Add two coordinate lists
;;;
;;; c0 c1 - coords to be added
;;;
(defun coadd (c0 c1 / )

  (mapcar '+ c0 c1)
)

;;;
;;; FA  Quick entry
;;;
(defun c:fa ( / ) 

  (c:fontasm)
  (princ)
)

;;;
;;;*******************************
;;;*******************************
;;;***
;;;***  FONTasm - Main routine
;;;***
;;;*******************************
;;;*******************************
;;;
(defun c:fontasm ( / )

  (setq olderr *errro*
        *error* err)

  (setvar "CMDECHO" 0)
  (command "GRAPHSCR")

  (get_ver)
  (get_font)                           ; Prompt user for path & name of font.SHX
  (parse_font)                         ; From any input, extract font name
  (set_up)                             ; Organize screen for font drawing
  (scan_font)                          ; Cycle all 256 chars for widest char

  (if (tblsearch "LAYER" "table")
    (command "LAYER" "on" "table" "")
    (table_frame) )                    ; Draw labelled ASCII table

  (table_font)                         ; Drop font chars into ASCII table
  (draw_frame)                         ; Add borders & titles to drawing

  (if (tblsearch "LAYER" "logo")
    (command "LAYER" "on" "logo" "")
    (draw_logo) )                      ; Draw logo

  (if (= demo 1)                       ;if demo version of *.SHX
    (if (tblsearch "LAYER" "demo")
      (command "LAYER" "on" "demo" "") 
      (draw_demo) ) )                  ;draw DEMO stamp

  (command "LAYER" "s" "0" "")         ; set to layer '0'

  (setvar "CMDECHO" 1)
  (setq *error* olderr)
  (terpri)(princ "FONTasm...ends")
  (princ)
)

(prompt "\nroutine by Integrex Engineering, J.D.Williams,P.Eng.")
(prompt "\ncompanion to FONTasm! by G.Gibson & Associates, 1992\n")
(c:fontasm)

;;; EOF
