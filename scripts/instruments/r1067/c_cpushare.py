"""(1067) C：1914 年までの計算量の割合（近似・読むだけ）。
clock_s は発見の時計の表（kind・quarter・year・row …）で CPU の列が無い＝使えない。代わりに .cost の行ごとの推定（秒）を、
その行の件数に比例すると見なせる計器の量の〈1914 年より前の割合〉で配る：
  px＝pxstep の goodsCount（dormant を除く）＝価格：財の歩 と同じ物（MarketFlow の財の歩の内訳）
  daily＝pxstep の dailyRuns＝価格：日次の写像の回
  prod＝prodcap の行（10 年ごとの 1 四半期の生産者の一覧）を年へ線形に補い、四半期ごとに足す
  reg＝region_s の activeFlag=1 の州×四半期（POP・清算・消費の代わり）・reg2＝その 2 乗（州×州の組）
  nat＝nation_s の国×四半期・nat2＝その 2 乗（国×国）
  q＝四半期の数（どれにも当たらない行）
python -X utf8 c_cpushare.py <ins> <cost> <出力>"""
import sys, os, csv, glob, collections, re
ins, costp, out = sys.argv[1], sys.argv[2], sys.argv[3]
Y = 1914
pre = collections.Counter(); tot = collections.Counter()
seeds = sorted(int(re.search(r'_s(\d+)\.tsv$', p).group(1)) for p in glob.glob(os.path.join(ins, 'pxstep_s*.tsv')))
for s in seeds:
    with open(os.path.join(ins, f'pxstep_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            y = int(r['year']); a = y < Y
            if r['branch'] != 'dormant':
                v = float(r['goodsCount']); tot['px'] += v; pre['px'] += v * a
            v = float(r['dailyRuns']); tot['daily'] += v; pre['daily'] += v * a
    regq = collections.Counter(); natq = collections.Counter(); qy = {}
    with open(os.path.join(ins, f'region_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['activeFlag'] == '1': regq[int(r['quarter'])] += 1; qy[int(r['quarter'])] = int(r['year'])
    with open(os.path.join(ins, f'nation_s{s}.tsv'), encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            natq[int(r['quarter'])] += 1; qy.setdefault(int(r['quarter']), int(r['year']))
    for qq, n in regq.items():
        a = qy[qq] < Y; tot['reg'] += n; pre['reg'] += n * a; tot['reg2'] += n * n; pre['reg2'] += n * n * a
    for qq, n in natq.items():
        a = qy[qq] < Y; tot['nat'] += n; pre['nat'] += n * a; tot['nat2'] += n * n; pre['nat2'] += n * n * a
        tot['q'] += 1; pre['q'] += a
    pc = collections.Counter()
    with open(os.path.join(ins, f'prodcap_s{s}.tsv'), encoding='utf-8') as f:
        next(f)
        for ln in f: pc[int(ln.split('\t', 1)[0])] += 1
    ys = sorted(pc)
    for qq, yy in qy.items():
        lo = max([y for y in ys if y <= yy], default=ys[0]); hi = min([y for y in ys if y >= yy], default=ys[-1])
        v = pc[lo] if hi == lo else pc[lo] + (pc[hi] - pc[lo]) * (yy - lo) / (hi - lo)
        tot['prod'] += v; pre['prod'] += v * (yy < Y)
share = {k: pre[k] / tot[k] for k in tot}
MAP = [('財の歩', 'px'), ('日次の写像', 'daily'), ('生産', 'prod'), ('請求', 'prod'), ('GDP', 'prod'), ('設備の配り', 'prod'), ('労働の重み', 'prod'), ('発見：世界の高度', 'prod'),
       ('覇権', 'nat2'), ('脅威', 'nat2'), ('移民', 'reg2'), ('外交', 'nat'), ('信用', 'nat'), ('共有層', 'nat'), ('発見', 'nat'), ('官僚', 'nat'),
       ('消費', 'reg'), ('月次', 'reg'), ('四半期', 'reg'), ('清算', 'reg'), ('建設', 'reg'), ('奢侈', 'reg'), ('不満', 'reg'), ('最低賃金', 'reg'), ('年齢', 'reg'), ('主食', 'reg'), ('海軍', 'reg')]
rows = []
for ln in open(costp, encoding='utf-8'):
    p = ln.rstrip('\n').split('\t')
    if len(p) == 4 and re.match(r'^\s+\S', p[0]) and re.match(r'^[\d.]+$', p[3]):
        name = p[0].strip(); est = float(p[3]); key = next((k for w, k in MAP if w in name), 'q')
        rows.append((name, est, key, share[key]))
T = sum(r[1] for r in rows); P = sum(r[1] * r[3] for r in rows)
with open(out, 'w', encoding='utf-8') as fo:
    fo.write(f'# 種 {seeds}・{costp}\n## 量ごとの 1914 年より前の割合: ' + '  '.join(f'{k} {share[k]:.3f}' for k in sorted(share)) + '\n')
    fo.write(f'## 量の全体（10 種の和）: ' + '  '.join(f'{k} {tot[k]:.4g}' for k in sorted(tot)) + '\n')
    fo.write('## 行・推定（秒）・量・割合・1914 年より前の推定（秒）\n')
    for r in rows: fo.write(f'{r[0][:40]}\t{r[1]:.2f}\t{r[2]}\t{r[3]:.3f}\t{r[1] * r[3]:.2f}\n')
    fo.write(f'## 合計 推定 {T:.1f} 秒・うち 1914 年より前 {P:.1f} 秒＝{P / T:.3f}（近似）・四半期の割合 {share["q"]:.3f}\n')
print('ok', round(P / T, 3))
