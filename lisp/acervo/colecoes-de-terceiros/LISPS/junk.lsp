; Cleanup things - an end for all programs
(DeFun End ()
  (Command "OSnap" "None")
  (SetVar "Aperture" 8)
  (SetVar "BlipMode" 1)
  (GC) ; garbage collection
  (Princ)
)

; make a temporary block
(DeFun C:MBlk()
; <<<< Here is a missing quote error >>>>
; <<<< The quote error will have to  >>>>
; <<<< be fixed to check the balance >>>>
; <<<< on the parenthesis            >>>>
  (Prompt "\Pick items for block)
;                          block") is correct
  (SetQ SS (SSGet))
  (SetQ Base (GetPoint "\Pick the Base point "))
  (If (Or (Null SS) (Null Base))
    (SetQ Base Nil SS Nil))
)

; copy temporary block multiple times
(DeFun C:IBlk()
; <<<< Here is code with 2 missing   >>>>
; <<<< quotes. Even though it checks >>>>
; <<<< okay you can see by the gray  >>>>
; <<<< color what's in quotes        >>>>
  (Command "Copy SS "" "M Base)
; (Command "Copy" SS "" "M" Base)
; <<<< is what it should be >>>>
  (Princ)
)

; change snap value up (use whole numbers)  2 to double
;              or down (use decimal value) .5 to halve it
;             and tell user new value.
; put in screen or tablet menu
; [Snap Up](ChgSnap 2)
; [Snap Dn](ChgSnap 0.5)
(DeFun ChgSnap (Loc)
; <<<< Here is a missing right parenthesis >>>>
  (SetVar "SnapUnit"  ; << this yellow level >>
    (MapCar '* (GetVar "SnapUnit") (List Loc Loc))
;   (MapCar '* (GetVar "SnapUnit") (List Loc Loc)) )
  (Prompt             ; << should be yellow level >>
    (StrCat "\nSnap = " (RToS (Car (GetVar "SnapUnit")) 4 8)) )
  (End)               ; << should be yellow level >>
)
