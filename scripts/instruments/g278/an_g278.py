"""(278) 線G2：政治資本の在庫の読み手。python an_g278.py <計器の出力ディレクトリ> [出力ファイル]
nation_s<seed>.tsv の末尾の列（(278) の openFrontCosts・openFrontSpentAtDecision・politicalCapitalSpentAtLevelHead ほか。(274) の openFrontStates）を読む（読むだけ・本体は動かさない）。
 A. 符号 2（政治資本で固定）の分割：〈代価＞政治資本の最大量 b＝構造（在庫が空でも永久に届かない）〉と〈代価≦b だが残り a が足りない＝飢え〉（全ての国・種類別・段の法の軸別）
 B. 英・仏・普・露・日・清の20年窓ごとの表：在庫（頭の S÷b・在庫が空の割合）・その四半期に払った額（段の法・R・M・T）÷b・符号 2 の分割
 C. 仮説の検定「連続の軸の小さな押しが毎四半期の在庫を使い切り、段の法を飢えさせている」：飢えで固定された段の法の戦線を、静的な反事実で救えるか
 D. 分母を揃える線（CountOpenFronts を係数が掛かる連続の軸 R・T・M〔解禁の間を除く〕に揃える）の予測の数字（事前登録の材料）
符号（Instruments.GFront）：0 施行のとき閉じていた／1 純支持で固定／2 政治資本で固定／3 無償で動いた〔未到達〕／4 払って動いた〔未到達〕／5 不可逆で固定／6 前提で固定／
7 無償で動いて届いた／8 払って動いて届いた／9 軍事費の失効で動いた／-1 結果が来なかった。
政治資本の単位は最大量 b と同じ（目減り S＝払った額の積算・残り a＝max(0,b−S)・門は S＋代価≦b で通る）。Politics.Step が S を b/40×実行度ずつ回復する（四半期の間）。
★読み方の注意（自己申告）：
  ①C の反事実は静的で、1つの原因だけを取り除く（回復の max(0,·) の非線形・連鎖・取り除いた後の他の戦線の払いの増減は無視）。「救える」＝代価＋払う前の S−b ≦ 取り除いた額。
  ②目減り S の頭の値の内訳（連続の軸の払い／段の法の払い）は直近の払いからの帰属＝直近の払いから順に S の頭の値を満たす（回復は古い払いから消えると仮定。実際の回復は源を区別しない）。
     帰属しきれない残りは「窓の外・Step の外（Politics の内戦講和の譲歩ほか）」。
  ③D は連続の軸の歩幅が政治予算の係数に比例する（moved∝speed）ことから払いを係数の比で拡大するだけ（moved の上限 1・目標への収束・係数が変わった後の戦線の開閉は無視）。
  ④L.Nationalization の5部門は同じ軸名で並ぶ（軸ごとの表では1行に合算）。
"""
import csv, glob, os, sys, collections, statistics, math

NATIONS = [(2, 'イギリス'), (0, 'フランス'), (5, 'プロイセン'), (4, 'ロシア'), (1, '日本'), (11, '清')]
WIN = 20
GATE = 0.5
BACK = 80   # 直近の払いからの帰属で遡る最大の四半期数（b/40 の回復が最も遅くて 147 四半期だが、直近の払いで S の頭はほぼ満たされる）


def factor(n):
    return (1.0 / n) ** GATE if n > 1 else 1.0


def num(s):
    return float('nan') if s == 'nan' else (float('inf') if s == 'inf' else (float('-inf') if s == '-inf' else float(s)))


