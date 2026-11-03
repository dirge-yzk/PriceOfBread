# (802) 門 O の集計（読むだけ）：inv_income の tsv（9国計）から 1850〜1949 の 配当の払い÷それ以外の受け取り。python -I o_gate.py 名前=tsv ...
#   rec801.sh の heredoc（/d/ の道を Windows の python に渡して空になった＝反証役 low）の置き換え。式は rec801.sh と同じ。
import csv, sys
sys.stdout.reconfigure(encoding='utf-8')
for arg in sys.argv[1:]:
    k, f = arg.split('=', 1); d = o = 0.0
    for r in csv.DictReader(open(f, encoding='utf-8'), delimiter='\t'):
        if r['nation'] == '9国計' and 1850 <= int(r['dec']) <= 1940: d += -float(r['div']); o += float(r['otherPos'])
    print(f'O {k}: 配当の払い÷それ以外の受け取り（9 国・1850〜1949）= {d / o:.4f}（配当の払い {d:.6g}・受け取り {o:.6g}）')
