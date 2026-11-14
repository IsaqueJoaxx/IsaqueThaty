
;  ED ver 2.0 (c) 1987 by Kenneth E. Morgan
;                         145-3 Roxane Ct.
;                         Walnut Creek, Ca. 94596
;                         Tele 415-938-2806 
;                         ID #72770,2673
   
;  EDit text - Edits text in the acad drawing editor,
;              on the GRAPHIC screen command prompt line.

;  Special keys:
;   ctrl-curser left   moves curser left (relative to text)
;   ctrl-curser right  moves curser right (relative to text)
;   del                deletes character directly under curser

;      I'm sure that you will find ED to be the easiest and fastest way to 
;   edit text in the acad drawing editor. It's possibly the only program of 
;   it's kind that works right on the graphics screen. To use ED, first add
;   it to your "acad.lsp" file or load it at the acad command prompt by 
;   typing: 
;                              (load "ed")
;   At the command prompt, type "ED" and select the text entities as you
;   normally would, (you can make multiple selections using "window" etc.). 
;   Then with numlock off, press the control key and cuser right or left 
;   simultaniously to move the curser one character at a time. You can insert
;   or delete text as you wish, and when you are done, press enter. If you
;   make a mistake and do not want to change the text press control-C to
;   cancel and the entity will not be changed on the drawing. That's all
;   there is to it.
;      If you like ED and find it to be a valuable time and money saver,
;   please send $25 in check or money order to the above address. In addition
;   to becoming a registered user, you will also receive ED ver 3.0 featuring:
;         * Full curser control; allowing movement of curser one character, 
;           one word, or whole strings with a single keystroke.
;         * Text block functions, allowing the creation of 3 different 
;           types of text blocks for insertion in other text strings
;           at any time in the drawing session.
;         * Deletion of all or portions of text with single key stroke.
;         * More
;      You will also be put on the mailing list for notification of updates
;   as further enhancements are added. ED ver 3.0 is an existing package, has
;   been fully tested, and is ready to ship. It's not "vaporware". 
;       You may copy and distribute this program to others, only
;   if it is copied in its' entirety and given free of charge.  
;       The contents of this program, whole or in part, cannot be used 
;   for commercial purposes without the written consent of the author.

;   One further note:            
;       ED has be tested extensively with the EGA and similar drivers. 
;   Some drivers may use a curser on the graphic screen command line. 
;   If you have one of these, simply use the command "EDT" instead of "ED" 
;   this will switch to the text screen for editing and back again when you 
;   press enter. If you have any problems please call or write and let me 
;   know. Thank you, and enjoy using ED.

(defun gettxt (strng)
   (setq a (ssget) b (sslength a) c 0)						 
   (while (< c b) (setq d (ssname a c) e (entget d))
      (if (= "TEXT" (cdr (assoc 0 e)))(strng)) (setq c (1+ c))))
(defun strng (/ i ib s sb sc sd)
    (if f (progn    
            (prompt "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n")
            (textscr)) (redraw d 3))    
    (setq i 1 sb "                                   " 
          sc nil s (cdr (assoc 1 e)) ib nil)
    (prompt "\nKeys:   ctrl-crsr lft   ctrl-crsr rgt   del   enter   ctrl-c")   
    (prompt "\n                                    _\n")
    (while (/= sc 13)
       (prompt (strcat "\r" (setq sd (substr (strcat sb "<" s ">" sb) i 72))))
       (setq sc (cadr (grread)))
       (cond ((listp sc)(setq sc nil))((= sc 244)(sc244))((= sc 243)(sc243))
             ((= sc 211)(sc211))((and (> sc 31)(< sc 127))(sc31+))))
    (setq e (subst (cons 1 s) (assoc 1 e) e))(entmod e))
(defun sc244 ()
   (if (>= (strlen s) i) (setq i (1+ i))))
(defun sc243 ()
   (if (and (> i 1)(>= (strlen s) (1- i)))(setq i (1- i))))
(defun sc211 ()
   (setq s (strcat (substr s 1 (1- i))(substr s (1+ i)))))
(defun sc31+ ()
   (setq s (strcat (substr s 1 (1- i))(chr sc)(substr s i)) i (1+ i)))
(defun C:EDT (/ a b c d e f)
   (setq f t)(gettxt strng)(print))
(defun C:ED (/ a b c d e f)
   (setq f nil)(gettxt strng)(print))
