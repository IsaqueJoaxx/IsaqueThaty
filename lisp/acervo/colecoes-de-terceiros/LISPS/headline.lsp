;
; HEADLINE.LSP
;
; The Dr.Lisp Utilities TM SM, Version 3.0
; (c)Copyright 1990 RobLand Corporation
; Licensed Material, All Rights Reserved
; ------------------------------------------------------------------------
;
;			W A R N I N G
;
; THIS SOFTWARE AND MANUAL ARE BOTH PROTECTED BY U.S. COPYRIGHT
; LAW (TITLE 17 UNITED STATES CODE).  UNAUTHORIZED REPRODUCTION
; AND/OR SALES MAY RESULT IN IMPRISONMENT OF UP TO ONE YEAR AND
; FINES OF UP TO $10,000 (17 USC 506). COPYRIGHT INFRINGERS MAY
; ALSO BE SUBJECT TO CIVIL LIABILITY.
;
; ------------------------------------------------------------------------

(PrinC 
	(StrCat
		"\nThe Dr.Lisp Utilities TM SM, Version 3.0, Headline..."
		"\n(c)Copyright 1990 RobLand Corporation, Licensed Material, All Rights Reserved"
		"\n-----------------------------------------------------------------------------"
	)
)

(DeFun
	C:Headline
	(/ 
		Template TmplVar Hedlin LinCt BoxCt
		Hedlin1 Hedlin2 Hedlin3 Hedlin4 Str
	) 
	(Start)
	(SetQ 
		MorErr 
		'((NilVar 
			'(
				Template TmplVar Hedlin LinCt BoxCt
				Hedlin1 Hedlin2 Hedlin3 Hedlin4 Str
			)
		))
		Hedlin ""
		LinCt 0
		Str ""
	)
	(Cls White Blue Nil)
	(Blink)
	(Prompt (Center 10 41 "Creating Headline..."))
	(Unblink)
	(Park Nil)
	(SetQ Template (Nth (Random 0 (1- (Length TmplLst))) TmplLst))
	(ForEach
		TmplVar
		Template
		(SetQ 
			Hedlin 
			(StrCat
				Hedlin
				(Cond
					((= (Type TmplVar) 'STR) TmplVar)
					(
						(And
							(ListP TmplVar)
							(Member
								(Car TmplVar) 
								'(UpCase IToA)
							) 
						)
						(Eval TmplVar)
					)
					(
						T
						(Nth 
							(Random 0 (1- (Length (Eval TmplVar))))
							(Eval TmplVar)
						)
					)
				)
			)
		)
	)
	(While
		(/= Hedlin "")
		(Set 
			(Read (StrCat "Hedlin" (IToA (SetQ LinCt (1+ LinCt)))))
			(BeforeChr Hedlin "~")
		)
		(SetQ 
			HedLin 
			(SubStr 
				Hedlin 
				(+ 2 (StrLen (Eval (Read (StrCat "Hedlin" (IToA LinCt))))))
			)
		)
	)
	(Park Nil)
	(FColor Red)
	(BColor White)
	(SetQ BoxCt 0)
	(Repeat
		(+ 2 LinCt)
		(Prompt 
			(StrCat 
				"\e[" 
				(IToA (+ 9 (SetQ BoxCt (1+ BoxCt)))) 
				";5H"
			)
		)
		(Prompt "                                    ")
                (Prompt "                                    ")
	)

	(Prompt (Center 11 41 Hedlin1))
	(If Hedlin2 (Prompt (Center 12 41 Hedlin2)))
	(If Hedlin3 (Prompt (Center 13 41 Hedlin3)))
	(If Hedlin4 (Prompt (Center 14 41 Hedlin4)))
	(Clone 24 1 "\360" 26)
	(Blink)
	(Place Nil Nil " Press any key to continue ")
	(Unblink)
	(Clone Nil Nil "\360" 27)
	(Park '(AnyKey))
	(Done Nil)
)

(SetQ
	TmplLst
	'(
		(
			"Top " 
			Pros
			" agree!~"
			(UpCase Ing-To-Animate-Verb)
			" " 
			AnInanimates
			" makes you "
			Er-Adj
			"!"
		)
		(
			(UpCase Amazing)
			" proof of life after death!~"
			(UpCase Famous)
			" gets "
			KilledBy
			" by "
			BadGuy
			",~dies, and is "
			BroughtBack
			" by the ghost of "
			Dead
			"!"
		)
		(
			(UpCase Famous)
			"'s "
			Amazing
			" "
			Inanimate
			" diet!~"
			"Eat all the "
			Inanimates
			" you want~and lose up to "
			(IToA (Random 2 150))
			" pounds in just "
			(IToA (Random 2 1000))
			" short weeks,~or your "
			AnInanimates
			" back!"
		)
		(
			(UpCase Famous)
			" "
			KilledBy
			" by "
			Ing-To-Inanimate-Verb
			" "
			AnInanimates
			"~in "
			Amazing
			" escape from "
			Place-Name
			" "
			Place-Type
			"!"
		)
		(
			(UpCase Person)
			" "
			Verbs
			" "
			Person
			"~in "
			Place-Name
			" "
			Place-Type
			"!"
		)
		(
			(UpCase Amazing)
			" expose!~The beauty secrets of "
			Person
			" revealed!"
		)
		(
			(UpCase Person)
			" kicked out of "
			Place-Name
			"~for being too "
			Amazing
			"!"
		)
		(
			"Filled with "
			Animates
			" and "
			Inanimates
			",~"
			Amazing
			" "
			InAnimate
			" "
			Adverb
			" cures the sick and crippled!"
		)
	)
)

(SetQ
	Pros
	'(
		"experts" "researchers" "atomic scientists" "golfers"
		"nuclear physicists" "feminists" "football players"
		"porno stars" "studs" "sado-masochists"
		"geologists" "accountants" "secretaries"
		"CEOs" "corporate raiders" "billionaires" "housewives"
		"cab drivers" "hockey players" "pizza chefs" "headwaiters"
		"waitresses" "call girls" "pimps" "panderers" "mafiosi"
		"rock stars" "groupies" "roadies" "lawyers" "doctors"
		"ditch diggers" "film producers" "disco owners"
		"circus clowns" "arsonists" "wrestlers" "hitmen" 
		"space aliens" "bag ladies"
	)
	Ing-To-Animate-Verb
	'(
		"eating" "smelling" "licking" "kissing" "being kind to"
		"loving" "shampooing" "bathing" "disciplining" "hiring" 
		"sniffing" "worshipping" "dominating" "whipping" "massaging" 
		"healing" "comforting" "sheltering" "sueing" "crippling"
		"mugging" "reading the minds of" "psychic contact with"
	)
	Animates
	'(
		"dinosaur remains" "cats" "dogs" "wild boars" "wrestlers"
		"sex stars" "elephants" "space aliens" "elbows"
		"golfers" "football players" "lobsters" "octopusses"
		"porno stars" "studs" "bitch queens" "sado-masochists"
		"housewives" "cab drivers" "hockey players" 
		"pizza chefs" "headwaiters" "waitresses" "call girls" 
		"pimps" "panderers" "mafiosi" "rock stars" "groupies" 
		"disco owners" "circus clowns" "arsonists" "wrestlers" 
		"hitmen" "the handicapped" "screaming fans"
		"vampires" "vampire bats"
	)
	Verbs
	'(
		"eats" "smells" "licks" "kisses" "is kind to"
		"loves" "shampoos" "bathes" "disciplines" "hires" 
		"sniffs" "worships" "dominates" "whips" "massages" 
		"cheats" "does battle with" "cures" "inhales" 
		"reads the mind of" "fires" "abuses"
	)
	Verb
	'(
		"eat" "smell" "lick" "kiss" "marry" "abduct" "french kiss"
		"love" "shampoo" "bathe" "discipline" "hire" "sing to"
		"sniff" "worship" "dominate" "whip" "massage" "cheat"
		"do battle with" "cure" "inhale" "read the mind of"
		"fire" "abuse"
	)
	Ing-To-Inanimate-Verb
	'(
		"falling" "exploding" "melting" "burning" "flying"
		"electrified" "misfired" "out of control"
		"runaway"
	)
)

