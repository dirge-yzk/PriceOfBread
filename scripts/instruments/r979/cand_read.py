# (979) c88rb 読むだけの台本：(i) 錨の窓の門の力（置き換えた年の窓で規則 A・B が通る割合＝★(879) の恒真の確かめ）・
# (ii) 規則の形の案 α（期待インフレに比例する目標・速さ 1 の上限）と γ（凍結の目減りの比に比例する戻し）を、同じ basisx_s の列で紙で数える。
# 使い方：python -X utf8 -I cand_read.py <書き出すファイル> <腕の名前>=<basisx_s*.tsv のあるフォルダ> [...]
# 全て〈事後の診断・在標本〉＝draft.md §0 の判定（no-go・過多・K＝5・中央値）を替えない。新しい係数を置かない（0.5 は bx_read の 0/1 の目標の境と同じ数え方の境）。
import sys, os, glob, statistics

ANCHOR = {'フランス': ('P->F', 1790), 'プロイセン': ('P->F', 1861), '日本': ('P->F', 1873), '清': ('F->P', 1941)}
OPEN_FIXED = {'清', 'イギリス'}   # 開幕で定額の国（bx_read_M の H4 の比例への列が有る国＝名簿で Fixed）
WIN_YEARS = 5; OPEN_Q = 20; K = 5

def anchor_counts(seq, years, qs, direction, ay):
    n = 0; na = 0; wq = 0
    for i in range(len(seq)):
        if qs[i] < OPEN_Q or abs(years[i] - ay) > WIN_YEARS: continue
        wq += 1
        if i == 0: continue
        a, b = seq[i - 1], seq[i]
        if (a >= 0.5) != (b >= 0.5): na += 1
        if direction == 'P->F' and a < 0.5 <= b: n += 1
        if direction == 'F->P' and a >= 0.5 > b: n += 1
    return n, na, wq

def verdict(cs):
    if not cs or all(wq == 0 for _, _, wq in cs): return '窓なし'
    if all(n == 0 for n, _, _ in cs): return 'no-go'
    if statistics.median([na for _, na, _ in cs]) > K: return '過多'
    return '通る'

def load(path):
    data = open(path, 'rb').read().decode('utf-8').split('\n')
    head = data[0].rstrip('\r').split('\t')
    return [dict(zip(head, ln.rstrip('\r').split('\t'))) for ln in data[1:] if ln.strip()]

def num(s):
    try: return float(s)
    except (ValueError, TypeError): return float('nan')

def seed_of(p):
    b = os.path.basename(p); return int(b[len('basisx_s'):-len('.tsv')])

def series(ins):
    out = {}   # nation -> [ (seed, qs, years, pi, R, tA, tB) ]
    for p in sorted(glob.glob(os.path.join(ins, 'basisx_s*.tsv')), key=seed_of):
        sd = seed_of(p); byn = {}
        for r in load(p): byn.setdefault(r['nation'], []).append(r)
        for nat, rs in byn.items():
            rs.sort(key=lambda r: int(r['quarter']))
            out.setdefault(nat, []).append((sd, [int(r['quarter']) for r in rs], [int(r['year']) for r in rs],
                [num(r['bxExpInflRulePerYear']) for r in rs], [num(r['bxRBase']) for r in rs],
                [num(r['bxTargetA']) for r in rs], [num(r['bxTargetB']) for r in rs]))
    return out

def alpha(pi, start):
    """案 α の上限：φ_{q+1}＝[0,1] の中で φ_q − π_q（1 年先の向きつきの相対の得をそのまま 1 四半期で動く＝速さ 1・期間 1 年＝規則 A と同じ期間）。"""
    phi = start; s = []
    for x in pi:
        if x == x: phi = min(1.0, max(0.0, phi - x))
        s.append(phi)
    return s

