# (1435) c154sd 門 G3・O1・O2 と記録 O3・同じ盤面の新÷旧（facfloor2_s を読むだけ・エンジンと係数を読まない）。
#   使い方: python sd_gate.py <生のフォルダ> [種の数=10] [種の起点=1] [G3 上の上限（省けば記録）]
#   G3 下：Σ facSeenTargetK ≥ 1.10×Σ targetAboveNK（相対 1e-6）・G3 上：Σ facSeenTargetK÷Σ targetK ≤ 上限・案 A の値 v＝Σ targetAboveRK÷Σ targetK も出す。
#   O1：facBelow1P の和＝0。O2：facMinSizeP ≥ 0.01×minRegionPopP（facEngCount>0 の行・相対 1e-6）。O3：partyQualFac・partyQualSizeMinP。
import sys, os, csv
csv.field_size_limit(1 << 30)
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10; s0 = int(sys.argv[3]) if len(sys.argv) > 3 else 1
up = float(sys.argv[4]) if len(sys.argv) > 4 else None
S = dict(seen=0.0, aboveN=0.0, aboveR=0.0, aboveP=0.0, target=0.0, milD=0.0, milM2=0.0, fight=0.0, cN=0, cR=0, cP=0, seenC=0, engC=0, fac=0)
rows = 0; o1 = 0; o2bad = 0; o2rows = 0; minsz = []; qual = 0; qmins = []; flags = set(); miss = 0; engNe = 0; riot = 0
for s in range(s0, s0 + ns):
    p = os.path.join(d, f"facfloor2_s{s}.tsv")
    if not os.path.exists(p): miss += 1; continue
    with open(p, encoding="utf-8", newline="") as f:
        r = csv.reader(f, delimiter="\t"); h = next(r); ix = {k: i for i, k in enumerate(h)}
        for x in r:
            rows += 1; g = lambda k: float(x[ix[k]])
            flags.add((x[ix['floorFlag']], x[ix['strengthFlag']]))
            S['seen'] += g('facSeenTargetK'); S['aboveN'] += g('targetAboveNK'); S['aboveR'] += g('targetAboveRK'); S['aboveP'] += g('targetAbovePK')
            S['target'] += g('targetK'); S['milD'] += g('milSumDK'); S['milM2'] += g('milSumM2K'); S['fight'] += g('fightHeadsK')
            S['cN'] += int(g('cellsAboveN')); S['cR'] += int(g('cellsAboveR')); S['cP'] += int(g('cellsAboveP'))
            S['seenC'] += int(g('facSeenCount')); S['engC'] += int(g('facEngCount')); S['fac'] += int(g('factionsCount'))
            if int(g('facSeenCount')) != int(g('facEngCount')): engNe += 1
            o1 += int(g('facBelow1P')); riot += int(g('riotFire'))
            if int(g('facEngCount')) > 0:
                o2rows += 1; m = g('facMinSizeP'); minsz.append(m)
                if m < 0.01 * g('minRegionPopP') * (1 - 1e-6): o2bad += 1
            qual += int(g('partyQualFac'))
            if g('partyQualSizeMinP') >= 0: qmins.append(g('partyQualSizeMinP'))
def q(a, ps):
    a = sorted(a); return ['%.4g' % a[min(len(a) - 1, int(p * len(a)))] for p in ps] if a else []
print(f"ins={d} 種={s0}..{s0+ns-1} 表なし={miss} 行={rows} 旗(floor,strength)={sorted(flags)}")
r3 = S['seen'] / S['aboveN'] if S['aboveN'] > 0 else float('nan')
print(f"G3下\tΣfacSeenTargetK={S['seen']:.6g}\tΣtargetAboveNK={S['aboveN']:.6g}\t比={r3:.6f}\t門≥1.10：{'通る' if r3 >= 1.10 * (1 - 1e-6) else '赤'}")
v = S['aboveR'] / S['target'] if S['target'] > 0 else float('nan'); u = S['seen'] / S['target'] if S['target'] > 0 else float('nan')
print(f"G3上\t案Aの値 v=ΣtargetAboveRK÷ΣtargetK={v:.6f}\t腕の値 ΣfacSeenTargetK÷ΣtargetK={u:.6f}\tφP_n の値 ΣtargetAbovePK÷ΣtargetK={S['aboveP']/S['target'] if S['target']>0 else float('nan'):.6f}"
      + (f"\t門≤{up}：{'通る' if u <= up * (1 + 1e-6) else '赤'}" if up is not None else "\t（記録）"))
print(f"O1\tfacBelow1P の和={o1}\t門＝0：{'通る' if o1 == 0 else '赤'}")
print(f"O2\tfacEngCount>0 の行={o2rows}\tφ×最小の州を下回る行={o2bad}\t門＝0：{'通る' if o2bad == 0 else '赤'}\tfacMinSizeP（人）p1/p10/p50={q(minsz,[0.01,0.1,0.5])}")
print(f"O3（記録）\tpartyQualFac の和={qual}\tpartyQualSizeMinP（人）p1/p10/p50={q(qmins,[0.01,0.1,0.5])}")
print(f"記録\t組の数 φN={S['cN']}・φN_r={S['cR']}・φP_n={S['cP']}\t見えた勢力={S['seenC']}・エンジンが書いた勢力={S['engC']}・不一致の行={engNe}\t勢力の数の平均={S['fac']/max(1,rows):.4f}")
print(f"記録\t強さの頭数（見えた勢力）ΣNπM={S['milD']:.6g}・ΣNM²={S['milM2']:.6g}・ΣNM={S['fight']:.6g}\t新÷旧（ΣNM²÷ΣNπM）={S['milM2']/S['milD'] if S['milD']>0 else float('nan'):.4f}\triotFire の和={riot}")
