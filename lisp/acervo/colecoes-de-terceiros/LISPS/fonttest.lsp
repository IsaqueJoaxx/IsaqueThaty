;
; FONTTEST.LSP
;
; The Dr.Lisp Utilities TM SM, Version 3.0
; (c)Copyright 1990 RobLand Corporation
; Licensed Material, All Rights Reserved
; ---------------------------------------------------------------------
;
;			W A R N I N G
;
; THIS SOFTWARE AND MANUAL ARE BOTH PROTECTED BY U.S. COPYRIGHT
; LAW (TITLE 17 UNITED STATES CODE).  UNAUTHORIZED REPRODUCTION
; AND/OR SALES MAY RESULT IN IMPRISONMENT OF UP TO ONE YEAR AND
; FINES OF UP TO $10,000 (17 USC 506). COPYRIGHT INFRINGERS MAY
; ALSO BE SUBJECT TO CIVIL LIABILITY.
;
; ---------------------------------------------------------------------

(TextScr)

(PrinC "\n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n ")
(PrinC "\n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n")

(PrinC 
	(StrCat
		"\nThe Dr.Lisp Utilities TM SM, Version 3.0, AutoFont Test..."
		"\n(c)Copyright 1990 RobLand Corporation, Licensed Material, All Rights Reserved"
		"\n-----------------------------------------------------------------------------"
	)
)

(SetVar "CmdEcho" 0)

(DeFun 
	C:FontTest 
	(/ 
		SS Next Ct CoupCt DoIt FontFl GridMd
		Char ChrLst InsPt ChrCt ErsEnt Resp
	)
	(If
		(Null DidFnt)
		(Progn
			(InitGet "Yes No")
			(SetQ Resp (GetKword "\nAre you sure you would like to use FontTest? Yes/<No>: "))
		)
		(SetQ Resp "Yes")
	)
	(If
		(= Resp "Yes")
		(Progn
			(Initlz T)
			(SetQ 
				MorErr 
				'(
					(If GridMd (SetVar "GridMode" GridMd))
					(NilVar 
						'(
							SS Next Ct CoupCt DoIt FontFl GridMd
							Char ChrLst InsPt ChrCt ErsEnt Resp
						)
					)
				)
				InsPt '(60 60)
				CoupCt 0
				GridMd (GetVar "GridMode")
			)
			(If
				(Null Font)
				(While
					(Null Font)
					(SetQ Font (GetString "\nFont name: "))
					(If
						(= Font "")
						(Progn
							(PrinC "\nInvalid response. *Invalid* ")
							(SetQ Font Nil)
						)
						(If
							(And
								(Not (FindFile (StrCat "\\DRL\\" Font "\\" Font ".Shx")))
								(Not (FindFile (StrCat "\\DRL\\" Font ".Shx")))
							)
							(Progn
								(PrinC 
									(StrCat
										"\nShape file "
										(StrCase Font)
										" .SHX does not exist.  *Invalid*"
									)
								)
								(SetQ Font Nil)
							)
							(If
								(Not (SetQ FontFl (FindFile (StrCat "\\DRL\\" Font "\\" Font ".Shx"))))
								(If
									(Not (SetQ FontFl (FindFile (StrCat "\\DRL\\" Font ".Shx"))))
									(SetQ Font Nil)
								)
								
							)
						)
					)
				)
				(If
					(Not (SetQ FontFl (FindFile (StrCat "\\DRL\\" Font "\\" Font ".Shx"))))
					(PrinC
						(StrCat
							"\nShape file "
							(StrCase Font)
							".SHX does not exist yet. "
						)
					)
				)
			)
			(If
				FontFl
				(Progn
					(SetVar "GridMode" 0)
					(If
						(Null DidFnt)
						(Progn
							(PrinC "\nInserting FONTTEST.DWG...\n")
							(SetVar "Expert" 0)
							(Command ".Layer" "S" "FontTest" "Off" "*" "N" "")
							(Command ".Zoom" "W" '(57 -12) '(158 64))
							(Command ".ViewRes" "Yes" "20000")
							(Command 
								".Insert" 
								"*\\DRL\\FontTest"
								'(60 60) "1" "0"
							)
							(SetQ 
								Ct -1 
                                				SS 
								(SSGet "X" (List (Cons 0 "TEXT") (Cons 8 "FONTTEST")))
							)
							(If
								(And 
									SS 
									(> (SSLength SS) 0)
									(/= (Cdr (Assoc 7 (EntGet (SSName SS 0)))) Font)
								)
								(Progn
									(Command 
										".Style" 
										Font FontFl
										"0" "1" "0" "N" "N" "N"
									)
									(PrinC
										(StrCat
											"\nChanging character couplets from font TXT to font "
											(StrCase Font)
											"...\n"
										)
									)
									(While
										(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
										(EntMod
											(Subst
												(Cons 7 Font)
												(Assoc 7 (EntGet Next))
												(EntGet Next)
											)
										)
										(PrinC
											(StrCat
												"\rChanged "
												(IToA (1+ Ct))
												" character couplet"
												(If (= Ct 0) "." "s. ")
												(Percent (1+ Ct) (SSLength SS) 2)
											)
										)
									)
									(SetQ DidFnt T)
								)
							)
							(SetQ SS Nil)
							(GC)
						)
					)
					(ViewChr)
				)
			)
			(If GridMd (SetVar "GridMode" GridMd))
			(SetBck T)
		)
		(PrinC)
	)
)

