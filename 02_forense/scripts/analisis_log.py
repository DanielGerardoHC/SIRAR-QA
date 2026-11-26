import json
import sys
from collections import Counter, defaultdict
from datetime import datetime, timedelta
from pathlib import Path

log_path = Path(sys.argv[1])
out_csv = Path(sys.argv[2])

events = []
for n, line in enumerate(log_path.read_text(encoding="utf-8").splitlines(), start=1):
    if not line.strip():
        continue
    e = json.loads(line)
    e["_linea"] = n
    e["_ts"] = datetime.fromisoformat(e["timestamp"])
    e["_cuenta"] = e.get("usuario") or e.get("usuario_intentado")
    events.append(e)

print("== Resumen")
print("eventos:", len(events))
print("primer evento:", events[0]["timestamp"], "linea", events[0]["_linea"])
print("ultimo evento:", events[-1]["timestamp"], "linea", events[-1]["_linea"])
print("orden cronologico:", all(a["_ts"] <= b["_ts"] for a, b in zip(events, events[1:])))
print("zona horaria declarada:", any("+" in e["timestamp"] or e["timestamp"].endswith("Z") for e in events))

print("\n== Niveles")
print(Counter(e["nivel"] for e in events))

print("\n== Eventos por cuenta")
for k, v in Counter(e["_cuenta"] for e in events).most_common():
    print(f"  {k}: {v}")

print("\n== Eventos por IP")
for k, v in Counter(e["ip"] for e in events).most_common():
    print(f"  {k}: {v}")

print("\n== Cuentas por IP")
ip_users = defaultdict(set)
for e in events:
    ip_users[e["ip"]].add(e["_cuenta"])
for ip, users in sorted(ip_users.items()):
    print(f"  {ip}: {sorted(users)}")

print("\n== Primera y ultima aparicion por cuenta")
seen = defaultdict(list)
for e in events:
    seen[e["_cuenta"]].append(e)
for k, lst in seen.items():
    print(f"  {k}: {lst[0]['timestamp']} -> {lst[-1]['timestamp']} ({len(lst)})")

print("\n== Endpoints")
for k, v in Counter(e["endpoint"] for e in events).most_common():
    print(f"  {k}: {v}")

print("\n== Campos presentes por tipo de evento")
shapes = Counter()
for e in events:
    keys = tuple(sorted(k for k in e if not k.startswith("_")))
    shapes[(e["mensaje"], keys)] += 1
for (msg, keys), v in shapes.items():
    print(f"  {msg} x{v}: {list(keys)}")

print("\n== Intervalos sin registros mayores a 12 horas")
for a, b in zip(events, events[1:]):
    gap = b["_ts"] - a["_ts"]
    if gap > timedelta(hours=12):
        print(f"  {a['timestamp']} (linea {a['_linea']}) -> {b['timestamp']} (linea {b['_linea']}): {gap}")

print("\n== Consultas autenticadas sin inicio de sesion previo en el registro")
logged = set()
for e in events:
    if e["mensaje"] == "login exitoso":
        logged.add(e["_cuenta"])
    elif e["nivel"] == "INFO" and e["_cuenta"] not in logged and e["endpoint"] != "GET /api/modulos":
        print(f"  linea {e['_linea']} {e['timestamp']} {e['_cuenta']} {e['endpoint']}")

print("\n== Eventos WARN y eventos de escritura")
for e in events:
    if e["nivel"] != "INFO" or e["endpoint"].startswith(("POST /api/matricula", "PUT ")):
        extra = {k: v for k, v in e.items() if k in ("cuerpo_peticion", "origen_cliente", "intento_numero", "nota_anterior_observada_en_backup")}
        print(f"  linea {e['_linea']} {e['timestamp']} {e['nivel']} {e['_cuenta']} {e['ip']} {e['endpoint']} {extra}")

print("\n== Secuencia desde la IP 10.20.9.77")
seq = [e for e in events if e["ip"] == "10.20.9.77"]
for a, b in zip(seq, seq[1:]):
    print(f"  {a['timestamp']} -> {b['timestamp']}: {(b['_ts'] - a['_ts']).total_seconds():.0f} s")
if seq:
    print("  duracion total:", seq[-1]["_ts"] - seq[0]["_ts"])

print("\n== Actividad diaria")
for k, v in sorted(Counter(e["_ts"].date().isoformat() for e in events).items()):
    print(f"  {k}: {v}")

with out_csv.open("w", encoding="utf-8") as f:
    f.write("linea,timestamp,nivel,cuenta,ip,endpoint,mensaje,cliente\n")
    for e in events:
        f.write(",".join([str(e["_linea"]), e["timestamp"], e["nivel"], e["_cuenta"], e["ip"],
                          e["endpoint"], e["mensaje"], e.get("origen_cliente", "")]) + "\n")
