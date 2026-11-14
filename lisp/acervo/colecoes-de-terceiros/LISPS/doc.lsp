 Introduction:
 ------------
     LEDIT is an Ascii text editor which has been developed for use as an
     AutoLisp development tool.  While specifically intended as an
     AutoLisp Editor it nevertheless can also be used to edit any other
     Ascii format file.

     LEDIT's features include full mouse support, moveable-zoomable windows,
     multiple concurrent editing of files, online help for AutoLisp functions
     including many examples of their usage, file printing, bracket closure
     testing, and automatic saving and restoration of the LEDIT desktop.

     LEDIT can be run under AutoCad as an external command by including
     it in the AutoCad Acad.pgp file, or separately as a standalone
     editor.  

     This edition of LEDIT is restricted to file sizes of 64K, but this
     is more than adequate for most AutoLisp files.
 
 Installation
 ------------
 
     To install LEDIT simply copy the files LEDIT.EXE and LEDIT.HLP to an
     existing directory, or make a directory called LEDIT and copy those
     same files into it.  Be sure that your current path includes that
     directory if you intend to use LEDIT.
     
     If the user intends to run LEDIT as an external AutoCad command insert
     the line LEDIT,LEDIT,127000,,0 into the ACAD.PGP file.
     
 
 Environment Variables
 ---------------------

     To set up LEDIT for use outside its home directory it is necessary
     to set the LEDIT enviroment variable LEDIT.  This can be done by
     using the SET command in the autoexec.bat file or any other batch
     file desired by the user.  The proper syntax is:
     
         SET LEDIT = PATH\
         
         For example, SET LEDIT = C:\LEDIT\
     
     Note that a '\' must be added to the end of the LEDIT home directory.
     This variable must be set in order for LEDIT to find its help file.
     
     Also, if the user wants to set the lines per printed page parameter
     for LEDIT (it defaults to standard 60 lines) the environment variable
     LPRN can be set with SET LPRN = page length.  For example, to use 70
     lines per page, SET LPRN = 70.  Again, this can be set in a batch file.
     
     If so desired, a user can set these variables from the command line.
            
       
 LEDIT Windows
 -------------

     Several edit windows at any one time may be open in LEDIT, but only
     one of them can be active.  The active window is the one in which
     any editing is being done.  Any command or editing applies to this
     window only.

     Each window has the following characteristics:

         a) a title bar
	 b) a close box
	 c) scroll bars
	 d) a resize corner
	 e) a zoom window
     
     The active window has a double-lined border around it, and it 
     always has a close box, a zoom box, scroll bars, and a resize
     corner.  It is also always the frontmost window.

     The active window also displays the current line and column numbers
     in the lower left corner.  The close box of a window is the box in the
     upper left corner.  Clicking the left mouse button on this box closes
     the window.  If the file has been changed the user is prompted for a
     choice of saving all changes or not ( Alt-F3 is the same command).
     The title bar, the topmost horizontal bar of a window, contains the
     name of the file.  Double-clicking on the title bar zooms the window
     (and vice-versa).  One can also drag the title bar to move the window
     around by placing the mouse cursor on the bar, holding the left button
     down, and moving the mouse.  Releasing the key completes the operation.
     To move the window via the keyboard select Windows, then Size, and
     move the arrow key.  Hit escape when done.  The zoom box of a window
     appears in the upper right corner.  If the icon in that corner is an
     up arrow you can click on it to enlarge the window to the largest
     size possible.  If the icon is a double-headed arrow the window is
     already at maximum size.  Clicking will then return the window to
     its previous size.  To zoom a window from the keyboard choose Windows,
     then Zoom, or else press F5.

     Scroll bars are visible on the right and bottom of the active window.
     These bars can be used with a mouse to scroll the contents of the
     window.  Click the arrow at either end to scroll the contents of the
     window.  Click the arrow at either end to scroll one line at a time.
     Click the shaded area either side of the scroll box to scroll a page
     at a time.

     The resize box is in the lower right corner of a window.  The user
     can drag any corner of it to make the window larger or smaller.  The
     resize box has only single line borders.  To resize using the keyboard,
     select Windows, then Size.  Hold down the shift and use the arrow keys
     to resize the window.  Hit escape when done.  To make the next window
     active the user can select Windows, then Next.  A much quicker way
     is to hit F6.  If other windows are visible the mouse cursor can be
     moved over one, and if the left key is clicked that window becomes
     active.

     Table One gives a summary of window manipulation.

                          TABLE ONE

     To Do                              How

     Open an Edit Window          Select File, the Open, or press F3.

     Close a Window               Select Window, then Close. Or, click
     			          the window close box. Or, press Alt-F3.

     Activate a Window            Click mouse anywhere in the window. Or,
                                  choose Windows, then Next.  Or, press F6.

     Move the Active Window       Drag the title bar.  Or, select Windows,
                                  then Size.  Or, press Ctrl-F5, and use
				  the arrow keys to move.  Hit enter when
				  done.  

     Resize the Active Window     Drag the resize corner.  Or, choose
                                  Windows, then Size, and use the arrow
				  keys to resize the Window.  Or, press
				  Ctrl-F5 and use the arrow keys to resize.
				  The SHIFT key must be held down when
				  arrow keys are used.

     Zoom the Active Window       Click the zoom box in the upper right
                                  corner of the window.  Or, double-click
				  the left mouse button on the title bar.
				  Or, choose Windows, then Zoom.  Or, press
				  F5.

     Status Line
     -----------

     The status line is present at the bottom of the screen.  It displays
     several keystroke hotkeys and their functions, and allows the user
     to click on them with a mouse to activate if so desired.  If a
     hardware error occurs the status line will display an error message.

     Dialog Box
     ----------

     A dialog box is a selection or message box that appears to prompt
     the user for selections or return an alert or an error message.  They
     can be closed by clicking the close box in the upper left corner or
     by hitting the ESC key, and if OK is displayed, by hittint the ENTER
     key.  If there are highlight options they can be selected by either
     clicking the left mouse button or by pressing <ALT><letter>.  Also,
     all options can be cycled through by pressing the TAB key.  An example
     of a dialog box is the file selection menu box that appears when the
     user selects the File-Open option.
     				  				  				  
 Editor Environment
 ------------------

     Features
     --------

     The editor environment is bounded by two selection bars, one at the
     top of the screen and one at the bottom.  The top bar is referred to
     as the main menu bar, and the bottom bar is referred to as the status
     line.

     Main Menu Bar
     -------------

     The main menu bar is located at the top of the screen.  It consists of
     six separate sections: File, Edit, Search, Windows, Desktop, and Help.
     Also, on the right side of the bar, can be found a time display which
     indicates the current system time.

     To access the main menu the user can hit the F10 function key or simply
     move the mouse cursor up to the menu bar.  The user will notice that
     the first letter of each of the menu sections is highlighted.  This
     indicates that by entering the letter of a highlighted character one
     can select that menu subsection.  If a user is using the mouse for
     selection they must simply move the mouse cursor onto the desired menu
     keyword and click the left button.  This selects the desired submenu.
     If the user is working within the editor they can immediately jump to
     any one of the menu sections by entering <Alt><Highlighted Menu Letter>
     simultaneously.

  File Menu
  ---------

     This section consists of nine menu items: Open, New, Save, Save As...,
     Print Files, Change Dir, Dos Shell, User Info, and Exit.  As in the
     main menu bar the user can see that one letter in each menu keyword
     has been highlighted.  By entering a highlighted letter of a keyword
     the corresponding submenu is selected.  To select with a mouse move
     the mouse cursor to the submenu and click the left button.

     Open  ( Hot Key : F3)
     ----

     This command will open an existing file for editing.  A file selection
     window, centered in the desktop, opens and displays a list of all files
     in the current working directory with a lsp file extension.  The cursor
     defaults to a position within the Name box and displays *.lsp.  The user
     can enter a file name, and if no file extension is entered a lsp 
     extension is added.  To jump to the Files section of this window hit
     the TAB key, then use the arrow keys to move up and down through the 
     list.  To select a file highlight it by moving the arrow keys, then hit
     ENTER.  The file will be opened.  To exit hit ESC.

     If the user is selecting with a mouse simply move to a desired file and
     click the left mouse button.  The file will be opened.  Any other option
     can be executed by moving the mouse cursor over the function keyword and
     clicking the left button.

     New
     ---

     This command opens a new file for editing.  It is assigned the name
     untitled which is used until the user saves it and assigns a proper
     file name to it.

     Save  ( Hot Key : F2 )
     ----

     If selected this command will save the current file to disk, then
     resume the editing session.  If the file is untitled the user will
     be prompted for a new file name.  If no extension is entered a lsp
     extension will be added.

     Save As...
     ----------

     This command will prompt the user for a file or file name to save the
     current file to disk.  A valid file or a new name can be entered.  The
     editing session is resumed once the save is complete.

     Print Files  ( Hot Key : <Alt-P> )
     -----------

     This command will print a selected text file.  A file selection window
     is opened and the user may select a file to be printed.  By default,
     only files with lsp extensions are displayed for selection.  If other
     text files are to be printed the user must enter the file extension to
     print or display files with that extension.

     Change Dir
     ----------

     This command allows the user to change the current working directory.
     Upon selection of this option the user will see a directory tree
     displayed.  To change the current directory one simply has to click
     on one of the displayed directories, or, by using the arrow keys, move
     through the list and hit enter on the desired directory.  The Revert
     option allows the user to return to the previous directory.

     Dos Shell
     ---------

     This command allows the user to suspend LEDIT and operate at dos level.
     To return to LEDIT type EXIT.

     User Info
     ---------

     This command provides the user with the option of printing out a banner
     page when using the Print Files command.  The banner page lists the
     user's name, the file name, the file time and date, and the file size.
     It should be noted that the printer must set to print out IBM character
     set characters to get the proper text borders.

     Exit  ( Hot Key : <Alt-X> )
     ----

     This command allows the user to quit LEDIT.  If any of the open files
     have been changed without saving the user will be prompted to save
     each one.
       
     				  				  
 Edit Menu
 ---------

     When blocking text for Cut and Copy operations the user can position
     the mouse cursor at the text start, then depress the left mouse key
     and drag the cursor to the desired end of block.  The button can then
     be released and the text will be blocked.  While dragging the cursor
     it can be seen that text is highlighted as the cursor moves.

     Undo
     ----

     This command undoes the last editing operation.

     Cut
     --- 	         

     This command removes the selected text from the active window and 
     places it in the clipboard.

     Copy
     ----

     This command leaves the selected text unchanged but places
     a copy of it into the clipboard.

     Paste
     -----

     This command inserts text from the Clipboard into the active window
     at the current position.

     Show Clipboard
     --------------

     This command opens the Clipboard window.  It is like any other edit
     window except for cutting and pasting.  These operations append the
     text to the bottom of the screen.

     Clear
     -----

     This command removes the selected text but doesn't put it into the
     clipboard.

     Bracket Check ( Hot Key : <Alt-B> )
     -------------

     This command checks to see whether the bracket hierarchy is closed.
     To use this feature a user should demarcate their lisp procedures with
     ;&.  Examples:

          ;&
	  ( defun test ()
	        ........
		........
          )
	  ;&
	  ( defun test2 ()
	        ........
		........
	  )
	  ;&
    
     When Bracket Check is run it will go through each demarcated area
     and indicate if there is a closure error.  An error window will pop
     up and show if a left or right ellipsis is missing.  When the error
     window is closed the corresponding unclosed bracket is highlighted.
     It should be noted that the test only determines whether there is
     ellipsis closure.  There is no testing of positional correctness.
     If there are no errors a success message will be displayed.
     
     Editor Commands
     ---------------
     
     The following is a list of editor commands available to the user:
     
     Basic Movement Commands

     Character Left                    Ctrl-S or Left Arrow
     Character Right                   Ctrl-D or Right Arrow
     Word Left                         Ctrl-A or Ctrl-Left Arrow
     Word Right                        Ctrl-F or Ctrl-Right Arrow
     Line Up                           Ctrl-E or Up Arrow
     Line Down                         Ctrl-X or Down Arrow
     Page Up                           Ctrl-R or PgUp
     Page Down                         Ctrl-C or PgDn
    
     Extended Movement Commands
    
     Beginning of Line                 Ctrl-Q S or Home
     End of Line                       Ctrl-Q D or End
     Top of File                       Ctrl-Q R or Ctrl-PgUp
     End of File                       Ctrl-Q C or Ctrl-PgDn
    
     Insert and Delete Commands
    
     Insert Mode On/Off                Ctrl-V or Ins
     Insert Line                       Ctrl-N
     Delete Line                       Ctrl-Y
     Delete to End of Line             Ctrl-Q Y
     Delete Character Left of Cursor   Ctrl-H or Backspace  
     Delete Character Under Cursor     Ctrl-G or Del
     Delete Word Right of Cursor       Ctrl-T
    
     Block Commands
    
     Mark Block-Begin                  Ctrl-K B or F7
     Mark Block-End                    Ctrl-K K or F8
     Hide/Display Block                Ctrl-K H
     Move Block                        Ctrl-K V
    
     Miscellaneous Commands
    
     Autoindent On/Off                 Ctrl-O I or Ctrl-Q I
     Control Character Prefix          Ctrl-P
     Find                              Ctrl-Q F
     Find and Replace                  Ctrl-Q A
     Invoke Main Menu                  F10
     Load File                         F3
     Restore Line                      Ctrl-Q L
     Save and Edit                     Ctrl-K S or F2
     Tab                               Ctrl-I or Tab
     Tab Mode                          Ctrl-O T or Ctrl-Q T

 Search Menu
 -----------

     Find
     ----

     This command allows the user to search for selected text within the
     active edit window.  The text is entered in the input line in the
     Find dialog box, and several search options can be selected.

     Replace
     -------

     This command allows the user to replace selected text within the active
     window.  The text is entered in the appropriate input line in the
     Replace dialog box along with its replacement text.  Several Replace
     options can also be selected here.

     Search Again
     ------------

     This command allows the user to search for the next occurence of the
     text searched for with Find.

 Windows Menu
 ------------

     Size/Move ( Hot Key: <Ctrl-F5> )
     ---------
     
     This command is described in the LEDIT Windows section.
     
     Zoom ( Hot Key: F5 )
     ----
     
     This command is described in the LEDIT Windows section.
     
     Tile
     ----
     
     This command allows the user to lay out all open windows beside
     each other and sets them at an equal size.  This feature is
     especially useful in jumping from window to window when more than
     two windows are open at a time.
     
     Next
     ----
     
     This command allows the user to jump to the next open window. 
     Repeated use of this command cycles through all open windows in a
     circular sequence.  The active window is always at the fore.
     
     Close ( Hot Key: <Alt-F3> )
     -----
     
     This command will close the current active window.  If the file
     contained within is not saved the user will be prompted for a save.
     
     Indent ( Hot Key: <Alt-I> )
     ------
     
     This command turns the AutoIndent feature on.
     
     Calculator
     ----------
     
     This command activates a basic algebraic calculator.  It can be
     operated with keyboard commands or with the mouse.  Its operation
     is self-evident.
     
     Calendar
     --------
     
     This command activates a pop-up calendar.  The current month 
     corresponding to the system date is displayed by default, but
     other months can be displayed by clicking on the arrowhead icons
     or by using the up and down arrow keys.
     
 DeskTop Menu
 ------------
     
     This feature provides the user with the option of saving the entire
     desktop.  This includes the names and editing positions within all
     open edit windows.
     
     AutoSave On
     -----------
     
     This option, if selected, provides for the automatic saving of the
     desktop environment.  A file ( LEDIT.DSK) is created in the current
     working directory which stores all data necessary for a restoration
     of the desktop when an editing session is resumed.  If LEDIT is
     restarted in another directory, or if it is erased, the AutoSave
     feature is turned off and the user must reactivate it if so desired.
     Most users will find it convenient to have this option enabled.
     
     AutoSave Off
     ------------
     
     This command turns off the AutoSave feature so that when the user
     reenters LEDIT the previous desktop is not restored.
     
 Help Menu
 ---------
 
     General Help ( Hot Key : F1 )
     ------------
     
     This command will activate a help window which provides explanations
     and examples of AutoLisp functions.  The intial help screen and all
     secondary screens are organized on an alphabetic basis.  The user
     can select a key letter by mouse selection or by using the TAB key
     to move through the help screen and hitting enter to select.  The help
     window can be disposed of by hitting ESC or clicking the mouse on the
     close icon.  While in the help screen the user can always move back
     to the previous help screen by selecting the PREVIOUS HELP SCREEN
     option.
     
     About
     -----
     
     This command provides some general information about LEDIT.    
     
  
     Registration:
     ------------
     
     If you find this program of value and continue to make use of it
     please register by sending your name and address along with
     a registration fee ( made payable to KJ Software) of $30 if you
     are in the United States, or $35 if you are in Canada to :
           
           KJ Software
           Metropolitan Business Center
           1222 Alberta Ave.
           Saskatoon, Sask.
           S7K 1R4
           Canada     
           Attn: Floyd Kuechle

     Registered users will be eligible for a substantial discount on 
     future enhanced versions of LEDIT.        