# (1097) c105rr 段 0 の予言の外れの帰属（読むだけ・記録）：
#   ①trrail の州×年（水運でない・needEnd＞0＝発見の後）の phiEnd を、その国の発見からの年数ごとに（中央・φ＜0.99 の割合・φ＝0 の割合）。
#   ②φ＜0.99 の州×年を、〈脅威を除いた分母なら足りる〉（railTrackEnd ≧ needEnd×railTargetNoThreatIndex÷railTargetIndex）と〈それでも足りない〉に分ける＝脅威の項で need が膨らむ分か、買えない州か。
#   ③trnation の railFillShare（RailCapacity÷計器の目標）と、脅威を除いた分母での充足（RailCapacity÷railTargetNoThreatIndex）の 9 国の年ごとの中央。
#   ④発見の後（needEnd＞0）の T＝0 の側の数（g1ZeroSides）を腕と旗 0（ident の腕 b の trrail）で並べる。
# 使い方: python -X utf8 -I attrib_rr.py <腕の ins> <旗 0 の trrail のフォルダ>
import sys, os, collections, statistics as st
sys.stdout.reconfigure(newline='\n')
A, Z = sys.argv[1], sys.argv[2]
NAT9 = ['清', 'ロシア', 'オスマン', 'フランス', 'イギリス', 'プロイセン', 'オーストリア', '日本', 'アメリカ']
def read(p):
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {x: i for i, x in enumerate(h)}
        for ln in f: yield ix, ln.rstrip('\n').split('\t')
byk = collections.defaultdict(list); split = collections.Counter(); zero = collections.Counter()
for s in range(1, 11):
    nat = {}
    for ix, c in read(os.path.join(A, f'trnation_s{s}.tsv')):
        try: nat[(int(c[ix['year']]), c[ix['nation']])] = (float(c[ix['railTargetIndex']]), float(c[ix['railTargetNoThreatIndex']]))
        except ValueError: pass
    disc = {}
    rows = list(read(os.path.join(A, f'trrail_s{s}.tsv')))
    for ix, c in rows:
        if float(c[ix['needEnd']]) > 0:
            k = c[ix['nation']]; y = int(c[ix['year']])
            if k not in disc or y < disc[k]: disc[k] = y
    for ix, c in rows:
        if c[ix['waterFlag']] == '1': continue
        need = float(c[ix['needEnd']])
        if not (need > 0): continue
        y = int(c[ix['year']]); k = c[ix['nation']]; t = float(c[ix['railTrackEnd']]); phi = float(c[ix['phiEnd']])
        byk[min(40, (y - disc[k]) // 5 * 5)].append(phi)
        zero['arm'] += int(c[ix['g1ZeroSides']])
        if phi < 0.99:
            tg = nat.get((y, k))
            if tg and tg[0] > 0 and t >= need * tg[1] / tg[0]: split['脅威を除けば足りる'] += 1
            else: split['それでも足りない'] += 1
        else: split['φ≧0.99'] += 1
    zp = os.path.join(Z, f'trrail_s{s}.tsv')
    if os.path.exists(zp):
        disc0 = {}
        rz = list(read(zp))
        for ix, c in rz:
            g = int(c[ix['g1ZeroSides']])
            zero['flag0_all'] += g
print('① 発見からの年数（5 年刻み・40＝40 年以上）ごとの φ（州×年・水運でない・発見の後）：')
for k in sorted(byk):
    v = byk[k]
    print(f'  {k:>2} 年〜\t州×年 {len(v)}\t中央 {st.median(v):.3f}\tφ＜0.99 {sum(1 for x in v if x < 0.99) / len(v):.3f}\tφ＝0 {sum(1 for x in v if x == 0) / len(v):.3f}')
tot = sum(split.values())
print('② φ＜0.99 の州×年の内訳：' + '・'.join(f'{k} {v}（{v / max(1, tot):.3f}）' for k, v in split.items()))
fill = collections.defaultdict(list); filln = collections.defaultdict(list)
for s in range(1, 11):
    for ix, c in read(os.path.join(A, f'trnation_s{s}.tsv')):
        if c[ix['nation']] not in NAT9: continue
        y = int(c[ix['year']])
        if y % 10: continue
        try:
            fs = float(c[ix['railFillShare']]); tg = float(c[ix['railTargetIndex']]); tn = float(c[ix['railTargetNoThreatIndex']])
        except ValueError: continue
        if not (fs > 0): continue
        fill[y].append(fs); filln[y].append(fs * tg / tn if tn > 0 else float('nan'))
print('③ 9 国の railFillShare（RailCapacity÷目標）と脅威を除いた分母での充足の中央（10 年ごと・充足が正の国×種）：')
for y in sorted(fill): print(f'  {y}\t{st.median(fill[y]):.3f}\t{st.median([x for x in filln[y] if x == x]):.3f}\tn={len(fill[y])}')
print(f'④ T＝0 の側の数（g1ZeroSides・水運でない）：腕の発見の後 {zero["arm"]}・旗 0 の全期間 {zero["flag0_all"]}（旗 0 は全ての側が T＝0）')
