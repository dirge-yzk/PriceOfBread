# (900) c77lg：monlaw_s の段の変化の事件を分ける（読むだけ）。
# 使い方: python -X utf8 s19_events.py <生のフォルダ> [種の数]
# 出す物：(1) |差|>=2 の事件を軸×向き×〈同じ四半期に政体が変わった〉で数える
#         (2) 後退（参政権・女性の権利・社会保障・植民地参政権・移民原住民参政権は下がる＝後退／労働時間・児童労働は上がる＝後退／農奴解放・土地改革は下がる＝後退）を軸×〈政体が変わった〉で数える
#         (3) 種ごとの件数（尾の確かめ）
import sys, os, collections
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
REG_DOWN = ['Suffrage', 'WomenRights', 'SocialSecurity', 'ColonialSuffrage', 'MigrantNativeSuffrage', 'SerfEmancipation', 'LandReform']
REG_UP = ['ShorterHours', 'ChildLabor']
big = collections.Counter(); reg = collections.Counter(); regseed = collections.defaultdict(collections.Counter); polfrom = collections.Counter()
prev = {}
for s in range(1, ns + 1):
    f = open(os.path.join(d, f'monlaw_s{s}.tsv'), 'rb'); hdr = f.readline().decode().rstrip('\n').split('\t')
    ix = {h: i for i, h in enumerate(hdr)}
    axes = REG_DOWN + REG_UP + ['Censorship', 'ConscriptionLaw', 'MonetaryStandard']
    for raw in f:
        r = raw.decode().rstrip('\n').split('\t')
        key = (s, r[ix['nationUid']]); q = int(r[0]); pol = r[ix['polityCode']]
        cur = {a: int(float(r[ix[a]])) for a in axes}
        pv = prev.get(key)
        if pv and pv[0] == q - 1:
            pc = 'polity変化' if pv[1] != pol else '政体同じ'
            for a in axes:
                dl = cur[a] - pv[2][a]
                if dl == 0: continue
                if abs(dl) >= 2: big[(a, '上' if dl > 0 else '下', pc)] += 1
                if (a in REG_DOWN and dl < 0) or (a in REG_UP and dl > 0):
                    reg[(a, pc)] += 1; regseed[a][s] += 1
                    if pc == 'polity変化': polfrom[(a, pv[1] + '→' + pol)] += 1
        prev[key] = (q, pol, cur)
print('(1) |差|>=2'); [print(' ', k, n) for k, n in sorted(big.items())]
print('(2) 後退'); [print(' ', k, n) for k, n in sorted(reg.items())]
print('(2b) 後退の政体の移り（上位）'); [print(' ', k, n) for k, n in polfrom.most_common(12)]
print('(3) 後退の種ごと'); [print(' ', a, dict(sorted(c.items()))) for a, c in regseed.items()]
