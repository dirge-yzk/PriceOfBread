"""(371) 流れM の計器 2 本の読み手＝読むだけ・挙動は変えない。標準の計器 fm_s<seed>.tsv（と nation_s）を読む。
python an_fm.py <計器の出力フォルダ（…/ins）> [q1|q2|all]       （PYTHONUTF8=1）
出す物：
 Q1（戦時の軍事費の純支持）
  ・T1 増額の一歩（netIncreasingFlag=1）の純支持の項の平均を、戦時の区分ごとに：平時／交戦中で本国へ届かない／届く／大規模（規模≧1.2 または存亡>0.5）。項＝街頭の旧支配層・近代エリート・大衆・議会・軍。
  ・T2 区分ごとの純支持が負の割合・結果の符号（1 純支持で固定／2 政治資本で固定／3 無償で動いた／4 払って動いた）の割合・抵抗・歩幅。
  ・T3 同じ表を政体の区分（議会のある政体＝立憲君主制・共和制／ない政体）ごとに。
  ・T4 戦争の量（規模・存亡・戦場の強さ・交戦の月数）と純支持／軍の項の順位相関（国×四半期・増額の一歩）。
  ・T5 純支持が負の大規模戦の行の不足の分布（−純支持の p50／p90）と、ブロックの項の大きさ（比例の項を足すならこの桁）。
  ・T6 英だけの同じ表。
 Q2（海の金の割合の分母）
  ・T1 国ごと（挙げた国）・時代ごとの、陸の必要量の組成（本国どうしの国境／植民地を含む国境／投射）・内の守り・自国の軍の大きさ・どちらが分母を決めるか・海の必要量・理想の海の割合。
  ・T2 英の寄与の上位 3 国（頻度）と、植民地を含む国境の上位 3 国。
  ・T3 反実仮想（読むだけ・本体は動かさない）：英の分母から〈植民地を含む国境〉／〈投射〉／〈その両方〉を除いたときの海の金の割合。
限界の自己申告：種 N 本の国×四半期の記述統計＝因果ではない。純支持の項は軍事費の合計の施行の一歩が来た四半期だけ。反実仮想は分母の式だけを動かす静的な再計算（艦隊・他国の応答は動かさない）。
史実の海の金の割合（英 約 0.45）は記憶による【推】（出典未取得）。"""
import sys, csv, glob, collections, statistics, os

POLITY = ['絶対君主制', '専制君主制', '幕府', '立憲君主制', '共和制', 'ブルジョア独裁', 'プロレタリア独裁', '軍事独裁', '無政府']
LEG = {3, 4}


def f(x):
    try:
        return float(x)
    except Exception:
        return float('nan')


def isnan(x):
    return x != x


def mean(v):
    v = [x for x in v if not isnan(x)]
    return sum(v) / len(v) if v else float('nan')


def pct(v, p):
    v = sorted(x for x in v if not isnan(x))
    return v[min(len(v) - 1, int(len(v) * p))] if v else float('nan')


def spearman(xs, ys):
    pairs = [(x, y) for x, y in zip(xs, ys) if not isnan(x) and not isnan(y)]
    if len(pairs) < 10:
        return float('nan')
    def rank(v):
        o = sorted(range(len(v)), key=lambda i: v[i]); r = [0.0] * len(v); i = 0
        while i < len(o):
            j = i
            while j + 1 < len(o) and v[o[j + 1]] == v[o[i]]:
                j += 1
            for k in range(i, j + 1):
                r[o[k]] = (i + j) / 2 + 1
            i = j + 1
        return r
    a = rank([p[0] for p in pairs]); b = rank([p[1] for p in pairs])
    ma, mb = statistics.mean(a), statistics.mean(b)
    num = sum((p - ma) * (q - mb) for p, q in zip(a, b))
    den = (sum((p - ma) ** 2 for p in a) * sum((q - mb) ** 2 for q in b)) ** 0.5
    return num / den if den > 0 else float('nan')


def load(d):
    rows = []
    for fn in sorted(glob.glob(os.path.join(d, 'fm_s*.tsv'))):
        seed = int(os.path.basename(fn)[4:-4])
        with open(fn, encoding='utf-8') as fh:
            for x in csv.DictReader(fh, delimiter='\t'):
                x['seed'] = seed
                rows.append(x)
    return rows


