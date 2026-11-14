dcl_settings : default_dcl_settings { audit_level = 3; }

xsect : dialog {
   label = "Drawn Section Setup";
   : text {
       key = "xsect_max";
       width = 40;
       value = "Elevation maximum xx.xx";
   }
   : text {
       key = "xsect_min";
       width = 40;
       value = "Elevation minimum xx.xx";
   }
   spacer;
   : boxed_column {
      label = "Draw Vertical Axis";
      : edit_box {
         label = "Top elevation:";
         key = "xsect_top";
         edit_width = 10;
         value = "";
      }
      : edit_box {
         label = "Bottom elevation:";
         key = "xsect_bot";
         edit_width = 10;
         value = "";
      }
      : edit_box {
         label = "Tick interval:";
         key = "xsect_tik";
         edit_width = 10;
         value = "";
      }
      spacer;
   }
   : row {
      spacer;
      : column {
         : edit_box {
            label = "Axis label text height:";
            key = "xsect_txt";
            edit_width = 10;
            value = "";
         }
         : edit_box {
         label = "Section vertical exaggeration factor:";
         key = "xsect_scl";
         edit_width = 10;
         value = "";
         }
      }
      spacer;
   }
   spacer;
   ok_only;
}
