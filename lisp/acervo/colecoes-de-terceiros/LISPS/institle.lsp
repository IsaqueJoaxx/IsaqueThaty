; DrafTools   [Version 1.00a] 4/6/94       
;
; ***************************************
; ****  Author:  Owen Wengerd        ****
; ****                               ****
; ****  Manu-Soft Computer Services  ****
; ****  P.O. Box 84                  ****
; ****  Fredericksburg, OH  44627    ****
; ****  (216) 695-5903               ****
; ****  Compu-Serve ID:  71324,3252  ****
; ***************************************

(defun C:INSTITLE (/ 

;*** Local Variables ***

  bdr 
  ca 
  dcl_id 
  dlg_retcode 
  errflag 
  ia 
  insert_method
  ip 
  last_focus 
  olderr
  oldvar 
  pattern 
  restore
  scale 
  scale1
  scale2
  scale_ID 
  scale_list 
  select_method 
  t1 
  tb_dir 
  tb_file
  tbd 
  tblayer
  tblocks_found
  tbscales_path
  xdos_file
  xdos_loaded 


;*** Local Functions ***

  errexit
  institlex
  add_scale
  as_accept
  as_scale
  as_scale_ID
  change_path
  check_bdr
  check_layer
  check_scale
  clear_err
  compare_name
  delete_scale
  dismiss_dialog
  dlg_act
  edit_scale
  err
  find_scale
  fpath
  get_attrib_value
  get_default_ip
  get_help
  get_table
  get_tblayer
  get_tblock
  get_values
  is_visible
  open_file
  parse_path
  put_border
  remove_item
  rtd
  sort_list
  sort_search
  update_scale_file
  update_tbfile
  update_tbpath
  valid_name
  )

;*** Local Functions ***

(defun errexit (s)
  (if (= 8 (logand 8 (getvar "UNDOCTL"))) 
    (command "UNDO" "E" "UNDO" 1)
  )
  (if (member s '("console break" "Function cancelled"))
    (princ)    
    (princ (strcat "\nError:  " s))
  )
  (restore)
)

(defun institlex (/ t1)
  (setvar "ATTDIA" (nth 1 oldvar))
  (setvar "ATTMODE" (nth 2 oldvar))
  (setvar "ATTREQ" (nth 3 oldvar))
  (setvar "REGENMODE" (nth 4 oldvar))
  (setvar "EXPERT" (nth 5 oldvar))
  (setvar "CLAYER" (nth 6 oldvar))
  (setvar "LUPREC" (nth 7 oldvar))
  (if (/= 1 (setq t1 (logand 3 (nth 8 oldvar))))
    (progn
      (command "_UNDO")
      (if (/= 0 (logand 3 (getvar "UNDOCTL"))) (command "_C"))
      (command (if (= 0 t1) "_N" "_O"))
    )
  )
  (if xdos_loaded
    (progn
      (dt_dossetdrv (- (ascii (strcase (nth 9 oldvar))) 64))
      (dt_dossetdir (nth 9 oldvar))
      (if (and (/= T xdos_loaded) (not (xunload xdos_file nil)))
        (princ (strcat "\n**Cannot unload " xdos_file " from memory**\n "))
      )
    )
  )
  (setvar "CMDECHO" (car oldvar))
  (setq *error* olderr)
  (princ)
)

(defun rtd (a) (/ (* a 180.0) pi))

(defun dlg_act (key why value / t1)
  (cond
    ( (= key "replace")
      (setq insert_method value)
      (mode_tile (if errflag errflag last_focus) 2)
    )
    ( (= key "scale_select_method")
      (setq select_method (= "1" value))
      (if select_method
        (progn 
          (mode_tile "scale_ID" 0) 
          (mode_tile "scale1" 1)
          (mode_tile "scale2" 1)
          (set_tile "scale1" 
            (rtos (cadr (nth scale_ID scale_list)))
          )
          (set_tile "scale2" 
            (rtos (caddr (nth scale_ID scale_list)))
          )
          (check_scale)
          (mode_tile "scale_ID" 2)
          (if (> scale_ID 0) 
            (progn
              (mode_tile "delete_scale" 0)
              (mode_tile "edit_scale" 0)
            )
          )
        )
        (progn
          (mode_tile "scale_ID" 1) 
          (mode_tile "scale1" 0)
          (mode_tile "scale2" 0)
          (mode_tile "scale1" 2)
          (mode_tile "edit_scale" 1)
          (mode_tile "delete_scale" 1)
        )
      )
    )
    ( (and errflag (/= key errflag))
    )
    ( (= key "pattern")
      (if (not (wcmatch value "*[] `#`@`~`[`,`'!%^&()+={}|`\\:;\"<>/]*"))
        (progn
          (setq pattern value) 
          (update_tbfile)
          (clear_err)
        )
        (err "Pattern contains an invalid character." "pattern")
      )
    )
    ( (= key "scale_ID")
      (set_tile "scale1" 
        (rtos 
          (cadr (nth (setq scale_ID (atoi value)) scale_list))
        )
      )
      (set_tile "scale2" 
        (rtos 
          (caddr (nth scale_ID scale_list))
        )
      )
      (if (= scale_ID 0) 
        (progn (mode_tile "edit_scale" 1) (mode_tile "delete_scale" 1))
        (progn (mode_tile "edit_scale" 0) (mode_tile "delete_scale" 0))
      )
      (check_scale)
    )
    ( (= key "scale1") (check_scale))
    ( (= key "scale2") (check_scale))
    ( (= key "border")
      (setq bdr value)
      (check_bdr bdr)
    )
    ( (= key "angle")   
      (if (setq t1 (angtof (get_tile key)))
        (progn
          (setq ia t1)
          (set_tile key (angtos t1))
          (clear_err)
        )
        (err "Rotation angle must be a valid angle." key)
      )
    )
    ( (member key '("x_ip" "y_ip" "z_ip"))
      (if (numberp (setq t1 (distof value)))
        (progn
          (setq ip (subst t1 (nth (- (ascii key) 120) ip) ip))
          (set_tile key (rtos t1))
          (clear_err)
        )
        (err 
          (strcat 
            "Insertion Point "
            (chr (- (ascii key) 32)) 
            "-Coordinate must be a real number."
          )
          key
        )
      )
    )

  )
  (if errflag 
    (mode_tile errflag 2)
    (if (/= "replace" key) (setq last_focus key))
  )
)

(defun clear_err ()
  (set_tile "error" "")
  (setq errflag nil)
  (if (/= "" bdr)
    (progn
      (mode_tile "accept" 0)
      (mode_tile "preview" 0)
    )
  )
)

(defun err (msg key)
  (mode_tile "accept" 1)
  (mode_tile "preview" 1)
  (set_tile "error" msg)
  (setq errflag key)
)     

(defun is_visible (pt)
  (if
    (and
      pt
      (listp pt)
      (<= 
        (abs (- (car (getvar "VIEWCTR")) (car pt))) 
        (* (getvar "VIEWSIZE") (apply '/ (getvar "SCREENSIZE")) 0.51)
      )
      (<=
        (abs (- (cadr (getvar "VIEWCTR")) (cadr pt)))
        (/ (getvar "VIEWSIZE") 2)
      )
    )
    pt
  )
)

(defun get_default_ip (ss / cnt pt t1)
  (if
    (or
      (and ss 
        (if (= 1 (sslength ss)) 
          (setq pt (trans (cdr (assoc '10 (entget (ssname ss 0)))) 0 1))
          (progn
            (setq cnt -1)
            (while (< (setq cnt (1+ cnt)) (sslength ss)) 
              (if
                (setq t1 
                  (is_visible
                    (trans (cdr (assoc '10 (entget (ssname ss cnt)))) 0 1)
                  )
                )
                (setq pt (cons t1 pt))
              )
            )
            (setq pt (car pt))
          )
        )
      )
      (setq pt (is_visible '(0 0 0)))
      (setq pt (is_visible (getvar "LASTPOINT")))
    )
    pt
    (getvar "VIEWCTR")
  )
)

(defun compare_name (x y) (> (cdr (assoc '2 x)) (cdr (assoc '2 y))))

(defun sort_search (/ track)
  (mapcar '(lambda (x) (and x (sfunc x track) (setq track x))) lst)
  (setq lst (subst nil track lst))
  track
)

(defun sort_list (lst sfunc / tlst)
  (while 
    (apply 'or lst) 
    (setq tlst (append tlst (list (sort_search))))
  )
  tlst
)

(defun get_table (table / t1 t2)
  (while (setq t1 (tblnext table (not t1))) (setq t2 (append t2 (list t1))))
  t2
)

(defun fpath (filename / path)
  (if 
    (and
      *DT_PATH 
      (setq path
        (findfile 
          (strcat 
            *DT_PATH 
            (if (= "\\" (substr *DT_PATH (strlen *DT_PATH) 1)) "" "\\") 
            filename
          )
        )
      )
    )
    path
    (findfile filename)
  )
)

(defun get_help (/ help_path)
  (if (setq help_path (fpath "INSTITLE.HLP"))
    (dt_helpdlg help_path "")
    (alert "Cannot locate help file 'INSTITLE.HLP'!")
  )
  (mode_tile (if errflag errflag last_focus) 2)
)

(defun parse_path (name / ct)
  (setq ct (strlen name))
  (while (and (> ct 0) (/= "\\" (substr name ct 1))) (setq ct (1- ct)))
  (if (> ct 0) (setq name (substr name ct)) name)
)

(defun find_scale (scale / cnt)
  (setq cnt (length scale_list))
  (while 
    (and 
      (> (setq cnt (1- cnt)) 0)
      (not 
        (equal 
          scale 
          (/ (cadr (nth cnt scale_list)) (caddr (nth cnt scale_list)))
          (expt 10.0 (- -1 (getvar "LUPREC")))
        )
      )
    )
  )
  cnt
)

(defun open_file (filename / path t1 t2)
  (if (wcmatch filename "*[\\:]*") 
    (if (setq t1 (findfile filename)) 
      t1  
      (if (setq t1 (open filename "w")) (progn (close t1) filename))
    )
    (if (setq t1 (fpath filename))
      t1
      (if
        (setq t2
          (open
            (setq path
              (strcat
                (if 
                  (= 
                    "\\"
                    (substr 
                      (setq t1 
                        (if *DT_PATH *DT_PATH (dt_doscurdir))
                      )
                      (strlen t1) 
                      1
                    )
                  )
                  t1
                  (strcat t1 "\\")
                )
                filename
              )
            )
            "w"
          )
        )
        (progn (close t2) path)
      )
    )
  )
)

(defun check_bdr (bdr)
  (if (findfile (strcat tbd "\\" bdr))
    (clear_err)
    (if (= bdr "")
      (progn 
        (mode_tile "accept" 1) 
        (mode_tile "preview" 1) 
        (setq errflag nil)
      )
      (if (/= errflag "border")
        (err "Specified Border Doesn't Exist" "border")
      )
    )
  )
)

(defun get_attrib_value (ent tag / t1 t2)
  (while 
    (and 
      ent 
      (not t2)
      (setq ent (entnext ent))
      (/= "SEQEND" (cdr (assoc '0 (setq t1 (entget ent '("TBLOCK"))))))
    )
    (if 
      (and (= tag (cdr (assoc '2 t1))) (= "ATTRIB" (cdr (assoc '0 t1))))
      (if 
        (not
          (setq t2
            (cdr (assoc '1000 (cdr (assoc "TBLOCK" (cdr (assoc '-3 t1))))))
          )
        )
        (setq t2 (cdr (assoc '1 t1)))
      )          
    )
  )  
  t2
)

(defun get_values ()
  (check_scale)
  (setq *TBLAYER tblayer)
  (setq ip 
    (list 
      (distof (get_tile "x_ip")) 
      (distof (get_tile "y_ip")) 
      (distof (get_tile "z_ip")) 
    )
  )
  (setq insert_method (get_tile "replace"))
  (setq tbd 
    (progn
      (setq tbd (get_tile "path"))
      (if (= "\\" (substr tbd (strlen tbd) 1))
        (substr tbd 1 (1- (strlen tbd)))
        tbd
      )
    )
  )
  (setq bdr (get_tile "border"))
  (check_bdr bdr)
  (if (findfile (strcat tbd "\\" bdr))
    (progn
      (setq ia (angtof (get_tile "angle")))
      (done_dialog 1)
    )
    (progn
      (err 
        (if (= "" bdr) 
          "You Must Specify a Border."
          "Specified Border Doesn't Exist"
        )
        "border"
      )
      (mode_tile "border" 2)
      (mode_tile "border" 3)
    )
  )
)

(defun update_tbpath ()
  (start_list "tbdir")
  (mapcar 'add_list 
    (setq tb_dir 
      (append 
        '("\\")
        (if 
          (= "." (car (setq t1 (acad_strlsort (dt_dossubdir))))) 
          (cdr t1) 
          t1
        )
        (acad_strlsort 
          (mapcar 
            '(lambda (x) 
              (strcat ">" (chr (+ 64 x)) ":")
             ) 
            (dt_dosdrv)
          )
        ) 
      )
    )
  )
  (end_list)
)

(defun update_tbfile (/ t1)
  (setq t1 (dt_dosdir pattern 0))
  (start_list "tbfile")
  (if t1 (mapcar 'add_list (setq tb_file (acad_strlsort t1))))
  (end_list)
)

(defun put_border (value)
  (if (or (not errflag) (= errflag "border")) 
    (progn
      (set_tile "border" (setq bdr (nth (atoi value) tb_file)))
      (check_bdr bdr)
    )
    (mode_tile errflag 2)
  )
)

(defun change_path (value / t1 drv dir)
  (if (= errflag "border") 
    (progn (setq bdr "") (set_tile "border" "") (clear_err))
  )
  (if errflag
    (mode_tile errflag 2)
    (progn
      (setq dir (dt_doscurdir)
            drv (dt_doscurdrv)
      )
      (if (= ">" (substr (setq t1 (nth (atoi value) tb_dir)) 1 1))  
        (dt_dossetdrv (- (ascii (strcase (substr t1 2))) 64))
        (dt_dossetdir t1)
      )
      (if (setq t1 (dt_doscurdir))
        (progn
          (setq tbd 
            (progn
              (set_tile "path" (setq tbd (strcase t1 '1)))
              (if (= "\\" (substr tbd (strlen tbd) 1))
                (substr tbd 1 (1- (strlen tbd)))
                tbd
              )
            )
          )
          (update_tbpath)
          (update_tbfile)
          (check_bdr (get_tile "border"))
        )
        (progn
          (dt_dossetdrv drv)
          (dt_dossetdir dir)
          (alert "          Drive Not Ready          ")
        )
      )
    )
  )
)

(defun check_scale (/ t1)
  (if (and (numberp (setq t1 (distof (get_tile "scale1")))) (> t1 0))
    (progn
      (set_tile "scale1" (rtos (setq scale1 t1)))
      (clear_err)
    )
    (progn
      (err "Scale must be a positive, non-zero number." "scale1")
      (if select_method (edit_scale) (mode_tile "scale1" 2))
    )
  )
  (if (and (numberp (setq t1 (distof (get_tile "scale2")))) (> t1 0))
    (progn
      (set_tile "scale2" (rtos (setq scale2 t1)))
      (clear_err)
    )
    (progn
      (err "Scale must be a positive, non-zero number." "scale2")
      (if select_method (edit_scale) (mode_tile "scale2" 2))
    )
  )
  (setq scale (/ scale1 scale2))
)

(defun as_accept ()
  (if (and s1 s2 sitem)
    (dismiss_dialog 1)
    (if (or (= errflag "scale1") (= errflag "scale2"))
      (progn
        (err "Scale must be a positive number." errflag)
        (mode_tile errflag 2)
      )
      (progn
        (err "Enter a description." "scale_ID")
        (mode_tile errflag 2)
      )
    )
  )
)

(defun as_scale (value key)
  (if (= errflag "scale_ID")
    (mode_tile errflag 2)
    (if 
      (and 
        (numberp (setq t1 (distof value))) 
        (> t1 0)
      )
      (progn
        (set (if (= key "scale1") 's1 's2) t1)
        (if (= errflag key)
          (progn
            (set_tile "error" "")
            (setq errflag nil)
            (if (/= "< Default >" (get_tile "scale_ID")) 
              (mode_tile "accept" 0)
            )
          )
        )
      )
      (progn
        (err
          (if (> (strlen value) 0) 
            "Scale must be a positive number." 
            "Enter a scale factor."
          )
          key
        )
        (set (if (= key "scale1") 's1 's2) nil)
        (mode_tile errflag 2)
      )
    )
  )
)

(defun as_scale_ID (value)
  (if (or (= errflag "scale1") (= errflag "scale2"))
    (mode_tile errflag 2)
    (if (and (/= "< Default >" value) (> (strlen (setq sitem value)) 0))
      (progn (set_tile "error" "") (setq errflag nil) (mode_tile "accept" 0))
      (progn (setq sitem nil errflag nil) (set_tile "error" ""))
    )
  )
)

(defun add_scale (/ sitem s1 s2 t1 t2)
  (if (and errflag (/= errflag "border"))
    (mode_tile errflag 2)
    (if (new_dialog "SCALE_A" dcl_id)
      (progn
        (setq s1      (if select_method (cadr (car scale_list)) scale1)
              s2      (if select_method (caddr (car scale_list)) scale2)
              sitem   nil
        )
        (set_tile "scale_ID" (car (car scale_list)))
        (set_tile "scale1" (rtos s1))
        (set_tile "scale2" (rtos s2))
        (action_tile "accept" "(as_accept)")
        (action_tile "cancel" "(done_dialog 0)")
        (action_tile "scale1" "(as_scale $value $key)")
        (action_tile "scale2" "(as_scale $value $key)")
        (action_tile "scale_ID" "(as_scale_ID $value)")
        (mode_tile "accept" 1)
        (if (= 1 (start_dialog))    
          (progn  
            (setq t1     (list (car scale_list))
                  scale1 s1
                  scale2 s2
                  scale  (/ scale1 scale2)
            )
            (while 
              (and
                (setq scale_list (cdr scale_list))
                (> 
                  scale 
                  (/ (cadr (car scale_list)) (caddr (car scale_list)))
                ) 
              )
              (setq t1 (append t1 (list (car scale_list))))
            )
            (setq t2 (itoa (setq scale_ID (length t1))))
            (setq scale_list 
              (append t1 (list (list sitem scale1 scale2)) scale_list)
            )
            (update_scale_file)
            (start_list "scale_ID")
            (foreach t1 scale_list (add_list (car t1)))
            (end_list)
            (set_tile "select_method" "1")  
            (setq select_method (not nil))
            (mode_tile "scale_ID" 0)
            (mode_tile "scale1" 1)
            (mode_tile "scale2" 1)
            (set_tile "scale1" (rtos scale1))                  
            (set_tile "scale2" (rtos scale2))                  
            (set_tile "scale_ID" t2)
            (mode_tile "scale_ID" 2)
            (setq last_focus "scale_ID")
            (mode_tile "edit_scale" 0)
            (mode_tile "delete_scale" 0)
          )
          (if last_focus (mode_tile last_focus 2))
        )
      )
      (err "Child Dialog Box 'SCALE_A' Cannot Initialize" "add_scale")
    )
  )
)
  
(defun edit_scale (/ sitem s1 s2 t1 t2)
  (if (and errflag (/= errflag "border"))
    (mode_tile errflag 2)
    (if (new_dialog "SCALE_E" dcl_id)
      (progn
        (setq s1      (cadr (nth scale_ID scale_list))
              s2      (caddr (nth scale_ID scale_list))
              sitem   (car (nth scale_ID scale_list))
        )
        (set_tile "scale_ID" sitem)
        (set_tile "scale1" (rtos s1))
        (set_tile "scale2" (rtos s2))
        (action_tile "accept" "(as_accept)")
        (action_tile "cancel" "(done_dialog 0)")
        (action_tile "scale1" "(as_scale $value $key)")
        (action_tile "scale2" "(as_scale $value $key)")
        (action_tile "scale_ID" "(as_scale_ID $value)")
        (if (= 1 (start_dialog))    
          (progn  
            (remove_item)
            (setq t1     (list (car scale_list))
                  scale1 s1
                  scale2 s2
                  scale  (/ scale1 scale2)
            )
            (while 
              (and
                (setq scale_list (cdr scale_list))
                (> 
                  scale 
                  (/ (cadr (car scale_list)) (caddr (car scale_list)))
                ) 
              )
              (setq t1 (append t1 (list (car scale_list))))
            )
            (setq t2 (itoa (setq scale_ID (length t1))))
            (setq scale_list 
              (append t1 (list (list sitem s1 s2)) scale_list)
            )
            (update_scale_file)
            (start_list "scale_ID")
            (foreach t1 scale_list (add_list (car t1)))
            (end_list)
            (set_tile "select_method" "1")  
            (setq select_method (not nil))
            (set_tile "scale1" (rtos scale1))                  
            (set_tile "scale2" (rtos scale2))                  
            (set_tile "scale_ID" t2)
            (mode_tile "scale_ID" 2)
            (setq last_focus "scale_ID")
            (mode_tile "edit_scale" 0)
            (mode_tile "delete_scale" 0)
          )
          (if last_focus (mode_tile last_focus 2))
        )
      )
      (err "Child Dialog Box 'SCALE_E' Cannot Initialize" "edit_scale")
    )
  )
)
  
(defun remove_item (/ t1 cnt)
  (setq cnt (1- (length scale_list)))
  (while (>= cnt 0) 
    (progn 
      (if (/= cnt scale_ID) (setq t1 (cons (nth cnt scale_list) t1))) 
      (setq cnt (1- cnt))
    )
  )
  (setq scale_list t1)
)

(defun delete_scale ()
  (if (and errflag (/= errflag "border"))
    (mode_tile errflag 2)
    (if (new_dialog "SCALE_D" dcl_id)
      (progn
        (action_tile "delete" "(done_dialog 1)")
        (action_tile "cancel" "(done_dialog 0)")
        (if (= 1 (start_dialog))
          (progn
            (remove_item)
            (update_scale_file)
            (start_list "scale_ID")
            (foreach t1 scale_list (add_list (car t1)))
            (end_list)
            (setq scale_ID (1- scale_ID))
            (set_tile "scale_ID" (itoa scale_ID))
            (setq scale 
              (/ 
                (setq scale1 (cadr (nth scale_ID scale_list)))
                (setq scale2 (caddr (nth scale_ID scale_list)))
              )
            )
            (set_tile "scale1" (rtos scale1))
            (set_tile "scale2" (rtos scale2))
            (if (= scale_ID 0) (mode_tile "delete_scale" 1))
            (mode_tile "scale_ID" 2)
            (setq last_focus "scale_ID")
          )
          (if last_focus (mode_tile last_focus 2))
        )
      )
      (err "Child Dialog Box 'SCALE_D' Cannot Initialize" "delete_scale")
    )
  )
)

(defun update_scale_file (/ fh)
  (if (setq fh (open tbscales_path "w"))
    (progn
      (foreach t1 (cdr scale_list)
        (progn 
          (write-line (car t1) fh) 
          (write-line (rtos (cadr t1) 2 10) fh) 
          (write-line (rtos (caddr t1) 2 10) fh) 
        )
      )
      (close fh)
    )
  )
)

(defun valid_name (name)
  (not (wcmatch name "*[] `#`@`.`?`*`~`[`,`'!%^&()+={}|`\\:;\"<>/]*"))
)

(defun check_layer (/ t1)
  (if (and (/= "" (setq t1 (get_tile "layer"))) (valid_name t1))
    (progn
      (setq tblayer (strcase t1))
      (done_dialog 1)
    )
    (progn
      (err 
        (if (= t1 "")
          "Press <Cancel> or specify a layer name."
          "Layer name contains invalid characters."
        )
        "layer"
      )
      (mode_tile "layer" 2)
    )
  )
)

(defun get_tblock (tblockss / d p il t1 t2 sp)
  (setq p 0)
  (if (< 1 (sslength tblockss))
    (progn
      (setq t1 0)
      (setq il nil)
      (repeat (sslength tblockss)
        (setq il
          (cons
            (cdr (assoc '10 (entget (ssname tblockss t1))))
            il
          )
        )
        (setq t1 (1+ t1))
      )
      (setq il (reverse il))
      (if
        (not 
          (setq sp 
            (getpoint 
              "\n \nPick Insertion Point of Title Block To Replace:  "
            )
          )
        )
        (setq sp (getvar "LASTPOINT"))
      )
      (setq d (distance (car il) sp))
      (setq t1 1)
      (while (< t1 (length il))
        (if
          (> d (setq t2 (distance (nth t1 il) sp)))
          (progn
            (setq d t2)
            (setq p t1)
          )
        )
        (setq t1 (1+ t1))
      )
    )
  )                  
  (ssname tblockss p)
)

(defun get_tblayer (/ layer_list)
  (if (and errflag (/= errflag "border"))
    (mode_tile errflag 2)
    (if (new_dialog "TBLAYER" dcl_id)
      (progn
        (start_list "existing")
        (mapcar 'add_list 
          (setq layer_list
            (mapcar 
              '(lambda (x) 
                (cdr (assoc '2 x))
              ) 
              (reverse (sort_list (get_table "LAYER") compare_name))
            )
          )
        )
        (end_list)
        (action_tile "accept" "(check_layer)")
        (action_tile "cancel" "(done_dialog 0)")
        (action_tile "existing" 
          (strcat
            "(and (= 4 $reason)" 
            " (set_tile \"layer\" (nth (atoi $value) layer_list))"
            " (check_layer))"
          )
        )
        (set_tile "layer" tblayer)
        (start_dialog)
        (set_tile "clayer" tblayer)
        (if last_focus (mode_tile last_focus 2))
      )
      (set_tile "error" "Child Dialog Box 'TBLAYER' Cannot Initialize")
    )
  )
)

(defun dismiss_dialog (retcode)
  (if 
    (and
      errflag
      (not (and (= retcode 3) (wcmatch errflag "?_ip")))
      (not (and (= retcode 4) (= errflag "angle")))
      (and (= retcode 2) (= errflag "border"))
    )
    (mode_tile errflag 2) 
    (progn
      (if (and errflag (/= errflag "border"))
        (progn (setq last_focus errflag) (clear_err))
      )
      (done_dialog retcode)
    )
  )
)


;*********************************************************
;*******************  MAIN PROGRAM  **********************
;*********************************************************

  (setq T (not nil))
  (if 
    (and 
      (or 
        (setq xdos_loaded (= 'EXSUBR (type dt_dosdir))) 
        (setq xdos_loaded 
          (if
            (setq t1
              (fpath
                (setq xdos_file
                  (if (= (getvar "PLATFORM") "Microsoft Windows")
                    "xdosw_dt.exe"
                    "xdos_dt.exp"
                  )
                )
              ) 
            )
            (if (setq t2 (xload t1 nil)) 
              t2 
              (progn (xunload xdos_file) (xload t1 nil))
            )
          )
        )
      )
      (cond 
        ( (not *TBSCALES)
          (setq tbscales_path (open_file "TBSCALES.TBD"))
        )
        ( (wcmatch *TBSCALES "*`.[Tt][Bb][Dd]")
          (setq tbscales_path (open_file *TBSCALES))
        )
        ( (wcmatch *TBSCALES "~*`.*") 
          (setq tbscales_path (open_file (strcat *TBSCALES ".TBD")))
        )
        ( T
          (alert 
            (strcat
              "  Invalid extension specified in *TBSCALES   "
              "\n   For Predefined Scale Definition File."
              "\n\n     Using:  TBSCALES.TBD"
            )
          )
          (setq tbscales_path (open_file "TBSCALES.TBD"))
        )
      )    
      (setq dcl_id (if (setq t1 (fpath "INSTITLE.DCL")) (load_dialog t1)))
    ) 
    (progn
      (setq oldvar
        (list
          (getvar "CMDECHO")
          (getvar "ATTDIA")
          (getvar "ATTMODE")
          (getvar "ATTREQ")
          (getvar "REGENMODE")
          (getvar "EXPERT")
          (getvar "CLAYER")
          (getvar "LUPREC")
          (getvar "UNDOCTL")
          (dt_doscurdir)
        )
      )
      (setq olderr  *error*
            restore institlex
            *error* errexit
      )
      (setvar "CMDECHO" 0)
      (setvar "REGENMODE" 0)
      (setvar "EXPERT" 0)
      (setvar "ATTDIA" 1)
      (setvar "ATTMODE" 0)
      (setvar "ATTREQ" 0)
      (if (/= 1 (setq t1 (logand 3 (getvar "UNDOCTL"))))
        (progn
          (command "_UNDO") 
          (if (/= 0 t1) (command "_C")) 
          (command "_A")
        )
      )
      (terpri)
      (setq tblayer (if *TBLAYER *TBLAYER "TITLE"))
      (setq scale
        (if (and *DWGSCALE (numberp *DWGSCALE)) (/ 1.0 *DWGSCALE) 1.0)
      )
      (setq scale_list 
        (list (list "< Default >" (distof "1.0") (/ 1.0 scale)))
      )
      (setq tbd (open tbscales_path "r"))
      (while (setq t1 (read-line tbd))
        (setq scale_list
          (append 
            scale_list 
            (list 
              (list 
                t1
                (if (setq t1 (read-line tbd)) (atof t1) '1.0)
                (if (setq t1 (read-line tbd)) (atof t1) '1.0)
              )
            )
          )
        )
      )
      (close tbd)
      (or
        (setq tblocks_found (ssget "X" '((0 . "INSERT") (-3 ("TBLOCK")))))
        (setq tblocks_found 
          (ssget "X" 
            (list 
              (cons 0 "INSERT") 
              (cons 8 (if *TBLAYER *TBLAYER "TITLE"))
            )
          )
        )
      )
      (setq ip            (get_default_ip tblocks_found)
            ia            '0.0
            pattern       "*.bdr"
            tbd           (if *DT_PATH *DT_PATH tbscales_path)
            bdr           ""
            dlg_retcode   6 
            last_focus    "border"
            scale1        '1.0
            scale2        (/ 1.0 scale)
            scale_ID      nil
            select_method (not nil)
      )         
      (setq tbd
        (progn
          (dt_dossetdrv (- (ascii (strcase tbd)) 64))
          (dt_dossetdir 
            (if 
              (and 
                tbd 
                (> (strlen tbd) 3) 
                (= "\\" (substr tbd (strlen tbd) 1))
              )
              (substr tbd 1 (1- (strlen tbd)))
              tbd
            )
          )
        )
      )         
      (while (and (> dlg_retcode 1) (new_dialog "INSTITLE" dcl_id))
        (if tblocks_found
          (progn
            (mode_tile "replace" 0)
            (set_tile "replace" (if insert_method insert_method "0"))
          )
          (progn
            (mode_tile "replace" 1)
            (set_tile "replace" "0")
          )
        )
        (set_tile "angle" (angtos ia))
        (set_tile "x_ip" (rtos (car ip) 2))
        (set_tile "y_ip" (rtos (cadr ip) 2))
        (set_tile "z_ip" (rtos (caddr ip) 2))
        (set_tile "path" tbd)
        (set_tile "border" bdr)
        (set_tile "clayer" tblayer)
        (if (findfile (strcat tbd "\\" bdr)) (clear_err) (err "" "border"))
        (set_tile "pattern" pattern)
        (start_list "scale_ID")
        (foreach t1 scale_list (add_list (car t1)))
        (end_list)
        (set_tile 
          "scale_ID" 
          (itoa 
            (if scale_ID scale_ID (setq scale_ID (find_scale scale)))
          )
        )
        (if (and select_method (> scale_ID 0))
          (progn
            (mode_tile "edit_scale" 0)
            (mode_tile "delete_scale" 0)
          )
        )
        (set_tile "scale_select_method" (if select_method "1" "0"))
        (mode_tile "scale_ID" (if select_method '0 '1))
        (mode_tile "scale1" (if select_method '1 '0))
        (mode_tile "scale2" (if select_method '1 '0))
        (set_tile "scale1" (rtos scale1))
        (set_tile "scale2" (rtos scale2))
        (update_tbpath)      
        (update_tbfile)
        (action_tile "help" "(get_help)")
        (action_tile "tbfile" "(put_border $value)")
        (action_tile "tbdir" "(if (= 4 $reason) (change_path $value))")
        (action_tile "add_scale" "(add_scale)")
        (action_tile "edit_scale" "(edit_scale)")
        (action_tile "delete_scale" "(delete_scale)")
        (action_tile "layer" "(get_tblayer)")
        (action_tile "preview" 
          (strcat
            "(if (= bdr \"\")" 
              " (progn"
                " (err \"You Must Specify a Border.\" \"border\")" 
                " (mode_tile \"border\" 2)"
              ")"
              " (dismiss_dialog 2)"
            ")"
          )
        )
        (action_tile "pick_ip" "(dismiss_dialog 3)")
        (action_tile "digitize_angle" "(dismiss_dialog 4)")
        (action_tile "accept" "(get_values)")
        (action_tile "cancel" "(done_dialog 0)")
        (foreach t1
          '(  "pattern"   "border"    "scale1"    "scale2"
              "scale_ID"  "x_ip"      "y_ip"      "z_ip"     
              "angle"     "scale_select_method"   "replace"
          )
          (action_tile t1 "(dlg_act $key $reason $value)")
        )
        (if last_focus (mode_tile last_focus 2))
        (setq dlg_retcode (start_dialog))
        (cond
          (
            (= 2 dlg_retcode)
            (prompt "\nPress any key to continue\n")
            (command 
              "_UNDO"
              "_G"
              "_LAYER"
              (if
                (tblsearch "LAYER" (if tblayer tblayer "TITLE"))
                "_S"
                "_M"
              )
              (if tblayer tblayer "TITLE")
              ""
              "_INSERT"
              (strcat (substr bdr 1 (- (strlen bdr) 4)) "=" tbd "\\" bdr)
              "_rotate"
              (rtd ia)
              "_scale"
              (/ 1.0 scale)
              ip
              (grread)
              (grread 1)
              cancel
              "_UNDO"
              "_E"
              "_UNDO"
              "1"
            )  
            (redraw)
            (princ "\nReturning to Dialog Box\n \n ")
          )
          (
            (= 3 dlg_retcode)
            (if (and tbd bdr (findfile (strcat tbd "\\" bdr)))
              (progn
                (prompt "\nPick the Title Block Insertion Point: ")
                (command 
                  "_UNDO"
                  "_G"
                  "_INSERT"
                  (strcat (substr bdr 1 (- (strlen bdr) 4)) "=" tbd "\\" bdr)
                  "_rotate"
                  (rtd ia)
                  "_pscale"
                  (/ 1.0 scale)
                  pause
                  cancel
                )
                (setq t1 (getvar "LASTPOINT"))
                (command
                  "_UNDO"
                  "_E"
                  "_UNDO"
                  "1"
                )
                (redraw)
              )
              (setq t1 
                (getpoint 
                  (strcat "\nPick the Title Block Insertion Point <"
                          (rtos (car ip))
                          ","
                          (rtos (cadr ip))
                          ","
                          (if (caddr ip) (rtos (caddr ip)) (rtos '0.0))
                          ">:  "
                  )
                )
              )
            )
            (if t1
              (progn
                (princ "\nNew Insertion Point Selected\n \n ")
                (set_tile "x_ip" (rtos (car (setq ip t1)) 2))
                (set_tile "y_ip" (rtos (cadr ip) 2))
                (set_tile "z_ip" (rtos (caddr ip) 2))
              )
              (princ "\n \n ")
            )
          )
          (
            (= 4 dlg_retcode)
            (setq t1
              (getorient
                (strcat
                  "\nPick the Title Block Rotation Angle <"
                  (angtos ia)
                  ">:  "
                )
              )
            )
            (terpri)
            (if t1 (set_tile "angle" (angtos (setq ia t1))))
          )
          (
            (= 1 dlg_retcode)
            (setq *TBLAYER tblayer)
            (setq scale     (/ 1.0 scale)
                  *DWGSCALE scale
            )
            (if (setq t1 (fpath "TBLOCK.LSP")) (load t1))
            (setq tblocks_found
              (if (and tblocks_found (= "1" insert_method))
                (get_tblock tblocks_found)
                nil
              )
            )
            (if 
              (setq t1 
                (fpath 
                  (strcat 
                    tbd 
                    "\\" 
                    (substr bdr 1 (- (strlen bdr) 4)) 
                    ".lsp"
                  )
                )
              )
              (load t1)
            )
            (if (listp SETSCALES) (SETSCALES scale))
            (terpri)
            (graphscr)
            (command 
              "_UNDO"
              "_G"
              "_LAYER"
              (if
                (tblsearch "LAYER" (if *TBLAYER *TBLAYER "TITLE"))
                "_S"
                "_M"
              )
              (if *TBLAYER *TBLAYER "TITLE")
              ""
            )
            (command "_INSERT"
              (strcat (substr bdr 1 (- (strlen bdr) 4)) "=" tbd "\\" bdr)
              cancel
            )
            (prompt "\n \nResolving Title Block Attribute Values...")
            (setq ca 
              (cdr 
                (assoc 
                  '-2 
                  (tblsearch "BLOCK" (substr bdr 1 (- (strlen bdr) 4)))
                )
              )
            )
            (while
              (and ca (setq t1 (entget ca)))
              (and
                (= "ATTDEF" (cdr (assoc '0 t1)))
                (progn
                  (setq t2 nil)
                  (if 
                    (and
                      tblocks_found
                      (setq t2 
                        (get_attrib_value tblocks_found (cdr (assoc '2 t1)))
                      )
                    )
                    (setq t2
                      (entmod
                        (subst
                          (cons '1 t2)
                          (assoc '1 t1)
                          t1
                        )
                      )
                    )
                  )
                  (if t2 (setq t1 t2) T)
                )
                (and 
                  (= "=" (substr (cdr (assoc '1 t1)) 1 1))
                  (/= "=" (substr (cdr (assoc '1 t1)) 2 1))
                )
                (entmod
                  (subst
                    (cons '1 (eval (read (substr (cdr (assoc '1 t1)) 2))))
                    (assoc '1 t1)
                    t1
                  )
                )
              )
              (setq ca (entnext ca))
            )
            (prompt "\n \nAttribute Values Updated\n ")
            (if tblocks_found (command "_ERASE" tblocks_found ""))
            (setvar "ATTREQ" 1)
            (regapp "TBLOCK")
            (command "_INSERT"
              (substr bdr 1 (- (strlen bdr) 4))
              "_rotate"
              (rtd ia)
              "_scale"
              scale
              ip
            )  
            (setq *TBATTRIB (setq ca (entlast)))
            (if (/= 0 (cdr (assoc '66 (entget ca))))
              (progn
                (while
                  (and (setq ca (entnext ca)) (setq t1 (entget ca)))
                  (and
                    (= "ATTRIB" (cdr (assoc '0 t1)))
                    (= "==" (substr (cdr (assoc '1 t1)) 1 2))
                    (entmod
                      (append
                        (subst
                          (cons '1 
                            (eval 
                              (read (substr (setq t2 (cdr (assoc '1 t1))) 3))
                            )
                          )
                          (assoc '1 t1)
                          t1
                        )
                        (list (list -3 (list "TBLOCK" (cons 1000 t2))))
                      )
                    )
                  )
                )
                (setvar "ATTMODE" 1)
              )
            )
            (entmod
              (append 
                (entget (entlast)) 
                (list (list '-3 (list "TBLOCK" (cons 1071 scale))))
              )
            )
            (command "_UNDO" "_E")
          )
        )
      )
      (unload_dialog dcl_id)
      (restore)
    )
    (alert 
      (cond 
        ( xdos_loaded
          (strcat 
            "Dialog Box Definition File 'INSTITLE.DCL' not Found"
            "\n                Cannot Continue!"
          )
        )
        ( tbscales_path
          (strcat 
            " ADS Application '"
            xdos_file
            "' not Found "
            "\n           Cannot Continue!"
          )
        )
        ( T
          (strcat
            "Illegal Path for Predefined Scale Definition File"
            "\n               Cannot Continue!"
          )
        )
      )
    )
  )
)
