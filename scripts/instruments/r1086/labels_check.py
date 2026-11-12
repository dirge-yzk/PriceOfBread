# (1086) 読むだけ：flows.py の〈不明〉を world.json に無い州と空欄に分け、同じ文化の組の上位を出す（結合の確かめ）。
# 使い方：python -X utf8 -I labels_check.py <base_ins> <world.json> <種の数>
# 限界：札は world.json の静的な札。在シミュの植民地格の州は宗主国の首都の札を持つ（Simulation.cs の PromoteColonyToRegion）＝ここでは〈world.json に無い〉に入る。
import sys, json, collections as C
base, wj, nseed = sys.argv[1], sys.argv[2], int(sys.argv[3])
W = json.load(open(wj, encoding='utf-8'))
reg = {p['city']: (p.get('culture', ''), p.get('religion', '')) for x in W['nations'] for p in x['provinces']}
k = C.Counter(); same = C.Counter(); T = 0.0
for s in range(1, nseed + 1):
    f = open(f'{base}/xmig_s{s}.tsv', encoding='utf-8'); f.readline()
    for line in f:
        c = line.rstrip('\n').split('\t'); v = float(c[5])
        if not (v > 0): continue
        T += v
        a, b = reg.get(c[1]), reg.get(c[2])
        side = ('出' if a is None else '出空' if not a[0] else '出有') + '・' + ('入' if b is None else '入空' if not b[0] else '入有')
        k[side] += v
        if a and b and a[0] and a[0] == b[0]:
            same[(c[1], c[3], c[2], c[4], a[0])] += v
    f.close()
print('札の有無（出＝world.json に無い州・出空＝空欄・出有＝札あり）：')
for kk, v in k.most_common(): print(f'  {kk}\t{v/T:.1%}')
print('同じ文化の組の上位（量の割合）：')
for kk, v in same.most_common(8): print('  ' + '→'.join(kk[:4]) + f'\t{kk[4]}\t{v/T:.3%}')
print(f'同じ文化の合計：{sum(same.values())/T:.3%}')
