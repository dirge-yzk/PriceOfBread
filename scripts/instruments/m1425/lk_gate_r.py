# (1425) 合流：c151lk/lk_gate.py の写し（種の起点を環境変数 SB で渡す＝S11 は SB=11・他は同じ）。
# (1415) c151lk の門と記録の台本（生を読むだけ・エンジンを呼ばない）。
# 使い方: python lk_gate.py <生のフォルダ> [種の数=10] [--wars-only]
# 門 G1（主・誰に届いたか＝全ての戦争の全ての月）：続いている戦争の月の戦勝点の差 d が WarPushSpeed(0.10) を超えた月の数。
#   今の式 d = 0.10×pushShare + 運（|pushShare|≤1）＝運が 0 なら |d|≤0.10 は式から必ず成り立つ。旗 0（運 0.12）では運が上乗せした月が数えられる。
#   数えから外す月：その戦争の最初の行（始まりの戦勝点を知らない）・その月の調停（forcedFlag＝1・Score を 0 に置く）・
#   前の月に崩壊の書き換え（oldCollapse*Flag＝1・Score を代入する）・端（|S|≥1.5−1e-9＝切り詰め）。
# 記録：終わり方（崩壊・決定的・疲れ果て・調停・他）・決定的で 10 か月未満・白紙の講和（wars_s verdictCode＝0）・
#   最後まで続いた戦争（wars_s verdictCode＝−1）の数と種ごとの最大・最長の戦争の月数・戦争の平均の月数。
# 1 本の戦争＝wexit の同じ組（A・B・内戦の旗）で月が続く行（月が飛んだら別の戦争）。
import sys, collections

WP = 0.10

def load(p):
    rows = open(p, encoding='utf-8').read().splitlines()
    h = rows[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
    return ix, [r.split('\t') for r in rows[1:] if r]

def ended_row(ix, r):
    # 旗 WarExitFromCapacity＝0 の道で、その月に必ず終わる行（決定的・疲れ果て・調停）。崩壊は代入の後の値で決まる＝ここでは割らない。
    return r[ix['oldDecisiveFlag']] == '1' or r[ix['oldWearyFlag']] == '1' or r[ix['forcedFlag']] == '1'

def groups(ix, rows):
    # 同じ組で次の月に新しい戦争が始まる形（前の行が終わりの行）も別の戦争に割る。
    d = collections.OrderedDict(); last = {}; cnt = collections.Counter(); prev_end = {}
    for r in rows:
        g = (r[ix['sideA']], r[ix['sideB']], r[ix['civilFlag']]); m = int(r[ix['month']])
        if g not in last or m != last[g] + 1 or prev_end.get(g, False): cnt[g] += 1
        last[g] = m; prev_end[g] = ended_row(ix, r); d.setdefault(g + (cnt[g],), []).append(r)
    return d

def endpath(ix, last):
    f = lambda c: last[ix[c]] == '1'
    if f('forcedFlag'): return 'mediation'
    if f('oldCollapseAFlag') or f('oldCollapseBFlag'): return 'collapse'
    if f('oldDecisiveFlag'): return 'decisive'
    if f('oldWearyFlag'): return 'weary'
    return 'other'

def wexit_seed(p, lastmonth):
    ix, rows = load(p)
    usable = over = moved = 0; ends = collections.Counter(); dec_short = 0; first_over = 0; maxabs = 0.0
    for g, rs in groups(ix, rows).items():
        prev = None; prev_collapse = False
        for k, r in enumerate(rs):
            s = float(r[ix['scoreIndex']])
            if k == 0:
                if abs(s) > WP * (1 + 1e-9) + 1e-12: first_over += 1
            elif not (r[ix['forcedFlag']] == '1' or prev_collapse or abs(s) >= 1.5 - 1e-9 or abs(prev) >= 1.5 - 1e-9):
                d = s - prev; usable += 1
                if d != 0: moved += 1
                if abs(d) > WP * (1 + 1e-9) + 1e-12: over += 1
                maxabs = max(maxabs, abs(d))
            prev = s; prev_collapse = r[ix['oldCollapseAFlag']] == '1' or r[ix['oldCollapseBFlag']] == '1'
        ep = endpath(ix, rs[-1])
        if int(rs[-1][ix['month']]) >= lastmonth and ep == 'other': ep = 'running'
        ends[ep] += 1
        if ep == 'decisive' and len(rs) < 10: dec_short += 1
    return dict(usable=usable, over=over, moved=moved, firstOver=first_over, maxAbsD=maxabs, decShort=dec_short, **{'end_' + k: v for k, v in ends.items()})

def wars_seed(p):
    ix, rows = load(p)
    v = collections.Counter(r[ix['verdictCode']] for r in rows)
    months = [int(r[ix['months']]) for r in rows]
    run = [int(r[ix['months']]) for r in rows if r[ix['verdictCode']] == '-1']
    return dict(wars=len(rows), blank=v.get('0', 0), running=len(run), longest=max(months) if months else 0,
                longestRunning=max(run) if run else 0, meanMonths=(sum(months) / len(months)) if months else 0.0)

if __name__ == '__main__':
    d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2].isdigit() else 10
    wars_only = '--wars-only' in sys.argv
    tot = collections.Counter(); per = []
    for s in range(int(__import__('os').environ.get('SB','1')), int(__import__('os').environ.get('SB','1')) + ns):
        w = wars_seed(f'{d}/wars_s{s}.tsv')
        x = {} if wars_only else wexit_seed(f'{d}/wexit_s{s}.tsv', 2003)
        row = dict(seed=s, **w, **x); per.append(row)
        for k, v in row.items():
            if k in ('seed', 'longest', 'longestRunning', 'meanMonths', 'maxAbsD'): continue
            tot[k] += v
    keys = [k for k in per[0] if k != 'seed']
    for r in per: print('seed', r['seed'], ' '.join(f'{k}={r.get(k, 0):.4g}' if isinstance(r.get(k, 0), float) else f'{k}={r.get(k, 0)}' for k in keys))
    print('TOTAL', ' '.join(f'{k}={v}' for k, v in sorted(tot.items())))
    print('MAX longest=%d longestRunning=%d runningPerSeedMax=%d' % (max(r['longest'] for r in per), max(r['longestRunning'] for r in per), max(r['running'] for r in per)),
          '' if wars_only else 'maxAbsD=%.6g' % max(r['maxAbsD'] for r in per))
    print('MEAN meanMonths=%.3f' % (sum(r['meanMonths'] * r['wars'] for r in per) / max(1, sum(r['wars'] for r in per))))
    if not wars_only: print('GATE G1 over=%d (accept if 0) firstOver=%d (record)' % (tot['over'], tot['firstOver']))
