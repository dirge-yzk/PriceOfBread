"""(756) c4dg：事前登録 m756 の門 D1・D2・D3 と記録を pgrip_s から読む（読むだけ）。
python -X utf8 gate756.py <計器の出力ディレクトリ> [種...]
  D1 ＝ 全行で debtGripUsed＝debtGripNew・gripUsed＝gripNew（相対差 ≦1e-12）。違う行の数を出す。
  D2 ＝ 使った debtGrip が 1 に張り付く行（国×四半期）の割合（全種の和）≦0.066。旧の割合（debtGripOld が 1）も並べる。
  D3 ＝ 債務が正の行（debtToGdpYearRatio>0）で Σ gripUsed ÷ Σ gripOld ≦0.85。
記録 ＝ 英仏普露清の掌握・実効・文民制度の新旧の平均（同じ盤面）。年の名目GDPが正でない行は ratio＝0 なので債務が正の行には入らない（全行の割合には入る）。"""
import sys, os, collections
d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(f[len('pgrip_s'):-4]) for f in os.listdir(d) if f.startswith('pgrip_s') and f.endswith('.tsv'))
NAT = ['イギリス', 'フランス', 'プロイセン', 'ロシア', '清']
rows = bad1 = bad2 = 0
grows = gUsed = gOldS = 0   # 記録：年の名目GDPが正の行（nation_s の nominalGdpPerYear を (quarter,nation) で結ぶ）
stickUsed = stickOld = stickNew = 0
pos = 0; sUsed = sOld = sNew = 0.0
dpos = 0; dUsedPos = dOldPos = 0.0
per = collections.defaultdict(lambda: [0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0])
def close(a, b): return abs(a - b) <= 1e-12 * max(1.0, abs(b))
for sd in seeds:
    gdp = {}
    nf = os.path.join(d, f'nation_s{sd}.tsv')
    if os.path.exists(nf):
        with open(nf, encoding='utf-8') as fh:
            nh = next(fh).rstrip('\n').split('\t'); qi, ni, gi = nh.index('quarter'), nh.index('nation'), nh.index('nominalGdpPerYear')
            for ln in fh:
                c = ln.rstrip('\n').split('\t')
                if len(c) == len(nh) and c[qi].isdigit(): gdp[(c[qi], c[ni])] = float(c[gi])
    with open(os.path.join(d, f'pgrip_s{sd}.tsv'), encoding='utf-8') as fh:
        h = next(fh).rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            c = ln.rstrip('\n').split('\t')
            if len(c) != len(h): continue
            g = lambda k: float(c[ix[k]])
            rows += 1
            if not close(g('debtGripUsed'), g('debtGripNew')): bad1 += 1
            if not close(g('gripUsed'), g('gripNew')): bad2 += 1
            if g('debtGripUsed') >= 1: stickUsed += 1
            if gdp.get((c[ix['quarter']], c[ix['nation']]), 0) > 0:
                grows += 1; gUsed += g('debtGripUsed') >= 1; gOldS += g('debtGripOld') >= 1
            if g('debtGripOld') >= 1: stickOld += 1
            if g('debtGripNew') >= 1: stickNew += 1
            if g('debtToGdpYearRatio') > 0:
                pos += 1; sUsed += g('gripUsed'); sOld += g('gripOld'); sNew += g('gripNew')
            nm = c[ix['nation']]
            if nm in NAT:
                p = per[nm]; p[0] += 1; p[1] += g('gripOld'); p[2] += g('gripNew'); p[3] += g('peOld'); p[4] += g('peNew'); p[5] += g('civOld'); p[6] += g('civNew')
print(f'rows={rows} seeds={len(seeds)}')
print(f'D1\tdebtGripUsed≠New の行={bad1}\tgripUsed≠New の行={bad2}\t' + ('通る' if rows and bad1 == 0 and bad2 == 0 else '落ちる'))
f = lambda n: n / rows if rows else float('nan')
print(f'D2\t使った張り付き={f(stickUsed):.4f}（門 ≦0.066）\t旧={f(stickOld):.4f}\t新={f(stickNew):.4f}\t' + ('通る' if rows and f(stickUsed) <= 0.066 else '落ちる'))
print(f'REC	年の名目GDPが正の行={grows}	使った張り付き={(gUsed / grows if grows else float("nan")):.4f}	旧={(gOldS / grows if grows else float("nan")):.4f}')
r3 = sUsed / sOld if sOld else float('nan')
print(f'D3\t債務が正の行={pos}\tΣgripUsed÷ΣgripOld={r3:.4f}（門 ≦0.85）\tΣgripNew÷ΣgripOld={(sNew / sOld if sOld else float("nan")):.4f}\t' + ('通る' if r3 <= 0.85 else '落ちる'))
for nm in NAT:
    p = per.get(nm)
    if p and p[0]: print(f'REC\t{nm}\tn={p[0]}\t掌握 {p[1]/p[0]:.3f}→{p[2]/p[0]:.3f}\t実効 {p[3]/p[0]:.3f}→{p[4]/p[0]:.3f}\t文民制度 {p[5]/p[0]:.3f}→{p[6]/p[0]:.3f}')
