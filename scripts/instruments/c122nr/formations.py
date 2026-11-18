"""(1216) c122nr 記録：統一の成立（nation_s の displayName が CultureRealms の名に替わった種と最初の年）。使い方: python formations.py タグ=<生> ..."""
import sys, os
NAMES = ['北ドイツ連邦', 'ドイツ帝国', '神聖ローマ帝国', 'イタリア王国', 'ローマ帝国', 'アラブ王国', 'カリフ国', 'マラーター王国', 'マラーター帝国', '中華', '中華帝国']
for arg in sys.argv[1:]:
    tag, raw = arg.split('=', 1)
    seen = {}
    for s in range(1, 11):
        p = os.path.join(raw, f'nation_s{s}.tsv')
        if not os.path.exists(p): print(tag, f'種 {s} 無し'); continue
        with open(p, encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            if 'displayName' not in ix: print(tag, 'displayName が無い'); break
            for line in fh:
                c = line.rstrip('\n').split('\t'); dn = c[ix['displayName']]
                if dn in NAMES:
                    k = (dn, c[ix['nation']]); y = int(c[ix['year']])
                    seen.setdefault(k, {}); seen[k].setdefault(s, y)
    print(f'== {tag}')
    if not seen: print('  成立 0')
    for (dn, base), d in sorted(seen.items()):
        print(f"  {dn}（{base}）: {len(d)}/10 種・最初の年 {sorted(d.items())}")
