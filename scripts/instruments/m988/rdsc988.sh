#!/usr/bin/env bash
# (988) c89dd の門を種 11〜20 で読む（予言の追記 [8]）：CEARN=1 の合わせた木 mSc と腕 Z の木 zSc を同じ台本で数え、judge984.py で 1 回だけ判定・生の比べ 2 本。
export PYTHONUTF8=1 PYTHONIOENCODING=utf-8
cd D:/PoB_work/wt_m988 || exit 9
W=D:/PoB_work/m988_work; R=$W/read/sc; S=scripts/instruments; mkdir -p $R
python -X utf8 -I $S/c89dd/gate984.py mSc $W/out_mSc_10_s11/ins --seeds 11 20 > $R/gate_mSc.txt 2> $R/gate_mSc.log; echo "gate mSc rc=$?"
python -X utf8 -I $S/c89dd/gate984.py zSc $W/out_zSc_10_s11/ins --seeds 11 20 > $R/gate_zSc.txt 2> $R/gate_zSc.log; echo "gate zSc rc=$?"
python -X utf8 -I $S/c89dd/judge984.py $R/gate_mSc.txt $R/gate_zSc.txt > $R/judge.txt 2>&1; echo "judge rc=$?"
python -X utf8 -I $S/m981/rawcmp.py $W/out_mSc_10_s11/ins $W/out_mS_10_s11/ins > $R/rawcmp_mSc_vs_mS.txt 2>&1; echo "rawcmp1 rc=$?"
python -X utf8 -I $S/m981/rawcmp.py $W/out_zSc_10_s11/ins D:/PoB_work/keep_981/a11 > $R/rawcmp_zSc_vs_a11.txt 2>&1; echo "rawcmp2 rc=$?"
echo RDSCDONE
