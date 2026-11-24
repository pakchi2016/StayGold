BEGIN {
    FS = ","
    OFS = ","
}

# 日時文字列を "YYYYMMDDHHMMSS" の14桁に正規化する関数
function normalize_dt(str,    a, n) {
    n = split(str, a, /[^0-9]+/)
    if (n < 6) return ""
    return sprintf("%04d%02d%02d%02d%02d%02d", a[1], a[2], a[3], a[4], a[5], a[6])
}

# Windows由来の改行コード(CRLF)対策
{
    sub(/\r$/, "")
}

# 1つ目のファイル：監査用データの読み込み
NR == FNR {
    # ヘッダー行がある場合はスキップ（不要なら削除）
    if (FNR == 1) next

    acc = $1
    cnt[acc]++
    idx = cnt[acc]

    audit_ip[acc, idx]    = $2
    audit_host[acc, idx]  = $3
    audit_start[acc, idx] = normalize_dt($4)
    audit_end[acc, idx]   = normalize_dt($5)
    audit_id[acc, idx]    = $6  # 申請IDを保持
    next
}

# 2つ目のファイル：テキストログの突合
{
    # ヘッダー行がある場合は見出しを追加して出力（不要なら削除）
    if (FNR == 1) {
        print $0, "監査結果", "申請ID"
        next
    }

    log_term = $5
    log_acc  = $6
    log_dt   = normalize_dt($1 " " $2 " " $3 " " $4)

    matched = 0
    matched_id = ""

    # 該当アカウントの申請レコードをすべてチェック
    if (log_acc in cnt) {
        for (i = 1; i <= cnt[log_acc]; i++) {
            term_ok = ((audit_ip[log_acc, i] != "" && log_term == audit_ip[log_acc, i]) ||
                       (audit_host[log_acc, i] != "" && log_term == audit_host[log_acc, i]))

            time_ok = (log_dt != "" &&
                       log_dt >= audit_start[log_acc, i] &&
                       log_dt <= audit_end[log_acc, i])

            if (term_ok && time_ok) {
                matched = 1
                matched_id = audit_id[log_acc, i]
                break
            }
        }
    }

    # 一致した場合のみ行末に「,〇,申請ID」を付与して出力
    if (matched) {
        print $0, "〇", matched_id
    } else {
        print $0
    }
}
