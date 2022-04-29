#!/bin/bash

MY_DIR=$(dirname $(readlink -f $0))
BACKUP_LEVEL=$1
TIDY_LEVEL=$2

CONFIG_LOC="$MY_DIR/app_config.sh"
if [[ -f $CONFIG_LOC ]]; then
    echo "$0: Loading Configuration"
    source $CONFIG_LOC
fi

add_job () {
    case "$1" in
        "1") SCHEDULE_STRING="0 0 * * *" ;;
        "2") SCHEDULE_STRING="0 0 * * 0" ;;
        "3") SCHEDULE_STRING="0 0 1,15 * *" ;;
        "4") SCHEDULE_STRING="0 0 1 * *" ;;
    esac

    if [ -n "$SCHEDULE_STRING" ]; then
        (crontab -l 2> /dev/null ; echo "$SCHEDULE_STRING $2") | crontab -
    fi
}

crontab -r 2> /dev/null

for path in ${BACKUP_PATHS[*]}; do
    add_job $1 "${MY_DIR}/backup.sh ${path}"
done
add_job $2 "${MY_DIR}/tools/TidyVault"
