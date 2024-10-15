SECTIONS {
    mystrings 0 : {
        * (.data* .rodata*)
    }
    /DISCARD/ : {
        * (*)
    }
}
