#!/bin/bash
# sudo ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/evince.sh /usr/local/bin/pdfe

# Запускаем программу от имени пользователя 'evince' с оставшимися параметрами
sudo -u evince bash -lc 'evince -- "$@"' -- "$@"
#/usr/bin/evince "$@"
