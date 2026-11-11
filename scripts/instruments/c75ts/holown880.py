# (880) c74id 持ち主の分かれた州の空洞化の読み方（読むだけ・事前登録 measurements/m880_20261106_prereg_id.txt [2]）。holclass839.py は書き換えない（A2 をここに写す）。
#   python -X utf8 -I holown880.py <腕の an_h2 の出力> <腕の ins> <出力> --base <基線の an_h2 の出力> <基線の ins> [--base ...] [--level 1/10|1/4] [--mu exempt-split|no-owner]
#   規則（OR 形）：R880 の〈線が原因〉＝A2 の〈線が原因〉∪〈A2 は元からの弱さだが、腕の qp〜qe の全ての四半期で基線の同じ州の持ち主（nation の名前）が腕と同じ基線が 1 本も無い〉。
#     A2＝holclass839.py と同じ（基線の出力に同じ種・同じ州の事象の行があれば元からの弱さ・無ければ線が原因・例外の名簿の行は件数の外）。
#     qp・qe は腕の region_s から an_h2.py と同じ走査（populationK・有限の行・ピークは 1 千人以上）で数え直す。an_h2 の行の年と食い違えば〈読めない〉＝線が原因。
#   --level 1/4：1/4 の事象を同じ規則で振り分ける（自己試験の口・実の判定は 1/10）。
#   --mu：突然変異の口（exempt-split＝腕で持ち主が替わった州は免除する／no-owner＝持ち主の照合を外す＝A2 と同じ）。
#   VERDICT：pass＝R880 の〈線が原因〉が 0 件。
import sys, re, os, math, collections

pat = re.compile(r'^\s*\[([^\]・]+)・(1/10|1/4)(・例外の名簿)?\]\s+s(\d+)\s+(\S+?)（')   # holclass839.py と同じ
pk = re.compile(r'\s([0-9.e+]+)\((\d{4})\)→([0-9.e+]+)\((\d{4})\)')
KMIN = 1.0   # an_h2.py の既定（-min を渡さない stoplines866.sh と同じ）


def cases(path):
    out = []
    for line in open(path, encoding='utf-8', errors='replace'):
        m = pat.match(line)
        if not m: continue
        k = pk.search(line)
        out.append(dict(level=m.group(2), exempt=m.group(3) is not None, seed=int(m.group(4)), region=m.group(5), cause=m.group(1),
                        pky=int(k.group(2)) if k else None, evy=int(k.group(4)) if k else None, text=line.strip()[:200]))
    return out


def fl(s):
    try: return float(s)
    except Exception: return float('nan')


