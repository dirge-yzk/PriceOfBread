# (1198) 合流：読む線 c119ra (1196)・設計の材料の線 c119t2 (1197) の作業フォルダをリポジトリへ写す（中身はバイトのまま・既存の写しと同じ台本は写さない）。
import os, shutil, filecmp, sys
R = sys.argv[1]  # 木の根
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst); print('copy', dst[len(R)+1:])
W = 'D:/PoB_work/r1196_work'
same = {'dl_readers.py': 'r1189', 'ledger_check.py': 'r1189', 'hunkfuncs.py': 'r1169', 'ur.py': 'c118ur'}
for f in sorted(os.listdir(W + '/scripts')):
    if f in same and filecmp.cmp(W + '/scripts/' + f, f'{R}/scripts/instruments/{same[f]}/{f}', shallow=False):
        print('skip(同じ)', f, '=', same[f]); continue
    cp(W + '/scripts/' + f, f'{R}/scripts/instruments/r1196/{f}')
cp(W + '/q1/run_all.sh', f'{R}/scripts/instruments/r1196/run_all.sh')
M = f'{R}/measurements/m1196_20261118'
for f in ['r1196_2026-11-18.md', 'plan.txt']:
    cp(f'{W}/{f}', f'{M}/{f}')
for q in ['q1', 'q2', 'q3']:
    for f in sorted(os.listdir(f'{W}/{q}')):
        if f.endswith('.done') or f == 'run_all.sh': continue
        cp(f'{W}/{q}/{f}', f'{M}/{q}/{f}')
T = 'D:/PoB_work/r1197_work'
for f in sorted(os.listdir(T + '/scripts')):
    cp(f'{T}/scripts/{f}', f'{R}/scripts/instruments/r1197/{f}')
for f in ['out_an7.txt', 'out_an7_s1.txt', 'out_an8.txt']:
    cp(f'{T}/{f}', f'{R}/measurements/m1197_20261118/{f}')
D = f'{R}/docs/202611/20261118'
cp(f'{T}/material.md', f'{D}/20261118_貿易のT2-1_設計_材料.md')
cp(f'{T}/draft.md', f'{D}/20261118_貿易のT2-1_設計_下書き.md')
