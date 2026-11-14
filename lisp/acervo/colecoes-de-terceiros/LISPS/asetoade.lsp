;This is a utility that will look for links to tables created by ASE and
;add EED to them so they will work with ADE....HEY GREAT IDEA
(defun c:asetoade()
;
;********   Functions   *********
;
   (defun set_column()
      (setq ColumnList (get_tile "ColumnNames"))
      (done_dialog 1)
   )

;
   (defun show_columns(ListName)
      (load_dcl "asetoade" "ShowColumns")
      (start_list "ColumnNames")
      (mapcar 'add_list Columns)
      (end_list)
      (action_tile "accept" "(set_column)")
      (action_tile "cancel" "(done_dialog 0)")
      (start_dialog)
      (unload_dialog DclID)
   )

;
   (defun load_dcl(DclFile DclName)
      (setq DclID (load_dialog (strcat DclFile ".dcl")))
      (if (not (new_dialog DclName DclID))
         (exit)
      )   
   )

;
   (defun update_lks() 
      (setq file2 (open (findfile "ade.lks") "a")
            KeyName (strcat ColName "_" (itoa (setq key (+ 1 key))))
      )
      (cond
         ((= KeyType "MultipleColumns") 
            (progn
               (setq LinkList      (nth 1 (assoc TableName TableList))  ;get list of columns to link
                     FinalColName  nil
                     FinalColValue nil
                     ListStatus    "start"
                     ColumnNumber  (substr LinkList 1 1)
                     NextNumber    (substr LinkList 2 1)
                     LinkList      (substr LinkList 3)
               )
               (while (/= ListStatus "end")  
                     ;  look for white space in table list
                  (if (/= "" NextNumber) 
                     (progn    
                        (while (/= " " NextNumber)
                           (setq ColumnNumber (strcat ColumnNumber NextNumber)
                                 NextNumber   (substr LinkList 1 1)
                           )
                        )     ; end NextNumber loop
                     )
                  )
                  (if (= nil FinalColName)       ;build final column name
                     (setq FinalColName (cdr (nth (+ 5 (atoi ColumnNumber)) LinkInfo)))
                     (setq FinalColName (strcat FinalColName ","
                                                (cdr (nth (+ 5 (atoi ColumnNumber)) LinkInfo))))
                  )
                  (if (= nil FinalColValue)      ;build final column value
                     (setq FinalColValue (nth (atoi ColumnNumber) ColValList))
                     (setq FinalColValue (strcat FinalColValue ","
                                                 (nth (atoi ColumnNumber) ColValList)))
                  )
                  (if (/= NextNumber "")
                     (setq ColumnNumber (substr LinkList 1 1)
                           NextNumber   (substr LinkList 2 1)
                           LinkList     (substr LinkList 3)
                     )
                     (setq ListStatus "end")
                  )
               )       ; end LinkList loop
            ) ; end multiple column progn
         )
         ((= KeyType "FirstColumn")
            (setq FinalColName ColName FinalColValue ColumnValue))
         (t nil)
      )      ; end condition KeyType
      (setq TableLink (strcat " | " KeyName " | " TableName " | " FinalColName 
                              " | " DatabaseDrv  " | " DatabaseName
                      )
      )
      (write-line TableLink file2)
      (close file2)
   )

;
   (defun match_brace()
        (setq ColumnValue (cdr (nth (setq CounterB (+ 1 CounterB)) LinkInfo))   ;first column value
              ColValList  (list ColumnValue)                                   ;start list of column values
              NextValue   (cdr (nth (setq CounterB (+ 1 CounterB)) LinkInfo))
        )
        (while (/= NextValue "}")
            (setq ColValList (cons NextValue ColValList) ;add column value to list
                  CounterB   (+ 1 CounterB)
                  NextValue  (cdr (nth CounterB LinkInfo))
         )
      )
      (setq ColValList (reverse ColValList))
   )

;
   (defun Build_ColValList()
      (setq FinalColValue (car ColValList)
            ColValueList  (cdr ColValList)
      )
      (repeat (length ColValueList)
         (setq FinalColValue (strcat FinalColValue "," (car ColValList))
               ColValueList  (cdr ColValueList)
         )
      )
   )

;
   (defun EED_list()
      (Build_ColValList)
; Check if entity is still there...
      (if (setq EntityName (handent EntityHandle))
         (setq EntityInfo (entget EntityName '("*"))
               LinkStuff  (strcat "&" KeyName "=" (chr 39) FinalColValue (chr 39))
               EEDInfo    (list '(1002 . "}"))
               EEDInfo    (cons (cons 1000 LinkStuff) EEDInfo)
               EEDInfo    (cons '(1002 . "{") EEDInfo)
         )
         nil
      )
   )

;
   (defun Add_EED()
       (setq EEDInfo    (cons "ADE" EEDInfo)
             EEDInfo    (list -3 EEDInfo)
             EntityInfo (append EntityInfo (list EEDInfo))
             regflag    1    
       )
       (princ "\nFirst App w/EED is ADE")
   )

;
   (defun Add_Ade()
      (setq NewSize (length EEDInfo)) 
      (if (< NewSize (xdroom EntityName)) ; If there is room for more...
         (progn
            (setq regflag 1
                  EEDInfo (list  (cons "ADE" EEDInfo))
                  OldInfo (cdr (assoc -3 EntityInfo))
                  EEDInfo (append OldInfo EEDInfo)
               EEDInfo (cons -3 EEDInfo)
            )
            (setq EntityInfo (subst EEDInfo (assoc -3 EntityInfo) EntityInfo))
            (princ "\nADE info added to existing Apps")
         )
      )
   )

;
   (defun Apnd_Ade()
      (setq NewSize (length EEDInfo))
      (if (< NewSize (xdroom EntityName)) ; If there is room for more...
         (progn
            (setq regflag    1
                  OldApps    (cdr (assoc -3 EntityInfo))
                  OldInfo    (cddr (assoc "ADE" OldApps))
                  EEDInfo    (append EEDInfo OldInfo)
                  EEDInfo    (cons (cons 1002 "{") EEDInfo)
                  EEDInfo    (cons "ADE" EEDInfo)
                  NewInfo    (subst EEDInfo (assoc "ADE" OldApps) OldApps)
                  NewInfo    (cons -3 NewInfo)
                  EntityInfo (subst NewInfo (assoc -3 EntityInfo) EntityInfo)                      
            )
            (princ "\nMore ADE info added!")
         )
      )
   )

;
   (setvar "cmdecho" 0)
;**********    Let's Get BUSY     *********
;
   (load_dcl "asetoade" "AsetoAde")
   (setq DoIt "no") 
   (action_tile "KeyType" "(setq KeyType $value)")
   (action_tile "accept" "(done_dialog 1)")
   (action_tile "cancel" "(done_dialog 0)")
   (start_dialog)
   (unload_dialog DclID)
;
;create selection set of application name "ACADASE" 
   (setq AseSet   (ssget "x" '((-3 ("ACADASE"))))
         counter  -1 
         key      0
         test2    nil
         CounterB nil
   )
;
   (if (tblsearch "appid" "ADE")
      (princ "\n ADE already registered. ")
      (regapp "ADE")
   )
;
   (repeat (sslength AseSet)
; get first entity which represents the first table linked to dwg
      (setq table1    (entget (ssname AseSet (setq counter (+ 1 counter))))
            record    (entget (entnext (cdr (assoc -1 table1))) '("ACADASE"))
            LinkFile  nil
            KeyName   nil
            TableList nil
            test1     (cdr (assoc 0 record))
      )
;start loop through ent list   
      (while (/= "SEQEND" test1)
         (setq LinkInfo (cadr (assoc -3 record)))  ;read ASE info
         (if (/= nil LinkInfo) 
            (progn
               (setq  DatabaseDrv  (cdr (nth 1 LinkInfo))      ;Driver Type
                      DatabaseName (cdr (nth 2 LinkInfo))     ;Database Name
                      TableName    (cdr (nth 3 LinkInfo))  ;First/Only Table Name
                      GroupA       (cdr (nth 4 LinkInfo))     ;Marks first list "{"
                      CounterB     nil
               )
               (if (= 1000 (car (nth 2 LinkInfo)))      ;test#1 Database Name
                  (progn
                     (if (= "{" GroupA) 
                        (setq ColName   (cdr (nth 5 LinkInfo))      ;First Column Name
                              Columns   (list ColName)               ;start list of columns
                              NextValue (cdr (nth 6 LinkInfo))     ;next value
                              CounterB  6
                        )
                        (setq test2 1070) ; this means the link was deleted
                     )      ; end if
                     (if (/= test2 1070)   ;test#2
                        (progn 
                           (while (/= NextValue "}")
                              (setq Columns (cons NextValue Columns)) ;add column name to list  
                              (setq CounterB (+ 1 CounterB))
                              (setq NextValue (cdr (nth CounterB LinkInfo)))
                           )
                           (setq Columns (reverse Columns))
                           (cond
                              ((= 1070 (car (nth (+ 1 CounterB) LinkInfo)))
                                 (setq test2 1070)
                              )
                              ((= 1002 (car (nth (setq CounterB (+ 1 CounterB)) LinkInfo)))
                                 (match_brace)
                              )
                              ((= 1005 (car (nth (+ 1 CounterB) LinkInfo)))
                                 (setq EntityHandle (cdr (nth (+ 1 CounterB) LinkInfo)))
                              )
                              (t nil)
                           ) ; end condition
                        )    ; progn completed
                     )      ; end test#2
                     (setq EntityHandle (cdr (nth (+ 1 CounterB) LinkInfo)))     
                     (if (/= test2 1070) ;   test for handles test#3
                        (progn
                           (if (= nil (assoc TableName TableList)) 
                              (progn
                           ;  select columns from table to link if multiple option selected
                                 (if (= KeyType "MultipleColumns")
                                   (show_columns Columns))
                            ; 
                                 (if (= nil TableList)              ;build a table list
                                    (setq TableList (list (list TableName ColumnList)))
                                    (setq TableList (cons (list TableName ColumnList) TableList))
                                 )
                                 (update_lks)   ;  update ADE.lks file  
                              )
                           ) ;  end if
; If entity is there...
                           (if (EED_list)  ;  create EED list
; check for EED already present on entity
                              (progn
                                 (cond
                                    ((= nil (assoc -3 EntityInfo)) ;NO EED
                                       (Add_EED)
                                    )
                                    ((and (/= nil (assoc -3 EntityInfo)) ;NO ADE EED
                                          (= nil (cddr (assoc "ADE" (cdr (assoc -3 EntityInfo)))))) 
                                       (Add_Ade)
                                    )
                                    ((/= nil (cddr (assoc "ADE" (cdr (assoc -3 EntityInfo))))) ;YES ADE EED
                                       (Apnd_ADE)
                                    )
                                    (t nil)
                                 )     ; end check for ADE EED data
; Finally update the entity in the database to contain the new xdata.
                                 (if (entmod EntityInfo)
                                    (princ "\nASE info Converted")
                                    (princ "\nNothing done")
                                 )     
                              )
                              (princ "\nNo entity.")
                           )
                        ) ;end entity modification
                     )     ;end test#3
                  )    ; end test2
               )  ;  end database name test1
            ) ;;progn 
         ) ;;if 
         (setq record (entget (entnext (cdr (assoc -1 record))) '("ACADASE"))
               test1  (cdr (assoc 0 record))
         )
      )  ;  end loop go to next entity
      ;  go to next table  
   )     ; end repeat       
;complete
   (setvar "cmdecho" 1)
   (princ "\n Finished...")
)
(princ "ASE to ADE loaded:")
      

   
 

