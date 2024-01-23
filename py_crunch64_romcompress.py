# SPDX-FileCopyrightText: 2024 Dragorn421
# SPDX-License-Identifier: CC0-1.0


USE_CRUNCH64_PYTHON_BINDINGS = False
USE_CRUNCH64_C_BINDINGS = not USE_CRUNCH64_PYTHON_BINDINGS


if USE_CRUNCH64_PYTHON_BINDINGS:
    import crunch64
import struct
import dataclasses
import sys
from pathlib import Path
from tqdm import tqdm
import ctypes
import pickle


if USE_CRUNCH64_C_BINDINGS:
    compress_seconds_vals = []

    cwrapcrunch64_dll = ctypes.CDLL("./cwrapcrunch64.so")
    # void *cwrapcrunch64_yaz0_compress(size_t src_size, const void *src, size_t *res_size_p, double *compress_seconds_p)
    cwrapcrunch64_dll.cwrapcrunch64_yaz0_compress.argtypes = [
        ctypes.c_size_t,
        ctypes.c_void_p,
        ctypes.POINTER(ctypes.c_size_t),
        ctypes.POINTER(ctypes.c_double),
    ]
    cwrapcrunch64_dll.cwrapcrunch64_yaz0_compress.restype = ctypes.c_void_p

    cwrapcrunch64_dll.cwrapcrunch64_free.argtypes = [ctypes.c_void_p]
    cwrapcrunch64_dll.cwrapcrunch64_free.restype = None

    def pyw_cwrapcrunch64_yaz0_compress(data: bytes):
        src = (ctypes.c_byte * len(data)).from_buffer_copy(data)
        res_size = ctypes.c_size_t()
        compress_seconds = ctypes.c_double()
        res_p = cwrapcrunch64_dll.cwrapcrunch64_yaz0_compress(
            len(data),
            src,
            ctypes.pointer(res_size),
            ctypes.pointer(compress_seconds),
        )
        if res_p is None:
            raise Exception()

        # copy ...
        data_comp = bytes((ctypes.c_byte * res_size.value).from_address(res_p))
        # ... and free
        cwrapcrunch64_dll.cwrapcrunch64_free(res_p)

        compress_seconds_vals.append((len(data), compress_seconds.value))

        return data_comp


STRUCT_IIII = struct.Struct(">IIII")


@dataclasses.dataclass
class DmaEntry:
    vromStart: int
    vromEnd: int
    romStart: int
    romEnd: int

    def __repr__(self):
        return (
            "DmaEntry("
            f"vromStart=0x{self.vromStart:08X}, "
            f"vromEnd=0x{self.vromEnd:08X}, "
            f"romStart=0x{self.romStart:08X}, "
            f"romEnd=0x{self.romEnd:08X}"
            ")"
        )

    def to_bin(self, data: memoryview):
        STRUCT_IIII.pack_into(
            data,
            0,
            self.vromStart,
            self.vromEnd,
            self.romStart,
            self.romEnd,
        )

    @staticmethod
    def from_bin(data: memoryview):
        return DmaEntry(*STRUCT_IIII.unpack_from(data))


in_rom = Path(sys.argv[1])
out_rom = Path(sys.argv[2])

data = memoryview(in_rom.read_bytes())

dmadata_start_offset = 0x7430
dmadata_len = 1526

skip_compress = set(
    [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]
    + [15, 16, 17, 18, 19, 20, 21, 22, 23]
    + [24, 25, 26, 942, 944, 946, 948, 950, 952]
    + [954, 956, 958, 960, 962, 964, 966, 968, 970, 972]
    + [974, 976, 978, 980, 982, 984, 986, 988, 990]
    + [992, 994, 996, 998, 1000, 1002, 1004, 1510, 1511, 1512]
    + [1513, 1514, 1515, 1516, 1517, 1518, 1519, 1520, 1521]
    + [1522, 1523, 1524, 1525]
)


def align_romEnd(v: int):
    v += 0xF
    return v // 0x10 * 0x10


class Rom:
    def __init__(self):
        self.files: list[tuple[DmaEntry, bytes]] = []

    def add_file(self, vromStart: int, vromEnd: int, compressed: bool, data: bytes):
        if self.files:
            prev_dma_entry, prev_data = self.files[-1]
            romStart = align_romEnd(prev_dma_entry.romStart + len(prev_data))
        else:
            romStart = 0
        if compressed:
            romEnd = romStart + len(data)
        else:
            romEnd = 0
        self.files.append((DmaEntry(vromStart, vromEnd, romStart, romEnd), data))

    def mkrom(self, dmadata_romStart: int, dmadata_romEnd: int):
        rom_data = bytearray(align_romEnd(self.files[-1][0].romEnd))
        dmadata_data = memoryview(bytearray(dmadata_romEnd - dmadata_romStart))
        dmadata_offset = 0
        prev_romEnd = 0
        for dma_entry, data in self.files:
            print(dma_entry, hex(len(data)))
            assert prev_romEnd <= dma_entry.romStart, (hex(prev_romEnd), dma_entry)

            dma_entry.to_bin(dmadata_data[dmadata_offset:])
            dmadata_offset += STRUCT_IIII.size
            romEnd = dma_entry.romStart + len(data)
            rom_data[dma_entry.romStart : romEnd] = data

            prev_romEnd = romEnd

        rom_data[dmadata_romStart:dmadata_romEnd] = dmadata_data
        return rom_data


offset = dmadata_start_offset

rom = Rom()

for i in tqdm(range(dmadata_len)):
    dma_entry = DmaEntry.from_bin(data[offset:])
    offset += STRUCT_IIII.size
    if dma_entry.vromStart == dma_entry.vromEnd:
        assert dma_entry.vromStart == dma_entry.vromEnd == 0, dma_entry
        continue
    if dma_entry.romStart == dmadata_start_offset:
        dmadata_dma_entry = dma_entry
    assert dma_entry.romEnd == 0, dma_entry
    data_uncompressed = data[
        dma_entry.romStart : (
            dma_entry.romStart + (dma_entry.vromEnd - dma_entry.vromStart)
        )
    ]
    if i not in skip_compress:
        if USE_CRUNCH64_PYTHON_BINDINGS:
            data_compressed = crunch64.yaz0.compress(bytes(data_uncompressed))
        if USE_CRUNCH64_C_BINDINGS:
            data_compressed = pyw_cwrapcrunch64_yaz0_compress(bytes(data_uncompressed))
        data_compressed += b"\x00" * (
            align_romEnd(len(data_compressed)) - len(data_compressed)
        )
        data_write = data_compressed
        compressed = True
    else:
        data_write = data_uncompressed
        compressed = False
    assert len(data_write) % 0x10 == 0, (hex(len(data_write)), compressed, dma_entry)
    rom.add_file(dma_entry.vromStart, dma_entry.vromEnd, compressed, data_write)

out_data = rom.mkrom(
    dmadata_start_offset,
    dmadata_start_offset + dmadata_len * STRUCT_IIII.size,
)
start = len(out_data)
end = 32 * 1024 * 1024
matching_padding = bytearray(end - start)
for i in range(end - start):
    matching_padding[i] = (start + i) % 256
out_rom.write_bytes(out_data + matching_padding)

# python3 -m cProfile -o cprof_pycomp.txt ./py_crunch64_romcompress.py oot_u10.decompressed.z64 oot_u10.recompressed_py.z64

if USE_CRUNCH64_C_BINDINGS:
    with Path("compress_seconds_vals.pickle").open("wb") as f:
        pickle.dump(compress_seconds_vals, f)
