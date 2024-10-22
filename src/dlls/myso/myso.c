#include <stdio.h>

extern char code_string[];
extern char code_string_percents[];

char pad[0xC000];

int bss_value;

void myso_function(void);

void (*fptr)(void) = myso_function;

void myso(void)
{
    printf("This is myso()\n");
    printf("%d\n", bss_value);
    printf(code_string);
    printf(code_string_percents, "hi indirectly");
    printf(code_string_percents, pad);
    myso_function();
    fptr();
}
