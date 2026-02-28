alpha_bleeder.so: alpha_bleeder.c
#	$(CC) -Wall -Wextra -Og -ggdb3 -fsanitize=address $^ -o $@
	$(CC) -Wall -Wextra -O3 -shared $^ -o $@
