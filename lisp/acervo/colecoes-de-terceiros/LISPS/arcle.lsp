(DEFUN C:ARCLE ()
    (SETQ A (ENTSEL))
    (SETQ B (ENTGET (CAR A)))
      (IF (EQUAL "ARC" (CDR (ASSOC '0 B)))
        (PROGN
          (SETQ RAD (CDR (ASSOC '40 B)))                  ;RADIUS
          (SETQ 1stA (CDR (ASSOC '50 B)))                 ;1st ANGLE
          (SETQ 2ndA (CDR (ASSOC '51 B)))                 ;2nd ANGLE
           (IF (< 1stA 2ndA)
            (SETQ RANG (- 2ndA 1stA))                     ;ANGLE IN RADIANS
            (SETQ RANG (+ (* 2 PI) (- 2ndA 1stA))))       ;ANGLE IN RADIANS
              (SETQ ARL (* RAD RANG))                     ;ARC LENGTH
              (LIST ARL))
             (LIST "NOT AN ARC") 
          )
)
