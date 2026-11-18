# (1212) 合流：読む線 c121rn (1210)・設計の材料の線 c121ra (1211) の作業フォルダをリポジトリへ写す（中身はバイトのまま・同じ写しは写さない）
import os, shutil, filecmp, sys
R = sys.argv[1]  # 木の根
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst); print('copy', dst[len(R)+1:])
W = 'D:/PoB_work/r1210_work'
same = {'ledger_check.py': 'r1189', 'dl_readers.py': 'r1189', 'dl_table.py': 'r1189', 'mk_tsv_s21.py': 'r1203'}
for f in sorted(os.listdir(W + '/scripts')):
    if not os.path.isfile(W + '/scripts/' + f): continue
    if f in same and filecmp.cmp(W + '/scripts/' + f, f'{R}/scripts/instruments/{same[f]}/{f}', shallow=False):
        print('skip(同じ)', f, '=', same[f]); continue
    cp(W + '/scripts/' + f, f'{R}/scripts/instruments/r1210/{f}')
M = f'{R}/measurements/m1210_20261118'
for f in ['plan.txt', 'result.txt', 'log_1201_1204.txt']:
    cp(f'{W}/{f}', f'{M}/{f}')
for q in ['out', 'q3']:
    for f in sorted(os.listdir(f'{W}/{q}')):
        if f.endswith('.done') or f.endswith('.mark') or not os.path.isfile(f'{W}/{q}/{f}'): continue
        cp(f'{W}/{q}/{f}', f'{M}/{q}/{f}')
T = 'D:/PoB_work/r1211_work'
for f in sorted(os.listdir(T + '/scripts')):
    if os.path.isfile(f'{T}/scripts/{f}'): cp(f'{T}/scripts/{f}', f'{R}/scripts/instruments/r1211/{f}')
for f in sorted(os.listdir(T + '/out')):
    if f.startswith('summary'): cp(f'{T}/out/{f}', f'{R}/measurements/m1211_20261118/{f}')
D = f'{R}/docs/202611/20261118'
cp(f'{T}/material.md', f'{D}/20261118_支配の力のA_設計_材料.md')
cp(f'{T}/draft.md', f'{D}/20261118_支配の力のA_設計_下書き.md')
