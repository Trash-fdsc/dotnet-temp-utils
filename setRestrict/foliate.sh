#!/bin/bash
# sudo ln -s /Arcs/Repos/smalls/dotnet-temp-utils/setRestrict/foliate.sh /usr/local/bin/epub

# Запускаем программу от имени пользователя 'xreader' с оставшимися параметрами
export XAUTHORITY=
if [[ -z "$@" ]]
then
    sudo -u foliate XAUTHORITY= /usr/bin/foliate
else
    sudo -u foliate XAUTHORITY= /usr/bin/foliate "$@"
fi

