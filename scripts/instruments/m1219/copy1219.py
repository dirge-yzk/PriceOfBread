# (1219) 合流：読む線 c122rs (1217)・設計の材料の線 c122r2 (1218) の作業フォルダをリポジトリへ写す（中身はバイトのまま・既存の写しと同じ台本は写さない）。
#   台帳 docs/機能の台帳.md の S13 を (1217) の 57 行へ差し替える（r1217/trial_ledger_s13.py の形をそのまま使い、節の見出しの <日> を 20261118 へ）。
#   python -X utf8 -I scripts/instruments/m1219/copy1219.py <木の根>
import os, shutil, filecmp, sys, subprocess
R = sys.argv[1]
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst); print('copy', dst[len(R)+1:])
W = 'D:/PoB_work/r1217_work'
same = {'cmp_rows.py': 'r1210', 'anchor_hunks.py': 'r1210', 'dl_readers.py': 'r1189', 'dl_table.py': 'r1189'}
for f in sorted(os.listdir(W + '/r1217')):
    if f in same and filecmp.cmp(f'{W}/r1217/{f}', f'{R}/scripts/instruments/{same[f]}/{f}', shallow=False):
        print('skip(同じ)', f, '=', same[f]); continue
    cp(f'{W}/r1217/{f}', f'{R}/scripts/instruments/r1217/{f}')
M = f'{R}/measurements/m1217_20261118'
for f in ['plan.txt', 'result.txt']:
    cp(f'{W}/{f}', f'{M}/{f}')
for q in ['out', 'q']:
    for f in sorted(os.listdir(f'{W}/{q}')):
        if f.endswith('.done') or f.endswith('.mark') or f.endswith('.log'): continue
        cp(f'{W}/{q}/{f}', f'{M}/{q}/{f}')
T = 'D:/PoB_work/r1218_work'
for f in sorted(os.listdir(T + '/scripts/instruments/r1218')):
    cp(f'{T}/scripts/instruments/r1218/{f}', f'{R}/scripts/instruments/r1218/{f}')
for f in ['probe_read.txt', 'probe_class.txt', 'board0.tsv', 'board1.tsv']:
    cp(f'{T}/{f}', f'{R}/measurements/m1218_20261118/{f}')
D = f'{R}/docs/202611/20261119'
cp(f'{T}/material.md', f'{D}/20261119_貿易のB-1のR2_設計_材料.md')
cp(f'{T}/draft.md', f'{D}/20261119_貿易のB-1のR2_設計_下書き.md')
# 台帳
led = f'{R}/docs/機能の台帳.md'; tmp = f'{R}/../m1219_work/ledger_new.md'
tmp = 'D:/PoB_work/m1219_work/ledger_new.md'
r = subprocess.run([sys.executable, '-X', 'utf8', '-I', f'{R}/scripts/instruments/r1217/trial_ledger_s13.py', led, f'{M}/q/ledger_S13_2026-11-18.tsv', tmp], capture_output=True, text=True, encoding='utf-8')
print(r.stdout.strip(), r.stderr.strip()[:300])
b = open(tmp, 'rb').read()
assert b.count('m1217_<日>'.encode()) == 1, b.count('m1217_<日>'.encode())
open(led, 'wb').write(b.replace('m1217_<日>'.encode(), 'm1217_20261118'.encode()))
print('台帳を書いた')
