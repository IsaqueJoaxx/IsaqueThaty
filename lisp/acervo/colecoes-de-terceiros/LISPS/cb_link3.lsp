
;  CB_LINK3
;               For each row of database,
;               inserts block at xy point from database, 
;               and links the block to the database row.
;(C:_ADECONFIG "APPNAME" "ADE")
;(C:_ADECLRQUERY)
;(C:_ADECLOSEDWGS "*")
;(C:_ADECLRFEA)
;(C:_ADETERMSQL)
;(setq linkstring (getstring "\nEnter TableLink for SQL database: "))
;(princ (strcat "\nUser entered for linkstring:" linkstring))
;(setq linkstring (strcat "\"" linkstring "\""))  ; linkstring must have quotes
;(princ (strcat "\nNew value for linkstring:" linkstring))
;(princ (strcat "\nNew value for linkstring:" "RES"))
;(if (c:_adetablink linkstring) (princ "\nSuccess."))
;(setvar result (command "ADETABLINK")) ; "S" linkstring "X" "" ""))
(setq xnum (getint "\nEnter column number of X coordinate for entity: "))
(setq ynum (getint "\nEnter column number of Y coordinate for entity: "))
;(setq blkname (getstring "\nEnter name of block to insert: "))
(setq rowcount (c:_adedbfrowqty))  ; how many rows selected/active
(setq rowcount (fix rowcount))
(princ (strcat "\n#Rows=" (itoa rowcount)))
(setq rownum (c:_adedbfcurrow))
(if (= rownum -1.0) (setq rownum (c:_adedbffet)))
(setq rownum (fix rownum))
(princ (strcat "\nCurrentRow#=" (itoa rownum)))
(princ "\nEntering While...")
(while 
	(and (< rownum rowcount) (> rownum -2) )  ; after last record rownum=-2
	(progn
		(princ (strcat "\nRownum=" (itoa rownum) " < rowcount= " (itoa rowcount)))
		(princ "\nGetting current Row values...")
		(setq rowlist (c:_adedbfcvl))  ; puts current row values in rowlist
		(princ "\nGot Rowlist")
		(setq colcount (length rowlist))
		(setq xcoord (nth (- xnum 1) rowlist))
		(setq ycoord (nth (- ynum 1) rowlist))
		(setq cb (nth (- 8 1) rowlist))
		(princ (strcat "\nRecord Type: " cb))
		(setq blkname "")
		(if (equal cb "CB1") (setq blkname "CB1"))
		(if (equal cb "GM") (setq blkname "GM"))
		(if (equal cb "MON") (setq blkname "MON"))
		(if (equal cb "PP") (setq blkname "PP"))
		(if (equal blkname "") (setq blkname "OTHER"))
		(princ (strcat "\nAbout to insert " blkname " at (" (rtos xcoord) "," (rtos ycoord) ")"))
		(setq insertpoint (list xcoord ycoord))
		(command "INSERT" blkname insertpoint "1" "" "")
		(setq ename (entlast))
		(setq result (c:_adesetlink ename))
		(setq rownum (c:_adedbffet))    ;next row # for next time thru
		(setq rownum (fix rownum))
	) ;progn
) ;while                                
