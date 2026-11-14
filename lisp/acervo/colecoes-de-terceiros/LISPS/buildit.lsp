(defun c:buildit ()
   (setq String "Door WIndow WAll Stair Quit")
   (initget 1 String)
   (setq Temp (getkword (strcat "Enter Function ["String "] :")))
   (if (/= Temp "Quit") (load temp)))