(SetQ
	Inanimate
	'(
		"computer" "mack truck" "street light" "stop sign" "pig slop"
		"toll booth" "submarine" "taxi cab" "door knob" "mop"
		"kitchen sink" "boiler" "waterbed" "stapler" "broomstick"
		"chicken scratch" "alarm clock" "diskette" "deodorizer"
		"xerox machine" "boom box" "garbage can" "M-16" "cherry bomb"
		"fire exit" "wall switch" "cigarette" "toilet bowl"
		"toothpaste" "atom bomb" "'57 Ford Thunderbird"
		"'61 Coupe DeVille" "Harley Hog" "AIDS" "UFO"
	)
	Inanimates
	'(
		"cars" "poems" "snowballs" "eggs" "paint thinners" "drugs"
		"dirty socks" "scotch tape" "beer cans" "cassette tapes"
		"motor oil" "automotive parts" "bicycle chains" 
		"Elmer's Glue" "dog food" "LSD" "AIDS" "UFOs"
	)
	AnInanimates
	'(
		"dinosaurs" "cats" "dogs" "wild boars" "wrestlers"
		"sex stars" "elephants" "space aliens" "elbows"
		"golfers" "football players" "lobsters" "octopusses"
		"porno stars" "studs" "bitch queens" "sado-masochists"
		"housewives" "cab drivers" "hockey players" 
		"pizza chefs" "headwaiters" "waitresses" "call girls" 
		"pimps" "panderers" "mafiosi" "rock stars" "groupies" 
		"disco owners" "circus clowns" "arsonists" "wrestlers" 
		"hitmen" "screaming fans"
		"cars" "poems" "snowballs" "eggs" "paint thinners" "drugs"
		"dirty socks" "scotch tape" "beer cans" "cassette tapes"
		"motor oil" "automotive parts" "bicycle chains" 
		"Elmer's Glue" "dog food" "LSD"
		"computers" "mack trucks" "street lights" "stop signs" 
		"pig slop" "toll booths" "submarines" "taxi cabs" "door knobs" 
		"mops" "kitchen sinks" "boilers" "waterbeds" "staplers" 
		"broomsticks" "chicken scratch" "alarm clocks" "diskettes" 
		"deodorizers" "xerox machines" "boom boxes" "garbage cans" 
		"M-16s" "cherry bombs" "fire exits" "wall switches" 
		"cigarettes" "toilet bowls" "toothpaste" "atom bombs" 
		"'57 Ford Thunderbirds" "'61 Coupe DeVilles" "Harley Hogs"
		"vampires" "vampire bats"
	)
	KilledBy
	'(
		"smothered" "hung" "electrocuted" "run over" "drowned"
		"suffocated" "shot" "blown up" "strangled" "stabbed"
		"speared" "blow-gunned" "machine gunned" "executed"
		"burned alive" "mutilated" "crushed" "ripped limb from limb"
		"ripped apart" "drawn and quartered" "poisoned" "gassed"
		"brutally beaten" "splattered"
	)
)

