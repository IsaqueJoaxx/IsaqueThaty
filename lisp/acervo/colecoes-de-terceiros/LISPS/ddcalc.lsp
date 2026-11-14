;;; This is a shareware developed by ArCAD utilities(r)
;;; Copyright(c)1994 by Guri A. Kravitz
;;; 1 Hate'ena st. RAANANA 43577 ISRAEL 
;;; Tel/fax +972-9-918047
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;; Dialog calculator for AutoCAD rel. 11 and up
;;; simulates MS Windows calculator
;;; numbers and operands may be entered in the edit box,
;;; but enter is yet ignored (to be corrected in the next version)
;;; feel free to distribute this program on the condition that
;;; the header of this file and the corresponding ddcalc.dcl file
;;; are intact.
;;;************************ DISCLAIMER ********************************
;;; This software is provided AS-IS wihtout express or implied warranty. 
;;; All implied warranties or fitness for any purpose whatsoever are
;;; hereby disclaimed.
;;;********************************************************************
;;; Revision history:
;;; focus set to input box, 
;;; minor bugs repared 21/7/94
;;;================================================================================
;;; About ArCAD Utilities(R):
;;; ArCAD Utilities is an Architectural oriented environment and commands
;;; add-on for Autocad. The architectural section include feature such as
;;; walls, doors & windows, stairs, electrical layout, roof design, plumbing
;;; and furniture in 2d and 3d. Schedules (doors & windows, electrical accessories)
;;; can be written to the drawing and/or a BOM file.
;;; Other features include drawing session time-keeping, reference table file,
;;; auto-stamping and titling, automatic revision symbol and notes insertion 
;;; and more
;;;================================================================================
(defun c:ddcalc(/ x_reg y_reg act_reg act do_calc feedback result done)
  (setq dcl_id(load_dialog "ddcalc.dcl"))
  (if (not(new_dialog"dd_calc" dcl_id))
    (exit)
    )
  ;definitions
  (setq x_reg 0 y_reg 0 dec 2)
  (set_tile "result" "0")
  (set_tile "error" "Copyright(c)1994 by ArCAD/G. Kravitz")
  (mode_tile "errtile" 0)
  (action_tile "done""(term_dialog)")
  (set_tile "root" (chr 250))
  ;***********************************************************************  
  ;action keys: devide root times percent minus plus invert plusminus
  (action_tile "devide""(act $reason (chr 47))")
  (action_tile "root""(act $reason $key)")
  (action_tile "times""(act $reason (chr 42))")
  (action_tile "percent""(act $reason (chr 37))")
  (action_tile "minus""(act $reason (chr 45))")
  (action_tile "plus""(act $reason (chr 43))")
  (action_tile "invert""(act $reason (strcat(chr 49)(chr 47)))")
  (action_tile "plusminus""(act $reason (strcat(chr 43)(chr 47)(chr 45)))")
  ;***********************************************************************  
  ;number keys: b0 b1 b2 b3 b4 b5 b6 b7 b8 b9
  (action_tile "b0""(feedback $reason (chr 48))")
  (action_tile "b1""(feedback $reason (chr 49))")
  (action_tile "b2""(feedback $reason (chr 50))")
  (action_tile "b3""(feedback $reason (chr 51))")
  (action_tile "b4""(feedback $reason (chr 52))")
  (action_tile "b5""(feedback $reason (chr 53))")
  (action_tile "b6""(feedback $reason (chr 54))")
  (action_tile "b7""(feedback $reason (chr 55))")
  (action_tile "b8""(feedback $reason (chr 56))")
  (action_tile "b9""(feedback $reason (chr 57))")
  ;(action_tile "result""(check_result $value $reason)")
  ;***********************************************************************  
  ;other keys:clear dec2 float point calc result
  (action_tile "clear" "(do_clear)")
  (action_tile "dec2""(setq dec 2)")
  (action_tile "float""(setq dec 4)")
  (action_tile "point""(feedback $reason (chr 46))")
  (action_tile "calc""(do_calc $reason)")
  (action_tile "about""(welcome)")
  (setq welcome(lambda()
      (new_dialog "dd_welcome"dcl_id)
      (start_dialog)
      ))
  ;***********************************************************************  
  (setq check_result(lambda(value reason)
                      (if(= reason 1)
                        (if(numberp(read value))
                          (if nxt(progn(setq nxt nil)(do_calc reason))
                            (setq y_reg(atof value)nxt T)
                            )
                          (set_tile"result""")
                          )
                        )))
  
  (setq feedback
    (lambda
      (reason value / result)
      (setq result(get_tile"result"))
      (if(or(eq result ".")(eq result ""))(setq result(strcat "0" result)))
      (if(and(not done)(numberp(read result))(or (= reason 1)(= reason 2)))
        (progn
          (if(or(eq value ".")(numberp(read value)))
            (progn
              (setq x_reg (strcat result value))
              (set_tile"result" x_reg)(setq x_reg(atof x_reg))
              (if(eq value".")
                (mode_tile"point"1)  ;disable it until calc activated!
                )
              );progn
            );if
          );progn
        (progn                    ;result is operand, or done T
          (setq x_reg value done nil)
          (if(eq value".")(setq x_reg (atof(strcat "0" value))))
          (set_tile"result" value)
          );progn
        );if
      );lambda
    )
  ;***********************************************************************  
  (setq act
    (lambda
      (reason key / result)
      (setq result (get_tile "result"))
      (if(or(eq result ".")(eq result ""))(setq result(strcat "0" result)))
      (setq y_reg(atof result))
      (mode_tile "point"0)
      (if(or(= reason 1)(= reason 2))
        (cond
          ((member key (list "/" "-" "*" "+" "%"))
            ;(eq key "/")
            ;(eq key "*")
            ;(eq key "-")
            ;(eq key "+")
            ;(eq key "%")
            ;)                ;basic operands- register current display
            (progn
              (setq act_reg key)
              (set_tile "result" key)
              )
            )
          ((eq key "+/-")
            (progn
              (setq x_reg(* -1 x_reg))
              (set_tile"result"
                (if(minusp(read result))
                  (substr result 2)
                  (strcat "-" result)
                  )
                )
              )
            )
          ((eq key "%")
            (progn
              (setq x_reg(/ 1.0000 x_reg))
              (set_tile"result"
                (rtos x_reg 2 dec)
                )
              )
            )    
          ((eq key "root")
            (progn
              (setq x_reg (atof result) x_reg (sqrt x_reg))
              (set_tile"result" (rtos x_reg 2 dec))
              )
            )
          ((eq key "1/")
            (progn
              (setq x_reg(atof result) x_reg(/ 1 x_reg))
              (set_tile"result" (rtos x_reg 2 dec))
              )
            )
          );cond
        );if
      (if(member key (list "*" "+" "-" "/"))
        (mode_tile "result" 2))
      );lambda
    );setq act
  ;***********************************************************************  
  (setq do_clear(lambda()
                  (setq x_reg 0 y_reg 0)
                  (set_tile"result" "")
                  (mode_tile "result" 2)
                  )
    )  
  (setq do_calc
    (lambda(reason / result)
      (setq result(get_tile"result")done T)
      (if(numberp(read result))
        (if(or(= reason 1)(= reason 2))
          (progn
            (setq x_reg (atof result)
              result (if
                       (/= act_reg "%")
                       (list (read act_reg) y_reg x_reg)
                       (list '* 1 (eval(* (/ x_reg 100.0) y_reg)))
                       ))
            (setq result(eval result)x_reg result)
            )
          )
        )
      (set_tile "result"(rtos result 2 dec))
      (mode_tile "result" 2)
      )
    )
  ;***********************************************************************  
  (if(not(start_dialog))(exit))
  (unload_dialog dcl_id)(if arcad (reset))
  ;(if(/=(getvar"cmdactive")0)(princ result))
  )
(c:ddcalc)

