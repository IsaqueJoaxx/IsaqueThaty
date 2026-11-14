;;; ============================================================
;;; StreetView.lsp  -  Abre o Google Street View no ponto picado
;;; Para AutoCAD Civil 3D  (coordenadas em UTM - SIRGAS 2000 / WGS84)
;;;
;;;  Comandos:
;;;    SV      -> pica um ponto no desenho e abre o Street View
;;;    SVMAPA  -> pica um ponto e abre o Google Maps comum
;;;    SVZONA  -> redefine a zona UTM e o hemisferio
;;;
;;;  Na primeira vez ele pergunta a zona UTM e o hemisferio,
;;;  e guarda para a sessao atual.  Brasil = hemisferio Sul,
;;;  zonas comuns: 22, 23, 24 (centro-sul/sudeste).
;;; ============================================================

(vl-load-com)

;; --- Converte E,N (UTM, metros) -> (lat lon) em graus  (elipsoide WGS84/GRS80)
(defun sv:utm->latlon (E N zone south / a f e2 k0 x y M mu e1 phi1
                         ep2 C1 T1 N1 R1 D lat lon lon0 dd)
  (setq a    6378137.0
        f    (/ 1.0 298.257223563)
        k0   0.9996
        e2   (* f (- 2.0 f))
        x    (- E 500000.0)
        y    (if south (- N 10000000.0) N)
        lon0 (* (/ pi 180.0) (- (* zone 6.0) 183.0))
        M    (/ y k0)
        mu   (/ M (* a (- 1.0 (/ e2 4.0) (/ (* 3.0 e2 e2) 64.0)
                        (/ (* 5.0 e2 e2 e2) 256.0))))
        e1   (/ (- 1.0 (sqrt (- 1.0 e2))) (+ 1.0 (sqrt (- 1.0 e2))))
        phi1 (+ mu
                (* (- (/ (* 3.0 e1) 2.0) (/ (* 27.0 e1 e1 e1) 32.0)) (sin (* 2.0 mu)))
                (* (- (/ (* 21.0 e1 e1) 16.0) (/ (* 55.0 e1 e1 e1 e1) 32.0)) (sin (* 4.0 mu)))
                (* (/ (* 151.0 e1 e1 e1) 96.0) (sin (* 6.0 mu)))
                (* (/ (* 1097.0 e1 e1 e1 e1) 512.0) (sin (* 8.0 mu))))
        ep2  (/ e2 (- 1.0 e2))
        C1   (* ep2 (cos phi1) (cos phi1))
        T1   (* (/ (sin phi1) (cos phi1)) (/ (sin phi1) (cos phi1)))
        N1   (/ a (sqrt (- 1.0 (* e2 (sin phi1) (sin phi1)))))
        R1   (/ (* a (- 1.0 e2)) (expt (- 1.0 (* e2 (sin phi1) (sin phi1))) 1.5))
        D    (/ x (* N1 k0))
        lat  (- phi1
                (* (/ (* N1 (/ (sin phi1) (cos phi1))) R1)
                   (- (/ (* D D) 2.0)
                      (* (+ 5.0 (* 3.0 T1) (* 10.0 C1) (* -4.0 C1 C1) (* -9.0 ep2))
                         (/ (expt D 4) 24.0))
                      (* -1.0 (+ 61.0 (* 90.0 T1) (* 298.0 C1) (* 45.0 T1 T1)
                                 (* -252.0 ep2) (* -3.0 C1 C1))
                         (/ (expt D 6) 720.0)))))
        lon  (+ lon0
                (/ (- D
                      (* (+ 1.0 (* 2.0 T1) C1) (/ (expt D 3) 6.0))
                      (* -1.0 (+ 5.0 (* -2.0 C1) (* 28.0 T1) (* -3.0 C1 C1)
                                 (* 8.0 ep2) (* 24.0 T1 T1))
                         (/ (expt D 5) 120.0)))
                   (cos phi1)))
        dd   (/ 180.0 pi))
  (list (* lat dd) (* lon dd)))

;; --- Pergunta/guarda a zona e o hemisferio (so na 1a vez da sessao)
(defun sv:get-zone ( / z h)
  (if (null *sv-zone*)
    (progn
      (initget 6)
      (setq z (getint "\nZona UTM (ex.: 22, 23, 24): "))
      (if z (setq *sv-zone* z))
      (initget "Sul Norte")
      (setq h (cond ((getkword "\nHemisferio [Sul/Norte] <Sul>: ")) ("Sul")))
      (setq *sv-hemi-sul* (= h "Sul"))
    )
  )
  *sv-zone*
)

;; --- Monta a coordenada lat,lon a partir de um ponto picado
(defun sv:pega-latlon ( / pt ll)
  (if (sv:get-zone)
    (if (setq pt (getpoint "\nPique o ponto: "))
      (progn
        (setq ll (sv:utm->latlon (car pt) (cadr pt) *sv-zone* *sv-hemi-sul*))
        (princ (strcat "\nLat/Long: " (rtos (car ll) 2 8) ", " (rtos (cadr ll) 2 8)))
        ll
      )
    )
  )
)

;; --- Abre direto no Street View
(defun c:SV ( / ll)
  (if (setq ll (sv:pega-latlon))
    (startapp "explorer.exe"
      (strcat "https://www.google.com/maps/@?api=1&map_action=pano&viewpoint="
              (rtos (car ll) 2 8) "," (rtos (cadr ll) 2 8)))
  )
  (princ)
)

;; --- Abre no Google Maps comum (vista de mapa/satelite, arraste o boneco p/ Street View)
(defun c:SVMAPA ( / ll)
  (if (setq ll (sv:pega-latlon))
    (startapp "explorer.exe"
      (strcat "https://www.google.com/maps/search/?api=1&query="
              (rtos (car ll) 2 8) "," (rtos (cadr ll) 2 8)))
  )
  (princ)
)

;; --- Redefinir zona/hemisferio
(defun c:SVZONA ( ) (setq *sv-zone* nil) (sv:get-zone) (princ))

(princ "\nStreetView.lsp carregado.  Comandos: SV  |  SVMAPA  |  SVZONA")
(princ)
