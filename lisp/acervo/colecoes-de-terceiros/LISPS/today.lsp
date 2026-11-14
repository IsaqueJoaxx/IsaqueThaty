;;;
;;;   LISP:    Today.lsp
;;;   Version: 4.0
;;;   DCL:     Today93, 94, 95, 96, 97, 98, 99 & 00.
;;;   Copyright (C) 1993-94 by Cadd Software.
;;;
;;;   Permission to use, copy, modify, and distribute this software
;;;   for any purpose and without fee is hereby granted, provided
;;;   that the above copyright notice appears in all copies and that
;;;   both that copyright notice and this permission notice appear in
;;;   all supporting documentation.
;;;
;;;   THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED
;;;   WARRANTY.  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR
;;;   PURPOSE AND OF MERCHANTABILITY ARE HEREBY DISCLAIMED.
;;;
;;;   MORE YEARS:  To increase the years just add another TodayYR.DCL where
;;;   YR will be another year.  Make sure  you make the appropriate changes
;;;   to the widgets that contain the holiday selectables...
;;;
;;;   ADD/SUBTRACT HOLIDAYS:To add or take  away holiday or just to add
;;;   your own holidays redefine the areas in the routines Check & Holidays...
;;;   under the appropriate months...
;;;
;;;   Version 1.0 - initial release of Today.lsp by Gary Kohan.
;;;
;;;   Version 2.0 - added more years - today now supports years 1993-1997.
;;;
;;;   Version 2.1 - fixed problem w/ TODAY95.DCL widgets - AUGUST95.
;;;                 fixed problem w/ TODAY97.DCL widgets - MARCH97, JULY97.
;;;
;;;   Version 2.2 - cleaned up and fine tuned this program.
;;;
;;;   Version 3.0 - tweeked this program even more; polished the Holiday
;;;                 function to increase integrity of overall program.
;;;
;;;   Version 4.0 - extended to the year 2000...
;;;
;;;
;;;
;;;   Comments/criticism [72707,1412] or (703)-569-3459
;;;
(princ "\rInitializing TODAY...")
(defun C:Today (/ ReadLog  Time  Set_T   Check  Holidays DDG_id
                  Calendar   olderr  Merr  Blurb  version  fname
                  GoEnd)

 (defun Blurb (subR  flag  origin)
    (cond ( (NOT (findfile "Today#.lsp"))
            (if (zerop  flag)
             (alert "Logo file missing...")
             'FNF) )

          (t  (load "Today#")
    ));;;condx2
 );;;defum Blurb

 (defun Holidays (mnth)
       (cond ( (=  mnth  "09")
               (setq  LDay    ", Labor Day") )
             ( (=  mnth  "10")
               (setq  CDay    ", Columbus Day"
                      HDay    ", Halloween") )
             ( (=  mnth  "11")
               (setq EDay    ", Election Day"
                     VDay    ", Veterans Day"
                     TDay    ", Thanksgiving Day"
                     AuDay   ", Happy Birthday Gary") )
             ( (=  mnth  "12")
               (setq WTDay   ", 1st Day of Winter"
                     XMDay   ", Christmas Day") )
             ( (=  mnth  "01")
               (setq NYDay   ", New Year's Day"
                     MDay    ", Martin L. King Jr. Day") )
             ( (=  mnth  "02")
               (setq GHDay   ", Ground Hog Day"
                     LCDay   ", Lincoln's Birthday"
                     VTDay   ", Valentine's Day"
                     AWDay   ", Ash Wednesday"
                     WHDay   ", Washington's Birthday Observed") )
            ( (=  mnth  "03")
              (setq SPDay   ", St. Patrick's Day") )
            ( (=  mnth  "04")
              (setq GFDay   ", Good Friday"
                    ETDay   ", Easter Sunday") )
            ( (=  mnth  "05")
              (setq MYDay   ", Mother's Day"
                    ARDay   ", Armed Forces Day"
                    MRDay   ", Memorial Day") )
            ( (=  mnth  "06")
              (setq FLDay   ", Flag Day"
                    FTDay   ", Father's Day") )
            (t ;; (=  mnth  "07")
              (setq IDDay   ", Independance Day")
       ));;condx2
  );;;Holidays


  (defun Check (yrmnth  day / mnth  year  xday  quotes
     LDay    CDay     HDay    EDay     VDay    TDay    NYDay   MDay   FTDay
     GHDay   LCDay    VTDay   WHDay    AWDay   GFDay   ETDay   FLDay  AuDay
     SPDay   IDDay    MYDay   ARDay    MRDay   WTDay   XMDay)

    (setq year  (substr yrmnth 1 2)     day   (itoa day)
          mnth  (substr yrmnth 3 2)     xday  (atoi day)
        quotes  "")

    (Holidays  mnth)

    (set_tile "error" (strcat "Today is "
       ;;;September
       (cond ( (=  mnth  "09")
                (strcat "September " day ", 19"   year
                  (cond ( (or (=  year "93")(= year "98") )
                          (if (=  xday  6) LDay  quotes) )
                        ( (or (=  year "94")(= year "99") )
                          (if (=  xday  5) LDay  quotes) )
                        ( (or (=  year "95")(= year "00") )
                          (if (=  xday  4) LDay  quotes) )
                        ( (=  year "96")
                          (if (=  xday  2) LDay  quotes) )
                        (t ;;(=  year "97")
                          (if (=  xday  1) LDay  quotes)
                  ));;;condx2
                );;;str
               )
             ;;;October
             ( (=    mnth   "10")
                (strcat "October " day ", 19"   year
                  (cond ( (or (=  year "93")(=  year "98") )
                          (cond ( (=  xday  11) CDay)
                                ( (=  xday  31) HDay   )
                                (t quotes ))
                        )
                        ( (or (=  year "94")(= year "99") )
                          (cond ( (=  xday  10) CDay)
                                ( (=  xday  31) HDay   )
                                (t quotes ))
                        )
                        ( (or (=  year "95")(= year "00") )
                          (cond ( (=  xday   9) CDay)
                                ( (=  xday  31) HDay   )
                                (t quotes ))
                        )
                        ( (=  year "96")
                          (cond ( (=  xday  14) CDay)
                                ( (=  xday  31) HDay   )
                                (t quotes ))
                        )
                        (t  ;;(=  year  "97")
                           (cond ( (=  xday  12) CDay)
                                 ( (=  xday  31) HDay   )
                                 (t quotes
                           ));;;condx2
                  ))
                ))
             ;;;November
             ( (=    mnth   "11")
               (strcat "November " day ", 19"   year
                  (cond ( (or (=  year "93")(= year "98") )
                          (cond ( (=  xday   4) AuDay)
                                ( (=  xday   9) EDay)
                                ( (=  xday  11) VDay)
                                ( (=  xday  25) TDay   )
                                (t quotes ))
                        )
                        ( (or (=  year "94")(=  year "99"))
                          (cond ( (=  xday   4) AuDay)
                                ( (=  xday   8) EDay)
                                ( (=  xday  11) VDay)
                                ( (=  xday  24) TDay   )
                                (t quotes ))
                        )
                        ( (or (=  year "95")(= year "00") )
                          (cond ( (=  xday   4) AuDay)
                                ( (=  xday   7) EDay)
                                ( (=  xday  11) VDay)
                                ( (=  xday  23) TDay   )
                                (t quotes ))
                        )
                        ( (=  year "96")
                          (cond ( (=  xday   4) AuDay)
                                ( (=  xday   6) EDay)
                                ( (=  xday  11) VDay)
                                ( (=  xday  22) TDay   )
                                (t quotes ))
                        )
                        (t ;;(=  year "97")
                           (cond ( (=  xday   4) AuDay)
                                 ( (=  xday   5) EDay)
                                 ( (=  xday  11) VDay)
                                 ( (=  xday  21) TDay)
                                 (t quotes
                           ));;;condx2
                 ))
               ))
             ;;;December
             ( (=    mnth   "12")
                 (strcat "December " day ", 19"   year
                    (cond ( (=  xday  21)  WTDay)
                          ( (=  xday  25)  XMDay)
                          (t quotes
                    ));;;condx2
             ))
             ;;;January
             ( (=    mnth   "01")
                 (strcat "January " day ", 19"   year
                   (cond ( (or (=  year "93")(= year "98") )
                          (cond ( (=  xday   1) NYDay)
                                ( (=  xday  18) MDay)
                                (t quotes
                          ));;;condx2
                        )
                        ( (or  (=  year "94")(= year "99") )
                          (cond ( (=  xday   1) NYDay)
                                ( (=  xday  17) MDay)
                                (t quotes
                          ));;;condx2
                        )
                        ( (or (=  year "95")(=  year "00") )
                          (cond ( (=  xday   1) NYDay)
                                ( (=  xday  16) MDay)
                                (t quotes
                          ));;;condx2
                        )
                        ( (=  year "96")
                          (cond ( (=  xday   1) NYDay)
                                ( (=  xday  15) MDay)
                                (t quotes
                          ));;;condx2
                        )
                        (t  ;;1997
                          (cond ( (=  xday   1) NYDay)
                                 ( (=  xday  20) MDay)
                                 (t quotes
                           ));;;condx2
                  ))
             ))
             ;;;February
             ( (=    mnth   "02")
                 (strcat "February " day ", 19"   year
                  (cond ( (or (=  year "93")(=  year "98") )
                          (cond ( (=  xday   2) GHDay)
                                ( (=  xday  12) LCDay)
                                ( (=  xday  14) VTDay)
                                ( (=  xday  15) WHDay)
                                ( (=  xday  24) AWDay)
                                (t quotes ))
                        )
                        ( (or  (=  year  "94")(=  year "99") )
                          (cond ( (=  xday   1) GHDay)
                                ( (=  xday  12) LCDay)
                                ( (=  xday  14) VTDay)
                                ( (=  xday  15) WHDay)
                                ( (=  xday  16) AWDay)
                                (t quotes
                          ));;;condx2
                         )
                         ( (or  (=  year  "95")(= year "00") )
                          (cond ( (=  xday   7) GHDay)
                                ( (=  xday  12) LCDay)
                                ( (=  xday  14) VTDay)
                                ( (=  xday  15) WHDay)
                                ( (=  xday  17) AWDay)
                                (t quotes
                          ));;;condx2
                         )
                         ( (=  year  "96")
                          (cond ( (=  xday   6) GHDay)
                                ( (=  xday  12) LCDay)
                                ( (=  xday  14) VTDay)
                                ( (=  xday  15) WHDay)
                                ( (=  xday  18) AWDay)
                                (t quotes
                          ));;;condx2
                         )
                         (t  ;;(= year  "97")
                           (cond ( (=  xday   5) GHDay)
                                 ( (=  xday  12) LCDay)
                                 ( (=  xday  14) VTDay)
                                 ( (=  xday  15) WHDay)
                                 ( (=  xday  19) AWDay)
                                (t quotes
                          ));;;condx2
                  ))
             ))
             ;;;March
             ( (=    mnth   "03")
                (strcat "March " day ", 19"   year
                  (cond ( (=  xday  17)  SPDay)
                        (t quotes
                  ));;;condx2
             ))
             ;;;April
             ( (=    mnth   "04")
                 (strcat "April " day ", 19"   year
                  (cond ( (or (=  year "93")(=  year "98") )
                          (cond ( (=  xday   9) GFDay)
                                ( (=  xday  11) ETDay)
                                (t quotes ))
                        )
                        ( (or (=  year "94")(= year "99") )
                          (cond ( (=  xday   1) GFDay)
                                ( (=  xday   3) ETDay)
                                (t quotes ))
                        )
                        ( (or  (=  year "95")(= year "00") )
                          (cond ( (=  xday  14) GFDay)
                                ( (=  xday  16) ETDay)
                                (t quotes ))
                        )
                        ( (=  year "96")
                          (cond ( (=  xday   5) GFDay)
                                ( (=  xday   7) ETDay)
                                (t quotes ))
                        )
                        (t ;;1997
                          (cond ( (=  xday   28) GFDay)
                                ( (=  xday   30) ETDay)
                                (t quotes
                          ));;;condx2
                    ))
             ))
             ;;;May
             ( (=    mnth   "05")
                (strcat "May " day ", 19"   year
                   (cond ( (=  xday   9) MYDay)
                         ( (=  xday  21) ARDay)
                         ( (=  xday  31) MRDay)
                         (t quotes
                   ));;;condx2
             ))
             ;;;June
             ( (=    mnth   "06")
               (strcat "June " day ", 19"   year
                  (cond ( (or (=  year "93")(= year "98") )
                          (cond ( (=  xday  14) FLDay)
                                ( (=  xday  20) FTDay)
                                (t quotes ))
                        )
                        ( (or (=  year "94")(= year "99") )
                          (cond ( (=  xday  14) FLDay)
                                ( (=  xday  19) FTDay)
                                (t quotes ))
                        )
                        ( (or (=  year "95")(= year "00") )
                          (cond ( (=  xday  14) FLDay)
                                ( (=  xday  18) FTDay)
                                (t quotes ))
                        )
                        ( (=  year "96")      
                          (cond ( (=  xday  14) FLDay)
                                ( (=  xday  16) FTDay)
                                (t quotes ))
                        )
                        (t ;;1997
                          (cond ( (=  xday  14) FLDay)
                                ( (=  xday  15) FTDay)
                                (t quotes
                          ));;;condx2
                 ))
             ))
             ;;;July
             ( (=    mnth   "07")
               (strcat "July " day ", 19"   year
                 (cond ( (=  xday  4)  IDDay)
                       (t quotes
                 ));;;condx2
             ))
             ;;;August NO HOLIDAYS
             (t ;;;(=    mnth   "08")
               (strcat "August " day ", 19"   year)
       ));;;condx2
   "."));;;set_tile
  );;;check

 (defun merr (s)
   (cond ( (=  s  "quit / exit abort")  nil)

         ( (member s '("Function cancelled"  "console break"))
           nil)

         (t  (princ (strcat "\n** Today error:"  s))
   ));condx2

   (GoEnd DDG_id)
   (princ) )

 (defun set_T (flag  day)
   (mapcar 'mode_tile
     '("s01" "s02" "s03" "s04" "s05" "s06" "s07" "s08" "s09" "s10"
       "s11" "s12" "s13" "s14" "s15" "s16" "s17" "s18" "s19" "s20"
       "s21" "s22" "s23" "s24" "s25" "s26" "s27" "s28" "s29" "s30" "s31"
       "z" "a" "b" "c" "d" "e" "f" "aa" "bb" "cc" "dd" "ee" "ff")
      (list  1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1 1
             1 1 1 1 1 1 1 1 1 1 1 1 1)
   );;;map
   (cond ( (zerop  flag)
           (mode_tile  (strcat "s"  day)   0)  ))
 );;;set_T

 (defun time (flag /   cd  mnth  day  yr)
   (SetQ  cd (substr (rtos (getvar "CDATE") 2 6) 1 15)
          yr (substr cd 3 2)  day (substr cd 7 2)
        mnth (substr cd 5 2))
   (if  (zerop flag)
     (list (strcat yr mnth)  day)
     (strcat yr  mnth  day)
   );;;if
 );;;DEFUN TIME

   (defun ReadLog (/ id fname dateck)
    (setq    dateck(atoi (time  1))
          fname (strcase

      (cond ( (and  (>=   dateck  930101)(<=   dateck  931231) ) "today93.dcl")
            ( (and  (>=   dateck  940101)(<=   dateck  941231) ) "today94.dcl")
            ( (and  (>=   dateck  950101)(<=   dateck  951231) ) "today95.dcl")
            ( (and  (>=   dateck  960101)(<=   dateck  961231) ) "today96.dcl")
            ( (and  (>=   dateck  970101)(<=   dateck  971231) ) "today97.dcl")
            ( (and  (>=   dateck  980101)(<=   dateck  981231) ) "today98.dcl")
            ( (and  (>=   dateck  990101)(<=   dateck  991231) ) "today99.dcl")
            (t  ;; (and  (>=   dateck  990101)(<=   dateck  001231) ) "today99.dcl")
                                                                      "today20.dcl"
      ));;;condx2
    ));;;strcase,setq

    ;;;;(princ (strcat "\n file is " fname))

     (cond ( (minusp (setq id (load_dialog fname)))
             (alert (strcat fname " file is missing - aborting!"))
             (exit) ))
   id);;;;defun Read

 (defun Calendar ( / done what_next mnth  mnth&day&yr  answer  yr)



   (setq         done  0
            what_next  2
               DDG_id  (ReadLog)
                 mnth  (nth 0  (setq mnth&day&yr (time 0)))
                   yr  (substr mnth 1 2)

               answer

     (while (and  (/=  done  1)  (>  what_next  1))
        (new_dialog
             (cond ( (=  yr "93")

                     (cond ( (=        mnth          "9301") "JANUARY93")
                           ( (=        mnth          "9302") "FEBRUARY93")
                           ( (=        mnth          "9303") "MARCH93")
                           ( (=        mnth          "9304") "APRIL93")
                           ( (=        mnth          "9305") "MAY93")
                           ( (=        mnth          "9306") "JUNE93")
                           ( (=        mnth          "9307") "JULY93")
                           ( (=        mnth          "9308") "AUGUST93")
                           ( (=        mnth          "9309") "SEPTEMBER93")
                           ( (=        mnth          "9310") "OCTOBER93")
                           ( (=        mnth          "9311") "NOVEMBER93")
                           (t                                "DECEMBER93"
                      ))
                   )
                   ( (=  yr  "94");;;; 1994 STARTS
                     (cond ( (=        mnth          "9401") "JANUARY94")
                           ( (=        mnth          "9402") "FEBRUARY94")
                           ( (=        mnth          "9403") "MARCH94")
                           ( (=        mnth          "9404") "APRIL94")
                           ( (=        mnth          "9405") "MAY94")
                           ( (=        mnth          "9406") "JUNE94")
                           ( (=        mnth          "9407") "JULY94")
                           ( (=        mnth          "9408") "AUGUST94")
                           ( (=        mnth          "9409") "SEPTEMBER94")
                           ( (=        mnth          "9410") "OCTOBER94")
                           ( (=        mnth          "9411") "NOVEMBER94")
                           (t                                "DECEMBER94"
                     ))
                   )
                   ( (=  yr  "95");;;; 1995 STARTS
                     (cond ( (=        mnth          "9501") "JANUARY95")
                           ( (=        mnth          "9502") "FEBRUARY95")
                           ( (=        mnth          "9503") "MARCH95")
                           ( (=        mnth          "9504") "APRIL95")
                           ( (=        mnth          "9505") "MAY95")
                           ( (=        mnth          "9506") "JUNE95")
                           ( (=        mnth          "9507") "JULY95")
                           ( (=        mnth          "9508") "AUGUST95")
                           ( (=        mnth          "9509") "SEPTEMBER95")
                           ( (=        mnth          "9510") "OCTOBER95")
                           ( (=        mnth          "9511") "NOVEMBER95")
                           (t                                "DECEMBER95"
                    ))
                   )
                   ( (=  yr  "96");;;; 1996 STARTS
                     (cond ( (=        mnth          "9601") "JANUARY96")
                           ( (=        mnth          "9602") "FEBRUARY96")
                           ( (=        mnth          "9603") "MARCH96")
                           ( (=        mnth          "9604") "APRIL96")
                           ( (=        mnth          "9605") "MAY96")
                           ( (=        mnth          "9606") "JUNE96")
                           ( (=        mnth          "9607") "JULY96")
                           ( (=        mnth          "9608") "AUGUST96")
                           ( (=        mnth          "9609") "SEPTEMBER96")
                           ( (=        mnth          "9610") "OCTOBER96")
                           ( (=        mnth          "9611") "NOVEMBER96")
                           (t                                "DECEMBER96"
                     ))
                  )
                  ( (=  yr  "97") ;; 1997 STARTS
                    (cond ( (=        mnth          "9701") "JANUARY97")
                             ( (=        mnth          "9702") "FEBRUARY97")
                             ( (=        mnth          "9703") "MARCH97")
                             ( (=        mnth          "9704") "APRIL97")
                             ( (=        mnth          "9705") "MAY97")
                             ( (=        mnth          "9706") "JUNE97")
                             ( (=        mnth          "9707") "JULY97")
                             ( (=        mnth          "9708") "AUGUST97")
                             ( (=        mnth          "9709") "SEPTEMBER97")
                             ( (=        mnth          "9710") "OCTOBER97")
                             ( (=        mnth          "9711") "NOVEMBER97")
                             (t                                "DECEMBER97"
                       ))
                  )
                  ( (=  yr  "98") ;;1998 starts
                       (cond ( (=        mnth          "9801") "JANUARY98")
                             ( (=        mnth          "9802") "FEBRUARY98")
                             ( (=        mnth          "9803") "MARCH98")
                             ( (=        mnth          "9804") "APRIL98")
                             ( (=        mnth          "9805") "MAY98")
                             ( (=        mnth          "9806") "JUNE98")
                             ( (=        mnth          "9807") "JULY98")
                             ( (=        mnth          "9808") "AUGUST98")
                             ( (=        mnth          "9809") "SEPTEMBER98")
                             ( (=        mnth          "9810") "OCTOBER98")
                             ( (=        mnth          "9811") "NOVEMBER98")
                             (t                                "DECEMBER98"
                       ))
                  )
                  ( (=  yr  "99") ;; 1999 starts
                       (cond ( (=        mnth          "9901") "JANUARY99")
                             ( (=        mnth          "9902") "FEBRUARY99")
                             ( (=        mnth          "9903") "MARCH99")
                             ( (=        mnth          "9904") "APRIL99")
                             ( (=        mnth          "9905") "MAY99")
                             ( (=        mnth          "9906") "JUNE99")
                             ( (=        mnth          "9907") "JULY99")
                             ( (=        mnth          "9908") "AUGUST99")
                             ( (=        mnth          "9909") "SEPTEMBER99")
                             ( (=        mnth          "9910") "OCTOBER99")
                             ( (=        mnth          "9911") "NOVEMBER99")
                             (t                                "DECEMBER99"
                       ))
                  )
                  (t  ;;(=  yr  "00") ;; 2000 starts
                       (cond ( (=        mnth          "0001") "JANUARY00")
                             ( (=        mnth          "0002") "FEBRUARY00")
                             ( (=        mnth          "0003") "MARCH00")
                             ( (=        mnth          "0004") "APRIL00")
                             ( (=        mnth          "0005") "MAY00")
                             ( (=        mnth          "0006") "JUNE00")
                             ( (=        mnth          "0007") "JULY00")
                             ( (=        mnth          "0008") "AUGUST00")
                             ( (=        mnth          "0009") "SEPTEMBER00")
                             ( (=        mnth          "0010") "OCTOBER00")
                             ( (=        mnth          "0011") "NOVEMBER00")
                             (t                                "DECEMBER00"
                       ));;condx2
                           ;;;; add more years here...
             ));;condx2
          DDG_id)

        (set_T  0  (nth 1 mnth&day&yr))

        (action_tile  "accept"       "(done_dialog   0)(setq done 1)"  )
        (action_tile  "origin"       "(Blurb  T   0  \"ORIGIN\")"      )
        (action_tile  "otay"         "(done_dialog   0)"               )

        (action_tile  "s01"          "(Check  mnth  01)" )
        (action_tile  "s02"          "(Check  mnth  02)" )
        (action_tile  "s03"          "(Check  mnth  03)" )
        (action_tile  "s04"          "(Check  mnth  04)" )
        (action_tile  "s05"          "(Check  mnth  05)" )
        (action_tile  "s06"          "(Check  mnth  06)" )
        (action_tile  "s07"          "(Check  mnth  07)" )
        (action_tile  "s08"          "(Check  mnth  08)" )
        (action_tile  "s09"          "(Check  mnth  09)" )
        (action_tile  "s10"          "(Check  mnth  10)" )
        (action_tile  "s11"          "(Check  mnth  11)" )
        (action_tile  "s12"          "(Check  mnth  12)" )
        (action_tile  "s13"          "(Check  mnth  13)" )
        (action_tile  "s14"          "(Check  mnth  14)" )
        (action_tile  "s15"          "(Check  mnth  15)" )
        (action_tile  "s16"          "(Check  mnth  16)" )
        (action_tile  "s17"          "(Check  mnth  17)" )
        (action_tile  "s18"          "(Check  mnth  18)" )
        (action_tile  "s19"          "(Check  mnth  19)" )
        (action_tile  "s20"          "(Check  mnth  20)" )
        (action_tile  "s21"          "(Check  mnth  21)" )
        (action_tile  "s22"          "(Check  mnth  22)" )
        (action_tile  "s23"          "(Check  mnth  23)" )
        (action_tile  "s24"          "(Check  mnth  24)" )
        (action_tile  "s25"          "(Check  mnth  25)" )
        (action_tile  "s26"          "(Check  mnth  26)" )
        (action_tile  "s27"          "(Check  mnth  27)" )
        (action_tile  "s28"          "(Check  mnth  28)" )
        (action_tile  "s29"          "(Check  mnth  29)" )
        (action_tile  "s30"          "(Check  mnth  30)" )
        (action_tile  "s31"          "(Check  mnth  31)" )

        (setq what_next (start_dialog))
     );while
   );setq answer main
  );;;calendar

  (defun GoEnd (id)(unload_dialog  id)(setq *error*  olderr))

;;;
;;;                      M A I N   B E G I N S
;;;
  (setq  olderr   *error*
        version  "V4.0"
        fname    "Today"
        *error*   merr)

  (setvar "cmdecho" 0)

  (Blurb T  1  fname)

  (cond ( (<    (atoi (getvar "ACADVER"))   12)
           (princ "\n** Today requires AutoCAD(tm) Release 12 or greater. **")
           (exit) )

        ( (and (>=  (setq mnth&day&yr (atoi (time  1)))   921231)
               (<=        mnth&day&yr                     200101) )


          (alert (strcat "              Calendar is active from"
                         "\nJanuary 01, 1993 - December 31, 2000 - aborting!"))
          (exit) )

        (t (print)
           (princ (strcat "\nToday "  version " 1993-94 by Cadd Software"))
           (Calendar) ));;;condx2

  (GoEnd  DDG_id)(princ) );;;defun today

(princ "loaded.")
(setvar "cmdecho" 0)
(command  "_.Delay" "1200")
(print)(C:Today)
