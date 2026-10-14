# -*- coding: utf-8 -*-
"""(333) 記録の列（読むだけ・採否の規則に入れない列は明記）。nation_s<seed>.tsv と帯の報告から。
python sa333_20261014_an_rec.py <腕名=ins のディレクトリ=帯の報告> ...   （例 B2=D:/…/ins=measurements/b10_…B2_ins）
出す物：
  南アジアの独立 6 国の生き残り（populationK＞0 の種の数・Q40・Q160・最後の四半期）と、6 国のうち最大の国の人口（種の中央値・Q0・Q40・Q160・最後）。
  ロシアの GDP 順位〔代理＝nominalGdpPerYear の全国の順位・国力ランク NationalScore の順位ではない〕と交戦月数の和（Q0〜Q39・Q40〜Q159・全期）の種の中央値。
    ⚠国力ランク（列強/準列強…）は計器に列が無く、四半期ごとには読めない。開幕のランクは initdump（前半の記録）・1914 の順位は帯の列強の行。代理の順位は参考。
  帯の報告の行：列強の顔ぶれ一致とロシアの順位／列強・再現率／人口の行／exact・near。
限界：国の名前は表示名＝建国・改名・併合で行が途切れうる。生き残りは「その名前の行の populationK＞0」。"""
import sys, os, re, glob, csv, statistics
SA6 = ['マラータ同盟', 'マイソール', 'ハイデラバード', 'シク連邦', 'デリー', 'アワド']
WIN = [0, 40, 160, 10 ** 6]


def read_nation(path):
    rows = []
    with open(path, encoding='utf-8') as f:
        rd = csv.reader(f, delimiter='\t'); h = next(rd)
        iq, inat, ip, ig, iw = h.index('quarter'), h.index('nation'), h.index('populationK'), h.index('nominalGdpPerYear'), h.index('atWarMonths')
        for r in rd:
            try: rows.append((int(r[iq]), r[inat], float(r[ip]), float(r[ig]), float(r[iw])))
            except ValueError: pass
    return rows


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def main():
    for a in sys.argv[1:]:
        name, d, band = a.split('=', 2)
        print('######## %s' % name)
        seeds = sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')))
        alive = {q: {n: 0 for n in SA6} for q in WIN}; maxpop = {q: [] for q in WIN}; rus_rank = {q: [] for q in WIN}; war = {k: [] for k in ('Q0-39', 'Q40-159', '全期')}
        for p in seeds:
            rows = read_nation(p)
            last = max(r[0] for r in rows)
            for q in WIN:
                qq = min(q, last)
                cur = [r for r in rows if r[0] == qq]
                for n in SA6:
                    alive[q][n] += 1 if any(r[1] == n and r[2] > 0 for r in cur) else 0
                maxpop[q].append(max([r[2] for r in cur if r[1] in SA6] or [0]) / 1e3)
                gd = sorted([r for r in cur if r[2] > 0], key=lambda r: -r[3])
                rk = [i + 1 for i, r in enumerate(gd) if r[1] == 'ロシア']
                rus_rank[q].append(rk[0] if rk else float('nan'))
            w = [(r[0], r[4]) for r in rows if r[1] == 'ロシア']
            war['Q0-39'].append(sum(x for q, x in w if q < 40)); war['Q40-159'].append(sum(x for q, x in w if 40 <= q < 160)); war['全期'].append(sum(x for q, x in w))
        ns = len(seeds)
        lab = {0: 'Q0', 40: 'Q40', 160: 'Q160', 10 ** 6: '最後'}
        print('南アジア独立 6 国の生き残り（populationK＞0 の種の数／%d）' % ns)
        for n in SA6:
            print('  %-10s ' % n + '　'.join('%s %d' % (lab[q], alive[q][n]) for q in WIN))
        print('6 国のうち最大の国の人口 中央値（百万）: ' + '　'.join('%s %.1f' % (lab[q], med(maxpop[q])) for q in WIN))
        print('ロシアの名目GDP順位（代理・全国中）中央値: ' + '　'.join('%s %.1f' % (lab[q], med(rus_rank[q])) for q in WIN))
        print('ロシアの交戦月数の和 中央値: ' + '　'.join('%s %.1f' % (k, med(v)) for k, v in war.items()))
        if os.path.exists(band):
            for ln in open(band, encoding='utf-8'):
                if re.match(r'\s*(列強:|人口:|平均:|未達 列強|合格 列強|報告 人口|6次元の到達)', ln):
                    print('帯: ' + ln.rstrip()[:200])


if __name__ == '__main__':
    main()
