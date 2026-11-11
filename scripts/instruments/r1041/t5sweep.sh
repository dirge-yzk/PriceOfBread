#!/usr/bin/env bash
# (1041) c98bp 台本（まだ回していない・案 B の再開の線が事前登録の前に回す）
# CorncfInstrumentTests.T5 の盤面（英仏・DiplomacyFrozen・3 四半期）で、種 1791〜1802 の〈穀物の輸入が正の組の数〉を数える。
# ★(1030) 追記 3 ①の誤り（MultiNationSim の coef 引数は国の播種に届かない）を避けるため、旗は〈木の既定値〉で切り替える＝木を 2 本渡す。
#   木 A＝既定 0 の木（main か、再開の線の器のコミット）・木 B＝既定 1 の木（旗の既定を 1 にしたコミット）。
# 使い方: bash t5sweep.sh <木A> <木B> <出力フォルダ D:/PoB_work/...>
# 前提: probe_t5sweep.cs.txt を各木の PriceOfBread.Tests/ へ ProbeT5Sweep.cs として写す（コミットしない・回した後に消す）。
set -u
A="${1:?既定 0 の木}"; B="${2:?既定 1 の木}"; OUT="${3:?出力フォルダ}"
mkdir -p "$OUT"
for T in "$A" "$B"; do
  tag=$(basename "$T")
  cp "$(dirname "$0")/probe_t5sweep.cs.txt" "$T/PriceOfBread.Tests/ProbeT5Sweep.cs"
  dotnet test "$T/PriceOfBread.Tests" -c Release --artifacts-path "D:/PoB_work/r1041_t5_art_$tag" \
    --filter "FullyQualifiedName~ProbeT5Sweep" --logger "console;verbosity=detailed" > "$OUT/t5sweep_$tag.txt" 2>&1
  rm -f "$T/PriceOfBread.Tests/ProbeT5Sweep.cs"
done
grep -h "T5SWEEP" "$OUT"/t5sweep_*.txt > "$OUT/t5sweep_summary.txt"
# 読み方（事前登録へ写す規則・結果を見る前に固定する）：
#   種を選ぶ規則＝〈木 A と木 B の両方で正の組が 2 以上〉の種のうち最小の番号。該当が無ければ T5 は軌道に依る前提＝種を 5 本に広げる（★(1020) c95ub の型）。
