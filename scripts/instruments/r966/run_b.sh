cd /d/PoB_work/r966_work
python -X utf8 -I D:/PoB_work/r966_work/r966_ef.py D:/PoB_work/r966_work/m947 b947=D:/PoB_work/keep_947/base_ins:1:10 n1_947=D:/PoB_work/keep_947/n1:1:10 n2_947=D:/PoB_work/keep_947/n2:1:10 a11_947=D:/PoB_work/keep_947/a11:11:20 || exit 1
python -X utf8 -I D:/PoB_work/r966_work/r966_chain.py D:/PoB_work/r966_work/m947_pairs.tsv D:/PoB_work/r966_work/m947_chain.tsv b947=D:/PoB_work/keep_947/base_ins n1_947=D:/PoB_work/keep_947/n1 n2_947=D:/PoB_work/keep_947/n2 a11_947=D:/PoB_work/keep_947/a11 || exit 1
python -X utf8 -I D:/PoB_work/r966_work/r966_chain.py --case D:/PoB_work/keep_947/n1 6 ハイデラバード 300 600 4 D:/PoB_work/r966_work/case_n1_947_hyderabad.tsv || exit 1
python -X utf8 -I D:/PoB_work/r966_work/r966_chain.py --case D:/PoB_work/keep_947/n2 5 共和制スペイン 180 667 6 D:/PoB_work/r966_work/case_n2_947_s5_rep_spain.tsv
