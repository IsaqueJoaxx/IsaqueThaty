
dcl_settings : default_dcl_settings { audit_level = 3; }

fl1 : dialog {
    label = "f1";
    value = "Flatten v1.00";
    spacer_1;
    : boxed_column {
        label = "Select Options";
        key = "selopt";
        mnemonic = "";
        children_alignment = centered;
        children_fixed_width = true;
        width = 35;
        fixed_width = true;
        alignment = centered;
        spacer_1;
        : toggle {
            label = "Include TEXT";
            value = "0";
            key = "inc_text";
            mnemonic = "T";
            width = 20;
            fixed_width = true;
            fixed_height = false;
        }
        : toggle {
            label = "Include LINES";
            value = "0";
            key = "inc_line";
            mnemonic = "L";
            width = 20;
            fixed_width = true;
            fixed_height = false;
        }
        : toggle {
            label = "Include POLYLINES";
            value = "0";
            key = "inc_poly";
            mnemonic = "P";
            width = 20;
            fixed_width = true;
            fixed_height = false;
        }
        spacer_1;
        : toggle {
            label = "Auto AUDIT";
            value = "0";
            key = "inc_audi";
            mnemonic = "A";
            width = 20;
            fixed_width = true;
            fixed_height = false;
    }
    spacer_1;
    }
    : image_button {
        key = "flat_img";
        color = dialog_background;
        width = 25;
        fixed_width = true;
        height = 8;
        alignment = centered;
    }
    spacer_1;
    ok_only;
}

reg1 : dialog {
    label = "reg1";
    value = "Flatten v1.00 Registration";
    key = "reg1";
    initial_focus = "sec_code";
    : spacer {
        key = "spc1";
        width = 36;
        fixed_width = true;
        alignment = centered;
    }
    : boxed_column {
        key = "col1";
        children_alignment = centered;
        children_fixed_width = true;
        width = 18;
        fixed_width = true;
        alignment = centered;
        : edit_box {
            label = "Enter Security code ";
            key = "sec_code";
            mnemonic = "S";
            edit_width = 6;
            edit_limit = 6;
            allow_accept = true;
            width = 15;
            fixed_width = true;
            height = 1;
            alignment = centered;
        }
    }
    spacer_1;
    : image {
        key = "flat_img";
        width = 24;
        color = dialog_background;
        fixed_width = true;
        height = 8;
        alignment = centered;
    }
    ok_only;
}