(SetQ
	BroughtBack
	'(
		"resurrected" "brought back to life" "reincarnated"
		"made love to" "caressed" "resussitated" "loaned money"
	)
	BadGuy
	'(
		"David Berkowitz" "the ghost of Adolph Hitler"
		"Charles Manson" "Juan Corona" "John Wayne Gacy"
		"Tex Watson" "the ghost of Al Capone" "Richard Nixon"
		"G. Gordon Liddy" "the ghost of Che Guevara" "David Chapman"
		"Robert Chambers" "Charles Whitman" "Julio Gonzales"
		"Freddy Krueger" "space aliens" "The Abominable Snowman"
		"the ghost of The Boston Strangler" "Sirhan Sirhan"
		"the ghost of Jack the Ripper" "the KKK" "skinheads"
		"James Earl Ray"
	)
	Er-Adj
	'(
		"dumber" "more retarded" "smarter" "more handsome" "cooler" 
		"more lethargic" "more creative" "hairier" "more suave" 
		"richer" "more thoughtful" "nicer" "meaner" "sexier"
		"a better person" "tighter" "leaner" "nastier"
		"smellier" "more shiftless" "crabbier" "more square"
		"taller" "shorter" "more potent" "more impotent" 
		"more important" "more insignificant" "happier" 
		"more content" "more beautiful" "more alluring"
		"more desirable"
	)
	Amazing
        '(
		"amazing" "incredible" "unbelievable" "crazy" "bitchin'"
		"radical" "terrific" "astounding" "awesome" "excellent"
		"ridiculous" "inane" "stupendous" "retarded" "dramatic"
		"cute" "vapid" "unreal" "magnificent" "grandiose"
		"pathetic" "dumb" "dopey" "looney" "weird" "nutty" "wacky"
		"daring" "useless" "hopeless" "miraculous" "dirty" "foul"
		"filthy" "garbage strewn" "bizarre"
	)
	Adverb
        '(
		"amazingly" "incredibly" "unbelievably" "crazily"
		"radically" "terrificly" "astoundingly" "awesomely"
		"ridiculously" "inanely" "stupendously" "dramatically"
		"magnificently" "insanely"
		"patheticly" "dumbly" "weirdly" "miraculously"
		"daringly" "uselessly" "hopelessly" 
	)
	Famous
	'(
		"Michael Landon" "Shirley MacLaine" "David Letterman"
		"Michael Jackson" "Eddie Murphy" "John Travolta"
		"Howard Stern" "Andy Griffith" "Alex Trebek" "Pat Sajak"
		"Don Johnson" "Melanie Griffith" "Meg Ryan" "Roseanne Barr"
		"Bill Cosby" "Daphne Zuniga" "Virginia Madsen" "Hulk Hogan"
		"Gorilla Monsoon" "Rob Lowe" "Warren Beatty" "Jack Lord" 
		"Ann-Margret" "Jim Bakker" "Tammy Bakker" "Donald Trump" 
		"Ivana Trump" "Cher" "Paul McCartney" "Janet Jackson" 
		"Prince" "Richard Pryor" "Gary Shandling" "Lisa Marie Presley"
		"Priscilla Presley" "Larry Hagman" "Charles Manson" 
		"Johnny Carson" "Merle Haggard" "Patty Hearst" "Dr.Lisp"
		"unidentified man" "unidentified woman"
	)
)