def load(d):
    rows = []
    for path in sorted(glob.glob(os.path.join(d, 'nation_s*.tsv')), key=lambda p: int(os.path.basename(p)[8:-4])):
        seed = int(os.path.basename(path)[8:-4])
        with open(path, encoding='utf-8') as f:
            for r in csv.DictReader(f, delimiter='\t'):
                n = int(r['openFrontsCount'])
                if n < 0:
                    continue
                st = r['openFrontStates']; cs = r['openFrontCosts']; ss = r['openFrontSpentAtDecision']
                its = st.split(';') if st else []; cl = cs.split(';') if cs else []; sl = ss.split(';') if ss else []
                if not (len(its) == len(cl) == len(sl)):
                    raise SystemExit('項目の数が合わない：seed %d q %s' % (seed, r['quarter']))
                items = []
                for it, c, s in zip(its, cl, sl):
                    ka, _, code = it.rpartition(':')
                    items.append((ka[0], ka[2:], int(code.rstrip('x')), num(c), num(s)))
                rows.append({'seed': seed, 'q': int(r['quarter']), 'year': int(r['year']), 'idx': int(r['nationIndex']), 'uid': r.get('nationUid', r['nationIndex']),
                             'n': n, 'items': items, 'f': float(r['pushBudgetFactor']), 'open': r['warStageOpenAfterGovFlag'] == '1',
                             'head': num(r['politicalCapitalSpentAtLevelHead']), 'end': num(r['politicalCapitalSpentAtStepEnd']), 'max': num(r['politicalCapitalMaxAtLevelHead']),
                             'pL': num(r['levelLawPaidInQuarter']), 'pR': num(r['ratePaidInQuarter']), 'pM': num(r['militaryPaidInQuarter']),
                             'pT': num(r['tariffPaidInQuarter']), 'pC': num(r['securityPaidInQuarter']), 'pK': num(r['continuousPaidInQuarter'])})
    return rows


def pct(a, b):
    return '%5.1f%%' % (100.0 * a / b) if b else '    - '


def mean(xs):
    xs = [x for x in xs if not math.isnan(x)]
    return statistics.mean(xs) if xs else float('nan')


def link(rows):
    """国の個体（seed, uid）ごとに四半期の順の系列を作り、前の四半期（q−1）の行を prev に付ける"""
    by = collections.defaultdict(list)
    for r in rows:
        by[(r['seed'], r['uid'])].append(r)
    for lst in by.values():
        lst.sort(key=lambda r: r['q'])
        for i, r in enumerate(lst):
            r['prev'] = lst[i - 1] if i > 0 and lst[i - 1]['q'] == r['q'] - 1 else None
            r['series'] = lst; r['i'] = i
    return by


def attribute(r):
    """目減り S の頭の値を、直近の払いからの帰属で〔連続の軸の払い・前の四半期以前の段の法の払い・残り〕に分ける"""
    head = r['head']
    if not (head > 0):
        return (0.0, 0.0, 0.0)
    lst = r['series']; i = r['i']; rem = head; cont = lvl = 0.0
    j = i - 1; q = r['q']
    while rem > 1e-15 and j >= 0 and i - j <= BACK and lst[j]['q'] == q - (i - j):
        p = lst[j]
        c = p['pK'] if not math.isnan(p['pK']) else 0.0
        t = min(c, rem); cont += t; rem -= t
        if rem <= 1e-15:
            break
        l = p['pL'] if not math.isnan(p['pL']) else 0.0
        t = min(l, rem); lvl += t; rem -= t
        j -= 1
    return (cont, lvl, max(rem, 0.0))


def split_stats(rs):
    """符号 2 の分割（件数）"""
    c = collections.Counter()
    for r in rs:
        b = r['max']
        for k, a, code, cost, sdec in r['items']:
            c['all'] += 1
            kk = 'L' if k == 'L' else 'K'
            c[kk] += 1
            if code == 2:
                s = 'S' if cost > b else 'H'   # S＝構造・H＝飢え
                c[kk + s] += 1; c['2' + s] += 1; c['2'] += 1
            elif code in (4, 8):
                c[kk + 'P'] += 1
            if code in (1, 5, 6):
                c['pure'] += 1
    return c


