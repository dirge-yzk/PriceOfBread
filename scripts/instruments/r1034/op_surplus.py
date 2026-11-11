# (1034) c97op：世界の財ごとに〈国の余り Σmax(0,作った−使った)〉と〈不足 Σshortage〉と〈越境の輸出〉を並べる（読むだけ）
# 余り＞不足なのに越境が細い財＝売り手の側の門か量の規則で止まっている候補。余り＜不足＝世界の不足（貿易の問題でない）。
# 使い方: python op_surplus.py <pre> <出力>　（base_ins の種 1〜10・国＝ALL の行を除く・値の重み priceMeanGold）
import sys, os, statistics as st
from collections import defaultdict
PRE, OUT = sys.argv[1], sys.argv[2]
per = defaultdict(lambda: defaultdict(list))
tot = defaultdict(lambda: defaultdict(list))
for arm in ('base_ins', 'n1', 'n2'):
    for s in range(1, 11):
        acc = defaultdict(lambda: defaultdict(float))
        with open(os.path.join(PRE, 'ng_%s_s%d.tsv' % (arm, s)), encoding='utf-8') as f:
            for ln in f:
                v = ln.rstrip('\n').split('\t')
                if v[1] == 'ALL': continue
                y, g = v[0], v[2]
                P, D, S, EQ, px = float(v[4]), float(v[5]), float(v[6]), float(v[11]), float(v[12])
                if not (px == px) or px <= 0: continue
                a = acc[(y, g)]
                a['sur'] += max(0.0, P - D) * px; a['short'] += S * px; a['exp'] += EQ * px; a['D'] += D * px
        for (y, g), a in acc.items():
            per[(y, g)][arm].append(a)
        for y in ('1870', '1913'):
            t = defaultdict(float)
            for (yy, g), a in acc.items():
                if yy != y: continue
                for k in a: t[k] += a[k]
                if a['sur'] > a['short']: t['goodsSurGtShort'] += 1
                t['goods'] += 1
            tot[y][arm].append(t)
with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
    f.write('# 世界（全ての国）・値は種の中央値（base_ins）・括弧なし＝額÷使った額\n')
    for y in ('1870', '1913'):
        for arm in ('base_ins', 'n1', 'n2'):
            L = tot[y][arm]
            m = lambda k: st.median([t[k] / t['D'] for t in L])
            f.write('%s\t%s\t余り÷使った %.4f\t不足÷使った %.4f\t越境の輸出÷使った %.4f\t余り＞不足の財の割合 %.3f\n' % (
                y, arm, m('sur'), m('short'), m('exp'), st.median([t['goodsSurGtShort'] / t['goods'] for t in L])))
    f.write('\nyear\tgood\t使った額\t余り÷使った\t不足÷使った\t越境の輸出÷使った\t越境の輸出÷余り\n')
    for y in ('1913', '1870'):
        rows = []
        for (yy, g), d in per.items():
            if yy != y: continue
            L = d['base_ins']
            md = lambda k: st.median([a[k] for a in L])
            D = md('D')
            if D <= 0: continue
            rows.append((md('short') + md('sur'), g, D, md('sur') / D, md('short') / D, md('exp') / D, md('exp') / md('sur') if md('sur') > 0 else float('nan')))
        rows.sort(reverse=True)
        for r in rows[:25]:
            f.write('%s\t%s\t%.4g\t%.3f\t%.3f\t%.4f\t%.3f\n' % ((y, r[1]) + r[2:]))
print('done')
