"""(1254) 記録：統一が成立した後の同じ規則の宣戦（(1249) 反証役 v2 の中を合わせた木で数える・dec3.py の汎用版）。
成立＝nation_s の displayName が CultureRealms の名（formations.py の NAMES）に替わった最初の年。規則＝formation2_s の rule（国ごとの最初の行）。
数える物：wars_s の entryCode=declare の宣戦のうち、攻め手の規則が成立した国の規則と同じで、成立の年より後に始まった物。
使い方: python dec_after.py <生> <種の起点> <種の終わり>"""
import sys, os, csv, collections
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'c124nr'))
NAMES = ['北ドイツ連邦', 'ドイツ帝国', '神聖ローマ帝国', 'イタリア王国', 'ローマ帝国', 'アラブ王国', 'カリフ国', 'マラーター王国', 'マラーター帝国']
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
T = collections.Counter()
for s in range(s0, s1 + 1):
    rule = {}
    for r in csv.DictReader(open(f'{raw}/formation2_s{s}.tsv', encoding='utf-8'), delimiter='\t'): rule.setdefault(r['nation'], r['rule'])
    formed = {}
    with open(f'{raw}/nation_s{s}.tsv', encoding='utf-8') as fh:
        h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in fh:
            c = line.rstrip('\n').split('\t')
            if c[ix['displayName']] in NAMES:
                ru = rule.get(c[ix['nation']])
                if ru: formed.setdefault(ru, int(c[ix['year']]))
    dec = [r for r in csv.DictReader(open(f'{raw}/wars_s{s}.tsv', encoding='utf-8'), delimiter='\t') if r['entryCode'] == 'declare' and r['civilWarFlag'] == '0']
    after = [r for r in dec if rule.get(r['sideA']) in formed and int(r['startYear']) > formed[rule[r['sideA']]]]
    T['declare'] += len(dec); T['after'] += len(after)
    print(f'種 {s}: 宣戦 {len(dec)}・成立の後の同じ規則 {len(after)}・成立 {formed}・攻め手 {collections.Counter(r["sideA"] for r in after).most_common(3)}')
print(f'和: 宣戦 {T["declare"]}・成立の後の同じ規則 {T["after"]}（{(T["after"]/T["declare"]*100 if T["declare"] else 0):.1f}%）・除いた Hu1 の平均 {(T["declare"]-T["after"])/(s1-s0+1):.1f}')
