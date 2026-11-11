# (1032) c97dp 設計の材料：地主の政治 D-2 の紙（読むだけ・エンジンを呼ばない・腕 0 本）
# 使い方: python -X utf8 -I lordstat1032.py <出力ファイル> <名>=<生のフォルダ> ...
#   各フォルダの lordstat_s1..10・monlaw_s1..10・burblock_s1..10 を読む（国×四半期の鍵＝quarter と nation の名）。
# 問い 1：§3-2 の取り直し（和÷和の 2 倍の差の分解・比が跳ねる件数）
# 問い 2：構造の形 S_up(軸)＝(cut÷Income0)×(I−I'軸)÷I を官僚制度と土地改革の両方で（中の項 R1÷(R1＋R0(1−f)) は生に無い＝上限）
# 問い 4：官僚制度の目標＞今の国×四半期を、次の一段で I が下がるかどうかで分ける（床あり・床なしの 2 本の見積もりの材料）
import sys, os, csv, statistics as st
from collections import defaultdict

def f(x):
    try:
        v = float(x)
        return v if v == v else 0.0
    except Exception:
        return 0.0

def estate(b):
    return 1.0 if b == 0 else 0.5 if b == 1 else 0.0

def lrdeg(l):
    return 1.0 if l == 0 else 0.5 if l == 1 else 0.0

def inter(b, l):
    return max(estate(b), lrdeg(l))

def q(xs, p):
    if not xs:
        return float('nan')
    xs = sorted(xs)
    k = min(len(xs) - 1, max(0, int(round(p * (len(xs) - 1)))))
    return xs[k]

PERIODS = [(1783, 1830), (1831, 1870), (1871, 1910), (1911, 1951)]

def per(y):
    for i, (a, b) in enumerate(PERIODS):
        if a <= y <= b:
            return i
    return 3

