# (1015) c94rh 史実の側（hist.tsv）と盤面（world.json・region_s・board.tsv）を突き合わせ、行ごとに区分 D/L/T/H を機械で付ける（prereg.txt §2）。
# 使い方: python -X utf8 -I rh_hole.py <world.json> <Catalog.cs> <hist.tsv> <board.tsv> <生のフォルダ> <種の範囲> <出力のファイル>
# 区分の規則（prereg のまま）：main=H→H／州なし→T(ii)／州の 1783 の持ち主が別の国→T(iii)（データにも無ければ +D）／
#   データに無い→opened≦1850 なら D・それより後は L／データにあり、その国が 1913 にその州を持った種が半分未満→T(i)／それ以外→一致。
import sys, os, re, json
WJ, CAT, HIST, BOARD, RAW, RNG, OUT = sys.argv[1:8]
lo, hi = map(int, RNG.split('-'))
w = json.load(open(WJ, encoding='utf-8-sig'))
RES = {}; OWN0 = {}
for n in w['nations']:
    for p in n['provinces']: RES[p['city']] = set(p['resources']); OWN0[p['city']] = n['name']
    for c in n.get('initialColonies', []): RES[c['name']] = set(c['resources']); OWN0[c['name']] = n['name']
src = open(CAT, encoding='utf-8-sig').read()
IDS = {m.group(1): int(m.group(2)) for m in re.finditer(r'public const int (\w+Id)\s*=\s*(\d+);', src)}
blk = src[src.index('public static readonly SiteRow[] Sites'):]; blk = blk[:blk.index('];')]
SITEAT = {(m.group(2), IDS[m.group(3)]) for m in re.finditer(r'new\("([^"]+)",\s*"([^"]+)",\s*(\w+Id),', blk)}
goods = json.load(open(os.path.join(os.path.dirname(WJ), 'goods.json'), encoding='utf-8-sig'))
gid = {g['name']: g['id'] for g in goods}
hist = []
with open(HIST, encoding='utf-8') as f:
    for line in f:
        if line.startswith('#') or line.startswith('nation\t'): continue
        c = line.rstrip('\n').split('\t')
        if len(c) >= 8: hist.append(c)
# board.tsv の国×財の行（国の穴の判定：1783 の本国と植民地と産地の行・1870／1913 の持ち州の中央値が全て 0）
NB = {}
with open(BOARD, encoding='utf-8') as f:
    for line in f:
        if line.startswith('#'): continue
        c = line.rstrip('\n').split('\t')
        if c[0] == 'nation': h = {k: i for i, k in enumerate(c)}; continue
        NB[(c[0], c[1])] = c
provs = {c[3] for c in hist if c[3] != '-'}
held = {}   # (year, region) -> {seed: owner}
for s in range(lo, hi + 1):
    p = os.path.join(RAW, f'region_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        hh = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(hh)}
        for line in f:
            r = line.rstrip('\n').split('\t')
            y = int(r[ix['year']])
            if y not in (1870, 1913) or r[ix['region']] not in provs or int(r[ix['quarter']]) % 4 != 3 or r[ix['activeFlag']] != '1': continue
            held.setdefault((y, r[ix['region']]), {})[s] = r[ix['nation']]
NS = hi - lo + 1
o = open(OUT, 'w', encoding='utf-8', newline='\n')
o.write(f'# rh_hole hist={HIST} board={BOARD} raw={RAW} seeds={RNG}\n')
o.write('nation\tgood\thistRegion\tsimProvince\towner1783\tdataHas\theld1870\theld1913\tclass\tnationHole\tmain\tconf\topened\n')
for n, g, reg, pv, op, main, conf, note in (c[:8] for c in hist):
    gi = gid[g]
    if pv == '-': has, own, h70, h13 = '-', '-', '-', '-'
    else:
        has = 'あり' if (gi in RES.get(pv, ()) or (pv, gi) in SITEAT) else 'なし'
        own = OWN0.get(pv, '対応なし')
        h70 = sum(1 for s, nn in held.get((1870, pv), {}).items() if nn == n)
        h13 = sum(1 for s, nn in held.get((1913, pv), {}).items() if nn == n)
    if main == 'H': cl = 'H'
    elif pv == '-': cl = 'T(ii)'
    elif own != n: cl = 'T(iii)' + ('+D' if has == 'なし' else '')
    elif has == 'なし': cl = 'D' if int(op) <= 1850 else 'L'
    elif h13 * 2 < NS: cl = 'T(i)'
    else: cl = '一致'
    b = NB.get((n, g))
    if b is None: nh = '国の行なし（産地 0・産出 0）'
    else:
        zero = b[h['P1783']] == '0' and b[h['C1783']] == '0' and b[h['siteRows']] == '-' and float(b[h['held1870_med']]) == 0 and float(b[h['held1913_med']]) == 0
        nh = '国の穴' if zero else ('持ち州で 0（1783 はあり）' if float(b[h['held1913_med']]) == 0 else '国はあり')
    o.write(f'{n}\t{g}\t{reg}\t{pv}\t{own}\t{has}\t{h70}/{NS}\t{h13}/{NS}\t{cl}\t{nh}\t{main}\t{conf}\t{op}\n')
o.close(); print('ok', OUT, len(hist))
