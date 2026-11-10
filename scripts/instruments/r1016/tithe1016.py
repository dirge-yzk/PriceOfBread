# (1016) 紙 2：国教の〈次の一段で失う十分の一税の割合〉の近似（読むだけ・年 1 回の全国の断面）
# 使い方: python -X utf8 -I tithe1016.py <出力ファイル> <名>=<生のフォルダ> ...（class_s・monlaw_s・cgrp_s を読む）
# 近似の名乗り（上限の側）：
#  - 大衆の所得＝階級 1 農民・2 工場労働者・11 職人・12 使用人の earnedPerMonth の和（IsMassPop＝平民で近代でない・兵士 5 と犯罪者 14 は除く）。
#    エンジンの請求は〈聖職者の居る州の大衆の前の月の所得〉（Fiscal.cs TitheClaim）＝州の聖職者の有無は class_s から復元できない＝上限。
#  - 払える率（PayRate）を数えない＝上限。
#  - 旧支配層の所得＝階級 0 貴族・6 聖職者の earnedPerMonth の和（Build の Income[0] は POP ごとの max(0,·) の和＝ここは階級の和）。
#  - 段の十分の一税の度合い：国教のみ 1・内心の自由 0.6・信仰の自由 0.3・国家無神論 0（Coefficients.cs InnerFreedomTithe・ReligiousFreedomTithe）。
#  - 次の段：段+1（信仰の自由〔2〕の次は 2＝失う 0・Parliament の上限 2）。戻す段：段−1（0 の前は 0）。
import sys, os, statistics as st

RATE = 0.05
DEG = [1.0, 0.6, 0.3, 0.0]
MASS = {1, 2, 11, 12}
BLOC0 = {0, 6}
PERIODS = [(1783, 1830), (1831, 1870), (1871, 1910), (1911, 1951)]


def read_tsv(p):
    with open(p, 'rb') as f:
        lines = f.read().decode('utf-8').split('\n')
    h = lines[0].rstrip('\r').split('\t')
    rows = [l.rstrip('\r').split('\t') for l in lines[1:] if l.strip()]
    return h, rows


def qs(xs):
    if not xs: return "n=0"
    s = sorted(xs)
    g = lambda p: s[min(len(s) - 1, int(round(p * (len(s) - 1))))]
    return f"n={len(s)} p10={g(.1):.4f} 中央={g(.5):.4f} p90={g(.9):.4f} p99={g(.99):.4f} 最大={s[-1]:.4f}"


def fnum(x):
    try:
        v = float(x)
        return v if v == v else 0.0
    except ValueError:
        return 0.0


