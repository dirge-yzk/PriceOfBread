"""(274) 線G：幽霊戦線の読み手。python an_ghost.py <計器の出力ディレクトリ> [出力ファイル]
nation_s<seed>.tsv の末尾の列（openFrontsCount ほか・openFrontStates）を読み、次の表を出す（読むだけ・本体は動かさない）。
 A. 英・仏・普・露・日・清の20年窓ごとの表（開いた戦線 N の中身・動く戦線だけで数えた N′ と係数）
 B. 全ての国の和・種類別（段の法 L／連続の軸 R・M・T）・符号別
 C. 軸ごとの内訳（英・仏・普・露・日・清の合算）：開いていた四半期・固定の割合・主な理由・固定が連続した長さ
 D. (270) vb#1 の静的な反事実：戦時段の解禁の間に軍事費の戦線を N から外したら
 E. N の数え方の代替案（静的な反事実）：純幽霊を除く／連続の軸だけ（係数が実際に掛かる側と同じ集合）／動いた戦線だけ
符号（Instruments.GFront）：0 施行のとき閉じていた／1 純支持で固定／2 政治資本で固定／3 無償で動いた〔未到達〕／4 払って動いた〔未到達〕／5 不可逆で固定／6 前提で固定／
7 無償で動いて届いた／8 払って動いて届いた／9 軍事費の失効で動いた／-1 結果が来なかった。「動く」＝3・4・7・8・9。
「純幽霊」＝1・5・6（純支持の門・不可逆・前提＝政治資本があっても動かない）。2（政治資本の在庫が代価に届かず固定）は「飢え」＝資本があれば動く。
⚠静的な反事実：係数を変えれば歩幅が変わり、政治資本の払いも変わる（固定が解ける軸も増える）。この読み手は N′ から係数を引くだけで、動力学の再現ではない。
"""
import csv, glob, os, sys, collections, math, statistics

NATIONS = [(2, 'イギリス'), (0, 'フランス'), (5, 'プロイセン'), (4, 'ロシア'), (1, '日本'), (11, '清')]
MOVING = {3, 4, 7, 8, 9}
PURE = {1, 5, 6}
FROZEN = {1, 2, 5, 6}
WIN = 20
GATE = 0.5   # PoliticalBudgetGate の既定（Coefficients）。nation_s の pushBudgetFactor から N を使って確かめる


def factor(n):
    return (1.0 / n) ** GATE if n > 1 else 1.0


