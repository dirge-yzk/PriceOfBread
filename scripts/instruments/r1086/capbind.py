# (1086) 読むだけ：受入の上限（MultiNationSim.cs:1725＝ImmigrationReceiveCapPerQuarter 0.012×州の人口×移民制限法の率×AbsorbCap）にどれだけ近いか。
# 使い方：python -X utf8 -I capbind.py <base_ins> <種の数>
# 分母＝0.012×州の人口×移民制限法の率（monlaw_s の ImmigrationRestriction を国×四半期で結合・開放 1.0／選別 0.5／閉鎖 0.2）。
# 群：行き先の国のその四半期の法で 3 つ（開放×国教のみ＝案 A の異教の上限が効く唯一の群／選別か閉鎖＝min(率, 選別) で変わらない／開放×内心の自由以上）。
# 限界（自己申告）：AbsorbCap（Territory.cs:203＝(食料の充足×住居の充足×土地の因子)^2・0〜1）は表に無い＝分母は上限の上界、比は実際の比の下界。
#   region_s の越境の流入は上限で縮めた後の量（MoveMigrants が Instruments.CrossBorder に足した flow）。比が 1 に近い所は上限が掛かっている。
import sys, collections as C
base, nseed = sys.argv[1], int(sys.argv[2])
RATE = {0: 1.0, 1: 0.5, 2: 0.2}
B = ['<0.1', '0.1-0.25', '0.25-0.5', '0.5-0.9', '>=0.9']
def bin_(r):
    return B[0] if r < 0.1 else B[1] if r < 0.25 else B[2] if r < 0.5 else B[3] if r < 0.9 else B[4]
tot = C.Counter(); bins = C.Counter(); cut = 0.0; gv = 0.0
for s in range(1, nseed + 1):
    law = {}; grp = {}
    f = open(f'{base}/monlaw_s{s}.tsv', encoding='utf-8')
    h = f.readline().rstrip('\n').split('\t')
    iq, inn, iim, isr = h.index('quarter'), h.index('nation'), h.index('ImmigrationRestriction'), h.index('StateReligion')
    for line in f:
        c = line.rstrip('\n').split('\t'); k = (int(c[iq]), c[inn]); im = int(c[iim]); sr = int(c[isr])
        law[k] = RATE.get(im, 1.0)
        grp[k] = '開放×国教のみ' if im == 0 and sr == 0 else '選別か閉鎖' if im > 0 else '開放×内心の自由以上'
    f.close()
    f = open(f'{base}/region_s{s}.tsv', encoding='utf-8')
    h = f.readline().rstrip('\n').split('\t')
    iq, inn, ip, ii = h.index('quarter'), h.index('nation'), h.index('populationK'), h.index('crossBorderInKPerQuarter')
    for line in f:
        c = line.rstrip('\n').split('\t')
        v = float(c[ii]); p = float(c[ip])
        if not (v > 0) or not (p > 0): continue
        n = c[inn]; lr = law.get((int(c[iq]), n), 1.0)
        b = bin_(v / (0.012 * p * lr))
        if grp.get((int(c[iq]), n)) == '開放×国教のみ':
            gv += v; cut += max(0.0, v - 0.5 * 0.012 * p)   # 流入が全て異教で AbsorbCap=1 とした時に、異教の上限（選別 0.5）で削られる量
        for g in ('全て', n if n in ('清', '日本') else 'その他', grp.get((int(c[iq]), n), '法の不明')):
            tot[g] += v; bins[(g, b)] += v
    f.close()
print('流入÷(0.012×州の人口×移民制限法の率) の帯ごとの流入の量の割合（種 1〜%d・AbsorbCap を除く＝比の下界）' % nseed)
for g in ('全て', '清', '日本', 'その他', '開放×国教のみ', '選別か閉鎖', '開放×内心の自由以上', '法の不明'):
    if not tot[g]: continue
    print(g, f'(量 {tot[g]/tot["全て"]:.1%})', ' '.join(f'{b}:{bins[(g, b)]/tot[g]:.1%}' for b in B))
print(f'開放×国教のみの群：流入が全て異教・AbsorbCap=1 とした時の削られる量の割合 {cut/gv:.1%}（群の中）・全体の {cut/tot["全て"]:.1%}（異教の割合で下がり、AbsorbCap<1 で上がる）')