def war_class(x):
    aw = f(x['atWarAtDecisionFlag']); sc = f(x['warScaleRatioAtDecision']); lo = f(x['homeLosingScoreAtDecision'])
    if isnan(aw) or aw < 0:
        return 'x判断なし'
    if aw == 0:
        return '1平時'
    if sc >= 1.2 or lo > 0.5:
        return '4大規模'
    if sc > 0 or lo > 0:
        return '3届く'
    return '2届かない'


TERMS = ['netStreetTradIndex', 'netStreetModernIndex', 'netStreetMassIndex', 'netParliamentIndex', 'netArmyIndex', 'moveNetSupportIndex']


def q1(rows, title='全国'):
    inc = [x for x in rows if x['netIncreasingFlag'] == '1']
    print(f'\n#### Q1 {title}：増額の一歩の行 {len(inc)}（全行 {len(rows)}・種 {len(set(x["seed"] for x in rows))}）')
    g = collections.defaultdict(list)
    for x in inc:
        g[war_class(x)].append(x)
    print('T1/T2 区分 | 行 | 街頭旧支配層 | 近代 | 大衆 | 議会 | 軍 | 純支持 | 負の割合 | 結果1/2/3/4の割合 | 抵抗 | 歩幅 | 軍の人口比')
    for k in sorted(g):
        v = g[k]
        t = [mean([f(x[c]) for x in v]) for c in TERMS]
        neg = sum(1 for x in v if f(x['moveNetSupportIndex']) < 0) / len(v)
        oc = collections.Counter(int(f(x['moveOutcomeCode'])) for x in v)
        ocs = '/'.join('%.2f' % (oc.get(i, 0) / len(v)) for i in (1, 2, 3, 4))
        print(f'  {k} | {len(v)} | ' + ' | '.join('%+.3f' % a for a in t) + f' | {neg:.3f} | {ocs} | {mean([f(x["moveResistIndex"]) for x in v]):.3f} | {mean([f(x["moveStepFraction"]) for x in v]):.4f} | {mean([f(x["soldierShare"]) for x in v]):.4f}')
    base = g.get('1平時', [])
    if base:
        for k in ('3届く', '4大規模'):
            if k in g:
                dA = mean([f(x['netArmyIndex']) for x in g[k]]) - mean([f(x['netArmyIndex']) for x in base])
                dN = mean([f(x['moveNetSupportIndex']) for x in g[k]]) - mean([f(x['moveNetSupportIndex']) for x in base])
                dS = {t: mean([f(x[t]) for x in g[k]]) - mean([f(x[t]) for x in base]) for t in TERMS[:5]}
                print(f'  平時との差 {k}：純支持 {dN:+.3f}（うち軍の項 {dA:+.3f}・' + '・'.join(f'{t[3:-5]} {v:+.3f}' for t, v in dS.items()) + '）')
    print('T3 政体の区分（議会のある政体＝立憲君主制・共和制）')
    for lab, leg in (('議会あり', True), ('議会なし', False)):
        gg = collections.defaultdict(list)
        for x in inc:
            if (int(f(x['polityCode'])) in LEG) == leg:
                gg[war_class(x)].append(x)
        for k in sorted(gg):
            v = gg[k]
            print(f'  {lab} {k} | {len(v)} | 純支持 {mean([f(x["moveNetSupportIndex"]) for x in v]):+.3f} | 負の割合 {sum(1 for x in v if f(x["moveNetSupportIndex"]) < 0) / len(v):.3f} | 軍の項 {mean([f(x["netArmyIndex"]) for x in v]):+.3f} | 議会の項 {mean([f(x["netParliamentIndex"]) for x in v]):+.3f} | 大衆の項 {mean([f(x["netStreetMassIndex"]) for x in v]):+.3f} | 抵抗 {mean([f(x["moveResistIndex"]) for x in v]):.3f}')
    print('T4 順位相関（増額の一歩の行）')
    for w in ('warScaleRatioAtDecision', 'homeLosingScoreAtDecision', 'warIntensityMaxIndex', 'atWarMonthsCount'):
        xs = [f(x[w]) for x in inc]
        print(f'  {w} × 純支持 {spearman(xs, [f(x["moveNetSupportIndex"]) for x in inc]):+.3f} ／ × 軍の項 {spearman(xs, [f(x["netArmyIndex"]) for x in inc]):+.3f} ／ × 抵抗 {spearman(xs, [f(x["moveResistIndex"]) for x in inc]):+.3f}')
    big = [x for x in inc if war_class(x) in ('3届く', '4大規模')]
    neg = [-f(x['moveNetSupportIndex']) for x in big if f(x['moveNetSupportIndex']) < 0]
    print(f'T5 届く／大規模の増額の一歩 {len(big)} 行のうち純支持が負 {len(neg)} 行（{len(neg) / max(1, len(big)):.3f}）')
    if neg:
        print(f'  不足（−純支持）p50 {pct(neg, 0.5):.3f} p90 {pct(neg, 0.9):.3f}／ブロックの項の大きさ（|項|の平均）街頭旧 {mean([abs(f(x["netStreetTradIndex"])) for x in big]):.3f} 近代 {mean([abs(f(x["netStreetModernIndex"])) for x in big]):.3f} 大衆 {mean([abs(f(x["netStreetMassIndex"])) for x in big]):.3f} 議会 {mean([abs(f(x["netParliamentIndex"])) for x in big]):.3f} 軍 {mean([abs(f(x["netArmyIndex"])) for x in big]):.3f}')
        gt = collections.Counter()
        for x in big:
            if f(x['moveNetSupportIndex']) < 0:
                gt[min(TERMS[:5], key=lambda t: f(x[t]))[3:-5]] += 1
        print('  最も負の項の内訳 ' + '・'.join(f'{k} {v}' for k, v in gt.most_common()))
    print('T2b 区分ごとの抵抗・歩幅の分布（p10／p50／p90）と抵抗が 0.5 を超える割合')
    for k in sorted(g):
        v = g[k]; rs = [f(x['moveResistIndex']) for x in v]; sp = [f(x['moveStepFraction']) for x in v]
        print(f'  {k} | 抵抗 {pct(rs, 0.1):.3f}／{pct(rs, 0.5):.3f}／{pct(rs, 0.9):.3f} | >0.5 の割合 {sum(1 for r in rs if r > 0.5) / len(rs):.3f} | 歩幅 {pct(sp, 0.1):.4f}／{pct(sp, 0.5):.4f}／{pct(sp, 0.9):.4f}')
    print('T7 国ごとの大規模・届く戦の増額の一歩 | 行 | 純支持 | 負の割合 | 軍の項 | 議会の項 | 抵抗 | 歩幅')
    for nm in ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', 'スペイン', 'ロシア', 'プロイセン', 'オーストリア', 'アメリカ', '日本', '清', 'オスマン']:
        v = [x for x in big if x['nation'] == nm]
        if len(v) < 5:
            continue
        print(f'  {nm} | {len(v)} | {mean([f(x["moveNetSupportIndex"]) for x in v]):+.3f} | {sum(1 for x in v if f(x["moveNetSupportIndex"]) < 0) / len(v):.3f} | {mean([f(x["netArmyIndex"]) for x in v]):+.3f} | {mean([f(x["netParliamentIndex"]) for x in v]):+.3f} | {mean([f(x["moveResistIndex"]) for x in v]):.3f} | {mean([f(x["moveStepFraction"]) for x in v]):.4f}')
    print('T8 増額の一歩の目標との隔たり（目標−現在の軍事費の割合）と、1 回で埋まる割合（歩幅）')
    for k in sorted(g):
        v = g[k]; gp = [f(x['moveTargetShare']) - f(x['moveCurrentShare']) for x in v]
        print(f'  {k} | 行 {len(v)} | 隔たり p10／p50／p90 {pct(gp, 0.1):.4f}／{pct(gp, 0.5):.4f}／{pct(gp, 0.9):.4f} | 現在 {mean([f(x["moveCurrentShare"]) for x in v]):.4f} 目標 {mean([f(x["moveTargetShare"]) for x in v]):.4f}')
    # 減額の一歩（戦時の復員）も参考
    dec = [x for x in rows if x['netIncreasingFlag'] == '0']
    gd = collections.defaultdict(list)
    for x in dec:
        gd[war_class(x)].append(x)
    print('(参考) 減額の一歩 ' + ' / '.join(f'{k} {len(v)}行 純支持 {mean([f(x["moveNetSupportIndex"]) for x in v]):+.3f}' for k, v in sorted(gd.items())))


