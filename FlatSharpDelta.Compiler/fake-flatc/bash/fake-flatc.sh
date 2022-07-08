#!/bin/bash

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

for last; do true; done

cp "$last" -t "$o"