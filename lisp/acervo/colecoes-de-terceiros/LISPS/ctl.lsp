;move objects to a new layer
;
;
(defun c:ctl()
(princ "\nPick an object on the layer to Copy.")
(setq ss (ssget))
(setq ul1(getstring "\nLayer to copy entities to : "))
(prompt "Copying to layer.....")(princ ul1)
(command "copy" ss "" "0,0" "0,0")
(command "CHANGE" ss "" "P" "LA" ul1 "")
(princ))
(princ "\nCopies selected entities to a new layer.")
(princ "\nType CTL to execute.")