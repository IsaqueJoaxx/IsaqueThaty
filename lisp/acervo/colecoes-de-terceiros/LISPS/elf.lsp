;;; ELF.LSP
;;; Copyright 1992,93 by Mountain Software
;;;
;;;*===================================================================*
;;;
;;; This file contains symbolics for color attributes, border types,
;;; and key code values. This file can be included in any AutoLISP
;;; program file to define these symbols in any program that uses ELF.
;;;
;;; Screen attributes (colors)
;;; --------------------------

(Setq
  ;======================== Foreground colors
  black       0 	  ; Black
  blue	      1 	  ; Blue
  green       2 	  ; Green
  cyan	      3 	  ; Cyan
  red	      4 	  ; Red
  magenta     5 	  ; Magenta
  brown       6 	  ; Brown
  lgrey       7 	  ; Light grey (white)
  dgrey       8 	  ; Dark grey
  lblue       9           ; Light blue
  lgreen      10	  ; Light green
  lcyan       11	  ; Light cyan
  lred	      12	  ; Light red
  lmagenta    13	  ; Light magenta
  yellow      14	  ; Yellow
  white       15	  ; Intense white
  intense     16	  ; Intensity bit
  ;======================== Background colors
  black_bg    0 	  ; Black
  blue_bg     16	  ; Blue
  green_bg    32	  ; Green
  cyan_bg     48	  ; Cyan
  red_bg      64	  ; Red
  magenta_bg  80	  ; Magenta
  brown_bg    96	  ; Brown
  lgrey_bg    112	  ; Grey
  blink       128	  ; Blink/intensity bit
  ;======================== Border Attributes
  no_bd       0 	  ; single line border
  single_bd   1 	  ; single line border
  double_bd   2 	  ; double line border
  vdouble_bd  3 	  ; single horizontal, double vertical
  hdouble_bd  4           ; single vertical, double horizontal
  solid_bd    5 	  ; solid border
  tlhl_bd     8 	  ; btype & 8  = hilight upper left
  lrhl_bd     16	  ; btype & 16 = hilight lower right
  shadow_bd   32	  ; btype & 32 = add drop shadow
)

;;;*===== Key Codes returned by (GetKey), the char code can be extracted
;;;	  with the function (lobyte keycode), the scan code is extracted
;;;*===== with (hibyte keycode). The hex keycode is for reference only.

(Setq
;;======================================
;;Key	   decimal	 hex  char  scan
;;	   keycode   keycode  code  code
;;======================================
  F1_Key     15104  ; 0x3b00	 0    59
  F2_Key     15360  ; 0x3c00	 0    60
  F3_Key     15616  ; 0x3d00	 0    61
  F4_Key     15872  ; 0x3e00	 0    62
  F5_Key     16128  ; 0x3f00	 0    63
  F6_Key     16384  ; 0x4000	 0    64
  F7_Key     16640  ; 0x4100	 0    65
  F8_Key     16896  ; 0x4200     0    66
  F9_Key     17152  ; 0x4300	 0    67
  F10_Key    17408  ; 0x4400	 0    68

  S_F1_Key   21504  ; 0x5400	 0    84
  S_F2_Key   21760  ; 0x5500	 0    85
  S_F3_Key   22016  ; 0x5600	 0    86
  S_F4_Key   22272  ; 0x5700	 0    87
  S_F5_Key   22528  ; 0x5800	 0    88
  S_F6_Key   22784  ; 0x5900	 0    89
  S_F7_Key   23040  ; 0x5a00	 0    90
  S_F8_Key   23296  ; 0x5b00	 0    91
  S_F9_Key   23552  ; 0x5c00	 0    92
  S_F10_Key  23808  ; 0x5d00	 0    93

  A_F1_Key   26624  ; 0x6800	 0   104
  A_F2_Key   26880  ; 0x6900	 0   105
  A_F3_Key   27136  ; 0x6a00	 0   106
  A_F4_Key   27392  ; 0x6b00	 0   107
  A_F5_Key   27648  ; 0x6c00	 0   108
  A_F6_Key   27904  ; 0x6d00	 0   109
  A_F7_Key   28160  ; 0x6e00	 0   110
  A_F8_Key   28416  ; 0x6f00	 0   111
  A_F9_Key   28672  ; 0x7000	 0   112
  A_F10_Key  28928  ; 0x7100	 0   113

  C_F1_Key   24064  ; 0x5e00	 0    94
  C_F2_Key   24320  ; 0x5f00	 0    95
  C_F3_Key   24576  ; 0x6000	 0    96
  C_F4_Key   24832  ; 0x6100	 0    97
  C_F5_Key   25088  ; 0x6200	 0    98
  C_F6_Key   25344  ; 0x6300	 0    99
  C_F7_Key   25600  ; 0x6400	 0   100
  C_F8_Key   25856  ; 0x6500	 0   101
  C_F9_Key   26112  ; 0x6600	 0   102
  C_F10_Key  26368  ; 0x6700	 0   103

  Enter_Key   7181  ; 0x1c0d    13    28
  Esc_Key      283  ; 0x011b	27     1
  Tab_Key     3849  ; 0x0f09     9    15
  Ins_Key    20992  ; 0x5200     0    82
  Del_Key    21248  ; 0x5300     0    83
  Home_Key   18176  ; 0x4700     0    71
  End_Key    20224  ; 0x4f00     0    79
  C_Home_Key 30464  ; 0x7700     0   119
  C_End_Key  29952  ; 0x7500     0   117
  C_Ent_Key   7178  ; 0x1c0a    10    28
  C_BS_Key    3711  ; 0x0e7f   127    14
  Up_Key     18432  ; 0x4800     0    72
  Down_Key   20480  ; 0x5000     0    80
  Left_Key   19200  ; 0x4b00     0    75
  Right_Key  19712  ; 0x4d00     0    77
  PgDn_Key   20736  ; 0x5100	 0    81
  PgUp_Key   18688  ; 0x4900	 0    73
  G_Ast_Key  14122  ; 0x372a	42    55
  G_Pls_Key  20011  ; 0x4e2b	43    78
  G_Min_Key  18989  ; 0x4a2d	45    74
)

;;;----- Load ELF.EXP if not already loaded

(If(Not(Member(Findfile "ELF.EXP") (ADS)))
  (If(Not(Xload"ELF" nil))
    (Princ (Strcat
      "\nERROR: Unable to xload ELF.EXP"
      "\nThis program requires ELF to operate!\007"))
  )
)

(princ)

;;; End of ELF.LSP

