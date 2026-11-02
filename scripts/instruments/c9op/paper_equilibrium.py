# (787) c9op 紙の台本：m784 の計器つき 10 種（habitcap_s・旗 0）から、蓋なしの釣り合い s*=(成長/減衰)×1人あたり消費 を行ごとに推定する。
# 行（州×四半期）の最後の月だけ：pc = (newNextLast - (1-0.02)*before)/0.1 （蓋なしの式 s' = s + 0.1pc - 0.02 s の逆算）
import sys, glob, math, io
G, D = 0.1, 0.02
def run(d):
    tot = 0; lowL = 0; rowsBind = 0; hist = {}
    pcs = []; star = []
    nat = {}
    for f in sorted(glob.glob(d + '/habitcap_s*.tsv')):
        with open(f, encoding='utf-8') as fh:
            fh.readline()
            for l in fh:
                p = l.rstrip('\n').split('\t')
                if len(p) < 15: continue
                tot += 1
                before = float(p[9]); old = float(p[11]); new = float(p[12]); nm = float(p[14]); bind = int(p[6])
                if new > 0.2 and old >= 0.2: lowL += 1       # 下限の門 L の旗0版：旧が蓋に当たる行で、新(蓋なし)が 0.2 を超える
                pc = (new - (1 - D) * before) / G
                pcs.append(pc)
                s = G / D * pc
                star.append(s)
                if bind > 0: rowsBind += 1
    n = len(pcs)
    star.sort(); pcs.sort()
    def q(a, x): return a[min(len(a) - 1, int(len(a) * x))]
    print(d, 'rows', tot, 'rowsBindLastMonthOrAny', rowsBind, 'L_rows(old>=0.2 & new>0.2)', lowL)
    print('  implied pc (last month) max %.4f p99 %.4f p999 %.4f' % (pcs[-1], q(pcs, .99), q(pcs, .999)))
    print('  implied s*=5pc max %.4f p99 %.4f p999 %.4f' % (star[-1], q(star, .99), q(star, .999)))
    for thr in (0.2, 0.5, 1.0):
        c = sum(1 for s in star if s >= thr)
        print('  rows with implied s* >= %.1f : %d (%.4f%%)' % (thr, c, 100 * c / n))
for d in sys.argv[1:]: run(d)
