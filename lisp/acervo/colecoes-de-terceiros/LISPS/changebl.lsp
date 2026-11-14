; program to change a block's definition
; program prompts for a selection set and then steps through
; changing all the selected blocks to the one entered from the
; keyboard
; the new block takes on all the properties of the old block
; including the rotation and scale
;
; written by Steffen Toxopeus
;

(defun c:changebl ( / blkent blknew blkent inspt counter)
  (prompt "\nSelect the BLOCKS you want to change: ")
  (setq blkset (ssget))
  (if blkset    ;if anything was selected
    (setq blknew (getstring "\nWhat is the name of the new BLOCK?"))
  );if
  (if (and (not (= "" blknew)) blkset)
    (progn
      ; get the insertion point of the first block in the selection set
      (setq inspt (cdr (assoc 10 (entget (ssname blkset 0)))))
      ; insert the new block at that insertion point and erase it
      ; this avoids changing the extents arbitrarially
      (command "insert" blknew inspt "" "" ""
               "erase" "L" ""
      )
      ; now we can step through the blocks and change their assoc
      ; lists with the new block name since the new block is now
      ; defined in the database
      (setq counter 1)
      (while (<= counter (sslength blkset))
        ; get the next block
        (setq blkent (entget (ssname blkset (1- counter)))
          ; change the name of the block in the assoc list
          ; (assoc 2)
          blkent (subst (cons 2 blknew) (assoc 2 blkent) blkent)
          ; while we're about it, clock the counter
          counter (1+ counter)
        );setq
        ; modify the block in the database (and on screen)
        (entmod blkent)
      );while
    );progn
  );if
(princ)
);defun
