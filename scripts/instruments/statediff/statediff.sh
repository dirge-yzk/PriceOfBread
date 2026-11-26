#!/usr/bin/env bash
# (1391) 短い確かめ：基点の木と線の木を同じ種で数四半期だけ回し、最初にずれた四半期と群（欄の道）を出す。
#   bash statediff.sh <基点の木> <線の木> [種=1789] [四半期=12] [群の深さ=6]    出力＝/d/PoB_work/statediff_<pid>/ ・結果の最後の行＝一致 か 最初のずれ
# 木ごとに隔離ビルド（--artifacts-path）し、artifacts の根に statediff.cfg を置いて試験 1 本（状態の指紋を書き出す）を回す。Runner・環境変数は使わない。
set -u
A=${1:?基点の木}; B=${2:?線の木}; SEED=${3:-1789}; Q=${4:-12}; DEP=${5:-6}
W=/d/PoB_work/statediff_$$; mkdir -p "$W"
for t in A B; do
  tree=${!t}; art="$W/art_$t"; mkdir -p "$art"
  printf 'seed=%s\nquarters=%s\ndepth=%s\nout=%s\n' "$SEED" "$Q" "$DEP" "D:/PoB_work/statediff_$$/$t.tsv" > "$art/statediff.cfg"
  ( cd "$tree" && dotnet test PriceOfBread.Tests -c Release --artifacts-path "D:/PoB_work/statediff_$$/art_$t" --filter "FullyQualifiedName~状態の指紋を書き出す" > "$W/$t.log" 2>&1 ) || { echo "試験が失敗 $t：$W/$t.log"; tail -5 "$W/$t.log"; exit 2; }
done
python "$(dirname "$0")/cmp.py" "$W/A.tsv" "$W/B.tsv"
