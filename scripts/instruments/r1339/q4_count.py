# (1339) q4：名目 GDP の 1 四半期の比 ≧TH（前の四半期が正）を、名目＝実質×価格水準の項に分けて数える（読むだけ・止める線の定義は変えない）
#   区分：建国の直後（系列の最初の行から 4 四半期以内）・人口の跳び（比 ≧10 か ≦1/10）・それ以外。それ以外の行で、実質の比と価格水準の比の大きい方を〈担う項〉とする
#   追加の読み：跳びの前の 4 四半期の実質の比が全て 0.6141±0.01（＝供給の追従 0.85^3 の幾何の減り）かどうか（列 decay4）
#   使い方: python -I -X utf8 q4_count.py [--th 10] [--rows] <名前>=<ins フォルダ> ...
import csv, glob, math, os, sys
a = sys.argv[1:]; TH = 10.0; rows = '--rows' in a
if '--th' in a: TH = float(a[a.index('--th') + 1])
args = [x for x in a if '=' in x]
def num(v):
    try: return float(v)
    except ValueError: return float('nan')
DEC = 0.85 ** 3
print(f'# q4_count TH={TH:g} 建国の直後≦4 人口の跳び≧10 か≦0.1 decay={DEC:.4f}±0.01')
for arg in args:
    name, d = arg.split('=', 1)
    seeds = sorted(int(os.path.basename(p)[8:-4]) for p in glob.glob(os.path.join(d, 'nation_s*.tsv')))
    ev = []; n100 = 0
    for s in seeds:
        with open(os.path.join(d, f'nation_s{s}.tsv'), encoding='utf-8', newline='') as f:
            r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            iu, inm, iq, iy = ix['nationUid'], ix['nation'], ix['quarter'], ix['year']
            ig, ire, ipl, ipp = ix['nominalGdpPerYear'], ix['realGdpPerYear'], ix['priceLevel'], ix['populationK']
            first = {}; prev = {}; hr = {}
            for x in r:
                u = x[iu]; q = int(x[iq]); g = num(x[ig]); re = num(x[ire]); pl = num(x[ipl]); pp = num(x[ipp])
                if u not in first: first[u] = q
                if not math.isfinite(g): continue
                hist = hr.setdefault(u, [])
                if u in prev and prev[u][0] > 0:
                    pg, pre, ppl, ppp = prev[u]; ra = g / pg
                    if ra >= TH:
                        age = q - first[u]; rp = pp / ppp if ppp > 0 else math.inf
                        rre = re / pre if pre > 0 else math.inf; rpl = pl / ppl if ppl > 0 else math.inf
                        if age <= 4: k = '建国の直後'
                        elif rp >= 10 or rp <= 0.1: k = '人口の跳び'
                        else: k = 'それ以外'
                        term = '実質' if abs(math.log(max(rre, 1e-300))) >= abs(math.log(max(rpl, 1e-300))) else '価格水準'
                        last4 = hist[-4:]
                        dec4 = len(last4) == 4 and all(abs(v - DEC) <= 0.01 for v in last4)
                        ev.append((s, x[inm], int(x[iy]), q, age, ra, rre, rpl, rp, pp, k, term, dec4))
                    hist.append(re / pre if pre > 0 else float('nan'))
                prev[u] = (g, re, pl, pp)
    oth = [e for e in ev if e[10] == 'それ以外']
    mx = max(oth, key=lambda e: e[5]) if oth else None
    print(f'{name}\t種 {len(seeds)}\t≧{TH:g} 全 {len(ev)}（建国の直後 {sum(e[10]=="建国の直後" for e in ev)}・人口の跳び {sum(e[10]=="人口の跳び" for e in ev)}・それ以外 {len(oth)}）'
          f'\tそれ以外のうち 担う項=実質 {sum(e[11]=="実質" for e in oth)}・価格水準 {sum(e[11]=="価格水準" for e in oth)}'
          f'\t直前 4 四半期が 0.85^3 の減り {sum(e[12] for e in oth)}\t≧100 {sum(e[5]>=100 for e in oth)}'
          + (f'\t最大 種{mx[0]} {mx[1]} {mx[2]} q{mx[3]} ×{mx[5]:.4g}（実質×{mx[6]:.4g}・価格水準×{mx[7]:.4g}・人口 {mx[9]:.0f}千）' if mx else ''))
    if rows:
        for e in sorted(oth, key=lambda e: -e[5]):
            print(f'   行\t{name}\t種{e[0]}\t{e[1]}\t{e[2]}\tq{e[3]}\t建国から{e[4]}\t名目×{e[5]:.4g}\t実質×{e[6]:.4g}\t価格水準×{e[7]:.4g}\t人口の比 {e[8]:.4g}\t人口 {e[9]:.0f}千\t{e[11]}\t減り4={int(e[12])}')