(SetQ
	Dead
	'(
		"Officer Joe Bolton" "Liberace" "Moe Howard" "Harry Truman"
		"Rock Hudson" "Zero Mostel" "Dick Shawn" "Jim Morrison"
		"Elvis Presley" "Jimi Hendrix" "Janis Joplin" "Joseph Stalin"
		"John Lennon" "Len Bias" "Indira Ghandi" "Andy Kaufman"
		"Isaac Newton" "Leon Trotsky" "Robert Kennedy" "Elvis"
		"Liberace" "Halston" "Elvis" "John Belushi" "Larry Fine"
                "Martin Luther King" "Abbie Hoffman" "Mao Tse Tung"
	)
	Place-Name
	'(
		"Des Moines" "Alcatraz" "French Riviera" "Costa del Sol"
		"London" "Las Vegas" "Atlantic City" "outer space"
		"space shuttle" "Hackensack" "Monaco" "North Vietnam"
		"Parsippany, N.J." "Antarctica" "South Africa"
		"Brazilian rain forest" "ozone layer" "Costa Rica"
		"Bakersfield" "Key West" "Tijuana" "Bermuda Triangle"
		"UFO" "flying saucer"
	)
	Place-Type
	'(
		"mens room" "ladies room" "fitting room" "gas station"
		"train station" "bus stop" "waiting room" "bedroom"
		"bowling alley" "dog pound" "prison" "welfare office"
		"cafeteria" "bar" "cocktail lounge" "racetrack" "farm"
		"church" "fortress" "hospital" "cave"
	)
	Person
	'(
		"David Berkowitz" "the ghost of Adolph Hitler"
		"Charles Manson" "Juan Corona" "John Wayne Gacy"
		"Tex Watson" "the ghost of Al Capone" "Richard Nixon"
		"G. Gordon Liddy" "the ghost of Che Guevara" "David Chapman"
		"Robert Chambers" "Charles Whitman" "Julio Gonzales"
		"Freddy Krueger" "space alien" "The Abominable Snowman"
		"the ghost of The Boston Strangler" "Sirhan Sirhan"
		"the ghost of Jack the Ripper" "the KKK" "skinhead"
		"James Earl Ray" "Unidentified man" "Unidentified woman"
		"Michael Landon" "Shirley MacLaine" "David Letterman"
		"Michael Jackson" "Eddie Murphy" "John Travolta"
		"Howard Stern" "Andy Griffith" "Alex Trebek" "Pat Sajak"
		"Don Johnson" "Melanie Griffith" "Meg Ryan" "Roseanne Barr"
		"Bill Cosby" "Daphne Zuniga" "Virginia Madsen" "Hulk Hogan"
		"Gorilla Monsoon" "Rob Lowe" "Warren Beatty" "Jack Lord" 
		"Ann-Margret" "Jim Bakker" "Tammy Bakker" "Donald Trump" 
		"Ivana Trump" "Cher" "Paul McCartney" "Janet Jackson" 
		"Prince" "Richard Pryor" "Gary Shandling" "Lisa Marie Presley"
		"Priscilla Presley" "Larry Hagman" "Charles Manson" 
		"Johnny Carson" "Merle Haggard" "Patty Hearst" "Dr.Lisp"
		"expert" "researcher" "atomic scientist" "golfer"
		"nuclear physicist" "feminist" "football player"
		"porno star" "stud" "sado-masochist"
		"geologist" "accountant" "secretary"
		"CEO" "corporate raider" "billionaire" "housewife"
		"cab driver" "hockey player" "pizza chef" "headwaiter"
		"waitress" "call girl" "pimp" "panderer" "mafioso"
		"rock star" "groupie" "roadie" "lawyer" "doctor"
		"ditch digger" "film producer" "disco owner"
		"circus clown" "arsonist" "wrestler" "hitman" 
	)
)

