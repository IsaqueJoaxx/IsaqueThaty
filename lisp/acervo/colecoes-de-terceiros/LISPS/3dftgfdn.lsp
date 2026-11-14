;               
;*********************************************************************
;*                                                                   *
;*                 3D FOOTING AND FOUNDATION PROGRAM                 *
;*                                                                   *
;*                      BY: Tim Johnson                              *
;*                          March 8 1994                             *
;*                                                                   *
;*                        DR 238 AutoLisp Class                      *
;*               Billings Vocational Technical Center                *
;*                                                                   *
;*  This program will construct a simple 3D footing and foundation.  *
;*            (A nice companion to 3DHOUSE or 3DHOUSE2)              *
;*********************************************************************
;
;This program is placed into the public domain -
;                        Support Education in Your Area
;
;Tim Urbaniak - Instructor   76264,2273
;Billings Vo-Tech
;3803 Central
;Billings, MT  59102
;406-656-4445
;
  (DEFUN DTR (A) (* PI (/ A 180.0)))
  (DEFUN RTD (A) (/ (* A 180.0)))
(DEFUN C:FDN ()
(SETQ CL (GETVAR "CLAYER"))
(SETVAR "BLIPMODE" 0)
(GRAPHSCR)
 (SETQ L    (GETDIST  "\nLength of structure: "))
 (SETQ W    (GETDIST  "\nWidth of structure: "))
 (SETQ FDW  (GETDIST  "\nWidth of foundation: "))
 (SETQ FDH  (GETDIST  "\nHeight of foundation: "))
 (SETQ FTW  (GETDIST  "\nWidth of footing: "))
 (SETQ FTH  (GETDIST  "\nHeight of footing: "))
 (SETQ SP   (GETPOINT "\nStart point for construction: "))
;foundation top points
 (SETQ FD1  (POLAR SP (DTR 0) L)) 
 (SETQ FD2  (POLAR FD1 (DTR 90) W))
 (SETQ FD3  (POLAR FD2 (DTR 180) L))
 (SETQ FD4  (LIST (+ (CAR SP) FDW) (+ (CADR SP) FDW) (CADDR SP))) 
 (SETQ FD5  (LIST (- (CAR FD1) FDW) (+ (CADR FD1) FDW) (CADDR FD1)))
 (SETQ FD6  (LIST (- (CAR FD2) FDW) (- (CADR FD2) FDW) (CADDR FD2)))
 (SETQ FD7  (LIST (+ (CAR FD3) FDW) (- (CADR FD3) FDW) (CADDR FD3)))
    (COMMAND "LAYER" "M" "FOUNDATION" "C"  "WHITE" "" "")
    (COMMAND "3DFACE" SP FD4 FD5 FD1 "")
    (COMMAND "3DFACE" FD1 FD5 FD6 FD2 "")
    (COMMAND "3DFACE" FD2 FD6 FD7 FD3 "")
    (COMMAND "3DFACE" FD3 FD7 FD4 SP "")
;foundation bottom points
 (SETQ FD8  (LIST (CAR SP) (CADR SP) (- (CADDR SP) FDH)))
 (SETQ FD9  (LIST (CAR FD1) (CADR FD1) (- (CADDR FD1) FDH)))
 (SETQ FD10 (LIST (CAR FD2) (CADR FD2) (- (CADDR FD2) FDH)))
 (SETQ FD11 (LIST (CAR FD3) (CADR FD3) (- (CADDR FD3) FDH)))
 (SETQ FD12 (LIST (CAR FD4) (CADR FD4) (- (CADDR FD4) FDH)))
 (SETQ FD13 (LIST (CAR FD5) (CADR FD5) (- (CADDR FD5) FDH)))
 (SETQ FD14 (LIST (CAR FD6) (CADR FD6) (- (CADDR FD6) FDH)))
 (SETQ FD15 (LIST (CAR FD7) (CADR FD7) (- (CADDR FD7) FDH)))
    (COMMAND "3DFACE" SP FD1 FD9 FD8 "")
    (COMMAND "3DFACE" FD1 FD2 FD10 FD9 "")
    (COMMAND "3DFACE" FD2 FD3 FD11 FD10 "")
    (COMMAND "3DFACE" FD3 SP FD8 FD11 "")
    (COMMAND "3DFACE" FD4 FD5 FD13 FD12 "")
    (COMMAND "3DFACE" FD5 FD6 FD14 FD13 "")
    (COMMAND "3DFACE" FD6 FD7 FD15 FD14 "")
    (COMMAND "3DFACE" FD7 FD4 FD12 FD15 "")
;footing top points
 (SETQ FL (/ (- FTW FDW) 2.0))
 (SETQ FT1  (LIST (- (CAR FD8) FL) (- (CADR FD8) FL) (CADDR FD8)))
 (SETQ FT2  (LIST (+ (CAR FD12) FL) (+ (CADR FD12) FL) (CADDR FD12)))
 (SETQ FT3  (LIST (+ (CAR FD9) FL) (- (CADR FD9) FL) (CADDR FD9)))
; 
 (SETQ FT4  (LIST (- (CAR FD13) FL) (+ (CADR FD13) FL) (CADDR FD13)))
 (SETQ FT5  (LIST (+ (CAR FD10) FL) (+ (CADR FD10) FL) (CADDR FD10)))
 (SETQ FT6  (LIST (- (CAR FD14) FL) (- (CADR FD14) FL) (CADDR FD14)))
 (SETQ FT7  (LIST (- (CAR FD11) FL) (+ (CADR FD11) FL) (CADDR FD11)))
 (SETQ FT8  (LIST (+ (CAR FD15) FL) (- (CADR FD15) FL) (CADDR FD15)))
    (COMMAND "LAYER" "M" "FOOTING" "C" "BLUE" "" "")
    (COMMAND "3DFACE" FT1 FT2 FT4 FT3 "")
    (COMMAND "3DFACE" FT3 FT4 FT6 FT5 "")
    (COMMAND "3DFACE" FT5 FT6 FT8 FT7 "")
    (COMMAND "3DFACE" FT7 FT8 FT2 FT1 "")
;footing bottom points
 (SETQ FT9  (LIST (CAR FT1) (CADR FT1) (- (CADDR FT1) FTH))) 
 (SETQ FT10 (LIST (CAR FT2) (CADR FT2) (- (CADDR FT2) FTH)))
 (SETQ FT11 (LIST (CAR FT4) (CADR FT4) (- (CADDR FT4) FTH)))
 (SETQ FT12 (LIST (CAR FT3) (CADR FT3) (- (CADDR FT3) FTH)))
 (SETQ FT13 (LIST (CAR FT5) (CADR FT5) (- (CADDR FT5) FTH)))
 (SETQ FT14 (LIST (CAR FT6) (CADR FT6) (- (CADDR FT6) FTH)))
 (SETQ FT15 (LIST (CAR FT7) (CADR FT7) (- (CADDR FT7) FTH)))
 (SETQ FT16 (LIST (CAR FT8) (CADR FT8) (- (CADDR FT8) FTH)))
    (COMMAND "3DFACE" FT1 FT3 FT12 FT9 "")
    (COMMAND "3DFACE" FT3 FT5 FT13 FT12 "")
    (COMMAND "3DFACE" FT5 FT7 FT15 FT13 "")
    (COMMAND "3DFACE" FT7 FT1 FT9 FT15 "")
    (COMMAND "3DFACE" FT2 FT4 FT11 FT10 "")
    (COMMAND "3DFACE" FT4 FT6 FT14 FT11 "")
    (COMMAND "3DFACE" FT6 FT8 FT16 FT14 "")
    (COMMAND "3DFACE" FT8 FT2 FT10 FT16 "")
    (COMMAND "LAYER" "S" CL "")
 )
 (PRINC "\nThis program will create a 3d footing and foundation ")
 (PRINC "\nExecute with FDN ")
 (PRINC)

