# (1205) 合流：読む線 c120rh (1203)・設計の材料の線 c120mb (1204) の作業フォルダと、k11 の突然変異 M1 のログをリポジトリへ写す（中身はバイトのまま・既存の写しと同じ台本は写さない）。
import os, shutil, filecmp, sys
R = sys.argv[1]  # 木の根
def cp(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    shutil.copyfile(src, dst); print('copy', dst[len(R)+1:])
W = 'D:/PoB_work/r1203_work'
same = {'dl_readers.py': 'r1189', 'ledger_check.py': 'r1189', 'hunkfuncs.py': 'r1169', 'cmp_rows.py': 'r1196', 'codediff.py': 'r1189'}
for f in sorted(os.listdir(W + '/scripts')):
    if f in same and os.path.exists(f'{R}/scripts/instruments/{same[f]}/{f}') and filecmp.cmp(W + '/scripts/' + f, f'{R}/scripts/instruments/{same[f]}/{f}', shallow=False):
        print('skip(同じ)', f, '=', same[f]); continue
    cp(W + '/scripts/' + f, f'{R}/scripts/instruments/r1203/{f}')
M = f'{R}/measurements/m1203_20261118'
for f in ['plan.txt', 'harness_lines.txt', 'ledger_S21_2026-11-12.md', 'ledger_S21_2026-11-12.tsv', 'ledger_S21_2026-11-12_反証.md']:
    cp(f'{W}/{f}', f'{M}/{f}')
for q in ['out', 'q3']:
    for f in sorted(os.listdir(f'{W}/{q}')):
        if f.endswith('.done') or f.endswith('.mark'): continue
        cp(f'{W}/{q}/{f}', f'{M}/{q}/{f}')
T = 'D:/PoB_work/r1204_work'
for f in sorted(os.listdir(T + '/scripts/r1204')):
    cp(f'{T}/scripts/r1204/{f}', f'{R}/scripts/instruments/r1204/{f}')
for f in ['mc_base.txt', 'mr_base.txt', 'cv_base.txt', 'mshare_base.txt', 'xb_base.txt']:
    cp(f'{T}/{f}', f'{R}/measurements/m1204_20261118/{f}')
for f in sorted(os.listdir(T + '/c118mb')):
    cp(f'{T}/c118mb/{f}', f'{R}/measurements/m1204_20261118/c118mb/{f}')
D = f'{R}/docs/202611/20261118'
cp(f'{T}/material.md', f'{D}/20261118_貿易のB′-1の2回目_設計_材料.md')
cp(f'{T}/draft.md', f'{D}/20261118_貿易のB′-1の2回目_設計_下書き.md')
cp('D:/PoB_work/c120k11_work/m1.mark.log', f'{R}/measurements/m1200_20261118_mut_m1.txt')
