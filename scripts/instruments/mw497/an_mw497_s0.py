#!/usr/bin/env python
# (497) 線 mw 段 0 の読み手＝ma_s*.tsv（計器つき 10 種）から、最低賃金法の AI が前の四半期に置かれていた目標を書き戻した回数を、出所と結果で割って数える（読むだけ）。
# 使い方：python an_mw497_s0.py <ins のフォルダ> [種の数=10]    例）python an_mw497_s0.py D:/PoB_work/keep_491/m491_ins
# 読み方（行 a＝四半期 q-1 の行・行 b＝四半期 q の行・同じ国・連続する四半期だけ。連続する行があるのは ma_s の挙げた国〔毎四半期〕だけ）：
#   立っていた目標＝policyTarget(a) ≠ lawCode(a)（前の四半期の末に目標が現行法と違った）。この四半期の初めの法は lawCode(a)（法は四半期の途中では動かない前提）。
#   出所：規則由来＝ruleTargetOrd(a) == policyTarget(a)（規則の値がそのまま末まで残った）／それ以外＝要求・譲歩・引き継ぎ（規則が書かなかった〔-1〕か、規則の後に別の経路が置いた）。
#   結果（行 b の規則の書き）：
#     保ち＝ruleTargetOrd(b) == 立っていた目標（同じ値を書き直した）／null＝規則が書かなかった（-1）／書き戻し＝ruleTargetOrd(b) == lawCode(a)（現行法へ戻した）／別＝それ以外（向きを変えた）。
#   書き戻しのうち 正味で消えた＝policyTarget(b) == lawCode(b) かつ lawCode(b) == lawCode(a)（規則の後の要求の置き直しも無く、法も動かなかった）／要求で置き直された＝それ以外。
# 限界：①policyTarget は四半期の末（GateAllLawTargets と施行の後）の値で、規則の直後ではない／②出所の分けは行の値からの推定／③ruleTargetOrd＝−1 は〈書かない〉と〈間の帯で書かない〔旗 mw=1〕〉の二義（wantNew==lawCode なら後者）。
import csv, sys, collections, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
D = sys.argv[1].rstrip('/') + '/'
N = int(sys.argv[2]) if len(sys.argv) > 2 else 10
def period(q):
    return 'P1' if q < 68 else 'P2' if q < 268 else 'P3' if q < 468 else 'P4'
tot = collections.Counter(); per = collections.defaultdict(collections.Counter); byNation = collections.Counter()
for s in range(1, N + 1):
    rows = collections.defaultdict(list)
    for r in csv.DictReader(open(D + f'ma_s{s}.tsv', encoding='utf-8'), delimiter='\t'):
        rows[r['nation']].append((int(r['quarter']), int(r['lawCode']), int(r['ruleTargetOrd']), int(r['policyTarget']), int(r['wantNew'])))
    c = collections.Counter()
    for n, rs in rows.items():
        rs.sort()
        for a, b in zip(rs, rs[1:]):
            if b[0] != a[0] + 1: continue
            c['pairs'] += 1
            cur0, prior = a[1], a[3]
            if prior == cur0: continue
            origin = 'rule' if a[2] == prior else 'other'
            c['standing'] += 1; c['standing_' + origin] += 1
            rb = b[2]
            if rb == prior: res = 'kept'
            elif rb == -1: res = 'null'
            elif rb == cur0:
                res = 'erased_net' if (b[3] == b[1] and b[1] == cur0) else 'erased_replaced'
            else: res = 'other'
            c[f'{origin}_{res}'] += 1
            per[period(b[0])][f'{origin}_{res}'] += 1
            if res == 'erased_net': byNation[n] += 1
    tot.update(c)
    print(f's{s}', dict(sorted(c.items())))
print('TOTAL', dict(sorted(tot.items())))
for p in sorted(per): print(p, dict(sorted(per[p].items())))
print('正味で消えた回数の多い国（10 種合計）', byNation.most_common(8))
