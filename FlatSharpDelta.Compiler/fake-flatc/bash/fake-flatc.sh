#!/bin/bash

# Make sure this file uses LF line endings.

next=0
for arg in "$@"
do
    if [ $next = 1 ]; then
      next=0
      o=$arg
    fi
    if [ "$arg" = "-o" ]; then
      next=1
    fi
done

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
find $SCRIPT_DIR -maxdepth 1 -name '*.bfbs' -exec cp -p '{}' "$o" ';'