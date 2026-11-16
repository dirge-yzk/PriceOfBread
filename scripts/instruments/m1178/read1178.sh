#!/usr/bin/env bash
# (1178) 合流の読み手（m1171/read1171.sh の型）。門は線の台本をそのまま（c116t1/t1gates3.py・c116k1/gates_k1.py・c116hd/hd.py）。ヌル腕と NG の基準は m1171（keep_1171/n1・n2・ng1171）。
#   使い方: bash read1178.sh <ins（D:/ の形）> <成績表（D:/ の形・無ければ -）> <タグ> <出力フォルダ（D:/ の形）> <種の起点 1|11> <基準の生（D:/ の形）> <門の組（t1,k1,hd のカンマ区切り・無ければ -）> [S11z の ins（種 11〜20 の閾値）]
#   種 1〜10：線の閾値のまま（t1 の床は [A1] 46,539,221・3,476,739・hd は measurements/m1174_20261116 の paper の M・N1・N2・F1・F2）。
#   種 11〜20：閾値は S11z から（予言 [3]・m1178/gs11_1178.py）。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
D0="$(cd "$(dirname "$0")" && pwd)"; R="$(cd "$D0/../../.." && pwd -W)"; S=$R/scripts/instruments
INS=${1:?ins}; CARD=${2:?成績表}; TAG=${3:?タグ}; OUT=${4:?出力}; SB=${5:?種の起点}; BASE=${6:?基準}; GS=${7:--}; OFF=${8:--}
SE=$((SB+9)); K=D:/PoB_work/keep_1171; P=$R/measurements/m1174_20261116
mkdir -p "$OUT"
cd "$R"
case ",$GS," in *,t1,*)
  if [ "$SB" = 1 ]; then FS=46539221; FN=3476739
  else python -X utf8 -I "$S/c116t1/t1gates3.py" "$OFF" "$SB" "$SE" > "$OUT/t1_flag0.txt" 2>&1
       FS=$(grep '^床の材料' "$OUT/t1_flag0.txt" | cut -f2); FN=$(grep '^床の材料' "$OUT/t1_flag0.txt" | cut -f4); fi
  echo "t1 floors $FS $FN" > "$OUT/t1_floors.txt"
  python -X utf8 -I "$S/c116t1/t1gates3.py" "$INS" "$SB" "$SE" "$FS" "$FN" > "$OUT/gates_t1.txt" 2>&1; echo "t1 rc=$?"
  [ "$SB" != 1 ] && python -X utf8 -I "$S/c116t1/t1gates3.py" "$INS" "$SB" "$SE" 46539221 3476739 > "$OUT/gates_t1_A1floor.txt" 2>&1
  python -X utf8 -I "$S/c116t1/impratio.py" "$INS" "$BASE" "$K/n1" "$K/n2" > "$OUT/impratio.txt" 2>&1; echo "impratio rc=$?" ;;
esac
case ",$GS," in *,k1,*)
  python -X utf8 -I "$S/c116k1/gates_k1.py" "$INS" "$SB" "$SE" "$OUT/gates_k1.txt" "$TAG" > "$OUT/gates_k1.log" 2>&1; echo "k1 rc=$?"
  python -X utf8 -I "$S/c116k1/g1_split.py" "$INS" "$SB" "$SE" > "$OUT/g1_split.txt" 2>&1
  if [ "$SB" != 1 ]; then
    python -X utf8 -I "$S/c116k1/gates_k1.py" "$OFF" "$SB" "$SE" "$OUT/gates_k1_flag0.txt" "${TAG}_z" > "$OUT/gates_k1_flag0.log" 2>&1
    python -X utf8 -I "$S/m1178/gs11_1178.py" k1 "$OUT/gates_k1.txt" "$OUT/gates_k1_flag0.txt" > "$OUT/gates_k1_s11.txt" 2>&1; echo "k1 s11 rc=$?"
  fi ;;
esac
case ",$GS," in *,hd,*)
  python -X utf8 -I "$S/c116hd/hd.py" paper "$INS" "$SB" "$SE" "$OUT/paper_hd.json" > "$OUT/paper_hd.txt" 2>&1; echo "hd paper rc=$?"
  if [ "$SB" = 1 ]; then
    python -X utf8 -I "$S/c116hd/hd.py" gates "$OUT/paper_hd.json" $P/paper_M.json $P/paper_N1.json $P/paper_N2.json $P/paper_F1.json $P/paper_F2.json > "$OUT/gates_hd.txt" 2>&1; echo "hd rc=$?"
  else
    python -X utf8 -I "$S/c116hd/hd.py" paper "$OFF" "$SB" "$SE" "$OUT/paper_hd_flag0.json" > "$OUT/paper_hd_flag0.txt" 2>&1
    python -X utf8 -I "$S/m1178/gs11_1178.py" hd "$OUT/paper_hd.json" "$OUT/paper_hd_flag0.json" $P/paper_M.json $P/paper_N1.json $P/paper_N2.json $P/paper_F1.json $P/paper_F2.json > "$OUT/gates_hd_s11.txt" 2>&1; echo "hd s11 rc=$?"
  fi ;;
esac
python -X utf8 -I $S/m1017/popjump1017.py B=$INS base=$BASE N1=$K/n1 N2=$K/n2 > "$OUT/popjump.txt" 2>&1; echo "popjump rc=$?"
bash "$S/c92gf/stoplines1002.sh" "$INS" "$OUT/stop1002.txt" "$SB" > "$OUT/stop1002.log" 2>&1; echo "stop1002 rc=$?"
python -X utf8 "$S/hol/an_h2.py" "$INS" -v4 > "$OUT/hol_v4.txt" 2>&1; echo "hol rc=$?"
python -X utf8 -I "$S/m808/gdp_zero10.py" arm=$INS base=$BASE > "$OUT/gdp_zero10.txt" 2>&1; echo "gdpz rc=$?"
python -X utf8 -I "$S/brk693/plvl_seeds.py" "$INS" > "$OUT/plvl.txt" 2>&1; echo "plvl rc=$?"
python -X utf8 -I "$S/m975/ukdef975.py" "$INS" "$SB" > "$OUT/ukdef.txt" 2>&1; echo "ukdef rc=$?"
if [ "$CARD" != "-" ]; then
  python -X utf8 "$S/tools/nullgate.py" "$CARD" "$R/measurements/b10_20261116_m1171_ins" "$R/measurements/b10_20261116_m1171_n1_ins" "$R/measurements/b10_20261116_m1171_n2_ins" > "$OUT/nullgate.txt" 2>&1; echo "nullgate rc=$?"
  [ -f "$CARD.cost" ] && grep -h "推定" "$CARD.cost" > "$OUT/cost.txt" 2>&1
fi
python -X utf8 -I "$S/brk693/home_break.py" arm=$INS base=$BASE n1=$K/n1 n2=$K/n2 --test arm > "$OUT/home_break.txt" 2>&1; echo "home rc=$?"
bash "$S/ng.sh" "$INS" "$TAG" "$OUT/ng" rd:$R/measurements/ng1171_20261116:m1171 > "$OUT/ng.txt" 2>&1; echo "ng rc=$?"
echo READDONE
