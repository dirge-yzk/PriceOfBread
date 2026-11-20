# (1270) c130rc 空洞化の振り分け：an_h2.py -v4 の出力の〈1/10〉の件ごとに、基準の生の同じ種・同じ州のピーク後の最小÷ピークを読む。
# 基準ですでに 1/4 未満＝〈元からの弱さ〉（止めない）・それ以外＝〈線が原因〉（止める線）。結果の後に規則を変えない（事前登録の写し）。
# 使い方: hol_attr.py <an_h2 -v4 の出力> <基準の生>
import sys,re,csv
txt,B=sys.argv[1],sys.argv[2]
pat=re.compile(r'\[([^\]・]+)・1/10\]\s+s(\d+)\s+(\S+?)（')
cases=[(m.group(1),int(m.group(2)),m.group(3)) for line in open(txt,encoding='utf-8') for m in [pat.search(line)] if m]
print(f"# hol_attr 件 {len(cases)}・基準 {B}")
for kind,s,reg in cases:
    f=open(f"{B}/region_s{s}.tsv",encoding='utf-8',newline=''); r=csv.reader(f,delimiter='\t'); h=next(r); ix={c:i for i,c in enumerate(h)}
    t=[(int(x[ix['quarter']]),float(x[ix['populationK']])) for x in r if x[ix['region']]==reg]; f.close()
    if not t: print(f"{kind}\ts{s}\t{reg}\t基準に州が無い\t線が原因"); continue
    t.sort(); pk=max(v for _,v in t); qp=[q for q,v in t if v==pk][0]; mn=min(v for q,v in t if q>=qp)
    ratio=mn/pk if pk>0 else float('nan')
    print(f"{kind}\ts{s}\t{reg}\t基準 ピーク {pk:.1f}・後の最小 {mn:.1f}・比 {ratio:.4f}\t{'元からの弱さ' if ratio<0.25 else '線が原因'}")