def main():
    out = sys.argv[1]
    L = ["# (1016) 国教：次の一段で失う十分の一税の割合（近似・上限の側）", f"率 {RATE}・度合い {DEG}", ""]
    for name, d in (a.split('=', 1) for a in sys.argv[2:]):
        loss0 = {p: [] for p in PERIODS}   # ÷旧支配層の所得
        lossC = {p: [] for p in PERIODS}   # ÷聖職者の所得
        gain0 = []                          # 戻す向き（段≧1 の行だけ）
        titheOverC = []                     # 今の段の十分の一税（上限）÷聖職者の所得
        clergyShare0 = []                   # 聖職者の所得÷旧支配層の所得
        noClergy = 0; rowsN = 0; zeroI0 = 0
        byLevel = {}
        sumNum = 0.0; sumDen = 0.0          # 和÷和（段 0 の行）
        cg_closed_in = 0.0; cg_est = 0.0    # cgrp の閉鎖の層の pop からの受け取りと、推定の十分の一税（同じ四半期）
        maj = {}
        formC = []; over1 = 0
        for s in range(1, 11):
            pc = os.path.join(d, f"class_s{s}.tsv"); pm = os.path.join(d, f"monlaw_s{s}.tsv")
            if not (os.path.exists(pc) and os.path.exists(pm)): continue
            hm, rm = read_tsv(pm)
            iq, inat, isr = hm.index("quarter"), hm.index("nation"), hm.index("StateReligion")
            law = {(int(r[iq]), r[inat]): int(float(r[isr])) for r in rm}
            hc, rc = read_tsv(pc)
            it, inn, iy, icl, isz, iea = (hc.index(c) for c in ("time", "nation", "year", "classId", "sizeK", "earnedPerMonth"))
            agg = {}
            for r in rc:
                t = r[it]
                if not t.startswith('q'): continue
                k = (int(t[1:]), r[inn]); a = agg.setdefault(k, [0.0, 0.0, 0.0, 0.0, int(r[iy])])
                c = int(r[icl]); e = fnum(r[iea]); z = fnum(r[isz])
                if c in MASS: a[0] += e
                if c in BLOC0: a[1] += e
                if c == 6: a[2] += e; a[3] += z
            # cgrp の突き合わせ（閉鎖の層の inFromPopsPerQuarter・その年の断面の四半期だけ）
            pg = os.path.join(d, f"cgrp_s{s}.tsv")
            cg = {}
            if os.path.exists(pg):
                hg, rg = read_tsv(pg)
                gq, gn, gg, gin = hg.index("quarter"), hg.index("nation"), hg.index("group"), hg.index("inFromPopsPerQuarter")
                for r in rg:
                    if r[gg] != "closed": continue
                    k = (int(r[gq]), r[gn]); cg[k] = cg.get(k, 0.0) + fnum(r[gin])
            for k, (M, I0, C, Cz, y) in agg.items():
                if k not in law: continue
                lv = law[k]; rowsN += 1
                if Cz <= 1e-9 or C <= 0: noClergy += 1; continue
                if I0 <= 1e-9: zeroI0 += 1; continue
                dn = DEG[min(lv + 1, 2)] if lv <= 2 else 0.0
                dnow = DEG[lv] if lv <= 3 else 0.0
                db = DEG[max(lv - 1, 0)] if lv <= 3 else 0.0
                loss = RATE * max(0.0, dnow - dn) * M
                per = next(p for p in PERIODS if p[0] <= y <= p[1])
                loss0[per].append(loss / I0); lossC[per].append(loss / C)
                if lv >= 1: gain0.append(RATE * max(0.0, db - dnow) * M / I0)
                titheOverC.append(RATE * dnow * M / C); clergyShare0.append(C / I0)
                byLevel.setdefault(lv, []).append(loss / I0)
                # 案 (A-c)：受け取る側の所得から＝(今の度合い−次)÷今の度合い×聖職者の所得÷旧支配層の所得（聖職者の所得⊂旧支配層の所得＝構造で ≦ 1）
                if dnow > 0: formC.append((dnow - dn) / dnow * C / I0)
                if loss / I0 > 1.0: over1 += 1
                if lv == 0: sumNum += loss; sumDen += I0
                if k in cg: cg_closed_in += cg[k]; cg_est += 3 * RATE * dnow * M
                if k[1] in ("イギリス", "フランス", "プロイセン", "ロシア", "オーストリア", "スペイン", "オスマン", "清", "日本"):
                    maj.setdefault(k[1], []).append(loss / I0)
        L.append(f"## 腕 {name}")
        L.append(f"行（国×年の断面）{rowsN}・聖職者 0 の行 {noClergy}・旧支配層の所得 0 の行 {zeroI0}")
        for p in PERIODS:
            L.append(f"- 失う割合 ÷旧支配層の所得 {p[0]}–{p[1]}: {qs(loss0[p])}")
        for p in PERIODS:
            L.append(f"- 失う割合 ÷聖職者の所得 {p[0]}–{p[1]}: {qs(lossC[p])}")
        L.append(f"- 段 0 の行の和÷和（失う額÷旧支配層の所得）: {sumNum/sumDen:.4f}" if sumDen > 0 else "- 段 0 の和÷和: nan")
        for lv in sorted(byLevel): L.append(f"- 段 {lv} の行: {qs(byLevel[lv])}")
        L.append(f"- 戻す向きの得 ÷旧支配層の所得（段≧1）: {qs(gain0)}")
        L.append(f"- 今の段の十分の一税（上限）÷聖職者の所得: {qs(titheOverC)}")
        L.append(f"- 聖職者の所得÷旧支配層の所得: {qs(clergyShare0)}")
        L.append(f"- 突き合わせ：cgrp 閉鎖の層の pop からの受け取り（四半期）の和 {cg_closed_in:.4g} ÷ 推定の十分の一税×3 の和 {cg_est:.4g} = {cg_closed_in/cg_est:.4f}" if cg_est > 0 else "- 突き合わせ: nan")
        for n, xs in sorted(maj.items()): L.append(f"  - {n}: 中央 {st.median(xs):.4f} 最大 {max(xs):.4f} n={len(xs)}")
        a_pref = [-2.0 * (0.5 + x) for p in PERIODS for x in loss0[p]]
        L.append(f"- 案 (A) 底あり −2×(0.5＋割合) の値: {qs(a_pref)}（今の札 −1）")
        L.append(f"- 案 (A′) 底なし −2×割合 の値: {qs([-2.0*x for p in PERIODS for x in loss0[p]])}")
        L.append(f"- 請求の形（大衆の所得×率×度合いの差÷旧支配層の所得）が 1 を超えた行 {over1}（分母が痩せると跳ねる）")
        L.append(f"- 案 (A-c) 受け取る側の所得の形 (度合いの差÷今の度合い)×聖職者の所得÷旧支配層の所得: {qs(formC)}")
        L.append(f"- 案 (A-c) 底あり −2×(0.5＋A-c) の値: {qs([-2.0*(0.5+x) for x in formC])}")
        L.append(f"- 案 (B) 払う側（大衆）の得＝率×度合いの差（所得に依らない定数）: 段0→1 {RATE*(DEG[0]-DEG[1]):.4f}・段1→2 {RATE*(DEG[1]-DEG[2]):.4f}")
        L.append("")
    with open(out, 'wb') as f:
        f.write(('\n'.join(L) + '\n').encode('utf-8'))


if __name__ == '__main__':
    main()
