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


(defun EDATTRIB (entity / 

; *** Local Variables ***

  dcl_id         
  dlg_retcode    
  entlist        
  errflag 
  just_def_list  
  last_focus     
  old_entlist    
  olderr         
  oldvar         
  restore
  style_list
  t1             
  t2             
  

; *** Local Functions ***  
  
  errexit        
  edattribx          
  ap_set             
  check_layer        
  check_ltype        
  clear_err          
  color_name         
  compare_name       
  dismiss_dialog     
  dlg_act            
  ent_edit           
  err                
  find_idx           
  fpath              
  get_color          
  get_help           
  get_layer          
  get_ltype          
  get_table          
  ip_set             
  parse_path         
  rtd
  sort_list          
  sort_search        
  update_color       
  update_dlg         
  valid_name         
  )


;*****************  Function Definitions  ******************

(defun errexit (s)
  (princ "\nError:  ")
  (princ s)
  (restore)
)

(defun edattribx ()
  (setvar "ATTMODE" (nth 1 oldvar))
  (setvar "ATTREQ" (nth 2 oldvar))
  (setvar "REGENMODE" (nth 3 oldvar))
  (setvar "EXPERT" (nth 4 oldvar))
  (setvar "CMDECHO" (car oldvar))
  (command "_UCS" "_P")
  (setq *error* olderr)
)

(defun rtd (a) (/ (* a 180.0) pi))

(defun dlg_act (key why value / t1)
  (cond
    ( (= key "invisible")
      (ent_edit 
        (if (= "0" value)  
          (logand 254 (cdr (assoc '70 entlist)))  
          (logior 1 (cdr (assoc '70 entlist)))
        )
        '70
      )
    )
    ( (= key "constant")
      (ent_edit 
        (if (= "0" value)  
          (logand 253 (cdr (assoc '70 entlist)))  
          (logior 2 (cdr (assoc '70 entlist)))
        )
        '70
      )
    )
    ( (= key "verify")
      (ent_edit 
        (if (= "0" value)  
          (logand 251 (cdr (assoc '70 entlist)))  
          (logior 4 (cdr (assoc '70 entlist)))
        )
        '70
      )
    )
    ( (= key "preset")
      (ent_edit 
        (if (= "0" value)  
          (logand 247 (cdr (assoc '70 entlist)))  
          (logior 8 (cdr (assoc '70 entlist)))
        )
        '70
      )
    )
    ( (= key "upside_down")
      (ent_edit 
        (if (= "0" value)  
          (logand 251 (cdr (assoc '71 entlist)))  
          (logior 4 (cdr (assoc '71 entlist)))
        )
        '71
      )
    )
    ( (= key "backward")
      (ent_edit 
        (if (= "0" value)  
          (logand 253 (cdr (assoc '71 entlist)))  
          (logior 2 (cdr (assoc '71 entlist)))
        )
        '71
      )
    )
    ( (= key "update_style")
      (ent_edit 
        (cdr 
          (assoc '70 (setq t1 (nth (atoi (get_tile "style")) style_list)))
        )  
        '70
      )
      (ent_edit 
        (if (= '0 (setq t2 (cdr (assoc '40 t1)))) (cdr (assoc '42 t1)) t2)  
        '40
      )
      (ent_edit (cdr (assoc '41 t1)) '41)
      (ent_edit (cdr (assoc '50 t1)) '51)
      (if 
        (or 
          (not errflag)
          (member errflag 
            '("height" "width" "oblique" "upside_down" "backward")
          )
        )
        (progn
          (if errflag (mode_tile errflag 2) (mode_tile last_focus 2))
          (update_dlg entlist)
          (clear_err)
        )
        (progn
          (setq t1 (get_tile errflag))
          (update_dlg entlist)
          (set_tile errflag t1)
          (mode_tile errflag 2)
        )
      )
    )
    ( (and errflag (/= errflag key))  
    )
    ( (= key "thickness")    
      (if (numberp (setq t1 (distof value)))
        (progn
          (clear_err)
          (ent_edit t1 '39)
          (set_tile key (rtos t1))
        )
        (err "Thickness must be a real number." key)
      )
    )
    ( (= key "height")    
      (if (and (numberp (setq t1 (distof value))) (> t1 0))
        (progn
          (clear_err)
          (ent_edit t1 '40)
          (set_tile key (rtos t1))
        )
        (err "Height must be positive and non-zero." key)
      )
    )
    ( (= key "width")    
      (if (and (numberp (setq t1 (distof value))) (> t1 0))
        (progn
          (clear_err)
          (ent_edit t1 '41)
          (set_tile key (rtos t1))
        )
        (err "Width Factor must be positive and non-zero." key)
      )
    )
    ( (= key "oblique")    
      (if (setq t1 (angtof (get_tile key)))
        (progn
          (clear_err)
          (ent_edit t1 '51)
          (set_tile key (angtos t1))
        )
        (err "Oblique angle must be a valid angle." key)
      )
    )
    ( (= key "rotation")    
      (if (setq t1 (angtof (get_tile key)))
        (progn
          (clear_err)
          (ent_edit t1 '50)
          (set_tile key (angtos t1))
        )
        (err "Rotation angle must be a valid angle." key)
      )
    )
    ( (= key "tag")
      (ent_edit value '2)
      (set_tile key (strcase value))
    )
    ( (= key "prompt")
      (ent_edit value '3)
    )
    ( (= key "default")
      (ent_edit value '1)
    )
    ( (member key '("x_ip" "y_ip" "z_ip"))
      (if (numberp (setq t1 (distof value)))
        (progn
          (clear_err)
          (ent_edit
            (subst
              t1
              (nth (- (ascii key) 120) (setq t2 (cdr (assoc '10 entlist))))
              t2
            )
            '10
          )
          (set_tile key (rtos t1))
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
    ( (member key '("x_ap" "y_ap" "z_ap"))
      (if (numberp (setq t1 (distof value)))
        (progn
          (clear_err)
          (ent_edit
            (subst
              t1
              (nth (- (ascii key) 120) (setq t2 (cdr (assoc '11 entlist))))
              t2
            )
            '11
          )
          (set_tile key (rtos t1))
        )
        (err 
          (strcat 
            "Alignment Point "
            (chr (- (ascii key) 32))  
            "-Coordinate must be a real number."
          )
          key
        )
      )
    )
    ( (= key "style")
      (ent_edit (cdr (assoc '2 (setq t1 (nth (atoi value) style_list)))) '7)
    )
    ( (= key "justify")
      (ent_edit
        (car (nth (atoi value) just_def_list))
        '72
      )  
      (ent_edit
        (cadr (nth (atoi value) just_def_list))
        '74
      )  
      (update_dlg entlist)
    )
  )
  (if errflag (mode_tile errflag 2) (setq last_focus key))
)

(defun clear_err ()
  (set_tile "error" "")
  (setq errflag nil)
  (mode_tile "accept" 0)
  (mode_tile "preview" 0)
)

(defun err (msg key)
  (mode_tile "accept" 1)
  (mode_tile "preview" 1)
  (set_tile "error" msg)
  (setq errflag key)
)      

(defun ent_edit (newvalue grp)
  (setq entlist
    (if (assoc grp entlist)
      (subst (cons grp newvalue) (assoc grp entlist) entlist)
      (append entlist (list (cons grp newvalue)))
    )
  )
)

(defun update_dlg (el / t1)
  (set_tile "current_layer" (cdr (assoc '8 el)))
  (set_tile "handle" (if (setq t1 (cdr (assoc '5 el))) t1 ""))
  (set_tile "thickness" (rtos (if (setq t1 (cdr (assoc '39 el))) t1 0.0)))
  (set_tile "current_linetype"
    (if (setq t1 (cdr (assoc '6 el))) t1 "BYLAYER")
  )
  (update_color el)
  (set_tile "default" (if (setq t1 (cdr (assoc '1 el))) t1 ""))
  (set_tile "prompt" (if (setq t1 (cdr (assoc '3 el))) t1 ""))
  (set_tile "tag" (if (setq t1 (cdr (assoc '2 el))) t1 ""))
  (setq t1 (if (setq t1 (cdr (assoc '70 el))) t1 '0))  
  (set_tile "invisible" (itoa (logand 1 t1)))                  
  (set_tile "constant" (itoa (logand 1 (lsh t1 -1))))
  (set_tile "verify" (itoa (logand 1 (lsh t1 -2))))
  (set_tile "preset" (itoa (logand 1 (lsh t1 -3))))
  (setq t1 (if (setq t1 (cdr (assoc '71 el))) t1 '0))
  (set_tile "backward" (itoa (logand 1 (lsh t1 -1))))
  (set_tile "upside_down" (itoa (logand 1 (lsh t1 -2))))
  (set_tile "rotation" (angtos (if (setq t1 (cdr (assoc '50 el))) t1 '0)))
  (set_tile "height" (rtos (if (setq t1 (cdr (assoc '40 el))) t1 '0)))
  (set_tile "width" (rtos (if (setq t1 (cdr (assoc '41 el))) t1 '0)))
  (set_tile "oblique" (angtos (if (setq t1 (cdr (assoc '51 el))) t1 '0)))
  (set_tile "style"  
    (itoa         
      (find_idx (if (setq t1 (cdr (assoc '7 el))) t1 "STANDARD") style_list)
    )                  
  )
  (setq t1 (if (setq t1 (cdr (assoc '10 el))) (append t1 '(0)) '(0 0 0)))
  (set_tile "x_ip" (rtos (car t1)))
  (set_tile "y_ip" (rtos (cadr t1)))
  (set_tile "z_ip" (rtos (caddr t1)))
  (setq t1 (if (setq t1 (cdr (assoc '11 el))) (append t1 '(0)) '(0 0 0)))
  (set_tile "x_ap" (rtos (car t1)))
  (set_tile "y_ap" (rtos (cadr t1)))
  (set_tile "z_ap" (rtos (caddr t1)))
  (set_tile "justify"  
    (itoa 
      (- 15 
        (length 
          (member 
            (list (cdr (assoc '72 el)) (cdr (assoc '74 el)))  
            just_def_list
          )
        )    
      )
    )
  )
  (cond 
    ( (= 3 (setq t1 (cdr (assoc '72 el))))  
      (mode_tile "rotation" 1)
      (mode_tile "digitize_angle" 1)
      (mode_tile "height" 1)
      (ap_set '0)
      (ip_set '0)
    )
    ( (= 5 t1)
      (mode_tile "rotation" 1)
      (mode_tile "digitize_angle" 1)
      (mode_tile "height" 0)
      (ap_set '0)
      (ip_set '0)
    )
    ( (if 
        (progn
          (mode_tile "rotation" 0)
          (mode_tile "digitize_angle" 0)
          (mode_tile "height" 0)
          (/= 0 (logior t1 (cdr (assoc '74 el))))  
        )
        T 
        (progn (ap_set '1) (ip_set '0) nil)
      )
      (ip_set '1)
      (ap_set '0)
    )
  )
)

(defun ap_set (sset /)
  (foreach t1 '("x_ap" "y_ap" "z_ap" "pick_ap") (mode_tile t1 sset))
)

(defun ip_set (sset /)
  (foreach t1 '("x_ip" "y_ip" "z_ip" "pick_ip") (mode_tile t1 sset))
)

(defun find_idx (name lst / cnt)
  (setq cnt (1- (length lst)))
  (while 
    (and (>= cnt 0) (/= name (cdr (assoc '2 (nth cnt lst)))))  
    (setq cnt (1- cnt))
  )
  (if (< cnt 0) nil cnt)
)

(defun color_name (color / t1)
  (if (= 256 color)
    "BYLAYER"
    (if
      (setq t1 (nth color '("BYBLOCK"  "RED"      "YELLOW"   "GREEN"    "CYAN"    
                            "BLUE"     "MAGENTA"  "WHITE"    "BLACK"
                           )
               )
      )
      t1
      ""
    )
  )
)

(defun update_color (el)
  (set_tile "current_color"
    (if (setq t1 (cdr (assoc '62 el))) (color_name t1) "BYLAYER")
  )                    
  (start_image "color_image")
  (fill_image 
    0 0 
    (dimx_tile "color_image") (dimy_tile "color_image")
    (if 
      (and t1 (/= t1 256))  
      t1 
      (abs (cdr (assoc '62 (tblsearch "LAYER" (cdr (assoc '8 el))))))
    )
  )
  (end_image)
)

(defun compare_name (x y) (> (cdr (assoc '2 x)) (cdr (assoc '2 y))))

(defun sort_search (/ track)
  (mapcar '(lambda (x) (if (and x (sfunc x track)) (setq track x))) lst)
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
  (if (setq help_path (fpath "EDATTRIB.HLP"))
    (dt_helpdlg help_path "")
    (alert "Cannot locate help file 'EDATTRIB.HLP'!")
  )
  (mode_tile (if errflag errflag last_focus) 2)
)

(defun parse_path (name / ct)
  (setq ct (strlen name))
  (while (and (> ct 0) (/= "\\" (substr name ct 1))) (setq ct (1- ct)))
  (if (> ct 0) (setq name (substr name ct)) name)
)

(defun valid_name (name)
  (not (wcmatch name "*[] `#`@`.`?`*`~`[`,`'!%^&()+={}|`\\:;\"<>/]*"))
)

(defun check_ltype (/ t1)
  (if 
    (and 
      (/= "" (setq t1 (strcase (get_tile "ltype"))))  
      (member t1 ltype_names)
    )
    (progn
      (setq cltype t1)
      (done_dialog 1)
    )
    (progn
      (set_tile "error"  
        (if (= t1 "")
          "Press <Cancel> or specify a linetype."
          "Select an existing layer name."
        )
      )
      (mode_tile "ltype" 2)
    )
  )
)

(defun get_ltype (eltype / ltype_list ltype_names cltype t1)
  (if errflag
    (mode_tile errflag 2)
    (if (new_dialog "LTYPE" dcl_id)
      (progn
        (setq ltype_list 
          (reverse (sort_list (get_table "LTYPE") compare_name))
        )
        (start_list "existing")
        (mapcar 'add_list 
          (setq ltype_names
            (append
              '("BYLAYER" "BYBLOCK")
              (mapcar '(lambda (x) (cdr (assoc '2 x))) ltype_list)
            )
          )
        )
        (end_list)
        (set_tile "existing"  
          (itoa (- (length ltype_names) (length (member eltype ltype_names))))
        )
        (set_tile "ltype" eltype)
        (action_tile "accept" "(check_ltype)")
        (action_tile "cancel" "(done_dialog 0)")
        (action_tile "existing"  
          (strcat
            "(and"
              " (set_tile \"ltype\" (nth (atoi $value) ltype_names))"
              " (= 4 $reason)"
              " (check_ltype)"
            ")"
          )
        )
        (set_tile "ltype" (if (setq t1 (cdr (assoc '6 entlist))) t1 "BYLAYER"))
        (if (= 1 (start_dialog))  
          (progn 
            (ent_edit cltype '6)
            (set_tile "current_linetype" cltype)
          )
        )
      )
      (set_tile "error" "Child Dialog Box 'LTYPE' Cannot Initialize")
    )
  )
)

(defun check_layer (/ t1)
  (if (and (/= "" (setq t1 (strcase (get_tile "layer")))) (valid_name t1))
    (progn
      (setq clayer t1)
      (done_dialog 1)
    )
    (progn
      (set_tile "error"  
        (if (= t1 "")
          "Press <Cancel> or specify a layer name."
          "Layer name contains invalid characters."
        )
      )
      (mode_tile "layer" 2)
    )
  )
)

(defun get_layer (elayer / layer_list layer_names clayer t1)
  (if errflag
    (mode_tile errflag 2)
    (if (new_dialog "LAYER" dcl_id)
      (progn
        (setq layer_list 
          (reverse (sort_list (get_table "LAYER") compare_name))
        )
        (start_list "existing")
        (mapcar 'add_list 
          (setq layer_names
            (mapcar '(lambda (x) (cdr (assoc '2 x))) layer_list)
          )
        )
        (end_list)
        (set_tile "existing"  
          (itoa (- (length layer_names) (length (member elayer layer_names))))
        )
        (set_tile "layer" elayer)
        (action_tile "accept" "(check_layer)")
        (action_tile "cancel" "(done_dialog 0)")
        (action_tile "existing"  
          (strcat
            "(and"
              " (set_tile \"layer\" (nth (atoi $value) layer_names))"
              " (= 4 $reason)"
              " (check_layer)"
            ")"
          )
        )
        (set_tile "layer" (cdr (assoc '8 entlist)))
        (if (= 1 (start_dialog))  
          (progn 
            (ent_edit clayer '8)
            (set_tile "current_layer" clayer)
            (update_color entlist)
          )
        )
      )
      (set_tile "error" "Child Dialog Box 'LAYER' Cannot Initialize")
    )
  )
)

(defun get_color (/ t1)
  (if errflag
    (mode_tile errflag 2)
    (progn
      (ent_edit
        (acad_colordlg (if (setq t1 (cdr (assoc '62 entlist))) t1 '256))
        '62
      )
      (update_color entlist)
    )
  )
)

(defun dismiss_dialog (retcode)
  (if 
    (and
     errflag 
      (not (and (= retcode 3) (wcmatch errflag "?_ip")))
      (not (and (= retcode 4) (wcmatch errflag "?_ap")))
      (not (and (= retcode 5) (= errflag "rotation")))
    )
    (mode_tile errflag 2)  
    (progn
      (if errflag 
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
      (setq dcl_id (if (setq t1 (fpath "EDATTRIB.DCL")) (load_dialog t1)))
      (entlast)
      (= "ATTDEF"  
        (cdr 
          (assoc 
            '0 
            (entget 
              (if entity 
                entity
                (setq entity
                  (if
                    (setq t1 
                      (entsel 
                        "\n \nSelect an Attribute Definition to Edit:  "
                      )
                    )
                    (car t1)
                    (entlast)
                  )
                )
              )
            )
          )  
        )
      )
    )  
    (progn
      (setq oldvar
        (list
          (getvar "CMDECHO")
          (getvar "ATTMODE")
          (getvar "ATTREQ")
          (getvar "REGENMODE")
          (getvar "EXPERT")
        )
      )
      (setq olderr   *error*
            restore  edattribx
            *error*  errexit
      )
      (setvar "CMDECHO" 0)
      (setvar "REGENMODE" 1)
      (setvar "EXPERT" 0)
      (setvar "ATTDIA" 1)
      (setvar "ATTMODE" 0)
      (setvar "ATTREQ" 0)
      (terpri)
      (command "_UCS" "_W")
      (setq dlg_retcode   6
            old_entlist   (entget entity)
            last_focus    "default"
            just_def_list (list '(0 0)
                                '(1 0)  
                                '(2 0)
                                '(3 0)
                                '(4 0)
                                '(5 0)
                                '(0 2)
                                '(1 2)
                                '(2 2)
                                '(0 3)
                                '(1 3)
                                '(2 3)
                                '(0 1)
                                '(1 1)
                                '(2 1)
                          )
      )          
      (while (and (> dlg_retcode 1) (new_dialog "ATTEDIT" dcl_id))
        (start_list "justify")
        (foreach t1
          (list
            "Left"           "Center"          "Right"
            "Aligned"        "Middle"          "Fit"
            "Middle Left"    "Middle Center"   "Middle Right"
            "Top Left"       "Top Center"      "Top Right"
            "Bottom Left"    "Bottom Center"   "Bottom Right"
          )
          (add_list t1)
        )
        (end_list)
        (start_list "style")
        (foreach t1 
          (setq style_list 
            (reverse (sort_list (get_table "STYLE") compare_name))
          )
          (add_list (cdr (assoc '2 t1)))
        )
        (end_list)
        (update_dlg (setq entlist (entget entity)))
        (action_tile "help" "(get_help)")
        (action_tile "preview" "(dismiss_dialog 2)")
        (action_tile "pick_ip" "(dismiss_dialog 3)")
        (action_tile "pick_ap" "(dismiss_dialog 4)")
        (action_tile "digitize_angle" "(dismiss_dialog 5)")
        (action_tile "color" "(get_color)")
        (action_tile "layer" "(get_layer (get_tile \"current_layer\"))")
        (action_tile "linetype"  
          "(get_ltype (get_tile \"current_linetype\"))"
        )
        (action_tile "accept" "(done_dialog 1)")
        (action_tile "cancel" "(done_dialog 0)")
        (foreach t1
          '("invisible"      "constant"      "verify"          "preset"
            "thickness"      "tag"           "prompt"          "default"
            "justify"        "style"         "height"          "width"
            "oblique"        "upside_down"   "backward"        "rotation"
            "update_style"   "x_ip"          "y_ip"            "z_ip"          
            "x_ap"           "y_ap"          "z_ap"
          )
          (action_tile t1 "(dlg_act $key $reason $value)")
        )
        (if last_focus (mode_tile last_focus 2))
        (setq dlg_retcode (start_dialog))
        (cond
          ( (= 0 dlg_retcode) (entmod old_entlist))
          (
            (= 2 dlg_retcode)
            (entmod entlist)      
            (prompt "\nPress any key to continue\n")
            (grread)
            (grread 1)
            (redraw)
            (princ "\nReturning to Dialog Box\n \n ")
          )
          (
            (= 3 dlg_retcode)
            (entmod entlist)
            (prompt "\nPick the Attribute Insertion Point:  ")
            (setq t1 (cdr (assoc '10 entlist)))
            (command 
              cancel
              cancel
              "_MOVE"
              entity
              ""
              t1
              pause
            )
            (princ "\nInsertion Point Selected\n \n ")
          )
          (
            (= 4 dlg_retcode)
            (entmod entlist)
            (setq t1 (cdr (assoc '11 entlist)))
            (if 
              (or 
                (= 3 (setq t2 (cdr (assoc '72 entlist))))  
                (= 5 t2)
                (and (= 0 t2) (= 0 (cdr (assoc '74 entlist))))
              )
              (setq entlist
                (entmod 
                  (subst 
                    (cons '11 
                      (getpoint 
                        "\nPick the Alignment Point:  "
                        (cdr (assoc '11 entlist))
                      )
                    )
                    (assoc '11 entlist)
                    entlist
                  )
                )
              )      
              (command 
                cancel
                cancel
                "_MOVE"
                entity
                ""
                t1         
                pause
              )
            )      
            (princ "\nAlignment Point Selected\n \n ")
          )
          (
            (= 5 dlg_retcode)
            (entmod entlist)
            (setq t2 (if (setq t2 (cdr (assoc '50 entlist))) t2 '0))
            (prompt "\nPick the Attribute Rotation Angle:  ")
            (setq t1 
              (cdr 
                (assoc 
                  (if 
                    (or 
                      (= 3 (setq t1 (cdr (assoc '72 entlist))))
                      (= 5 t1)
                      (and (= 0 t1)  (= 0 (cdr (assoc '74 entlist))))
                    )
                    '10 
                    '11
                  )
                  entlist
                )
              )
            )
            (command 
              cancel
              cancel
              "_ROTATE"
              entity
              ""
              t1
              "_R"
              (rtd t2)
              pause
            )
            (princ "\nRotation Angle Selected\n \n ")
          )
          (T (entmod entlist))
        )
      )
      (unload_dialog dcl_id)
      (restore)
    )
    (alert 
      (cond 
        ( (not dcl_id)
          (strcat 
            "Dialog Box Definition File 'EDATTRIB.DCL' not Found"
            "\n                Cannot Continue!"
          )
        )
        ((entlast) "Selected Entity Must Be An Attribute Definition!")
        (T "There are no entities to edit!")
      )
    )
  )
  dlg_retcode
)



