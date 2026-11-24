# (1350) c142lr 記録の読み手（読むだけ・門にしない・エンジンの関数を呼ばない）：
#   ① 時短（ShorterHours）の後退が 1 試合で 5 回以上ある（種・国）の組の数（monlaw_s・全ての国）。
#   ② 議会の統制力：crownpc_s の数値の列のうち名に parl を含む列の、6 大国×四半期の平均（列が無ければ NA）。
#   使い方：python rec_lr.py <ins>
import sys, os, glob, re, statistics
ins = sys.argv[1]
SIX = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'オーストリア', 'アメリカ']
def rows(p):
    with open(p, 'rb') as fh:
        h = fh.readline().rstrip(b'\r\n').decode('utf-8').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            yield ix, line.rstrip(b'\r\n').decode('utf-8').split('\t')
def seed(p): return int(re.search(r'_s(\d+)', os.path.basename(p)).group(1))
pairs5 = 0; pairs1 = 0
for p in sorted(glob.glob(os.path.join(ins, 'monlaw_s*.tsv')), key=seed):
    prev = {}; cnt = {}
    for ix, c in rows(p):
        u = c[ix['nationUid']]; v = int(float(c[ix['ShorterHours']]))
        if u in prev and v > prev[u]: cnt[u] = cnt.get(u, 0) + 1
        prev[u] = v
    pairs5 += sum(1 for k in cnt.values() if k >= 5); pairs1 += len(cnt)
print(f'時短の後退が 5 回以上の（種・国）の組\t{pairs5}\t1 回以上の組\t{pairs1}')
cols = None; acc = {}
for p in sorted(glob.glob(os.path.join(ins, 'crownpc_s*.tsv')), key=seed):
    for ix, c in rows(p):
        if cols is None: cols = [k for k in ix if 'parl' in k.lower()]
        if c[ix['nation']] not in SIX: continue
        for k in cols:
            try: acc.setdefault(k, []).append(float(c[ix[k]]))
            except ValueError: pass
if not acc: print('議会の統制力\tNA')
for k, v in acc.items(): print(f'議会（crownpc_s の {k}）の 6 大国×四半期の平均\t{statistics.mean(v):.4f}\t行 {len(v)}')
