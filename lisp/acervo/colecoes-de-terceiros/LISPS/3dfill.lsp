(vmon)
;* UPOINT Userinterface point function
(defun upoint (bit kwd msg def bpt / msg bit inp)
  (if def            ;check for default
  (setq pts (strcat (rtos (car def)) "," (rtos (cadr def)));format for 2d point
       msg (strcat "\n" msg " <" pts ">: ") ; string them with default
       bit (* 2 (fix (/ bit 2))) ;a default and no null bit code conflict so
  )                           ;this reduces bit by 1 if odd, to allow null
   (setq msg (strcat "\n" msg ": "))  ;or without
 ) ;if a default was specified
 (initget bit kwd)
 (if bpt ;check for base point
   (setq inp (getpoint msg bpt)) ;and use it
   (setq inp (getpoint msg))  ;but not if nil
 ) ;if a base point exist
 (if inp inp def) ;evaluate results an return proper value
);defun
;*
;* UINT user interface integer function
(defun uint (bit kwd msg def / msg bit inp)
  (if def ;test for a default
    (setq msg (strcat "\n" msg " <" (itoa def) ">: ") ;string'em with default
       bit (* 2 (fix (/ bit 2 ))) ; adefault and no null bit code conflict so
  )                                ;this reduces bit by 1 if odd, to allow null
  (setq msg (strcat "\n" msg ": ")) ;without default
 );if
  (initget bit kwd)  ;the getint function
  (setq inp (getint msg)) ;the getint funciton
  (if inp inp def);compare the results, return appropriate value
);defun 
;CALC-INC RETURNS  A LIST OF XYZ POINT OFFSETS BETWEEN TWO POINTS AND DIVIDED   
;ROW OR COLUMNS
(DEFUN CALC-INC ( p1 p2 inc )
  (mapcar '/        ;divide the delta in xyz by increment number
    (mapcar '- p1 p2)   ;gets delta of coordinates
    (list inc inc inc)  ;build a dummy lists ot serve as the dividing numbers
  );mapcar
);defun
;*
;*
 ;* 3DFILL fills four 3-D points with rows and columns of 3DFACE's
(defun C:3DFILL ( / pl p2 p3 p4 tp row col inc1-2 inc3-4 inc1-4 inc2-3 )
  (setq p1 (upoint 17 "" "Enter 1st point" nil nil) ;get 1st point
        p2 (upoint 17 "" "Emter 2nd point" nil p1)     ;get 2nd point
        p3 (upoint 17 "" "Enter 3rd point" nil p2)     ;get 3rd point
        p4 (upoint 17 "" "Enter 4th point" nil p3)     ;get 4th point
        row (uint 1 "" "Number of rows (--)" 1) ;get number of rows, 1 is def
        col (uint 1 "" "Number of columns (|||)" 1);get number of columns 1 def
   inc1-2 (calc-inc p2 p1 row) ;calculate XYZ increment from 1st to 2nd point
   inc3-4 (calc-inc p3 p4 row) ;calculate XYZ increment from 4th to 3rd point
   );setq
  (repeat row                    ;repeat 3DFACE fo each row
     (setq inc1-4 (calc-inc p4 p1 col) ;cal temp xyz inc from 4th to 1th pt
          p2 (mapcar '+ p1 inc1-2)   ;cal new 2nd pt
          p3 (mapcar '+ p4 inc3-4)   ;cal new 3rd pt
          inc2-3 (calc-inc p3 p2 col);cal tenp xyz inc for 3rd to 2nd pt
          bp p2    ;save the base point
     );setq
     (repeat col                  ;repeat 3DFACE for each column
       (setq p4 (mapcar '+ p1 inc1-4)        ;cal new 4th pt
              tp (mapcar '+ p2 inc2-3)        ;cal temp pt
       );setq
       (command "3DFACE" p1 p2 tp p4 "")  ;draw 3dface
       (setq p1 p4 p2 tp)          ;set 1st to 4th pt, set 2nd to temp pt
     );repeat                     ;and column repeat
      (setq p4 p3 p1 bp)         ; set 1st & 4th pt to 3rd ptd
  );repeat                    ;and row repeat
 (princ)
);defun c:3DFILL
;*
