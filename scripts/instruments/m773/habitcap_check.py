"""(773) 合流：habitcap_s の旗 0 の不変条件（全ての行で usedNextLastIndex＝oldNextLastIndex〔"R" の文字列で比べる〕・capOffFlag＝0・bind≦months）を読む（読むだけ）。
python -I habitcap_check.py <ins> [<ins> ...]　列は見出しの名前で読む。"""
import sys, os
for d in sys.argv[1:]:
    fs = sorted(f for f in os.listdir(d) if f.startswith('habitcap_s') and f.endswith('.tsv'))
    rows = bad = flag = bindbad = months = binds = 0
    for f in fs:
        with open(os.path.join(d, f), encoding='utf-8') as fh:
            h = fh.readline().rstrip('\n').split('\t'); ix = {k: i for i, k in enumerate(h)}
            for l in fh:
                c = l.rstrip('\n').split('\t')
                if len(c) < len(h): continue
                rows += 1
                if c[ix['usedNextLastIndex']] != c[ix['oldNextLastIndex']]: bad += 1
                if c[ix['capOffFlag']] != '0': flag += 1
                m, b = int(c[ix['monthsCount']]), int(c[ix['bindMonthsCount']])
                months += m; binds += b
                if b > m: bindbad += 1
    print(f'{d}: 表 {len(fs)} 本・行 {rows}・used≠old {bad}・capOffFlag≠0 {flag}・bind＞months {bindbad}・月 {months}・蓋が効いた月 {binds}（{binds/max(months,1):.4f}）', '→ 通る' if fs and not (bad or flag or bindbad) else '→ 外れ')
