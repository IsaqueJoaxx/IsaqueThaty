;      AutoLISP TURTLE_GRAPHICS functions :
;                                   written by Paul Petersen
;                                   A B Consulting Co., Inc.
;                                   3939 N 48th
;                                   Lincoln NE, 68504
;
;                     copyright July 1986
;
;
;  This library of functions implement the turtle graphics language.  The
;  basic idea of this method is to simulate a turtle holding a pen, placed
;  on a sheet of paper, that you give commands to.  The commands are
;  move FORWARD, move BACK, TURN, PENUP, PENDOWN, change PENCOLOR, SETHEADING
;  to a particular angle, SETPOSITION to a particular location, report the
;  current turtle position, report the current turtle angle, and initilize
;  the turtle by placing it in the center heading to the right.
;  Included with the TURTLE_GRAPHICS library are graphics programs that
;  have been translated from example programs in BYTE magazine.
;  The program C:HILBERT was published in BYTE June, 1986 in BASIC
;

(SETQ FILELIST (QUOTE  (FILELIST FORWARD BACK TURN PENUP PENDOWN PENCOLOR
                        SETHEADING SETPOSITION TURTLE_POSITION TURTLE_HEADING
                        INIT_TURTLE DRAWHILBERT C:HILBERT DESIGN WHEEL
                        TRIPIECE PENTPIECE PENTL PENTR TRIPOLYL TRIPOLYR
                        CENTERPIECE C:SPIRA)))

(DEFUN FORWARD (DELTA / OFFSET)
        (SETQ OFFSET (STRCAT "@" (RTOS (* 1.0 DELTA) 1 16)
                             "<" (RTOS TURTLE_ANGLE 1 16)))
        (IF  TURTLE_PEN
                (COMMAND  "LINE" "@" OFFSET "")
                (COMMAND  "POINT" OFFSET)))

(DEFUN BACK (DELTA / OFFSET)
        (SETQ OFFSET (STRCAT "@" (RTOS (* -1.0 DELTA) 1 16)
                             "<" (RTOS TURTLE_ANGLE 1 16)))
        (IF  TURTLE_PEN
                (COMMAND  "LINE" "@" OFFSET "")
                (COMMAND  "POINT" OFFSET)))

(DEFUN TURN (ANG)
        (SETQ TURTLE_ANGLE (REM (+ TURTLE_ANGLE ANG) 360.0)))

(DEFUN PENUP nil
        (SETQ TURTLE_PEN nil))

(DEFUN PENDOWN nil
        (SETQ TURTLE_PEN T))

(DEFUN PENCOLOR (COLOR / LNAME)
        (COND  ((EQUAL (TYPE COLOR) (QUOTE STR))
                (SETQ LNAME (STRCAT "TURTLE-" COLOR)))
                ((EQUAL (TYPE COLOR) (QUOTE INT))
                (SETQ LNAME (STRCAT "TURTLE-" (ITOA COLOR))))
                (T  (SETQ LNAME "TURTLE")))
        (IF  (NOT (MEMBER LNAME TURTLE_LAYERS))
                (PROGN  (SETQ TURTLE_LAYERS (CONS LNAME TURTLE_LAYERS))
                        (COMMAND  "LAYER" "NEW" LNAME "")))
        (COMMAND  "LAYER" "SET" LNAME "COLOR" COLOR LNAME ""))

(DEFUN SETHEADING (ANG)
        (SETQ TURTLE_ANGLE (FLOAT ANG)))

(DEFUN SETPOSITION (PT)
        (COMMAND  "POINT" PT))

(DEFUN TURTLE_POSITION nil
        (GETVAR "LASTPOINT"))

(DEFUN TURTLE_HEADING nil
       TURTLE_ANGLE)

(DEFUN INIT_TURTLE (YMAX)
        (SETVAR "CMDECHO" 0)
        (SETQ TURTLE_LAYERS nil)
        (COMMAND  "ZOOM" "C" (QUOTE (0 0)) YMAX)
        (COMMAND  "POINT" (QUOTE  (0 0)))
        (SETHEADING 0.0)
        (PENDOWN)
        (PENCOLOR "WHITE"))

;
; Nth Order Hilbert Curve written for AutoLISP TURTLE GRAPHICS library
;     adapted from Programming Insight: Hilbert Curves Made Simple
;     BYTE June 1986
;

(DEFUN DRAWHILBERT nil
        (SETQ ORDER (1- ORDER) SIGN (- SIGN))
        (TURN (* SIGN 90.0))
        (IF  (> ORDER 0)
                (DRAWHILBERT))
        (FORWARD DIST)
        (SETQ SIGN (- SIGN))
        (TURN (* SIGN 90.0))
        (IF  (> ORDER 0)
                (DRAWHILBERT))
        (FORWARD DIST)
        (IF  (> ORDER 0)
                (DRAWHILBERT))
        (TURN (* SIGN 90.0))
        (SETQ SIGN (- SIGN))
        (FORWARD DIST)
        (IF  (> ORDER 0)
                (DRAWHILBERT))
        (TURN (* SIGN 90.0))
        (SETQ ORDER (1+ ORDER) SIGN (- SIGN)))

(DEFUN C:HILBERT (/ TEMP DIST SIGN)
        (SETQ ORDER (GETINT "\nEnter ORDER of Hilbert curve: "))
        (SETQ TEMP (EXPT 2.0 ORDER) CMDSAVE (GETVAR "CMDECHO"))
        (SETVAR "CMDECHO" 0)
        (INIT_TURTLE TEMP)
        (SETQ DIST 1.0 SIGN -1.0)
        (DRAWHILBERT)
        (COMMAND  "ZOOM" "E")
        (SETVAR "CMDECHO" CMDSAVE)
        nil)