def window_row(label, rs):
    k = len(rs)
    if not k:
        return label + ' 0 行'
    c = split_stats(rs)
    heads = [r['head'] / r['max'] if r['max'] > 0 else float('nan') for r in rs]
    ends = [r['end'] / r['max'] if r['max'] > 0 else float('nan') for r in rs]
    emptyH = sum(1 for r in rs if r['max'] > 0 and r['head'] >= r['max'])
    emptyE = sum(1 for r in rs if r['max'] > 0 and r['end'] >= r['max'])
    mb = mean([r['max'] for r in rs])
    pl = mean([r['pL'] for r in rs]); pr = mean([r['pR'] for r in rs]); pm = mean([r['pM'] for r in rs]); pt = mean([r['pT'] for r in rs]); pk = mean([r['pK'] for r in rs])
    return ('%-10s %6d  b=%6.2f  S頭/b=%.2f(空%s) S末/b=%.2f(空%s)  払い/b：段%.3f 連続%.3f〔R%.3f M%.3f T%.3f〕 連続の割合%s  '
            'Lの戦線%.2f本：資本固定 構造%s 飢え%s／払って通った%s  連続の資本固定 構造%s 飢え%s'
            % (label, k, mb, mean(heads), pct(emptyH, k), mean(ends), pct(emptyE, k), pl / mb, pk / mb, pr / mb, pm / mb, pt / mb, pct(pk, pk + pl),
               c['L'] / k, pct(c['LS'], c['L']), pct(c['LH'], c['L']), pct(c['LP'], c['L']), pct(c['KS'], c['K']), pct(c['KH'], c['K'])))


