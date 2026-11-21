# (1284) c132df 記録の読み手：arrears_s（有れば）と mintcap_s から、停止の門と本位の外の続きを数える。
#   arrears_s：種ごとの行・新と旧の門の開いた回数・国ごとの新の門の最大（往復の数）・英の新の門の年・計器の影の不履行（旧・新）の最後の値の和。
#   mintcap_s：本位の外の続き（onGold＝0 の四半期の続き・本位の法が金か銀の国だけ・続きの長さの平均と最大・国×四半期の和）。
# 使い方: python -X utf8 -I arrears_sum.py <生（D:/ の形）> <種0> <種1>
import sys, os, csv, statistics
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
UK = 'イギリス'
tot_new = tot_old = 0; spells_all = []
for s in range(s0, s1 + 1):
    p = f'{raw}/arrears_s{s}.tsv'
    if os.path.exists(p):
        rows = 0; nt = {}; ot = 0; uky = []; lastdef = {}
        for r in csv.DictReader(open(p, encoding='utf-8'), delimiter='\t'):
            rows += 1
            if r['newTrip'] == '1':
                nt[r['nation']] = nt.get(r['nation'], 0) + 1
                if r['nation'] == UK: uky.append(r['year'])
            if r['oldTrip'] == '1': ot += 1
            lastdef[r['nation']] = (int(r['oldShadowDefaults']), int(r['newShadowDefaults']))
        n = sum(nt.values()); tot_new += n; tot_old += ot
        mx = max(nt.items(), key=lambda kv: kv[1]) if nt else ('-', 0)
        od = sum(v[0] for v in lastdef.values()); nd = sum(v[1] for v in lastdef.values())
        print(f'種 {s}\tarrears_s 行 {rows}\t新の門 {n}（国 {len(nt)}・最大 {mx[0]} {mx[1]}）\t旧の門 {ot}\t英の新の門の年 {uky}\t計器の影の不履行 旧 {od}・新 {nd}')
    mp = f'{raw}/mintcap_s{s}.tsv'
    if os.path.exists(mp):
        q = {}
        for r in csv.DictReader(open(mp, encoding='utf-8'), delimiter='\t'):
            q.setdefault(r['nation'], []).append((r['law'], r['onGold']))
        sp = []
        for nat, v in q.items():
            run = 0
            for law, g in v:
                if law in ('1', '2') and g == '0': run += 1
                else:
                    if run: sp.append(run)
                    run = 0
            if run: sp.append(run)
        spells_all += sp
        print(f'種 {s}\t本位の外の続き（金か銀の法の国・四半期）件 {len(sp)}・平均 {statistics.mean(sp) if sp else 0:.1f}・最大 {max(sp) if sp else 0}・国×四半期 {sum(sp)}')
print(f'# 計：新の門 {tot_new}・旧の門 {tot_old}・本位の外の続き 件 {len(spells_all)}・平均 {statistics.mean(spells_all) if spells_all else 0:.1f}・中央 {statistics.median(spells_all) if spells_all else 0}・国×四半期 {sum(spells_all)}')