def gamma(R, pi, start):
    """案 γ：定額の側の目標＝凍結の額 f÷(R×(1+π))（[0,1] の中）。f は φ が 0.5 を下から越えた四半期の R（開幕で定額の国は最初の行の R）。
    定額へ向かう側は規則 A の P→F（π＜0）を借りる（γ は戻す側だけの案）。"""
    s = []; fixed = start; f = R[0] if start else float('nan'); phi = 1.0 if start else 0.0
    for r, x in zip(R, pi):
        if not (r == r and x == x): s.append(phi); continue
        if not fixed:
            if x < 0: fixed = True; f = r; phi = 1.0
        else:
            phi = min(1.0, max(0.0, f / (r * (1 + x)))) if r * (1 + x) > 0 else 1.0
            if phi < 0.5: fixed = False; phi = 0.0
        s.append(phi)
    return s

def main():
    out = sys.argv[1]; arms = [a.split('=', 1) for a in sys.argv[2:]]
    L = []; W = L.append
    W('# cand_read（(979) c88rb）：事後の診断・在標本（draft.md §0 の判定は替えない）。窓＝前後 %d 年 かつ q>=%d・K＝%d・中央値。' % (WIN_YEARS, OPEN_Q, K))
    D = {name: series(ins) for name, ins in arms}
    W('')
    W('## 1. 門の力：錨の年を置き換えた窓（1795〜1940 の 5 年ごと・錨の年から 10 年以内を除く）で規則 A・B が〈通る〉割合（向きは錨と同じ）')
    W('規則\t国\t腕\t置き換えた窓の数\t通る\tno-go\t過多\t錨の年の判定')
    for k, col in (('A', 5), ('B', 6)):
        for nat, (d, ay) in ANCHOR.items():
            for name, _ in arms:
                ss = D[name].get(nat, [])
                cnt = {'通る': 0, 'no-go': 0, '過多': 0, '窓なし': 0}; nwin = 0
                for y in range(1795, 1941, 5):
                    if abs(y - ay) < 10: continue
                    v = verdict([anchor_counts(s[col], s[2], s[1], d, y) for s in ss]); cnt[v] += 1; nwin += 1
                va = verdict([anchor_counts(s[col], s[2], s[1], d, ay) for s in ss])
                W('%s\t%s\t%s\t%d\t%d\t%d\t%d\t%s' % (k, nat, name, nwin, cnt['通る'], cnt['no-go'], cnt['過多'], va))
    W('')
    W('## 2. 案 α（期待インフレに比例する目標・速さ 1 の上限）：錨の窓の判定・種ごとの n/na・窓の中の φ の最大（種ごと）・全長の切り替え（0.5 をまたぐ回数の中央）')
    W('国\t腕\t判定\t種ごとの n/na\t窓の中の φ の最大\t全長の切り替えの中央')
    for nat, (d, ay) in ANCHOR.items():
        for name, _ in arms:
            ss = D[name].get(nat, []); cs = []; mx = []; tot = []
            for s in ss:
                a = alpha(s[3], 1.0 if nat in OPEN_FIXED else 0.0)
                cs.append(anchor_counts(a, s[2], s[1], d, ay))
                mx.append(max([v for v, q, y in zip(a, s[1], s[2]) if q >= OPEN_Q and abs(y - ay) <= WIN_YEARS] or [float('nan')]))
                tot.append(sum(1 for i in range(1, len(a)) if (a[i - 1] >= 0.5) != (a[i] >= 0.5)))
            W('%s\t%s\t%s\t%s\t%s\t%g' % (nat, name, verdict(cs), ','.join('%d/%d' % (n, na) for n, na, _ in cs), ','.join('%.2f' % m for m in mx), statistics.median(tot)))
    W('')
    W('## 3. 案 γ（戻す側を凍結の目減りの比 f÷(R×(1+π)) に比例・定額へは規則 A の P→F を借りる）：錨の窓の判定・種ごとの n/na・全長の切り替えの中央')
    W('国\t腕\t判定\t種ごとの n/na\t全長の切り替えの中央\t開幕で定額の国の f÷R が 0.5 を最初に下回った年（種ごと）')
    for nat in list(ANCHOR) + ['イギリス']:
        d, ay = ANCHOR.get(nat, ('F->P', 0))
        for name, _ in arms:
            ss = D[name].get(nat, []); cs = []; tot = []; yr = []
            for s in ss:
                st = nat in OPEN_FIXED
                g = gamma(s[4], s[3], st)
                if nat in ANCHOR: cs.append(anchor_counts(g, s[2], s[1], d, ay))
                tot.append(sum(1 for i in range(1, len(g)) if (g[i - 1] >= 0.5) != (g[i] >= 0.5)))
                if st:
                    R0 = s[4][0]; y0 = '-'
                    for r, y in zip(s[4], s[2]):
                        if r == r and r > 0 and R0 / r < 0.5: y0 = str(y); break
                    yr.append(y0)
            W('%s\t%s\t%s\t%s\t%g\t%s' % (nat, name, verdict(cs) if cs else '-', ','.join('%d/%d' % (n, na) for n, na, _ in cs) if cs else '-', statistics.median(tot) if tot else float('nan'), ','.join(yr) if yr else '-'))
    W('')
    W('## 4. 目標 T を Move の速さで追う今の値 φ*（φ*_{q+1}＝φ*＋s×(T−φ*)・s＝PolicyBaseSpeed 0.12〔Coefficients.cs:506〕＝抵抗 0・押しの予算 1 の上限の速さ）：')
    W('##    φ* が 0.5 をまたぐ回数で錨の窓を数え直す・窓の中と全長の Σ|Δφ*|（往復＝Σ|Δφ*|÷2 の桁）・置き換えた窓で〈通る〉の数')
    W('規則\t国\t腕\t判定\t種ごとの n/na\t全長の 0.5 をまたぐ回数の中央\t窓の中の Σ|Δφ*| の中央\t全長の Σ|Δφ*| の中央\t置き換えた窓で通る／窓の数')
    S = 0.12
    def follow(T, start):
        phi = start; s = []
        for t in T:
            if t == t: phi = phi + S * (t - phi)
            s.append(phi)
        return s
    def alpha_s(pi, start):
        phi = start; s = []
        for x in pi:
            if x == x: phi = min(1.0, max(0.0, phi + S * (min(1.0, max(0.0, phi - x)) - phi)))
            s.append(phi)
        return s
    for k, col in (('A', 5), ('B', 6), ('α', 3)):
        for nat, (d, ay) in ANCHOR.items():
            for name, _ in arms:
                ss = D[name].get(nat, []); cs = []; tot = []; tvw = []; tvt = []; paths = []
                for s in ss:
                    st = 1.0 if nat in OPEN_FIXED else 0.0
                    ph = alpha_s(s[3], st) if k == 'α' else follow(s[col], st)
                    paths.append((ph, s))
                    cs.append(anchor_counts(ph, s[2], s[1], d, ay))
                    tot.append(sum(1 for i in range(1, len(ph)) if (ph[i - 1] >= 0.5) != (ph[i] >= 0.5)))
                    tvt.append(sum(abs(ph[i] - ph[i - 1]) for i in range(1, len(ph))))
                    tvw.append(sum(abs(ph[i] - ph[i - 1]) for i in range(1, len(ph)) if s[1][i] >= OPEN_Q and abs(s[2][i] - ay) <= WIN_YEARS))
                npass = 0; nwin = 0
                for y in range(1795, 1941, 5):
                    if abs(y - ay) < 10: continue
                    nwin += 1
                    if verdict([anchor_counts(ph, s[2], s[1], d, y) for ph, s in paths]) == '通る': npass += 1
                W('%s\t%s\t%s\t%s\t%s\t%g\t%.3g\t%.3g\t%d/%d' % (k, nat, name, verdict(cs), ','.join('%d/%d' % (n, na) for n, na, _ in cs), statistics.median(tot), statistics.median(tvw), statistics.median(tvt), npass, nwin))
    open(out, 'wb').write(('\n'.join(L) + '\n').encode('utf-8'))

if __name__ == '__main__':
    main()
