# c119k3 (1194) 紙の台本：今の盤面（keep_1191/base_ins）で、PeacetimeConscriptGrievance の対象になる州×四半期を数える。
# 対象＝平時（その四半期の atWarMonths==0）・徴兵法≥選抜（conscriptionLaw>=1）・(tier==Colonial か nowRule1GtRule0==1)。
# Military.cs:150〜:161 の条件を表の列から自分で選ぶ（エンジンの関数を呼ばない）。
# 使い方: python paper_k3.py <生の ins フォルダ> <種の始め> <種の終わり> [--tag 名]
import csv, sys, statistics as st, os
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
tag = sys.argv[5] if len(sys.argv) > 5 and sys.argv[4] == '--tag' else os.path.basename(raw.rstrip('/'))
def q(xs, p):
    if not xs: return float('nan')
    xs = sorted(xs); return xs[min(len(xs)-1, int(p*len(xs)))]
print(f"# paper_k3 tag={tag} raw={raw} seeds={s0}..{s1}")
print("seed\tqualRQ\tqualColonialRQ\tqualRuleOnlyRQ\tqualNations\tpeaceLawSoldierUpK\tmilQualMed\tmilQualP90\tmilQualMax\tmilOtherColMed\tmilOtherColP90\tmilHomeMed\tdecadeFirstQual")
tot = {}
for s in range(s0, s1+1):
    nat = {}
    with open(f"{raw}/nation_s{s}.tsv", newline='', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            nat[(r['quarter'], r['nation'])] = (float(r['atWarMonths']), int(float(r['conscriptionLaw'])), float(r['soldierK']))
    # 平時・法≥選抜の四半期に兵が増えた量（国の合計・本国を含む上界）
    prev = {}; up = 0.0
    for (qq, n), (aw, law, sk) in sorted(nat.items(), key=lambda kv: (kv[0][1], int(kv[0][0]))):
        if n in prev and aw == 0 and law >= 1 and sk > prev[n]: up += sk - prev[n]
        prev[n] = sk
    qual = colq = ruleq = 0; nats = set(); mq = []; mo = []; mh = []; first = None
    with open(f"{raw}/rule_s{s}.tsv", newline='', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            k = (r['quarter'], r['nation'])
            if k not in nat: continue
            aw, law, _ = nat[k]
            col = r['tier'] == 'Colonial'; ru = r['nowRule1GtRule0'] in ('1', '1.0', 'True')
            m = float(r['militancyRegionIndex'])
            if aw == 0 and law >= 1 and (col or ru):
                qual += 1; colq += col; ruleq += (ru and not col); nats.add(r['nation']); mq.append(m)
                if first is None: first = int(r['year'])
            elif col: mo.append(m)
            else: mh.append(m)
    print(f"{s}\t{qual}\t{colq}\t{ruleq}\t{len(nats)}\t{up:.1f}\t{q(mq,.5):.4f}\t{q(mq,.9):.4f}\t{max(mq) if mq else float('nan'):.4f}\t{q(mo,.5):.4f}\t{q(mo,.9):.4f}\t{q(mh,.5):.4f}\t{first}")
# 国ごとの対象の州×四半期（10 種の和・上位 12）と、植民地格の州を持つ国×四半期のうち法が志願制で対象にならない量
import collections
byn = collections.Counter(); volcol = collections.Counter()
for s in range(s0, s1+1):
    nat = {}
    with open(f"{raw}/nation_s{s}.tsv", newline='', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            nat[(r['quarter'], r['nation'])] = (float(r['atWarMonths']), int(float(r['conscriptionLaw'])))
    with open(f"{raw}/rule_s{s}.tsv", newline='', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            k = (r['quarter'], r['nation'])
            if k not in nat: continue
            aw, law = nat[k]
            if r['tier'] == 'Colonial' and aw == 0:
                if law >= 1: byn[r['nation']] += 1
                else: volcol[r['nation']] += 1
print("# 平時の植民地格の州×四半期（法≥選抜＝対象・10 種の和・上位 12）:", ' '.join(f"{n}={c}" for n, c in byn.most_common(12)))
print("# 平時の植民地格の州×四半期（志願制＝Military.cs:150 の continue で対象外・上位 12）:", ' '.join(f"{n}={c}" for n, c in volcol.most_common(12)))