def era(q):
    y = 1783 + q // 4
    return '1783-1813' if y < 1814 else '1814-1853' if y < 1854 else '1854-1883' if y < 1884 else '1884-1913' if y < 1914 else '1914-1950'


def q2(rows):
    print(f'\n#### Q2 海の金の割合の分母の組成（国×四半期 {len(rows)} 行）')
    A = [x for x in rows if x['landSideBindsFlag'] != '']
    MAIN = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', 'スペイン', 'ロシア', 'プロイセン', 'オーストリア', 'アメリカ', '日本', '清', 'オスマン']
    names = [n for n in MAIN if any(x['nation'] == n for x in A)]   # 挙げた国（毎四半期の行）だけ。他の国は年末の行しか無い
    print('T1 国×時代 | 行 | 陸の必要量 = 本国国境 / 植民地国境 / 投射 | 内の守り | 自国の軍の大きさ | 陸側が縛る割合 | 海の必要量 | 理想の海の割合 | 本国国境の長さ / 植民地国境の長さ')
    for nm in names:
        for e in ('1783-1813', '1814-1853', '1854-1883', '1884-1913', '1914-1950'):
            v = [x for x in A if x['nation'] == nm and era(int(f(x['quarter']))) == e]
            if len(v) < 8:
                continue
            nl = mean([f(x['needLandIndex']) for x in v])
            print(f'  {nm} {e} | {len(v)} | {nl:.2f} = {mean([f(x["needLandBorderHomeIndex"]) for x in v]):.2f} / {mean([f(x["needLandBorderColonialIndex"]) for x in v]):.2f} / {mean([f(x["needLandProjectionIndex"]) for x in v]):.2f} | {mean([f(x["needInternalIndex"]) for x in v]):.3f} | {mean([f(x["forcePotentialIndex"]) for x in v]):.2f} | {mean([int(f(x["landSideBindsFlag"])) for x in v]):.2f} | {mean([f(x["needSeaIndex"]) for x in v]):.2f} | {mean([f(x["navalSpendShareIdealShare"]) for x in v]):.3f} | {mean([f(x["borderHomeLengthIndex"]) for x in v]):.2f} / {mean([f(x["borderColonialLengthIndex"]) for x in v]):.2f}')
    uk = [x for x in A if x['nation'] == 'イギリス']
    if uk:
        print('T2 英の陸の必要量への寄与の上位 3 国（四半期の先頭の頻度）')
        c1 = collections.Counter(); c2 = collections.Counter()
        for x in uk:
            t = x['needLandTop3'].split(';')[0].split(':')[0] if x['needLandTop3'] else ''
            tc = x['colonialBorderTop3'].split(';')[0].split(':')[0] if x['colonialBorderTop3'] else ''
            c1[t] += 1; c2[tc] += 1
        print('  全体の先頭 ' + '・'.join(f'{k or "なし"} {v}' for k, v in c1.most_common(8)))
        print('  植民地を含む国境の先頭 ' + '・'.join(f'{k or "なし"} {v}' for k, v in c2.most_common(8)))
        print('T4 英の陸の必要量の最大の相手：その陸軍の大きさと外へ向く割合（上位の相手の平均）')
        for e in ('1783-1813', '1814-1853', '1854-1883', '1884-1913', '1914-1950'):
            v = [x for x in uk if era(int(f(x['quarter']))) == e]
            if len(v) < 8:
                continue
            print(f'  {e} | 最大の相手の陸軍の大きさ {mean([f(x["top1LandPotentialIndex"]) for x in v]):.2f} | 外へ向く割合 {mean([f(x["top1OutwardShare"]) for x in v]):.3f}')
        print('T2b 英の陸の必要量への寄与（上位 3 国に入った分の平均・時代別・上位 3 に入らない分は 0 として平均した下限）')
        for e in ('1783-1813', '1814-1853', '1854-1883', '1884-1913', '1914-1950'):
            v = [x for x in uk if era(int(f(x['quarter']))) == e]
            if len(v) < 8:
                continue
            agg = collections.defaultdict(float); aggC = collections.defaultdict(float)
            for x in v:
                for tok in x['needLandTop3'].split(';'):
                    if ':' in tok:
                        nm, val = tok.rsplit(':', 1); agg[nm] += f(val)
                for tok in x['colonialBorderTop3'].split(';'):
                    if ':' in tok:
                        nm, val = tok.rsplit(':', 1); aggC[nm] += f(val)
            print(f'  {e} | 全体 ' + '・'.join(f'{k} {s / len(v):.2f}' for k, s in sorted(agg.items(), key=lambda kv: -kv[1])[:4]) + ' ／ 植民地国境 ' + '・'.join(f'{k} {s / len(v):.2f}' for k, s in sorted(aggC.items(), key=lambda kv: -kv[1])[:4]))
        print('T3 反実仮想（英・読むだけ・分母の式だけを動かす静的な再計算）：海の金の割合 s＝海÷(海＋max(陸の必要量＋内, 自国の軍の大きさ))')
        for e in ('1783-1813', '1814-1853', '1854-1883', '1884-1913', '1914-1950', '全体'):
            v = [x for x in uk if e == '全体' or era(int(f(x['quarter']))) == e]
            if len(v) < 8:
                continue
            def s(drop_col, drop_proj):
                out = []
                for x in v:
                    nl = f(x['needLandIndex']) - (f(x['needLandBorderColonialIndex']) if drop_col else 0) - (f(x['needLandProjectionIndex']) if drop_proj else 0)
                    sea = f(x['needSeaIndex']); den = max(nl + f(x['needInternalIndex']), f(x['forcePotentialIndex']))
                    out.append(sea / (sea + den))
                return mean(out), pct(out, 0.5)
            a = s(False, False); b = s(True, False); c = s(False, True); d = s(True, True)
            def qing_half():
                out = []
                for x in v:
                    cq = 0.0
                    for tok in x['needLandTop3'].split(';'):
                        if tok.startswith('清:'):
                            cq = f(tok.split(':', 1)[1])
                    nl = f(x['needLandIndex']) - 0.5 * cq
                    sea = f(x['needSeaIndex']); den = max(nl + f(x['needInternalIndex']), f(x['forcePotentialIndex']))
                    out.append(sea / (sea + den))
                return mean(out), pct(out, 0.5)
            h = qing_half()
            print(f'  {e} | 行 {len(v)} | 今 {a[0]:.3f}（中央 {a[1]:.3f}）| 植民地国境を除く {b[0]:.3f}（{b[1]:.3f}）| 投射を除く {c[0]:.3f}（{c[1]:.3f}）| 両方を除く {d[0]:.3f}（{d[1]:.3f}）| 清の寄与を半分 {h[0]:.3f}（{h[1]:.3f}）')


