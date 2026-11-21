# (1290) c133mr labeltier_s の集計（一番動かす量〈空の主流の札の国×四半期〉と周辺の点検・記録）。エンジンの関数を呼ばない（表の列を数えるだけ）。
#   empty*＝Power の札が空の国×四半期（エンジンの値）・changed*＝計器の新の規則≠旧の規則・pw≠new＝Power の札と計器の新の規則の食い違い（月の同期と四半期の末の差）
#   H1＝Power の文化の札≠首都の州の札。周辺の点検＝p.Culture の読み手（CultureGroup :1143・民族の緊張 :1177・距離 :1225・割譲の sameCore :2955・形成の資格 Formations :322）は changedC、
#   p.Religion の読み手（宗教の家 :1144・宗教の緊張 :1182・保護の利害 :1207・距離 :1231・形成の資格 Formations :319 :323）は changedR の国×四半期で変わる。
# 使い方: python -X utf8 -I lt_sum.py <生> <種の起点> <種の数>
import sys, csv, collections
raw, sb, ns = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
csv.field_size_limit(10**8)
tot = collections.Counter(); natsC = collections.Counter(); miss = []
print(f'# (1290) lt_sum {raw} 種 {sb}..{sb+ns-1}')
for s in range(sb, sb + ns):
    try: f = open(f'{raw}/labeltier_s{s}.tsv', encoding='utf-8')
    except FileNotFoundError: miss.append(s); continue
    rd = csv.reader(f, delimiter='\t'); h = next(rd); ix = {k: i for i, k in enumerate(h)}
    o = collections.Counter()
    for c in rd:
        if len(c) < len(h): o['short'] += 1; continue
        o['nq'] += 1
        if c[ix['powerCultureEmptyFlag']] == '1': o['emptyC'] += 1
        if c[ix['powerReligionEmptyFlag']] == '1': o['emptyR'] += 1
        if c[ix['changedC']] == '1': o['changedC'] += 1; natsC[c[ix['nation']]] += 1
        if c[ix['changedR']] == '1': o['changedR'] += 1
        if c[ix['powerCulture']] != c[ix['newCulture']]: o['pwNeNewC'] += 1
        if c[ix['powerReligion']] != c[ix['newReligion']]: o['pwNeNewR'] += 1
        if c[ix['powerCulture']] != c[ix['capCulture']]: o['H1'] += 1
        o['tierC_' + c[ix['tierC']]] += 1; o['tierR_' + c[ix['tierR']]] += 1
    f.close()
    tot += o
    print(f'種 {s}\t' + '\t'.join(f'{k}={v}' for k, v in sorted(o.items())))
print('計\t' + '\t'.join(f'{k}={v}' for k, v in sorted(tot.items())))
print('changedC の国（国×四半期）: ' + '・'.join(f'{k} {v}' for k, v in natsC.most_common(30)))
if miss: print('欠け 種: ' + ' '.join(map(str, miss)))
