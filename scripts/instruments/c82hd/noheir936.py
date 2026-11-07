# (936) c82hd 紙（読むだけ）：U1 の危うさ＝相続人の無い州で人の居ない POP に債務が残る行の目安。
#   python -X utf8 -I noheir936.py <ins フォルダ> [--seeds 1 10]
#   cearn_s の行で emptyDebtAfterEstate ＞0 かつ estateNoHeirPerQuarter ＞0 の行数（相続人の無い州で、在庫が正の空の POP があった四半期）を tier ごとに数える。
#   ⚠estateNoHeirPerQuarter は貯蓄が正の空の POP だけを見る＝貯蓄 0・債務ありだけの相続人の無い州は拾えない（目安）。
import sys, os, collections
args = sys.argv[1:]
s0, s1 = 1, 10
if '--seeds' in args:
    i = args.index('--seeds'); s0, s1 = int(args[i + 1]), int(args[i + 2]); del args[i:i + 3]
d = args[0]
c = collections.Counter(); tot = collections.Counter()
for s in range(s0, s1 + 1):
    p = os.path.join(d, f'cearn_s{s}.tsv')
    if not os.path.exists(p): continue
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        it, iu, inh = ix['tier'], ix['emptyDebtAfterEstate'], ix['estateNoHeirPerQuarter']
        for line in f:
            x = line.rstrip('\n').split('\t')
            if int(x[iu]) > 0:
                tot[x[it]] += 1
                try: nh = float(x[inh])
                except ValueError: nh = 0.0
                if nh > 0: c[x[it]] += 1
print(f'# フォルダ {d}')
print('emptyDebtAfterEstate＞0 の行（tier ごと）：' + '・'.join(f'{k} {v}' for k, v in sorted(tot.items())))
print('その中で estateNoHeirPerQuarter＞0：' + ('・'.join(f'{k} {v}' for k, v in sorted(c.items())) or '0'))