def q2b(rows):
    A = [x for x in rows if x['landSideBindsFlag'] != '']
    MAIN = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル', 'スペイン', 'ロシア', 'プロイセン', 'オーストリア', 'アメリカ', '日本', '清', 'オスマン']
    print('\n#### Q2b 自国の陸軍が外へ向く割合（注意配分の分母 u＋Σ のうち u 以外）＝1 に近いほど〈内向きの軍は無い〉と読まれる')
    print('  国 | ' + ' | '.join(['1783-1813', '1814-1853', '1854-1883', '1884-1913', '1914-1950']) + '   各欄＝外へ向く割合の平均／内向きの u（不穏度）の平均／外向きの重みの和の平均')
    for nm in MAIN:
        cells = []
        for e in ('1783-1813', '1814-1853', '1854-1883', '1884-1913', '1914-1950'):
            v = [x for x in A if x['nation'] == nm and era(int(f(x['quarter']))) == e]
            cells.append(f'{mean([f(x["ownArmyOutwardShare"]) for v_ in [0] for x in v]):.3f}／{mean([f(x["unrestShare"]) for x in v]):.3f}／{mean([f(x["ownArmyOutwardWeightIndex"]) for x in v]):.2f}' if len(v) >= 8 else '-')
        print(f'  {nm} | ' + ' | '.join(cells))


def main():
    d = sys.argv[1]; what = sys.argv[2] if len(sys.argv) > 2 else 'all'
    rows = load(d)
    if not rows:
        print('fm_s が無い'); return
    if what in ('q1', 'all'):
        q1(rows, '全ての挙げた国')
        q1([x for x in rows if x['nation'] == 'イギリス'], '英だけ')
    if what in ('q2', 'all'):
        q2(rows)
        q2b(rows)


if __name__ == '__main__':
    main()
