# (924) 問い3・4：全ての国で〈開幕の王冠が専制でない国〉の王冠=専制の四半期と、押し付けの代理（政変の印＋正統性≦0.315）の件数
# 代理であることを名乗る：矯正講和の計器は無い（勝者・敗者・戦勝点の列が無い）＝正統性が ImposedRegimeLegitimacy 0.3 の近くの政変を数えるだけ。
# 使い方: python -X utf8 -I q5_imposed.py <out>
import csv, sys, math, os
csv.field_size_limit(10**8)
ROOT = 'D:/PoB_work/keep_919'
ARMS = [('base', 'base_ins', range(1, 11)), ('n1', 'n1', range(1, 11)), ('n2', 'n2', range(1, 11)), ('a11', 'a11', range(11, 21))]
POL = {0: '絶対', 1: '専制', 2: '幕府', 3: '立憲', 4: '共和', 5: 'ブル独', 6: 'プロ独', 7: '軍政', 8: '無政府'}
CROWN = {0: '無', 1: '絶対', 2: '専制', 3: '幕府'}
def f(x):
    try: return float(x)
    except Exception: return math.nan
out = open(sys.argv[1], 'w', encoding='utf-8', newline='\n')
out.write('arm\t開幕の王冠が専制でない国の王冠=専制の四半期\tうち英\t同じく札=専制の四半期\t押し付けの代理の件数\t代理のうち王冠が替わった件（前の王冠→後の王冠）\t代理のうち英\t種ごとの〈王冠=専制の四半期・開幕が専制でない国〉\n')
for arm, sub, seeds in ARMS:
    tot = 0; uk = 0; lab = 0; imp = 0; chg = {}; impuk = 0; per = []
    for s in seeds:
        first = {}; t_s = 0
        with open(os.path.join(ROOT, sub, f'crownpc_s{s}.tsv'), encoding='utf-8', newline='') as fh:
            for r in csv.DictReader(fh, delimiter='\t'):
                n = r['nation']
                if n not in first: first[n] = r['crownCode']
                if first[n] != '2' and r['crownCode'] == '2':
                    tot += 1; t_s += 1
                    if n == 'イギリス': uk += 1
                    if r['polityCode'] == '1': lab += 1
                if r['regimeEventFlag'] == '1' and f(r['legitimacyIndex']) <= 0.315 and r['firstRowFlag'] != '1':
                    imp += 1
                    a, b = r['crownPrevCode'], r['crownCode']
                    if a != b:
                        k = f'{CROWN.get(int(f(a)) if not math.isnan(f(a)) else -1, a)}→{CROWN.get(int(f(b)), b)}'
                        chg[k] = chg.get(k, 0) + 1
                    if n == 'イギリス': impuk += 1
        per.append(t_s)
    out.write(f'{arm}\t{tot}\t{uk}\t{lab}\t{imp}\t' + ' '.join(f'{k}:{v}' for k, v in sorted(chg.items())) + f'\t{impuk}\t{per}\n')
out.close()
