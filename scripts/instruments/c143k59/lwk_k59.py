# (1356) c143k59 記録の読み手（読むだけ・門にしない）：femp_s の差の行（oldStruct−newStruct＞0.01）の国×四半期で、lwk_s の partShare と workEngineIndex を平均する。
#   法の段に届く国×年の数（6 大国の内訳つき）も数える。種の番号はファイルの名から取る。python lwk_k59.py <生のフォルダ>
import sys, os, re, glob
d = sys.argv[1]; BIG = {'イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', 'オーストリア'}
sn = lambda p, t: int(re.match(t + r'_s(\d+)\.tsv$', os.path.basename(p)).group(1))
ps = pw = n = 0; ny = set(); nyb = set()
for fp in sorted(glob.glob(os.path.join(d, 'femp_s*.tsv')), key=lambda p: sn(p, 'femp')):
    s = sn(fp, 'femp'); keys = set()
    with open(fp, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if float(x[ix['oldStruct']]) - float(x[ix['newStruct']]) > 0.01:
                keys.add((x[ix['quarter']], x[ix['nation']])); ny.add((s, x[ix['nation']], x[ix['year']]))
                if x[ix['nation']] in BIG: nyb.add((s, x[ix['nation']], x[ix['year']]))
    lp = os.path.join(d, f'lwk_s{s}.tsv')
    if not os.path.exists(lp): continue
    with open(lp, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t')
            if (x[ix['quarter']], x[ix['nation']]) in keys:
                ps += float(x[ix['partShare']]); pw += float(x[ix['workEngineIndex']]); n += 1
print(f'# {d}')
print(f'差の行の国×年（種×国×年）{len(ny)}・うち 6 大国 {len(nyb)}：' + '・'.join(sorted({f"{a}" for _, a, _ in nyb})))
print(f'差の行の lwk_s の POP 行 {n}・partShare の平均 {ps/n if n else float("nan"):.4f}・workEngineIndex の平均 {pw/n if n else float("nan"):.4f}')
