"""(292) 線P：港の資本家の支出を「要る分だけ」に＝腕 A（旧式・貯蓄×率）と腕 B（要る分）の読み手（読むだけ）。
  python an_p292.py <腕 A の ins> <腕 B の ins> <表の出力ディレクトリ>
  入力＝`INS=1 POB_INSTRUMENTS_NATIONS=イギリス,フランス,ネーデルラント,ポルトガル bash scripts/instruments/batch.sh run <腕> 10` の <出力>/ins の
        port_s<seed>.tsv（挙げた国×四半期×活きた港の州・(292) の新しい表）と nation_s<seed>.tsv（earnedTotalPerMonth）。
  窓（四半期の番号 q は 0 始まり・表の Q は q+1）：Q5-40＝q4..39・Q41-160＝q40..159（measurements/p292_20261013_prereg.txt の窓・an_d289 と同じ）。
  量は種ごとに窓で足してから10種の中央値（括弧は最小〜最大）。分母＝国の所得（全階級の Earned の和＝nation_s の earnedTotalPerMonth×3 を四半期ごとに足す）。
  P1 資本家ルートの払った額÷国の所得（英）・B<A の種の数。P2 混雑した（律速率＜0.9）港の州×四半期の割合・取扱高で重み付けた律速率。
  記録：要る分÷所得・国家ルートの払い÷所得・貯蓄が要る分に足りなかった割合・資本家と国家が同じ港の州で同じ四半期に両方払った割合・港の容量の伸び。
  P3（粗投資の総額）は an_d289.py の (a) の表（`python scripts/instruments/d289/an_d289.py <ins> <出力> a`）を腕ごとに回して読む＝この読み手は 港の資本家ルートの列だけ突き合わせる（port_s の払った額と an_d289 の flow_s 経由の値が同じ桁になること）。
  計器の限界の自己申告：①port_s は挙げた国だけ②律速率・潜在量・取扱高は四半期の終わりの断面（StepPorts が見た値と同じ）③混雑の割合の分母は活きた港の州の全ての行（取扱高 0 の州も入る）④A の要る分は反事実。"""
import sys, os, statistics, collections
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..'))
from instr import num as fl, seeds  # noqa: E402

DA, DB, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(OUT, exist_ok=True)
WIN = {'Q5-40': (4, 39), 'Q41-160': (40, 159)}
NATIONS = ['イギリス', 'フランス', 'ネーデルラント', 'ポルトガル']
TARGET = 0.9   # PortCongestionTarget（既定）


def table(path, cols=None):
    with open(path, encoding='utf-8') as fh:
        head = fh.readline().rstrip('\n').split('\t')
        idx = [(h, i) for i, h in enumerate(head) if cols is None or h in cols]
        for ln in fh:
            f = ln.rstrip('\n').split('\t')
            yield {h: f[i] for h, i in idx}


def med(xs):
    xs = [x for x in xs if x == x]
    return statistics.median(xs) if xs else float('nan')


def cell(xs, pct=True, nd=2):
    xs = [x for x in xs if x == x]
    if not xs:
        return '—'
    k = 100.0 if pct else 1.0; s = '%' if pct else ''
    return f'{med(xs)*k:.{nd}f}{s} ({min(xs)*k:.{nd}f}〜{max(xs)*k:.{nd}f})'


def load(D):
    """→ {seed: {nation: {'income': {q: 所得(四半期)}, 'rows': [port_s の行（数に直した）]}}}"""
    out = {}
    for sd in seeds(D, 'port'):
        inc = collections.defaultdict(dict)
        for r in table(os.path.join(D, f'nation_s{sd}.tsv'), {'quarter', 'nation', 'earnedTotalPerMonth'}):
            if r['nation'] in NATIONS:
                inc[r['nation']][int(r['quarter'])] = fl(r['earnedTotalPerMonth']) * 3.0   # 四半期の所得（月あたり×3か月）
        rows = collections.defaultdict(list)
        for r in table(os.path.join(D, f'port_s{sd}.tsv')):
            q = int(r['quarter'])
            rows[r['nation']].append({'q': q, 'region': r['region'], 'thr': fl(r['tradeThrottleIndex']), 'pot': fl(r['tradePotentialWeight']), 'tp': fl(r['portThroughput']),
                                      'cap': fl(r['portCapacityEnd']), 'sav': fl(r['investorSavingsStock']), 'gate': r['congestedGate'] == '1', 'need': fl(r['needMoneyAtQuarterEnd']),
                                      'int': fl(r['capitalIntendedMoneyAtQuarterEnd']), 'paid': fl(r['capitalPaidMoneyAtQuarterEnd']), 'sint': fl(r['stateIntendedMoneyAtQuarterEnd']),
                                      'spaid': fl(r['statePaidMoneyAtQuarterEnd']), 'built': fl(r['builtCapacity'])})
        out[sd] = {n: {'income': inc[n], 'rows': rows[n]} for n in NATIONS}
    return out


