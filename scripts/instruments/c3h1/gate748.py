"""(748) c3h1：事前登録 m748 の門 T・M1・M2 を計器の出力から読む（読むだけ）。
python -X utf8 gate748.py <計器の出力ディレクトリ> [種...]
  T  ＝出る州が植民地格（region_s の tier=Colonial・その四半期）の国内の移民のうち大圏 3,000 km 超の量（千人・全四半期の和）の種の中央値。
  M1 ＝migform_s の liveFarPlanK÷oldFarPlanK（全四半期の和）の種の中央値（旗 0 で 1.000）。
  M2 ＝1−Σ livePosPairs÷Σ rawPosPairs（生の差が正の組のうち距離の項を引いて負になった割合）の種の中央値（旗 0 で 0）。
距離＝hol_common.dist（州の座標の大圏 km・an_h2 と同じ）。migform_s が無い出力（基線）は M1・M2 を出さない。"""
import sys, os, statistics as st, collections
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'hol'))
import hol_common
d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(f[len('migration_s'):-4]) for f in os.listdir(d) if f.startswith('migration_s'))
Ts, M1, M2, FS, ALL = [], [], [], [], []
for sd in seeds:
    tier = {}
    with open(os.path.join(d, f'region_s{sd}.tsv'), encoding='utf-8') as fh:
        next(fh)
        for ln in fh:
            c = ln.split('\t', 7); tier[(int(c[0]), c[4])] = c[5]
    T = tot = far = 0.0
    for ln in open(os.path.join(d, f'migration_s{sd}.tsv'), encoding='utf-8'):
        c = ln.rstrip('\n').split('\t')
        if len(c) < 4 or not c[0].isdigit(): continue   # (749) c3rt の後は migration_s が 5 列（末尾 routeKm）
        v = float(c[3]); km = hol_common.dist(c[1], c[2])
        if km != km: continue
        tot += v
        if km > 3000:
            far += v
            if tier.get((int(c[0]), c[1])) == 'Colonial': T += v
    Ts.append(T); ALL.append(tot); FS.append(far / tot if tot else float('nan'))
    mf = os.path.join(d, f'migform_s{sd}.tsv')
    if os.path.exists(mf):
        rp = lp = lf = of = la = oa = 0.0
        with open(mf, encoding='utf-8') as fh:
            next(fh)
            for ln in fh:
                c = ln.rstrip('\n').split('\t')
                if len(c) != 9: continue
                rp += float(c[3]); lp += float(c[4]); lf += float(c[5]); la += float(c[6]); of += float(c[7]); oa += float(c[8])
        M1.append(lf / of if of else float('nan')); M2.append(1 - lp / rp if rp else float('nan'))
        print(f'seed {sd}\tT={T:.0f}\tM1={M1[-1]:.4f}\tM2={M2[-1]:.4f}\tfarPlan 新{lf:.0f} 旧{of:.0f}\tallPlan 新{la:.0f} 旧{oa:.0f}\tfarShare={FS[-1]:.4f}\ttotal={tot:.0f}')
    else:
        print(f'seed {sd}\tT={T:.0f}\tfarShare={FS[-1]:.4f}\ttotal={tot:.0f}')
print(f'MEDIAN\tT={st.median(Ts):.0f}\tfarShare={st.median(FS):.4f}\ttotal={st.median(ALL):.0f}' + (f'\tM1={st.median(M1):.4f}\tM2={st.median(M2):.4f}' if M1 else ''))
