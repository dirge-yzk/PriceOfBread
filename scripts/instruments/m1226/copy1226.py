# (1226) 合流の写し（バイトのまま＝CR と BOM を保つ）：読む線 rb (1224)・設計の材料 nf (1225)・恒等 cu の突然変異の記録・tc の突然変異と探索の記録。
import os, shutil, sys
R = 'D:/PoB_work/wt_m1226'
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(src, 'rb') as f: b = f.read()
    with open(dst, 'wb') as g: g.write(b)
    return len(b)
n = 0; tot = 0
def go(src, dst):
    global n, tot
    tot += cp(src, dst); n += 1
# rb (1224)
W = 'D:/PoB_work/r1224_work'
for x in os.listdir(W + '/r1224'):
    go(f'{W}/r1224/{x}', f'{R}/scripts/instruments/r1224/{x}')
for d in ('out', 'q'):
    for x in os.listdir(f'{W}/{d}'):
        go(f'{W}/{d}/{x}', f'{R}/measurements/m1224_20261119/{d}/{x}')
for x in ('result.txt', 'plan.txt', 'run_probe.sh', 'run_probe2.sh', 'run_f0.log', 'run_f1.log'):
    go(f'{W}/{x}', f'{R}/measurements/m1224_20261119/{x}')
# nf (1225)
W = 'D:/PoB_work/r1225_work'
for x in os.listdir(W + '/r1225'):
    go(f'{W}/r1225/{x}', f'{R}/scripts/instruments/r1225/{x}')
for x in os.listdir(W + '/out'):
    go(f'{W}/out/{x}', f'{R}/measurements/m1225_20261119/out/{x}')
# cu (1220) の突然変異の記録（.gitignore の *.log で落ちていた＝.txt へ）
for x in os.listdir('D:/PoB_work/wt_c123cu/measurements/m1220_20261119/mut'):
    go(f'D:/PoB_work/wt_c123cu/measurements/m1220_20261119/mut/{x}', f'{R}/measurements/m1220_20261119/mut/{x[:-4]}.txt')
# tc (1221) の突然変異・番人・探索の記録
W = 'D:/PoB_work/c123tc_work'
for x in sorted(os.listdir(W)):
    p = f'{W}/{x}'
    if os.path.isfile(p) and (x.endswith('.txt') or x.endswith('.py') or x.endswith('.sh')):
        go(p, f'{R}/measurements/m1221_20261119_mut/{x}')
for x in sorted(os.listdir(W + '/mut')):
    p = f'{W}/mut/{x}'
    if os.path.isfile(p) and os.path.getsize(p) < 3_000_000:
        go(p, f'{R}/measurements/m1221_20261119_mut/mut/{x}')
print(n, 'files', tot, 'bytes')
