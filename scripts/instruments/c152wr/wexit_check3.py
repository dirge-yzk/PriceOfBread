# (1414) c151wr 項 1：wexit_s の oldDecisiveFlag が、旧の崩壊の書き換えの後の戦勝点で数え直されたことと、newEndsFlag の値の検算。
# 呼び方：python wexit_check3.py <基準の表(m1418)> <腕の表> [<旧の生の wars_s＝省けば基準の表の隣の wars_s>]。0＝宣言どおり。
# (1421) 3 つ目の引数を渡すと (e) を足す：終戦の記録（wars_s の verdictCode＝EndedWars の Verdict の写し）との値の検算。
# 検算：(a) newEndsFlag が列 scoreIndex・kA・kB だけから再計算した値（s>=kB または -s>=kA）と一致する（表の桁の丸めの境は数える）。
#       (b) 崩壊の旗が無い行は oldDecisiveFlag が |scoreIndex|>=1 と一致する。
#       (c) 基準との差は oldDecisiveFlag の列だけで、崩壊の旗が立つ行だけ。他の列・行の数・見出しは同じ。
#       (d) 崩壊の旗が立つ行の旧の規則との食い違い（|scoreIndex|>=1 と oldDecisiveFlag が違う行）を数えて書く（記録）。
import sys, collections
def rows(p):
    with open(p, encoding='utf-8') as f: return [l.rstrip('\n').split('\t') for l in f]
a, b = rows(sys.argv[1]), rows(sys.argv[2])
if len(a) != len(b): print(f'行の数が違う {len(a)} {len(b)}'); sys.exit(1)
if b[0][:len(a[0])] != a[0] or b[0][len(a[0]):] != ['powerIdA', 'powerIdB']: print('見出しが違う（基準＋powerIdA・powerIdB のはず）'); sys.exit(1)
b = [r[:len(a[0])] for r in b]   # 項 7 の足した 2 列は基準に無い＝比べる列は基準の列だけ
h = a[0]; ix = {n: h.index(n) for n in h}
TOL = 1e-9   # 結果の前に固定（計器の印字の桁＝有効 10 桁より広い）
bad = 0; edge = 0; chg = 0; cdiff = 0
for x, y in zip(a[1:], b[1:]):
    s, kA, kB = float(y[ix['scoreIndex']]), float(y[ix['kA']]), float(y[ix['kB']])
    ends = 1 if (s >= kB or -s >= kA) else 0
    if int(y[ix['newEndsFlag']]) != ends:
        # 表は有効桁 10 桁で丸めて書く（0.99999999999 は 1 と印字される）＝境の TOL（1e-9・印字の桁の 1e-10 より広い）以内は丸めの差として数える
        if min(abs(s - kB), abs(-s - kA)) < TOL: edge += 1
        else: bad += 1; print('newEndsFlag が再計算と違う', y[:4])
    col = x[ix['oldCollapseAFlag']] == '1' or x[ix['oldCollapseBFlag']] == '1'
    if not col and int(y[ix['oldDecisiveFlag']]) != (1 if abs(s) >= 1.0 else 0):
        if abs(abs(s) - 1.0) < TOL: edge += 1
        else: bad += 1; print('崩壊なしの行の oldDecisiveFlag が違う', y[:4])
    if col and int(y[ix['oldDecisiveFlag']]) != (1 if abs(s) >= 1.0 else 0): cdiff += 1
    if x != y:
        chg += 1
        if not col: bad += 1; print('崩壊の旗が無い行が違う', x[:4])
        d = [h[i] for i in range(len(x)) if x[i] != y[i] and h[i] != 'oldDecisiveFlag']
        if d: bad += 1; print('宣言の外の列が違う', d)
print(f'wexit_s 検算2 {"OK" if bad==0 else "NG"} 違う行 {chg}（oldDecisiveFlag だけ）・崩壊の行で scoreIndex の旧規則と食い違う行 {cdiff}・newEnds の丸めの境 {edge}・不一致 {bad}')
# (e) (1421) 値の検算：戦争ごとの最後の行（旧の崩壊 1 側だけ・強制の空白でない・oldDecisiveFlag=1）は、wars_s の verdictCode（0 白紙・1 A の勝ち・2 B の勝ち）が 崩壊の側の反対の勝ちである。
import os
wp = sys.argv[3] if len(sys.argv) > 3 else os.path.join(os.path.dirname(sys.argv[1]), os.path.basename(sys.argv[1]).replace('wexit_s', 'wars_s'))   # ident からは 2 引数で呼ばれる＝基準の表の隣の wars_s を使う（wars_s の中身は ident が基準と全行比べる）
if os.path.isfile(wp):
    w = rows(wp); wi = {n: w[0].index(n) for n in w[0]}
    ended = collections.defaultdict(list)
    for x in w[1:]:
        if x[wi['verdictCode']] != '-1': ended[(x[wi['sideA']], x[wi['sideB']], int(x[wi['months']]))].append(int(x[wi['verdictCode']]))
    groups = []; cur = {}
    for y in b[1:]:
        key = (y[ix['sideA']], y[ix['sideB']], y[ix['civilFlag']]); m = int(y[ix['month']]); g = cur.get(key)
        if g and m == g['last'] + 1: g['rows'].append(y); g['last'] = m
        else: g = {'rows': [y], 'last': m, 'key': key}; groups.append(g); cur[key] = g
    checked = 0; unmatched = 0; vbad = 0; nonfinal = 0
    for g in groups:
        for y in g['rows'][:-1]:
            if y[ix['oldDecisiveFlag']] == '1' and (y[ix['oldCollapseAFlag']] == '1') != (y[ix['oldCollapseBFlag']] == '1') and y[ix['forcedFlag']] == '0': nonfinal += 1
        y = g['rows'][-1]; ca, cb = y[ix['oldCollapseAFlag']] == '1', y[ix['oldCollapseBFlag']] == '1'
        if ca == cb or y[ix['forcedFlag']] != '0' or y[ix['oldDecisiveFlag']] != '1': continue
        vs = ended.get((g['key'][0], g['key'][1], len(g['rows'])))
        if not vs: unmatched += 1; continue   # 終わっていない（走りの終わり）か記録の外（256 件のリング）
        checked += 1
        if (2 if ca else 1) not in vs: vbad += 1; print('終戦の記録の勝ちと oldDecisiveFlag の崩壊の側が違う', g['key'], len(g['rows']), vs)
    print(f'wexit_s 値の検算 (e) {"OK" if vbad==0 else "NG"} 照合 {checked}・記録なし {unmatched}・最後の行でない決定的の崩壊 {nonfinal}・食い違い {vbad}')
    if vbad: bad += vbad
sys.exit(1 if bad else 0)
