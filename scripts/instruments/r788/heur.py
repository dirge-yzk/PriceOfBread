# r788：生（cessp_s・wars_s）だけで、同じ年・同じ敗者の複数の講和を分ける近似（月と連鎖の根が無い腕＝N1・N2・S11 用）。
# 各講和を wars_s の戦争（当事者の組・終わりの年の幅）に当て、同じ（開戦の年・月数）を共有する講和＝〈同じ開戦の連鎖で同じ月に終わった〉の近似。
# 使い方: python -I heur.py <ins フォルダ> [種の起点=1] [種の数=10] [国=全て]
import sys, os, csv, collections
d = sys.argv[1]; s0 = int(sys.argv[2]) if len(sys.argv) > 2 else 1; n = int(sys.argv[3]) if len(sys.argv) > 3 else 10
only = sys.argv[4] if len(sys.argv) > 4 else None
tot = collections.Counter(); unmatched = 0; sa1 = collections.Counter()
for s in range(s0, s0 + n):
    wars = [r for r in csv.DictReader(open(os.path.join(d, f'wars_s{s}.tsv'), encoding='utf-8'), delimiter='\t')]
    for i, w in enumerate(wars): w['i'] = i; w['used'] = False
    peaces = [r for r in csv.DictReader(open(os.path.join(d, f'cessp_s{s}.tsv'), encoding='utf-8'), delimiter='\t') if r['loserIsNationFlag'] == '1' and r['winnerIsNationFlag'] == '1']
    for p in peaces:
        y = int(p['year']); pair = {p['winner'], p['loser']}
        cand = [w for w in wars if not w['used'] and w['civilWarFlag'] == '0' and {w['sideA'], w['sideB']} == pair
                and int(w['startYear']) + int(float(w['months'])) // 12 <= y <= int(w['startYear']) + (int(float(w['months'])) + 11) // 12]
        if not cand: p['w'] = None; unmatched += 1; continue
        w = cand[0]; w['used'] = True; p['w'] = w
    g = collections.defaultdict(list)
    for p in peaces: g[(p['year'], p['loser'])].append(p)
    for (y, lo), ps in g.items():
        if only and lo != only: continue
        k = len(ps)
        key = collections.Counter((p['w']['startYear'], p['w']['months']) for p in ps if p['w'])
        for p in ps:
            if k == 1: c = 'single'
            elif p['w'] and key[(p['w']['startYear'], p['w']['months'])] >= 2: c = 'sameStartAndLen'
            else: c = 'otherInYear'
            tot[c] += 1
            if abs(float(p['absScoreIndex']) - 1.5) < 1e-9: sa1[c] += 1
T = sum(tot.values())
print(f'# {d} 種 {s0}〜{s0+n-1} 国={only or "全て"}・当たらなかった講和 {unmatched}')
for c in ['single', 'sameStartAndLen', 'otherInYear']:
    print(f'{c}\t{tot[c]}\t{tot[c]/T if T else 0:.3f}\t崩壊の満点(1.5) {sa1[c]}')
