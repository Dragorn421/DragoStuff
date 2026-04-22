.include "macro.inc"

.section .text, "ax"

nonmatching __1FA0_textbin

glabel __1FA0_textbin
.incbin "assets/1FA0.textbin.bin"
endlabel __1FA0_textbin
