# (918) c79lo 読み手 2（読むだけ）：B-1（土地税の払えない分を持ち分の手放しで払う）の桁の紙と、RGO の数の分母。
# 使い方：python -X utf8 -I read918b.py <出力.txt>
# 入力（読むだけ）：D:/PoB_work/keep_912/base_ins・keep_905/n1・n2 の landbase_s・nation_s・landshare_s（種 1〜10）
# 式（AI の案・確証なし）：土地の値＝R×12÷金利（R＝landbase_s の baseRPerMonth・金利＝nation_s の bondRateMeanPerYear）。
#   払えない土地税＝landClaimPerMonth−landTaxPaidPerMonth（請求は免税の後か前かを計器の注で確かめていない＝両方の列を並べる）。
#   年に手放す割合（上限）＝12×払えない分÷土地の値＝12×(claim−paid)×rate÷(12×R)＝(claim−paid)×rate÷R。
# 限界（自己申告）：①今の盤面は収穫比例の基数（A-2 の定額でない）＝凶作で基数も落ちる＝払えない分は A-2 の後より小さい見込み。
#   ②払えない分は国の和＝持ち分を持たない POP（不在地主・官僚の LandRent）の分を含む。③四半期の末の月だけ。④金利は四半期の平均。
import csv, io, statistics as st, sys

OUT = sys.argv[1]
ARMS = {'m905': 'D:/PoB_work/keep_912/base_ins', 'n1': 'D:/PoB_work/keep_905/n1', 'n2': 'D:/PoB_work/keep_905/n2'}
NAT = ['ロシア', 'プロイセン', 'ポーランド', '日本', 'スウェーデン', 'デンマーク', 'スペイン', 'フランス', 'イギリス', '清', 'オーストリア', 'オスマン']
SEEDS = range(1, 11)
WIN = {'1783-85': range(0, 12), '1800-09': range(68, 108), '1840-49': range(228, 268), '1900-09': range(468, 508)}


def read_tsv(path):
    txt = open(path, 'rb').read().decode('utf-8-sig')
    rows = [r for r in csv.reader(io.StringIO(txt), delimiter='\t') if r and not r[0].startswith('#')]
    return rows[0], rows[1:]


def f(x):
    try:
        return float(x)
    except ValueError:
        return float('nan')


def med(xs):
    xs = [x for x in xs if x == x]
    return st.median(xs) if xs else float('nan')


res = {}
cnt = {}
for a, base in ARMS.items():
    for s in SEEDS:
        hb, rb = read_tsv(f'{base}/landbase_s{s}.tsv')
        ib = {k: i for i, k in enumerate(hb)}
        hn, rn = read_tsv(f'{base}/nation_s{s}.tsv')
        jn = {k: i for i, k in enumerate(hn)}
        rate = {(r[jn['nation']], int(r[jn['quarter']])): f(r[jn['bondRateMeanPerYear']]) for r in rn if r[jn['nation']] in NAT}
        if a == 'm905' and s == 1:
            hl, rl = read_tsv(f'{base}/landshare_s{s}.tsv')
            il = {k: i for i, k in enumerate(hl)}
            for r in rl:
                if r[il['quarter']] == '0' and r[il['nation']] in NAT:
                    cnt[r[il['nation']]] = (r[il['rgoNullCount']], r[il['rgoFieldCount']], r[il['rgoEndowCount']])
        for r in rb:
            n = r[ib['nation']]
            if n not in NAT:
                continue
            q = int(r[ib['quarter']])
            for wn, rg in WIN.items():
                if q in rg:
                    R = f(r[ib['baseRPerMonth']])
                    cl = f(r[ib['landClaimPerMonth']])
                    ce = f(r[ib['landClaimExemptPerMonth']])
                    pd = f(r[ib['landTaxPaidPerMonth']])
                    rt = rate.get((n, q), float('nan'))
                    e = res.setdefault((a, n, wn), {'sell': [], 'sellE': [], 'poc': [], 'rate': [], 'claimOverR': []})
                    if R > 0:
                        e['sell'].append(max(0.0, cl - pd) * rt / R)
                        e['sellE'].append(max(0.0, ce - pd) * rt / R)
                        e['claimOverR'].append(cl / R)
                    e['poc'].append(f(r[ib['landPaidOverClaim']]))
                    e['rate'].append(rt)
out = ['# (918) read918b.py の出力（B-1 の桁の紙・盤面 m905＝keep_912/base_ins・ヌル腕 keep_905/n1・n2・種と四半期をまとめた中央）', '',
       '## ① 年に手放す割合の上限（(請求−払えた土地税)×金利÷R）・払えた÷請求・金利・請求÷R',
       'nation\tarm\twindow\tsellPerYear\tsellPerYearExemptClaim\tpaidOverClaim\tbondRatePerYear\tclaimOverR']
for n in NAT:
    for a in ARMS:
        for wn in WIN:
            e = res.get((a, n, wn))
            if e:
                out.append(f"{n}\t{a}\t{wn}\t{med(e['sell']):.5f}\t{med(e['sellE']):.5f}\t{med(e['poc']):.4f}\t{med(e['rate']):.4f}\t{med(e['claimOverR']):.4f}")
out += ['', '## ② RGO の数（m905・種 1・q0・landshare_s）：rgoNullCount・rgoFieldCount・rgoEndowCount', 'nation\tnull\tfield\tendow']
for n in NAT:
    if n in cnt:
        out.append(f'{n}\t' + '\t'.join(cnt[n]))
open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('wrote', OUT, len(out))
