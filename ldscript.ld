SECTIONS {
    .text 0x80000000 : AT(0) {
        build/src/test.o (.text)
    }

    /DISCARD/ : {
        *(*);
    }
}