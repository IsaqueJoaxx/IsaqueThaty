;Drawing compression program (carefull !! make sure you want to.)
;
;
(DEFUN C:COMPRESS()
(setq oer  *error*  *error*  err2)
(PRINC "\nPlease standby..........Compressing drawing file.")
(PRINC "\nAfter compression, drawing will automatically be recalled.")
(COMMAND "LAYER" "T" "*" "ON" "*" "")
(command "zoom""all")
(SETQ WP1(GETVAR "VSMIN"))
(SETQ WP2(GETVAR "VSMAX"))
(SETQ DWG(GETVAR "DWGNAME"))
(COMMAND "WBLOCK" DWG "Y" "" "0,0" "C" WP1 WP2 "" "script" "COMPRESS"))
