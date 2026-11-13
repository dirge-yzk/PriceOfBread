# (1109) c106rn〈追加の読み・集計の後に書いた〉：案 1（need の取り分＝州の産出の名目の比）の上側の誤りの紙の数。
# 発見の後・水運でない州×年（trrail）で、①産出の参照値（region_s の工場＋採掘の列・年の最後の行）が 0 なのに今の取り分（needEnd÷railTargetIndex）が正の割合、
# ②産出の取り分が今の取り分の 1/4 未満（need が 4 倍以上縮む＝わずかな線路で φ＝1 になりうる州）の割合、③その②の中で今 φ＜0.99 の割合。
# 呼び方：python -X utf8 -I outshare_rn.py <ins> <出力の txt>
import sys, os, glob
from collections import defaultdict

ins, outp = sys.argv[1], sys.argv[2]
rows_all = 0; out_zero = 0; shrink4 = 0; shrink4_low = 0; grow4 = 0
for path in sorted(glob.glob(os.path.join(ins, 'trrail_s*.tsv'))):
    sd = path.rsplit('_s', 1)[1].split('.')[0]
    outv = {}; natout = defaultdict(float)
    with open(os.path.join(ins, f'region_s{sd}.tsv'), encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            if c[ix['activeFlag']] != '1': continue
            try: outv[(c[ix['region']], int(c[ix['year']]))] = (c[ix['nation']], float(c[ix['factoryOutputRefValuePerMonth']]) + float(c[ix['extractionOutputRefValuePerMonth']]))
            except ValueError: pass
    for (rg, y), (nn, v) in outv.items(): natout[(nn, y)] += max(0.0, v)
    tg = {}
    with open(os.path.join(ins, f'trnation_s{sd}.tsv'), encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            try: tg[(int(c[ix['year']]), c[ix['nation']])] = float(c[ix['railTargetIndex']])
            except (ValueError, IndexError): pass
    with open(path, encoding='utf-8') as f:
        hd = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hd)}
        for line in f:
            c = line.rstrip('\n').split('\t')
            need = float(c[ix['needEnd']])
            if not (need > 0) or c[ix['waterFlag']] == '1': continue
            y = int(c[ix['year']]); nn = c[ix['nation']]; rg = c[ix['region']]
            t = tg.get((y, nn)); ov = outv.get((rg, y))
            if not t or ov is None or ov[0] != nn or not (natout[(nn, y)] > 0): continue
            rows_all += 1
            pot_share = need / t; out_share = max(0.0, ov[1]) / natout[(nn, y)]
            if not (out_share > 0): out_zero += 1
            if out_share < 0.25 * pot_share:
                shrink4 += 1
                if float(c[ix['phiEnd']]) < 0.99: shrink4_low += 1
            if out_share > 4 * pot_share: grow4 += 1
out = [f'== {ins}',
       f'州×年 {rows_all}・①産出 0 で取り分が正 {out_zero}（{out_zero / max(1, rows_all):.4f}）・②産出の取り分＜今の取り分÷4 {shrink4}（{shrink4 / max(1, rows_all):.3f}）・③②のうち今 φ＜0.99 {shrink4_low}（{shrink4_low / max(1, shrink4):.3f}）・産出の取り分＞今の取り分×4 {grow4}（{grow4 / max(1, rows_all):.3f}）']
with open(outp, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(out) + '\n')
print('\n'.join(out))
