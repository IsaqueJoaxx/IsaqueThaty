perfil : dialog {
  label = "Perfil                www.dwgsoft.com.br";
  : row {
    fixed_width = true;
    : boxed_column {
      label = "Gerar Perfil:";
      : text {
        label = "do Projeto";
      }
      : text {
        label = "da Rocha Sã";
      }
      : text {
        label = "da Rocha Decomposta";
      }
      : text {
        label = "do Terreno Natural";
      }
      : text {
        label = " ";
      }
      : text {
        label = "ou Selecionar Objeto";
      }
      : text {
        label = "[Plotagem] ESCALA 1:";
      }
    }
    : boxed_column {
      label = "Layer:";
      : edit_box {
        edit_width = 15;
        key = "lprj";
      }
      : edit_box {
        edit_width = 15;
        key = "lsa";
      }
      : edit_box {
        edit_width = 15;
        key = "ldec";
      }
      : edit_box {
        edit_width = 15;
        key = "ltn";
      }
      : edit_box {
        edit_width = 15;
        key = "ltns";
      }
      spacer;
      : toggle {
        label = "Select";
        key = "sel";
      }
      :row {
        : edit_box {
          edit_width = 7;
          key = "escl";
        }
        : text {
          label = "         ";
        }
      }
      spacer;
    }
  }
  : boxed_column {
    label = "Estaca/Distância na Linha de Referência:";
    : row {
      : toggle {
        label = "Est.0--------1--------2--";
        key = "est0";
      }
      : toggle {
        label = "0--------20--------40m";
        key = "estm";
      }
    }
      :row {
        : edit_box {
          edit_width = 3;
          key = "inie";
        }
        : text {
          label = "Início na EST.0";
        }
        : edit_box {
          edit_width = 3;
          key = "inim";
        }
        : text {
          label = "Início em 0     ";
        }
      }
  }
  : boxed_column {
   label = "Título e Símbolo de Corte:";
   : row {
    : toggle {
      label = "SEÇÃO A-A  ";
      key = "abrir";
    }
      : toggle {
        label = "Inserir _A";
        key = "cvct";
      }
        : edit_box {
          label = "Esc.1:";
          edit_width = 5;
          key = "escsc";
        }
   }
   spacer;
  }
  : row {
    : button {
      label = "Ajuda";
      mnemonic = "A";
      key = "ajuda";
      fixed_width = true;
    }
    ok_cancel;
    : button {
      label = "Projeto";
      mnemonic = "A";
      key = "lcprj";
    }
  }
}
