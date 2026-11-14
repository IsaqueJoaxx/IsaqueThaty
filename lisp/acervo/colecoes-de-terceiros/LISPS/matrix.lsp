;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; CADENCE 6/93   ADVANCED AUTOLISP CONCEPTS
;; Bill Kramer
;;
;; Matrix Math with AutoLISP
;; Listing 1: Multiplication
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; M_COL_MULT column matrix multiplicator.  This function takes a
;; MxN matrix and multiplies a Nx1 matrix or column matrix.  The
;; typical use of this function is to transform point coordinates
;; through a transformation matrix.
;; Result is a Nx1 matrix or nil if the matrices do not match in size.
;;
(defun M_COL_MULT (A B / U)
  (if (= (length B) (length (car A))) ;;check row size of A against B
   (mapcar ;; returns list with length equal to number of rows in A
    '(lambda (U) ;; U is each row in A as supplied by mapcar
       (apply '+ ;; sum the result of...
         (mapcar '* U B) ;;multiply row from A [in U] by B
       )
    )
    A) ;;parameter for first MAPCAR
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; M_MN_MULT  Multiply MxN matrix by NxM matrix returning a NxN
;; square matrix.  N must be greater than 1.  This function is
;; used to merge transformation matrices into a single one.
;;
;; result is NxN matrix multiplication or nil if matrices are not
;;        the right size to multiply.
;;
(defun M_MN_MULT (A B / U V)
  (if (= (length B) (length (car A))) ;;#rows in B = #cols in A?
     (progn
       (setq B (M_REV B))
       (mapcar '(lambda (U)
          (mapcar '(lambda (V)
            (apply '+
              (mapcar '* U V))) B)) A)
     )
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; Listing 2: Reverse and Add/subtract
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; M_REV  reverses MxN storage order of matrix.  Normal default is to
;; store matrices in row order.  This routine takes a row order matrix
;; and returns one with column order, or visa versa.
;;
(defun M_REV (A / N U V)
  (setq N 0)
  (repeat (length A)
     (setq U (cons (mapcar '(lambda (V) (nth N V)) A) U)
           N (1+ N))
  )
  (reverse U)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; M_ADD  Addition/subtraction of same sized matrices
;; op is either '+ or '-
;; result is matrix result of addition
;;        or nil if matrices A and B do not match in size.
(defun M_ADD (A B OP)
 (if (and (member OP (list '+ '-)) ;;check valid operation
          (= (length A) (length B))) ;;check #rows for match
    (if (and
          (listp (car A)) ;;check for N>1 condition
          (listp (car B)) ;;both must be N>1
          (= (length (car A)) (length (car B)))) ;;match 1st column count
      (mapcar '(lambda (U V)
         (mapcar OP U V)) A B) ;;nested MxN with N>1
      (if (and (numberp (car A)) (numberp (car B))) ;;else, N=1?
         (mapcar OP A B) ;;single columns[N=1]
      )
    )
 )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; Listing 3:  Block Extrema Example for Matrix math
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;
(defun MEXAMPLE (EL / ESAVE BP SX SY SZ RT EN X1 X2)
  (setq ESAVE (cdr (assoc -1 EL))
        TRMX (M_MX_BLK (cdr (assoc 10 EL))
                       (cdr (assoc 41 EL))
                       (cdr (assoc 42 EL))
                       (cdr (assoc 43 EL))
                       (cdr (assoc 50 EL))
             )
        EL (tblsearch "BLOCK" (cdr (assoc 2 EL)))
        EN (cdr (assoc -2 EL))
        X1 (list 99999.9999 99999.9999 99999.9999)
        X2 (list -99999.9999 -99999.9999 -99999.9999)
  )
  ;; reads through block looking at group 10,11,12,13 points
  ;; to find the max and min block def points.
  (while EN
    (setq EL (entget EN)
          P1 10)
    (while (assoc P1 EL)
      (setq BP (cdr (assoc P1 EL))
            BP (list (car BP) (cadr BP) (caddr BP) 1.0)
            NP (m_col_mult TRMX BP)
            NP (list (car NP) (cadr NP) (caddr NP))
            EL (subst (cons P1 NP) (assoc P1 EL) EL)
            P1 (1+ P1)
            X1 (list
                 (min (car X1) (car NP))
                 (min (cadr X1) (cadr NP))
                 (min (caddr X1) (caddr NP))
               )
            X2 (list
                 (max (car X2) (car NP))
                 (max (cadr X2) (cadr NP))
                 (max (caddr X2) (caddr NP))
               )
      )
    )
    (setq EN (entnext EN))
  )
  (list X1 X2)
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;; M_MX_BLK Creates matrix to multiple local point list by to obtain
;; a world coordinate point. Used when transforming coordinates inside
;; a block definition or similar structure.
;;
;; MATRIX is always 4x4
;;   IP  base point of block insertion [translation point]
;;   SX..SZ  scaling factors along the X,Y,Z axes.
;;   RT  rotation about Z axis in radians.
;;
(defun M_MX_BLK (IP SX SY SZ RT)
  (list
    (list (* SX (cos RT)) (* SY -1.0 (sin RT)) 0.0 (car IP))
    (list (* SX (sin RT)) (* SY (cos RT))      0.0 (cadr IP))
    (list 0.0             0.0                  SZ  (caddr IP))
    (list 0.0             0.0                  0.0 1.0)
  )
)
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