def load(d):
    rows = []
    for path in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')), key=lambda p: int(os.path.basename(p)[8:-4])):
        seed = int(os.path.basename(path)[8:-4])
        with open(path, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                n = int(r['openFrontsCount'])
                if n < 0:
                    continue
                st = r['openFrontStates']
                items = []
                for it in (st.split(';') if st else []):
                    ka, _, c = it.rpartition(':')
                    items.append((ka[0], ka[2:], int(c.rstrip('x')), c.endswith('x')))
                rows.append({'seed': seed, 'q': int(r['quarter']), 'year': int(r['year']), 'idx': int(r['nationIndex']), 'nation': r['nation'], 'n': n, 'items': items,
                             'f': float(r['pushBudgetFactor']), 'open': r['warStageOpenAfterGovFlag'] == '1', 'mil': r['militaryFrontCountedFlag'] == '1',
                             'atwar': r['atWarAtDecisionFlag'] == '1'})
    return rows


def pct(a, b):
    return '%5.1f%%' % (100.0 * a / b) if b else '    - '


def summarize(rs):
    """rs＝行のリスト → 要約の辞書"""
    out = collections.Counter()
    sumN = sumNp = sumNc = 0
    fs = []; fps = []; ratios = []; fcs = []
    for r in rs:
        n = r['n']; its = r['items']
        mv = sum(1 for k, a, c, x in its if c in MOVING)
        mvc = sum(1 for k, a, c, x in its if c in MOVING and k != 'L')
        sumN += n; sumNp += mv; sumNc += mvc
        for k, a, c, x in its:
            out[c] += 1
            out['L' if k == 'L' else 'K'] += 1
            if k != 'L' and c in FROZEN:
                out['Kfrozen'] += 1
            if k == 'L' and c in FROZEN:
                out['Lfrozen'] += 1
        f = r['f']; fp = factor(mv); fc = factor(mvc)
        fs.append(f); fps.append(fp); fcs.append(fc); ratios.append(fp / f if f > 0 else float('nan'))
    k = len(rs)
    return {'rows': k, 'sumN': sumN, 'N': sumN / k if k else float('nan'), 'Np': sumNp / k if k else float('nan'), 'Nc': sumNc / k if k else float('nan'),
            'cnt': out, 'f': statistics.mean(fs) if fs else float('nan'), 'fp': statistics.mean(fps) if fps else float('nan'),
            'fc': statistics.mean(fcs) if fcs else float('nan'), 'ratio': statistics.mean(ratios) if ratios else float('nan')}


def table_row(label, s):
    c = s['cnt']; N = s['sumN']
    reached = c[7] + c[8]; part = c[3] + c[4] + c[9]
    return ('%-14s %6d  N=%5.2f (段%4.2f+連続%4.2f)  動く%s [届いた%s・途中%s]  固定%s [純支持%s・資本%s・不可逆%s・前提%s]  閉じ%s 不明%s  N\'=%5.2f  係数 %.3f→%.3f（比%.2f）N\'連続のみ=%4.2f→%.3f'
            % (label, s['rows'], s['N'], c['L'] / s['rows'] if s['rows'] else 0, c['K'] / s['rows'] if s['rows'] else 0,
               pct(reached + part, N), pct(reached, N), pct(part, N), pct(c[1] + c[2] + c[5] + c[6], N), pct(c[1], N), pct(c[2], N), pct(c[5], N), pct(c[6], N),
               pct(c[0], N), pct(c[-1], N), s['Np'], s['f'], s['fp'], s['ratio'], s['Nc'], s['fc']))


def streaks(rs):
    """(種類.軸) ごとに、同じ国・同じ種の中で固定（FROZEN）が連続した四半期の長さを数える"""
    by = collections.defaultdict(list)
    for r in rs:
        by[(r['seed'], r['idx'])].append(r)
    lens = []; axis_lens = collections.defaultdict(list)
    for key, lst in by.items():
        lst.sort(key=lambda r: r['q'])
        cur = {}
        prevq = None
        for r in lst:
            now = {}
            seen = collections.Counter()
            for k, a, c, x in r['items']:
                seen[(k, a)] += 1
                ka = (k, a, seen[(k, a)])
                if c in FROZEN:
                    now[ka] = cur.get(ka, 0) + 1
            for ka, v in cur.items():
                if ka not in now:
                    lens.append(v); axis_lens[ka[:2]].append(v)
            cur = now
        for ka, v in cur.items():
            lens.append(v); axis_lens[ka[:2]].append(v)
    return lens, axis_lens


def main():
    d = sys.argv[1]
    outp = sys.argv[2] if len(sys.argv) > 2 else None
    rows = load(d)
    seeds = sorted(set(r['seed'] for r in rows))
    lines = []
    P = lines.append
    P('(274) 幽霊戦線の表  入力=%s  種=%s  行=%d（開いた戦線を数えた国×四半期）' % (d, seeds, len(rows)))
    P('係数 (1/N)^%.1f。N′＝その四半期の施行で動いた戦線（符号 3・4・7・8・9）の数。N′≦1 の係数は 1.0。「連続のみ」＝動いた戦線のうち段の法を除く数（政治予算が歩幅を薄めるのは連続の軸だけ）。' % GATE)
    P('')
    years = sorted(set(r['year'] for r in rows))
    y0 = min(years); y1 = max(years)
    wins = [(a, min(a + WIN, y1 + 1)) for a in range(y0, y1 + 1, WIN)]
    P('==== A. 国別・20年窓（%d〜%d）====' % (y0, y1))
    for idx, name in NATIONS:
        P('-- %s（nationIndex %d）' % (name, idx))
        sub = [r for r in rows if r['idx'] == idx]
        for a, b in wins:
            rs = [r for r in sub if a <= r['year'] < b]
            if rs:
                P(table_row('%d-%d' % (a, b - 1), summarize(rs)))
        P(table_row('全期間', summarize(sub)))
    P('')
    P('==== B. 全ての国（死んだ国・建国の再利用を含む nationIndex 全部）====')
    for a, b in wins:
        rs = [r for r in rows if a <= r['year'] < b]
        if rs:
            P(table_row('%d-%d' % (a, b - 1), summarize(rs)))
    allS = summarize(rows)
    P(table_row('全期間', allS))
    c = allS['cnt']
    P('種類別（全期間・戦線の数に対する割合）：段の法 L %s・連続の軸（税と支出の段 R・軍事費 M・治安維持力 C・関税 T）%s／L のうち固定 %s・連続のうち固定 %s'
      % (pct(c['L'], allS['sumN']), pct(c['K'], allS['sumN']), pct(c['Lfrozen'], c['L']), pct(c['Kfrozen'], c['K'])))
    P('')
    sel = [r for r in rows if r['idx'] in dict(NATIONS)]
    lens, axl = streaks(sel)
    tot_frozen_q = sum(lens)
    P('==== C. 軸ごとの内訳（英・仏・普・露・日・清の合算）====')
    P('固定（純支持・資本・不可逆・前提）が連続した長さ：回数 %d・中央 %s 四半期・平均 %.1f・8 四半期以上の連なりが固定の四半期に占める割合 %s・40 四半期以上 %s'
      % (len(lens), statistics.median(lens) if lens else '-', statistics.mean(lens) if lens else float('nan'),
         pct(sum(v for v in lens if v >= 8), tot_frozen_q), pct(sum(v for v in lens if v >= 40), tot_frozen_q)))
    cnt_open = collections.Counter(); cnt_code = collections.defaultdict(collections.Counter)
    for r in sel:
        for k, a, c_, x in r['items']:
            cnt_open[(k, a)] += 1; cnt_code[(k, a)][c_] += 1
    P('%-34s %7s %7s  %s' % ('種類.軸', '開いた', '固定', '符号の内訳（件数）  /固定の連なりの中央・最長'))
    for ka, n in cnt_open.most_common(40):
        cc = cnt_code[ka]
        fz = sum(cc[c_] for c_ in FROZEN)
        ls = axl.get(ka, [])
        P('%-34s %7d %7s  %s  /%s・%s' % ('%s.%s' % ka, n, pct(fz, n), ' '.join('%d:%d' % (c_, v) for c_, v in sorted(cc.items())),
                                         statistics.median(ls) if ls else '-', max(ls) if ls else '-'))
    P('')
    P('==== D. (270) vb#1 の静的な反事実：戦時段の解禁の間（warStageOpenAfterGovFlag=1）に軍事費の戦線を N から外したら ====')
    P('軍事費の戦線は解禁の間 fullPush で自分は政治予算の係数を掛けない（B2）が、N には数えられ他の軸の歩幅を薄めている。外すと他の軸の係数が (N/(N−1))^%.1f 倍。' % GATE)
    for label, pred in [('英・解禁の間', lambda r: r['idx'] == 2 and r['open'] and r['mil']), ('全ての国・解禁の間', lambda r: r['open'] and r['mil']),
                        ('英・交戦中で解禁の外', lambda r: r['idx'] == 2 and r['atwar'] and not r['open']), ('英・平時', lambda r: r['idx'] == 2 and not r['atwar'] and not r['open'])]:
        rs = [r for r in rows if pred(r)]
        if not rs:
            P('%-22s 0 行' % label); continue
        ns = [r['n'] for r in rs]
        f_now = [factor(n) for n in ns]; f_nomil = [factor(n - 1) if r['mil'] else factor(n) for n, r in zip(ns, rs)]
        mv = [sum(1 for k, a, c_, x in r['items'] if c_ in MOVING and k != 'M') for r in rs]
        P('%-22s %6d 行  N 平均 %.2f（中央 %s）  係数 %.3f → 軍事費を外すと %.3f（比 %.3f）  同じ行で「動いた他の戦線」の平均 %.2f 本' % (
            label, len(rs), statistics.mean(ns), statistics.median(ns), statistics.mean(f_now), statistics.mean(f_nomil),
            statistics.mean(b / a for a, b in zip(f_now, f_nomil)), statistics.mean(mv)))
    P('')
    P('==== E. N の数え方の代替案（静的な反事実）＝係数 (1/N)^%.1f の分母に何を数えるか ====' % GATE)
    P('政治予算の係数が掛かるのは連続の軸（R・M〔解禁の間を除く〕・T）の歩幅だけ。段の法（L）は StepLevel が一歩ずつ進み係数を掛けない。')
    P('案0＝今（開いた戦線の全部）／案1＝純幽霊（符号 1・5・6）を除く／案2＝連続の軸の開いた戦線だけ（係数が実際に掛かる側と同じ集合）／案3＝動いた戦線だけ N′（全種類）／案4＝動いた連続の軸だけ')
    P('各案の「N の平均→係数の平均（案0 に対する比）」')
    def alts(rs):
        ns = [[], [], [], [], []]; fs = [[], [], [], [], []]
        for r in rs:
            its = r['items']
            n0 = r['n']
            n1 = sum(1 for k, a, c_, x in its if c_ not in PURE)
            n2 = sum(1 for k, a, c_, x in its if k != 'L')
            n3 = sum(1 for k, a, c_, x in its if c_ in MOVING)
            n4 = sum(1 for k, a, c_, x in its if c_ in MOVING and k != 'L')
            for i, n in enumerate((n0, n1, n2, n3, n4)):
                ns[i].append(n); fs[i].append(factor(n))
        return ns, fs
    def alt_line(label, rs):
        if not rs:
            return
        ns, fs = alts(rs)
        f0 = statistics.mean(fs[0])
        parts = ['案%d %4.2f→%.3f(%.2f)' % (i, statistics.mean(ns[i]), statistics.mean(fs[i]), statistics.mean(fs[i]) / f0) for i in range(5)]
        P('%-12s %6d行  %s' % (label, len(rs), '  '.join(parts)))
    for idx, name in NATIONS:
        alt_line(name, [r for r in rows if r['idx'] == idx])
    alt_line('六国の合算', sel)
    alt_line('全ての国', rows)
    alt_line('全・平時', [r for r in rows if not r['atwar']])
    alt_line('全・交戦中', [r for r in rows if r['atwar']])
    cl = collections.Counter(); cc = collections.Counter()
    for r in rows:
        for k, a, c_, x in r['items']:
            (cl if k == 'L' else cc)[c_] += 1
    tl = sum(cl.values()); tc = sum(cc.values())
    P('')
    P('段の法 L の符号（全ての国・%d 件）：%s' % (tl, ' '.join('%d:%s' % (c_, pct(v, tl).strip()) for c_, v in sorted(cl.items()))))
    P('連続の軸の符号（全ての国・%d 件）：%s' % (tc, ' '.join('%d:%s' % (c_, pct(v, tc).strip()) for c_, v in sorted(cc.items()))))
    mvc = cc[3] + cc[4] + cc[7] + cc[8]
    P('連続の軸の動いた一歩のうち政治資本を払った割合 %s（段の法が政治資本の在庫で止まる四半期が多い裏づけ＝連続の軸の小さな押しが毎四半期在庫を使う）' % pct(cc[4] + cc[8], mvc))
    text = '\n'.join(lines) + '\n'
    if outp:
        with open(outp, 'w', encoding='utf-8', newline='\n') as f:
            f.write(text)
    sys.stdout.reconfigure(encoding='utf-8')
    print(text)


if __name__ == '__main__':
    main()
