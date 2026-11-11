# (900) c77lg：crownpc_s から 予算闘争の効きの入力（議会の統制力＞0 かつ 前の協力度＜0）と緊縮の強度（austerityIndex）を数える（読むだけ）。
# ⚠協力度は前の四半期の値（crownpc の列 coopParliamentCrownPrevIndex）＝Move が読む今の値とは 1 四半期ずれる（近似・自己申告）。
# 使い方: python -X utf8 s19_crownpc.py <生のフォルダ>
import sys, os, collections
d = sys.argv[1]; c = collections.Counter(); amax = 0.0; aseed = collections.Counter()
for s in range(1, 11):
    p = os.path.join(d, f'crownpc_s{s}.tsv')
    if not os.path.exists(p): print('無い', p); sys.exit(2)
    f = open(p, 'rb'); h = f.readline().decode().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    for raw in f:
        r = raw.decode().rstrip('\r\n').split('\t')
        def v(k):
            try: return float(r[ix[k]])
            except ValueError: return float('nan')
        c['行'] += 1
        pc, co, au = v('pcNowShare'), v('coopParliamentCrownPrevIndex'), v('austerityIndex')
        if pc > 0 and co < 0: c['統制力>0×協力度<0（拒否権の項>0）'] += 1
        if pc > 0: c['統制力>0'] += 1
        if au > 0: c['緊縮>0'] += 1; aseed[s] += 1
        if au >= 0.999: c['緊縮が満額'] += 1
        if au == au: amax = max(amax, au)
print(dict(c), '緊縮の最大', round(amax, 4), '緊縮>0 の種ごと', dict(sorted(aseed.items())))