(DeFun
	BeforeChr
	(Str Char / NxtChr NewStr)
	(SetQ NewStr "")
	(While
		(And
			(/=
				(SetQ 
					NxtChr 
					(SubStr Str 1 1)
				) 
				Char
			)
			(/= NxtChr "")
		)
		(SetQ 
			NewStr (StrCat NewStr NxtChr)
			Str (SubStr Str 2)
		)
	)
	NewStr
)

(DeFun
	UpCase
	(LstNam / LstItm)
	(SetQ 
		LstItm
		(Nth 
			(Random 0 (1- (Length LstNam)))
			LstNam
		)
	)
	(StrCat (StrCase (SubStr LstItm 1 1)) (SubStr LstItm 2))
)

(DeFun
	Random
	(LoNum HiNum)
	(+ 
		LoNum 
		(Fix 
			(* 
				(/ 
					(AToF 
						(StrCat
							(SubStr (RToS (GetVar "Date") 2 16) 22 1)
							(SubStr (RToS (GetVar "Date") 2 16) 22 1)
							(SubStr (RToS (GetVar "Date") 2 16) 22 1)
						)
					)
					1000.0
				)
				(1+ (- HiNum LoNum))
			)
		)
	)
)

(Defun
	NilVar
	(VarLst)
	(ForEach Var VarLst (Set Var Nil))
)

(SetQ 
	AppMsg 
	"Headline (c)1990 The Dr.Lisp Utilities TM SM, All Rights Reserved."
)

