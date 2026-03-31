typedef struct my_struct {
    float value;
} my_struct;

int my_func(my_struct* ctx) {
#ifdef EXPECTED
    return ctx->value < 60.0f;
#else
    int var_v0;
    var_v0 = 0;
    if (ctx->value < 60.0f) {
        return 1;
    }
    return var_v0;
#endif
}

void padding(my_struct* ctx) {
    ctx->value = 0;
}
