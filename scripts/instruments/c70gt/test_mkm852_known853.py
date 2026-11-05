# (853) c70gt：mkm852.py が ident.cfg の KNOWN を読むことの試し（事前登録 [3]）。
#   python -I -X utf8 test_mkm852_known853.py <mkm852.py のパス> <リポジトリ> <作業フォルダ>
#   (a) burscan_s の seconds だけが違う盤面＝〈既知の揺れだけ〉で M ≡ B2 になる (b) 同じ表の別の列が違う盤面＝〈違う〉で採らない。
#   突然変異＝KNOWN を読まない旧の mkm852.py（git show fe21d8a1:…）で (a) を回すと〈違う〉になり赤。
import sys, os, csv, shutil, subprocess
mk, repo, work = sys.argv[1:4]
shutil.rmtree(work, ignore_errors=True)
def W(d, name, hdr, rows):
    os.makedirs(d, exist_ok=True)
    with open(os.path.join(d, name), 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter='\t', lineterminator='\n'); w.writerow(hdr); w.writerows(rows)
cfg = os.path.join(work, 'ident.cfg'); os.makedirs(work, exist_ok=True)
open(cfg, 'w', encoding='utf-8', newline='\n').write('KNOWN=burscan_s:seconds,clock_s:worldTotalPerMonth@kind=C\n')
rows = [[str(i), str(i * 2)] for i in range(5)]
def case(name, arm_rows, arm_hdr, want_known):
    M, B = os.path.join(work, name, 'M'), os.path.join(work, name, 'B')
    W(B, 'burscan_s1.tsv', ['a', 'seconds'], rows); W(M, 'burscan_s1.tsv', arm_hdr, arm_rows)
    W(B, 'same_s1.tsv', ['a'], [['1']]); W(M, 'same_s1.tsv', ['a'], [['1']])
    out = os.path.join(work, name, 'out.tsv')
    r = subprocess.run([sys.executable, '-I', '-X', 'utf8', mk, M, B, repo, 'x', 'y', out, cfg], capture_output=True, encoding='utf-8')
    txt = open(out, encoding='utf-8').read() if os.path.exists(out) else r.stderr
    judged = bool(txt) and 'M ≡ B2' in txt.splitlines()[-1]   # 先頭行の見出しにも M ≡ B2 の字がある＝判定の行（最後）だけを見る
    return judged == want_known, txt.splitlines()[-1] if txt else r.stderr[-200:]
res = []
ok, t = case('a', [[r[0], r[1] + '9'] for r in rows], ['a', 'seconds'], True); res.append(('(a) seconds だけが違う盤面＝既知の揺れだけ（M ≡ B2）：' + t, ok))
ok, t = case('b', [[r[0] + '9', r[1] + '9'] for r in rows], ['a', 'seconds'], False); res.append(('(b) a の列も違う盤面＝違う（採らない）：' + t, ok))
for n, o in res: print(('○ ' if o else '✗ ') + n)
sys.exit(0 if all(o for _, o in res) else 1)
