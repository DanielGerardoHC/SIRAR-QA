import sqlite3
import struct
import sys
from pathlib import Path

db_path = Path(sys.argv[1]).resolve()
raw = db_path.read_bytes()

header = raw[:100]
page_size = struct.unpack(">H", header[16:18])[0]
page_size = 65536 if page_size == 1 else page_size
page_count = struct.unpack(">I", header[28:32])[0]
freelist_trunk = struct.unpack(">I", header[32:36])[0]
freelist_count = struct.unpack(">I", header[36:40])[0]
change_counter = struct.unpack(">I", header[24:28])[0]
schema_cookie = struct.unpack(">I", header[40:44])[0]
user_version = struct.unpack(">I", header[60:64])[0]
app_id = struct.unpack(">I", header[68:72])[0]
version_valid_for = struct.unpack(">I", header[92:96])[0]
sqlite_version = struct.unpack(">I", header[96:100])[0]

print("== Cabecera")
print("firma:", header[:16])
print("tamano_pagina:", page_size)
print("paginas:", page_count, "bytes_esperados:", page_size * page_count, "bytes_reales:", len(raw))
print("contador_cambios:", change_counter)
print("version_valida_para:", version_valid_for)
print("version_sqlite_escritora:", sqlite_version)
print("cookie_esquema:", schema_cookie)
print("user_version:", user_version, "application_id:", app_id)
print("paginas_libres:", freelist_count, "tronco_lista_libre:", freelist_trunk)
print("wal_shm_presentes:", Path(str(db_path) + "-wal").exists(), Path(str(db_path) + "-shm").exists())

uri = "file:" + db_path.as_posix() + "?mode=ro&immutable=1"
con = sqlite3.connect(uri, uri=True)
cur = con.cursor()

print("\n== Esquema")
for row in cur.execute("SELECT type, name, tbl_name, rootpage, sql FROM sqlite_master ORDER BY rootpage"):
    print(row)

print("\n== Contenido")
for (name,) in cur.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name").fetchall():
    rows = con.execute(f'SELECT * FROM "{name}"').fetchall()
    print(f"-- {name}: {len(rows)} filas")
    for r in rows:
        print("  ", r)

print("\n== Integridad")
print(cur.execute("PRAGMA integrity_check").fetchall())
print("journal_mode:", cur.execute("PRAGMA journal_mode").fetchone())

print("\n== Bytes no asignados con texto residual")
for page in range(1, page_count + 1):
    start = (page - 1) * page_size
    data = raw[start:start + page_size]
    off = 100 if page == 1 else 0
    ptype = data[off]
    if ptype not in (2, 5, 10, 13):
        nonzero = sum(1 for b in data if b)
        print(f"pagina {page}: tipo {ptype} bytes_no_cero {nonzero}")
        continue
    hdr = 12 if ptype in (2, 5) else 8
    ncells = struct.unpack(">H", data[off + 3:off + 5])[0]
    content_start = struct.unpack(">H", data[off + 5:off + 7])[0] or 65536
    first_free = struct.unpack(">H", data[off + 1:off + 3])[0]
    gap = data[off + hdr + 2 * ncells:content_start]
    residual = gap.strip(b"\x00")
    print(f"pagina {page}: tipo {ptype} celdas {ncells} bloque_libre {first_free} hueco {len(gap)} residual {len(residual)}")
    if residual:
        print("   ", residual[:400])
    fb = first_free
    while fb:
        nxt, size = struct.unpack(">HH", data[fb:fb + 4])
        print(f"    bloque libre en {fb} tamano {size}:", data[fb + 4:fb + size].strip(b"\x00")[:300])
        fb = nxt
