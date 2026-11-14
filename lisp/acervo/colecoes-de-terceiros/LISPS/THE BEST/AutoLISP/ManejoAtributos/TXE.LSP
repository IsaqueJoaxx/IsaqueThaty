;|         ษออออออออออออออออออออออออออออออออออออออออออออป
           บ                  TXE2.LSP                  บ
           บ   By Michael K. Weaver  Fri 12-31-1993     บ
           บ         Compuserve Id. 71461,1775          บ
           บ   2175 George Road, Fairbanks, AK. 99705   บ
           บ           (907)488-3577 (voice)            บ
           บ      Email:71461.1775@compuserve.com       บ
           บ      (c)1994, 1996 Michael K. Weaver       บ
           ศออออออออออออออออออออออออออออออออออออออออออออผ

Modified Mon 09-23-1996 for release 13 mtext entities.
        The safe_ddedit routine was copied directly from
	ddmodify.lsp by Autodesk.

This routine will edit the following text type entities:
	Text
	Mtext
	Attributes
	Attribute definitions
	Dimension Text
	Nested text entities

  It will not successfully edit the following:
	Nested dimensions
	nested mtext
	any entities nested in an external reference

  Note that when editing text nested more than one level deep, (text
  within a block within a block), a regeneration must be performed
  to see the results of the edit.  Note also that this actually
  modifies the block definition and subsequently all insertions of
  the block.

Suggestions and or comments are welcome.

  Thanks to Jim Arthur for the encouragement to do the update for
  release 13 mtext entities.


|;


(defun c:txe(;			edit text type entities
  /;				no formal arguments
  ent;				entity info returned by nentsel
  elist;			entity list
  etype;			entity type
  etype2;			entity type for dialog box label
  oldval;			original text value
  newval;			new text value
  elist2;			new entity list
  get_newval;			local function
  *error*;			error handler
 );				end local variable list
;ฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤฤ Start local functions ฤฤฤฤฤฤฤฤฤฤฤฤฤฤ
 (defun *error*(msg);	error handler
  (princ msg)
  (setq *error* nil)
  (princ)
 );				end *error*

 (defun get_newval(
   oldval;			old value to be changed
   etype;			entity type for dialog box label
   /;				end of formal argument list
  );				end of local variable list
  (setq dcl_id (load_dialog "txe.dcl"))
  (if (not (new_dialog "text_edit" dcl_id)) (exit))
  (set_tile "text" oldval)
  (if etype
   (set_tile "box" etype)
  )
  (action_tile "text" "(setq newval $value)" )
  (action_tile "accept" "(done_dialog 1)" )
  (if (equal (start_dialog) 1)
   nil
   (setq newval nil)
  )
  (unload_dialog dcl_id)
  newval
 );				end get_newval
 ;;This routine copied directly from ddmodify.lsp by Autodesk.
 (defun safe_ddedit (ename / orgMTextEd work)
   (setq orgMTextEd (getvar "MTEXTED"))
   (setq work orgMTextEd)
   (if (= ":" (substr work 1 1))
     (progn
       (while (and (< 0 (strlen work)) (/= "#" (substr work 1 1)))
         (setq work (substr work 2))
       )
       (if (= "#" (substr work 1 1))
         (setq work (substr work 2))
       )
       (setvar "MTEXTED" work)
     )
   )
   (command "_ddedit" ename "")
   (setvar "MTEXTED" orgMTextEd)
 )
 
 
;ออออออออออออออออออออออออออออออ Start Main Function ออออออออออออออออ
 (while (setq ent (nentsel))
  (setq
   ent2 ent
   elist (entget (car ent))
   etype (cdr (assoc 0 elist))
  );				end setq
  (if (member etype '("TEXT" "ATTDEF" "ATTRIB" "MTEXT"))
   (progn
    (setq
     etype2 (cond
      ((= etype "ATTDEF")
       (strcat "Attribute definition default " (cdr (assoc 3 elist)))
      )
      ((= etype "ATTRIB")
       (strcat "Attribute with Tag: " (cdr (assoc 2 elist)))
      )
      ((= etype "MTEXT")
       (if (and
         (<= 4 (length ent))
         (or
          (= "DIMENSION" (cdr (assoc 0 (entget (car (last ent))))))
          (= "INSERT" (cdr (assoc 0 (entget (car (last ent))))))
         )
        );			end and
        (safe_ddedit (car (last ent)))
        (safe_ddedit (car ent))
       );			end if a dimension?
      );			end mtext entity
      ((= etype "TEXT")
       (if (= 4 (length ent))
        (progn
         (setq
          diment (car (last ent))
          dimlist (entget diment)
         );			end setq
         (if (= "DIMENSION" (cdr (assoc 0 dimlist)))
          (princ "Dimension Text")
          (princ "Nested text")
         );			end if dimension?
        );			end progn nested entity
       );			end progn nested entity
      );			end cond text
     );				end cond etype
     oldval (cdr (assoc 1 elist))
     newval (if (/= "MTEXT" etype) (get_newval oldval etype2))
     elist2 (if newval
      (subst (cons 1 newval) (assoc 1 elist) elist)
      nil
     );				end if newval returned
    );				end setq
    (if elist2
     (progn
      (entmod elist2)
      (if (and
        (= 4 (length ent))
        (setq
         diment (car (last ent))
         dimlist (entget diment)
        )
       );			end and
       (progn
        (if (= "DIMENSION" (cdr (assoc 0 dimlist)))
         (progn
          (setq dimlist2 (subst (cons 1 newval) (assoc 1 dimlist) dimlist))
          (entmod dimlist2)
          (entupd diment)
         );			end progn
        );			end if dimension
        (if (= "INSERT" (cdr (assoc 0 dimlist)))
         (entupd diment)
        );			end if insert
       );			end progn ent is nested
       (entupd (cdr (assoc -1 elist)))
      );			end if ent is nested?
     );				end progn elist2 exists
    );				end if elist2 exists?
   );				end progn correct type of entity
   (princ "\nNot a text, mtext, attdef, dimension, or attribute entity. ")
  );				end if
 );				end while
 (princ)
);				end c:txe


(princ "\nC:TXE the TeXt Editor for Release 13,  Michael K. Weaver, 71461.1775@compuserve.com\n")
(princ)
