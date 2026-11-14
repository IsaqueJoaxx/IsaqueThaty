;;; --------------------------------------------------------------------------
;;; File     : BAR.LSP
;;; Author   : S J Johnson
;;; Date     : 11 August 1992
;;; Purpose  : Example of "percentage done" bar in lisp/DCL.
;;; Notes    : This is of limited usefulness due to the restrictions of R12's
;;;            dialogue box API.  No lisp functions which could affect the
;;;            screen or drawing database are allowed while a dialogue box is
;;;            visible, so this technique can only be used where a lot of
;;;            internal calculations are required and something is needed to
;;;            keep the user amused.  If a step of 1/100 is used, this routine
;;;            costs just over a second on a 386/25 running R12 DOS, so it
;;;            should be used with larger steps (eg. 1/50, 1/20) if the
;;;            processing period is to be as short as that in this example.
;;;            Potential for improvement: updating the bar could be made
;;;            conditional on time elapsed as well as the step.
;;; --------------------------------------------------------------------------
;;; Function definition structure:
;;;    C:BAR
;;;     ÃÄ init_vars
;;;     ÀÄ start_bar
;;;         ÃÄ init_bar
;;;         ÃÄ update_bar
;;;         ÃÄ update_num
;;;         ÃÄ update_both
;;;         ÀÄ do_something
;;; -------------------------------------------------------------------------
;;; Modified : S J Johnson
;;; Date     : 8 November 1993
;;; Notes    : Added ability to choose type of percentage done indication.
;;;            Improved calling mechanism to reduce number of times dialogue
;;;            is updated in tight loops.  General tidy-up.
;;; -------------------------------------------------------------------------


;; --------------------------------------------------------------------------
;; Function: C:BAR
;; Purpose : AutoCAD command function to demonstrate percentage done bar
;;           in dialogue box.
;; Local   : dcl_id: dialogue box identifier
;;           xdim, ydim: x and y dimensions of bar tile
;; Uses    : (init_vars) (start_bar): local
;; --------------------------------------------------------------------------

(defun C:BAR (/ ; Local functions and variables

; Local functions
  init_vars start_bar

; Local variables
  dcl_id xdim ydim)


;; --------------------------------------------------------------------------
;; Function: INIT_VARS
;; Purpose : Sets up variables and clears text percentage indicator.
;; --------------------------------------------------------------------------

  (defun init_vars ()
    (setq
      xdim (dimx_tile "per_bar")
      ydim (dimy_tile "per_bar")
    )
    (set_tile "per_num" "0")
  ) ; End init_vars


;; --------------------------------------------------------------------------
;; Function: START_BAR
;; Purpose : Callback function for "start" button.  Performs a pointless loop
;;           1000 times, optionally updating the dialogue box a total of
;;           1/step times.  Displays the total amount of time taken to do the
;;           loop.
;; Local   : #:         loop counter
;;           end#:      last value of # when loop is finished
;;           choice:    value of "choice" tile
;;           tim:       time remembered at start of loop
;;           last_done: amount done last time dialogue was updated
;;           step:      portion performed between dialogue updates
;;           done:      portion done so far (0.0 to 1.0)
;; Uses    : (init_bar) (update_bar) (update_num) (update_both)
;;             (do_something): local
;; Note    : Multiple (if) used rather than (cond) to ensure same evaluations
;;           are performed for each method, allowing a more valid timing
;;           comparison.
;; --------------------------------------------------------------------------

  (defun start_bar (/ init_bar update_bar update_bar update_both do_something
                      # end# choice tim last_done step done)


;; --------------------------------------------------------------------------
;; Function: INIT_BAR
;; Purpose : Clears bar percentage indicator.
;; --------------------------------------------------------------------------

    (defun init_bar ()
      (start_image "per_bar")
      (fill_image 0 0 xdim ydim -15) ; Clear bar
      (end_image)
    ) ; End init_bar


;; --------------------------------------------------------------------------
;; Function: UPDATE_BAR
;; Purpose : Updates the percentage done bar.
;; Params  : AMT: amount done so far (portion: 0.0 to 1.0)
;; --------------------------------------------------------------------------

    (defun update_bar (AMT)
      (start_image "per_bar")
      (fill_image 0 0 (fix (* AMT xdim)) ydim 1)
      (end_image)
    ) ; End update_bar


;; --------------------------------------------------------------------------
;; Function: UPDATE_NUM
;; Purpose : Updates the percentage done number.
;; Params  : AMT: amount done so far (portion: 0.0 to 1.0)
;; --------------------------------------------------------------------------

    (defun update_num (AMT)
      (set_tile "per_num" (rtos (* AMT 100) 2 0))
    ) ; End update_num


;; --------------------------------------------------------------------------
;; Function: UPDATE_BOTH
;; Purpose : Updates both the percentage done bar and number.
;; Params  : AMT: amount done so far (portion: 0.0 to 1.0)
;; Uses    : (update_bar) (update_num)
;; --------------------------------------------------------------------------

    (defun update_both (AMT)
      (update_bar AMT)
      (update_num AMT)
    ) ; End update_both


;; --------------------------------------------------------------------------
;; Function: DO_SOMETHING
;; Purpose : Does nothing.  Replace with something which does something.
;; --------------------------------------------------------------------------

    (defun do_something ()
      nil
    ) ; End do_something


; Start start_bar ----------

    (setq
      # 0.0
      end# 1000.0
      last_done #
      step (/ 1.0 100.0)
      choice (get_tile "choice")
      tim (getvar "DATE")
    )
    (if (or (= choice "bar") (= choice "both"))
      (init_bar)
    )
    (while (<= (setq # (1+ #)) end#)
      (do_something)
      (if (>= (setq done (/ # end#)) last_done)
        (progn
          (if (= choice "bar")
            (update_bar done)
          )
          (if (= choice "num")
            (update_num done)
          )
          (if (= choice "both")
            (update_both done)
          )
          (setq last_done (+ done step))
        )
      )
    )
    (if (= choice "bar")
      (update_bar 1.0)
    )
    (if (= choice "num")
      (update_num 1.0)
    )
    (if (= choice "both")
      (update_both 1.0)
    )
    (princ
      (strcat
        "\nTime taken: "
        (rtos (* 86400.0 (- (getvar "DATE") tim)) 2 2)
        " secs."
      )
    )
  ) ; End start_bar


;; Start C:BAR ----------

  (if (< (setq dcl_id (load_dialog "bar.dcl")) 0) (exit))
  (if (not (new_dialog "bar" dcl_id)) (exit))
  (set_tile "choice" "both")
  (init_vars)
  (action_tile "start" "(start_bar)")
  (start_dialog)                            ; Throw up the dialogue.
  (unload_dialog dcl_id)
  (princ)
) ; End C:BAR

'LOADED
