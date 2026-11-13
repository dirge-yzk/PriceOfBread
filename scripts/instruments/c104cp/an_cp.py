# (1090) c104cp：center_s の生から事前登録 [4] の予言 1〜7 と点火の件数を数える（読むだけ）。使い方：python -X utf8 -I an_cp.py <計器の出力フォルダ> [seeds]
import csv, sys, collections, math

d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2].split(',')] if len(sys.argv) > 2 else range(1, 11)


def fl(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def pct(v, p):
    v = sorted(x for x in v if x == x)
    if not v:
        return float('nan')
    return v[min(len(v) - 1, int(p * len(v)))]


PNAME = {0: '絶対', 1: '専制', 2: '幕府', 3: '立憲', 4: '共和', 5: 'ブル独', 6: 'プロ独', 7: '軍政', 8: '無政府'}
tot = collections.Counter()
by_pol = collections.defaultdict(collections.Counter)
q0 = collections.Counter()
crowd = collections.defaultdict(list)
ratio = collections.defaultdict(list)
dw = []
safety_up = collections.Counter()
da65 = []
coup_rows = []
name_match = collections.Counter()
mref = collections.Counter()
for s in seeds:
    rows = list(csv.DictReader(open(f'{d}/center_s{s}.tsv'), delimiter='\t'))
    last = collections.defaultdict(dict)
    prevw = {}
    for r in rows:
        n = r['nation']; q = int(r['quarter']); pol = int(r['polityCode'])
        if q == 0:
            q0['rows'] += 1
            reg = fl(r['shareRegime']);
            if reg > fl(r['shareArmy']) and reg > fl(r['shareRev']):
                q0['regime_biggest'] += 1
            if r['nameCode'] == r['polityCode']:
                q0['name_eq'] += 1
            if fl(r['sharePart']) > fl(r['shareExec']):
                q0['part_gt_exec'] += 1
            if r['coupFlag'] == '1':
                q0['coup'] += 1
        busy = r['inCivilWarFlag'] == '1' or r['pendingRebelFlag'] == '1'
        for k in ('ignitionViolentFlag', 'ignitionPeacefulFlag', 'coupFlag', 'immediateFlag'):
            if r[k] == '1' and not busy:
                tot[k + '_raw'] += 1
                by_pol[k][PNAME.get(pol, pol)] += 1
                if last[k].get(n) != q - 1:
                    tot[k + '_fold'] += 1
                last[k][n] = q
        if r['coupFlag'] == '1' and not busy:
            coup_rows.append((s, n, int(r['year']), PNAME.get(pol, pol), fl(r['armyShare'])))
        if fl(r['armyShare']) > 0.65:
            da65.append((s, n, int(r['year'])))
        c = fl(r['crowdToGIndex'])
        if c == c:
            crowd[PNAME.get(pol, pol)].append(c)
        rr = fl(r['radicalToReformIndex'])
        if rr == rr and rr != float('inf'):
            ratio[PNAME.get(pol, pol)].append(rr)
        w = fl(r['walletShare'])
        if n in prevw and prevw[n][0] == q - 1:
            dw.append(abs(w - prevw[n][1]))
        prevw[n] = (q, w)
        name_match['rows'] += 1
        name_match['name_eq_pol'] += r['nameCode'] == r['polityCode']
        name_match['name_eq_derived'] += r['nameCode'] == r['derivedPolityCode']
        sk, sp = fl(r['safetyKIndex']), fl(r['safetyKPlusIndex'])
        if sp == sp and sk == sk and sp < sk:
            safety_up[(n, int(r['year']) // 10 * 10)] += 1
        if n == '日本':
            for k in ('mRefOutClosedIndex', 'mRefOutEliteIndex', 'mRefOutMassIndex'):
                mref[k] += fl(r[k])

print('q0 (種×61国):', dict(q0))
for k in ('ignitionViolentFlag', 'ignitionPeacefulFlag', 'coupFlag', 'immediateFlag'):
    print(k, '行', tot[k + '_raw'], '畳み後', tot[k + '_fold'], '政体別', dict(by_pol[k]))
print('d_A>0.65 の国×四半期', len(da65), '国数', len(set(x[1] for x in da65)), 'クーデタ行', len(coup_rows), coup_rows[:12])
for k, v in sorted(crowd.items()):
    print('qM/G', k, 'n', len(v), 'p1', round(pct(v, .01), 4), 'p50', round(pct(v, .5), 4), 'p99', round(pct(v, .99), 4))
for k, v in sorted(ratio.items()):
    print('急進/改革', k, 'n', len(v), 'p50', round(pct(v, .5), 3), 'p90', round(pct(v, .9), 3))
print('|Δw| p99', round(pct(dw, .99), 4), 'p50', round(pct(dw, .5), 4), 'n', len(dw))
print('名=今の政体', name_match['name_eq_pol'], '/', name_match['rows'], ' 名=導出の政体', name_match['name_eq_derived'])
print('K+1 で安全の比が下がる (国,年代) 上位', safety_up.most_common(12))
print('日本 M_改外 階級別の和', dict(mref))