def metrics(sd_nat, w):
    a, b = w
    inc = sum(v for q, v in sd_nat['income'].items() if a <= q <= b)
    rows = [r for r in sd_nat['rows'] if a <= r['q'] <= b]
    if not rows or not (inc > 0):
        return None
    m = {}
    m['paid'] = sum(r['paid'] for r in rows) / inc
    m['need'] = sum(r['need'] for r in rows if r['gate']) / inc
    m['spaid'] = sum(r['spaid'] for r in rows) / inc
    m['cong'] = sum(1 for r in rows if r['thr'] < TARGET) / len(rows)
    tp = sum(r['tp'] for r in rows if r['tp'] == r['tp'] and r['tp'] > 0)
    m['wthr'] = sum(r['thr'] * r['tp'] for r in rows if r['tp'] == r['tp'] and r['tp'] > 0) / tp if tp > 0 else float('nan')
    gates = [r for r in rows if r['gate']]
    m['gates'] = len(gates)
    m['savbound'] = (sum(1 for r in gates if r['sav'] < r['need'] * (1 - 1e-12)) / len(gates)) if gates else float('nan')
    paidrows = [r for r in rows if r['paid'] > 0]
    m['both'] = (sum(1 for r in paidrows if r['spaid'] > 0) / len(paidrows)) if paidrows else float('nan')
    m['built'] = sum(r['built'] for r in rows)
    return m


def main():
    A, B = load(DA), load(DB)
    common = sorted(set(A) & set(B))
    md = ['# (292) 線P 港の資本家の支出：A（旧式）と B（要る分）', '',
          f'種：A {sorted(A)}・B {sorted(B)}（突き合わせる種＝{common}）。量は種ごとに窓で足してから中央値（最小〜最大）。分母＝国の所得（Earned の和）。', '']
    verdict = []
    for nat in NATIONS:
        md += [f'## {nat}', '', '| 量 | 窓 | A | B | B<A の種数 |', '|---|---|---|---|---|']
        for wl, w in WIN.items():
            ma = {sd: metrics(A[sd][nat], w) for sd in A}
            mb = {sd: metrics(B[sd][nat], w) for sd in B}
            def col(M, k):
                return [M[sd][k] for sd in M if M[sd] is not None and M[sd][k] == M[sd][k]]
            def lt(k):
                n = sum(1 for sd in common if ma[sd] and mb[sd] and ma[sd][k] == ma[sd][k] and mb[sd][k] == mb[sd][k] and mb[sd][k] < ma[sd][k])
                return f'{n}/{len(common)}'
            for k, label, pct, nd in [('paid', 'P1 資本家ルートの払い÷所得', True, 2), ('need', '要る分÷所得（門を通った州）', True, 2), ('spaid', '国家ルートの払い÷所得', True, 2),
                                      ('cong', 'P2 混雑した（律速率＜0.9）港の州×四半期の割合', True, 1), ('wthr', 'P2 取扱高で重み付けた律速率', False, 3),
                                      ('savbound', '貯蓄が要る分に足りなかった割合（門を通った州）', True, 1), ('both', '資本家と国家が同じ州で両方払った割合', True, 1),
                                      ('gates', '門を通った港の州×四半期の数', False, 0), ('built', '港の容量の伸びの合計', False, 1)]:
                md.append(f'| {label} | {wl} | {cell(col(ma, k), pct, nd)} | {cell(col(mb, k), pct, nd)} | {lt(k)} |')
            if nat == 'イギリス':
                pa, pb = med(col(ma, 'paid')), med(col(mb, 'paid'))
                nlt = sum(1 for sd in common if ma[sd] and mb[sd] and mb[sd]['paid'] < ma[sd]['paid'])
                ca, cb = med(col(ma, 'cong')), med(col(mb, 'cong'))
                ta, tb = med(col(ma, 'wthr')), med(col(mb, 'wthr'))
                verdict.append((wl, pa, pb, nlt, len(common), ca, cb, ta, tb))
        md.append('')
    md += ['## 事前登録の線（英・measurements/p292_20261013_prereg.txt の P1・P2。P3 は an_d289 (a) の表で読む）', '',
           '| 窓 | P1 B 中央値<A 中央値 | P1 B<A の種数≧8 | P2 混雑の割合 B≦A+10pt | P2 重み付き律速率 B≧A−0.05 |', '|---|---|---|---|---|']
    for wl, pa, pb, nlt, n, ca, cb, ta, tb in verdict:
        md.append(f'| {wl} | {"合格" if pb < pa else "不合格"}（A {pa*100:.2f}% → B {pb*100:.2f}%） | {"合格" if nlt >= 8 else "不合格"}（{nlt}/{n}） | '
                  f'{"合格" if cb <= ca + 0.10 else "不合格"}（A {ca*100:.1f}% → B {cb*100:.1f}%） | {"合格" if tb >= ta - 0.05 else "不合格"}（A {ta:.3f} → B {tb:.3f}）|')
    open(os.path.join(OUT, 't_p292.md'), 'w', encoding='utf-8', newline='\n').write('\n'.join(md) + '\n')
    print('\n'.join(md))


main()
