# (853) c70gt T9：ident_check.py の cmp_ins の表示の内訳の和が共通の表の数に等しい（事前登録 [3]）。
#   python -I -X utf8 test_ident_display853.py <ident_check.py のパス> <作業フォルダ>
#   盤面＝(a) cearn_s の列が増えた表（経路の写しの判定は差し替え＝表示の数え方の試し）を含む (b) 行が増え列も増えた表（rows_grown と grown の両方の宣言）を含む。
#   突然変異＝内訳の二重計上を戻した旧の ident_check.py（git show fe21d8a1:…）で回すと T9 が赤になる。
import sys, os, re, io, csv, importlib.util, contextlib, shutil
path, work = sys.argv[1:3]
spec = importlib.util.spec_from_file_location('ic', path); ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
shutil.rmtree(work, ignore_errors=True); B, A = os.path.join(work, 'base'), os.path.join(work, 'arm'); os.makedirs(B); os.makedirs(A)
def W(d, name, hdr, rows):
    with open(os.path.join(d, name), 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter='\t', lineterminator='\n'); w.writerow(hdr); w.writerows(rows)
base_rows = [[str(i), str(i * 2)] for i in range(6)]
W(B, 'same_s1.tsv', ['a', 'b'], base_rows); W(A, 'same_s1.tsv', ['a', 'b'], base_rows)
W(B, 'burscan_s1.tsv', ['a', 'seconds'], base_rows); W(A, 'burscan_s1.tsv', ['a', 'seconds'], [[r[0], r[1] + '9'] for r in base_rows])
W(B, 'cearn_s1.tsv', ['a', 'b'], base_rows); W(A, 'cearn_s1.tsv', ['a', 'b', 'x1', 'x2'], [r + ['1', '2'] for r in base_rows])
W(B, 'rowsg_s1.tsv', ['a', 'b'], base_rows); W(A, 'rowsg_s1.tsv', ['a', 'b', 'x'], [r + ['1'] for r in base_rows] + [['7', '14', '1']])
ic.cearn_same_but_location = lambda pa, pb, allow_grown=False: (True, 3, ['x1', 'x2'])   # 表示の数え方の試し＝経路の写しは差し替える
ic.OUT.clear(); ic.FAILS.clear(); ic.MISSING.clear(); ic.ROWS_ADDED.clear(); ic.COLS_REMOVED.clear()
buf = io.StringIO()
with contextlib.redirect_stdout(buf):
    ic.cmp_ins(A, B, ic.parse_known(['burscan_s:seconds']), [], [], ['cearn_s', 'rowsg_s'], ['rowsg_s'], [])
out = '\n'.join(ic.OUT)
print(out)
n_common = 4
m = re.search(r'バイト一致 (\d+)・既知の揺れ (\d+)・宣言された増減 (\d+)', out)
res = []
res.append(('判定は恒等（FAILS・MISSING が空）', not ic.FAILS and not ic.MISSING))
tot_ok_line = sum(int(x) for x in m.groups()) if m else -1
res.append(('T9a ok の行の内訳の和＝共通の表の数（%d）：実際 %d' % (n_common, tot_ok_line), tot_ok_line == n_common))
m2 = re.search(r'内訳：共通の表 (\d+) 本＝バイト一致 (\d+)＋既知の揺れ (\d+)＋列が増えた (\d+)＋行が増えた (\d+)＋列が消えた (\d+)＋宣言の無い違い (\d+)＋その他 (\d+)（和 (\d+)）', out)
res.append(('T9b 内訳の行が在り和＝共通の表の数', bool(m2) and int(m2.group(1)) == n_common and int(m2.group(9)) == n_common and '※' not in out))
for n, okv in res: print(('○ ' if okv else '✗ ') + n)
sys.exit(0 if all(o for _, o in res) else 1)
