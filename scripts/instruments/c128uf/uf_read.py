"""(1258) c128uf 機構の観測量の読み手（読むだけ・エンジンの関数を呼ばない）。
  U＝統一の宣戦＝unifwar_s の engineCode=0（Formations.UnificationWarBlock が None）の行のうち、同じ四半期・同じ攻め手と標的の wars_s の declare（内戦でない）がある物。
  攻め手の候補＝formation2_s の同じ四半期・同じ国の行のうち、未達（ownDirect+ownDep＜need）で段が最小の規則（規則の札＝文化があれば文化・無ければ規則の名）。
  UA＝U のうち、候補の札と同じ札の名（下の表）を他の国が先の四半期から名乗り、その国がその四半期にまだある物（段≧候補の段）。
  EX＝候補の四半期（国×四半期）のうち、その札が他の国で成立済みの物＝新しい規則が候補を消す四半期（旧の規則なら候補が立つ・同じ盤面の旧と新の差）。
  表（読み手が自分で持つ・エンジンの表を読まない）：名→（札, 段）。
  使い方: python uf_read.py <生> <種の起点> <種の終わり> [名札]"""
import sys, csv, collections
FORMED = {'ドイツ帝国': ('ドイツ', 2), '神聖ローマ帝国': ('ドイツ', 3), 'ドイツ': ('ドイツ', 2), '神聖ローマ': ('ドイツ', 3),
          'イタリア王国': ('イタリア', 2), 'ローマ帝国': ('イタリア', 3), 'イタリア': ('イタリア', 2), '北ドイツ連邦': ('北ドイツ連邦', 1)}
KEY = {'ドイツ': ('ドイツ', 2), 'イタリア': ('イタリア', 2), '北ドイツ連邦': ('北ドイツ連邦', 1)}
raw, s0, s1 = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
lab = sys.argv[4] if len(sys.argv) > 4 else ''
TU = TA = TN = TX = 0; att = collections.Counter(); keyc = collections.Counter()
for s in range(s0, s1 + 1):
    f = open(f'{raw}/nation_s{s}.tsv', encoding='utf-8'); h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
    alive = collections.defaultdict(set); formed = []   # (q, nation, key, tier)
    last = {}
    for line in f:
        c = line.rstrip('\n').split('\t'); n = c[ix['nation']]; dn = c[ix['displayName']]; q = int(c[ix['quarter']])
        alive[q].add(n)
        if last.get(n) != dn and dn in FORMED and dn != n: formed.append((q, n) + FORMED[dn])
        last[n] = dn
    cand = {}
    for r in csv.DictReader(open(f'{raw}/formation2_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        if r['rule'] not in KEY: continue
        if int(r['ownDirect']) + int(r['ownDep']) >= int(r['need']): continue
        k = (r['nation'], int(r['q'])); t = int(r['ruleTier'])
        if k not in cand or t < cand[k][1]: cand[k] = KEY[r['rule']]
    def fe(nat, q, cd): return any(fq < q and fn != nat and fk == cd[0] and ft >= cd[1] and fn in alive[q] for fq, fn, fk, ft in formed)
    EX = sum(1 for (nat, q), cd in cand.items() if fe(nat, q, cd))
    dec = set()
    for r in csv.DictReader(open(f'{raw}/wars_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        if r['entryCode'] == 'declare' and r['civilWarFlag'] == '0': dec.add((r['sideA'], r['sideB'], int(r['q'])))
    U = A = nocand = 0; seen = set()
    for r in csv.DictReader(open(f'{raw}/unifwar_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        if r['engineCode'] != '0': continue
        k = (r['attacker'], r['target'], int(r['q']))
        if k not in dec or k in seen: continue
        seen.add(k); U += 1
        cd = cand.get((r['attacker'], k[2]))
        if cd is None: nocand += 1; continue
        keyc[cd[0]] += 1
        if fe(r['attacker'], k[2], cd):
            A += 1; att[r['attacker']] += 1
    TU += U; TA += A; TN += nocand; TX += EX
    print(f'{lab} 種 {s}: 統一の宣戦 U {U}・成立の後の同じ札 UA {A}・候補の読めない行 {nocand}・EX {EX}・成立 {[(fn, fk, ft, 1783 + fq // 4) for fq, fn, fk, ft in formed if fk in ("ドイツ", "イタリア", "北ドイツ連邦")]}')
print(f'{lab} 和: U {TU}・UA {TA}（{100.0 * TA / max(TU, 1):.1f}%）・候補の読めない行 {TN}・EX {TX}・札 {dict(keyc)}・UA の攻め手 {att.most_common(6)}')
