cwrapcrunch64.so: cwrapcrunch64.c
	gcc -fPIC -shared -Icrunch64-staticlib-x86_64-unknown-linux-gnu/include cwrapcrunch64.c -o cwrapcrunch64.so -L./crunch64-staticlib-x86_64-unknown-linux-gnu/lib/ -l:libcrunch64.a
