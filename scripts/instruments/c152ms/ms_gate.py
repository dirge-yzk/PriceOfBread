# (1423) c152ms の門と記録の台本（生を読むだけ・エンジンを呼ばない・Coefficients を読まない）。
# 使い方: python -X utf8 ms_gate.py <生のフォルダ> [種の数=10]
# 門 G1（主・誰に届いたか）：armsfill_s の militarismMul 列が 1 から 1e-6 を超えて離れた国×四半期の数（採る条件＝10 種の合計 0・
#   各の種で有限の行が 1 本以上＝対照の件数）。許し 1e-6 は印字（G10＝1 の付近で 1e-9 刻み）より広く、旧の最小の上乗せ（0.3×練度）より狭い。
#   旗 0（係数 1.3）の盤面では練度＞0 の国×四半期が全て数えられる＝門は落ちる（紙の台本で確かめる）。
#   練度そのものの列は無い＝分母〈練度＞0 の国×四半期〉は腕 B では見えない＝全ての行（上位の集合）で読む。基線の行数は〈旧で練度＞0〉の数。
# 記録：国ごとの 1880 年以後の militarismMul の平均と、そこから出る p.Military の減り 1−1/m（同じ兵数・同じ即応度のとき）。
import sys, collections, math

def load(p):
    rows = open(p, encoding='utf-8').read().splitlines()
    h = rows[0].split('\t'); ix = {k: i for i, k in enumerate(h)}
    return ix, [r.split('\t') for r in rows[1:] if r]

def main():
    d = sys.argv[1]; ns = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    TOL = 1e-6
    tot = collections.Counter(); late = collections.defaultdict(list); minbonus = math.inf; maxm = 0.0
    for s in range(1, ns + 1):
        ix, rows = load(f'{d}/armsfill_s{s}.tsv'); k = ix['militarismMul']
        fin = off = above = nonfin = 0
        for r in rows:
            m = float(r[k])
            if not math.isfinite(m): nonfin += 1; continue
            fin += 1
            if abs(m - 1.0) > TOL: off += 1
            if m > 1.0 + TOL: above += 1; minbonus = min(minbonus, m - 1.0)
            maxm = max(maxm, m)
            if int(r[ix['year']]) >= 1880: late[r[ix['nation']]].append(m)
        print(f'seed {s} rows={len(rows)} finite={fin} nonfinite={nonfin} off1={off} above1={above}')
        tot['finite'] += fin; tot['off'] += off; tot['above'] += above; tot['nonfinite'] += nonfin
        tot['seedsWithRows'] += 1 if fin > 0 else 0
    print('TOTAL', ' '.join(f'{k}={v}' for k, v in sorted(tot.items())), f'maxMul={maxm:.6g}', f'minBonusAbove={minbonus if math.isfinite(minbonus) else 0:.6g}')
    for n in sorted(late, key=lambda x: -sum(late[x]) / len(late[x])):
        v = late[n]; m = sum(v) / len(v)
        print(f'LATE1880 {n} meanMul={m:.4f} maxMul={max(v):.4f} militaryDropIfRemoved={1 - 1 / m:.4f} n={len(v)}')
    ok = tot['off'] == 0 and tot['seedsWithRows'] == ns and tot['nonfinite'] == 0
    print(f'GATE G1 off1={tot["off"]} nonfinite={tot["nonfinite"]} seedsWithRows={tot["seedsWithRows"]}/{ns} -> {"PASS" if ok else "FAIL"} (accept if off1=0 and every seed has rows)')

main()