_cache = {}
def series(ins, seed, region):
    """region_s<seed>.tsv の州の行を四半期の順に [(q, 年, 人口, nation の名前)]。無ければ None。1 つの種のファイルは一度だけ読む。"""
    key = (ins, seed)
    if key not in _cache:
        p = os.path.join(ins, 'region_s%d.tsv' % seed)
        if not os.path.exists(p): _cache[key] = None
        else:
            D = collections.defaultdict(list)
            with open(p, encoding='utf-8') as f:
                h = f.readline().rstrip('\r\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
                iq, iy, ir, ipop, inat = ix['quarter'], ix['year'], ix['region'], ix['populationK'], ix['nation']
                for line in f:
                    c = line.rstrip('\r\n').split('\t')
                    D[c[ir]].append((int(c[iq]), int(c[iy]), fl(c[ipop]), c[inat]))
            _cache[key] = D
    D = _cache[key]
    if D is None or region not in D: return None
    return D[region]


def event_window(rows, div):
    """an_h2.py と同じ走査：ピークは有限の行の最大（1 千人以上から事象を数える）・最初に v < pk/div になった行。戻り (qp, 年p, qe, 年e) か None。"""
    pkv = 0.0; pki = 0
    for i, (q, y, v, _) in enumerate(rows):
        if not math.isfinite(v): continue
        if v > pkv: pkv = v; pki = i
        if pkv < KMIN: continue
        if v < pkv / div: return rows[pki][0], rows[pki][1], q, y
    return None


def owners_agree(arm_rows, base_rows, qp, qe):
    B = {q: n for q, _, _, n in base_rows} if base_rows else {}
    for q, _, _, n in arm_rows:
        if qp <= q <= qe:
            if q not in B or B[q] != n: return False
    return True


def classify(arm_an, arm_ins, bases, level='1/10', mu=None):
    div = 10 if level == '1/10' else 4
    bsets = []
    for an, ins in bases:
        bsets.append(({(c['seed'], c['region']) for c in cases(an)}, ins))
    base_any = set().union(*[s for s, _ in bsets]) if bsets else set()
    res = []; C = collections.Counter()
    for c in cases(arm_an):
        if c['level'] != level: continue
        C['行'] += 1
        key = (c['seed'], c['region'])
        if c['exempt']:
            res.append(('L0', '例外の名簿（件数の外）', '例外', c)); C['例外'] += 1; continue
        a2 = '元からの弱さ' if key in base_any else '線が原因'
        C['A2 ' + a2] += 1
        if a2 == '線が原因':
            lab, why = '線が原因', 'A2（基線の同じ種・同じ州に事象の行が無い）'
        else:
            ar = series(arm_ins, c['seed'], c['region'])
            win = event_window(ar, div) if ar else None
            if win is None or c['pky'] is None or (win[1], win[3]) != (c['pky'], c['evy']):
                lab, why = '線が原因', '読めない（腕の region_s の走査と an_h2 の行の年が食い違う）'; C['読めない'] += 1
            else:
                qp, _, qe, _ = win
                split = len({n for q, _, _, n in ar if q <= qe}) > 1
                ok = False
                for s, ins in bsets:
                    if key not in s: continue
                    if mu == 'no-owner' or owners_agree(ar, series(ins, c['seed'], c['region']), qp, qe): ok = True; break
                if mu == 'exempt-split' and split: ok = True
                if ok: lab, why = '元からの弱さ', 'A2 の免除が立つ（qp〜qe の持ち主が合う基線がある）'
                else: lab, why = '線が原因', '持ち主の軌道が基線と違う（A2 は元からの弱さ・基線の弱さは反実仮想にならない）'; C['照合で無効'] += 1
        if mu == 'exempt-split' and lab == '線が原因':   # 突然変異：腕で持ち主が替わった州は免除する（A2 の線が原因も）
            ar = series(arm_ins, c['seed'], c['region'])
            win = event_window(ar, div) if ar else None
            if win is not None and len({n for q, _, _, n in ar if q <= win[2]}) > 1: lab, why = '元からの弱さ', '突然変異 exempt-split（分かれた州を免除）'
        C[lab] += 1
        res.append((lab, why, a2, c))
    return res, C


def main():
    a = sys.argv[1:]
    level = '1/10'; mu = None
    if '--level' in a:
        i = a.index('--level'); level = a[i + 1]; a = a[:i] + a[i + 2:]
    if '--mu' in a:
        i = a.index('--mu'); mu = a[i + 1]; a = a[:i] + a[i + 2:]
    arm_an, arm_ins, out = a[0], a[1], a[2]; bases = []
    i = 3
    while i < len(a):
        if a[i] == '--base': bases.append((a[i + 1], a[i + 2])); i += 3
        else: raise SystemExit('引数が分からない：' + a[i])
    if not bases: raise SystemExit('--base <基線の an_h2 の出力> <基線の ins> が 1 本以上要る')
    res, C = classify(arm_an, arm_ins, bases, level, mu)
    L = ['%s\t%s\tA2=%s\t%s' % (lab, why, a2, c['text']) for lab, why, a2, c in res]
    L.append('(880) holown880（水準 %s・突然変異 %s・基線 %d 本）：行 %d＝例外 %d・A2 の線が原因 %d・A2 の元からの弱さ %d・R880 の線が原因 %d（うち照合で無効 %d・読めない %d）・R880 の元からの弱さ %d'
             % (level, mu or 'なし', len(bases), C['行'], C['例外'], C['A2 線が原因'], C['A2 元からの弱さ'], C['線が原因'], C['照合で無効'], C['読めない'], C['元からの弱さ']))
    L.append('VERDICT holown %s 線が原因 %d 件（A2 %d 件・持ち主の照合で足した %d 件）' % ('pass' if C['線が原因'] == 0 else 'fail', C['線が原因'], C['A2 線が原因'], C['線が原因'] - C['A2 線が原因']))
    with open(out, 'w', encoding='utf-8', newline='\n') as f: f.write('\n'.join(L) + '\n')
    print('\n'.join(L))
    return 0


if __name__ == '__main__':
    sys.exit(main())
