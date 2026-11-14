;;; NOTEPAD.LSP
;;; Copyright 1992,93 by Mountain Software
;;;
;;; This program requires ELF, the Extended List Function library
;;;
;;; THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED
;;; WARRANTY.  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR
;;; PURPOSE AND OF MERCHANTABILITY ARE HEREBY DISCLAIMED.
;;;
;;;*===================================================================*
;;;
;;; The ELF Notepad is a demonstration of the powerful capabilities
;;; of the ELF library in a "real" application. Notepad works well
;;; in editing small to medium text files. It is not a full featured
;;; editor, but is fully programmable by any AutoLISP programmer.
;;; In fact Notepad can be used to modify itself, so try your hand
;;; at creating your own custom editor.

(Princ "\nLoading Notepad.Lsp...")
(Load"ELF")                             ;load ELF.EXP, color and key symbols

;*----- This Function merely executes Notepad using specifed colors and size

(DeFun C:NP()
  (Notepad 80 25 (| lgrey blue_bg) (| yellow blue_bg))
)

(DeFun C:NOTEPAD() (C:NP))             ;;; an alias

;*----- The Main Notepad Function

(DeFun NOTEPAD (cols rows color hcolor /
               i str txt done wrows lasti repaint lastwrow typ tmpval tmpstr
               rslt key wcol wrow col edit_help)

  ;*----- The following functions a local to NOTEPAD

  (DeFun title(np_filename)
    (Wtitle (Strnset "Í" cols) 1 (| lgrey blue_bg))
    (Wtitle (If np_filename np_filename "[ ELF Notepad ]") 1 (| white red_bg))
  )
  (DeFun repaint() (paint wrows i color)(SetQ wrow 0) (gc))
  (DeFun reset() (SetQ np_data '("") i 0 wrow 0 wcol 0 lasti 0 changed nil np_filename nil)
                 (title np_filename) (repaint))

  (SetQ olderr *error* *error* NPerror)
  (Wpopup cols rows color color double_bd)
  (Wtitle "[F1] Help  [F2] Search  [F3] New  [F4] Load  [F10] Save  [Esc] Exit"
          4 (| dgrey lgrey_bg))
  (title np_filename)
  (SetQ i 0 wrow 0 wcol 0 col (Car (Winfo)) done nil
        lasti (Max (1-(Length np_data)) 0)
        wrows (- rows 2) lastwrow (1- wrows))

  ;*----- User Defined help for Notepad

  (Set_Edit_Help
        '("ELF Notepad Help" "" "F2\t\tString search\t\t[PgDn - More]"
          "F3\t\tOpen a new file" "F4\t\tLoad a file"
          "F10\t\tSave file" "Ctrl-Home\tMove to top of file"
          "Ctrl-End\tMove to bottom of file"
          "PgUp\tmove up one full page" "PgDn\tmove down one full page"
          "Enter\tSplit line" "Ctrl-Bkspc\tJoin line"
          "Esc\t\tClose Notepad window" ""
          "Notepad is an ELFapp (tm), ELF (c) Mountain Software"
          "" ""
          "Grey Plus\t\tCopy Line"
          "Grey Minus\t\tCut Line"
          "Grey Asterisk\tPaste Line"
          "Ctrl-F1\t\tAutoLISP evaluate line"
          "Ctrl-F10\t\tSave and load AutoLISP file" ""
          "The Notepad text is stored in the global AutoLISP symbol"
          "\"NP_DATA\" as a list if strings."
          "" "" "Press <F1> for additional help" "" "" "" "" ""))

  (If(Not(Car np_data)) (Progn
    (SetQ np_data '("") np_filename nil)
    (Wmenu '("Welcome to the ELF Notepad" ""
             "Notepad is a demonstration of the string and"
             "list handling functions of ELF and demonstrates"
             "both the power and weaknesses of AutoLISP for"
             "a \"real\" application." ""
             "Notepad works fine for small to medium text"
             "files, large files will slow it to a crawl."
             "You can customize Notepad to your own liking"
             "using Notepad itself!" ""
             "Press <Esc> to enter Notepad...")
             -1 -1 48 63 48 (| 5 32))))
  (repaint)

  ;*----- This is the primary program loop

  (While (Not done) (Progn
    (Wtitle (Sprintf "Row: %-3d" (1+ i)) 2)
    (WgotoXY 0 wrow)
    (SetQ prevwrow wrow
          txt (Nth i np_data)
          ;*----- StrGet is the "Workhorse" function for Notepad. The
          ;       keycodes within the function call will cause StrGet
          ;       to terminate. The returned value can then be tested with
          ;       "Cond" to determine the key pressed to exit StrGet
          rslt    (StrGet txt (- cols 2) (1-(+ wcol col)) " " hcolor F2_Key
                     F3_Key F4_Key F10_Key PgUp_Key PgDn_Key G_Pls_Key
                     G_Min_Key G_Ast_Key C_Home_Key C_F1_Key C_BS_Key
                     C_F10_Key C_End_Key)
          str    (Car rslt)
          key    (Cadr rslt)
          scol   (Caddr rslt)
          wcol   (1+ (- scol col)))
    (Wprts 0 wrow str color)
    ;*----- Update primary buffer.
    (If(/= str txt)
      (SetQ np_data (Replace np_data i str) changed T))
    (Cond
      ;*----- quit
      ((= key Esc_Key)    (SetQ done T))
      ;*----- search
      ((= key F2_Key)     (search 'i lasti) (repaint))
      ;*----- clear buffer and screen
      ((= key F3_Key)     (If(ok_to_close) (reset)))
      ;*----- load a new file
      ((= key F4_Key)     (If(ok_to_close)(Progn
                          (reset)
                          (Load_file)
                          (title np_filename)
                          (SetQ i 0 wrow 0 wcol 0
                                lasti (1-(Length np_data))
                                changed nil)
                          (repaint))))
      ;*----- copy current line to cut buffer
      ((= key G_Pls_Key)  (SetQ tmpstr str)(beep 700 0.1))
      ;*----- cut current line to cut buffer
      ((= key G_Min_Key)  (SetQ tmpstr str changed T
                                np_data  (If(> lasti 0) (Delete np_data i) '("")))
                          (If(= i lasti)
                            (SetQ i     (Max (1- i) 0)
                                  wrow  (Max (1- wrow) 0)))
                          (SetQ lasti (Max (1- lasti) 0))
                          (paint wrows (- i wrow) color))
      ;*----- paste cut buffer to cursor
      ((= key G_Ast_Key)  (SetQ np_data (Insert np_data i tmpstr)
                                lasti (1+ lasti) changed T)
                          (paint wrows (- i wrow) color))
      ;*----- save the file
      ((= key F10_Key)    (save_file)(title np_filename))
      ;*----- save the file and load as an AutoLISP file
      ((= key C_F10_Key)  (save_file)   (title np_filename)
                          (Save_Screen) (CLS 7)
                          (If(Load np_filename)
                            (Wmsg (Strcat np_filename "\nloaded successfully"))
                            (Wmsg (Strcat np_filename "\nfailed to load"))
                          )
                          (Restore_Screen))
      ;*----- Have AutoLISP evaluate the current line, this can produce some
      ;*----- useful, strange and disasterous results, use at your own risk!
      ;*----- The return value is placed in the cut buffer and can be pasted
      ;*----- to the screen with the paste [Grey Asterick] key.
      ((= key C_F1_Key)     (Save_Screen)
                            (SetQ tmpval (Eval(Read str))
                                typ    (type tmpval)
                                tmpstr (Cond
                            ((null tmpval) "nil")
                            ((= typ 'INT) (itoa tmpval))
                            ((= typ 'STR) tmpval)
                            ((= typ 'REAL) (rtos tmpval 2))
                            ((= typ 'SUBR) "AutoLISP Internal Function")
                            ((= typ 'EXSUBR) "External Function")
                            ((= typ 'LIST) "List or Function")
                            (T "Unsupported Type")))
                          (Restore_Screen)
                          (Wmsg (StrCat str "\nEvaluates To:\n" tmpstr)))
      ;*----- Move to first line
      ((= key C_Home_Key) (If(> i 0)(Progn(SetQ i 0 wcol 0 wrow 0) (repaint))))
      ;*----- Move to last line
      ((= key C_End_Key)  (If(< i lasti) (Progn
                          (SetQ i (Max(1+(- lasti wrows)) 0) wcol 0 wrow 0)
                          (repaint)
                          (SetQ i lasti wrow (Min (1- wrows) i)))))
      ;*----- Move up one line
      ((= key Up_Key)     (moveup 'wrow 'i 1))
      ;*----- Move up one page
      ((= key PgUp_Key)   (If(> i 0)(Progn(moveup 'wrow 'i wrows)(repaint))))
      ;*----- Move down one page
      ((= key PgDn_Key)   (If(< i lasti) (Progn
                            (movedn 'wrow 'i wrows lasti lastwrow)(repaint))))
      ;*----- Split line at cursor
      ((= key C_BS_Key)   (If(> i 0) (Progn
                            (SetQ i (1- i)
                                  np_data  (Delete (Replace np_data i
                                          (StrCat (Nth i np_data) str)) (1+ i))
                                  wrow  (1- wrow)
                                  lasti (1- lasti)
                                  changed T)
                            (paint wrows (- i wrow) color)
                          )))
      ;*----- insert / split line
      ((= key Enter_Key)  (SetQ np_data (Replace np_data i (SubStr str 1 scol))
                             tmp (SubStr str (1+ scol) 255)
                             changed T)
                          (If(/= i lasti)
                            (SetQ np_data (Insert np_data (1+ i) tmp))
                            (SetQ np_data (Append np_data (List tmp))))
                          (SetQ lasti (1+ lasti) wcol 0)
                          (movedn 'wrow 'i 1 lasti lastwrow)
                          (paint wrows (- i wrow) color))
      ;*----- move the cursor down 1 line scrolling if necessary
      (T                  (movedn 'wrow 'i 1 lasti lastwrow))
    )
  ))
  (Wclose)
  (SetQ *error* olderr olderr nil)
  (Cls 7)
)

;*----- Error Routine

(DeFun NPERROR(s)
  (Beep)
  (Wmsg (Sprintf "Notepad ERROR\n%s" s) 1 (| white red_bg))
  (WcloseAll)
  (Cls 7)
  (SetQ *error* olderr olderr nil)
  (Princ)
)

;*----- Check for modified buffer

(DeFun OK_TO_CLOSE( / ans file fl fname)
  (SetQ fl (SplitPath np_filename)
        fname (MakePath "" "" (Caddr fl) (Cadddr fl))
        file (If np_filename fname "UnNamed File"))
  (If changed
    (wgetyn (Sprintf "Discard changes to \"%s\" ?" file))
    T
  )
)

;*----- Get a yes or no response

(DeFun WGETYN(msg / yn ans col)
  (SetQ yn '("No" "Yes") col (| black cyan_bg))
  (WpopUp (+(strlen msg) 2) 6 col col (| no_brd shadow_bd))
  (Wtitle msg)
  (setq ans (Wmenu yn -1 -1 col col (| white black_bg) (| single_bd tlhl_bd)))
  (setq ans
    (cond
      ((= (cadr ans) Esc_Key) nil)
      ((= (car ans) 1) T)
      (T nil)
    )
  )
  (Wclose)
  ans
)

;*----- Scroll the window up

(DeFun MOVEUP(&row &i rows / _row _idx)
  (SetQ _row  (- (Eval &row) rows)
        _idx  (- (Eval &i) rows))
  (If (< _row 0) (Progn
    (SetQ _row 0)
    (If (>= _idx 0)
      (If(= rows 1) (Wscroll 1))
      (SetQ _idx 0))
  );else
    (If (< _idx 0)
      (SetQ _idx 0 _row 0))
  )
  (Set &row _row) (Set &i _idx)
)

;*----- Scroll the window down

(DeFun MOVEDN(&row &i rows lasti lastwrow / _row _idx)
  (SetQ _row (+ (Eval &row) rows)
        _idx (+ (Eval &i) rows))
  (If (> _row lastwrow) (Progn
    (SetQ _row lastwrow)
    (If (<= _idx lasti)
      (If(= rows 1) (Wscroll 0))
      (SetQ _idx lasti))
  );else
    (If (> _idx lasti)
      (SetQ _idx lasti _row (Eval &row)))
  )
  (Set &row _row) (Set &i _idx)
)

;*----- Display a screen of text

(DeFun PAINT(wrows i color / row)
  (Wcls)
  (SetQ row 0)
  (Repeat wrows
    (Wprts 0 row (Nth i np_data) color)
    (SetQ i (1+ i) row (1+ row))
  )
)

;*----- Load a file

(DeFun LOAD_FILE( / col)
  (SetQ col (| white red_bg))
  (If (GetFilename)
     (if(file_exists np_filename) (progn
       (Wmsg (Sprintf "Loading %s..." np_filename) nil col)
       (SetQ np_data (Read_File np_filename))
       (Wclose)
     );else
     (progn
       (Wmsg "New File" 1 col)
       (setq np_filename (FullPath (StrCase np_filename)))
     ))
  )
  (If(Not np_data) (SetQ np_data '("") np_filename nil))
)

;*----- Prompt for a filename

(DeFun GETFILENAME( / stdat file temp fin)
   (SetQ  file  (If np_filename np_filename ""))
   (WpopUp 42 3 (| yellow black_bg))
   (Wtitle "Enter Filename" 0)
   (Wtitle "[ F2 - Directory ]" 3)
   (While(not fin) (progn
     (Wgotoxy 0 0)
     (SetQ stdat (Strget file 40 0 " " (| white black_bg) F2_Key)
           file    (car stdat)
           key    (cadr stdat))
     (cond
       ((Or(= key F2_Key)(And(= file "")(= key Enter_Key)))
         (If(Setq temp (WgetFile file 33 -1 -1 (| white cyan_bg)))
            (Setq file temp)))
       ((= key Esc_Key) (Setq fin t))
       (T (SetQ np_filename (If(Not(= file "")) (FullPath file) nil) fin T))
     )
   ))
   (Wclose)
   (If (= key Esc_Key) nil np_filename)
)

;*----- Save the list buffer to a file

(DeFun SAVE_FILE( / fl bakfn)
  (If (GetFileName) (Progn
    (SetQ changed nil)
    (Wmsg (Sprintf "Saving %s..." np_filename) nil (| white red_bg))
    (SetQ fl (SplitPath np_filename)
          bakfn (MakePath (car fl) (cadr fl) (caddr fl) ".BAK"))
    (If(file_exists file) (progn
      (EraseFile bakfn)
      (MoveFile np_filename bakfn)
    ))
    (If(Not(Write_File np_filename "" np_data))
      (Wmsg (Sprintf "Error Writing file \"%s\"" np_filename)))
    (Wclose)
  ))
)

;*----- Search from current line for a string

(DeFun SEARCH(&i lastline / lin fin found dat col)
  (If(Not srch_str) (SetQ srch_str ""))
  (SetQ lin (Eval &i) fin nil found nil col (| white red_bg)
        srch_str (StrCase(WgetStr "Search String" srch_str 40 col)))
  (If (>(StrLen srch_str) 0) (Progn
    (Wmsg (Sprintf "Searching for %s..." srch_str) nil col)
    (If(>= lin lastline) (SetQ fin t))
    (While (Not fin) (Progn
      (SetQ lin (1+ lin))
      (SetQ dat (StrCase (Nth lin np_data)))
      (If(>(StrLen dat) 0)
        (If(StrStr dat srch_str) (Progn
          (SetQ fin t found t)
          (Set &i lin)
        ))
      )
      (If(>= lin lastline) (SetQ fin t))
    ))
    (Wclose)
    (If (Not found)
      (Wmsg (Sprintf "\"%s\" Not Found" srch_str) 1 col))
  ))
)

(Princ "\nNotePad.Lsp loaded, enter \"NP\" or \"NotePad\" to run...")
(Princ)

;;;*----- End of Notepad.Lsp

