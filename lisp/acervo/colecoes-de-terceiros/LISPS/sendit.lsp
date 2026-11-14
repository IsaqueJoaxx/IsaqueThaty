(defun C:SENDIT ( / )
;Open a channel, request topics, create message string
  (setq chnl (initiate "Excel" "System")
        topics (request chnl "topics")
        topicmsg (strcat "The available topics are: " topics "\n\n")
  )
;Terminate conversation and close DDE channel
  (terminate chnl)
;Check for open sheet (System as first topic indicates
;that there are no open sheets).
  (if (= topics "System")
    (alert "There are no spreadsheets!\n\nCannot send to a null sheet.")
    (progn
      ;Get length of topics string and start a counter.
      (setq toplen (strlen topics) 
            count 1
      )
      ;This while function parses the topics string
      ;to locate the first tab character (chr 009).  A tab
      ;is used as the separator between items in the
      ;returned topics list.  When the tab is encountered,
      ;the code "backs up" one character and stores its
      ;location so it can extract the string up to the tab
      ;character (the first topic).
      (while (<= count toplen)
        (setq topchar (substr topics count 1))
        (if (= topchar (chr 009))
          (setq topstop (1- count)
                sheet (substr topics 1 topstop)
                count toplen
          )
        )
        (setq count (1+ count))
      )
      (setq sheetmsg (strcat "Excel returned " sheet " as the first topic.")
            msg (strcat topicmsg sheetmsg)
      )
      (alert msg)
      ;Build a user-defined selection set.
      (setq ss1 (ssget)
        dwgname (getvar "DWGNAME")
        dwgpref (getvar "DWGPREFIX")
        whoami (getvar "LOGINNAME")
      )
      ;Open a channel to Excel and poke data to Excel. Note  
      ;that the variables pk# are used to store the return
      ;code from each poke statement.
      (setq chnl (initiate "Excel" sheet)
            pk1 (poke chnl "R1C1" "Sample DDE Exchange")
            pk2 (poke chnl "R3C1" "Drawing Prefix")
            pk3 (poke chnl "R4C1" "Drawing Name")
            pk4 (poke chnl "R5C1" "User")
            pk5 (poke chnl "R7C1" "Set Data")
            pk6 (poke chnl "R3C2" dwgpref)
            pk7 (poke chnl "R4C2" dwgname)
            pk8 (poke chnl "R5C2" whoami)
            pk9 (pokeset chnl ss1 8 1)
      )
      (terminate chnl)
    )
  )
)
