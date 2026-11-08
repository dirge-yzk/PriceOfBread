# (970) c87bx 読むだけの台本：計器 basisx_s（土地税の算定の基準 A1 の段 A-2b-0）の生から、AI の規則 A・B・C1・C2・S の
# 切り替えの回数・向き・危機の述語・no-go（窓つき）・往復の過多・H4 の G÷B と moved の分布を国×種で数える。
# 使い方：python bx_read.py <計器の出力フォルダ（basisx_s*.tsv のある所）> <書き出すファイル> [腕の名前]
# 事前登録 measurements/m970_20261108_prereg_bx.txt [6] の窓と過多の定義を数える前に固定したとおりに書いた（結果を見た後に替えない）。
#   窓＝錨の年の前後 5 年 かつ 開幕の 20 四半期の後（q>=20）。no-go＝窓の中の錨の向きの切り替えが 10 種とも 0 回。過多＝窓の中の切り替えが窓の四半期の数の半分以上。
# 出力の書き先は引数（読む線は作業フォルダに置く）。生は読むだけ。
import sys, os, glob, statistics

ANCHOR = {'フランス': ('P->F', 1790), 'プロイセン': ('P->F', 1861), '日本': ('P->F', 1873), '清': ('F->P', 1941)}
RULES = ['A', 'B', 'C1', 'C2', 'S']
WIN_YEARS = 5
OPEN_Q = 20

def anchor_counts(seq, years, qs, direction, anchor_year):
    """seq＝目標（0/1）の系列。窓の中の錨の向きの切り替えの回数と窓の四半期の数を返す。"""
    n = 0; na = 0; wq = 0
    for i in range(len(seq)):
        if qs[i] < OPEN_Q or abs(years[i] - anchor_year) > WIN_YEARS:
            continue
        wq += 1
        if i == 0:
            continue
        a, b = seq[i - 1], seq[i]
        if (a >= 0.5) != (b >= 0.5):
            na += 1
        if direction == 'P->F' and a < 0.5 <= b:
            n += 1
        if direction == 'F->P' and a >= 0.5 > b:
            n += 1
    return n, na, wq

def verdict(counts):
    """counts＝種ごとの (窓の中の錨の向きの切り替え, 窓の中の全ての切り替え, 窓の四半期の数)。no-go／過多／通る／窓なし。"""
    if not counts or all(wq == 0 for _, _, wq in counts):
        return '窓なし'
    if all(n == 0 for n, _, _ in counts):
        return 'no-go'
    if any(wq > 0 and na * 2 >= wq for _, na, wq in counts):
        return '過多'
    return '通る'

