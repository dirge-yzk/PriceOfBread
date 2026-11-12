# (1073) 紙 §4 の W1・W2 の材料：tskshadow_s（置き換えの影）と tskpair_s（国の組の門の内訳）を種ごとに集計して種の中央値を出す（読むだけ）。
#   楔 D の組（今は安くない）のうち、売り手が級 E で、案 B′ の影なら通る組の数と、その行の不足の額（dToBpShort＝max(0,目標＋需要−生産−在庫)×lp・行×級で 1 回）。
#   着値は距離・危険料を含まない（計器の宣言した限界）。
# 使い方: python -X utf8 -I shadow_w2.py <ins フォルダ>
import os, re, statistics as st, sys
from collections import defaultdict

YEARS = ('1870', '1913')


def fl(x):
    try: return float(x)
    except Exception: return float('nan')


def seeds(d):
    return sorted(int(m.group(1)) for f in os.listdir(d) for m in [re.fullmatch(r'tskshadow_s(\d+)\.tsv', f)] if m)


def read(path):
    with open(path, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in f:
            yield ln.rstrip('\n').split('\t'), ix


def main(d):
    per = defaultdict(list)   # (年, 級, 指標) -> 種ごとの値
    for s in seeds(d):
        acc = defaultdict(float)
        for v, ix in read(os.path.join(d, 'tskshadow_s%d.tsv' % s)):
            y, c = v[ix['year']], v[ix['sellerClass']]
            if y not in YEARS: continue
            for k in ('pairs', 'nowPass', 'nowWedgeD', 'bpPass', 'dToBpPass', 'dToBpShort', 'ownN', 'ownNowLt1', 'ownBpLt1'):
                acc[(y, c, k)] += fl(v[ix[k]])
        for (y, c, k), x in acc.items(): per[(y, c, k)].append(x)
    for y in YEARS:
        print('== %s（種の中央値・種の数 %d）' % (y, len(seeds(d))))
        print('  級   組の数    今通る  楔D      B′で通る  D→B′で通る  その不足の額      着値÷自己費用<1（今→B′）')
        for c in 'ENSFXZU-':
            g = lambda k: st.median(per[(y, c, k)]) if per.get((y, c, k)) else 0.0
            if g('pairs') == 0: continue
            print('  %s  %8.0f %8.0f %8.0f %8.0f %10.0f %16.1f   %8.0f → %8.0f' % (c, g('pairs'), g('nowPass'), g('nowWedgeD'), g('bpPass'), g('dToBpPass'), g('dToBpShort'), g('ownNowLt1'), g('ownBpLt1')))


if __name__ == '__main__':
    main(sys.argv[1])
