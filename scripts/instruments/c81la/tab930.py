# (930) c81la：同じ盤面の新÷旧の表（★(919)・記録＝採否に使わない）。読むだけ・エンジンの関数を呼ばない・生の TSV を自分で読む。
#   使い方: python tab930.py <腕の生> <基線の生> <N1 の生> <N2 の生> [種の起点 1|11]
#   列（事前登録 [9]）：名簿 7 国ごとに
#     ①階級ごとの地代 landshare_s の landRentActual{Noble,Clergy,Peasant,Capitalist}PerQuarter（q0 と q0〜q39 の平均）
#     ②名目 GDP nation_s の nominalGdpPerYear（q0 と q0〜q39 の平均）
#   仏だけ（開幕の政治の門・(909) の AiFirstWave:325・ReformPath:430 の信号の代わりの観測量）：
#     ③reform_s の suffrageLawCode が q0 の値から最初に変わる四半期（無ければ -1）・wallPassCount の q0〜q39 の和・wallPassCostIndex の q0〜q39 の平均・blockedLawCount の q0〜q39 の平均・supportShare の q0
#     ④fiscal_s の ledgerTariffPerMonth の最初の 120 か月の平均
#   値は種の中央値。比＝腕÷基線・ヌル腕の比＝N1÷基線・N2÷基線（同じ列の雑音の幅の目安）。
import sys, os, statistics
ROSTER = ['フランス', 'イギリス', 'スペイン', 'プロイセン', 'デンマーク', '日本', 'ポーランド']
CL = ['Noble', 'Clergy', 'Peasant', 'Capitalist']

def rows(p, keyset=None):
    with open(p, 'rb') as f:
        L = f.read().decode('utf-8').splitlines()
    h = L[0].split('\t'); ni = h.index('nation')
    out = []
    for l in L[1:]:
        if not l: continue
        c = l.split('\t')
        if keyset is not None and c[ni] not in keyset: continue
        out.append(dict(zip(h, c)))
    return out

def per_seed(folder, s):
    v = {}
    ls = rows(os.path.join(folder, f'landshare_s{s}.tsv'), set(ROSTER))
    na = rows(os.path.join(folder, f'nation_s{s}.tsv'), set(ROSTER))
    for n in ROSTER:
        lr = [r for r in ls if r['nation'] == n and int(r['quarter']) < 40]
        q0 = [r for r in lr if r['quarter'] == '0'][0]
        for c in CL:
            k = f'landRentActual{c}PerQuarter'
            v[(n, f'rent{c}_q0')] = float(q0[k])
            v[(n, f'rent{c}_q0-39')] = statistics.mean(float(r[k]) for r in lr)
        nr = [r for r in na if r['nation'] == n and int(r['quarter']) < 40]
        v[(n, 'gdp_q0')] = float([r for r in nr if r['quarter'] == '0'][0]['nominalGdpPerYear'])
        v[(n, 'gdp_q0-39')] = statistics.mean(float(r['nominalGdpPerYear']) for r in nr)
    rf = [r for r in rows(os.path.join(folder, f'reform_s{s}.tsv'), {'フランス'}) if int(r['quarter']) < 40]
    rf.sort(key=lambda r: int(r['quarter']))
    s0 = rf[0]['suffrageLawCode']
    v[('フランス', 'suffrageFirstChangeQ')] = next((int(r['quarter']) for r in rf if r['suffrageLawCode'] != s0), -1)
    v[('フランス', 'wallPassCount_sum')] = sum(float(r['wallPassCount']) for r in rf)
    v[('フランス', 'wallPassCostIndex_mean')] = statistics.mean(float(r['wallPassCostIndex']) for r in rf)
    v[('フランス', 'blockedLawCount_mean')] = statistics.mean(float(r['blockedLawCount']) for r in rf)
    v[('フランス', 'supportShare_q0')] = float(rf[0]['supportShare'])
    fs = [r for r in rows(os.path.join(folder, f'fiscal_s{s}.tsv'), {'フランス'}) if int(r['monthIndex']) < 120]
    v[('フランス', 'tariff_m0-119_mean')] = statistics.mean(float(r['ledgerTariffPerMonth']) for r in fs) if fs else float('nan')
    return v

def med(folder, seeds):
    S = [per_seed(folder, s) for s in seeds]
    return {k: statistics.median(x[k] for x in S) for k in S[0]}

def rat(a, b):
    return a / b if b != 0 else (1.0 if a == 0 else float('inf'))

def main():
    arm, base, n1, n2 = sys.argv[1:5]; sb = int(sys.argv[5]) if len(sys.argv) > 5 else 1
    A = med(arm, range(sb, sb + 10)); B = med(base, range(sb, sb + 10))
    N1 = med(n1, range(1, 11)); N2 = med(n2, range(1, 11)); B1 = med(os.environ.get('BASE1', base), range(1, 11)) if sb != 1 else B   # 種 11〜20 のとき、ヌル腕の比の分母は種 1〜10 の基線（環境変数 BASE1）
    print(f'# tab930 arm={arm} base={base} 種 {sb}〜{sb+9}（ヌル腕の比は種 1〜10 の基線との比）')
    print('nation\tcolumn\tarm\tbase\tarm/base\tN1/base\tN2/base')
    for k in A:
        print(f'{k[0]}\t{k[1]}\t{A[k]:.6g}\t{B[k]:.6g}\t{rat(A[k], B[k]):.4f}\t{rat(N1[k], B1[k]):.4f}\t{rat(N2[k], B1[k]):.4f}')

if __name__ == '__main__':
    main()
