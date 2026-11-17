"""(1186) c118nc 紙の台本：基線の secession_s の consciousnessFactorShare（＝1−SecessionConsciousnessWeight×州の国民意識）の分布。
重みを 0 にすると分離の圧は 1/factor 倍になる（同じ盤面の 1 四半期の上げ）。popK 加重の平均と中央値・goFlag の行数。
使い方: python secfac.py <ins の生のフォルダ> [種の数]"""
import sys, os, statistics
d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
for s in range(1, ns + 1):
    p = os.path.join(d, f'secession_s{s}.tsv')
    if not os.path.exists(p): continue
    fs = []; wsum = 0.0; w = 0.0; go = 0; n = 0
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); i_f, i_p, i_g = h.index('consciousnessFactorShare'), h.index('popK'), h.index('goFlag')
        for line in f:
            a = line.rstrip('\n').split('\t'); n += 1
            try: x = float(a[i_f]); pk = float(a[i_p])
            except ValueError: continue
            if a[i_g] == '1': go += 1
            if x > 0: fs.append(1.0 / x); wsum += pk / x; w += pk
    fs.sort()
    print(f's{s} rows={n} go={go} uplift_med={statistics.median(fs):.4f} uplift_p90={fs[int(0.9*len(fs))]:.4f} uplift_popw={wsum/w:.4f}')
