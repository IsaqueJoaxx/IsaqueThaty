;
; AFONT1.LSP
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
		"\nThe Dr.Lisp Utilities TM SM, Version 3.0, AutoFont Pre-Processor..."
		"\n(c)Copyright 1990 RobLand Corporation, Licensed Material, All Rights Reserved"
		"\n-----------------------------------------------------------------------------"
	)
)

(PrinC "\r*")

(SetVar "PDMode" 34)
(SetVar "CmdEcho" 0)

(SetQ HPi (/ Pi 2.0))

(If (Null ShapCt) (SetQ ShapCt 149))

(DeFun 
	DefChr 
	(/ Resp Go DwgFil Hdr)
	(Initlz T)
	(SetQ 
		MorErr 
		'(
			(If DwgFil (SetQ DwgFil (Close DwgFil)))
			(NilVar '(Resp Go DwgFil Hdr))
		)
		Go Nil
	)
	(SetVar "GridMode" 1)
	(Command ".View" "R" "Normal")
	(SetVar "Expert" 2)
	(If
		DidFnt
		(Progn
			(PrinC "\nErasing character couplets... ")
			(Command ".Erase" "W" '(57 -12) '(158 64) "")
			(SetQ DidFnt Nil)
		)
	)
	(Command 
		".Layer" "On" "*" "S" "Charactr" 
		"Off" "Ascii,NextLett,Vertical,FontTest" ""
	)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(If 
		(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
		(Progn
			(InitGet "No Yes")
			(SetQ 
				Resp 
				(GetKword "\nDo you wish to erase the objects drawn on the screen? Yes/<No>: ")
			)
			(If (= Resp "Yes") (Command ".Erase" SS ""))
		)
	)
	(AscShp)
	(If
		(SetQ SS1 (SSGet "X" (List (Cons 8 "NextLett"))))
		(Progn
			(Command ".Erase" SS1 "")
			(SetQ SS1 Nil)
			(GC)
		)
	)
	(If
		(SetQ SS1 (SSGet "X" (List (Cons 8 "Vertical"))))
		(Progn
			(Command ".Erase" SS1 "")
			(SetQ SS1 Nil)
			(GC)
		)
	)
	(SetVar "RegenMode" 0)
	(Command ".ViewRes" "Y" "20000")
	(SetQ 
		ChrFil Nil 
		Go Nil 
		TypSpc 
		(If (= ChrTyp "Ascii") "Ascii character" "Shape")
	)
	(While 
		(/= Go "Yes")
		(While 
			(Or (= ChrFil Nil) (= ChrFil ""))
			(SetQ 
				ChrFil 
				(GetString (StrCat "\n" TypSpc " to create: "))
				ChrFil
				(If
					(= ChrTyp "Ascii")
					(SubStr ChrFil 1 1)
					ChrFil
				)
			)
		)
		(SetQ ChrFil (If (= ChrTyp "Ascii") ChrFil (StrCase ChrFil)))
		(InitGet "Yes No")
		(SetQ 
			Go 
			(GetKword 
				(StrCat 
					"\n" 
					TypSpc 
					" to create is " 
					ChrFil 
					" No/<Yes>: "
				)
			)
		)
		(If (Null Go) (SetQ Go "Yes"))
		(If (/= Go "Yes") (SetQ ChrFil Nil))
	)
	(SetQ Go Nil)
	(While 
		(/= Go "Yes") 
		(SetQ FonTst Nil)
		(While 
			(Member FonTst '("" Nil))
			(SetQ 
				FonTst 
				(GetString 
					(StrCat 
						"\nName of font for " 
						TypSpc
						" " 
						ChrFil 
						" "
						(If 
							Font 
							(StrCat 
								"<" 
								(StrCase Font) 
								">"
							) 
							""
						)
						": "
					)
				)
			)
			(If
				(And Font (= FonTst "")) 
				(SetQ FonTst Font)
			)
		)
		(InitGet "Yes No")
		(SetQ 
			Go 
			(GetKword 
				(StrCat
					"\nName of font for " 
					TypSpc 
					" " 
					ChrFil 
					" is " 
					(StrCase FonTst) 
					" No/<Yes>: "
				)
			)
		)
		(If 
			(Or (= Go "Yes") (= Go Nil))
			(SetQ Font FonTst Go "Yes")
			(SetQ FonTst Nil Font Nil)
		)
	)
	(SetQ OpnFil Nil)
	(If (/= NewFnt Font) (SetQ OverW Nil))
	(If 
		(Not 
			(SetQ 
				OpnFil 
				(Open 
					(SetQ 
						Hdr 
						(StrCat 
							"\\DRL\\"
							Font 
							"\\Afont.Hdr"
						)
					) 
					"r"
				)
			)
		)
		(If
			(= ChrTyp "Ascii")
			(Progn
				(Command ".Sh" (StrCat "MD \\DRL\\" Font))
				(SetQ OpnFil (Open Hdr "w"))
				(Write-Line (StrCat "*0,4," Font " " (TheDate)) OpnFil)
				(Write-Line "21,7,2,0" OpnFil)
				(Write-Line "*10,9,lf" OpnFil)
				(Write-Line "2,8,(0,-36),14,8,(30,36),0" OpnFil)
				(Write-Line "*32,9,spc" OpnFil)
				(Write-Line "2,8,(19,0),14,8,(-19,-30),0" OpnFil)
				(SetQ OpnFil (Close OpnFil))
			)
			(Progn
				(Command ".Sh" (StrCat "MD \\DRL\\" Font))
				(SetQ OpnFil (Open Hdr "w"))
				(SetQ OpnFil (Close OpnFil))
			)
		)
	)
	(SetQ Go Nil)
	(GraphScr)
	(While 
		(/= Go "Yes")
		(InitGet "Yes No")
		(SetQ 
			Resp
			(GetKword 
				(StrCat 
					"\nAdapt " 
					ChrTyp 
					" " 
					ChrFil 
					" from an existing Ascii character or shape? Yes/<No>: "
				)
			)
		)
		(If 
			(= Resp "Yes")
			(Progn
				(SetQ 
					OldChr 
					(GetString 
						(StrCat
							"\nCharacter to adapt from <" 
							ChrFil 
							">: "
						)
					)
				)
				(If (= OldChr "") (SetQ OldChr ChrFil))
				(SetQ 
					OldFnt 
					(GetString 
						(StrCat
							"\nName of font for existing " 
							OldChr 
							" <" 
							(SetQ 
								OFnt 
								(StrCase 
									(If 
										(Or 
											(= OldFnt Nil) 
											(= OldFnt "")
										) 
										Font 
										OldFnt
									)
								)
							) 
							">: "
						)
					)
				)
				(If (= OldFnt "") (SetQ OldFnt OFnt))
				(If 
					(SetQ 
						DwgFil 
						(Open 
							(StrCat 
								"\\DRL\\"
								OldFnt

								"\\" 
								(SetQ
									OldDwg 
									(If 
										(= ChrTyp "Ascii")
										(ChrNam OldChr) 
										OldChr
									)
								) 
								".Dwg"
							) 
							"r"
						)
					)
					(Progn
						(SetQ 
							DwgFil 
							(Close DwgFil) 
							Go 
							"Yes"
						)
						(Command 
							".Insert" 
							(StrCat 
								OldDwg 
								"=\\DRL\\"
								OldFnt 
								"\\" 
								OldDwg
							)
							(If 
								(= ChrTyp "Ascii")
								"0,0" 
								"63,28"
							) 
							"" 
							"" 
							"0"
						)
						(Command ".Explode" "l")
					)
					(Prompt 
						(StrCat 
							"\\DRL\\" 
							(StrCase OldFnt)
							"\\" 
							(StrCase OldDwg) 
							".DWG does not exist. "
						)
					)
				)
			)
			(SetQ Go "Yes")
		)
	)
	(If 
		(And
			(Null Overw)
			(FindFile (StrCat "\\DRL\\" Font "\\PostProc.Lst"))
		)
		(Progn
			(InitGet "Yes No")
			(SetQ 
				OverW
				(GetKword 
					(StrCat
						"\nOverwrite post-processing list for font "
						(StrCase Font)
						"? Yes/<No>: "
					)
				)
				OverW (If (= OverW "Yes") "Yes" "No")
			)
		)
		(SetQ OverW "No")
	)
	(If 
		(= OverW "Yes")
		(Progn
			(Command 
				"Sh" 
				(StrCat 
					"Erase \\DRL\\" 
					Font "\\PostProc.Lst"
				)
			)
			(SetQ OverW "No")
		)
	)
	(SetVar "Expert" 2)
	(Command 
		".Layer" "On" 
		(If
			(= ChrTyp "Ascii")
			"0,Charactr,Ascii" 
			"0,Charactr"
		)
		"S" "Charactr" "Off" "NextLett,Vertical,FontTest" ""
	)
	(GraphScr)
	(SetQ 
		ChrFl1 
		(If 
			(= ChrTyp "Ascii") 
			(ChrNam ChrFil) 
			ChrFil
		) 
	)
	(PrinC 
		(StrCat
			"\nDefinition complete. Ready to create "
			TypSpc
			" "
			ChrFil " "
			(StrCase Font) 
			"\\" (StrCase ChrFl1) 
			". "
		)
	)
	(SetQ NewFnt Font)
	(SetBck T)
)

(PrinC "\010------*")

(DeFun 
	SavCh1 
	(/ 
		DoneCt Next Ct Ent P1 CPt Dis Ang1 Ang2 
		IncAng Pt10 Pt11 DoAgin JoinCt High Go
	)
	(Initlz T)
	(SetQ 
		MorErr 
		(Append 
			MorErr 
			(List 
				'(If High (SetVar "Highlight" High))
				'(NilVar 
					'(
						Ent P1 CPt Dis DoneCt DoAgin
						Ang1 Ang2 IncAng Pt10 Pt11
						Next Ct JoinCt High Go
					)
				)
			)
		)
		Go 0
		High (GetVar "Highlight")
	)
	(SetVar "Highlight" 1)
	(If
		DidFnt
		(Progn
			(PrinC "\nErasing character couplets... ")
			(Command ".Erase" "W" '(57 -12) '(158 64) "")
			(SetQ DidFnt Nil)
		)
	)
	(PrinC 
		(StrCat 
			"\nCleaning up " 
			TypSpc 
			" " 
			ChrFil 
			(If (= TypSpc "Shape") "..." " ...")
		)
	)
	(SetVar "GridMode" 1)
	(SetVar "Expert" 2)
	(Command 
		".Layer" "On" "Charactr" "S" "Charactr"
		"Off" "0,Ascii,NextLett,Vertical,FontTest" ""
	)
	(SetVar "BlipMode" 0)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Ct -1 DoneCt 0 DoAgin Nil)
	(While
		(And
			(/= DoAgin "GetOut")
			(Or
				(And
					(NumberP DoAgin)
					(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
				)
				(And
					(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
					(And SS (> (SSLength SS) 0))
				)
			)
		)
		(Progn
			(PrinC "\n \nExploding blocks")
			(If 
				DoAgin 
				(PrinC (StrCat ", Pass " (IToA DoAgin)))
			)
			(PrinC "...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If
					(= (Cdr (Assoc 0 (EntGet Next))) "INSERT")
					(Progn
						(Command ".Explode" Next)
						(SetQ DoneCt (1+ DoneCt))
					)
				)
				(PrinC
					(StrCat
						"\rExploded "
						(IToA DoneCt)
						" block"
						(If (= DoneCt 1) "" "s")
						". "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
			(If 
				(= DoneCt 0) 
				(SetQ DoAgin "GetOut") 
				(SetQ 
					Ct -1 
					DoneCt 0 
					DoAgin
					(If (Null DoAgin) 2 (1+ DoAgin))
				)
			)
		)
	)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Ct -1 DoneCt 0)
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nErasing points...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If 
					(= (Cdr (Assoc 0 (SetQ Ent (EntGet Next)))) "POINT")
					(Progn
						(Command ".Erase" Next "")
						(SetQ DoneCt (1+ DoneCt))
					)
				)
				(PrinC
					(StrCat
						"\rErased "
						(IToA DoneCt)
						" point"
						(If (= DoneCt 1) "" "s")
						". "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Ct -1 DoneCt 0)
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(And SS (> (SSLength SS) 0))
		)
		(Progn
			(PrinC "\n \nExploding polylines...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If
					(= (Cdr (Assoc 0 (EntGet Next))) "POLYLINE")
					(Progn
						(Command ".Explode" Next)
						(SetQ DoneCt (1+ DoneCt))
					)
				)
				(PrinC
					(StrCat
						"\rExploded "
						(IToA DoneCt)
						" polyline"
						(If (= DoneCt 1) "" "s")
						". "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(SetQ Ct -1 DoneCt 0)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nConverting circles to arcs...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If 
					(= (Cdr (Assoc 0 (SetQ Ent (EntGet Next)))) "CIRCLE")
					(Progn
						(Command ".Erase" Next "")
						(Command 
							".Arc" 
							(SetQ 
								P1 
								(Polar 
									(SetQ 
										CPt 
										(Cdr (Assoc 10 Ent))
									) 
									(+ Pi (/ Pi 2.0))
									(SetQ Dis (Cdr (Assoc 40 Ent)))
								)
							)
							"C" 
							CPt 
							(SetQ P2 (Polar CPt (/ Pi 2.0) Dis))
						)
						(Command ".Arc" P2 "C" CPt P1)
						(SetQ DoneCt (1+ DoneCt))
					)
				)
				(PrinC
					(StrCat
						"\rConverted "
						(IToA DoneCt)
						" circle"
						(If (= DoneCt 1) " to an" "s to")
						" arc"
						(If (= DoneCt 1) ". " "s.  ")
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(SetQ Go 0 Ct -1 DoneCt 0)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Ct 0)
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nBreaking large angle arcs...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If 
					(= (Cdr (Assoc 0 (SetQ Ent (EntGet Next)))) "ARC")
					(Progn
						(SetQ 
							Ang1 (Cdr (Assoc 50 Ent))
							Ang2 (Cdr (Assoc 51 Ent))
							IncAng 
							(If 
								(< Ang1 Ang2) 
								(- Ang2 Ang1)
								(+ 
									(- (* 2 Pi) Ang1) 
									Ang2
								)
							)
						)
						(If 
							(> IncAng Pi)
							(Progn
								(Command 
									".Break" 
									Next 
									(SetQ 
										BPt 
										(Polar 
											(Cdr (Assoc 10 Ent))
											(If 
												(< Ang1 Ang2) 
												(/ (+ Ang1 Ang2) 2.0)
												(- (/ (+ Ang1 Ang2) 2.0) Pi)
											)
											(Cdr (Assoc 40 Ent))
										)
									) 
									BPt
								)
								(SetQ DoneCt (1+ DoneCt))
							)
						)
					)
				)
				(PrinC
					(StrCat
						"\r"
						(IToA DoneCt)
						" large angle arc"
						(If (= DoneCt 1) "" "s")
						" broken. "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(SetQ Go 0 Ct -1 DoneCt 0)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nBreaking up long arcs...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If 
					(= (Cdr (Assoc 0 (SetQ Ent (EntGet Next)))) "ARC")
					(Progn
						(SetQ 
							Ang1 (Cdr (Assoc 50 Ent))
							Ang2 (Cdr (Assoc 51 Ent))
							CPt (Cdr (Assoc 10 Ent))
							Pt10 (Polar CPt Ang1 (Cdr (Assoc 40 Ent)))
							Pt11 (Polar CPt Ang2 (Cdr (Assoc 40 Ent)))
						)
						(If 
							(> (Distance Pt10 Pt11) 127)
							(Progn
								(Command 
									".Break" 
									Next 
									(SetQ 
										BPt 
										(Polar 
											(Cdr (Assoc 10 Ent))
											(If 
												(< Ang1 Ang2) 
												(/ (+ Ang1 Ang2) 2.0)
												(- (/ (+ Ang1 Ang2) 2.0) Pi)
											)
											(Cdr (Assoc 40 Ent))
										)
									) 
									BPt
								)
								(SetQ DoneCt (1+ DoneCt))
							)
						)
					)
				)
				(PrinC
					(StrCat
						"\r"
						(IToA DoneCt)
						" long arc"
						(If (= DoneCt 1) "" "s")
						" broken. "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(SetQ Go 0 Ct -1 DoneCt 0)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nBreaking up long lines...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If 
					(= (Cdr (Assoc 0 (SetQ Ent (EntGet Next)))) "LINE")
					(Progn
						(SetQ 
							Pt10 (Cdr (Assoc 10 Ent))
							Pt11 (Cdr (Assoc 11 Ent))
						)
						(If 
							(> (Distance Pt10 Pt11) 127)
							(Progn
								(Command 
									".Break" 
									Next
									(SetQ BPt (MidOf Pt10 Pt11))
									BPt
								)
								(SetQ DoneCt (1+ DoneCt))
							)
						)
					)
				)
				(PrinC
					(StrCat
						"\r"
						(IToA DoneCt)
						" long line"
						(If (= DoneCt 1) "" "s")
						" broken. "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(SetQ Ct -1)
	(SetVar "Highlight" 0)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Ct -1)
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nConverting arcs and lines to polylines...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(Command ".Pedit" Next "Y" "X")
				(PrinC
					(StrCat
						"\rConverted "
						(IToA (1+ Ct))
						" object"
						(If (= (1+ Ct) 1) " to a" "s to")
						" polyline"
						(If (= (1+ Ct) 1) "" "s")
						". "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Ct -1)
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nChanging polyline vertices to 1,1 snap points...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(While
					(/= (Cdr (Assoc 0 (EntGet (SetQ Next (EntNext Next))))) "SEQEND")
					(EntMod
						(SubSt
							(Cons 10 (Fixup (Cdr (Assoc 10 (EntGet Next)))))
							(Assoc 10 (EntGet Next))
							(EntGet Next)
						)
					)
				)
				(PrinC
					(StrCat
						"\rChanged "
						(IToA (1+ Ct))
						" polyline"
						(If (= (1+ Ct) 1) "" "s")
						". "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
				(EntUpd Next)
			)
		)
	)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetQ Ct -1 JoinCt 0)
	(If 
		(And
			(SetQ SS (SSGet "X" (List (Cons 8 "CHARACTR"))))
			(> (SSLength SS) 0)
		)
		(Progn
			(PrinC "\n \nJoining polylines...\n")
			(While 
				(SetQ Next (SSName SS (SetQ Ct (1+ Ct))))
				(If
					(EntGet Next)
					(Progn
						(Command 
							".Pedit" Next "J" "W" 
							(GetVar "ExtMin") (GetVar "ExtMax") 
							"" "X"
						)
						(SetQ JoinCt (1+ JoinCt))
					)
				)
				(PrinC
					(StrCat
						"\rJoined "
						(IToA JoinCt)
						" polyline"
						(If (= JoinCt 1) "" "s")
						". "
						(Percent (1+ Ct) (SSLength SS) 2)
					)
				)
			)
		)
	)
	(PrinC 
		(StrCat
			"\n \n \nCleanup of "
			TypSpc " " ChrFil " "
			(StrCase Font) 
			"\\" (StrCase ChrFl1) 
			" complete.\n \n"
		)
	)
	(SetVar "Highlight" High)
	(SetBck T)
)

(PrinC "\010------*")

(DeFun
	Spacing
	(/ SS1 ErsPt1 ErsPt2 High)
	(Initlz T)
	(SetQ 
		MorErr 
		(Append 
			MorErr 
			(List 
				'(If
					(SetQ SS1 (SSGet "X" (List (Cons 8 "NextLett"))))
					(Progn
						(Command ".Erase" SS1 "")
						(SetQ SS1 Nil)
						(GC)
					)
				)
				'(If
					(SetQ SS1 (SSGet "X" (List (Cons 8 "Vertical"))))
					(Progn
						(Command ".Erase" SS1 "")
						(SetQ SS1 Nil)
						(GC)
					)
				)
				'(If High (SetVar "Highlight" High))
				'(Command 
					".Layer" "S" "Charactr" 
					"Off" "NextLett,Vertical" ""
				)
				'(NilVar '(SS1 ErsPt1 ErsPt2 High))
			)
		)
		DidSpc T
	)
	(SetQ High (GetVar "Highlight"))
	(If
		DidFnt
		(Progn
			(PrinC "\nErasing character couplets... ")
			(Command ".Erase" "W" '(57 -12) '(158 64) "")
			(SetQ DidFnt Nil)
		)
	)
	(If SS (Progn (SetQ SS Nil) (GC)))
	(SetVar "Highlight" 1)
	(Command ".Snap" 1)
	(If 
		(= ChrTyp "Ascii")
		(Progn
			(SetVar "Expert" 2)
			(Command ".Layer" "On" "Ascii,NextLett" "")
			(If
				(And
					(SetQ 
						SS 
						(SSGet "X" (List (Cons 8 "NEXTLETT")))
					)
					(> (SSLength SS) 0)
				)
				(InitGet "Current")
				(InitGet 1)
			)
			(SetQ 
				FinPt 
				(GetPoint 
					(StrCat
						"\n \n \nPick horizontal start point of NEXT character"
						(If (And SS (> (SSLength SS) 0)) " <Current>" "")
						": "
					)
				)
				ErsPt1 FinPt
				FinPt
				(List
					(If
						(Not (Member FinPt '(Nil "Current")))
						(Car FinPt)
						(Cadr (Assoc 10 (EntGet (SSName SS 0))))
					)
					0
				)
			)
			(If
				(And
					(SetQ 
						SS 
						(SSGet "X" (List (Cons 8 "NEXTLETT")))
					)
					(> (SSLength SS) 0)
				)
				(SetQ 
					FinPt
					(List
						(Cadr (Assoc 10 (EntGet (SSName SS 0))))
						0
					)
					InsPt (List 0 0)
				)
			)
			(If 
				(And 
					SS (> (SSLength SS) 0)
					(Not (Member ErsPt1 '(Nil "Current")))
				)
				(Command ".Erase" SS "")
			)
			(SetQ SS Nil) 
			(GC)
			(If 
				(Not (Member ErsPt1 '(Nil "Current")))
				(Progn
					(Command ".Line" FinPt (List (Car FinPt) 168) "")
					(Command ".Change" "L" "" "L" "NextLett")
				)
			)
			(SetVar "Expert" 2)
			(Command ".Layer" "Off" "NextLett" "On" "Vertical" "")                
			(If
				(And
					(SetQ 
						SS 
						(SSGet "X" (List (Cons 8 "VERTICAL")))
					)
					(> (SSLength SS) 0)
				)
				(InitGet "Current")
				(InitGet 1)
			)
			(SetQ 
				VPt1 
				(GetPoint 
					(StrCat
						"\n \n \nPick vertical start point of THIS character"
						(If (And SS (> (SSLength SS) 0)) " <Current>" "")
						": "
					)
				)
				ErsPt1 VPt1
				VPt1
				(If
					(Not (Member VPt1 '(Nil "Current")))
					VPt1
					(Cdr (Assoc 10 (EntGet (SSName SS 0))))
				)
			)
			(If
				(And SS (> (SSLength SS) 0))
				(InitGet "Current")
				(InitGet 1)
			)
			(SetQ
				VPt2 
				(GetPoint 
					(StrCat
						"\n \n \nPick vertical start point of NEXT character"
						(If (And SS (> (SSLength SS) 0)) " <Current>" "")
						": "
					)
				)
				ErsPt2 VPt2
				VPt2 
				(If
					(Not (Member ErsPt2 '(Nil "Current")))
					(List (Car VPt1) (Cadr VPt2))
					(List 
						(Car VPt1) 
						(Caddr (Assoc 11 (EntGet (SSName SS 0))))
					)
				)
			)
			(If 
				(And 
					SS 
					(> (SSLength SS) 0)
					(Or
						(Not (Member ErsPt1 '(Nil "Current")))
						(Not (Member ErsPt2 '(Nil "Current")))
					)
				)
				(Command ".Erase" SS "")
			)
			(SetQ SS Nil) 
			(GC)
			(If 
				(Or 
					(Not (Member ErsPt1 '(Nil "Current")))
					(Not (Member ErsPt2 '(Nil "Current")))
				)
				(Progn
					(Command ".Line" VPt1 VPt2 "")
					(Command ".Change" "L" "" "L" "Vertical")
				)
			)
		)
		(Progn
			(InitGet 1)
			(SetQ 
				InsPt 
				(GetPoint "\n \nStarting point of shape: ") 
				FinPt InsPt
			)
		)
	)
	(Command ".Layer" "S" "Charactr" "Off" "Vertical" "")
	(PrinC 
		(StrCat
			"\n \n \nSpacing of "
			TypSpc " " ChrFil " "
			(StrCase Font) 
			"\\" (StrCase ChrFl1) 
			" complete.\n \n"
		)
	)
	(SetVar "Highlight" High)
	(SetBck T)
)

(PrinC "\010------*")

(DeFun 
	SavCh2 
	(/ TmpPt SS SS1 SS2 High)
	(Initlz T)
	(SetQ 
		MorErr 
		'(
			(If High (SetVar "Highlight" High))
			(NilVar '(TmpPt SS SS1 SS2 High))
		)
		High (GetVar "Highlight")
	)
	(SetVar "Highlight" 1)
	(If
		(And 
			TypSpc ChrFil Font
			(If
				(And
					(SetQ 
						SS1
						(SSGet "X" (List (Cons 8 "NEXTLETT")))
					)
					(> (SSLength SS1) 0)
				)
				(SetQ 
					FinPt
					(List
						(Cadr (Assoc 10 (EntGet (SSName SS1 0))))
						0
					)
				)
				Nil
			)
			(If
				(And
					(SetQ 
						SS2
						(SSGet "X" (List (Cons 8 "VERTICAL")))
					)
					(> (SSLength SS2) 0)
				)
				(Progn
					(SetQ 
						VPt1
						(Cdr (Assoc 10 (EntGet (SSName SS2 0))))
						VPt2
						(List
							(Car VPt1)
							(Caddr (Assoc 11 (EntGet (SSName SS2 0))))
						)
					)
				)
				Nil
			)
		)
		(Progn
			(SetQ SS1 Nil SS2 Nil)
			(GC)
			(If
				DidFnt
				(Progn
					(PrinC "\nErasing character couplets... ")
					(Command ".Erase" "W" '(57 -12) '(158 64) "")
					(SetQ DidFnt Nil)
				)
			)
			(SetQ 
				InsPt 
				(If
					(= ChrTyp "Ascii")
					(List 0 0)
					(GetPoint "\n \nStarting point of shape: ") 
				)
			)
			(PrinC 
				(StrCat 
					"\nSaving " 
					TypSpc 
					" " 
					ChrFil 
					(If (= TypSpc "Shape") "..." " ...")
					" \n"
				)
			)
			(SetVar "Highlight" 1)
			(SetQ 
				ChrFl1 
				(If 
					(= ChrTyp "Ascii") 
					(ChrNam ChrFil) 
					ChrFil
				) 
				OpnFil Nil
			)
			(If 
				(SetQ 
					OpnFil 
					(FindFile (StrCat "\\DRL\\" Font "\\" ChrFl1 ".Dwg"))
				)
				(Progn
					(Command 
						"Sh" 
						(StrCat 
							"Copy " OpnFil
							" \\DRL\\" Font "\\"
							ChrFl1 ".Bak"
						)
					)
					(Command "Sh" (StrCat "Copy " OpnFil))
					(SetQ OpnFil Nil)
				)
			)
			(SetVar "Expert" 2)
			(Command ".Layer" "On" "*" "Off" "Ascii,FontTest" "")
			(If SS (Progn (SetQ SS Nil) (GC)))
			(SetQ SS (SSGet "C" (GetVar "VSMin") (GetVar "VSMax")))
			(Command 
				".Wblock" 
				(StrCat "\\DRL\\" Font "\\" ChrFl1) 
				"" InsPt SS ""
			)
			(Command ".Oops")
			(Command 
				".Layer" "On" "0,Charactr" "S" "Charactr" "Off"
				"Ascii,NextLett,Vertical,FontTest" ""
			)
			(GC)
			(SetQ OpnFil (Open (StrCat "\\DRL\\" Font "\\PostProc.Lst") "a"))
			(Print 
				(List 
					(If 
						(= ChrTyp "Shape") 
						(IToA (SetQ ShapCt (1+ ShapCt)))
						(IToA (Ascii ChrFil)) 
					)
					ChrFl1 
					FinPt
					VPt1 
					VPt2
				) 
				OpnFil
			)
			(SetQ OpnFil (Close OpnFil))
			(Prompt 
				(StrCat 
					"\nPre-processing of " 
					TypSpc " " ChrFil " "
					(StrCase Font) 
					"\\" (StrCase ChrFl1) 
					" complete.\n"
					"Ready to create another character or post process font "
					"\\DRL\\"
					(StrCase Font)
					". "
				)
			)
		)
		(PrinC "\nYou must use Define Character and Spacing before using Save Character. ")
	)
	(SetVar "Highlight" High)
	(SetBck T)
)

(PrinC "\010------*")                           

(DeFun 
	EndSes 
	(/ Go)
	(Initlz T)
	(InitGet "Yes No")
	(SetQ Go (GetKword "\nDo you want to end this session? Yes/<No>: "))
	(If (= Go "Yes") (Command ".Quit" "Y"))
	(SetBck T)
)

(DeFun
	C:PLineJoin
	(/ PlnEnt)
	(Initlz T)
	(SetQ 
		MorErr '((NilVar '(PlnEnt)))
		PlnEnt 
		(OneEnt Nil "\nSelect line or arc: ")
	)
	(If
		(Member 
			(Cdr (Assoc 0 (EntGet PlnEnt))) 
			'("LINE" "ARC")
		)
		(Progn
			(PrinC "\nConverting object to polyline and joining... ")
			(Command
				"Pedit"
				PlnEnt "Y" "J" "C"
				(GetVar "VSMin")
				(GetVar "VSMax")
				"" "X"
			)
			(Command "Change" "L" "" "P" "C" "3" "")
		)
		(PrinC "\nObject selected must be a line or an arc. ")
	)
	(SetBck T)
)

(PrinC "\010------*")

(If (Null AscLst) (SetQ AscLst '(("32" "spc") ("33" "kexc") ("34" "kdblqt") 
("35" "kns") ("36" "kds") ("37" "kpc") ("38" "kand") ("39" "kapos") 
("40" "klp") ("41" "krp") ("42" "kas") ("43" "kpls") ("44" "kcma") 
("45" "ksub") ("46" "kper") ("47" "kdiv") ("48" "n0") ("49" "n1") 
("50" "n2") ("51" "n3") ("52" "n4") ("53" "n5") ("54" "n6") ("55" "n7") 
("56" "n8") ("57" "n9") ("58" "kcol") ("59" "ksmc") ("60" "klt") ("61" "keq") 
("62" "kgt") ("63" "kqm") ("64" "kea") ("65" "uca") ("66" "ucb") ("67" "ucc")
("68" "ucd") ("69" "uce") ("70" "ucf") ("71" "ucg") ("72" "uch") ("73" "uci") 
("74" "ucj") ("75" "uck") ("76" "ucl") ("77" "ucm") ("78" "ucn") ("79" "uco") 
("80" "ucp") ("81" "ucq") ("82" "ucr") ("83" "ucs") ("84" "uct") ("85" "ucu") 
("86" "ucv") ("87" "ucw") ("88" "ucx") ("89" "ucy") ("90" "ucz") ("91" "klb")
("92" "kbkslsh") ("93" "krb") ("94" "kcaret") ("95" "kundrl") ("96" "krvap") 
("97" "lca") ("98" "lcb") ("99" "lcc") ("100" "lcd") ("101" "lce") 
("102" "lcf") ("103" "lcg") ("104" "lch") ("105" "lci") ("106" "lcj") 
("107" "lck") ("108" "lcl") ("109" "lcm") ("110" "lcn") ("111" "lco") 
("112" "lcp") ("113" "lcq") ("114" "lcr") ("115" "lcs") ("116" "lct") 
("117" "lcu") ("118" "lcv") ("119" "lcw") ("120" "lcx") ("121" "lcy")
("122" "lcz") ("123" "klbr") ("124" "kvbar") ("125" "krbr") ("126" "ktilde") 
("127" "kdeg") ("128" "kpls-min") ("129" "kdiam"))))

(PrinC "\010------*")

(DeFun 
	AscShp 
	(/ TmpTyp)
	(InitGet "Ascii Shape")
	(SetQ MorErr (Append MorErr '((NilVar '(TmpTyp)))))
	(If (Null ChrTyp) (SetQ ChrTyp "Ascii"))
	(SetQ 
		TmpTyp 
		(GetKword 
			(StrCat
				"\nIs character an Ascii or Shape character? "
				"<" ChrTyp ">: "
			)
		)
		ChrTyp (If (Null TmpTyp) ChrTyp TmpTyp)
	)
	(If (= ChrTyp "Ascii") (Command ".Layer" "On" "0,Ascii" ""))
)

(DeFun 
	ChrNam 
	(ChrFil / Name)
	(SetQ Name (RToS (Ascii ChrFil) 5 0))
	(Cadr (Assoc Name AscLst))
)

(DeFun 
	MidOf
	(Pt21 Pt22)
	(List 
		(/ (+ (Car Pt22) (Car Pt21)) 2.0) 
		(/ (+ (Cadr Pt22) (Cadr Pt21)) 2.0)
	)
)

(PrinC "\010------*")

(DeFun 
	TheDate 
	(/ ToDay Year Day Month)
	(SetQ 
		ToDay (RToS (GetVar "CDate") 2 0)
		Year (SubStr ToDay 3 2)
		Day
		(If 
			(< (AToI (SubStr ToDay 7 2)) 10)
			(SubStr ToDay 8 1)
			(SubStr ToDay 7 2)
		)
		Month
		(If 
			(< (AToI (SubStr ToDay 5 2)) 10)
			(SubStr ToDay 6 1)
			(SubStr ToDay 5 2)
		)
	)
	(StrCat Month "/" Day "/" Year)
)

(PrinC "\010------*")

(DeFun 
	Updt1 
	()
	(If 
		(/= (Float (/ XDisp 2.0)) 0.0) 
		(IToA (If (MinusP XDisp) (1- XDisp1) (1+ XDisp1))) 
		(IToA XDisp1)
	)
)

(DeFun 
	Updt2 ()
	(If 
		(/= (Float (/ YDisp 2.0)) 0.0)
		(IToA (If (MinusP YDisp) (1- YDisp1) (1+ YDisp1)))
		(IToA YDisp1)
	)
)

(PrinC "\010------*")

(DeFun
	ViewSpac
	()
	(Initlz T)
	(SetQ MorErr '((Command ".U")))
	(Command ".Layer" "On" "NextLett,Vertical" "")
	(PrinC "\nIf there are spacing guide lines, they should be displayed now. ")
	(PrinC "\nPress any key or digitizer button, or pick point or menu item to continue... ")
	(While (Not (Member (Car (GrRead)) '(2 3 4 6))))
	(Command ".U")
	(SetBck T)
)

(Defun
	NilVar
	(VarLst)                     
	(ForEach Var VarLst (Set Var Nil))
)

(PrinC "\010------*")

(DeFun 
	C:SnapUp 
	(/ Same)
	(Cond 	(
			(Equal (GetVar "SnapUnit") '(1 1)) 
			(SetVar "SnapUnit" '(2 2))
		)
		(
			(Equal (GetVar "SnapUnit") '(2 2)) 
			(SetVar "SnapUnit" '(4 4))
		)
		(
			(Equal (GetVar "SnapUnit") '(4 4)) 
			(SetVar "SnapUnit" '(7 7))
		)
		(
			(Equal (GetVar "SnapUnit") '(7 7)) 
			(SetVar "SnapUnit" '(14 14))
		)
		(
			(Equal (GetVar "SnapUnit") '(14 14)) 
			(PrinC "\nCurrent snap value 14,14 is maximum allowed in AutoFONT. ") 
			(SetQ Same T)
		)
	)
	(If
		(Null Same)
		(PrinC 
			(StrCat 
				"\nSnap is now "
				(IToA (Fix (Car (GetVar "SnapUnit"))))
				"," 
				(IToA (Fix (Cadr (GetVar "SnapUnit"))))
				". "
			)
		)
	)
	(PrinC)
)

(DeFun 
	C:SnapDn 
	()
	(Cond 	
		(
			(Equal (GetVar "SnapUnit") '(1 1)) 
			(PrinC "\nCurrent snap value 1,1 is mimimum allowed in AutoFONT. ") 
			(SetQ Same T)
		)
		(
			(Equal (GetVar "SnapUnit") '(2 2)) 
			(SetVar "SnapUnit" '(1 1))
		)
		(
			(Equal (GetVar "SnapUnit") '(4 4)) 
			(SetVar "SnapUnit" '(2 2))
		)
		(
			(Equal (GetVar "SnapUnit") '(7 7)) 
			(SetVar "SnapUnit" '(4 4))
		)
		(
			(Equal (GetVar "SnapUnit") '(14 14)) 
			(SetVar "SnapUnit" '(7 7))
		)
	)
	(If
		(Null Same)
		(PrinC 
			(StrCat 
				"\nSnap is now "
				(IToA (Fix (Car (GetVar "SnapUnit"))))
				"," 
				(IToA (Fix (Cadr (GetVar "SnapUnit"))))
				". "
			)
		)
	)
	(PrinC)
)

(PrinC "\010------*")

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

(PrinC "\010-----*")

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

(PrinC "\010-----*")

(DeFun 
	Fixup 
	(Pt)
	(SetQ 
		Pt
		(List
			(If 
				(>= (- (Car Pt) (Fix (Car Pt))) 0.5)
				(Fix (1+ (Car Pt))) 
				(Fix (Car Pt))
			)
			(If 
				(>= (- (Cadr Pt) (Fix (Cadr Pt))) 0.5)
				(Fix (1+ (Cadr Pt))) 
				(Fix (Cadr Pt))
			)
		)
	)
)

(DeFun
	C:AutoFont
	()
	(Command ".Menu" "\\DRL\\AutoFont")
	(PrinC)
)

(DeFun 
	Percent
	(NumDon Total Prec)
	(StrCat
		(RToS (* (/ (* NumDon 1.0) (* Total 1.0)) 100) 2 Prec)
		"% complete. "
	)
)

(DeFun 
	OneEnt
	(EntTyp Msg / E1 SelTyp)
	(SetQ MorErr (Append MorErr '(NilVar '(E1 SelTyp))))
	(While
		(Or 
			(Null E1) 
			(And
				EntTyp
				(/= (Cdr (Assoc 0 (EntGet E1))) EntTyp)
			)
		)
		(If Msg (PrinC Msg))
		(PrinC " Last object selected will be used... ")
		(SetQ E1 (SSGet))
		(If
			(And E1 (> (SSLength E1) 0))
			(SetQ E1 (SSName E1 (1- (SSLength E1))))
		)
		(Cond
			(
				(Null E1) 
				(PrinC "\nNo object selected. *Invalid*")
			)
			(
				(And
					EntTyp
					(EntGet E1)
					(/=
						(Cdr (Assoc 0 (EntGet E1)))
						EntTyp
					)
				)
				(PrinC 
					(StrCat
						"\nObject selected is not a "
						(If
							(= EntTyp "INSERT")
							"block"
							(StrCase EntTyp T)
						)
						(If (= EntTyp "TEXT") " object" "")
						". *Invalid*"
					)
				)
				(SetQ E1 Nil)
			)
		)
	)
	E1
)

(PrinC "\010-")

(GraphScr)

(PrinC "\n \n \nAutoFONT Pre-Processor loaded. ")

(SetQ AFont1 T)

(PrinC)