(DeFun 
	Start 
	()
	(SetQ
		OldErr *Error*
		CmdEc (GetVar "CmdEcho")
	)
	(SetVar "CmdEcho" 0)
	(DeFun 
		*Error* 
		(EMsg)
		(If (/= EMsg "Function cancelled")
			(Prompt (StrCat "\nError: " EMsg "\n"))
		)
		(If CmdOpn (Command ""))
		(If OpnFil (SetQ OpnFil (Close OpnFil)))
		(Prompt "\e[=7h\e[59;59;71;71;72;72;73;73;75;75;77;77;79;79;80;80;81;81;82;82p")
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
		(SetQ *Error* OldErr)
		(NilVar '(CmdEc CLay CmdOpn Item MorErr OldErr))
		(Done Nil)
		(If Err (Err))
		(PrinC)
	)
	(SetQ 
		Black 630 Red 631 Green 632 Yellow 633 Blue 634 Magenta 635 
		Cyan 636 White 637 GFClr White GBClr Black
                CmdEc (GetVar "CmdEcho") BlnkVl Nil BrgtVl Nil WrapVl Nil 
	)
	(SetVar "CmdEcho" 0)
	(TextScr) 
	(Home)
	(Prompt "\e[30;40m")
	(Clone Nil Nil "\e[K\n" 25)
	(Prompt "\e[=7l")
	(Prompt "\e[m")
	(BColor White)
	(FColor White)
	(Prompt " ")
	(FColor Black)
	(BlankRest)
        (Prompt (Center 25 40 AppMsg))
	(Home)
)

(DeFun 
	Done 
	(Scroll)
	(TextScr)
	(Clear)
	(WrapOn)
	(Prompt "\e[=7h\e[30;40m")
	(If 
		(Null Scroll)
		(Progn
			(Prompt "\e[H")
			(Clone Nil Nil "\e[K\n" 25)
		)
		(Progn
			(Prompt "\e[25;80H")
			(Prompt "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n")
		)
	)
	(Prompt "\e[m")
	(Prompt "\e[37;40;1m")
	(PrinC "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n")
	(If CmdEc (SetVar "CmdEcho" CmdEc))
	(SetQ *Error* OldErr)
	(NilVar 
		'(
			CmdEc CLay CmdOpn Item 
			MorErr OldErr GFClr GBClr
			BlnkVl BrgtVl WrapVl MnuLst
		)
	)
	(PrinC)
)

(DeFun 
	Cls 
	(FClr BClr Retain) 
	(If Retain (SavClr 0))
	(Prompt "\e[H")
	(FColor FClr) 
	(BColor BClr)
	(Clone Nil Nil "\e[K\n" 24)
	(Home)
	(If Retain (ResClr 0))
)

(DeFun Home () (Prompt "\e[H"))

(DeFun SavCur () (Prompt "\e[s"))

(DeFun ResCur () (Prompt "\e[u"))

(DeFun WrapOn () (SetQ WrapVl 1) (Prompt "\e[=7h"))

(DeFun 
	SavClr 
	(ColSet) 
	(Set 
		(Read 
			(StrCat 
				"TmpCl" 
				(IToA ColSet)
			)
		)
		(List GFClr GBClr)
	)
)

(DeFun 
	ResClr 
	(ColSet / TmpCl) 
	(If 
		ColSet
		(Progn
			(SetQ 
				TmpCl 
		                (Eval 
					(Read 
						(StrCat 
							"TmpCl" 
		                                        (IToA ColSet)
						)
					)
				)
			)
			(Prompt 
				(StrCat
					"\e["
					(IToA (- (SetQ GFClr (Car TmpCl)) 600))
					";"     		
					(IToA (- (SetQ GBClr (Cadr TmpCl)) 590))
					"m"
				)
			)
		)
		(Prompt 
			(StrCat 
				"\e[" 
                                (IToA (- GFClr 600)) 
                                ";" 
                                (IToA (- GBClr 590)) 
                                "m"
			)
		)
	)
)

(DeFun 
	Clear 
	(/ Ct Next)
	(SetQ Ct -1)
	(While 
		(Eval
			(Read 
				(SetQ 
					Next
					(StrCat 
						"TmpCl" 
                	                       	(IToA (SetQ Ct (1+ Ct)))
					)
				)
			) 
		)
		(Set (Read Next) Nil)
	)
)

(DeFun 
	AnyKey 
	() 
	(Prompt "\e[59;32;71;32;72;32;73;32;75;32;77;32;79;32;80;32;81;32;82;32p")
	(While (/= (Car (GrRead)) 2))
	(Prompt "\e[59;59;71;71;72;72;73;73;75;75;77;77;79;79;80;80;81;81;82;82p")
)

