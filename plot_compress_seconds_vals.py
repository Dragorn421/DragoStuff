# SPDX-FileCopyrightText: 2024 Dragorn421
# SPDX-License-Identifier: CC0-1.0

from pathlib import Path
import pickle

import matplotlib.pyplot as plt

plt.switch_backend("WebAgg")


with Path("compress_seconds_vals.pickle").open("rb") as f:
    compress_seconds_vals = pickle.load(f)

print(sum([t for n, t in compress_seconds_vals]))

plt.scatter(
    [n for n, t in compress_seconds_vals],
    [t for n, t in compress_seconds_vals],
)
plt.xlabel("n (bytes)")
plt.ylabel("t (s)")
plt.show()
