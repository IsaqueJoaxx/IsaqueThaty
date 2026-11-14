;;;   ACADR12.LSP
;;;   Copyright (C) 1992 by Autodesk, Inc.
;;;
;;;   Permission to use, copy, modify, and distribute this software
;;;   for any purpose and without fee is hereby granted, provided
;;;   that the above copyright notice appears in all copies and that
;;;   both that copyright notice and this permission notice appear in
;;;   all supporting documentation.
;;;
;;;   THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT EXPRESS OR IMPLIED
;;;   WARRANTY.  ALL IMPLIED WARRANTIES OF FITNESS FOR ANY PARTICULAR
;;;   PURPOSE AND OF MERCHANTABILITY ARE HEREBY DISCLAIMED.
;;;
;;;   Note:  This file is loaded automatically by ACAD.MNL at the start
;;;          of each editing session.  It establishes an autoloader and
;;;          other utility functions.

;;;=== General Utility Functions ===

(defun *merr* (msg)
  (setq *error* m:err m:err nil)
  (princ)
)

(defun *merrmsg* (msg)
  (princ msg)
  (setq *error* m:err m:err nil)
  (princ)
)

;;;=== AME and Region Modeler Functions ===

(defun ai_setup (len area lu mass vol)
  (setq m:err *error* *error* *merrmsg*)
  (ai_loadame)
  (if SOLUNION
    (progn
      (sollength len)
      (solareau area)
      (setvar "lunits" lu)
      (princ "\nLength unit is set to ")(princ len)(princ ". ")
      (princ "Area unit is set to ")(princ area)(princ ".\n")
       (if SOLBOX
         (progn
            (solmass mass)    ;don't ask for regions
            (solvolume vol)   ;don't ask for regions
            (princ "Mass unit is set to ")(princ mass)(princ ". ")
            (princ "Volume unit is set to ")(princ vol)(princ ".\n")
         )
       )
      (c:soldisplay)
      (c:solwdens)
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

(defun ai_upgvar ()
  (setq m:err *error* *error* *merrmsg*)
  (ai_loadame)
  (if SOLUNION
    (progn
     (solhangle 45)
     (solhpat "U")
     (solsolidify 3)
     (princ "\nVariables Upgraded. Objects will be automatically solidified.")
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

(defun ai_upgprec ()
  (setq m:err *error* *error* *merrmsg*)
  (ai_loadame)
  (if SOLUNION
    (progn
      (solupgrade 1)
      (solpurge "bfile")
      (princ "\nAME R1 solids will be converted to Double Precision.")
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

(defun ai_scrcomp ()
  (setq m:err *error* *error* *merrmsg*)
  (ai_loadame)
  (if SOLUNION
    (progn
      (solamecomp "ame1")
      (princ "\nScript compatibility set to AME R1.")
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

(defun ai_loadame ()
  (setq m:err *error* *error* *merrmsg*)
  (if (not SOLUNION)
    (if (or (findfile "region.exp") (findfile "region"))
      (_autoxload "region")
      (_autoxload "ame")
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

(defun ai_unloadame ()
  (setq m:err *error* *error* *merrmsg*)
  (if (xunload "ame" nil)
    (progn
      (ai_aloadame)
      (princ "\nAME unloaded.\n")
    )
  )
  (if (xunload "region" nil)
    (progn
      (autoxload "region" '("solidify" "solint" "solsub" "solunion" "solarea"
                            "solmove" "solchp" "solsep" "sollist" "solmassp"
                            "sollength" "solvar" "solmesh" "solwire" "solfeat"
                            "solwdens" "solucs" "solpurge" "ddsolvar"
                            "ddsolmassp" "solamever" "solareau" "solaxcol"
                            "soldelent" "soldisplay" "solhangle" "solhpat"
                            "solhsize" "sollength" "solpagelen" "solrender"
                            "solservmsg" "solsolidify" "solmatcurr")
      )
      (if (or (findfile "ame.exp") (findfile "ame"))
        (ai_amegrey " ")
      )
      (princ "\nRegion unloaded.\n")
    )
  )
  (menucmd "S=X")
  (menucmd "S=S")
  (setq *error* m:err m:err nil)
  (princ)
)

(defun ai_select (/ isloaded)
  (setq m:err *error* *error* *merrmsg*)
  (if (and
        (or (findfile "ame.exp") (findfile "ame"))
        (or (findfile "region.exp") (findfile "region"))
      )
    (progn
      (princ "\nNo modeler is loaded yet. ")
      (princ "Both AME and Region Modeler are available. ")
      (initget "Ame Region" )
      (setq isloaded (getkword "\nAutoload Region/<AME>: "))
      (if (= isloaded "Region")
        (setq app "region")
        (setq app "ame")
      )
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

;;;  AI_AMEGREY is used to enable/disable the items in the "Model" (POP9)
;;;  pulldown menu, based on whether AME or the Region Modeler has been
;;;  loaded.  If you move "Model" to a different pulldown menu or add
;;;  items to it, adjust the "base" value and/or the menu-item number
;;;  list accordingly.

(defun ai_amegrey (action / base)

  (setq base (list "P9" 0))           ; Identify top of "Model" menu

  (foreach a '(1 2   4   13   15 16   27 28   33   35 36   44 45   47   50 51)
    (menucmd (strcat (car base) "." (itoa (+ (cadr base) a)) "=" action))
  )
)

(defun ai_aloadame (/ exist_reg)
 (if (null c:solunion)
  (progn
  (setq m:err *error* *error* *merrmsg*)
  (if (or (findfile "region.exp") (findfile "region"))
    (progn
      (setq exist_reg "yes")
      (autoxload "region" '("solidify" "solint" "solsub" "solunion" "solarea"
                            "solmove" "solchp" "solsep" "sollist" "solmassp"
                            "sollength" "solvar" "solmesh" "solwire" "solfeat"
                            "solwdens" "solucs" "solpurge" "ddsolvar"
                            "ddsolmassp" "solamever" "solareau" "solaxcol"
                            "soldelent" "soldisplay" "solhangle" "solhpat"
                            "solhsize" "sollength" "solpagelen" "solrender"
                            "solservmsg" "solsolidify" "solmatcurr")
      )
      (autoxload "ame" '("solbox" "solcone" "solcyl" "solsphere" "soltorus"
                         "solwedge" "solext" "solrev" "solcham" "solfill"
                         "solcut" "solinterf" "soldecomp" "solsubdiv"
                         "solmass" "solvolume" "solsect" "solprof"
                         "solmat" "solin" "solout" "ddsolmat" "solamecomp"
                         "solmass" "solsectype" "solupgrade" "solvolume")
      )
    )
    (autoxload "ame" '("solbox" "solcone" "solcyl" "solsphere" "soltorus"
                       "solwedge" "solext" "solrev" "solidify" "solint"
                       "solsub" "solunion" "solcham" "solfill" "solcut"
                       "solmove" "solchp" "solsep" "sollist" "solmassp"
                       "solarea" "solinterf" "soldecomp" "solsubdiv"
                       "sollength" "solmass" "solvar" "solvolume" "solmesh"
                       "solwire" "solfeat" "solsect" "solprof" "solwdens"
                       "solmat" "solucs" "solin" "solout" "solpurge"
                       "ddsolvar" "ddsolmassp" "ddsolmat" "solamecomp"
                       "solamever" "solareau" "solaxcol" "soldelent"
                       "soldisplay" "solhangle" "solhpat" "solhsize"
                       "sollength" "solmass" "solmatcurr" "solpagelen"
                       "solrender" "solsectype" "solservmsg" "solsolidify"
                       "solupgrade" "solvolume" )
    )
  )
  (if (and
        (null (or (findfile "ame.exp") (findfile "ame")))
        (= exist_reg "yes")
      )
    (ai_amegrey "~")
  )
  (setq *error* m:err m:err nil)
  )
 )
 (princ)
)

;;;=== AVE Render Functions ===

(defun ai_loadaverendr ()
  (if (null c:render)
    (autoxload "autovis" '("render" "light" "scene" "rmat" "rpref"
                            "setuv" "showmat" "stats" "replay" "saveimg"
                            "3dsin" "3dsout" "vlconv" "rconfig" "matlib")
    )
  )
  (princ)
)

(defun c:ai_unloadave ()
  (princ "\n")
  (if (xunload "autovis" nil)
    (progn
      (princ "\nAutoVision unloaded.\n")
      (ai_loadaverendr)
    )
  )
  (menucmd "S=X")
  (menucmd "S=S")
  (princ)
)

;;;=== ASE Functions ===

;;;  AI_ASEGREY is used to enable/disable the ASE items in the "File" (POP1)
;;;  pulldown menu, based on whether ASE has been loaded.  If you move
;;;  "File" to a different pulldown menu, change the "P1" string
;;;  accordingly.  If you add items to the "File" menu prior to the "->ASE"
;;;  item, adjust the "base" value accordingly.

(defun ai_asegrey (action / base)

  (setq base (list "P1" 9))           ; Identify "->ASE" item number

  (foreach a '(2   4 5   7 8   10 11 12   14 15   17 18 19 20  21 22 23 24
               25 26 27 28 29  30 31 32 33  34 35 36 37 38  39 40   42)
    (menucmd (strcat (car base) "." (itoa (+ (cadr base) a)) "=" action))
  )
  (if (= action "~")
    (setq ai_aseinit_yet nil)
  )
  (setq action (if (= action "~") " " "~"))  ; Invert action for "Initialize"
  (menucmd (strcat (car base) "." (itoa (1+ (cadr base))) "=" action))
)

(defun ai_aseinit_chk ()
  (menucmd "S=X")
  (if ai_aseinit_yet
    (menucmd "S=ASE")
    (menucmd "S=ASEINIT0")
  )
  (princ)
)

(defun ai_aseinit ()
  (setq m:err *error* *error* *merrmsg*)
  (if (= nil c:aseinit)
    (progn
      (princ "\nAutoCAD SQL Extension")
      (if (and (or (/= nil (findfile "ase"))
                   (/= nil (findfile "ase.exp"))
                   (/= nil (findfile "ase.exe"))
               )
               (/= nil (findfile "ase.lsp"))
          )
        (progn
          (xload "ase")
          (load "ase.lsp")
          (ai_asegrey " ")
          (menucmd "S=X")
          (menucmd "S=ASE")
          (setq ai_aseinit_yet T)
          (princ " loaded.")
          (c:aseinit)
        )
        (progn
          (princ " not loaded.")
          (princ "\nThe ASE files were not found in your search path directories.")
          (princ "\nCheck the installation of the SQL Extension and try again.")
        )
      )
    )
    (progn
      (ai_asegrey " ")
      (menucmd "S=X")
      (menucmd "S=ASE")
      (if (not ai_aseinit_yet)
        (progn
          (if (findfile "ase.lsp")
            (load "ase.lsp")
            (princ "\nASE.LSP not found.  'Quick' commands not available.")
          )
          (c:aseinit)
        )
      )
      (setq ai_aseinit_yet T)
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

(defun ai_aseterm ()
  (setq m:err *error* *error* *merrmsg*)
  (if (= ase_errno 0)
    (progn
      (xunload "ase")
      (ai_asegrey "~")
      (menucmd "S=X")
      (menucmd "S=S")
      (princ "\nAutoCAD SQL Extension unloaded.")
    )
  )
  (setq *error* m:err m:err nil)
  (princ)
)

;;; ===== AutoLoad =====

(defun _autoqload (quoi app cmdliste / qapp symnam)
  (setq qapp (strcat "\"" app "\""))
  (mapcar
   '(lambda (cmd / nom_cmd)
      (progn
        (setq nom_cmd (strcat "C:" cmd))
        (if (not (eval (read nom_cmd)))
            (eval
             (read (strcat
                    "(defun " nom_cmd "( / rtn)"
                    "(setq m:err *error* *error* *merrmsg*)"
                    "(if (ai_ffile " qapp ")"
                    "(progn (princ \"\nInitializing...\")"
                    "(_auto" quoi "load " qapp ") (setq rtn (" nom_cmd ")))"
                    "(ai_nofile " qapp "))"
                    "(setq *error* m:err m:err nil)"
                    "rtn)"
                    ))))))
   cmdliste)
  nil
)

(defun autoload (app cmdliste)
  (_autoqload "" app cmdliste)
)

(defun autoxload (app cmdliste)
  (_autoqload "x" app cmdliste)
)

(defun _autoload (app)
; (princ "Auto:(load ") (princ app) (princ ")") (terpri)
  (load app)
)

(defun _autoxload (app)
; (princ "Auto:(xload ") (princ app) (princ ")") (terpri)
  (if (= app "region") (ai_select))
  (xload app)
  (if (= app "region") (ai_amegrey "~"))
)

(defun ai_ffile (app)
  (or (findfile (strcat app ".lsp"))
      (findfile (strcat app ".exp"))
      (findfile (strcat app ".exe"))
      (findfile app)
  )
)

(defun ai_nofile (filename)
  (princ
    (strcat "\nThe file "
            filename
            "(.lsp/.exp/.exe) was not found in your search path directories."
    )
  )
  (princ "\nCheck the installation of the Support Files and try again.")
  (princ)
)


;;;===== AutoLoad LISP Applications =====

(autoload "appload" '("appload"))

(autoload "filter" '("filter"))

(autoload "dline" '("dline" "dl"))

(autoload "3d" '("3d" "ai_box" "ai_pyramid" "ai_wedge" "ai_dome"
                 "ai_mesh" "ai_sphere" "ai_cone" "ai_torus" "ai_dish")
)

(autoload "ddinsert" '("ddinsert"))

(autoload "asctext" '("asctext"))

(autoload "ddattdef" '("ddattdef"))

(autoload "ddattext" '("ddattext"))

(autoload "3darray" '("3darray"))

(autoload "ddmodify" '("ddmodify"))

(autoload "ddchprop" '("ddchprop"))

(autoload "ddview" '("ddview"))

(autoload "ddvpoint" '("ddvpoint"))

(autoload "mvsetup" '("mvsetup"))

(autoload "ddosnap" '("ddosnap"))

(autoload "ddptype" '("ddptype"))

(autoload "dducsp" '("dducsp"))

(autoload "ddunits" '("ddunits"))

(autoload "ddgrips" '("ddgrips"))

(autoload "ddselect" '("ddselect"))

(autoload "ddrename" '("ddrename"))

(autoload "rman_dcl" '("rmmenu"))

(autoload "ddsolprm" '("ddsolprm"))

(autoload "plud" '("plud"))

;;;===== AutoXLoad ADS Applications =====

(autoxload "rasterin" '("gifin" "pcxin" "riaspect" "ribackg"
                        "riedge" "rigamut" "rigrey" "rithresh" "tiffin")
)

(autoxload "geomcal" '("cal"))

(autoxload "geom3d" '("mirror3d" "rotate3d" "align"))

(princ)
; Next available MSG number is 97 
; MODULE_ID ASURFA_LSP_
;;;===== AutoXLoad AutoSurf Application =====
 
#define MODULE_ID ASURFA_LSP_

(defun ai_aloadasurf (/ x adsl adss xld app)
  (setq m:err *error* *error* *merrmsg*)
  (setq x 0 adsl (length (ads)) adss (ads) xld nil)
  (if (eq (getvar ;|MSG0|;"platform") ;|MSG0|;"386 DOS Extender")
    (setq app ;|MSG0|;"asurf.exp")
    (setq app ;|MSG0|;"asurf")
  )
  (while (< x adsl)
    (if (=
          (strcase
            (substr (nth x adss)
              (- (strlen (nth x adss))(1- (strlen app) ))
            )
          )  
          (strcase app)
        )
      (setq x adsl xld 1)
    )
    (setq x (1+ x))
  )
  (if (and (not xld) (null c:asurfver))
    (if (or (findfile ;|MSG0|;"asurf.exp") (findfile ;|MSG0|;"asurf"))
      (autoxload ;|MSG0|;"asurf" '(
                                   ;|MSG1|;"asangle" ;|MSG2|;"asaugvector"
                                   ;|MSG3|;"asdeforder" ;|MSG4|;"asdisprot"
                                   ;|MSG5|;"asfillsfrad" ;|MSG96|;"asfillsftrim"
                                   ;|MSG6|;"asflowsu" ;|MSG7|;"asflowsv"
                                   ;|MSG8|;"asgrpprefix" ;|MSG92|;"asintsftrim"
                                   ;|MSG9|;"asjoingap" ;|MSG10|;"aslength"
                                   ;|MSG94|;"ame2sf" ;|MSG89|;"aspageln"
                                   ;|MSG95|;"asprojsftrim" ;|MSG14|;"assaveoriginal"
                                   ;|MSG15|;"assurfdisp" ;|MSG16|;"assurfu"
                                   ;|MSG17|;"assurfv" ;|MSG18|;"assplineconv"
                                   ;|MSG19|;"assurfvector" ;|MSG20|;"assystol"
                                   ;|MSG21|;"asurfapihand" ;|MSG22|;"asurfver"
                                   ;|MSG88|;"asvarfile" ;|MSG23|;"acad2sf"
                                   ;|MSG24|;"blendsf" ;|MSG25|;"breaksf"
                                   ;|MSG26|;"breakvw" ;|MSG27|;"cornersf"
                                   ;|MSG28|;"createaug" ;|MSG29|;"direction"
                                   ;|MSG30|;"display" ;|MSG74|;"dispsf"
                                   ;|MSG31|;"dist3d" ;|MSG32|;"editaug"
                                   ;|MSG33|;"editborder" ;|MSG66|;"editsf"
                                   ;|MSG34|;"editsp" ;|MSG35|;"ellipse3d"
                                   ;|MSG37|;"entvis" ;|MSG38|;"explode3d"
                                   ;|MSG39|;"extendsf" ;|MSG40|;"extendvw"
                                   ;|MSG41|;"extrudesf" ;|MSG42|;"fillet3d"
                                   ;|MSG43|;"filletsf" ;|MSG44|;"flowsf"
                                   ;|MSG45|;"groupas" ;|MSG46|;"igescurve"
                                   ;|MSG47|;"igessurf" ;|MSG48|;"intersf"
                                   ;|MSG49|;"intervw" ;|MSG50|;"join3d"
                                   ;|MSG51|;"joinsf" ;|MSG52|;"line2pt"
                                   ;|MSG53|;"linevw" ;|MSG36|;"listas"
                                   ;|MSG54|;"loftsf" ;|MSG93|;"normalsf"
                                   ;|MSG55|;"meshsf" ;|MSG56|;"modin"
                                   ;|MSG57|;"modout" ;|MSG58|;"offsetsf"
                                   ;|MSG59|;"offsetvw" ;|MSG60|;"outputaug"
                                   ;|MSG61|;"partsf" ;|MSG62|;"planesf"
                                   ;|MSG63|;"primsf" ;|MSG75|;"profsf"
                                   ;|MSG64|;"project3d" ;|MSG65|;"projectsf"
                                   ;|MSG67|;"purgesf" ;|MSG68|;"refine3d"
                                   ;|MSG69|;"refinesf" ;|MSG70|;"revolvesf"
                                   ;|MSG71|;"rulesf" ;|MSG72|;"sectsf"
                                   ;|MSG73|;"spline3d" ;|MSG76|;"surfvar"
                                   ;|MSG77|;"sweepsf" ;|MSG78|;"trimvw"
                                   ;|MSG79|;"truncsf" ;|MSG80|;"tubesf"
                                   ;|MSG81|;"updateappname" ;|MSG82|;"updatedb"
                                   ;|MSG83|;"unspline"
                                 )
      )
      (if (eq (getvar ;|MSG0|;"platform") ;|MSG0|;"386 DOS Extender")
        (progn
          (princ ;|MSG11|;"\nasurf.exp not found.  AutoSurf is not enabled. ")
          (princ ;|MSG12|;"\nOther utilities ")
        )
        (progn
          (princ ;|MSG84|;"\nasurf not found.  AutoSurf is not enabled. ")
          (princ ;|MSG12|;"\nOther utilities ")
        )
      )
    )
  )  
  (setq *error* m:err m:err nil)
  (princ)
)
 
(if (findfile ;|MSG0|;"asurf.lsp")
  (if (not (load ;|MSG0|;"asurf.lsp" nil))
    (progn
      (princ ;|MSG85|;"loading error.")
      (princ ;|MSG86|;"\nasurf.lsp not found.  AutoSurf shortkeys are not enabled.")
      (princ ;|MSG12|;"\nOther utilitites ")
    )
  )  
)

(defun ai_unloadasurf ()
  (setq m:err *error* *error* *merrmsg*)
  (if (xunload ;|MSG0|;"asurf" nil)
    (progn
      (ai_aloadasurf)
      (princ ;|MSG87|;"\nAutoSurf unloaded.\n")
    )
  )
  (menucmd ;|MSG0|;"S=X")
  (menucmd ;|MSG0|;"S=S")
  (setq *error* m:err m:err nil)
  (princ)
)
 
(defun ai_loadasurf ()
  (setq m:err *error* *error* *merrmsg*)
  (if (not asurfver)
    (if (or (findfile ;|MSG0|;"asurf.exp") (findfile ;|MSG0|;"asurf"))
      (_autoxload ;|MSG0|;"asurf")
      (if (eq (getvar ;|MSG0|;"platform") ;|MSG0|;"386 DOS Extender")
        (princ ;|MSG11|;"\nasurf.exp not found.  AutoSurf is not enabled")
        (princ ;|MSG84|;"\nasurf not found.  AutoSurf is not enabled")
      )
    )
  )  
  (setq *error* m:err m:err nil)
  (princ)
)

(autoload ;|MSG90|;"asfilter" '(;|MSG91|;"asfilter"))

;;;===== AutoXLoad ADESIGN Application =====
  
(load "acdload")

(princ)

