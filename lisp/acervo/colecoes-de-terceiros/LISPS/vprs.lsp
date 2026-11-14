;******************************************************************************
; VPRS.LSP - Written by Scott McFarlane
;
; SHEET.EXP AutoLisp interface demo program.  Requires SHEET.EXP to be loaded.
;
; Provides ability to restore a named sheet in selected paperspace viewports.
;
(defun C:VPRS( / old_error old_cmdecho is_visible sheet_name ss fstr lyr_list)

	; Save *error* function and CMDECHO
	(setq
		old_error *error*
		old_cmdecho (getvar "cmdecho")
	);setq

	; Define new error function
	(defun *error*(st)
		(princ "Error: ") (princ st)
		(setq *error* old_error)
		(setvar "cmdecho" old_cmdecho)
		(princ)
	);defun

	; Make sure SHEET.EXP is loaded
	(if (null sheet_version)
		(xload "sheet" "\nCould not load SHEET.EXP!")
	);endif

	(defun is_visible(x)
		(and
			(not (is_flag_bit_set 1 (cdr (assoc 70 x))))
			(> (cdr (assoc 62 x)) 0)
		);and
	);defun

	; Allow user to select library
	(get_library)

	; Select sheet name
	(setq sheet_name (get_sheet))

	; Initialize layer name string for VPLAYER Freeze
	(setq fstr "")

	(if (setq lyr_list (sheet_layers sheet_name)) (progn

		; Get a selection set of viewports from user
		(setq ss (ssget '((0 . "VIEWPORT"))))

		(if ss (progn

			; Turn off CMDECHO
			(setvar "cmdecho" 0)

			; Make sure all layers are initially thawed in selected viewports
			(command "vplayer" "thaw" "*" "s" ss "")

			; Freeze each layer in selected viewports if not visible in sheet
			(foreach lyr lyr_list

				(if (not (is_visible lyr)) (progn

					; Build a comma-delimited list of layers to freeze
					(setq fstr (strcat fstr "," (cdr (assoc 2 lyr))))

					; When the list becomes longer than 200 chars, send to command
					(if (> (strlen fstr) 200) (progn
						(command "freeze" (substr fstr 2) "s" ss "")
						(setq fstr "")
					));if

				));endif

			);foreach

			; Send any left-over layers to VPLAYER command
			(if (> (strlen fstr) 0)
				(command "freeze" (substr fstr 2) "s" ss "")
			);endif

			; Exit VPLAYER command
			(command "")

			; Restore CMDECHO function
			(setvar "cmdecho" old_cmdecho)

		));endif

	));endif

	(princ)

);defun

;*********************************************
; get_library

; This function prompts the user for one of the available
; sheet libraries

(defun get_library( / libs new_lib)

	; Get current sheet configuration
	(setq cfg (sheet_config))

	(if (or (system_lib_available) (drawing_lib_available)) (progn

		(setq libs
			(list
				'("Drawing"   . 0)
				'("Directory" . 1)
				'("System"    . 2)
			);list
		);setq

		(initget "Drawing Directory System")
		(setq new_lib
			(getkword
				(strcat
					"\nSelect library\n"
					(if (drawing_lib_available) "DRawing/" "")
					"DIrectory"
					(if (system_lib_available) "/System <" " <")
					(car (nth (cdr (assoc 60 cfg)) libs))
					">: "
				);strcat
			);getkword
		);setq

		(if new_lib
			(sheet_config (list (cons 60 (cdr (assoc new_lib libs)))))
		);endif

	));endif

);defun

;*********************************************
; get_sheet

; Prompt user for sheet name (allow "?" for list)

(defun get_sheet( / sht_name)
	(setq sht_name "?")
	(while (= sht_name "?")
		(setq sht_name (getstring "\nSheet name (or ?): "))
		(if (= sht_name "?")
			(progn
				(textpage)
				(princ   "Defined Sheets:\n")
				(princ "\nName      Description")
				(princ "\n---------------------------------------------------------")
				(foreach sht (sheet_namelist)
					(princ
						(strcat "\n"
							(pad (cdr (assoc 1 sht)) 10)
							     (cdr (assoc 2 sht))
						);strcat
					);princ
				);foreach
				(princ "\n")
			);progn
		);endif
	);while
	sht_name
);defun

; Return TRUE if a system library has been configured
(defun system_lib_available()
	(/= (cdr (assoc 1 (sheet_config))) "")
);defun

; Return TRUE if a drawing library has not been disabled
(defun drawing_lib_available()
	(= (cdr (assoc 71 (sheet_config))) 0)
);defun

;**************************************************************************
; Other utility functions

; Pad a string with spaces
(defun pad(string width)
	(substr (strcat string
		"                                                                      ")
		1 width
	);substr
);defun

; Check for setting of a bit code in an integer flag
(defun is_flag_bit_set(bit_value flag)
	(= (logand flag bit_value) bit_value)
);defun

(princ)