def selftest():
    qs = list(range(60)); years = [1783 + q // 4 for q in qs]
    flip = [float(q % 2) for q in qs]            # 毎四半期に反転する偽の規則
    none = [0.0] * 60                            # 何もしない偽の規則
    one = [0.0] * 30 + [1.0] * 30                # 1 回だけ切り替える規則（1783+30/4＝1790 の少し後）
    ay = 1790
    vf = verdict([anchor_counts(flip, years, qs, 'P->F', ay)])
    vn = verdict([anchor_counts(none, years, qs, 'P->F', ay)])
    vo = verdict([anchor_counts(one, years, qs, 'P->F', ay)])
    ok = (vf == '過多' and vn == 'no-go' and vo == '通る')
    return ok, vf, vn, vo

def load(path):
    with open(path, 'rb') as f:
        data = f.read().decode('utf-8').split('\n')
    head = data[0].rstrip('\r').split('\t')
    rows = []
    for ln in data[1:]:
        if not ln.strip():
            continue
        c = ln.rstrip('\r').split('\t')
        rows.append(dict(zip(head, c)))
    return rows

def num(s):
    try:
        return float(s)
    except (ValueError, TypeError):
        return float('nan')

def pct(xs, p):
    xs = sorted(x for x in xs if x == x)
    if not xs:
        return float('nan')
    k = min(len(xs) - 1, max(0, int(round(p * (len(xs) - 1)))))
    return xs[k]

def main():
    ins, out = sys.argv[1], sys.argv[2]
    arm = sys.argv[3] if len(sys.argv) > 3 else os.path.basename(os.path.normpath(ins))
    L = []
    W = L.append
    ok, vf, vn, vo = selftest()
    W('# bx_read：basisx_s から AI の規則 A・B・C1・C2・S の切り替え・no-go（窓つき）・往復の過多を数える（腕 %s・%s）' % (arm, ins))
    W('# 自己検査（偽の規則）：毎四半期に反転＝%s（過多が出る）・何もしない＝%s（no-go が出る）・1 回だけ＝%s（通る）→ %s' % (vf, vn, vo, '合格' if ok else '不合格（読み手を直す）'))
    W('# 窓＝錨の年の前後 %d 年 かつ q>=%d。no-go＝窓の中の錨の向きの切り替えが全ての種で 0。過多＝窓の中の切り替えが窓の四半期の数の半分以上の種がある。' % (WIN_YEARS, OPEN_Q))
    W('# 下限の no-go は〈毎四半期に反転する規則〉を通す（窓の中で 1 回以上切り替わる）＝下限の門だけでは足りない。')
    if not ok:
        with open(out, 'w', encoding='utf-8', newline='\n') as f:
            f.write('\n'.join(L) + '\n')
        sys.exit(2)
    files = sorted(glob.glob(os.path.join(ins, 'basisx_s*.tsv')))
    W('# 種のファイル %d 本' % len(files))
    per = {}     # (nation, rule) -> [種ごとの (n, wq)]
    tot = {}     # (nation, rule) -> [種ごとの最終の切り替えの回数]
    cr = {}      # (nation, 'c1'/'c2') -> [種ごとの危機の四半期の数]
    stn = {}     # nation -> [種ごとの姿勢が有効な四半期の数]
    gb = {}      # nation -> G÷B の値の列（定額へ・比例へ）
    mv = {}      # nation -> moved の値の列
    nan_rows = 0; rows_n = 0
    for p in files:
        rows = load(p)
        byn = {}
        for r in rows:
            byn.setdefault(r['nation'], []).append(r)
        for nat, rs in byn.items():
            rs.sort(key=lambda r: int(r['quarter']))
            qs = [int(r['quarter']) for r in rs]; years = [int(r['year']) for r in rs]
            for k in RULES:
                seq = [num(r['bxTarget' + k]) for r in rs]
                tot.setdefault((nat, k), []).append(int(num(rs[-1]['bxSwitch' + k])))
                if nat in ANCHOR:
                    d, ay = ANCHOR[nat]
                    per.setdefault((nat, k), []).append(anchor_counts(seq, years, qs, d, ay))
            cr.setdefault((nat, 'c1'), []).append(sum(1 for r in rs if num(r['bxCrisis1']) == 1))
            cr.setdefault((nat, 'c2'), []).append(sum(1 for r in rs if num(r['bxCrisis2']) == 1))
            stn.setdefault(nat, []).append(sum(1 for r in rs if num(r['bxStance']) >= 0))
            for r in rs:
                rows_n += 1
                if num(r['bxNanFlag']) == 1:
                    nan_rows += 1
                B = num(r['bxH4BasePbPerMonth'])
                if B > 0:
                    for c in ('bxH4GToFixed', 'bxH4GToProp'):
                        v = num(r[c])
                        if v == v:
                            gb.setdefault((nat, c), []).append(v / B)
                for c in ('bxMovedToFixed', 'bxMovedToProp'):
                    v = num(r[c])
                    if v == v:
                        mv.setdefault((nat, c), []).append(v)
    W('# 行 %d・bxNanFlag＝1 の行 %d' % (rows_n, nan_rows))
    nations = sorted({n for n, _ in tot})
    W('')
    W('## 錨の国の no-go の読み（窓つき・規則ごと）')
    W('規則\t国\t錨\t窓の中の錨の向きの切り替え（種ごと）\t窓の四半期の数（種の中央）\t読み')
    verdicts = {}
    for k in RULES:
        for nat, (d, ay) in ANCHOR.items():
            cs = per.get((nat, k), [])
            v = verdict(cs)
            verdicts[(k, nat)] = v
            W('%s\t%s\t%s %d\t%s\t%s\t%s' % (k, nat, d, ay, ','.join(str(n) for n, _, _ in cs), statistics.median([wq for _, _, wq in cs]) if cs else 'nan', v))
    W('')
    W('## 規則ごとの総括（錨の国のどれかが no-go か過多なら使えない側）')
    for k in RULES:
        vs = {nat: verdicts[(k, nat)] for nat in ANCHOR}
        bad = [n for n, v in vs.items() if v in ('no-go', '過多')]
        W('%s：%s%s' % (k, ' '.join('%s=%s' % (n, v) for n, v in vs.items()), '　→ 使えない（%s）' % ','.join(bad) if bad else '　→ 錨の国の全てで通る'))
    allbad = all(any(verdicts[(k, n)] in ('no-go', '過多') for n in ANCHOR) for k in RULES)
    W('全ての候補が no-go か過多か：%s%s' % ('はい' if allbad else 'いいえ', '＝作者への問いの材料として書く（決めない）' if allbad else ''))
    W('')
    W('## 切り替えの回数（国×規則・種ごとの最終の bxSwitch の最小／中央／最大）')
    W('国\t' + '\t'.join(RULES))
    for nat in nations:
        W(nat + '\t' + '\t'.join('%d/%s/%d' % (min(tot[(nat, k)]), ('%g' % statistics.median(tot[(nat, k)])), max(tot[(nat, k)])) for k in RULES))
    W('')
    W('## 危機の四半期の数（国・種の中央／最大）と姿勢が有効な四半期の数（種の中央／最大）')
    W('国\tC1 の述語\tC2 の述語\t姿勢の有効な四半期')
    for nat in nations:
        W('%s\t%g/%d\t%g/%d\t%g/%d' % (nat, statistics.median(cr[(nat, 'c1')]), max(cr[(nat, 'c1')]), statistics.median(cr[(nat, 'c2')]), max(cr[(nat, 'c2')]), statistics.median(stn[nat]), max(stn[nat])))
    W('')
    W('## H4 の G÷B と moved の分布（記録・門にしない）。moved÷(LandTaxTop÷3) の LandTaxTop＝0.4（Government.cs:35）')
    W('国\t列\t中央\tp95\t最大')
    for nat in nations:
        for c in ('bxH4GToFixed', 'bxH4GToProp'):
            xs = gb.get((nat, c), [])
            if xs:
                W('%s\t%s÷B\t%.4g\t%.4g\t%.4g' % (nat, c, statistics.median(xs), pct(xs, 0.95), max(xs)))
        for c in ('bxMovedToFixed', 'bxMovedToProp'):
            xs = mv.get((nat, c), [])
            if xs:
                W('%s\t%s÷(0.4/3)\t%.4g\t%.4g\t%.4g' % (nat, c, statistics.median(xs) / (0.4 / 3), pct(xs, 0.95) / (0.4 / 3), max(xs) / (0.4 / 3)))
    with open(out, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(L) + '\n')

if __name__ == '__main__':
    main()
