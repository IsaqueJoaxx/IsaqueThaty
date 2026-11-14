; This is my intersection program to speed up the 
; daily grind and make it little easier
; It has one problem it works some times but crashes at others.
; I have no idea what causes it . If you do let me Kkow I can be reached
; at compuserve 75030,2351  My name is Bob Mc Donald. Enjoy
;
(DEFUN C:I (/ LN1 LN2 LENT1 LENT2 
	      PT1 PT2 PT3 PT4 PT5
	      PX5 PY5 L1 L2 L3 L4 )
(SETQ LENT1 (ENTSEL "PICK FIRST"))
(print)
(SETQ LENT1 (ENTGET( CAR LENT1)))
(SETQ LN1 (CDDDR LENT1))
(SETQ PT1 (CDAR LN1))
(SETQ PT2 (CDADR LN1))
(SETQ LENT2 (ENTSEL "PICK SECOND"))
(print)
(SETQ LENT2 (ENTGET ( CAR LENT2)))
(SETQ LN2 (CDDDR LENT2))
(SETQ PT3 (CDAR LN2))
(SETQ PT4 (CDADR LN2))
(PRINT PT1)
(PRINT PT2)
(SETQ PT5 (INTERS PT1 PT2 PT3 PT4 nil))
(IF (= PT5 NIL) (PROGN PRINT "lines are parralell" QUIT))
(SETQ L1 ( DISTANCE PT1 PT5))
(SETQ L2 ( DISTANCE PT2 PT5))
(SETQ L3 ( DISTANCE PT3 PT5))
(SETQ L4 ( DISTANCE PT4 PT5))
(SETQ PX5 (CAR PT5)) (SETQ PY5 (CADR PT5))
(IF (< L1 L2)
	(PROGN
	(SETQ LENT1 (SUBST (LIST '10 PX5 PY5 0) (ASSOC 10 LENT1) LENT1))
	(ENTMOD LENT1))
	(PROGN
	(SETQ LENT1 (SUBST (LIST '11 PX5 PY5 0) (ASSOC 11 LENT1) LENT1))
	(ENTMOD LENT1))
	) (print)
	
 (IF ( < L3 L4)
	(PROGN
	(SETQ LENT2 (SUBST (LIST '10 PX5 PY5 0) (ASSOC 10 LENT2) LENT2))
	(ENTMOD LENT2))
	(PROGN
	(SETQ LENT2 (SUBST (LIST '11 PX5 PY5 0) (ASSOC 11 LENT2) LENT2))
	(ENTMOD LENT2))
	) (print)  )
	