def main():
    d = sys.argv[1]
    outp = sys.argv[2] if len(sys.argv) > 2 else None
    rows = [r for r in load(d)]
    seeds = sorted(set(r['seed'] for r in rows))
    led = [r for r in rows if not math.isnan(r['head'])]   # Step が来た行（台帳のある行）
    link(rows)
    q0 = {}
    for r in rows:
        if r['seed'] not in q0 or r['q'] < q0[r['seed']]:
            q0[r['seed']] = r['q']
    uid_of = {(r['seed'], r['idx']): r['uid'] for r in rows if r['q'] == q0[r['seed']]}
    for r in rows:
        r['six'] = next((i for i, _ in NATIONS if uid_of.get((r['seed'], i)) == r['uid']), None)
    lines = []
    P = lines.append
    P('(278) 政治資本の在庫の表  入力=%s  種=%s  行=%d（うち台帳のある行 %d）' % (d, seeds, len(rows), len(led)))
    P('')

    # ---------------- A
    P('==== A. 符号 2（政治資本で固定）の分割：構造（代価＞最大量 b）／飢え（代価≦b だが残り a が足りない） ====')
    c = split_stats(rows)
    N = c['all']
    P('全ての国：戦線 %d 件＝段の法 L %s・連続の軸 %s' % (N, pct(c['L'], N), pct(c['K'], N)))
    P('  符号 2 全体 %s〔(274) の 47.8%% と同じ〕＝構造 %s＋飢え %s（符号 2 に対する割合 構造 %s・飢え %s）' % (pct(c['2'], N), pct(c['2S'], N), pct(c['2H'], N), pct(c['2S'], c['2']), pct(c['2H'], c['2'])))
    P('  段の法 L：資本固定 %s＝構造 %s＋飢え %s（L に対する割合）／払って通った %s' % (pct(c['LS'] + c['LH'], c['L']), pct(c['LS'], c['L']), pct(c['LH'], c['L']), pct(c['LP'], c['L'])))
    P('  連続の軸：資本固定 %s＝構造 %s＋飢え %s／払って通った（符号 4・8）%s' % (pct(c['KS'] + c['KH'], c['K']), pct(c['KS'], c['K']), pct(c['KH'], c['K']), pct(c['KP'], c['K'])))
    P('  幽霊（政治資本の在庫が空でも・満タンでも動かない）＝純支持・不可逆・前提 %s＋構造 %s＝%s（(274) の「真の幽霊 10.2%%」は下限だった）' % (pct(c['pure'], N), pct(c['2S'], N), pct(c['pure'] + c['2S'], N)))
    # 段の法の軸別
    ax = collections.defaultdict(collections.Counter)
    ratio = collections.defaultdict(list)
    for r in rows:
        for k, a, code, cost, sdec in r['items']:
            if k != 'L':
                continue
            ax[a]['open'] += 1; ax[a][code] += 1
            if code == 2:
                ax[a]['S' if cost > r['max'] else 'H'] += 1
                if r['max'] > 0:
                    ratio[a].append(cost / r['max'])
    P('段の法の軸別（開いた件数の多い順・符号 2 の分割・代価÷b の中央）：')
    P('%-26s %8s %8s %8s %8s  %s' % ('軸', '開いた', '資本固定', '構造', '飢え', '通った(3,4,7,8)／符号2の戦線だけの代価÷b中央'))
    for a, cc in sorted(ax.items(), key=lambda kv: -kv[1]['open']):
        n = cc['open']
        P('%-26s %8d %8s %8s %8s  %s／%s' % (a, n, pct(cc[2], n), pct(cc['S'], n), pct(cc['H'], n), pct(cc[3] + cc[4] + cc[7] + cc[8], n),
                                              ('%.2f' % statistics.median(ratio[a])) if ratio[a] else '-'))
    P('')

    # ---------------- B
    P('==== B. 英・仏・普・露・日・清の20年窓（S頭/b＝段の法のループの頭の目減り÷最大量・空＝S頭≧b〔残り 0〕の割合・払い/b＝その四半期の払い÷b の平均） ====')
    years = sorted(set(r['year'] for r in rows))
    y0 = min(years); y1 = max(years)
    wins = [(a, min(a + WIN, y1 + 1)) for a in range(y0, y1 + 1, WIN)]
    for idx, name in NATIONS:
        P('-- %s' % name)
        sub = [r for r in led if r['six'] == idx]
        for a, b in wins:
            rs = [r for r in sub if a <= r['year'] < b]
            if rs:
                P(window_row('%d-%d' % (a, b - 1), rs))
        P(window_row('全期間', sub))
    P('-- 全ての国')
    for a, b in wins:
        rs = [r for r in led if a <= r['year'] < b]
        if rs:
            P(window_row('%d-%d' % (a, b - 1), rs))
    P(window_row('全期間', led))
    P('')

    # ---------------- C
    P('==== C. 仮説の検定：連続の軸の小さな押しが毎四半期の在庫を使い切り、段の法を飢えさせているか ====')
    rl = [r for r in led if r['prev'] is not None and not math.isnan(r['prev']['end'])]
    unclamped = [r['prev']['end'] - r['head'] for r in rl if r['head'] > 0]
    P('台帳（全ての国・台帳のある行）：1国×四半期あたり 段の法の払い %.4f・連続の軸の払い %.4f（R %.4f・M %.4f・T %.4f・C %.4f）・b の平均 %.2f' % (
        mean([r['pL'] for r in led]), mean([r['pK'] for r in led]), mean([r['pR'] for r in led]), mean([r['pM'] for r in led]), mean([r['pT'] for r in led]), mean([r['pC'] for r in led]),
        mean([r['max'] for r in led])))
    P('  連続の軸の払いの割合（払いの合計に対して）%s・払いが 0 の四半期の割合：段の法 %s・連続の軸 %s' % (
        pct(sum(r['pK'] for r in led), sum(r['pK'] + r['pL'] for r in led)), pct(sum(1 for r in led if r['pL'] == 0), len(led)), pct(sum(1 for r in led if r['pK'] == 0), len(led))))
    P('  回復の実測（前の四半期の S末−今の S頭、S頭＞0 の行のみ＝0 に張り付いていない行）：平均 %.4f（1四半期あたり・b の平均に対して %s）／S頭＝0 の行 %s' % (
        mean(unclamped), pct(mean(unclamped), mean([r['max'] for r in led])), pct(sum(1 for r in led if r['head'] <= 0), len(led))))
    P('  払いの合計（段＋連続）の平均 %.4f と回復 %.4f の比＝%.2f（1 に近い＝在庫が壁で釣り合っている）' % (
        mean([r['pK'] + r['pL'] for r in led]), mean(unclamped), (mean([r['pK'] + r['pL'] for r in led]) / mean(unclamped)) if mean(unclamped) else float('nan')))
    P('')
    # 飢えで固定された段の法の戦線
    cnt = collections.Counter(); sums = collections.Counter()
    heads_cont_share = []
    for r in led:
        if r['prev'] is None:
            continue
        b = r['max']
        fronts = [(a, cost, sdec) for k, a, code, cost, sdec in r['items'] if k == 'L' and code == 2 and not (cost > b)]
        if not fronts:
            continue
        cont, lvl, rest = attribute(r)
        head = r['head']
        heads_cont_share.append((cont, lvl, rest, head))
        for a, cost, sdec in fronts:
            short = sdec + cost - b          # 不足（正）
            earlier = max(sdec - head, 0.0)    # この四半期に先に施行された段の法の払い
            cnt['n'] += 1
            cnt['head>=b'] += 1 if head >= b else 0
            cnt['rescue_cont'] += 1 if short <= cont else 0
            cnt['rescue_lvlprev'] += 1 if short <= lvl else 0
            cnt['rescue_earlier'] += 1 if short <= earlier else 0
            cnt['rescue_alllevel'] += 1 if short <= lvl + earlier else 0
            cnt['rescue_head'] += 1 if short <= head + earlier else 0       # 前の四半期以前の払いが全部無かった（頭の S＝0）
            cnt['rescue_contprev1'] += 1 if short <= min(head, r['prev']['pK'] if not math.isnan(r['prev']['pK']) else 0.0) else 0
            cnt['rescue_none'] += 1 if short > head + earlier else 0       # 頭の S も先の払いも全部取り除いても足りない（b の別の理由）
            cnt['contprev>0'] += 1 if (not math.isnan(r['prev']['pK']) and r['prev']['pK'] > 0) else 0
            sums['short'] += short; sums['cost'] += cost; sums['shortratio'] += short / cost if cost > 0 else 0
    n = cnt['n']
    P('飢えで固定された段の法の戦線（代価≦b・S＋代価＞b・前の四半期の行がある）：%d 件' % n)
    if n:
        P('  不足（S＋代価−b）の平均 %.4f・代価の平均 %.4f・不足÷代価の平均 %.2f' % (sums['short'] / n, sums['cost'] / n, sums['shortratio'] / n))
        P('  段の法のループの頭で既に残り 0（S頭≧b）だった割合 %s  前の四半期に連続の軸が払っていた割合 %s' % (pct(cnt['head>=b'], n), pct(cnt['contprev>0'], n)))
        cc_ = split_stats(led)
        P('  全体への効き：飢え（代価≦b）は L の資本固定の %s（L の戦線の %s）／連続の軸の払いを全て無くして救える分は L の戦線の %.2f%%・資本固定の戦線の %.2f%%' % (
            pct(cc_['LH'], cc_['LS'] + cc_['LH']), pct(cc_['LH'], cc_['L']), 100.0 * cc_['LH'] / cc_['L'] * cnt['rescue_cont'] / n, 100.0 * cc_['LH'] / (cc_['LS'] + cc_['LH']) * cnt['rescue_cont'] / n))
        P('  1つの原因だけを取り除いたとき、その戦線が通る割合（静的な反事実・救える＝不足≦取り除く額）：')
        P('    連続の軸の払いを全て無くす（S頭の直近の払いからの帰属分）                 %s' % pct(cnt['rescue_cont'], n))
        P('    〔上限〕前の四半期の連続の軸の払いだけを無くす（min(S頭, 前の四半期の払い)） %s' % pct(cnt['rescue_contprev1'], n))
        P('    前の四半期以前の段の法の払いを全て無くす（S頭の帰属分）                   %s' % pct(cnt['rescue_lvlprev'], n))
        P('    この四半期の先の段の法の払いを無くす（S頭と払う前の S の差）              %s' % pct(cnt['rescue_earlier'], n))
        P('    段の法の払い（過去分＋この四半期の先の分）を全て無くす                     %s' % pct(cnt['rescue_alllevel'], n))
        P('    S頭を 0 にする（前の四半期以前の払い全部＋この四半期の先の分）             %s  ←これでも通らない（b そのものの不足）%s' % (pct(cnt['rescue_head'], n), pct(cnt['rescue_none'], n)))
        tc = sum(x[0] for x in heads_cont_share); tl = sum(x[1] for x in heads_cont_share); tr = sum(x[2] for x in heads_cont_share); th = sum(x[3] for x in heads_cont_share)
        P('  飢えの戦線がある四半期の S頭の内訳（直近の払いからの帰属）：連続の軸の払い %s・段の法の払い %s・窓の外／Step の外 %s' % (pct(tc, th), pct(tl, th), pct(tr, th)))
    P('')

    # ---------------- D
    P('==== D. 分母を揃える線の予測の数字（事前登録の材料・静的）＝CountOpenFronts の数え方を連続の軸 R・T・M（解禁の間は M を外す）に限る ====')
    P('政治予算の係数 f(N)=(1/N)^%.1f（N≦1 は 1）。連続の軸の歩幅∝係数 ⇒ 連続の軸の払いも係数の比で増える（M は解禁の間は係数が掛からない＝不変）。' % GATE)
    def Nc(r):
        n = 0
        for k, a, code, cost, sdec in r['items']:
            if k in ('R', 'T') or (k == 'M' and not r['open']):
                n += 1
        return n
    mult = []; dpaid = []; base_k = []; newstarved = collections.Counter(); byc = collections.defaultdict(list)
    for r in led:
        nc = Nc(r)
        m = factor(nc) / r['f'] if r['f'] > 0 else float('nan')
        mult.append(m)
        pk = (r['pR'] + r['pT']) * m + (r['pM'] if r['open'] else r['pM'] * m) + r['pC']   # 解禁の間の M は不変
        dpaid.append(pk - r['pK']); base_k.append(r['pK'])
        r['dK'] = pk - r['pK']
    P('  係数の比（新/今）：平均 %.3f・中央 %.3f（動いた戦線で数えず、開いた連続の軸で数える）。N の平均 %.2f→N_連続の平均 %.2f' % (
        mean(mult), statistics.median([m for m in mult if not math.isnan(m)]), mean([r['n'] for r in led]), mean([Nc(r) for r in led])))
    P('  連続の軸の払い（1国×四半期）：今 %.4f → 予測 %.4f（%.2f 倍）・b に対して %.3f → %.3f' % (
        mean(base_k), mean(base_k) + mean(dpaid), (mean(base_k) + mean(dpaid)) / mean(base_k) if mean(base_k) else float('nan'),
        mean(base_k) / mean([r['max'] for r in led]), (mean(base_k) + mean(dpaid)) / mean([r['max'] for r in led])))
    # 新しく飢える段の法：払って通った段の法の戦線が、前の四半期の払いの増分で通らなくなる
    npaid = nnew = 0; nopen = 0; nL = 0; nwall = 0
    for r in led:
        for k, a, code, cost, sdec in r['items']:
            if k != 'L':
                continue
            nL += 1
            if code in (4, 8):
                npaid += 1
                dprev = r['prev']['dK'] if r['prev'] is not None and 'dK' in r['prev'] and not math.isnan(r['prev']['dK']) else 0.0
                if r['head'] > 0 and sdec + dprev + cost > r['max']:
                    nnew += 1
        if r['head'] >= r['max']:
            nwall += 1
    P('  一歩の遅れだけで読む影響（S頭′＝S頭＋前の四半期の払いの増分・S頭＝0 の行は 0 のまま＝過小）：')
    P('    今は払って通った段の法の戦線 %d 件（段の法の戦線の %s）のうち、予測で通らなくなる %d 件（%s）' % (npaid, pct(npaid, nL), nnew, pct(nnew, npaid)))
    P('    段の法の資本固定は L の %s（飢え %s＋構造 %s）→ 予測で L の戦線の %.3f%% ぶん増える（通れなくなる件数÷L の戦線）' % (
        pct(split_stats(led)['LS'] + split_stats(led)['LH'], nL), pct(split_stats(led)['LH'], nL), pct(split_stats(led)['LS'], nL), 100.0 * nnew / nL))
    P('    段の法のループの頭で既に残り 0 の四半期 %s（この行では増分が効かない＝悪化の余地が小さい側）' % pct(nwall, len(led)))
    P('  ⚠これは1四半期の遅れだけの下限寄りの見立て。回復が一定（b/40×実行度）で払いが回復を上回れば S は壁（b）へ張り付く＝均衡では増分が段の法の払いの総量を押し出す側に効く。')
    P('    均衡の見立て：段の法の払い %.4f＋連続の軸の払い %.4f（今）／%.4f（予測）に対し、回復の実測 %.4f（1四半期）。' % (
        mean([r['pL'] for r in led]), mean(base_k), mean(base_k) + mean(dpaid), mean(unclamped)))
    text = '\n'.join(lines) + '\n'
    if outp:
        with open(outp, 'w', encoding='utf-8', newline='\n') as f:
            f.write(text)
    sys.stdout.reconfigure(encoding='utf-8')
    print(text)


if __name__ == '__main__':
    main()