def run(name, d, L):
    rows = 0; tiny = 0
    cutshare = []; over1_cut = 0
    s0 = []; over1_s0 = 0; over1_s0_above = 0; above_pos = 0
    num = 0.0; den = 0.0
    numP = [0.0] * 4; denP = [0.0] * 4
    cell = defaultdict(lambda: [0, 0.0, 0.0, [], 0.0])  # 行・Σnum・Σinc0・share0 の列・Σcut
    nat = defaultdict(lambda: [0.0, 0.0, 0.0, 0.0])      # Σnum・Σinc0・Σcut・行
    natnum = defaultdict(float)
    sbur = []; slr = []; both0 = 0; bur_pos = 0; lr_pos = 0
    tgt_rows = 0; tgt_up = 0; tgt_up_burmoves = 0; tgt_up_cell = defaultdict(int)
    tgt_up_trad_s = []
    need_rows = 0; need_up = 0; bb_rows = 0
    for s in range(1, 11):
        pl = os.path.join(d, f"lordstat_s{s}.tsv"); pm = os.path.join(d, f"monlaw_s{s}.tsv")
        if not (os.path.exists(pl) and os.path.exists(pm)):
            L.append(f"- {name} 種 {s}：表が無い"); continue
        law = {}
        with open(pm, encoding='utf-8', newline='') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r)
            iq, inn, ilr, ib, ibt = h.index('quarter'), h.index('nation'), h.index('LandReform'), h.index('Bureaucracy'), h.index('BureaucracyTarget')
            for x in r:
                law[(x[iq], x[inn])] = (int(f(x[ilr])), int(f(x[ib])), int(f(x[ibt])))
        with open(pl, encoding='utf-8', newline='') as fh:
            r = csv.reader(fh, delimiter='\t'); h = next(r); ix = {c: i for i, c in enumerate(h)}
            for x in r:
                rows += 1
                key = (x[ix['quarter']], x[ix['nation']])
                y = int(f(x[ix['year']]))
                inc0 = f(x[ix['lsIncome0PerMonth']]); cut = f(x[ix['dbuRuleCutPerMonth']])
                sh = f(x[ix['dbuRuleCutLossNextShare0']]); ab = f(x[ix['dbuAboveTargetShare']])
                b = int(f(x[ix['bureaucracyCode']]))
                imn = f(x[ix['dbuIntermediaryNow']]); imx = f(x[ix['dbuIntermediaryNext']])
                lw = law.get(key)
                if not (inc0 > 1e-9):
                    tiny += 1; continue
                cs = cut / inc0; cutshare.append(cs)
                if cs > 1: over1_cut += 1
                s0.append(sh)
                if ab > 0: above_pos += 1
                if sh > 1:
                    over1_s0 += 1
                    if ab > 0: over1_s0_above += 1
                n_ = sh * inc0; num += n_; den += inc0
                p = per(y); numP[p] += n_; denP[p] += inc0
                nm = x[ix['nation']]
                nat[nm][0] += n_; nat[nm][1] += inc0; nat[nm][2] += cut; nat[nm][3] += 1
                if lw is None: continue
                lr, bm, bt = lw
                c = cell[(bm, lr)]; c[0] += 1; c[1] += n_; c[2] += inc0; c[3].append(sh); c[4] += cut
                I = inter(bm, lr)
                if abs(I - imn) > 1e-9: pass
                Ib = inter(min(bm + 1, 2), lr); Il = inter(bm, min(lr + 1, 2))
                sb = cs * (I - Ib) / I if I > 0 else 0.0
                sl = cs * (I - Il) / I if I > 0 else 0.0
                sbur.append(sb); slr.append(sl)
                if sb > 0: bur_pos += 1
                if sl > 0: lr_pos += 1
                if sb == 0 and sl == 0: both0 += 1
                tgt_rows += 1
                if bt > bm:
                    tgt_up += 1; tgt_up_cell[(bm, lr)] += 1
                    if Ib < I: tgt_up_burmoves += 1
                    tgt_up_trad_s.append(sb)
        pb = os.path.join(d, f"burblock_s{s}.tsv")
        if os.path.exists(pb):
            with open(pb, encoding='utf-8', newline='') as fh:
                r = csv.reader(fh, delimiter='\t'); h = next(r)
                ib, io, inn2 = h.index('bur'), h.index('burOut'), h.index('need')
                for x in r:
                    bb_rows += 1
                    if int(f(x[inn2])) > int(f(x[ib])): need_rows += 1
                    if int(f(x[io])) > int(f(x[ib])): need_up += 1
    L.append(f"## {name}（{d}）")
    L.append(f"- 行 {rows}・Income0≦1e-9 の行 {tiny}（除いた）")
    L.append(f"- 問い1 cut÷Income0：中央 {q(cutshare,.5):.4f}・p90 {q(cutshare,.9):.4f}・最大 {max(cutshare):.4f}・1 を超えた行 {over1_cut}")
    L.append(f"- 問い1 計器の share0（官僚制度の次の一段）：中央 {q(s0,.5):.4f}・p90 {q(s0,.9):.4f}・p99 {q(s0,.99):.4f}・最大 {max(s0):.4f}・正の行 {sum(1 for v in s0 if v>0)}・1 を超えた行 {over1_s0}（うち dbuAboveTargetShare＞0 {over1_s0_above}）・dbuAboveTargetShare＞0 の行 {above_pos}")
    L.append(f"- 問い1 和÷和（通貨を混ぜた和）：{(num/den if den>0 else 0):.4f}・期ごと " + " / ".join(f"{(numP[i]/denP[i] if denP[i]>0 else 0):.4f}" for i in range(4)))
    nr = sorted(((v[0] / v[1] if v[1] > 0 else 0.0), k) for k, v in nat.items())
    L.append(f"- 問い1 国ごとの和÷和：国 {len(nr)}・中央 {q([a for a,_ in nr],.5):.4f}・p90 {q([a for a,_ in nr],.9):.4f}・正の国 {sum(1 for a,_ in nr if a>0)}")
    top = sorted(nat.items(), key=lambda kv: -kv[1][0])[:5]
    L.append("- 問い1 通貨を混ぜた分子の上位 5 国（分子の割合・分母の割合・国の和÷和）：" + "・".join(f"{k} {v[0]/num:.3f}/{v[1]/den:.3f}/{(v[0]/v[1] if v[1]>0 else 0):.3f}" for k, v in top) if num > 0 else "- 分子 0")
    L.append("- 問い1 法の組（官僚制度, 土地改革）ごと：行・Σ分子の割合・ΣIncome0 の割合・組の和÷和・share0 の中央・cut÷Income0 の和÷和")
    for k in sorted(cell):
        c = cell[k]
        L.append(f"  - ({k[0]},{k[1]}) I={inter(k[0],k[1]):.1f}→官{inter(min(k[0]+1,2),k[1]):.1f}/土{inter(k[0],min(k[1]+1,2)):.1f}：{c[0]}・{(c[1]/num if num>0 else 0):.3f}・{(c[2]/den if den>0 else 0):.3f}・{(c[1]/c[2] if c[2]>0 else 0):.4f}・{q(c[3],.5):.4f}・{(c[4]/c[2] if c[2]>0 else 0):.4f}")
    L.append(f"- 問い2 S_up 官僚制度：正の行 {bur_pos}/{len(sbur)}・中央 {q(sbur,.5):.4f}・p90 {q(sbur,.9):.4f}・最大 {(max(sbur) if sbur else 0):.4f}")
    L.append(f"- 問い2 S_up 土地改革：正の行 {lr_pos}/{len(slr)}・中央 {q(slr,.5):.4f}・p90 {q(slr,.9):.4f}・最大 {(max(slr) if slr else 0):.4f}")
    L.append(f"- 問い2 どちらの一段でも I が下がらない行 {both0}/{len(sbur)}（{(both0/len(sbur) if sbur else 0):.4f}）")
    L.append(f"- 問い4 官僚制度の目標＞今：{tgt_up}/{tgt_rows}（{(tgt_up/tgt_rows if tgt_rows else 0):.4f}）・うち次の一段で I が下がる（S＞0 になりうる）{tgt_up_burmoves}・S_up の中央 {q(tgt_up_trad_s,.5):.4f}・p90 {q(tgt_up_trad_s,.9):.4f}")
    L.append("  - 目標＞今の組ごと：" + "・".join(f"({k[0]},{k[1]}) {v}" for k, v in sorted(tgt_up_cell.items())))
    L.append(f"- 問い4 burblock：行 {bb_rows}・要る段＞今 {need_rows}・出した目標＞今 {need_up}")

if __name__ == '__main__':
    out = sys.argv[1]; L = ["# lordstat1032（読むだけ・(1032) c97dp）"]
    for a in sys.argv[2:]:
        nm, d = a.split('=', 1); run(nm, d, L)
    with open(out, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write("\n".join(L) + "\n")
