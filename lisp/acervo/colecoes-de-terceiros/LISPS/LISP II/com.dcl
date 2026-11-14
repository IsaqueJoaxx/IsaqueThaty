dcl_settings : default_dcl_settings { audit_level = 3; }
exit_button : button 
{ label           = "Exit";
  key             = "exit";
  is_cancel       = true;
  width        = 9;
  fixed_width = true;
  aspect_ratio = 0;
  mnemonic        = "E";
}
about_button : button 
{ label           = "Info";
  key             = "about";
  width        = 9;
  fixed_width = true;
  aspect_ratio = 0;
}
assist_button : button 
{ label           = "Help";
  key             = "assist";
  width        = 9;
  fixed_width = true;
  aspect_ratio = 0;
}

exit_about_assist : column 
{ : row 
  { alignment = centered;
    exit_button;
    : spacer { width = 2; }
    about_button;
    : spacer { width = 2; }
    assist_button;
  }
}

main : dialog 
{ label = "AutoCAD Scientific Calculator";
  : boxed_column 
  { : boxed_row 
    { : text 
      {  key = "disp";
         width = 35;
      }
    } 
  } 
  spacer;
  : boxed_column 
  { : row 
    { fixed_width = true;
      : button 
      { label        = "2ndf";
        key          = "shf";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "kin";
        key          = "ki";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "kout";
        key          = "ko";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "root";
        key          = "root";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "mod";
        key          = "mod";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "sqrt";
        key          = "sqrt";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "sqr";
        key          = "sqr";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "log";
        key          = "log";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "ln";
        key          = "ln";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "x^y";
        key          = "xpy";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "sexa";
        key          = "sexa";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "hyp";
        key          = "hyp";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "tan";
        key          = "tan";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "sin";
        key          = "sin";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "cos";
        key          = "cos";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "ab/c";
        key          = "frc";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "+/-";
        key          = "pm";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "back";
        key          = "ers";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "Min";
        key          = "mi";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "MR";
        key          = "mo";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    spacer_1;
  }
  : boxed_column 
  { : row 
    { fixed_width = true;
      : button 
      { label        = "7";
        key          = "7";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "8";
        key          = "8";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label = "9";
        key          = "9";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "C";
        key          = "C";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "AC";
        key          = "AC";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "4";
        key          = "4";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "5";
        key          = "5";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "6";
        key          = "6";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "x";
        key          = "x";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "/";
        key          = "/";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "1";
        key          = "1";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "2";
        key          = "2";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "3";
        key          = "3";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "+";
        key          = "+";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "-";
        key          = "-";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "0";
        key          = "0";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = ".";
        key          = ".";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "EXP";
        key          = "exp";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "=";
        key          = "=";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "SD";
        key          = "sd";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    spacer_1;
  }
  : boxed_row 
  { exit_about_assist; }
  : errtile { width = 30;}
  spacer;
}

//Second function keys
second : dialog 
{ label = "AutoCAD Scientific Calculator";
  : boxed_column 
  { : boxed_row 
    { : text 
      {  key = "disp";
         width = 35;
      }
    } 
  } 
  : boxed_column 
  { : row 
    { fixed_width = true;
      : button 
      { label        = "1stf";
        key          = "1stf";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "cnst";
        key          = "cnst";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "alog";
        key          = "alog";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "aln";
        key          = "aln";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "KAC";
        key          = "kac";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "asex";
        key          = "asex";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "ran#";
        key          = "rand";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "atan";
        key          = "atan";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "asin";
        key          = "asin";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "acos";
        key          = "acos";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "<-";
        key          = "engl";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "->";
        key          = "engr";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "R->P";
        key          = "rtp";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "P->R";
        key          = "ptr";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "X-K";
        key          = "xk";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "alrm";
        key          = "beep";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "cubt";
        key          = "cubt";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "x!";
        key          = "fact";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "X-Y";
        key          = "xy";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "M-";
        key          = "M-";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "cvrt";
        key          = "cvrt";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "d/c";
        key          = "d/c";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "1/x";
        key          = "rcp";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "kbrd";
        key          = "kbrd";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "M+";
        key          = "M+";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
  }
  spacer;
  : boxed_column 
  { : row 
    { fixed_width = true;
      : button 
      { label        = "7";
        key          = "7";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "8";
        key          = "8";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label = "9";
        key          = "9";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "C";
        key          = "C";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "AC";
        key          = "AC";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "4";
        key          = "4";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "5";
        key          = "5";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "6";
        key          = "6";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "x";
        key          = "x";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "/";
        key          = "/";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "1";
        key          = "1";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "2";
        key          = "2";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "3";
        key          = "3";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "+";
        key          = "+";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "-";
        key          = "-";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "0";
        key          = "0";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = ".";
        key          = ".";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "EXP";
        key          = "exp";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "=";
        key          = "=";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
      : button 
      { label        = "SD";
        key          = "sd";
        fixed_width  = true;
        width        = 8;
        aspect_ratio = 0;
      }
    }
    spacer_1;
  }
  : boxed_row 
  { exit_about_assist; }
  : errtile { width = 30;}
  spacer;
}

//Mode control dialog
mode : dialog 
{ label = "Calculator Mode Control";     
  : boxed_row 
  { : row 
    { : boxed_column 
      { label = "Angular mode:";
        : radio_column 
        { : radio_button 
          { key      = "degree";
            label    = "Degree mode";
            mnemonic = "D";
          }
          : radio_button 
          { key      = "radian";
            label    = "Radian mode";
            mnemonic = "R";
          }
          : radio_button 
          { key      = "gradian";
            label    = "Gradian mode";
            mnemonic = "G";
          }
       }
       : boxed_column 
       { label = "Active Mode:";
         : text 
         { key   = "ang_mode";
           width = 15;
         }
       }
       spacer_0;
    }
    spacer_0;
    : boxed_column 
    { label = "Display mode:";
      : radio_column 
      { : radio_button 
        { key      = "fix";
          label    = "Fix mode";
          mnemonic = "F";
        }
        : radio_button 
        { key      = "scientific";
          label    = "Scientific mode";
          mnemonic = "S";
        }
        : radio_button 
        { key      = "default";
          label    = "Standard mode";
          mnemonic = "t";
        }
      }
      : boxed_column 
      { label = "Active Mode:";
        : text 
        { key   = "dis_mode";
          width = 15;
        }
      }
      spacer_0;
    }
   }
  }
  spacer;
  ok_cancel_help;
  spacer;
}

scip : dialog 
{ label = "Sci Mode Precision Control";
  : boxed_row 
  { : row 
    { : edit_box 
      { label        = "Input Precision:";
        key          = "scn";
        edit_width   = 12;
        allow_accept = true;
        mnemonic     = "I";
       }
    }
  }
  spacer_1;
  ok_cancel;
  : errtile { width = 22;}
}

fixp : dialog 
{ label = "Fix Mode Precision Control";
  : boxed_row 
  { : row 
    { : edit_box 
      { label        = "Input Precision:";
        key          = "fxn";
        edit_width   = 12;
        allow_accept = true;
        mnemonic     = "I";
      }
    }
  }
  spacer_1;
  ok_cancel;
  : errtile { width = 22;}
}

//Table dialog
table : dialog 
{ label = "Scientific Constants Table";     
  : paragraph 
  { : text_part 
    { label = "  Note:  The values of these scientific constants are based"; }
    : text_part 
    { label = "            on JIS-Z-8202-1988 (JIS=Japan Industrial Standard)"; }
    spacer_1;
    : text_part 
    { label = "Const. no.   Symbol        Name                     Numerical value            Unit"; }
  }
  : list_box 
  { tabs = "3 10 20 37 57";
    width = 67;
    height = 15;
    key = "list_box";
    fixed_width = true;
    allow_accept = true;
    tab_truncate = true;
  }
  : ok_button {is_cancel = true; }
}

info : dialog 
{ label = "";
  key = "title";
  : row 
  { : image
    { key          = "logo";
      fixed_height = true;
      fixed_width  = true;
      height = 4;
      width  = 10;
      color  = dialog_background;
    }
    : paragraph 
    { : text_part 
      { key   = "text1";
        width = 50;
      }
      : text_part 
      { key   = "text2";
        width = 50;
      }
      : text_part 
      { key   = "text3";
        width = 50;
      }
      : text_part 
      { key   = "text4";
        width = 50;
      }
    }
  }
  spacer;
  : list_box 
  { width  = 55;
    key    = "list_box";
  }
  : ok_button {is_cancel = true;}
}


//Memory input/ouput dialog
mem : dialog 
{ label   = "";
  key     = "header";
  : boxed_column 
  { : row 
    { : button 
      { label        = "a";
        key          = "a";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "b";
        key          = "b";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "c";
        key          = "c";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "d";
        key          = "d";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "e";
        key          = "e";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { : button 
      { label        = "f";
        key          = "f";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "g";
        key          = "g";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "h";
        key          = "h";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "i";
        key          = "i";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "j";
        key          = "j";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { : button 
      { label        = "k";
        key          = "k";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "l";
        key          = "l";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "m";
        key          = "m";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "n";
        key          = "n";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "o";
        key          = "o";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { : button 
      { label        = "p";
        key          = "p";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "q";
        key          = "q";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "r";
        key          = "r";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "s";
        key          = "s";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "t";
        key          = "t";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { : button 
      { label        = "u";
        key          = "u";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "v";
        key          = "v";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "w";
        key          = "w";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "x";
        key          = "x";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "y";
        key          = "y";
        width        = 4;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
  }
  spacer;
  : row 
  { fixed_width = true;
    alignment = centered;
    : retirement_button 
    { label = "Display";
      key   = "display";
    }
    cancel_button;
    help_button;
  }
  spacer;
}

//Memory display dialog
memlist : dialog 
{ label = "Memory list Table";     
  spacer_1;
  : column 
  { : list_box 
    { label = " ADDRESS:           VALUE:" ;
      tabs = "11 16";
      width = 35;
      key = "mem_list";
      fixed_width = true;
      tab_truncate = true;
    }
  }
  spacer;
  : ok_button {is_cancel = true;}
  spacer;
}

//Hyperbolic function dialog
hyper : dialog 
{ label = "Hyperbolic Functions";
  : boxed_column 
  { : row 
    { fixed_width = true;
      : button 
      { label        = "tanh";
        key          = "tanh";
        width        = 9;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "sinh";
        key          = "sinh";
        width        = 9;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "cosh";
        key          = "cosh";
        width        = 9;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "iTanh";
        key          = "itanh";
        width        = 9;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "iSinh";
        key          = "isinh";
        width        = 9;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "iCosh";
        key          = "icosh";
        width        = 9;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
  }
  spacer;
  : row 
  {  fixed_width = true;
     alignment = centered;
     cancel_button;
     : spacer { width = 2; }
     help_button;
  }
  spacer;
}


//Standard deviation dialog
sd : dialog 
{ label = "Std deviation keys";
  : boxed_column 
  { : row 
    { fixed_width = true;
      : button 
      { label        = "sx2";
        key          = "sqr2";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "sx";
        key          = "dts";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "cnt";
        key          = "dtn";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "del";
        key          = "del";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
    : row 
    { fixed_width = true;
      : button 
      { label        = "ave";
        key          = "ave";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "sdxn";
        key          = "strd1";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "sdx";
        key          = "strd0";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
      : button 
      { label        = "data";
        key          = "data";
        width        = 8;
        fixed_width  = true;
        aspect_ratio = 0;
      }
    }
  }
  spacer;
  : row 
  { fixed_width = true;
    alignment = centered;
    : retirement_button 
    { label    = "File...";
      key      = "file";
      mnemonic = "F";
    }
    spacer;
    cancel_button;
    spacer;
    help_button;
  }
  spacer;
}

//Scientific constant dialog
constant : dialog 
{ label = "Scientific Constants";     
  initial_focus = "const_num";
  : boxed_column 
  { : row 
    { : button 
      { label = "Constant number...";
        key = "const_tab";
        mnemonic = "C";
      }
      : edit_box 
      { key = "const_num";
        width = 10;
        fixed_width = true;
      }
    }
  }
  spacer;
  ok_cancel_help;
  : errtile { width = 22;}
  spacer;
}

//Units display dialog
units : dialog 
{ label = "Calculator Units Table";
  spacer_1;
  : column 
  { : list_box 
    { label = "Choose Desired Unit:" ;
      key = "unit_list";
      width =  20;
      allow_accept = true;
    }
  }
  spacer;
  ok_cancel_help;
  spacer;
}

//Data display dialog
datalist : dialog 
{ label = "Data list Table";  
  spacer_1;
  : column {
    : list_box 
    { label = "Pick Data To Delete:" ;
      key = "data_list";
      width =  25;
      multiple_select = true;
    }
  }
  spacer;
  :  row 
  { fixed_width = true;
    alignment = centered;
    : retirement_button 
    { label = "Clear";
      key   = "clear";
    }
    : spacer { width = 2; }
    ok_button;
    : spacer { width = 2; }
    cancel_button;
  }
  : errtile { width = 25; }
  spacer;
}

//SD data file
sdfile : dialog 
{ label = "Data File";
  : boxed_column 
  { : row 
    { : button 
      { label = "Read Data...";
        key   = "rf";
        mnemonic = "R";
      }
    }
    spacer;
    : row 
    { : button 
      { label = "Save Data...";
        key    = "sf";
        mnemonic = "S";
      }
    }
    spacer;
    : row 
    { : button 
      { label = "Append Data...";
        key    = "af";
        mnemonic = "A";
      }
    }
  }
  spacer;
  cancel_button;
  spacer;
}

//Conversion dialog
convert : dialog 
{ label         = "Calculator Unit Converter";
  initial_focus = "sunt1";
  : boxed_column 
  { : row 
    { : button 
      { label    = "Base Unit...  ";
        key      = "unt1";
        mnemonic = "B";
      }
      : edit_box 
      { key         = "sunt1";
        width       = 17;
        fixed_width = true;
      }
    }
    spacer;
    : row 
    { : button 
      { label    = "Target Unit...";
        key      = "unt2";
        mnemonic = "T";
      }
      : edit_box 
      { key         = "sunt2";
        width       = 17;
        fixed_width = true;
      }
    }
  }
  spacer;
  ok_cancel_help;
  : errtile { width = 22; }
  spacer;
}

com_warn : dialog 
{ label = "Calculator Warning";     
  : boxed_column 
  { : concatenation 
    { : column 
      { : text_part 
        { key    = "msg1";
          width  = 22;
        }    
        : text_part 
        { key    = "msg2";
          width  = 22;
        }    
      }
    }
  }
  spacer_1;
  ok_cancel;
  spacer;
}
