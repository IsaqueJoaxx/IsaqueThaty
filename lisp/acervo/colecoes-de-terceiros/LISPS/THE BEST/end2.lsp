Código: 

; no comando INSERT o "bloco" neste exempo é uma linha 
;            com nome de bloco dai é só modificar..... 
(defun c:endp() ; 
      (setq ssel (ssget)) ;;;;;;; seleciona os Plines ou lines 
      (setq qt (sslength ssel) num 0 nx 0 ) 
      (setq nxl 0 nl qtl volt 1000) 
(repeat qt 
    (setq snm   (ssname ssel num) tipo nil) 
    (setq xent  (entget snm)) 
    (setq p1 (cdr (assoc 10 xent))) 
    (setq p2 (cdr (assoc 11 xent))) 
    (setq tipo (cdr (assoc 0 xent))) 
  (if (= tipo "LINE") 
           (progn (command "insert" "bloco" p1 "" "" "" ) 
                  (command "insert" "bloco" p2 "" "" "" ) 
  )) 

  (if (= tipo "LWPOLYLINE") 
     (foreach ent xent (if (= (car ent) 10) 
              (progn 
                (setq p1 (cdr ent)) 
                (command "insert" "bloco" p1 "" "" "") ) 
              ) 
      ) 
  ); fim do if tipo 
              (setq num (+ num 1)) 
); fim do repeat 
);fim defun 

 
