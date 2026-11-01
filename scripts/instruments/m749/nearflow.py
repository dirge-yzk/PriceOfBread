"""(749) 合流：c3h1 の反証役 中 1 の記録＝近い流れ（3,000 km 以下）の計画の新÷旧＝(liveAll−liveFar)/(oldAll−oldFar)を国ごとに（migform_s・読むだけ・全四半期と種の和）。
python -X utf8 nearflow.py <計器の出力> [種...]  （計画は受け入れの上限で縮める前の量＝実現した流れではない）
⚠(749) の記録の読み手：migform_s は (753) c4bk の焼き込みで削除＝keep_749 の生（m749 の木の出力）にだけ当てる（新しい木の出力では種が空になり 0/0 で落ちる）。"""
import sys, os, collections
d = sys.argv[1]
seeds = [int(x) for x in sys.argv[2:]] or sorted(int(f[len('migform_s'):-4]) for f in os.listdir(d) if f.startswith('migform_s'))
acc = collections.defaultdict(lambda: [0.0] * 4)
for sd in seeds:
    with open(os.path.join(d, f'migform_s{sd}.tsv'), encoding='utf-8') as fh:
        h = next(fh).rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for ln in fh:
            c = ln.rstrip('\n').split('\t')
            if len(c) != len(h): continue
            a = acc[c[ix['nation']]]
            for j, k in enumerate(['liveFarPlanK', 'liveAllPlanK', 'oldFarPlanK', 'oldAllPlanK']): a[j] += float(c[ix[k]])
tot = [0.0] * 4
rows = []
for n, (lf, la, of, oa) in acc.items():
    for j, v in enumerate((lf, la, of, oa)): tot[j] += v
    rows.append((oa, n, (la - lf) / (oa - of) if oa - of else float('nan'), lf / of if of else float('nan'), la / oa if oa else float('nan')))
print('nation\tnear(新÷旧)\tfar(新÷旧)\tall(新÷旧)\toldAllPlanK')
KEY = {'イギリス', 'フランス', 'プロイセン', 'ロシア', 'スペイン', '清', 'アメリカ', 'オーストリア', '日本', 'オスマン'}
for k, (oa, n, nr, fr, ar) in enumerate(sorted(rows, reverse=True)):
    if k < 25 or n in KEY: print(f'{n}\t{nr:.3f}\t{fr:.3f}\t{ar:.3f}\t{oa:.0f}')
lf, la, of, oa = tot
print(f'WORLD\t{(la-lf)/(oa-of):.3f}\t{lf/of:.3f}\t{la/oa:.3f}\t{oa:.0f}')
