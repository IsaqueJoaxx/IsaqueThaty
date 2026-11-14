// PDB for editing Dimension text
// use with ddimedit.lsp

text_edit : dialog {
//   label      = "Edit Text";
   key = "box";
   value = "Text";
   fixed_width = true;
   width = 60;
   initial_focus    = "text";
   : edit_box {
      key           = "text";
      edit_limit = 256 ;
      allow_accept  = true;
   }
   ok_cancel;
}
