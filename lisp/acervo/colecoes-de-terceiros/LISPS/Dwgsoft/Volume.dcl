volume : dialog {
label = "Volume.lsp                  www.dwgsoft.com.br";
: row {
fixed_width = true;
: boxed_column {
label = "Referências :";
: edit_box {
label = "Projeto:";
mnemonic = "1";
key = "titu1";
allow_accept = true;
edit_width = 42;
edit_limit = 256;
}
: edit_box {
label = "Estrutura:";
mnemonic = "2";
key = "titu2";
edit_width = 42;
edit_limit = 256;
allow_accept = true;
}
: edit_box {
label = "Material:";
mnemonic = "3";
key = "titu3";
edit_width = 42;
edit_limit = 256;
allow_accept = true;
}
}
}
: row {
fixed_width = true;
: boxed_column {
label = "Estaca 20/20m :";
: edit_box {
label = "Inicial";
mnemonic = "I";
key = "estini";
edit_width = 6;
edit_limit = 256;
allow_accept = true;
}
}
: boxed_column {
label = "Elevação:";
: edit_box {
label = "Inicial";
mnemonic = "l";
key = "elvini";
edit_width = 6;
edit_limit = 256;
allow_accept = true;
}
}
: boxed_column {
label = "Incremento";
spacer;
: toggle {
label = "  Informar   ";
mnemonic = "f";
key = "incre";
}}}
:row {
fixed_width = true;
: boxed_row {
label = "Gráfico C.A.V e arquivo Estacas.inc";
: toggle {
label = "Gráfico ";
mnemonic = "G";
key = "graf";
}
: toggle {
label = "Arquivo";
mnemonic = "A";
key = "arqext";
}
}
: boxed_row {
label = "Fórmula:";
: edit_box {
label = "Fórmula:";
mnemonic = "F";
key = "fmlas";
edit_width = 2;
allow_accept = true;
}
}
}
: boxed_column {
fixed_width = true;
label = "Fórmulas:";
: row {
: text {
label = "1-A1.D   2-(A1+A2)/2 . D     3-T. pirâmide  4- idem 2";
}
}
: row {
: text {
label = "1-A1.D   2-Estaca 20/20m   3- Elevação   4-Média . D";
}
}
}
: row {
: button {
label = "Ajuda";
mnemonic = "A";
key = "ajudavol";
fixed_width = true; 
}
: button {
label = "Fórmulas";
mnemonic = "F";
key = "formudcl";
fixed_width = true;
}
: button {
label = " Infor ";
mnemonic = "I";
key = "infor";
fixed_width = true;
}
}
ok_cancel;
}
