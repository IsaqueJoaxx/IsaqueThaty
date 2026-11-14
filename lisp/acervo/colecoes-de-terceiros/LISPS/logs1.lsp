;----------------------------------------------------------
; LOGS.LSP       Advanced AutoLISP Concepts  April 1993
; W.Kramer      CADENCE Magazine
;----------------------------------------------------------
;  Listing 2:  LOGON Function
;
(defun LOGON ( / DH WHO PRJS DRWS ACT DRW)
  (if LOGON_STR (logoff))
  (setq DH (load_dialog "cdnc4-93.dcl")
        WHO ""
        TMP 0)
  (if (> DH 0)
     (progn
       (while (and (= WHO "") (/= TMP 1)) ;; wait until WHO is entered
         (new_dialog "LOGON" DH)
         (action_tile "WHO" "(setq WHO $value)")
         (mode_tile "WHO" 2)
         (setq TMP (start_dialog))
         (if (= TMP 1) ;; OK
           (setq TMP (LOGON2)))
       )
       (unload_dialog DH)
     )
  )
  (princ)
)
;----------------------------------------------------------
; Listing 3:  Project-drawing link logon
;
(defun LOGON2 ( / PRJ)
  (new_dialog "LOGON1" DH)
  (if (findfile "projects.pms")
    (progn
       (load_prjs)
       ;;
       (start_list "PRJS")
       (foreach PRJ PRJS
         (add_list (cadr PRJ))
       )
       (end_list)
       (set_tile "PRJS" "0")
       (logon_drws "0")
       ;;
       (action_tile "PRJS" "(logon_drws $value)")
       (action_tile "DRWS" "(logon_actv $value)")
       ;;
       (setq TMP (start_dialog))
       ;;
       (cond
        ((= TMP 1)
          (alert "You must select a drawing and action")
          (setq TMP 0)
        )
        ((> TMP 1)
          (setq TMP 1 ;; valid exit
                LOGON_WHO WHO
                LOGON_DWG DWG
                LOGON_ACT ACT
                LOGON_STR (getvar "DATE")
          )
        )
       )
       TMP
    )
    (progn
      (prompt "PROJECT.PMS file not found in ACAD search path!")
      (exit) ;abort program
    )
  )
)
;----------------------------------------------------------
;  Listing 4:  Call back functions
;
(defun LOGON_DRWS (P / TMP)
  (setq P (atoi P)
        DRWS (caddr (nth P PRJS)))
  (start_list "DRWS")
  (mapcar 
    '(lambda (TMP) 
       (add_list (cadr TMP)))  ;; show descriptions
    DRWS)
  (end_list)
  (setq DRWS (mapcar 'car DRWS)) ;;save only drawing names
)

;----------------------------------------------------------
(defun LOGON_ACTV (D / TMP)
  (setq D (atoi D)
        DWG (nth D DRWS))
  (new_dialog "LOGON2" DH)
  (mode_tile "ACT" 2)
  (setq ACT "")
  (action_tile "ACT" "(setq ACT $value)")
  (setq TMP (start_dialog))
  (if (= TMP 1) ; OK
     (if (= ACT "")
        (alert "You must enter an activity description")
        (done_dialog 10)
     )
  )
)

;----------------------------------------------------------
; Listing 5:  Log off function
;
(defun LOGOFF ( / FH)
  (setq FH (findfile "LOG.LOG"))
  (if (null FH) (setq FH "LOG.LOG"))
  (setq FH (open FH "a"))
  (write-line (strcat "\"" LOGON_DWG "\" \""
                           LOGON_WHO "\" \""
                           LOGON_ACT "\" "
                           (rtos LOGON_STR 2 8) " "
                           (rtos (getvar "DATE") 2 8)
              ) FH)
  (setq FH (close FH)
        LOGON_STR nil
  )
)

;----------------------------------------------------------
; Listing 6:  File load functions
;
(defun LOAD_PRJS ( / FH FH2 LN TMP)
   (setq FH (open (findfile "projects.pms") "r")
         PRJS nil)
   (while (setq LN (read-line FH)) ;; read each project name
     (setq FH2 (findfile (strcat LN ".prj"))
           PRJ nil)
     (if FH2
       (progn
         (setq FH2 (open FH2 "r") ;; open project database
               PRJ (list LN (read-line FH2))  ;; description
               TMP nil)
         (while (setq LN (read-line FH2))
           (setq LN (read (strcat "(" LN ")"))
                 TMP (cons LN TMP))
         )
         (setq FH2 (close FH2)
               PRJ (append PRJ (list (reverse TMP)))
               PRJS (cons PRJ PRJS)
         )
     ))
   )
   (setq FH (close FH)
         PRJS (reverse PRJS))
)

;----------------------------------------------------------
;  Listing 7:  Time log report
;
(defun C:LOGS ( / PRJ FH TMP CNT LN DH)
  (if (setq FH (findfile "LOG.LOG"))
    (progn
      (prompt "\nLoading project data...")
      (load_prjs)
      (setq PRJS   ;;modify PRJS
        (mapcar 
          '(lambda (PRJ)
             (list (car PRJ) (cadr PRJ) ;;save name/descr
                   (mapcar ;;add 0.0 to end of each drw list
                     '(lambda (TMP)
                       (append TMP (list 0.0))
                      )
                      (caddr PRJ)
                   )
             )
           )
          PRJS)
      )
      (prompt "log file...")
      (setq FH (open (findfile "LOG.LOG") "r"))
      (while (setq LN (read-line FH))
         (setq LN (read (strcat "(" LN ")")) ;;convert to list
               TMP (- (nth 4 LN) (nth 3 LN)) ;;delta time calc
               CNT 0
         )
         (while (< CNT (length PRJS))
            (setq PRJ (nth CNT PRJS) ;;get project list member
                  CNT (1+ CNT)
            )
            (if (assoc (car LN) (caddr PRJ)) ;;drawing name match?
               (setq PRJS
                  (subst
                    (list 
                       (car PRJ) (cadr PRJ)
                       (subst
                          (append (reverse  ;;first part of
                                      (cdr   ;;list only
                                        (reverse
                                           (assoc (car LN) (caddr PRJ)))))
                                   (list 
                                     (+ TMP  ;;add time data to last
                                        (last ;member in PRJ drw list
                                          (assoc (car LN) (caddr PRJ))))))
                          (assoc (car LN) (caddr PRJ))
                          (caddr PRJ)
                       )
                    )
                    PRJ ;;old PRJ to remove
                    PRJS ;source list
                  )
                    CNT (1+ (length PRJS)) ;;set CNT to exit (WHILE)
               )
            )
         )
      )
      (prompt "completed")
      (setq DH (load_dialog "cdnc4-93.dcl"))
      (new_dialog "REPORT1" DH)
      (start_list "PROJ")
      (mapcar
        '(lambda (PRJ)
          (add_list (cadr PRJ))
         )
        PRJS)
      (end_list)
      (LOG_REP1 "0")
      (action_tile "PROJ" "(LOG_REP1 $value)")
      (start_dialog)
      (unload_dialog DH)
    )
    (alert "Log file not found!")
  )
  (princ)
)
                               
;----------------------------------------------------------
(defun LOG_REP1 (INX / TOT)
  (setq TMP (caddr (nth (atoi INX) PRJS))
        TOT 0.0)
  (start_list "DRWS")
  (foreach D TMP
     (add_list 
        (strcat 
           (cadr D) 
           "\t" 
           (rtos (* (last D) 1440.0) 2 3))) ;;minutes
     (setq TOT (+ TOT (last D)))
  )
  (end_list)
  (set_tile "TOT" (strcat "Total hours = " (rtos (* TOT 24.0) 2 3)))
)