(DeFun 
	Center 
	(Row Col Msg / MaxLen)
	(SetQ MaxLen (* (- 40 (abs (- 40 Col))) 2))
	(If
		(> (StrLen Msg) MaxLen)
		(SetQ 
			Msg 
	                (SubStr 
				Msg 
	                        (/ (- (StrLen Msg) MaxLen) 2)
				MaxLen
			)
		)
	)
	(StrCat 
		"\e["
		(IToA Row)
		";" 
		(IToA
			(- 
				Col
				(/
					(If 
						(= (StrLen Msg) MaxLen)
						MaxLen
						(StrLen Msg)
					)
					2
				)
			)
		)
		"H"                                         
		Msg
	)
)

(DeFun 
	Place 
	(Row Col String) 
	(If 
		(And Row Col)
		(Prompt (StrCat "\e[" (IToA Row) ";" (IToA Col) "H"))
	)
	(Prompt String)
)

(DeFun 
	Clone 
	(Row Col String Num / Str)
	(SetQ Str "")
	(Repeat Num (SetQ Str (StrCat Str String)))
	(Place Row Col Str)
)

(DeFun 
	Cursor 
	(Row Col)
	(If (> Row 24) (SetQ Row 24))
	(Prompt 
		(StrCat 
			"\e[" 			
			(IToA Row) 
			";" 
			(IToA Col) 
			"H"
		) 
	) 	
)

(DeFun 
	Park 
	(Func)
	(If Func (SavCur))
	(Prompt "\e[25;1H")
	(If Func (Progn (Eval Func) (ResCur)))
)

(DeFun BlankCols (Cols) (Clone Nil Nil " " Cols))

(DeFun BlankRest () (Prompt "\e[K"))

(DeFun Blink () (SetQ BlnkVl 1) (Prompt "\e[5m"))

(DeFun 
	UnBlink 
	() 
	(Prompt "\e[0m") 
        (ResClr Nil)
	(SetQ BlnkVl Nil)
	(If BrgtVl (Prompt "\e[1m"))
)

(DeFun 
	FColor 
	(Clr)
	(Prompt (StrCat "\e[" (IToA (- (SetQ GFClr Clr) 600)) "m"))
)

(DeFun 
	BColor 
	(Clr)
	(Prompt (StrCat "\e[" (IToA (- (SetQ GBClr Clr) 590)) "m"))
)

(DeFun 
	Box 
	(
		Row Col BChar BRows BCols BFClr BBClr FilClr 
		/ 
		BLst CurRow
	)
	(SetQ 
		BLst
		(Cond
			(
				(= (StrCase BChar) "SINGLE")
				'("\332" "\304" "\277" "\300" "\331")
			)
			(
				(= (StrCase BChar) "DOUBLE")
				'("\311" "\315" "\273" "\310" "\274")
			)
		)
	)
	(SetQ CurRow Row)
	(SavClr 0)
	(FColor BFClr)
	(BColor BBClr)
	(If
		BLst
		(Progn
			(Place Row Col (Car BLst))
			(Clone Nil Nil (Cadr Blst) (- BCols 2))
			(Place Nil Nil (Nth 2 BLst))
			(If (Not (Member BChar '("\263" "\272"))) (SetQ BChar " "))
			(Repeat
				(1- BRows)
				(Place (SetQ CurRow (1+ CurRow)) Col BChar)
				(If 
					(Member 
						FilClr 
						'(630 631 632 633 634 635 636 637)
					)
		                        (Progn 
						(BColor FilClr) 
		                                (BlankCols (- BCols 2))
						(BColor BBClr)
					)
				)
				(Place CurRow (1- (+ Col BCols)) BChar)
			)
			(Place CurRow Col (Nth 3 BLst))
			(Clone Nil Nil (Cadr Blst) (- BCols 2))
			(Place Nil Nil (Nth 4 BLst))
		)
		(Repeat
			BRows
			(Cursor CurRow Col)
			(BColor FilClr) 
			(BlankCols BCols)
			(BColor BBClr)
			(SetQ CurRow (1+ CurRow))
		)
	)
	(ResClr 0)
)	

(PrinC "\n \n \n \n")

(PrinC)
