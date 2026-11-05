# (860) c71gt T10：ident_check.py の cmp_ins の〈既知の揺れ〉の内訳に、宣言の無い違い（real）の表を二重に数えない（事前登録 [1-4]）。
#   python -I -X utf8 test_ident_display860.py <ident_check.py のパス> <作業フォルダ>
#   盤面＝(a) 既知の揺れの列だけが違う表（clock_s）(b) 既知の揺れの列が違い、かつ宣言の無い列が増えた表（burscan_s＝real にも known_hit にも入る）。
#   期待＝既知の揺れの行は clock_s だけを挙げる（burscan_s を挙げない）・内訳の和は共通の表の数に等しい。
#   突然変異＝by から realf を除かない旧の ident_check.py（git show 62190ef0:scripts/instruments/ident/ident_check.py）で回すと T10 が赤になる。
import sys, os, re, io, csv, importlib.util, contextlib, shutil
sys.dont_write_bytecode = True
path, work = sys.argv[1:3]
spec = importlib.util.spec_from_file_location('ic', path); ic = importlib.util.module_from_spec(spec); spec.loader.exec_module(ic)
shutil.rmtree(work, ignore_errors=True); B, A = os.path.join(work, 'base'), os.path.join(work, 'arm'); os.makedirs(B); os.makedirs(A)
def W(d, name, hdr, rows):
    with open(os.path.join(d, name), 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter='\t', lineterminator='\n'); w.writerow(hdr); w.writerows(rows)
base_rows = [[str(i), str(i * 2)] for i in range(6)]
W(B, 'same_s1.tsv', ['a', 'b'], base_rows); W(A, 'same_s1.tsv', ['a', 'b'], base_rows)
W(B, 'clock_s1.tsv', ['a', 'total'], base_rows); W(A, 'clock_s1.tsv', ['a', 'total'], [[r[0], r[1] + '9'] for r in base_rows])
W(B, 'burscan_s1.tsv', ['a', 'seconds'], base_rows); W(A, 'burscan_s1.tsv', ['a', 'seconds', 'x'], [[r[0], r[1] + '9', '1'] for r in base_rows])
ic.OUT.clear(); ic.FAILS.clear(); ic.MISSING.clear(); ic.ROWS_ADDED.clear(); ic.COLS_REMOVED.clear()
buf = io.StringIO()
with contextlib.redirect_stdout(buf):
    ic.cmp_ins(A, B, ic.parse_known(['burscan_s:seconds', 'clock_s:total']), [], [], [], [], [])
out = '\n'.join(ic.OUT)
print(out)
line = [x for x in ic.OUT if '既知の揺れの列だけが違う表' in x]
res = []
res.append(('T10a 既知の揺れの行が在る', len(line) == 1))
res.append(('T10b 既知の揺れの行は clock_s を挙げ burscan_s を挙げない（real の表を二重に数えない）：' + (line[0].strip() if line else '行なし'),
            bool(line) and 'clock_s:total×1' in line[0] and 'burscan_s' not in line[0]))
m2 = re.search(r'内訳：共通の表 (\d+) 本＝バイト一致 (\d+)＋既知の揺れ (\d+)＋列が増えた (\d+)＋行が増えた (\d+)＋列が消えた (\d+)＋宣言の無い違い (\d+)＋その他 (\d+)（和 (\d+)）', out)
res.append(('T10c 内訳の和＝共通の表の数（3）・宣言の無い違い 1・既知の揺れ 1', bool(m2) and int(m2.group(1)) == 3 and int(m2.group(9)) == 3 and int(m2.group(7)) == 1 and int(m2.group(3)) == 1))
for n, okv in res: print(('○ ' if okv else '✗ ') + n)
sys.exit(0 if all(o for _, o in res) else 1)
