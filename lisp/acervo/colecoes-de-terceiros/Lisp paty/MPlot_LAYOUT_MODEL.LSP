(princ "\nMplot")
  ;;;----------
  (defun *error* (msg)
    (if (not (member msg '("Function cancelled" "quit / exit abort")))
      (princ (strcat "\n** Erro: " msg " **"))
      (princ "\nFunção cancelada pelo usuário!!!")
    )
    (setvar "osmode" osm)
    (setvar "cmdecho" echo)
    (setvar "VTENABLE" 3)
    (setq *error* nil)
    (princ)
  )
;;;----------
(defun c:mPlot (/ osm echo sel prancha prop p1 p3 Al1 Al2 EntLista n)
  (setq	n    0
	osm  (getvar "osmode")
	echo (getvar "cmdecho")
  )
  ;| Seleção das pranchas |;
  (setq	sel (ssget '((0 . "LWPOLYLINE")
		     (8 . "FORMATO EXTERNO,FORMATO")
		     (90 . 4)
		    )
	    )
  )
  (setq Prog100 (sslength sel))
  (repeat (sslength sel)
    (setq EntLista (cons (ssname sel n) EntLista))
    (setq n (1+ n))
  )
  (setq n 0)
  (setq	EntLista (vl-sort EntLista
			  (function (lambda (x y)
				      (< (cadr (assoc 10 (entget x)))
					 (cadr (assoc 10 (entget y)))
				      )
				    )
			  )
		 )
  )
  ;| Desativa OSNAP |;
  (if (< osm 16384)
    (setvar "osmode" (+ 16384 osm))
  )
  (setvar "cmdecho" 1)
  (setvar "VTENABLE" 7)
  (acet-ui-progress-init "Plotando:" Prog100)
  (if sel
    (repeat (sslength sel)
      (setq prancha (nth n EntLista))
      (setq prop (entget prancha))
      (progn
	(setq Al1  (assoc 10 prop)
	      prop (vl-remove Al1 prop)
	      prop (vl-remove (assoc 10 prop) prop)
	      Al2  (assoc 10 prop)
	      p1   (cdr Al1)
	      p3   (cdr Al2)
	)
      )
      ;| Imprime |;
      (command "ZOOM" "W" P1 P3) 
      (if (<= (atoi (getvar "ACADVER")) 17)
	(if (= (getvar "TILEMODE") 1);MODEL (ACAD 2007 OU INFERIOR)
	  (command "_.plot"
		   "Y"	;;;Detailed plot configuration? [Yes/No] <No>:
		   ""	;;;Enter a layout name or [?]:
		   ""	;;;Enter an output device name or [?]:
		   ""	;;;Enter paper size or [?]:
		   ""	;;;Enter paper units [Inches/Millimeters]:
		   ""	;;;Enter drawing orientation [Portrait/Landscape]:
		   ""	;;;Plot upside down? [Yes/No]:
		   "W"	;;;Enter plot area [Display/Extents/Limits/View/Window] <Window>:
		   p1	;;;Enter lower left corner of window <XXXX,YYYY>:
		   p3	;;;Enter upper right corner of window <XXXX,YYYY>:
		   ""	;;;Enter plot scale (Plotted Millimeters=Drawing Units) or [Fit] <1=1>:
		   ""	;;;Enter plot offset (x,y) or [Center] <Center>:
		   ""	;;;Plot with plot styles? [Yes/No] <Yes>:
		   ""	;;;Enter plot style table name or [?] (enter . for none) <>:
		   ""	;;;Plot with lineweights? [Yes/No] <Yes>:
		   ""	;;;Enter shade plot setting [As displayed/Wireframe/Hidden/Visual styles/Rendered]<As displayed>:
		   ""	;;;Write the plot to a file [Yes/No] <N>:
		   ""	;;;Save changes to page setup [Yes/No]? <N>
		   "Y"	;;;Proceed with plot [Yes/No] <Y>:
	  )
	  (command "_.plot" ;LAYOUT (ACAD 2007 OU INFERIOR)
		   "Y"	;;;Detailed plot configuration? [Yes/No] <No>: y
		   ""	;;;Enter a layout name or [?] <?>:
		   ""	;;;Enter an output device name or [?] <?>:
		   ""	;;;Enter paper size or [?] <A4>:
		   ""	;;;Enter paper units [Inches/Millimeters] <Millimeters>:
		   ""	;;;Enter drawing orientation [Portrait/Landscape] <Portrait>:
		   ""	;;;Plot upside down? [Yes/No] <No>:
		   "W"	;;;Enter plot area [Display/Extents/Layout/View/Window] <Extents>:
		   p1	;;;Enter lower left corner of window <XXXX,YYYY>:
		   p3	;;;Enter upper right corner of window <XXXX,YYYY>:
		   ""	;;;Enter plot scale (Plotted Millimeters=Drawing Units) or [Fit] <Fit>:
		   ""	;;;Enter plot offset (x,y) or [Center] <Center>:
		   ""	;;;Plot with plot styles? [Yes/No] <Yes>:
		   ""	;;;Enter plot style table name or [?] (enter . for none) <>:
		   ""	;;;Plot with lineweights? [Yes/No] <Yes>:
		   ""	;;;Scale lineweights with plot scale? [Yes/No] <Yes>:
		   ""	;;;Plot paper space first? [Yes/No] <No>:
		   ""	;;;Hide paperspace objects? [Yes/No] <No>:
		   ""	;;;Write the plot to a file [Yes/No] <N>:
		   ""	;;;Save changes to page setup [Yes/No]? <N>
		   "Y"	;;;Proceed with plot [Yes/No] <Y>:)
	  )
	)
	(if (= (getvar "TILEMODE") 1);MODEL (ACAD 2010 OU SUPERIOR)
	  (command "_.plot"
		   "Y"	;;;Detailed plot configuration? [Yes/No] <No>: y
		   ""	;;;Enter a layout name or [?] <Model>:
		   ""	;;;Enter an output device name or [?] <Canon ENGENHARIA>:
		   ""	;;;Enter paper size or [?] <A4>:
		   ""	;;;Enter paper units [Inches/Millimeters] <Millimeters>:
		   ""	;;;Enter drawing orientation [Portrait/Landscape] <Portrait>:
		   ""	;;;Plot upside down? [Yes/No] <No>:
		   "W"	;;;Enter plot area [Display/Extents/Limits/View/Window] <Window>: w
		   p1	;;;Enter lower left corner of window <71.892087,7425.059174>:
		   p3	;;;Enter upper right corner of window <281.892087,7722.059174>:
		   ""	;;;Enter plot scale (Plotted Millimeters=Drawing Units) or [Fit] <Fit>:
		   ""	;;;Enter plot offset (x,y) or [Center] <Center>:
		   ""	;;;Plot with plot styles? [Yes/No] <Yes>:
		   ""	;;;Enter plot style table name or [?] (enter . for none) <Nota de Serviço.ctb>:
		   ""	;;;Plot with lineweights? [Yes/No] <Yes>:
		   ""	;;;Enter shade plot setting [As displayed/Wireframe/Hidden/Visual styles/Rendered]
		   ""	;;;<As displayed>:
		   ""	;;;Write the plot to a file [Yes/No] <N>:
		   ""	;;;Save changes to page setup [Yes/No]? <N>
		   "Y"	;;;Proceed with plot [Yes/No] <Y>: n
		   )
	  (command "_.plot" ;LAYOUT (ACAD 2010 OU SUPERIOR)
		   "Y"	;;;Detailed plot configuration? [Yes/No] <No>: y
		   ""	;;;Enter a layout name or [?] <Layout1>:
		   ""	;;;Enter an output device name or [?] <Canon ENGENHARIA>:
		   ""	;;;Enter paper size or [?] <A4>:
		   ""	;;;Enter paper units [Inches/Millimeters] <Millimeters>:
		   ""	;;;Enter drawing orientation [Portrait/Landscape] <Landscape>:
		   ""	;;;Plot upside down? [Yes/No] <No>:
		   "W"	;;;Enter plot area [Display/Extents/Layout/View/Window] <Window>: w
		   p1	;;;Enter lower left corner of window <-644.265438,326.192738>:
		   p3	;;;Enter upper right corner of window <-357.265438,506.192738>:
		   ""	;;;Enter plot scale (Plotted Millimeters=Drawing Units) or [Fit] <Fit>:
		   ""	;;;Enter plot offset (x,y) or [Center] <Center>:
		   ""	;;;Plot with plot styles? [Yes/No] <Yes>:
		   ""	;;;Enter plot style table name or [?] (enter . for none) <acad.ctb>:
		   ""	;;;Plot with lineweights? [Yes/No] <Yes>:
		   ""	;;;Scale lineweights with plot scale? [Yes/No] <No>:
		   ""	;;;Plot paper space first? [Yes/No] <No>:
		   ""	;;;Hide paperspace objects? [Yes/No] <No>:
		   ""	;;;Write the plot to a file [Yes/No] <N>:
		   ""	;;;Save changes to page setup [Yes/No]? <N>
		   ""	;;;Proceed with plot [Yes/No] <Y>: n
	  )
	)
      )
      
      (acet-ui-progress-safe (setq n (1+ n)))
    )
  )
  ;| restaura OSNAP |;
  (acet-ui-progress-done)
  (setvar "osmode" osm)
  (setvar "cmdecho" echo)
  (setvar "VTENABLE" 3)
  (command "ZOOM" "E")
  (princ)
)