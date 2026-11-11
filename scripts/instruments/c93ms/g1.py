# (1007) c93ms G1 の集計：merch_s と cpop_s の突き合わせ（植民地格の州×四半期）・行の数・突然変異の当て直し
# 使い方：python g1.py <計器つき 10 種の生のフォルダ> [種の数=10]
import csv, sys, os
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
out = []
tot = dict(matched=0, diff=0, diffMut3=0, core=0, coreWithCp=0, rows=0, cearnRows=0, lockB=0, blockedM=0, blockedA=0, colRows=0, colEmpty=0, colLockB=0, colBlocked=0, mEmptyAll=0)
for s in range(1, ns + 1):
    mp = os.path.join(d, f'merch_s{s}.tsv'); cp = os.path.join(d, f'cpop_s{s}.tsv'); ce = os.path.join(d, f'cearn_s{s}.tsv')
    cpm = {}
    with open(cp, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        ci, pk = ix['className'], ix['popK']
        for x in r:
            if x[ci] == '商人': cpm[(x[0], x[2], x[3])] = float(x[pk]) <= 1e-9
    m = dict(matched=0, diff=0, diffMut3=0, core=0, coreWithCp=0, rows=0, lockB=0, blockedM=0, blockedA=0, colRows=0, colEmpty=0, colLockB=0, colBlocked=0, mEmptyAll=0)
    with open(mp, encoding='utf-8', newline='') as f:
        r = csv.reader(f, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
        assert len(h) >= 22, len(h)   # (1027) 合流：merch_s は (1024) c96kf で末尾に 7 列（29 列）＝列は名で引くので下限だけ
        for x in r:
            m['rows'] += 1
            col = x[ix['tier']] == 'Colonial'
            me = x[ix['merchantEmpty']] == '1'
            lb = x[ix['lockBOpen']] == '1'
            m['lockB'] += lb; m['blockedM'] += x[ix['lockABlockedMerchant']] == '1'; m['blockedA'] += x[ix['lockABlockedArtisan']] == '1'
            m['mEmptyAll'] += me
            if col:
                m['colRows'] += 1; m['colEmpty'] += me; m['colLockB'] += lb; m['colBlocked'] += (lb and me)
            else: m['core'] += 1
            k = (x[0], x[ix['nation']], x[ix['region']])
            if k in cpm:
                if not col: m['coreWithCp'] += 1
                m['matched'] += 1
                if cpm[k] != me: m['diff'] += 1
                mk = float(x[ix['merchantK']]); ak = float(x[ix['artisanK']])
                if cpm[k] != (mk + ak <= 1e-9): m['diffMut3'] += 1
    nce = sum(1 for _ in open(ce, encoding='utf-8')) - 1
    out.append(f"s{s}: merch行 {m['rows']} cearn行 {nce} {'一致' if nce == m['rows'] else '不一致'}  植民地格 {m['colRows']} 商人が空 {m['colEmpty']}（{m['colEmpty']/max(1,m['colRows']):.4f}） 突き合わせ {m['matched']} ずれ {m['diff']} 突然変異Mu3のずれ {m['diffMut3']}  本国格の行 {m['core']} うち cpop有 {m['coreWithCp']}  錠B開 {m['lockB']} 植民地の錠B開 {m['colLockB']} 錠Aで止まる(商人) {m['blockedM']} (植民地 {m['colBlocked']}) 職人 {m['blockedA']}")
    for k in m:
        if k in tot: tot[k] += m[k]
    tot['cearnRows'] += nce
out.append('合計 ' + ' '.join(f'{k}={v}' for k, v in tot.items()))
print('\n'.join(out))
