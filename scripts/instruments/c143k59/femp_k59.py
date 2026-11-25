# (1356) c143k59 femp_s の読み手（読むだけ・エンジンの関数を呼ばない・表の列から自分で数える）。
#   狙いの観測量＝oldStruct−newStruct＞0.01 の行（旧の規則で構造の参加率が上がる国×四半期）の actualStruct が newStruct に一致する割合（旗 1 で 1.0・旗 0 で 0）。
#   記録＝その行の数・法の段≧1 の行・届いた国・6 大国と清・日本の 1913 年の actualStruct（種の中央）。種の番号はファイルの名から取る（os.path.basename・★(1348)）。
#   python femp_k59.py <生のフォルダ>
import sys, os, re, glob, statistics as st
d = sys.argv[1]
BIG = ['イギリス', 'フランス', 'プロイセン', 'ロシア', 'アメリカ', 'オーストリア']; REC = ['清', '日本']
files = sorted(glob.glob(os.path.join(d, 'femp_s*.tsv')), key=lambda p: int(re.match(r'femp_s(\d+)\.tsv$', os.path.basename(p)).group(1)))
tot = dn = mt = mo = law = 0; ever = {}; y1913 = {}
print(f'# 生 {d}・種のファイル {len(files)}')
for p in files:
    s = int(re.match(r'femp_s(\d+)\.tsv$', os.path.basename(p)).group(1))
    sd = sm = 0
    with open(p, encoding='utf-8') as f:
        h = f.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
        for line in f:
            x = line.rstrip('\n').split('\t'); tot += 1
            old, new, act = float(x[ix['oldStruct']]), float(x[ix['newStruct']]), float(x[ix['actualStruct']])
            if int(x[ix['womenRightsCode']]) >= 1: law += 1
            if old - new > 0.01:
                dn += 1; sd += 1; ever.setdefault(x[ix['nation']], set()).add(s)
                if abs(act - new) <= 1e-6: mt += 1; sm += 1
                if abs(act - old) <= 1e-6: mo += 1
            if x[ix['year']] == '1913' and x[ix['nation']] in BIG + REC:
                y1913.setdefault(x[ix['nation']], {}).setdefault(s, []).append(act)
    print(f'種 {s}\t差の行 {sd}\t一致 {sm}')
print(f'行の数 {tot}・法の段≧1 の行 {law}')
print(f'差の行（oldStruct−newStruct＞0.01）{dn}・actualStruct＝newStruct {mt}（割合 {mt/dn if dn else float("nan"):.4f}）・actualStruct＝oldStruct {mo}')
print(f'差の行のある国（どれかの種）{len(ever)}：' + '・'.join(f'{k} {len(v)}' for k, v in sorted(ever.items(), key=lambda z: -len(z[1]))))
for n in BIG + REC:
    if n in y1913:
        xs = [sum(v) / len(v) for v in y1913[n].values()]
        print(f'1913 {n}\t種 {len(xs)}\tactualStruct の中央 {st.median(xs):.3f}\t最大 {max(xs):.3f}')
