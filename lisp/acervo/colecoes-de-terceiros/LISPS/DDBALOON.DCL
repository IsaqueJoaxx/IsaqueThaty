//
//    
//
//      DDBALOON.DCL 1.1
//
// (c) September 1995, Greg MacGeorge
// DDBALOON.DCL - Used to get input for the DDBALOON.LSP program.



dcl_settings : default_dcl_settings { audit_level = 0; }

balloon : dialog {
    label = "BALLOON";
        :column {
            :image {
                key = "image";
                width = 15;
                aspect_ratio = 0.90;
                }
            :edit_box {
                label = "Ballon Size: ";
                key = "bsize";
                width = 8;
                }
            :edit_box {
                label = "Text Size: ";
                key = "tsize";
                width = 8;
                }
        }
        spacer_1;
        : boxed_column {
        label = "Balloon Type";
           :radio_button {
                key = "sball";
                label = "Single";
                }
           :radio_button {
                key = "babove";
                label = "Above";
                }
            :radio_button {
                key = "bside";
                label = "Side";
                }
            :radio_button {
                key = "bbelow";
                label = "Below";
                }
        }
        spacer_1;
        : boxed_row {
        label = "Messages";
            : text {
                key = "message";
                fixed_width;
                }
        }
        :row {ok_button; cancel_button;}
}
//
//
//
//
registr : dialog {
    label = "DDBALOON.LSP Registration";
        : boxed_column {
        : text {
            label = "                DDBALOON SHAREWARE VERSION 1.1";
            }
        }

        spacer_1;
        : boxed_column {
        : text {
            label = "          This program is NOT a registered version!!";
            }

        :boxed_row {
        :column {
        : text {
            label = "DDBALOON.LSP will become disabled after";
            }}
        :column {
        : text {
            key = "run_count";
            label = "";
            fixed_width = true;
            width = 2;
            }}
        :column {
        : text {
            label = "more executions";
            }}
        }

        spacer_1;
        : text {
            label = "To register: E-MAIL Greg M. MacGeorge 76725,1023";
            }
        spacer_1;
        : text {
            label = "When registration is complete you get:";
            }
        : text {
            label = "1. The latest program version.";
            }
        : text {
            label = "2. Program documentation.";
            }
        : text {
            label = "3. Deletion of this annoying screen.";
            }
        spacer_1;
        : text {
            label = "   $9.95 + $1.50 S/H";
            }
        }
        :row {cancel_button;}
}

