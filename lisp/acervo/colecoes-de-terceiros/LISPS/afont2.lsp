;
; AFONT2.LSP
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
(SetVar "MenuEcho" 0)

(PrinC "\n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n ")
(PrinC "\n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n")

(PrinC 
	(StrCat
		"\nThe Dr.Lisp Utilities TM SM, Version 3.0, AutoFont Post-Processor..."
		"\n(c)Copyright 1990 RobLand Corporation, Licensed Material, All Rights Reserved"
		"\n-----------------------------------------------------------------------------"
	)
)

(PrinC "\r*")

(SetVar "CmdEcho" 0)

(SetQ InsPt '(0 0))

(DeFun 
	PostProc 
	(
		/ OpnFil Fin BytNm XDisp YDisp XDisp1 YDisp1 Vertx1 Next
		PCount VCount Bulge Vertx NVertx LVertx PlnEnt FileX File1
	)
	(Initlz T)
	(SetQ 
		MorErr 
		(Append
			MorErr
			'(
				(If File1 (SetQ File1 (Close File1)))
				(If FileX (SetQ FileX (Close FileX)))
				(NilVar 
					'(
						OpnFil Fin BytNm XDisp YDisp 
						XDisp1 YDisp1 Vertx1 Next
						PCount VCount Bulge Vertx 
						NVertx LVertx 
						PlnEnt FileX File1
					)
				)
			)
		)
	)
	(If
		DidFnt
		(Progn
			(PrinC "\nErasing character couplets... ")
			(Command ".Erase" "W" '(57 -12) '(158 64) "")
			(SetQ DidFnt Nil)
		)
	)
	(SetVar "LUnits" 5)	
	(SetVar "LUPrec" 0)
	(If (Null Font) (GetFnt "post-process"))
	(PrinC 
		(StrCat 
			"\nPost-processing font " 
			(StrCase DRLDir)
			"\\"
			(StrCase Font) 
			"... \n"
		)
	)
	(SetVar "GridMode" 1)
	(Command ".View" "R" "Normal")
	(Command 
		".Layer" "On" "0,Charactr" "S" "0" "Off" 
		"Ascii,NextLett,Vertical,FontTest" ""
	)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Flag Nil SS (SSGet "C" (GetVar "ExtMin") (GetVar "ExtMax")))
	(If SS (Command ".Erase" SS ""))
	(SetQ FileX (Open (StrCat DRLDir "\\" Font "\\PostProc.Lst") "r"))
	(While 
		(SetQ ChrLst (Read-Line FileX))
		(If (Null Flag) (SetQ Flag 1 ChrLst (Read-Line FileX)))
		(SetQ 
			ChrLst (Read ChrLst) 
			ChrFl1 (Nth 0 ChrLst)
			ChrFil (Nth 1 ChrLst) 
			FinPt (Nth 2 ChrLst) 
			VPt1 (Nth 3 ChrLst) 
			VPt2 (Nth 4 ChrLst)
		)
		(If 
			(SetQ 
				OpnFil 
				(Open 
					(StrCat 
						DRLDir "\\" Font 
						"\\" ChrFl1 ".Fnt"
					) 
					"r"
				)
			)
			(Progn
				(SetQ OpnFil (Close OpnFil))
				(Command 
					"Sh" 
					(StrCat 
						"Copy " 
						DRLDir "\\" Font "\\" ChrFl1 ".Fnt " 
						DRLDir "\\" Font "\\" ChrFl1 ".Old"
					)
				)
				(Command 
					"Sh" 
					(StrCat 
						"Erase " DRLDir "\\" 
						Font "\\" ChrFl1 ".Fnt"
					)
				)
			)
		)
		(GraphScr)
		(PrinC 
			(StrCat 
				"\nPost-processing "
				(If VPt1 "Ascii character " "shape ")
				(StrCase DRLDir)
				"\\"  
				(StrCase Font) 
				"\\"  
				(StrCase ChrFil) 
				"... "
			)
		)
		(Command 
			".Insert" 
			(StrCat ChrFil "=" DRLDir "\\" Font "\\" ChrFil) 
			"0,0"
			"" "" "0"
		)
		(Command ".Explode" "l")
		(If
			VPt1
			(SetQ 
				ChrNum ChrFil
				Start (StrCat "*" ChrFl1 ",") 
				NewStr ",3,6,2,14," 
				XDisp (- (Car InsPt) (Car VPt1))
				YDisp (- (Cadr InsPt) (Cadr VPt1))
				BytNm 4 LVertx InsPt PCount 0
			)
			(SetQ 
				ChrNum ChrFil
				Start (StrCat "*" ChrFl1 ",") 
				NewStr ",3,96,2," 
				XDisp 0
				YDisp 0
				BytNm 3 LVertx InsPt PCount 0
			)
		)
		(If 
			(SetQ File1 (Open (StrCat DRLDir "\\" Font "\\Temp.Tmp") "r")) 
			(Progn
				(SetQ File1 (Close File1))
				(Command 
					"Sh" 
					(StrCat "Erase " DRLDir "\\" Font "\\Temp.Tmp")
				)
			)
		)
		(If
			(Or 
				(> (Abs XDisp) 127) 
				(> (Abs YDisp) 127)
			)
			(SetQ 
				XDisp1 (Fix (/ XDisp 2.0)) 
				YDisp1 (Fix (/ YDisp 2.0))
				NewStr 
				(StrCat 
					NewStr "8,(" (RToS XDisp1) "," 
					(RToS YDisp1) "),14,8,(" (Updt1) "," 
					(Updt2) "),"
				) 
				BytNm (+ BytNm 7)
			)
			(If
				(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
				(SetQ 
					NewStr 
					(StrCat 
						NewStr "8,(" (RToS XDisp) "," 
						(RToS YDisp) "),"
					) 
					BytNm (+ BytNm 3)
				)
			)
		)
		(MakStr)
		(If SS (Progn (SetQ SS Nil) (GC)))
		(SetQ SS (SSGet "C" (GetVar "VSMin") (GetVar "VSMax")) LinCt 0 Fin Nil)
		(While 
			(SetQ PlnEnt (SSName SS PCount))
			(SetQ VCount 0 NVertx (EntNext PlnEnt) NBulge 0 Cl 0)
			(If (= (Cdr (Assoc 70 (EntGet PlnEnt))) 1) (SetQ Cl 1))
			(While 
				(/= 
					(Cdr (Assoc 0 (SetQ Vertx (EntGet NVertx)))) 
					"SEQEND"
				)
				(Prnt Vertx)
			)
			(If 
				(= Cl 1) 
				(Progn
					(NDisp Vertx1)
					(If
						(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
						(SetQ 
							NewStr 
							(StrCat 
								NewStr "(" (RToS XDisp)
								"," (RToS YDisp) "," Bulge "),"
							) 
							BytNm (+ BytNm 3)
						)
					)
					(MakStr)
					(SetQ LVertx (Cdr (Assoc 10 (EntGet (EntNext PlnEnt)))))
				)
			)
			(SetQ 
				PCount (1+ PCount) 
				NewStr (StrCat NewStr "(0,0),2,")
				BytNm (+ BytNm 3)
			)
			(MakStr)
		)
		(SetQ 
			XDisp (- (Car FinPt) (Car LVertx))
			YDisp (- (Cadr FinPt) (Cadr LVertx))
		)
		(If 	
			(Or 
				(> (Abs XDisp) 127) 
				(> (Abs YDisp) 127)
			)
			(SetQ 
				XDisp1 (Fix (/ XDisp 2.0)) 
				YDisp1 (Fix (/ YDisp 2.0))
				NewStr 
				(StrCat 
					NewStr "9,(" (RToS XDisp1) "," 
					(RToS YDisp1) "),(" (Updt1) "," 
					(Updt2) "),(0,0),"
				) 
				BytNm (+ BytNm 7)
			)
			(If
				(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
				(SetQ 
					NewStr 
					(StrCat 
						NewStr "8,(" (RToS XDisp) "," 
						(RToS YDisp) "),"
					) 
					BytNm (+ BytNm 3)
				)
				(SetQ NewStr (StrCat NewStr ","))
			)
		)
		(If 
			VPt1 
			(SetQ 
				NewStr (StrCat NewStr "14,") 
				BytNm (1+ BytNm)
			)
		)
		(MakStr)
		(If
			VPt2
			(SetQ 
				XDisp (- (Car VPt2) (Car FinPt))
				YDisp (- (Cadr VPt2) (Cadr FinPt))
			)
			(SetQ
				XDisp (Car FinPt)
				YDisp (Cadr FinPt)
			)
		)
		(If
			(Or 
				(> (Abs XDisp) 127) 
				(> (Abs YDisp) 127)
			)
			(SetQ 
				XDisp1 (Fix (/ XDisp 2.0)) 
				YDisp1 (Fix (/ YDisp 2.0))
				NewStr 
				(StrCat 
					NewStr "8,(" (RToS XDisp1) "," 
					(RToS YDisp1) "),14,8,(" (Updt1) "," 
					(Updt2) "),"
				) 
				BytNm (+ BytNm 7)
			)
			(If
				(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
				(SetQ 
					NewStr 
					(StrCat 
						NewStr "8,(" (RToS XDisp) "," 
						(RToS YDisp) "),"
					) 
					BytNm (+ BytNm 3)
				)
				(SetQ NewStr (StrCat NewStr ","))
			)
		)
		(MakStr)
		(SetQ 
			NewStr 
			(If
				VPt1
				(StrCat NewStr "4,6,0") 
				(StrCat NewStr "4,96,0")
			)
			BytNm (+ BytNm 3) 
			Fin 1
		)
		(MakStr)
		(SetQ OpnFil (Open (StrCat DRLDir "\\" Font "\\Start.Tmp") "w"))
		(Write-Line (StrCat Start (IToA BytNm) "," ChrFil) OpnFil)
		(SetQ OpnFil (Close OpnFil))
		(If 
			(SetQ 
				OpnFil 
				(Open (StrCat DRLDir "\\" Font "\\" ChrFl1 ".Fnt") "r")
			)
			(Progn
				(SetQ OpnFil (Close OpnFil))
				(Command 
					"Sh" 
					(StrCat 
						"Copy " DRLDir "\\" Font "\\" ChrFl1 ".Fnt " Font 
						"\\" (IToA ChrNum) ".Old"
					)
				)
				(Command 
					"Sh" 
					(StrCat "Erase " DRLDir "\\" Font "\\" ChrFl1 ".Fnt")
				)
			)
		)
		(Command 
			"Sh" 
			(StrCat 
				"Copy " 
				DRLDir "\\" Font "\\Start.Tmp +" 
				DRLDir "\\" Font "\\Temp.Tmp "
				DRLDir "\\" Font "\\" ChrFl1 ".Fnt"
			)
		)
		(Command "Sh" (StrCat "Erase " DRLDir "\\" Font "\\*.Tmp"))
		(Command ".Erase" "w" (GetVar "ExtMin") (GetVar "ExtMax") "")
	)
	(SetQ FileX (Close FileX))
	(PrinC 
		(StrCat 
			"\nPost-processing of font "
			(StrCase DRLDir)
			"\\"  
			(StrCase Font) 
			" complete. "
		)
	)
	(SetBck T)
)

(PrinC "\010-------*")

(DeFun 
	SavFil 
	(/ FonTst Go)
	(Initlz T)
	(SetQ 
		MorErr (Append MorErr '((NilVar '(FonTst Go))))
		Go Nil
	)
	(If
		DidFnt
		(Progn
			(PrinC "\nErasing character couplets... ")
			(Command ".Erase" "W" '(57 -12) '(158 64) "")
			(SetQ DidFnt Nil)
		)
	)
	(If (Null Font) (GetFnt "save"))
	(If 
		(SetQ 
			OpnFil 
			(Open (SetQ Hdr (StrCat DRLDir "\\" Font "\\" "Afont.Hdr")) "r")
		)
		(Progn
			(SetQ OpnFil (Close OpnFil))
			(If 
				(SetQ OpnFil (Open (StrCat DRLDir "\\" Font "\\" Font ".Shp") "r"))
				(Progn
					(SetQ OpnFil (Close OpnFil))
					(Command 
						"Sh" 
						(StrCat 
							"Copy " 
							DRLDir "\\" Font "\\" Font ".Shp " 
							DRLDir "\\" Font "\\" Font ".Bak"
						)
					)
				)
			)
			(Command 
				"Sh" 
				(StrCat 
					"Copy " 
					DRLDir "\\" Font "\\Afont.Hdr+" 
					DRLDir "\\" Font "\\??.Fnt+" 
					DRLDir "\\" Font "\\1??.Fnt " 
					DRLDir "\\" Font "\\" Font ".Shp"
				)
			)
			(GraphScr)
			(PrinC (StrCat "\nFont " (StrCase DRLDir) "\\" (StrCase Font) " saved. \n"))
		)
		(Prompt 
			(StrCat 
				"\nThere are no shape descriptions for shape file "
				(StrCase DRLDir)
				"\\"
				(StrCase Font) ". "
			)
		)
	)
	(SetBck T)
)

(PrinC "\010-------*")

(DeFun 
	Prnt 
	(Vertx)
	(If 
		(< VCount 1)
		(Progn
			(PrinC "*")
			(SetQ Vertx1 Vertx)
			(NDisp Vertx)
			(If
				(Or 
					(> (Abs XDisp) 127) 
					(> (Abs YDisp) 127)
				)
				(SetQ 
					XDisp1 (Fix (/ XDisp 2.0)) 
					YDisp1 (Fix (/ YDisp 2.0))
					NewStr 
					(StrCat 
						NewStr "8,(" (RToS XDisp1) "," 
						(RToS YDisp1) "),8,(" (Updt1) "," 
						(Updt2) "),1,"
					) 
					BytNm (+ BytNm 7)
				)
				(If
					(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
					(SetQ 
						NewStr 
						(StrCat 
							NewStr "8,(" (RToS XDisp) "," 
							(RToS YDisp) "),1,"
						) 
						BytNm (+ BytNm 4)
					)
					(SetQ NewStr (StrCat NewStr ","))
				)
			)
			(MakStr)
		)
		(Progn
			(PrinC "*")
			(NDisp Vertx)
			(If 
				(< VCount 2)
				(If 
					(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
					(Progn
						(If
							(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
							(SetQ 
								NewStr 
								(StrCat 
									NewStr "13,("
									(RToS XDisp) "," 
									(RToS YDisp)
									"," Bulge "),"
								)
								BytNm (+ BytNm 4)
							)
						)
						(MakStr)
					)
					(SetQ 
						NewStr 
						(StrCat NewStr "13,")
						BytNm (+ BytNm 1)
					)
				)
				(Progn
					(If 
						(Not (Equal '(0 0) (List XDisp YDisp) 0.75))
						(SetQ 
							NewStr 
							(StrCat 
								NewStr "("
								(RToS XDisp) "," 
								(RToS YDisp)
								"," Bulge "),"
							)
							BytNm (+ BytNm 3)
						)
					)
					(MakStr)
				)
			)
			(SetQ NVertx (EntNext NVertx))
		)
	)
	(SetQ 
		VCount (1+ VCount)
		LVertx (Cdr (Assoc 10 Vertx))
	)
)

(PrinC "\010-------*")

(DeFun 
	NDisp 
	(Vertx)
	(SetQ 
		LXDlta (Car LVertx)
		LYDlta (Cadr LVertx)
		NXDlta (Cadr (Assoc 10 Vertx))
		NYDlta (Caddr (Assoc 10 Vertx))
		XDisp 
		(If
			(/= (Abs (- NXDlta LXDlta)) 0)
			(- NXDlta LXDlta)
			0
		)
		YDisp 
		(If
			(/= (Abs (- NYDlta LYDlta)) 0)
			(- NYDlta LYDlta)
			0
		)
		Bulge (IToA NBulge)
		NBulge (Fix (* (Cdr (Assoc 42 Vertx)) 127))
	)
	(If (< NBulge 0) (SetQ NBulge (1- NBulge)))
)

(PrinC "\010-------*")

(DeFun 
	MakStr 
	()
	(If 
		(Or (= Fin 1) (> (StrLen NewStr) 45))
		(Progn
			(If 
				(= (SubStr NewStr (StrLen NewStr)) ",")
				(SetQ 
					NewStr 
					(SubStr 
						NewStr 1
						(- (StrLen NewStr) 1)
					)
				)
			)
			(If 
				(= (SubStr NewStr 1 1) ",")
				(SetQ NewStr (SubStr NewStr 2))
			)
			(SetQ File1 (Open (StrCat DRLDir "\\" Font "\\Temp.Tmp") "a")) 
			(Write-Line NewStr File1)
			(SetQ 
				File1 (Close File1)
				NewStr ""
			)
			(PrinC "*")
		)
	)
)

(PrinC "\010-------*")

(DeFun Updt1 () (RToS (- XDisp XDisp1)))

(DeFun Updt2 () (RToS (- YDisp YDisp1)))

(DeFun 
	ChrNam 
	(ChrFil / Name)
	(SetQ Name (RToS (Ascii ChrFil) 5 0))
	(Cadr (Assoc Name AscLst))
)

(PrinC "\010-------*")

(DeFun
	AllPost
	(/ NoAsk)
	(Initlz T)
	(SetQ 
		MorErr (Append MorErr '((NilVar '(NoAsk))))
		NoAsk T
	)
	(If (Null Font) (GetFnt "compile"))
	(PostProc)
	(SavFil)
	(EndSes)
	(SetBck T)
)

(PrinC "\010-------*")

(DeFun 
	EndSes 
	(/ Go)
	(Initlz T)
	(SetQ MorErr (Append MorErr '((NilVar '(Go)))))
	(If (Null Font) (GetFnt "compile"))
	(If
		(Null NoAsk)
		(Progn
			(InitGet "Yes No")
			(SetQ 
				Go 
				(GetKword 
					(StrCat
						"\nDo you wish to terminate this editing session"
						"\nand compile font " 
						(StrCase DRLDir)
						"\\"
						(StrCase Font)
						"? Yes/<No>: "
					)
				)
			)
		)
	)
	(If 
		(Or (= Go "Yes") NoAsk)
		(Progn
			(SetQ OpnFil (Open (StrCat DRLDir "\\AutoFONT.Scr") "w"))
			(PrinC
				(StrCat 
					"Quit\nY\n7\n"
					DRLDir "\\" Font "\\" Font "\n"
				)
				OpnFil
			)
			(SetQ OpnFil (Close OpnFil))
			(Command ".Script" (StrCat DRLDir "\\AutoFONT"))
		)
	)
	(SetBck T)
)

(PrinC "\010-------*")

(Defun
	NilVar
	(VarLst)
	(ForEach Var VarLst (Set Var Nil))
)

(PrinC "\010-------*")

(DeFun
	Initlz
	(IniFlg)
	(If
		IniFlg
		(Progn
			(SetQ
				PasVal Nil
				CmdEc (GetVar "CmdEcho")
			)
			(SetVar "CmdEcho" 0)
			(DeFun
				*Error*
				(EMsg)
				(If 
					(/= EMsg "Function cancelled")
					(Prompt (StrCat "\nError: " EMsg "\n"))
				)
				(If CmdOpn (Command))
				(If OpnFil (SetQ OpnFil (Close OpnFil)))
				(If
					MorErr 
					(ForEach
						Item
						MorErr
						(Eval Item)
					)
				)
				(If CLay (Command ".Layer" "S" CLay ""))
				(If CmdEc (SetVar "CmdEcho" CmdEc))
				(SetQ *Error* PrvErr)
				(NilVar 
					'(
						CmdEc CLay CmdOpn Item 
						MorErr PasVal
					)
				)
				(GC)
				(PrinC)
			)
		)
		(SetQ PasVal Nil)
	)               
)

(PrinC "\010-------*")

(DeFun
	SetBck
	(StbFlg / Item)
	(If
		StbFlg
		(Progn
			(If CmdOpn (Command))
			(If OpnFil (SetQ OpnFil (Close OpnFil)))
			(If CLay (Command ".Layer" "S" CLay ""))
			(If CmdEc (SetVar "CmdEcho" CmdEc))
			(SetQ *Error* PrvErr)
			(NilVar '(CmdEc CLay CmdOpn Item MorErr))
			(GC)
		)
	)
	(If PasVal PasVal (PrinC))
)

(PrinC "\010-------*")

(DeFun
	GetFnt
	(Msg)
	(SetQ Go Nil)
	(While 
		(/= Go "Yes") 
		(SetQ FonTst Nil)
		(While 
			(O)
			)
		)
		(If 
			(Or (= Go "Yes") (= Go Nil)) 
			(SetQ Font FonTst Go "Yes")
		)
	)
)

(PrinC "\010-")

(GraphScr)

(PrinC "\n \n \nAutoFONT Post-Processor loaded. ")

(SetQ AFont2 T)

(PrinC) 
opqrstuvwxyzˇˇ|ˇˇ~ÄÅÇÉÑÖÜáàâäãåçéèêëíìîïñóòôöˇˇúˇˇûü†°¢£§•¶ß®ˇˇ™´ˇˇ≠ÆØ∞±≤≥¥µ∂ˇˇ∏ˇˇ∫ªºΩæˇˇ                                    “”‘’÷◊ÿŸ⁄€‹›ﬁﬂ‡·‚„‰ÂÊÁËÈÍÎÏˇˇÓÔÒÚÛÙıˆ˜¯F              )
			)
		)
		(If 
			(Or (= Go "Yes") (= Go Nil)) 
			(SetQ Font FonTst Go "Yes")
		)
	)
)

(PrinC "\010-")

(GraphScr)

(PrinC "\n \n \nAutoFONT Post-Processor loaded. ")

(SetQ AFont2 T)

(PrinC) 