(DeFun
	ViewChr
	(/ ChrNam LL ChrNum NextGr Resp)
	(Initlz T)
	(SetQ 
		MorErr 
		'(
			(Command ".Zoom" "W" '(57 -12) '(158 64))
			(NilVar '(ChrNam LL ChrNum NextGr Resp))
		)
		ChrNum 32
	)
	(While
		(Null ChrNam)
		(SetQ 
			ChrNam 
			(GetString 
				T 
				(StrCat
					"\nNext "
					(StrCase Font)
					" character to view (Space Bar and Enter when done) <"
					(Chr (SetQ ChrNum (1+ ChrNum)))
					">: "
				)
			)
		)
		(Cond
			(
				(> (StrLen ChrNam) 1)
				(PrinC "\nYou must type ONE character. *Invalid* ")
				(SetQ ChrNam Nil)
			)
			((= ChrNam " "))
			(
				T
				(If (= ChrNam "") (SetQ ChrNam (Chr ChrNum)))
				(PrinC
					(StrCat
						"\nViewing character "
						ChrNam
					)
				)
				(Command 
					".Zoom" 
					"W"
					(SetQ
						LL
						(List
							(Nth (Rem (1- (Ascii ChrNam)) 4) '(59 83 107 131))
							(- 53 (* (Fix (/ (- (Ascii ChrNam) 33) 4)) 3))
						)
					)
					(List
						(+ (Car LL) 25)
						(+ (Cadr LL) 15)
					)
				)
				(GrDraw 
					(SetQ NextGr (Polar LL (/ Pi 2.0) 6))
					(SetQ NextGr (Polar NextGr 0 25))
					1 1
				)
				(GrDraw 
					NextGr
					(SetQ NextGr (Polar NextGr (/ Pi 2.0) 3))
					1 1
				)
				(GrDraw 
					NextGr
					(SetQ NextGr (Polar NextGr Pi 25))
					1 1
				)
				(GrDraw 
					NextGr
					(SetQ NextGr (Polar NextGr (+ Pi (/ Pi 2.0)) 3))
					1 1
				)
				(SetQ 
					ChrNum 
					(If 
						(= (Ascii ChrNam) 126) 
						32 (Ascii ChrNam)
					)
					ChrNam Nil
				)
			)
		)
	)
	(Command ".Zoom" "W" '(57 -12) '(158 64))
	(SetBck T)
)

(PrinC "\n \n \n \n")

(PrinC)
