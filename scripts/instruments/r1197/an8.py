# (1197) c119t2：史実の桁の線の材料＝石炭・鉄鉱石・金・銀の世界の産出の 1913÷1784（基線の生）。
# prodcap_s（生産者の月の産出の和・年末の断面）と metal_s（金と銀の産出・四半期の行）を別々に数え、金と銀は 2 つを突き合わせる。
# 呼び方：python -X utf8 -I an8.py <生のフォルダ> <種の始め> <種の終わり> <出力>
import csv, collections, sys
csv.field_size_limit(10**9)
G = ['石炭', '鉄鉱石', '金', '銀']
def main():
    raw, s0, s1, outp = sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), sys.argv[4]
    lines = []
    for s in range(s0, s1+1):
        pc = collections.Counter()
        with open(f'{raw}/prodcap_s{s}.tsv', encoding='utf-8', newline='') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
            for r in rd:
                y = r[ix['year']]
                if y not in ('1784', '1850', '1913'): continue
                g = r[ix['good']]
                if g not in G: continue
                try: o = float(r[ix['output']])
                except ValueError: continue
                if o > 0: pc[(int(y), g)] += o
        mt = collections.defaultdict(lambda: [0.0, 0.0, 0])
        with open(f'{raw}/metal_s{s}.tsv', encoding='utf-8', newline='') as f:
            rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k:i for i,k in enumerate(h)}
            for r in rd:
                y = int(r[ix['year']])
                if y not in (1784, 1850, 1913): continue
                a = mt[y]; a[0] += float(r[ix['goldProduced']]); a[1] += float(r[ix['silverProduced']]); a[2] += 1
        row = [f'種{s}']
        for g in G:
            a, b = pc[(1784, g)], pc[(1913, g)]
            row.append(f'{g} {b/a:.2f}' if a > 0 else f'{g} nan')
        for k, nm in ((0, '金metal'), (1, '銀metal')):
            a, b = mt[1784][k]/max(1, mt[1784][2]), mt[1913][k]/max(1, mt[1913][2])
            row.append(f'{nm} {b/a:.2f}' if a > 0 else f'{nm} nan')
        # 突き合わせ：prodcap の金の産出（月）×3 と metal の四半期の平均
        for k, g in ((0, '金'), (1, '銀')):
            q = mt[1913][k]/max(1, mt[1913][2]); p = pc[(1913, g)]
            row.append(f'{g}1913 metal/四半期÷prodcap/月 {q/p:.3f}' if p > 0 else f'{g} nan')
        lines.append('\t'.join(row))
    with open(outp, 'w', encoding='utf-8', newline='\n') as w:
        w.write(f'# an8 種 {s0}〜{s1}・世界の産出の 1913÷1784（prodcap_s の年末の断面の月の産出・metal_s は年の四半期の平均）\n')
        for l in lines: w.write(l + '\n')
main()
