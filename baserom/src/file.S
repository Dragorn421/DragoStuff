.set noreorder

.text

.global func
func:
    la $t0, data
    add $t0, $t0, $a0
    lw $v0, 0($t0)
    jr $ra
    nop

data:
    .word 0x02010202
    jal bss_symbol
    .word 0x02030204

.bss

bss_symbol:
    .space 4
