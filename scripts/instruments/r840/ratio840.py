# (840) c68hl 州ごとの〈最大の落ち込み〉の比の表（読むだけ・事前登録 m840 §1）
#   python -X utf8 -I ratio840.py <名=region_s のあるフォルダ>... > ratios.tsv
#   比 r＝min_i pop[i]÷runPeak[i]（runPeak＝その行までの人口の最大）。an_h2.py と同じ行の集合：全ての行（activeFlag を見ない）・ファイルの順・
#   populationK が非有限の行は飛ばす（ピークも更新しない）・runPeak ≧ KMIN の行だけで min を取る。一度も KMIN に届かない系列は r＝nan。
#   an_h2 の事象〈それまでのピークの 1/div 未満へ初めて落ちた〉は〈r < 1/div〉と同値。
import sys, os, math
sys.stdout.reconfigure(newline='\n')   # Windows の文字モードの CRLF を避ける（出力は LF）
KMIN = 1.0   # an_h2.py の既定（-min を渡さない）と同じ
def fl(x):
    try: return float(x)
    except: return float('nan')
out = sys.stdout
out.write('set\tseed\tregion\tratio\tpeakK\tpeakYear\tlowK\tlowYear\n')
for arg in sys.argv[1:]:
    lab, d = arg.split('=', 1)
    fs = sorted([f for f in os.listdir(d) if f.startswith('region_s') and f.endswith('.tsv')], key=lambda f: int(f[len('region_s'):-4]))
    for gf in fs:
        sd = int(gf[len('region_s'):-4])
        S = {}   # 州 → [runPeak, その年, ratio, 落ちた時のピーク, lowK, lowYear, 落ちた時のピークの年]
        with open(os.path.join(d, gf), encoding='utf-8') as fh:
            head = fh.readline().rstrip('\n').split('\t')
            iR = head.index('region'); iP = head.index('populationK'); iY = head.index('year')
            for line in fh:
                x = line.rstrip('\n').split('\t')
                reg = x[iR]; v = fl(x[iP])
                s = S.get(reg)
                if s is None: s = S[reg] = [0.0, None, float('nan'), float('nan'), float('nan'), None, None]
                if not math.isfinite(v): continue
                if v > s[0]: s[0] = v; s[1] = int(x[iY])
                if s[0] < KMIN: continue
                r = v / s[0]
                if not (r >= s[2]):   # nan の初期値でも最初の行で入る
                    s[2] = r; s[3] = s[0]; s[4] = v; s[5] = int(x[iY]); s[6] = s[1]
        for reg in sorted(S):
            s = S[reg]
            out.write(f'{lab}\t{sd}\t{reg}\t{s[2]!r}\t{s[3]!r}\t{s[6]}\t{s[4]!r}\t{s[5]}\n')
